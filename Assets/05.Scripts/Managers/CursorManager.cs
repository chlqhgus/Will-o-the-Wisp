using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro;

public class CursorManager : MonoBehaviour
{
    [Header("Cursor Settings")]
    [SerializeField] private Texture2D defaultCursor;
    [SerializeField] private Texture2D hoverCursor;
    [SerializeField] private Vector2 cursorHotspot = Vector2.zero;
    
    [Header("Arrow Indicator Settings")]
    [SerializeField] private Sprite arrowSprite; // 화살표 스프라이트 (없으면 텍스트 사용)
    [SerializeField] private string arrowText = ">"; // 화살표 텍스트 (스프라이트가 없을 때)
    [SerializeField] private float arrowOffsetX = -50f; // 버튼 왼쪽으로부터의 거리
    [SerializeField] private Color arrowColor = Color.white;
    [SerializeField] private float arrowScale = 1f;
    
    [Header("UI Elements (Optional - Leave empty to auto-detect)")]
    [SerializeField] private List<Button> interactiveButtons = new List<Button>();
    
    private Dictionary<Button, GameObject> arrowIndicators = new Dictionary<Button, GameObject>();
    private Button currentHoveredButton;
    
    private void Start()
    {
        // 기본 커서 설정
        SetCursor(defaultCursor);
        
        // 버튼이 지정되지 않았으면 씬의 모든 버튼 찾기
        if (interactiveButtons.Count == 0)
        {
            FindAllButtons();
        }
        
        // 모든 버튼에 이벤트 리스너 추가
        SetupButtonEvents();
    }
    
    private void FindAllButtons()
    {
        // 씬의 모든 Button 컴포넌트 찾기
        Button[] allButtons = FindObjectsOfType<Button>();
        interactiveButtons.AddRange(allButtons);
    }
    
    private void SetupButtonEvents()
    {
        foreach (Button button in interactiveButtons)
        {
            if (button != null)
            {
                // EventTrigger 컴포넌트 추가 또는 가져오기
                EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
                if (trigger == null)
                {
                    trigger = button.gameObject.AddComponent<EventTrigger>();
                }
                
                // 기존 트리거 제거 (중복 방지)
                trigger.triggers.Clear();
                
                // Pointer Enter 이벤트 (마우스가 버튼 위로 올라갈 때)
                EventTrigger.Entry enterEntry = new EventTrigger.Entry();
                enterEntry.eventID = EventTriggerType.PointerEnter;
                enterEntry.callback.AddListener((data) => { OnButtonPointerEnter(button); });
                trigger.triggers.Add(enterEntry);
                
                // Pointer Exit 이벤트 (마우스가 버튼에서 벗어날 때)
                EventTrigger.Entry exitEntry = new EventTrigger.Entry();
                exitEntry.eventID = EventTriggerType.PointerExit;
                exitEntry.callback.AddListener((data) => { OnButtonPointerExit(button); });
                trigger.triggers.Add(exitEntry);
            }
        }
    }
    
    private void OnButtonPointerEnter(Button button)
    {
        // 버튼 위에 마우스를 올렸을 때 호버 커서로 변경
        SetCursor(hoverCursor);
        
        // 화살표 표시
        ShowArrow(button);
    }
    
    private void OnButtonPointerExit(Button button)
    {
        // 버튼에서 마우스가 벗어났을 때 기본 커서로 변경
        SetCursor(defaultCursor);
        
        // 화살표 숨기기
        HideArrow(button);
    }
    
    private void ShowArrow(Button button)
    {
        if (button == null) return;
        
        // 이미 표시 중이면 무시
        if (currentHoveredButton == button) return;
        
        // 이전 화살표 숨기기
        if (currentHoveredButton != null)
        {
            HideArrow(currentHoveredButton);
        }
        
        currentHoveredButton = button;
        
        // 화살표가 이미 있으면 표시
        if (arrowIndicators.ContainsKey(button) && arrowIndicators[button] != null)
        {
            arrowIndicators[button].SetActive(true);
            return;
        }
        
        // Canvas 찾기
        Canvas canvas = button.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
        }
        
        if (canvas == null)
        {
            Debug.LogError("Canvas not found!");
            return;
        }
        
        // 화살표 GameObject 생성
        GameObject arrowObj = new GameObject("ArrowIndicator");
        
        // RectTransform 설정
        RectTransform rectTransform = arrowObj.AddComponent<RectTransform>();
        RectTransform buttonRect = button.GetComponent<RectTransform>();
        
        // 버튼의 부모와 같은 부모를 사용
        arrowObj.transform.SetParent(buttonRect.parent, false);
        
        // 버튼의 왼쪽에 위치하도록 설정
        rectTransform.anchorMin = new Vector2(0, 0.5f);
        rectTransform.anchorMax = new Vector2(0, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        
        // 버튼의 위치 기준으로 화살표 위치 계산
        Vector2 buttonAnchoredPos = buttonRect.anchoredPosition;
        Vector2 buttonSize = buttonRect.sizeDelta;
        rectTransform.anchoredPosition = new Vector2(buttonAnchoredPos.x - buttonSize.x / 2 + arrowOffsetX, buttonAnchoredPos.y);
        
        // 크기 설정
        rectTransform.sizeDelta = new Vector2(50, 50);
        
        // 스프라이트가 있으면 Image 사용, 없으면 TextMeshPro 사용
        if (arrowSprite != null)
        {
            Image arrowImage = arrowObj.AddComponent<Image>();
            arrowImage.sprite = arrowSprite;
            arrowImage.color = arrowColor;
        }
        else
        {
            TextMeshProUGUI arrowTextComponent = arrowObj.AddComponent<TextMeshProUGUI>();
            arrowTextComponent.text = arrowText;
            arrowTextComponent.color = arrowColor;
            arrowTextComponent.alignment = TextAlignmentOptions.Center;
            arrowTextComponent.fontSize = 36;
        }
        
        // 스케일 설정
        arrowObj.transform.localScale = Vector3.one * arrowScale;
        
        // 레이캐스트 비활성화
        Graphic graphic = arrowObj.GetComponent<Graphic>();
        if (graphic != null)
        {
            graphic.raycastTarget = false;
        }
        
        // 딕셔너리에 추가
        arrowIndicators[button] = arrowObj;
    }
    
    private void HideArrow(Button button)
    {
        if (button == null) return;
        
        if (button == currentHoveredButton)
        {
            currentHoveredButton = null;
        }
        
        if (arrowIndicators.ContainsKey(button) && arrowIndicators[button] != null)
        {
            arrowIndicators[button].SetActive(false);
        }
    }
    
    private void SetCursor(Texture2D cursorTexture)
    {
        if (cursorTexture != null)
        {
            Cursor.SetCursor(cursorTexture, cursorHotspot, CursorMode.Auto);
        }
    }
    
    private void OnDestroy()
    {
        // 씬이 종료될 때 커서를 기본값으로 리셋
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        
        // 모든 화살표 제거
        foreach (var arrow in arrowIndicators.Values)
        {
            if (arrow != null)
            {
                Destroy(arrow);
            }
        }
        arrowIndicators.Clear();
    }
}

