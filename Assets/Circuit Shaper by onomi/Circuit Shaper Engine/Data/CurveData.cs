using System.Collections.Generic;

namespace OnomiCircuitShaper.Engine.Data
{
    /// <summary>
    /// Represents the data for a single, continuous Bézier spline.
    /// A curve is defined by a list of points and whether it forms a closed loop.
    /// </summary>
    [System.Serializable]
    public class CurveData
    {
        /// <summary>
        /// The list of points that make up this curve. The order of points in this list
        /// defines the path of the spline.
        /// </summary>
        public List<PointData> CurvePoints = new List<PointData>();

        /// <summary>
        /// If true, the last point in the curve will connect back to the first point,
        /// forming a continuous, closed loop. If false, the curve has a distinct start and end.
        /// </summary>
        public bool IsClosed = true;
    }
}
