using System.Collections.Generic;

namespace OnomiCircuitShaper.Engine.Data
{


    [System.Serializable]
    public abstract class CurveData<TPoint> where TPoint : PointData
    {
        /// <summary>
        /// If true, the last point in the curve will connect back to the first point,
        /// forming a continuous, closed loop. If false, the curve has a distinct start and end.
        /// </summary>
        public bool IsClosed = true;

        /// <summary>
        /// The list of points that make up this curve. The order of points in this list
        /// defines the path of the spline.
        /// </summary>
        public List<TPoint> CurvePoints = new List<TPoint>();
    }
}
