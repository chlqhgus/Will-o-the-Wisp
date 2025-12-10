// Inventory.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Inventory : MonoBehaviour
{
    private static Inventory instance;
    public static Inventory Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<Inventory>();
            }
            return instance;
        }
    }

    public int lotusRice = 40;
    public int herbalMedicine = 10;
    public int money = 0; 

    public TextMeshProUGUI lotusText;    // assign UI Text
    public TextMeshProUGUI herbText;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            // 첫 생성 시에만 초기값 설정 (씬 전환 시에는 값 유지)
        }
        else if (instance != this)
        {
            // 이미 인스턴스가 있으면 새로 생성된 것을 파괴 (값은 기존 인스턴스 유지)
            Destroy(gameObject);
        }
        // instance != null && instance == this인 경우는 씬 전환 시 DontDestroyOnLoad로 유지된 경우
        // 이 경우 값은 그대로 유지되므로 초기화하지 않음
    }

    void Start() 
    { 
        Refresh(); 
    }
    
    void OnEnable()
    {
        // 씬이 활성화될 때마다 UI 업데이트 (다음날 이동 시 등)
        Refresh();
    }

    public bool UseLotusRice()
    {
        if (lotusRice <= 0) return false;
        lotusRice--;
        Refresh();
        return true;
    }

    public bool UseHerbalMedicine()
    {
        if (herbalMedicine <= 0) return false;
        herbalMedicine--;
        Refresh();
        return true;
    }

    public void AddLotus(int n) { lotusRice += n; Refresh(); }
    public void AddHerb(int n) { herbalMedicine += n; Refresh(); }
    public void AddMoney(int n) { money += n; Refresh(); }
    
    public bool SpendMoney(int amount)
    {
        if (money < amount) return false;
        money -= amount;
        Refresh();
        return true;
    }

    public void Refresh()
    {
        if (lotusText != null) lotusText.text = "x" + lotusRice;
        if (herbText != null) herbText.text = "x" + herbalMedicine;
        
        // Nighttime UI가 활성화되어 있으면 자원 UI 업데이트
        NighttimePurchaseSystem purchaseSystem = FindFirstObjectByType<NighttimePurchaseSystem>();
        if (purchaseSystem != null)
        {
            purchaseSystem.UpdateResourceUI();
        }
    }
}

