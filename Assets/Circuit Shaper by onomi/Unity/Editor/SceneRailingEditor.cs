using OnomiCircuitShaper.Engine.EditRealm;
using OnomiCircuitShaper.Engine.Interface;
using UnityEditor;
using UnityEngine;

namespace OnomiCircuitShaper.Unity.Editor
{
    /// <summary>
    /// Custom editor for SceneRailing that provides railing-specific UI.
    /// </summary>
    [CustomEditor(typeof(SceneRailing))]
    public class SceneRailingEditor : UnityEditor.Editor
    {
        private SceneRailing _sceneRailing;
        private Railing _railing;
        private Road _parentRoad;
        private OnomiCircuitShaper _shaper;
        
        // Track rebuild timing
        private float _lastRebuildTime = 0f;
        private const float MinRebuildInterval = 0.1f;

        private void OnEnable()
        {
            _sceneRailing = (SceneRailing)target;
            _railing = _sceneRailing?.associatedRailing;
            _shaper = _sceneRailing?.onomiCircuitShaper;
            
            // Get parent road from the railing
            if (_railing != null)
            {
                _parentRoad = _railing.ParentRoad;
            }
        }

        public override void OnInspectorGUI()
        {
            // Validate references
            if (_sceneRailing == null)
            {
                EditorGUILayout.HelpBox("SceneRailing reference is null.", MessageType.Error);
                return;
            }

            if (_sceneRailing.associatedRailing == null || _sceneRailing.associatedRailing.Data == null)
            {
                EditorGUILayout.HelpBox("No associated railing data. This SceneRailing may be orphaned.", MessageType.Warning);
                DrawDefaultInspector();
                return;
            }

            if (_shaper == null)
            {
                EditorGUILayout.HelpBox("No OnomiCircuitShaper reference. Cannot edit railing properties.", MessageType.Warning);
                DrawDefaultInspector();
                return;
            }

            _railing = _sceneRailing.associatedRailing;
            _parentRoad = _railing.ParentRoad;

            // Draw header
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Railing Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Draw railing settings using shared utility
            if (RoadEditorUtility.DrawRailingSettings(_railing, _parentRoad, _shaper))
            {
                EditorUtility.SetDirty(_shaper);
                ProcessRebuildQueue();
            }

            // Navigation
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Navigation", EditorStyles.boldLabel);
            
            if (_parentRoad != null)
            {
                // Find the SceneRoad parent
                SceneRoad parentSceneRoad = _sceneRailing.transform.parent?.GetComponent<SceneRoad>();
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
                if (road == _parentRoad)
                {
                    // Find the parent SceneRoad and rebuild
                    SceneRoad parentSceneRoad = _sceneRailing.transform.parent?.GetComponent<SceneRoad>();
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
