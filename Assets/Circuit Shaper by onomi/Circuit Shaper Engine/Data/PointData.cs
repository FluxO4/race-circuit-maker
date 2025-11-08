using System.Numerics;

namespace OnomiCircuitShaper.Engine.Data
{
    /// <summary>
    /// The base class for any point in 3D space that is part of a Bézier curve.
    /// It stores the core positional data for an anchor point and its two control points,
    /// which together define the shape of the curve segments connected to it.
    /// </summary>
    [System.Serializable]
    public class PointData
    {
        /// <summary>
        /// The main position of the anchor point in 3D space.
        /// </summary>
        public Vector3 PointPosition;

        /// <summary>
        /// The position of the "forward" control point. This handle influences the
        /// shape of the curve segment leading to the *next* anchor point.
        /// </summary>
        public Vector3 ForwardControlPointPosition;

        /// <summary>
        /// The position of the "backward" control point. This handle influences the
        /// shape of the curve segment coming from the *previous* anchor point.
        /// </summary>
        public Vector3 BackwardControlPointPosition;

        /// <summary>
        /// Defines the "up" direction at this specific point. This is crucial for
        /// controlling the orientation (or "roll") of the track, allowing for banked corners.
        /// </summary>
        public Vector3 UpDirection = Vector3.UnitY;
    }
}
