using OnomiCircuitShaper.Engine.EditRealm;
using OnomiCircuitShaper.Engine.Interface;
using UnityEditor;
using UnityEngine;

namespace OnomiCircuitShaper.Unity.Editor
{
    /// <summary>
    /// Custom editor for SceneBridge that provides bridge-specific UI.
    /// </summary>
    [CustomEditor(typeof(SceneBridge))]
    public class SceneBridgeEditor : UnityEditor.Editor
    {
        private SceneBridge _sceneBridge;
        private Bridge _bridge;
        private OnomiCircuitShaper _shaper;
        
        // Track rebuild timing
        private float _lastRebuildTime = 0f;
        private const float MinRebuildInterval = 0.1f;

        private void OnEnable()
        {
            _sceneBridge = (SceneBridge)target;
            _bridge = _sceneBridge?.associatedBridge;
            _shaper = _sceneBridge?.onomiCircuitShaper;
        }

        public override void OnInspectorGUI()
        {
            // Validate references
            if (_sceneBridge == null)
            {
                EditorGUILayout.HelpBox("SceneBridge reference is null.", MessageType.Error);
                return;
            }

            if (_sceneBridge.associatedBridge == null || _sceneBridge.associatedBridge.Data == null)
            {
                EditorGUILayout.HelpBox("No associated bridge data. This SceneBridge may be orphaned.", MessageType.Warning);
                DrawDefaultInspector();
                return;
            }

            if (_shaper == null)
            {
                EditorGUILayout.HelpBox("No OnomiCircuitShaper reference. Cannot edit bridge properties.", MessageType.Warning);
                DrawDefaultInspector();
                return;
            }

            _bridge = _sceneBridge.associatedBridge;

            // Draw header
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Bridge Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Draw bridge settings using shared utility
            if (RoadEditorUtility.DrawBridgeSettings(_bridge, _shaper))
            {
                EditorUtility.SetDirty(_shaper);
                ProcessRebuildQueue();
            }

            // Navigation
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Navigation", EditorStyles.boldLabel);
            
            if (_bridge.ParentRoad != null)
            {
                // Find the SceneRoad parent
                SceneRoad parentSceneRoad = _sceneBridge.transform.parent?.GetComponent<SceneRoad>();
                if (parentSceneRoad != null)
                {
                    if (GUILayout.Button("Select Parent Road"))
                    {
                        Selection.activeGameObject = parentSceneRoad.gameObject;
                    }
                }
            }

            if (GUILayout.Button("Select Circuit Shaper"))
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
                if (road == _bridge?.ParentRoad)
                {
                    // Find the parent SceneRoad and rebuild
                    SceneRoad parentSceneRoad = _sceneBridge.transform.parent?.GetComponent<SceneRoad>();
                    if (parentSceneRoad != null)
                    {
                        RoadEditorUtility.RebuildRoad(road, parentSceneRoad, _shaper);
                    }
                }
                else
                {
                    // Re-add other roads back to the queue
                    RoadRebuildQueue.MarkDirty(road);
                }
            }

            _lastRebuildTime = currentTime;
        }
    }
}
