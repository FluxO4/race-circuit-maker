using OnomiCircuitShaper.Engine.EditRealm;
using System.Numerics;

namespace OnomiCircuitShaper.Engine.Processors
{
    /// <summary>
    /// A static class containing logic for processing and modifying Curve objects.
    /// </summary>
    public static class CurveProcessor
    {
        /// <summary>
        /// Iterates through all points in a curve and applies the auto-set logic for their control points.
        /// </summary>
        public static void AutoSetAllControlPoints(CircuitCurve curve)
        {
            // To be implemented.
        }

        /// <summary>
        /// Calculates the total length of the curve and assigns a normalized position (0 to 1)
        /// to each point along the curve's length.
        /// </summary>
        public static void NormaliseCurvePoints(CircuitCurve curve)
        {
            // To be implemented.
        }

        /// <summary>
        /// Gets a point at a normalized distance along the entire curve.
        /// </summary>
        public static Vector3 LerpAlongCurve(CircuitCurve curve, float value01)
        {
            // To be implemented.
            return Vector3.Zero;
        }
    }
}
