using OnomiCircuitShaper.Engine.Data;
using System.Numerics;
using OnomiCircuitShaper.Engine.Processors;
using System.Collections.Generic;
using OnomiCircuitShaper.Engine.Interface;
using System;
using System.Linq;


namespace OnomiCircuitShaper.Engine.EditRealm
{
    /// <summary>
    /// Represents a live, editable curve that defines the main path of the circuit.
    /// </summary>
    public class CircuitCurve
    {

        /// <summary>
        /// Gets or sets whether the curve is a closed loop. When set, it will
        /// update the neighbor references of the first and last points.
        /// </summary>

        public CircuitCurveData Data { get; private set; }
        public CircuitAndEditorSettings Settings { get; private set; }
        public List<CircuitPoint> Points { get; private set; } = new List<CircuitPoint>();
        public List<Road> Roads { get; private set; } = new List<Road>();

        public event System.Action CurveStateChanged;

        public bool IsClosed
        {
            get => Data.IsClosed;
            set
            {
                if (Data.IsClosed == value) return;
                Data.IsClosed = value;
                // Logic to update first/last point neighbors will be here or in a processor.
                UpdateNeighborReferences();
                OnCurveStateChanged();
            }
        }


        // constructor
        public CircuitCurve(CircuitCurveData data, CircuitAndEditorSettings settings)
        {
            Data = data;
            Settings = settings;

            // Create live CircuitPoint objects for each data point
            for (int i = 0; i < Data.CurvePoints.Count; i++)
            {
                CircuitPointData pointData = Data.CurvePoints[i];
                CircuitPoint circuitPoint = new CircuitPoint(this, pointData, Settings);
                Points.Add(circuitPoint);
                //Subscribe to point change event
            }

            for (int i = 0; i < Data.Roads.Count; i++)
            {
                RoadData roadData = Data.Roads[i];
                Road road = new Road(roadData, Settings, this);
                UnityEngine.Debug.Log("Created road from data with range: " + roadData.startSegmentIndex + " to " + roadData.endSegmentIndex + " with parent " + this);
                Roads.Add(road);
            }
            
            UnityEngine.Debug.Log("Initialized CircuitCurve with " + Points.Count + " points and " + Roads.Count + " roads.");

            UpdateNeighborReferences();
            NormalisePointPositionAlongCurve();
            ResolveRoadClashes();
        }

        
        // Convert segment range to point array
        public List<CircuitPoint> GetPointsFromSegmentRange(int startSeg, int endSeg)
        {
            // Segment N uses points N and N+1
            // So we need points from startSeg to (endSeg+1)
            List<CircuitPoint> points = new List<CircuitPoint>();
            points.Add(Points[startSeg]);
            
            int pos = startSeg;
            while (pos != endSeg)
            {
                pos = (pos + 1) % Points.Count;
                points.Add(Points[pos]);
            }
            points.Add(Points[(endSeg + 1) % Points.Count]); // End point of last segment
            
            return points;
        }



        /// <summary>
        /// Creates a new point, inserts it into the curve at a specific index,
        /// and updates all neighboring points and curve properties.
        /// </summary>
        public CircuitPoint AddPointAtIndex(Vector3 pointPosition, int index)
        {

            CircuitPointData newPointData = new CircuitPointData()
            {
                PointPosition = pointPosition
            };
            

            CircuitPoint newPoint = new CircuitPoint(this, newPointData, Settings);
            // Insert into the live points list at the same index to keep ordering consistent
            if (index < 0) index = 0;
            if (index > Points.Count) index = Points.Count;

            Data.CurvePoints.Insert(index, newPointData);
            Points.Insert(index, newPoint);

            foreach (var road in Roads)
            {
                bool roadAltered = false;
                // If insertion is before or at startSegment, shift start
                if (index <= road.Data.startSegmentIndex)
                {
                    road.Data.startSegmentIndex++;
                    //Mark road dirty for rebuild
                    roadAltered = true;
                    
                }

                // If insertion is before or at endSegment, shift end  
                if (index <= road.Data.endSegmentIndex)
                {
                    road.Data.endSegmentIndex++;
                    //Mark road dirty for rebuild
                    roadAltered = true;
                }

                if (roadAltered)
                {
                    RoadRebuildQueue.MarkDirty(road);
                }
            }
            
    



            UpdateNeighborReferences();
            newPoint.AutoSetControlPoints();
            NormalisePointPositionAlongCurve();
            
            OnCurveStateChanged();
            return newPoint;
        }

        /// <summary>
        /// Finds the two closest consecutive points on the curve to the given position
        /// and inserts a new point there.
        /// </summary>
        public CircuitPoint AddPointOnCurve(Vector3 position)
        {
            // Find the closest segment, find the index to be inserted to and insert the point there.
            int closestSegmentIndex = -1;
            float closestDistanceSqr = float.MaxValue;
            for (int i = 0; i < Data.CurvePoints.Count - 1 + (Data.IsClosed ? 1 : 0); i++)
            {
                CircuitPointData p1 = Data.CurvePoints[i];
                CircuitPointData p2 = Data.CurvePoints[(i + 1) % Data.CurvePoints.Count];

                Vector3 segmentStart = p1.PointPosition;
                Vector3 segmentEnd = p2.PointPosition;

                Vector3 projectedPoint = ProjectPointOnSegment(position, segmentStart, segmentEnd);
                float distanceSqr = Vector3.DistanceSquared(position, projectedPoint);

                if (distanceSqr < closestDistanceSqr)
                {
                    closestDistanceSqr = distanceSqr;
                    closestSegmentIndex = i;
                }
            }

            if (closestSegmentIndex != -1)
            {
                return AddPointAtIndex(position, closestSegmentIndex + 1);
            }
            return null;
        }

        // Add point based on camera position and ray direction
        public CircuitPoint AddPointOnCurve(Vector3 cameraPosition, Vector3 cameraDirection)
        {
            // Go through every segment and track the minimum distance from the ray to the segment
            int closestSegmentIndex = -1;
            Vector3 pointToadd = Vector3.Zero;

            float closestDistanceSqr = float.MaxValue;
            for (int i = 0; i < Data.CurvePoints.Count - 1 + (Data.IsClosed ? 1 : 0); i++)
            {
                CircuitPointData p1 = Data.CurvePoints[i];
                CircuitPointData p2 = Data.CurvePoints[(i + 1) % Data.CurvePoints.Count];

                Vector3 segmentStart = p1.PointPosition;
                Vector3 segmentEnd = p2.PointPosition;

                // Find the closest points on the ray and the segment
                ClosestPointsOnRayAndSegment(
                    cameraPosition, cameraDirection,
                    segmentStart, segmentEnd,
                    out Vector3 closestPointOnRay,
                    out Vector3 closestPointOnSegment
                );


                float distanceSqr = Vector3.DistanceSquared(closestPointOnRay, closestPointOnSegment);
                if (distanceSqr < closestDistanceSqr)
                {
                    closestDistanceSqr = distanceSqr;
                    closestSegmentIndex = i;
                    pointToadd = closestPointOnSegment;
                }
            }

            return AddPointAtIndex(pointToadd, closestSegmentIndex + 1);
        }


        public Road AddRoadFromRange(int startIndex, int endIndex)
        {
            // Get the requested segment set
            HashSet<int> requestedSegments = GetSegmentSetFromEndPoints(startIndex, endIndex);
            
            // Subtract all existing road segments
            foreach (var road in Roads)
            {
                HashSet<int> existingSegments = GetSegmentSet(road.Data.startSegmentIndex, road.Data.endSegmentIndex, Points.Count);
                requestedSegments.ExceptWith(existingSegments);
            }
            
            // If nothing left, can't create road
            if (requestedSegments.Count == 0)
            {
                UnityEngine.Debug.LogWarning("Cannot create road: entire range overlaps with existing roads.");
                return null;
            }
            
            // Find all contiguous ranges in the remaining segments
            List<(int start, int end)> contiguousRanges = FindContiguousRanges(requestedSegments, Points.Count);
            
            // Create roads for each contiguous range (or just the longest one)
            // For now, let's create the longest contiguous range as a single road
            var longestRange = contiguousRanges.OrderByDescending(r => GetSegmentSet(r.start, r.end, Points.Count).Count).First();
            
            RoadData newRoadData = new RoadData()
            {
                startSegmentIndex = longestRange.start,
                endSegmentIndex = longestRange.end
            };

            Data.Roads.Add(newRoadData);
            Road newRoad = new Road(newRoadData, Settings, this);
            Roads.Add(newRoad);

            //Set dirty for rebuild
            RoadRebuildQueue.MarkDirty(newRoad);

            UnityEngine.Debug.Log($"Created road with segment range {longestRange.start} to {longestRange.end}");
            
            // Optionally: create additional roads for other contiguous ranges
            // You could return List<Road> instead and create multiple roads here
            
            return newRoad;
        }
        
        public void RemoveRoad(Road road)
        {
            if (Roads.Contains(road))
            {
                Roads.Remove(road);
                // Also remove from data
                if (Data != null && Data.Roads.Contains(road.Data))
                {
                    Data.Roads.Remove(road.Data);
                }
                // Optionally mark dirty for rebuild
                road.MarkedForDeletion = true;
                RoadRebuildQueue.MarkDirty(road);
            }
        }
        
        /// <summary>
        /// Attempts to set the start segment index for a road. Returns false if it would cause overlap.
        /// </summary>
        public bool TrySetRoadStartSegment(Road road, int newStartSegment)
        {
            if (!Roads.Contains(road)) return false;
            
            int oldStart = road.Data.startSegmentIndex;
            
            // Temporarily set new value to check for overlaps
            road.Data.startSegmentIndex = newStartSegment;
            
            // Check if new range overlaps with any other road
            foreach (var otherRoad in Roads)
            {
                if (otherRoad == road) continue;
                
                if (DoSegmentRangesOverlap(
                    road.Data.startSegmentIndex, road.Data.endSegmentIndex,
                    otherRoad.Data.startSegmentIndex, otherRoad.Data.endSegmentIndex,
                    Points.Count))
                {
                    // Revert and return false
                    road.Data.startSegmentIndex = oldStart;
                    return false;
                }
            }
            
            // No overlap, change is valid
            RoadRebuildQueue.MarkDirty(road);
            return true;
        }
        
        /// <summary>
        /// Attempts to set the end segment index for a road. Returns false if it would cause overlap.
        /// </summary>
        public bool TrySetRoadEndSegment(Road road, int newEndSegment)
        {
            if (!Roads.Contains(road)) return false;
            
            int oldEnd = road.Data.endSegmentIndex;
            
            // Temporarily set new value to check for overlaps
            road.Data.endSegmentIndex = newEndSegment;
            
            // Check if new range overlaps with any other road
            foreach (var otherRoad in Roads)
            {
                if (otherRoad == road) continue;
                
                if (DoSegmentRangesOverlap(
                    road.Data.startSegmentIndex, road.Data.endSegmentIndex,
                    otherRoad.Data.startSegmentIndex, otherRoad.Data.endSegmentIndex,
                    Points.Count))
                {
                    // Revert and return false
                    road.Data.endSegmentIndex = oldEnd;
                    return false;
                }
            }
            
            // No overlap, change is valid
            RoadRebuildQueue.MarkDirty(road);
            return true;
        }

        /// <summary>
        /// Projects point p onto the segment ab and returns the closest point on the segment.
        /// </summary>
        private static Vector3 ProjectPointOnSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            var ab = b - a;
            var abLenSq = ab.LengthSquared();
            if (abLenSq < 1e-9f) return a;
            var ap = p - a;
            var t = Vector3.Dot(ap, ab) / abLenSq;
            if (t <= 0f) return a;
            if (t >= 1f) return b;
            return a + ab * t;
        }


        /// <summary>
        /// Calculates the closest points between a ray and a line segment.
        /// </summary>
        /// <param name="rayOrigin">The origin of the ray.</param>
        /// <param name="rayDirection">The direction of the ray (must be normalized).</param>
        /// <param name="segmentStart">The start point of the segment.</param>
        /// <param name="segmentEnd">The end point of the segment.</param>
        /// <param name="closestPointOnRay">The resulting closest point on the ray.</param>
        /// <param name="closestPointOnSegment">The resulting closest point on the segment.</param>
        private static void ClosestPointsOnRayAndSegment(Vector3 rayOrigin, Vector3 rayDirection, Vector3 segmentStart, Vector3 segmentEnd, out Vector3 closestPointOnRay, out Vector3 closestPointOnSegment)
        {
            Vector3 segmentDirection = segmentEnd - segmentStart;
            Vector3 w0 = rayOrigin - segmentStart;

            float a = Vector3.Dot(rayDirection, rayDirection);
            float b = Vector3.Dot(rayDirection, segmentDirection);
            float c = Vector3.Dot(segmentDirection, segmentDirection);
            float d = Vector3.Dot(rayDirection, w0);
            float e = Vector3.Dot(segmentDirection, w0);

            float denom = a * c - b * b;

            float rayT, segmentT;

            // If the lines are parallel, find the closest point on the segment to the ray's origin
            if (Math.Abs(denom) < 1e-5f)
            {
                rayT = 0; // We can pick any point on the ray, so we choose the origin.
                segmentT = Math.Clamp(-e / c, 0, 1);
            }
            else
            {
                // General case for non-parallel lines
                rayT = (b * e - c * d) / denom;
                segmentT = (a * e - b * d) / denom;

                // Clamp parameters to the bounds of the ray (t>=0) and segment (0<=t<=1)
                rayT = Math.Max(rayT, 0);
                segmentT = Math.Clamp(segmentT, 0, 1);
            }

            closestPointOnRay = rayOrigin + rayDirection * rayT;
            closestPointOnSegment = segmentStart + segmentDirection * segmentT;
        }



        /// <summary>
        /// Removes a point from the curve and updates all neighboring points and curve properties.
        /// </summary>

        public void RemovePoint(CircuitPoint point)
        {
            if (Points.Contains(point))
            {

                int index = Points.IndexOf(point);
                Points.RemoveAt(index);
                Data.CurvePoints.RemoveAt(index);

                // Road ranges update
                foreach (var road in Roads)
                {
                    // If removal is before startSegment, shift start
                    if (index <= road.Data.startSegmentIndex)
                        road.Data.startSegmentIndex = (road.Data.startSegmentIndex - 1) % Points.Count;


                    // If removal is before endSegment, shift end  
                    if (index <= road.Data.endSegmentIndex)
                        road.Data.endSegmentIndex = (road.Data.endSegmentIndex - 1) % Points.Count;
                }

                //Check for any road clashes and resolve them

                ResolveRoadClashes();
                //mark all roads dirty for rebuild
                foreach (var road in Roads)
                {
                    RoadRebuildQueue.MarkDirty(road);
                }
                
                UpdateNeighborReferences();
            }

            OnCurveStateChanged();
        }
        

        private void ResolveRoadClashes()
        {
           
            //Strategy, first find intersections
            List<(Road, Road)> overlappingRoads = new List<(Road, Road)>();
            for (int i = 0; i < Roads.Count; i++)
            {
                for (int j = i + 1; j < Roads.Count; j++)
                {
                    if (DoSegmentRangesOverlap(
                        Roads[i].Data.startSegmentIndex, Roads[i].Data.endSegmentIndex,
                        Roads[j].Data.startSegmentIndex, Roads[j].Data.endSegmentIndex,
                        Points.Count))
                    {
                        overlappingRoads.Add((Roads[i], Roads[j]));
                    }
                }
            }

            //go through each overlappng pair and resolve by subtracting the intersection from one of them
            foreach (var (roadA, roadB) in overlappingRoads)
            {
                //get segment sets
                HashSet<int> segmentsA = GetSegmentSet(roadA.Data.startSegmentIndex, roadA.Data.endSegmentIndex, Points.Count);
                HashSet<int> segmentsB = GetSegmentSet(roadB.Data.startSegmentIndex, roadB.Data.endSegmentIndex, Points.Count);

                //find intersection
                HashSet<int> intersection = new HashSet<int>(segmentsA);
                intersection.IntersectWith(segmentsB);

                //subtract intersection from roadB
                segmentsB.ExceptWith(intersection);

                //find contiguous ranges in remaining segments of roadB
                List<(int start, int end)> remainingRanges = FindContiguousRanges(segmentsB, Points.Count);

                if (remainingRanges.Count == 0)
                {
                    //no segments left, remove roadB
                    RemoveRoad(roadB);

                }
                else
                {
                    //set roadB to first remaining range
                    var firstRange = remainingRanges[0];
                    roadB.Data.startSegmentIndex = firstRange.start;
                    roadB.Data.endSegmentIndex = firstRange.end;
                    
                }
            }
        }


        private void UpdateNeighborReferences()
        {
            // Look through all points and correctly set their neighbor references
            for (int i = 0; i < Points.Count; i++)
            {
                CircuitPoint currentPoint = Points[i];
                CircuitPoint nextPoint = Points[(i + 1) % Points.Count];
                CircuitPoint previousPoint = Points[(i - 1 + Points.Count) % Points.Count];

                if (i == Points.Count - 1 && !Data.IsClosed)
                {
                    nextPoint = null;
                }
                if (i == 0 && !Data.IsClosed)
                {
                    previousPoint = null;
                }

                currentPoint.NextPoint = nextPoint;
                currentPoint.PreviousPoint = previousPoint;
            }
        }
        
        bool DoSegmentRangesOverlap(int start1, int end1, int start2, int end2, int totalSegments)
        {
            HashSet<int> range1 = GetSegmentSet(start1, end1, totalSegments);
            HashSet<int> range2 = GetSegmentSet(start2, end2, totalSegments);
            return range1.Overlaps(range2);
        }

        /// <summary>
        /// Converts a segment range (start, end) into a HashSet of all segment indices in that range.
        /// Handles wraparound for circular curves.
        /// </summary>
        HashSet<int> GetSegmentSet(int start, int end, int totalSegments)
        {
            HashSet<int> segments = new HashSet<int>();
            int pos = start;
            do
            {
                segments.Add(pos);
                if (pos == end) break;
                pos = (pos + 1) % totalSegments;
            } while (true);
            return segments;
        }

        
        HashSet<int> GetSegmentSetFromEndPoints(int startPointIndex, int endPointIndex)
        {
            HashSet<int> segments = new HashSet<int>();
            int pos = startPointIndex;
            
            while(pos != endPointIndex)
            {
                segments.Add(pos);
                pos = (pos + 1) % Points.Count;
            } 

            return segments;
        }
        
        /// <summary>
        /// Finds all contiguous segment ranges in a set of segments.
        /// Returns list of (startSegment, endSegment) tuples.
        /// </summary>
        List<(int start, int end)> FindContiguousRanges(HashSet<int> segments, int totalSegments)
        {
            if (segments.Count == 0) return new List<(int, int)>();
            
            List<(int start, int end)> ranges = new List<(int, int)>();
            List<int> sortedSegments = new List<int>(segments);
            sortedSegments.Sort();
            
            int rangeStart = sortedSegments[0];
            int rangeEnd = sortedSegments[0];
            
            for (int i = 1; i < sortedSegments.Count; i++)
            {
                // Check if current segment is contiguous with previous
                if (sortedSegments[i] == rangeEnd + 1)
                {
                    rangeEnd = sortedSegments[i];
                }
                else
                {
                    // End current range, start new one
                    ranges.Add((rangeStart, rangeEnd));
                    rangeStart = sortedSegments[i];
                    rangeEnd = sortedSegments[i];
                }
            }
            
            // Add final range
            ranges.Add((rangeStart, rangeEnd));
            
            // Check for wraparound: if first and last segments are adjacent across boundary
            if (ranges.Count > 1 && sortedSegments[0] == 0 && sortedSegments[^1] == totalSegments - 1)
            {
                // Merge first and last ranges
                var firstRange = ranges[0];
                var lastRange = ranges[^1];
                ranges.RemoveAt(ranges.Count - 1);
                ranges.RemoveAt(0);
                ranges.Add((lastRange.start, firstRange.end));
            }
            
            return ranges;
        }

        public void NormalisePointPositionAlongCurve()
        {
            // Implementation may be provided by processors or overridden by derived classes.
            CurveProcessor.NormaliseCurvePoints(Data);
        }
        
        public void HandlePointTransformed(Point<CircuitPointData> point)
        {
            NormalisePointPositionAlongCurve();
            OnCurveStateChanged();

            //find which roads are affected by this point and mark them dirty
            int pointIndex = Points.IndexOf((CircuitPoint)point);
            foreach (var road in Roads)
            {
                // Check if point is used by this road (remember: segment N uses points N and N+1)
                HashSet<int> roadSegments = GetSegmentSet(road.Data.startSegmentIndex, road.Data.endSegmentIndex, Points.Count);
                
                // Point affects segments (pointIndex-1) and (pointIndex)
                if (roadSegments.Contains(pointIndex) || 
                    roadSegments.Contains((pointIndex - 1 + Points.Count) % Points.Count))
                {
                    RoadRebuildQueue.MarkDirty(road);
                }
            }
        }

        public void AutoSetAllControlPoints()
        {
            // Call autoset function for each point in the cross-section, but do the end points last
            for (int i = 1; i < Points.Count - 1; i++)
            {
                Points[i].AutoSetControlPoints();
            }
            Points[0].AutoSetControlPoints();
            Points[^1].AutoSetControlPoints();
        }

        public void OnCurveStateChanged()
        {
            CurveStateChanged?.Invoke();
        }
    }
}
