using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class MenuManager : MonoBehaviour
{
    [Header("Button References")]
    public Button startButton;
    public Button exitButton;
    public Button howtoButton;
    [SerializeField] private string gameSceneName = "GameScene"; // Input GameScene's name
    
    [Header("Panel References")]
    [SerializeField] private GameObject howtoPanel; // Howto 패널 GameObject
    
    [Header("Fade Settings")]
    [SerializeField] private float fadeOutDuration = 1f;
    [SerializeField] private Color fadeColor = Color.black;
    
    private Image fadeOverlay;
    private bool isTransitioning = false;
    
    void Start()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartButtonClicked);
        }
        
        if (exitButton != null)
        {
            exitButton.onClick.AddListener(OnExitButtonClicked);
        }
        
        if (howtoButton != null)
        {
            howtoButton.onClick.AddListener(OnHowtoButtonClicked);
        }
        
        // 초기 상태: Howto 패널 비활성화
        if (howtoPanel != null)
        {
            howtoPanel.SetActive(false);
        }
        
        // Close 버튼 찾기 및 설정
        SetupCloseButton();
        
        // 페이드아웃 오버레이 생성
        CreateFadeOverlay();
    }
    
    private void SetupCloseButton()
    {
        if (howtoPanel == null) return;
        
        // Howto 패널 내부에서 Close 버튼 찾기
        Button closeButton = howtoPanel.GetComponentInChildren<Button>();
        if (closeButton != null)
        {
            // Close 텍스트가 있는 버튼 찾기
            TMPro.TextMeshProUGUI[] texts = howtoPanel.GetComponentsInChildren<TMPro.TextMeshProUGUI>();
            foreach (var text in texts)
            {
                if (text.text.Contains("Close") || text.text.Contains("OK"))
                {
                    Button btn = text.GetComponentInParent<Button>();
                    if (btn != null)
                    {
                        btn.onClick.AddListener(OnCloseButtonClicked);
                        return;
                    }
                }
            }
            
            // Close 텍스트를 찾지 못했으면 첫 번째 버튼 사용
            closeButton.onClick.AddListener(OnCloseButtonClicked);
        }
    }
    
    private void OnHowtoButtonClicked()
    {
        // Howto 패널 표시
        if (howtoPanel != null)
        {
            howtoPanel.SetActive(true);
        }
    }
    
    private void OnCloseButtonClicked()
    {
        // Howto 패널 숨기기
        if (howtoPanel != null)
        {
            howtoPanel.SetActive(false);
        }
    }
    
    private void CreateFadeOverlay()
    {
        // Canvas 찾기
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Canvas not found!");
            return;
        }
        
        // 페이드아웃용 Image GameObject 생성
        GameObject fadeObj = new GameObject("FadeOverlay");
        fadeObj.transform.SetParent(canvas.transform, false);
        
        // RectTransform 설정 (전체 화면 덮기)
        RectTransform rectTransform = fadeObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
        
        // Image 컴포넌트 추가
        fadeOverlay = fadeObj.AddComponent<Image>();
        fadeOverlay.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f); // 초기에는 투명
        fadeOverlay.raycastTarget = false; // 클릭 이벤트 차단하지 않음
        
        // 가장 위에 표시되도록 설정
        fadeObj.transform.SetAsLastSibling();
    }
    
    private void OnStartButtonClicked()
    {
        if (isTransitioning) return; // 이미 전환 중이면 무시
        
        isTransitioning = true;
        StartCoroutine(FadeOutAndLoadScene());
    }
    
    private IEnumerator FadeOutAndLoadScene()
    {
        if (fadeOverlay == null)
        {
            // 오버레이가 없으면 바로 씬 전환
            SceneManager.LoadScene(gameSceneName);
            yield break;
        }
        
        float elapsedTime = 0f;
        Color startColor = fadeOverlay.color;
        Color targetColor = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f);
        
        // 페이드아웃 시작
        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeOutDuration;
            
            // Ease in quadratic
            t = t * t;
            
            fadeOverlay.color = Color.Lerp(startColor, targetColor, t);
            
            yield return null;
        }
        
        // 최종 색상 설정
        fadeOverlay.color = targetColor;
        
        // 씬 전환
        SceneManager.LoadScene(gameSceneName);
    }
    
    private void OnExitButtonClicked()
    {
        // exit game
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    private void OnDestroy()
    {
        // 메모리 누수 방지를 위해 이벤트 제거
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(OnStartButtonClicked);
        }
        
        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(OnExitButtonClicked);
        }
        
        if (howtoButton != null)
        {
            howtoButton.onClick.RemoveListener(OnHowtoButtonClicked);
        }
    }
}

