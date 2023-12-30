using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class CircuitPointGizmo : MonoBehaviour
{

    public Point correspondingPoint;
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
        }
    }
}
