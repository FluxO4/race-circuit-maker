using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.ComponentModel;
using Unity.VisualScripting;
public class Point : MonoBehaviour
{
    [Range(2, 10)]
    public int crossSectionPointCount = 5;

    

    [Range(1, 10)]
    public float rotatorDistance = 5;


    public List<Road> includedInRoads = new List<Road>();

    public int pointIndex;

    public bool GizmoVisibility; //If true, a gizmo is shown that can be selected

    bool _selected = false;
    public bool Selected
    {
        get { return _selected; }
        set
        {
            _selected = value;
        }
    } //If true, the gizmo is highlighted, and can be moved

    [SerializeField]
    Transform controlPointBackward;
    [SerializeField]
    Transform controlPointForward;
    [SerializeField]
    Transform rotatorPoint;



    public Vector3 pointPosition
    {
        get
        {
            return transform.position;
        }

        set
        {
            transform.position = value;
        }

    }

    public Vector3 controlPointPositionBackward
    {
        get
        {
            return controlPointBackward.position;
        }

        set
        {
            transform.rotation = Quaternion.FromToRotation(transform.forward, controlPointPositionForward - value) * transform.rotation;
            controlPointBackward.position = value;
        }
    }
    public Vector3 controlPointPositionForward
    {
        get { return controlPointForward.position; }
        set {
            transform.rotation = Quaternion.FromToRotation(transform.forward, value - controlPointPositionBackward) * transform.rotation;
            controlPointForward.position = value; 
        }
    }

    public Vector3 rotatorPointPosition
    {
        get { return rotatorPoint.position; }
        set
        {
            Vector3 newRotatorPosition = Vector3.ProjectOnPlane(value - transform.position, transform.forward) + transform.position;

            transform.rotation = Quaternion.FromToRotation(transform.up, value - transform.position) * transform.rotation;

            rotatorPoint.position = transform.position + transform.up * rotatorDistance;
        }
    }


    [ReadOnly(true)]
    public Point nextPoint;
    [ReadOnly(true)]
    public Point prevPoint;

    public Curve parentCurve;
    public RaceCircuitCreator creator;

    [ReadOnly(true)]
    public float nextSegmentLength;
    [ReadOnly(true)]
    public float prevSegmentLength;

    float circuitPosition; // % along the circuit's length. Initially, this will just be distance along curve, but ideally this needs to be normalised average time taken to reach here from the starting point

    public float normalizedPositionAlongCurve;

    public bool active = true;

    public Curve crossSectionCurve;

    public CircuitPointGizmo myGizmo;

   

    public void ChangeCrossSectionPointCount(int newPointCount)
    {

        //Change tactics here, instead of removing everything and starting over, just add what is needed and reorganise
        if (newPointCount < 2 || newPointCount > 10) return; // Adding hard limits because this is a sensitive thing

        if (newPointCount == crossSectionCurve.points.Count)
        {
            return;
        }
        else
        {
            //Normalising the points again, just in case
            crossSectionCurve.NormalizeCurvePoints();

            //Collecting the new positions for the new points; using a jump value and lerping along the curve

            List<Vector3> newPointPositions = new List<Vector3>();
            newPointPositions.Add(crossSectionCurve.points[0].pointPosition);

            if(newPointCount > 2)
            {
                //add intermediate poits
                float jumpLength = 1f / (newPointCount-1);
                
                for(int i = 1; i <= newPointCount - 2; i++)
                {
                    newPointPositions.Add(crossSectionCurve.LerpAlongCurve(jumpLength * i));
                    Debug.Log("Added a curve point at lerp position: " + jumpLength * i);
                }

            }

            newPointPositions.Add(crossSectionCurve.points[crossSectionCurve.points.Count - 1].pointPosition);

            //Instantiating testspheres for debug purposes
            /*
            for (int i = 0; i < newPointPositions.Count; i++) { 
                Instantiate(creator.testSphere, newPointPositions[i], Quaternion.identity);
            }*/

            //If there are more points needed, we instantiate and add new points to the curve
            if(newPointCount > crossSectionCurve.points.Count)
            {
                while(newPointCount > crossSectionCurve.points.Count)
                {
                    crossSectionCurve.points.Add((PrefabUtility.InstantiatePrefab(creator.crossSectionPointPrefab, crossSectionCurve.transform) as GameObject).GetComponent<Point>());
                }
            }

            //If there are less needed, we delete some of the points
            else
            {
                while(crossSectionCurve.points.Count > newPointCount)
                {
                    Point toDestroy = crossSectionCurve.points[^1];
                    crossSectionCurve.points.RemoveAt(crossSectionCurve.points.Count - 1);
                    DestroyImmediate(toDestroy.gameObject);
                }
            }

            //Setting the new positions to all the points
            for (int i = 0; i < crossSectionCurve.points.Count; i++)
            {
                crossSectionCurve.points[i].pointPosition = newPointPositions[i];
                crossSectionCurve.points[i].parentCurve = crossSectionCurve;
                crossSectionCurve.points[i].creator = creator;
                
            }
            crossSectionCurve.AutoSetPreviousAndNextPoints();

            //Autosettitng all control points
            crossSectionCurve.AutoSetAllControlPoints();



            PerpendicularizeCrossSection();

            crossSectionCurve.NormalizeCurvePoints();
            creator.pointTransformChanged = true;


        }
    }


    public void moveToPosition(Vector3 position)
    {
        pointPosition = position;
        UpdateLength();
    }

    public void UpdateLength()
    {
        if (nextPoint)
            nextSegmentLength = EstimateCurveLength(pointPosition, controlPointPositionForward, nextPoint.controlPointPositionBackward, nextPoint.pointPosition);
        else
            nextSegmentLength = 0f;

        if (prevPoint)
            prevSegmentLength = EstimateCurveLength(prevPoint.pointPosition, prevPoint.controlPointPositionForward, controlPointPositionBackward, pointPosition);
        else
            prevSegmentLength = 0f;
    }

    void AutoSetAnchorHelper()
    {
        Vector3 anchorPos = pointPosition;
        Vector3 dir = Vector3.zero;
        float[] neighbourDistances = new float[2];

        {
            Vector3 offset = prevPoint.pointPosition - anchorPos;
            dir += offset.normalized;
            neighbourDistances[0] = offset.magnitude;
        }

        {
            Vector3 offset = nextPoint.pointPosition - anchorPos;
            dir -= offset.normalized;
            neighbourDistances[1] = -offset.magnitude;
        }

        dir.Normalize();

        controlPointPositionBackward = anchorPos + dir * neighbourDistances[0] * .5f;
        controlPointPositionForward = anchorPos + dir * neighbourDistances[1] * .5f;
    }

    void AutoSetStart()
    {
        if(nextPoint)
        controlPointPositionForward = (pointPosition + nextPoint.controlPointPositionBackward) * .5f;
    }

    void AutoSetEnd()
    {
        if(prevPoint)
        controlPointPositionBackward = (prevPoint.controlPointPositionForward + pointPosition) * .5f;
    }

    public void AutoSetAnchorControlPoints()
    {
        if (prevPoint && nextPoint)
        {
            AutoSetAnchorHelper();
        }
        else if (!prevPoint)
        {
            AutoSetStart();
        }
        else if (!nextPoint)
        {
            AutoSetEnd();
        }
    }

    // NOTE: is there a better way to do this?
    public static float EstimateCurveLength(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, int subdivisions = 10)
    {
        float length = 0.0f;
        Vector3 previousPoint = p0;

        for (int i = 1; i <= subdivisions; i++)
        {
            float t = (float)i / subdivisions;
            Vector3 currentPoint = CalculateBezierPoint(p0, p1, p2, p3, t);
            length += Vector3.Distance(previousPoint, currentPoint);
            previousPoint = currentPoint;
        }

        return length;
    }

    public static Vector3 EvaluateQuadratic(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        Vector3 p0 = Vector3.Lerp(a, b, t);
        Vector3 p1 = Vector3.Lerp(b, c, t);
        return Vector3.Lerp(p0, p1, t);
    }

    public static Vector3 CalculateBezierPoint(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
    {
        Vector3 p0 = EvaluateQuadratic(a, b, c, t);
        Vector3 p1 = EvaluateQuadratic(b, c, d, t);
        return Vector3.Lerp(p0, p1, t);
    }

    public Vector3 GetPointFromi(float i)
    {
        // [OPTIMIZE]

        // NOTE we're assuming that i is across meaning there can be no branches when computing i
        // meaning we can only have at max one forward point
        // meaning this only works for CrossSection curves

        // linearly searching for now
        i = Mathf.Max(0, Mathf.Min(i, crossSectionCurve.points.Last().normalizedPositionAlongCurve));

        for (int index = 0; index < crossSectionCurve.points.Count; index++)
        {
            if (Mathf.Abs(crossSectionCurve.points[index].normalizedPositionAlongCurve - i) < 0.01f)
            {
                // we're exactly on the thing
                return crossSectionCurve.points[index].pointPosition;
            }
            else if ((index < crossSectionCurve.points.Count - 1) && crossSectionCurve.points[index].normalizedPositionAlongCurve < i && crossSectionCurve.points[index + 1].normalizedPositionAlongCurve > i)
            {
                // remap the range i.e. ilerp [p[i].normalized, p[i + 1].normalized] -> 0, 1

                // lerp: x = a + (b-a) * t
                // ilerp x - a /  b - a

                float a = crossSectionCurve.points[index].normalizedPositionAlongCurve;
                float b = crossSectionCurve.points[index + 1].normalizedPositionAlongCurve;
                float t = (i - a) / (b - a);
                return Point.CalculateBezierPoint(crossSectionCurve.points[index].pointPosition,
                                                    crossSectionCurve.points[index].controlPointPositionForward,
                                                    crossSectionCurve.points[index + 1].controlPointPositionBackward,
                                                    crossSectionCurve.points[index + 1].pointPosition,
                                                    t);

            }
        }

        Debug.Log("OUT OF RANGE SOMEHOW! FIX THIS!");
       // return Vector3.zero;
        return crossSectionCurve.points.Last().pointPosition;
    }

    // a and b refer to the anchor points in the big loop which contains the curves we're getting the point from
    public Vector3 GetPointFromij(float i, float j)
    {
        if(nextPoint == null)
        {
            Debug.Log("Trying to get ij for i = " + i + " and j = " + j);
            return GetPointFromi(i);
        }

        Vector3 iaPos = GetPointFromi(i);
        Vector3 ibPos = nextPoint.GetPointFromi(i);

        // TODO: rethink this
        // scaling the control points 
        float curveLength = nextSegmentLength;

        float curveLengthFromStraightLineDistanceEstimatorMultiplier = curveLength / (pointPosition - nextPoint.pointPosition).magnitude;
        float dist = (iaPos - ibPos).magnitude;
        float estimatedCurvedLength = dist * curveLengthFromStraightLineDistanceEstimatorMultiplier;
        float scale = estimatedCurvedLength / curveLength;

        Vector3 controlForward = ((controlPointPositionForward - pointPosition) * scale) + iaPos;
        Vector3 controlBackward = ((nextPoint.controlPointPositionBackward - nextPoint.pointPosition) * scale) + ibPos;


        return Point.CalculateBezierPoint(iaPos, controlForward, controlBackward, ibPos, j);
    }



    public Vector3 GetUp()
    {
        // [OPTIMIZE]
        // TODO cache edge vector and ac
        Vector3 ac = controlPointPositionForward - controlPointPositionBackward;
        return Vector3.Cross(ac, GetEV()).normalized;
    }

    public Vector3 GetEV()
    {
        // [OPTIMIZE]
        // TODO cache edge vector
        return (crossSectionCurve.points.Last().pointPosition - crossSectionCurve.points.First().pointPosition).normalized;
    }

    public Vector3 GetAC()
    {
        return (controlPointPositionForward - controlPointPositionBackward);
    }

    // NOTE: function call is only valid if we contain a cross section


    //If the position of control points have changed, this should be called to appropriately rotate the point object

    //If the position of rotator point has changed, this should be called to appropriate rotate the point object

    public void transformToAlignEndPoints()
    {
        Vector3 edgeMidpoint = (crossSectionCurve.points.Last().pointPosition + crossSectionCurve.points.First().pointPosition) * 0.5f;
        Vector3 shift = pointPosition - edgeMidpoint;

        foreach (Point point in crossSectionCurve.points)
        {
            Vector3 newPos = point.pointPosition + shift;
            point.pointPosition = newPos;
        }

        pointPosition -= shift;
    }

    public void ProjectCrossSection()
    {
        if (crossSectionCurve == null)
        {
            return;
        }

        foreach (Point point in crossSectionCurve.points)
        {
            Vector3 newPos = Vector3.ProjectOnPlane(point.pointPosition - pointPosition, transform.forward) + pointPosition;
            // TODO do this for the controlPoints as well
            point.moveToPosition(newPos);
        }
    }

    public void ProjectSelf(Vector3 origin, Vector3 normal)
    {
        Vector3 newPos = Vector3.ProjectOnPlane(pointPosition - origin, normal) + origin;
        moveToPosition(newPos);
    }

    public void PerpendicularizeCrossSection(bool autoset = false)
    {
        if(crossSectionCurve == null)
        {
            return;
        }

        //Overhauled by cyborg-chan to add further constraints, which simplify calculations
        //Constraints are: anchorPoint is always in the middle of the two end points, and two end points are along the local x axis of the anchor
        //All cross section points are in the plane represented by anchor's forward as normal

        //First, moving all points to satisfy the midpoint constraint
        Vector3 edgeMidpoint = (crossSectionCurve.points.Last().pointPosition + crossSectionCurve.points.First().pointPosition) *0.5f;
        Vector3 shift = pointPosition - edgeMidpoint;

        foreach (Point point in crossSectionCurve.points)
        {
            Vector3 newPos = point.pointPosition + shift;
            point.pointPosition = newPos;
        }

        //Then we rotate all cross section points so they are in the xy plane of our anchor
        foreach (Point point in crossSectionCurve.points)
        {
            Quaternion firstRotation = Quaternion.FromToRotation((point.pointPosition - pointPosition).normalized, Vector3.ProjectOnPlane(point.pointPosition - pointPosition, transform.forward).normalized);

            Vector3 newPos = firstRotation * (point.pointPosition - pointPosition) + pointPosition;
            point.pointPosition = newPos;
        }

        Vector3 edgeVector = crossSectionCurve.points.Last().pointPosition - crossSectionCurve.points.First().pointPosition;
        Quaternion secondRotation = Quaternion.FromToRotation(edgeVector.normalized, transform.right);


        //Then we rotate all cross section points so that the end points reach the x axis
        foreach (Point point in crossSectionCurve.points)
        {
            Vector3 newPos = secondRotation * (point.pointPosition - pointPosition) + pointPosition;
            point.moveToPosition(newPos);
            point.AutoSetAnchorControlPoints();
        }


        //Once these constraints have been set, any user-driven update on the points should keep them true, hence this function does not need to be called after point updates. So it will only be activated by a button press in the inspector and nowhere else

    }

}
