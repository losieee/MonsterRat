using Fusion;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    private CharacterController _cc;
    public Camera playerCamera;

    [Header("Settings")]
    public float moveSpeed = 5f;
    public float sensitivity = 2f;

    // [추가됨] 중력 설정 변수
    public float gravity = -20f; // -9.81보다 좀 더 세게 줘야 게임에서 느낌이 좋습니다.
    public float jumpForce = 5f; // (나중에 점프 쓰고 싶을까봐 미리 넣어둠)

    private float _cameraPitch = 0f;
    private float _verticalVelocity; // [추가됨] 현재 떨어지는 속도 저장용

    public override void Spawned()
    {
        _cc = GetComponent<CharacterController>();

        if (HasInputAuthority)
        {
            if (playerCamera != null) playerCamera.gameObject.SetActive(true);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            if (playerCamera != null) playerCamera.gameObject.SetActive(false);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            // --- 1. 시야 회전 ---
            transform.Rotate(0, data.lookRotation.x * sensitivity, 0);
            _cameraPitch -= data.lookRotation.y * sensitivity;
            _cameraPitch = Mathf.Clamp(_cameraPitch, -85f, 85f);

            if (playerCamera != null)
            {
                playerCamera.transform.localRotation = Quaternion.Euler(_cameraPitch, 0, 0);
            }

            // --- 2. 중력 처리 (핵심 추가 부분) ---

            // 땅에 닿아있다면 수직 속도 초기화 (계속 떨어지는 가속도 방지)
            if (_cc.isGrounded)
            {
                // 0으로 하면 가끔 땅에서 뜨는 판정이 생길 수 있어서 
                // 살짝 아래로(-2) 계속 눌러주는 게 안정적입니다.
                if (_verticalVelocity < 0)
                {
                    _verticalVelocity = -2f;
                }

                // (선택사항) 점프 기능을 넣고 싶다면 여기에 추가하면 됩니다.
                // if (data.jumpButton) _verticalVelocity = jumpForce;
            }

            // 중력 가속도 적용 (매 프레임마다 아래로 속도 증가)
            _verticalVelocity += gravity * Runner.DeltaTime;


            // --- 3. 이동 처리 ---

            // 수평 이동 벡터 (좌우/앞뒤)
            Vector3 horizontalMove = transform.forward * data.moveDirection.y + transform.right * data.moveDirection.x;
            horizontalMove.Normalize();
            horizontalMove *= moveSpeed; // 속도 적용

            // 수직 이동 벡터 (위아래)
            Vector3 verticalMove = Vector3.up * _verticalVelocity;

            // 최종 이동 (수평 + 수직)
            // Move 함수 한번에 모든 이동을 처리해야 합니다.
            _cc.Move((horizontalMove + verticalMove) * Runner.DeltaTime);
        }
    }
}