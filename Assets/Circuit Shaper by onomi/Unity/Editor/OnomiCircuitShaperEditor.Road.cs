using OnomiCircuitShaper.Engine.Data;
using OnomiCircuitShaper.Engine.EditRealm;
using OnomiCircuitShaper.Unity.Utilities;
using UnityEditor;
using UnityEngine;
using System.Linq;
using OnomiCircuitShaper.Engine.Processors;
using System.Collections.Generic;


namespace OnomiCircuitShaper.Unity.Editor
{
    public partial class OnomiCircuitShaperEditor : UnityEditor.Editor
    {
        //Dict to map RoadData to SceneRoad GameObjects
        Dictionary<Road, SceneRoad> _sceneRoads = new Dictionary<Road, SceneRoad>();


        /// <summary>
        /// Clears _sceneRoads, delets all children of target, and rebuilds all roads from data.
        /// </summary>
        private void RebuildAllRoadsFromData()
        {
            if (_circuitShaper == null || _circuitShaper.GetLiveCircuit == null) return;
            
            var circuit = _circuitShaper.GetLiveCircuit;

            UnityEngine.Debug.Log($"[Editor] Rebuilding roads from data");

            //Clear existing scene roads
            _sceneRoads.Clear();

            // Delete all children of the target GameObject
            foreach (Transform child in _target.transform)
            {
                DestroyImmediate(child.gameObject);
            }

            foreach(CircuitCurve circuitCurve in circuit.Curves)
            {
                foreach(Road road in circuitCurve.Roads)
                {
                    var meshData = RoadProcessor.BuildRoadMesh(road);

                    // Create or update the SceneRoad
                    UpdateRoadMesh(road, meshData);
                }
            }

        }


        /// <summary>
        /// Processes the road rebuild queue. Rebuilds roads that have been marked dirty.
        /// Throttled to prevent excessive updates.
        /// </summary>
        private void ProcessDirtyRoads()
        {
            if (_circuitShaper == null || _circuitShaper.GetLiveCircuit == null) return;

            float currentTime = (float)EditorApplication.timeSinceStartup;
            if (currentTime - _lastRoadUpdateTime < MinRoadUpdateInterval) return;

            // Get all dirty roads from the queue via interface
            var dirtyRoads = _circuitShaper.GetAndClearDirtyRoads();
            if (dirtyRoads.Count == 0) return;

            UnityEngine.Debug.Log($"[Editor] Processing {dirtyRoads.Count} dirty roads from queue");

            foreach (var road in dirtyRoads)
            {
                // Validate road still exists in persistent data
                if (!_target.Data.circuitData.CircuitCurves.Contains(road.parentCurve.Data) || !road.parentCurve.Data.Roads.Contains(road.Data))
                {
                    // Road was deleted, clean up SceneRoad
                    UnityEngine.Debug.Log("[Editor] Road deleted from data, cleaning up SceneRoad");
                    if (_sceneRoads.TryGetValue(road, out SceneRoad sceneRoad))
                    {
                        if (sceneRoad != null)
                        {
                            DestroyImmediate(sceneRoad.gameObject);
                        }
                        _sceneRoads.Remove(road);
                    }
                    continue;
                }


                // Generate mesh
                var meshData = RoadProcessor.BuildRoadMesh(road);

                // Update SceneRoad
                UpdateRoadMesh(road, meshData);

                _lastRoadUpdateTime = currentTime;
            }
        }




        /// <summary>
        /// Updates or creates the SceneRoad GameObject with the provided mesh data.
        /// </summary>
        private void UpdateRoadMesh(Road road, GenericMeshData meshData)
        {
            UnityEngine.Debug.Log($"[Editor] UpdateRoadMesh called. Vertices: {meshData.Vertices?.Length ?? 0}");
            
            // If meshData has no vertices
            if (meshData.Vertices == null) return;


            GenericMeshData mesh = meshData;

            SceneRoad existingRoad;
            _sceneRoads.TryGetValue(road, out existingRoad);

            // Get or create the SceneRoad
            if (existingRoad == null)
            {
                UnityEngine.Debug.Log("[Editor] Creating new SceneRoad GameObject");
                // Create new GameObject
                GameObject roadObject = new GameObject("Road");
                roadObject.transform.SetParent(_target.transform);
                roadObject.transform.localPosition = UnityEngine.Vector3.zero;
                roadObject.transform.localRotation = UnityEngine.Quaternion.identity;
                roadObject.transform.localScale = UnityEngine.Vector3.one;

                existingRoad = roadObject.AddComponent<SceneRoad>();
                existingRoad.onomiCircuitShaper = _target;
                existingRoad.associatedRoad = road;

                _sceneRoads[road] = existingRoad;
            }
            else
            {
                UnityEngine.Debug.Log("[Editor] Updating existing SceneRoad");
            }

            // Convert System.Numerics.Vector3 to UnityEngine.Vector3
            UnityEngine.Vector3[] vertices = new UnityEngine.Vector3[mesh.Vertices.Length];
            for (int i = 0; i < mesh.Vertices.Length; i++)
            {
                var v = mesh.Vertices[i];
                vertices[i] = new UnityEngine.Vector3(v.X, v.Y, v.Z);
            }

            // Convert System.Numerics.Vector2 to UnityEngine.Vector2
            UnityEngine.Vector2[] uvs = new UnityEngine.Vector2[mesh.UVs.Length];
            for (int i = 0; i < mesh.UVs.Length; i++)
            {
                var uv = mesh.UVs[i];
                uvs[i] = new UnityEngine.Vector2(uv.X, uv.Y);
            }

            UnityEngine.Debug.Log($"[Editor] Calling SceneRoad.UpdateMesh with {vertices.Length} vertices, {uvs.Length} UVs, {mesh.Triangles.Length} triangle indices");

            // Update the mesh
            existingRoad.UpdateMesh(vertices, uvs, mesh.Triangles);
            
            UnityEngine.Debug.Log("[Editor] Mesh updated successfully");
        }



        

    }
}