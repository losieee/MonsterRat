using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Unity.VisualScripting;
using System.Linq;
using UnityEngine.Jobs;


// 이 스크립트는 멀티용 스크립트입니다
[RequireComponent(typeof(CharacterController))]
public class PhotonPlayerMove : NetworkBehaviour 
{

    [Header("움직임 관련")]
    public float speed = 5f;
    public float gravity = -9.81f;
    public float jumpForce = 5f;
    [Header("시야")]
    public Transform cam;
    public float sensitiv = 2f;
    public float maxAngle = 85;

    CharacterController controller;
    Vector3 vel;
    float pitch;


    public struct NetworkInputData : INetworkInput
    {
        public Vector2 direction; 
        public NetworkButtons buttons; 
    }
    public enum NetworkInputButtons
    {
        Jump = 0,
        Fire = 1,
    }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (cam == null)
        {
            Camera c = GetComponentInChildren<Camera>();
            if (c != null) cam = c.transform;
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override void Spawned()
    {
        if(HasInputAuthority)
        {
            Cursor.visible = false; 
            Cursor.lockState = CursorLockMode.Locked;

            if (cam != null) cam.gameObject.SetActive(true);
        }
        else
        {
            if (cam != null) cam.gameObject.SetActive(false);
            var listener = GetComponentInChildren<AudioListener>();
            if (listener) listener.enabled = false;
        }
    }
    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            Vector3 move = (transform.right * data.direction.x + transform.forward * data.direction.y) * speed;

            if (controller.isGrounded)
            {
                if(vel.y < 0f) vel.y = -2f;
                if(data.buttons.IsSet(NetworkInputButtons.Jump))
                {
                    vel.y = Mathf.Sqrt(jumpForce * -2f * gravity);
                }
            }
            vel.y += gravity * Runner.DeltaTime;
            controller.Move((move + vel)* Runner.DeltaTime);
        }
    }

    public override void Render()
    {
        if (!HasInputAuthority) return;
        Look();
    }
    void Look()
    {
        if (cam == null) return;

        float mouseX = Input.GetAxis("Mouse X") * sensitiv;
        float mouseY = Input.GetAxis("Moust Y") * sensitiv;

        transform.Rotate(Vector3.up * mouseX);

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -maxAngle, maxAngle);
        cam.localRotation = Quaternion.Euler(pitch, 0f, 0f);

    }
        


}
