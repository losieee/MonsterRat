using UnityEngine;
using Fusion;
using System.Collections.Generic;

public class RandomGasMaker : NetworkBehaviour
{
    [SerializeField] private GasValveSync[] randomGas;
    [SerializeField] private int spawnGas;

    [Header("Json")]
    [SerializeField] private TextAsset stageDBJson;
    [SerializeField] private int currentStageNum = 1;
    [SerializeField] private bool isRePollution;

    [Networked, Capacity(20)]       // Capacity 가 크기 - 1스테이지 가스는 6개니까 6
    private NetworkArray<NetworkBool> GasActive => default;
    // Fusion용 bool 배열
    // 사용법
    // GasActive[0] = true      켜기
    // GasActive[1] = false     끄기
    // GasActive[2] = true      켜기
    // 이런거 저장해주는거

    private bool[] _appliedCache;

    public override void Spawned()
    {
        _appliedCache = new bool[randomGas.Length];

        if (Object.HasStateAuthority)
        {
            if (!isRePollution)
            {
                ApplyStageData(currentStageNum);
                // 호스트가 먼저 GasActive 결정
                RandomSpawnGas(spawnGas);
            }
        }

        // 동기화 된 GasActive 받아서 본인 로컬 유니티에 적용
        ApplyGasState(true);
    }

    // 늦게 들어온 클라이언트에도 똑같은 값 적용시켜야해서 계속 확인
    public override void FixedUpdateNetwork()
    {
        ApplyGasState(false);
    }

    public void RandomSpawnGas(int gasSpawnCount)
    {
        if (!Object.HasStateAuthority) return;

        int count = randomGas.Length;

        // 먼저 전부 false
        for (int i = 0; i < count; i++)
        {
            GasActive.Set(i, false);
        }

        // 중복 없이 4개 뽑기
        List<int> indices = new List<int>();
        for (int i = 0; i < count; i++)
        {
            indices.Add(i);
        }

        bool[] selectedGas = new bool[count];

        for (int i = 0; i < gasSpawnCount; i++)
        {
            int randIndex = Random.Range(0, indices.Count);
            int selected = indices[randIndex];

            selectedGas[selected] = true;
            GasActive.Set(selected, true);
            indices.RemoveAt(randIndex);        // 선택됐으면 제거
        }

        for (int i = 0; i < count; i++)
        {
            if (randomGas[i] == null) continue;

            if (!selectedGas[i])
            {
                Transform parent = randomGas[i].transform.parent;

                if (parent != null)
                    parent.gameObject.SetActive(false);
            }
        }
    }

    // 네트워크에서 GasActive값 추출을 했으니 유니티로 옮겨서 실제로 적용 시켜야함
    public void ApplyGasState(bool force)
    {
        for (int i = 0; i < randomGas.Length; i++)
        {
            if (randomGas[i] == null) continue;

            bool state = GasActive[i];

            if (force || _appliedCache[i] != state)
            {
                _appliedCache[i] = state;
                randomGas[i].SetVisible(state);
            }
        }
    }

    // 잔향 배관 가스 전용
    public List<NetworkObject> RandomSpawnRePollutionGas(int gasSpawnCount)
    {
        List<NetworkObject> activatedGas = new List<NetworkObject>();

        if (!Object.HasStateAuthority)
            return activatedGas;

        int count = randomGas.Length;

        for (int i = 0; i < count; i++)
        {
            GasActive.Set(i, false);
        }

        List<int> indices = new List<int>();

        for (int i = 0; i < count; i++)
        {
            indices.Add(i);
        }

        for (int i = 0; i < gasSpawnCount; i++)
        {
            int randIndex = Random.Range(0, indices.Count);
            int selected = indices[randIndex];

            GasActive.Set(selected, true);

            NetworkObject netObj = randomGas[selected].GetComponent<NetworkObject>();

            if (netObj != null)
            {
                activatedGas.Add(netObj);
            }

            indices.RemoveAt(randIndex);
        }

        return activatedGas;
    }

    void ApplyStageData(int stageNum)
    {
        if (stageDBJson == null)
            return;

        string wrappedJson = "{\"stages\":" + stageDBJson.text + "}";

        StageDataList dataList = JsonUtility.FromJson<StageDataList>(wrappedJson);

        if (dataList == null || dataList.stages == null)
            return;

        StageJson data = dataList.stages.Find(stage => stage.Stg_Num == stageNum);

        if (data == null)
            return;

        spawnGas = data.Stg_Pipe;
    }
}
