using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Day와 남은 NPC 수를 표시하는 UI 스크립트
/// </summary>
public class DayNPCStatusDisplay : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Day를 표시할 TextMeshProUGUI 컴포넌트")]
    public TextMeshProUGUI dayText;
    
    [Tooltip("남은 NPC 수를 표시할 TextMeshProUGUI 컴포넌트")]
    public TextMeshProUGUI remainingNPCText;
    
    [Header("References")]
    [Tooltip("NPCQueueSystem 참조")]
    public NPCQueueSystem npcQueueSystem;
    
    [Header("Display Settings")]
    [Tooltip("Day 텍스트 포맷 (예: \"Day {0}\")")]
    public string dayFormat = "Day {0}";
    
    [Tooltip("남은 NPC 수 텍스트 포맷 (예: \"{0}명 남음\")")]
    public string remainingNPCFormat = "{0}명 남음";
    
    [Tooltip("업데이트 주기 (초 단위, 0이면 매 프레임 업데이트)")]
    public float updateInterval = 0.1f;
    
    private float updateTimer = 0f;
    
    void Start()
    {
        // NPCQueueSystem이 Inspector에서 할당되었는지 확인
        if (npcQueueSystem == null)
        {
            Debug.LogError("DayNPCStatusDisplay: NPCQueueSystem이 Inspector에서 할당되지 않았습니다. Inspector에서 NPCQueueSystem을 할당해주세요.");
            return;
        }
        
        // 초기 업데이트
        UpdateDisplay();
    }
    
    void OnEnable()
    {
        // NPCQueueSystem이 할당되었는지 확인
        if (npcQueueSystem == null)
        {
            Debug.LogWarning("DayNPCStatusDisplay: NPCQueueSystem이 할당되지 않았습니다. UI가 업데이트되지 않습니다.");
            return;
        }
        
        // 초기 업데이트
        UpdateDisplay();
    }
    
    void Update()
    {
        // 업데이트 주기가 설정되어 있으면 타이머 사용
        if (updateInterval > 0f)
        {
            updateTimer += Time.deltaTime;
            if (updateTimer >= updateInterval)
            {
                updateTimer = 0f;
                UpdateDisplay();
            }
        }
        else
        {
            // 매 프레임 업데이트
            UpdateDisplay();
        }
    }
    
    /// <summary>
    /// UI 표시를 업데이트합니다.
    /// </summary>
    void UpdateDisplay()
    {
        // Day 표시
        UpdateDayText();
        
        // 남은 NPC 수 표시
        UpdateRemainingNPCText();
    }
    
    /// <summary>
    /// Day 텍스트를 업데이트합니다.
    /// </summary>
    void UpdateDayText()
    {
        if (dayText == null) return;
        
        // DayManager에서 현재 Day 가져오기
        int currentDay = 1;
        if (DayManager.Instance != null)
        {
            currentDay = DayManager.Instance.GetCurrentDay();
        }
        
        // 텍스트 업데이트
        dayText.text = string.Format(dayFormat, currentDay);
    }
    
    /// <summary>
    /// 남은 NPC 수 텍스트를 업데이트합니다 (아직 상호작용하지 않은 NPC 수).
    /// </summary>
    void UpdateRemainingNPCText()
    {
        if (remainingNPCText == null || npcQueueSystem == null) return;
        
        // 남은 NPC 수 계산 (아직 상호작용하지 않은 NPC 수)
        int remainingCount = GetRemainingNPCCount();
        
        // 텍스트 업데이트
        remainingNPCText.text = string.Format(remainingNPCFormat, remainingCount);
    }
    
    /// <summary>
    /// 남은 NPC 수를 계산합니다 (아직 상호작용하지 않은 NPC 수).
    /// </summary>
    int GetRemainingNPCCount()
    {
        if (npcQueueSystem == null) return 0;
        
        // 전체 NPC 수 (랜덤 큐에서 가져오기)
        int totalNPCs = npcQueueSystem.GetTotalNPCCount();
        
        if (totalNPCs == 0)
        {
            return 0;
        }
        
        // 현재 스폰 인덱스 (이미 스폰된 NPC 수)
        int spawnedCount = npcQueueSystem.GetCurrentSpawnIndex();
        
        // 현재 큐에 있는 NPC 수 (아직 상호작용하지 않은 NPC)
        int activeCount = npcQueueSystem.GetActiveNPCCount();
        
        // 상호작용이 완료된 NPC 수 = 스폰된 수 - 현재 큐에 있는 수
        int completedCount = spawnedCount - activeCount;
        
        // 남은 NPC 수 = 전체 - 상호작용 완료된 수
        // 또는 = 현재 큐에 있는 수 + 아직 스폰 안 된 수
        int remaining = totalNPCs - completedCount;
        
        // 음수가 되지 않도록 보정
        return Mathf.Max(0, remaining);
    }
}

