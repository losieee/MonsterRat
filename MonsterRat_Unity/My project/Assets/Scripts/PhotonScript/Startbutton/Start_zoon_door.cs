using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;

public class Start_zoon_door : Fusion.NetworkBehaviour
{
    [Header("UI 설정")]
    public GameObject uiCanvas;
    public Image fillImage;
    public TextMeshProUGUI infoText;

    [Header("상호작용 설정")]
    public float holdDuration = 3.0f;
    public Animator anim;

    [Header("세이프 존 영역 (문 닫힘 판정용)")]
    public BoxCollider safeZoneArea;

    [Networked]
    public NetworkBool IsDoorOpen { get; set; }

    [Networked]
    public NetworkBool IsDoorPermanentlyClosed { get; set; }

    private bool isPlayerInZone = false;
    private float currentHoldTime = 0f;
    private bool _localDoorOpenState = false;

    void Start()
    {
        if (uiCanvas != null) uiCanvas.SetActive(false);
        if (fillImage != null) fillImage.fillAmount = 0f;
        if (anim == null) anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (Object == null || !Object.IsValid) return;

        if (IsDoorOpen || IsDoorPermanentlyClosed)
        {
            if (uiCanvas != null && uiCanvas.activeSelf) uiCanvas.SetActive(false);
            return;
        }

        if (!isPlayerInZone) return;

        if (Input.GetKey(KeyCode.E))
        {
            currentHoldTime += Time.deltaTime;

            if (fillImage != null)
            {
                fillImage.fillAmount = currentHoldTime / holdDuration;
            }

            if (currentHoldTime >= holdDuration)
            {
                CompleteInteraction();
            }
        }
        else
        {
            currentHoldTime = 0f;
            if (fillImage != null) fillImage.fillAmount = 0f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Object == null || !Object.IsValid) return;

        if (IsDoorOpen || IsDoorPermanentlyClosed) return;

        if (other.CompareTag("Player"))
        {
            NetworkObject netObj = other.GetComponent<NetworkObject>();

            if (netObj != null && netObj.HasInputAuthority)
            {
                isPlayerInZone = true;
                if (uiCanvas != null) uiCanvas.SetActive(true);
                if (infoText != null) infoText.text = "E키를 꾹 눌러 문 열기";
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (Object == null || !Object.IsValid) return;

        if (other.CompareTag("Player"))
        {
            NetworkObject netObj = other.GetComponent<NetworkObject>();

            if (netObj != null && netObj.HasInputAuthority)
            {
                isPlayerInZone = false;
                currentHoldTime = 0f;
                if (fillImage != null) fillImage.fillAmount = 0f;
                if (uiCanvas != null) uiCanvas.SetActive(false);
            }
        }
    }

    void CompleteInteraction()
    {
        isPlayerInZone = false;
        if (uiCanvas != null) uiCanvas.SetActive(false);

        if (Runner.IsServer)
        {
            IsDoorOpen = true;
        }
        else
        {
            RPC_RequestOpenDoor();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestOpenDoor(RpcInfo info = default)
    {
        if (!IsDoorPermanentlyClosed)
        {
            IsDoorOpen = true;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Runner.IsServer) return;

        if (IsDoorOpen && !IsDoorPermanentlyClosed)
        {
            if (safeZoneArea != null)
            {
                bool isAnyPlayerInZone = false;

                foreach (PlayerRef player in Runner.ActivePlayers)
                {
                    NetworkObject playerObj = Runner.GetPlayerObject(player);
                    if (playerObj != null)
                    {
                        if (safeZoneArea.bounds.Contains(playerObj.transform.position))
                        {
                            isAnyPlayerInZone = true;
                            break;
                        }
                    }
                }

                if (!isAnyPlayerInZone)
                {
                    IsDoorOpen = false;
                    IsDoorPermanentlyClosed = true;
                }
            }
        }
    }

    public override void Render()
    {
        if (Object == null || !Object.IsValid) return;

        if (IsDoorOpen && !_localDoorOpenState)
        {
            _localDoorOpenState = true;
            if (anim != null) anim.SetTrigger("DoorOpen");
        }
    }
}