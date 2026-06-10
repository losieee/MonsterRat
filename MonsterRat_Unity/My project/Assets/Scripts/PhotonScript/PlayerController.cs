using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.Analytics;
using System.Collections;

public struct MyNetworkInput : INetworkInput
{
    public Vector2 moveInput;
    public float yaw;
    public float pitch;
    public NetworkBool isRunning;
}

public class PlayerController : NetworkBehaviour, INetworkRunnerCallbacks
{
    public static PlayerController LocalPlayer;

    [Header("속도 설정")]
    public float moveSpeed = 3.0f;
    public float runSpeed = 6.0f;
    public float mouseSensitivity = 2f;

    [Header("연결 요소")]
    public GameObject myCamObj;
    public Transform flashPivot;

    [Header("발소리")]
    public AudioClip[] footSteps;
    public AudioSource footstepAudioSource;
    public float walkStepInterval = 0.5f;
    public float runStepInterval = 0.3f;

    [Header("벽 거리조절")]
    [SerializeField] private float wallCheckDistance = 0.6f;
    [SerializeField] private LayerMask wallLayer;

    [Header("카메라 설정(멀미 전용)")]
    public Vector3 cameraEffectEuler;
    public Vector3 cameraEffectPosition;
    public float cameraEffectFovOffset;

    private Vector3 originalCameraLocalPosition;
    private float originalFov;
    private Camera playerCamera;

    private Rigidbody rb;
    private float pitch = 0f;
    private float yaw = 0f;
    private Animator animator;
    private float footstepTimer = 0f;
    private PlayerFootStepType footStepType;

    private Vector2 currentMouseDelta;

    private float cachedXSens = 1f;
    private float cachedYSens = 1f;
    private float cachedSmoothing = 0f;

    [Networked] public float NetPitch { get; set; }

    // 플레이어 사망
    public Transform spectatePoint;
    public GameObject gameOverUI;
    public float restartDelay = 3f;
    [Networked] public NetworkBool IsDead { get; set; }

    private bool isSpectating;
    private Transform spectateTarget;

    public override void Spawned()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
        footstepAudioSource = GetComponent<AudioSource>();
        footStepType = GetComponent<PlayerFootStepType>();

        if (HasInputAuthority)
        {
            LocalPlayer = this;

            myCamObj.SetActive(true);
            gameOverUI.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Runner.AddCallbacks(this);
            yaw = transform.eulerAngles.y;

            originalCameraLocalPosition = myCamObj.transform.localPosition;

            playerCamera = myCamObj.GetComponentInChildren<Camera>();
            if (playerCamera != null)
            {
                originalFov = playerCamera.fieldOfView;
            }

            if (SceneManager.GetActiveScene().name == "Ending")
            {
                GameInputLock.Lock();
            }
            else
            {
                GameInputLock.Unlock();
            }
            LoadSettingsDataSafely();
        }
        else
        {
            myCamObj.SetActive(false);
            var listener = GetComponentInChildren<AudioListener>();
            if (listener != null) listener.enabled = false;
        }
    }

    private void LoadSettingsDataSafely()
    {
        string jsonString = PlayerPrefs.GetString("MasterGameSettings", "");
        if (!string.IsNullOrEmpty(jsonString))
        {
            SlimUI.ModernMenu.GameSettingsData data = JsonUtility.FromJson<SlimUI.ModernMenu.GameSettingsData>(jsonString);
            cachedXSens = data.xSensitivity;
            cachedYSens = data.ySensitivity;
            cachedSmoothing = data.mouseSmoothing;
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

        if (isSpectating)
        {
            if (spectateTarget != null && myCamObj != null)
            {
                myCamObj.transform.position = spectateTarget.position;
                myCamObj.transform.rotation = spectateTarget.rotation;
            }

            return;
        }

        bool isMenuRealOpen = SlimUI.ModernMenu.UISettingsManager.isMenuOpen && SlimUI.ModernMenu.UISettingsManager.Instance != null;
        if (isMenuRealOpen || PhotonPlayerUIState.isGlobalStoreOpen)
        {
            return;
        }

        if (GameInputLock.IsLocked)
            return;

        float xSens = cachedXSens;
        float ySens = cachedYSens;
        float smoothing = cachedSmoothing;

       
        if (SlimUI.ModernMenu.UISettingsManager.Instance != null)
        {
            xSens = SlimUI.ModernMenu.UISettingsManager.Instance.XSensitivity;
            ySens = SlimUI.ModernMenu.UISettingsManager.Instance.YSensitivity;
            smoothing = SlimUI.ModernMenu.UISettingsManager.Instance.MouseSmoothing;
        }

        xSens = Mathf.Max(0.1f, xSens);
        ySens = Mathf.Max(0.1f, ySens);

        Vector2 targetMouseDelta = new Vector2(Input.GetAxis("Mouse X") * xSens, Input.GetAxis("Mouse Y") * ySens);

        if (smoothing <= 0.01f)
        {
            currentMouseDelta = targetMouseDelta;
        }
        else
        {
            float lerpSpeed = Mathf.Lerp(40f, 5f, smoothing);
            currentMouseDelta = Vector2.Lerp(currentMouseDelta, targetMouseDelta, Time.deltaTime * lerpSpeed);
        }

        yaw += currentMouseDelta.x * mouseSensitivity * 0.1f; 
        pitch -= currentMouseDelta.y * mouseSensitivity * 0.1f; 
        pitch = Mathf.Clamp(pitch, -90f, 90f);

        if (myCamObj != null)
        {
            myCamObj.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f) * Quaternion.Euler(cameraEffectEuler);
            myCamObj.transform.localPosition = originalCameraLocalPosition + cameraEffectPosition;

            if (playerCamera != null)
            {
                playerCamera.fieldOfView = originalFov + cameraEffectFovOffset;
            }
        }
    }

    public void Die()
    {
        if (!HasStateAuthority) return;
        if (IsDead) return;

        IsDead = true;

        RPC_PlayDeadAnimation();

        StartCoroutine(DeadRoutine());

        CheckAllPlayersDead();
    }

    IEnumerator DeadRoutine()
    {
        yield return new WaitForSeconds(2f);

        RPC_OnDead();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_OnDead()
    {
        StartSpectateOtherPlayer();
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayDeadAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("IsDead");
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }
    }

    void CheckAllPlayersDead()
    {
        if (!HasStateAuthority) return;

        PlayerController[] players = FindObjectsOfType<PlayerController>();

        foreach (PlayerController p in players)
        {
            if (!p.IsDead)
                return;
        }

        StartCoroutine(RestartCurrentSceneRoutine());
    }

    void StartSpectateOtherPlayer()
    {
        isSpectating = true;
        GameInputLock.Lock();

        PlayerController[] players = FindObjectsOfType<PlayerController>();

        foreach (PlayerController p in players)
        {
            if (p == this) continue;
            if (p.IsDead) continue;

            spectateTarget = p.flashPivot;
            break;
        }

        if (spectateTarget == null)
        {
            if (gameOverUI != null)
                gameOverUI.SetActive(true);

            if (HasStateAuthority)
                StartCoroutine(RestartCurrentSceneRoutine());
        }
    }

    IEnumerator RestartCurrentSceneRoutine()
    {
        yield return new WaitForSeconds(restartDelay);

        if (Runner == null) yield break;

        if (HasStateAuthority)
        {
            if (PollutionSpawner.Instance != null)
            {
                PollutionSpawner.Instance.DespawnAllCleaningObjects();
            }

            if (ClearManager.Instance != null)
            {
                ClearManager.Instance.ResetProgress();
            }
        }

        yield return null;

        int sceneIndex = SceneManager.GetActiveScene().buildIndex;

        if (sceneIndex >= 0)
        {
            Runner.LoadScene(SceneRef.FromIndex(sceneIndex));
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

        if (GameInputLock.IsLocked)
        {
            myInput.moveInput = Vector2.zero;
            myInput.isRunning = false;
            myInput.yaw = yaw;
            myInput.pitch = pitch;

            input.Set(myInput);
            return;
        }

        myInput.moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        myInput.isRunning = Input.GetKey(KeyCode.LeftShift);
        myInput.yaw = yaw;
        myInput.pitch = pitch;

        input.Set(myInput);
    }

    public override void FixedUpdateNetwork()
    {
        if (GameInputLock.IsLocked || IsDead) return;

        if (GetInput(out MyNetworkInput input))
        {
            Vector3 beforePos = transform.position;

            transform.rotation = Quaternion.Euler(0, input.yaw, 0);

            if (HasStateAuthority)
            {
                NetPitch = input.pitch;
            }

            float currentSpeed = input.isRunning ? runSpeed : moveSpeed;
            Vector3 moveDir = (transform.right * input.moveInput.x + transform.forward * input.moveInput.y).normalized;

            if (moveDir != Vector3.zero)
            {
                Vector3 origin = transform.position + Vector3.up * 1f;

                bool isBlocked = Physics.SphereCast(origin, 0.3f, moveDir, out RaycastHit hit, wallCheckDistance, wallLayer);

                if (!isBlocked)
                {
                    transform.position += moveDir * currentSpeed * Runner.DeltaTime;
                }
                else
                {
                    Vector3 slideDir = Vector3.ProjectOnPlane(moveDir, hit.normal).normalized;

                    if (slideDir.sqrMagnitude > 0.01f)
                    {
                        transform.position += slideDir * currentSpeed * 0.5f * Runner.DeltaTime;
                    }
                }
            }

            Vector3 afterPos = transform.position;
            Vector3 actualDelta = afterPos - beforePos;
            actualDelta.y = 0f;

            float actualMoveDistance = actualDelta.magnitude;

            if (animator != null)
            {
                float animMultiplier = input.isRunning ? 1.0f : 0.5f;
                animator.SetFloat("MoveX", input.moveInput.x * animMultiplier, 0.1f, Runner.DeltaTime);
                animator.SetFloat("MoveY", input.moveInput.y * animMultiplier, 0.1f, Runner.DeltaTime);
            }

            if (HasInputAuthority && Runner.IsForward)
            {
                HandleFootstep(input, actualMoveDistance);
            }
        }

        if (flashPivot != null)
        {
            flashPivot.localRotation = Quaternion.Euler(NetPitch, 0f, 0f);
        }
    }

    void HandleFootstep(MyNetworkInput input, float actualMoveDistance)
    {
        if (footstepAudioSource == null) return;

        bool hasInput = input.moveInput.sqrMagnitude > 0.01f;
        bool actuallyMoving = actualMoveDistance > 0.001f;
        float interval = input.isRunning ? runStepInterval : walkStepInterval;

        if (!hasInput || !actuallyMoving)
        {
            footstepTimer = interval;
            return;
        }

        footstepTimer -= Runner.DeltaTime;

        if (footstepTimer > 0f) return;

        FootStepRangeType currentType = footStepType != null
            ? footStepType.CurrentRangeType
            : FootStepRangeType.Stone;

        Rpc_PlayFootstep(currentType);

        footstepTimer = interval;
    }

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

    void PlayFootstep(FootStepRangeType stepType)
    {
        if (footstepAudioSource == null || footSteps == null || footSteps.Length == 0) return;

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