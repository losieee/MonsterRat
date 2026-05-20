using SlimUI.ModernMenu;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMov : MonoBehaviour
{
    public static PlayerMov instance;

    public bool canControl = false;

    [Header("Move")]
    public float speed = 5f;
    public float gravity = -9.81f;

    [Header("Look")]
    public Transform cam;
    public float sensitiv = 2f;
    public float maxAngle = 85f;

    [Header("발소리")]
    public AudioClip[] footSteps;
    public AudioSource footstepAudioSource;
    public float walkStepInterval = 0.5f;
    public float runStepInterval = 0.3f;

    CharacterController control;
    Vector3 vel;
    float pitch;
    float footstepTimer;

    PlayerUIState ui;
    PlayerFootStepType footStepType;


    void Awake()
    {
        instance = this;

        control = GetComponent<CharacterController>();
        ui = GetComponent<PlayerUIState>();
        footStepType = GetComponent<PlayerFootStepType>();

        if (cam == null)
        {
            Camera c = GetComponentInChildren<Camera>();
            if (c != null) cam = c.transform;
        }
    }

    void Start()
    {
        
    }

    void Update()
    {
        if (TutorialGameInputLock.IsLocked)
            return;

        if (!canControl)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        bool isStoreOpen = ui != null && ui.IsUIOpen;
        bool isSettingsOpen = UISettingsManager.Instance != null && UISettingsManager.isMenuOpen;

        if (isStoreOpen || isSettingsOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Move();
        Look();
    }

    void Move()
    {
        Vector3 beforePos = transform.position;

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = (transform.right * x + transform.forward * z) * speed;
        control.Move(move * Time.deltaTime);

        if (control.isGrounded && vel.y < 0f)
            vel.y = -2f;

        vel.y += gravity * Time.deltaTime;
        control.Move(vel * Time.deltaTime);

        Vector3 afterPos = transform.position;

        Vector3 horizontalDelta = afterPos - beforePos;
        horizontalDelta.y = 0f;

        HandleFootstep(x, z, horizontalDelta.magnitude);
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

    void HandleFootstep(float x, float z, float actualMoveDistance)
    {
        if (footstepAudioSource == null)
            return;

        bool hasInput = Mathf.Abs(x) > 0.01f || Mathf.Abs(z) > 0.01f;

        // 이동하는 거리 생각해서 이동 안하면 소리X
        bool actuallyMoving = actualMoveDistance > 0.001f;

        float interval = walkStepInterval;

        if (!hasInput || !actuallyMoving || !control.isGrounded)
        {
            footstepTimer = interval;
            return;
        }

        footstepTimer -= Time.deltaTime;

        if (footstepTimer > 0f)
            return;

        FootStepRangeType currentType = footStepType != null
            ? footStepType.CurrentRangeType
            : FootStepRangeType.Stone;

        PlayFootstep(currentType);

        footstepTimer = interval;
    }

    void PlayFootstep(FootStepRangeType stepType)
    {
        if (footstepAudioSource == null || footSteps == null || footSteps.Length == 0)
            return;

        float sfxVolume = 1f;

        if (UISettingsManager.Instance != null)
        {
            sfxVolume = UISettingsManager.Instance.EffectVolume;
        }

        footstepAudioSource.volume = sfxVolume;

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
}
