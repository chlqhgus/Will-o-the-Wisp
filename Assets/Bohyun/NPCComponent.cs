using UnityEngine;

/// <summary>
/// NPC GameObject에 BohyunNPCData를 저장하는 컴포넌트
/// OwenSin의 NPC 컴포넌트는 수정하지 않고 별도로 추가
/// </summary>
public class NPCComponent : MonoBehaviour
{
    [Header("Bohyun NPC Data")]
    public BohyunNPCData bohyunData;
    
    public string GetNPCName()
    {
        if (bohyunData != null && !string.IsNullOrEmpty(bohyunData.npcName))
            return bohyunData.npcName;
        return gameObject.name.Replace("(Clone)", "").Trim();
    }

    public bool DetermineRequestType()
    {
        if (bohyunData == null) return false;
        
        float randomValue = Random.value;
        float totalProbability = bohyunData.foodRequestProbability + bohyunData.medicineRequestProbability;
        
        if (totalProbability <= 0f) return false;
        
        float normalizedMedicineProb = bohyunData.medicineRequestProbability / totalProbability;
        return randomValue < normalizedMedicineProb;
    }
}

