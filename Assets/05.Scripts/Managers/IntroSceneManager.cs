using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class IntroSceneManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject backgroundObject; // 배경 GameObject (SpriteRenderer 사용)
    [SerializeField] private TextMeshProUGUI scenarioText; // 시나리오 텍스트
    [SerializeField] private GameObject scenarioPanel; // 대사 패널 GameObject (null이면 scenarioText의 부모 사용)
    [SerializeField] private Image blackOverlay; // 검정색 오버레이
    [SerializeField] private TextMeshProUGUI endDialogText; // End dialog 텍스트
    [SerializeField] private Button startButton; // Start 버튼
    
    [Header("Animation Settings")]
    [SerializeField] private float typingSpeed = 0.05f; // 타이핑 속도 (초당 문자 수)
    [SerializeField] private float delayBeforeTyping = 1f; // 타이핑 시작 전 딜레이
    [SerializeField] private float panelFadeDuration = 0.5f; // 패널 페이드 시간
    [SerializeField] private float blackFadeOutDuration = 2f; // 검정색 페이드아웃 시간
    [SerializeField] private float endDialogFadeInDuration = 1f; // End dialog 페이드인 시간
    [SerializeField] private float buttonFadeInDuration = 0.5f; // 버튼 페이드인 시간
    [SerializeField] private float buttonFadeInDelay = 0.5f; // End dialog 후 버튼 페이드인 딜레이
    
    [Header("Scene Transition Settings")]
    [SerializeField] private string gameSceneName = "GameScene"; // Game Scene 이름
    [SerializeField] private float sceneTransitionFadeDuration = 2f; // 씬 전환 페이드아웃 시간
    
    [Header("Audio Settings")]
    [SerializeField] private AudioSource typingAudioSource; // 타이핑 소리용 AudioSource
    [SerializeField] private AudioClip typingSound; // 타이핑 소리 클립
    [SerializeField] private float typingSoundVolume = 0.5f; // 타이핑 소리 볼륨
    
    [System.Serializable]
    public class BackgroundEntry
    {
        public Sprite backgroundSprite; // 배경 스프라이트
        public string[] scenarios; // 해당 배경의 시나리오들
    }
    
    [Header("Backgrounds and Scenarios")]
    [SerializeField] private BackgroundEntry[] backgrounds = new BackgroundEntry[4]; // 4개의 배경과 각각의 대사들
    
    private SpriteRenderer backgroundSpriteRenderer;
    private GameObject panelObject; // 실제 사용할 패널 GameObject
    private bool isTyping = false; // 현재 타이핑 중인지 여부
    private string currentFullText = ""; // 현재 타이핑 중인 전체 텍스트
    private int currentTypingIndex = 0; // 현재 타이핑 인덱스
    private Color blackOverlayOriginalColor; // 검정색 오버레이 원본 색상
    private Color endDialogOriginalColor; // End dialog 원본 색상
    private List<Color> buttonOriginalColors = new List<Color>(); // 버튼 원본 색상들
    private List<Graphic> buttonGraphics = new List<Graphic>(); // 버튼 Graphic 컴포넌트들
    private bool isTransitioning = false; // 씬 전환 중인지 여부
    
    void Start()
    {
        InitializeScene();
        InitializeAudio();
        SetupStartButton();
        StartCoroutine(PlayIntroSequence());
    }
    
    private void SetupStartButton()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartButtonClicked);
        }
    }
    
    private void InitializeAudio()
    {
        // 타이핑 소리용 AudioSource 초기화
        if (typingAudioSource == null)
        {
            GameObject typingAudioObject = new GameObject("TypingAudioSource");
            typingAudioObject.transform.SetParent(transform);
            typingAudioSource = typingAudioObject.AddComponent<AudioSource>();
            typingAudioSource.playOnAwake = false;
            typingAudioSource.loop = true;
            typingAudioSource.volume = typingSoundVolume;
        }
        else
        {
            // 기존 AudioSource 설정
            typingAudioSource.playOnAwake = false;
            typingAudioSource.loop = true;
            typingAudioSource.volume = typingSoundVolume;
        }
    }
    
    private void InitializeScene()
    {
        // 배경 GameObject의 SpriteRenderer 가져오기
        if (backgroundObject != null)
        {
            backgroundSpriteRenderer = backgroundObject.GetComponent<SpriteRenderer>();
            if (backgroundSpriteRenderer == null)
            {
                Debug.LogWarning("IntroSceneManager: backgroundObject에 SpriteRenderer가 없습니다!");
            }
            else
            {
                // 초기 상태: 불투명하게 설정
                Color bgColor = backgroundSpriteRenderer.color;
                bgColor.a = 1f;
                backgroundSpriteRenderer.color = bgColor;
            }
        }
        
        // 패널 GameObject 결정 (scenarioPanel이 없으면 scenarioText의 부모 사용)
        if (scenarioPanel != null)
        {
            panelObject = scenarioPanel;
        }
        else if (scenarioText != null && scenarioText.transform.parent != null)
        {
            panelObject = scenarioText.transform.parent.gameObject;
        }
        else if (scenarioText != null)
        {
            panelObject = scenarioText.gameObject;
        }
        
        // CanvasGroup 초기화
        if (panelObject != null)
        {
            CanvasGroup canvasGroup = panelObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = panelObject.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        
        // 시나리오 텍스트 초기 상태
        if (scenarioText != null)
        {
            scenarioText.gameObject.SetActive(true);
            scenarioText.text = "";
        }
        
        // 검정색 오버레이 초기화
        if (blackOverlay != null)
        {
            blackOverlayOriginalColor = blackOverlay.color;
            Color startColor = blackOverlayOriginalColor;
            startColor.a = 0f; // 초기에는 투명
            blackOverlay.color = startColor;
            blackOverlay.gameObject.SetActive(true);
        }
        
        // End dialog 초기화 (이미 GameObject로 존재)
        if (endDialogText != null)
        {
            endDialogOriginalColor = endDialogText.color;
            Color startColor = endDialogOriginalColor;
            startColor.a = 0f;
            endDialogText.color = startColor;
            // GameObject는 이미 존재하므로 활성화만 확인
            if (endDialogText.gameObject != null)
            {
                endDialogText.gameObject.SetActive(true);
            }
        }
        
        // Start 버튼 초기화 (이미 GameObject로 존재)
        InitializeStartButton();
    }
    
    private void InitializeStartButton()
    {
        buttonGraphics.Clear();
        buttonOriginalColors.Clear();
        
        if (startButton != null)
        {
            SetupButtonTransparency(startButton);
            startButton.interactable = false;
        }
    }
    
    private void SetupButtonTransparency(Button button)
    {
        Graphic graphic = button.targetGraphic;
        if (graphic != null)
        {
            buttonGraphics.Add(graphic);
            buttonOriginalColors.Add(graphic.color);
            Color startColor = graphic.color;
            startColor.a = 0f;
            graphic.color = startColor;
        }
        
        // 버튼의 텍스트도 함께 처리
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
    
    private IEnumerator PlayIntroSequence()
    {
        if (backgrounds == null || backgrounds.Length == 0)
        {
            Debug.LogWarning("IntroSceneManager: backgrounds가 설정되지 않았습니다!");
            yield break;
        }
        
        // 각 배경에 대해 순차적으로 처리
        for (int i = 0; i < backgrounds.Length; i++)
        {
            if (backgrounds[i] == null) continue;
            
            // 배경 전환 (즉시 변경, 페이드 효과 없음)
            if (backgrounds[i].backgroundSprite != null && backgroundSpriteRenderer != null)
            {
                backgroundSpriteRenderer.sprite = backgrounds[i].backgroundSprite;
                // 배경을 즉시 불투명하게 설정
                Color bgColor = backgroundSpriteRenderer.color;
                bgColor.a = 1f;
                backgroundSpriteRenderer.color = bgColor;
            }
            
            // 첫 번째 배경이 아니면 이미 이전 배경의 마지막 대사에서 배경 전환과 페이드인 완료됨
            // 타이핑 시작 전 딜레이 (첫 번째 배경만)
            if (i == 0)
            {
                yield return new WaitForSeconds(delayBeforeTyping);
            }
            
            // 해당 배경의 모든 시나리오 타이핑
            if (backgrounds[i].scenarios != null && backgrounds[i].scenarios.Length > 0)
            {
                int scenarioCount = backgrounds[i].scenarios.Length;
                for (int j = 0; j < scenarioCount; j++)
                {
                    string scenario = backgrounds[i].scenarios[j];
                    if (!string.IsNullOrEmpty(scenario))
                    {
                        // 마지막 배경의 마지막 시나리오인지 확인
                        bool isLastScenarioOverall = (i == backgrounds.Length - 1) && (j == scenarioCount - 1);
                        bool allowSkip = !isLastScenarioOverall; // 마지막 시나리오는 스킵 불가
                        
                        yield return StartCoroutine(TypeText(scenarioText, scenario, typingSpeed, allowSkip));
                        
                        // 클릭을 기다림
                        yield return StartCoroutine(WaitForClick());
                        
                        // 마지막 대사이고 다음 배경이 있으면 패널 페이드아웃 → 배경 전환 → 페이드인
                        bool isLastScenario = (j == scenarioCount - 1);
                        bool hasNextBackground = (i < backgrounds.Length - 1);
                        
                        if (isLastScenario && hasNextBackground)
                        {
                            // 패널 페이드아웃
                            yield return StartCoroutine(FadeOutPanel());
                            
                            // 다음 배경으로 전환
                            if (backgrounds[i + 1] != null && 
                                backgrounds[i + 1].backgroundSprite != null && 
                                backgroundSpriteRenderer != null)
                            {
                                backgroundSpriteRenderer.sprite = backgrounds[i + 1].backgroundSprite;
                                Color bgColor = backgroundSpriteRenderer.color;
                                bgColor.a = 1f;
                                backgroundSpriteRenderer.color = bgColor;
                            }
                            
                            // 패널 페이드인 (딜레이 없이 바로)
                            yield return StartCoroutine(FadeInPanel());
                        }
                        else if (!isLastScenario)
                        {
                            // 같은 배경 내에서 다음 대사로 넘어갈 때는 대사만 리셋
                            if (scenarioText != null)
                            {
                                scenarioText.text = "";
                            }
                        }
                    }
                }
            }
        }
        
        // 모든 시나리오가 끝나면 검정색 페이드아웃 후 End dialog와 Start 버튼 표시
        yield return StartCoroutine(ShowEndScreen());
    }
    
    private IEnumerator ShowEndScreen()
    {
        // 검정색 페이드아웃
        if (blackOverlay != null)
        {
            yield return StartCoroutine(FadeOutBlackOverlay());
        }
        
        // End dialog 페이드인
        if (endDialogText != null)
        {
            yield return StartCoroutine(FadeInEndDialog());
        }
        
        // 딜레이 후 Start 버튼 페이드인
        yield return new WaitForSeconds(buttonFadeInDelay);
        
        if (startButton != null)
        {
            yield return StartCoroutine(FadeInStartButton());
        }
    }
    
    private IEnumerator FadeOutBlackOverlay()
    {
        if (blackOverlay == null) yield break;
        
        float elapsedTime = 0f;
        Color startColor = blackOverlay.color;
        Color targetColor = Color.black;
        targetColor.a = 1f; // 완전히 검정색으로
        
        while (elapsedTime < blackFadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / blackFadeOutDuration);
            
            // Smooth step (부드러운 전환)
            t = t * t * (3f - 2f * t);
            
            if (blackOverlay != null)
            {
                Color currentColor = Color.Lerp(startColor, targetColor, t);
                blackOverlay.color = currentColor;
            }
            
            yield return null;
        }
        
        // 최종 색상 설정
        if (blackOverlay != null)
        {
            blackOverlay.color = targetColor;
        }
    }
    
    private IEnumerator FadeInEndDialog()
    {
        if (endDialogText == null) yield break;
        
        endDialogText.gameObject.SetActive(true);
        float elapsedTime = 0f;
        Color startColor = endDialogOriginalColor;
        startColor.a = 0f;
        Color targetColor = endDialogOriginalColor;
        targetColor.a = 1f;
        
        while (elapsedTime < endDialogFadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / endDialogFadeInDuration);
            
            // Smooth step
            t = t * t * (3f - 2f * t);
            
            if (endDialogText != null)
            {
                Color currentColor = Color.Lerp(startColor, targetColor, t);
                endDialogText.color = currentColor;
            }
            
            yield return null;
        }
        
        // 최종 색상 설정
        if (endDialogText != null)
        {
            endDialogText.color = targetColor;
        }
    }
    
    private IEnumerator FadeInStartButton()
    {
        if (startButton == null) yield break;
        
        startButton.interactable = true;
        float elapsedTime = 0f;
        
        for (int i = 0; i < buttonGraphics.Count; i++)
        {
            if (buttonGraphics[i] == null) continue;
            
            Color startColor = buttonOriginalColors[i];
            startColor.a = 0f;
            Color targetColor = buttonOriginalColors[i];
            targetColor.a = 1f;
            
            elapsedTime = 0f;
            while (elapsedTime < buttonFadeInDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / buttonFadeInDuration);
                
                // Smooth step
                t = t * t * (3f - 2f * t);
                
                if (buttonGraphics[i] != null)
                {
                    Color currentColor = Color.Lerp(startColor, targetColor, t);
                    buttonGraphics[i].color = currentColor;
                }
                
                yield return null;
            }
            
            // 최종 색상 설정
            if (buttonGraphics[i] != null)
            {
                buttonGraphics[i].color = targetColor;
            }
        }
    }
    
    private IEnumerator TypeText(TextMeshProUGUI textComponent, string fullText, float typingSpeed, bool allowSkip = true)
    {
        if (textComponent == null) yield break;
        
        textComponent.text = "";
        textComponent.gameObject.SetActive(true);
        
        // 타이핑 상태 설정
        isTyping = true;
        currentFullText = fullText;
        currentTypingIndex = 0;
        
        // 타이핑 소리 시작
        bool isPlayingTypingSound = false;
        if (typingSound != null && typingAudioSource != null)
        {
            typingAudioSource.clip = typingSound;
            typingAudioSource.volume = typingSoundVolume;
            typingAudioSource.loop = true;
            typingAudioSource.Play();
            isPlayingTypingSound = true;
        }
        
        for (int i = 0; i <= fullText.Length; i++)
        {
            // 현재 문자 표시
            textComponent.text = fullText.Substring(0, i);
            currentTypingIndex = i;
            
            // 다음 문자까지의 대기 시간 동안 매 프레임 클릭 체크
            float elapsedTime = 0f;
            while (elapsedTime < typingSpeed)
            {
                // 클릭으로 즉시 완료 체크 (allowSkip이 true일 때만)
                if (allowSkip && IsClickDetected())
                {
                    // 즉시 전체 텍스트 표시 (타이핑이 완전히 끝난 상태로)
                    textComponent.text = fullText;
                    currentTypingIndex = fullText.Length;
                    
                    // 타이핑 소리 즉시 중지
                    if (isPlayingTypingSound && typingAudioSource != null)
                    {
                        typingAudioSource.Stop();
                    }
                    
                    // 타이핑 상태 해제
                    isTyping = false;
                    currentFullText = "";
                    currentTypingIndex = 0;
                    
                    // 한 프레임 대기 (텍스트가 완전히 렌더링되도록)
                    yield return null;
                    
                    // 루프 종료 (타이핑 완료 상태로)
                    yield break;
                }
                
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }
        
        // 타이핑 소리 중지 (정상적으로 타이핑이 끝난 경우)
        if (isPlayingTypingSound && typingAudioSource != null)
        {
            typingAudioSource.Stop();
        }
        
        // 타이핑 상태 해제
        isTyping = false;
        currentFullText = "";
        currentTypingIndex = 0;
    }
    
    private bool IsClickDetected()
    {
        // Input System 또는 Legacy Input 모두 지원
        #if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Mouse.current != null && 
            UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
        {
            return true;
        }
        #else
        if (Input.GetMouseButtonDown(0))
        {
            return true;
        }
        #endif
        
        return false;
    }
    
    private IEnumerator WaitForClick()
    {
        // 마우스 버튼이 눌릴 때까지 대기
        bool clicked = false;
        while (!clicked)
        {
            // Input System 또는 Legacy Input 모두 지원
            #if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current != null && 
                UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
            {
                clicked = true;
            }
            #else
            if (Input.GetMouseButtonDown(0))
            {
                clicked = true;
            }
            #endif
            
            yield return null;
        }
        
        // 클릭이 감지되면 한 프레임 대기 (연속 클릭 방지)
        yield return null;
    }
    
    private IEnumerator FadeOutPanel()
    {
        if (panelObject == null) yield break;
        
        // CanvasGroup이 있으면 사용
        CanvasGroup canvasGroup = panelObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            // CanvasGroup이 없으면 추가
            canvasGroup = panelObject.AddComponent<CanvasGroup>();
        }
        
        // 대사 텍스트 리셋
        if (scenarioText != null)
        {
            scenarioText.text = "";
        }
        
        float elapsedTime = 0f;
        float startAlpha = canvasGroup.alpha;
        
        while (elapsedTime < panelFadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / panelFadeDuration);
            t = t * t * (3f - 2f * t); // Smooth step
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
    
    private IEnumerator FadeInPanel()
    {
        if (panelObject == null) yield break;
        
        // CanvasGroup이 있으면 사용
        CanvasGroup canvasGroup = panelObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            // CanvasGroup이 없으면 추가
            canvasGroup = panelObject.AddComponent<CanvasGroup>();
        }
        
        // 패널 활성화
        panelObject.SetActive(true);
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        
        // 대사 텍스트 리셋
        if (scenarioText != null)
        {
            scenarioText.text = "";
        }
        
        float elapsedTime = 0f;
        float startAlpha = canvasGroup.alpha;
        
        while (elapsedTime < panelFadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / panelFadeDuration);
            t = t * t * (3f - 2f * t); // Smooth step
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, t);
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
    }
    
    private void OnStartButtonClicked()
    {
        if (isTransitioning) return; // 이미 전환 중이면 무시
        
        isTransitioning = true;
        StartCoroutine(FadeOutAndLoadScene());
    }
    
    private IEnumerator FadeOutAndLoadScene()
    {
        // 페이드아웃 없이 바로 씬 전환
        yield return new WaitForEndOfFrame();
        
        // Game Scene으로 전환
        if (!string.IsNullOrEmpty(gameSceneName))
        {
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            Debug.LogError("IntroSceneManager: gameSceneName이 설정되지 않았습니다!");
        }
    }
    
    private void OnDestroy()
    {
        // 메모리 누수 방지를 위해 이벤트 리스너 제거
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(OnStartButtonClicked);
        }
        
        // 오디오 소스 정리
        if (typingAudioSource != null)
        {
            typingAudioSource.Stop();
        }
    }
}
