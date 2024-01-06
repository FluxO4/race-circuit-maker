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
            DestroyImmediate(this);
        }
    }
}
