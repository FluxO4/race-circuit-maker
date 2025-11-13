using OnomiCircuitShaper.Engine.Data;
using System.Numerics;
using OnomiCircuitShaper.Engine.Processors;
using System.Collections.Generic;


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

        public event System.Action CurveStateChanged;

        public bool IsClosed
        {
            get => Data.IsClosed;
            set
            {
                if (Data.IsClosed == value) return;
                Data.IsClosed = value;
                // Logic to update first/last point neighbors will be here or in a processor.
                OnCurveStateChanged();
            }
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
            

            CircuitPoint newPoint = new CircuitPoint(this, newPointData, Settings, null);
            // Insert into the live points list at the same index to keep ordering consistent
            if (index < 0) index = 0;
            if (index > Points.Count) index = Points.Count;

            Data.CurvePoints.Insert(index, newPointData);
            Points.Insert(index, newPoint);
            newPoint.AutoSetControlpoints();
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
        /// Removes a point from the curve and updates all neighboring points and curve properties.
        /// </summary>

        public void RemovePoint(CircuitPoint point)
        {
            if (Points.Contains(point))
            {

                int index = Points.IndexOf(point);
                Points.RemoveAt(index);
                Data.CurvePoints.RemoveAt(index);
            }
            
            OnCurveStateChanged();
        }

        // constructor
        public CircuitCurve(CircuitCurveData data, CircuitAndEditorSettings settings)
        {
            Data = data;
            Settings = settings;

            // Create live CircuitPoint objects for each data point
            for(int i = 0; i < Data.CurvePoints.Count; i++)
            {
                CircuitPointData pointData = Data.CurvePoints[i];
                CircuitPoint circuitPoint = new CircuitPoint(this, pointData, Settings, null);
                Points.Add(circuitPoint);
            }
        }

        public void AutoSetAllControlPoints()
        {
            // Implementation may be provided by processors or overridden by derived classes.
        }

        protected void OnCurveStateChanged()
        {
            CurveStateChanged?.Invoke();
        }
    }
}
