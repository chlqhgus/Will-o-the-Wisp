using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// 범용 호버 버튼 스크립트
/// 호버 시 이미지 변경 및 클릭 시 크기 애니메이션을 제공합니다.
/// UI Image와 SpriteRenderer 모두 지원합니다.
/// </summary>
public class HoverButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Sprites")]
    [SerializeField] private Sprite normalSprite; // 일반 이미지
    [SerializeField] private Sprite hoverSprite; // 호버 시 이미지
    
    [Header("Animation Settings")]
    [SerializeField] private bool enableClickAnimation = true; // 클릭 애니메이션 활성화 여부
    [SerializeField] private float clickScaleDuration = 0.2f; // 클릭 시 크기 변화 시간
    [SerializeField] private float clickScaleAmount = 0.9f; // 클릭 시 축소 비율 (0.9 = 90% 크기)
    
    [Header("Sound Effect (Optional)")]
    [SerializeField] private AudioClip clickSoundEffect;  // 클릭 사운드 이펙트 (선택사항)
    [SerializeField] private AudioSource audioSource;    // AudioSource (없으면 자동으로 찾거나 생성)
    
    [Header("Hover Scale (Optional)")]
    [SerializeField] private bool enableHoverScale = false; // 호버 시 크기 변경 활성화 여부
    [SerializeField] private float hoverScaleAmount = 1.1f; // 호버 시 확대 비율 (1.1 = 110% 크기)
    [SerializeField] private float hoverScaleDuration = 0.15f; // 호버 크기 변화 시간
    
    private Image imageComponent;
    private SpriteRenderer spriteRenderer;
    private RectTransform rectTransform;
    private Transform spriteTransform;
    private Vector3 originalScale;
    private bool isUIElement = false;
    // isHovering은 현재 사용되지 않지만 향후 확장을 위해 남겨둠
    // private bool isHovering = false;
    private Coroutine hoverScaleCoroutine;
    private Coroutine clickScaleCoroutine;
    
    void Start()
    {
        // AudioSource 설정 (없으면 자동으로 찾거나 생성)
        if (clickSoundEffect != null && audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
        // Image 컴포넌트 확인 (UI 요소인 경우)
        imageComponent = GetComponent<Image>();
        if (imageComponent != null)
        {
            isUIElement = true;
            rectTransform = GetComponent<RectTransform>();
            originalScale = rectTransform.localScale;
            
            // 초기 스프라이트 설정
            if (normalSprite == null && imageComponent.sprite != null)
            {
                normalSprite = imageComponent.sprite;
            }
        }
        else
        {
            // SpriteRenderer 확인 (2D 스프라이트인 경우)
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteTransform = transform;
                originalScale = spriteTransform.localScale;
                
                // 초기 스프라이트 설정
                if (normalSprite == null && spriteRenderer.sprite != null)
                {
                    normalSprite = spriteRenderer.sprite;
                }
            }
        }
        
        if (imageComponent == null && spriteRenderer == null)
        {
            Debug.LogWarning($"HoverButton ({gameObject.name}): Image 또는 SpriteRenderer 컴포넌트를 찾을 수 없습니다.");
        }
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        // isHovering = true; // 현재 사용되지 않음
        
        // 호버 시 이미지 변경
        if (hoverSprite != null)
        {
            if (isUIElement && imageComponent != null)
            {
                imageComponent.sprite = hoverSprite;
            }
            else if (spriteRenderer != null)
            {
                spriteRenderer.sprite = hoverSprite;
            }
        }
        
        // 호버 시 크기 변경 (선택사항)
        if (enableHoverScale)
        {
            if (hoverScaleCoroutine != null)
            {
                StopCoroutine(hoverScaleCoroutine);
            }
            hoverScaleCoroutine = StartCoroutine(HoverScaleAnimation(true));
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        // isHovering = false; // 현재 사용되지 않음
        
        // 호버 해제 시 일반 이미지로 변경
        if (normalSprite != null)
        {
            if (isUIElement && imageComponent != null)
            {
                imageComponent.sprite = normalSprite;
            }
            else if (spriteRenderer != null)
            {
                spriteRenderer.sprite = normalSprite;
            }
        }
        
        // 호버 해제 시 크기 복원 (선택사항)
        if (enableHoverScale)
        {
            if (hoverScaleCoroutine != null)
            {
                StopCoroutine(hoverScaleCoroutine);
            }
            hoverScaleCoroutine = StartCoroutine(HoverScaleAnimation(false));
        }
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        // 사운드 재생 (선택사항)
        PlayClickSound();
        
        // 클릭 시 크기 애니메이션 (선택사항)
        if (enableClickAnimation)
        {
            if (clickScaleCoroutine != null)
            {
                StopCoroutine(clickScaleCoroutine);
            }
            clickScaleCoroutine = StartCoroutine(ClickScaleAnimation());
        }
    }
    
    private void PlayClickSound()
    {
        if (clickSoundEffect != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSoundEffect);
        }
    }
    
    private IEnumerator HoverScaleAnimation(bool isEntering)
    {
        Vector3 startScale = isEntering ? originalScale : originalScale * hoverScaleAmount;
        Vector3 targetScale = isEntering ? originalScale * hoverScaleAmount : originalScale;
        float elapsedTime = 0f;
        
        while (elapsedTime < hoverScaleDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / hoverScaleDuration;
            
            // Ease out quadratic
            t = 1f - (1f - t) * (1f - t);
            
            Vector3 currentScale = Vector3.Lerp(startScale, targetScale, t);
            if (isUIElement && rectTransform != null)
            {
                rectTransform.localScale = currentScale;
            }
            else if (spriteTransform != null)
            {
                spriteTransform.localScale = currentScale;
            }
            
            yield return null;
        }
        
        // 최종 크기 설정
        if (isUIElement && rectTransform != null)
        {
            rectTransform.localScale = targetScale;
        }
        else if (spriteTransform != null)
        {
            spriteTransform.localScale = targetScale;
        }
        
        hoverScaleCoroutine = null;
    }
    
    private IEnumerator ClickScaleAnimation()
    {
        Vector3 targetScale = originalScale * clickScaleAmount;
        float elapsedTime = 0f;
        
        // 축소
        while (elapsedTime < clickScaleDuration / 2f)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / (clickScaleDuration / 2f);
            
            Vector3 currentScale = Vector3.Lerp(originalScale, targetScale, t);
            if (isUIElement && rectTransform != null)
            {
                rectTransform.localScale = currentScale;
            }
            else if (spriteTransform != null)
            {
                spriteTransform.localScale = currentScale;
            }
            
            yield return null;
        }
        
        // 원래 크기로 복원
        elapsedTime = 0f;
        while (elapsedTime < clickScaleDuration / 2f)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / (clickScaleDuration / 2f);
            
            Vector3 currentScale = Vector3.Lerp(targetScale, originalScale, t);
            if (isUIElement && rectTransform != null)
            {
                rectTransform.localScale = currentScale;
            }
            else if (spriteTransform != null)
            {
                spriteTransform.localScale = currentScale;
            }
            
            yield return null;
        }
        
        // 최종적으로 원래 크기로 설정
        if (isUIElement && rectTransform != null)
        {
            rectTransform.localScale = originalScale;
        }
        else if (spriteTransform != null)
        {
            spriteTransform.localScale = originalScale;
        }
        
        clickScaleCoroutine = null;
    }
    
    void OnDisable()
    {
        // 비활성화 시 크기 복원
        if (isUIElement && rectTransform != null)
        {
            rectTransform.localScale = originalScale;
        }
        else if (spriteTransform != null)
        {
            spriteTransform.localScale = originalScale;
        }
        
        // 코루틴 정리
        if (hoverScaleCoroutine != null)
        {
            StopCoroutine(hoverScaleCoroutine);
            hoverScaleCoroutine = null;
        }
        
        if (clickScaleCoroutine != null)
        {
            StopCoroutine(clickScaleCoroutine);
            clickScaleCoroutine = null;
        }
    }
}

