using UnityEngine;
using System.Collections.Generic;

public class DuakEffect : MonoBehaviour
{
    private static DuakEffect instance;
    public static DuakEffect Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<DuakEffect>();
                if (instance == null)
                {
                    GameObject go = new GameObject("DuakEffect");
                    instance = go.AddComponent<DuakEffect>();
                }
            }
            return instance;
        }
    }

    [Header("NPCs Duak can target (player editable)")]
    public List<string> targetNPCs = new List<string>();

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

    // ▶ Re-Reject → choose 1 random NPC FROM PLAYER LIST → force medicine tomorrow
    public void OnReReject()
    {
        if (targetNPCs.Count == 0)
        {
            Debug.LogWarning("[Duak] targetNPCs list is EMPTY — nothing to choose.");
            return;
        }

        string npc = targetNPCs[Random.Range(0, targetNPCs.Count)];

        NPCStateManager.Instance.GetOrCreateState(npc).forceMedicineTomorrow = true;

        Debug.Log($"[Duak] Selected {npc} → must request medicine tomorrow.");
    }
}


