using System.Collections.Generic;

namespace OnomiCircuitShaper.Engine.Data
{
    /// <summary>
    /// A specialized curve type used for cross-section profiles (road cross sections).
    /// Inherits from <see cref="CurveData"/> so the data model remains compatible,
    /// but gives a distinct type for clearer semantics in higher layers.
    /// </summary>
    [System.Serializable]
    public class CrossSectionCurveData : CurveData<CrossSectionPointData>
    {
        // Reserved for cross-section-specific properties in the future.
    }
}
