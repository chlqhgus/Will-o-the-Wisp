using UnityEngine;

/// <summary>
/// 날짜를 관리하는 매니저
/// </summary>
public class DayManager : MonoBehaviour
{
    private static DayManager instance;
    public static DayManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<DayManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("DayManager");
                    instance = go.AddComponent<DayManager>();
                }
            }
            return instance;
        }
    }

    private int currentDay = 1;

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

    public int GetCurrentDay()
    {
        return currentDay;
    }

    public void NextDay()
    {
        currentDay++;
        Debug.Log($"Day {currentDay} 시작");
    }

    public void ResetDay()
    {
        currentDay = 1;
        NPCStateManager.Instance.ResetAllStates();
    }
}

