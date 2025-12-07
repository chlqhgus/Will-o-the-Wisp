using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Nighttime 구매 시스템만 담당하는 클래스
/// Food와 Medicine 구매를 처리합니다.
/// </summary>
public class NighttimePurchaseSystem : MonoBehaviour
{
    [Header("Purchase Settings")]
    [Tooltip("Food 가격")]
    public int foodPrice = 1;
    
    [Tooltip("Medicine 가격")]
    public int medicinePrice = 1;
    
    [Header("UI References - Buttons")]
    [Tooltip("Food 구매 버튼")]
    public Button buyFoodButton;
    
    [Tooltip("Medicine 구매 버튼")]
    public Button buyMedicineButton;
    
    [Header("UI References - Resources")]
    [Tooltip("총 코인을 표시할 텍스트 (Total coins)")]
    public TextMeshProUGUI totalCoinsText;
    
    [Tooltip("남은 식량을 표시할 텍스트 (Remaining food)")]
    public TextMeshProUGUI remainingFoodText;
    
    [Tooltip("남은 약을 표시할 텍스트 (Remaining medicine)")]
    public TextMeshProUGUI remainingMedicineText;
    
    private Inventory inventory;
    private NighttimeStatisticsCalculator statisticsCalculator;
    private int foodPurchased = 0;
    private int medicinePurchased = 0;
    
    void Start()
    {
        // Inventory 자동으로 찾기
        if (inventory == null)
            inventory = Inventory.Instance;
        
        // StatisticsCalculator 자동으로 찾기
        if (statisticsCalculator == null)
            statisticsCalculator = GetComponent<NighttimeStatisticsCalculator>();
        
        // 버튼 이벤트 설정
        SetupButtons();
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
    }
    
    /// <summary>
    /// 구매 카운터를 리셋합니다.
    /// </summary>
    public void ResetPurchaseCounters()
    {
        foodPurchased = 0;
        medicinePurchased = 0;
    }
    
    /// <summary>
    /// 버튼 상태를 업데이트합니다.
    /// </summary>
    public void UpdateButtons()
    {
        if (statisticsCalculator == null) return;
        
        int maxFood = statisticsCalculator.GetBlessedNPCCount(NPCTypeHelper.NPCType.Merchant);
        int maxMedicine = statisticsCalculator.GetBlessedNPCCount(NPCTypeHelper.NPCType.Physician);
        
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
    /// 자원 UI를 업데이트합니다.
    /// </summary>
    public void UpdateResourceUI()
    {
        if (inventory == null) return;
        
        if (totalCoinsText != null)
            totalCoinsText.text = inventory.money.ToString();
        
        if (remainingFoodText != null)
            remainingFoodText.text = inventory.lotusRice.ToString();
        
        if (remainingMedicineText != null)
            remainingMedicineText.text = inventory.herbalMedicine.ToString();
    }
    
    /// <summary>
    /// Food 구매 버튼 클릭 이벤트
    /// </summary>
    void OnBuyFoodClicked()
    {
        if (statisticsCalculator == null) return;
        
        int maxFood = statisticsCalculator.GetBlessedNPCCount(NPCTypeHelper.NPCType.Merchant);
        
        if (foodPurchased >= maxFood)
        {
            Debug.Log("더 이상 구매할 수 없습니다.");
            return;
        }
        
        if (inventory == null || !inventory.SpendMoney(foodPrice))
        {
            Debug.Log("돈이 부족합니다.");
            return;
        }
        
        foodPurchased++;
        inventory.AddLotus(1);
        UpdateResourceUI();
        UpdateButtons();
    }
    
    /// <summary>
    /// Medicine 구매 버튼 클릭 이벤트
    /// </summary>
    void OnBuyMedicineClicked()
    {
        if (statisticsCalculator == null) return;
        
        int maxMedicine = statisticsCalculator.GetBlessedNPCCount(NPCTypeHelper.NPCType.Physician);
        
        if (medicinePurchased >= maxMedicine)
        {
            Debug.Log("더 이상 구매할 수 없습니다.");
            return;
        }
        
        if (inventory == null || !inventory.SpendMoney(medicinePrice))
        {
            Debug.Log("돈이 부족합니다.");
            return;
        }
        
        medicinePurchased++;
        inventory.AddHerb(1);
        UpdateResourceUI();
        UpdateButtons();
    }
    
    /// <summary>
    /// 돈을 계산하고 추가합니다.
    /// </summary>
    public void CalculateAndAddMoney()
    {
        if (inventory == null || statisticsCalculator == null) return;
        
        var blessedNPCs = statisticsCalculator.GetBlessedNPCs();
        int totalMoney = 0;
        
        foreach (var kvp in blessedNPCs)
        {
            int reward = NPCTypeHelper.GetRewardMoney(kvp.Key);
            totalMoney += reward * kvp.Value;
        }
        
        if (totalMoney > 0)
        {
            inventory.AddMoney(totalMoney);
            Debug.Log($"NighttimePurchaseSystem: 총 {totalMoney}원 추가됨");
            UpdateResourceUI();
        }
    }
}

