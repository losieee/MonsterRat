using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMov : MonoBehaviour
{
    static PlayerMov instance;

    [Header("Move")]
    public float speed = 5f;
    public float gravity = -9.81f;

    [Header("Jump")]
    public float jumpForce = 1.5f;

    [Header("Look")]
    public Transform cam;
    public float sensitiv = 2f;
    public float maxAngle = 85f;

    CharacterController control;
    Vector3 vel;
    float pitch;

    PlayerUIState ui;

    void Awake()
    {
        instance = this;

        control = GetComponent<CharacterController>();
        ui = GetComponent<PlayerUIState>();

        if (cam == null)
        {
            Camera c = GetComponentInChildren<Camera>();
            if (c != null) cam = c.transform;
        }
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (ui != null && ui.IsUIOpen) return;
        Move();
        Look();
    }

    void Move()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = (transform.right * x + transform.forward * z) * speed;
        control.Move(move * Time.deltaTime);

        if (control.isGrounded && vel.y < 0f)
            vel.y = -2f;

        if(control.isGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            vel.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        }

        vel.y += gravity * Time.deltaTime;
        control.Move(vel * Time.deltaTime);
    }

    void Look()
    {
        if (cam == null) return;

        float mouseX = Input.GetAxis("Mouse X") * sensitiv;
        float mouseY = Input.GetAxis("Mouse Y") * sensitiv;

        transform.Rotate(Vector3.up * mouseX);

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -maxAngle, maxAngle);
        cam.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}
