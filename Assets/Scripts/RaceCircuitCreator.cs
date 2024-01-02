using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections;

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
    public RaceCircuit raceCircuit;
    public List<Curve> raceCurves;

    // won't touch the gizmo stuff
   /* public GameObject gizmoHolder;
    public GameObject circuitPointGizmoHolder;
    public List<GameObject> circuitPointGizmoList = new List<GameObject>();*/
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

    public bool circuitSelected = false;
    public Road selectedRoad; //Null if none selected
    public Point selectedPoint; //Null if none selected

    public bool showCurves = true;
    public bool editingCrossSection = false;
    public bool editingControlPoints = false;
    public bool autoSetControlPoints = false;
    public bool independentControlPoints = false;
    
    public bool updateOnlyOnRelease = true;

    [Range(0.05f, 1f)]
    public float roadRebuildingFrequency = 0.2f;


    public void SelectCircuit()
    {
        Debug.Log("SELECTED CIRCUIT");
        //activated when circuit object is selected
        circuitSelected = true;
        //Spline is shown for the entire network

        foreach (Road road in raceCircuit.roads)
        {
            road.buildRoad();
        }
        // Draw(raceCircuit.circuitCurve);

        //Gizmos are created at each POINT on the circuit curve
        foreach (Curve curve in raceCurves)
        {
            foreach (Point point in curve.points)
            {
                point.EnableGizmo(true);
                
                /*if (!point.myGizmo)
                {
                    GameObject t = Instantiate(circuitPointGizmoPrefab, point.transform.position, Quaternion.identity);
                    t.transform.SetParent(gizmoHolder.transform);
                    t.GetComponent<CircuitPointGizmo>().correspondingPoint = point;
                    point.myGizmo = t.GetComponent<CircuitPointGizmo>();
                    t.GetComponent<CircuitPointGizmo>().creator = this;
                    circuitPointGizmoList.Add(t);
                }*/
            }
        }

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

                /*EditorApplication.delayCall += () =>
                {
                    Selection.activeGameObject = this.gameObject;
                };*/
            }

            /*if (circuitPointGizmoList.Count > 0)
            {
                for (int i = circuitPointGizmoList.Count - 1; i >= 0; i = i - 1)
                {
                    //circuitPointGizmoList[i].GetComponent<CircuitPointGizmo>().correspondingPoint.myGizmo = null;
                    DestroyImmediate(circuitPointGizmoList[i]);
                }
                circuitPointGizmoList.Clear();
            }
            circuitSelected = false;
            */

            foreach (Curve curve in raceCurves)
            {
                foreach (Point point in curve.points)
                {
                    point.EnableGizmo(false);
                }
            }
            circuitSelected = false;
        }

        selectedRoad = null;
        selectedPoint = null;



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

        raceCircuit = GetComponent<RaceCircuit>();


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
                    crossSectionPoint.AutoSetAnchorControlPoints();
                }
            }
        }

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

        foreach(Road road in raceCircuit.roads)
        {
            road.buildRoad();
        }

        roadRebuildLimiter = StartCoroutine(RoadRebuildingLimiter());

    }

    // this draws it all the time instead of just on selection
    //void OnRenderObject()
    //{
    //    Draw(raceCircuit.circuitCurve);
    //}
    

    private void OnDestroy()
    {
        StopCoroutine(roadRebuildLimiter);
        StopAllCoroutines();
        Selection.selectionChanged -= OnSelectionChanged;
    }
    private void OnDisable()
    {
        StopCoroutine(roadRebuildLimiter);
        StopAllCoroutines();
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
            else
            {
                Transform t = currentSelectedObject.transform;
                bool childOfRaceCircuit = false;

                while(t.parent != null)
                {
                    t = t.parent;
                    if(t == transform)
                    {
                        childOfRaceCircuit = true;
                        break;
                    }
                }

                if(!childOfRaceCircuit)
                {
                    DeselectAll();
                }
            }
        }
        else //When clicking elsewhere
        {
            DeselectAll();
        }
    }


    public bool pointTransformChanged = false;

    Coroutine roadRebuildLimiter = null;
    public IEnumerator RoadRebuildingLimiter()
    {
        for (; ; )
        {
            if (pointTransformChanged)
            {
                Debug.Log("REBUILDING ROADS!");

                foreach (Road road in raceCircuit.roads)
                {

                    road.buildRoad();
                }
            }


            yield return new WaitForSeconds(0.2f);
        }
            
    }


    private void OnDrawGizmos()
    {
        if (circuitSelected)
        {
            if (selectedRoad != null)
            {
                DrawRoadHandles(selectedRoad);
            }
            else if (selectedPoint != null)
            {
                DrawCircuitPointHandles(selectedPoint);
            }
            else
            {
                foreach (Curve curve in raceCurves)
                {
                    DrawCircuitCurveHandles(curve);
                }
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
    }

    void DrawCircuitCurveHandles(Curve curve)
    {
        for (int i = 0; i < curve.points.Count; i++)
        {
            Point point = curve.points[i];
            DrawCircuitPointHandles(point);

            if (showCurves)
            {
                if (i < curve.points.Count - 1)
                {
                    Point nextPoint = curve.points[i + 1];
                    Handles.DrawBezier(point.transform.position, nextPoint.transform.position, point.controlPointPositionForward, nextPoint.controlPointPositionBackward, Color.green, null, 2);
                }
            }
        }
    }


    void DrawRoadHandles(Road road)
    {
        for (int i = 0; i < road.associatedPoints.Count; i++)
        {
            Point point = road.associatedPoints[i];
            DrawCircuitPointHandles(point);

            if (showCurves)
            {
                if (i < road.associatedPoints.Count - 1)
                {
                    Point nextPoint = road.associatedPoints[i + 1];
                    Handles.DrawBezier(point.transform.position, nextPoint.transform.position, point.controlPointPositionForward, nextPoint.controlPointPositionBackward, Color.green, null, 2);
                }
            }
        }
    }


    void DrawCircuitPointHandles(Point point)
    {
        Handles.color = Color.red;

        Vector3 newPos = Handles.FreeMoveHandle(point.pointPosition, Quaternion.identity, 0.3f, Vector2.zero, Handles.SphereHandleCap);
        if (newPos != point.pointPosition)
        {
            Undo.RecordObject(this, "Move Anchor Point 1");
            point.transform.position = newPos;
            point.moveToTransform();
            if (autoSetControlPoints)
            {
                point.AutoSetAnchorControlPoints();
            }
        }

        if (editingCrossSection)
        {
            DrawCrossSectionCurveHandles(point);
        }

        if (editingControlPoints)
        {
            DrawControlPointHandles(point);

        }
    }



    void DrawCrossSectionCurveHandles(Point circuitPoint)
    {
        for (int i = 0; i < circuitPoint.crossSectionCurve.points.Count; i++)
        {
            Point point = circuitPoint.crossSectionCurve.points[i];

            Handles.color = Color.cyan;
            Vector3 newPos = Handles.FreeMoveHandle(point.pointPosition, Quaternion.identity, 0.3f, Vector2.zero, Handles.SphereHandleCap);

            if (newPos != point.pointPosition)
            {
                Undo.RecordObject(this, "Move Anchor Point 1");
                point.pointPosition = newPos;
                circuitPoint.PerpendicularizeCrossSection();
                point.AutoSetAnchorControlPoints();
            }

            if (i < circuitPoint.crossSectionCurve.points.Count - 1)
            {
                Point nextPoint = circuitPoint.crossSectionCurve.points[i + 1];
                Handles.DrawBezier(point.transform.position, nextPoint.transform.position, point.controlPointPositionForward, nextPoint.controlPointPositionBackward, Color.green, null, 2);
            }

        }
    }



    void DrawControlPointHandles(Point point)
    {
        Handles.color = Color.blue;

        Handles.DrawLine(point.controlPointPositionForward, point.pointPosition, 2);
        Handles.DrawLine(point.pointPosition, point.controlPointPositionBackward, 2);

        Vector3 newPos = Handles.FreeMoveHandle(point.controlPointPositionForward, Quaternion.identity, 0.3f, Vector2.zero, Handles.SphereHandleCap);
        if (newPos != point.controlPointPositionForward)
        {
            Undo.RecordObject(this, "Move Anchor Point 1");
            point.controlPointPositionForward = newPos;

            if (!independentControlPoints)
            {
                float dist = (point.pointPosition - point.controlPointPositionBackward).magnitude;
                Vector3 dir = (point.pointPosition - newPos).normalized;
                point.controlPointPositionBackward = point.pointPosition + dir * dist;
            }
        }


        newPos = Handles.FreeMoveHandle(point.controlPointPositionBackward, Quaternion.identity, 0.3f, Vector2.zero, Handles.SphereHandleCap);
        if (newPos != point.controlPointPositionBackward)
        {
            Undo.RecordObject(this, "Move Anchor Point 2");
            point.controlPointPositionBackward = newPos;

            if (!independentControlPoints)
            {
                float dist = (point.pointPosition - point.controlPointPositionForward).magnitude;
                Vector3 dir = (point.pointPosition - newPos).normalized;
                point.controlPointPositionForward = point.pointPosition + dir * dist;
            }
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