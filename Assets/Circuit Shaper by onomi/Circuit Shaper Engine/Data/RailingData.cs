namespace OnomiCircuitShaper.Engine.Data
{
    /// <summary>
    /// Defines the properties for a single railing mesh that runs alongside a road.
    /// This data is used by a processor to generate the railing geometry.
    /// </summary>
    [System.Serializable]
    public class RailingData
    {
        /// <summary>
        /// The material index to use when rendering the railing mesh.
        /// </summary>
        public int MaterialIndex = 0;

        /// <summary>
        /// Whether to render this railing (false makes it invisible).
        /// </summary>
        public bool IsVisible = true;

        /// <summary>
        /// The vertical height of the railing mesh from the road surface.
        /// </summary>
        public float RailingHeight = 1.0f;

        /// <summary>
        /// The normalized start position of the railing along the length of its parent road.
        /// A value of 0 corresponds to the start of the road, and 1 corresponds to the end.
        /// </summary>
        public float Min = 0.0f;

        /// <summary>
        /// The normalized end position of the railing along the length of its parent road.
        /// A value of 0 corresponds to the start of the road, and 1 corresponds to the end.
        /// </summary>
        public float Max = 1.0f;

        /// <summary>
        /// The horizontal position of the railing across the road's surface, normalized.
        /// A value of 0 places it on one edge of the road's cross-section, and 1 places it on the opposite edge.
        /// Values in between will place it somewhere across the road surface.
        /// </summary>
        public float HorizontalPosition = 0.0f;
    }
}
