using UnityEngine;
using UnityEditor;

// Unity 에디터 상단 메뉴에서 찾을 수 없는 경우를 위한 대안
// 이 스크립트는 Hierarchy 창에서 우클릭 메뉴로도 접근 가능합니다
public class QuickGameSetup : MonoBehaviour
{
    [ContextMenu("Open Game Setup Helper")]
    void OpenSetupHelper()
    {
        EditorApplication.ExecuteMenuItem("Will-O-The-Wisp/Game Setup Helper");
        if (!EditorApplication.ExecuteMenuItem("Will-O-The-Wisp/Game Setup Helper"))
        {
            EditorApplication.ExecuteMenuItem("Tools/Game Setup Helper");
        }
    }
}

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(QuickGameSetup))]
public class QuickGameSetupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Open Game Setup Helper", GUILayout.Height(30)))
        {
            if (System.Type.GetType("GameSetupHelper") != null)
            {
                System.Reflection.MethodInfo showWindow = System.Type.GetType("GameSetupHelper").GetMethod("ShowWindow");
                if (showWindow != null)
                {
                    showWindow.Invoke(null, null);
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Game Setup Helper", 
                    "Game Setup Helper 창을 열 수 없습니다.\n\nUnity 에디터 상단 메뉴에서:\nWindow > Will-O-The-Wisp > Game Setup Helper\n또는\nTools > Game Setup Helper\n를 찾아보세요.", 
                    "확인");
            }
        }
    }
}
#endif

