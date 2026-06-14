#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LegacyNavMeshCleaner
{
    [MenuItem("Tools/NavMesh/Force Clear Legacy NavMesh")]
    private static void ForceClearLegacyNavMesh()
    {
        // 예전 방식으로 씬에 박혀있는 NavMesh 제거
        UnityEditor.AI.NavMeshBuilder.ClearAllNavMeshes();

        // 현재 에디터/플레이 상태에 로드된 NavMesh도 언로드
        UnityEngine.AI.NavMesh.RemoveAllNavMeshData();

        // 열려있는 모든 씬 저장 대상으로 표시
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);

            if (scene.isLoaded)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }

        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();

        Debug.Log("Legacy NavMesh Clear 완료. 그래도 보이면 Unity를 재시작하세요.");
    }
}
#endif