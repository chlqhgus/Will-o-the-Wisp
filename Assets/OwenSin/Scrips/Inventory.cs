using UnityEngine;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    public int lotusRice = 40;
    public int herbalMedicine = 25;

    public Text lotusText;
    public Text herbText;

    void Start()
    {
        UpdateUI();
    }

    public void UseLotusRice()
    {
        if (lotusRice > 0)
        {
            lotusRice--;
            UpdateUI();
        }
    }

    public void UseHerbalMedicine()
    {
        if (herbalMedicine > 0)
        {
            herbalMedicine--;
            UpdateUI();
        }
    }

    public void UpdateUI()
    {
        lotusText.text = "x" + lotusRice;
        herbText.text = "x" + herbalMedicine;
    }
}

