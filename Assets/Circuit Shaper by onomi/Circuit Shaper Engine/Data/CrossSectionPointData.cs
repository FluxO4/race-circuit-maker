namespace OnomiCircuitShaper.Engine.Data
{
    /// <summary>
    /// Represents a point that belongs to a cross-section curve.
    /// This is functionally identical to a base PointData but the distinct type
    /// helps to clarify its role within the data hierarchy. It defines a vertex
    /// on the 2D profile of the road.
    /// </summary>
    [System.Serializable]
    public class CrossSectionPointData : PointData
    {
        // Currently has no additional data beyond a standard PointData.
        // This class exists for structural clarity and future extension.
    }
}
