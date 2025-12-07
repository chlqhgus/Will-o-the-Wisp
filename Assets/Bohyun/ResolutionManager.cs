using UnityEngine;

/// <summary>
/// 게임 시작 시 해상도를 1920x1080으로 고정하는 스크립트
/// </summary>
public class ResolutionManager : MonoBehaviour
{
    [Header("Resolution Settings")]
    [SerializeField] private int targetWidth = 1920;
    [SerializeField] private int targetHeight = 1080;
    [SerializeField] private bool forceResolutionOnStart = true;
    [SerializeField] private bool preventWindowResize = true;
    
    private static ResolutionManager instance;
    
    void Awake()
    {
        // 싱글톤 패턴으로 중복 생성 방지
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        if (forceResolutionOnStart)
        {
            SetResolution();
        }
    }
    
    void Start()
    {
        // Start에서도 한 번 더 설정 (혹시 모를 경우 대비)
        if (forceResolutionOnStart)
        {
            SetResolution();
        }
    }
    
    void Update()
    {
        // 매 프레임 해상도 확인 및 강제 설정 (빌드 시 해상도 변경 방지)
        if (preventWindowResize)
        {
            if (Screen.width != targetWidth || Screen.height != targetHeight)
            {
                SetResolution();
            }
        }
    }
    
    /// <summary>
    /// 해상도를 목표 해상도로 설정
    /// </summary>
    private void SetResolution()
    {
        // Unity 최신 API 사용 (refreshRate 대신 refreshRateRatio 사용)
        RefreshRate refreshRate = Screen.currentResolution.refreshRateRatio;
        Screen.SetResolution(targetWidth, targetHeight, Screen.fullScreenMode, refreshRate);
        Debug.Log($"Resolution set to: {targetWidth}x{targetHeight}");
    }
    
    /// <summary>
    /// 외부에서 해상도를 변경하고 싶을 때 사용
    /// </summary>
    public void ChangeResolution(int width, int height)
    {
        targetWidth = width;
        targetHeight = height;
        SetResolution();
    }
}

