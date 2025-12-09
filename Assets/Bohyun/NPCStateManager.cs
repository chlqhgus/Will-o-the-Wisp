using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// NPC별 상태를 추적하는 매니저 (약을 받지 못한 날 수, 죽었는지 등)
/// Backwards-compatible API:
/// - GetState(string)
/// - GetOrCreateState(string)
/// - MarkNeedMedicineTomorrow(string) (used by Duak/Birak)
/// - ReceivedFoodToday(string), ReceivedMedicineToday(string)
/// - RecordFoodGiven/RecordMedicineGiven/RecordRefusal/GetRefusalCount/ResetRefusalCount
/// - EndDay(), OnNewDay(), ResetAllStates()
/// 
/// Important fix:
/// - Duak/Birak "will need medicine tomorrow" flag is applied on OnNewDay AFTER we reset daily flags,
///   preventing the previous bug where it was immediately overwritten.
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

        // kept for possible use / compat
        public int daysWithoutMedicine = 0;
        public int daysWithoutFood = 0;

        public bool isDead = false;

        // today's flags
        public bool requestedMedicineToday = false;
        public int refusalCount = 0;
        public bool receivedFoodToday = false;
        public bool receivedMedicineToday = false;

        // ⭐ used by Dokkaebi curses (Duak/Birak/Gaksi etc.)
        // When true at the end of previous night, OnNewDay will cause the NPC to request medicine automatically.
        public bool willNeedMedicineTomorrow = false;
    }

    // internal storage
    private Dictionary<string, NPCState> npcStates = new Dictionary<string, NPCState>();

    // shaman event flag (kept for compatibility with NPCQueueSystem / NighttimeManager)
    private bool hasShamanEventTriggered = false;

    // list of NPCs that appear today (set by NPCQueueSystem)
    private List<string> allNPCNames = new List<string>();

    // ---------------------------
    // Unity lifecycle
    // ---------------------------
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

    // ---------------------------
    // Get / Create helpers
    // ---------------------------
    /// <summary>
    /// Returns the state object if it exists, otherwise null.
    /// (Some older code expects a GetState method.)
    /// </summary>
    public NPCState GetState(string npcName)
    {
        if (string.IsNullOrEmpty(npcName)) return null;
        npcStates.TryGetValue(npcName, out NPCState s);
        return s;
    }

    /// <summary>
    /// Returns the state, creating it if missing.
    /// </summary>
    public NPCState GetOrCreateState(string npcName)
    {
        if (string.IsNullOrEmpty(npcName)) return null;

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
                receivedMedicineToday = false,
                willNeedMedicineTomorrow = false
            };
        }
        return npcStates[npcName];
    }

    // ---------------------------
    // Dokkaebi / curse utilities
    // ---------------------------
    /// <summary>
    /// Mark the NPC so they will request medicine on the next day.
    /// Use this from DuakEffect/BirakEffect without touching NPCQueueSystem.
    /// </summary>
    public void MarkNeedMedicineTomorrow(string npcName)
    {
        if (string.IsNullOrEmpty(npcName)) return;
        NPCState s = GetOrCreateState(npcName);
        s.willNeedMedicineTomorrow = true;
        Debug.Log($"[NPCStateManager] MarkNeedMedicineTomorrow: {npcName} flagged to request medicine next day.");
    }

    // ---------------------------
    // Interaction recording (called from NPCQueueSystem)
    // ---------------------------
    public void RecordMedicineRequest(string npcName)
    {
        if (string.IsNullOrEmpty(npcName)) return;
        GetOrCreateState(npcName).requestedMedicineToday = true;
    }

    public void RecordMedicineGiven(string npcName)
    {
        if (string.IsNullOrEmpty(npcName)) return;
        NPCState s = GetOrCreateState(npcName);
        s.daysWithoutMedicine = 0;
        s.daysWithoutFood = 0;
        s.requestedMedicineToday = false;
        s.receivedMedicineToday = true;
    }

    public void RecordFoodGiven(string npcName)
    {
        if (string.IsNullOrEmpty(npcName)) return;
        NPCState s = GetOrCreateState(npcName);
        s.receivedFoodToday = true;
        s.daysWithoutFood = 0;
    }

    /// <summary>
    /// Called when player refuses the NPC's request.
    /// requestedMedicine param indicates whether the NPC had requested medicine (true) or food (false).
    /// </summary>
    public void RecordRefusal(string npcName, bool requestedMedicine)
    {
        if (string.IsNullOrEmpty(npcName)) return;
        NPCState s = GetOrCreateState(npcName);
        s.refusalCount++;

        if (requestedMedicine)
        {
            // Mark that they did not get medicine today (so EndDay can kill them accordingly)
            s.receivedMedicineToday = false;
        }
        // for food refusal we rely on EndDay starvation logic
    }

    public int GetRefusalCount(string npcName)
    {
        if (string.IsNullOrEmpty(npcName)) return 0;
        return GetOrCreateState(npcName).refusalCount;
    }

    public void ResetRefusalCount(string npcName)
    {
        if (string.IsNullOrEmpty(npcName)) return;
        GetOrCreateState(npcName).refusalCount = 0;
    }

    // ---------------------------
    // Simple query helpers (kept for compatibility)
    // ---------------------------
    public bool ReceivedFoodToday(string npcName)
    {
        NPCState s = GetState(npcName);
        return s != null && s.receivedFoodToday;
    }

    public bool ReceivedMedicineToday(string npcName)
    {
        NPCState s = GetState(npcName);
        return s != null && s.receivedMedicineToday;
    }

    public bool IsDead(string npcName)
    {
        NPCState s = GetState(npcName);
        return s != null && s.isDead;
    }

    // ---------------------------
    // All-NPC list (set by NPCQueueSystem when building today's queue)
    // ---------------------------
    public void SetAllNPCNames(List<string> npcNames)
    {
        allNPCNames = npcNames != null ? new List<string>(npcNames) : new List<string>();
    }
    public List<string> GetAllNPCNames()
    {
        return new List<string>(allNPCNames);
    }

    // ---------------------------
    // Shaman event helpers (compat)
    // ---------------------------
    public bool HasShamanEventTriggered()
    {
        return hasShamanEventTriggered;
    }
    public void SetShamanEventTriggered()
    {
        hasShamanEventTriggered = true;
    }

    // ---------------------------
    // Night processing: apply deaths, starvation rules
    // ---------------------------
    /// <summary>
    /// Called at night to process deaths (preserve original behaviour).
    /// - If NPC requested medicine and did not receive it -> dead.
    /// - If NPC received neither food nor medicine for 2 consecutive days -> dead.
    /// </summary>
    public void EndDay()
    {
        foreach (var kvp in npcStates)
        {
            NPCState s = kvp.Value;
            if (s.isDead) continue;

            // medicine request death
            if (s.requestedMedicineToday && !s.receivedMedicineToday)
            {
                s.isDead = true;
                Debug.Log($"[NPCStateManager] EndDay: {s.npcName} died because they requested medicine but didn't receive it.");
                continue;
            }

            // starvation logic
            bool gotAny = s.receivedFoodToday || s.receivedMedicineToday;
            if (!gotAny)
            {
                s.daysWithoutFood++;
                if (s.daysWithoutFood >= 2)
                {
                    s.isDead = true;
                    Debug.Log($"[NPCStateManager] EndDay: {s.npcName} died after {s.daysWithoutFood} days without food/medicine.");
                }
            }
            else
            {
                s.daysWithoutFood = 0; // reset when got help
            }
        }
    }

    // ---------------------------
    // Morning reset & apply curses
    // ---------------------------
    /// <summary>
    /// Called at the start of a new day.
    /// Order is important:
    /// 1) Reset daily flags from previous day (refusalCount, received flags).
    /// 2) THEN apply curses (willNeedMedicineTomorrow) so they are not overwritten.
    /// </summary>
    public void OnNewDay()
    {
        // 1) Reset daily flags for all alive states
        foreach (var kvp in npcStates)
        {
            NPCState s = kvp.Value;

            // don't reset isDead or willNeedMedicineTomorrow here
            // clear daily flags
            s.requestedMedicineToday = false;
            s.refusalCount = 0;
            s.receivedFoodToday = false;
            s.receivedMedicineToday = false;
            // note: daysWithoutFood / daysWithoutMedicine are kept as is (used by EndDay)
        }

        // 2) Apply curses that ask for medicine this morning
        foreach (var kvp in npcStates)
        {
            NPCState s = kvp.Value;
            if (s.isDead) continue;

            if (s.willNeedMedicineTomorrow)
            {
                s.requestedMedicineToday = true;
                s.willNeedMedicineTomorrow = false;
                Debug.Log($"[NPCStateManager] OnNewDay: curse applied -> {s.npcName} will request medicine today.");
            }
        }
    }

    // ---------------------------
    // Debug & reset utilities
    // ---------------------------
    public void ResetAllStates()
    {
        npcStates.Clear();
        hasShamanEventTriggered = false;
        allNPCNames.Clear();
    }
}



