using UnityEngine;
using System.Collections.Generic;

public class NolgaeEffect : MonoBehaviour
{
    private static NolgaeEffect instance;
    public static NolgaeEffect Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<NolgaeEffect>();
                if (instance == null)
                {
                    GameObject go = new GameObject("NolgaeEffect");
                    instance = go.AddComponent<NolgaeEffect>();
                }
            }
            return instance;
        }
    }

    [Header("NPCs Nolgae makes sick (Re-Accept)")]
    public List<string> sicknessTargets = new List<string>();

    [Header("NPCs Nolgae will kill (Re-Reject)")]
    public List<string> killTargets = new List<string>();

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

    // ▶ Re-Accept → choose 1 → force medicine tomorrow
    public void OnReAccept()
    {
        if (sicknessTargets.Count == 0) return;

        string npc = sicknessTargets[Random.Range(0, sicknessTargets.Count)];
        NPCStateManager.Instance.GetOrCreateState(npc).forceMedicineTomorrow = true;

        Debug.Log($"[Nolgae] (ReAccept) {npc} will require medicine tomorrow.");
    }

    // ▶ Re-Reject → kill 4
    public void OnReReject()
    {
        NPCStateManager SM = NPCStateManager.Instance;

        int count = Mathf.Min(4, killTargets.Count);

        for (int i = 0; i < count; i++)
        {
            string npc = killTargets[i];
            SM.GetOrCreateState(npc).isDead = true;

            Debug.Log($"[Nolgae] KILLED: {npc}");
        }
    }
}


