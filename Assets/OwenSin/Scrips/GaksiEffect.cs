using UnityEngine;

public class GaksiEffect : MonoBehaviour
{
    private static GaksiEffect instance;
    public static GaksiEffect Instance
    {
        get
        {
            if (instance == null)
                instance = FindFirstObjectByType<GaksiEffect>();
            return instance;
        }
    }

    private int pendingMedicineGain = 0;
    private int pendingRiceLoss = 0;

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

    // RE-ACCEPT = gain 1 medicine at night
    public void OnReAccept()
    {
        pendingMedicineGain += 1;
        Debug.Log("[Gaksi] RE-ACCEPT → +1 medicine tonight");
    }

    // RE-REJECT = lose 2 rice at night
    public void OnReReject()
    {
        pendingRiceLoss += 2;
        Debug.Log("[Gaksi] RE-REJECT → -2 rice tonight");
    }

    // Apply resource changes at night
    public void ApplyNightEffects()
    {
        Inventory inv = Inventory.Instance;
        if (inv == null)
        {
            Debug.LogWarning("[Gaksi] Inventory not found.");
            return;
        }

        // Gain medicine
        if (pendingMedicineGain > 0)
        {
            inv.AddHerb(pendingMedicineGain);
            Debug.Log($"[Gaksi] NIGHT: +{pendingMedicineGain} medicine");
        }

        // Lose rice
        if (pendingRiceLoss > 0)
        {
            int loss = Mathf.Min(inv.lotusRice, pendingRiceLoss);
            inv.lotusRice -= loss;

            Debug.Log($"[Gaksi] NIGHT: -{loss} rice");
        }

        pendingMedicineGain = 0;
        pendingRiceLoss = 0;
    }
}


