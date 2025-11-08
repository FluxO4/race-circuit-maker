using OnomiCircuitShaper.Unity;
using UnityEditor;
using UnityEngine;

namespace OnomiCircuitShaper.Unity.Editor
{
    /// <summary>
    /// The custom editor for the main OnomiCircuitShaper MonoBehaviour. This class is
    /// responsible for drawing all the UI in the Inspector and all the interactive
    /// handles in the Scene View. It is the primary entry point for user interaction.
    /// </summary>
    [CustomEditor(typeof(OnomiCircuitShaper))]
    public class OnomiCircuitShaperEditor : UnityEditor.Editor
    {
        private OnomiCircuitShaper _target;

        private void OnEnable()
        {
            _target = (OnomiCircuitShaper)target;
        }

        public override void OnInspectorGUI()
        {
            // Draw the default inspector for now.
            DrawDefaultInspector();

            // Buttons for starting and stopping edit mode will go here.
            if (GUILayout.Button("Begin Edit"))
            {
                // Logic to initialize the ICircuitShaper interface and enter edit mode.
            }

            if (GUILayout.Button("End Edit"))
            {
                // Logic to tear down the editing session.
            }
        }

        private void OnSceneGUI()
        {
            // All Scene View drawing logic will go here.
            // This will involve:
            // 1. Getting the live "Edit Realm" data from the ICircuitShaper interface.
            // 2. Drawing handles for points, control points, etc.
            // 3. Detecting user input on those handles.
            // 4. Calling the appropriate methods on the ICircuitShaper interface in response to input.
        }
    }
}
