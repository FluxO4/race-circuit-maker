namespace OnomiCircuitShaper.Engine.Data
{
    /// <summary>
    /// Represents a specialized point that lies on the main circuit path spline.
    /// In addition to its positional data inherited from <see cref="PointData"/>,
    /// a <see cref="CircuitPointData"/> holds a reference to its own cross-section curve,
    /// which defines the shape of the road at that specific point.
    /// </summary>
    [System.Serializable]
    public class CircuitPointData : PointData
    {
        /// <summary>
        /// The curve that defines the 2D profile or cross-section of the road surface
        /// at this specific point along the main track spline. This allows for variable
        /// road shapes (e.g., banked corners, wider sections). Uses the specialized
        /// <see cref="CrossSectionCurveData"/> type for clearer semantics.
        /// </summary>
        public CrossSectionCurveData CrossSectionCurve = new CrossSectionCurveData();
    }
}
