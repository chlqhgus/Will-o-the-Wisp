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
                instance = FindFirstObjectByType<DuakEffect>();
            return instance;
        }
    }

    public void ApplyDuak(string duakName)
    {
        List<string> npcList = NPCStateManager.Instance.GetAllNPCNames();
        if (npcList == null || npcList.Count == 0) return;

        npcList.Remove(duakName);
        if (npcList.Count == 0) return;

        string target = npcList[Random.Range(0, npcList.Count)];

        // FIXED — correct property name
        NPCStateManager.Instance.GetOrCreateState(target).willNeedMedicineTomorrow = true;

        Debug.Log($"[DUAK CURSE] {duakName} cursed {target} → needs medicine tomorrow.");
    }
}

