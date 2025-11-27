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
    public int money = 0; // 돈

    public Text lotusText;    // assign UI Text
    public Text herbText;
    public TextMeshProUGUI moneyText; // 돈 표시용 TextMeshProUGUI (선택사항)

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start() { Refresh(); }

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

    void Refresh()
    {
        if (lotusText != null) lotusText.text = "x" + lotusRice;
        if (herbText != null) herbText.text = "x" + herbalMedicine;
        if (moneyText != null) moneyText.text = money.ToString();
    }
}

