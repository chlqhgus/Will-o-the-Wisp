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

    [Header("NPC list for RE-REJECT → becomes sick tomorrow")]
    public List<string> sickTargetList = new List<string>();

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

    // RE-ACCEPT = no effect
    public void OnReAccept()
    {
        Debug.Log("[Duak] RE-ACCEPT → No effect");
    }

    // RE-REJECT = choose 1 random NPC → sick tomorrow
    public void OnReReject()
    {
        if (sickTargetList.Count == 0)
        {
            Debug.LogWarning("[Duak] Sick target list is EMPTY.");
            return;
        }

        string target = sickTargetList[Random.Range(0, sickTargetList.Count)];
        pendingSick.Add(target);

        Debug.Log($"[Duak] RE-REJECT → {target} will be SICK tomorrow.");
    }

    // Apply effect at night
    public void ApplyNightEffects()
    {
        foreach (string npc in pendingSick)
        {
            NPCStateManager.Instance.MarkNeedMedicineTomorrow(npc);
            Debug.Log($"[Duak] NIGHT: {npc} set to NEED MEDICINE tomorrow.");
        }

        pendingSick.Clear();
    }
}



