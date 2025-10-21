using UnityEngine;
using UnityEditor;
using System.Linq;

[ExecuteAlways]
public class CircuitPointGizmo : MonoBehaviour
{

    
    public Point correspondingPoint;
    Vector3 prevPos = Vector3.zero;
    bool hasChanged = false;
    bool stillInEditor = true;

    // Start is called before the first frame update
    void Start()
    {
        prevPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (!correspondingPoint.creator) return;
        if (transform.hasChanged)
        {

            


            if (!correspondingPoint.creator.updateOnlyOnRelease)
            {

                Vector3 difference = transform.position - prevPos;

                if (difference.sqrMagnitude > 0.0001f)
                {
                    correspondingPoint.creator.pointTransformChanged = true;
                    prevPos = transform.position;
                }
            }

            


            hasChanged = true;
            transform.hasChanged = false;
        }
        else
        {
            if (hasChanged)
            {
                correspondingPoint.UpdateLength();

                if (correspondingPoint.creator.updateOnlyOnRelease)
                {
                    Vector3 difference = transform.position - prevPos;

                    if (difference.sqrMagnitude > 0.0001f)
                    {
                        correspondingPoint.creator.pointTransformChanged = true;
                        prevPos = transform.position;
                    }
                }
            }

            hasChanged = false;
        }

        if (EditorApplication.isPlaying)
        {
            stillInEditor = false;
            DestroyImmediate(this);
        }
    }

    private void OnDestroy()
    {
        if (!EditorApplication.isPlayingOrWillChangePlaymode && stillInEditor && correspondingPoint.active && !correspondingPoint.creator.creatingCurve)
        {
            Debug.Log("STILL IN EDITOR, SO REMOVING ALL REFERENCES TO THIS POINT SOMEHOW");
            if (correspondingPoint.prevPoint)
            {
                correspondingPoint.prevPoint.nextPoint = correspondingPoint.nextPoint;
                
                correspondingPoint.prevPoint.AutoSetAnchorControlPoints();
                
            }
            if (correspondingPoint.nextPoint)
            {
                correspondingPoint.nextPoint.prevPoint = correspondingPoint.prevPoint;
                correspondingPoint.nextPoint.AutoSetAnchorControlPoints();
            }

            if (correspondingPoint.prevPoint)
            {

                correspondingPoint.prevPoint.UpdateLength();

            }
            if (correspondingPoint.nextPoint)
            {
                correspondingPoint.nextPoint.UpdateLength();
            }

            if (correspondingPoint.parentCurve.points.Count > correspondingPoint.pointIndex)
            {
                correspondingPoint.parentCurve.points.RemoveAt(correspondingPoint.pointIndex);
                correspondingPoint.parentCurve.NormalizeCurvePoints();
            }

           foreach(Road road in correspondingPoint.includedInRoads)
            {
                road.associatedPoints.Remove(correspondingPoint);
            }

            Debug.Log("REFERENCES IN CURVE AND ROAD REMOVED");
            correspondingPoint.creator.pointTransformChanged = true;
            //correspondingPoint.creator.initialise();
        }
    }
}
