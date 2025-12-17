using OnomiCircuitShaper.Engine.EditRealm;
using UnityEngine;

namespace OnomiCircuitShaper.Unity
{
    /// <summary>
    /// A MonoBehaviour attached to a GameObject that represents a railing mesh.
    /// Holds the MeshFilter, MeshRenderer, and MeshCollider for a railing structure.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class SceneRailing : MonoBehaviour
    {
        public OnomiCircuitShaper onomiCircuitShaper;
        public Railing associatedRailing;

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
        public void UpdateMesh(Vector3[] vertices, Vector2[] uvs, int[] triangles, int materialID, bool isVisible)
        {
            // Ensure components are initialized
            if (_meshFilter == null) _meshFilter = GetComponent<MeshFilter>();
            if (_meshCollider == null) _meshCollider = GetComponent<MeshCollider>();
            if (_meshRenderer == null) _meshRenderer = GetComponent<MeshRenderer>();

            // Handle visibility
            _meshRenderer.enabled = isVisible;

            if (_mesh == null)
            {
                _mesh = new Mesh();
                _mesh.name = "SceneRailing_Mesh";
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
            if (associatedRailing != null && associatedRailing.Data != null)
            {
                _meshCollider.enabled = associatedRailing.Data.EnableCollider;

                if (associatedRailing.Data.EnableCollider && onomiCircuitShaper != null && onomiCircuitShaper.RailingPhysicsMaterials != null)
                {
                    int physMatIndex = associatedRailing.Data.PhysicsMaterialIndex;
                    if (physMatIndex >= 0 && physMatIndex < onomiCircuitShaper.RailingPhysicsMaterials.Count)
                    {
                        _meshCollider.material = onomiCircuitShaper.RailingPhysicsMaterials[physMatIndex];
                    }
                }
            }

            // Update material
            if (onomiCircuitShaper != null && onomiCircuitShaper.RailingMaterials != null &&
                materialID >= 0 && materialID < onomiCircuitShaper.RailingMaterials.Count)
            {
                _meshRenderer.material = onomiCircuitShaper.RailingMaterials[materialID];
            }
            else
            {
                UnityEngine.Debug.LogWarning($"[SceneRailing] Invalid material ID {materialID} or materials list not set.");
            }

            // Apply layer and tag if specified
            if (associatedRailing != null && associatedRailing.Data != null)
            {
                if (!string.IsNullOrEmpty(associatedRailing.Data.Layer))
                {
                    int layerIndex = LayerMask.NameToLayer(associatedRailing.Data.Layer);
                    if (layerIndex != -1)
                    {
                        gameObject.layer = layerIndex;
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning($"[SceneRailing] Layer '{associatedRailing.Data.Layer}' does not exist in the project.");
                    }
                }

                if (!string.IsNullOrEmpty(associatedRailing.Data.Tag))
                {
                    try
                    {
                        gameObject.tag = associatedRailing.Data.Tag;
                    }
                    catch
                    {
                        UnityEngine.Debug.LogWarning($"[SceneRailing] Tag '{associatedRailing.Data.Tag}' does not exist in the project.");
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
