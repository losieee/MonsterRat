using UnityEngine;
using Fusion;

public class StartGameLever : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // 방장만 트리거 내리기 가능
        if (Runner.IsServer)
        {
            // 플레이어인지
            if (other.CompareTag("Player"))
            {
                Runner.LoadScene("GameScene");
            }
        }
    }
}