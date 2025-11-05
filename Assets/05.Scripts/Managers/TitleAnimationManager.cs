using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using System.Collections.Generic;

public class TitleAnimationManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private RectTransform titleRectTransform;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private List<Button> buttons = new List<Button>();
    
    [Header("Animation Settings")]
    [SerializeField] private float backgroundFadeInDuration = 2f;
    [SerializeField] private float titleSlideDuration = 3f;
    [SerializeField] private float buttonFadeInDuration = 0.5f;
    [SerializeField] private float buttonFadeInStartDelay = 2.5f; // Title 애니메이션 시작 후 이 시간만큼 지나면 버튼 페이드인 시작
    [SerializeField] private float titleStartOffsetY = 100f;
    
    private Vector2 titleOriginalPosition;
    private Color backgroundOriginalColor;
    private Color titleOriginalColor;
    private List<Color> buttonOriginalColors = new List<Color>();
    private List<Graphic> buttonGraphics = new List<Graphic>();
    
    void Start()
    {
        InitializeAnimations();
        PlayStartAnimations();
    }
    
    private void InitializeAnimations()
    {
        // Store original values
        if (titleRectTransform != null)
        {
            titleOriginalPosition = titleRectTransform.anchoredPosition;
        }
        
        if (backgroundImage != null)
        {
            backgroundOriginalColor = backgroundImage.color;
            
            // Set initial state - background transparent
            Color startColor = backgroundOriginalColor;
            startColor.a = 0f;
            backgroundImage.color = startColor;
        }
        
        // Set initial state - title below screen and transparent
        if (titleRectTransform != null)
        {
            Vector2 startPos = titleOriginalPosition;
            startPos.y = titleOriginalPosition.y - titleStartOffsetY; 
            titleRectTransform.anchoredPosition = startPos;
        }
        
        if (titleText != null)
        {
            titleOriginalColor = titleText.color;
            
            // Set initial state - title transparent
            Color startColor = titleOriginalColor;
            startColor.a = 0f;
            titleText.color = startColor;
        }
        
        // Initialize buttons - make them transparent
        buttonGraphics.Clear();
        buttonOriginalColors.Clear();
        foreach (Button button in buttons)
        {
            if (button != null)
            {
                // Get the Image component
                Graphic graphic = button.targetGraphic;
                if (graphic != null)
                {
                    buttonGraphics.Add(graphic);
                    buttonOriginalColors.Add(graphic.color);
                    
                    // Set initial state - button transparent
                    Color startColor = graphic.color;
                    startColor.a = 0f;
                    graphic.color = startColor;
                }
                
                // Also handle TextMeshProUGUI in button children
                TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonGraphics.Add(buttonText);
                    buttonOriginalColors.Add(buttonText.color);
                    
                    // Set initial state - button text transparent
                    Color startColor = buttonText.color;
                    startColor.a = 0f;
                    buttonText.color = startColor;
                }
                
                // Disable button interaction initially
                button.interactable = false;
            }
        }
    }
    
    private void PlayStartAnimations()
    {
        // Start background fade in animation
        if (backgroundImage != null)
        {
            StartCoroutine(FadeInBackground());
        }
    }
    
    private IEnumerator FadeInBackground()
    {
        float elapsedTime = 0f;
        Color startColor = backgroundImage.color;
        
        while (elapsedTime < backgroundFadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / backgroundFadeInDuration;
            
            // Ease out quadratic
            t = 1f - (1f - t) * (1f - t);
            
            Color currentColor = startColor;
            currentColor.a = Mathf.Lerp(0f, backgroundOriginalColor.a, t);
            backgroundImage.color = currentColor;
            
            yield return null;
        }
        
        backgroundImage.color = backgroundOriginalColor;
        
        // Background 완료 후 Title 애니메이션 시작
        StartCoroutine(SlideUpTitle());
        
        // Title 애니메이션 시작 후 지정된 시간만큼 지나면 버튼 애니메이션 시작
        StartCoroutine(StartButtonAnimationAfterDelay());
    }
    
    private IEnumerator StartButtonAnimationAfterDelay()
    {
        yield return new WaitForSeconds(buttonFadeInStartDelay);
        StartCoroutine(FadeInButtons());
    }
    
    private IEnumerator SlideUpTitle()
    {
        float elapsedTime = 0f;
        float startY = titleOriginalPosition.y - titleStartOffsetY;
        float targetY = titleOriginalPosition.y;
        
        while (elapsedTime < titleSlideDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / titleSlideDuration;
            
            // Ease out cubic
            t = 1f - Mathf.Pow(1f - t, 3f);
            
            // Update position
            Vector2 currentPos = titleRectTransform.anchoredPosition;
            currentPos.y = Mathf.Lerp(startY, targetY, t);
            titleRectTransform.anchoredPosition = currentPos;
            
            // Update alpha (fade in)
            if (titleText != null)
            {
                Color currentColor = titleText.color;
                currentColor.a = Mathf.Lerp(0f, titleOriginalColor.a, t);
                titleText.color = currentColor;
            }
            
            yield return null;
        }
        
        // Set final values
        titleRectTransform.anchoredPosition = titleOriginalPosition;
        if (titleText != null)
        {
            titleText.color = titleOriginalColor;
        }
    }
    
    private IEnumerator FadeInButtons()
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < buttonFadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / buttonFadeInDuration;
            
            // Ease out quadratic
            t = 1f - (1f - t) * (1f - t);
            
            // Update all button graphics
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
        
        // Set final values and enable button interaction
        for (int i = 0; i < buttonGraphics.Count && i < buttonOriginalColors.Count; i++)
        {
            if (buttonGraphics[i] != null)
            {
                buttonGraphics[i].color = buttonOriginalColors[i];
            }
        }
        
        // Enable button interactions
        foreach (Button button in buttons)
        {
            if (button != null)
            {
                button.interactable = true;
            }
        }
    }
}

