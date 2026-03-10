using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using System;

public struct MyNetworkInput : INetworkInput
{
    public Vector2 moveInput;
    public float yaw;
    public NetworkBool isRunning;
}

public class PlayerController : NetworkBehaviour, INetworkRunnerCallbacks
{
    [Header("속도 설정")]
    public float moveSpeed = 3.0f;
    public float runSpeed = 6.0f;
    public float mouseSensitivity = 2f;

    [Header("연결 요소")]
    public GameObject myCamObj;

    // 다시 익숙한 Rigidbody로 돌아옵니다!
    private Rigidbody rb;
    private float pitch = 0f;
    private float yaw = 0f;
    private Animator animator;

    public override void Spawned()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();

        if (HasInputAuthority)
        {
            myCamObj.SetActive(true);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Runner.AddCallbacks(this);
            yaw = transform.eulerAngles.y;
        }
        else
        {
            myCamObj.SetActive(false);

            // ?? [핵심 해결 포인트] ??
            // 이전에는 여기에 rb.isKinematic = true; 가 있어서 덜덜 떨렸습니다.
            // 방장 화면에서도 게스트가 중력을 받아야 하므로 그 줄을 완전히 삭제했습니다!

            var listener = GetComponentInChildren<AudioListener>();
            if (listener != null) listener.enabled = false;
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (HasInputAuthority)
        {
            Runner.RemoveCallbacks(this);
        }
    }

    void Update()
    {
        if (!HasInputAuthority) return;

        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, -90f, 90f);

        if (myCamObj != null)
        {
            myCamObj.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        MyNetworkInput myInput = new MyNetworkInput();
        myInput.moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        myInput.isRunning = Input.GetKey(KeyCode.LeftShift);
        myInput.yaw = yaw;

        input.Set(myInput);
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out MyNetworkInput input))
        {
            // 좌우 회전
            transform.rotation = Quaternion.Euler(0, input.yaw, 0);

            // 이동 계산
            float currentSpeed = input.isRunning ? runSpeed : moveSpeed;
            Vector3 moveDir = (transform.right * input.moveInput.x + transform.forward * input.moveInput.y).normalized;

            // Rigidbody와 Network Transform을 같이 쓸 때는 position을 직접 밀어주는 방식이 가장 충돌이 적습니다.
            transform.position += moveDir * currentSpeed * Runner.DeltaTime;

            // 애니메이션
            if (animator != null)
            {
                float animMultiplier = input.isRunning ? 1.0f : 0.5f;
                animator.SetFloat("MoveX", input.moveInput.x * animMultiplier, 0.1f, Runner.DeltaTime);
                animator.SetFloat("MoveY", input.moveInput.y * animMultiplier, 0.1f, Runner.DeltaTime);
            }
        }
    }

    #region 안 쓰는 퓨전 콜백들
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnHostMigrationCleanUp(NetworkRunner runner) { }
    #endregion
}