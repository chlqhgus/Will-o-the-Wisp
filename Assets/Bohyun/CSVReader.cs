using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// CSV 파일을 읽어서 NPC 대사 데이터를 파싱하는 유틸리티
/// </summary>
public static class CSVReader
{
    /// <summary>
    /// CSV 파일을 읽어서 Dictionary로 반환합니다.
    /// 첫 번째 행은 헤더로 사용됩니다.
    /// </summary>
    /// <param name="filePath">Resources 폴더 기준 경로 또는 Assets 폴더 기준 전체 경로</param>
    public static List<Dictionary<string, string>> ReadCSV(string filePath)
    {
        List<Dictionary<string, string>> result = new List<Dictionary<string, string>>();
        string csvText = "";

        #if UNITY_EDITOR
        // 에디터에서는 Assets 폴더 기준 경로로도 읽기 가능
        if (filePath.StartsWith("Assets/"))
        {
            // 확장자가 없으면 .csv 추가
            string assetPath = filePath;
            if (!assetPath.EndsWith(".csv"))
            {
                assetPath += ".csv";
            }
            
            // Assets 폴더를 실제 파일 시스템 경로로 변환
            // Application.dataPath는 "프로젝트폴더/Assets" 경로
            string relativePath = assetPath.Replace("Assets/", "");
            string fullPath = Path.Combine(Application.dataPath, relativePath);
            
            // 경로 정규화 (백슬래시를 슬래시로 통일)
            fullPath = fullPath.Replace('\\', '/');
            
            if (File.Exists(fullPath))
            {
                // UTF-8 인코딩으로 파일 읽기 (BOM 처리 포함)
                csvText = File.ReadAllText(fullPath, Encoding.UTF8);
            }
            else
            {
                Debug.LogError($"CSVReader: 파일을 찾을 수 없습니다.");
                Debug.LogError($"  입력 경로: {filePath}");
                Debug.LogError($"  변환된 경로: {fullPath}");
                Debug.LogError($"  Application.dataPath: {Application.dataPath}");
                return result;
            }
        }
        else
        #endif
        {
            // Resources 폴더에서 읽기 (기존 방식)
            TextAsset csvFile = Resources.Load<TextAsset>(filePath);
            if (csvFile == null)
            {
                Debug.LogError($"CSVReader: 파일을 찾을 수 없습니다: {filePath}");
                return result;
            }
            csvText = csvFile.text;
        }

        string[] lines = csvText.Split('\n');
        if (lines.Length < 2)
        {
            Debug.LogWarning("CSVReader: CSV 파일에 데이터가 없습니다.");
            return result;
        }

        // 헤더 파싱
        string[] headers = ParseCSVLine(lines[0]);
        Debug.Log($"CSVReader: 헤더 파싱 완료. 컬럼 수: {headers.Length}, 컬럼명: {string.Join(", ", headers)}");

        // 데이터 파싱
        int parsedRowCount = 0;
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;
            
            // 줄 끝의 \r 제거 (Windows 줄바꿈 처리)
            line = line.TrimEnd('\r');

            string[] values = ParseCSVLine(line);
            
            // 컬럼 수가 다르면 경고하지만 계속 진행 (일부 컬럼이 비어있을 수 있음)
            if (values.Length != headers.Length)
            {
                Debug.LogWarning($"CSVReader: 행 {i + 1}의 컬럼 수가 헤더와 일치하지 않습니다. (헤더: {headers.Length}, 값: {values.Length})");
                // 컬럼 수가 부족하면 빈 문자열로 채움
                if (values.Length < headers.Length)
                {
                    string[] paddedValues = new string[headers.Length];
                    for (int k = 0; k < values.Length; k++)
                    {
                        paddedValues[k] = values[k];
                    }
                    for (int k = values.Length; k < headers.Length; k++)
                    {
                        paddedValues[k] = "";
                    }
                    values = paddedValues;
                }
                else
                {
                    // 컬럼 수가 많으면 잘라냄
                    string[] trimmedValues = new string[headers.Length];
                    for (int k = 0; k < headers.Length; k++)
                    {
                        trimmedValues[k] = values[k];
                    }
                    values = trimmedValues;
                }
            }

            Dictionary<string, string> row = new Dictionary<string, string>();
            for (int j = 0; j < headers.Length; j++)
            {
                string header = headers[j].Trim();
                string value = j < values.Length ? values[j].Trim() : "";
                row[header] = value;
            }
            result.Add(row);
            parsedRowCount++;
            
            // 첫 번째 컬럼(npc-name) 값 로그
            if (row.ContainsKey("npc-name"))
            {
                Debug.Log($"CSVReader: 행 {i + 1} 파싱 완료 - NPC 이름: '{row["npc-name"]}'");
            }
        }

        Debug.Log($"CSVReader: 총 {parsedRowCount}개의 데이터 행을 파싱했습니다.");
        return result;
    }

    /// <summary>
    /// CSV 라인을 파싱합니다 (쉼표로 구분, 큰따옴표 처리).
    /// CSV 표준에 따라 큰따옴표(")만 따옴표로 처리합니다.
    /// </summary>
    private static string[] ParseCSVLine(string line)
    {
        List<string> result = new List<string>();
        bool inDoubleQuotes = false;
        string currentField = "";

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inDoubleQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    // 이스케이프된 큰따옴표 ("")
                    currentField += '"';
                    i++;
                }
                else
                {
                    // 큰따옴표 시작/끝
                    inDoubleQuotes = !inDoubleQuotes;
                }
            }
            else if (c == ',' && !inDoubleQuotes)
            {
                // 필드 구분자 (큰따옴표 밖일 때만)
                result.Add(currentField);
                currentField = "";
            }
            else
            {
                // 일반 문자 (작은따옴표 포함)
                currentField += c;
            }
        }

        // 마지막 필드 추가
        result.Add(currentField);

        return result.ToArray();
    }
}

