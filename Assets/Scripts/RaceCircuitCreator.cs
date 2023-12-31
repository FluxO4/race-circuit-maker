using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections;
using System;
using Unity.VisualScripting;
using System.Drawing;

[ExecuteAlways]
public class RaceCircuitCreator : MonoBehaviour
{
    [Range(2, 10)]
    public int cross_section_point_count = 3;




    
    //References
    public RaceCircuit raceCircuit;

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

    [Range(1, 5)]
    public float rotatorHandleLength = 3.0f;

    [Range(0.05f, 1f)]
    public float roadRebuildingFrequency = 0.2f;


    #region SELECTION STUFF

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
        foreach (Curve curve in raceCircuit.circuitCurves)
        {
            foreach (Point point in curve.points)
            {
                point.EnableGizmo(true);
            }
        }

    }

    public void SelectRoad(Road _selectedRoad)
    {
        selectedRoad = _selectedRoad;
    }

    public void SelectPoint(Point _selectedPoint)
    {
        selectedPoint = _selectedPoint;

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

                //THE THING BELOW MOVES SELECTION TO ANOTHER OBJECT, USEFUL WHILE CLICKING ON MANY THINGS AT ONCE
                /*EditorApplication.delayCall += () =>
                {
                    Selection.activeGameObject = this.gameObject;
                };*/
            }

            foreach (Curve curve in raceCircuit.circuitCurves)
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


    private void OnSelectionChanged() //Called when selection changes in the editor
    {
        if(this.gameObject == null)
        {
            Destroy(this.gameObject);
        }
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

                while (t.parent != null)
                {
                    t = t.parent;
                    if (t == transform)
                    {
                        childOfRaceCircuit = true;
                        break;
                    }
                }

                if (!childOfRaceCircuit)
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

    #endregion


    #region USER CONTROL FUNCTIONS AND VARIABLES

    public bool AddPoint = false;


    public void mouseInput(Vector2 screenPos, Ray inputRayWS)
    {
        if (AddPoint)
        {
            findClosestPoints(inputRayWS);

        }

    }

    public Vector3 ClosestPointOnLine(Ray ray, Vector3 point)
    {
        Vector3 lineToPoint = point - ray.origin;
        Vector3 projectedVector = Vector3.Project(lineToPoint, ray.direction);
        Vector3 projectedPoint = ray.origin + projectedVector;

        return projectedPoint;
    }

    public void PerpendicularizeAllCrossSections()
    {
        foreach (Curve curve in raceCircuit.circuitCurves)
        {
            foreach (Point point in curve.points)
            {
                point.PerpendicularizeCrossSection();

                foreach (Point crossSectionPoint in point.crossSectionCurve.points)
                {
                    crossSectionPoint.AutoSetAnchorControlPoints();
                }
            }
        }
    }

    public void findClosestPoints(Ray inputRayWS)
    {
        /*position*/
        //Vector3 mousePoint = screen2xzplane(guiEvent);


        Point closestPoint = raceCircuit.circuitCurves[0].points[0];
        float minDistance = float.MaxValue;
        Vector3 closestPosition = Vector3.zero;

        foreach (Curve curve in raceCircuit.circuitCurves)
        {
            foreach (Point point in curve.points)
            {
                Vector3 tempClosestPosition = ClosestPointOnLine(inputRayWS, point.pointPosition);
                Vector3 differenceVector = tempClosestPosition - point.pointPosition;
                float calcDistance = (new Vector3(differenceVector.x, differenceVector.y * 10, differenceVector.z)).sqrMagnitude;

                if (calcDistance < minDistance)
                {
                    closestPoint = point;
                    minDistance = calcDistance;
                    closestPosition = tempClosestPosition;
                }
            }
        }

        Debug.Log(closestPoint + " " + closestPoint.pointPosition);
        testSphere.transform.position = closestPosition;


        Curve closestCurve = closestPoint.parentCurve;
        int closestIndex = closestCurve.points.IndexOf(closestPoint);

        Point otherPoint = null;
        Point leftPoint;
        Point rightPoint;

        if (closestCurve.isClosed)
        {
            leftPoint = closestIndex > 0 ? closestCurve.points[closestIndex - 1] : closestCurve.points[closestCurve.points.Count - 1];
            rightPoint = closestIndex < closestCurve.points.Count - 1 ? closestCurve.points[closestIndex + 1] : closestCurve.points[0];
        }
        else
        {
            leftPoint = closestIndex > 0 ? closestCurve.points[closestIndex - 1] : null;
            rightPoint = closestIndex < closestCurve.points.Count - 1 ? closestCurve.points[closestIndex + 1] :null;
        }

        if (leftPoint != null && rightPoint != null)
        {
            Vector3 leftRay = leftPoint.pointPosition - closestPosition;
            Vector3 rightRay = rightPoint.pointPosition - closestPosition;
            Vector3 closestRay = closestPoint.pointPosition - closestPosition;

            float left_angle = Vector3.Angle(leftRay, closestRay);
            float right_angle = Vector3.Angle(rightRay, closestRay);

            if (left_angle > right_angle)
            {
                Debug.Log("Left Point: " + leftPoint + " Position: " + leftPoint.pointPosition);
                otherPoint = leftPoint;
            }
            else
            {
                Debug.Log("Right Point: " + rightPoint + " Position: " + rightPoint.pointPosition);
                otherPoint = rightPoint;
            }
        }else if(leftPoint == null)
        {
            otherPoint = rightPoint;
       
        }else if (rightPoint == null)
        {
            otherPoint = leftPoint;
            
        }

        Debug.Log("MAIN POINT IS " + closestPoint.name + " OTHER POINT IS " + otherPoint.name);

        int mainPointIndex = closestCurve.points.IndexOf(closestPoint);
        int secondPointIndex = closestCurve.points.IndexOf(otherPoint);

        if(mainPointIndex > secondPointIndex)
        {
            int t = mainPointIndex;
            mainPointIndex = secondPointIndex;
            secondPointIndex = t;

            closestPoint = closestCurve.points[mainPointIndex];
            otherPoint = closestCurve.points[secondPointIndex];
        }
        if(secondPointIndex - mainPointIndex != 1)
        {
            Debug.LogError("Problem with finding two closest points");
        }


        Point newpoint = (PrefabUtility.InstantiatePrefab(pointPrefab, closestCurve.transform) as GameObject).GetComponent<Point>();
        newpoint.transform.SetSiblingIndex(secondPointIndex);
        closestCurve.AutoSetPreviousAndNextPoints();
        closestCurve.NormalizeCurvePoints();

        newpoint.pointPosition = closestPosition;
        newpoint.rotatorPointPosition = newpoint.transform.up;

        Vector3 avgUp = (closestPoint.transform.up + otherPoint.transform.up).normalized;
        Vector3 avgZ = (closestPoint.transform.forward + otherPoint.transform.forward).normalized;
        Matrix4x4 rotationMat = new Matrix4x4(-Vector3.Cross(avgZ, avgUp), avgUp, avgZ, Vector4.zero);
        newpoint.transform.rotation = rotationMat.rotation;

        newpoint.AutoSetAnchorControlPoints();

        newpoint.crossSectionCurve.creator = this;
        
        newpoint.crossSectionCurve.AutoSetPreviousAndNextPoints();
        newpoint.crossSectionCurve.NormalizeCurvePoints();
        newpoint.PerpendicularizeCrossSection();
        newpoint.crossSectionCurve.AutoSetAllControlPoints();

        /*Roads*/
        foreach(Road road in raceCircuit.roads)
        {
            for(int i = 0; i < road.associatedPoints.Count; i++)
            {
                if (road.associatedPoints[i] == closestCurve.points[mainPointIndex])
                {
                    if (i < road.associatedPoints.Count - 1)
                    {
                        road.associatedPoints.Insert(i + 1, newpoint);
                    }
                    else
                    {
                        road.associatedPoints.Add(newpoint);
                    }
                    pointTransformChanged = true;
                    break;
                }
            }
        }
    }


    #endregion


    #region UNITY MESSAGES

    private void OnDrawGizmos()
    {
        foreach (Curve curve in raceCircuit.circuitCurves)
        {
            if (curve.isClosed != curve.prevIsClosed)
            {
                curve.AutoSetPreviousAndNextPoints();
                curve.prevIsClosed = curve.isClosed;
            }
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

                pointTransformChanged = false;
            }


            yield return new WaitForSeconds(0.2f);
        }

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


        foreach (Curve curve in raceCircuit.circuitCurves)
        {
            curve.AutoSetPreviousAndNextPoints();

            foreach (Point point in curve.points)
            {
                point.crossSectionCurve.AutoSetPreviousAndNextPoints();

                //point.PerpendicularizeCrossSection();


                /*point.AutoSetAnchorControlPoints();
                foreach (Point crossSectionPoint in point.crossSectionCurve.points)
                {
                    crossSectionPoint.AutoSetAnchorControlPoints();
                }*/
            }
        }

        // NOTE: we'll do this only when the corresponding gizmo moves but that's only after @Bella adds them
        foreach (Curve curve in raceCircuit.circuitCurves)
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

        foreach (Curve curve in raceCircuit.circuitCurves)
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

    #endregion

}

