using UnityEngine;
using UnityEngine.UI;
using Fusion;

public class TransStageDoor : NetworkBehaviour
{
    // 1스테이지의 SafeZoneTrigger에서 기록한 "남은 오염물질 여부"
    public static bool isPollutionLeft = false;

    [Header("이동할 위치")]
    public Transform destination;
    public float timeToTeleport = 2.0f;

    [Header("UI 설정")]
    public GameObject interactionUI;
    public Image progressCircle;

    [Networked] public NetworkBool IsDoorActive { get; set; }
    private bool isPlayerInZone = false;
    private NetworkObject localPlayerObj;
    private float currentTimer = 0f;

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            // ★ 수정: 매니저를 기다리지 않고 내가 직접 세이브 파일을 읽어서 적용!
            string json = PlayerPrefs.GetString("MasterWorldSave", "");
            if (!string.IsNullOrEmpty(json))
            {
                WorldSaveData data = JsonUtility.FromJson<WorldSaveData>(json);
                IsDoorActive = data.isDoorActive;
            }
            else
            {
                IsDoorActive = isPollutionLeft;
            }
            Debug.Log($"[Door] 문 활성 여부: {IsDoorActive}");
        }

        if (interactionUI != null) interactionUI.SetActive(false);
    }

    // 호스트가 충돌을 감지하여 클라이언트에게 알려주기
    private void OnTriggerEnter(Collider other)
    {
        if (Object == null || !Object.IsValid || !HasStateAuthority) return;
        if (!IsDoorActive) return;

        if (other.CompareTag("Player"))
        {
            NetworkObject netObj = other.GetComponentInParent<NetworkObject>();
            if (netObj != null)
            {
                // 충돌한 해당 플레이어에게 UI를 켜라고 명령합니다.
                RPC_SetDoorUI(netObj.InputAuthority, netObj, true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (Object == null || !Object.IsValid || !HasStateAuthority) return;
        if (!IsDoorActive) return;

        if (other.CompareTag("Player"))
        {
            NetworkObject netObj = other.GetComponentInParent<NetworkObject>();
            if (netObj != null)
            {
                // 충돌한 해당 플레이어에게 UI를 끄기
                RPC_SetDoorUI(netObj.InputAuthority, netObj, false);
            }
        }
    }

    //클라이언트가 호스트의 명령을 받아 자신의 UI를 켭니다.
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SetDoorUI(PlayerRef targetPlayer, NetworkObject playerObj, NetworkBool isInside)
    {
        //LocalPlayer만 즉 나 자신만
        if (Runner.LocalPlayer == targetPlayer)
        {
            isPlayerInZone = isInside;
            localPlayerObj = isInside ? playerObj : null; // 나중에 텔레포트할 때 쓰기 위해 저장해둡니다.

            if (interactionUI != null) interactionUI.SetActive(isInside);

            if (!isInside)
            {
                ResetInteraction();
            }
        }
    }

    void Update()
    {
        if (Object == null || !Object.IsValid) return;
        if (!IsDoorActive) return;

        // 구역 안에 없거나 내 캐릭터를 아직 못 찾았으면 작동 금지
        if (!isPlayerInZone || localPlayerObj == null) return;

        if (Input.GetKey(KeyCode.E))
        {
            currentTimer += Time.deltaTime;

            if (progressCircle != null)
            {
                progressCircle.fillAmount = currentTimer / timeToTeleport;
            }

            if (currentTimer >= timeToTeleport)
            {
                // 캐릭터 이동시키기
                RPC_RequestTeleport(localPlayerObj);

                // 화면 UI 즉시 정리
                ResetInteraction();
                isPlayerInZone = false;
                localPlayerObj = null;
                if (interactionUI != null) interactionUI.SetActive(false);
            }
        }
        else
        {
            ResetInteraction();
        }
    }

    private void ResetInteraction()
    {
        currentTimer = 0f;
        if (progressCircle != null) progressCircle.fillAmount = 0f;
    }

    //클라이언트의 텔레포트 요청을 호스트한테 보내기
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestTeleport(NetworkObject targetObj)
    {
        if (destination == null || targetObj == null) return;

        PlayerController targetPlayer = targetObj.GetComponent<PlayerController>();
        if (targetPlayer != null)
        {
            var characterController = targetPlayer.GetComponent<NetworkCharacterController>();
            if (characterController != null)
            {
                characterController.Teleport(destination.position);
                targetPlayer.transform.rotation = destination.rotation;
            }
            else
            {
                var networkTransform = targetPlayer.GetComponent<NetworkTransform>();
                if (networkTransform != null)
                {
                    networkTransform.Teleport(destination.position, destination.rotation);
                }
                else
                {
                    targetPlayer.transform.position = destination.position;
                    targetPlayer.transform.rotation = destination.rotation;
                }
            }
        }
    }
}