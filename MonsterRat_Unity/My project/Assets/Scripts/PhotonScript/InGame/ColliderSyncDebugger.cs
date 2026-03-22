using UnityEngine;
using Fusion;

public class ColliderSyncDebugger : NetworkBehaviour
{
    private Rigidbody rb;
    private Collider col;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    // OnDrawGizmos는 유니티 씬(Scene) 뷰와 게임(Game) 뷰에 디버그용 도형을 그려주는 유용한 함수야!
    private void OnDrawGizmos()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (col == null) col = GetComponent<Collider>();

        if (rb != null && col != null)
        {
            // 1. 눈에 보이는 렌더링(Transform) 위치: ?? 초록색 구체
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.2f);

            // 2. 실제 물리 엔진(Rigidbody)이 인식하는 진짜 위치: ?? 빨간색 큐브
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(rb.position, col.bounds.size);

            // 3. 동기화 파괴(Desync) 경고: ?? 두 위치가 10cm 이상 어긋나면 노란색 선으로 연결!
            float distance = Vector3.Distance(transform.position, rb.position);
            if (distance > 0.1f)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, rb.position);

                // 어긋났을 때만 콘솔에 경고 로그 출력
                Debug.LogWarning($"[동기화 경고] {gameObject.name}의 시각적 위치와 물리적 위치가 {distance:F2}m 어긋났습니다!");
            }
        }
    }
}