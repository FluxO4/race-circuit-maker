using UnityEngine;
using UnityEditor;
using System.Linq;

[ExecuteAlways]
public class CircuitPointGizmo : MonoBehaviour
{

    public Point correspondingPoint;
    Vector3 prevPos = Vector3.zero;
    bool hasChanged = false;

    // Start is called before the first frame update
    void Start()
    {
        prevPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (transform.hasChanged)
        {
            correspondingPoint.moveToTransform();



            if (!correspondingPoint.creator.updateOnlyOnRelease)
            {

                Vector3 difference = transform.position - prevPos;

                if (difference.sqrMagnitude > 0.01f)
                {
                    correspondingPoint.UpdateLength();


                    if (correspondingPoint.creator.autoSetControlPoints)
                    {
                        foreach (Point point in correspondingPoint.crossSectionCurve.points)
                        {
                            point.AutoSetAnchorControlPoints();
                        }
                    }

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
                if (correspondingPoint.creator.updateOnlyOnRelease)
                {
                    Vector3 difference = transform.position - prevPos;

                    if (difference.sqrMagnitude > 0.01f)
                    {

                        correspondingPoint.UpdateLength();



                        if (correspondingPoint.creator.autoSetControlPoints)
                        {
                            foreach (Point point in correspondingPoint.crossSectionCurve.points)
                            {
                                point.AutoSetAnchorControlPoints();
                            }
                        }

                        correspondingPoint.creator.pointTransformChanged = true;

                        prevPos = transform.position;
                    }
                }


                {
                    Vector3 difference = transform.position - prevPos;

                    if (difference.sqrMagnitude > 0.01f)
                    {
                        correspondingPoint.PerpendicularizeCrossSection();
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
