using System;
using System.Numerics;
using OnomiCircuitShaper.Engine.Data;

namespace OnomiCircuitShaper.Engine.EditRealm
{
    /// <summary>
    /// Represents a single editable point in 3D space within the circuit shaper engine.
    /// Points are used to define the geometry of curves and roads. This class provides
    /// lightweight helpers and an event for consumers to react to edits.
    /// </summary>
    [Serializable]
    public class Point
    {
        /// <summary>
        /// Editor and circuit settings (shared reference)
        /// </summary>
        public CircuitAndEditorSettings Settings;

        /// <summary>
        /// Backing data object that holds persistent values for this point.
        /// </summary>
        public PointData Data = new PointData();

        /// <summary>
        /// Index within the parent curve (if applicable).
        /// </summary>
        public int PointIndex;

        /// <summary>
        /// Links to neighbour points (set by the owning Curve/Circuit)
        /// </summary>
        public Point NextPoint;
        public Point PreviousPoint;

        /// <summary>
        /// Fired whenever this point's state changes. Subscribers receive the point instance.
        /// </summary>
        public event Action<Point> PointStateChanged;

        /// <summary>
        /// Raises the PointStateChanged event.
        /// </summary>
        protected void OnPointStateChanged()
        {
            PointStateChanged?.Invoke(this);
        }

        /// <summary>
        /// The normalised right/across vector for this point (cross of forward and up).
        /// </summary>
        public Vector3 GetRightVector
        {
            get
            {
                var forward = GetForwardVector;
                var up = GetUpVector;
                var right = Vector3.Cross(forward, up);
                if (right.LengthSquared() < 1e-6f) return Vector3.UnitX;
                return Vector3.Normalize(right);
            }
        }

        /// <summary>
        /// The forward tangent direction for the point. Calculated as the average
        /// of the (forward control - point) and (point - backward control) directions.
        /// </summary>
        public Vector3 GetForwardVector
        {
            get
            {
                var a = Data.ForwardControlPointPosition - Data.PointPosition;
                var b = Data.PointPosition - Data.BackwardControlPointPosition;
                if (a.LengthSquared() < 1e-6f && b.LengthSquared() < 1e-6f)
                    return Vector3.UnitZ;

                var na = a.LengthSquared() < 1e-6f ? Vector3.Zero : Vector3.Normalize(a);
                var nb = b.LengthSquared() < 1e-6f ? Vector3.Zero : Vector3.Normalize(b);
                var avg = na + nb;
                if (avg.LengthSquared() < 1e-6f)
                    return Vector3.UnitZ;
                return Vector3.Normalize(avg);
            }
        }

        /// <summary>
        /// The up direction for this point from the stored data (normalised).
        /// </summary>
        public Vector3 GetUpVector =>
            Data.UpDirection.LengthSquared() < 1e-6f ? Vector3.UnitY : Vector3.Normalize(Data.UpDirection);

        /// <summary>
        /// Convenience accessors into the backing PointData.
        /// </summary>
        public Vector3 PointPosition => Data.PointPosition;
        public Vector3 ForwardControlPointPosition => Data.ForwardControlPointPosition;
        public Vector3 BackwardControlPointPosition => Data.BackwardControlPointPosition;

        /// <summary>
        /// A rotator handle position some units above the point along the up vector.
        /// </summary>
        // Use a default rotator distance if not provided by settings implementation.
        private const float DefaultRotatorDistance = 1f;
        public Vector3 RotatorPointPosition => PointPosition + GetUpVector * DefaultRotatorDistance;

        /// <summary>
        /// Called to auto-set control points for this point. Default implementation
        /// simply raises the changed event; concrete processors may call into
        /// CircuitPointProcessor or CurveProcessor instead.
        /// </summary>
        public virtual void AutoSetControlpoints()
        {
            // Default: notify listeners. More advanced algorithms live in processor classes.
            OnPointStateChanged();
        }
    }
}