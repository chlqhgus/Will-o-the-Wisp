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
    private string csvFilePath = "Assets/Bohyun/HumanNPC_Script.csv";
    private string npcNameColumn = "npc-name";
    private bool autoAssignToPrefabs = true;

    [MenuItem("Tools/NPC Data CSV Importer")]
    public static void ShowWindow()
    {
        GetWindow<NPCDataCSVImporter>("NPC Data CSV Importer");
    }

    void OnGUI()
    {
        GUILayout.Label("CSV 파일에서 NPC 대사 임포트", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        csvFilePath = EditorGUILayout.TextField("CSV 파일 경로 (Assets 기준, 확장자 포함):", csvFilePath);
        npcNameColumn = EditorGUILayout.TextField("NPC 이름 컬럼:", npcNameColumn);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("옵션", EditorStyles.boldLabel);
        autoAssignToPrefabs = EditorGUILayout.Toggle("NPC 프리팹에 자동 할당", autoAssignToPrefabs);

        EditorGUILayout.Space();

        if (GUILayout.Button("CSV에서 모든 NPCData 업데이트", GUILayout.Height(30)))
        {
            ImportAllNPCData();
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("CSV 파일 형식:\n" +
                                "- 첫 번째 행: 헤더 (컬럼 이름)\n" +
                                "- npc-name: NPC 신분 (King, Yangban, Physician, Merchant, Slave, Shaman)\n" +
                                "- rice-request: 밥 요청 대사\n" +
                                "- medicine-request: 약 요청 대사\n" +
                                "- rice-accept: 밥 받았을 때 대사\n" +
                                "- medicine-accept: 약 받았을 때 대사\n" +
                                "- rice-reject: 밥 거절당했을 때 대사\n" +
                                "- medicine-reject: 약 거절당했을 때 대사\n" +
                                "- rice-re-accept: 밥 재요청 후 수락 대사\n" +
                                "- medicine-re-accept: 약 재요청 후 수락 대사\n" +
                                "- rice-re-reject: 밥 재요청 후 재거절 대사\n" +
                                "- medicine-re-reject: 약 재요청 후 재거절 대사\n" +
                                "\n재요청 대사(Please...)는 자동으로 추가됩니다.", MessageType.Info);
    }

    void ImportAllNPCData()
    {
        var csvData = CSVReader.ReadCSV(csvFilePath);
        if (csvData == null || csvData.Count == 0)
        {
            EditorUtility.DisplayDialog("오류", $"CSV 파일을 읽을 수 없습니다: {csvFilePath}", "확인");
            return;
        }

        Debug.Log($"NPCDataCSVImporter: CSV 파일에서 {csvData.Count}개의 행을 읽었습니다.");

        // CSV에서 모든 NPC 이름 수집
        HashSet<string> npcNames = new HashSet<string>();
        foreach (var row in csvData)
        {
            if (row.ContainsKey(npcNameColumn) && !string.IsNullOrEmpty(row[npcNameColumn]))
            {
                string npcName = row[npcNameColumn].Trim();
                if (!string.IsNullOrEmpty(npcName))
                {
                    npcNames.Add(npcName);
                    Debug.Log($"NPCDataCSVImporter: NPC 이름 발견: '{npcName}'");
                }
            }
            else
            {
                Debug.LogWarning($"NPCDataCSVImporter: 행에서 '{npcNameColumn}' 컬럼을 찾을 수 없습니다. 사용 가능한 컬럼: {string.Join(", ", row.Keys)}");
            }
        }

        Debug.Log($"NPCDataCSVImporter: 총 {npcNames.Count}개의 고유한 NPC 이름을 찾았습니다: {string.Join(", ", npcNames)}");

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
            Debug.Log($"NPCDataCSVImporter: {npcName}의 데이터를 CSV에서 로드 중...");
            npcData.LoadFromCSV(csvFilePath, npcNameColumn);
            
            // 로드된 대사 수 확인
            int foodRequestCount = npcData.foodRequestLines != null ? npcData.foodRequestLines.Length : 0;
            int medicineRequestCount = npcData.medicineRequestLines != null ? npcData.medicineRequestLines.Length : 0;
            Debug.Log($"NPCDataCSVImporter: {npcName} 로드 완료 - 밥 요청: {foodRequestCount}개, 약 요청: {medicineRequestCount}개");
        }

        AssetDatabase.SaveAssets();
        
        // NPC 프리팹에 자동 할당
        int assignedCount = 0;
        if (autoAssignToPrefabs)
        {
            // existingData에 새로 생성된 데이터도 추가
            foreach (string npcName in npcNames)
            {
                if (!existingData.ContainsKey(npcName))
                {
                    string fileName = npcName.Replace("'", "").Replace(" ", "").Replace("/", "");
                    string assetPath = $"Assets/Bohyun/{fileName}Data.asset";
                    
                    // 파일 찾기
                    int counter = 1;
                    while (!File.Exists(assetPath) && counter < 10)
                    {
                        assetPath = $"Assets/Bohyun/{fileName}Data_{counter}.asset";
                        counter++;
                    }
                    
                    if (File.Exists(assetPath))
                    {
                        BohyunNPCData npcData = AssetDatabase.LoadAssetAtPath<BohyunNPCData>(assetPath);
                        if (npcData != null)
                        {
                            existingData[npcName] = npcData;
                        }
                    }
                }
            }
            
            assignedCount = AssignDataToPrefabs(existingData);
        }
        
        EditorUtility.DisplayDialog("완료", 
            $"{createdCount}개의 NPCData를 생성하고, {updatedCount}개의 NPCData를 업데이트했습니다.\n총 {npcNames.Count}개의 NPC 데이터가 처리되었습니다.\n{assignedCount}개의 NPC 프리팹에 데이터가 할당되었습니다.", 
            "확인");
    }
    
    /// <summary>
    /// 생성된 NPCData를 같은 신분의 NPC 프리팹에 자동으로 할당합니다.
    /// </summary>
    int AssignDataToPrefabs(Dictionary<string, BohyunNPCData> npcDataDict)
    {
        int assignedCount = 0;
        
        // Assets/Bohyun/Prefab 폴더에서 모든 프리팹 찾기
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Bohyun/Prefab" });
        
        foreach (string guid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            
            if (prefab == null) continue;
            
            // NPCComponent 찾기 (없으면 추가)
            NPCComponent npcComponent = prefab.GetComponent<NPCComponent>();
            if (npcComponent == null)
            {
                npcComponent = prefab.AddComponent<NPCComponent>();
                Debug.Log($"NPCDataCSVImporter: {prefab.name} 프리팹에 NPCComponent를 추가했습니다.");
            }
            
            // 프리팹 이름에서 신분 추출 (예: "King", "Yangban1", "Shaman" 등)
            string prefabName = prefab.name;
            string npcStatus = ExtractNPCStatus(prefabName);
            
            if (string.IsNullOrEmpty(npcStatus)) continue;
            
            // 해당 신분의 NPCData 찾기
            if (npcDataDict.ContainsKey(npcStatus))
            {
                npcComponent.bohyunData = npcDataDict[npcStatus];
                EditorUtility.SetDirty(prefab);
                assignedCount++;
            }
        }
        
        if (assignedCount > 0)
        {
            AssetDatabase.SaveAssets();
        }
        
        return assignedCount;
    }
    
    /// <summary>
    /// 프리팹 이름에서 NPC 신분을 추출합니다.
    /// 예: "King" -> "King", "Yangban1" -> "Yangban", "Shaman" -> "Shaman"
    /// </summary>
    string ExtractNPCStatus(string prefabName)
    {
        // 일반적인 신분 이름들
        string[] statuses = { "King", "Yangban", "Physician", "Merchant", "Slave", "Shaman", "Concubine" };
        
        foreach (string status in statuses)
        {
            if (prefabName.StartsWith(status, System.StringComparison.OrdinalIgnoreCase))
            {
                return status;
            }
        }
        
        return null;
    }
}
#endif

