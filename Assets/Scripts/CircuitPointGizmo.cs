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
            correspondingPoint.transform.position = transform.position;
            correspondingPoint.moveToTransform();

            foreach (Point crossSectionPoint in correspondingPoint.crossSectionCurve.points)
            {
                crossSectionPoint.moveToTransform();
            }

            hasChanged = true;
            transform.hasChanged = false;
        }
        else
        {
            if (hasChanged)
            {
                Debug.Log("Released");

                correspondingPoint.PerpendicularizeCrossSection();
            }



            hasChanged = false;
        }

        if (EditorApplication.isPlaying)
        {
            DestroyImmediate(this);
        }
    }
}
