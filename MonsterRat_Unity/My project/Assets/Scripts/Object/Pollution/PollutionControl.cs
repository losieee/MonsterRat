using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class PollutionControl : MonoBehaviour
{
    public Material[] polstep;

    MeshRenderer render;
    int cleanCount = 0;

    void Awake()
    {
        render = GetComponent<MeshRenderer>();
    }

    void Start()
    {
        if (polstep != null && polstep.Length > 0)
            render.material = polstep[0];
    }

    public void CleanOnce()
    {
        cleanCount++;

        if (polstep != null && cleanCount < polstep.Length)
            render.material = polstep[cleanCount];

        if (cleanCount >= 3)
            Destroy(transform.root.gameObject);
    }
}
