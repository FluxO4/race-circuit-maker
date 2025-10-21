using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


[ExecuteAlways]
public class CrossSectionPointGizmo : MonoBehaviour
{

    public Point parentPoint;

    public Point correspondingPoint;
    bool hasChanged = false;
    public bool isEndPoint = false;
    Vector3 prevPos = Vector3.zero;

    // Update is called once per frame
    void Update()
    {
        if (!correspondingPoint.creator) return;

        if (!correspondingPoint) correspondingPoint = GetComponent<Point>();

        if (transform.hasChanged)
        {


            //correspondingPoint.AutoSetAnchorControlPoints();
            if(correspondingPoint.parentCurve != null)
            correspondingPoint.parentCurve.AutoSetAllControlPoints();

            if (!correspondingPoint.creator.updateOnlyOnRelease)
            {
                Vector3 difference = transform.position - prevPos;

                if (difference.sqrMagnitude > 0.0001f)
                {
                    correspondingPoint.creator.pointTransformChanged = true;
                }
                prevPos = transform.position;
            }

            transform.hasChanged = false;
            hasChanged = true;
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
            DestroyImmediate(this);
        }
}
}
