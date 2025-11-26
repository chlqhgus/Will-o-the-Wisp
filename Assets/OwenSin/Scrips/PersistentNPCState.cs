// PersistentNPCState.cs
using System;

[Serializable]
public class PersistentNPCState
{
    public string prefabID;
    public bool isDead;
    public bool isSick;
    public int daysWithoutFood;          // 0,1,2 -> die at 2
    public bool receivedFoodToday;
    public bool receivedMedicineToday;
    public bool metBefore;               // false until first encounter

    public PersistentNPCState() { }

    public PersistentNPCState(string id)
    {
        prefabID = id;
        isDead = false;
        isSick = false;
        daysWithoutFood = 0;
        receivedFoodToday = false;
        receivedMedicineToday = false;
        metBefore = false;
    }
}

