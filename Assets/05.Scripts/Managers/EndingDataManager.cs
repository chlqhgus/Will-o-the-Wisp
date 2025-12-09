using UnityEngine;

public class EndingDataManager : MonoBehaviour
{
    private static EndingDataManager instance;
    public static EndingDataManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<EndingDataManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("EndingDataManager");
                    instance = go.AddComponent<EndingDataManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    public enum EndingType
    {
        Good,
        Normal,
        Bad,
        Dead
    }

    private EndingType currentEnding = EndingType.Good;

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

    public void SetEndingType(EndingType endingType)
    {
        currentEnding = endingType;
    }

    public EndingType GetEndingType()
    {
        return currentEnding;
    }

    public void Reset()
    {
        currentEnding = EndingType.Good;
    }
}

