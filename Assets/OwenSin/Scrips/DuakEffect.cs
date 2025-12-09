using System.Collections.Generic;
using UnityEngine;

public class DuakEffect : MonoBehaviour
{
    private static DuakEffect instance;
    public static DuakEffect Instance
    {
        get
        {
            if (instance == null)
                instance = FindFirstObjectByType<DuakEffect>();
            return instance;
        }
    }

    [Header("Custom Duak Curse Target List (editable in Inspector)")]
    [Tooltip("Enter the names of NPCs who can be cursed by Duak.")]
    public List<string> duakTargetList = new List<string>();

    // NPCs chosen today to receive curse tomorrow
    private List<string> duakChosenTargets = new List<string>();


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


    // ===============================
    // Called when DUAK re-reject happens
    // ===============================
    public void ApplyDuakCurse()
    {
        Debug.Log("[DUAK] Re-reject detected → selecting target from custom list.");

        if (duakTargetList == null || duakTargetList.Count == 0)
        {
            Debug.LogWarning("[DUAK] Target list is EMPTY. No curse applied.");
            return;
        }

        // Pick a random NPC from your custom list
        string target = duakTargetList[Random.Range(0, duakTargetList.Count)];

        duakChosenTargets.Add(target);

        Debug.Log($"[DUAK] Target selected: {target}");
    }


    // ===============================
    // Apply curse at NIGHT
    // ===============================
    public void ApplyNightEffects()
    {
        if (duakChosenTargets.Count == 0) return;

        Debug.Log($"[DUAK] Applying {duakChosenTargets.Count} curses for tomorrow.");

        foreach (string npc in duakChosenTargets)
        {
            NPCStateManager.Instance.MarkNeedMedicineTomorrow(npc);
            Debug.Log($"[DUAK] {npc} will need medicine tomorrow.");
        }

        duakChosenTargets.Clear();
    }
}


