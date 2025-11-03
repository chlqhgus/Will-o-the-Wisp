using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    private const string FIRST_PLAY_KEY = "HasPlayedBefore";
    
    public void OnStartGameClicked()
    {
        // 첫 플레이인지 확인
        bool hasPlayedBefore = PlayerPrefs.GetInt(FIRST_PLAY_KEY, 0) == 1;
        
        if (!hasPlayedBefore)
        {
            // 첫 플레이면 스테이지 1부터 시작
            PlayerPrefs.SetInt(FIRST_PLAY_KEY, 1);
            PlayerPrefs.SetInt("CurrentStage", 1); // 스테이지 1로 설정
            PlayerPrefs.Save();
            SceneManager.LoadScene("GameScene");
        }
        else
        {
            // 이미 플레이한 적이 있으면 스테이지 선택 화면으로
            // TODO: 스테이지 선택 화면 구현 후 추가
            // 지금은 일단 GameScene으로 이동
            SceneManager.LoadScene("GameScene");
        }
    }
    
    public void OnSettingsClicked()
    {
        // TODO: 설정 화면 구현
        Debug.Log("Settings clicked");
    }
    
    public void OnExitGameClicked()
    {
        Application.Quit();
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}

