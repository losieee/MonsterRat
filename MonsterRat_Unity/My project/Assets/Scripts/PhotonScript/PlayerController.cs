using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using System;

public struct MyNetworkInput : INetworkInput
{
    public Vector2 moveInput;
    public float yaw;
    public float pitch;
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
    public Transform flashPivot;            // 손전등 전용 불빛

    [Header("발소리")]
    public AudioClip[] footSteps;
    public AudioSource footstepAudioSource;
    public float walkStepInterval = 0.5f;
    public float runStepInterval = 0.3f;

    // 다시 익숙한 Rigidbody로 돌아옵니다!
    private Rigidbody rb;
    private float pitch = 0f;
    private float yaw = 0f;
    private Animator animator;
    private float footstepTimer = 0f;
    private PlayerFootStepType footStepType;

    [Networked] public float NetPitch { get; set; }     // 플레이어 위아래 시선 처리

    public override void Spawned()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
        footstepAudioSource = GetComponent<AudioSource>();
        footStepType = GetComponent<PlayerFootStepType>();

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

        if (SlimUI.ModernMenu.UISettingsManager.isMenuOpen || PhotonPlayerUIState.isGlobalStoreOpen)
        {
            return; 
        }

        if (GameInputLock.IsLocked)
            return;

        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, -90f, 90f);

        if (myCamObj != null)
        {
            myCamObj.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }

    void LateUpdate()
    {
        if (!HasInputAuthority)
        {
            flashPivot.localRotation = Quaternion.Euler(NetPitch, 0f, 0f);
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        MyNetworkInput myInput = new MyNetworkInput();
        myInput.moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        myInput.isRunning = Input.GetKey(KeyCode.LeftShift);
        myInput.yaw = yaw;
        myInput.pitch = pitch;

        input.Set(myInput);
    }

    public override void FixedUpdateNetwork()
    {
        if (GameInputLock.IsLocked)
            return;

        if (GetInput(out MyNetworkInput input))
        {
            // 좌우 회전
            transform.rotation = Quaternion.Euler(0, input.yaw, 0);

            if (HasStateAuthority)
            {
                NetPitch = input.pitch;
            }

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

            // 발소리
            if (HasInputAuthority && Runner.IsForward)
            {   
                HandleFootstep(input, moveDir);
            }
        }

        // 상하 회전
        flashPivot.localRotation = Quaternion.Euler(NetPitch, 0f, 0f);      // 손전등 빛 오브젝트도 같이
    }

    void HandleFootstep(MyNetworkInput input, Vector3 moveDir)
    {
        if (footstepAudioSource == null)
            return;

        bool isMoving = input.moveInput.sqrMagnitude > 0.01f && moveDir.sqrMagnitude > 0.001f;
        float interval = input.isRunning ? runStepInterval : walkStepInterval;

        if (!isMoving)
        {
            footstepTimer = interval;
            return;
        }

        footstepTimer -= Runner.DeltaTime;

        if (footstepTimer > 0f)
            return;

        FootStepRangeType currentType = footStepType != null
            ? footStepType.CurrentRangeType
            : FootStepRangeType.Stone;

        Rpc_PlayFootstep(currentType);
        footstepTimer = interval;
    }

    // 다른 사람한테도 발소리 들릴 수 있도록
    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void Rpc_PlayFootstep(FootStepRangeType stepType)
    {
        float sfxVolume = 1f;

        if (SlimUI.ModernMenu.UISettingsManager.Instance != null)
        {
            sfxVolume = SlimUI.ModernMenu.UISettingsManager.Instance.EffectVolume;
        }

        if (HasInputAuthority)
            footstepAudioSource.volume = 0.6f * sfxVolume;
        else
            footstepAudioSource.volume = 1f * sfxVolume;

        PlayFootstep(stepType);
    }

    // 타입 별 발소리 (돌, 철, 물)
    void PlayFootstep(FootStepRangeType stepType)
    {
        if (footstepAudioSource == null || footSteps == null || footSteps.Length == 0)
            return;

        AudioClip clip = null;

        switch (stepType)
        {
            case FootStepRangeType.Metal:
                if (footSteps.Length > 1) clip = footSteps[1];
                break;

            case FootStepRangeType.Water:
                if (footSteps.Length > 2) clip = footSteps[2];
                break;

            case FootStepRangeType.Stone:
            default:
                if (footSteps.Length > 0) clip = footSteps[0];
                break;
        }

        if (clip != null)
        {
            footstepAudioSource.PlayOneShot(clip);
        }
    }

    static void OnPitchChanged(PlayerController player)
    {
        player.ApplyPitch();
    }

    void ApplyPitch()
    {
        if (flashPivot != null)
        {
            flashPivot.localRotation = Quaternion.Euler(NetPitch, 0f, 0f);
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