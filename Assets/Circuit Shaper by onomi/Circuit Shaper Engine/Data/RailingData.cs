using System.Numerics;

namespace OnomiCircuitShaper.Engine.Data
{
    /// <summary>
    /// Defines which sides of the railing should have visible/collidable faces.
    /// </summary>
    public enum RailingSidedness
    {
        DoubleSided,
        LeftSided,
        RightSided
    }

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

        /// <summary>
        /// Controls the tiling of the UV coordinates on the railing mesh.
        /// </summary>
        public SerializableVector2 UVTile = (SerializableVector2)System.Numerics.Vector2.One;

        /// <summary>
        /// Controls the offset of the UV coordinates on the railing mesh.
        /// </summary>
        public SerializableVector2 UVOffset = (SerializableVector2)System.Numerics.Vector2.Zero;

        /// <summary>
        /// If true, U coordinates will be calculated based on world width rather than normalized 0-1 range.
        /// </summary>
        public bool UseDistanceBasedWidthUV = false;

        /// <summary>
        /// If true, V coordinates will be calculated based on world length rather than normalized 0-1 range.
        /// </summary>
        public bool UseDistanceBasedLengthUV = false;

        /// <summary>
        /// Determines which sides of the railing have visible/collidable faces.
        /// </summary>
        public RailingSidedness Sidedness = RailingSidedness.DoubleSided;

        /// <summary>
        /// The Unity layer name to assign to the railing GameObject.
        /// </summary>
        public string Layer = "";

        /// <summary>
        /// The Unity tag to assign to the railing GameObject.
        /// </summary>
        public string Tag = "";

        /// <summary>
        /// If true, the railing will have a collider enabled.
        /// </summary>
        public bool EnableCollider = true;

        /// <summary>
        /// The physics material index to use for the railing collider.
        /// </summary>
        public int PhysicsMaterialIndex = 0;

        /// <summary>
        /// If true, the railing mesh will be rendered (visible). If false, it will be invisible but can still have a collider.
        /// </summary>
        public bool EnableMeshRenderer = true;
    }
}
