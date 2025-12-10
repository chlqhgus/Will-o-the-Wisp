using System.Collections.Generic;
using UnityEngine;

public class NolgaeEffect : MonoBehaviour
{
    private static NolgaeEffect instance;
    public static NolgaeEffect Instance
    {
        get
        {
            if (instance == null)
                instance = FindFirstObjectByType<NolgaeEffect>();
            return instance;
        }
    }

    [Header("If RE-ACCEPT → Random NPC becomes sick tomorrow")]
    public List<string> sickTargetList = new List<string>();

    [Header("If RE-REJECT → FOUR NPCs from this list will die tonight")]
    public List<string> killTargetList = new List<string>();

    private List<string> pendingSick = new List<string>();
    private List<string> pendingKill = new List<string>();

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

    // ----------------------------------------------------
    // RE-ACCEPT → 1 random sick target
    // ----------------------------------------------------
    public void OnReAccept()
    {
        if (sickTargetList.Count == 0)
        {
            Debug.LogWarning("[Nolgae] RE-ACCEPT but SickTargetList is EMPTY");
            return;
        }

        string target = sickTargetList[Random.Range(0, sickTargetList.Count)];
        pendingSick.Add(target);

        Debug.Log($"[Nolgae] RE-ACCEPT → Mark {target} to be SICK tomorrow");
    }

    // ----------------------------------------------------
    // RE-REJECT → choose 4 unique NPCs to kill
    // ----------------------------------------------------
    public void OnReReject()
    {
        if (killTargetList.Count == 0)
        {
            Debug.LogWarning("[Nolgae] RE-REJECT but KillTargetList is EMPTY");
            return;
        }

        List<string> selected = SelectFourUnique();

        pendingKill.AddRange(selected);

        Debug.Log($"[Nolgae] RE-REJECT → Selected {selected.Count} NPCs to DIE tonight");

        foreach (var s in selected)
            Debug.Log($"[Nolgae]  • Marked for death: {s}");
    }

    // ----------------------------------------------------
    // Pick 4 unique random NPCs (or fewer if not enough)
    // ----------------------------------------------------
    private List<string> SelectFourUnique()
    {
        List<string> copy = new List<string>(killTargetList);
        List<string> result = new List<string>();

        int count = Mathf.Min(4, copy.Count);

        for (int i = 0; i < count; i++)
        {
            int index = Random.Range(0, copy.Count);
            result.Add(copy[index]);
            copy.RemoveAt(index);
        }

        return result;
    }

    // ----------------------------------------------------
    // NIGHT EFFECTS — executed by NighttimeManager
    // ----------------------------------------------------
    public void ApplyNightEffects()
    {
        NPCStateManager sm = NPCStateManager.Instance;

        if (sm == null)
        {
            Debug.LogError("[Nolgae] NPCStateManager missing!");
            return;
        }

        // SICK EFFECT
        foreach (string npc in pendingSick)
        {
            sm.MarkNeedMedicineTomorrow(npc);
            Debug.Log($"[Nolgae] NIGHT → {npc} will need medicine tomorrow.");
        }

        // KILL EFFECT
        foreach (string npc in pendingKill)
        {
            var st = sm.GetOrCreateState(npc);
            st.isDead = true;

            Debug.Log($"[Nolgae] NIGHT → {npc} has been KILLED by Nolgae!");
        }

        pendingSick.Clear();
        pendingKill.Clear();
    }
}



