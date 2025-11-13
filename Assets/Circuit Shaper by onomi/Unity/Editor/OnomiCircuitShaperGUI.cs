/*using OnomiCircuitShaper.Engine.Data;
using OnomiCircuitShaper.Unity.Utilities;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace OnomiCircuitShaper.Unity.Editor
{
    /// <summary>
    /// Handles the drawing of the custom inspector GUI for all Circuit Shaper data.
    /// This class is responsible for creating a user-friendly, collapsible interface
    /// for the complex, nested data structures.
    /// </summary>
    public class OnomiCircuitShaperGUI
    {
        // Fields to store the state of UI foldouts
        private bool _settingsFoldout = true;
        private bool _circuitDataFoldout = true;
        private readonly Dictionary<object, bool> _foldoutStates = new Dictionary<object, bool>();

        public void DrawInspector(OnomiCircuitShaperData data)
        {
            if (data == null) return;

            DrawSettingsGUI(data.settingsData);
            DrawCircuitDataGUI(data.circuitData);
        }

        private void DrawSettingsGUI(CircuitAndEditorSettings settings)
        {
            _settingsFoldout = EditorGUILayout.Foldout(_settingsFoldout, "Editor Settings", true);
            if (!_settingsFoldout) return;

            EditorGUI.indentLevel++;
            settings.ScaleMultiplier = EditorGUILayout.FloatField("Scale Multiplier", settings.ScaleMultiplier);
            settings.AutoSetControlPoints = EditorGUILayout.Toggle("Auto Set Control Points", settings.AutoSetControlPoints);
            settings.IndependentControlPoints = EditorGUILayout.Toggle("Independent Control Points", settings.IndependentControlPoints);
            settings.RotatorPointDistance = EditorGUILayout.FloatField("Rotator Point Distance", settings.RotatorPointDistance);
            EditorGUI.indentLevel--;
        }

        private void DrawCircuitDataGUI(CircuitData circuit)
        {
            _circuitDataFoldout = EditorGUILayout.Foldout(_circuitDataFoldout, "Circuit Data", true);
            if (!_circuitDataFoldout) return;

            EditorGUI.indentLevel++;
            DrawEditableList("Curves", circuit.CircuitCurves, DrawCurve, () => new CurveData());
            DrawEditableList("Roads", circuit.CircuitRoads, DrawRoad, () => new RoadData());
            EditorGUI.indentLevel--;
        }

        private void DrawCurve(CircuitCurveData curve, int index)
        {
            if (curve == null) return;

            EditorGUILayout.LabelField($"Curve {index}", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            curve.IsClosed = EditorGUILayout.Toggle("Is Closed", curve.IsClosed);
            
            // Here, we specify that new points should be created as CircuitPointData
            DrawEditableList("Points", curve.CurvePoints, DrawPoint, () => new CircuitPointData());
            
            EditorGUI.indentLevel--;
        }

        private void DrawPoint(PointData point, int index)
        {
            if (point == null) return;

            SetFoldoutState(point, EditorGUILayout.Foldout(GetFoldoutState(point), $"Point {index}", true));
            if (!GetFoldoutState(point)) return;

            EditorGUI.indentLevel++;
            point.PointPosition = EditorGUILayout.Vector3Field("Position", point.PointPosition.ToUnityVector3()).ToNumericsVector3();
            point.ForwardControlPointPosition = EditorGUILayout.Vector3Field("Forward Control", point.ForwardControlPointPosition.ToUnityVector3()).ToNumericsVector3();
            point.BackwardControlPointPosition = EditorGUILayout.Vector3Field("Backward Control", point.BackwardControlPointPosition.ToUnityVector3()).ToNumericsVector3();
            point.UpDirection = EditorGUILayout.Vector3Field("Up Direction", point.UpDirection.ToUnityVector3()).ToNumericsVector3();

            if (point is CircuitPointData circuitPoint)
            {
                 // We can use the simple DrawCurve method here since cross-sections are simpler
                 DrawSimpleCurve(circuitPoint.CrossSectionCurve, "Cross Section Curve");
            }

            EditorGUI.indentLevel--;
        }

        private void DrawRoad(RoadData road, int index)
        {
            if (road == null) return;

            EditorGUILayout.LabelField($"Road {index}", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            road.WidthWiseVertexCount = EditorGUILayout.IntField("Width Wise Vertices", road.WidthWiseVertexCount);
            road.LengthWiseVertexCountPerUnitWidthWiseVertexCount = EditorGUILayout.IntField("Length Wise Vertices", road.LengthWiseVertexCountPerUnitWidthWiseVertexCount);
            road.UVTile = EditorGUILayout.Vector2Field("UV Tile", road.UVTile.ToUnityVector2()).ToNumericsVector2();
            road.UVOffset = EditorGUILayout.Vector2Field("UV Offset", road.UVOffset.ToUnityVector2()).ToNumericsVector2();
            EditorGUI.indentLevel--;
        }

        /// <summary>
        /// A generic method to draw a list of items with add/remove functionality.
        /// </summary>
        private void DrawEditableList<T>(string label, List<T> list, Action<T, int> drawElement, Func<T> createElement)
        {
            SetFoldoutState(list, EditorGUILayout.Foldout(GetFoldoutState(list), $"{label} ({list.Count})", true));
            if (!GetFoldoutState(list)) return;

            EditorGUI.indentLevel++;
            
            int toRemove = -1;
            for (int i = 0; i < list.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                // We wrap the element drawing in a vertical group to keep the remove button aligned
                EditorGUILayout.BeginVertical();
                drawElement(list[i], i);
                EditorGUILayout.EndVertical();
                
                // Add a small remove button
                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    toRemove = i;
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();
            }

            if (toRemove != -1)
            {
                list.RemoveAt(toRemove);
            }

            // Add a button to create a new element
            if (GUILayout.Button($"Add New {typeof(T).Name}"))
            {
                list.Add(createElement());
            }

            EditorGUI.indentLevel--;
        }
        
        // A simpler version of DrawCurve for non-nested scenarios like cross-sections.
        private void DrawSimpleCurve(CurveData curve, string label)
        {
             SetFoldoutState(curve, EditorGUILayout.Foldout(GetFoldoutState(curve), label, true));
            if (!GetFoldoutState(curve)) return;
            
            EditorGUI.indentLevel++;
            curve.IsClosed = EditorGUILayout.Toggle("Is Closed", curve.IsClosed);
            DrawEditableList("Points", curve.CurvePoints, DrawPoint, () => new CrossSectionPointData());
            EditorGUI.indentLevel--;
        }

        private bool GetFoldoutState(object key)
        {
            _foldoutStates.TryGetValue(key, out bool state);
            return state;
        }

        private void SetFoldoutState(object key, bool state)
        {
            _foldoutStates[key] = state;
        }
    }
}*/