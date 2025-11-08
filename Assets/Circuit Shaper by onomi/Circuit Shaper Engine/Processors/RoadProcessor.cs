using OnomiCircuitShaper.Engine.EditRealm;
using System.Numerics;

namespace OnomiCircuitShaper.Engine.Processors
{
    /// <summary>
    /// A struct to hold generic mesh data, making it engine-agnostic.
    /// </summary>
    public struct GenericMeshData
    {
        public Vector3[] Vertices;
        public Vector2[] UVs;
        public int[] Triangles;
    }

    /// <summary>
    /// A static class containing logic for generating mesh data for roads, bridges, and railings.
    /// </summary>
    public static class RoadProcessor
    {
        /// <summary>
        /// Generates the vertex, UV, and triangle data for a road mesh.
        /// </summary>
        public static GenericMeshData BuildRoadMesh(Road road)
        {
            // To be implemented.
            return new GenericMeshData();
        }

        /// <summary>
        /// Generates the vertex, UV, and triangle data for a bridge mesh.
        /// </summary>
        public static GenericMeshData BuildBridgeMesh(Bridge bridge)
        {
            // To be implemented.
            return new GenericMeshData();
        }

        /// <summary>
        /// Generates the vertex, UV, and triangle data for a railing mesh.
        /// </summary>
        public static GenericMeshData BuildRailingMesh(Railing railing)
        {
            // To be implemented.
            return new GenericMeshData();
        }
    }
}
