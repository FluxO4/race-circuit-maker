using UnityEngine;
using UnityEditor;
using System.Linq;

[ExecuteAlways]
public class CircuitPointGizmo : MonoBehaviour
{

    public Point correspondingPoint;
    

    bool hasChanged = false;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (transform.hasChanged)
        {
            correspondingPoint.moveToTransform();

            if (!correspondingPoint.creator.updateOnlyOnRelease)
            {
                correspondingPoint.UpdateLength();

                correspondingPoint.PerpendicularizeCrossSection();

                if (correspondingPoint.creator.autoSetControlPoints)
                {
                    foreach (Point point in correspondingPoint.crossSectionCurve.points)
                    {
                        point.AutoSetAnchorControlPoints();
                    }
                }

                correspondingPoint.creator.pointTransformChanged = true;
            }

            


            hasChanged = true;
            transform.hasChanged = false;
        }
        else
        {
            if (hasChanged)
            {
                if (correspondingPoint.creator.updateOnlyOnRelease)
                {
                    correspondingPoint.UpdateLength();

                    correspondingPoint.PerpendicularizeCrossSection();

                    if (correspondingPoint.creator.autoSetControlPoints)
                    {
                        foreach (Point point in correspondingPoint.crossSectionCurve.points)
                        {
                            point.AutoSetAnchorControlPoints();
                        }
                    }

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
