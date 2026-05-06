using Fusion;
using System.Collections.Generic;
using UnityEngine;

public class FlashRange : NetworkBehaviour
{
    public float range => _range;
    public float angle => _angle;

    [SerializeField] private float _range = 10f;
    [SerializeField] private float _angle = 45f;
    [SerializeField] LayerMask watcherMask;

    public bool IsActive => flashController != null && flashController.IsOn;

    [SerializeField] FlashController flashController;

    // 현재 비추고있는 왓쳐
    private HashSet<MonsterWatcher> currentWatchers = new HashSet<MonsterWatcher>();

    void Update()
    {
        if (!Object.HasInputAuthority) return;

        HashSet<MonsterWatcher> newWatchers = new HashSet<MonsterWatcher>();

        // 이 범위 안에 있는 왓쳐 탐색
        Collider[] hits = Physics.OverlapSphere(transform.position, range, watcherMask);

        foreach (var hit in hits)
        {
            MonsterWatcher watcher = hit.GetComponentInParent<MonsterWatcher>();
            if (watcher == null) continue;

            // 손전등 각도 구해서
            Vector3 dir = (watcher.transform.position - transform.position).normalized;
            float deg = Vector3.Angle(transform.forward, dir);

            // 그 각도 안이면 플레이어 중독수치 올리는거 stop
            if (deg <= angle)
            {
                newWatchers.Add(watcher);

                if (!currentWatchers.Contains(watcher))
                {
                    watcher.Rpc_AddWatcher(Runner.LocalPlayer);
                }
            }
        }

        // 아니면 공격
        foreach (var watcher in currentWatchers)
        {
            if (!newWatchers.Contains(watcher))
            {
                if (watcher != null)
                    watcher.Rpc_RemoveWatcher(Runner.LocalPlayer);
            }
        }

        currentWatchers = newWatchers;
    }

    // 껐을때도 공격
    void OnDisable()
    {
        foreach (var watcher in currentWatchers)
        {
            if (watcher != null)
                watcher.Rpc_RemoveWatcher(Runner.LocalPlayer);
        }

        currentWatchers.Clear();
    }
}