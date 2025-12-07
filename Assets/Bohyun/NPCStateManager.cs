using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// NPC별 상태를 추적하는 매니저 (약을 받지 못한 날 수, 죽었는지 등)
/// </summary>
public class NPCStateManager : MonoBehaviour
{
    private static NPCStateManager instance;
    public static NPCStateManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<NPCStateManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("NPCStateManager");
                    instance = go.AddComponent<NPCStateManager>();
                }
            }
            return instance;
        }
    }

    [System.Serializable]
    public class NPCState
    {
        public string npcName; // NPC 이름 (prefab 이름 또는 고유 ID, 예: "Shaman 1_Shaman")
        public int daysWithoutMedicine = 0; // 약을 받지 못한 연속 날 수
        public int daysWithoutFood = 0; // 밥을 받지 못한 연속 날 수
        public bool isDead = false; // 죽었는지 여부
        public bool requestedMedicineToday = false; // 오늘 약을 요청했는지
        public int refusalCount = 0; // 오늘 거절당한 횟수 (재요청 가능 여부 판단용)
        public bool receivedFoodToday = false; // 오늘 밥을 받았는지
        public bool receivedMedicineToday = false; // 오늘 약을 받았는지
    }

    private Dictionary<string, NPCState> npcStates = new Dictionary<string, NPCState>();
    
    // 무당 이벤트 관련
    private bool hasShamanEventTriggered = false;
    
    // 오늘 등장한 모든 NPC 이름 목록 (daySchedule에서 설정됨)
    private List<string> allNPCNames = new List<string>();

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
        }
    }

    /// <summary>
    /// NPC의 상태를 가져오거나 생성합니다.
    /// </summary>
    public NPCState GetOrCreateState(string npcName)
    {
        if (!npcStates.ContainsKey(npcName))
        {
            npcStates[npcName] = new NPCState
            {
                npcName = npcName,
                daysWithoutMedicine = 0,
                daysWithoutFood = 0,
                isDead = false,
                requestedMedicineToday = false,
                refusalCount = 0,
                receivedFoodToday = false,
                receivedMedicineToday = false
            };
        }
        return npcStates[npcName];
    }

    /// <summary>
    /// NPC가 약을 요청했음을 기록합니다.
    /// </summary>
    public void RecordMedicineRequest(string npcName)
    {
        NPCState state = GetOrCreateState(npcName);
        state.requestedMedicineToday = true;
    }

    /// <summary>
    /// NPC에게 약을 주었음을 기록합니다.
    /// </summary>
    public void RecordMedicineGiven(string npcName)
    {
        NPCState state = GetOrCreateState(npcName);
        state.daysWithoutMedicine = 0; // 약을 받았으므로 리셋
        state.requestedMedicineToday = false;
        state.receivedMedicineToday = true;
    }

    /// <summary>
    /// NPC를 거절했음을 기록합니다.
    /// </summary>
    public void RecordRefusal(string npcName, bool requestedMedicine)
    {
        NPCState state = GetOrCreateState(npcName);
        state.refusalCount++;
        
        if (requestedMedicine)
        {
            // 약을 요청했는데 거절당함 = 약을 받지 못함 (밤에 사망 처리됨)
            // requestedMedicineToday는 유지 (재요청 가능하므로)
            // receivedMedicineToday는 false로 유지 (약을 받지 못했으므로)
            state.receivedMedicineToday = false;
            // 사망은 OnNewDay()에서 처리됨
        }
        else
        {
            // 밥을 요청했는데 거절당함 (이틀 연속 못 먹으면 사망은 OnNewDay에서 처리)
            // 오늘 밥을 못 받은 것은 OnNewDay에서 처리됨
        }
    }

    /// <summary>
    /// NPC의 거절 횟수를 가져옵니다.
    /// </summary>
    public int GetRefusalCount(string npcName)
    {
        NPCState state = GetOrCreateState(npcName);
        return state.refusalCount;
    }

    /// <summary>
    /// NPC의 거절 횟수를 리셋합니다 (수락했을 때).
    /// </summary>
    public void ResetRefusalCount(string npcName)
    {
        NPCState state = GetOrCreateState(npcName);
        state.refusalCount = 0;
    }

    /// <summary>
    /// NPC가 죽었는지 확인합니다.
    /// </summary>
    public bool IsDead(string npcName)
    {
        if (!npcStates.ContainsKey(npcName))
            return false;
        return npcStates[npcName].isDead;
    }

    /// <summary>
    /// NPC에게 밥을 주었음을 기록합니다.
    /// </summary>
    public void RecordFoodGiven(string npcName)
    {
        NPCState state = GetOrCreateState(npcName);
        state.receivedFoodToday = true;
        state.daysWithoutFood = 0; // 밥을 받았으므로 리셋
    }
    
    /// <summary>
    /// NPC가 오늘 밥을 받았는지 확인합니다.
    /// </summary>
    public bool ReceivedFoodToday(string npcName)
    {
        if (!npcStates.ContainsKey(npcName))
            return false;
        return npcStates[npcName].receivedFoodToday;
    }
    
    /// <summary>
    /// NPC가 오늘 약을 받았는지 확인합니다.
    /// </summary>
    public bool ReceivedMedicineToday(string npcName)
    {
        if (!npcStates.ContainsKey(npcName))
            return false;
        return npcStates[npcName].receivedMedicineToday;
    }
    
    /// <summary>
    /// 오늘 등장할 모든 NPC 이름 목록을 설정합니다. (NPCQueueSystem에서 호출)
    /// </summary>
    public void SetAllNPCNames(List<string> npcNames)
    {
        allNPCNames = new List<string>(npcNames);
    }
    
    /// <summary>
    /// 오늘 등장한 모든 NPC 이름 목록을 반환합니다.
    /// </summary>
    public List<string> GetAllNPCNames()
    {
        return new List<string>(allNPCNames);
    }
    
    /// <summary>
    /// 새로운 날이 시작될 때 호출 (모든 NPC의 오늘 요청 상태 리셋 및 사망 체크)
    /// 밤이 되었을 때 약을 받지 못한 NPC는 사망 처리됨
    /// </summary>
    public void OnNewDay()
    {
        // 사망 처리 (상태 리셋 전에 먼저 처리)
        foreach (var state in npcStates.Values)
        {
            // 이미 죽은 NPC는 건너뛰기
            if (state.isDead) continue;
            
            // 약을 요청했는데 받지 못한 NPC는 사망 (밤에 사망 처리)
            if (state.requestedMedicineToday && !state.receivedMedicineToday)
            {
                state.isDead = true;
                Debug.Log($"{state.npcName}이(가) 약을 받지 못해 밤에 죽었습니다.");
                continue; // 이미 사망했으므로 다른 체크는 불필요
            }
            
            // 오늘 밥을 받지 못한 NPC는 daysWithoutFood 증가
            if (!state.receivedFoodToday)
            {
                state.daysWithoutFood++;
                
                // 이틀 연속 밥을 못 먹으면 사망
                if (state.daysWithoutFood >= 2)
                {
                    state.isDead = true;
                    Debug.Log($"{state.npcName}이(가) 이틀 연속 밥을 못 먹어 죽었습니다.");
                }
            }
            else
            {
                // 밥을 받았으면 리셋 (이미 RecordFoodGiven에서 리셋되지만 안전을 위해)
                state.daysWithoutFood = 0;
            }
        }
        
        // 상태 리셋 (사망 처리 후)
        foreach (var state in npcStates.Values)
        {
            // 오늘 상태 리셋
            state.requestedMedicineToday = false;
            state.refusalCount = 0;
            state.receivedFoodToday = false;
            state.receivedMedicineToday = false;
        }
        
        // allNPCNames는 Nighttime 씬에서 사용하므로 여기서 클리어하지 않음
        // SetAllNPCNames()가 호출되면 자동으로 새로운 리스트로 교체되므로 별도 클리어 불필요
    }

    /// <summary>
    /// 모든 상태를 리셋합니다 (게임 재시작 시)
    /// </summary>
    public void ResetAllStates()
    {
        npcStates.Clear();
        hasShamanEventTriggered = false;
    }
    
    /// <summary>
    /// 무당 이벤트가 발생했는지 확인합니다.
    /// </summary>
    public bool HasShamanEventTriggered()
    {
        return hasShamanEventTriggered;
    }
    
    /// <summary>
    /// 무당 이벤트 발생을 기록합니다.
    /// </summary>
    public void SetShamanEventTriggered()
    {
        hasShamanEventTriggered = true;
    }
}

