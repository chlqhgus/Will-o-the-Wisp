using UnityEngine;

[CreateAssetMenu(fileName = "DaySchedule", menuName = "Game/Day Schedule")]
public class DaySchedule : ScriptableObject
{
    [Header("Day Information")]
    public int dayNumber;
    
    [Header("NPC Prefabs in Order (순서대로 등장할 NPC들)")]
    public GameObject[] npcPrefabs;
    
    [Header("Spawn Settings")]
    public float spawnInterval = 3f; // 각 NPC 사이의 스폰 간격
}

