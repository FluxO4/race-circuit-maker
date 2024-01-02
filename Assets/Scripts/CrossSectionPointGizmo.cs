using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


[ExecuteAlways]
public class CrossSectionPointGizmo : MonoBehaviour
{


    public Point correspondingPoint;
    bool hasChanged = false;
    Vector3 prevPos = Vector3.zero;

    // Update is called once per frame
    void Update()
    {
        if (!correspondingPoint) correspondingPoint = GetComponent<Point>();
        if (transform.hasChanged)
        {

            correspondingPoint.moveToTransform();
            if (!correspondingPoint.creator.updateOnlyOnRelease)
            {
                Vector3 difference = transform.position - prevPos;

                if (difference.sqrMagnitude > 0.01f)
                {

                    correspondingPoint.UpdateLength();
                    correspondingPoint.AutoSetAnchorControlPoints();

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
                Vector3 difference = transform.position - prevPos;

                if (difference.sqrMagnitude > 0.01f)
                {
                    if (correspondingPoint.creator.updateOnlyOnRelease)
                    {
                        correspondingPoint.UpdateLength();
                        correspondingPoint.AutoSetAnchorControlPoints();

                        correspondingPoint.creator.pointTransformChanged = true;
                    }
                    prevPos = transform.position;
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
