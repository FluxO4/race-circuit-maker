namespace OnomiCircuitShaper.Engine.Data
{
    /// <summary>
    /// Represents a specialized point that lies on the main circuit path.
    /// In addition to its positional data, a CircuitPointData holds a reference
    /// to its own cross-section curve, which defines the shape of the road at that point.
    /// </summary>
    [System.Serializable]
    public class CircuitPointData : PointData
    {
        /// <summary>
        /// The curve that defines the 2D profile or cross-section of the road surface
        /// at this specific point along the main track spline. Uses the specialized
        /// CrossSectionCurve type for clearer semantics.
        /// </summary>
        public CrossSectionCurveData CrossSectionCurve = new CrossSectionCurveData();
    }
}
