using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class StageData
{
    public int stageNumber;
    public TileType[,] tileLayout = new TileType[4, 4];
    public Vector2Int startPosition;
    public Vector2Int clearPosition;
    public Vector2Int losePosition;
    public int movesLimit = -1; // -1 = 무제한
}

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }
    
    [Header("Stage Data")]
    public List<StageData> stages = new List<StageData>();
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public StageData GetStageData(int stageNumber)
    {
        if (stageNumber > 0 && stageNumber <= stages.Count)
        {
            return stages[stageNumber - 1];
        }
        return null;
    }
    
    public void LoadStage(int stageNumber)
    {
        StageData data = GetStageData(stageNumber);
        if (data == null) return;
        
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null) return;
        
        // 스테이지 데이터 로드
        gameManager.currentStage = stageNumber;
        gameManager.movesRemaining = data.movesLimit;
        
        // 타일 그리드 생성
        CreateTileGrid(data);
    }
    
    private void CreateTileGrid(StageData data)
    {
        // GridGenerator가 직접 처리하도록 변경됨
        // 이 메서드는 더 이상 사용되지 않음
    }
}

