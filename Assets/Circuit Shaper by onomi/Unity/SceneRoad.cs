using OnomiCircuitShaper.Engine.EditRealm;
using OnomiCircuitShaper.Engine.Processors;
using OnomiCircuitShaper.Engine.Data;
using System.Collections.Generic;
using UnityEngine;

namespace OnomiCircuitShaper.Unity
{
    /// <summary>
    /// A MonoBehaviour attached to a GameObject in the scene that represents a single road.
    /// Its primary role is to hold the MeshFilter, MeshRenderer, and MeshCollider, and to
    /// receive updated mesh data from the controller to render the road.
    /// Also manages child bridge and railing objects.
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

        // Child objects for bridge and railings
        private SceneBridge _sceneBridge;
        private List<SceneRailing> _sceneRailings = new List<SceneRailing>();






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

            if (_mesh == null)
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

            UnityEngine.Debug.Log($"[SceneRoad] Mesh updated successfully. Vertex count: {_mesh.vertexCount}, Triangle count: {_mesh.triangles.Length / 3}");

            // Bridge and Railings are now updated separately via UpdateBridge and UpdateRailings methods
        }

        /// <summary>
        /// Updates the bridge child object with the provided mesh data.
        /// </summary>
        public void UpdateBridge(GenericMeshData bridgeMesh, Bridge bridge, int materialID)
        {
            if (bridgeMesh.Vertices != null && bridgeMesh.Vertices.Length > 0)
            {
                if (_sceneBridge == null)
                {
                    // Create bridge child object
                    GameObject bridgeObj = new GameObject("Bridge");
                    bridgeObj.transform.SetParent(transform, false);
                    //bridgeObj.hideFlags = HideFlags.HideInHierarchy;
                    _sceneBridge = bridgeObj.AddComponent<SceneBridge>();
                    _sceneBridge.onomiCircuitShaper = onomiCircuitShaper;
                    _sceneBridge.associatedBridge = bridge;
                }

                Vector3[] unityVertices = new Vector3[bridgeMesh.Vertices.Length];
                for (int i = 0; i < bridgeMesh.Vertices.Length; i++)
                {
                    var v = bridgeMesh.Vertices[i];
                    unityVertices[i] = new Vector3(v.X, v.Y, v.Z);
                }

                Vector2[] unityUVs = new Vector2[bridgeMesh.UVs.Length];
                for (int i = 0; i < bridgeMesh.UVs.Length; i++)
                {
                    var uv = bridgeMesh.UVs[i];
                    unityUVs[i] = new Vector2(uv.X, uv.Y);
                }

                _sceneBridge.UpdateMesh(unityVertices, unityUVs, bridgeMesh.Triangles, materialID);
            }
            else
            {
                // Remove bridge if it exists but shouldn't
                if (_sceneBridge != null)
                {
                    DestroyImmediate(_sceneBridge.gameObject);
                    _sceneBridge = null;
                }
            }
        }

        /// <summary>
        /// Updates the railing child objects with the provided list of mesh data.
        /// </summary>
        public void UpdateRailings(List<(GenericMeshData mesh, Railing railing)> railingUpdates)
        {
            if (railingUpdates != null && railingUpdates.Count > 0)
            {
                // Remove excess railing objects
                while (_sceneRailings.Count > railingUpdates.Count)
                {
                    int lastIndex = _sceneRailings.Count - 1;
                    if (_sceneRailings[lastIndex] != null)
                    {
                        DestroyImmediate(_sceneRailings[lastIndex].gameObject);
                    }
                    _sceneRailings.RemoveAt(lastIndex);
                }

                // Create or update railing objects
                for (int i = 0; i < railingUpdates.Count; i++)
                {
                    var update = railingUpdates[i];
                    var railingMesh = update.mesh;
                    var railing = update.railing;

                    SceneRailing sceneRailing;

                    if (i >= _sceneRailings.Count)
                    {
                        // Create new railing child object
                        GameObject railingObj = new GameObject($"Railing_{i}");
                        railingObj.transform.SetParent(transform, false);
                        //railingObj.hideFlags = HideFlags.HideInHierarchy;
                        sceneRailing = railingObj.AddComponent<SceneRailing>();
                        sceneRailing.onomiCircuitShaper = onomiCircuitShaper;
                        _sceneRailings.Add(sceneRailing);
                    }
                    else
                    {
                        sceneRailing = _sceneRailings[i];
                    }

                    sceneRailing.associatedRailing = railing;

                    if (railingMesh.Vertices != null && railingMesh.Vertices.Length > 0)
                    {
                        Vector3[] unityVertices = new Vector3[railingMesh.Vertices.Length];
                        for (int j = 0; j < railingMesh.Vertices.Length; j++)
                        {
                            var v = railingMesh.Vertices[j];
                            unityVertices[j] = new Vector3(v.X, v.Y, v.Z);
                        }

                        Vector2[] unityUVs = new Vector2[railingMesh.UVs.Length];
                        for (int j = 0; j < railingMesh.UVs.Length; j++)
                        {
                            var uv = railingMesh.UVs[j];
                            unityUVs[j] = new Vector2(uv.X, uv.Y);
                        }

                        sceneRailing.UpdateMesh(unityVertices, unityUVs, railingMesh.Triangles,
                            railingMesh.MaterialID, railing.Data.IsVisible);
                    }
                    else
                    {
                        // If mesh is empty but object exists, we might want to hide it or just update with empty mesh
                        // For now, let's just clear the mesh
                        sceneRailing.UpdateMesh(new Vector3[0], new Vector2[0], new int[0], 0, false);
                    }
                }
            }
            else
            {
                // Remove all railings
                foreach (var railing in _sceneRailings)
                {
                    if (railing != null)
                    {
                        DestroyImmediate(railing.gameObject);
                    }
                }
                _sceneRailings.Clear();
            }
        }

        private void OnDestroy()
        {
            // Clean up child objects
            if (_sceneBridge != null)
            {
                DestroyImmediate(_sceneBridge.gameObject);
            }

            foreach (var railing in _sceneRailings)
            {
                if (railing != null)
                {
                    DestroyImmediate(railing.gameObject);
                }
            }
            _sceneRailings.Clear();
        }
    }
}
