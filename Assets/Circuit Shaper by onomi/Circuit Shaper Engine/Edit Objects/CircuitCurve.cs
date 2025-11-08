using OnomiCircuitShaper.Engine.Data;
using System.Numerics;

namespace OnomiCircuitShaper.Engine.EditRealm
{
    /// <summary>
    /// Represents a live, editable curve that defines the main path of the circuit.
    /// </summary>
    public class CircuitCurve : Curve
    {
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
            // To be implemented.
            OnCurveStateChanged();
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
    }

    /// <summary>
    /// Represents a live, editable cross-section curve. These are always open.
    /// </summary>
    public class CrossSectionCurve : Curve
    {
        /// <summary>
        /// Changes the number of points in the cross-section while attempting to preserve its shape
        /// by interpolating new point positions.
        /// </summary>
        public void ChangeCrossSectionPointCount(int newCount)
        {
            // To be implemented.
            OnCurveStateChanged();
        }

        /// <summary>
        /// A handler that listens for changes in its child points and automatically
        /// recalculates all control points to maintain a smooth shape.
        /// </summary>
        public void HandleCrossSectionPointChanged()
        {
            AutoSetAllControlPoints();
        }
    }
}
