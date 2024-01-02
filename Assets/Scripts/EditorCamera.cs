using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


[ExecuteAlways]
public class EditorCamera : MonoBehaviour
{
    public float fixedYValue = 0f; // Set your fixed Y value here

    void Update()
    {
        if (!Application.isEditor) // This script is intended to run in the Unity Editor
            return;

        Vector3 mousePosition = Event.current.mousePosition;
        
        mousePosition.y = Camera.current.pixelHeight - mousePosition.y; // Convert Y to origin at bottom left
        mousePosition.z = Camera.current.nearClipPlane; // Set the z position to the camera's near clip plane

        Vector3 worldPosition = Camera.current.ScreenToWorldPoint(mousePosition);

        // Adjust the z position so that the y position matches the fixed Y value
        float distance = (fixedYValue - Camera.current.transform.position.y) / Camera.current.transform.forward.y;
        worldPosition = Camera.current.transform.position + Camera.current.transform.forward * distance;

        Debug.Log("World Position: " + worldPosition);
    }
}
