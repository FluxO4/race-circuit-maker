namespace OnomiCircuitShaper.Engine.Data
{
    /// <summary>
    /// Settings for waypoint generation from bezier curves.
    /// Waypoints approximate curves with straight-line segments for easier pathfinding and AI use.
    /// </summary>
    [System.Serializable]
    public class WaypointSettings
    {
        /// <summary>
        /// Quality of the curve approximation. Higher values place more waypoints on curves.
        /// Range 0.1-100, where 100 is the highest quality with most points.
        /// Lower values (0.1-5) suitable for simple checkpoint triggers.
        /// This affects the adaptive sampling - more points on curves, fewer on straights.
        /// </summary>
        public float ApproximationQuality = 10f;

        /// <summary>
        /// Additional width buffer added to the road width when scaling waypoints.
        /// Positive values make waypoints slightly wider than the road, negative values narrower.
        /// Useful for ensuring AI stays within track boundaries or for collision detection zones.
        /// </summary>
        public float WidthBuffer = 0.5f;

        /// <summary>
        /// Height of waypoint objects in world units (perpendicular to road surface / UP direction).
        /// Based on the UP direction at each curve point.
        /// </summary>
        public float Height = 2.0f;

        /// <summary>
        /// Depth of waypoint objects in world units (along road direction / FORWARD direction).
        /// Represents how long a segment is along the path, typically matching car length.
        /// </summary>
        public float Depth = 4.0f;

        /// <summary>
        /// Minimum distance between waypoints in world units.
        /// Prevents excessive point density on sharp curves.
        /// </summary>
        public float MinWaypointSpacing = 1.0f;

        /// <summary>
        /// Maximum distance between waypoints in world units.
        /// Ensures adequate point density on straight sections.
        /// </summary>
        public float MaxWaypointSpacing = 10.0f;

        /// <summary>
        /// Curvature threshold for adaptive sampling. Higher values = more sensitive to curves.
        /// Measured as 1/radius (unitless). Typical values 0.01 - 1.0.
        /// </summary>
        public float CurvatureThreshold = 0.1f;
    }
}
