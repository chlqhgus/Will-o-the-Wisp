using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class BookButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Sprites")]
    [SerializeField] private Sprite normalSprite; // 일반 이미지
    [SerializeField] private Sprite hoverSprite; // 하얀 테두리가 있는 이미지
    
    [Header("Animation Settings")]
    [SerializeField] private float clickScaleDuration = 0.2f; // 클릭 시 크기 변화 시간
    [SerializeField] private float clickScaleAmount = 0.9f; // 클릭 시 축소 비율 (0.9 = 90% 크기)
    
    [Header("Click Actions")]
    [SerializeField] private GameObject[] objectsToActivate; // 클릭 시 활성화할 오브젝트들
    
    private Image imageComponent;
    private SpriteRenderer spriteRenderer;
    private RectTransform rectTransform;
    private Transform spriteTransform;
    private Vector3 originalScale;
    private bool isUIElement = false;
    
    void Start()
    {
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
            Debug.LogWarning("BookButton: Image 또는 SpriteRenderer 컴포넌트를 찾을 수 없습니다.");
        }
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 호버 시 하얀 테두리 이미지로 변경
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
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
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
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        // 클릭 시 크기 축소 효과
        StartCoroutine(ClickScaleAnimation());
        
        // 클릭 시 오브젝트 활성화
        ActivateObjects();
    }
    
    private void ActivateObjects()
    {
        if (objectsToActivate != null && objectsToActivate.Length > 0)
        {
            foreach (GameObject obj in objectsToActivate)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                }
            }
        }
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
    }
}

