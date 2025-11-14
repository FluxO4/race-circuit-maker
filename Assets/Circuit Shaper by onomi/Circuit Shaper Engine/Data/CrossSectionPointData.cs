namespace OnomiCircuitShaper.Engine.Data
{
    /// <summary>
    /// Represents a point that belongs to a cross-section curve.
    /// This is functionally identical to a base <see cref="PointData"/>, but the distinct type
    // helps to clarify its role within the data hierarchy. It defines a vertex
    /// on the 2D profile of the road, relative to a parent <see cref="CircuitPointData"/>.
    /// </summary>
    [System.Serializable]
    public class CrossSectionPointData : PointData
    {
        // Currently has no additional data beyond a standard PointData.
        // This class exists for structural clarity, type safety, and future extension.
        // For example, it could later hold data about UV coordinates or vertex colors
        // specific to the cross-section point.
    }
}
