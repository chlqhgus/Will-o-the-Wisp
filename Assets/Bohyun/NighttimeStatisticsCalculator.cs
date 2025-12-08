using System.Collections.Generic;
using UnityEngine;

public class NighttimeStatisticsCalculator : MonoBehaviour
{
    private Dictionary<NPCTypeHelper.NPCType, int> blessedNPCs = new Dictionary<NPCTypeHelper.NPCType, int>();
    private int totalSurvivors = 0;
    private int todayDeathToll = 0;
    
    /// <summary>
    /// 통계를 계산합니다.
    /// </summary>
    public void CalculateStatistics()
    {
        CalculateBlessedNPCs();
        CalculateTodayDeathToll();
    }
    
    /// <summary>
    /// 밥 또는 약을 준 NPC 수를 계산합니다.
    /// </summary>
    void CalculateBlessedNPCs()
    {
        blessedNPCs.Clear();
        foreach (NPCTypeHelper.NPCType type in System.Enum.GetValues(typeof(NPCTypeHelper.NPCType)))
        {
            if (type != NPCTypeHelper.NPCType.Other)
                blessedNPCs[type] = 0;
        }
        
        NPCStateManager stateManager = NPCStateManager.Instance;
        if (stateManager == null)
        {
            Debug.LogWarning("NighttimeStatisticsCalculator: NPCStateManager를 찾을 수 없습니다.");
            return;
        }
        
        List<string> allNPCNames = stateManager.GetAllNPCNames();
        if (allNPCNames == null || allNPCNames.Count == 0)
        {
            Debug.LogWarning("NighttimeStatisticsCalculator: 등장한 NPC 목록을 찾을 수 없습니다.");
            return;
        }
        
        // 총 인원 수
        int totalNPCs = allNPCNames.Count;
        
        // 누적된 죽은 사람 수 계산
        int totalDead = 0;
        foreach (string npcName in allNPCNames)
        {
            if (string.IsNullOrEmpty(npcName)) continue;
            if (stateManager.IsDead(npcName))
            {
                totalDead++;
            }
        }
        
        // 생존자 수 = 총 인원 수 - 누적된 죽은 사람 수
        totalSurvivors = totalNPCs - totalDead;
        
        // 물자를 준 사람 class별 카운트 계산
        foreach (string npcName in allNPCNames)
        {
            if (string.IsNullOrEmpty(npcName)) continue;
            
            bool isDead = stateManager.IsDead(npcName);
            bool receivedFood = stateManager.ReceivedFoodToday(npcName);
            bool receivedMedicine = stateManager.ReceivedMedicineToday(npcName);
            bool receivedHelp = receivedFood || receivedMedicine;
            
            // 죽지 않았고 도움을 받은 NPC만 카운트
            if (!isDead && receivedHelp)
            {
                NPCTypeHelper.NPCType type = NPCTypeHelper.GetNPCType(npcName);
                Debug.Log($"NighttimeStatisticsCalculator: NPC {npcName} - Type: {type}, receivedHelp: {receivedHelp}");
                if (blessedNPCs.ContainsKey(type))
                {
                    blessedNPCs[type]++;
                    Debug.Log($"NighttimeStatisticsCalculator: {type} 카운트 증가: {blessedNPCs[type]}");
                }
                else
                {
                    Debug.LogWarning($"NighttimeStatisticsCalculator: {type} 타입이 blessedNPCs에 없습니다.");
                }
            }
        }
        
        Debug.Log($"NighttimeStatisticsCalculator: 총 인원 수: {totalNPCs}, 누적 사망자 수: {totalDead}, 총 생존자 수: {totalSurvivors}");
        foreach (var kvp in blessedNPCs)
        {
            Debug.Log($"NighttimeStatisticsCalculator: {kvp.Key} - {kvp.Value}명 도움받음");
        }
    }

    void CalculateTodayDeathToll()
    {
        NPCStateManager stateManager = NPCStateManager.Instance;
        if (stateManager == null)
        {
            Debug.LogWarning("NighttimeStatisticsCalculator: NPCStateManager를 찾을 수 없습니다.");
            todayDeathToll = 0;
            return;
        }
        
        List<string> allNPCNames = stateManager.GetAllNPCNames();
        if (allNPCNames == null || allNPCNames.Count == 0)
        {
            Debug.LogWarning("NighttimeStatisticsCalculator: 등장한 NPC 목록을 찾을 수 없습니다.");
            todayDeathToll = 0;
            return;
        }
        
        todayDeathToll = 0;
        
        foreach (string npcName in allNPCNames)
        {
            if (string.IsNullOrEmpty(npcName)) continue;
            
            if (stateManager.IsDead(npcName))
            {
                todayDeathToll++;
            }
        }
        
        Debug.Log($"NighttimeStatisticsCalculator: 오늘 사망한 사람 수: {todayDeathToll}");
    }
    
    // Getter 메서드들
    public int GetTotalSurvivors() => totalSurvivors;
    public int GetTodayDeathToll() => todayDeathToll;
    public Dictionary<NPCTypeHelper.NPCType, int> GetBlessedNPCs() => new Dictionary<NPCTypeHelper.NPCType, int>(blessedNPCs);
    public int GetBlessedNPCCount(NPCTypeHelper.NPCType type) => blessedNPCs.ContainsKey(type) ? blessedNPCs[type] : 0;
}

