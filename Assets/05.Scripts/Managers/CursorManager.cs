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
        Button[] allButtons = FindObjectsByType<Button>(FindObjectsSortMode.None);
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
        if (cursorTexture == null)
        {
            // 커서가 null이면 시스템 기본 커서로 리셋
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            return;
        }
        
        // 텍스처가 커서로 사용 가능한지 확인
        bool isValid = true;
        string errorMessage = $"Cursor texture '{cursorTexture.name}' 설정 오류:\n";
        
        // Format 확인
        if (cursorTexture.format != TextureFormat.RGBA32)
        {
            isValid = false;
            errorMessage += $"- Format이 RGBA32가 아닙니다. 현재: {cursorTexture.format}\n";
        }
        
        // Readable 확인 (런타임에서는 확인 불가, Import Settings에서만 가능)
        // Mip chain 확인 (런타임에서는 확인 불가, Import Settings에서만 가능)
        
        if (!isValid)
        {
            Debug.LogError(errorMessage + 
                "\nUnity 에디터에서 텍스처 Import Settings를 확인하세요:\n" +
                "1. 텍스처 선택 → Inspector 창\n" +
                "2. Texture Type: Default (또는 Cursor)\n" +
                "3. sRGB (Color Texture): 체크 해제\n" +
                "4. Read/Write Enabled: 체크 ✓\n" +
                "5. Generate Mip Maps: 체크 해제 ✗\n" +
                "6. Alpha Is Transparency: 체크 ✓\n" +
                "7. Format: RGBA 32 bit\n" +
                "8. Apply 버튼 클릭");
            return;
        }
        
        // 커서 설정 시도
        try
        {
            Cursor.SetCursor(cursorTexture, cursorHotspot, CursorMode.Auto);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"커서 설정 실패 '{cursorTexture.name}': {e.Message}\n\n" +
                "Unity 에디터에서 텍스처 Import Settings를 확인하세요:\n" +
                "1. 텍스처 선택 → Inspector 창\n" +
                "2. Texture Type: Default (또는 Cursor)\n" +
                "3. sRGB (Color Texture): 체크 해제\n" +
                "4. Read/Write Enabled: 체크 ✓\n" +
                "5. Generate Mip Maps: 체크 해제 ✗\n" +
                "6. Alpha Is Transparency: 체크 ✓\n" +
                "7. Format: RGBA 32 bit\n" +
                "8. Apply 버튼 클릭");
        }
    }
    
    private void OnDestroy()
    {
        // 씬이 종료될 때 커서를 기본값으로 리셋
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
}

