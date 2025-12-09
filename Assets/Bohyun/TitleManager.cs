using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class TitleManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image blackOverlay; 
    [SerializeField] private Image backgroundImage; // 배경 이미지
    
    [Header("Menu References")]
    [SerializeField] private RectTransform logoRectTransform; // Logo RectTransform (로고 이미지)
    [SerializeField] private Image logoImage; // Logo Image 컴포넌트
    [SerializeField] private Button startButton; // Start 버튼
    [SerializeField] private Button exitButton; // Exit 버튼
    [SerializeField] private string introSceneName = "IntroScene"; // Intro 씬 이름
    [SerializeField] private float sceneTransitionFadeDuration = 1f; // 씬 전환 페이드아웃 시간

    
    [Header("Audio Settings")]
    [SerializeField] private AudioSource bgmAudioSource; // BGM용 AudioSource
    [SerializeField] private AudioClip bgmClip; // BGM 클립
    [SerializeField] private float bgmVolume = 0.5f; // BGM 볼륨
    
    [Header("Title Animation Settings")]
    [SerializeField] private float logoFloatDuration = 2f; // 로고 떠오르는 시간
    [SerializeField] private float buttonFadeInDuration = 0.5f; // 버튼 페이드인 시간
    [SerializeField] private float buttonFadeInDelay = 0.5f; // 로고 애니메이션 후 버튼 페이드인 딜레이
    [SerializeField] private float logoStartOffsetY = 100f; // 로고 시작 위치 오프셋
    [SerializeField] private float blackOverlayFadeDuration = 1f; // 검정 오버레이 페이드 시간
    
    private Color blackOverlayOriginalColor;
    private Vector2 logoOriginalPosition;
    private Color logoOriginalColor;
    private List<Color> buttonOriginalColors = new List<Color>();
    private List<Graphic> buttonGraphics = new List<Graphic>();
    private bool isTransitioning = false;
    
    void Start()
    {
        blackOverlay.gameObject.SetActive(true);
        InitializeAudio();
        InitializeScene();
        SetupMenuButtons();
        StartTitleSequence();
    }
    
    private void InitializeScene()
    {
        // 검정 오버레이 초기화
        if (blackOverlay != null)
        {
            blackOverlayOriginalColor = blackOverlay.color;
            Color startColor = blackOverlayOriginalColor;
            startColor.a = 1f; // 시작 시 검정색
            blackOverlay.color = startColor;
        }
        
        // 로고 초기화
        InitializeLogo();
        
        // 버튼 초기화
        InitializeButtons();
    }
    
    private void InitializeLogo()
    {
        // logoImage가 없으면 자동으로 찾기
        if (logoImage == null && logoRectTransform != null)
        {
            logoImage = logoRectTransform.GetComponent<Image>();
        }
        
        if (logoRectTransform != null)
        {
            logoOriginalPosition = logoRectTransform.anchoredPosition;
            // 시작 위치를 아래로 설정
            Vector2 startPos = logoOriginalPosition;
            startPos.y = logoOriginalPosition.y - logoStartOffsetY;
            logoRectTransform.anchoredPosition = startPos;
        }
        
        if (logoImage != null)
        {
            logoOriginalColor = logoImage.color;
            if (logoOriginalColor.a < 0.1f)
            {
                logoOriginalColor.a = 1f;
            }
            Color startColor = logoOriginalColor;
            startColor.a = 0f; // 시작 시 투명
            logoImage.color = startColor;
        }
    }
    
    private void InitializeAudio()
    {
        // BGM용 AudioSource 초기화
        if (bgmAudioSource == null && bgmClip != null)
        {
            GameObject bgmAudioObject = new GameObject("BGMAudioSource");
            bgmAudioObject.transform.SetParent(transform);
            bgmAudioSource = bgmAudioObject.AddComponent<AudioSource>();
            bgmAudioSource.playOnAwake = false;
            bgmAudioSource.loop = true;
            bgmAudioSource.volume = bgmVolume;
            bgmAudioSource.clip = bgmClip;
        }
        else if (bgmAudioSource != null)
        {
            bgmAudioSource.playOnAwake = false;
            bgmAudioSource.loop = true;
            bgmAudioSource.volume = bgmVolume;
            if (bgmClip != null)
            {
                bgmAudioSource.clip = bgmClip;
            }
        }
    }
    
    private void StartTitleSequence()
    {
        StartCoroutine(TitleSequenceCoroutine());
    }
    
    private IEnumerator TitleSequenceCoroutine()
    {
        // BGM 재생 (게임 시작하자마자)
        if (bgmAudioSource != null && bgmClip != null)
        {
            bgmAudioSource.Play();
        }
        
        // 검정 오버레이 페이드아웃 (BGM과 동시에)
        if (blackOverlay != null)
        {
            yield return StartCoroutine(FadeBlackOverlay(0f));
        }
        
        // 로고 떠오르기
        yield return StartCoroutine(FadeInLogo());
        
        // 버튼 페이드인 딜레이
        yield return new WaitForSeconds(buttonFadeInDelay);
        
        // 버튼 페이드인
        yield return StartCoroutine(FadeInButtons());
    }
    
    private void InitializeButtons()
    {
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
   
    private IEnumerator FadeBlackOverlay(float targetAlpha)
    {
        if (blackOverlay == null) yield break;
        
        if (targetAlpha > 0f && !blackOverlay.gameObject.activeSelf)
        {
            blackOverlay.gameObject.SetActive(true);
        }
        
        float elapsedTime = 0f;
        Color startColor = blackOverlay.color;
        Color targetColor = startColor;
        targetColor.a = targetAlpha;
        
        while (elapsedTime < blackOverlayFadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / blackOverlayFadeDuration);
            t = t * t * (3f - 2f * t); // Smooth step
            
            Color currentColor = Color.Lerp(startColor, targetColor, t);
            blackOverlay.color = currentColor;
            
            yield return null;
        }
        
        blackOverlay.color = targetColor;
        
        // 알파값이 0이 되면 비활성화
        if (targetAlpha <= 0f)
        {
            blackOverlay.gameObject.SetActive(false);
        }
    }

    private IEnumerator FadeInLogo()
    {
        if (logoRectTransform == null) yield break;
        
        float elapsedTime = 0f;
        float startY = logoOriginalPosition.y - logoStartOffsetY;
        float targetY = logoOriginalPosition.y;
        
        while (elapsedTime < logoFloatDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / logoFloatDuration);
            // Ease out cubic for smooth floating effect
            t = 1f - Mathf.Pow(1f - t, 3f);
            
            Vector2 currentPos = logoRectTransform.anchoredPosition;
            currentPos.y = Mathf.Lerp(startY, targetY, t);
            logoRectTransform.anchoredPosition = currentPos;
            
            if (logoImage != null)
            {
                Color currentColor = logoImage.color;
                currentColor.a = Mathf.Lerp(0f, logoOriginalColor.a, t);
                logoImage.color = currentColor;
            }
            
            yield return null;
        }
        
        logoRectTransform.anchoredPosition = logoOriginalPosition;
        if (logoImage != null)
        {
            logoImage.color = logoOriginalColor;
        }
    }
    
    private IEnumerator FadeInButtons()
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < buttonFadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / buttonFadeInDuration);
            t = t * t * (3f - 2f * t);
            
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
        
        for (int i = 0; i < buttonGraphics.Count && i < buttonOriginalColors.Count; i++)
        {
            if (buttonGraphics[i] != null)
            {
                buttonGraphics[i].color = buttonOriginalColors[i];
            }
        }
        
        if (startButton != null)
        {
            startButton.interactable = true;
        }
        
        if (exitButton != null)
        {
            exitButton.interactable = true;
        }
    }
    
    private void SetupMenuButtons()
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
    
    private void OnStartButtonClicked()
    {
        if (isTransitioning) return;
        
        Debug.Log("Start 버튼 클릭됨");
        isTransitioning = true;
        StartCoroutine(FadeOutAndLoadScene());
    }
    
    private IEnumerator FadeOutAndLoadScene()
    {
        // 다른 코루틴들은 중지하되, 이 코루틴은 계속 실행되도록 함
        // (StopAllCoroutines는 이 코루틴도 중지하므로 사용하지 않음)
        
        // 검정 페이드아웃 효과
        if (blackOverlay != null)
        {
            // blackOverlay 활성화
            if (!blackOverlay.gameObject.activeSelf)
            {
                blackOverlay.gameObject.SetActive(true);
            }
            
            float elapsedTime = 0f;
            Color startColor = blackOverlay.color;
            Color targetColor = Color.black;
            targetColor.a = 1f; // 완전히 검정색으로
            
            // 현재 alpha 값을 시작점으로 사용 (더 자연스러운 전환)
            // alpha가 낮으면 0에서 시작하도록 조정
            if (startColor.a < 0.1f)
            {
                startColor.a = 0f;
            }
            
            // 페이드아웃 시작 시간 기록 (디버그용)
            float startTime = Time.time;
            
            while (elapsedTime < sceneTransitionFadeDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / sceneTransitionFadeDuration);
                
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
            
            // 실제 경과 시간 확인 (디버그용)
            float actualDuration = Time.time - startTime;
            Debug.Log($"페이드아웃 완료: 설정 시간 {sceneTransitionFadeDuration}초, 실제 시간 {actualDuration}초");
        }
        else
        {
            // blackOverlay가 없으면 최소한의 딜레이
            yield return new WaitForSeconds(sceneTransitionFadeDuration);
        }
        
        // 잠시 대기 (에디터가 객체를 정리할 시간 제공)
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.1f); // 추가 대기 시간
        
        // Intro Scene으로 전환
        if (!string.IsNullOrEmpty(introSceneName))
        {
            Debug.Log($"씬 전환 시작: {introSceneName}");
            // 씬 전환 전에 모든 참조 정리
            CleanupBeforeSceneTransition();
            
            try
            {
                SceneManager.LoadScene(introSceneName);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"씬 전환 실패: {e.Message}");
            }
        }
        else
        {
            Debug.LogError("씬 이름이 비어있습니다!");
        }
    }
    
    private void CleanupBeforeSceneTransition()
    {
        // 버튼 이벤트 정리
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(OnStartButtonClicked);
        }
        
        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(OnExitButtonClicked);
        }
    
        
        if (bgmAudioSource != null)
        {
            bgmAudioSource.Stop();
        }
    }
    
    private void OnExitButtonClicked()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    private void OnDestroy()
    {
        CleanupBeforeSceneTransition();
    }
    
    private void OnDisable()
    {
        // 에디터에서도 안전하게 정리 (씬 전환 중 호출될 수 있음)
        // 하지만 이미 CleanupBeforeSceneTransition에서 정리했으므로 중복 체크만
        if (isTransitioning)
        {
            return; // 이미 정리됨
        }
        
        // 에디터 모드에서만 실행 (플레이 모드가 아닐 때)
        #if UNITY_EDITOR
        if (!UnityEditor.EditorApplication.isPlaying)
        {
            return;
        }
        #endif
        
        CleanupBeforeSceneTransition();
    }
}
