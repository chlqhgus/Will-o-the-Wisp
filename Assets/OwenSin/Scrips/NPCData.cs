// NPCData.cs
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class NPCData
{
    [Header("Identity (edit in prefab)")]
    public string prefabID = "";           // unique string per prefab (set manually)
    public string displayName = "NPC";
    public string npcName = "";            // NPC name (used by BohyunQueueManager)
    public int prefabIndex = 0;            // optional if you map prefabs by index

    [Header("Random Speech Lines")]
    public string[] speechLines = new string[0];  // random lines shown when NPC arrives at front

    [Header("Day1 (first-time) dialogues (multi-sentence lists)")]
    [TextArea(1, 4)] public List<string> arrivalDay1 = new List<string>();
    [TextArea(1, 4)] public List<string> acceptRiceDay1 = new List<string>();
    [TextArea(1, 4)] public List<string> acceptMedDay1 = new List<string>();
    [TextArea(1, 4)] public List<string> rejectRiceDay1 = new List<string>();
    [TextArea(1, 4)] public List<string> rejectMedDay1 = new List<string>();
    [TextArea(1, 4)] public List<string> redemandDay1 = new List<string>();

    [Header("Returning (days 2+) dialogues)")]
    [TextArea(1, 4)] public List<string> arrivalReturning = new List<string>();
    [TextArea(1, 4)] public List<string> acceptRiceReturning = new List<string>();
    [TextArea(1, 4)] public List<string> acceptMedReturning = new List<string>();
    [TextArea(1, 4)] public List<string> rejectRiceReturning = new List<string>();
    [TextArea(1, 4)] public List<string> rejectMedReturning = new List<string>();
    [TextArea(1, 4)] public List<string> redemandReturning = new List<string>();
}

