using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// CSV 파일에서 NPC 대사를 로드하여 NPCData에 자동으로 채워주는 컴포넌트
/// </summary>
public class NPCDataCSVLoader : MonoBehaviour
{
    [Header("CSV File Settings")]
    [Tooltip("Resources 폴더 기준 경로 (확장자 제외, 예: 'Data/NPC_Dialogue')")]
    public string csvFilePath = "Data/NPC_Dialogue";
    
    [Header("CSV Column Names")]
    [Tooltip("CSV 파일의 컬럼 이름들")]
    public string npcNameColumn = "NPCName";
    public string requestTypeColumn = "RequestType";
    public string dialogueColumn = "Dialogue";
    public string dialogueTypeColumn = "DialogueType"; // "general", "food", "medicine"

    private Dictionary<string, BohyunNPCData> npcDataCache = new Dictionary<string, BohyunNPCData>();

    void Start()
    {
        LoadCSVData();
    }

    /// <summary>
    /// CSV 파일을 읽어서 NPCData를 업데이트합니다.
    /// </summary>
    public void LoadCSVData()
    {
        List<Dictionary<string, string>> csvData = CSVReader.ReadCSV(csvFilePath);
        
        if (csvData == null || csvData.Count == 0)
        {
            Debug.LogWarning($"NPCDataCSVLoader: CSV 데이터를 읽을 수 없습니다: {csvFilePath}");
            return;
        }

        // NPC별로 그룹화
        Dictionary<string, List<Dictionary<string, string>>> npcGroups = new Dictionary<string, List<Dictionary<string, string>>>();
        
        foreach (var row in csvData)
        {
            if (!row.ContainsKey(npcNameColumn)) continue;
            
            string npcName = row[npcNameColumn];
            if (string.IsNullOrEmpty(npcName)) continue;

            if (!npcGroups.ContainsKey(npcName))
            {
                npcGroups[npcName] = new List<Dictionary<string, string>>();
            }
            npcGroups[npcName].Add(row);
        }

        // 각 NPC의 데이터를 NPCData에 적용
        foreach (var npcGroup in npcGroups)
        {
            ApplyDataToNPCData(npcGroup.Key, npcGroup.Value);
        }

        Debug.Log($"NPCDataCSVLoader: {npcGroups.Count}명의 NPC 데이터를 로드했습니다.");
    }

    /// <summary>
    /// CSV 데이터를 BohyunNPCData에 적용합니다.
    /// </summary>
    void ApplyDataToNPCData(string npcName, List<Dictionary<string, string>> rows)
    {
        // BohyunNPCData 찾기 (Resources에서 로드하거나 씬에서 찾기)
        BohyunNPCData npcData = FindNPCData(npcName);
        
        if (npcData == null)
        {
            Debug.LogWarning($"NPCDataCSVLoader: {npcName}의 BohyunNPCData를 찾을 수 없습니다.");
            return;
        }

        List<string> generalLines = new List<string>();
        List<string> foodLines = new List<string>();
        List<string> medicineLines = new List<string>();

        foreach (var row in rows)
        {
            // 대사 타입 확인
            string dialogueType = row.ContainsKey(dialogueTypeColumn) ? row[dialogueTypeColumn].ToLower() : "general";
            string dialogue = row.ContainsKey(dialogueColumn) ? row[dialogueColumn] : "";

            if (string.IsNullOrEmpty(dialogue)) continue;

            // 대사 타입에 따라 분류
            if (dialogueType == "food" || dialogueType == "밥")
            {
                foodLines.Add(dialogue);
            }
            else if (dialogueType == "medicine" || dialogueType == "약")
            {
                medicineLines.Add(dialogue);
            }
            else
            {
                generalLines.Add(dialogue);
            }
        }

        // NPCData에 적용 (런타임에서는 ScriptableObject를 직접 수정할 수 없으므로 주의)
        // 에디터에서만 작동하도록 하거나, 런타임 데이터 구조를 별도로 만들어야 함
        Debug.Log($"NPCDataCSVLoader: {npcName} - 일반:{generalLines.Count}, 밥:{foodLines.Count}, 약:{medicineLines.Count}");
        
        // 런타임에서는 NPC 컴포넌트에 직접 적용하는 방식 사용
        ApplyToNPCComponents(npcName, generalLines, foodLines, medicineLines);
    }

    /// <summary>
    /// NPC 이름으로 BohyunNPCData를 찾습니다.
    /// </summary>
    BohyunNPCData FindNPCData(string npcName)
    {
        // Resources 폴더에서 모든 BohyunNPCData 로드
        BohyunNPCData[] allNPCData = Resources.LoadAll<BohyunNPCData>("");
        
        foreach (var data in allNPCData)
        {
            if (data.npcName == npcName)
            {
                return data;
            }
        }

        return null;
    }

    /// <summary>
    /// NPC 컴포넌트에 직접 데이터를 적용합니다 (런타임용).
    /// </summary>
    void ApplyToNPCComponents(string npcName, List<string> generalLines, List<string> foodLines, 
                              List<string> medicineLines)
    {
        // NPC.cs가 삭제되어 NPCComponent를 직접 찾기
        NPCComponent[] allNPCComponents = FindObjectsByType<NPCComponent>(FindObjectsSortMode.None);
        
        foreach (NPCComponent bohyunNPC in allNPCComponents)
        {
            if (bohyunNPC != null && bohyunNPC.bohyunData != null && bohyunNPC.bohyunData.npcName == npcName)
            {
                // BohyunNPCData는 ScriptableObject이므로 런타임 수정 불가
                // 에디터에서만 수정 가능
            }
        }
    }
}

