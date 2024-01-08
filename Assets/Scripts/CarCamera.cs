using UnityEngine;

public class CarCamera : MonoBehaviour
{
    public Transform car; // The car that the camera will follow
    public Vector3 offset = new Vector3(0, 5, -10); // Offset from the car

    void LateUpdate()
    {
        // Update the position of the camera to follow the car, with an offset
        transform.position = car.position + offset;

        // Optional: Make the camera look at the car
        transform.LookAt(car);
    }
}
