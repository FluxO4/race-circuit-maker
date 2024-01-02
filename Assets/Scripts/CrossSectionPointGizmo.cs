using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


[ExecuteAlways]
public class CrossSectionPointGizmo : MonoBehaviour
{


    public Point correspondingPoint;
    bool hasChanged = false;

    // Update is called once per frame
    void Update()
    {
        if (!correspondingPoint) correspondingPoint = GetComponent<Point>();
        if (transform.hasChanged)
        {

            correspondingPoint.moveToTransform();

            if (!correspondingPoint.creator.updateOnlyOnRelease)
            {
                correspondingPoint.UpdateLength();
                correspondingPoint.AutoSetAnchorControlPoints();

                correspondingPoint.creator.pointTransformChanged = true;
            }


            transform.hasChanged = false;
            hasChanged = true;
        }
        else
        {
            if (hasChanged)
            {
                if (correspondingPoint.creator.updateOnlyOnRelease)
                {
                    correspondingPoint.UpdateLength();
                    correspondingPoint.AutoSetAnchorControlPoints();

                    correspondingPoint.creator.pointTransformChanged = true;
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
