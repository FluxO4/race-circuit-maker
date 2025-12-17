using OnomiCircuitShaper.Engine.Interface;
using UnityEditor;
using UnityEngine;
using OnomiCircuitShaper.Unity.Utilities;
using OnomiCircuitShaper.Engine.EditRealm;
using OnomiCircuitShaper.Engine.Presets;
using OnomiCircuitShaper.Engine.Data;
using OnomiCircuitShaper.Engine.Processors;
using OnomiCircuitShaper.Engine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace OnomiCircuitShaper.Unity.Editor
{
    /// <summary>
    /// The custom editor for the main OnomiCircuitShaper MonoBehaviour. This class is
    /// responsible for drawing all the UI in the Inspector and all the interactive
    /// handles in the Scene View. It is the primary entry point for user interaction.
    /// </summary>
    [CustomEditor(typeof(OnomiCircuitShaper))]
    public partial class OnomiCircuitShaperEditor : UnityEditor.Editor // Inherits scene GUI logic
    {
        private OnomiCircuitShaper _target;
        private ICircuitShaper _circuitShaper;
        private bool _creatingNewPointMode = false;
        private bool _addingToSelectedCurveMode = false;
        private bool _isEditingCrossSection = false;

        // Road management
        private Road _selectedRoad => _circuitShaper?.SelectedRoad;
        private float _lastRoadUpdateTime = 0f;
        private const float MinRoadUpdateInterval = 0.2f; // 5 updates per second max
        
        // Track freeze state to detect changes
        private bool _wasFrozen = false;

        private void OnEnable()
        {
            _target = (OnomiCircuitShaper)target;
            _wasFrozen = _target.freeze;

            if (_target.freeze) return;

            //If playing, do not initialize editor
            if (Application.isPlaying) return;

            // The OnAfterDeserialize on the target will have already run,
            // so _target.Data should be fully loaded here.

            // Now, we can safely initialize the interface with the loaded data.
            _circuitShaper = new CircuitShaper(_target.Data.circuitData, _target.Data.settingsData);


            _circuitShaper.BeginEdit();

            // Rebuild all existing roads from data
            RebuildAllRoadsFromData();

            _creatingNewPointMode = false;
            _addingToSelectedCurveMode = false;
        }
        
        private void OnDisable()
        {

            if (_target.freeze) return;

            //If playing, do not disable editor
            if (Application.isPlaying) return;



            // Clear the rebuild queue when editor is disabled via interface
            if (_circuitShaper != null)
            {
                _circuitShaper.ClearRoadRebuildQueue();
                _circuitShaper.QuitEdit();
            }
            
            // Note: We DON'T destroy SceneRoads - they persist in the scene!
        }




        public override void OnInspectorGUI()
        {
            //Draw default inspector
            DrawDefaultInspector();

            // Detect freeze state changes
            if (_target.freeze != _wasFrozen)
            {
                if (_target.freeze)
                {
                    // Just got frozen - clean up like OnDisable
                    Debug.Log("[Editor] Freezing - cleaning up");
                    if (_circuitShaper != null)
                    {
                        _circuitShaper.ClearRoadRebuildQueue();
                        _circuitShaper.QuitEdit();
                        _circuitShaper = null;
                    }
                }
                else
                {
                    // Just got unfrozen - initialize like OnEnable
                    Debug.Log("[Editor] Unfreezing - reinitializing");
                    if (!Application.isPlaying)
                    {
                        _circuitShaper = new CircuitShaper(_target.Data.circuitData, _target.Data.settingsData);
                        _circuitShaper.BeginEdit();
                        RebuildAllRoadsFromData();
                        _creatingNewPointMode = false;
                        _addingToSelectedCurveMode = false;
                    }
                }
                _wasFrozen = _target.freeze;
            }

            // If frozen, only show default inspector
            if (_target.freeze) return;

            //If playing, do not initialize editor
            if (Application.isPlaying) return;

            

            // Process the road rebuild queue
            ProcessDirtyRoads();

            EditorGUILayout.Space();

            // Show selected road inspector if a road is selected
            if (_selectedRoad != null)
            {
                DrawSelectedRoadInspector();
            }
            
            // Context-sensitive panel based on selection
            if (_circuitShaper != null)
            {
                switch (_circuitShaper.SelectedPoints.Count)
                {
                    case 0:
                        // No points selected, maybe show general info or instructions
                        if (_selectedRoad == null)
                        {
                            EditorGUILayout.HelpBox("Select a point to edit its properties, or enable a creation mode to add new points.", MessageType.Info);
                        }
                        break;
                    case 1:
                        DrawSinglePointInspector();
                        break;
                    default: // More than 1 point selected
                        DrawMultiPointInspector();
                        break;
                }
            }
            
            //editing controls will go here.
            DrawEditingModesPanel();

            EditorGUILayout.Space();

            // Global waypoint section
            DrawWaypointInspector();

            EditorGUILayout.Space();


            // If any GUI element has changed, mark the object as "dirty"
            // to ensure OnBeforeSerialize() is called and the JSON is updated.
            if (GUI.changed)
            {
                EditorUtility.SetDirty(_target);
            }
        }



        private void DrawEditingModesPanel()
        {
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
                if (_addingToSelectedCurveMode) _addingToSelectedCurveMode = false;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            //Begin another horizontal for curve delete and close toggles
            GUILayout.BeginHorizontal();
            if (_circuitShaper != null && _circuitShaper.SelectedCurve != null)
            {
                // Button to delete the selected curveif (GUILayout.Button("Delete Selected Curve"))
                if (GUILayout.Button("Delete Selected Curve"))
                {
                    _circuitShaper.DeleteSelectedCurve();
                    _addingToSelectedCurveMode = false;
                }

                // Toggle the selected curve's closed state
                bool isClosed = _circuitShaper.SelectedCurve.Data.IsClosed;
                bool newIsClosed = GUILayout.Toggle(isClosed, "Closed", GUI.skin.button);
                if (newIsClosed != isClosed)
                {
                    _circuitShaper.SetSelectedCurveIsClosed(newIsClosed);
                }
            }
            GUILayout.EndHorizontal();

            // Ensure mutual exclusivity (only one mode active at a time)
            if (createToggle != _creatingNewPointMode)
            {
                _creatingNewPointMode = createToggle;
                if (_creatingNewPointMode)
                {
                    _addingToSelectedCurveMode = false;
                    _isEditingCrossSection = false;
                }
            }

            if (addToSelectedToggle != _addingToSelectedCurveMode)
            {
                _addingToSelectedCurveMode = addToSelectedToggle;
                if (_addingToSelectedCurveMode)
                {
                    _creatingNewPointMode = false;
                    _isEditingCrossSection = false;
                }
            }
        }

        private void DrawSinglePointInspector()
        {
            EditorGUILayout.LabelField("Selected Point:", EditorStyles.boldLabel);
            var point = _circuitShaper.SelectedPoints.First();

            // Point info
            EditorGUILayout.Vector3Field("Position", point.PointPosition.ToUnityVector3());
            
            // Navigation and Tools
            GUILayout.BeginHorizontal();
            GUI.enabled = point.PreviousPoint != null;
            if (GUILayout.Button("Select Previous")) _circuitShaper.SelectPoint(point.PreviousPoint as CircuitPoint);
            GUI.enabled = point.NextPoint != null;
            if (GUILayout.Button("Select Next")) _circuitShaper.SelectPoint(point.NextPoint as CircuitPoint);
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Move Tool")) Tools.current = Tool.Move;
            if (GUILayout.Button("Rotate Tool")) Tools.current = Tool.Rotate;
            GUILayout.EndHorizontal();

            // Cross-Section Editing
            EditorGUILayout.Space();
            bool editCrossSectionToggle = GUILayout.Toggle(_isEditingCrossSection, "Edit Cross-Section", GUI.skin.button);
            if (editCrossSectionToggle != _isEditingCrossSection)
            {
                _isEditingCrossSection = editCrossSectionToggle;
                if (_isEditingCrossSection)
                {
                    _creatingNewPointMode = false;
                    _addingToSelectedCurveMode = false;
                }
            }

            if (_isEditingCrossSection)
            {
                DrawCrossSectionEditorInspector(point);
            }
        }

        private void DrawCrossSectionEditorInspector(CircuitPoint point)
        {
            EditorGUILayout.LabelField("Cross-Section Editor", EditorStyles.boldLabel);
            var cs = point.CrossSection;

            // Point count editor
            if (cs != null && cs.Points.Count > 0)
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Point Count", GUILayout.Width(80));
                if (GUILayout.Button("-", GUILayout.Width(20))) _circuitShaper.SetCrossSectionPointCount(cs, cs.Points.Count - 1);
                EditorGUILayout.LabelField(cs.Points.Count.ToString(), EditorStyles.centeredGreyMiniLabel, GUILayout.Width(30));
                if (GUILayout.Button("+", GUILayout.Width(20))) _circuitShaper.SetCrossSectionPointCount(cs, cs.Points.Count + 1);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                // Tension slider for selected cross-section point
                if (_circuitShaper.SelectedPoints.Count == 1)
                {
                    var firstCsPoint = cs.Points.FirstOrDefault(); 
                    
                    if (firstCsPoint != null)
                    {
                        EditorGUI.BeginChangeCheck();
                        float newTension = EditorGUILayout.Slider("Auto-Set Tension", firstCsPoint.Data.AutoSetTension, 0f, 1f);
                        if (EditorGUI.EndChangeCheck())
                        {
                            // Apply tension change to all cross-section points
                            foreach (var csPoint in cs.Points)
                            _circuitShaper.SetCrossSectionPointAutoSetTension(csPoint, newTension);
                        }
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("This point has no cross-section. Apply a preset to create one.", MessageType.Info);
            }

            // Presets
            EditorGUILayout.LabelField("Presets", EditorStyles.boldLabel);
            if (GUILayout.Button("Flat")) _circuitShaper.SetCrossSectionPreset(point, CrossSectionPresets.FlatPreset);
            if (GUILayout.Button("Triangular")) _circuitShaper.SetCrossSectionPreset(point, CrossSectionPresets.TriangularPreset);
            if (GUILayout.Button("Trapezoidal")) _circuitShaper.SetCrossSectionPreset(point, CrossSectionPresets.TrapezoidalPreset);
            if (GUILayout.Button("Inverted Trapezoid")) _circuitShaper.SetCrossSectionPreset(point, CrossSectionPresets.InvertedTrapezoidalPreset);
        }

        private void DrawMultiPointInspector()
        {
            EditorGUILayout.LabelField($"{_circuitShaper.SelectedPoints.Count} Points Selected", EditorStyles.boldLabel);
            if (GUILayout.Button("Build Road From Selection"))
            {
                _circuitShaper.CreateRoadFromSelectedPoints();
            }
        }

      

        private void OnSceneGUI()
        {
            if (_target == null) return;
            
            if (_target.freeze) return;
            
            if (_circuitShaper == null) return;

            //If playing, do not initialize editor
            if (Application.isPlaying) return;

            // Process the road rebuild queue for immediate visual feedback
            ProcessDirtyRoads();

            



            Event e = Event.current;


            if (_isEditingCrossSection)
            {
                DrawCrossSectionEditorHandles(_target);
            }
            else
            {
                // Draw all regular handles when not in cross-section edit mode
                DrawAllHandles(_target, e);
            }

            HandleUtility.Repaint();


            // Handle point creation
            if ((_creatingNewPointMode || _addingToSelectedCurveMode) && e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {

                if (_creatingNewPointMode)
                {

                    //Pass camera position and forward direction
                    Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                    CircuitPoint createdPoint = _circuitShaper.AddPointAsNewCurve(ray.origin.ToLocalSpace(_target.transform.position, _target.Data.settingsData.ScaleMultiplier), ray.direction.ToNumericsVector3());
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

                _circuitShaper.DeselectRoad();

                // consume the event so Unity's scene view doesn't also use it
                e.Use();

            }
            else if (e.type == EventType.MouseDown && e.button == 0 && !e.alt && _hoveredRoad != null)
            {
                _circuitShaper.SelectRoad(_hoveredRoad);
                _circuitShaper.ClearSelection(); // Deselect any points
                _creatingNewPointMode = false;
                _addingToSelectedCurveMode = false;
                _isEditingCrossSection = false;
                Debug.Log("Clicked and seleceted road: " + _hoveredRoad.RoadStartIndex + " as " + _selectedRoad);
                e.Use();
                Repaint(); // Update inspector
            }
            else if ((_circuitShaper.SelectedPoints.Count > 0) && e.type == EventType.KeyDown && e.keyCode == KeyCode.Delete && !e.alt)
            {

                _circuitShaper.DeleteSelectedPoints();


                // consume the event so Unity's scene view doesn't also use it
                e.Use();
            }
            else if ((_circuitShaper.SelectedPoints.Count > 0) && e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {

                _circuitShaper.ClearSelection();
                _circuitShaper.DeselectRoad();
                // exit all modes
                _creatingNewPointMode = false;
                _addingToSelectedCurveMode = false;
                _isEditingCrossSection = false;

                // consume the event so Unity's scene view doesn't also use it
                e.Use();

            }

            

            //deselect road if any points are selected
            if (_circuitShaper.SelectedPoints.Count > 0 && _selectedRoad != null)
            {
                _circuitShaper.DeselectRoad();
                Repaint(); // Update inspector
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
