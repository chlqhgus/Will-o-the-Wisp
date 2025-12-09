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

    public void ApplyBirak(string npcBehind)
    {
        if (string.IsNullOrEmpty(npcBehind))
        {
            Debug.Log("[BIRAK] No NPC behind → no curse.");
            return;
        }

        // FIXED — correct property name
        NPCStateManager.Instance.GetOrCreateState(npcBehind).willNeedMedicineTomorrow = true;

        Debug.Log($"[BIRAK CURSE] {npcBehind} will need medicine tomorrow.");
    }
}


