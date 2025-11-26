using UnityEngine;

/// <summary>
/// NPC GameObject에 BohyunNPCData를 저장하는 컴포넌트
/// OwenSin의 NPC 컴포넌트는 수정하지 않고 별도로 추가
/// </summary>
public class NPCComponent : MonoBehaviour
{
    [Header("Bohyun NPC Data")]
    public BohyunNPCData bohyunData;
    
    /// <summary>
    /// NPC 이름을 반환합니다 (BohyunNPCData 또는 GameObject 이름).
    /// </summary>
    public string GetNPCName()
    {
        if (bohyunData != null && !string.IsNullOrEmpty(bohyunData.npcName))
            return bohyunData.npcName;
        return gameObject.name.Replace("(Clone)", "").Trim();
    }
    
    /// <summary>
    /// 요청 타입을 반환합니다.
    /// </summary>
    public BohyunNPCRequestType GetRequestType()
    {
        if (bohyunData != null)
            return bohyunData.requestType;
        return BohyunNPCRequestType.Food;
    }
}

