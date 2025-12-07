using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NighttimeUIManager : MonoBehaviour
{
    [Header("Canvas Management")]
    [Tooltip("메인 게임 Canvas (NPC 큐 표시용)")]
    public Canvas mainGameCanvas;
    
    [Tooltip("Nighttime UI Canvas")]
    public Canvas nighttimeCanvas;
    
    [Header("UI References - Main Info")]
    [Tooltip("현재 날짜를 표시할 텍스트 (DAY 1, DAY 2 등)")]
    public TextMeshProUGUI dayText;
    
    [Tooltip("생존자 수를 표시할 텍스트 (숫자만)")]
    public TextMeshProUGUI survivorsCountText;
    
    [Tooltip("오늘 사망한 사람 수를 표시할 텍스트 (숫자만)")]
    public TextMeshProUGUI deathTollText;
    
    [Tooltip("은혜를 입은 사람 메시지 텍스트")]
    public TextMeshProUGUI blessedMessageText;
    
    [Header("UI References - NPC Counts (숫자만 표시)")]
    [Tooltip("King 수를 표시할 텍스트 (숫자만)")]
    public TextMeshProUGUI kingCountText;
    
    [Tooltip("Nobleman 수를 표시할 텍스트 (숫자만)")]
    public TextMeshProUGUI noblemanCountText;
    
    [Tooltip("Slave 수를 표시할 텍스트 (숫자만)")]
    public TextMeshProUGUI slaveCountText;
    
    [Tooltip("Shaman 수를 표시할 텍스트 (숫자만)")]
    public TextMeshProUGUI shamanCountText;
    
    [Tooltip("Physician 수를 표시할 텍스트 (숫자만)")]
    public TextMeshProUGUI physicianCountText;
    
    [Tooltip("Merchant 수를 표시할 텍스트 (숫자만)")]
    public TextMeshProUGUI merchantCountText;
    
    [Header("UI References - Day Start Button")]
    [Tooltip("Day Start 버튼")]
    public Button dayStartButton;
    
    [Tooltip("Day Start 버튼 텍스트 (자동으로 찾을 수 있음)")]
    private TextMeshProUGUI dayStartButtonText;
    
    [Header("Audio Management")]
    [Tooltip("Day BGM용 AudioSource (SoundManager에 있는 AudioSource)")]
    public AudioSource dayBGMAudioSource;
    
    [Tooltip("Night BGM용 AudioSource (Canvas(Night)에 있는 Audio Source(Night))")]
    public AudioSource nightBGMAudioSource;
    
    private NighttimeStatisticsCalculator statisticsCalculator;
    private NighttimePurchaseSystem purchaseSystem;
    
    void Start()
    {
        // 초기에는 비활성화 상태로 시작 (큐가 비면 활성화됨)
        gameObject.SetActive(false);
        
        // 컴포넌트 자동 찾기
        if (statisticsCalculator == null)
            statisticsCalculator = GetComponent<NighttimeStatisticsCalculator>();
        
        if (purchaseSystem == null)
            purchaseSystem = GetComponent<NighttimePurchaseSystem>();
        
        // Canvas 초기 설정
        SetupCanvases();
        
        // Day Start 버튼 이벤트 설정
        if (dayStartButton != null)
            dayStartButton.onClick.AddListener(OnDayStartClicked);
        
        // Day Start 버튼 텍스트 자동 찾기
        if (dayStartButtonText == null && dayStartButton != null)
        {
            dayStartButtonText = dayStartButton.GetComponentInChildren<TextMeshProUGUI>();
        }
    }
    
    /// <summary>
    /// Canvas를 초기 설정합니다.
    /// </summary>
    void SetupCanvases()
    {
        if (mainGameCanvas != null)
            mainGameCanvas.gameObject.SetActive(true);
        
        if (nighttimeCanvas != null)
            nighttimeCanvas.gameObject.SetActive(false);
    }
    
    public void ShowNighttimeUI()
    {
        // 하루 종료 처리 (사망 처리만 수행, 상태는 리셋하지 않음)
        if (NPCStateManager.Instance != null)
        {
            Debug.Log("[NighttimeUIManager] ShowNighttimeUI() - 하루 종료 처리 시작");
            NPCStateManager.Instance.EndDay();
            Debug.Log("[NighttimeUIManager] ShowNighttimeUI() - 하루 종료 처리 완료");
        }
        
        if (statisticsCalculator != null)
        {
            statisticsCalculator.CalculateStatistics();
        }
        
        // 돈 계산 및 추가 (통계 계산 후)
        if (purchaseSystem != null)
        {
            purchaseSystem.CalculateAndAddMoney();
        }
        
        // Canvas 전환: 메인 게임 Canvas 비활성화, Nighttime Canvas 활성화
        if (mainGameCanvas != null)
            mainGameCanvas.gameObject.SetActive(false);
        if (nighttimeCanvas != null)
            nighttimeCanvas.gameObject.SetActive(true);
        
        // Nighttime UI 활성화
        gameObject.SetActive(true);
        
        // 사운드 전환: Day BGM 뮤트, Night BGM 재생
        SwitchToNighttimeAudio();
        
        // UI 업데이트
        UpdateUI();
        
        // 구매 시스템 버튼 업데이트
        if (purchaseSystem != null)
        {
            purchaseSystem.UpdateButtons();
            purchaseSystem.UpdateResourceUI();
        }
    }
    
    /// <summary>
    /// Nighttime 사운드로 전환합니다 (Day BGM 뮤트, Night BGM 재생).
    /// </summary>
    void SwitchToNighttimeAudio()
    {
        // Day BGM 뮤트
        if (dayBGMAudioSource != null)
        {
            dayBGMAudioSource.volume = 0f;
            Debug.Log("[NighttimeUIManager] Day BGM 뮤트됨");
        }
        
        // Night BGM 재생
        if (nightBGMAudioSource != null)
        {
            // 볼륨 복원 (뮤트 해제)
            nightBGMAudioSource.volume = 1f;
            if (!nightBGMAudioSource.isPlaying)
            {
                nightBGMAudioSource.Play();
                Debug.Log("[NighttimeUIManager] Night BGM 재생 시작");
            }
        }
        else
        {
            Debug.LogWarning("[NighttimeUIManager] Night BGM AudioSource를 찾을 수 없습니다.");
        }
    }
    
    /// <summary>
    /// Day 사운드로 전환합니다 (Night BGM 뮤트, Day BGM 처음부터 재생).
    /// </summary>
    void SwitchToDayAudio()
    {
        // Night BGM 뮤트
        if (nightBGMAudioSource != null)
        {
            nightBGMAudioSource.volume = 0f;
            Debug.Log("[NighttimeUIManager] Night BGM 뮤트됨");
        }
        
        // Day BGM 처음부터 재생
        if (dayBGMAudioSource != null)
        {
            dayBGMAudioSource.Stop();
            dayBGMAudioSource.time = 0f; // 처음부터 재생
            dayBGMAudioSource.volume = 1f; // 볼륨 복원
            dayBGMAudioSource.Play();
            Debug.Log("[NighttimeUIManager] Day BGM 처음부터 재생 시작");
        }
        else
        {
            Debug.LogWarning("[NighttimeUIManager] Day BGM AudioSource를 찾을 수 없습니다.");
        }
    }
    
    /// <summary>
    /// Nighttime UI를 숨깁니다 (다음 날 시작 시 호출됨).
    /// </summary>
    public void HideNighttimeUI()
    {
        // Nighttime UI 비활성화
        gameObject.SetActive(false);
        
        // Canvas 전환: Nighttime Canvas 비활성화, 메인 게임 Canvas 활성화
        if (nighttimeCanvas != null)
            nighttimeCanvas.gameObject.SetActive(false);
        if (mainGameCanvas != null)
            mainGameCanvas.gameObject.SetActive(true);
        
        // 구매 카운터 리셋
        if (purchaseSystem != null)
        {
            purchaseSystem.ResetPurchaseCounters();
        }
        
        // 사운드 전환: Night BGM 뮤트, Day BGM 처음부터 재생
        SwitchToDayAudio();
    }
    
    /// <summary>
    /// UI를 업데이트합니다.
    /// </summary>
    void UpdateUI()
    {
        if (statisticsCalculator == null) return;
        
        if (dayText != null && DayManager.Instance != null)
        {
            int currentDay = DayManager.Instance.GetCurrentDay();
            dayText.text = $"DAY {currentDay}";
        }
        
        // 생존자 수 표시 (숫자만)
        if (survivorsCountText != null)
            survivorsCountText.text = statisticsCalculator.GetTotalSurvivors().ToString();
        
        // 사망자 수 표시 (숫자만)
        if (deathTollText != null)
            deathTollText.text = statisticsCalculator.GetTodayDeathToll().ToString();
        
        // NPC 타입별 수 표시 (숫자만)
        if (kingCountText != null)
            kingCountText.text = statisticsCalculator.GetBlessedNPCCount(NPCTypeHelper.NPCType.King).ToString();
        
        if (noblemanCountText != null)
            noblemanCountText.text = statisticsCalculator.GetBlessedNPCCount(NPCTypeHelper.NPCType.Nobleman).ToString();
        
        if (slaveCountText != null)
            slaveCountText.text = statisticsCalculator.GetBlessedNPCCount(NPCTypeHelper.NPCType.Slave).ToString();
        
        if (shamanCountText != null)
            shamanCountText.text = statisticsCalculator.GetBlessedNPCCount(NPCTypeHelper.NPCType.Shaman).ToString();
        
        if (physicianCountText != null)
            physicianCountText.text = statisticsCalculator.GetBlessedNPCCount(NPCTypeHelper.NPCType.Physician).ToString();
        
        if (merchantCountText != null)
            merchantCountText.text = statisticsCalculator.GetBlessedNPCCount(NPCTypeHelper.NPCType.Merchant).ToString();
        

        if (dayStartButtonText != null && DayManager.Instance != null)
        {
            int currentDay = DayManager.Instance.GetCurrentDay();
            int nextDay = currentDay + 1;
            dayStartButtonText.text = $"Day {nextDay} Start";
            Debug.Log($"[NighttimeUIManager] UpdateUI() - 버튼 텍스트: Day {nextDay} Start (현재 날짜: {currentDay})");
        }
    }
    
    void OnDayStartClicked()
    {
        NPCQueueSystem queueSystem = FindFirstObjectByType<NPCQueueSystem>();
        
        Image fadeOverlay = FindFirstObjectByType<Canvas>()?.GetComponentInChildren<Image>();
        if (fadeOverlay != null && fadeOverlay.name == "FadeOverlay")
        {
            StartCoroutine(FadeInOverlayAndStartNewDay(fadeOverlay, queueSystem));
        }
        else
        {
            HideNighttimeUI();
            StartNewDay(queueSystem);
        }
    }
    
    /// <summary>
    /// 다음 날을 시작합니다.
    /// </summary>
    void StartNewDay(NPCQueueSystem queueSystem)
    {
        // 날짜 증가 (UI 숨긴 후)
        if (DayManager.Instance != null)
        {
            int currentDayBefore = DayManager.Instance.GetCurrentDay();
            DayManager.Instance.NextDay();
            int currentDayAfter = DayManager.Instance.GetCurrentDay();
            Debug.Log($"[NighttimeUIManager] StartNewDay() - 날짜 증가: {currentDayBefore} → {currentDayAfter}");
        }
        
        // 다음 날 큐 생성
        if (queueSystem != null)
        {
            queueSystem.StartNewDay();
        }
    }
    
    /// <summary>
    /// 페이드인 후 다음 날을 시작합니다.
    /// </summary>
    IEnumerator FadeInOverlayAndStartNewDay(Image overlay, NPCQueueSystem queueSystem)
    {
        float elapsedTime = 0f;
        float fadeDuration = 0.5f;
        Color startColor = overlay.color;
        Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
        
        // 페이드인 시작
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeDuration;
            overlay.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }
        
        overlay.color = targetColor;
        
        // 페이드인 완료 후 UI 숨기고 다음 날 시작
        HideNighttimeUI();
        StartNewDay(queueSystem);
    }
    
    /// <summary>
    /// 페이드인 후 Nighttime UI를 숨깁니다.
    /// </summary>
    IEnumerator FadeInOverlayAndHideUI(Image overlay)
    {
        float elapsedTime = 0f;
        float fadeDuration = 0.5f;
        Color startColor = overlay.color;
        Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
        
        // 페이드인 시작
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeDuration;
            overlay.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }
        
        overlay.color = targetColor;
        
        // 페이드인 완료 후 UI 숨기기
        HideNighttimeUI();
    }
    
    /// <summary>
    /// 페이드아웃 오버레이를 페이드인합니다.
    /// </summary>
    IEnumerator FadeInOverlay(Image overlay)
    {
        float elapsedTime = 0f;
        float fadeDuration = 0.5f;
        Color startColor = overlay.color;
        Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
        
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeDuration;
            overlay.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }
        
        overlay.color = targetColor;
    }
}
