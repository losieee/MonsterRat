using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;

public class Start_zoon_door : NetworkBehaviour
{
    [Header("UI 설정")]
    public GameObject uiCanvas;
    public Image fillImage;
    public TextMeshProUGUI infoText;

    [Header("상호작용 설정")]
    public float holdDuration = 1.0f;    
    public float doorOpenTime = 5.0f;  // 문 몇초 열려있게 할건지

    [Header("문 움직임 설정")]
    public Transform doorVisual;         
    public Vector3 openOffset = new Vector3(0f, 0f, -5f);  // 문 어디로 열리게 할건지 // 스테이지 문 같은경우에는 Z축임
    public float moveSpeed = 5f;        

    private Vector3 closedPosition;
    private Vector3 openPosition;

    [Networked] public NetworkBool IsDoorOpen { get; set; }

    [Networked] public TickTimer DoorTimer { get; set; }

    private bool isPlayerInZone = false;
    private float currentHoldTime = 0f;

    void Start()
    {
        if (uiCanvas != null) uiCanvas.SetActive(false);
        if (fillImage != null) fillImage.fillAmount = 0f;

        // 문의 닫힌 위치와 열릴 위치를 미리 계산해서 저장해둠
        if (doorVisual != null)
        {
            closedPosition = doorVisual.localPosition;
            openPosition = closedPosition + openOffset;
        }
    }

    void Update()
    {
        if (Object == null || !Object.IsValid) return;

        // 문이 열려있으면 UI를 끄고 타이머 초기화
        if (IsDoorOpen)
        {
            if (uiCanvas != null && uiCanvas.activeSelf) uiCanvas.SetActive(false);
            currentHoldTime = 0f;
            if (fillImage != null) fillImage.fillAmount = 0f;
            return;
        }

        // 구역에 없으면 무시
        if (!isPlayerInZone) return;

        // E키 상호작용
        if (Input.GetKey(KeyCode.E))
        {
            currentHoldTime += Time.deltaTime;

            if (fillImage != null)
            {
                fillImage.fillAmount = currentHoldTime / holdDuration;
            }

            if (currentHoldTime >= holdDuration)
            {
                currentHoldTime = 0f;
                CompleteInteraction();
            }
        }
        else
        {
            currentHoldTime = 0f;
            if (fillImage != null) fillImage.fillAmount = 0f;
        }
    }

    void CompleteInteraction()
    {
        if (uiCanvas != null) uiCanvas.SetActive(false);
        RPC_RequestOpenDoor();
    }

    // 클라이언트, 호스트 상관없이 모두 서버로 요청
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestOpenDoor(RpcInfo info = default)
    {
        // 문이 닫혀있을 때만 열기 열고 타이머 시작
        if (!IsDoorOpen)
        {
            IsDoorOpen = true;
            DoorTimer = TickTimer.CreateFromSeconds(Runner, doorOpenTime);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        //타이머가 만료되면 문을 다시 닫음 
        if (IsDoorOpen && DoorTimer.Expired(Runner))
        {
            IsDoorOpen = false;
            DoorTimer = TickTimer.None; // 타이머 초기화
        }
    }

    //렌더 함수에서 부드러운 이동 처리
    public override void Render()
    {
        if (doorVisual == null) return;

        // IsDoorOpen이 true면 openPosition으로 false면 closedPosition으로 부드럽게 이동
        Vector3 targetPos = IsDoorOpen ? openPosition : closedPosition;
        doorVisual.localPosition = Vector3.Lerp(doorVisual.localPosition, targetPos, Time.deltaTime * moveSpeed);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Object == null || !Object.IsValid) return;
        if (!HasStateAuthority) return;

        if (other.CompareTag("Player"))
        {
            NetworkObject netObj = other.GetComponentInParent<NetworkObject>();
            if (netObj != null)
            {
                RPC_SetZoneUI(netObj.InputAuthority, true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (Object == null || !Object.IsValid) return;
        if (!HasStateAuthority) return;

        if (other.CompareTag("Player"))
        {
            NetworkObject netObj = other.GetComponentInParent<NetworkObject>();
            if (netObj != null)
            {
                RPC_SetZoneUI(netObj.InputAuthority, false);
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SetZoneUI(PlayerRef targetPlayer, NetworkBool isInside)
    {
        if (Runner.LocalPlayer == targetPlayer)
        {
            isPlayerInZone = isInside;

            // 문이 닫혀있을 때만 UI를 켬
            if (uiCanvas != null)
            {
                uiCanvas.SetActive(isInside && !IsDoorOpen);
            }

            if (isInside && infoText != null)
            {
                infoText.text = "E키를 꾹 눌러 문 열기";
            }
            else
            {
                currentHoldTime = 0f;
                if (fillImage != null) fillImage.fillAmount = 0f;
            }
        }
    }
}