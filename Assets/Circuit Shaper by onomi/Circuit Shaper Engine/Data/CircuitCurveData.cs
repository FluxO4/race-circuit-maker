using System.Collections.Generic;

namespace OnomiCircuitShaper.Engine.Data
{
    /// <summary>
    /// A specialized curve type used for the main circuit paths. It inherits the basic
    /// spline storage from <see cref="CurveData{TPoint}"/>, using <see cref="CircuitPointData"/>
    /// as its point type. This distinction allows higher-level code to identify and handle
    /// main circuit splines specifically, as opposed to other curve types like cross-sections.
    /// </summary>
    [System.Serializable]
    public class CircuitCurveData : CurveData<CircuitPointData>
    {
        // This class is currently a placeholder for any future properties that are
        // specific to a main circuit path, such as track-wide properties or metadata.
    }
}
