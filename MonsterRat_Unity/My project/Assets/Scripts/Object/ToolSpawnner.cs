using UnityEngine;

public class ToolSpawnner : MonoBehaviour
{
    public GameObject[] tools;

    public void SpawnTool(Transform pos, int n)
    {
        Instantiate(tools[n], pos, default);
    }
}
