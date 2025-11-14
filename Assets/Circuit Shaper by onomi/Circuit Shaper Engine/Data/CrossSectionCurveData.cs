using System.Collections.Generic;

namespace OnomiCircuitShaper.Engine.Data
{
    /// <summary>
    /// A specialized curve type used for defining the 2D profile of a road at a specific point
    /// (a cross-section). It inherits from <see cref="CurveData{TPoint}"/> with
    /// <see cref="CrossSectionPointData"/> as its point type. This provides a distinct type
    /// for clearer semantics in higher layers of the data model.
    /// </summary>
    [System.Serializable]
    public class CrossSectionCurveData : CurveData<CrossSectionPointData>
    {
        // This class is reserved for any future properties that might be specific
        // to cross-sections, such as material overrides or profile-specific metadata.
    }
}
