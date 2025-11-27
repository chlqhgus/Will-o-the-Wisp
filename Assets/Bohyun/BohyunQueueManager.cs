using System.Collections.Generic;
using UnityEngine;

public class BohyunQueueManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    public Transform spawnPoint;
    public Transform[] queueSlots; // slot0 = center (가장 앞)
    
    [Header("Day Schedule (하루별 NPC 순서)")]
    public DaySchedule daySchedule;
    
    [Header("Spawn Settings")]
    public float spawnInterval = 3f;
    
    [Header("Queue Layout (줄 배치 설정)")]
    [Tooltip("뒤로 갈수록 옆으로 이동하는 거리")]
    public float sideOffset = 0.5f; // 옆으로 이동하는 거리
    [Tooltip("뒤로 갈수록 뒤로 이동하는 거리")]
    public float backOffset = 0.3f; // 뒤로 이동하는 거리

    // NPC.cs가 삭제되어 GameObject로 변경
    private List<GameObject> activeNPCs = new List<GameObject>();
    private Dictionary<GameObject, Vector3> npcTargetPositions = new Dictionary<GameObject, Vector3>(); // 각 NPC의 목표 위치 저장
    private float timer = 0f;
    private int currentSpawnIndex = 0; // 현재 스폰할 NPC 인덱스
    private bool isSpawningComplete = false; // 모든 NPC 스폰 완료 여부

    private string[] testLines = {
        "I am hungry… please help.",
        "The night was dangerous…",
        "Do you have medicine?",
        "I heard goblins nearby…"
    };

    [Header("Inventory Reference (선택사항)")]
    [Tooltip("Inventory가 없으면 아이템 소모 없이 NPC에게 줄 수 있습니다")]
    public Inventory inventory;

    void Start()
    {
        if (daySchedule != null && daySchedule.npcPrefabs != null && daySchedule.npcPrefabs.Length > 0)
        {
            // 첫 NPC 스폰
            SpawnNPCFromSchedule();
        }
    }

    void Update()
    {
        // DaySchedule이 있고 아직 스폰할 NPC가 남아있으면 계속 스폰
        if (daySchedule != null && daySchedule.npcPrefabs != null && !isSpawningComplete)
        {
            timer += Time.deltaTime;
            float interval = daySchedule.spawnInterval > 0 ? daySchedule.spawnInterval : spawnInterval;
            
            if (timer >= interval)
            {
                SpawnNPCFromSchedule();
                timer = 0;
            }
        }

        UpdateSortingOrders();
        UpdateQueueTargets();
        UpdateSpeechBubble();
    }

    // -------------------------------------------------------------------
    // Spawn from DaySchedule (하루별 순서대로 스폰)
    // -------------------------------------------------------------------
    void SpawnNPCFromSchedule()
    {
        if (daySchedule == null || daySchedule.npcPrefabs == null)
        {
            Debug.LogWarning("BohyunQueueManager: DaySchedule이 설정되지 않았습니다.");
            return;
        }

        // 모든 NPC를 스폰했으면 더 이상 스폰하지 않음
        if (currentSpawnIndex >= daySchedule.npcPrefabs.Length)
        {
            isSpawningComplete = true;
            return;
        }

        GameObject prefabToSpawn = daySchedule.npcPrefabs[currentSpawnIndex];
        
        if (prefabToSpawn == null)
        {
            Debug.LogWarning($"BohyunQueueManager: 인덱스 {currentSpawnIndex}의 prefab이 null입니다.");
            currentSpawnIndex++;
            return;
        }

        // NPC 이름으로 죽었는지 확인
        string npcName = prefabToSpawn.name;
        
        // NPCComponent 확인 (우선)
        NPCComponent bohyunNPC = prefabToSpawn.GetComponent<NPCComponent>();
        if (bohyunNPC != null && bohyunNPC.bohyunData != null)
        {
            npcName = bohyunNPC.bohyunData.npcName;
        }
        // 기존 NPCData 확인 (OwenSin 호환)
        else
        {
            // NPC.cs가 삭제되어 NPCComponent만 사용
            // NPCData npcData = prefabToSpawn.GetComponent<NPC>()?.data;
            // if (npcData != null && !string.IsNullOrEmpty(npcData.npcName))
            // {
            //     npcName = npcData.npcName;
            // }
        }
        
        // 죽은 NPC는 스폰하지 않음
        if (NPCStateManager.Instance.IsDead(npcName))
        {
            Debug.Log($"{npcName}은(는) 죽어서 스폰되지 않습니다.");
            currentSpawnIndex++;
            if (currentSpawnIndex >= daySchedule.npcPrefabs.Length)
            {
                isSpawningComplete = true;
            }
            return;
        }
        
        GameObject obj = Instantiate(prefabToSpawn, spawnPoint.position, Quaternion.identity);
        // NPC.cs가 삭제되어 GameObject만 사용
        
        activeNPCs.Add(obj);
        // NPC의 초기 목표 위치는 spawnPoint 위치로 설정
        npcTargetPositions[obj] = spawnPoint.position;

        currentSpawnIndex++;
        
        // 마지막 NPC를 스폰했으면 완료 표시
        if (currentSpawnIndex >= daySchedule.npcPrefabs.Length)
        {
            isSpawningComplete = true;
        }
    }

    // -------------------------------------------------------------------
    // Assign movement target for each NPC
    // -------------------------------------------------------------------
    void UpdateQueueTargets()
    {
        // null인 NPC 제거
        activeNPCs.RemoveAll(npc => npc == null);
        
        // queueSlots가 없거나 비어있으면 처리하지 않음
        if (queueSlots == null || queueSlots.Length == 0)
        {
            Debug.LogWarning("BohyunQueueManager: queueSlots가 설정되지 않았습니다.");
            return;
        }
        
        for (int i = 0; i < activeNPCs.Count; i++)
        {
            GameObject npc = activeNPCs[i];
            if (npc == null) continue;

            // NPC가 이미 나가는 중이면 타겟 업데이트하지 않음
            // 목표 위치가 왼쪽 화면 밖(-14f 근처)이면 업데이트하지 않음
            if (npcTargetPositions.ContainsKey(npc))
            {
                Vector3 currentTarget = npcTargetPositions[npc];
                if (currentTarget.x < -10f) // 이미 나가는 중
                {
                    continue;
                }
            }

            Vector3 targetPos;
            
            if (i < queueSlots.Length && queueSlots[i] != null)
            {
                // 큐 슬롯이 있으면 그 위치 사용
                targetPos = queueSlots[i].position;
                
                // 뒤로 갈수록 약간 뒤-옆으로 이동
                if (i > 0)
                {
                    targetPos += new Vector3(sideOffset * i, 0, -backOffset * i);
                }
            }
            else
            {
                // 큐 슬롯을 넘어서면 마지막 슬롯 기준으로 뒤-옆으로 배치
                int queueIndex = queueSlots.Length - 1;
                
                // 안전 체크: queueIndex가 유효한지 확인
                if (queueIndex < 0 || queueIndex >= queueSlots.Length || queueSlots[queueIndex] == null)
                {
                    // queueSlots가 비어있으면 spawnPoint 위치 사용
                    if (spawnPoint != null)
                    {
                        targetPos = spawnPoint.position + new Vector3(sideOffset * i, 0, -backOffset * i);
                    }
                    else
                    {
                        Debug.LogWarning($"BohyunQueueManager: NPC {i}의 타겟 위치를 설정할 수 없습니다.");
                        continue;
                    }
                }
                else
                {
                    int offsetIndex = i - queueIndex;
                    Vector3 basePos = queueSlots[queueIndex].position;
                    targetPos = basePos + new Vector3(
                        sideOffset * (queueIndex + offsetIndex), 
                        0, 
                        -backOffset * (queueIndex + offsetIndex)
                    );
                }
            }

            // 목표 위치가 변경되었을 때만 업데이트
            if (!npcTargetPositions.ContainsKey(npc) || 
                Vector3.Distance(npcTargetPositions[npc], targetPos) > 0.01f)
            {
                npcTargetPositions[npc] = targetPos;
                // NPC.cs가 삭제되어 SetTarget 메서드 사용 불가
                // npc.SetTarget(targetPos);
            }
        }
    }

    // -------------------------------------------------------------------
    // Sorting order
    // -------------------------------------------------------------------
    void UpdateSortingOrders()
    {
        int baseOrder = 100;

        for (int i = 0; i < activeNPCs.Count; i++)
        {
            if (activeNPCs[i] == null) continue;
            
            int order = baseOrder - i;
            // NPC.cs가 삭제되어 SetSortingOrder 메서드 사용 불가
            // SpriteRenderer를 직접 사용
            SpriteRenderer[] spriteRenderers = activeNPCs[i].GetComponentsInChildren<SpriteRenderer>();
            foreach (SpriteRenderer sr in spriteRenderers)
            {
                if (sr != null) sr.sortingOrder = order;
            }
        }
    }

    // -------------------------------------------------------------------
    // Speech Bubble Logic
    // -------------------------------------------------------------------
    void UpdateSpeechBubble()
    {
        // NPC.cs가 삭제되어 말풍선 기능 사용 불가
        // 말풍선 기능은 NPCQueueSystem에서 처리
        // for (int i = 0; i < activeNPCs.Count; i++)
        // {
        //     GameObject npc = activeNPCs[i];
        //     if (npc == null) continue;
        //     ...
        // }
    }

    /// <summary>
    /// NPC의 요청 타입에 맞는 대사를 반환합니다.
    /// </summary>
    string GetNPCDialogue(GameObject npc)
    {
        if (npc == null) return "";
        // NPCComponent 확인 (우선)
        NPCComponent bohyunNPC = npc.GetComponent<NPCComponent>();
        if (bohyunNPC != null && bohyunNPC.bohyunData != null)
        {
            BohyunNPCData data = bohyunNPC.bohyunData;
            string[] linesToUse = null;

            // 요청 타입에 따라 대사 선택
            if (data.requestType == BohyunNPCRequestType.Medicine)
            {
                // 약 요청 대사가 있으면 사용
                if (data.medicineRequestLines != null && data.medicineRequestLines.Length > 0)
                {
                    linesToUse = data.medicineRequestLines;
                }
                // NPC 상태 매니저에 약 요청 기록
                NPCStateManager.Instance.RecordMedicineRequest(data.npcName);
            }
            else if (data.requestType == BohyunNPCRequestType.Food)
            {
                // 밥 요청 대사가 있으면 사용
                if (data.foodRequestLines != null && data.foodRequestLines.Length > 0)
                {
                    linesToUse = data.foodRequestLines;
                }
            }

            // 요청 타입별 대사가 없으면 기본 대사 사용
            if (linesToUse == null || linesToUse.Length == 0)
            {
                // 기본 대사가 없으면 테스트 라인 사용
                return testLines[Random.Range(0, testLines.Length)];
            }

            // 대사가 있으면 랜덤 선택
            if (linesToUse != null && linesToUse.Length > 0)
            {
                return linesToUse[Random.Range(0, linesToUse.Length)];
            }
        }
        
        // 기존 NPCData 사용 (OwenSin 호환) - NPC.cs가 삭제되어 사용 불가
        // if (npc.data != null)
        // {
        //     NPCData data = npc.data;
        //     if (data.speechLines != null && data.speechLines.Length > 0)
        //     {
        //         return data.speechLines[Random.Range(0, data.speechLines.Length)];
        //     }
        // }

        // 기본 대사 사용
        return testLines[Random.Range(0, testLines.Length)];
    }

    // -------------------------------------------------------------------
    // 선택지: 거절 / 음식 주기 / 약 주기
    // -------------------------------------------------------------------
    
    public void RefuseFrontNPC()
    {
        if (activeNPCs.Count == 0) return;

        GameObject front = activeNPCs[0];
        string npcName = GetNPCName(front);
        bool requestedMedicine = DidNPCRequestMedicine(front);
        
        // 상태 기록
        NPCStateManager.Instance.RecordRefusal(npcName, requestedMedicine);
        
        activeNPCs.RemoveAt(0);
        
        // Dictionary에서도 제거
        if (npcTargetPositions.ContainsKey(front))
        {
            npcTargetPositions.Remove(front);
        }

        if (front != null)
        {
            // NPC.cs가 삭제되어 LeaveScene 메서드 사용 불가
            // 왼쪽 화면 밖으로 이동하도록 타겟 설정
            npcTargetPositions[front] = new Vector3(-14f, front.transform.position.y, front.transform.position.z);
        }
        
        // 다음 NPC들이 앞으로 이동하도록 타겟 업데이트
        UpdateQueueTargets();
    }

    public void GiveLotusRice()
    {
        if (activeNPCs.Count == 0) return;
        
        // Inventory가 있으면 아이템 확인 및 소모
        if (inventory != null)
        {
            if (inventory.lotusRice <= 0) return;
            inventory.UseLotusRice();
        }

        GameObject front = activeNPCs[0];
        string npcName = GetNPCName(front);
        
        // 밥을 주었으므로 상태 기록 (약 요청이었으면 거절로 기록)
        if (DidNPCRequestMedicine(front))
        {
            // 약을 요청했는데 밥을 줌 = 거절로 간주
            NPCStateManager.Instance.RecordRefusal(npcName, true);
        }
        
        activeNPCs.RemoveAt(0);
        
        // Dictionary에서도 제거
        if (npcTargetPositions.ContainsKey(front))
        {
            npcTargetPositions.Remove(front);
        }

        if (front != null)
        {
            // NPC.cs가 삭제되어 AcceptAndLeave 메서드 사용 불가
            // 왼쪽 화면 밖으로 이동하도록 타겟 설정
            npcTargetPositions[front] = new Vector3(-14f, front.transform.position.y, front.transform.position.z);
        }
        
        // 다음 NPC들이 앞으로 이동하도록 타겟 업데이트
        UpdateQueueTargets();
    }

    public void GiveHerbalMedicine()
    {
        if (activeNPCs.Count == 0) return;
        
        // Inventory가 있으면 아이템 확인 및 소모
        if (inventory != null)
        {
            if (inventory.herbalMedicine <= 0) return;
            inventory.UseHerbalMedicine();
        }

        GameObject front = activeNPCs[0];
        string npcName = GetNPCName(front);
        
        // 약을 주었으므로 상태 기록
        NPCStateManager.Instance.RecordMedicineGiven(npcName);
        
        activeNPCs.RemoveAt(0);
        
        // Dictionary에서도 제거
        if (npcTargetPositions.ContainsKey(front))
        {
            npcTargetPositions.Remove(front);
        }

        if (front != null)
        {
            // NPC.cs가 삭제되어 AcceptAndLeave 메서드 사용 불가
            // 왼쪽 화면 밖으로 이동하도록 타겟 설정
            npcTargetPositions[front] = new Vector3(-14f, front.transform.position.y, front.transform.position.z);
        }
        
        // 다음 NPC들이 앞으로 이동하도록 타겟 업데이트
        UpdateQueueTargets();
    }

    /// <summary>
    /// NPC의 이름을 가져옵니다 (BohyunNPCData 또는 GameObject 이름).
    /// </summary>
    string GetNPCName(GameObject npc)
    {
        if (npc == null) return "Unknown";
        
        // NPCComponent 확인 (우선)
        NPCComponent bohyunNPC = npc.GetComponent<NPCComponent>();
        if (bohyunNPC != null)
        {
            return bohyunNPC.GetNPCName();
        }
        
        // 기존 NPCData 확인 (OwenSin 호환) - NPC.cs가 삭제되어 사용 불가
        // if (npc.data != null && !string.IsNullOrEmpty(npc.data.npcName))
        //     return npc.data.npcName;
        
        return npc.name.Replace("(Clone)", "").Trim();
    }
    
    /// <summary>
    /// NPC가 약을 요청했는지 확인합니다.
    /// </summary>
    bool DidNPCRequestMedicine(GameObject npc)
    {
        if (npc == null) return false;
        // NPCComponent 확인 (우선)
        NPCComponent bohyunNPC = npc.GetComponent<NPCComponent>();
        if (bohyunNPC != null && bohyunNPC.bohyunData != null)
        {
            return bohyunNPC.bohyunData.requestType == BohyunNPCRequestType.Medicine;
        }
        
        // 기존 시스템에서는 항상 false (OwenSin의 NPCData에는 requestType이 없음)
        return false;
    }

    // -------------------------------------------------------------------
    // Day Management (하루 관리)
    // -------------------------------------------------------------------
    
    /// <summary>
    /// 새로운 DaySchedule을 설정하고 하루를 시작합니다.
    /// </summary>
    public void StartDay(DaySchedule newSchedule)
    {
        // 기존 NPC들 모두 제거
        ClearAllNPCs();
        
        // 새로운 스케줄 설정
        daySchedule = newSchedule;
        currentSpawnIndex = 0;
        isSpawningComplete = false;
        timer = 0f;
        
        // 첫 NPC 스폰
        if (daySchedule != null && daySchedule.npcPrefabs != null && daySchedule.npcPrefabs.Length > 0)
        {
            SpawnNPCFromSchedule();
        }
    }
    
    /// <summary>
    /// 현재 하루를 리셋합니다 (같은 스케줄로 다시 시작).
    /// </summary>
    public void ResetDay()
    {
        if (daySchedule != null)
        {
            StartDay(daySchedule);
        }
    }
    
    /// <summary>
    /// 모든 활성 NPC를 제거합니다.
    /// </summary>
    private void ClearAllNPCs()
    {
        foreach (GameObject npc in activeNPCs)
        {
            if (npc != null)
            {
                // NPC.cs가 삭제되어 LeaveScene 메서드 사용 불가
                // Destroy(npc);
            }
        }
        activeNPCs.Clear();
        npcTargetPositions.Clear();
    }
    
    /// <summary>
    /// 모든 NPC가 스폰되었는지 확인합니다.
    /// </summary>
    public bool IsSpawningComplete()
    {
        return isSpawningComplete;
    }
    
    /// <summary>
    /// 현재 활성 NPC 수를 반환합니다.
    /// </summary>
    public int GetActiveNPCCount()
    {
        return activeNPCs.Count;
    }
}

