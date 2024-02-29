using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public GameObject camera;
    public float moveSpeed = 5f;

    void Update()
    {
        // use camera's XZ rotation
        Vector3 forward = camera.transform.forward;
        forward.y = 0;
        forward.Normalize();

        Vector3 right = camera.transform.right;
        right.y = 0;
        right.Normalize();

        // move with keys
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        Vector3 movement = forward * verticalInput + right * horizontalInput;
        movement *= moveSpeed * Time.deltaTime;

        transform.Translate(movement, Space.Self);
    }
}