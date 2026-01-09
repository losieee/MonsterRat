using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class PollutionControl : MonoBehaviour
{
    public PlayerTest player;
    public Material[] polstep;

    MeshRenderer render;

    private void Awake()
    {
        render = GetComponent<MeshRenderer>();
    }

    void Start()
    {
        render.material = polstep[0];
    }

    void Update()
    {
        switch (player.cleaningTime)
        {
            case 1:
                render.material = polstep[1];
                break;
            case 2:
                render.material = polstep[2];
                break;
            case 3:
                Destroy(gameObject);
                break;
        }
    }
}
