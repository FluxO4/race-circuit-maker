using OnomiCircuitShaper.Engine.Data;
using OnomiCircuitShaper.Engine.Interface;
using OnomiCircuitShaper.Engine.EditRealm;
using OnomiCircuitShaper.Unity.Utilities;
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace OnomiCircuitShaper.Unity.Editor
{
    public partial class OnomiCircuitShaperEditor : UnityEditor.Editor
    {
        // --- Handle Drawing Logic ---



        private void DrawCrossSectionEditorHandles(OnomiCircuitShaper target)
        {
            if (_circuitShaper.SelectedPoints.Count != 1) return;

            CircuitPoint selectedPoint = _circuitShaper.SelectedPoints.First() as CircuitPoint;
            if (selectedPoint == null || selectedPoint.CrossSection == null) return;

            var matrix = Handles.matrix;
            Vector3 basePosition = target.transform.position;
            float scale = target.Data.settingsData.ScaleMultiplier;

            // Draw selected point anchor and up vector
            Vector3 anchorPos = selectedPoint.PointPosition.ToGlobalSpace(basePosition, scale);
            float anchorSize = HandleUtility.GetHandleSize(anchorPos) * 0.12f;
            Handles.color = Color.yellow;
            Handles.SphereHandleCap(0, anchorPos, Quaternion.identity, anchorSize, EventType.Repaint);

            Vector3 upDir = selectedPoint.GetUpVector.ToUnityVector3();
            Handles.color = Color.magenta;
            Handles.ArrowHandleCap(0, anchorPos, Quaternion.LookRotation(upDir), HandleUtility.GetHandleSize(anchorPos) * 0.5f * target.Data.settingsData.RotatorPointDistance, EventType.Repaint);

            // Draw the cross-section curve and its points
            CrossSectionCurve csCurve = selectedPoint.CrossSection;
            
            // Draw Bezier segments for the cross-section
            for (int i = 0; i < csCurve.Points.Count - 1; i++)
            {
                CrossSectionPoint p1 = csCurve.Points[i];
                CrossSectionPoint p2 = csCurve.Points[i + 1];

                Vector3 worldP1 = p1.GetWorldPosition().ToGlobalSpace(basePosition, scale);
                Vector3 worldP2 = p2.GetWorldPosition().ToGlobalSpace(basePosition, scale);
                Vector3 worldCP1 = p1.GetWorldForwardControlPointPosition().ToGlobalSpace(basePosition, scale);
                Vector3 worldCP2 = p2.GetWorldBackwardControlPointPosition().ToGlobalSpace(basePosition, scale);

                Handles.DrawBezier(worldP1, worldP2, worldCP1, worldCP2, Color.white, null, 2f);
            }

            // Draw handles for each cross-section point
            foreach (CrossSectionPoint csPoint in csCurve.Points)
            {
                Vector3 csPointPos = csPoint.GetWorldPosition().ToGlobalSpace(basePosition, scale);
                float csPointSize = HandleUtility.GetHandleSize(csPointPos) * 0.08f;

                // Draw tangents
                Handles.color = Color.cyan;
                Vector3 forwardCP = csPoint.GetWorldForwardControlPointPosition().ToGlobalSpace(basePosition, scale);
                Vector3 backwardCP = csPoint.GetWorldBackwardControlPointPosition().ToGlobalSpace(basePosition, scale);
                Handles.DrawLine(csPointPos, forwardCP);
                Handles.DrawLine(csPointPos, backwardCP);

                Handles.color = Color.red;
                EditorGUI.BeginChangeCheck();
                Vector3 newWorldPos = Handles.FreeMoveHandle(csPointPos, csPointSize, Vector3.zero, Handles.SphereHandleCap);
                if (EditorGUI.EndChangeCheck())
                {
                    csPoint.MoveCrossSectionPoint(newWorldPos.ToLocalSpace(basePosition, scale));
                }
            }

            Handles.matrix = matrix;
        }

        protected void DrawAllHandles(OnomiCircuitShaper target)
        {
            if (target.Data?.circuitData?.CircuitCurves == null) return;

            // Use a local copy of the handle matrix to avoid affecting other editor GUI
            var matrix = Handles.matrix;

            Vector3 basePosition = target.transform.position;
            float scale = target.Data.settingsData.ScaleMultiplier;

            foreach (CircuitCurve curve in _circuitShaper.GetLiveCircuit.Curves)
            {
                // First, draw the Bézier curve segments
                for (int i = 0; i < curve.Points.Count; i++)
                {
                    CircuitPoint p1 = curve.Points[i];

                    if (curve.IsClosed || i < curve.Points.Count - 1)
                    {
                        CircuitPoint p2 = curve.Points[(i + 1) % curve.Points.Count];

                        Handles.DrawBezier(
                            p1.PointPosition.ToGlobalSpace(basePosition, scale),
                            p2.PointPosition.ToGlobalSpace(basePosition, scale),
                            p1.ForwardControlPointPosition.ToGlobalSpace(basePosition, scale),
                            p2.BackwardControlPointPosition.ToGlobalSpace(basePosition, scale),
                            Color.green,
                            null,
                            2f
                        );
                    }
                }
                

                // Then, draw the interactive point handles on top
                foreach (CircuitPoint point in curve.Points)
                {
                    DrawPointHandles(target, point, basePosition, scale);
                    

                    if (!_isEditingCrossSection &&point.CrossSection != null && point.CrossSection.Points.Count > 1)
                    {
                        CrossSectionCurve csCurve = point.CrossSection;
                        //Handles.color = new Color(0.8f, 0.8f, 0.8f, 0.5f); // Light grey, semi-transparent

                        for (int i = 0; i < csCurve.Points.Count - 1; i++)
                        {
                            CrossSectionPoint p1 = csCurve.Points[i];
                            CrossSectionPoint p2 = csCurve.Points[i + 1];

                            Vector3 worldP1 = p1.GetWorldPosition().ToGlobalSpace(basePosition, scale);
                            Vector3 worldP2 = p2.GetWorldPosition().ToGlobalSpace(basePosition, scale);
                            Vector3 worldCP1 = p1.GetWorldForwardControlPointPosition().ToGlobalSpace(basePosition, scale);
                            Vector3 worldCP2 = p2.GetWorldBackwardControlPointPosition().ToGlobalSpace(basePosition, scale);

                            Handles.DrawBezier(worldP1, worldP2, worldCP1, worldCP2, Color.blue, null, 1.5f);
                        }
                    }
                }
            }

            Handles.matrix = matrix;
        }

        private void DrawPointHandles(OnomiCircuitShaper target, CircuitPoint point, Vector3 basePosition, float scale)
        {
            if (point == null || _circuitShaper == null) return;

            bool isSelected = _circuitShaper.SelectedPoints.Contains(point);

            Vector3 anchorPos = point.PointPosition.ToGlobalSpace(basePosition, scale);
            Vector3 forwardPos = point.ForwardControlPointPosition.ToGlobalSpace(basePosition, scale);
            Vector3 backwardPos = point.BackwardControlPointPosition.ToGlobalSpace(basePosition, scale);

            // --- Draw Lines connecting control points to anchor ---
            Handles.color = isSelected ? Color.cyan : Color.blue;
            Handles.DrawLine(anchorPos, forwardPos);
            Handles.DrawLine(anchorPos, backwardPos);

            // --- Draw an arrow indicating the up direction ---
            Vector3 upDir = point.GetUpVector.ToUnityVector3();
            Handles.color = Color.magenta;
            Handles.ArrowHandleCap(0, anchorPos, Quaternion.LookRotation(upDir), HandleUtility.GetHandleSize(anchorPos) * 0.5f * target.Data.settingsData.RotatorPointDistance, EventType.Repaint);

            // Choose which handle mode to use
            if (target.Data.settingsData.FreeMoveMode)
            {
                DrawPointHandles_FreeMove(target, point, anchorPos, forwardPos, backwardPos, isSelected, basePosition, scale);
            }
            else
            {
                DrawPointHandles_Regular(target, point, anchorPos, forwardPos, backwardPos, isSelected, basePosition, scale);
            }
        }

        private void DrawPointHandles_FreeMove(OnomiCircuitShaper target, CircuitPoint point, 
            Vector3 anchorPos, Vector3 forwardPos, Vector3 backwardPos, bool isSelected,
            Vector3 basePosition, float scale)
        {
            float cpSize = HandleUtility.GetHandleSize(forwardPos) * 0.08f;
            float anchorSize = HandleUtility.GetHandleSize(anchorPos) * 0.12f;

            // Forward CP
            Handles.color = isSelected ? Color.cyan : new Color(0, 0.5f, 0.5f);
            EditorGUI.BeginChangeCheck();
            Vector3 newForwardPos = Handles.FreeMoveHandle(forwardPos, cpSize, Vector3.zero, Handles.SphereHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                _circuitShaper.MoveCircuitPointForwardControl(point, newForwardPos.ToLocalSpace(basePosition, scale));
            }

            // Backward CP
            Handles.color = isSelected ? Color.cyan : new Color(0, 0.5f, 0.5f);
            EditorGUI.BeginChangeCheck();
            Vector3 newBackwardPos = Handles.FreeMoveHandle(backwardPos, cpSize, Vector3.zero, Handles.SphereHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                _circuitShaper.MoveCircuitPointBackwardControl(point, newBackwardPos.ToLocalSpace(basePosition, scale));
            }

            // Anchor Point - also as free move handle
            Handles.color = isSelected ? Color.yellow : Color.red;
            EditorGUI.BeginChangeCheck();
            Vector3 newAnchorPos = Handles.FreeMoveHandle(anchorPos, anchorSize, Vector3.zero, Handles.SphereHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                _circuitShaper.MoveCircuitPoint(point, newAnchorPos.ToLocalSpace(basePosition, scale));
            }

            // Selection by shift-clicking
            if (Handles.Button(anchorPos, Quaternion.identity, anchorSize, anchorSize, Handles.SphereHandleCap))
            {
                if (Event.current.shift)
                {
                    _circuitShaper.AddPointToSelection(point);
                }
                Repaint();
            }
        }

        private void DrawPointHandles_Regular(OnomiCircuitShaper target, CircuitPoint point,
            Vector3 anchorPos, Vector3 forwardPos, Vector3 backwardPos, bool isSelected,
            Vector3 basePosition, float scale)
        {
            float cpSize = HandleUtility.GetHandleSize(forwardPos) * 0.08f;
            float anchorSize = HandleUtility.GetHandleSize(anchorPos) * 0.12f;

            // Draw clickable control point buttons
            Handles.color = isSelected ? Color.cyan : new Color(0, 0.5f, 0.5f);
            
            // Forward CP button
            if (Handles.Button(forwardPos, Quaternion.identity, cpSize, cpSize, Handles.SphereHandleCap))
            {
                if (Event.current.shift)
                {
                    _circuitShaper.AddPointToSelection(point);
                }
                else
                {
                    _circuitShaper.SelectPoint(point);
                    _circuitShaper.SetSinglePointSelectionMode(SinglePointSelectionMode.ForwardControlPoint);
                }
                Repaint();
            }

            // Backward CP button
            if (Handles.Button(backwardPos, Quaternion.identity, cpSize, cpSize, Handles.SphereHandleCap))
            {
                if (Event.current.shift)
                {
                    _circuitShaper.AddPointToSelection(point);
                }
                else
                {
                    _circuitShaper.SelectPoint(point);
                    _circuitShaper.SetSinglePointSelectionMode(SinglePointSelectionMode.BackwardControlPoint);
                }
                Repaint();
            }

            // Anchor Point button
            Handles.color = isSelected ? Color.yellow : Color.red;
            if (Handles.Button(anchorPos, Quaternion.identity, anchorSize, anchorSize, Handles.SphereHandleCap))
            {
                if (Event.current.shift)
                {
                    _circuitShaper.AddPointToSelection(point);
                }
                else
                {
                    _circuitShaper.SelectPoint(point);
                    _circuitShaper.SetSinglePointSelectionMode(SinglePointSelectionMode.AnchorPoint);
                }
                Repaint();
            }

            // Draw position handle only if single point is selected
            if (isSelected && _circuitShaper.SelectedPoints.Count == 1)
            {
                SinglePointSelectionMode mode = _circuitShaper.GetSinglePointSelectionMode();
                Vector3 handlePosition;
                
                switch (mode)
                {
                    case SinglePointSelectionMode.ForwardControlPoint:
                        handlePosition = forwardPos;
                        break;
                    case SinglePointSelectionMode.BackwardControlPoint:
                        handlePosition = backwardPos;
                        break;
                    case SinglePointSelectionMode.AnchorPoint:
                    default:
                        handlePosition = anchorPos;
                        break;
                }

                EditorGUI.BeginChangeCheck();

                // Depending on the selected tool, draw a position or rotation handle
                if (Tools.current == Tool.Move)
                {
                    Vector3 newPosition = Handles.PositionHandle(handlePosition, Quaternion.identity);
                    if (EditorGUI.EndChangeCheck())
                    {
                        switch (mode)
                        {
                            case SinglePointSelectionMode.ForwardControlPoint:
                                _circuitShaper.MoveCircuitPointForwardControl(point, newPosition.ToLocalSpace(basePosition, scale));
                                break;
                            case SinglePointSelectionMode.BackwardControlPoint:
                                _circuitShaper.MoveCircuitPointBackwardControl(point, newPosition.ToLocalSpace(basePosition, scale));
                                break;
                            case SinglePointSelectionMode.AnchorPoint:
                            default:
                                _circuitShaper.MoveCircuitPoint(point, newPosition.ToLocalSpace(basePosition, scale));
                                break;
                        }
                    }
                }
                else if (Tools.current == Tool.Rotate && mode == SinglePointSelectionMode.AnchorPoint) // Rotation only for anchor
                {
                    Quaternion initialRotation = Quaternion.LookRotation(point.GetForwardVector.ToUnityVector3(), point.GetUpVector.ToUnityVector3());
                    
                    Quaternion newRotation = Handles.RotationHandle(initialRotation, handlePosition);

                    if (EditorGUI.EndChangeCheck())
                    {
                        Quaternion deltaRotation = newRotation * Quaternion.Inverse(initialRotation);
                        _circuitShaper.RotateCircuitPoint(point, deltaRotation.eulerAngles.ToNumericsVector3());
                    }
                }
            }
        }
    }
}