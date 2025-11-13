using OnomiCircuitShaper.Engine.Interface;
using UnityEditor;
using UnityEngine;
using OnomiCircuitShaper.Unity.Utilities;
using OnomiCircuitShaper.Engine.EditRealm;


namespace OnomiCircuitShaper.Unity.Editor
{
    /// <summary>
    /// The custom editor for the main OnomiCircuitShaper MonoBehaviour. This class is
    /// responsible for drawing all the UI in the Inspector and all the interactive
    /// handles in the Scene View. It is the primary entry point for user interaction.
    /// </summary>
    [CustomEditor(typeof(OnomiCircuitShaper))]
    public class OnomiCircuitShaperEditor : OnomiCircuitShaperEditorSceneGUI // Inherits scene GUI logic
    {
        private OnomiCircuitShaper _target;
        private bool _creatingNewPointMode = false;
        private bool _addingToSelectedCurveMode = false;

        private void OnEnable()
        {
            _target = (OnomiCircuitShaper)target;

            // The OnAfterDeserialize on the target will have already run,
            // so _target.Data should be fully loaded here.

            // Now, we can safely initialize the interface with the loaded data.
            _circuitShaper = new CircuitShaper(_target.Data.circuitData, _target.Data.settingsData);
            _circuitShaper.BeginEdit();

            _creatingNewPointMode = false;
            _addingToSelectedCurveMode = false;
        }
        
        private void OnDisable()
        {
            _circuitShaper.QuitEdit();
        }

        public override void OnInspectorGUI()
        {
            //Draw default inspector
            DrawDefaultInspector();

            EditorGUILayout.Space();

            //editing controls will go here.
            //section: Point creating modes
            EditorGUILayout.LabelField("Point Creation Modes:", EditorStyles.boldLabel);
            
            GUILayout.BeginHorizontal();
            // Toggle-style buttons: use Toggle with button style so they look like selectable buttons
            bool createToggle = GUILayout.Toggle(_creatingNewPointMode, "Create Point As New Curve", GUI.skin.button);

            // Only show the "Add Point To Selected Curve" button if a curve is selected
            bool addToSelectedToggle = _addingToSelectedCurveMode;
            if (_circuitShaper != null && _circuitShaper.SelectedCurve != null)
            {
            addToSelectedToggle = GUILayout.Toggle(_addingToSelectedCurveMode, "Add Point To Selected Curve", GUI.skin.button);
            }
            else
            {
            // ensure mode is off when no curve is selected and hide the button
            addToSelectedToggle = false;
            _addingToSelectedCurveMode = false;
            }
            GUILayout.EndHorizontal();

            // Ensure mutual exclusivity (only one mode active at a time)
            if (createToggle != _creatingNewPointMode)
            {
            _creatingNewPointMode = createToggle;
            if (_creatingNewPointMode)
                _addingToSelectedCurveMode = false;
            }

            if (addToSelectedToggle != _addingToSelectedCurveMode)
            {
            _addingToSelectedCurveMode = addToSelectedToggle;
            if (_addingToSelectedCurveMode)
                _creatingNewPointMode = false;
            }

            // If modes are toggled on but no curve is selected, disable add-to-selected mode
            if (_addingToSelectedCurveMode && (_circuitShaper.SelectedCurve == null || _circuitShaper.SelectedPoints == null || _circuitShaper.SelectedPoints.Count == 0))
            {
                // keep the button visible but turn it off automatically
                _addingToSelectedCurveMode = false;
            }
            

            // If any GUI element has changed, mark the object as "dirty"
            // to ensure OnBeforeSerialize() is called and the JSON is updated.
            if (GUI.changed)
            {
                EditorUtility.SetDirty(_target);
            }
        }

        private void OnSceneGUI()
        {
            // This is crucial! It tells Unity to record changes for Undo/Redo
            // and marks the object as needing to be saved.
            Undo.RecordObject(_target, "Modify Circuit Point");

            // Call the handle drawing logic from the base class
            DrawAllHandles(_target);

            //If there are any selected points, we should not draw the transform gizmo for the scene object, target. However, we can not clear the current tool since that will be used to tranform the selected points.
            Tools.hidden = _circuitShaper.SelectedPoints.Count > 0;
            
            // Handle scene clicks for adding points when in one of the edit-modes.
            Event e = Event.current;
            if ((_creatingNewPointMode || _addingToSelectedCurveMode) && e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {

                if (_creatingNewPointMode)
                {

                    //Pass camera position and forward direction
                    Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                    CircuitPoint createdPoint =_circuitShaper.AddPointAsNewCurve(ray.origin.ToLocalSpace(_target.transform.position, _target.Data.settingsData.ScaleMultiplier), ray.direction.ToNumericsVector3());
                    // switch to adding-to-selected after creation so subsequent clicks add to the freshly created curve
                    _creatingNewPointMode = false;
                    _addingToSelectedCurveMode = true;

                    //select the newly added point
                    _circuitShaper.SelectPoint(createdPoint);
                }
                else if (_addingToSelectedCurveMode)
                {
                    Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                    CircuitPoint createdPoint = _circuitShaper.AddPointToSelectedCurve(ray.origin.ToLocalSpace(_target.transform.position, _target.Data.settingsData.ScaleMultiplier), ray.direction.ToNumericsVector3());

                    //If shift is not held, exit adding mode after one addition
                    if (!e.shift)
                    {
                        _addingToSelectedCurveMode = false;
                    }
   
                    _circuitShaper.SelectPoint(createdPoint);
                    
                }

                // consume the event so Unity's scene view doesn't also use it
                e.Use();

            }
            else if ((_circuitShaper.SelectedPoints.Count > 0) && e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {

                _circuitShaper.ClearSelection();

                // consume the event so Unity's scene view doesn't also use it
                e.Use();

            }
            else if ((_circuitShaper.SelectedPoints.Count > 0) && e.type == EventType.KeyDown && e.keyCode == KeyCode.Delete && !e.alt)
            {

                _circuitShaper.DeleteSelectedPoints();

                // consume the event so Unity's scene view doesn't also use it
                e.Use();
            }
            
            
            // If any handle was moved, mark the object as "dirty"
            // to ensure OnBeforeSerialize() is called and the JSON is updated.
            if (GUI.changed)
            {
                EditorUtility.SetDirty(_target);
            }
        }
    }
}
