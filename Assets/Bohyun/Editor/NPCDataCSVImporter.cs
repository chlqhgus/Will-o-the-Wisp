#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// NPCData를 CSV 파일에서 자동으로 임포트하는 에디터 윈도우
/// </summary>
public class NPCDataCSVImporter : EditorWindow
{
    private string csvFilePath = "Assets/Bohyun/CE_HumanNPC_dialogue";
    private string npcNameColumn = "npc-name";

    [MenuItem("Tools/NPC Data CSV Importer")]
    public static void ShowWindow()
    {
        GetWindow<NPCDataCSVImporter>("NPC Data CSV Importer");
    }

    void OnGUI()
    {
        GUILayout.Label("CSV 파일에서 NPC 대사 임포트", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        csvFilePath = EditorGUILayout.TextField("CSV 파일 경로 (Resources 기준, 확장자 제외):", csvFilePath);
        npcNameColumn = EditorGUILayout.TextField("NPC 이름 컬럼:", npcNameColumn);

        EditorGUILayout.Space();

        if (GUILayout.Button("CSV에서 모든 NPCData 업데이트", GUILayout.Height(30)))
        {
            ImportAllNPCData();
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("CSV 파일 형식:\n" +
                                "- 첫 번째 행: 헤더 (컬럼 이름)\n" +
                                "- npc-name: NPC 이름\n" +
                                "- rice-request: 밥 요청 대사\n" +
                                "- medicine-request: 약 요청 대사\n" +
                                "- rice-accept: 밥 받았을 때 대사\n" +
                                "- medicine-accept: 약 받았을 때 대사\n" +
                                "- rice-reject: 밥 거절당했을 때 대사\n" +
                                "- medicine-reject: 약 거절당했을 때 대사\n" +
                                "- special: 특별 대사", MessageType.Info);
    }

    void ImportAllNPCData()
    {
        var csvData = CSVReader.ReadCSV(csvFilePath);
        if (csvData == null || csvData.Count == 0)
        {
            EditorUtility.DisplayDialog("오류", $"CSV 파일을 읽을 수 없습니다: {csvFilePath}", "확인");
            return;
        }

        // CSV에서 모든 NPC 이름 수집
        HashSet<string> npcNames = new HashSet<string>();
        foreach (var row in csvData)
        {
            if (row.ContainsKey(npcNameColumn) && !string.IsNullOrEmpty(row[npcNameColumn]))
            {
                npcNames.Add(row[npcNameColumn]);
            }
        }

        if (npcNames.Count == 0)
        {
            EditorUtility.DisplayDialog("오류", "CSV 파일에서 NPC 이름을 찾을 수 없습니다.", "확인");
            return;
        }

        // 기존 BohyunNPCData 찾기 (이름으로 매칭)
        Dictionary<string, BohyunNPCData> existingData = new Dictionary<string, BohyunNPCData>();
        string[] guids = AssetDatabase.FindAssets("t:BohyunNPCData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            BohyunNPCData npcData = AssetDatabase.LoadAssetAtPath<BohyunNPCData>(path);
            if (npcData != null && !string.IsNullOrEmpty(npcData.npcName))
            {
                existingData[npcData.npcName] = npcData;
            }
        }

        int createdCount = 0;
        int updatedCount = 0;

        // 각 NPC에 대해 데이터 생성 또는 업데이트
        foreach (string npcName in npcNames)
        {
            BohyunNPCData npcData;
            
            if (existingData.ContainsKey(npcName))
            {
                // 기존 데이터 업데이트
                npcData = existingData[npcName];
                updatedCount++;
            }
            else
            {
                // 새 데이터 생성
                npcData = ScriptableObject.CreateInstance<BohyunNPCData>();
                npcData.npcName = npcName;
                
                // 파일 이름 생성 (특수문자 제거)
                string fileName = npcName.Replace("'", "").Replace(" ", "").Replace("/", "");
                string assetPath = $"Assets/Bohyun/{fileName}Data.asset";
                
                // 파일이 이미 있으면 번호 추가
                int counter = 1;
                while (File.Exists(assetPath))
                {
                    assetPath = $"Assets/Bohyun/{fileName}Data_{counter}.asset";
                    counter++;
                }
                
                AssetDatabase.CreateAsset(npcData, assetPath);
                createdCount++;
            }
            
            // CSV에서 데이터 로드
            npcData.LoadFromCSV(csvFilePath, npcNameColumn);
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("완료", 
            $"{createdCount}개의 NPCData를 생성하고, {updatedCount}개의 NPCData를 업데이트했습니다.\n총 {npcNames.Count}개의 NPC 데이터가 처리되었습니다.", 
            "확인");
    }
}
#endif

