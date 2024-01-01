using UnityEngine;
using UnityEditor;
using System.Linq;

[ExecuteAlways]
public class CircuitPointGizmo : MonoBehaviour
{

    public Point correspondingPoint;

    bool hasChanged = false;

    public RaceCircuitCreator creator;
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
                //correspondingPoint.AutoSetAnchorControlPoints();
                //// correspondingPoint.UpdateLengths();
                //foreach (Point crossSectionPoint in correspondingPoint.crossSectionCurve.points)
                //{
                //    // crossSectionPoint.UpdateLengths();
                //    crossSectionPoint.AutoSetAnchorControlPoints();
                //}
                
                //correspondingPoint.crossSectionCurve.points.First().AutoSetStart();
                //correspondingPoint.crossSectionCurve.points.Last().AutoSetEnd();


                // creator.raceCircuit.circuitCurve.ComputeNormalizedPoints();
            }



            hasChanged = false;
        }

        if (EditorApplication.isPlaying)
        {
            DestroyImmediate(this);
        }
    }
}
