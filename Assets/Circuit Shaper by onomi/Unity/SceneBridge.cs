using OnomiCircuitShaper.Engine.EditRealm;
using UnityEngine;

namespace OnomiCircuitShaper.Unity
{
    /// <summary>
    /// A MonoBehaviour attached to a GameObject that represents a bridge mesh.
    /// Holds the MeshFilter, MeshRenderer, and MeshCollider for a bridge structure.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class SceneBridge : MonoBehaviour
    {
        public OnomiCircuitShaper onomiCircuitShaper;
        public Bridge associatedBridge;

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private MeshCollider _meshCollider;
        private Mesh _mesh;

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshCollider = GetComponent<MeshCollider>();
        }

        /// <summary>
        /// Receives new mesh data and applies it to the MeshFilter and MeshCollider.
        /// </summary>
        public void UpdateMesh(Vector3[] vertices, Vector2[] uvs, int[] triangles, int materialID)
        {
            // Ensure components are initialized
            if (_meshFilter == null) _meshFilter = GetComponent<MeshFilter>();
            if (_meshCollider == null) _meshCollider = GetComponent<MeshCollider>();
            if (_meshRenderer == null) _meshRenderer = GetComponent<MeshRenderer>();

            if (_mesh == null)
            {
                _mesh = new Mesh();
                _mesh.name = "SceneBridge_Mesh";
            }

            _mesh.Clear();
            _mesh.vertices = vertices;
            _mesh.uv = uvs;
            _mesh.triangles = triangles;
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();

            _meshFilter.mesh = _mesh;
            _meshCollider.sharedMesh = _mesh;

            // Apply collider settings
            if (associatedBridge != null && associatedBridge.Data != null)
            {
                _meshCollider.enabled = associatedBridge.Data.EnableCollider;

                if (associatedBridge.Data.EnableCollider && onomiCircuitShaper != null && onomiCircuitShaper.BridgePhysicsMaterials != null)
                {
                    int physMatIndex = associatedBridge.Data.PhysicsMaterialIndex;
                    if (physMatIndex >= 0 && physMatIndex < onomiCircuitShaper.BridgePhysicsMaterials.Count)
                    {
                        _meshCollider.material = onomiCircuitShaper.BridgePhysicsMaterials[physMatIndex];
                    }
                }
            }

            // Update material
            if (onomiCircuitShaper != null && onomiCircuitShaper.BridgeMaterials != null &&
                materialID >= 0 && materialID < onomiCircuitShaper.BridgeMaterials.Count)
            {
                _meshRenderer.material = onomiCircuitShaper.BridgeMaterials[materialID];
            }
            else
            {
                UnityEngine.Debug.LogWarning($"[SceneBridge] Invalid material ID {materialID} or materials list not set.");
            }

            // Apply layer and tag if specified
            if (associatedBridge != null && associatedBridge.Data != null)
            {
                if (!string.IsNullOrEmpty(associatedBridge.Data.Layer))
                {
                    int layerIndex = LayerMask.NameToLayer(associatedBridge.Data.Layer);
                    if (layerIndex != -1)
                    {
                        gameObject.layer = layerIndex;
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning($"[SceneBridge] Layer '{associatedBridge.Data.Layer}' does not exist in the project.");
                    }
                }

                if (!string.IsNullOrEmpty(associatedBridge.Data.Tag))
                {
                    try
                    {
                        gameObject.tag = associatedBridge.Data.Tag;
                    }
                    catch
                    {
                        UnityEngine.Debug.LogWarning($"[SceneBridge] Tag '{associatedBridge.Data.Tag}' does not exist in the project.");
                    }
                }
            }
        }

        /// <summary>
        /// Clears the mesh data.
        /// </summary>
        public void ClearMesh()
        {
            if (_mesh != null)
            {
                _mesh.Clear();
            }
        }
    }
}
