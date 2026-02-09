using UnityEngine;
using Photon.Pun;

public class PlayerController : MonoBehaviourPun  // 전에 프로젝트에서 사용하던 방식을 가져왔습니다.
{
    [Header("속도 설정")]
    public float moveSpeed = 5f;
    public float mouseSensitivity = 2f; // 마우스 감도

    [Header("연결 요소")]
    public GameObject myCamObj; // 자식으로 있는 카메라 오브젝트를 연결

    private Rigidbody rb;
    private float xRotation = 0f; // 위아래 시야각 제한용

    void Start()
    {
        rb = GetComponent<Rigidbody>();

       
        if (photonView.IsMine)
        {
            myCamObj.SetActive(true); // 내 카메라는 켠다

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            myCamObj.SetActive(false); // 남의 카메라는 끈다

            rb.isKinematic = true;

            // 남의 캐릭터에 AudioListener가 켜져 있으면 에러 나거나 소리가 겹침 -> 끈다
            var listener = GetComponentInChildren<AudioListener>();
            if (listener != null) listener.enabled = false;
        }
    }

    void Update()
    {
        if (!photonView.IsMine) return;

        Move();
        Look();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    // 캐릭터 이동 
    void Move()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        // 내가 바라보는 방향 기준으로 이동 벡터 계산
        Vector3 moveDir = (transform.right * x + transform.forward * z).normalized;
        transform.Translate(moveDir * moveSpeed * Time.deltaTime, Space.World);
    }

    // 마우스 시야 회전
    void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        //  좌우 회전  
        transform.Rotate(Vector3.up * mouseX);

        //  위아래 회전  
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // 고개가 뒤로 꺾이지 않게 제한

        // 카메라의 로컬 회전 적용
        if (myCamObj != null)
        {
            myCamObj.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
    }
}