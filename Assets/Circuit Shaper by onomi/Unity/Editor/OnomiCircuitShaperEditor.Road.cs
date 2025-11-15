using OnomiCircuitShaper.Engine.Data;
using OnomiCircuitShaper.Engine.EditRealm;
using OnomiCircuitShaper.Engine.Interface;
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
        Road _hoveredRoad = null;


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
            // Use a reverse for-loop to avoid modifying the Transform collection while enumerating.
            for (int i = _target.transform.childCount - 1; i >= 0; i--)
            {
                var child = _target.transform.GetChild(i);
                Debug.Log("[Editor] Deleted child GameObject: " + child.name);
                DestroyImmediate(child.gameObject);
            }
           // Debug.Log("[Editor] Number of Children Destroyed: " + _target.transform.childCount);

            foreach (CircuitCurve circuitCurve in circuit.Curves)
            {
                foreach (Road road in circuitCurve.Roads)
                {
                    var meshData = RoadProcessor.BuildRoadMesh(road);

                    // Create or update the SceneRoad
                    UpdateRoadMesh(road, meshData);
                }
            }
            
            //Hide the scene roads from the hierarchy
            foreach (var sceneRoad in _sceneRoads.Values)
            {
                if (sceneRoad != null)
                {
                    sceneRoad.gameObject.hideFlags = HideFlags.HideInHierarchy;
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

            //if road is marked for deletion, delete and return
            if (road.MarkedForDeletion)
            {
                UnityEngine.Debug.Log("[Editor] Road marked for deletion, removing SceneRoad if exists");
                if (_sceneRoads.TryGetValue(road, out SceneRoad sceneRoad))
                {
                    if (sceneRoad != null)
                    {
                        DestroyImmediate(sceneRoad.gameObject);
                    }
                    _sceneRoads.Remove(road);
                }
                //Let's just rebuild everything instead to be safe
                
                return;
            }


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
            existingRoad.UpdateMesh(vertices, uvs, mesh.Triangles, mesh.MaterialID);

            UnityEngine.Debug.Log("[Editor] Mesh updated successfully");
        }
        

                /// <summary>
        /// Draws the inspector UI for the selected road, allowing editing of UV settings,
        /// material assignment, mesh resolution, and deletion.
        /// </summary>
        private void DrawSelectedRoadInspector()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Selected Road", EditorStyles.boldLabel);
            
            if (_selectedRoad == null || _selectedRoad.Data == null)
            {
                return;
            }

            // Get the SceneRoad for material editing
            SceneRoad sceneRoad = null;
            _sceneRoads.TryGetValue(_selectedRoad, out sceneRoad);

            EditorGUI.BeginChangeCheck();

            // UV Settings
            EditorGUILayout.LabelField("UV Settings", EditorStyles.boldLabel);
            var uvTile = (System.Numerics.Vector2)_selectedRoad.Data.UVTile;
            var uvOffset = (System.Numerics.Vector2)_selectedRoad.Data.UVOffset;
            
            UnityEngine.Vector2 tileUV = new UnityEngine.Vector2(uvTile.X, uvTile.Y);
            UnityEngine.Vector2 offsetUV = new UnityEngine.Vector2(uvOffset.X, uvOffset.Y);
            
            tileUV = EditorGUILayout.Vector2Field("Tile", tileUV);
            offsetUV = EditorGUILayout.Vector2Field("Offset", offsetUV);

            if (EditorGUI.EndChangeCheck())
            {
                _selectedRoad.Data.UVTile = (SerializableVector2)(new System.Numerics.Vector2(tileUV.x, tileUV.y));
                _selectedRoad.Data.UVOffset = (SerializableVector2)(new System.Numerics.Vector2(offsetUV.x, offsetUV.y));
                RoadRebuildQueue.MarkDirty(_selectedRoad);
            }

            EditorGUI.BeginChangeCheck();

            // Mesh Resolution
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Mesh Resolution", EditorStyles.boldLabel);
            int widthWiseVertexCount = EditorGUILayout.IntSlider("Width Vertices", _selectedRoad.Data.WidthWiseVertexCount, 2, 50);
            float lengthMult = EditorGUILayout.Slider("Length Density", _selectedRoad.Data.LengthWiseVertexCountPerUnitWidthWiseVertexCount, 0.1f, 10f);

            if (EditorGUI.EndChangeCheck())
            {
                _selectedRoad.Data.WidthWiseVertexCount = widthWiseVertexCount;
                _selectedRoad.Data.LengthWiseVertexCountPerUnitWidthWiseVertexCount = lengthMult;
                RoadRebuildQueue.MarkDirty(_selectedRoad);
            }

            // Material Selection
            //Material should now simply set the material index in road data, we should simply use a number input with inrease decrease buttons on either side
            
            EditorGUILayout.Space();

            bool hasMaterials = _target != null && _target.RoadMaterials != null && _target.RoadMaterials.Count > 0;

            // Grey out the whole material section if there are no materials
            EditorGUI.BeginDisabledGroup(!hasMaterials);
            EditorGUILayout.LabelField("Material", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            int maxIndex = (_target != null && _target.RoadMaterials != null) ? Mathf.Max(0, _target.RoadMaterials.Count - 1) : 0;
            int materialIndex = EditorGUILayout.IntSlider("Material Index", _selectedRoad.Data.MaterialIndex, 0, maxIndex);
            if (EditorGUI.EndChangeCheck())
            {
                _selectedRoad.Data.MaterialIndex = materialIndex;
                RoadRebuildQueue.MarkDirty(_selectedRoad);
            }
            EditorGUI.EndDisabledGroup();

            if (!hasMaterials)
            {
                EditorGUILayout.HelpBox("No road materials assigned on target. Assign at least one material to enable material selection.", MessageType.Info);
            }

            // --- NEW: Min/Max index controls with wrap-around ---
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Road Point Index Range", EditorStyles.boldLabel);

            int pointCount = 0;
            if (_selectedRoad.parentCurve != null && _selectedRoad.parentCurve.Points != null)
            {
                pointCount = _selectedRoad.parentCurve.Points.Count;
            }
            int maxAllowed = Mathf.Max(0, pointCount - 1);

            EditorGUILayout.BeginHorizontal();
            // Min Index control
            EditorGUILayout.BeginHorizontal(GUILayout.Width(220));
            GUI.enabled = (pointCount > 0);
            GUILayout.Label("Start Seg", GUILayout.Width(80));
            if (GUILayout.Button("-", GUILayout.Width(24)))
            {
                int v = _selectedRoad.Data.startSegmentIndex - 1;
                if (pointCount > 0 && v < 0) v = maxAllowed;
                if (!_circuitShaper.TrySetRoadStartSegment(_selectedRoad, v))
                {
                    UnityEngine.Debug.LogWarning("Cannot decrease start segment: would overlap with another road");
                }
            }
            // display value (not directly editable - changed only via +/-)
            EditorGUILayout.LabelField(_selectedRoad.Data.startSegmentIndex.ToString(), GUILayout.Width(30), GUILayout.ExpandWidth(false));
            if (GUILayout.Button("+", GUILayout.Width(24)))
            {
                int v = _selectedRoad.Data.startSegmentIndex + 1;
                if (pointCount > 0 && v > maxAllowed) v = 0;
                if (!_circuitShaper.TrySetRoadStartSegment(_selectedRoad, v))
                {
                    UnityEngine.Debug.LogWarning("Cannot increase start segment: would overlap with another road");
                }
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            // Max Index control
            EditorGUILayout.BeginHorizontal(GUILayout.Width(220));
            GUI.enabled = (pointCount > 0);
            GUILayout.Label("End Seg", GUILayout.Width(80));
            if (GUILayout.Button("-", GUILayout.Width(24)))
            {
                int v = _selectedRoad.Data.endSegmentIndex - 1;
                if (pointCount > 0 && v < 0) v = maxAllowed;
                if (!_circuitShaper.TrySetRoadEndSegment(_selectedRoad, v))
                {
                    UnityEngine.Debug.LogWarning("Cannot decrease end segment: would overlap with another road");
                }
            }
            EditorGUILayout.LabelField(_selectedRoad.Data.endSegmentIndex.ToString(), GUILayout.Width(30), GUILayout.ExpandWidth(false));
            if (GUILayout.Button("+", GUILayout.Width(24)))
            {
                int v = _selectedRoad.Data.endSegmentIndex + 1;
                if (pointCount > 0 && v > maxAllowed) v = 0;
                if (!_circuitShaper.TrySetRoadEndSegment(_selectedRoad, v))
                {
                    UnityEngine.Debug.LogWarning("Cannot increase end segment: would overlap with another road");
                }
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndHorizontal();

            if (pointCount == 0)
            {
                EditorGUILayout.HelpBox("Parent curve has no points (or parent curve missing). Cannot edit indices.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"Segment indices wrap between 0 and {maxAllowed}. Segment N connects point N to point N+1. Buttons disabled if change would overlap another road.", MessageType.None);
            }
            // --- END NEW SECTION ---

            // Deletion
            EditorGUILayout.Space();
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Delete Road", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Delete Road", 
                    "Are you sure you want to delete this road?", 
                    "Delete", "Cancel"))
                {
                    _circuitShaper.RemoveRoad(_selectedRoad);
                    _circuitShaper.DeselectRoad();
                
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space();
        }



        

    }
}