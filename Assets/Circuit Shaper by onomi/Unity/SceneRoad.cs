using OnomiCircuitShaper.Engine.Data;
using UnityEngine;

namespace OnomiCircuitShaper.Unity
{
    /// <summary>
    /// A MonoBehaviour attached to a GameObject in the scene that represents a single road.
    /// Its primary role is to hold the MeshFilter, MeshRenderer, and MeshCollider, and to
    /// receive updated mesh data from the controller to render the road.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class SceneRoad : MonoBehaviour
    {
        public OnomiCircuitShaper onomiCircuitShaper;

        public RoadData roadData;


        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private MeshCollider _meshCollider;
        private Mesh _mesh;

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshCollider = GetComponent<MeshCollider>();
            _mesh = new Mesh();
            _mesh.name = "SceneRoad_Mesh";
            _meshFilter.mesh = _mesh;
            _meshCollider.sharedMesh = _mesh;
        }

        /// <summary>
        /// Receives new mesh data and applies it to the MeshFilter and MeshCollider.
        /// </summary>
        public void UpdateMesh(Vector3[] vertices, Vector2[] uvs, int[] triangles)
        {
            _mesh.Clear();
            _mesh.vertices = vertices;
            _mesh.uv = uvs;
            _mesh.triangles = triangles;
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
        }
    }
}
