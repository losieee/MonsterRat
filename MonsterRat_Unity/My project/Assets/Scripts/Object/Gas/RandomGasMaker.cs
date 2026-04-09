using UnityEngine;
using Fusion;
using System.Collections.Generic;

public class RandomGasMaker : NetworkBehaviour
{
    [SerializeField] private GasValveSync[] randomGas;

    [Networked, Capacity(6)]       // Capacity 가 크기 - 1스테이지 가스는 6개니까 6
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
            // 호스트가 먼저 GasActive 결정
            RandomSpawnGas();
        }

        // 동기화 된 GasActive 받아서 본인 로컬 유니티에 적용
        ApplyGasState(true);
    }

    // 늦게 들어온 클라이언트에도 똑같은 값 적용시켜야해서 계속 확인
    public override void FixedUpdateNetwork()
    {
        ApplyGasState(false);
    }

    void RandomSpawnGas()
    {
        int count = randomGas.Length;
        int pickCount = 4;

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

        for (int i = 0; i < pickCount; i++)
        {
            int randIndex = Random.Range(0, indices.Count);
            int selected = indices[randIndex];

            GasActive.Set(selected, true);
            indices.RemoveAt(randIndex);        // 선택됐으면 제거
        }
    }

    // 네트워크에서 GasActive값 추출을 했으니 유니티로 옮겨서 실제로 적용 시켜야함
    void ApplyGasState(bool force)
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
}
