using UnityEngine;
using TMPro;

public class Inventory : MonoBehaviour
{
    public int lotusRice = 40;
    public int herbalMedicine = 10;

    public TextMeshProUGUI riceText;       // correct name
    public TextMeshProUGUI medicineText;   // correct name

    void Start()
    {
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

    public void AddLotus(int n)
    {
        lotusRice += n;
        Refresh();
    }

    public void AddHerb(int n)
    {
        herbalMedicine += n;
        Refresh();
    }

    void Refresh()
    {
        if (riceText != null)
            riceText.text = "X" + lotusRice;       // FIXED

        if (medicineText != null)
            medicineText.text = "X" + herbalMedicine; // FIXED
    }
}

