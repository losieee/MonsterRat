using UnityEngine;

public class TutorialPollutionControl : MonoBehaviour, IClearTarget
{
    public Material[] polstep;
    public Material[] outline;

    MeshRenderer render;
    MeshRenderer outlineRender;
    int cleanCount = 0;
    
    public float Remain01 => 1f;
    public float Weight => 1f;

    void Awake()
    {
        render = GetComponent<MeshRenderer>();

        if (transform.parent != null)
            outlineRender = transform.parent.GetComponent<MeshRenderer>();
    }

    void Start()
    {
        if (ClearManager.Instance != null)
            ClearManager.Instance.Register(this);
    }

    public void CleanOnce()
    {
        cleanCount++;

        if (polstep != null && cleanCount < polstep.Length)
            render.material = polstep[cleanCount];

        if(cleanCount < outline.Length)
            outlineRender.material = outline[cleanCount];

        if (cleanCount >= 3)
        {
            Destroy(transform.root.gameObject);
        }
    }

    void OnDestroy()
    {
        if (polstep != null && polstep.Length > 0)
            render.material = polstep[0];

        if (ClearManager.Instance != null)
            ClearManager.Instance.Unregister(this);
    }
}
