using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class BohyunMenuManager : MonoBehaviour
{
    [Header("Button References")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private string gameSceneName = "GameScene"; // 게임 씬 이름
    
    [Header("UI References")]
    [SerializeField] private RectTransform titleRectTransform; // Title RectTransform
    [SerializeField] private TextMeshProUGUI titleText; // Title 텍스트
    
    [Header("Animation Settings")]
    [SerializeField] private float titleFadeInDuration = 1f; // Title 페이드인 시간
    [SerializeField] private float buttonFadeInDuration = 0.5f; // 버튼 페이드인 시간
    [SerializeField] private float buttonFadeInDelay = 0.5f; // Title 애니메이션 후 버튼 페이드인 딜레이
    [SerializeField] private float titleStartOffsetY = 100f; // Title 시작 위치 오프셋
    
    [Header("Fade Settings")]
    [SerializeField] private float fadeOutDuration = 1f; // 씬 전환 페이드아웃 시간
    [SerializeField] private Color fadeColor = Color.black; // 페이드 색상
    
    private Vector2 titleOriginalPosition;
    private Color titleOriginalColor;
    private List<Color> buttonOriginalColors = new List<Color>();
    private List<Graphic> buttonGraphics = new List<Graphic>();
    private Image fadeOverlay;
    private bool isTransitioning = false;
    
    void Start()
    {
        InitializeUI();
        SetupButtons();
    }
    
    private void InitializeUI()
    {
        // Title 초기 상태 설정
        if (titleRectTransform != null)
        {
            titleOriginalPosition = titleRectTransform.anchoredPosition;
            Vector2 startPos = titleOriginalPosition;
            startPos.y = titleOriginalPosition.y - titleStartOffsetY;
            titleRectTransform.anchoredPosition = startPos;
        }
        
        if (titleText != null)
        {
            titleOriginalColor = titleText.color;
            Color startColor = titleOriginalColor;
            startColor.a = 0f;
            titleText.color = startColor;
        }
        
        // 버튼 초기 상태 설정 (투명하게)
        buttonGraphics.Clear();
        buttonOriginalColors.Clear();
        
        if (startButton != null)
        {
            SetupButtonTransparency(startButton);
            startButton.interactable = false;
        }
        
        if (exitButton != null)
        {
            SetupButtonTransparency(exitButton);
            exitButton.interactable = false;
        }
        
        // 페이드아웃 오버레이 생성
        CreateFadeOverlay();
    }
    
    private void SetupButtonTransparency(Button button)
    {
        // Button의 Image 컴포넌트
        Graphic graphic = button.targetGraphic;
        if (graphic != null)
        {
            buttonGraphics.Add(graphic);
            buttonOriginalColors.Add(graphic.color);
            Color startColor = graphic.color;
            startColor.a = 0f;
            graphic.color = startColor;
        }
        
        // Button의 TextMeshProUGUI
        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonGraphics.Add(buttonText);
            buttonOriginalColors.Add(buttonText.color);
            Color startColor = buttonText.color;
            startColor.a = 0f;
            buttonText.color = startColor;
        }
    }
    
    private void SetupButtons()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartButtonClicked);
        }
        
        if (exitButton != null)
        {
            exitButton.onClick.AddListener(OnExitButtonClicked);
        }
    }
    
    // 대화가 끝난 후 호출할 메서드
    public void ShowMenu()
    {
        StartCoroutine(ShowMenuSequence());
    }
    
    private IEnumerator ShowMenuSequence()
    {
        // 1. Title 페이드인 및 슬라이드
        if (titleRectTransform != null && titleText != null)
        {
            yield return StartCoroutine(FadeInTitle());
        }
        
        // 2. 버튼 페이드인 딜레이
        yield return new WaitForSeconds(buttonFadeInDelay);
        
        // 3. 버튼 페이드인
        yield return StartCoroutine(FadeInButtons());
    }
    
    private IEnumerator FadeInTitle()
    {
        if (titleRectTransform == null || titleText == null) yield break;
        
        float elapsedTime = 0f;
        float startY = titleOriginalPosition.y - titleStartOffsetY;
        float targetY = titleOriginalPosition.y;
        
        while (elapsedTime < titleFadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / titleFadeInDuration);
            
            // Smooth step
            t = t * t * (3f - 2f * t);
            
            // 위치 업데이트
            Vector2 currentPos = titleRectTransform.anchoredPosition;
            currentPos.y = Mathf.Lerp(startY, targetY, t);
            titleRectTransform.anchoredPosition = currentPos;
            
            // 알파 업데이트
            Color currentColor = titleText.color;
            currentColor.a = Mathf.Lerp(0f, titleOriginalColor.a, t);
            titleText.color = currentColor;
            
            yield return null;
        }
        
        // 최종 값 설정
        titleRectTransform.anchoredPosition = titleOriginalPosition;
        titleText.color = titleOriginalColor;
    }
    
    private IEnumerator FadeInButtons()
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < buttonFadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / buttonFadeInDuration);
            
            // Smooth step
            t = t * t * (3f - 2f * t);
            
            // 모든 버튼 그래픽 업데이트
            for (int i = 0; i < buttonGraphics.Count && i < buttonOriginalColors.Count; i++)
            {
                if (buttonGraphics[i] != null)
                {
                    Color currentColor = buttonOriginalColors[i];
                    currentColor.a = Mathf.Lerp(0f, buttonOriginalColors[i].a, t);
                    buttonGraphics[i].color = currentColor;
                }
            }
            
            yield return null;
        }
        
        // 최종 값 설정 및 버튼 활성화
        for (int i = 0; i < buttonGraphics.Count && i < buttonOriginalColors.Count; i++)
        {
            if (buttonGraphics[i] != null)
            {
                buttonGraphics[i].color = buttonOriginalColors[i];
            }
        }
        
        // 버튼 상호작용 활성화
        if (startButton != null)
        {
            startButton.interactable = true;
        }
        
        if (exitButton != null)
        {
            exitButton.interactable = true;
        }
    }
    
    private void CreateFadeOverlay()
    {
        // Canvas 찾기
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("BohyunMenuManager: Canvas not found!");
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
            float t = Mathf.Clamp01(elapsedTime / fadeOutDuration);
            
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
        // 게임 종료
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
    }
}

