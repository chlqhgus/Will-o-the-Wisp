using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class CursorManager : MonoBehaviour
{
    [Header("Cursor Settings")]
    [SerializeField] private Texture2D defaultCursor;
    [SerializeField] private Texture2D hoverCursor;
    [SerializeField] private Vector2 cursorHotspot = Vector2.zero;
    
    [Header("UI Elements (Optional - Leave empty to auto-detect)")]
    [SerializeField] private List<Button> interactiveButtons = new List<Button>();
    
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
                
                // Pointer Enter 이벤트 (마우스가 버튼 위로 올라갈 때)
                EventTrigger.Entry enterEntry = new EventTrigger.Entry();
                enterEntry.eventID = EventTriggerType.PointerEnter;
                enterEntry.callback.AddListener((data) => { OnButtonPointerEnter(); });
                trigger.triggers.Add(enterEntry);
                
                // Pointer Exit 이벤트 (마우스가 버튼에서 벗어날 때)
                EventTrigger.Entry exitEntry = new EventTrigger.Entry();
                exitEntry.eventID = EventTriggerType.PointerExit;
                exitEntry.callback.AddListener((data) => { OnButtonPointerExit(); });
                trigger.triggers.Add(exitEntry);
            }
        }
    }
    
    private void OnButtonPointerEnter()
    {
        // 버튼 위에 마우스를 올렸을 때 호버 커서로 변경
        SetCursor(hoverCursor);
    }
    
    private void OnButtonPointerExit()
    {
        // 버튼에서 마우스가 벗어났을 때 기본 커서로 변경
        SetCursor(defaultCursor);
    }
    
    private void SetCursor(Texture2D cursorTexture)
    {
        if (cursorTexture != null)
        {
            // 텍스처가 커서로 사용 가능한지 확인
            if (cursorTexture.format != TextureFormat.RGBA32)
            {
                Debug.LogWarning($"Cursor texture '{cursorTexture.name}' must be RGBA32 format. Current format: {cursorTexture.format}");
                return;
            }
            
            // 커서 설정 시도
            try
            {
                Cursor.SetCursor(cursorTexture, cursorHotspot, CursorMode.Auto);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to set cursor texture '{cursorTexture.name}': {e.Message}. " +
                    "Please check Texture Import Settings:\n" +
                    "- Texture Type: Cursor (or Default)\n" +
                    "- Format: RGBA 32 bit\n" +
                    "- Read/Write Enabled: ✓\n" +
                    "- Generate Mip Maps: ✗ (unchecked)\n" +
                    "- Alpha Source: From Input");
            }
        }
    }
    
    private void OnDestroy()
    {
        // 씬이 종료될 때 커서를 기본값으로 리셋
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
}

