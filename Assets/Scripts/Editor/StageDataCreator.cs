using UnityEngine;
using UnityEditor;

public class StageDataCreator : EditorWindow
{
    [MenuItem("Will-O-The-Wisp/Create Stage Data")]
    [MenuItem("Tools/Create Stage Data")]
    public static void ShowWindow()
    {
        GetWindow<StageDataCreator>("Create Stage Data");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Stage Data Creator", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        if (GUILayout.Button("Create Stage 1 & 2", GUILayout.Height(30)))
        {
            CreateStages();
        }
        
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox("StageManager 오브젝트를 선택한 상태에서 실행하세요.", MessageType.Info);
    }
    
    private void CreateStages()
    {
        StageManager stageManager = FindObjectOfType<StageManager>();
        
        if (stageManager == null)
        {
            EditorUtility.DisplayDialog("오류", "StageManager를 찾을 수 없습니다.\nGameScene에서 StageManager 오브젝트를 선택한 후 다시 시도하세요.", "확인");
            return;
        }
        
        // 기존 스테이지 데이터 클리어
        stageManager.stages.Clear();
        
        // Stage 1 생성
        StageData stage1 = CreateStage1();
        stageManager.stages.Add(stage1);
        
        // Stage 2 생성
        StageData stage2 = CreateStage2();
        stageManager.stages.Add(stage2);
        
        // 변경사항 저장
        EditorUtility.SetDirty(stageManager);
        
        EditorUtility.DisplayDialog("완료", $"Stage 1과 Stage 2가 생성되었습니다!\n총 {stageManager.stages.Count}개의 스테이지가 있습니다.", "확인");
        
        Debug.Log("Stage data created successfully!");
    }
    
    private StageData CreateStage1()
    {
        StageData stage = new StageData();
        stage.stageNumber = 1;
        stage.movesLimit = -1; // 무제한 (Stage 1-2는 무제한)
        
        // Stage 1 타일 레이아웃
        // 기획서의 Stage 1 디자인을 기반으로 작성
        // 좌표: (x, y) = (열, 행), 0부터 시작
        // [0,0] [1,0] [2,0] [3,0]
        // [0,1] [1,1] [2,1] [3,1]
        // [0,2] [1,2] [2,2] [3,2]
        // [0,3] [1,3] [2,3] [3,3]
        
        stage.tileLayout = new TileType[4, 4]
        {
            // 행 0 (위에서 첫 번째)
            { TileType.Start, TileType.Straight, TileType.Corner, TileType.Straight },
            // 행 1
            { TileType.Corner, TileType.Straight, TileType.Corner, TileType.Clear },
            // 행 2
            { TileType.Straight, TileType.Straight, TileType.Straight, TileType.Straight },
            // 행 3 (아래에서 첫 번째)
            { TileType.Straight, TileType.Lose, TileType.Corner, TileType.Corner }
        };
        
        // 위치 설정 (그리드 좌표: x=열, y=행)
        stage.startPosition = new Vector2Int(0, 0);  // H 위치 (왼쪽 위)
        stage.clearPosition = new Vector2Int(3, 1); // B 위치 (오른쪽 위에서 두 번째)
        stage.losePosition = new Vector2Int(1, 3);  // A 위치 (왼쪽 아래에서 두 번째)
        
        return stage;
    }
    
    private StageData CreateStage2()
    {
        StageData stage = new StageData();
        stage.stageNumber = 2;
        stage.movesLimit = -1; // 무제한
        
        // Stage 2 타일 레이아웃 (기획서 initial 상태 기반)
        // 기획서에서 본 디자인:
        // 1행: H(원), -, L, |
        // 2행: L, -, ¬, B(원)
        // 3행: |, |, |, |
        // 4행: -, A(원), L, L
        stage.tileLayout = new TileType[4, 4]
        {
            { TileType.Start, TileType.Straight, TileType.Corner, TileType.Straight },  // H, -, L, |
            { TileType.Corner, TileType.Straight, TileType.Corner, TileType.Clear },     // L, -, ¬, B
            { TileType.Straight, TileType.Straight, TileType.Straight, TileType.Straight }, // |, |, |, |
            { TileType.Straight, TileType.Lose, TileType.Corner, TileType.Corner }      // -, A, L, L
        };
        
        // 위치 설정 (기획서 기반)
        stage.startPosition = new Vector2Int(0, 0);  // H 위치 (왼쪽 위)
        stage.clearPosition = new Vector2Int(3, 1);    // B 위치 (오른쪽 위에서 두 번째)
        stage.losePosition = new Vector2Int(1, 3);   // A 위치 (왼쪽 아래에서 두 번째)
        
        return stage;
    }
}

