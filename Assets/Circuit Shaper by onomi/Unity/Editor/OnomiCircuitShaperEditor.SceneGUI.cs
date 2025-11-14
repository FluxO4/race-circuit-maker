using OnomiCircuitShaper.Engine.Interface;
using OnomiCircuitShaper.Engine.EditRealm;
using OnomiCircuitShaper.Unity.Utilities;
using UnityEditor;
using UnityEngine;
using System.Linq;
using OnomiCircuitShaper.Engine.Processors;

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
        

          /// <summary>
        /// Draws road boundary handles using Bezier curves computed from road data.
        /// This is more robust than extracting mesh vertices and doesn't require the mesh to exist.
        /// The boundary is drawn as a closed region for better selectability.
        /// </summary>
        private void DrawRoadHandles()
        {
            if (_circuitShaper == null || _circuitShaper.GetLiveCircuit == null) return;
            
            var circuit = _circuitShaper.GetLiveCircuit;
            if (circuit.Roads == null || circuit.Roads.Count == 0) return;

            Event e = Event.current;
            Road hoveredRoad = null;
            float closestDistance = float.MaxValue;
            Vector3 basePosition = _target.transform.position;
            float scale = _target.Data.settingsData.ScaleMultiplier;

            foreach (var road in circuit.Roads)
            {
                if (road.AssociatedPoints == null || road.AssociatedPoints.Count < 2) continue;

                bool isSelected = (_selectedRoad == road);

                // Build left and right edge Bezier curves from road points
                var leftPoints = new System.Collections.Generic.List<UnityEngine.Vector3>();
                var rightPoints = new System.Collections.Generic.List<UnityEngine.Vector3>();

                for (int i = 0; i < road.AssociatedPoints.Count; i++)
                {
                    var point = road.AssociatedPoints[i];
                    if (point.CrossSection == null || point.CrossSection.Data.CurvePoints.Count < 2) continue;

                    // Get left and right end positions from cross-section
                    var leftPos = point.GetLeftEndPointPosition.ToGlobalSpace(basePosition, scale);
                    var rightPos = point.GetRightEndPointPosition.ToGlobalSpace(basePosition, scale);

                    leftPoints.Add(leftPos);
                    rightPoints.Add(rightPos);
                }

                if (leftPoints.Count < 2) continue;

                // Sample both edge curves using simplified Bezier (through anchor points with scaled control offsets)
                int samplesPerSegment = 10;
                var leftCurve = SampleEdgeCurve(road.AssociatedPoints, leftPoints, basePosition, scale, samplesPerSegment, true);
                var rightCurve = SampleEdgeCurve(road.AssociatedPoints, rightPoints, basePosition, scale, samplesPerSegment, false);

                // Check for mouse hover on the region
                if (!isSelected)
                {
                    float minDist = float.MaxValue;

                    // Check distance to all edge segments
                    for (int i = 0; i < leftCurve.Count - 1; i++)
                    {
                        float dist = HandleUtility.DistancePointLine(e.mousePosition,
                            HandleUtility.WorldToGUIPoint(leftCurve[i]),
                            HandleUtility.WorldToGUIPoint(leftCurve[i + 1]));
                        minDist = Mathf.Min(minDist, dist);
                    }

                    for (int i = 0; i < rightCurve.Count - 1; i++)
                    {
                        float dist = HandleUtility.DistancePointLine(e.mousePosition,
                            HandleUtility.WorldToGUIPoint(rightCurve[i]),
                            HandleUtility.WorldToGUIPoint(rightCurve[i + 1]));
                        minDist = Mathf.Min(minDist, dist);
                    }

                    // Also check if mouse is inside the region (basic polygon test)
                    if (minDist > 15f)
                    {
                        // Create closed polygon for inside test
                        var polygon = new System.Collections.Generic.List<UnityEngine.Vector2>();
                        foreach (var p in leftCurve)
                            polygon.Add(HandleUtility.WorldToGUIPoint(p));
                        for (int i = rightCurve.Count - 1; i >= 0; i--)
                            polygon.Add(HandleUtility.WorldToGUIPoint(rightCurve[i]));

                        if (IsPointInPolygon(e.mousePosition, polygon))
                        {
                            minDist = 0f;
                        }
                    }

                    if (minDist < 15f && minDist < closestDistance)
                    {
                        closestDistance = minDist;
                        hoveredRoad = road;
                    }
                }

                // Determine visual style
                Color edgeColor = Color.cyan;
                float lineWidth = 2f;

                if (isSelected)
                {
                    edgeColor = Color.yellow;
                    lineWidth = 6f;
                }
                else if (hoveredRoad == road)
                {
                    edgeColor = Color.white;
                    lineWidth = 4f;
                }

                // Draw the closed boundary
                Handles.color = edgeColor;
                
                // Draw left edge
                for (int i = 0; i < leftCurve.Count - 1; i++)
                    Handles.DrawLine(leftCurve[i], leftCurve[i + 1], lineWidth);
                
                // Draw right edge
                for (int i = 0; i < rightCurve.Count - 1; i++)
                    Handles.DrawLine(rightCurve[i], rightCurve[i + 1], lineWidth);
                
                // Connect ends to close the region
                if (leftCurve.Count > 0 && rightCurve.Count > 0)
                {
                    Handles.DrawLine(leftCurve[0], rightCurve[0], lineWidth);
                    Handles.DrawLine(leftCurve[leftCurve.Count - 1], rightCurve[rightCurve.Count - 1], lineWidth);
                }
            }

            // Handle click selection
            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt && hoveredRoad != null)
            {
                _selectedRoad = hoveredRoad;
                _circuitShaper.ClearSelection(); // Deselect any points
                _creatingNewPointMode = false;
                _addingToSelectedCurveMode = false;
                _isEditingCrossSection = false;
                e.Use();
                Repaint(); // Update inspector
            }
            
            // Deselect road if clicking elsewhere
            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt && hoveredRoad == null && _selectedRoad != null)
            {
                // Check if we're not clicking on a point handle
                if (_circuitShaper.SelectedPoints.Count == 0 && !_creatingNewPointMode && !_addingToSelectedCurveMode)
                {
                    _selectedRoad = null;
                    Repaint();
                }
            }
        }

        /// <summary>
        /// Samples an edge curve along the road using simplified Bezier interpolation.
        /// </summary>
        private System.Collections.Generic.List<UnityEngine.Vector3> SampleEdgeCurve(
            System.Collections.Generic.List<CircuitPoint> points,
            System.Collections.Generic.List<UnityEngine.Vector3> edgePoints,
            UnityEngine.Vector3 basePosition,
            float scale,
            int samplesPerSegment,
            bool isLeftEdge)
        {
            var samples = new System.Collections.Generic.List<UnityEngine.Vector3>();

            for (int i = 0; i < points.Count - 1; i++)
            {
                var p0 = edgePoints[i];
                var p3 = edgePoints[i + 1];

                // Get control points from the circuit points, scaled toward the edge
                var cp0 = points[i];
                var cp1 = points[i + 1];

                // Calculate control point positions (scaled to prevent pinching)
                var forwardDir = (cp0.ForwardControlPointPosition - cp0.PointPosition).ToUnityVector3().normalized;
                var backwardDir = (cp1.BackwardControlPointPosition - cp1.PointPosition).ToUnityVector3().normalized;

                float dist = UnityEngine.Vector3.Distance(p0, p3);
                float controlScale = dist * 0.3f; // Scale factor to prevent pinching

                var p1 = p0 + forwardDir * controlScale;
                var p2 = p3 + backwardDir * controlScale;

                // Sample the cubic Bezier curve
                for (int s = 0; s < samplesPerSegment; s++)
                {
                    float t = s / (float)samplesPerSegment;
                    var point = CubicBezier(p0, p1, p2, p3, t);
                    samples.Add(point);
                }
            }

            // Add final point
            samples.Add(edgePoints[edgePoints.Count - 1]);

            return samples;
        }

        /// <summary>
        /// Evaluates a cubic Bezier curve at parameter t.
        /// </summary>
        private UnityEngine.Vector3 CubicBezier(UnityEngine.Vector3 p0, UnityEngine.Vector3 p1, UnityEngine.Vector3 p2, UnityEngine.Vector3 p3, float t)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            UnityEngine.Vector3 p = uuu * p0;
            p += 3 * uu * t * p1;
            p += 3 * u * tt * p2;
            p += ttt * p3;

            return p;
        }

        /// <summary>
        /// Tests if a 2D point is inside a polygon using ray casting algorithm.
        /// </summary>
        private bool IsPointInPolygon(UnityEngine.Vector2 point, System.Collections.Generic.List<UnityEngine.Vector2> polygon)
        {
            bool inside = false;
            int count = polygon.Count;

            for (int i = 0, j = count - 1; i < count; j = i++)
            {
                if (((polygon[i].y > point.y) != (polygon[j].y > point.y)) &&
                    (point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x))
                {
                    inside = !inside;
                }
            }

            return inside;
        }
    }
}