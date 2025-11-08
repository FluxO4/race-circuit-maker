namespace OnomiCircuitShaper.Engine.Data
{
    /// <summary>
    /// Defines the properties for a single railing mesh that runs alongside a road.
    /// </summary>
    [System.Serializable]
    public class RailingData
    {
        /// <summary>
        /// The vertical height of the railing mesh.
        /// </summary>
        public float RailingHeight = 1.0f;

        /// <summary>
        /// The normalized start position of the railing along the length of its parent road.
        /// A value of 0 is the start of the road, 1 is the end.
        /// </summary>
        public float Min = 0.0f;

        /// <summary>
        /// The normalized end position of the railing along the length of its parent road.
        /// A value of 0 is the start of the road, 1 is the end.
        /// </summary>
        public float Max = 1.0f;

        /// <summary>
        /// The horizontal position of the railing across the road's surface.
        /// A value of 0 places it on one edge, and 1 places it on the opposite edge.
        /// </summary>
        public float HorizontalPosition = 0.0f;
    }
}
