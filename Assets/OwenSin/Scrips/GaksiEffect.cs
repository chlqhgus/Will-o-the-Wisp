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

    private bool acceptedToday = false;
    private bool reRefusedToday = false;

    public void OnAcceptGaksi()
    {
        acceptedToday = true;
        reRefusedToday = false;
        Debug.Log("[GAKSI] Accepted → +1 medicine tomorrow.");
    }

    public void OnReRefuseGaksi()
    {
        reRefusedToday = true;
        acceptedToday = false;
        Debug.Log("[GAKSI] Re-refuse → lose 2 rice tonight.");
    }

    public void ApplyNightEffects()
    {
        Inventory inv = Inventory.Instance;
        if (inv == null) return;

        // 1. Accept effect
        if (acceptedToday)
        {
            inv.AddHerb(1);
            Debug.Log("[GAKSI NIGHT] +1 Herbal Medicine.");
        }

        // 2. Re-refuse effect
        if (reRefusedToday)
        {
            inv.lotusRice = Mathf.Max(0, inv.lotusRice - 2);

            // FIXED — Refresh is now public
            inv.Refresh();

            Debug.Log("[GAKSI NIGHT] -2 Lotus Rice.");
        }

        acceptedToday = false;
        reRefusedToday = false;
    }
}

