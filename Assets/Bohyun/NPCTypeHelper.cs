using UnityEngine;

/// <summary>
/// NPC 타입을 구분하는 헬퍼 클래스
/// </summary>
public static class NPCTypeHelper
{
    /// <summary>
    /// NPC 이름으로 타입을 구분합니다.
    /// </summary>
    public enum NPCType
    {
        King,
        Nobleman,  // Yangban
        Slave,
        Shaman,
        Physician,
        Merchant,
        Other
    }
    
    /// <summary>
    /// NPC 이름으로 타입을 반환합니다.
    /// </summary>
    public static NPCType GetNPCType(string npcName)
    {
        if (string.IsNullOrEmpty(npcName))
            return NPCType.Other;
        
        string nameLower = npcName.ToLower();
        
        if (nameLower.StartsWith("king"))
            return NPCType.King;
        else if (nameLower.StartsWith("yangban") || nameLower.Contains("nobleman"))
            return NPCType.Nobleman;
        else if (nameLower.StartsWith("slave"))
            return NPCType.Slave;
        else if (nameLower.StartsWith("shaman"))
            return NPCType.Shaman;
        else if (nameLower.StartsWith("physician"))
            return NPCType.Physician;
        else if (nameLower.StartsWith("merchant"))
            return NPCType.Merchant;
        else
            return NPCType.Other;
    }
    
    /// <summary>
    /// NPC가 살아남았을 때 주는 돈을 반환합니다.
    /// </summary>
    public static int GetRewardMoney(NPCType type)
    {
        switch (type)
        {
            case NPCType.King:
                return 2;
            case NPCType.Nobleman:
                return 1;
            case NPCType.Slave:
            case NPCType.Shaman:
            case NPCType.Physician:
            case NPCType.Merchant:
            case NPCType.Other:
            default:
                return 0;
        }
    }
}

