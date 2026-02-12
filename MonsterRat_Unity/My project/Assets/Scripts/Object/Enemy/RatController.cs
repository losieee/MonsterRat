using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class RatController : MonoBehaviour
{
    [Header("Chase")]
    public string playerTag = "Player";
    public float stopDistance = 0.7f;
    public float repathInterval = 0.2f;

    NavMeshAgent agent;
    Transform player;
    float repathTimer;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.stoppingDistance = stopDistance;

        // 회전/이동은 Agent가 해줌
        agent.updateRotation = true;
        agent.updatePosition = true;
    }

    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void Start()
    {
        FindPlayer();
    }

    void Update()
    {
        if (player == null)
        {
            FindPlayer();
            return;
        }

        repathTimer -= Time.deltaTime;
        if (repathTimer <= 0f)
        {
            repathTimer = repathInterval;

            // 플레이어 위치로 설정
            agent.SetDestination(player.position);
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        player = null;
        FindPlayer();
    }

    void FindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag(playerTag);
        if (p != null) player = p.transform;    
    }
}
