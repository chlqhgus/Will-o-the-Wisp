using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class DictionaryPageData
{
    [Header("Page Content")]
    public Sprite faceImage;        // 얼굴 이미지
    
    [Header("Text Content")]
    [TextArea(3, 10)]
    public string featureText;     // Feature 설명 텍스트
    [TextArea(3, 10)]
    public string descriptionText; // 내용 설명 텍스트
}

public class DictionaryDetailManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image faceImage;           // FaceImage
    [SerializeField] private Image pageImage;           // Dictionary_page (공통 이미지)
    [SerializeField] private TextMeshProUGUI featureText; // Feature 설명 TextMeshProUGUI
    [SerializeField] private TextMeshProUGUI descriptionText; // 내용 설명 TextMeshProUGUI
    [SerializeField] private Button leftButton;         // LeftButton
    [SerializeField] private Button rightButton;       // RightButton
    [SerializeField] private Button closeButton;        // CloseButton
    
    [Header("Page Image (Common)")]
    [SerializeField] private Sprite commonPageImage;    // 모든 페이지에서 공통으로 사용하는 페이지 이미지
    
    [Header("Sound Effect")]
    [SerializeField] private AudioClip bookSoundEffect;  // 책 관련 사운드 이펙트 (클릭, 넘기기, 닫기)
    [SerializeField] private AudioSource audioSource;    // AudioSource (없으면 자동으로 찾거나 생성)
    
    [Header("Dictionary Data")]
    [SerializeField] private DictionaryPageData[] pages = new DictionaryPageData[3]; // 3가지 도깨비 데이터
    
    private int currentPageIndex = 0;
    
    void Start()
    {
        // AudioSource 설정 (없으면 자동으로 찾거나 생성)
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
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
        
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseButtonClicked);
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
            PlayBookSound();
            currentPageIndex--;
            UpdatePage();
        }
    }
    
    private void OnRightButtonClicked()
    {
        if (currentPageIndex < pages.Length - 1)
        {
            PlayBookSound();
            currentPageIndex++;
            UpdatePage();
        }
    }
    
    private void OnCloseButtonClicked()
    {
        PlayBookSound();
        // Dictionary_detail을 비활성화 (Unity의 OnClick 이벤트에서도 처리되지만, 사운드를 먼저 재생)
        gameObject.SetActive(false);
    }
    
    public void PlayBookSound()
    {
        if (bookSoundEffect != null && audioSource != null)
        {
            audioSource.PlayOneShot(bookSoundEffect);
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
        
        // Feature 텍스트 업데이트
        if (featureText != null)
        {
            featureText.text = currentPage.featureText ?? "";
        }
        
        // 내용 설명 텍스트 업데이트
        if (descriptionText != null)
        {
            descriptionText.text = currentPage.descriptionText ?? "";
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
        
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(OnCloseButtonClicked);
        }
    }
}

