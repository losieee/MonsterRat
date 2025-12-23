using Mirror;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMov : NetworkBehaviour
{
    [Header("Move")]
    public float moveSpeed = 5f;
    public float gravity = -18f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 2.2f;
    public float pitchMin = -60f;
    public float pitchMax = 70f;

    private CharacterController cc;
    private Vector3 velocity;

    private Transform camPivot;     // 카메라 피치용 피벗
    private Camera cam;
    private float pitch;            // 위아래 각도

    void Awake()
    {
        cc = GetComponent<CharacterController>();
    }

    public override void OnStartLocalPlayer()
    {
        // 내 플레이어 표시(선택)
        var r = GetComponentInChildren<Renderer>();
        if (r != null) r.material.color = Color.green;

        // 카메라 세팅
        cam = Camera.main;
        if (cam != null)
        {
            // 피벗 생성(머리 위치)
            var pivotGo = new GameObject("CamPivot");
            camPivot = pivotGo.transform;
            camPivot.SetParent(transform);
            camPivot.localPosition = new Vector3(0, 1.6f, 0);
            camPivot.localRotation = Quaternion.identity;

            // 카메라를 피벗 자식으로
            cam.transform.SetParent(camPivot);
            cam.transform.localPosition = new Vector3(0, 0f, -3.5f);
            cam.transform.localRotation = Quaternion.identity;
        }

        // 마우스 잠금
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnDisable()
    {
        // 로컬 플레이어가 비활성화될 때 마우스 풀기
        if (isLocalPlayer)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void Update()
    {
        if (!isLocalPlayer) return; // 내 입력만

        HandleMouseLook();
        HandleMoveAndJump();
    }

    private void HandleMouseLook()
    {
        float mx = Input.GetAxis("Mouse X") * mouseSensitivity;
        transform.Rotate(0f, mx, 0f);

        if (camPivot != null)
        {
            float my = Input.GetAxis("Mouse Y") * mouseSensitivity;
            pitch -= my;
            pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
            camPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }

    private void HandleMoveAndJump()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 input = new Vector3(h, 0, v);
        input = Vector3.ClampMagnitude(input, 1f);

        Vector3 moveDir = transform.forward * input.z + transform.right * input.x;
        cc.Move(moveDir * moveSpeed * Time.deltaTime);

        // 중력
        velocity.y += gravity * Time.deltaTime;
        cc.Move(velocity * Time.deltaTime);
    }
}
