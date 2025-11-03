using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Game Settings")]
    public int currentStage = 1;
    public int movesRemaining = -1; // -1 = 무제한
    
    [Header("References")]
    public HumanController humanController;
    public UIManager uiManager;
    public TileController[,] tileGrid = new TileController[4, 4];
    
    private GamePhase currentPhase = GamePhase.Preparation;
    private int initialMoves;
    
    public GamePhase CurrentPhase => currentPhase;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // 씬이 로드되면 스테이지 데이터 로드
        LoadCurrentStage();
        InitializeGame();
    }
    
    private void LoadCurrentStage()
    {
        // PlayerPrefs에서 현재 스테이지 가져오기 (없으면 1)
        int stageToLoad = PlayerPrefs.GetInt("CurrentStage", 1);
        currentStage = stageToLoad;
        
        // StageManager에서 스테이지 데이터 가져오기
        StageManager stageManager = FindObjectOfType<StageManager>();
        if (stageManager != null)
        {
            StageData stageData = stageManager.GetStageData(currentStage);
            if (stageData != null)
            {
                movesRemaining = stageData.movesLimit;
                initialMoves = movesRemaining;
                
                // GridGenerator로 타일 그리드 생성
                GridGenerator gridGenerator = FindObjectOfType<GridGenerator>();
                if (gridGenerator != null)
                {
                    gridGenerator.GenerateGrid(stageData);
                }
            }
        }
        else
        {
            // StageManager가 없으면 기본값 설정
            movesRemaining = -1; // 무제한
            initialMoves = movesRemaining;
        }
    }
    
    public void InitializeGame()
    {
        currentPhase = GamePhase.Preparation;
        
        if (uiManager != null)
        {
            uiManager.UpdateUI();
        }
    }
    
    public void OnTileRotated()
    {
        if (currentPhase == GamePhase.Preparation && movesRemaining > 0)
        {
            movesRemaining--;
            if (uiManager != null)
            {
                uiManager.UpdateUI();
            }
        }
    }
    
    public void OnStartButtonClicked()
    {
        if (currentPhase == GamePhase.Preparation)
        {
            StartExecution();
        }
    }
    
    public void OnResetButtonClicked()
    {
        ResetStage();
    }
    
    private void StartExecution()
    {
        currentPhase = GamePhase.Execution;
        
        if (humanController != null)
        {
            humanController.StartMovement();
        }
        
        if (uiManager != null)
        {
            uiManager.UpdateUI();
        }
    }
    
    public void OnHumanReachedDestination(TileType destinationType)
    {
        currentPhase = GamePhase.Result;
        
        bool isClear = (destinationType == TileType.Clear);
        
        if (uiManager != null)
        {
            uiManager.ShowResult(isClear);
        }
    }
    
    public void OnHumanReachedDeadEnd()
    {
        currentPhase = GamePhase.Result;
        
        if (uiManager != null)
        {
            uiManager.ShowResult(false); // LOSE
        }
    }
    
    public void ResetStage()
    {
        currentPhase = GamePhase.Preparation;
        movesRemaining = initialMoves;
        
        // 모든 타일 초기화
        for (int x = 0; x < 4; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                if (tileGrid[x, y] != null)
                {
                    tileGrid[x, y].ResetRotation();
                }
            }
        }
        
        // 인간 초기화
        if (humanController != null)
        {
            humanController.ResetPosition();
        }
        
        if (uiManager != null)
        {
            uiManager.UpdateUI();
            uiManager.HideResult();
        }
    }
    
    public void NextStage()
    {
        currentStage++;
        PlayerPrefs.SetInt("CurrentStage", currentStage);
        PlayerPrefs.Save();
        
        // 새 스테이지 로드
        LoadCurrentStage();
        ResetStage();
        
        if (uiManager != null)
        {
            uiManager.UpdateUI();
        }
    }
    
    public void RestartCurrentStage()
    {
        ResetStage();
    }
    
    public TileController GetTileAt(int x, int y)
    {
        if (x >= 0 && x < 4 && y >= 0 && y < 4)
        {
            return tileGrid[x, y];
        }
        return null;
    }
}

