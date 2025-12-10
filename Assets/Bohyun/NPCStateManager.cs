using System.Collections.Generic;
using UnityEngine;

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

        // ⭐ Used by Duak + Birak
        public bool forceMedicineTomorrow = false;
    }

    private Dictionary<string, NPCState> npcStates = new Dictionary<string, NPCState>();

    // List of NPCs that appear today
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
                npcName = npcName
            };
        }
        return npcStates[npcName];
    }

    public void RecordMedicineRequest(string npcName)
    {
        GetOrCreateState(npcName).requestedMedicineToday = true;
    }

    public void RecordMedicineGiven(string npcName)
    {
        var st = GetOrCreateState(npcName);
        st.daysWithoutMedicine = 0;
        st.receivedMedicineToday = true;
        st.receivedFoodToday = true;
        st.requestedMedicineToday = false;
    }

    public void RecordFoodGiven(string npcName)
    {
        var st = GetOrCreateState(npcName);
        st.daysWithoutFood = 0;
        st.receivedFoodToday = true;
    }

    // ⭐ REQUIRED BY NIGHTTIME + QUEUE SYSTEM
    public bool ReceivedFoodToday(string npcName)
    {
        if (!npcStates.ContainsKey(npcName)) return false;
        return npcStates[npcName].receivedFoodToday;
    }

    public bool ReceivedMedicineToday(string npcName)
    {
        if (!npcStates.ContainsKey(npcName)) return false;
        return npcStates[npcName].receivedMedicineToday;
    }

    public void RecordRefusal(string npcName, bool medicineRequest)
    {
        var st = GetOrCreateState(npcName);
        st.refusalCount++;
    }

    public int GetRefusalCount(string npcName)
    {
        return GetOrCreateState(npcName).refusalCount;
    }

    public void ResetRefusalCount(string npcName)
    {
        GetOrCreateState(npcName).refusalCount = 0;
    }

    public bool IsDead(string npcName)
    {
        return npcStates.ContainsKey(npcName) && npcStates[npcName].isDead;
    }

    public void SetAllNPCNames(List<string> names)
    {
        allNPCNames = new List<string>(names);
    }

    public List<string> GetAllNPCNames()
    {
        return new List<string>(allNPCNames);
    }

    // ⭐ Nighttime death logic
    public void EndDay()
    {
        foreach (var st in npcStates.Values)
        {
            if (st.isDead) continue;

            if (st.requestedMedicineToday && !st.receivedMedicineToday)
            {
                st.isDead = true;
                Debug.Log($"{st.npcName} died from no medicine.");
                continue;
            }

            bool hadHelp = st.receivedFoodToday || st.receivedMedicineToday;

            if (!hadHelp)
            {
                st.daysWithoutFood++;
                if (st.daysWithoutFood >= 2)
                {
                    st.isDead = true;
                    Debug.Log($"{st.npcName} starved for 2 days.");
                }
            }
            else
            {
                st.daysWithoutFood = 0;
            }
        }
    }

    // ⭐ FIXED FOR DOKKAEBI EFFECTS
    public void OnNewDay()
    {
        foreach (var st in npcStates.Values)
        {
            bool forcedMedicine = st.forceMedicineTomorrow;
            st.forceMedicineTomorrow = false;

            st.refusalCount = 0;
            st.receivedFoodToday = false;
            st.receivedMedicineToday = false;

            if (forcedMedicine && !st.isDead)
            {
                st.requestedMedicineToday = true;
                Debug.Log($"[Dokkaebi Effect] {st.npcName} must request medicine today.");
            }
            else
            {
                st.requestedMedicineToday = false;
            }
        }
    }

    public void ResetAllStates()
    {
        npcStates.Clear();
    }

    // ------------------------------
    // SHAMAN EVENT FLAG
    // ------------------------------

    private bool shamanEventTriggered = false;

    /// <summary>
    /// 무당 이벤트가 이미 발동했는지 확인
    /// </summary>
    public bool HasShamanEventTriggered()
    {
        return shamanEventTriggered;
    }

    /// <summary>
    /// 무당 이벤트 발동을 기록 (한번만 실행됨)
    /// </summary>
    public void SetShamanEventTriggered()
    {
        shamanEventTriggered = true;
    }

}


