using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerTest : MonoBehaviour
{
    [Header("Move")]
    public float speed = 5f;
    public float gravity = -9.81f;

    [Header("Look")]
    public Transform cam;
    public float sensitiv = 2f;
    public float maxAngle = 85f;

    [Header("Raycast")]
    public float distance = 3f;
    public LayerMask interactMask;

    [Header("Grab")]
    public float grabHoldDistance = 3f;
    public float grabMoveSpeed = 15f;
    public float throwBoost = 2.5f;
    public LayerMask grabBlock;
    public float grabPadding = 0.05f;
    public float minHoldDistance = 0.6f;

    [Header("PollutionCleaning")]
    public int cleaningTime = 0;
    public float coolTime = 1;

    private CharacterController control;
    private Vector3 vel;
    private GameObject lookTarget;
    private Rigidbody targetRb;
    private Vector3 lastGrabPos;
    private Vector3 lastGrabVel;
    private float pitch;
    private bool canCleaning = true;
    private float grabbedRadius = 0.25f;

    void Start()
    {
        control = GetComponent<CharacterController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cam == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null) this.cam = cam.transform;
        }
    }

    void Update()
    {
        Look();
        Move();
        LookRaycast();
        HandleGrabInput();
    }

    void FixedUpdate()
    {
        if (targetRb != null)
            MoveGrabbedObject();
    }

    // 움직임
    void Move()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = (transform.right * x + transform.forward * z) * speed;
        control.Move(move * Time.deltaTime);

        if (control.isGrounded && vel.y < 0f)
            vel.y = -2f;

        vel.y += gravity * Time.deltaTime;
        control.Move(vel * Time.deltaTime);
    }

    // 화면 회전
    void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensitiv;
        float mouseY = Input.GetAxis("Mouse Y") * sensitiv;

        transform.Rotate(Vector3.up * mouseX);
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -maxAngle, maxAngle);

        if (cam != null)
            cam.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    // 에임
    void LookRaycast()
    {
        if (cam == null) return;
        Ray ray = new Ray(cam.position, cam.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, distance, interactMask, QueryTriggerInteraction.Ignore))
            lookTarget = hit.collider.gameObject;
        else 
            lookTarget = null;
    }

    // 물체 잡기
    void HandleGrabInput()
    {
        if (Input.GetMouseButtonDown(1))
        {
            if (lookTarget != null && lookTarget.layer == 3)
                TryGrab();
        }

        if (Input.GetMouseButtonUp(1))
        {
            Release();
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (lookTarget != null && lookTarget.layer == 6 && canCleaning)
            {
                cleaningTime++;
                StartCoroutine(CleaningCoolTime());
            }
        }
    }

    // 물체 잡기
    void TryGrab()
    {
        if (lookTarget == null) return;

        Rigidbody rb = lookTarget.GetComponent<Rigidbody>();
        if (rb == null) return;

        targetRb = rb;
        targetRb.freezeRotation = true;
        targetRb.useGravity = false;

        Collider col = targetRb.GetComponent<Collider>();
        if (col != null)
        {
            Vector3 e = col.bounds.extents;
            grabbedRadius = Mathf.Max(e.x, e.y, e.z);
        }
        else
            grabbedRadius = 0.25f;

        lastGrabPos = targetRb.position;
        lastGrabVel = Vector3.zero;
    }

    // 잡기 취소
    void Release()
    {
        if (targetRb == null) return;

        targetRb.freezeRotation = false;
        targetRb.useGravity = true;
        targetRb.velocity = lastGrabVel + cam.forward * throwBoost;
        targetRb = null;
    }

    // 잡고 있을 때 물체 움직임
    void MoveGrabbedObject()
    {
        // 원래 목표 거리
        float desiredDist = grabHoldDistance;
        float actualDist = desiredDist;

        // 카메라 → 목표 지점까지 막히는지 체크
        if (Physics.SphereCast(cam.position, grabbedRadius, cam.forward, out RaycastHit hit, desiredDist, grabBlock, QueryTriggerInteraction.Ignore))
        {
            // 벽에 닿기 직전 거리 축소
            actualDist = Mathf.Clamp(hit.distance - grabPadding, minHoldDistance, desiredDist);
        }

        Vector3 targetPos = cam.position + cam.forward * actualDist;

        Vector3 toTarget = targetPos - targetRb.position;
        Vector3 newPos = targetRb.position + toTarget * grabMoveSpeed * Time.fixedDeltaTime;

        lastGrabVel = (newPos - lastGrabPos) / Time.fixedDeltaTime;
        lastGrabPos = newPos;

        targetRb.MovePosition(newPos);
    }

    IEnumerator CleaningCoolTime()
    {
        canCleaning = false;
        yield return new WaitForSeconds(coolTime);
        canCleaning = true;
    }
}
