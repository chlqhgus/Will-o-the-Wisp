using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Nighttime 통계 계산만 담당하는 클래스
/// 생존자 수, 사망자 수, 도움받은 NPC 수 등을 계산합니다.
/// </summary>
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
        
        totalSurvivors = 0;
        
        foreach (string npcName in allNPCNames)
        {
            if (string.IsNullOrEmpty(npcName)) continue;
            
            bool isDead = stateManager.IsDead(npcName);
            bool receivedFood = stateManager.ReceivedFoodToday(npcName);
            bool receivedMedicine = stateManager.ReceivedMedicineToday(npcName);
            bool receivedHelp = receivedFood || receivedMedicine;
            
            if (!isDead && receivedHelp)
            {
                totalSurvivors++;
                NPCTypeHelper.NPCType type = NPCTypeHelper.GetNPCType(npcName);
                if (blessedNPCs.ContainsKey(type))
                {
                    blessedNPCs[type]++;
                }
            }
        }
        
        Debug.Log($"NighttimeStatisticsCalculator: 총 생존자 수: {totalSurvivors}");
    }
    
    /// <summary>
    /// 오늘 사망한 사람 수를 계산합니다.
    /// </summary>
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

