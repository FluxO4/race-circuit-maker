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
        //Dict to map RoadData to SceneRoad GameObjects - STATIC so all editor instances share it
        static Dictionary<Road, SceneRoad> _sceneRoads = new Dictionary<Road, SceneRoad>();
        Road _hoveredRoad = null;

        // Track which curves should have waypoints generated
        private bool[] _selectedCurvesForWaypoints = null;
        private bool _waypointCurveSelectionFoldout = false;


        /// <summary>
        /// Clears _sceneRoads, delets all children of target, and rebuilds all roads from data.
        /// </summary>
        private void RebuildAllRoadsFromData()
        {
            if (_circuitShaper == null || _circuitShaper.GetLiveCircuit == null) return;

            var circuit = _circuitShaper.GetLiveCircuit;

            UnityEngine.Debug.Log($"[Editor] RebuildAllRoadsFromData() called - Dictionary has {_sceneRoads.Count} entries before clear");

            //Clear existing scene roads
            _sceneRoads.Clear();

            // Delete all children of the target GameObject except waypoint curves
            // Use a reverse for-loop to avoid modifying the Transform collection while enumerating.
            for (int i = _target.transform.childCount - 1; i >= 0; i--)
            {
                var child = _target.transform.GetChild(i);
                
                // Skip waypoint curve objects
                if (child.GetComponent<SceneWaypointCurve>() != null)
                {
                    Debug.Log("[Editor] Skipping waypoint curve: " + child.name);
                    continue;
                }
                
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
            if(_target.Data.settingsData.HideRoadsInHierarchy)
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

            UnityEngine.Debug.Log($"[Editor #{_editorInstanceId}] Processing {dirtyRoads.Count} dirty roads from queue");

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
            UnityEngine.Debug.Log($"[Editor #{_editorInstanceId}] UpdateRoadMesh called. Vertices: {meshData.Vertices?.Length ?? 0}, Road hashcode: {road.GetHashCode()}, Dictionary contains road: {_sceneRoads.ContainsKey(road)}, Dict count: {_sceneRoads.Count}");

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
            UnityEngine.Debug.Log($"[Editor #{_editorInstanceId}] Creating new SceneRoad GameObject");
                // Create new GameObject
                GameObject roadObject = new GameObject("Road");
                roadObject.transform.SetParent(_target.transform);
                roadObject.transform.localPosition = UnityEngine.Vector3.zero;
                roadObject.transform.localRotation = UnityEngine.Quaternion.identity;
                roadObject.transform.localScale = UnityEngine.Vector3.one;

                // Apply hide flags if setting is enabled
                /*if (_circuitShaper?.settings?.HideRoadsInHierarchy == true)
                {
                    roadObject.hideFlags = HideFlags.HideInHierarchy;
                }*/

                existingRoad = roadObject.AddComponent<SceneRoad>();
                existingRoad.onomiCircuitShaper = _target;
                existingRoad.associatedRoad = road;

                _sceneRoads[road] = existingRoad;
            }
           /* else
            {
                UnityEngine.Debug.Log("[Editor] Updating existing SceneRoad");
                
                // Update hide flags in case setting changed
                if (_circuitShaper?.Settings?.HideRoadsInHierarchy == true)
                {
                    existingRoad.gameObject.hideFlags = HideFlags.HideInHierarchy;
                }
                else
                {
                    existingRoad.gameObject.hideFlags = HideFlags.None;
                }
            }*/

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

            // --- Handle Bridge ---
            GenericMeshData bridgeMesh = new GenericMeshData();
            if (road.Bridge != null && road.Bridge.Data.Enabled)
            {
                bridgeMesh = RoadProcessor.BuildBridgeMesh(road.Bridge, road);
                Debug.Log("[Editor] Bridge is enabled, generated mesh with " + bridgeMesh.Vertices?.Length + " vertices.");
            }
            else
            {
                Debug.Log("[Editor] Bridge is disabled or null, sending empty mesh. Bridge:" +road.Bridge+"Bridge disabled: " + (road.Bridge != null ? !road.Bridge.Data.Enabled : true));
            }
            // Pass bridge mesh to SceneRoad (empty if disabled/null, which SceneRoad handles by removing the object)
            int bridgeMatID = (road.Bridge != null) ? road.Bridge.Data.MaterialIndex : 0;

            Debug.Log("[Editor] Updating bridge mesh with " + bridgeMesh.Vertices?.Length + " vertices");
            existingRoad.UpdateBridge(bridgeMesh, road.Bridge, bridgeMatID);




            // --- Handle Railings ---
            var railingUpdates = new List<(GenericMeshData, Railing)>();
            if (road.Railings != null)
            {
                foreach (var railing in road.Railings)
                {
                    // Generate mesh for each railing
                    var railingMesh = RoadProcessor.BuildRailingMesh(railing, road);
                    railingUpdates.Add((railingMesh, railing));
                }
            }
            existingRoad.UpdateRailings(railingUpdates);

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
            bool useDistanceBasedWidthUV = EditorGUILayout.Toggle("Distance-Based Width UV", _selectedRoad.Data.UseDistanceBasedWidthUV);
            bool useDistanceBasedLengthUV = EditorGUILayout.Toggle("Distance-Based Length UV", _selectedRoad.Data.UseDistanceBasedLengthUV);
            if (EditorGUI.EndChangeCheck())
            {
                _selectedRoad.Data.UseDistanceBasedWidthUV = useDistanceBasedWidthUV;
                _selectedRoad.Data.UseDistanceBasedLengthUV = useDistanceBasedLengthUV;
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

            // Layer and Tag
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Unity Scene Settings", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            string roadLayer = EditorGUILayout.TextField("Layer", _selectedRoad.Data.Layer ?? "");
            string roadTag = EditorGUILayout.TextField("Tag", _selectedRoad.Data.Tag ?? "");
            if (EditorGUI.EndChangeCheck())
            {
                _selectedRoad.Data.Layer = roadLayer;
                _selectedRoad.Data.Tag = roadTag;
                RoadRebuildQueue.MarkDirty(_selectedRoad);
            }

            // Collider and Physics Material
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Physics Settings", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            bool enableRoadCollider = EditorGUILayout.Toggle("Enable Collider", _selectedRoad.Data.EnableCollider);
            
            bool hasRoadPhysicsMaterials = _target != null && _target.RoadPhysicsMaterials != null && _target.RoadPhysicsMaterials.Count > 0;
            EditorGUI.BeginDisabledGroup(!hasRoadPhysicsMaterials || !enableRoadCollider);
            int maxRoadPhysMatIndex = hasRoadPhysicsMaterials ? Mathf.Max(0, _target.RoadPhysicsMaterials.Count - 1) : 0;
            int roadPhysicsMaterialIndex = EditorGUILayout.IntSlider("Physics Material Index", _selectedRoad.Data.PhysicsMaterialIndex, 0, maxRoadPhysMatIndex);
            EditorGUI.EndDisabledGroup();
            
            if (!hasRoadPhysicsMaterials)
            {
                EditorGUILayout.HelpBox("No road physics materials assigned.", MessageType.Info);
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                _selectedRoad.Data.EnableCollider = enableRoadCollider;
                _selectedRoad.Data.PhysicsMaterialIndex = roadPhysicsMaterialIndex;
                RoadRebuildQueue.MarkDirty(_selectedRoad);
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

            // Bridge Section
            EditorGUILayout.Space();
            DrawBridgeInspector();

            // Railings Section
            EditorGUILayout.Space();
            DrawRailingsInspector();

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

        /// <summary>
        /// Draws the bridge inspector UI for the selected road.
        /// </summary>
        private void DrawBridgeInspector()
        {
            EditorGUILayout.LabelField("Bridge", EditorStyles.boldLabel);

            bool hasBridge = _selectedRoad.Bridge != null && _selectedRoad.Bridge.Data.Enabled;

            EditorGUI.BeginChangeCheck();
            bool enableBridge = EditorGUILayout.Toggle("Enable Bridge", hasBridge);

            if (EditorGUI.EndChangeCheck())
            {
                Debug.Log("[Editor] Enabling bridge: " + enableBridge);
                _circuitShaper.SetRoadBridgeEnabled(_selectedRoad, enableBridge);
            }

            if (hasBridge && _selectedRoad.Bridge != null)
            {
                EditorGUI.indentLevel++;

                // Material Index
                bool hasBridgeMaterials = _target != null && _target.BridgeMaterials != null && _target.BridgeMaterials.Count > 0;
                EditorGUI.BeginDisabledGroup(!hasBridgeMaterials);

                EditorGUI.BeginChangeCheck();
                int maxBridgeMatIndex = hasBridgeMaterials ? Mathf.Max(0, _target.BridgeMaterials.Count - 1) : 0;
                int bridgeMaterialIndex = EditorGUILayout.IntSlider("Material Index", _selectedRoad.Bridge.Data.MaterialIndex, 0, maxBridgeMatIndex);
                if (EditorGUI.EndChangeCheck())
                {
                    _selectedRoad.Bridge.Data.MaterialIndex = bridgeMaterialIndex;
                    RoadRebuildQueue.MarkDirty(_selectedRoad);
                }

                EditorGUI.EndDisabledGroup();
                if (!hasBridgeMaterials)
                {
                    EditorGUILayout.HelpBox("No bridge materials assigned. Add materials to the OnomiCircuitShaper component.", MessageType.Info);
                }

                // UV Settings for Bridge
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("UV Settings", EditorStyles.boldLabel);

                var bUvTile = (System.Numerics.Vector2)_selectedRoad.Bridge.Data.UVTile;
                var bUvOffset = (System.Numerics.Vector2)_selectedRoad.Bridge.Data.UVOffset;
                UnityEngine.Vector2 bridgeTile = new UnityEngine.Vector2(bUvTile.X, bUvTile.Y);
                UnityEngine.Vector2 bridgeOffset = new UnityEngine.Vector2(bUvOffset.X, bUvOffset.Y);

                EditorGUI.BeginChangeCheck();
                bridgeTile = EditorGUILayout.Vector2Field("Tile", bridgeTile);
                bridgeOffset = EditorGUILayout.Vector2Field("Offset", bridgeOffset);
                if (EditorGUI.EndChangeCheck())
                {
                    _selectedRoad.Bridge.Data.UVTile = (SerializableVector2)(new System.Numerics.Vector2(bridgeTile.x, bridgeTile.y));
                    _selectedRoad.Bridge.Data.UVOffset = (SerializableVector2)(new System.Numerics.Vector2(bridgeOffset.x, bridgeOffset.y));
                    RoadRebuildQueue.MarkDirty(_selectedRoad);
                }

                EditorGUI.BeginChangeCheck();
                bool bridgeUseDistanceBasedWidthUV = EditorGUILayout.Toggle("Distance-Based Width UV", _selectedRoad.Bridge.Data.UseDistanceBasedWidthUV);
                bool bridgeUseDistanceBasedLengthUV = EditorGUILayout.Toggle("Distance-Based Length UV", _selectedRoad.Bridge.Data.UseDistanceBasedLengthUV);
                if (EditorGUI.EndChangeCheck())
                {
                    _selectedRoad.Bridge.Data.UseDistanceBasedWidthUV = bridgeUseDistanceBasedWidthUV;
                    _selectedRoad.Bridge.Data.UseDistanceBasedLengthUV = bridgeUseDistanceBasedLengthUV;
                    RoadRebuildQueue.MarkDirty(_selectedRoad);
                }

                // Layer and Tag
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Unity Scene Settings", EditorStyles.label);
                EditorGUI.BeginChangeCheck();
                string bridgeLayer = EditorGUILayout.TextField("Layer", _selectedRoad.Bridge.Data.Layer ?? "");
                string bridgeTag = EditorGUILayout.TextField("Tag", _selectedRoad.Bridge.Data.Tag ?? "");
                if (EditorGUI.EndChangeCheck())
                {
                    _selectedRoad.Bridge.Data.Layer = bridgeLayer;
                    _selectedRoad.Bridge.Data.Tag = bridgeTag;
                    RoadRebuildQueue.MarkDirty(_selectedRoad);
                }

                // Collider and Physics Material
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Physics Settings", EditorStyles.label);
                EditorGUI.BeginChangeCheck();
                bool enableBridgeCollider = EditorGUILayout.Toggle("Enable Collider", _selectedRoad.Bridge.Data.EnableCollider);
                
                bool hasBridgePhysicsMaterials = _target != null && _target.BridgePhysicsMaterials != null && _target.BridgePhysicsMaterials.Count > 0;
                EditorGUI.BeginDisabledGroup(!hasBridgePhysicsMaterials || !enableBridgeCollider);
                int maxBridgePhysMatIndex = hasBridgePhysicsMaterials ? Mathf.Max(0, _target.BridgePhysicsMaterials.Count - 1) : 0;
                int bridgePhysicsMaterialIndex = EditorGUILayout.IntSlider("Physics Material Index", _selectedRoad.Bridge.Data.PhysicsMaterialIndex, 0, maxBridgePhysMatIndex);
                EditorGUI.EndDisabledGroup();
                
                if (!hasBridgePhysicsMaterials)
                {
                    EditorGUILayout.HelpBox("No bridge physics materials assigned.", MessageType.Info);
                }
                
                if (EditorGUI.EndChangeCheck())
                {
                    _selectedRoad.Bridge.Data.EnableCollider = enableBridgeCollider;
                    _selectedRoad.Bridge.Data.PhysicsMaterialIndex = bridgePhysicsMaterialIndex;
                    RoadRebuildQueue.MarkDirty(_selectedRoad);
                }

                // Template settings
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Template Settings", EditorStyles.miniBoldLabel);

                EditorGUI.BeginChangeCheck();
                float edgeWidth = EditorGUILayout.FloatField("Edge Width", _selectedRoad.Bridge.Data.TemplateEdgeWidth);
                float bridgeHeight = EditorGUILayout.FloatField("Bridge Height", _selectedRoad.Bridge.Data.TemplateBridgeHeight);
                float flangeWidth = EditorGUILayout.FloatField("Flange Width", _selectedRoad.Bridge.Data.TemplateFlangeWidth);
                float flangeHeight = EditorGUILayout.FloatField("Flange Height", _selectedRoad.Bridge.Data.TemplateFlangeHeight);
                float flangeDepth = EditorGUILayout.FloatField("Flange Depth", _selectedRoad.Bridge.Data.TemplateFlangeDepth);
                float curbHeight = EditorGUILayout.FloatField("Curb Height", _selectedRoad.Bridge.Data.TemplateCurbHeight);

                if (EditorGUI.EndChangeCheck())
                {
                    _selectedRoad.Bridge.Data.TemplateEdgeWidth = edgeWidth;
                    _selectedRoad.Bridge.Data.TemplateBridgeHeight = bridgeHeight;
                    _selectedRoad.Bridge.Data.TemplateFlangeWidth = flangeWidth;
                    _selectedRoad.Bridge.Data.TemplateFlangeHeight = flangeHeight;
                    _selectedRoad.Bridge.Data.TemplateFlangeDepth = flangeDepth;
                    _selectedRoad.Bridge.Data.TemplateCurbHeight = curbHeight;
                    RoadRebuildQueue.MarkDirty(_selectedRoad);
                }

                EditorGUI.indentLevel--;
            }
        }

        /// <summary>
        /// Draws the railings inspector UI for the selected road.
        /// </summary>
        private void DrawRailingsInspector()
        {
            EditorGUILayout.LabelField("Railings", EditorStyles.boldLabel);

            if (_selectedRoad.Railings == null)
            {
                _selectedRoad.Data.Railings = new List<RailingData>();
            }

            int railingCount = _selectedRoad.Railings.Count;
            EditorGUILayout.LabelField($"Count: {railingCount}");

            // Add/Remove buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Railing", GUILayout.Height(25)))
            {
                _circuitShaper.AddRailingToRoad(_selectedRoad);
            }

            EditorGUI.BeginDisabledGroup(railingCount == 0);
            if (GUILayout.Button("Remove Last", GUILayout.Height(25)))
            {
                if (railingCount > 0)
                {
                    _circuitShaper.RemoveRailingFromRoad(_selectedRoad, railingCount - 1);
                }
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            // Draw each railing
            for (int i = 0; i < _selectedRoad.Railings.Count; i++)
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                var railing = _selectedRoad.Railings[i];
                var railingData = railing.Data;

                EditorGUILayout.LabelField($"Railing {i}", EditorStyles.boldLabel);

                EditorGUI.indentLevel++;

                EditorGUI.BeginChangeCheck();

                // Visibility
                bool isVisible = EditorGUILayout.Toggle("Visible", railingData.IsVisible);

                // Material
                bool hasRailingMaterials = _target != null && _target.RailingMaterials != null && _target.RailingMaterials.Count > 0;
                EditorGUI.BeginDisabledGroup(!hasRailingMaterials);
                int maxRailingMatIndex = hasRailingMaterials ? Mathf.Max(0, _target.RailingMaterials.Count - 1) : 0;
                int railingMaterialIndex = EditorGUILayout.IntSlider("Material Index", railingData.MaterialIndex, 0, maxRailingMatIndex);
                EditorGUI.EndDisabledGroup();

                if (!hasRailingMaterials)
                {
                    EditorGUILayout.HelpBox("No railing materials assigned.", MessageType.Info);
                }

                // Properties
                float railingHeight = EditorGUILayout.FloatField("Height", railingData.RailingHeight);
                float min = EditorGUILayout.Slider("Min (Length)", railingData.Min, 0f, 1f);
                float max = EditorGUILayout.Slider("Max (Length)", railingData.Max, 0f, 1f);
                float horizontalPos = EditorGUILayout.Slider("Horizontal Position", railingData.HorizontalPosition, 0f, 1f);

                // UV Settings for this railing
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("UV Settings", EditorStyles.label);
                var rUvTile = (System.Numerics.Vector2)railingData.UVTile;
                var rUvOffset = (System.Numerics.Vector2)railingData.UVOffset;
                UnityEngine.Vector2 railingTile = new UnityEngine.Vector2(rUvTile.X, rUvTile.Y);
                UnityEngine.Vector2 railingOffset = new UnityEngine.Vector2(rUvOffset.X, rUvOffset.Y);

                railingTile = EditorGUILayout.Vector2Field("Tile", railingTile);
                railingOffset = EditorGUILayout.Vector2Field("Offset", railingOffset);

                bool railingUseDistanceBasedWidthUV = EditorGUILayout.Toggle("Distance-Based Width UV", railingData.UseDistanceBasedWidthUV);
                bool railingUseDistanceBasedLengthUV = EditorGUILayout.Toggle("Distance-Based Length UV", railingData.UseDistanceBasedLengthUV);

                // Sidedness
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Collision Settings", EditorStyles.label);
                RailingSidedness sidedness = (RailingSidedness)EditorGUILayout.EnumPopup("Sidedness", railingData.Sidedness);

                // Layer and Tag
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Unity Scene Settings", EditorStyles.label);
                string railingLayer = EditorGUILayout.TextField("Layer", railingData.Layer ?? "");
                string railingTag = EditorGUILayout.TextField("Tag", railingData.Tag ?? "");

                // Collider and Physics Material
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Rendering & Physics Settings", EditorStyles.label);
                bool enableMeshRenderer = EditorGUILayout.Toggle("Enable Mesh Renderer", railingData.EnableMeshRenderer);
                bool enableRailingCollider = EditorGUILayout.Toggle("Enable Collider", railingData.EnableCollider);
                
                bool hasRailingPhysicsMaterials = _target != null && _target.RailingPhysicsMaterials != null && _target.RailingPhysicsMaterials.Count > 0;
                EditorGUI.BeginDisabledGroup(!hasRailingPhysicsMaterials || !enableRailingCollider);
                int maxRailingPhysMatIndex = hasRailingPhysicsMaterials ? Mathf.Max(0, _target.RailingPhysicsMaterials.Count - 1) : 0;
                int railingPhysicsMaterialIndex = EditorGUILayout.IntSlider("Physics Material Index", railingData.PhysicsMaterialIndex, 0, maxRailingPhysMatIndex);
                EditorGUI.EndDisabledGroup();
                
                if (!hasRailingPhysicsMaterials)
                {
                    EditorGUILayout.HelpBox("No railing physics materials assigned.", MessageType.Info);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    railingData.IsVisible = isVisible;
                    railingData.MaterialIndex = railingMaterialIndex;
                    railingData.RailingHeight = railingHeight;
                    railingData.Min = Mathf.Min(min, max - 0.01f);
                    railingData.Max = Mathf.Max(max, min + 0.01f);
                    railingData.HorizontalPosition = horizontalPos;
                    railingData.UVTile = (SerializableVector2)(new System.Numerics.Vector2(railingTile.x, railingTile.y));
                    railingData.UVOffset = (SerializableVector2)(new System.Numerics.Vector2(railingOffset.x, railingOffset.y));
                    railingData.UseDistanceBasedWidthUV = railingUseDistanceBasedWidthUV;
                    railingData.UseDistanceBasedLengthUV = railingUseDistanceBasedLengthUV;
                    railingData.Sidedness = sidedness;
                    railingData.Layer = railingLayer;
                    railingData.Tag = railingTag;
                    railingData.EnableMeshRenderer = enableMeshRenderer;
                    railingData.EnableCollider = enableRailingCollider;
                    railingData.PhysicsMaterialIndex = railingPhysicsMaterialIndex;
                    RoadRebuildQueue.MarkDirty(_selectedRoad);
                }

                // Delete button for this specific railing
                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                if (GUILayout.Button($"Delete Railing {i}", GUILayout.Height(20)))
                {
                    _circuitShaper.RemoveRailingFromRoad(_selectedRoad, i);
                    break; // Exit loop since we modified the list
                }
                GUI.backgroundColor = Color.white;

                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();
            }
        }

        /// <summary>
        /// Draws the waypoint generation UI.
        /// </summary>
        private void DrawWaypointInspector()
        {
            EditorGUILayout.LabelField("Waypoints", EditorStyles.boldLabel);

            if (_target.WaypointSettings == null)
            {
                _target.WaypointSettings = new WaypointSettings();
            }

            EditorGUI.BeginChangeCheck();

            // Prefab assignment
            GameObject waypointPrefab = (GameObject)EditorGUILayout.ObjectField(
                "Waypoint Prefab",
                _target.WaypointPrefab,
                typeof(GameObject),
                false);

            if (waypointPrefab != _target.WaypointPrefab)
            {
                _target.WaypointPrefab = waypointPrefab;
                EditorUtility.SetDirty(_target);
            }

            if (_target.WaypointPrefab == null)
            {
                EditorGUILayout.HelpBox("Assign a prefab (e.g., a 1m cube) to use as the waypoint template.", MessageType.Info);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Waypoint Settings", EditorStyles.label);

            // Approximation Quality
            float quality = EditorGUILayout.Slider(
                new GUIContent("Approximation Quality", "Higher values place more waypoints on curves. Range 0.1-100. Low values (0.1-5) for checkpoints."),
                _target.WaypointSettings.ApproximationQuality,
                0.1f,
                100f);

            // Width Buffer
            float widthBuffer = EditorGUILayout.FloatField(
                new GUIContent("Width Buffer", "Added to road width. Positive = wider, negative = narrower."),
                _target.WaypointSettings.WidthBuffer);

            // Height
            float height = EditorGUILayout.FloatField(
                new GUIContent("Height", "Waypoint height (perpendicular to road surface)."),
                _target.WaypointSettings.Height);

            // Depth
            float depth = EditorGUILayout.FloatField(
                new GUIContent("Depth", "Waypoint depth (along road direction)."),
                _target.WaypointSettings.Depth);

            // Min Spacing
            float minSpacing = EditorGUILayout.FloatField(
                new GUIContent("Min Spacing", "Minimum distance between waypoints."),
                _target.WaypointSettings.MinWaypointSpacing);

            // Max Spacing
            float maxSpacing = EditorGUILayout.FloatField(
                new GUIContent("Max Spacing", "Maximum distance between waypoints."),
                _target.WaypointSettings.MaxWaypointSpacing);

            // Curvature Threshold
            float curvatureThreshold = EditorGUILayout.Slider(
                new GUIContent("Curvature Threshold", "Sensitivity to curves. Higher = more points on curves."),
                _target.WaypointSettings.CurvatureThreshold,
                0.01f,
                1.0f);

            if (EditorGUI.EndChangeCheck())
            {
                _target.WaypointSettings.ApproximationQuality = quality;
                _target.WaypointSettings.WidthBuffer = widthBuffer;
                _target.WaypointSettings.Height = height;
                _target.WaypointSettings.Depth = depth;
                _target.WaypointSettings.MinWaypointSpacing = Mathf.Max(0.1f, minSpacing);
                _target.WaypointSettings.MaxWaypointSpacing = Mathf.Max(_target.WaypointSettings.MinWaypointSpacing + 0.1f, maxSpacing);
                _target.WaypointSettings.CurvatureThreshold = curvatureThreshold;
                EditorUtility.SetDirty(_target);
            }

            EditorGUILayout.Space();

            // Curve Selection
            if (_circuitShaper != null && _circuitShaper.GetLiveCircuit != null)
            {
                var circuit = _circuitShaper.GetLiveCircuit;
                int curveCount = circuit.Curves.Count;

                // Initialize selection array if needed
                if (_selectedCurvesForWaypoints == null || _selectedCurvesForWaypoints.Length != curveCount)
                {
                    _selectedCurvesForWaypoints = new bool[curveCount];
                    // Default: all curves selected
                    for (int i = 0; i < curveCount; i++)
                    {
                        _selectedCurvesForWaypoints[i] = true;
                    }
                }

                _waypointCurveSelectionFoldout = EditorGUILayout.Foldout(_waypointCurveSelectionFoldout, "Curve Selection", true);
                
                if (_waypointCurveSelectionFoldout)
                {
                    EditorGUI.indentLevel++;
                    
                    // Select All / Deselect All buttons
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Select All", GUILayout.Width(100)))
                    {
                        for (int i = 0; i < _selectedCurvesForWaypoints.Length; i++)
                        {
                            _selectedCurvesForWaypoints[i] = true;
                        }
                    }
                    if (GUILayout.Button("Deselect All", GUILayout.Width(100)))
                    {
                        for (int i = 0; i < _selectedCurvesForWaypoints.Length; i++)
                        {
                            _selectedCurvesForWaypoints[i] = false;
                        }
                    }
                    GUILayout.EndHorizontal();

                    // Individual curve checkboxes
                    for (int i = 0; i < curveCount; i++)
                    {
                        int pointCount = circuit.Curves[i].Points.Count;
                        string curveLabel = $"Curve {i} ({pointCount} points)";
                        _selectedCurvesForWaypoints[i] = EditorGUILayout.Toggle(curveLabel, _selectedCurvesForWaypoints[i]);
                    }

                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space();
            }

            // Display existing waypoint info
            int totalWaypoints = 0;
            foreach (var curve in _target.WaypointCurves)
            {
                if (curve != null)
                {
                    totalWaypoints += curve.transform.childCount;
                }
            }

            if (_target.WaypointCurves.Count > 0 && totalWaypoints > 0)
            {
                EditorGUILayout.HelpBox($"{_target.WaypointCurves.Count} waypoint curves with {totalWaypoints} total waypoints.", MessageType.Info);
            }

            // Create Button
            EditorGUI.BeginDisabledGroup(_target.WaypointPrefab == null);
            GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
            if (GUILayout.Button("Create Waypoints for Selected Curves", GUILayout.Height(30)))
            {
                CreateWaypoints();
            }
            GUI.backgroundColor = Color.white;
            EditorGUI.EndDisabledGroup();

            // Clear Button
            if (_target.WaypointCurves.Count > 0)
            {
                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                if (GUILayout.Button("Clear All Waypoints", GUILayout.Height(25)))
                {
                    if (EditorUtility.DisplayDialog("Clear Waypoints",
                        "Are you sure you want to delete all waypoints?",
                        "Delete", "Cancel"))
                    {
                        ClearAllWaypoints();
                    }
                }
                GUI.backgroundColor = Color.white;
            }
        }

        /// <summary>
        /// Creates waypoints for selected curves in the circuit.
        /// </summary>
        private void CreateWaypoints()
        {
            if (_target.WaypointPrefab == null)
            {
                Debug.LogWarning("No waypoint prefab assigned!");
                return;
            }

            if (_circuitShaper == null || _circuitShaper.GetLiveCircuit == null)
            {
                Debug.LogWarning("No circuit data available!");
                return;
            }

            // Clear existing waypoints first
            ClearAllWaypoints();

            var circuit = _circuitShaper.GetLiveCircuit;
            int curveIndex = 0;
            int createdCount = 0;

            foreach (var circuitCurve in circuit.Curves)
            {
                // Skip if not enough points
                if (circuitCurve.Points.Count < 2)
                {
                    curveIndex++;
                    continue;
                }

                // Skip if curve not selected for waypoints
                if (_selectedCurvesForWaypoints != null && 
                    curveIndex < _selectedCurvesForWaypoints.Length && 
                    !_selectedCurvesForWaypoints[curveIndex])
                {
                    curveIndex++;
                    continue;
                }

                // Create waypoint curve container
                GameObject curveObj = new GameObject($"Waypoint Curve {curveIndex}");
                curveObj.transform.SetParent(_target.transform);
                curveObj.transform.localPosition = UnityEngine.Vector3.zero;
                curveObj.transform.localRotation = UnityEngine.Quaternion.identity;

                SceneWaypointCurve waypointCurve = curveObj.AddComponent<SceneWaypointCurve>();

                // Convert circuit points to array
                var pointDataArray = circuitCurve.Points.Select(p => p.Data).ToArray();

                // Create function to get road width at each point index
                System.Func<int, float> getRoadWidth = (pointIndex) =>
                {
                    if (pointIndex < 0 || pointIndex >= circuitCurve.Points.Count)
                    {
                        return 10f; // Default width
                    }

                    var point = circuitCurve.Points[pointIndex];
                    if (point.CrossSection != null && point.CrossSection.Points.Count >= 2)
                    {
                        // Calculate width from cross-section extents
                        float minX = float.MaxValue;
                        float maxX = float.MinValue;

                        foreach (var csPoint in point.CrossSection.Points)
                        {
                            // Convert SerializableVector3 to System.Numerics.Vector3 to access X
                            System.Numerics.Vector3 pos = csPoint.Data.PointPosition;
                            minX = Mathf.Min(minX, pos.X);
                            maxX = Mathf.Max(maxX, pos.X);
                        }

                        return maxX - minX;
                    }

                    return 10f; // Default width if no cross-section
                };

                // Get scale and offset for transforms
                Vector3 basePosition = _target.transform.position;
                float scale = _target.Data.settingsData.ScaleMultiplier;

                // Generate waypoints
                var waypoints = WaypointProcessor.GenerateWaypoints(
                    pointDataArray,
                    _target.WaypointSettings,
                    circuitCurve.Data.IsClosed,
                    getRoadWidth);

                // Create waypoint GameObjects with scale and offset
                waypointCurve.CreateWaypoints(waypoints, _target.WaypointPrefab, curveIndex, _target, basePosition, scale);

                _target.WaypointCurves.Add(waypointCurve);

                createdCount++;
                curveIndex++;
            }

            if (createdCount > 0)
            {
                Debug.Log($"Created waypoints for {createdCount} curve(s)");
            }
            else
            {
                Debug.LogWarning("No curves selected for waypoint generation!");
            }
            
            EditorUtility.SetDirty(_target);
        }

        /// <summary>
        /// Clears all waypoint curves and their children.
        /// </summary>
        private void ClearAllWaypoints()
        {
            foreach (var curve in _target.WaypointCurves)
            {
                if (curve != null)
                {
                    DestroyImmediate(curve.gameObject);
                }
            }
            _target.WaypointCurves.Clear();
            EditorUtility.SetDirty(_target);
            Debug.Log("Cleared all waypoints");
        }
    }
}