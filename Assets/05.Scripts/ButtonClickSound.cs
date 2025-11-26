using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonClickSound : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource; // 오디오 소스 (없으면 자동 생성)
    [SerializeField] private AudioClip clickSound; // 클릭 소리 클립
    [SerializeField] private float volume = 0.5f; // 볼륨
    
    private Button button;
    
    void Start()
    {
        InitializeButton();
        InitializeAudio();
    }
    
    private void InitializeButton()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClicked);
        }
    }
    
    private void InitializeAudio()
    {
        // AudioSource가 없으면 자동으로 생성
        if (audioSource == null)
        {
            GameObject audioObject = new GameObject("ButtonAudioSource");
            audioObject.transform.SetParent(transform);
            audioSource = audioObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.volume = volume;
        }
        else
        {
            // 기존 AudioSource 설정
            audioSource.playOnAwake = false;
            audioSource.volume = volume;
        }
    }
    
    private void OnButtonClicked()
    {
        // 클릭 소리 재생
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound, volume);
        }
    }
    
    private void OnDestroy()
    {
        // 메모리 누수 방지를 위해 이벤트 제거
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClicked);
        }
    }
}

