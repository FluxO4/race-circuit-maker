using OnomiCircuitShaper.Engine.Data;
using System.Numerics;
using OnomiCircuitShaper.Engine.Processors;
using System.Collections.Generic;
using System;


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
                circuitPoint.PointStateChanged += HandlePointTransformed;
            }

            for (int i = 0; i < Data.Roads.Count; i++)
            {
                RoadData roadData = Data.Roads[i];
                Road road = new Road(roadData, Settings, this);
                Roads.Add(road);
            }

            UpdateNeighborReferences();
            NormalisePointPositionAlongCurve();
        }
        
        /// <summary>
        /// Returns a list of CircuitPoints that are within the given range
        /// of indices, inclusive of both start and end
        /// </summary>
        public List<CircuitPoint> GetPointsInRange(int startIndex, int endIndex)
        {
            List<CircuitPoint> selectedPoints = new List<CircuitPoint>();

            int pos = startIndex;
            while(pos != endIndex)
            {
                selectedPoints.Add(Points[pos]);
                pos = (pos + 1) % Points.Count;
            }

            return selectedPoints;
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

            for (int i = 0; i < Roads.Count; i++)
            {
                Road road = Roads[i];

                if (road.Data.PointIndexRange.Item1 >= index) road.Data.PointIndexRange.Item1 = road.Data.PointIndexRange.Item1 + 1;
                if (road.Data.PointIndexRange.Item2 >= index) road.Data.PointIndexRange.Item2 = road.Data.PointIndexRange.Item2 + 1;
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

            //Check if range clashes with existing roads
            foreach (var road in Roads)
            {
                int roadStart = road.Data.PointIndexRange.Item1;
                int roadEnd = road.Data.PointIndexRange.Item2;

                if (!(endIndex < roadStart || startIndex > roadEnd))
                {
                    return null; // Clash detected
                }
            }


            RoadData newRoadData = new RoadData()
            {
                PointIndexRange = (startIndex, endIndex)
            };

            Data.Roads.Add(newRoadData);
            Road newRoad = new Road(newRoadData, Settings, this);
            Roads.Add(newRoad);

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
            }
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

                // Road ranges will need to be updated by the caller after adding/removing points, must shift indices after the removed point.
                for (int i = 0; i < Roads.Count; i++)
                {
                    Road road = Roads[i];
                    if (road.Data.PointIndexRange.Item1 > index) road.Data.PointIndexRange.Item1 = road.Data.PointIndexRange.Item1 - 1;
                    if (road.Data.PointIndexRange.Item2 > index) road.Data.PointIndexRange.Item2 = road.Data.PointIndexRange.Item2 - 1;
                   
                }

                //unsubscribe from point change event
                point.PointStateChanged -= HandlePointTransformed;
                UpdateNeighborReferences();
            }

            OnCurveStateChanged();
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

        public void NormalisePointPositionAlongCurve()
        {
            // Implementation may be provided by processors or overridden by derived classes.
            CurveProcessor.NormaliseCurvePoints(Data);
        }
        
        public void HandlePointTransformed(Point<CircuitPointData> point)
        {
            NormalisePointPositionAlongCurve();
            OnCurveStateChanged();

            //find which road if any this point belongs to and mark it dirty
            int pointIndex = Points.IndexOf((CircuitPoint)point);
            foreach (var road in Roads)
            {
                if (pointIndex >= road.Data.PointIndexRange.Item1 && pointIndex <= road.Data.PointIndexRange.Item2)
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

        protected void OnCurveStateChanged()
        {
            CurveStateChanged?.Invoke();
        }
    }
}
