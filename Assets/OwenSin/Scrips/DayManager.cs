// DayManager.cs
using System.Collections.Generic;
using UnityEngine;

public class DayManager : MonoBehaviour
{
    public int maxDays = 7;

    // Call this when you want to proceed to next day
    public void AdvanceDay()
    {
        var gm = GameManager.I;
        if (gm == null) return;

        List<string> diedIds = new List<string>();

        foreach (var s in gm.persistentStates)
        {
            if (s.isDead) continue;

            // Sickness -> if was sick and did NOT receive medicine today => die
            if (s.isSick && !s.receivedMedicineToday)
            {
                s.isDead = true;
                diedIds.Add(s.prefabID);
                continue;
            }

            // Starvation -> if daysWithoutFood >= 2 => die
            if (s.daysWithoutFood >= 2)
            {
                s.isDead = true;
                diedIds.Add(s.prefabID);
                continue;
            }

            // Reset today's flags
            s.receivedFoodToday = false;
            s.receivedMedicineToday = false;
        }

        gm.currentDay = Mathf.Min(gm.currentDay + 1, maxDays);
        gm.GenerateSpawnOrderFromPrefabs();

        Debug.Log($"Advanced to day {gm.currentDay}. Died: {diedIds.Count}");
    }
}

