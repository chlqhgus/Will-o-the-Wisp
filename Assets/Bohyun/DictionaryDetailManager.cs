using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class TextSegment
{
    [TextArea(3, 10)]
    public string text;           // 텍스트 내용
    public bool isBold;           // Bold 여부
}

[System.Serializable]
public class DictionaryPageData
{
    [Header("Page Content")]
    public Sprite faceImage;        // 얼굴 이미지
    
    [Header("Detail Text (Segments)")]
    public TextSegment[] textSegments;  // 텍스트 세그먼트 배열 (일반 텍스트와 bold 텍스트를 분리)
}

public class DictionaryDetailManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image faceImage;           // FaceImage
    [SerializeField] private Image pageImage;           // Dictionary_page (공통 이미지)
    [SerializeField] private TextMeshProUGUI detailText; // Detail_Text
    [SerializeField] private Button leftButton;         // LeftButton
    [SerializeField] private Button rightButton;       // RightButton
    
    [Header("Page Image (Common)")]
    [SerializeField] private Sprite commonPageImage;    // 모든 페이지에서 공통으로 사용하는 페이지 이미지
    
    [Header("Dictionary Data")]
    [SerializeField] private DictionaryPageData[] pages = new DictionaryPageData[3]; // 3가지 도깨비 데이터
    
    private int currentPageIndex = 0;
    
    void Start()
    {
        // 공통 페이지 이미지 설정
        if (pageImage != null && commonPageImage != null)
        {
            pageImage.sprite = commonPageImage;
        }
        
        // 버튼 이벤트 연결
        if (leftButton != null)
        {
            leftButton.onClick.AddListener(OnLeftButtonClicked);
        }
        
        if (rightButton != null)
        {
            rightButton.onClick.AddListener(OnRightButtonClicked);
        }
        
        // 초기 페이지 표시
        UpdatePage();
    }
    
    void OnEnable()
    {
        // Dictionary_detail이 활성화될 때 첫 페이지로 리셋
        currentPageIndex = 0;
        UpdatePage();
    }
    
    private void OnLeftButtonClicked()
    {
        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            UpdatePage();
        }
    }
    
    private void OnRightButtonClicked()
    {
        if (currentPageIndex < pages.Length - 1)
        {
            currentPageIndex++;
            UpdatePage();
        }
    }
    
    private void UpdatePage()
    {
        if (pages == null || pages.Length == 0)
        {
            Debug.LogWarning("DictionaryDetailManager: 페이지 데이터가 없습니다.");
            return;
        }
        
        if (currentPageIndex < 0 || currentPageIndex >= pages.Length)
        {
            Debug.LogWarning($"DictionaryDetailManager: 잘못된 페이지 인덱스: {currentPageIndex}");
            return;
        }
        
        DictionaryPageData currentPage = pages[currentPageIndex];
        
        // 얼굴 이미지 업데이트
        if (faceImage != null && currentPage.faceImage != null)
        {
            faceImage.sprite = currentPage.faceImage;
        }
        
        // 텍스트 업데이트 (일반 텍스트와 bold 텍스트를 조합)
        if (detailText != null)
        {
            detailText.text = BuildFormattedText(currentPage.textSegments);
        }
        
        // 버튼 활성화/비활성화
        if (leftButton != null)
        {
            leftButton.interactable = currentPageIndex > 0;
        }
        
        if (rightButton != null)
        {
            rightButton.interactable = currentPageIndex < pages.Length - 1;
        }
    }
    
    private string BuildFormattedText(TextSegment[] segments)
    {
        if (segments == null || segments.Length == 0)
        {
            return "";
        }
        
        string formattedText = "";
        
        for (int i = 0; i < segments.Length; i++)
        {
            TextSegment segment = segments[i];
            if (segment == null || string.IsNullOrEmpty(segment.text))
            {
                continue;
            }
            
            // 줄바꿈 문자 정규화 (\r\n -> \n)
            string normalizedText = segment.text.Replace("\r\n", "\n").Replace("\r", "\n");
            
            if (segment.isBold)
            {
                // TextMeshPro의 bold 태그 사용
                // 줄바꿈을 보존하면서 bold 태그 적용
                formattedText += "<b>" + normalizedText + "</b>";
            }
            else
            {
                formattedText += normalizedText;
            }
        }
        
        return formattedText;
    }
    
    void OnDestroy()
    {
        // 메모리 누수 방지를 위해 이벤트 제거
        if (leftButton != null)
        {
            leftButton.onClick.RemoveListener(OnLeftButtonClicked);
        }
        
        if (rightButton != null)
        {
            rightButton.onClick.RemoveListener(OnRightButtonClicked);
        }
    }
}

