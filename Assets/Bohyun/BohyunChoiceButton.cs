using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 선택지 버튼 (거절/음식/약)
/// </summary>
public class BohyunChoiceButton : MonoBehaviour
{
    public enum ButtonType
    {
        Refuse,     // 거절
        LotusRice,  // 연밥 (음식)
        HerbalMedicine // 약초 (약)
    }

    [Header("Button Type")]
    public ButtonType buttonType = ButtonType.Refuse;

    [Header("UI References")]
    [Tooltip("버튼 컴포넌트")]
    public Button button;
    [Tooltip("개수를 표시할 텍스트 (선택사항)")]
    public TextMeshProUGUI countText;
    [Tooltip("버튼이 비활성화될 때 표시할 이미지/게임오브젝트 (선택사항)")]
    public GameObject disabledVisual;

    [Header("Settings")]
    [Tooltip("이 버튼의 아이템 개수 (거절은 무제한)")]
    public int itemCount = -1; // -1이면 무제한
    [Tooltip("개수가 0일 때 버튼 비활성화")]
    public bool disableWhenEmpty = true;

    private NPCQueueSystem queueSystem;

    void Start()
    {
        // 버튼 찾기
        if (button == null)
            button = GetComponent<Button>();

        // QueueSystem 찾기
        queueSystem = FindObjectOfType<NPCQueueSystem>();

        // 버튼 이벤트 연결
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClicked);
        }

        // 초기 UI 업데이트
        UpdateButtonUI();
    }

    void Update()
    {
        // 실시간으로 개수 업데이트 (Inventory가 변경될 수 있으므로)
        UpdateButtonUI();
    }

    /// <summary>
    /// 버튼이 클릭되었을 때 호출됩니다.
    /// </summary>
    void OnButtonClicked()
    {
        if (queueSystem == null)
        {
            Debug.LogWarning("BohyunChoiceButton: QueueSystem을 찾을 수 없습니다.");
            return;
        }

        // Inventory에서 개수 확인 (실제 인벤토리 값 사용)
        if (buttonType == ButtonType.LotusRice || buttonType == ButtonType.HerbalMedicine)
        {
            Inventory inventory = queueSystem.inventory;
            if (inventory != null)
            {
                if (buttonType == ButtonType.LotusRice && inventory.lotusRice <= 0)
                {
                    Debug.Log("아이템이 부족합니다.");
                    return;
                }
                if (buttonType == ButtonType.HerbalMedicine && inventory.herbalMedicine <= 0)
                {
                    Debug.Log("아이템이 부족합니다.");
                    return;
                }
            }
            else
            {
                // Inventory가 없으면 itemCount 확인 (하위 호환성)
                if (itemCount >= 0 && itemCount <= 0)
                {
                    Debug.Log("아이템이 부족합니다.");
                    return;
                }
            }
        }

        // QueueSystem의 메서드 호출 (이 메서드 내부에서 Inventory 차감)
        switch (buttonType)
        {
            case ButtonType.Refuse:
                queueSystem.RefuseFrontNPC();
                break;
            case ButtonType.LotusRice:
                queueSystem.GiveLotusRice();
                break;
            case ButtonType.HerbalMedicine:
                queueSystem.GiveHerbalMedicine();
                break;
        }

        // itemCount는 더 이상 사용하지 않음 (Inventory가 실제 값)
        // UI는 UpdateButtonUI()에서 Inventory 값을 읽어서 업데이트됨
    }

    /// <summary>
    /// 버튼 UI를 업데이트합니다 (개수 표시, 활성/비활성).
    /// </summary>
    void UpdateButtonUI()
    {
        int currentCount = GetCurrentCount();

        // 개수 텍스트 업데이트
        if (countText != null)
        {
            if (buttonType == ButtonType.Refuse)
            {
                countText.text = ""; // 거절은 개수 표시 안 함
            }
            else
            {
                countText.text = currentCount > 0 ? currentCount.ToString() : "0";
            }
        }

        // 버튼 활성/비활성
        bool canUse = CanUseButton();
        if (button != null)
        {
            button.interactable = canUse;
        }

        // 비활성화 시각 효과
        if (disabledVisual != null)
        {
            disabledVisual.SetActive(!canUse);
        }
    }

    /// <summary>
    /// 현재 아이템 개수를 가져옵니다.
    /// </summary>
    int GetCurrentCount()
    {
        if (buttonType == ButtonType.Refuse)
            return -1; // 무제한

        if (queueSystem != null && queueSystem.inventory != null)
        {
            Inventory inv = queueSystem.inventory;
            if (buttonType == ButtonType.LotusRice)
                return inv.lotusRice;
            if (buttonType == ButtonType.HerbalMedicine)
                return inv.herbalMedicine;
        }

        return itemCount; // Inventory가 없으면 직접 설정한 개수 사용
    }

    /// <summary>
    /// 버튼을 사용할 수 있는지 확인합니다.
    /// </summary>
    bool CanUseButton()
    {
        // NPC가 없으면 사용 불가
        if (queueSystem == null)
            return false;

        // 개수 확인
        int currentCount = GetCurrentCount();
        if (disableWhenEmpty && currentCount <= 0 && buttonType != ButtonType.Refuse)
        {
            return false;
        }

        return true;
    }

    void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClicked);
        }
    }
}

