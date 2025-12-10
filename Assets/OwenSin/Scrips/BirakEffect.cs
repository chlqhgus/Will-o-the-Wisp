using System.Collections.Generic;
using UnityEngine;

public class BirakEffect : MonoBehaviour
{
    private static BirakEffect instance;
    public static BirakEffect Instance
    {
        get
        {
            if (instance == null)
                instance = FindFirstObjectByType<BirakEffect>();
            return instance;
        }
    }

    [Header("Birak makes the NPC BEHIND the current one sick tomorrow")]
    public List<string> birakPossibleTargets = new List<string>();

    private List<string> pendingSick = new List<string>();


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

    // ===========================================================
    // Called when Re-Reject occurs (Birak is the current NPC)
    // ===========================================================
    public void ApplyBirak(string npcBehind)
    {
        if (string.IsNullOrEmpty(npcBehind))
        {
            Debug.LogWarning("[Birak] npcBehind is NULL or EMPTY.");
            return;
        }

        pendingSick.Add(npcBehind);

        Debug.Log($"[Birak] Marked {npcBehind} to become sick tomorrow.");
    }

    // ===========================================================
    // Apply at NIGHT
    // ===========================================================
    public void ApplyNightEffects()
    {
        if (pendingSick.Count == 0)
            return;

        foreach (string npc in pendingSick)
        {
            NPCStateManager.Instance.MarkNeedMedicineTomorrow(npc);
            Debug.Log($"[Birak] NIGHT: {npc} will need medicine tomorrow.");
        }

        pendingSick.Clear();
    }
}



