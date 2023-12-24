using UnityEngine;

public class MainCamera : MonoBehaviour
{
    public float movementSpeed = 10.0f;
    public float mouseSensitivity = 100.0f;
    public float clampAngle = 80.0f;

    private float verticalRotation = 0.0f;
    private float horizontalRotation = 0.0f;

    void Start()
    {
        Vector3 rot = transform.localRotation.eulerAngles;
        horizontalRotation = rot.y;
        verticalRotation = rot.x;
    }

    void Update()
    {
        // Mouse movement for rotation
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = -Input.GetAxis("Mouse Y");

        horizontalRotation += mouseX * mouseSensitivity * Time.deltaTime;
        verticalRotation += mouseY * mouseSensitivity * Time.deltaTime;
        verticalRotation = Mathf.Clamp(verticalRotation, -clampAngle, clampAngle);

        Quaternion localRotation = Quaternion.Euler(verticalRotation, horizontalRotation, 0.0f);
        transform.rotation = localRotation;

        // Keyboard commands for movement
        float forwardBackward = Input.GetAxis("Vertical") * movementSpeed * Time.deltaTime;
        float leftRight = Input.GetAxis("Horizontal") * movementSpeed * Time.deltaTime;

        Vector3 forward = transform.forward * forwardBackward;
        Vector3 right = transform.right * leftRight;

        transform.position += forward + right;
    }
}
