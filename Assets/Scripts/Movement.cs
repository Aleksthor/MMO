using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Movement : MonoBehaviour
{
    // References
    private Camera cam;
    private Transform viewPoint;
    private Rigidbody rigidbody;
    private Transform cube;

    //Logic
    private Vector3 cameraForward;
    private Vector3 mousePosLastFrame;
    public float cameraDistance = 10f;
    public float cameraRotationSpeed = 0.66f;


    public float movementSpeed;
    private bool moving = false;


    private void Awake()
    {
        // Init all variables we need
        cam = Camera.main;
        cube = transform.Find("Cube");
        viewPoint = transform.Find("ViewPoint");
        rigidbody = GetComponent<Rigidbody>();
    }
    // Start is called before the first frame update
    void Start()
    {
        // Set Camera start Position
        cameraForward = transform.forward;
        cam.transform.position = viewPoint.position - (cameraForward * cameraDistance);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        moving = false;

        // If holding Right Click, Rotate Camera
        if (Input.GetMouseButton(1))
        {
            Vector3 mouseChange = mousePosLastFrame - Input.mousePosition;
            mouseChange *= cameraRotationSpeed;

            cameraForward = Quaternion.AngleAxis(-mouseChange.x, Vector3.up) * cameraForward;
            if ((Quaternion.AngleAxis(mouseChange.y, cam.transform.right) * cameraForward).y < 0.85f && (Quaternion.AngleAxis(mouseChange.y, cam.transform.right) * cameraForward).y > -0.85f)
            {
                cameraForward = Quaternion.AngleAxis(mouseChange.y, cam.transform.right) * cameraForward;
            }
        
            mousePosLastFrame = Input.mousePosition;
            cam.transform.position = viewPoint.position - (cameraForward.normalized * cameraDistance);
        }
        // Zoom camera
        if (Input.mouseScrollDelta.y != 0f)
        {
            cameraDistance = Mathf.Clamp(cameraDistance - Input.mouseScrollDelta.y, 5, 20);
            cam.transform.position = viewPoint.position - (cameraForward.normalized * cameraDistance);
        }

        // Movement - velocity based
        Vector3 forward = cameraForward;
        forward.y = 0f;
        forward = forward.normalized;
        forward *= Input.GetKey(KeyCode.W) ? 1 : Input.GetKey(KeyCode.S) ? -1 : 0;

        Vector3 right = cam.transform.right;
        right.y = 0f;
        right = right.normalized;
        right *= Input.GetKey(KeyCode.D) ? 1 : Input.GetKey(KeyCode.A) ? -1 : 0;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.D))
        {
            moving = true;
        }

        // Rotate the player mesh before we set y velocity
        Vector3 direction = (forward + right).normalized;
        if (direction.magnitude > 0.1f)
        {
            cube.transform.forward = direction;
        }
        direction.y = rigidbody.velocity.y;
        rigidbody.velocity = direction * movementSpeed;

        // Look at player
        cam.transform.position = viewPoint.position - (cameraForward.normalized * cameraDistance);
        cam.transform.LookAt(viewPoint);
        mousePosLastFrame = Input.mousePosition;
    }

    public bool Moving()
    {
        return moving;
    }
    public void RotateMesh(Vector3 forward)
    {
        cube.transform.forward = forward;
    }

    public void StopMovement(float time)
    {
        // Implement Later
    }
}
