using UnityEngine;
using System.Collections;
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
    
    [Header("Animation Settings")]
    [SerializeField] private float typingSpeed = 0.05f; // 타이핑 속도 (초당 문자 수)
    [SerializeField] private float delayBeforeTyping = 1f; // 타이핑 시작 전 딜레이
    [SerializeField] private float panelFadeDuration = 0.5f; // 패널 페이드 시간
    
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
    
    void Start()
    {
        InitializeScene();
        StartCoroutine(PlayIntroSequence());
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
                        yield return StartCoroutine(TypeText(scenarioText, scenario, typingSpeed));
                        
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
    }
    
    private IEnumerator TypeText(TextMeshProUGUI textComponent, string fullText, float typingSpeed)
    {
        if (textComponent == null) yield break;
        
        textComponent.text = "";
        textComponent.gameObject.SetActive(true);
        
        for (int i = 0; i <= fullText.Length; i++)
        {
            textComponent.text = fullText.Substring(0, i);
            yield return new WaitForSeconds(typingSpeed);
        }
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
}
