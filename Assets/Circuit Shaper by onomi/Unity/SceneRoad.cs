using OnomiCircuitShaper.Engine.EditRealm;
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

        public Road associatedRoad;


        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private MeshCollider _meshCollider;
        private Mesh _mesh;

        private void Awake()
        {
            // Prevent this GameObject from being saved in the scene or destroyed during play mode transitions
            this.gameObject.hideFlags = HideFlags.DontSaveInEditor;
            
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
        public void UpdateMesh(Vector3[] vertices, Vector2[] uvs, int[] triangles, int materialID)
        {
            UnityEngine.Debug.Log($"[SceneRoad] UpdateMesh called with {vertices?.Length ?? 0} vertices, {uvs?.Length ?? 0} UVs, {triangles?.Length ?? 0} triangle indices, MaterialID: {materialID}");
            
            // Ensure components are initialized (in case this is called before Awake)
            if (_meshFilter == null)
            {
                UnityEngine.Debug.Log("[SceneRoad] MeshFilter was null, getting component");
                _meshFilter = GetComponent<MeshFilter>();
            }
            if (_meshCollider == null)
            {
                UnityEngine.Debug.Log("[SceneRoad] MeshCollider was null, getting component");
                _meshCollider = GetComponent<MeshCollider>();
            }
            if (_meshRenderer == null)
            {
                UnityEngine.Debug.Log("[SceneRoad] MeshRenderer was null, getting component");
                _meshRenderer = GetComponent<MeshRenderer>();
            }
            
            if(_mesh == null)
            {
                UnityEngine.Debug.Log("[SceneRoad] Mesh was null, creating new mesh");
                _mesh = new Mesh();
                _mesh.name = "SceneRoad_Mesh";
            }
            
            _mesh.Clear();
            _mesh.vertices = vertices;
            _mesh.uv = uvs;
            _mesh.triangles = triangles;
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
            
            // Reassign to ensure it's set
            _meshFilter.mesh = _mesh;
            _meshCollider.sharedMesh = _mesh;


            // Update material
            if (onomiCircuitShaper != null && onomiCircuitShaper.RoadMaterials != null &&
                materialID >= 0 && materialID < onomiCircuitShaper.RoadMaterials.Count)
            {
                _meshRenderer.material = onomiCircuitShaper.RoadMaterials[materialID];
            }
            else
            {
                UnityEngine.Debug.LogWarning($"[SceneRoad] Invalid material ID {materialID} or missing OnomiCircuitShaper reference.");
            }
            
            UnityEngine.Debug.Log($"[SceneRoad] Mesh updated successfully. Vertex count: {_mesh.vertexCount}, Triangle count: {_mesh.triangles.Length/3}");
        }
    }
}
