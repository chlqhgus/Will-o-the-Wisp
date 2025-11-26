using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Nighttime 씬을 관리하는 매니저
/// 밥을 준 사람 수 표시, 돈 계산, 구매 시스템 관리
/// </summary>
public class NighttimeManager : MonoBehaviour
{
    [Header("UI References - Main Info")]
    [Tooltip("생존자 수를 표시할 텍스트 (Number of survivors)")]
    public TextMeshProUGUI survivorsCountText;
    
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
    
    [Header("UI References - Resources")]
    [Tooltip("총 코인을 표시할 텍스트 (Total coins)")]
    public TextMeshProUGUI totalCoinsText;
    
    [Tooltip("남은 식량을 표시할 텍스트 (Remaining food)")]
    public TextMeshProUGUI remainingFoodText;
    
    [Tooltip("남은 약품을 표시할 텍스트 (Remaining medicine)")]
    public TextMeshProUGUI remainingMedicineText;
    
    [Header("Purchase UI")]
    [Tooltip("Food 구매 버튼")]
    public UnityEngine.UI.Button buyFoodButton;
    
    [Tooltip("Medicine 구매 버튼")]
    public UnityEngine.UI.Button buyMedicineButton;
    
    [Tooltip("Day2 Start 버튼")]
    public UnityEngine.UI.Button day2StartButton;
    
    [Header("References")]
    [Tooltip("Inventory 참조 (자동으로 찾음, DontDestroyOnLoad로 설정되어 있어야 함)")]
    public Inventory inventory;
    
    [Header("Settings")]
    [Tooltip("Food 가격")]
    public int foodPrice = 1;
    
    [Tooltip("Medicine 가격")]
    public int medicinePrice = 1;
    
    [Tooltip("메인 씬 이름 (Day2 Start 버튼 클릭 시 이동)")]
    public string mainSceneName = "prototype_bohyun";
    
    private Dictionary<NPCTypeHelper.NPCType, int> blessedNPCs = new Dictionary<NPCTypeHelper.NPCType, int>();
    private int totalSurvivors = 0;
    private int foodPurchased = 0;
    private int medicinePurchased = 0;
    private bool moneyAdded = false; // 돈이 이미 추가되었는지 확인
    
    void Start()
    {
        // Inventory 자동으로 찾기 (DontDestroyOnLoad로 설정되어 있어야 함)
        if (inventory == null)
            inventory = Inventory.Instance;
        
        if (inventory == null)
        {
            Debug.LogWarning("NighttimeManager: Inventory를 찾을 수 없습니다. Inventory가 DontDestroyOnLoad로 설정되어 있는지 확인하세요.");
        }
        
        // 밥을 준 NPC 수 계산
        CalculateBlessedNPCs();
        
        // 돈 계산 및 추가 (한 번만)
        if (!moneyAdded)
        {
            CalculateAndAddMoney();
            moneyAdded = true;
        }
        
        // UI 업데이트
        UpdateUI();
        
        // 버튼 이벤트 설정
        SetupButtons();
    }
    
    /// <summary>
    /// 밥을 준 NPC 수를 계산합니다. (밥을 준 사람만 일할 수 있고 돈을 줄 수 있음)
    /// </summary>
    void CalculateBlessedNPCs()
    {
        // 초기화
        blessedNPCs.Clear();
        foreach (NPCTypeHelper.NPCType type in System.Enum.GetValues(typeof(NPCTypeHelper.NPCType)))
        {
            if (type != NPCTypeHelper.NPCType.Other)
                blessedNPCs[type] = 0;
        }
        
        NPCStateManager stateManager = NPCStateManager.Instance;
        if (stateManager == null)
        {
            Debug.LogWarning("NighttimeManager: NPCStateManager를 찾을 수 없습니다.");
            return;
        }
        
        // NPCStateManager에서 오늘 등장한 모든 NPC 이름 목록 가져오기
        List<string> allNPCNames = stateManager.GetAllNPCNames();
        if (allNPCNames == null || allNPCNames.Count == 0)
        {
            Debug.LogWarning("NighttimeManager: 등장한 NPC 목록을 찾을 수 없습니다.");
            return;
        }
        
        totalSurvivors = 0;
        
        // 모든 NPC 확인
        foreach (string npcName in allNPCNames)
        {
            if (string.IsNullOrEmpty(npcName)) continue;
            
            // 밥을 준 사람만 카운트 (죽지 않았고 밥을 받았어야 함)
            if (!stateManager.IsDead(npcName) && stateManager.ReceivedFoodToday(npcName))
            {
                totalSurvivors++;
                NPCTypeHelper.NPCType type = NPCTypeHelper.GetNPCType(npcName);
                if (blessedNPCs.ContainsKey(type))
                {
                    blessedNPCs[type]++;
                }
            }
        }
    }
    
    /// <summary>
    /// 돈을 계산하고 Inventory에 추가합니다. (한 번만 실행)
    /// </summary>
    void CalculateAndAddMoney()
    {
        int totalMoneyEarned = 0;
        
        foreach (var kvp in blessedNPCs)
        {
            int reward = NPCTypeHelper.GetRewardMoney(kvp.Key);
            totalMoneyEarned += reward * kvp.Value;
        }
        
        // Inventory에 돈 추가 (이미 추가되었을 수도 있으므로 체크 필요)
        // 하지만 매번 Start에서 호출되므로 중복 추가 방지 필요
        // 일단 그냥 추가하되, 실제로는 씬 전환 시 한 번만 추가되어야 함
        if (inventory != null)
        {
            inventory.AddMoney(totalMoneyEarned);
        }
    }
    
    /// <summary>
    /// UI를 업데이트합니다.
    /// </summary>
    void UpdateUI()
    {
        // 생존자 수 표시
        if (survivorsCountText != null)
            survivorsCountText.text = $"Number of survivors : {totalSurvivors}";
        
        // 은혜를 입은 사람 메시지
        if (blessedMessageText != null)
            blessedMessageText.text = "People who have been blessed by you want to help you.";
        
        // NPC 수 표시 (숫자만)
        if (kingCountText != null)
        {
            int count = blessedNPCs.ContainsKey(NPCTypeHelper.NPCType.King) ? blessedNPCs[NPCTypeHelper.NPCType.King] : 0;
            kingCountText.text = count.ToString();
        }
        
        if (noblemanCountText != null)
        {
            int count = blessedNPCs.ContainsKey(NPCTypeHelper.NPCType.Nobleman) ? blessedNPCs[NPCTypeHelper.NPCType.Nobleman] : 0;
            noblemanCountText.text = count.ToString();
        }
        
        if (merchantCountText != null)
        {
            int count = blessedNPCs.ContainsKey(NPCTypeHelper.NPCType.Merchant) ? blessedNPCs[NPCTypeHelper.NPCType.Merchant] : 0;
            merchantCountText.text = count.ToString();
        }
        
        if (physicianCountText != null)
        {
            int count = blessedNPCs.ContainsKey(NPCTypeHelper.NPCType.Physician) ? blessedNPCs[NPCTypeHelper.NPCType.Physician] : 0;
            physicianCountText.text = count.ToString();
        }
        
        if (slaveCountText != null)
        {
            int count = blessedNPCs.ContainsKey(NPCTypeHelper.NPCType.Slave) ? blessedNPCs[NPCTypeHelper.NPCType.Slave] : 0;
            slaveCountText.text = count.ToString();
        }
        
        if (shamanCountText != null)
        {
            int count = blessedNPCs.ContainsKey(NPCTypeHelper.NPCType.Shaman) ? blessedNPCs[NPCTypeHelper.NPCType.Shaman] : 0;
            shamanCountText.text = count.ToString();
        }
        
        // 총 코인 표시 (숫자만)
        if (totalCoinsText != null && inventory != null)
            totalCoinsText.text = inventory.money.ToString();
        
        // 남은 식량 표시 (숫자만)
        if (remainingFoodText != null && inventory != null)
            remainingFoodText.text = inventory.lotusRice.ToString();
        
        // 남은 약품 표시 (숫자만)
        if (remainingMedicineText != null && inventory != null)
            remainingMedicineText.text = inventory.herbalMedicine.ToString();
        
        // 버튼 활성화/비활성화
        UpdateButtons();
    }
    
    /// <summary>
    /// 버튼 이벤트를 설정합니다.
    /// </summary>
    void SetupButtons()
    {
        if (buyFoodButton != null)
            buyFoodButton.onClick.AddListener(OnBuyFoodClicked);
        
        if (buyMedicineButton != null)
            buyMedicineButton.onClick.AddListener(OnBuyMedicineClicked);
        
        if (day2StartButton != null)
            day2StartButton.onClick.AddListener(OnDay2StartClicked);
    }
    
    /// <summary>
    /// 버튼 상태를 업데이트합니다.
    /// </summary>
    void UpdateButtons()
    {
        int maxFood = blessedNPCs.ContainsKey(NPCTypeHelper.NPCType.Merchant) ? blessedNPCs[NPCTypeHelper.NPCType.Merchant] : 0;
        int maxMedicine = blessedNPCs.ContainsKey(NPCTypeHelper.NPCType.Physician) ? blessedNPCs[NPCTypeHelper.NPCType.Physician] : 0;
        
        bool canBuyFood = inventory != null && 
                         inventory.money >= foodPrice && 
                         foodPurchased < maxFood;
        
        bool canBuyMedicine = inventory != null && 
                              inventory.money >= medicinePrice && 
                              medicinePurchased < maxMedicine;
        
        if (buyFoodButton != null)
            buyFoodButton.interactable = canBuyFood;
        
        if (buyMedicineButton != null)
            buyMedicineButton.interactable = canBuyMedicine;
    }
    
    /// <summary>
    /// Food 구매 버튼 클릭 이벤트
    /// </summary>
    void OnBuyFoodClicked()
    {
        int maxFood = blessedNPCs.ContainsKey(NPCTypeHelper.NPCType.Merchant) ? blessedNPCs[NPCTypeHelper.NPCType.Merchant] : 0;
        
        if (foodPurchased >= maxFood)
        {
            Debug.Log("더 이상 Food를 구매할 수 없습니다.");
            return;
        }
        
        if (inventory == null || !inventory.SpendMoney(foodPrice))
        {
            Debug.Log("돈이 부족합니다.");
            return;
        }
        
        foodPurchased++;
        inventory.AddLotus(1); // Food는 lotusRice로 추가
        
        UpdateUI();
    }
    
    /// <summary>
    /// Medicine 구매 버튼 클릭 이벤트
    /// </summary>
    void OnBuyMedicineClicked()
    {
        int maxMedicine = blessedNPCs.ContainsKey(NPCTypeHelper.NPCType.Physician) ? blessedNPCs[NPCTypeHelper.NPCType.Physician] : 0;
        
        if (medicinePurchased >= maxMedicine)
        {
            Debug.Log("더 이상 Medicine을 구매할 수 없습니다.");
            return;
        }
        
        if (inventory == null || !inventory.SpendMoney(medicinePrice))
        {
            Debug.Log("돈이 부족합니다.");
            return;
        }
        
        medicinePurchased++;
        inventory.AddHerb(1); // Medicine은 herbalMedicine으로 추가
        
        UpdateUI();
    }
    
    /// <summary>
    /// Day2 Start 버튼 클릭 이벤트
    /// </summary>
    void OnDay2StartClicked()
    {
        // 다음 날로 넘어가기
        if (DayManager.Instance != null)
        {
            DayManager.Instance.NextDay();
        }
        
        // 메인 씬으로 전환
        if (!string.IsNullOrEmpty(mainSceneName))
        {
            SceneManager.LoadScene(mainSceneName);
        }
        else
        {
            Debug.LogWarning("NighttimeManager: 메인 씬 이름이 설정되지 않았습니다.");
        }
    }
}
