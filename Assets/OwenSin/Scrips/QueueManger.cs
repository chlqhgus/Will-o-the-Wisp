using System.Collections.Generic;
using UnityEngine;

public class QueueManager : MonoBehaviour
{
    public Transform spawnPoint;
    public Transform[] queueSlots; // slot0 = center
    public GameObject npcPrefab;

    public float spawnInterval = 3f;

    private List<NPC> activeNPCs = new List<NPC>();
    private float timer = 0f;

    private string[] testLines = {
        "I am hungry… please help.",
        "The night was dangerous…",
        "Do you have medicine?",
        "I heard goblins nearby…"
    };

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
        UpdateSpeechBubble();
    }

    // -------------------------------------------------------------------
    // Spawn
    // -------------------------------------------------------------------
    void SpawnNPC()
    {
        GameObject obj = Instantiate(npcPrefab, spawnPoint.position, Quaternion.identity);
        NPC npc = obj.GetComponent<NPC>();
        activeNPCs.Add(npc);
    }

    // -------------------------------------------------------------------
    // Assign movement target for each NPC
    // -------------------------------------------------------------------
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
                Vector3 waitPos =
                    queueSlots[queueSlots.Length - 1].position +
                    new Vector3(3f * (i - 3), 0, 0);

                activeNPCs[i].SetTarget(waitPos);
            }
        }
    }

    // -------------------------------------------------------------------
    // Sorting order
    // -------------------------------------------------------------------
    void UpdateSortingOrders()
    {
        int baseOrder = 100;

        for (int i = 0; i < activeNPCs.Count; i++)
        {
            int order = baseOrder - i;
            activeNPCs[i].SetSortingOrder(order);
        }
    }

    // -------------------------------------------------------------------
    // NEW — Speech Bubble Logic
    // -------------------------------------------------------------------
    void UpdateSpeechBubble()
    {
        for (int i = 0; i < activeNPCs.Count; i++)
        {
            NPC npc = activeNPCs[i];

            if (i == 0)
            {
                // FRONT NPC shows bubble
                if (!npc.bubbleBG.activeSelf)
                {
                    string randomLine = testLines[Random.Range(0, testLines.Length)];
                    npc.ShowBubble(randomLine);
                }
            }
            else
            {
                // Others hide bubble
                npc.HideBubble();
            }
        }
    }

    // -------------------------------------------------------------------
    // Refuse / Accept
    // -------------------------------------------------------------------
    public Inventory inventory;

    public void RefuseFrontNPC()
    {
        if (activeNPCs.Count == 0) return;

        NPC front = activeNPCs[0];
        activeNPCs.RemoveAt(0);

        front.LeaveScene();
    }

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


