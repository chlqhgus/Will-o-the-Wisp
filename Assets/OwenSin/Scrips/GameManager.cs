// GameManager.cs
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager I;

    [Header("Unique NPC prefabs (assign in inspector)")]
    public List<GameObject> npcPrefabs = new List<GameObject>();

    // persistent runtime state keyed by prefabID
    public List<PersistentNPCState> persistentStates = new List<PersistentNPCState>();

    [Header("Spawn order (prefabIDs)")]
    public List<string> spawnOrderToday = new List<string>(); // e.g. ["king01","slave03",...]

    [Header("Game day")]
    public int currentDay = 1;
    public int maxDays = 7;

    void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    // get or create persistent state for prefabID
    public PersistentNPCState GetOrCreateState(string prefabID)
    {
        var s = persistentStates.Find(x => x.prefabID == prefabID);
        if (s == null)
        {
            s = new PersistentNPCState(prefabID);
            persistentStates.Add(s);
        }
        return s;
    }

    // helper to check alive
    public bool IsAlive(string prefabID)
    {
        var s = GetOrCreateState(prefabID);
        return !s.isDead;
    }

    // regenerate spawn order using alive states (default ordering: prefabs list order)
    public void GenerateSpawnOrderFromPrefabs()
    {
        spawnOrderToday.Clear();
        foreach (var go in npcPrefabs)
        {
            var data = go.GetComponent<NPCDataHolder>().data;
            if (data == null) continue;
            var state = GetOrCreateState(data.prefabID);
            if (!state.isDead) spawnOrderToday.Add(data.prefabID);
        }
    }
}

