using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


public class RollerBallFollowerCamera: MonoBehaviour
{

    public Transform target; // The target object for the camera to follow
    public Rigidbody targetRigidBody;
    public float smoothSpeed = 0.125f; // The speed of the camera's movement
    public Vector3 offset = new Vector3(0, 5, -5); // The offset of the camera from the target


    public float speed = 5f; // The speed of the sphere
    public float rotationSpeed = 200f; // The rotation speed of the sphere
    void FixedUpdate()
    {
       

        float moveVertical = Input.GetAxis("Vertical");

        float rotateHorizontal = Input.GetAxis("Horizontal");

        Vector3 force = (Vector3.ProjectOnPlane(transform.forward, Vector3.up)).normalized * moveVertical * speed * Time.fixedDeltaTime;
        targetRigidBody.AddForce(force * 50f);

        float turnRotation = rotateHorizontal * rotationSpeed * Time.fixedDeltaTime;

        offset = RotateVectorAboutY(offset, turnRotation);
        offset.Set(offset.x, Mathf.Max((5 - force.magnitude * 10f), 1f), offset.z);

        Vector3 desiredPosition = target.position + offset;

        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;

        

        transform.LookAt(Vector3.Lerp(transform.position + transform.forward, target.position, smoothSpeed));

    }

    public static Vector3 RotateVectorAboutY(Vector3 vector, float angle)
    {
        Quaternion rotation = Quaternion.Euler(0, angle, 0);
        return rotation * vector;
    }
}


