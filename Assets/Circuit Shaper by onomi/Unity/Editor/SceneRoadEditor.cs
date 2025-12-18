using OnomiCircuitShaper.Engine.Data;
using OnomiCircuitShaper.Engine.EditRealm;
using OnomiCircuitShaper.Engine.Interface;
using OnomiCircuitShaper.Engine.Processors;
using OnomiCircuitShaper.Unity.Utilities;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace OnomiCircuitShaper.Unity.Editor
{
    /// <summary>
    /// Custom editor for SceneRoad that provides road-specific UI and scene handles.
    /// This allows editing individual roads without the overhead of the full OnomiCircuitShaperEditor.
    /// </summary>
    [CustomEditor(typeof(SceneRoad))]
    public class SceneRoadEditor : UnityEditor.Editor
    {
        private SceneRoad _sceneRoad;
        private Road _road;
        private OnomiCircuitShaper _shaper;
        private ICircuitShaper _circuitShaper;
        
        // Track if we need to rebuild
        private float _lastRebuildTime = 0f;
        private const float MinRebuildInterval = 0.1f;

        private void OnEnable()
        {
            _sceneRoad = (SceneRoad)target;
            _road = _sceneRoad?.associatedRoad;
            _shaper = _sceneRoad?.onomiCircuitShaper;
            
            if (_shaper != null && _road != null)
            {
                // Create a circuit shaper interface for segment range operations
                _circuitShaper = new CircuitShaper(_shaper.Data.circuitData, _shaper.Data.settingsData);
                _circuitShaper.BeginEdit();
            }
            
            // Subscribe to scene view updates
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            if (_circuitShaper != null)
            {
                _circuitShaper.QuitEdit();
                _circuitShaper = null;
            }
            
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        public override void OnInspectorGUI()
        {
            // Validate references
            if (_sceneRoad == null)
            {
                EditorGUILayout.HelpBox("SceneRoad reference is null.", MessageType.Error);
                return;
            }

            if (_sceneRoad.associatedRoad == null || _sceneRoad.associatedRoad.Data == null)
            {
                EditorGUILayout.HelpBox("No associated road data. This SceneRoad may be orphaned.", MessageType.Warning);
                DrawDefaultInspector();
                return;
            }

            if (_shaper == null)
            {
                EditorGUILayout.HelpBox("No OnomiCircuitShaper reference. Cannot edit road properties.", MessageType.Warning);
                DrawDefaultInspector();
                return;
            }

            _road = _sceneRoad.associatedRoad;

            // Draw header
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Road Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Road Name
            EditorGUI.BeginChangeCheck();
            string roadName = EditorGUILayout.TextField("Road Name", _road.Data.Name ?? "Road");
            if (EditorGUI.EndChangeCheck())
            {
                _road.Data.Name = roadName;
                _sceneRoad.gameObject.name = !string.IsNullOrEmpty(roadName) ? roadName : "Road";
                EditorUtility.SetDirty(_shaper);
            }

            EditorGUILayout.Space();

            // Draw road settings using shared utility
            if (RoadEditorUtility.DrawRoadSettings(_road, _shaper, _circuitShaper, showSegmentRange: true))
            {
                EditorUtility.SetDirty(_shaper);
                ProcessRebuildQueue();
            }

            // Bridge Section
            EditorGUILayout.Space();
            if (RoadEditorUtility.DrawBridgeInspector(_road, _shaper, _circuitShaper))
            {
                EditorUtility.SetDirty(_shaper);
                ProcessRebuildQueue();
            }

            // Railings Section
            EditorGUILayout.Space();
            if (RoadEditorUtility.DrawRailingsInspector(_road, _shaper, _circuitShaper))
            {
                EditorUtility.SetDirty(_shaper);
                ProcessRebuildQueue();
            }

            // Navigation
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Navigation", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Select Parent Circuit Shaper"))
            {
                Selection.activeGameObject = _shaper.gameObject;
            }

            // Process any pending rebuilds
            ProcessRebuildQueue();
        }

        /// <summary>
        /// Process any pending road rebuilds from the queue.
        /// </summary>
        private void ProcessRebuildQueue()
        {
            float currentTime = (float)EditorApplication.timeSinceStartup;
            if (currentTime - _lastRebuildTime < MinRebuildInterval) return;

            var dirtyRoads = RoadRebuildQueue.GetAndClearDirtyRoads();
            
            foreach (var road in dirtyRoads)
            {
                if (road == _road)
                {
                    RoadEditorUtility.RebuildRoad(road, _sceneRoad, _shaper);
                }
                else
                {
                    // Re-add other roads back to the queue for the main editor to handle
                    RoadRebuildQueue.MarkDirty(road);
                }
            }

            _lastRebuildTime = currentTime;
        }

        /// <summary>
        /// Scene GUI for drawing road curve handles.
        /// </summary>
        private void OnSceneGUI(SceneView sceneView)
        {
            if (_sceneRoad == null || _road == null || _shaper == null) return;
            if (_road.parentCurve == null) return;

            Vector3 basePosition = _shaper.transform.position;
            float scale = _shaper.Data.settingsData.ScaleMultiplier;
            bool freeMoveMode = _shaper.Data.settingsData.FreeMoveMode;
            float rotatorDistance = _shaper.Data.settingsData.RotatorPointDistance;

            // Get the points this road covers
            var roadPoints = _road.parentCurve.GetPointsFromSegmentRange(
                _road.Data.startSegmentIndex, 
                _road.Data.endSegmentIndex);

            if (roadPoints == null || roadPoints.Count < 2) return;

            // Draw the road curve (highlighted) using shared utility
            PointEditorUtility.DrawCurve(roadPoints, basePosition, scale, Color.white, 6f);

            // Draw point handles using shared utility
            bool changed = PointEditorUtility.DrawPointHandles(
                roadPoints, 
                _circuitShaper, 
                basePosition, 
                scale, 
                freeMoveMode,
                _circuitShaper?.SelectedPoints,
                rotatorDistance);

            // If any changes were made, mark road for rebuild
            if (changed)
            {
                Undo.RecordObject(_shaper, "Edit Road Points");
                RoadRebuildQueue.MarkDirty(_road);
                EditorUtility.SetDirty(_shaper);
                ProcessRebuildQueue();
                SceneView.RepaintAll();
            }
        }
    }
}
