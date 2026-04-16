using UnityEngine;
using Fusion;

public class GasRandomSpawner : NetworkBehaviour
{
    [Header("Spawn Settings")]
    public NetworkPrefabRef smallGasPrefab;  
    public int spawnCount = 15;              
    public float gasRadius = 0.5f;           
    public LayerMask gasLayerMask;           

    private BoxCollider spawnArea;

    private void Awake()
    {
        spawnArea = GetComponent<BoxCollider>();
    }

    public override void Spawned()
    {
       
        if (HasStateAuthority)
        {
            SpawnGasesRandomly();
        }
    }

    private void SpawnGasesRandomly()
    {
        if (spawnArea == null) return;

        Bounds bounds = spawnArea.bounds;
        int spawned = 0;
        int attempts = 0;

        // 목표 개수를 채우거나, 무한 루프 방지를 위해 시도 횟수가 100번을 넘으면 종료
        while (spawned < spawnCount && attempts < 100)
        {
            attempts++;

            // 콜라이더 내부의 랜덤한 좌표 추출
            Vector3 randomPos = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y),
                Random.Range(bounds.min.z, bounds.max.z)
            );
            Collider[] overlaps = Physics.OverlapSphere(randomPos, gasRadius, gasLayerMask);

            // 빈공간에 생성
            if (overlaps.Length == 0)
            {
               
                Runner.Spawn(smallGasPrefab, randomPos, Quaternion.identity);
                spawned++;
            }
        }
    }
}