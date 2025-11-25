using UnityEngine;

[CreateAssetMenu(fileName = "NPCData", menuName = "Game/NPC Data")]
public class NPCData : ScriptableObject
{
    public string npcName;
    [TextArea(2, 5)]
    public string[] speechLines;
    // Add any extra fields you want later (isGoblin, requiredItem, reward, portrait, etc.)
}



