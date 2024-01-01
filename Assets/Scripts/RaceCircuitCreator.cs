using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

[ExecuteAlways]
public class RaceCircuitCreator : MonoBehaviour
{
    [Range(2, 10)]
    public int cross_section_point_count = 3;

    [Range(2, 20)]
    public int width_wise_vertex_count = 10;

    [Range(0.1f, 10f)]
    public float length_wise_vertex_count_ratio = 1;

    //References
    public List<Curve> raceCurves;

    // won't touch the gizmo stuff
    public GameObject gizmoHolder;
    public GameObject circuitPointGizmoHolder;
    public List<GameObject> circuitPointGizmoList = new List<GameObject>();
    /*public GameObject gizmoPrefab;*//**/


    //Prefabs:
    public GameObject circuitPointGizmoPrefab; // Might want multiple gizmo prefabs of multiplesizes
    public GameObject smallGizmoPrefab;
    public GameObject pointPrefab;
    public GameObject roadPrefab;

    public GameObject testSphere;

    [Range(0, 1)]
    public float tempI = 0.5f;

    [Range(0, 1)]
    public float tempJ = 0.5f;
    //PUT ALL EDITOR RELATED CODE HERE


    //ADD FUNCTIONS AND VARIABLES, all are inspector button handlers
    public void ADD_POINT_ALONG_TRACK()
    {

    }

    public void ADD_POINT_OUTSIDE()
    {

    }

    public void CONNECT()
    {

    }

    public void ADD_ROAD()
    {

    }

    public void REMOVE_ROAD()
    {

    }

    public void EDIT_CROSS_SECTION()
    {
        //all gizmos are hidden or disabled or whatever, and new gizmos are created for only the cross-section points
    }







    //DRAWING FUNCTIONS AND VARIABLES


    //void DrawBezierHandles(Point p1, Point p2, Color bezierColor, Color handleColor)
    //{
    //    Handles.color = handleColor;
    //    Vector3 newPos = Handles.FreeMoveHandle(p1.controlPointPositionForward, Quaternion.identity, 0.3f, Vector2.zero, Handles.SphereHandleCap);


    //    if (newPos != p1.controlPointPositionForward)
    //    {
    //        p1.controlPointPositionForward = newPos;

    //        float dist = (p1.pointPosition - p1.controlPointPositionBackward).magnitude;
    //        Vector3 dir = (p1.pointPosition - newPos).normalized;
    //        p1.controlPointPositionBackward = p1.pointPosition + dir * dist;

    //    }

    //    newPos = Handles.FreeMoveHandle(p2.controlPointPositionBackward, Quaternion.identity, 0.3f, Vector2.zero, Handles.SphereHandleCap);
    //    if (newPos != p2.controlPointPositionBackward)
    //    {
    //        p2.controlPointPositionBackward = newPos;

    //        float dist = (p2.pointPosition - p2.controlPointPositionForward).magnitude;
    //        Vector3 dir = (p2.pointPosition - newPos).normalized;
    //        p2.controlPointPositionForward = p2.pointPosition + dir * dist;
    //    }
    //}

    void DrawCurve(Curve curve, bool crossSection = false)
    {
        // assuming the first point in the list is the first point
        // (though I guess it doesn't matter which one we start from)
        Point firstPoint = curve.points[0];
        Point point = firstPoint;
        do
        {
            Point nextPoint = point.nextPoint;

            if (nextPoint && (point.nextPoint != firstPoint || curve.isClosed) )
                Handles.DrawBezier(point.transform.position, nextPoint.transform.position, point.controlPointPositionForward, nextPoint.controlPointPositionBackward, Color.green, EditorGUIUtility.whiteTexture, 2);

            if (!crossSection)
            {
                DrawCurve(point.crossSectionCurve, true);
            }

            point = nextPoint;
        } while (point && point != firstPoint);
    }

    private void DrawCircuitCurve()
    {
        foreach (Curve curve in raceCurves)
        {
            DrawCurve(curve);
        }
    }

    private void DrawCrossSectionCurve(Point point)
    {

    }

    private void BuildRoad()
    {

    }

    void CreateGizmo()
    {
        // Creates a gizmo based on a prefab that is selected and used to move POINTs around
    }



    //SELECTION FUNCITONS AND VARIABLES

    //Some kind of state variable saying what is selected, circuit or road. This state will be read by a button refresher function that makes buttons interactive and non-interactive based on it

    bool circuitSelected;
    Road selectedRoad; //Null if none selected
    Point selectedPoint; //Null if none selected

    public void SelectCircuit()
    {
        Debug.Log("SELECTED CIRCUIT");
        //activated when circuit object is selected
        circuitSelected = true;
        //Spline is shown for the entire network

        // Draw(raceCircuit.circuitCurve);

        //Gizmos are created at each POINT on the circuit curve
        foreach (Curve curve in raceCurves)
        {
            foreach (Point point in curve.points)
            {
                if (!point.myGizmo)
                {
                    GameObject t = Instantiate(circuitPointGizmoPrefab, point.transform.position, Quaternion.identity);
                    t.transform.SetParent(gizmoHolder.transform);
                    t.GetComponent<CircuitPointGizmo>().correspondingPoint = point;
                    point.myGizmo = t.GetComponent<CircuitPointGizmo>();
                    t.GetComponent<CircuitPointGizmo>().creator = this;
                    circuitPointGizmoList.Add(t);
                }
            }
        }

    }

    private void OnDrawGizmos/*Selected*/()
    {
        DrawCircuitCurve();

        // NOTE: we'll do this only when the corresponding gizmo moves but that's only after @Bella adds them
        foreach (Curve curve in raceCurves)
        {
            foreach (Point point in curve.points)
            {
                // point.PerpendicularizeCrossSection();
                point.UpdateLength();

                foreach (Point crossSectionPoint in point.crossSectionCurve.points)
                {
                    crossSectionPoint.UpdateLength();
                    crossSectionPoint.AutoSetAnchorControlPoints();
                }
                point.crossSectionCurve.points.First().AutoSetStart();
                point.crossSectionCurve.points.Last().AutoSetEnd();
            }
        }

        foreach (Curve curve in raceCurves)
        {
            curve.NormalizeCurvePoints();
            foreach (Point point in curve.points)
            {
                point.crossSectionCurve.NormalizeCurvePoints();
            }
        }

        foreach (Curve curve in raceCurves)
        {
            if (curve.isClosed != curve.prevIsClosed)
            {
                curve.Reinitialize();
                curve.prevIsClosed = curve.isClosed;
            }
        }

        Vector3 spherePos = GetPointFromij(raceCurves[0].points[0], raceCurves[0].points[1], tempI, tempJ);
        testSphere.transform.position = spherePos;
    }

        public void SelectRoad(Road selectedRoad)
    {
        //Highlight the road somehow. Maybe give it a temporary material or something
        //Spline is shown for only the POINTS on the road
        //Only road points have gizmos, others are deleted
    }

    public void SelectPoint(Point _selectedPoint)
    {
        selectedPoint = _selectedPoint;
        //if Circuit is selected, circuit stays selected, and moving the gizmo moves the corresponding POINT


    }

    //Each of the above Select function also has a Deselect counterpart that destroys Gizmos and stuff like that


    public void DeselectAll()
    {
        //Activate this when you click on an empty space in the scene
        if (circuitSelected)
        {
            if (selectedPoint)
            {
                selectedPoint = null;
            }

            if (circuitPointGizmoList.Count > 0)
            {
                for (int i = circuitPointGizmoList.Count - 1; i >= 0; i = i - 1)
                {
                    //circuitPointGizmoList[i].GetComponent<CircuitPointGizmo>().correspondingPoint.myGizmo = null;
                    DestroyImmediate(circuitPointGizmoList[i]);
                }
                circuitPointGizmoList.Clear();
            }
            circuitSelected = false;

        }


    }

    public void ButtonRefresh()
    {
        //Reads the selection state and updates buttons
    }

    private void Update()
    {
        if (EditorApplication.isPlaying)
        {
            DestroyImmediate(this);
        }
    }

    void OnEnable()
    {
        //creator = (RaceCircuitCreator)target;

        Debug.Log("Started");
        //Debug.Log("Found a creator root object w ith name: " + raceCircuitCreator);

        //Selection.selectionChanged += OnSelectionChanged;
        Selection.selectionChanged += OnSelectionChanged;
        Debug.Log("Selection function set baby!");



        foreach (Curve curve in raceCurves)
        {
            curve.Reinitialize();

            foreach (Point point in curve.points)
            {
                point.crossSectionCurve.Reinitialize();

                point.PerpendicularizeCrossSection();
                point.AutoSetAnchorControlPoints();
                foreach (Point crossSectionPoint in point.crossSectionCurve.points)
                {
                    // crossSectionPoint.UpdateLengths();
                    crossSectionPoint.AutoSetAnchorControlPoints();
                }
                //point.crossSectionCurve.points.First().AutoSetStart();
                //point.crossSectionCurve.points.Last().AutoSetEnd();
            }
        }

    }

    // this draws it all the time instead of just on selection
    //void OnRenderObject()
    //{
    //    Draw(raceCircuit.circuitCurve);
    //}

    Vector3 GetPointFromi(Curve curve, float i)
    {
        // [OPTIMIZE]

        // NOTE we're assuming that i is across meaning there can be no branches when computing i
        // meaning we can only have at max one forward point
        // meaning this only works for CrossSection curves

        // linearly searching for now
        for (int index = 0; index < curve.points.Count; index++)
        {
            if (curve.points[index].normalizedPositionAlongCurve == i)
            {
                // we're exactly on the thing
                return curve.points[index].pointPosition;
            }
            else if ((index < curve.points.Count - 1) && curve.points[index].normalizedPositionAlongCurve < i && curve.points[index + 1].normalizedPositionAlongCurve > i)
            {
                // remap the range i.e. ilerp [p[i].normalized, p[i + 1].normalized] -> 0, 1

                // lerp: x = a + (b-a) * t
                // ilerp x - a /  b - a

                float a = curve.points[index].normalizedPositionAlongCurve;
                float b = curve.points[index + 1].normalizedPositionAlongCurve;
                float t = (i - a) / (b - a);
                return Point.CalculateBezierPoint(curve.points[index].pointPosition,
                                                    curve.points[index].controlPointPositionForward,
                                                    curve.points[index + 1].controlPointPositionBackward,
                                                    curve.points[index + 1].pointPosition,
                                                    t);

            }
        }

        return Vector3.zero;
    }

    // a and b refer to the anchor points in the big loop which contains the curves we're getting the point from
    public Vector3 GetPointFromij(Point a, Point b, float i, float j)
    {
        Vector3 iaPos = GetPointFromi(a.crossSectionCurve, i);
        Vector3 ibPos = GetPointFromi(b.crossSectionCurve, i);

        // TODO: rethink this
        // scaling the control points 
        float curveLength = a.nextSegmentLength;
        float scale = curveLength / (a.pointPosition - b.pointPosition).magnitude;
        float dist = (iaPos - ibPos).magnitude;

        Vector3 controlForward = ((a.controlPointPositionForward - a.pointPosition) / scale) *  dist + iaPos;
        Vector3 controlBackward = ((b.controlPointPositionBackward - b.pointPosition) / scale) * dist + ibPos;


        Handles.color = Color.blue;
        Handles.DrawLine(iaPos, controlForward);
        Handles.DrawLine(ibPos, controlBackward);
        Handles.DrawBezier(iaPos, ibPos, controlForward, controlBackward, Color.white, null, 2);

        return Point.CalculateBezierPoint(iaPos, controlForward, controlBackward, ibPos, j);
    }

    private void OnDestroy()
    {
        Selection.selectionChanged -= OnSelectionChanged;
    }
    private void OnDisable()
    {
        Selection.selectionChanged -= OnSelectionChanged;
    }

    private void OnSelectionChanged() //Called when selection changes in the editor
    {
        GameObject currentSelectedObject = Selection.activeGameObject;

        if (currentSelectedObject != null)
        {
            Debug.Log("Selected " + currentSelectedObject.name);
            if (currentSelectedObject == gameObject)
            {
                SelectCircuit();
            }
            else if (currentSelectedObject.GetComponent<CircuitPointGizmo>())
            {
                SelectPoint(currentSelectedObject.GetComponent<CircuitPointGizmo>().correspondingPoint);
            }
            else if (currentSelectedObject.GetComponent<Road>())
            {
                SelectRoad(currentSelectedObject.GetComponent<Road>());
            }
        }
        else //When clicking elsewhere
        {
            DeselectAll();
        }
    }
}

/*
public static class InitHelper
{
    public static RaceCircuitCreator raceCircuitCreator;


    //[InitializeOnLoadMethod]
    static void StartUp()
    {
        //creator = (RaceCircuitCreator)target;
        raceCircuitCreator = GameObject.FindGameObjectWithTag("RaceCircuitRootObject").GetComponent<RaceCircuitCreator>();
        Debug.Log("Started");
        Debug.Log("Found a creator root object w ith name: " + raceCircuitCreator);

        Selection.selectionChanged += OnSelectionChanged;
    }


    private static void OnSelectionChanged() //Called when selection changes in the editor
    {
        GameObject currentSelectedObject = Selection.activeGameObject;
        if (currentSelectedObject != null)
        {
            if (currentSelectedObject == raceCircuitCreator.gameObject)
            {
                raceCircuitCreator.SelectCircuit();
            }
            else if (currentSelectedObject.GetComponent<CircuitPointGizmo>())
            {
                raceCircuitCreator.SelectPoint(currentSelectedObject.GetComponent<CircuitPointGizmo>().correspondingPoint);
            }
            else if (currentSelectedObject.GetComponent<Road>())
            {
                raceCircuitCreator.SelectRoad(currentSelectedObject.GetComponent<Road>());
            }
        }
        else //When clicking elsewhere
        {
            raceCircuitCreator.DeselectAll();
        }
    }
}*/