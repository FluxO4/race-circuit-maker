using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RollerBall : MonoBehaviour
{
    Vector3 startPosition;

    private Rigidbody rb;

    void Start()

    {
        startPosition = transform.position;
        rb = GetComponent<Rigidbody>();
        rb.drag = 0.5f; // Air resistance
        rb.angularDrag = 0.05f; // Prevents the sphere from rolling
    }

    void FixedUpdate()
    {
        if(transform.position.y < -20)
        {
            transform.position = startPosition;
        }
    }
}
