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

    [Header("If RE-ACCEPT → randomly make 1 NPC sick (tomorrow)")]
    public List<string> sickTargetList = new List<string>();

    [Header("If RE-REJECT → randomly kill 4 NPCs tonight (unique)")]
    public List<string> killTargetList = new List<string>();

    // pending actions applied at night
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


    // =======================================================
    // RE-ACCEPT → Make 1 random NPC sick tomorrow
    // =======================================================
    public void OnReAccept()
    {
        if (sickTargetList.Count == 0)
        {
            Debug.LogWarning("[Nolgae] Sick target list is EMPTY.");
            return;
        }

        string target = sickTargetList[Random.Range(0, sickTargetList.Count)];
        pendingSick.Add(target);

        Debug.Log($"[Nolgae] RE-ACCEPT → {target} will become SICK tomorrow.");
    }


    // =======================================================
    // RE-REJECT → Kill 4 random NPCs tonight
    // =======================================================
    public void OnReReject()
    {
        if (killTargetList.Count == 0)
        {
            Debug.LogWarning("[Nolgae] Kill target list is EMPTY.");
            return;
        }

        // Maximum 4 kills, but if list < 4, kill as many as available
        int count = Mathf.Min(4, killTargetList.Count);

        // Temporary list to avoid duplicates
        List<string> temp = new List<string>(killTargetList);

        for (int i = 0; i < count; i++)
        {
            int idx = Random.Range(0, temp.Count);
            string chosen = temp[idx];

            pendingKill.Add(chosen);
            temp.RemoveAt(idx);

            Debug.Log($"[Nolgae] RE-REJECT → {chosen} added to TONIGHT kill list.");
        }
    }


    // =======================================================
    // NIGHT EFFECTS (called by NighttimeManager)
    // =======================================================
    public void ApplyNightEffects()
    {
        // Apply sick effects
        foreach (string npc in pendingSick)
        {
            NPCStateManager.Instance.MarkNeedMedicineTomorrow(npc);
            Debug.Log($"[Nolgae] NIGHT: {npc} will require MEDICINE tomorrow.");
        }

        // Apply kills
        foreach (string npc in pendingKill)
        {
            var st = NPCStateManager.Instance.GetOrCreateState(npc);
            st.isDead = true;

            Debug.Log($"[Nolgae] NIGHT: {npc} is now DEAD (bypassed hunger/medicine rules).");
        }

        pendingSick.Clear();
        pendingKill.Clear();
    }
}


