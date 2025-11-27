using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
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
    
    [Header("Special Events")]
    [Tooltip("도깨비사전 UI (무당 NPC를 만나면 활성화)")]
    public GameObject dictionaryUI;
    
    [Header("Typing Effect Settings")]
    [Tooltip("타이핑 속도 (초 단위, 작을수록 빠름)")]
    public float typingSpeed = 0.05f;
    [Tooltip("타이핑 소리 (선택사항)")]
    public AudioClip typingSound;
    [Tooltip("타이핑 소리 볼륨")]
    [Range(0f, 1f)]
    public float typingSoundVolume = 0.5f;
    
    [Header("Inventory (선택사항)")]
    public Inventory inventory;
    
    private List<GameObject> activeNPCs = new List<GameObject>(); // 활성 NPC들 (순서대로)
    private Dictionary<GameObject, Vector3> npcTargetPositions = new Dictionary<GameObject, Vector3>();
    private Dictionary<GameObject, bool> npcArrivedAtPosition = new Dictionary<GameObject, bool>(); // NPC가 목표 위치에 도착했는지
    private float timer = 0f;
    private int currentSpawnIndex = 0;
    private bool isInitialSpawnComplete = false; // 초기 스폰 완료 여부
    
    // 타이핑 효과 관련
    private AudioSource typingAudioSource;
    private Coroutine typingCoroutine;
    private bool isTypingComplete = true; // 타이핑이 완료되었는지 여부
    private string currentTypingText = ""; // 현재 타이핑 중인 전체 텍스트
    
    // 현재 가장 앞에 있는 NPC (queueSlots[0]에 있는 NPC = center)
    private GameObject frontNPC = null;
    private NPCComponent frontNPCComponent = null;
    
    // 상호작용 처리 중인지 여부 (대사 표시 중에는 다음 상호작용 방지)
    private bool isProcessingInteraction = false;
    
    // 무당 이벤트 관련
    private bool isCurrentNPCShaman = false; // 현재 상호작용 중인 NPC가 무당인지
    
    [Header("Position Settings")]
    [Tooltip("NPC가 목표 위치에 도착했다고 판단하는 거리")]
    public float arrivalDistance = 0.1f;
    [Tooltip("NPC 이동 속도")]
    public float npcMoveSpeed = 5f;
    
    [Header("Refusal Settings")]
    [Tooltip("거절당한 NPC가 재요청할 확률 (0.0 ~ 1.0, 예: 0.5 = 50%)")]
    [Range(0f, 1f)]
    public float reRequestChance = 0.5f;
    
    [Header("Visual Settings")]
    [Tooltip("뒤에 있는 NPC의 어둡기 정도 (0.0 = 완전히 어둡게, 1.0 = 변화 없음)")]
    [Range(0f, 1f)]
    public float minBrightness = 0.5f; // 뒤에 있는 NPC의 최소 밝기

    void Start()
    {
        // NPCStateManager에 오늘 등장할 모든 NPC 이름 목록 설정
        if (daySchedule != null && daySchedule.npcPrefabs != null && NPCStateManager.Instance != null)
        {
            List<string> npcNames = new List<string>();
            foreach (GameObject prefab in daySchedule.npcPrefabs)
            {
                if (prefab != null)
                {
                    NPCComponent npcComponent = prefab.GetComponent<NPCComponent>();
                    if (npcComponent != null && npcComponent.bohyunData != null && !string.IsNullOrEmpty(npcComponent.bohyunData.npcName))
                    {
                        npcNames.Add(npcComponent.bohyunData.npcName);
                    }
                }
            }
            NPCStateManager.Instance.SetAllNPCNames(npcNames);
        }
        
        // 말풍선 초기화
        // 타이핑 코루틴 중지
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        
        // 타이핑 소리 중지
        if (typingAudioSource != null && typingAudioSource.isPlaying)
        {
            typingAudioSource.Stop();
        }
        
        if (speechBubbleBG != null)
            speechBubbleBG.SetActive(false);
        if (speechBubbleText != null)
            speechBubbleText.gameObject.SetActive(false);
        
        // 타이핑 소리용 AudioSource 초기화
        if (typingSound != null)
        {
            typingAudioSource = gameObject.AddComponent<AudioSource>();
            typingAudioSource.playOnAwake = false;
            typingAudioSource.loop = true;
            typingAudioSource.volume = typingSoundVolume;
        }
        
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
        
        // 타이핑 중 클릭 감지
        CheckTypingSkip();
        
        // 모든 상호작용이 끝났는지 확인 (Update에서도 체크)
        if (!isProcessingInteraction)
        {
            CheckAndTransitionToNighttime();
        }
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
        
        // NPC.cs가 삭제되어 NPCComponent만 사용
        // NPC 컴포넌트는 더 이상 필요 없음
        
        activeNPCs.Add(npc);
        
        // 스폰 직후 목표 위치 설정 (UpdateNPCPositions에서 자동으로 이동)
        if (queueSlots != null && queueSlots.Length > 0 && activeNPCs.Count <= queueSlots.Length)
        {
            int targetIndex = activeNPCs.Count - 1; // 현재 추가된 NPC의 인덱스
            if (targetIndex < queueSlots.Length && queueSlots[targetIndex] != null)
            {
                Vector3 targetPos = queueSlots[targetIndex].position;
                npcTargetPositions[npc] = targetPos;
                npcArrivedAtPosition[npc] = false; // 아직 도착하지 않음
                // NPC는 UpdateNPCPositions()에서 자동으로 목표 위치로 이동함
            }
        }
        else
        {
            // 큐가 가득 찬 경우 스폰 위치에 대기
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
                
                // NPC.cs가 삭제되어 SetTarget 메서드 사용 불가
                // 타겟 위치는 Dictionary에만 저장됨
                // NPC는 UpdateNPCPositions에서 자동으로 이동함
            }
            
            // NPC를 목표 위치로 이동
            if (npcTargetPositions.ContainsKey(npc))
            {
                Vector3 currentPos = npc.transform.position;
                Vector3 npcTargetPos = npcTargetPositions[npc];
                float distanceToTarget = Vector3.Distance(currentPos, npcTargetPos);
                
                // 목표 위치에 도착하지 않았으면 이동
                if (distanceToTarget > arrivalDistance)
                {
                    npc.transform.position = Vector3.MoveTowards(
                        currentPos,
                        npcTargetPos,
                        npcMoveSpeed * Time.deltaTime
                    );
                    npcArrivedAtPosition[npc] = false;
                }
                else
                {
                    // 도착했으면 정확한 위치로 설정
                    npc.transform.position = npcTargetPos;
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
            
            // NPC.cs가 삭제되어 SetSortingOrder 메서드 사용 불가
            // SpriteRenderer를 직접 사용
            int order = baseOrder - i; // 앞에 있는 NPC가 더 높은 order
            SpriteRenderer[] spriteRenderers = activeNPCs[i].GetComponentsInChildren<SpriteRenderer>();
            foreach (SpriteRenderer sr in spriteRenderers)
            {
                if (sr != null) sr.sortingOrder = order;
            }
            
            // 뒤로 갈수록 어둡게 만들기
            UpdateNPCBrightness(activeNPCs[i], i);
        }
    }
    
    /// <summary>
    /// NPC의 밝기를 큐 위치에 따라 조절합니다. 뒤로 갈수록 어둡게 만듭니다.
    /// </summary>
    void UpdateNPCBrightness(GameObject npc, int queueIndex)
    {
        if (npc == null) return;
        
        // 첫 번째 NPC는 원래 밝기 유지
        if (queueIndex == 0)
        {
            // 원래 색상으로 복원
            SpriteRenderer[] spriteRenderers = npc.GetComponentsInChildren<SpriteRenderer>();
            foreach (var sr in spriteRenderers)
            {
                if (sr != null)
                {
                    Color color = sr.color;
                    color.r = 1f;
                    color.g = 1f;
                    color.b = 1f;
                    sr.color = color;
                }
            }
            return;
        }
        
        // 뒤로 갈수록 어둡게
        // queueIndex가 1일 때: 밝기 0.6
        // queueIndex가 2일 때: 밝기 0.4
        // queueIndex가 3 이상일 때: minBrightness까지 감소
        float brightness;
        if (queueIndex == 1)
        {
            brightness = 0.6f;
        }
        else if (queueIndex == 2)
        {
            brightness = 0.4f;
        }
        else
        {
            // 3 이상일 때는 minBrightness까지 선형 감소
            brightness = Mathf.Lerp(0.4f, minBrightness, (float)(queueIndex - 2) / Mathf.Max(1f, (queueSlots != null ? queueSlots.Length - 1 : 3f) - 2));
        }
        
        SpriteRenderer[] renderers = npc.GetComponentsInChildren<SpriteRenderer>();
        foreach (var renderer in renderers)
        {
            if (renderer != null)
            {
                Color color = renderer.color;
                color.r = brightness;
                color.g = brightness;
                color.b = brightness;
                renderer.color = color;
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
                // 무당 NPC인지 확인 (상호작용 완료 후 이벤트 트리거용)
                isCurrentNPCShaman = IsShamanNPC(frontNPCComponent.bohyunData);
                
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
                // 무당 NPC인지 확인 (상호작용 완료 후 이벤트 트리거용)
                isCurrentNPCShaman = IsShamanNPC(frontNPCComponent.bohyunData);
                
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

        // 말풍선 표시 (타이핑 효과 포함)
        ShowDialogueText(dialogue);
    }

    /// <summary>
    /// 말풍선을 숨깁니다.
    /// </summary>
    void HideSpeechBubble()
    {
        // 타이핑 코루틴 중지
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        
        // 타이핑 소리 중지
        if (typingAudioSource != null && typingAudioSource.isPlaying)
        {
            typingAudioSource.Stop();
        }
        
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
            
            // 재요청 가능 여부 확인 (re-accept나 re-reject 대사가 있으면 재요청 가능)
            bool hasReRequest = false;
            if (requestedMedicine)
            {
                hasReRequest = (npcData.medicineReAcceptLines != null && npcData.medicineReAcceptLines.Length > 0) ||
                               (npcData.medicineReRejectLines != null && npcData.medicineReRejectLines.Length > 0);
            }
            else
            {
                hasReRequest = (npcData.foodReAcceptLines != null && npcData.foodReAcceptLines.Length > 0) ||
                               (npcData.foodReRejectLines != null && npcData.foodReRejectLines.Length > 0);
            }
            
            // 상태 기록
            if (NPCStateManager.Instance != null)
            {
                NPCStateManager.Instance.RecordRefusal(npcName, requestedMedicine);
            }
            
            // 재요청 가능하고 첫 거절이면, 랜덤으로 재요청 여부 결정
            if (hasReRequest && refusalCount == 0)
            {
                // 랜덤으로 재요청할지 결정
                if (Random.value <= reRequestChance)
                {
                    // 재요청하는 경우: 거절 대사 건너뛰고 바로 재요청 대사만 표시
                    StartCoroutine(ProcessRefusalAndReRequest(frontNPC, skipRejectDialogue: true));
                }
                else
                {
                    // 재요청하지 않는 경우: 거절 대사 표시 후 지나감
                    string rejectDialogue = "";
                    if (requestedMedicine)
                    {
                        if (npcData.medicineRejectLines != null && npcData.medicineRejectLines.Length > 0)
                        {
                            rejectDialogue = npcData.medicineRejectLines[Random.Range(0, npcData.medicineRejectLines.Length)];
                        }
                    }
                    else
                    {
                        if (npcData.foodRejectLines != null && npcData.foodRejectLines.Length > 0)
                        {
                            rejectDialogue = npcData.foodRejectLines[Random.Range(0, npcData.foodRejectLines.Length)];
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(rejectDialogue))
                    {
                        ShowDialogueText(rejectDialogue);
                    }
                    
                    StartCoroutine(ProcessInteractionAndRemove(frontNPC));
                }
            }
            else
            {
                // 두 번째 거절이거나 재요청 불가능: 거절 대사 표시 후 제거
                string rejectDialogue = "";
                if (requestedMedicine)
                {
                    // 두 번 거절당했을 때
                    if (refusalCount >= 1)
                    {
                        if (npcData.medicineReRejectLines != null && npcData.medicineReRejectLines.Length > 0)
                        {
                            rejectDialogue = npcData.medicineReRejectLines[Random.Range(0, npcData.medicineReRejectLines.Length)];
                        }
                        else
                        {
                            // re-reject 대사가 없으면 기본 메시지
                            rejectDialogue = "I'm gonna die tomorrow...";
                        }
                    }
                    // 첫 거절
                    else if (npcData.medicineRejectLines != null && npcData.medicineRejectLines.Length > 0)
                    {
                        rejectDialogue = npcData.medicineRejectLines[Random.Range(0, npcData.medicineRejectLines.Length)];
                    }
                }
                else
                {
                    // 두 번 거절당했을 때
                    if (refusalCount >= 1)
                    {
                        if (npcData.foodReRejectLines != null && npcData.foodReRejectLines.Length > 0)
                        {
                            rejectDialogue = npcData.foodReRejectLines[Random.Range(0, npcData.foodReRejectLines.Length)];
                        }
                        else
                        {
                            // re-reject 대사가 없으면 기본 메시지
                            rejectDialogue = "I'm gonna die tomorrow...";
                        }
                    }
                    // 첫 거절
                    else if (npcData.foodRejectLines != null && npcData.foodRejectLines.Length > 0)
                    {
                        rejectDialogue = npcData.foodRejectLines[Random.Range(0, npcData.foodRejectLines.Length)];
                    }
                }
                
                if (!string.IsNullOrEmpty(rejectDialogue))
                {
                    ShowDialogueText(rejectDialogue);
                }
                
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
        
        // 무당 NPC인지 확인 (상호작용 완료 후 이벤트 트리거용)
        isCurrentNPCShaman = IsShamanNPC(npcData);
        
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
                
                // 밥을 준 기록
                if (NPCStateManager.Instance != null)
                {
                    NPCStateManager.Instance.RecordFoodGiven(npcName);
                    NPCStateManager.Instance.ResetRefusalCount(npcName);
                }
                
                // 한 번 거절 후 다시 받았을 때 대사
                if (refusalCount > 0)
                {
                    if (npcData.foodReAcceptLines != null && npcData.foodReAcceptLines.Length > 0)
                    {
                        dialogue = npcData.foodReAcceptLines[Random.Range(0, npcData.foodReAcceptLines.Length)];
                    }
                    else
                    {
                        // re-accept 대사가 없으면 기본 메시지
                        dialogue = "Thank you... Thank you...";
                    }
                }
                else if (npcData.foodAcceptLines != null && npcData.foodAcceptLines.Length > 0)
                {
                    dialogue = npcData.foodAcceptLines[Random.Range(0, npcData.foodAcceptLines.Length)];
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
                if (refusalCount >= 1)
                {
                    if (npcData.medicineReRejectLines != null && npcData.medicineReRejectLines.Length > 0)
                    {
                        dialogue = npcData.medicineReRejectLines[Random.Range(0, npcData.medicineReRejectLines.Length)];
                    }
                    else
                    {
                        // re-reject 대사가 없으면 기본 메시지
                        dialogue = "I'm gonna die tomorrow...";
                    }
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

        // 무당 이벤트 트리거 (상호작용 완료 후)
        TriggerShamanEvent();

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
        
        // 무당 NPC인지 확인 (상호작용 완료 후 이벤트 트리거용)
        isCurrentNPCShaman = IsShamanNPC(npcData);
        
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
                if (refusalCount > 0)
                {
                    if (npcData.medicineReAcceptLines != null && npcData.medicineReAcceptLines.Length > 0)
                    {
                        dialogue = npcData.medicineReAcceptLines[Random.Range(0, npcData.medicineReAcceptLines.Length)];
                    }
                    else
                    {
                        // re-accept 대사가 없으면 기본 메시지
                        dialogue = "Thank you... Thank you...";
                    }
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
                if (refusalCount >= 1)
                {
                    if (npcData.foodReRejectLines != null && npcData.foodReRejectLines.Length > 0)
                    {
                        dialogue = npcData.foodReRejectLines[Random.Range(0, npcData.foodReRejectLines.Length)];
                    }
                    else
                    {
                        // re-reject 대사가 없으면 기본 메시지
                        dialogue = "I'm gonna die tomorrow...";
                    }
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

        // 무당 이벤트 트리거 (상호작용 완료 후)
        TriggerShamanEvent();

        // 대사 표시 후 페이드아웃 및 제거
        StartCoroutine(ProcessInteractionAndRemove(frontNPC));
    }

    [Header("Interaction Settings")]
    [Tooltip("타이핑 완료 후 말풍선이 사라지기까지 대기 시간 (초)")]
    public float dialogueDisplayDuration = 0.5f;

    /// <summary>
    /// 거절 후 재요청을 처리합니다. (거절 대사 표시 → 재요청 대사 표시 → NPC는 큐에 남김)
    /// </summary>
    System.Collections.IEnumerator ProcessRefusalAndReRequest(GameObject npc, bool skipRejectDialogue = false)
    {
        // 거절 대사를 건너뛰지 않는 경우에만 대기
        if (!skipRejectDialogue)
        {
            // 거절 대사 표시 시간 대기
            yield return new WaitForSeconds(dialogueDisplayDuration);
        }
        
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
        
        // 무당 이벤트 트리거 (상호작용 완료 후)
        TriggerShamanEvent();
        
        // 상호작용 처리 완료 (NPC는 큐에 남아있음)
        isProcessingInteraction = false;
    }

    /// <summary>
    /// 상호작용을 처리하고 NPC를 제거합니다. (대사 표시 → 페이드아웃 → 제거 → 다음 NPC 이동)
    /// </summary>
    System.Collections.IEnumerator ProcessInteractionAndRemove(GameObject npcToRemove)
    {
        // 타이핑이 완료될 때까지 대기
        while (!isTypingComplete)
        {
            yield return null;
        }
        
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
        
        // 무당 이벤트 트리거 (상호작용 완료 후)
        TriggerShamanEvent();
        
        // frontNPC 초기화
        frontNPC = null;
        frontNPCComponent = null;
        
        // 상호작용 처리 완료
        isProcessingInteraction = false;
        
        // 모든 상호작용이 끝났는지 확인
        CheckAndTransitionToNighttime();
        
        // 다음 NPC들이 앞으로 이동 (UpdateNPCPositions가 자동으로 처리)
        // 끝에 새 NPC 생성 (Update에서 자동으로 처리)
    }
    
    [Header("Nighttime Settings")]
    [Tooltip("Nighttime 씬 이름")]
    public string nighttimeSceneName = "Nighttime";
    
    [Tooltip("페이드아웃 오버레이 (자동 생성 가능)")]
    public Image fadeOverlay;
    
    [Tooltip("씬 전환 페이드아웃 시간 (초)")]
    public float sceneTransitionFadeDuration = 1f;
    
    [Tooltip("페이드아웃 색상")]
    public Color fadeColor = Color.black;
    
    private bool isTransitioning = false; // 전환 중인지 여부
    
    /// <summary>
    /// 모든 상호작용이 끝났는지 확인하고 Nighttime으로 전환합니다.
    /// </summary>
    void CheckAndTransitionToNighttime()
    {
        // 이미 전환 중이면 무시
        if (isTransitioning) return;
        
        // 큐에 NPC가 없고, 모든 NPC가 스폰되었는지 확인
        if (daySchedule == null || daySchedule.npcPrefabs == null)
            return;
        
        int totalNPCs = daySchedule.npcPrefabs.Length;
        bool allSpawned = currentSpawnIndex >= totalNPCs;
        bool queueEmpty = activeNPCs.Count == 0;
        bool notProcessing = !isProcessingInteraction;
        
        if (allSpawned && queueEmpty && notProcessing)
        {
            // 모든 상호작용 완료 - Nighttime으로 전환
            Debug.Log("모든 상호작용이 완료되었습니다. Nighttime으로 전환합니다.");
            isTransitioning = true;
            StartCoroutine(TransitionToNighttime());
        }
    }
    
    /// <summary>
    /// 페이드아웃 오버레이를 생성합니다.
    /// </summary>
    void CreateFadeOverlay()
    {
        if (fadeOverlay != null) return; // 이미 있으면 생성하지 않음
        
        // Canvas 찾기
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("NPCQueueSystem: Canvas를 찾을 수 없어 페이드아웃 오버레이를 생성할 수 없습니다.");
            return;
        }
        
        // 페이드아웃용 Image GameObject 생성
        GameObject fadeObj = new GameObject("FadeOverlay");
        fadeObj.transform.SetParent(canvas.transform, false);
        
        // RectTransform 설정 (전체 화면 덮기)
        RectTransform rectTransform = fadeObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
        
        // Image 컴포넌트 추가
        fadeOverlay = fadeObj.AddComponent<Image>();
        fadeOverlay.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f); // 초기에는 투명
        fadeOverlay.raycastTarget = false; // 클릭 이벤트 차단하지 않음
        
        // 가장 위에 표시되도록 설정
        fadeObj.transform.SetAsLastSibling();
    }
    
    /// <summary>
    /// 페이드아웃 효과와 함께 Nighttime 씬으로 전환합니다.
    /// </summary>
    IEnumerator TransitionToNighttime()
    {
        // 페이드아웃 오버레이가 없으면 생성
        if (fadeOverlay == null)
        {
            CreateFadeOverlay();
        }
        
        if (fadeOverlay == null)
        {
            // 오버레이를 생성할 수 없으면 바로 씬 전환
            Debug.LogWarning("NPCQueueSystem: 페이드아웃 오버레이를 생성할 수 없어 바로 씬을 전환합니다.");
            if (!string.IsNullOrEmpty(nighttimeSceneName))
            {
                SceneManager.LoadScene(nighttimeSceneName);
            }
            yield break;
        }
        
        float elapsedTime = 0f;
        Color startColor = fadeOverlay.color;
        Color targetColor = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f);
        
        // 페이드아웃 시작
        while (elapsedTime < sceneTransitionFadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / sceneTransitionFadeDuration;
            
            // Ease in quadratic
            t = t * t;
            
            fadeOverlay.color = Color.Lerp(startColor, targetColor, t);
            
            yield return null;
        }
        
        // 최종 색상 설정
        fadeOverlay.color = targetColor;
        
        // 씬 전환
        if (!string.IsNullOrEmpty(nighttimeSceneName))
        {
            SceneManager.LoadScene(nighttimeSceneName);
        }
        else
        {
            Debug.LogWarning("Nighttime 씬 이름이 설정되지 않았습니다.");
            isTransitioning = false; // 전환 실패 시 플래그 리셋
        }
    }

    /// <summary>
    /// 대사 텍스트를 표시합니다 (타이핑 효과 포함).
    /// </summary>
    void ShowDialogueText(string dialogue)
    {
        if (speechBubbleBG != null)
            speechBubbleBG.SetActive(true);
        if (speechBubbleText != null)
        {
            speechBubbleText.gameObject.SetActive(true);
            // 기존 타이핑 코루틴이 있으면 중지
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }
            // 타이핑 효과 시작
            typingCoroutine = StartCoroutine(TypeText(dialogue));
        }
    }
    
    /// <summary>
    /// 텍스트를 타이핑 효과로 표시합니다.
    /// </summary>
    private System.Collections.IEnumerator TypeText(string fullText)
    {
        if (speechBubbleText == null)
        {
            isTypingComplete = true;
            currentTypingText = "";
            yield break;
        }
        
        isTypingComplete = false; // 타이핑 시작
        currentTypingText = fullText; // 현재 타이핑 중인 텍스트 저장
        speechBubbleText.text = "";
        
        // 타이핑 소리 시작
        bool isPlayingTypingSound = false;
        if (typingSound != null && typingAudioSource != null)
        {
            typingAudioSource.clip = typingSound;
            typingAudioSource.volume = typingSoundVolume;
            typingAudioSource.Play();
            isPlayingTypingSound = true;
        }
        
        // 한 글자씩 타이핑
        for (int i = 0; i <= fullText.Length; i++)
        {
            speechBubbleText.text = fullText.Substring(0, i);
            yield return new WaitForSeconds(typingSpeed);
        }
        
        // 타이핑 소리 중지
        if (isPlayingTypingSound && typingAudioSource != null)
        {
            typingAudioSource.Stop();
        }
        
        typingCoroutine = null;
        isTypingComplete = true; // 타이핑 완료
        currentTypingText = ""; // 타이핑 완료 후 초기화
    }

    /// <summary>
    /// 타이핑 중 말풍선 클릭 시 타이핑을 건너뛰고 전체 텍스트를 표시합니다.
    /// </summary>
    void CheckTypingSkip()
    {
        // 타이핑 중이 아니면 무시
        if (isTypingComplete || string.IsNullOrEmpty(currentTypingText)) return;
        
        // 말풍선이 활성화되어 있지 않으면 무시
        if (speechBubbleBG == null || !speechBubbleBG.activeSelf) return;
        
        // 마우스 클릭 감지 (새 Input System 사용)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            // 타이핑 건너뛰기 (말풍선이 활성화되어 있으면 클릭 시 스킵)
            SkipTyping();
        }
    }
    
    /// <summary>
    /// 타이핑을 건너뛰고 전체 텍스트를 즉시 표시합니다.
    /// </summary>
    void SkipTyping()
    {
        // 타이핑 코루틴 중지
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        
        // 전체 텍스트 즉시 표시
        if (speechBubbleText != null && !string.IsNullOrEmpty(currentTypingText))
        {
            speechBubbleText.text = currentTypingText;
        }
        
        // 타이핑 소리 중지
        if (typingAudioSource != null && typingAudioSource.isPlaying)
        {
            typingAudioSource.Stop();
        }
        
        // 타이핑 완료로 표시 (클릭으로 스킵한 경우도 타이핑 완료로 간주)
        isTypingComplete = true;
        currentTypingText = "";
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
    /// NPC가 무당인지 확인합니다.
    /// </summary>
    bool IsShamanNPC(BohyunNPCData npcData)
    {
        if (npcData == null) return false;
        return npcData.npcName != null && npcData.npcName.StartsWith("Shaman", System.StringComparison.OrdinalIgnoreCase);
    }
    
    /// <summary>
    /// 무당 NPC와의 상호작용 완료 후 도깨비사전을 활성화합니다.
    /// </summary>
    void TriggerShamanEvent()
    {
        // 현재 상호작용 중인 NPC가 무당인지 확인
        if (!isCurrentNPCShaman) return;
        
        // NPCStateManager에서 이미 이벤트가 발생했는지 확인
        if (NPCStateManager.Instance != null && NPCStateManager.Instance.HasShamanEventTriggered())
        {
            return; // 이미 발생했으면 다시 발생하지 않음
        }
        
        // 도깨비사전 활성화 (페이드인 효과 포함)
        if (dictionaryUI != null)
        {
            StartCoroutine(FadeInDictionaryUI());
            
            // NPCStateManager에 이벤트 발생 기록
            if (NPCStateManager.Instance != null)
            {
                NPCStateManager.Instance.SetShamanEventTriggered();
            }
            
            Debug.Log("무당 NPC와의 대화가 완료되었습니다! 도깨비사전이 활성화되었습니다.");
        }
        else
        {
            Debug.LogWarning("NPCQueueSystem: DictionaryUI가 할당되지 않았습니다.");
        }
    }
    
    /// <summary>
    /// 도깨비사전 UI를 페이드인 효과와 함께 표시합니다.
    /// </summary>
    System.Collections.IEnumerator FadeInDictionaryUI()
    {
        if (dictionaryUI == null) yield break;
        
        // CanvasGroup 컴포넌트 확인 및 추가
        CanvasGroup canvasGroup = dictionaryUI.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = dictionaryUI.AddComponent<CanvasGroup>();
        }
        
        // 초기 상태: 알파 0, 활성화
        canvasGroup.alpha = 0f;
        dictionaryUI.SetActive(true);
        
        // 페이드인 시간
        float fadeDuration = 0.5f;
        float elapsedTime = 0f;
        
        // 페이드인 애니메이션
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / fadeDuration);
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }
        
        // 최종 알파 설정
        canvasGroup.alpha = 1f;
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
                // NPC.cs가 삭제되어 LeaveScene 메서드 사용 불가
                // MoveNPCToLeft 사용
                MoveNPCToLeft(npc);
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
    
    /// <summary>
    /// 현재 스폰 인덱스를 반환합니다 (이미 스폰된 NPC 수).
    /// </summary>
    public int GetCurrentSpawnIndex()
    {
        return currentSpawnIndex;
    }
}

