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
        public string npcName;
        public int daysWithoutMedicine = 0;
        public int daysWithoutFood = 0;
        public bool isDead = false;
        public bool requestedMedicineToday = false;
        public int refusalCount = 0;
        public bool receivedFoodToday = false;
        public bool receivedMedicineToday = false;

        // ⭐ ADDED FOR DUAK
        public bool forceMedicineTomorrow = false;
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

    public void RecordMedicineRequest(string npcName)
    {
        NPCState state = GetOrCreateState(npcName);
        state.requestedMedicineToday = true;
    }

    public void RecordMedicineGiven(string npcName)
    {
        NPCState state = GetOrCreateState(npcName);
        state.daysWithoutMedicine = 0;
        state.daysWithoutFood = 0;
        state.requestedMedicineToday = false;
        state.receivedMedicineToday = true;
    }

    public void RecordRefusal(string npcName, bool requestedMedicine)
    {
        NPCState state = GetOrCreateState(npcName);
        state.refusalCount++;

        if (requestedMedicine)
        {
            state.receivedMedicineToday = false;
        }
    }

    public int GetRefusalCount(string npcName)
    {
        NPCState state = GetOrCreateState(npcName);
        return state.refusalCount;
    }

    public void ResetRefusalCount(string npcName)
    {
        NPCState state = GetOrCreateState(npcName);
        state.refusalCount = 0;
    }

    public bool IsDead(string npcName)
    {
        if (!npcStates.ContainsKey(npcName))
            return false;
        return npcStates[npcName].isDead;
    }

    public void RecordFoodGiven(string npcName)
    {
        NPCState state = GetOrCreateState(npcName);
        state.receivedFoodToday = true;
        state.daysWithoutFood = 0;
    }

    public bool ReceivedFoodToday(string npcName)
    {
        if (!npcStates.ContainsKey(npcName))
            return false;
        return npcStates[npcName].receivedFoodToday;
    }

    public bool ReceivedMedicineToday(string npcName)
    {
        if (!npcStates.ContainsKey(npcName))
            return false;
        return npcStates[npcName].receivedMedicineToday;
    }

    public void SetAllNPCNames(List<string> npcNames)
    {
        allNPCNames = new List<string>(npcNames);
    }

    public List<string> GetAllNPCNames()
    {
        return new List<string>(allNPCNames);
    }

    public void EndDay()
    {
        foreach (var state in npcStates.Values)
        {
            if (state.isDead) continue;

            if (state.requestedMedicineToday && !state.receivedMedicineToday)
            {
                state.isDead = true;
                Debug.Log($"{state.npcName}이(가) 약을 받지 못해 밤에 죽었습니다.");
                continue;
            }

            bool receivedFoodOrMedicine = state.receivedFoodToday || state.receivedMedicineToday;

            if (!receivedFoodOrMedicine)
            {
                state.daysWithoutFood++;

                if (state.daysWithoutFood >= 2)
                {
                    state.isDead = true;
                    Debug.Log($"{state.npcName}이(가) 이틀 연속 밥/약을 모두 받지 못해 죽었습니다.");
                }
            }
            else
            {
                state.daysWithoutFood = 0;
            }
        }
    }

    public void OnNewDay()
    {
        foreach (var state in npcStates.Values)
        {
            // ⭐ ADDED FOR DUAK
            if (state.forceMedicineTomorrow && !state.isDead)
            {
                state.requestedMedicineToday = true;
                state.forceMedicineTomorrow = false;
                Debug.Log($"[Duak Curse] {state.npcName} must request medicine today.");
            }

            // Reset daily flags
            state.requestedMedicineToday = false;
            state.refusalCount = 0;
            state.receivedFoodToday = false;
            state.receivedMedicineToday = false;
        }
    }

    public void ResetAllStates()
    {
        npcStates.Clear();
        hasShamanEventTriggered = false;
    }

    public bool HasShamanEventTriggered()
    {
        return hasShamanEventTriggered;
    }

    public void SetShamanEventTriggered()
    {
        hasShamanEventTriggered = true;
    }
}


