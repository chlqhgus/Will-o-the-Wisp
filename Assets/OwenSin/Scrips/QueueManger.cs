using System.Collections.Generic;
using UnityEngine;

public class QueueManager : MonoBehaviour
{
    public Transform spawnPoint;       // x=13, y=0.14 (off-screen right)
    public Transform[] queueSlots;     // 4 slot transforms at X = 0, 3, 6, 9
    public GameObject npcPrefab;
    public float spawnInterval = 3f;

    private List<NPC> activeNPCs = new List<NPC>();
    private float timer = 0f;

    void Start()
    {
        SpawnNPC();
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            SpawnNPC();
            timer = 0;
        }

        UpdateSortingOrders();
        UpdateQueueTargets();
    }

    // ---------------------------------------------------------------

    void SpawnNPC()
    {
        GameObject obj = Instantiate(npcPrefab, spawnPoint.position, Quaternion.identity);
        NPC npc = obj.GetComponent<NPC>();
        activeNPCs.Add(npc);
    }

    void UpdateQueueTargets()
    {
        for (int i = 0; i < activeNPCs.Count; i++)
        {
            if (i < queueSlots.Length)
            {
                activeNPCs[i].SetTarget(queueSlots[i].position);
            }
            else
            {
                // If more NPCs exist, they wait behind slot 4
                Vector3 waitPos = queueSlots[queueSlots.Length - 1].position + new Vector3(3f * (i - 3), 0, 0);
                activeNPCs[i].SetTarget(waitPos);
            }
        }
    }

    // ---------------------------------------------------------------

    void UpdateSortingOrders()
    {
        // Highest priority = front of queue (100 downward)
        int baseOrder = 100;

        for (int i = 0; i < activeNPCs.Count; i++)
        {
            int order = baseOrder - i;     // 100, 99, 98, 97...
            activeNPCs[i].SetSortingOrder(order);
        }
    }

    // ---------------------------------------------------------------

    public void RemoveFrontNPC()
    {
        if (activeNPCs.Count == 0) return;

        NPC first = activeNPCs[0];
        activeNPCs.RemoveAt(0);
        Destroy(first.gameObject);
    }

    public void RefuseFrontNPC()
    {
        if (activeNPCs.Count == 0) return;

        NPC front = activeNPCs[0];
        activeNPCs.RemoveAt(0);

        // tell the NPC to leave quickly
        front.LeaveScene();

        // queue shifts automatically because UpdateQueueTargets() handles positions
    }

    public Inventory inventory;

    public void GiveLotusRice()
    {
        if (activeNPCs.Count == 0) return;
        if (inventory.lotusRice <= 0) return;

        inventory.UseLotusRice();

        NPC front = activeNPCs[0];
        activeNPCs.RemoveAt(0);

        front.AcceptAndLeave();
    }

    public void GiveHerbalMedicine()
    {
        if (activeNPCs.Count == 0) return;
        if (inventory.herbalMedicine <= 0) return;

        inventory.UseHerbalMedicine();

        NPC front = activeNPCs[0];
        activeNPCs.RemoveAt(0);

        front.AcceptAndLeave();
    }


}


