using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.VisualScripting;

public class SharedEditorMethods : Editor
{
    public void DrawRotatorHandle(Curve curve)
    {
        
        Handles.color = Color.yellow;
        foreach (Point point in curve.points)
        {
            Vector3 handlePos = point.rotatorPointPosition;

            Handles.DrawLine(point.pointPosition, handlePos);
            Vector3 newPos = Handles.FreeMoveHandle(handlePos, Quaternion.identity, 2f, Vector3.zero, Handles.SphereHandleCap);

            if (newPos != handlePos)
            {
                point.rotatorPointPosition = newPos;
            }

        }
    }

    public void DrawCircuitCurveHandles(Curve curve, RaceCircuitCreator creator)
    {
        if (!creator) return;
        for (int i = 0; i < curve.points.Count; i++)
        {
            Point point = curve.points[i];
            DrawCircuitPointHandles(point, creator);

            if (creator.showCurves)
            {
                if (curve.isClosed)
                {
                    if (i < curve.points.Count)
                    {
                        Point nextPoint = curve.points[i].nextPoint;
                        Handles.DrawBezier(point.transform.position, nextPoint.transform.position, point.controlPointPositionForward, nextPoint.controlPointPositionBackward, Color.green, null, 2);
                    }
                }
                else
                {
                    if (i < curve.points.Count - 1)
                    {
                        Point nextPoint = curve.points[i + 1];
                        Handles.DrawBezier(point.transform.position, nextPoint.transform.position, point.controlPointPositionForward, nextPoint.controlPointPositionBackward, Color.green, null, 2);
                    }
                }
            }
        }
    }


    public void DrawBufferCurveHandles(Ray ray, RaceCircuitCreator creator)
    {
        if (!creator) return;
        Curve curve = creator.curveBuffer;

        if (curve)
        {

            for (int i = 0; i < curve.points.Count; i++)
            {
                Point point = curve.points[i];
                DrawCircuitPointHandles(point, creator);


                if (curve.isClosed)
                {
                    if (i < curve.points.Count)
                    {
                        Point nextPoint = curve.points[i].nextPoint;
                        Handles.DrawBezier(point.transform.position, nextPoint.transform.position, point.controlPointPositionForward, nextPoint.controlPointPositionBackward, Color.green, null, 2);
                    }
                }
                else
                {
                    if (i < curve.points.Count - 1)
                    {
                        Point nextPoint = curve.points[i + 1];
                        Handles.DrawBezier(point.transform.position, nextPoint.transform.position, point.controlPointPositionForward, nextPoint.controlPointPositionBackward, Color.green, null, 2);
                    }
                }

            }


            Vector3 mousePos = ray.origin;
            if (ray.direction.y == 0)
            {
                if (ray.origin.y != creator.curveBufferHeight)
                {
                    return;
                }
            }

            float t = (creator.curveBufferHeight - ray.origin.y) / ray.direction.y;

            if (t < 0)
            {
                return;
            }

            mousePos = ray.origin + t * ray.direction;

            if (curve.points.Count > 0)
                Handles.DrawBezier(curve.points[^1].transform.position, mousePos, curve.points[^1].controlPointPositionForward, (mousePos + curve.points[^1].controlPointPositionForward) * 0.5f, Color.green, null, 2);


        }
    }



    public void DrawRoadHandles(Road road, RaceCircuitCreator creator)
    {
        if (!creator) return;
        for (int i = 0; i < road.associatedPoints.Count; i++)
        {
            Point point = road.associatedPoints[i];
            DrawCircuitPointHandles(point, creator);

            if (creator.showCurves)
            {
                if (i < road.associatedPoints.Count - 1)
                {
                    Point nextPoint = road.associatedPoints[i + 1];
                    Handles.DrawBezier(point.transform.position, nextPoint.transform.position, point.controlPointPositionForward, nextPoint.controlPointPositionBackward, Color.green, null, 2);
                }
            }
        }
    }


    public void DrawCircuitPointHandles(Point point, RaceCircuitCreator creator)
    {
        if (!creator) return;
        Handles.color = point.Selected ? creator.selectedPointColor : creator.pointGizmoColor;
        //Handles.DrawSolidDisc(point.pointPosition, SceneView.GetAllSceneCameras()[0].transform.position - point.pointPosition, 2);
        Event e = Event.current;
        int controlID = GUIUtility.GetControlID(FocusType.Passive);
        switch (e.GetTypeForControl(controlID))
        {
            case EventType.MouseDown:
                if (HandleUtility.nearestControl == controlID && e.button == 0)
                {
                    Debug.Log("Handle clicked!");



                    if (creator.selectPoints)
                    {
                        if (point.Selected)
                        {
                            creator.DeselectPoint(point);
                        }
                        else
                        {
                            creator.SelectPoint(point);
                        }
                    }
                    // Handle logic when the handle is clicked but newPos isn't different
                }
                break;
        }


        Vector3 newPos = Handles.FreeMoveHandle(controlID, point.pointPosition, Quaternion.identity, 4f, Vector2.zero, Handles.SphereHandleCap);

        if (creator.selectPoints) return;

        if (newPos != point.pointPosition)
        {
            Undo.RecordObject(creator, "Move Anchor Point 1");
            point.transform.position = newPos;

            if (creator.autoSetControlPoints)
            {
                point.AutoSetAnchorControlPoints();
            }
        }

        if (creator.editingCrossSection)
        {
            DrawCrossSectionPointHandles(point, creator);
        }

        if (creator.editingControlPoints)
        {
            DrawControlPointHandles(point, creator);

        }


        Handles.color = Color.yellow;
        Vector3 handlePos = point.rotatorPointPosition;

        Handles.DrawLine(point.pointPosition, handlePos);
        Vector3 newPo2 = Handles.FreeMoveHandle(handlePos, Quaternion.identity, 2f, Vector3.zero, Handles.SphereHandleCap);

        if (newPo2 != handlePos)
        {
            point.rotatorPointPosition = newPo2;
        }
    }



    public void DrawCrossSectionPointHandles(Point circuitPoint, RaceCircuitCreator creator)
    {
        if (!creator) return;
        int c = circuitPoint.crossSectionCurve.points.Count;
        for (int i = 0; i < c; i++)
        {
            Point point = circuitPoint.crossSectionCurve.points[i];

            Handles.color = Color.cyan;
            Vector3 newPos = Handles.FreeMoveHandle(point.pointPosition, Quaternion.identity, 0.3f, Vector2.zero, Handles.SphereHandleCap);

            if (newPos != point.pointPosition)
            {
                Undo.RecordObject(creator, "Move Anchor Point 1");
                newPos = Vector3.ProjectOnPlane(newPos - circuitPoint.pointPosition, circuitPoint.transform.forward) + circuitPoint.pointPosition;
                //point.ProjectSelf(circuitPoint.pointPosition, circuitPoint.GetAC());

                if (i == 0 || i == c - 1)
                {
                    newPos = Vector3.Project(newPos - circuitPoint.pointPosition, circuitPoint.transform.right) + circuitPoint.pointPosition;
                    point.pointPosition = newPos;
                    circuitPoint.transformToAlignEndPoints();
                }
                else
                {
                    point.pointPosition = newPos;
                }

                point.AutoSetAnchorControlPoints();
            }

            if (i < circuitPoint.crossSectionCurve.points.Count - 1)
            {
                Point nextPoint = circuitPoint.crossSectionCurve.points[i + 1];
                Handles.DrawBezier(point.transform.position, nextPoint.transform.position, point.controlPointPositionForward, nextPoint.controlPointPositionBackward, Color.green, null, 2);
            }

        }
    }



    public void DrawControlPointHandles(Point point, RaceCircuitCreator creator)
    {
        if (!creator) return;
        Handles.color = Color.blue;

        Handles.DrawLine(point.controlPointPositionForward, point.pointPosition, 2);
        Handles.DrawLine(point.pointPosition, point.controlPointPositionBackward, 2);

        Vector3 newPos = Handles.FreeMoveHandle(point.controlPointPositionForward, Quaternion.identity, 2f, Vector2.zero, Handles.SphereHandleCap);


        if (newPos != point.controlPointPositionForward)
        {
            newPos = Vector3.ProjectOnPlane(newPos - point.pointPosition, point.transform.up) + point.pointPosition;

            Undo.RecordObject(creator, "Move Anchor Point 1");
            point.controlPointPositionForward = newPos;
            //creator.pointTransformChanged = true;


            if (!creator.independentControlPoints)
            {
                float dist = (point.pointPosition - point.controlPointPositionBackward).magnitude;
                Vector3 dir = (point.pointPosition - newPos).normalized;
                point.controlPointPositionBackward = point.pointPosition + dir * dist;
            }
        }


        newPos = Handles.FreeMoveHandle(point.controlPointPositionBackward, Quaternion.identity, 2f, Vector2.zero, Handles.SphereHandleCap);
        if (newPos != point.controlPointPositionBackward)
        {
            newPos = Vector3.ProjectOnPlane(newPos - point.pointPosition, point.transform.up) + point.pointPosition;

            Undo.RecordObject(creator, "Move Anchor Point 2");
            point.controlPointPositionBackward = newPos;
            //creator.pointTransformChanged = true;
            //point.PerpendicularizeCrossSection(true);


            if (!creator.independentControlPoints)
            {
                float dist = (point.pointPosition - point.controlPointPositionForward).magnitude;
                Vector3 dir = (point.pointPosition - newPos).normalized;
                point.controlPointPositionForward = point.pointPosition + dir * dist;
            }
        }
    }
}
