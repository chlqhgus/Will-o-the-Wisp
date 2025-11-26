using System.Collections.Generic;
using UnityEngine;

public enum BohyunNPCRequestType
{
    Food,      // 밥 요청
    Medicine   // 약 요청
}

[CreateAssetMenu(fileName = "BohyunNPCData", menuName = "Game/Bohyun NPC Data")]
public class BohyunNPCData : ScriptableObject
{
    public string npcName;
    
    [Header("Request Settings")]
    [Tooltip("이 NPC가 요청하는 것 (밥 또는 약)")]
    public BohyunNPCRequestType requestType = BohyunNPCRequestType.Food;
    
    [Header("Dialogue")]
    [TextArea(2, 5)]
    public string[] foodRequestLines; // 밥 요청 대사
    
    [TextArea(2, 5)]
    public string[] medicineRequestLines; // 약 요청 대사
    
    [TextArea(2, 5)]
    public string[] foodAcceptLines; // 밥 받았을 때 대사
    
    [TextArea(2, 5)]
    public string[] medicineAcceptLines; // 약 받았을 때 대사
    
    [TextArea(2, 5)]
    public string[] foodRejectLines; // 밥 거절당했을 때 대사
    
    [TextArea(2, 5)]
    public string[] medicineRejectLines; // 약 거절당했을 때 대사
    
    [TextArea(2, 5)]
    public string[] foodReRequestLines; // 밥 한 번 거절 후 다시 요청하는 대사 (Please...)
    
    [TextArea(2, 5)]
    public string[] medicineReRequestLines; // 약 한 번 거절 후 다시 요청하는 대사 (Please...)
    
    [TextArea(2, 5)]
    public string[] foodReAcceptLines; // 밥 한 번 거절 후 다시 받았을 때 대사
    
    [TextArea(2, 5)]
    public string[] medicineReAcceptLines; // 약 한 번 거절 후 다시 받았을 때 대사
    
    [TextArea(2, 5)]
    public string[] foodReRejectLines; // 밥 두 번 거절당했을 때 대사
    
    [TextArea(2, 5)]
    public string[] medicineReRejectLines; // 약 두 번 거절당했을 때 대사
    
    [TextArea(2, 5)]
    public string[] specialLines; // 특별 대사
    
    [Header("CSV Import (선택사항)")]
    [Tooltip("CSV 파일에서 자동으로 로드할 경로 (Resources 폴더 기준, 확장자 제외)")]
    public string csvImportPath = "";
    
    /// <summary>
    /// CSV 파일에서 데이터를 로드합니다 (에디터 전용).
    /// </summary>
    public void LoadFromCSV(string csvFilePath, string npcNameColumn = "npc-name")
    {
        #if UNITY_EDITOR
        var csvData = CSVReader.ReadCSV(csvFilePath);
        if (csvData == null || csvData.Count == 0) return;

        List<string> foodRequest = new List<string>();
        List<string> medicineRequest = new List<string>();
        List<string> foodAccept = new List<string>();
        List<string> medicineAccept = new List<string>();
        List<string> foodReject = new List<string>();
        List<string> medicineReject = new List<string>();
        List<string> foodReRequest = new List<string>();
        List<string> medicineReRequest = new List<string>();
        List<string> foodReAccept = new List<string>();
        List<string> medicineReAccept = new List<string>();
        List<string> foodReReject = new List<string>();
        List<string> medicineReReject = new List<string>();
        List<string> special = new List<string>();

        foreach (var row in csvData)
        {
            if (!row.ContainsKey(npcNameColumn) || row[npcNameColumn] != npcName) continue;
            
            // 밥 요청 대사
            if (row.ContainsKey("rice-request") && !string.IsNullOrEmpty(row["rice-request"]) && row["rice-request"] != "-")
                foodRequest.Add(row["rice-request"]);
            
            // 약 요청 대사
            if (row.ContainsKey("medicine-request") && !string.IsNullOrEmpty(row["medicine-request"]) && row["medicine-request"] != "-")
                medicineRequest.Add(row["medicine-request"]);
            
            // 밥 받았을 때
            if (row.ContainsKey("rice-accept") && !string.IsNullOrEmpty(row["rice-accept"]) && row["rice-accept"] != "-")
                foodAccept.Add(row["rice-accept"]);
            
            // 약 받았을 때
            if (row.ContainsKey("medicine-accept") && !string.IsNullOrEmpty(row["medicine-accept"]) && row["medicine-accept"] != "-")
                medicineAccept.Add(row["medicine-accept"]);
            
            // 밥 거절당했을 때
            if (row.ContainsKey("rice-reject") && !string.IsNullOrEmpty(row["rice-reject"]) && row["rice-reject"] != "-")
                foodReject.Add(row["rice-reject"]);
            
            // 약 거절당했을 때
            if (row.ContainsKey("medicine-reject") && !string.IsNullOrEmpty(row["medicine-reject"]) && row["medicine-reject"] != "-")
                medicineReject.Add(row["medicine-reject"]);
            
            // 밥 한 번 거절 후 다시 요청 (rice-re-request가 없으면 rice-re-accept나 rice-re-reject가 있으면 자동으로 재요청)
            if (row.ContainsKey("rice-re-accept") && !string.IsNullOrEmpty(row["rice-re-accept"]) && row["rice-re-accept"] != "-")
            {
                // 재요청 대사가 없으면 기본 메시지 사용
                if (foodReRequest.Count == 0)
                    foodReRequest.Add("Please...");
            }
            if (row.ContainsKey("rice-re-reject") && !string.IsNullOrEmpty(row["rice-re-reject"]) && row["rice-re-reject"] != "-")
            {
                // 재요청 대사가 없으면 기본 메시지 사용
                if (foodReRequest.Count == 0)
                    foodReRequest.Add("Please...");
            }
            
            // 약 한 번 거절 후 다시 요청
            if (row.ContainsKey("medicine-re-accept") && !string.IsNullOrEmpty(row["medicine-re-accept"]) && row["medicine-re-accept"] != "-")
            {
                if (medicineReRequest.Count == 0)
                    medicineReRequest.Add("Please...");
            }
            if (row.ContainsKey("medicine-re-reject") && !string.IsNullOrEmpty(row["medicine-re-reject"]) && row["medicine-re-reject"] != "-")
            {
                if (medicineReRequest.Count == 0)
                    medicineReRequest.Add("Please...");
            }
            
            // 밥 한 번 거절 후 다시 받았을 때
            if (row.ContainsKey("rice-re-accept") && !string.IsNullOrEmpty(row["rice-re-accept"]) && row["rice-re-accept"] != "-")
                foodReAccept.Add(row["rice-re-accept"]);
            
            // 약 한 번 거절 후 다시 받았을 때
            if (row.ContainsKey("medicine-re-accept") && !string.IsNullOrEmpty(row["medicine-re-accept"]) && row["medicine-re-accept"] != "-")
                medicineReAccept.Add(row["medicine-re-accept"]);
            
            // 밥 두 번 거절당했을 때
            if (row.ContainsKey("rice-re-reject") && !string.IsNullOrEmpty(row["rice-re-reject"]) && row["rice-re-reject"] != "-")
                foodReReject.Add(row["rice-re-reject"]);
            
            // 약 두 번 거절당했을 때
            if (row.ContainsKey("medicine-re-reject") && !string.IsNullOrEmpty(row["medicine-re-reject"]) && row["medicine-re-reject"] != "-")
                medicineReReject.Add(row["medicine-re-reject"]);
            
            // 특별 대사
            if (row.ContainsKey("special") && !string.IsNullOrEmpty(row["special"]) && row["special"] != "-")
                special.Add(row["special"]);
            
            // RequestType 결정 (rice-request가 있으면 Food, medicine-request가 있으면 Medicine)
            if (row.ContainsKey("medicine-request") && !string.IsNullOrEmpty(row["medicine-request"]) && row["medicine-request"] != "-")
            {
                requestType = BohyunNPCRequestType.Medicine;
            }
            else if (row.ContainsKey("rice-request") && !string.IsNullOrEmpty(row["rice-request"]) && row["rice-request"] != "-")
            {
                requestType = BohyunNPCRequestType.Food;
            }
        }

        foodRequestLines = foodRequest.ToArray();
        medicineRequestLines = medicineRequest.ToArray();
        foodAcceptLines = foodAccept.ToArray();
        medicineAcceptLines = medicineAccept.ToArray();
        foodRejectLines = foodReject.ToArray();
        medicineRejectLines = medicineReject.ToArray();
        foodReRequestLines = foodReRequest.ToArray();
        medicineReRequestLines = medicineReRequest.ToArray();
        foodReAcceptLines = foodReAccept.ToArray();
        medicineReAcceptLines = medicineReAccept.ToArray();
        foodReRejectLines = foodReReject.ToArray();
        medicineReRejectLines = medicineReReject.ToArray();
        specialLines = special.ToArray();
        
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }
}

