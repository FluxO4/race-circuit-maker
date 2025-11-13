using OnomiCircuitShaper.Engine.Data;
using System.Numerics;
using UnityEditor;

namespace OnomiCircuitShaper.Engine.EditRealm
{
    /// <summary>
    /// Represents a live, editable curve that defines the main path of the circuit.
    /// </summary>
    public class CircuitCurve : Curve<CircuitCurveData, CircuitPointData, CircuitPoint>
    {

        public CircuitCurveData Data;

        /// <summary>
        /// Gets or sets whether the curve is a closed loop. When set, it will
        /// update the neighbor references of the first and last points.
        /// </summary>
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
        public void AddPointAtIndex(Vector3 pointPosition, int index)
        {

            CircuitPointData newPointData = new CircuitPointData()
            {
                PointPosition = pointPosition
            };
            Data.CurvePoints.Insert(index, newPointData);
            CircuitPoint newPoint = new CircuitPoint(newPointData, Settings, null);
            Points[newPointData] = newPoint;
            newPoint.AutoSetControlpoints();
            
            OnCurveStateChanged();
        }

        /// <summary>
        /// Finds the two closest consecutive points on the curve to the given position
        /// and inserts a new point there.
        /// </summary>
        public void AddPointOnCurve(Vector3 position)
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
                AddPointAtIndex(position, closestSegmentIndex + 1);
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
        /// Finds the closest position on the curve to a given ray and inserts a new point there.
        /// </summary>
        public void AddPointOnCurve(Vector3 rayStart, Vector3 rayDirection)
        {
            // To be implemented.
        }

        /// <summary>
        /// Removes a point from the curve and updates all neighboring points and curve properties.
        /// </summary>

        public void RemovePoint(Point point)
        {
            // To be implemented.
            OnCurveStateChanged();
        }

        /// <summary>
        /// Constructor using raw data and settings.
        /// </summary>
        public CircuitCurve(CircuitCurveData data, CircuitAndEditorSettings settings) : base(settings)
        {
            Data = data;
        }
    }
}
