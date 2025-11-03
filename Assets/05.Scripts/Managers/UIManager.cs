using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI stageText;
    public TextMeshProUGUI movesText;
    public Button startButton;
    public Button resetButton;
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;
    public Button nextStageButton;
    public Button restartButton;
    
    private GameManager gameManager;
    
    void Awake()
    {
        gameManager = FindObjectOfType<GameManager>();
    }
    
    void Start()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(() => gameManager.OnStartButtonClicked());
        }
        
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(() => gameManager.OnResetButtonClicked());
        }
        
        if (nextStageButton != null)
        {
            nextStageButton.onClick.AddListener(() => gameManager.NextStage());
        }
        
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(() => gameManager.RestartCurrentStage());
        }
        
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
        
        UpdateUI();
    }
    
    public void UpdateUI()
    {
        if (gameManager == null) return;
        
        // Stage 표시
        if (stageText != null)
        {
            stageText.text = $"STAGE {gameManager.currentStage}";
        }
        
        // Moves 표시
        if (movesText != null)
        {
            if (gameManager.movesRemaining < 0)
            {
                movesText.text = "MOVES: ∞";
            }
            else
            {
                movesText.text = $"MOVES: {gameManager.movesRemaining}";
            }
        }
        
        // 버튼 상태 업데이트
        if (startButton != null)
        {
            startButton.interactable = (gameManager.CurrentPhase == GamePhase.Preparation);
        }
        
        if (resetButton != null)
        {
            resetButton.interactable = (gameManager.CurrentPhase == GamePhase.Preparation);
        }
    }
    
    public void ShowResult(bool isClear)
    {
        if (resultPanel == null) return;
        
        resultPanel.SetActive(true);
        
        if (resultText != null)
        {
            resultText.text = isClear ? "CLEAR" : "LOSE";
        }
        
        if (nextStageButton != null)
        {
            nextStageButton.gameObject.SetActive(isClear);
        }
        
        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(true);
        }
    }
    
    public void HideResult()
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
    }
}

