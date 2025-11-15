using System.Collections.Generic;
using System.Numerics;

namespace OnomiCircuitShaper.Engine.Data
{

    /// <summary>
    /// Contains all the data required to generate a road mesh.
    /// A road is defined by a sequence of points, mesh generation parameters,
    /// and data for any associated railings or bridges.
    /// </summary>
    [System.Serializable]
    public class RoadData
    {
        /// <summary>
        /// The min and max indices of the points that define this road within the parent curve's point list.
        /// </summary>

        public int minPointIndex = 0;
        public int maxPointIndex = 0;
        

        //<summary>
        /// The material index to use when generating the road mesh. This index corresponds to the material list in the parent circuit.
        ///</summary>
        public int MaterialIndex = 0;

        /// <summary>
        /// Determines the number of vertices used across the width of the road mesh.
        /// A higher value results in a smoother, more detailed road surface across its width.
        /// </summary>
        public int WidthWiseVertexCount = 10;

        /// <summary>
        /// Controls the density of vertices along the length of the road, relative to its width.
        /// A value of 1 means the vertices will form roughly square quads.
        /// </summary>
        public float LengthWiseVertexCountPerUnitWidthWiseVertexCount = 1;

        /// <summary>
        /// A boolean flag indicating whether smart meshing is enabled for this road, allowing for optimized mesh generation, less vertices for straighter sections.
        /// </summary>
        public bool UseSmartMeshing = true;

        /// <summary>
        /// Controls the tiling (repetition) of the UV coordinates across the mesh.
        /// X controls tiling along the road's width, Y controls tiling along its length.
        /// </summary>
        public SerializableVector2 UVTile = (SerializableVector2)Vector2.One;

        /// <summary>
        /// Offsets the starting point of the UV coordinates on the mesh.
        /// Allows for adjusting the texture's position without changing the model.
        /// </summary>
        public SerializableVector2 UVOffset = (SerializableVector2)Vector2.Zero;

        /// <summary>
        /// A list of RailingData objects associated with this road.
        /// Each railing will be generated as a separate mesh alongside the road.
        /// </summary>
        public List<RailingData> Railings = new List<RailingData>();

        /// <summary>
        /// Data for a bridge structure associated with this road.
        /// If not null, a bridge mesh will be generated based on these parameters.
        /// </summary>
        public BridgeData Bridge;
    }
}
