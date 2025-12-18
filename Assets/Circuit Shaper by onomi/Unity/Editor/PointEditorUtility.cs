using OnomiCircuitShaper.Engine.Interface;
using OnomiCircuitShaper.Engine.EditRealm;
using OnomiCircuitShaper.Unity.Utilities;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace OnomiCircuitShaper.Unity.Editor
{
    /// <summary>
    /// Provides shared point editing functionality for circuit editors.
    /// Used by both OnomiCircuitShaperEditor and SceneRoadEditor.
    /// </summary>
    public static class PointEditorUtility
    {
        /// <summary>
        /// Draws a single point handle with interactive controls.
        /// </summary>
        /// <param name="point">The circuit point to draw</param>
        /// <param name="circuitShaper">The circuit shaper interface for operations</param>
        /// <param name="basePosition">Base position for coordinate conversion</param>
        /// <param name="scale">Scale multiplier for coordinate conversion</param>
        /// <param name="freeMoveMode">Whether to use free move handles</param>
        /// <param name="isSelected">Whether this point is currently selected</param>
        /// <param name="rotatorPointDistance">Scale for the up direction arrow</param>
        /// <returns>True if any changes were made</returns>
        public static bool DrawPointHandle(
            CircuitPoint point,
            ICircuitShaper circuitShaper,
            Vector3 basePosition,
            float scale,
            bool freeMoveMode,
            bool isSelected,
            float rotatorPointDistance = 1f)
        {
            if (point == null) return false;

            bool changed = false;

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
            Handles.ArrowHandleCap(0, anchorPos, Quaternion.LookRotation(upDir), 
                HandleUtility.GetHandleSize(anchorPos) * 0.5f * rotatorPointDistance, EventType.Repaint);

            // Choose which handle mode to use
            if (freeMoveMode)
            {
                changed = DrawPointHandles_FreeMove(point, circuitShaper, 
                    anchorPos, forwardPos, backwardPos, isSelected, basePosition, scale);
            }
            else
            {
                changed = DrawPointHandles_Regular(point, circuitShaper, 
                    anchorPos, forwardPos, backwardPos, isSelected, basePosition, scale);
            }

            return changed;
        }

        /// <summary>
        /// Draw point handles in free move mode (drag anywhere).
        /// </summary>
        private static bool DrawPointHandles_FreeMove(
            CircuitPoint point,
            ICircuitShaper circuitShaper,
            Vector3 anchorPos, 
            Vector3 forwardPos, 
            Vector3 backwardPos, 
            bool isSelected,
            Vector3 basePosition, 
            float scale)
        {
            bool changed = false;
            float cpSize = HandleUtility.GetHandleSize(forwardPos) * 0.08f;
            float anchorSize = HandleUtility.GetHandleSize(anchorPos) * 0.12f;

            // Forward CP
            Handles.color = isSelected ? Color.cyan : new Color(0, 0.5f, 0.5f);
            EditorGUI.BeginChangeCheck();
            Vector3 newForwardPos = Handles.FreeMoveHandle(forwardPos, cpSize, Vector3.zero, Handles.SphereHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                circuitShaper?.MoveCircuitPointForwardControl(point, newForwardPos.ToLocalSpace(basePosition, scale));
                changed = true;
            }

            // Backward CP
            Handles.color = isSelected ? Color.cyan : new Color(0, 0.5f, 0.5f);
            EditorGUI.BeginChangeCheck();
            Vector3 newBackwardPos = Handles.FreeMoveHandle(backwardPos, cpSize, Vector3.zero, Handles.SphereHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                circuitShaper?.MoveCircuitPointBackwardControl(point, newBackwardPos.ToLocalSpace(basePosition, scale));
                changed = true;
            }

            // Anchor Point - also as free move handle
            Handles.color = isSelected ? Color.yellow : Color.red;
            EditorGUI.BeginChangeCheck();
            Vector3 newAnchorPos = Handles.FreeMoveHandle(anchorPos, anchorSize, Vector3.zero, Handles.SphereHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                circuitShaper?.MoveCircuitPoint(point, newAnchorPos.ToLocalSpace(basePosition, scale));
                changed = true;
            }

            // Selection by shift-clicking
            if (Handles.Button(anchorPos, Quaternion.identity, anchorSize, anchorSize, Handles.SphereHandleCap))
            {
                if (Event.current.shift)
                {
                    circuitShaper?.AddPointToSelection(point);
                }
                changed = true;
            }

            return changed;
        }

        /// <summary>
        /// Draw point handles in regular mode (click to select, use Unity transform tools).
        /// </summary>
        private static bool DrawPointHandles_Regular(
            CircuitPoint point,
            ICircuitShaper circuitShaper,
            Vector3 anchorPos, 
            Vector3 forwardPos, 
            Vector3 backwardPos, 
            bool isSelected,
            Vector3 basePosition, 
            float scale)
        {
            bool changed = false;
            float cpSize = HandleUtility.GetHandleSize(forwardPos) * 0.08f;
            float anchorSize = HandleUtility.GetHandleSize(anchorPos) * 0.12f;

            // Draw clickable control point buttons
            Handles.color = isSelected ? Color.cyan : new Color(0, 0.5f, 0.5f);

            // Forward CP button
            if (Handles.Button(forwardPos, Quaternion.identity, cpSize, cpSize, Handles.SphereHandleCap))
            {
                if (Event.current.shift)
                {
                    circuitShaper?.AddPointToSelection(point);
                }
                else
                {
                    circuitShaper?.SelectPoint(point);
                    circuitShaper?.SetSinglePointSelectionMode(SinglePointSelectionMode.ForwardControlPoint);
                }
                changed = true;
            }

            // Backward CP button
            if (Handles.Button(backwardPos, Quaternion.identity, cpSize, cpSize, Handles.SphereHandleCap))
            {
                if (Event.current.shift)
                {
                    circuitShaper?.AddPointToSelection(point);
                }
                else
                {
                    circuitShaper?.SelectPoint(point);
                    circuitShaper?.SetSinglePointSelectionMode(SinglePointSelectionMode.BackwardControlPoint);
                }
                changed = true;
            }

            // Anchor Point button
            Handles.color = isSelected ? Color.yellow : Color.red;
            if (Handles.Button(anchorPos, Quaternion.identity, anchorSize, anchorSize, Handles.SphereHandleCap))
            {
                if (Event.current.shift)
                {
                    circuitShaper?.AddPointToSelection(point);
                }
                else
                {
                    circuitShaper?.SelectPoint(point);
                    circuitShaper?.SetSinglePointSelectionMode(SinglePointSelectionMode.AnchorPoint);
                }
                changed = true;
            }

            // Draw position handle only if single point is selected
            if (isSelected && circuitShaper != null && circuitShaper.SelectedPoints.Count == 1)
            {
                SinglePointSelectionMode mode = circuitShaper.GetSinglePointSelectionMode();
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
                                circuitShaper.MoveCircuitPointForwardControl(point, newPosition.ToLocalSpace(basePosition, scale));
                                break;
                            case SinglePointSelectionMode.BackwardControlPoint:
                                circuitShaper.MoveCircuitPointBackwardControl(point, newPosition.ToLocalSpace(basePosition, scale));
                                break;
                            case SinglePointSelectionMode.AnchorPoint:
                            default:
                                circuitShaper.MoveCircuitPoint(point, newPosition.ToLocalSpace(basePosition, scale));
                                break;
                        }
                        changed = true;
                    }
                }
                else if (Tools.current == Tool.Rotate && mode == SinglePointSelectionMode.AnchorPoint) // Rotation only for anchor
                {
                    Quaternion initialRotation = Quaternion.LookRotation(
                        point.GetForwardVector.ToUnityVector3(), 
                        point.GetUpVector.ToUnityVector3());

                    Quaternion newRotation = Handles.RotationHandle(initialRotation, handlePosition);

                    if (EditorGUI.EndChangeCheck())
                    {
                        Quaternion deltaRotation = newRotation * Quaternion.Inverse(initialRotation);
                        circuitShaper.RotateCircuitPoint(point, deltaRotation.eulerAngles.ToNumericsVector3());
                        changed = true;
                    }
                }
            }

            return changed;
        }

        /// <summary>
        /// Draws cross-section curve handles for a selected point.
        /// </summary>
        /// <param name="selectedPoint">The point whose cross-section to edit</param>
        /// <param name="basePosition">Base position for coordinate conversion</param>
        /// <param name="scale">Scale multiplier for coordinate conversion</param>
        /// <returns>True if any changes were made</returns>
        public static bool DrawCrossSectionHandles(
            CircuitPoint selectedPoint,
            Vector3 basePosition,
            float scale)
        {
            if (selectedPoint == null || selectedPoint.CrossSection == null) return false;

            bool changed = false;
            CrossSectionCurve csCurve = selectedPoint.CrossSection;

            // Draw anchor and up direction indicator
            Vector3 anchorPos = selectedPoint.PointPosition.ToGlobalSpace(basePosition, scale);
            float anchorSize = HandleUtility.GetHandleSize(anchorPos) * 0.12f;
            Handles.color = Color.yellow;
            Handles.SphereHandleCap(0, anchorPos, Quaternion.identity, anchorSize, EventType.Repaint);

            Vector3 upDir = selectedPoint.GetUpVector.ToUnityVector3();
            Handles.color = Color.magenta;
            Handles.ArrowHandleCap(0, anchorPos, Quaternion.LookRotation(upDir), 
                HandleUtility.GetHandleSize(anchorPos) * 0.5f, EventType.Repaint);

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
                    changed = true;
                }
            }

            return changed;
        }

        /// <summary>
        /// Draws a display-only cross-section curve (no interactivity).
        /// Used when not in cross-section edit mode.
        /// </summary>
        /// <param name="point">The point whose cross-section to display</param>
        /// <param name="basePosition">Base position for coordinate conversion</param>
        /// <param name="scale">Scale multiplier for coordinate conversion</param>
        public static void DrawCrossSectionDisplay(
            CircuitPoint point,
            Vector3 basePosition,
            float scale)
        {
            if (point == null || point.CrossSection == null || point.CrossSection.Points.Count < 2) return;

            CrossSectionCurve csCurve = point.CrossSection;

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

        /// <summary>
        /// Draws a Bezier curve between two points.
        /// </summary>
        public static void DrawBezierSegment(
            CircuitPoint p1,
            CircuitPoint p2,
            Vector3 basePosition,
            float scale,
            Color color,
            float width)
        {
            if (p1 == null || p2 == null) return;

            Handles.DrawBezier(
                p1.PointPosition.ToGlobalSpace(basePosition, scale),
                p2.PointPosition.ToGlobalSpace(basePosition, scale),
                p1.ForwardControlPointPosition.ToGlobalSpace(basePosition, scale),
                p2.BackwardControlPointPosition.ToGlobalSpace(basePosition, scale),
                color,
                null,
                width
            );
        }

        /// <summary>
        /// Draws all points for a list of circuit points (e.g., a road's points).
        /// </summary>
        /// <param name="points">The points to draw</param>
        /// <param name="circuitShaper">The circuit shaper interface</param>
        /// <param name="basePosition">Base position for coordinate conversion</param>
        /// <param name="scale">Scale multiplier</param>
        /// <param name="freeMoveMode">Whether to use free move handles</param>
        /// <param name="selectedPoints">Collection of currently selected points</param>
        /// <param name="rotatorPointDistance">Scale for up direction arrows</param>
        /// <returns>True if any changes were made</returns>
        public static bool DrawPointHandles(
            IReadOnlyList<CircuitPoint> points,
            ICircuitShaper circuitShaper,
            Vector3 basePosition,
            float scale,
            bool freeMoveMode,
            IReadOnlyList<CircuitPoint> selectedPoints,
            float rotatorPointDistance = 1f)
        {
            bool changed = false;
            
            // Create a hashset for faster selection lookups
            HashSet<CircuitPoint> selectedSet = selectedPoints != null 
                ? new HashSet<CircuitPoint>(selectedPoints) 
                : new HashSet<CircuitPoint>();

            foreach (CircuitPoint point in points)
            {
                bool isSelected = selectedSet.Contains(point);
                if (DrawPointHandle(point, circuitShaper, basePosition, scale, 
                    freeMoveMode, isSelected, rotatorPointDistance))
                {
                    changed = true;
                }
            }

            return changed;
        }

        /// <summary>
        /// Draws the bezier curve connecting a list of points.
        /// </summary>
        /// <param name="points">The points to draw curves between</param>
        /// <param name="basePosition">Base position for coordinate conversion</param>
        /// <param name="scale">Scale multiplier</param>
        /// <param name="color">Curve color</param>
        /// <param name="width">Curve width</param>
        /// <param name="isClosed">Whether the curve is closed</param>
        public static void DrawCurve(
            IReadOnlyList<CircuitPoint> points,
            Vector3 basePosition,
            float scale,
            Color color,
            float width,
            bool isClosed = false)
        {
            if (points == null || points.Count < 2) return;

            int count = isClosed ? points.Count : points.Count - 1;
            for (int i = 0; i < count; i++)
            {
                CircuitPoint p1 = points[i];
                CircuitPoint p2 = points[(i + 1) % points.Count];
                DrawBezierSegment(p1, p2, basePosition, scale, color, width);
            }
        }
    }
}
