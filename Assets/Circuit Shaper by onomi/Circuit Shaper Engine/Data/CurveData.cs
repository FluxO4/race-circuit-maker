using System.Collections.Generic;

namespace OnomiCircuitShaper.Engine.Data
{
    /// <summary>
    /// An abstract base class for a curve composed of points. This generic class
    /// provides the fundamental structure for a spline, including a list of points
    /// and a flag for whether the curve is closed.
    /// </summary>
    /// <typeparam name="TPoint">The type of point data this curve holds, which must inherit from <see cref="PointData"/>.</typeparam>
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
        /// defines the path of the spline from start to finish.
        /// </summary>
        public List<TPoint> CurvePoints = new List<TPoint>();
    }
}
