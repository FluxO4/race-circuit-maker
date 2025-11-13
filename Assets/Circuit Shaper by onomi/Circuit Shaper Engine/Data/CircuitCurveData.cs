using System.Collections.Generic;

namespace OnomiCircuitShaper.Engine.Data
{
    /// <summary>
    /// A specialized curve type used for the main circuit paths.
    /// Inherits the basic spline storage from <see cref="CurveData"/> so higher
    /// layers can rely on a distinct type for circuit-specific behaviour later.
    /// </summary>
    [System.Serializable]
    public class CircuitCurveData : CurveData<CircuitPointData>
    {
        // Reserved for circuit-specific properties in the future.
    }
}
