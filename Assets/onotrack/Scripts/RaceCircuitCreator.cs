using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections;
using System;
using UnityEngine.TerrainTools;


[ExecuteAlways]
public class RaceCircuitCreator : MonoBehaviour
{


    public bool editMode = false;
    public bool EDIT
    {
        get { return editMode; }

        set {
            if (editMode == value) return;

            editMode = value; 
            if(editMode == true)
            {
                SetEditMode();
            }
            else
            {
                Freeze();
            }
        
        
        }
    }





    [Range(2, 10)]
    public int cross_section_point_count = 3;




    
    //References
    public RaceCircuit raceCircuit;

    //Prefabs:
    public GameObject curvePrefab;
    public GameObject pointPrefab;
    public GameObject crossSectionPointPrefab;
    public GameObject roadPrefab;

    public GameObject testSphere;

    [Range(0, 1)]
    public float tempI = 0.5f;

    [Range(0, 1)]
    public float tempJ = 0.5f;
    //PUT ALL EDITOR RELATED CODE HERE


    //SELECTION FUNCITONS AND VARIABLES



    public bool selectPoints = false;

    public bool circuitSelected = false;
    public Road selectedRoad; //Null if none selected

    public List<Point> selectedPoints = new List<Point>(); //Null if none selected

    public bool showCurves = true;
    public bool editingCrossSection = false;
    public bool editingControlPoints = false;
    public bool autoSetControlPoints = false;
    public bool independentControlPoints = false;

    public Color selectedPointColor = Color.yellow;
    public Color pointGizmoColor = Color.red;
    
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
        initialise();
        
        // Draw(raceCircuit.circuitCurve);

        //Gizmos are created at each POINT on the circuit curve
        /*foreach (Curve curve in raceCircuit.circuitCurves)
        {
            foreach (Point point in curve.points)
            {
                point.EnableGizmo(true);
            }
        }*/

    }

    public void SelectRoad(Road _selectedRoad)
    {
        selectedRoad = _selectedRoad;
    }

    public bool continuousPoints = false;
    public bool checkContinuousSelectedPoints()
    {
        int temp = selectedPoints[0].pointIndex - 1;
        int discontinuityCount = 0;
        int newStartIndex = 0;
        for (int i = 0; i < selectedPoints.Count; i++)
        {
            if (selectedPoints[(i+1)%selectedPoints.Count] != selectedPoints[i].nextPoint)
            {
                discontinuityCount++;
                newStartIndex = (i + 1) % selectedPoints.Count;
            }
        }
        if(discontinuityCount > 1)
        {
            return false;
        }

        for(int i = 0; i < newStartIndex; i++)
        {
            selectedPoints.Add(selectedPoints[0]);
            selectedPoints.RemoveAt(0);
        }

        return true;
    }
    public void SelectPoint(Point _selectedPoint)
    {
        _selectedPoint.Selected = true;
        selectedPoints.Add(_selectedPoint);
        // Assuming 'points' is your List<Point>
        selectedPoints.Sort((pointA, pointB) => pointA.pointIndex.CompareTo(pointB.pointIndex));


        continuousPoints = checkContinuousSelectedPoints();
    }

    public void DeselectPoint(Point _selectedPoint)
    {
        _selectedPoint.Selected = false;
        selectedPoints.Remove(_selectedPoint);

        if(selectedPoints.Count > 0)
        continuousPoints = checkContinuousSelectedPoints();
    }

    //Each of the above Select function also has a Deselect counterpart that destroys Gizmos and stuff like that

    public void DeselectAllPoints()
    {
        if (selectedPoints.Count > 0)
        {
            foreach (Point point in selectedPoints)
            {
                point.Selected = false;
            }
            selectedPoints.Clear();
        }
    }

    public void DeselectAll()
    {
        //Activate this when you click on an empty space in the scene
        if (circuitSelected)
        {
            if (selectedPoints.Count>0)
            {
                foreach(Point point in selectedPoints)
                {
                    point.Selected = false;
                }
                selectedPoints.Clear();

                //THE THING BELOW MOVES SELECTION TO ANOTHER OBJECT, USEFUL WHILE CLICKING ON MANY THINGS AT ONCE
                /*EditorApplication.delayCall += () =>
                {
                    Selection.activeGameObject = this.gameObject;
                };*/
            }

           /* foreach (Curve curve in raceCircuit.circuitCurves)
            {
                foreach (Point point in curve.points)
                {
                    point.EnableGizmo(false);
                }
            }*/
            circuitSelected = false;
        }
        selectPoints = false;
        selectedRoad = null;
        AddPoint = false;
    }


    private void OnSelectionChanged() //Called when selection changes in the editor
    {
        if (this== null)
        {
            Destroy(this);
        }

        if (this.gameObject == null)
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
            else if (currentSelectedObject.GetComponent<CircuitPointGizmo>() && selectPoints)
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
            addPoint(inputRayWS);

        }

    }

    public Vector3 ClosestPointOnLine(Ray ray, Vector3 point)
    {
        Vector3 lineToPoint = point - ray.origin;
        Vector3 projectedVector = Vector3.Project(lineToPoint, ray.direction);
        Vector3 projectedPoint = ray.origin + projectedVector;

        return projectedPoint;
    }

    public static Vector3 GetPointOnRayWithY(Ray inputRay, Vector3 inputPosition)
    {
        float t = (inputPosition.y - inputRay.origin.y) / inputRay.direction.y;
        Vector3 pointOnRay = inputRay.origin + t * inputRay.direction;
        return pointOnRay;
    }



    public void addPointBetween(Vector3 closestPosition, Point firstPoint, Point secondPoint)
    {
        Curve closestCurve = firstPoint.parentCurve;

        if (secondPoint.pointIndex - firstPoint.pointIndex != 1)
        {
            Debug.LogError("Problem with finding two closest points");
        }


        Point newpoint = (PrefabUtility.InstantiatePrefab(pointPrefab, closestCurve.transform) as GameObject).GetComponent<Point>();
        newpoint.transform.SetSiblingIndex(secondPoint.pointIndex);
        closestCurve.AutoSetPreviousAndNextPoints();
        

        newpoint.pointPosition = closestPosition;
        newpoint.rotatorPointPosition = newpoint.transform.up;

        newpoint.transform.rotation = Quaternion.Lerp(firstPoint.transform.rotation, secondPoint.transform.rotation, 0.5f);

        newpoint.AutoSetAnchorControlPoints();
        newpoint.prevPoint.AutoSetAnchorControlPoints();
        newpoint.nextPoint.AutoSetAnchorControlPoints();
        closestCurve.NormalizeCurvePoints();

        newpoint.crossSectionCurve.creator = this;

        newpoint.crossSectionCurve.AutoSetPreviousAndNextPoints();
        newpoint.crossSectionCurve.NormalizeCurvePoints();
        newpoint.PerpendicularizeCrossSection();
        newpoint.crossSectionCurve.AutoSetAllControlPoints();


        foreach (Road road in raceCircuit.roads)
        {
            for (int i = 0; i < road.associatedPoints.Count; i++)
            {
                if (road.associatedPoints[i] == closestCurve.points[firstPoint.pointIndex])
                {
                    if (i < road.associatedPoints.Count - 1)
                    {
                        road.associatedPoints.Insert(i + 1, newpoint);
                    }
                    else
                    {
                        road.associatedPoints.Add(newpoint);
                    }
                    
                    road.initializeRoad();
                    EditorUtility.SetDirty(road);
                    pointTransformChanged = true;
                    break;
                }
            }
        }


        EditorApplication.delayCall += () =>
        {
            Selection.activeGameObject = this.gameObject;
        };

    }

    public void addEndPoint(Vector3 pointPos, Point pairPoint)
    {
        Curve closestCurve = pairPoint.parentCurve;

        Point newpoint = null;

        if (pairPoint.nextPoint == null)
        {

            newpoint = (PrefabUtility.InstantiatePrefab(pointPrefab, closestCurve.transform) as GameObject).GetComponent<Point>();
            


        }else if(pairPoint.prevPoint == null)
        {
            newpoint = (PrefabUtility.InstantiatePrefab(pointPrefab, closestCurve.transform) as GameObject).GetComponent<Point>();
            newpoint.transform.SetSiblingIndex(pairPoint.pointIndex);
        }
        else
        {
            Debug.LogError("Tried adding end point to non-end point");
        }


        closestCurve.AutoSetPreviousAndNextPoints();


        newpoint.pointPosition = pointPos;
        newpoint.rotatorPointPosition = newpoint.transform.up;

        Vector3 avgUp = pairPoint.transform.up;
        Vector3 avgZ = pairPoint.transform.forward;
        //Matrix4x4 rotationMat = new Matrix4x4(-Vector3.Cross(avgZ, avgUp), avgUp, avgZ, Vector4.zero);
        //newpoint.transform.rotation = rotationMat.rotation;
        newpoint.transform.rotation = pairPoint.transform.rotation;
        newpoint.AutoSetAnchorControlPoints();

        if(newpoint.prevPoint)
        newpoint.prevPoint.AutoSetAnchorControlPoints();
        if(newpoint.nextPoint)
        newpoint.nextPoint.AutoSetAnchorControlPoints();

        closestCurve.NormalizeCurvePoints();

        newpoint.crossSectionCurve.creator = this;

        newpoint.crossSectionCurve.AutoSetPreviousAndNextPoints();
        newpoint.crossSectionCurve.NormalizeCurvePoints();
        newpoint.PerpendicularizeCrossSection();
        newpoint.crossSectionCurve.AutoSetAllControlPoints();


        foreach (Road road in raceCircuit.roads)
        {
            for (int i = 0; i < road.associatedPoints.Count; i++)
            {
                if (road.associatedPoints[i] == closestCurve.points[pairPoint.pointIndex])
                {
                    if(pairPoint.pointIndex > newpoint.pointIndex)
                    {
                        road.associatedPoints.Insert(i, newpoint);
                    }
                    else
                    {
                        if (i < road.associatedPoints.Count - 1)
                        {
                            road.associatedPoints.Insert(i + 1, newpoint);
                        }
                        else
                        {
                            road.associatedPoints.Add(newpoint);
                        }
                    }

                   

                    road.initializeRoad();
                    EditorUtility.SetDirty(road);
                    pointTransformChanged = true;
                    break;
                }
            }
        }


        EditorApplication.delayCall += () =>
        {
            Selection.activeGameObject = this.gameObject;
        };
    }


    public bool creatingCurve = false;
    public float curveBufferHeight = 0;
    public Curve curveBuffer;
    public void addPointToCurveBuffer(Ray ray)
    {
        Vector3 pointPos = ray.origin;

        if (curveBuffer == null)
        {
            Debug.Log("NO buffer curve, so creating one");
            curveBuffer = (PrefabUtility.InstantiatePrefab(curvePrefab, raceCircuit.transform) as GameObject).GetComponent<Curve>();
            curveBuffer.creator = this;
            Point closestPoint = null;
            if (raceCircuit.circuitCurves.Count > 0)
            {
                closestPoint = raceCircuit.circuitCurves[0].points[0];
                float minDistance = float.MaxValue;
                Vector3 closestPosition = Vector3.zero;

                foreach (Curve curve in raceCircuit.circuitCurves)
                {
                    for (int i = 0; i < curve.points.Count; i++)
                    {
                        Point point = curve.points[i];
                        //Vector3 tempClosestPosition = ClosestPointOnLine(inputRayWS, point.pointPosition);
                        Vector3 tempClosestPosition = GetPointOnRayWithY(ray, point.pointPosition);
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

                curveBufferHeight = closestPosition.y;
                pointPos = closestPosition;
            }
            else
            {
                curveBufferHeight = 0;
                float t = (curveBufferHeight - ray.origin.y) / ray.direction.y;
                pointPos = ray.origin + t * ray.direction;
            }


            Point newpoint = (PrefabUtility.InstantiatePrefab(pointPrefab, curveBuffer.transform) as GameObject).GetComponent<Point>();
            newpoint.creator = this;
            curveBuffer.AutoSetPreviousAndNextPoints();

            newpoint.pointPosition = pointPos;
            newpoint.rotatorPointPosition = newpoint.transform.position + newpoint.transform.up * newpoint.rotatorDistance;
            if (closestPoint)
            newpoint.transform.rotation = closestPoint.transform.rotation;

            //newpoint.AutoSetAnchorControlPoints();
            if(newpoint.prevPoint)
            newpoint.prevPoint.AutoSetAnchorControlPoints();
            if(newpoint.nextPoint)
            newpoint.nextPoint.AutoSetAnchorControlPoints();

            //curveBuffer.NormalizeCurvePoints();

            newpoint.crossSectionCurve.creator = this;

            newpoint.crossSectionCurve.AutoSetPreviousAndNextPoints();
            newpoint.crossSectionCurve.NormalizeCurvePoints();
            newpoint.PerpendicularizeCrossSection();
            newpoint.crossSectionCurve.AutoSetAllControlPoints();


        }
        else
        {

            if (ray.direction.y == 0)
            {
                if (ray.origin.y != curveBufferHeight)
                {
                    return;
                }
            }

            float t = (curveBufferHeight - ray.origin.y) / ray.direction.y;

            if (t < 0)
            {
                return;
            }

            pointPos = ray.origin + t * ray.direction;


            Point newpoint = (PrefabUtility.InstantiatePrefab(pointPrefab, curveBuffer.transform) as GameObject).GetComponent<Point>();
            newpoint.creator = this;
            curveBuffer.AutoSetPreviousAndNextPoints();

            newpoint.pointPosition = pointPos;
            newpoint.rotatorPointPosition = newpoint.transform.position + newpoint.transform.up * newpoint.rotatorDistance;
            newpoint.transform.rotation = newpoint.prevPoint.transform.rotation;

            
            if (newpoint.prevPoint)
                newpoint.prevPoint.AutoSetAnchorControlPoints();
            if (newpoint.nextPoint)
                newpoint.nextPoint.AutoSetAnchorControlPoints();
            if(newpoint.prevPoint && newpoint.nextPoint)
                newpoint.AutoSetAnchorControlPoints();

            curveBuffer.NormalizeCurvePoints();

            newpoint.crossSectionCurve.creator = this;

            newpoint.crossSectionCurve.AutoSetPreviousAndNextPoints();
            newpoint.crossSectionCurve.NormalizeCurvePoints();
            newpoint.PerpendicularizeCrossSection();
            newpoint.crossSectionCurve.AutoSetAllControlPoints();

        }

        EditorApplication.delayCall += () =>
        {
            Selection.activeGameObject = this.gameObject;
        };

    }

    public void abortCreatingCurve()
    {
        DestroyImmediate(curveBuffer.gameObject);
        curveBuffer = null;
        creatingCurve = false;
    }

    public void finishCreatingCurve()
    {
        raceCircuit.circuitCurves.Add(curveBuffer);
        curveBuffer = null;
        creatingCurve = false;
    }

    public void addCurve(Vector3 firstPointPosition)
    {

    }

    public void buildNewRoadFromSelectedPoints()
    {
        Road newRoad = (PrefabUtility.InstantiatePrefab(roadPrefab, raceCircuit.transform) as GameObject).GetComponent<Road>();
        raceCircuit.roads.Add(newRoad);
        newRoad.creator = this;
        newRoad.associatedPoints.Clear();
        for (int i = 0; i < selectedPoints.Count; i++)
        {
            newRoad.associatedPoints.Add(selectedPoints[i]);
        }
        DeselectAllPoints();
        selectPoints = false;
        initialise();
    }

    public void addPoint(Ray inputRayWS)
    {
        /*position*/
        //Vector3 mousePoint = screen2xzplane(guiEvent);


        Point closestPoint = raceCircuit.circuitCurves[0].points[0];
        Curve closestCurve = raceCircuit.circuitCurves[0];
        int closestIndex = 0;
        float minDistance = float.MaxValue;
        Vector3 closestPosition = Vector3.zero;

        foreach (Curve curve in raceCircuit.circuitCurves)
        {
            for (int i = 0; i < curve.points.Count; i++)
            {
                Point point = curve.points[i];
                //Vector3 tempClosestPosition = ClosestPointOnLine(inputRayWS, point.pointPosition);
                Vector3 tempClosestPosition = GetPointOnRayWithY(inputRayWS, point.pointPosition);
                Vector3 differenceVector = tempClosestPosition - point.pointPosition;
                float calcDistance = (new Vector3(differenceVector.x, differenceVector.y * 10, differenceVector.z)).sqrMagnitude;

                if (calcDistance < minDistance)
                {
                    closestPoint = point;
                    closestIndex = i;
                    closestCurve = curve;
                    minDistance = calcDistance;
                    closestPosition = tempClosestPosition;
                }
            }
        }

        Debug.Log(closestPoint + " " + closestPoint.pointPosition);
        //testSphere.transform.position = closestPosition;


        Point firstPoint = closestPoint;
        Point secondPoint = null;
        if (closestPoint.nextPoint && closestPoint.prevPoint)
        {
            Vector3 AB = closestPoint.pointPosition - closestPosition;
            Vector3 AC = closestPoint.prevPoint.pointPosition - closestPosition;
            Vector3 AD = closestPoint.nextPoint.pointPosition - closestPosition;
            if(Vector3.Angle(AB, AC) > Vector3.Angle(AB,AD))
            {
                firstPoint = closestPoint.prevPoint;
                secondPoint = closestPoint;
            }
            else
            {
                secondPoint = closestPoint.nextPoint;
            }
        }else if(closestPoint.nextPoint == null && closestPoint.prevPoint)
        {
            Vector3 projectedPoint = Vector3.Project(closestPosition, closestPoint.pointPosition - closestPoint.prevPoint.pointPosition);

            if((projectedPoint - closestPoint.prevPoint.pointPosition).sqrMagnitude < (closestPoint.pointPosition - closestPoint.prevPoint.pointPosition).sqrMagnitude)
            {
                firstPoint = closestPoint.prevPoint;
                secondPoint = closestPoint;
            }
        }
        else if (closestPoint.prevPoint == null && closestPoint.nextPoint)
        {
            Vector3 projectedPoint = Vector3.Project(closestPosition, closestPoint.pointPosition - closestPoint.nextPoint.pointPosition);

            if ((projectedPoint - closestPoint.nextPoint.pointPosition).sqrMagnitude < (closestPoint.pointPosition - closestPoint.nextPoint.pointPosition).sqrMagnitude)
            {
                secondPoint = closestPoint.nextPoint;
            }
            else
            {
                firstPoint = null;
                secondPoint = closestPoint;
            }
        }






        Debug.Log("MAIN POINT IS " + firstPoint?.name + " OTHER POINT IS " + secondPoint?.name);

        if(firstPoint && secondPoint)
        {
            addPointBetween(closestPosition, firstPoint, secondPoint);
        }

        else if(!firstPoint ^ !secondPoint)
        {
            addEndPoint(closestPosition, firstPoint ? firstPoint : secondPoint);
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
        if (!raceCircuit)
        {
            raceCircuit = transform.GetComponent<RaceCircuit>();
        }
        if (EDIT)
        {
            SetEditMode();
        }
        else
        {
            Freeze();
        }

    }

    public void initialise()
    {
        Debug.Log("Collecting roads and curves inside");

        raceCircuit.circuitCurves.Clear();
        raceCircuit.roads.Clear();

        curveBuffer = null;

        for(int i = 0; i < transform.childCount; i++)
        {
            Transform t = transform.GetChild(i);
            if (t.GetComponent<Curve>())
            {
                raceCircuit.circuitCurves.Add(t.GetComponent<Curve>());

            }else if (t.GetComponent<Road>())
            {
                raceCircuit.roads.Add(t.GetComponent<Road>());
            }
        }




        Debug.Log("ABOUT TO SET CURVE AUTO POINTS");
        foreach (Curve curve in raceCircuit.circuitCurves)
        {
            curve.AutoSetPreviousAndNextPoints();

            foreach (Point point in curve.points)
            {
                point.crossSectionCurve.AutoSetPreviousAndNextPoints();

            }
        }

        Debug.Log("CURVE AUTO POINTS SET!");

        // NOTE: we'll do this only when the corresponding gizmo moves but that's only after @Bella adds them
        foreach (Curve curve in raceCircuit.circuitCurves)
        {
            foreach (Point point in curve.points)
            {
                // point.PerpendicularizeCrossSection();
                point.creator = this;
                point.UpdateLength();

                foreach (Point crossSectionPoint in point.crossSectionCurve.points)
                {
                    crossSectionPoint.creator = this;
                    crossSectionPoint.UpdateLength();
                    crossSectionPoint.AutoSetAnchorControlPoints();
                }
            }
        }

        Debug.Log("Cross section points length updated and control points set");

        foreach (Curve curve in raceCircuit.circuitCurves)
        {
            curve.NormalizeCurvePoints();
            curve.creator = this;
            foreach (Point point in curve.points)
            {
                point.crossSectionCurve.NormalizeCurvePoints();
            }
        }

        Debug.Log("Curves normalised");

        foreach (Road road in raceCircuit.roads)
        {
            road.creator = this;
            road.initializeRoad();
            road.buildRoad();

        }
        Debug.Log("Roads built");

        roadRebuildLimiter = StartCoroutine(RoadRebuildingLimiter());

        Debug.Log("Road building limiter started, and all initialised");
    }

    // this draws it all the time instead of just on selection
    //void OnRenderObject()
    //{
    //    Draw(raceCircuit.circuitCurve);
    //}
    

    private void OnDestroy()
    {
        //StopCoroutine(roadRebuildLimiter);
        StopAllCoroutines();
        Selection.selectionChanged -= OnSelectionChanged;
    }
    private void OnDisable()
    {
        //StopCoroutine(roadRebuildLimiter);
        StopAllCoroutines();
        Selection.selectionChanged -= OnSelectionChanged;
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


    public void SetEditMode()
    {
        foreach (Curve curve in raceCircuit.circuitCurves)
        {

            curve.gameObject.SetActive(true);
            curve.gameObject.hideFlags = HideFlags.None;
            if (!Application.isPlaying) EditorUtility.SetDirty(curve.gameObject);


        }

        foreach(Road road in raceCircuit.roads)
        {
            road.gameObject.hideFlags = HideFlags.None; //NotEditable on release
            if (!Application.isPlaying) EditorUtility.SetDirty(road.gameObject);

            road.bridge.gameObject.hideFlags = HideFlags.None;
            if (!Application.isPlaying) EditorUtility.SetDirty(road.bridge.gameObject);

            foreach (Railing railing in road.railings)
            {
                railing.gameObject.hideFlags = HideFlags.None;
                if (!Application.isPlaying) EditorUtility.SetDirty(railing.gameObject);
            }
            road.enabled = true;
        }

        Selection.selectionChanged += OnSelectionChanged;
        initialise();

    }

    public void Freeze()
    {
        StopAllCoroutines();

        foreach (Curve curve in raceCircuit.circuitCurves)
        {
            curve.gameObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.NotEditable;
            curve.gameObject.SetActive(false);

            if (!Application.isPlaying) EditorUtility.SetDirty(curve.gameObject);


        }

        foreach (Road road in raceCircuit.roads)
        {
            road.gameObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.NotEditable;
            if (!Application.isPlaying) EditorUtility.SetDirty(road.gameObject);

            road.bridge.gameObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.NotEditable;
            if (!Application.isPlaying) EditorUtility.SetDirty(road.bridge.gameObject);

            foreach (Railing railing in road.railings)
            {
                railing.gameObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.NotEditable;
                if (!Application.isPlaying) EditorUtility.SetDirty(railing.gameObject);
            }

            road.enabled = false;
        }

        Selection.selectionChanged -= OnSelectionChanged;

        
    }



    #endregion

}

