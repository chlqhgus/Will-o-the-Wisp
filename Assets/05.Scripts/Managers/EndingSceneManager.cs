using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class EndingSceneManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject backgroundObject;
    [SerializeField] private TextMeshProUGUI endingText;
    [SerializeField] private GameObject textPanel;
    [SerializeField] private Button continueButton;
    [SerializeField] private Image logoImage;
    
    [Header("Background Images")]
    [SerializeField] private Sprite[] goodEndingBackgrounds = new Sprite[4];
    [SerializeField] private Sprite[] normalEndingBackgrounds = new Sprite[3];
    [SerializeField] private Sprite[] badEndingBackgrounds = new Sprite[4];
    [SerializeField] private Sprite[] deadEndingBackgrounds = new Sprite[4];
    
    [Header("Animation Settings")]
    [SerializeField] private float typingSpeed = 0.05f;
    [SerializeField] private float delayBeforeTyping = 1f;
    [SerializeField] private float delayBetweenTexts = 0.1f;
    [SerializeField] private float delayAfterLastText = 2f;
    [SerializeField] private float logoFadeInDuration = 1f;
    [SerializeField] private float buttonFadeInDuration = 0.5f;
    [SerializeField] private float buttonFadeInDelay = 0.5f;
    [SerializeField] private float backgroundFadeDuration = 0.5f;
    
    [Header("Scene Transition Settings")]
    [SerializeField] private string menuSceneName = "TitleScene";
    [SerializeField] private float sceneTransitionFadeDuration = 2f;
    
    [Header("Audio Settings")]
    [SerializeField] private AudioSource typingAudioSource;
    [SerializeField] private AudioClip typingSound;
    [SerializeField] private float typingSoundVolume = 0.5f;
    
    private Image backgroundImage;
    private GameObject panelObject;
    private bool isTyping = false;
    private string currentFullText = "";
    private int currentTypingIndex = 0;
    private List<Color> buttonOriginalColors = new List<Color>();
    private List<Graphic> buttonGraphics = new List<Graphic>();
    private bool isTransitioning = false;
    
    public enum EndingType
    {
        Good,
        Normal,
        Bad,
        Dead
    }
    
    private EndingType currentEnding;
    private string[][] endingTexts = new string[4][];
    
    void Start()
    {
        InitializeEndingTexts();
        DetermineEnding();
        InitializeAudio();
        InitializeScene();
        SetupContinueButton();
        StartCoroutine(PlayEndingSequence());
    }
    
    private void InitializeAudio()
    {
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
            typingAudioSource.playOnAwake = false;
            typingAudioSource.loop = true;
            typingAudioSource.volume = typingSoundVolume;
        }
    }
    
    private void InitializeEndingTexts()
    {
        endingTexts[(int)EndingType.Good] = new string[]
        {
            "At last the promised seventh night comes to an end. A ship of the neighboring nation appears beyond the blood-red sky.",
            "The survivors embrace one another, and their voices tremble with relief. Thanks to your guidance most of them now have a chance to see tomorrow. People look to you with gratitude and respect.",
            "Joseon begins to heal with the neighboring nation's aid. Peace settles once more and the sound of everyday life fills the land again.",
            "Dokkaebi have not vanished. Yet you stand with the people now. Human will keep Joseon's peace from ever breaking again."
        };
        
        endingTexts[(int)EndingType.Normal] = new string[]
        {
            "At last the promised seventh night comes to an end. A ship of the neighboring nation appears beyond the blood-red sky.",
            "Joseon begins to heal with the neighboring nation's aid. Yet the silence reveals the weight of those who could not be saved.",
            "As the night deepens a faint and familiar whisper curls at the edge of the woods—a reminder that Dokkaebi have never truly left."
        };
        
        endingTexts[(int)EndingType.Bad] = new string[]
        {
            "At last the promised seventh night comes to an end. A ship of the neighboring nation appears beyond the blood-red sky.",
            "The air is heavy with an unsettling quiet. The few survivors stare at you with hollow eyes. Some whisper accusations—\"Why didn't you save them?\"",
            "Even with the neighboring nation's aid, Joseon struggles to rise. Empty homes, abandoned fields, and the silence of the lost linger everywhere.",
            "And as night falls, the dokkaebi stir once more—stronger than before, feeding on the sorrow left behind. This is only the beginning of what remains."
        };
        
        endingTexts[(int)EndingType.Dead] = new string[]
        {
            "At last the promised seventh night comes to an end. A ship of the neighboring nation appears beyond the blood-red sky.",
            "But there is no one left to greet them. The silence is absolute.",
            "Joseon has fallen. The dokkaebi have claimed everything.",
            "This is the end."
        };
    }
    
    private void DetermineEnding()
    {
        int survivors = GetSurvivorCount();
        
        if (survivors == 0)
        {
            currentEnding = EndingType.Dead;
        }
        else if (survivors >= 13)
        {
            currentEnding = EndingType.Good;
        }
        else if (survivors >= 6)
        {
            currentEnding = EndingType.Normal;
        }
        else
        {
            currentEnding = EndingType.Bad;
        }
    }
    
    private int GetSurvivorCount()
    {
        NPCStateManager stateManager = NPCStateManager.Instance;
        if (stateManager == null)
        {
            return 0;
        }
        
        List<string> allNPCNames = stateManager.GetAllNPCNames();
        if (allNPCNames == null || allNPCNames.Count == 0)
        {
            return 0;
        }
        
        int survivors = 0;
        foreach (string npcName in allNPCNames)
        {
            if (string.IsNullOrEmpty(npcName)) continue;
            if (!stateManager.IsDead(npcName))
            {
                survivors++;
            }
        }
        
        return survivors;
    }
    
    private void SetupContinueButton()
    {
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueButtonClicked);
        }
    }
    
    private void InitializeScene()
    {
        if (backgroundObject != null)
        {
            backgroundImage = backgroundObject.GetComponent<Image>();
            if (backgroundImage != null)
            {
                Color bgColor = backgroundImage.color;
                bgColor.a = 1f;
                backgroundImage.color = bgColor;
            }
        }
        
        if (textPanel != null)
        {
            panelObject = textPanel;
        }
        else if (endingText != null && endingText.transform.parent != null)
        {
            panelObject = endingText.transform.parent.gameObject;
        }
        else if (endingText != null)
        {
            panelObject = endingText.gameObject;
        }
        
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
        
        if (endingText != null)
        {
            endingText.gameObject.SetActive(true);
            endingText.text = "";
        }
        
        if (logoImage != null)
        {
            Color logoColor = logoImage.color;
            logoColor.a = 0f;
            logoImage.color = logoColor;
            logoImage.gameObject.SetActive(true);
        }
        
        InitializeContinueButton();
    }
    
    private void SetBackgroundImage(int index)
    {
        if (backgroundImage == null)
        {
            return;
        }
        
        Sprite[] backgrounds = null;
        switch (currentEnding)
        {
            case EndingType.Good:
                backgrounds = goodEndingBackgrounds;
                break;
            case EndingType.Normal:
                backgrounds = normalEndingBackgrounds;
                break;
            case EndingType.Bad:
                backgrounds = badEndingBackgrounds;
                break;
            case EndingType.Dead:
                backgrounds = deadEndingBackgrounds;
                break;
        }
        
        if (backgrounds == null)
        {
            return;
        }
        
        if (index < 0 || index >= backgrounds.Length)
        {
            return;
        }
        
        if (backgrounds[index] == null)
        {
            return;
        }
        
        backgroundImage.sprite = backgrounds[index];
    }
    
    private void InitializeContinueButton()
    {
        buttonGraphics.Clear();
        buttonOriginalColors.Clear();
        
        if (continueButton != null)
        {
            SetupButtonTransparency(continueButton);
            continueButton.interactable = false;
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
    
    private IEnumerator PlayEndingSequence()
    {
        yield return new WaitForSeconds(delayBeforeTyping);
        
        string[] texts = endingTexts[(int)currentEnding];
        
        for (int i = 0; i < texts.Length; i++)
        {
            if (i == 0)
            {
                SetBackgroundImage(i);
            }
            
            string text = texts[i];
            if (!string.IsNullOrEmpty(text))
            {
                bool isLastText = (i == texts.Length - 1);
                bool allowSkip = !isLastText;
                
                yield return StartCoroutine(TypeText(endingText, text, typingSpeed, allowSkip));
                
                if (!isLastText)
                {
                    yield return StartCoroutine(WaitForClick());
                    
                    yield return StartCoroutine(FadeOutBackground());
                    
                    if (endingText != null)
                    {
                        endingText.text = "";
                    }
                    
                    if (i + 1 < texts.Length)
                    {
                        SetBackgroundImage(i + 1);
                        yield return null;
                    }
                    
                    yield return StartCoroutine(FadeInBackground());
                    yield return new WaitForSeconds(delayBetweenTexts);
                }
                else
                {
                    yield return StartCoroutine(WaitForClick());
                    yield return new WaitForSeconds(delayAfterLastText);
                    yield return StartCoroutine(FadeOutBackground());
                }
            }
        }
        
        yield return StartCoroutine(ShowLogoAndButton());
    }
    
    private IEnumerator ShowLogoAndButton()
    {
        yield return StartCoroutine(FadeOutPanel());
        
        if (logoImage != null)
        {
            yield return StartCoroutine(FadeInLogo());
        }
        
        yield return new WaitForSeconds(buttonFadeInDelay);
        
        if (continueButton != null)
        {
            yield return StartCoroutine(FadeInContinueButton());
        }
    }
    
    private IEnumerator FadeInLogo()
    {
        if (logoImage == null) yield break;
        
        logoImage.gameObject.SetActive(true);
        float elapsedTime = 0f;
        Color startColor = logoImage.color;
        startColor.a = 0f;
        Color targetColor = logoImage.color;
        targetColor.a = 1f;
        
        while (elapsedTime < logoFadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / logoFadeInDuration);
            t = t * t * (3f - 2f * t);
            
            if (logoImage != null)
            {
                Color currentColor = Color.Lerp(startColor, targetColor, t);
                logoImage.color = currentColor;
            }
            
            yield return null;
        }
        
        if (logoImage != null)
        {
            logoImage.color = targetColor;
        }
    }
    
    private IEnumerator FadeInContinueButton()
    {
        if (continueButton == null) yield break;
        
        continueButton.interactable = true;
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
                t = t * t * (3f - 2f * t);
                
                if (buttonGraphics[i] != null)
                {
                    Color currentColor = Color.Lerp(startColor, targetColor, t);
                    buttonGraphics[i].color = currentColor;
                }
                
                yield return null;
            }
            
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
        
        isTyping = true;
        currentFullText = fullText;
        currentTypingIndex = 0;
        
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
            textComponent.text = fullText.Substring(0, i);
            currentTypingIndex = i;
            
            float elapsedTime = 0f;
            while (elapsedTime < typingSpeed)
            {
                if (allowSkip && IsClickDetected())
                {
                    textComponent.text = fullText;
                    currentTypingIndex = fullText.Length;
                    
                    if (isPlayingTypingSound && typingAudioSource != null)
                    {
                        typingAudioSource.Stop();
                    }
                    
                    isTyping = false;
                    currentFullText = "";
                    currentTypingIndex = 0;
                    yield return null;
                    yield break;
                }
                
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }
        
        if (isPlayingTypingSound && typingAudioSource != null)
        {
            typingAudioSource.Stop();
        }
        
        isTyping = false;
        currentFullText = "";
        currentTypingIndex = 0;
    }
    
    private bool IsClickDetected()
    {
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
        bool clicked = false;
        while (!clicked)
        {
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
        
        yield return null;
    }
    
    private IEnumerator FadeOutPanel()
    {
        if (panelObject == null) yield break;
        
        CanvasGroup canvasGroup = panelObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = panelObject.AddComponent<CanvasGroup>();
        }
        
        if (endingText != null)
        {
            endingText.text = "";
        }
        
        float elapsedTime = 0f;
        float startAlpha = canvasGroup.alpha;
        
        while (elapsedTime < backgroundFadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / backgroundFadeDuration);
            t = t * t * (3f - 2f * t);
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
        
        CanvasGroup canvasGroup = panelObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = panelObject.AddComponent<CanvasGroup>();
        }
        
        panelObject.SetActive(true);
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        
        if (endingText != null)
        {
            endingText.text = "";
        }
        
        float elapsedTime = 0f;
        float startAlpha = canvasGroup.alpha;
        
        while (elapsedTime < backgroundFadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / backgroundFadeDuration);
            t = t * t * (3f - 2f * t);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, t);
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
    }
    
    private IEnumerator FadeOutBackground()
    {
        if (backgroundImage == null) yield break;
        
        float elapsedTime = 0f;
        Color startColor = backgroundImage.color;
        Color targetColor = startColor;
        targetColor.a = 0f;
        
        while (elapsedTime < backgroundFadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / backgroundFadeDuration);
            t = t * t * (3f - 2f * t);
            
            if (backgroundImage != null)
            {
                Color currentColor = Color.Lerp(startColor, targetColor, t);
                backgroundImage.color = currentColor;
            }
            
            yield return null;
        }
        
        if (backgroundImage != null)
        {
            backgroundImage.color = targetColor;
        }
    }
    
    private IEnumerator FadeInBackground()
    {
        if (backgroundImage == null) yield break;
        
        float elapsedTime = 0f;
        Color startColor = backgroundImage.color;
        startColor.a = 0f;
        Color targetColor = backgroundImage.color;
        targetColor.a = 1f;
        
        while (elapsedTime < backgroundFadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / backgroundFadeDuration);
            t = t * t * (3f - 2f * t);
            
            if (backgroundImage != null)
            {
                Color currentColor = Color.Lerp(startColor, targetColor, t);
                backgroundImage.color = currentColor;
            }
            
            yield return null;
        }
        
        if (backgroundImage != null)
        {
            backgroundImage.color = targetColor;
        }
    }
    
    private void OnContinueButtonClicked()
    {
        if (isTransitioning) return;
        
        isTransitioning = true;
        StartCoroutine(FadeOutAndLoadScene());
    }
    
    private IEnumerator FadeOutAndLoadScene()
    {
        yield return new WaitForEndOfFrame();
        
        if (!string.IsNullOrEmpty(menuSceneName))
        {
            SceneManager.LoadScene(menuSceneName);
        }
    }
    
    private void OnDestroy()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(OnContinueButtonClicked);
        }
        
        if (typingAudioSource != null)
        {
            typingAudioSource.Stop();
        }
    }
}

