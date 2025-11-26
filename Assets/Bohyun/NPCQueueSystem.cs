using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// NPC 줄세우기 및 대사 시스템
/// </summary>
public class NPCQueueSystem : MonoBehaviour
{
    [Header("Spawn Settings")]
    public Transform spawnPoint; // NPC가 처음 생성되는 위치
    public Transform[] queueSlots; // 줄서는 위치들 (첫 번째가 가장 앞)
    
    [Header("Day Schedule")]
    public DaySchedule daySchedule; // 하루별 NPC 순서
    
    [Header("Speech Bubble UI")]
    [Tooltip("말풍선 배경 이미지 (씬에 있는 UI)")]
    public GameObject speechBubbleBG;
    [Tooltip("말풍선 텍스트 (씬에 있는 UI)")]
    public TextMeshProUGUI speechBubbleText;
    
    [Header("Inventory (선택사항)")]
    public Inventory inventory;
    
    private List<GameObject> activeNPCs = new List<GameObject>(); // 활성 NPC들 (순서대로)
    private Dictionary<GameObject, Vector3> npcTargetPositions = new Dictionary<GameObject, Vector3>();
    private Dictionary<GameObject, bool> npcArrivedAtPosition = new Dictionary<GameObject, bool>(); // NPC가 목표 위치에 도착했는지
    private float timer = 0f;
    private int currentSpawnIndex = 0;
    private bool isInitialSpawnComplete = false; // 초기 스폰 완료 여부
    
    // 현재 가장 앞에 있는 NPC (queueSlots[0]에 있는 NPC = center)
    private GameObject frontNPC = null;
    private NPCComponent frontNPCComponent = null;
    
    // 상호작용 처리 중인지 여부 (대사 표시 중에는 다음 상호작용 방지)
    private bool isProcessingInteraction = false;
    
    [Header("Position Settings")]
    [Tooltip("NPC가 목표 위치에 도착했다고 판단하는 거리")]
    public float arrivalDistance = 0.1f;

    void Start()
    {
        // 말풍선 초기화
        if (speechBubbleBG != null)
            speechBubbleBG.SetActive(false);
        if (speechBubbleText != null)
            speechBubbleText.gameObject.SetActive(false);
        
        // 초기화
        isInitialSpawnComplete = false;
        currentSpawnIndex = 0;
        timer = 0f;
        
        // daySchedule이 없으면 스폰 완료로 표시
        if (daySchedule == null || daySchedule.npcPrefabs == null || daySchedule.npcPrefabs.Length == 0)
        {
            isInitialSpawnComplete = true;
        }
    }

    void Update()
    {
        // 초기 스폰만 수행 (한 번만 실행)
        if (!isInitialSpawnComplete)
        {
            // daySchedule에 등록된 NPC 수만큼만 스폰
            if (daySchedule != null && daySchedule.npcPrefabs != null && daySchedule.npcPrefabs.Length > 0)
            {
                int maxSpawnCount = daySchedule.npcPrefabs.Length; // daySchedule에 등록된 NPC 수
                int maxQueueSize = queueSlots != null ? queueSlots.Length : 0;
                
                // daySchedule의 NPC 수와 queueSlots 수 중 작은 값만큼만 스폰
                int targetCount = Mathf.Min(maxSpawnCount, maxQueueSize);
                
                if (activeNPCs.Count < targetCount && currentSpawnIndex < maxSpawnCount)
                {
                    timer += Time.deltaTime;
                    float interval = daySchedule.spawnInterval > 0 ? daySchedule.spawnInterval : 1f;
                    
                    if (timer >= interval)
                    {
                        SpawnNextNPC();
                        timer = 0;
                    }
                }
                else if (currentSpawnIndex >= maxSpawnCount)
                {
                    // 모든 NPC 스폰 완료
                    isInitialSpawnComplete = true;
                }
            }
            else
            {
                isInitialSpawnComplete = true;
            }
        }

        // NPC 위치 업데이트
        UpdateNPCPositions();
        
        // Sorting Order 업데이트
        UpdateSortingOrders();
        
        // 말풍선 업데이트
        UpdateSpeechBubble();
    }

    /// <summary>
    /// 다음 NPC를 스폰합니다. (daySchedule에 등록된 순서대로만 스폰, 순환 없음)
    /// </summary>
    void SpawnNextNPC()
    {
        if (daySchedule == null || daySchedule.npcPrefabs == null || daySchedule.npcPrefabs.Length == 0)
            return;

        // daySchedule에 등록된 NPC 수를 초과하면 스폰하지 않음
        if (currentSpawnIndex >= daySchedule.npcPrefabs.Length)
            return;

        // 큐가 가득 찼으면 스폰하지 않음
        int maxQueueSize = queueSlots != null ? queueSlots.Length : 0;
        if (activeNPCs.Count >= maxQueueSize)
            return;

        GameObject prefabToSpawn = daySchedule.npcPrefabs[currentSpawnIndex];
        if (prefabToSpawn == null)
        {
            currentSpawnIndex++;
            return;
        }

        // 죽은 NPC는 스폰하지 않고 다음으로 넘어감
        NPCComponent prefabComponent = prefabToSpawn.GetComponent<NPCComponent>();
        if (prefabComponent != null && prefabComponent.bohyunData != null)
        {
            string npcName = prefabComponent.bohyunData.npcName;
            if (NPCStateManager.Instance != null && NPCStateManager.Instance.IsDead(npcName))
            {
                Debug.Log($"{npcName}은(는) 죽어서 스폰되지 않습니다.");
                currentSpawnIndex++;
                // 다음 NPC 시도 (죽은 NPC는 건너뛰고 다음으로)
                if (currentSpawnIndex < daySchedule.npcPrefabs.Length)
                {
                    SpawnNextNPC(); // 재귀 호출로 다음 NPC 시도
                }
                return;
            }
        }

        // NPC 스폰 (spawnPoint에서 생성)
        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        
        GameObject npc = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
        
        // NPC 컴포넌트가 없으면 자동으로 추가
        NPC npcComponent = npc.GetComponent<NPC>();
        if (npcComponent == null)
        {
            npcComponent = npc.AddComponent<NPC>();
            Debug.Log($"BohyunNPCQueueSystem: {prefabToSpawn.name}에 NPC 컴포넌트를 자동으로 추가했습니다.");
        }
        
        activeNPCs.Add(npc);
        
        // 스폰 직후 즉시 목표 위치 설정 (첫 번째 위치로 이동)
        if (queueSlots != null && queueSlots.Length > 0 && activeNPCs.Count <= queueSlots.Length)
        {
            int targetIndex = activeNPCs.Count - 1; // 현재 추가된 NPC의 인덱스
            if (targetIndex < queueSlots.Length && queueSlots[targetIndex] != null)
            {
                Vector3 targetPos = queueSlots[targetIndex].position;
                npcTargetPositions[npc] = targetPos;
                npcArrivedAtPosition[npc] = false; // 아직 도착하지 않음
                
                // 즉시 목표 위치로 설정
                if (npcComponent != null)
                {
                    npcComponent.SetTarget(targetPos);
                }
                else
                {
                    // NPC 컴포넌트가 없으면 직접 위치 설정
                    npc.transform.position = targetPos;
                    // 직접 위치 설정한 경우 즉시 도착으로 표시
                    npcArrivedAtPosition[npc] = true;
                }
            }
        }
        else
        {
            npcTargetPositions[npc] = spawnPos;
            npcArrivedAtPosition[npc] = false;
        }

        currentSpawnIndex++;
    }

    /// <summary>
    /// NPC들의 위치를 업데이트합니다. activeNPCs[i]는 queueSlots[i]로 이동합니다.
    /// center = queueSlots[0] = activeNPCs[0]
    /// </summary>
    void UpdateNPCPositions()
    {
        // 상호작용 처리 중이면 위치 업데이트하지 않음 (페이드아웃 중인 NPC 방해 방지)
        if (isProcessingInteraction) return;
        
        // null인 NPC 제거
        activeNPCs.RemoveAll(npc => npc == null);
        
        if (queueSlots == null || queueSlots.Length == 0)
            return;

        // activeNPCs의 각 NPC를 해당하는 queueSlots 위치로 이동
        for (int i = 0; i < activeNPCs.Count && i < queueSlots.Length; i++)
        {
            GameObject npc = activeNPCs[i];
            if (npc == null) continue;
            if (queueSlots[i] == null) continue;

            Vector3 targetPos = queueSlots[i].position;

            // 목표 위치가 변경되었을 때만 업데이트
            if (!npcTargetPositions.ContainsKey(npc) || 
                Vector3.Distance(npcTargetPositions[npc], targetPos) > 0.01f)
            {
                npcTargetPositions[npc] = targetPos;
                // 목표 위치가 변경되면 도착 상태 리셋
                npcArrivedAtPosition[npc] = false;
                
                // NPC 컴포넌트가 있으면 SetTarget 사용
                NPC npcComponent = npc.GetComponent<NPC>();
                if (npcComponent != null)
                {
                    npcComponent.SetTarget(targetPos);
                }
                else
                {
                    // NPC 컴포넌트가 없으면 직접 이동 (더 빠른 속도)
                    float moveSpeed = 5f; // 이동 속도 증가
                    npc.transform.position = Vector3.MoveTowards(
                        npc.transform.position, 
                        targetPos, 
                        moveSpeed * Time.deltaTime
                    );
                }
            }
            
            // NPC가 목표 위치에 도착했는지 확인
            if (npcTargetPositions.ContainsKey(npc))
            {
                float distanceToTarget = Vector3.Distance(npc.transform.position, npcTargetPositions[npc]);
                if (distanceToTarget <= arrivalDistance)
                {
                    npcArrivedAtPosition[npc] = true;
                }
            }
        }
    }

    /// <summary>
    /// NPC들의 Sorting Order를 업데이트합니다. 앞에 있는 NPC가 뒤에 있는 NPC보다 위에 렌더링됩니다.
    /// </summary>
    void UpdateSortingOrders()
    {
        int baseOrder = 100;

        for (int i = 0; i < activeNPCs.Count; i++)
        {
            if (activeNPCs[i] == null) continue;
            
            NPC npcComponent = activeNPCs[i].GetComponent<NPC>();
            if (npcComponent != null)
            {
                int order = baseOrder - i; // 앞에 있는 NPC가 더 높은 order
                npcComponent.SetSortingOrder(order);
            }
        }
    }

    /// <summary>
    /// 말풍선을 업데이트합니다. center (queueSlots[0] = activeNPCs[0])에 있는 NPC의 대사를 표시합니다.
    /// NPC가 목표 위치에 도착했을 때만 대사를 표시합니다.
    /// </summary>
    void UpdateSpeechBubble()
    {
        // 상호작용 처리 중이면 업데이트하지 않음
        if (isProcessingInteraction) return;
        
        // center에 있는 NPC 찾기 (queueSlots[0] = activeNPCs[0])
        GameObject newFrontNPC = null;
        if (activeNPCs.Count > 0)
        {
            // activeNPCs[0]이 center에 있는 NPC
            newFrontNPC = activeNPCs[0];
        }
        
        // NPC가 목표 위치에 도착했는지 확인
        bool hasArrived = false;
        if (newFrontNPC != null && npcArrivedAtPosition.ContainsKey(newFrontNPC))
        {
            hasArrived = npcArrivedAtPosition[newFrontNPC];
        }
        
        // NPC가 변경되었거나, 같은 NPC가 도착했을 때만 대사 표시
        if (newFrontNPC != frontNPC)
        {
            // center NPC가 변경되었으면
            frontNPC = newFrontNPC;
            frontNPCComponent = frontNPC != null ? frontNPC.GetComponent<NPCComponent>() : null;
            
            // 도착했을 때만 대사 표시
            if (hasArrived && frontNPC != null && frontNPCComponent != null && frontNPCComponent.bohyunData != null)
            {
                ShowDialogue(frontNPCComponent.bohyunData);
            }
            else
            {
                HideSpeechBubble();
            }
        }
        else if (frontNPC != null && frontNPCComponent != null && frontNPCComponent.bohyunData != null)
        {
            // 같은 NPC가 center에 있지만 아직 도착하지 않았으면 대사 숨김
            if (!hasArrived)
            {
                HideSpeechBubble();
            }
            // 도착했고 대사가 표시되지 않았으면 표시
            else if (hasArrived && speechBubbleBG != null && !speechBubbleBG.activeSelf)
            {
                ShowDialogue(frontNPCComponent.bohyunData);
            }
        }
    }

    /// <summary>
    /// 대사를 표시합니다.
    /// </summary>
    void ShowDialogue(BohyunNPCData npcData)
    {
        if (npcData == null) return;

        string dialogue = "";
        int refusalCount = 0;
        
        // 거절 횟수 확인
        if (NPCStateManager.Instance != null)
        {
            refusalCount = NPCStateManager.Instance.GetRefusalCount(npcData.npcName);
        }
        
        // 요청 타입에 따라 대사 선택
        if (npcData.requestType == BohyunNPCRequestType.Medicine)
        {
            // 한 번 거절 후 재요청 대사가 있으면 재요청 대사 표시
            if (refusalCount > 0 && npcData.medicineReRequestLines != null && npcData.medicineReRequestLines.Length > 0)
            {
                dialogue = npcData.medicineReRequestLines[Random.Range(0, npcData.medicineReRequestLines.Length)];
            }
            else if (npcData.medicineRequestLines != null && npcData.medicineRequestLines.Length > 0)
            {
                dialogue = npcData.medicineRequestLines[Random.Range(0, npcData.medicineRequestLines.Length)];
            }
            // NPC 상태 매니저에 약 요청 기록
            if (NPCStateManager.Instance != null)
            {
                NPCStateManager.Instance.RecordMedicineRequest(npcData.npcName);
            }
        }
        else if (npcData.requestType == BohyunNPCRequestType.Food)
        {
            // 한 번 거절 후 재요청 대사가 있으면 재요청 대사 표시
            if (refusalCount > 0 && npcData.foodReRequestLines != null && npcData.foodReRequestLines.Length > 0)
            {
                dialogue = npcData.foodReRequestLines[Random.Range(0, npcData.foodReRequestLines.Length)];
            }
            else if (npcData.foodRequestLines != null && npcData.foodRequestLines.Length > 0)
            {
                dialogue = npcData.foodRequestLines[Random.Range(0, npcData.foodRequestLines.Length)];
            }
        }

        // 대사가 없으면 기본 메시지
        if (string.IsNullOrEmpty(dialogue))
        {
            dialogue = "...";
        }

        // 말풍선 표시
        if (speechBubbleBG != null)
            speechBubbleBG.SetActive(true);
        if (speechBubbleText != null)
        {
            speechBubbleText.gameObject.SetActive(true);
            speechBubbleText.text = dialogue;
        }
    }

    /// <summary>
    /// 말풍선을 숨깁니다.
    /// </summary>
    void HideSpeechBubble()
    {
        if (speechBubbleBG != null)
            speechBubbleBG.SetActive(false);
        if (speechBubbleText != null)
            speechBubbleText.gameObject.SetActive(false);
    }

    [Header("Fade Settings")]
    [Tooltip("NPC가 사라질 때 페이드아웃 시간")]
    public float fadeOutDuration = 0.5f;

    /// <summary>
    /// NPC를 왼쪽 끝으로 이동시키고 페이드아웃합니다.
    /// </summary>
    void MoveNPCToLeft(GameObject npc)
    {
        if (npc == null) return;

        Vector3 leftExitPos = new Vector3(-14f, npc.transform.position.y, npc.transform.position.z);
        npcTargetPositions[npc] = leftExitPos;
        
        // 페이드아웃과 함께 이동
        StartCoroutine(FadeOutAndMove(npc, leftExitPos));
    }

    /// <summary>
    /// NPC를 페이드아웃하며 왼쪽으로 이동시킵니다.
    /// </summary>
    System.Collections.IEnumerator FadeOutAndMove(GameObject npc, Vector3 targetPos)
    {
        if (npc == null) yield break;

        // SpriteRenderer들 찾기
        SpriteRenderer[] spriteRenderers = npc.GetComponentsInChildren<SpriteRenderer>();
        float elapsedTime = 0f;
        float moveSpeed = 12f;

        Vector3 startPos = npc.transform.position;
        Color[] startColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                startColors[i] = spriteRenderers[i].color;
            }
        }

        while (npc != null && elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeOutDuration;

            // 위치 이동
            npc.transform.position = Vector3.MoveTowards(
                npc.transform.position, 
                targetPos, 
                moveSpeed * Time.deltaTime
            );

            // 알파 페이드아웃
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    Color color = startColors[i];
                    color.a = Mathf.Lerp(1f, 0f, t);
                    spriteRenderers[i].color = color;
                }
            }

            yield return null;
        }

        // 완전히 사라지면 제거
        if (npc != null)
        {
            Destroy(npc);
        }
    }

    // -------------------------------------------------------------------
    // 선택지: 거절 / 음식 주기 / 약 주기
    // -------------------------------------------------------------------
    
    public void RefuseFrontNPC()
    {
        if (activeNPCs.Count == 0 || frontNPC == null || isProcessingInteraction) return;
        
        // 상호작용 시작
        isProcessingInteraction = true;

        BohyunNPCData npcData = frontNPCComponent != null ? frontNPCComponent.bohyunData : null;
        string npcName = GetNPCName(frontNPC);
        bool requestedMedicine = false;
        
        // 거절 횟수 확인
        int refusalCount = 0;
        if (NPCStateManager.Instance != null)
        {
            refusalCount = NPCStateManager.Instance.GetRefusalCount(npcName);
        }
        
        if (npcData != null)
        {
            requestedMedicine = npcData.requestType == BohyunNPCRequestType.Medicine;
            
            // 거절 대사 표시 (두 번 거절이면 재거절 대사, 아니면 일반 거절 대사)
            string rejectDialogue = "";
            bool hasReRequest = false; // 재요청 가능 여부 (re-accept나 re-reject 대사가 있으면 재요청 가능)
            
            if (requestedMedicine)
            {
                // 두 번 거절당했을 때
                if (refusalCount >= 1 && npcData.medicineReRejectLines != null && npcData.medicineReRejectLines.Length > 0)
                {
                    rejectDialogue = npcData.medicineReRejectLines[Random.Range(0, npcData.medicineReRejectLines.Length)];
                }
                // 첫 거절
                else if (npcData.medicineRejectLines != null && npcData.medicineRejectLines.Length > 0)
                {
                    rejectDialogue = npcData.medicineRejectLines[Random.Range(0, npcData.medicineRejectLines.Length)];
                }
                // 재요청 가능 여부 확인 (re-accept나 re-reject 대사가 있으면 재요청 가능)
                hasReRequest = (npcData.medicineReAcceptLines != null && npcData.medicineReAcceptLines.Length > 0) ||
                               (npcData.medicineReRejectLines != null && npcData.medicineReRejectLines.Length > 0);
            }
            else
            {
                // 두 번 거절당했을 때
                if (refusalCount >= 1 && npcData.foodReRejectLines != null && npcData.foodReRejectLines.Length > 0)
                {
                    rejectDialogue = npcData.foodReRejectLines[Random.Range(0, npcData.foodReRejectLines.Length)];
                }
                // 첫 거절
                else if (npcData.foodRejectLines != null && npcData.foodRejectLines.Length > 0)
                {
                    rejectDialogue = npcData.foodRejectLines[Random.Range(0, npcData.foodRejectLines.Length)];
                }
                // 재요청 가능 여부 확인 (re-accept나 re-reject 대사가 있으면 재요청 가능)
                hasReRequest = (npcData.foodReAcceptLines != null && npcData.foodReAcceptLines.Length > 0) ||
                               (npcData.foodReRejectLines != null && npcData.foodReRejectLines.Length > 0);
            }
            
            if (!string.IsNullOrEmpty(rejectDialogue))
            {
                ShowDialogueText(rejectDialogue);
            }
            
            // 상태 기록
            if (NPCStateManager.Instance != null)
            {
                NPCStateManager.Instance.RecordRefusal(npcName, requestedMedicine);
            }
            
            // 재요청 가능하면 NPC를 큐에 남겨두고, 아니면 제거
            if (hasReRequest && refusalCount == 0)
            {
                // 첫 거절이면 재요청 대사 표시하고 NPC는 큐에 남김
                StartCoroutine(ProcessRefusalAndReRequest(frontNPC));
            }
            else
            {
                // 두 번째 거절이거나 재요청 불가능하면 제거
                StartCoroutine(ProcessInteractionAndRemove(frontNPC));
            }
        }
        else
        {
            // NPC 데이터가 없으면 그냥 제거
            StartCoroutine(ProcessInteractionAndRemove(frontNPC));
        }
    }

    public void GiveLotusRice()
    {
        if (activeNPCs.Count == 0 || frontNPC == null || isProcessingInteraction) return;
        
        // Inventory 확인
        if (inventory != null && inventory.lotusRice <= 0)
        {
            ShowDialogueText("연밥이 부족합니다.");
            return;
        }
        
        // 상호작용 시작
        isProcessingInteraction = true;

        BohyunNPCData npcData = frontNPCComponent != null ? frontNPCComponent.bohyunData : null;
        string npcName = GetNPCName(frontNPC);
        
        // 거절 횟수 확인
        int refusalCount = 0;
        if (NPCStateManager.Instance != null)
        {
            refusalCount = NPCStateManager.Instance.GetRefusalCount(npcName);
        }
        
        string dialogue = "";
        bool isAccept = false;
        
        if (npcData != null)
        {
            // NPC가 요청한 것과 일치하는지 확인
            if (npcData.requestType == BohyunNPCRequestType.Food)
            {
                // 밥을 요청했고 밥을 줌 = Accept
                isAccept = true;
                
                // 한 번 거절 후 다시 받았을 때 대사
                if (refusalCount > 0 && npcData.foodReAcceptLines != null && npcData.foodReAcceptLines.Length > 0)
                {
                    dialogue = npcData.foodReAcceptLines[Random.Range(0, npcData.foodReAcceptLines.Length)];
                }
                else if (npcData.foodAcceptLines != null && npcData.foodAcceptLines.Length > 0)
                {
                    dialogue = npcData.foodAcceptLines[Random.Range(0, npcData.foodAcceptLines.Length)];
                }
                
                // 거절 횟수 리셋
                if (NPCStateManager.Instance != null)
                {
                    NPCStateManager.Instance.ResetRefusalCount(npcName);
                }
            }
            else if (npcData.requestType == BohyunNPCRequestType.Medicine)
            {
                // 약을 요청했는데 밥을 줌 = 거절로 간주
                isAccept = false;
                if (NPCStateManager.Instance != null)
                {
                    NPCStateManager.Instance.RecordRefusal(npcName, true);
                }
                
                // 두 번 거절당했을 때
                if (refusalCount >= 1 && npcData.medicineReRejectLines != null && npcData.medicineReRejectLines.Length > 0)
                {
                    dialogue = npcData.medicineReRejectLines[Random.Range(0, npcData.medicineReRejectLines.Length)];
                }
                else if (npcData.medicineRejectLines != null && npcData.medicineRejectLines.Length > 0)
                {
                    dialogue = npcData.medicineRejectLines[Random.Range(0, npcData.medicineRejectLines.Length)];
                }
            }
        }
        
        // 대사 표시
        if (!string.IsNullOrEmpty(dialogue))
        {
            ShowDialogueText(dialogue);
        }
        else if (isAccept)
        {
            // Accept 대사가 없으면 기본 메시지
            ShowDialogueText("Thank you!");
        }

        // Inventory 소모
        if (inventory != null)
        {
            inventory.UseLotusRice();
        }

        // 대사 표시 후 페이드아웃 및 제거
        StartCoroutine(ProcessInteractionAndRemove(frontNPC));
    }

    public void GiveHerbalMedicine()
    {
        if (activeNPCs.Count == 0 || frontNPC == null || isProcessingInteraction) return;
        
        // Inventory 확인
        if (inventory != null && inventory.herbalMedicine <= 0)
        {
            ShowDialogueText("약초가 부족합니다.");
            return;
        }
        
        // 상호작용 시작
        isProcessingInteraction = true;

        BohyunNPCData npcData = frontNPCComponent != null ? frontNPCComponent.bohyunData : null;
        string npcName = GetNPCName(frontNPC);
        
        // 거절 횟수 확인
        int refusalCount = 0;
        if (NPCStateManager.Instance != null)
        {
            refusalCount = NPCStateManager.Instance.GetRefusalCount(npcName);
        }
        
        string dialogue = "";
        bool isAccept = false;
        
        if (npcData != null)
        {
            // NPC가 요청한 것과 일치하는지 확인
            if (npcData.requestType == BohyunNPCRequestType.Medicine)
            {
                // 약을 요청했고 약을 줌 = Accept
                isAccept = true;
                if (NPCStateManager.Instance != null)
                {
                    NPCStateManager.Instance.RecordMedicineGiven(npcName);
                    NPCStateManager.Instance.ResetRefusalCount(npcName);
                }
                
                // 한 번 거절 후 다시 받았을 때 대사
                if (refusalCount > 0 && npcData.medicineReAcceptLines != null && npcData.medicineReAcceptLines.Length > 0)
                {
                    dialogue = npcData.medicineReAcceptLines[Random.Range(0, npcData.medicineReAcceptLines.Length)];
                }
                else if (npcData.medicineAcceptLines != null && npcData.medicineAcceptLines.Length > 0)
                {
                    dialogue = npcData.medicineAcceptLines[Random.Range(0, npcData.medicineAcceptLines.Length)];
                }
            }
            else if (npcData.requestType == BohyunNPCRequestType.Food)
            {
                // 밥을 요청했는데 약을 줌 = 거절로 간주
                isAccept = false;
                if (NPCStateManager.Instance != null)
                {
                    NPCStateManager.Instance.RecordRefusal(npcName, false);
                }
                
                // 두 번 거절당했을 때
                if (refusalCount >= 1 && npcData.foodReRejectLines != null && npcData.foodReRejectLines.Length > 0)
                {
                    dialogue = npcData.foodReRejectLines[Random.Range(0, npcData.foodReRejectLines.Length)];
                }
                else if (npcData.foodRejectLines != null && npcData.foodRejectLines.Length > 0)
                {
                    dialogue = npcData.foodRejectLines[Random.Range(0, npcData.foodRejectLines.Length)];
                }
            }
        }
        
        // 대사 표시
        if (!string.IsNullOrEmpty(dialogue))
        {
            ShowDialogueText(dialogue);
        }
        else if (isAccept)
        {
            // Accept 대사가 없으면 기본 메시지
            ShowDialogueText("Thank you!");
        }

        // Inventory 소모
        if (inventory != null)
        {
            inventory.UseHerbalMedicine();
        }

        // 대사 표시 후 페이드아웃 및 제거
        StartCoroutine(ProcessInteractionAndRemove(frontNPC));
    }

    [Header("Interaction Settings")]
    [Tooltip("대사 표시 후 페이드아웃까지 대기 시간")]
    public float dialogueDisplayDuration = 2f;

    /// <summary>
    /// 거절 후 재요청을 처리합니다. (거절 대사 표시 → 재요청 대사 표시 → NPC는 큐에 남김)
    /// </summary>
    System.Collections.IEnumerator ProcessRefusalAndReRequest(GameObject npc)
    {
        // 거절 대사 표시 시간 대기
        yield return new WaitForSeconds(dialogueDisplayDuration);
        
        // 재요청 대사 표시
        BohyunNPCData npcData = frontNPCComponent != null ? frontNPCComponent.bohyunData : null;
        if (npcData != null)
        {
            string reRequestDialogue = "";
            if (npcData.requestType == BohyunNPCRequestType.Medicine)
            {
                if (npcData.medicineReRequestLines != null && npcData.medicineReRequestLines.Length > 0)
                {
                    reRequestDialogue = npcData.medicineReRequestLines[Random.Range(0, npcData.medicineReRequestLines.Length)];
                }
            }
            else if (npcData.requestType == BohyunNPCRequestType.Food)
            {
                if (npcData.foodReRequestLines != null && npcData.foodReRequestLines.Length > 0)
                {
                    reRequestDialogue = npcData.foodReRequestLines[Random.Range(0, npcData.foodReRequestLines.Length)];
                }
            }
            
            if (!string.IsNullOrEmpty(reRequestDialogue))
            {
                ShowDialogueText(reRequestDialogue);
            }
        }
        
        // 상호작용 처리 완료 (NPC는 큐에 남아있음)
        isProcessingInteraction = false;
    }

    /// <summary>
    /// 상호작용을 처리하고 NPC를 제거합니다. (대사 표시 → 페이드아웃 → 제거 → 다음 NPC 이동)
    /// </summary>
    System.Collections.IEnumerator ProcessInteractionAndRemove(GameObject npcToRemove)
    {
        // 대사 표시 시간 대기
        yield return new WaitForSeconds(dialogueDisplayDuration);
        
        // center에 있는 NPC (activeNPCs[0]) 확인 및 제거
        if (activeNPCs.Count == 0)
        {
            isProcessingInteraction = false;
            yield break;
        }

        // activeNPCs[0]이 제거할 NPC인지 확인
        GameObject centerNPC = activeNPCs[0];
        if (centerNPC == null || centerNPC != npcToRemove)
        {
            Debug.LogWarning($"ProcessInteractionAndRemove: center NPC가 일치하지 않습니다. center={centerNPC?.name}, toRemove={npcToRemove?.name}");
            isProcessingInteraction = false;
            yield break;
        }

        // 리스트에서 제거 (첫 번째 요소 제거)
        activeNPCs.RemoveAt(0);
        npcTargetPositions.Remove(npcToRemove);
        npcArrivedAtPosition.Remove(npcToRemove);
        
        // 페이드아웃하며 왼쪽으로 이동
        Vector3 exitPos = new Vector3(-14f, npcToRemove.transform.position.y, npcToRemove.transform.position.z);
        yield return StartCoroutine(FadeOutAndMove(npcToRemove, exitPos));
        
        // NPC가 완전히 사라졌는지 확인하고 Destroy
        if (npcToRemove != null)
        {
            Destroy(npcToRemove);
        }
        
        // frontNPC 초기화
        frontNPC = null;
        frontNPCComponent = null;
        
        // 상호작용 처리 완료
        isProcessingInteraction = false;
        
        // 다음 NPC들이 앞으로 이동 (UpdateNPCPositions가 자동으로 처리)
        // 끝에 새 NPC 생성 (Update에서 자동으로 처리)
    }

    /// <summary>
    /// 대사 텍스트를 표시합니다.
    /// </summary>
    void ShowDialogueText(string dialogue)
    {
        if (speechBubbleBG != null)
            speechBubbleBG.SetActive(true);
        if (speechBubbleText != null)
        {
            speechBubbleText.gameObject.SetActive(true);
            speechBubbleText.text = dialogue;
        }
    }

    /// <summary>
    /// NPC 이름을 가져옵니다.
    /// </summary>
    string GetNPCName(GameObject npc)
    {
        if (npc == null) return "Unknown";
        
        NPCComponent component = npc.GetComponent<NPCComponent>();
        if (component != null && component.bohyunData != null)
        {
            return component.bohyunData.npcName;
        }
        
        return npc.name.Replace("(Clone)", "").Trim();
    }

    /// <summary>
    /// 새로운 DaySchedule로 하루를 시작합니다.
    /// </summary>
    public void StartDay(DaySchedule newSchedule)
    {
        // 기존 NPC들 모두 제거
        ClearAllNPCs();
        
        // 새로운 스케줄 설정
        daySchedule = newSchedule;
        currentSpawnIndex = 0;
        timer = 0f;
        isInitialSpawnComplete = false; // 새 날이 시작되면 초기 스폰 다시 시작
        frontNPC = null;
        frontNPCComponent = null;
        
        // 말풍선 숨기기
        HideSpeechBubble();
        
        // 첫 NPC 스폰
        if (daySchedule != null && daySchedule.npcPrefabs != null && daySchedule.npcPrefabs.Length > 0)
        {
            SpawnNextNPC();
        }
        else
        {
            isInitialSpawnComplete = true;
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
    public void ClearAllNPCs()
    {
        foreach (GameObject npc in activeNPCs)
        {
            if (npc != null)
            {
                NPC npcComponent = npc.GetComponent<NPC>();
                if (npcComponent != null)
                {
                    npcComponent.LeaveScene();
                }
                else
                {
                    MoveNPCToLeft(npc);
                }
            }
        }
        activeNPCs.Clear();
        npcTargetPositions.Clear();
        npcArrivedAtPosition.Clear();
    }

    /// <summary>
    /// 모든 NPC가 스폰되었는지 확인합니다.
    /// </summary>
    public bool IsSpawningComplete()
    {
        return isInitialSpawnComplete;
    }

    /// <summary>
    /// 현재 활성 NPC 수를 반환합니다.
    /// </summary>
    public int GetActiveNPCCount()
    {
        return activeNPCs.Count;
    }
}

