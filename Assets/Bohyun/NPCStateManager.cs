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
                instance = FindObjectOfType<NPCStateManager>();
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
        public string npcName; // NPC 이름 (prefab 이름 또는 고유 ID)
        public int daysWithoutMedicine = 0; // 약을 받지 못한 연속 날 수
        public bool isDead = false; // 죽었는지 여부
        public bool requestedMedicineToday = false; // 오늘 약을 요청했는지
        public int refusalCount = 0; // 오늘 거절당한 횟수 (재요청 가능 여부 판단용)
        public bool receivedFoodToday = false; // 오늘 밥을 받았는지
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
                isDead = false,
                requestedMedicineToday = false,
                refusalCount = 0,
                receivedFoodToday = false
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
            // 약을 요청했는데 거절당함
            state.daysWithoutMedicine++;
            state.requestedMedicineToday = false;
            
            // 이틀 연속 약을 받지 못하면 죽음
            if (state.daysWithoutMedicine >= 2)
            {
                state.isDead = true;
                Debug.Log($"{npcName}이(가) 약을 받지 못해 죽었습니다.");
            }
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
    /// 새로운 날이 시작될 때 호출 (모든 NPC의 오늘 요청 상태 리셋)
    /// </summary>
    public void OnNewDay()
    {
        foreach (var state in npcStates.Values)
        {
            state.requestedMedicineToday = false;
            state.refusalCount = 0; // 거절 횟수도 리셋
            state.receivedFoodToday = false; // 밥 받은 상태도 리셋
        }
        allNPCNames.Clear(); // NPC 목록도 리셋
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

