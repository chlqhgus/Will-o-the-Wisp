using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class TitleManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image blackOverlay; // 검정색 오버레이
    [SerializeField] private Image backgroundImage; // 배경 이미지
    [SerializeField] private TextMeshProUGUI subtitle1Text; // Subtitle1 텍스트
    [SerializeField] private TextMeshProUGUI subtitle2Text; // Subtitle2 텍스트
    [SerializeField] private TextMeshProUGUI childDialogueText; // 아이 대사 텍스트
    [SerializeField] private TextMeshProUGUI adultDialogueText; // 어른 대사 텍스트
    
    [Header("Menu References")]
    [SerializeField] private RectTransform titleRectTransform; // Title RectTransform
    [SerializeField] private TextMeshProUGUI titleText; // Title 텍스트
    [SerializeField] private Button startButton; // Start 버튼
    [SerializeField] private Button exitButton; // Exit 버튼
    [SerializeField] private string introSceneName = "IntroScene"; // Intro 씬 이름
    [SerializeField] private float sceneTransitionFadeDuration = 10f; // 씬 전환 페이드아웃 시간
    
    [Header("Animation Settings")]
    [SerializeField] private float subtitle1TypingSpeed = 0.05f; // Subtitle1 타이핑 속도
    [SerializeField] private float subtitle2TypingSpeed = 0.05f; // Subtitle2 타이핑 속도
    [SerializeField] private float blackOverlayFadeDuration = 2f; // Black overlay 페이드 시간
    [SerializeField] private float dialogueDisplayDuration = 3f; // 각 대사 표시 시간
    [SerializeField] private float delayBetweenDialogues = 0.5f; // 대사 사이 딜레이
    [SerializeField] private float dialogueFadeInDuration = 0.3f; // 대사 페이드인 시간
    [SerializeField] private float dialogueFadeOutDuration = 0.3f; // 대사 페이드아웃 시간
    
    [Header("Audio Settings")]
    [SerializeField] private AudioSource typingAudioSource; // 타이핑 소리용 AudioSource
    [SerializeField] private AudioClip typingSound; // 타이핑 소리 클립
    [SerializeField] private float typingSoundVolume = 0.5f; // 타이핑 소리 볼륨
    [SerializeField] private AudioSource bgmAudioSource; // BGM용 AudioSource
    [SerializeField] private AudioClip bgmClip; // BGM 클립
    [SerializeField] private float bgmVolume = 0.5f; // BGM 볼륨
    
    [Header("Title Animation Settings")]
    [SerializeField] private float titleFadeInDuration = 1f; // Title 페이드인 시간
    [SerializeField] private float buttonFadeInDuration = 0.5f; // 버튼 페이드인 시간
    [SerializeField] private float buttonFadeInDelay = 0.5f; // Title 애니메이션 후 버튼 페이드인 딜레이
    [SerializeField] private float titleStartOffsetY = 100f; // Title 시작 위치 오프셋
    
    [Header("Black Overlay Alpha Values")]
    [Range(0f, 1f)]
    [SerializeField] private float initialAlpha = 1f; // 초기 알파 값 (0-1, 1 = 255)
    [Range(0f, 1f)]
    [SerializeField] private float firstFadeTargetAlpha = 0.98f; // 첫 번째 페이드 목표 알파 (250/255 ≈ 0.98)
    [Range(0f, 1f)]
    [SerializeField] private float secondFadeTargetAlpha = 0.78f; // 두 번째 페이드 목표 알파 (200/255 ≈ 0.78)
    
    [System.Serializable]
    public class DialogueEntry
    {
        public string text;
    }
    
    [Header("Dialogue")]
    [SerializeField] private string subtitle1 = ""; // Subtitle1 텍스트
    [SerializeField] private DialogueEntry[] firstChildDialogues; // 첫 번째 아이 대사들
    [SerializeField] private DialogueEntry[] adultDialogues; // 어른 대사들
    [SerializeField] private DialogueEntry[] secondChildDialogues; // 두 번째 아이 대사들
    [SerializeField] private string subtitle2 = ""; // Subtitle2 텍스트
    
    private Color blackOverlayOriginalColor;
    private Vector2 titleOriginalPosition;
    private Color titleOriginalColor;
    private List<Color> buttonOriginalColors = new List<Color>();
    private List<Graphic> buttonGraphics = new List<Graphic>();
    private bool isTransitioning = false;
    
    void Start()
    {
        InitializeScene();
        SetupMenuButtons();
        InitializeAudio();
        StartCoroutine(PlayTitleSequence());
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
            typingAudioSource.loop = true; // 타이핑 소리는 루프
            typingAudioSource.volume = typingSoundVolume;
        }
        else
        {
            // 기존 AudioSource 설정
            typingAudioSource.playOnAwake = false;
            typingAudioSource.loop = true;
            typingAudioSource.volume = typingSoundVolume;
        }
        
        // BGM용 AudioSource 초기화
        if (bgmAudioSource == null && bgmClip != null)
        {
            GameObject bgmAudioObject = new GameObject("BGMAudioSource");
            bgmAudioObject.transform.SetParent(transform);
            bgmAudioSource = bgmAudioObject.AddComponent<AudioSource>();
            bgmAudioSource.playOnAwake = false;
            bgmAudioSource.loop = true;
            bgmAudioSource.volume = bgmVolume;
        }
        else if (bgmAudioSource != null)
        {
            // 기존 AudioSource 설정
            bgmAudioSource.playOnAwake = false;
            bgmAudioSource.loop = true;
            bgmAudioSource.volume = bgmVolume;
        }
    }
    
    private void InitializeScene()
    {
        // 검정색 오버레이를 지정된 알파 값으로 설정
        if (blackOverlay != null)
        {
            blackOverlayOriginalColor = blackOverlay.color;
            Color blackColor = Color.black;
            blackColor.a = initialAlpha;
            blackOverlay.color = blackColor;
        }
        
        // 배경 이미지 초기 상태
        if (backgroundImage != null)
        {
            backgroundImage.gameObject.SetActive(true);
        }
        
        // Subtitle1 초기 상태
        if (subtitle1Text != null)
        {
            subtitle1Text.gameObject.SetActive(true);
            subtitle1Text.text = "";
        }
        
        // Subtitle2 초기 상태
        if (subtitle2Text != null)
        {
            subtitle2Text.gameObject.SetActive(false);
            subtitle2Text.text = "";
        }
        
        // 대사 텍스트 초기 상태 (비활성화)
        if (childDialogueText != null)
        {
            childDialogueText.gameObject.SetActive(false);
        }
        
        if (adultDialogueText != null)
        {
            adultDialogueText.gameObject.SetActive(false);
        }
        
        // Title 초기 상태 설정
        InitializeTitle();
        
        // 버튼 초기 상태 설정
        InitializeButtons();
    }
    
    private void InitializeTitle()
    {
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
            if (titleOriginalColor.a < 0.1f)
            {
                titleOriginalColor.a = 1f;
            }
            Color startColor = titleOriginalColor;
            startColor.a = 0f;
            titleText.color = startColor;
        }
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
    
    private IEnumerator PlayTitleSequence()
    {
        // 1. Subtitle1 타이핑 (완전히 끝날 때까지 대기)
        if (!string.IsNullOrEmpty(subtitle1) && subtitle1Text != null)
        {
            yield return StartCoroutine(TypeText(subtitle1Text, subtitle1, subtitle1TypingSpeed));
        }
        
        // 1-1. Subtitle1 끝난 후 0.5초 딜레이 후 BGM 재생
        yield return new WaitForSeconds(0.5f);
        
        if (bgmClip != null && bgmAudioSource != null)
        {
            bgmAudioSource.clip = bgmClip;
            bgmAudioSource.volume = bgmVolume;
            bgmAudioSource.loop = true;
            bgmAudioSource.Play();
        }
        
        // 2. Black overlay alpha값이 250까지 서서히 변경 (완전히 끝날 때까지 대기)
        yield return StartCoroutine(FadeBlackOverlay(firstFadeTargetAlpha));
        
        // 3. 아이대사 시작 (모든 아이 대사가 완전히 끝날 때까지 대기)
        if (firstChildDialogues != null && firstChildDialogues.Length > 0)
        {
            foreach (var dialogue in firstChildDialogues)
            {
                if (dialogue != null && !string.IsNullOrEmpty(dialogue.text))
                {
                    yield return StartCoroutine(ShowDialogue(childDialogueText, dialogue.text, dialogueDisplayDuration, false));
                    yield return new WaitForSeconds(delayBetweenDialogues);
                }
            }
            // 마지막 아이 대사가 완전히 끝났는지 확인
            if (childDialogueText != null && childDialogueText.gameObject.activeSelf)
            {
                yield return new WaitForSeconds(delayBetweenDialogues);
            }
        }
        
        // 4. 아이대사 끝 후 어른대사 시작 (모든 어른 대사가 완전히 끝날 때까지 대기)
        if (adultDialogues != null && adultDialogues.Length > 0)
        {
            foreach (var dialogue in adultDialogues)
            {
                if (dialogue != null && !string.IsNullOrEmpty(dialogue.text))
                {
                    yield return StartCoroutine(ShowDialogue(adultDialogueText, dialogue.text, dialogueDisplayDuration, false));
                    yield return new WaitForSeconds(delayBetweenDialogues);
                }
            }
            // 마지막 어른 대사가 완전히 끝났는지 확인
            if (adultDialogueText != null && adultDialogueText.gameObject.activeSelf)
            {
                yield return new WaitForSeconds(delayBetweenDialogues);
            }
        }
        
        // 5. 아이대사 시작 (두 번째) - 모든 아이 대사가 완전히 끝날 때까지 대기
        if (secondChildDialogues != null && secondChildDialogues.Length > 0)
        {
            // 첫 번째 대사만 표시
            if (secondChildDialogues[0] != null && !string.IsNullOrEmpty(secondChildDialogues[0].text))
            {
                yield return StartCoroutine(ShowDialogue(childDialogueText, secondChildDialogues[0].text, dialogueDisplayDuration, false));
            }
            
            // 6. 아이대사 하나 후 subtitle2 타이핑 시작 (완전히 끝날 때까지 대기)
            if (!string.IsNullOrEmpty(subtitle2) && subtitle2Text != null)
            {
                subtitle2Text.gameObject.SetActive(true);
                yield return StartCoroutine(TypeText(subtitle2Text, subtitle2, subtitle2TypingSpeed));
            }
            
            // 나머지 아이 대사들 계속 표시
            for (int i = 1; i < secondChildDialogues.Length; i++)
            {
                if (secondChildDialogues[i] != null && !string.IsNullOrEmpty(secondChildDialogues[i].text))
                {
                    yield return StartCoroutine(ShowDialogue(childDialogueText, secondChildDialogues[i].text, dialogueDisplayDuration, false));
                    yield return new WaitForSeconds(delayBetweenDialogues);
                }
            }
            // 마지막 아이 대사가 완전히 끝났는지 확인
            if (childDialogueText != null && childDialogueText.gameObject.activeSelf)
            {
                yield return new WaitForSeconds(delayBetweenDialogues);
            }
        }
        
        // 7. 아이대사 종료 시 child/adult 텍스트 페이드아웃
        if (childDialogueText != null && childDialogueText.gameObject.activeSelf)
        {
            yield return StartCoroutine(FadeOutDialogue(childDialogueText));
        }
        if (adultDialogueText != null && adultDialogueText.gameObject.activeSelf)
        {
            yield return StartCoroutine(FadeOutDialogue(adultDialogueText));
        }
        
        // 7-1. subtitle1과 subtitle2 동시에 페이드아웃 (제목이 떠오르기 전에)
        bool subtitle1Active = subtitle1Text != null && subtitle1Text.gameObject.activeSelf;
        bool subtitle2Active = subtitle2Text != null && subtitle2Text.gameObject.activeSelf;
        
        if (subtitle1Active || subtitle2Active)
        {
            if (subtitle1Active)
            {
                StartCoroutine(FadeOutDialogue(subtitle1Text));
            }
            if (subtitle2Active)
            {
                StartCoroutine(FadeOutDialogue(subtitle2Text));
            }
            // 둘 중 더 긴 페이드아웃 시간만큼 대기
            yield return new WaitForSeconds(dialogueFadeOutDuration);
        }
        
        // 7-2. black overlay 200까지 서서히 변경 (완전히 끝날 때까지 대기)
        yield return StartCoroutine(FadeBlackOverlay(secondFadeTargetAlpha));
        
        // 7-3. title 표시됨 (완전히 끝날 때까지 대기)
        yield return StartCoroutine(FadeInTitle());
        
        // 8. title 표시되고 나면 start/exit 버튼 나타남 (완전히 끝날 때까지 대기)
        yield return new WaitForSeconds(buttonFadeInDelay);
        yield return StartCoroutine(FadeInButtons());
    }
    
    private IEnumerator TypeText(TextMeshProUGUI textComponent, string fullText, float typingSpeed)
    {
        if (textComponent == null) yield break;
        
        textComponent.text = "";
        textComponent.gameObject.SetActive(true);
        
        // 타이핑 소리 시작 (BGM과 함께 재생되도록)
        bool isPlayingTypingSound = false;
        if (typingSound != null && typingAudioSource != null)
        {
            typingAudioSource.clip = typingSound;
            typingAudioSource.volume = typingSoundVolume;
            typingAudioSource.loop = true;
            typingAudioSource.Play(); // Play() 사용하여 루프 재생 (BGM은 별도 AudioSource에서 재생되어야 함)
            isPlayingTypingSound = true;
        }
        
        for (int i = 0; i <= fullText.Length; i++)
        {
            textComponent.text = fullText.Substring(0, i);
            yield return new WaitForSeconds(typingSpeed);
        }
        
        // 타이핑 소리 중지
        if (isPlayingTypingSound && typingAudioSource != null)
        {
            typingAudioSource.Stop();
        }
    }
    
    private IEnumerator FadeBlackOverlay(float targetAlpha)
    {
        if (blackOverlay == null) yield break;
        
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
    }
    
    private IEnumerator ShowDialogue(TextMeshProUGUI dialogueText, string text, float duration, bool fadeOut = true)
    {
        if (dialogueText == null || string.IsNullOrEmpty(text)) yield break;
        
        dialogueText.gameObject.SetActive(true);
        dialogueText.text = text;
        
        Color originalColor = dialogueText.color;
        if (originalColor.a < 0.1f)
        {
            originalColor.a = 1f;
        }
        
        Color startColor = originalColor;
        startColor.a = 0f;
        dialogueText.color = startColor;
        
        float elapsedTime = 0f;
        
        // 페이드인 실행
        if (dialogueFadeInDuration > 0f)
        {
            while (elapsedTime < dialogueFadeInDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / dialogueFadeInDuration);
                t = t * t * (3f - 2f * t);
                Color currentColor = startColor;
                currentColor.a = Mathf.Lerp(0f, originalColor.a, t);
                dialogueText.color = currentColor;
                yield return null;
            }
        }
        
        dialogueText.color = originalColor;
        
        // 대사 표시 시간 대기
        yield return new WaitForSeconds(duration);
        
        // 페이드아웃
        if (fadeOut && dialogueFadeOutDuration > 0f)
        {
            elapsedTime = 0f;
            Color fadeOutStartColor = dialogueText.color;
            while (elapsedTime < dialogueFadeOutDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / dialogueFadeOutDuration);
                t = t * t * (3f - 2f * t);
                Color currentColor = fadeOutStartColor;
                currentColor.a = Mathf.Lerp(fadeOutStartColor.a, 0f, t);
                dialogueText.color = currentColor;
                yield return null;
            }
            
            Color finalColor = startColor;
            finalColor.a = 0f;
            dialogueText.color = finalColor;
        }
    }
    
    private IEnumerator FadeOutDialogue(TextMeshProUGUI dialogueText)
    {
        if (dialogueText == null || !dialogueText.gameObject.activeSelf) yield break;
        
        float elapsedTime = 0f;
        Color startColor = dialogueText.color;
        
        while (elapsedTime < dialogueFadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / dialogueFadeOutDuration);
            t = t * t * (3f - 2f * t); // Smooth step
            Color currentColor = startColor;
            currentColor.a = Mathf.Lerp(startColor.a, 0f, t);
            dialogueText.color = currentColor;
            yield return null;
        }
        
        Color finalColor = startColor;
        finalColor.a = 0f;
        dialogueText.color = finalColor;
        dialogueText.gameObject.SetActive(false);
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
            t = t * t * (3f - 2f * t);
            
            Vector2 currentPos = titleRectTransform.anchoredPosition;
            currentPos.y = Mathf.Lerp(startY, targetY, t);
            titleRectTransform.anchoredPosition = currentPos;
            
            Color currentColor = titleText.color;
            currentColor.a = Mathf.Lerp(0f, titleOriginalColor.a, t);
            titleText.color = currentColor;
            
            yield return null;
        }
        
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
        
        isTransitioning = true;
        StartCoroutine(FadeOutAndLoadScene());
    }
    
    private IEnumerator FadeOutAndLoadScene()
    {
        // 모든 코루틴 중지
        StopAllCoroutines();
        
        // 검정 페이드아웃 효과
        if (blackOverlay != null)
        {
            float elapsedTime = 0f;
            Color startColor = blackOverlay.color;
            Color targetColor = Color.black;
            targetColor.a = 1f; // 완전히 검정색으로
            
            // 현재 alpha가 이미 높으면 더 부드럽게 시작
            // 시작 alpha를 0으로 리셋하여 항상 부드럽게 페이드아웃
            if (startColor.a < 0.5f)
            {
                startColor.a = 0f; // 현재 alpha가 낮으면 0에서 시작
            }
            
            while (elapsedTime < sceneTransitionFadeDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / sceneTransitionFadeDuration);
                
                // Ease in (처음엔 느리게, 나중에 빠르게)
                t = t * t;
                
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
        
        // 잠시 대기 (에디터가 객체를 정리할 시간 제공)
        yield return new WaitForEndOfFrame();
        
        // Intro Scene으로 전환
        if (!string.IsNullOrEmpty(introSceneName))
        {
            SceneManager.LoadScene(introSceneName);
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
        // 모든 코루틴 중지
        StopAllCoroutines();
        
        // 버튼 이벤트 정리
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(OnStartButtonClicked);
        }
        
        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(OnExitButtonClicked);
        }
        
        // 오디오 소스 정리
        if (typingAudioSource != null)
        {
            typingAudioSource.Stop();
        }
        
        if (bgmAudioSource != null)
        {
            bgmAudioSource.Stop();
        }
    }
    
    private void OnDisable()
    {
        // 에디터에서도 안전하게 정리
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
