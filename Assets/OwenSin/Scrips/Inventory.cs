// Inventory.cs
using UnityEngine;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    public int lotusRice = 40;
    public int herbalMedicine = 10;

    public Text lotusText;    // assign UI Text
    public Text herbText;

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

    void Refresh()
    {
        if (lotusText != null) lotusText.text = "x" + lotusRice;
        if (herbText != null) herbText.text = "x" + herbalMedicine;
    }
}

