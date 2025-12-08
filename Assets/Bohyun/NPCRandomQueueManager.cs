using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// NPC 랜덤 큐를 생성하고 관리하는 매니저
/// - 전체 NPC를 랜덤으로 섞되 Shaman 2명은 앞 10번째 이내에 배치
/// - 다음 날에는 살아남은 NPC들만 랜덤으로 섞어서 배치
/// - 추가: Dokkaebi를 확률에 따라 선택하여 최종 큐에 삽입 (사람을 대체하지 않음)
/// </summary>
public class NPCRandomQueueManager : MonoBehaviour
{
    private static NPCRandomQueueManager instance;
    public static NPCRandomQueueManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<NPCRandomQueueManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("NPCRandomQueueManager");
                    instance = go.AddComponent<NPCRandomQueueManager>();
                }
            }
            return instance;
        }
    }

    [Header("NPC Prefab References")]
    [Tooltip("모든 NPC 프리팹을 여기에 드래그&드롭으로 할당하세요 (Day1에 사용할 전체 NPC 목록)\nAssets/Bohyun/Prefab 폴더에서 NPC 프리팹들을 드래그&드롭하세요")]
    public GameObject[] allNPCPrefabs;

    [Header("Settings")]
    [Tooltip("Shaman이 앞에서 몇 번째 이내에 배치되어야 하는지")]
    public int shamanEarlyPositionLimit = 10;

    // -----------------------
    // Dokkaebi inspector slots
    // -----------------------
    [Header("Dokkaebi Prefabs (drag: Heoju, Duak, Birak, Gaksi, Hoesa, Nolgae)")]
    public GameObject Heoju;
    public GameObject Duak;
    public GameObject Birak;
    public GameObject Gaksi;
    public GameObject Hoesa;
    public GameObject Nolgae;

    // map filled in Awake
    private Dictionary<string, GameObject> dokkaebiMap;

    // Dokkaebi spawn probabilities (must sum to ~1.0)
    private readonly Dictionary<string, float> dokkaebiProb = new Dictionary<string, float>()
    {
        { "Heoju", 0.19f },
        { "Duak", 0.17f },
        { "Birak", 0.16f },
        { "Gaksi", 0.18f },
        { "Hoesa", 0.15f },
        { "Nolgae", 0.15f }
    };

    // Day->dokkaebi count (index 0 unused)
    private readonly int[] dokkaebiDayCount = { 0, 2, 2, 2, 2, 3, 3, 3 };

    private List<GameObject> currentDayQueue = new List<GameObject>(); // 현재 날의 NPC 큐
    private List<GameObject> allAvailableNPCs = new List<GameObject>(); // 게임 시작 시 모든 NPC 프리팹


    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Awake에서 먼저 로드 (Start보다 먼저 실행됨)
        LoadAllNPCPrefabs();

        // initialize dokkaebi map (safe even if inspector fields not assigned)
        dokkaebiMap = new Dictionary<string, GameObject>()
        {
            { "Heoju", Heoju },
            { "Duak", Duak },
            { "Birak", Birak },
            { "Gaksi", Gaksi },
            { "Hoesa", Hoesa },
            { "Nolgae", Nolgae }
        };
    }

    void Start()
    {
        // Start에서도 한 번 더 확인
        if (allAvailableNPCs == null || allAvailableNPCs.Count == 0)
        {
            Debug.LogWarning("NPCRandomQueueManager: Start()에서 allAvailableNPCs가 비어있습니다. 다시 로드합니다.");
            LoadAllNPCPrefabs();
        }
    }

    /// <summary>
    /// 모든 NPC 프리팹을 로드합니다 (Inspector에서 할당된 프리팹 사용).
    /// </summary>
    void LoadAllNPCPrefabs()
    {
        if (allAvailableNPCs == null)
        {
            allAvailableNPCs = new List<GameObject>();
        }

        allAvailableNPCs.Clear();

        // Inspector에 할당된 프리팹이 있으면 사용
        if (allNPCPrefabs != null && allNPCPrefabs.Length > 0)
        {
            int nullCount = 0;
            foreach (GameObject prefab in allNPCPrefabs)
            {
                if (prefab != null)
                {
                    allAvailableNPCs.Add(prefab);
                }
                else
                {
                    nullCount++;
                }
            }
            Debug.Log($"NPCRandomQueueManager: Inspector에서 {allAvailableNPCs.Count}개의 NPC 프리팹을 로드했습니다. (null 프리팹: {nullCount}개, 전체 배열 길이: {allNPCPrefabs.Length})");

            if (allAvailableNPCs.Count == 0)
            {
                Debug.LogError("NPCRandomQueueManager: 로드된 NPC 프리팹이 0개입니다. Inspector에서 'All NPC Prefabs' 배열에 유효한 NPC 프리팹들을 할당해주세요.");
            }
        }
        else
        {
            Debug.LogError($"NPCRandomQueueManager: NPC 프리팹이 할당되지 않았습니다. allNPCPrefabs가 null이거나 길이가 0입니다. (길이: {allNPCPrefabs?.Length ?? 0})");
            Debug.LogError("NPCRandomQueueManager: Inspector에서 'All NPC Prefabs' 배열에 NPC 프리팹들을 드래그&드롭으로 할당해주세요.");
        }
    }

    /// <summary>
    /// 현재 날의 NPC 큐를 생성합니다.
    /// Day1: 모든 NPC를 랜덤으로 섞되 Shaman 2명은 앞 10번째 이내에 배치
    /// Day2~7: 살아남은 NPC들만 랜덤으로 섞어서 배치
    /// </summary>
    public List<GameObject> GenerateDayQueue()
    {
        Debug.Log("NPCRandomQueueManager: GenerateDayQueue() 호출됨");

        // allAvailableNPCs가 비어있으면 다시 로드 시도
        if (allAvailableNPCs == null || allAvailableNPCs.Count == 0)
        {
            Debug.LogWarning("NPCRandomQueueManager: GenerateDayQueue()에서 allAvailableNPCs가 비어있습니다. 다시 로드합니다.");
            LoadAllNPCPrefabs();

            if (allAvailableNPCs == null || allAvailableNPCs.Count == 0)
            {
                Debug.LogError("NPCRandomQueueManager: allAvailableNPCs를 로드할 수 없습니다. Inspector에서 'All NPC Prefabs' 배열에 NPC 프리팹들을 할당해주세요.");
                Debug.LogError($"NPCRandomQueueManager: allNPCPrefabs 배열 길이: {allNPCPrefabs?.Length ?? 0}");
                return new List<GameObject>();
            }
        }

        currentDayQueue.Clear();

        int currentDay = DayManager.Instance != null ? DayManager.Instance.GetCurrentDay() : 1;
        Debug.Log($"NPCRandomQueueManager: 현재 날짜: {currentDay}");
        Debug.Log($"NPCRandomQueueManager: allAvailableNPCs.Count = {allAvailableNPCs.Count}");

        // 살아남은 NPC들만 필터링
        List<GameObject> aliveNPCs = GetAliveNPCs();
        Debug.Log($"NPCRandomQueueManager: 살아남은 NPC 수: {aliveNPCs.Count}");

        if (aliveNPCs.Count == 0)
        {
            Debug.LogWarning("NPCRandomQueueManager: 살아남은 NPC가 없습니다. allAvailableNPCs를 확인해주세요.");
            Debug.LogWarning($"NPCRandomQueueManager: allAvailableNPCs.Count = {allAvailableNPCs.Count}");
            return currentDayQueue;
        }

        // NPC들을 신분별로 분류
        Dictionary<string, List<GameObject>> npcsByStatus = new Dictionary<string, List<GameObject>>();
        List<GameObject> shamanNPCs = new List<GameObject>();

        int npcComponentCount = 0;
        int noNPCComponentCount = 0;

        foreach (GameObject npcPrefab in aliveNPCs)
        {
            NPCComponent npcComponent = npcPrefab.GetComponent<NPCComponent>();
            string status = "";

            if (npcComponent != null && npcComponent.bohyunData != null)
            {
                status = npcComponent.bohyunData.npcName;
                npcComponentCount++;
            }
            else
            {
                noNPCComponentCount++;
                Debug.LogWarning($"NPCRandomQueueManager: 프리팹 '{npcPrefab.name}'에 NPCComponent 또는 bohyunData가 없습니다. 이름으로 확인합니다.");

                // NPCComponent가 없으면 이름에서 신분 추출
                status = ExtractStatusFromName(npcPrefab.name);
            }

            // Shaman은 별도로 관리
            if (IsShaman(status) || IsShaman(npcPrefab.name))
            {
                shamanNPCs.Add(npcPrefab);
            }
            else
            {
                // 신분별로 분류
                if (!npcsByStatus.ContainsKey(status))
                {
                    npcsByStatus[status] = new List<GameObject>();
                }
                npcsByStatus[status].Add(npcPrefab);
            }
        }

        Debug.Log($"NPCRandomQueueManager: NPCComponent 있는 프리팹: {npcComponentCount}개, 없는 프리팹: {noNPCComponentCount}개");
        Debug.Log($"NPCRandomQueueManager: 신분별 NPC 수 - Shaman: {shamanNPCs.Count}명");
        foreach (var kvp in npcsByStatus)
        {
            Debug.Log($"NPCRandomQueueManager: {kvp.Key}: {kvp.Value.Count}명");
        }

        // Shaman이 2명 이상이면 2명만 사용
        if (shamanNPCs.Count > 2)
        {
            shamanNPCs = ShuffleList(shamanNPCs);
            shamanNPCs = shamanNPCs.GetRange(0, 2);
        }

        // 각 신분별 리스트를 랜덤으로 섞기
        foreach (var status in npcsByStatus.Keys.ToList())
        {
            npcsByStatus[status] = ShuffleList(npcsByStatus[status]);
        }
        shamanNPCs = ShuffleList(shamanNPCs);

        // 라운드 로빈 방식으로 신분을 섞어서 배치
        // Shaman은 앞 10번째 이내에 배치하되, 다른 신분과 섞이도록

        // 신분별 인덱스 추적
        Dictionary<string, int> statusIndices = new Dictionary<string, int>();
        foreach (var status in npcsByStatus.Keys)
        {
            statusIndices[status] = 0;
        }
        int shamanIndex = 0;

        // Shaman을 배치할 위치들 선택 (앞 10번째 이내)
        int shamanCount = shamanNPCs.Count;
        int maxShamanPosition = Mathf.Min(shamanEarlyPositionLimit, aliveNPCs.Count);
        List<int> shamanPositions = new List<int>();
        if (shamanCount > 0)
        {
            for (int i = 0; i < shamanCount; i++)
            {
                int position;
                do
                {
                    position = Random.Range(0, maxShamanPosition);
                } while (shamanPositions.Contains(position));
                shamanPositions.Add(position);
            }
            shamanPositions.Sort();
        }

        // 큐 생성: 라운드 로빈 방식으로 신분을 섞어서 배치
        int shamanPositionIndex = 0;
        List<string> statusList = ShuffleStatusList(new List<string>(npcsByStatus.Keys)); // 신분 리스트 섞기

        // 먼저 Shaman 위치를 제외한 나머지 위치에 라운드 로빈으로 배치할 NPC 리스트 생성
        List<GameObject> mixedNPCs = new List<GameObject>();
        bool hasMoreNPCs = true;

        while (hasMoreNPCs)
        {
            hasMoreNPCs = false;
            foreach (string status in statusList)
            {
                if (statusIndices[status] < npcsByStatus[status].Count)
                {
                    mixedNPCs.Add(npcsByStatus[status][statusIndices[status]]);
                    statusIndices[status]++;
                    hasMoreNPCs = true;
                }
            }
        }

        // Shaman 위치에 Shaman 배치하고, 나머지 위치에 라운드 로빈으로 섞인 NPC 배치
        int mixedIndex = 0;
        for (int i = 0; i < aliveNPCs.Count; i++)
        {
            // Shaman 배치 위치인지 확인
            if (shamanPositionIndex < shamanPositions.Count && i == shamanPositions[shamanPositionIndex])
            {
                if (shamanIndex < shamanNPCs.Count)
                {
                    currentDayQueue.Add(shamanNPCs[shamanIndex]);
                    shamanIndex++;
                    shamanPositionIndex++;
                }
                else if (mixedIndex < mixedNPCs.Count)
                {
                    // Shaman이 부족하면 다른 NPC로 채움
                    currentDayQueue.Add(mixedNPCs[mixedIndex]);
                    mixedIndex++;
                }
            }
            else
            {
                // 라운드 로빈으로 섞인 NPC 배치
                if (mixedIndex < mixedNPCs.Count)
                {
                    currentDayQueue.Add(mixedNPCs[mixedIndex]);
                    mixedIndex++;
                }
                else if (shamanIndex < shamanNPCs.Count)
                {
                    // 다른 NPC가 부족하면 Shaman으로 채움
                    currentDayQueue.Add(shamanNPCs[shamanIndex]);
                    shamanIndex++;
                }
            }
        }

        Debug.Log($"NPCRandomQueueManager: Day {currentDay} 큐 생성 완료. 총 {currentDayQueue.Count}명의 NPC (Shaman: {shamanNPCs.Count}명)");

        // -----------------------
        // Dokkaebi insertion step
        // -----------------------
        // Pick how many dokkaebi for this day and insert them into random positions in the final queue.
        int dokCount = dokkaebiDayCount[Mathf.Clamp(currentDay, 1, 7)];
        if (dokCount > 0)
        {
            List<GameObject> todaysDokkaebi = PickDokkaebiByProbability(dokCount);
            // Insert each dokkaebi at a random position (they do NOT replace humans)
            foreach (var dok in todaysDokkaebi)
            {
                if (dok == null) continue; // safety
                int insertPos = Random.Range(0, currentDayQueue.Count + 1);
                currentDayQueue.Insert(insertPos, dok);
            }
            Debug.Log($"NPCRandomQueueManager: Dokkaebi 삽입됨: {todaysDokkaebi.Count} (Day {currentDay})");
        }

        if (currentDayQueue.Count == 0)
        {
            Debug.LogError("NPCRandomQueueManager: 큐가 비어있습니다!");
            Debug.LogError($"NPCRandomQueueManager: allAvailableNPCs.Count = {allAvailableNPCs.Count}");
            Debug.LogError($"NPCRandomQueueManager: aliveNPCs.Count = {aliveNPCs.Count}");
            Debug.LogError("NPCRandomQueueManager: 프리팹에 NPCComponent와 bohyunData가 할당되어 있는지 확인해주세요.");
        }

        return new List<GameObject>(currentDayQueue);
    }

    /// <summary>
    /// 살아남은 NPC들만 반환합니다.
    /// </summary>
    List<GameObject> GetAliveNPCs()
    {
        List<GameObject> aliveNPCs = new List<GameObject>();

        foreach (GameObject npcPrefab in allAvailableNPCs)
        {
            if (npcPrefab == null) continue;

            string npcName = GetNPCName(npcPrefab);

            // NPCStateManager에서 죽었는지 확인
            if (NPCStateManager.Instance != null && NPCStateManager.Instance.IsDead(npcName))
            {
                continue; // 죽은 NPC는 제외
            }

            aliveNPCs.Add(npcPrefab);
        }

        return aliveNPCs;
    }

    /// <summary>
    /// Dokkaebi를 확률로 선택해서 리스트로 반환합니다.
    /// null인 prefab은 건너뜁니다.
    /// </summary>
    List<GameObject> PickDokkaebiByProbability(int count)
    {
        List<GameObject> result = new List<GameObject>();

        if (dokkaebiProb == null || dokkaebiProb.Count == 0 || dokkaebiMap == null)
            return result;

        // Normalize cumulative (in case floats slightly don't sum to 1)
        float totalProb = dokkaebiProb.Sum(kv => kv.Value);
        if (totalProb <= 0f) totalProb = 1f;

        for (int i = 0; i < count; i++)
        {
            float r = Random.value * totalProb;
            float cum = 0f;
            foreach (var kv in dokkaebiProb)
            {
                cum += kv.Value;
                if (r <= cum)
                {
                    // get prefab by name if available
                    if (dokkaebiMap.TryGetValue(kv.Key, out GameObject prefab) && prefab != null)
                    {
                        result.Add(prefab);
                    }
                    else
                    {
                        Debug.LogWarning($"NPCRandomQueueManager: Dokkaebi prefab for '{kv.Key}' is null or not assigned in inspector.");
                    }
                    break;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// NPC 이름을 가져옵니다. (고유 ID로 사용 - 프리팹 이름만 사용)
    /// 프리팹 이름이 이미 고유하므로 (예: "Shaman 1", "Slave 2") 그대로 사용합니다.
    /// </summary>
    string GetNPCName(GameObject npcPrefab)
    {
        if (npcPrefab == null) return "";

        // 프리팹 이름을 고유 ID로 사용 (예: "Shaman 1", "Slave 2")
        // "(Clone)" 제거하고 공백 정리
        return npcPrefab.name.Replace("(Clone)", "").Trim();
    }

    /// <summary>
    /// NPC가 Shaman인지 확인합니다.
    /// </summary>
    bool IsShaman(string npcName)
    {
        if (string.IsNullOrEmpty(npcName)) return false;
        return npcName.StartsWith("Shaman", System.StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 리스트를 랜덤으로 섞습니다 (Fisher-Yates 알고리즘).
    /// </summary>
    List<GameObject> ShuffleList(List<GameObject> list)
    {
        List<GameObject> shuffled = new List<GameObject>(list);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            GameObject temp = shuffled[i];
            shuffled[i] = shuffled[randomIndex];
            shuffled[randomIndex] = temp;
        }
        return shuffled;
    }

    /// <summary>
    /// 신분 리스트를 랜덤으로 섞습니다.
    /// </summary>
    List<string> ShuffleStatusList(List<string> list)
    {
        List<string> shuffled = new List<string>(list);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            string temp = shuffled[i];
            shuffled[i] = shuffled[randomIndex];
            shuffled[randomIndex] = temp;
        }
        return shuffled;
    }

    /// <summary>
    /// 프리팹 이름에서 신분을 추출합니다.
    /// </summary>
    string ExtractStatusFromName(string prefabName)
    {
        if (string.IsNullOrEmpty(prefabName)) return "Unknown";

        // 프리팹 이름에서 신분 추출 (예: "King", "Yangban 1", "Slave 2" 등)
        string name = prefabName.Replace("(Clone)", "").Trim();

        if (name.StartsWith("King", System.StringComparison.OrdinalIgnoreCase)) return "King";
        if (name.StartsWith("Yangban", System.StringComparison.OrdinalIgnoreCase)) return "Yangban";
        if (name.StartsWith("Physician", System.StringComparison.OrdinalIgnoreCase)) return "Physician";
        if (name.StartsWith("Merchant", System.StringComparison.OrdinalIgnoreCase)) return "Merchant";
        if (name.StartsWith("Slave", System.StringComparison.OrdinalIgnoreCase)) return "Slave";
        if (name.StartsWith("Shaman", System.StringComparison.OrdinalIgnoreCase)) return "Shaman";

        return "Unknown";
    }

    /// <summary>
    /// 현재 날의 NPC 큐를 반환합니다.
    /// </summary>
    public List<GameObject> GetCurrentDayQueue()
    {
        return new List<GameObject>(currentDayQueue);
    }

    // DaySchedule은 더 이상 사용하지 않음 (랜덤 큐만 사용)
}
