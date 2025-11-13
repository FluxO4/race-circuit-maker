using System.Collections.Generic;

namespace OnomiCircuitShaper.Engine.Data
{
    /// <summary>
    /// Represents the data for a single, continuous Bézier spline.
    /// This is a generic, strongly-typed base so concrete curve types can
    /// declare the exact PointData subtype they contain.
    /// </summary>
    [System.Serializable]
    public abstract class CurveData<TPoint> where TPoint : PointData
    {
        /// <summary>
        /// The list of points that make up this curve. The order of points in this list
        /// defines the path of the spline.
        /// </summary>
        public List<TPoint> CurvePoints = new List<TPoint>();

        /// <summary>
        /// If true, the last point in the curve will connect back to the first point,
        /// forming a continuous, closed loop. If false, the curve has a distinct start and end.
        /// </summary>
        public bool IsClosed = true;
    }
}
