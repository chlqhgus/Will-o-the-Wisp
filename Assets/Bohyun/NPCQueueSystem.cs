using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// NPC 줄세우기 및 대사 시스템
/// </summary>
public class NPCQueueSystem : MonoBehaviour
{
    [Header("Spawn Settings")]
    public Transform spawnPoint; 
    public Transform[] queueSlots; 

    
    [Header("Random Queue Settings")]
    [Tooltip("랜덤 큐를 사용할지 여부 (true면 NPCRandomQueueManager에서 자동으로 큐 생성)")]
    public bool useRandomQueue = true; // 기본값: true (랜덤 큐 사용)
    private List<GameObject> randomQueuePrefabs = new List<GameObject>();
    
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
    
    [Header("Nighttime UI Manager")]
    [Tooltip("NighttimeUIManager 참조 (비어있으면 자동으로 찾음)")]
    public NighttimeUIManager nighttimeUIManager;
    
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
    
    // 현재 NPC의 요청 타입 (true = 약 요청, false = 밥 요청)
    private bool currentNPCRequestedMedicine = false;
    
    // 상호작용 처리 중인지 여부 (대사 표시 중에는 다음 상호작용 방지)
    private bool isProcessingInteraction = false;
    
    // 무당 이벤트 관련
    private bool isCurrentNPCShaman = false; // 현재 상호작용 중인 NPC가 무당인지
    
    [Header("Position Settings")]
    [Tooltip("NPC가 목표 위치에 도착했다고 판단하는 거리")]
    public float arrivalDistance = 0.1f;
    [Tooltip("NPC 이동 속도")]
    public float npcMoveSpeed = 5f;
    [Tooltip("NPC 스폰 간격 (초 단위, 작을수록 빠르게 스폰)")]
    public float spawnInterval = 3f;
    
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
        // 랜덤 큐 사용 시 NPCRandomQueueManager에서 직접 큐 가져오기
        if (useRandomQueue)
        {
            Debug.Log("NPCQueueSystem: useRandomQueue가 true입니다. NPCRandomQueueManager를 찾는 중...");
            
            if (NPCRandomQueueManager.Instance != null)
            {
                Debug.Log($"NPCQueueSystem: NPCRandomQueueManager.Instance를 찾았습니다. 큐 생성 중...");
                randomQueuePrefabs = NPCRandomQueueManager.Instance.GenerateDayQueue();
                
                Debug.Log($"NPCQueueSystem: 큐 생성 완료. 생성된 NPC 수: {randomQueuePrefabs?.Count ?? 0}");
                
                if (randomQueuePrefabs != null && randomQueuePrefabs.Count > 0)
                {
                    Debug.Log($"NPCQueueSystem: NPCRandomQueueManager에서 랜덤 큐를 가져왔습니다. ({randomQueuePrefabs.Count}개의 NPC)");
                    
                    // NPCStateManager에 오늘 등장할 모든 NPC 이름 목록 설정 (고유 ID 사용)
                    if (NPCStateManager.Instance != null)
                    {
                        List<string> npcNames = new List<string>();
                        int validNPCs = 0;
                        int invalidNPCs = 0;
                        
                        foreach (GameObject prefab in randomQueuePrefabs)
                        {
                            if (prefab != null)
                            {
                                // 고유 ID 가져오기 (프리팹 이름 사용)
                                string npcUniqueName = GetNPCName(prefab);
                                if (!string.IsNullOrEmpty(npcUniqueName))
                                {
                                    npcNames.Add(npcUniqueName);
                                    validNPCs++;
                                    Debug.Log($"NPCQueueSystem: NPC 이름 추가 - {npcUniqueName}");
                                }
                                else
                                {
                                    Debug.LogWarning($"NPCQueueSystem: 프리팹 '{prefab.name}'의 고유 이름을 가져올 수 없습니다.");
                                    invalidNPCs++;
                                }
                            }
                        }
                        
                        Debug.Log($"NPCQueueSystem: 유효한 NPC: {validNPCs}개, 유효하지 않은 NPC: {invalidNPCs}개");
                        Debug.Log($"NPCQueueSystem: NPC 이름 목록 설정 - 총 {npcNames.Count}개");
                        NPCStateManager.Instance.SetAllNPCNames(npcNames);
                    }
                    else
                    {
                        Debug.LogWarning("NPCQueueSystem: NPCStateManager.Instance를 찾을 수 없습니다.");
                    }
                }
                else
                {
                    Debug.LogError($"NPCQueueSystem: 랜덤 큐 생성에 실패했습니다. randomQueuePrefabs가 null이거나 비어있습니다. (Count: {randomQueuePrefabs?.Count ?? 0})");
                    Debug.LogError("NPCQueueSystem: NPCRandomQueueManager의 NPC 프리팹 설정을 확인해주세요.");
                }
            }
            else
            {
                Debug.LogError("NPCQueueSystem: useRandomQueue가 true이지만 NPCRandomQueueManager를 찾을 수 없습니다.");
                Debug.LogError("NPCQueueSystem: 씬에 NPCRandomQueueManager 컴포넌트가 있는 GameObject를 추가해주세요.");
            }
        }
        // DaySchedule은 더 이상 사용하지 않음 (랜덤 큐만 사용)
        
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
            typingAudioSource.clip = typingSound;
            
            Debug.Log($"NPCQueueSystem: 타이핑 소리 초기화 완료. Clip: {typingSound.name}, Volume: {typingSoundVolume}");
        }
        else
        {
            Debug.LogWarning("NPCQueueSystem: Typing Sound가 할당되지 않았습니다. Inspector에서 할당해주세요.");
        }
        
        // AudioListener 확인
        AudioListener listener = FindFirstObjectByType<AudioListener>();
        if (listener == null)
        {
            Debug.LogError("NPCQueueSystem: 씬에 AudioListener가 없습니다! Main Camera에 AudioListener 컴포넌트를 추가해주세요.");
        }
        else
        {
            Debug.Log($"NPCQueueSystem: AudioListener 발견 - {listener.gameObject.name}");
        }
        
        // 초기화
        isInitialSpawnComplete = false;
        currentSpawnIndex = 0;
        timer = 0f;
        
        // NPC 프리팹이 없으면 스폰 완료로 표시
        if (randomQueuePrefabs == null || randomQueuePrefabs.Count == 0)
        {
            isInitialSpawnComplete = true;
        }
    }
    
    public void StartNewDay()
    {
        Debug.Log("[NPCQueueSystem] StartNewDay() - 다음 날 시작");
        
        if (NPCStateManager.Instance != null)
        {
            NPCStateManager.Instance.OnNewDay();
            Debug.Log("[NPCQueueSystem] StartNewDay() - 상태 리셋 완료");
        }
        
        ClearAllNPCs();
        
        isInitialSpawnComplete = false;
        currentSpawnIndex = 0;
        timer = 0f;
        isTransitioning = false;
        isProcessingInteraction = false;
        frontNPC = null;
        frontNPCComponent = null;
        isCurrentNPCShaman = false;
        
        if (NPCRandomQueueManager.Instance != null)
        {
            randomQueuePrefabs = NPCRandomQueueManager.Instance.GenerateDayQueue();
            
            if (randomQueuePrefabs != null && randomQueuePrefabs.Count > 0 && NPCStateManager.Instance != null)
            {
                List<string> npcNames = new List<string>();
                foreach (GameObject prefab in randomQueuePrefabs)
                {
                    if (prefab != null)
                    {
                        // 고유 ID 가져오기 (프리팹 이름 사용)
                        string npcUniqueName = GetNPCName(prefab);
                        if (!string.IsNullOrEmpty(npcUniqueName))
                        {
                            npcNames.Add(npcUniqueName);
                        }
                    }
                }
                NPCStateManager.Instance.SetAllNPCNames(npcNames);
                Debug.Log($"[NPCQueueSystem] StartNewDay() - 새로운 큐 생성 완료: {randomQueuePrefabs.Count}명, NPC 이름 목록 설정: {npcNames.Count}개");
            }
        }
        else
        {
            Debug.LogWarning("[NPCQueueSystem] StartNewDay() - NPCRandomQueueManager를 찾을 수 없습니다.");
        }
    }

    void Update()
    {
        // 초기 스폰만 수행 (한 번만 실행)
        if (!isInitialSpawnComplete)
        {
            int maxSpawnCount = 0;
            
            // 랜덤 큐 사용
            if (randomQueuePrefabs != null && randomQueuePrefabs.Count > 0)
            {
                maxSpawnCount = randomQueuePrefabs.Count;
            }
            
            if (maxSpawnCount > 0)
            {
                int maxQueueSize = queueSlots != null ? queueSlots.Length : 0;
                int targetCount = Mathf.Min(maxSpawnCount, maxQueueSize);
                                
                if (activeNPCs.Count < targetCount && currentSpawnIndex < maxSpawnCount)
                {
                    timer += Time.deltaTime;
                    
                    if (timer >= spawnInterval)
                    {
                        Debug.Log($"[NPCQueueSystem] Update() - NPC 스폰 시도: currentSpawnIndex={currentSpawnIndex}");
                        SpawnNextNPC();
                        timer = 0;
                    }
                }
                else if (currentSpawnIndex >= maxSpawnCount)
                {
                    // 모든 NPC 스폰 완료
                    Debug.Log($"[NPCQueueSystem] Update() - 모든 NPC 스폰 완료: currentSpawnIndex={currentSpawnIndex}, maxSpawnCount={maxSpawnCount}");
                    isInitialSpawnComplete = true;
                }
            }
            else
            {
                Debug.LogWarning($"[NPCQueueSystem] Update() - maxSpawnCount가 0입니다. randomQueuePrefabs가 null이거나 비어있습니다. (Count: {randomQueuePrefabs?.Count ?? 0})");
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
    /// 다음 NPC를 스폰합니다. (랜덤 큐 순서대로 스폰)
    /// </summary>
    void SpawnNextNPC()
    {
        // 랜덤 큐 사용
        if (randomQueuePrefabs == null || randomQueuePrefabs.Count == 0)
            return;
        
        List<GameObject> prefabList = randomQueuePrefabs;
        int maxCount = randomQueuePrefabs.Count;

        // NPC 수를 초과하면 스폰하지 않음
        if (currentSpawnIndex >= maxCount)
            return;

        // 큐가 가득 찼으면 스폰하지 않음
        int maxQueueSize = queueSlots != null ? queueSlots.Length : 0;
        if (activeNPCs.Count >= maxQueueSize)
        {
            Debug.Log($"NPCQueueSystem: 큐가 가득 찼습니다. (현재: {activeNPCs.Count}/{maxQueueSize})");
            return;
        }

        GameObject prefabToSpawn = prefabList[currentSpawnIndex];
        if (prefabToSpawn == null)
        {
            Debug.LogWarning($"NPCQueueSystem: SpawnNextNPC - 프리팹이 null입니다. (인덱스: {currentSpawnIndex})");
            currentSpawnIndex++;
            return;
        }
        
        Debug.Log($"NPCQueueSystem: SpawnNextNPC - '{prefabToSpawn.name}' 스폰 중... (인덱스: {currentSpawnIndex}/{maxCount}, 큐: {activeNPCs.Count}/{maxQueueSize})");

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
                if (currentSpawnIndex < maxCount)
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
            Debug.Log($"[NPCQueueSystem] frontNPC 변경됨 - 이전: {(frontNPC != null ? frontNPC.name : "null")}, 새: {(newFrontNPC != null ? newFrontNPC.name : "null")}");
            frontNPC = newFrontNPC;
            frontNPCComponent = frontNPC != null ? frontNPC.GetComponent<NPCComponent>() : null;
            
            if (frontNPCComponent == null && frontNPC != null)
            {
                Debug.LogWarning($"[NPCQueueSystem] frontNPC '{frontNPC.name}'에 NPCComponent가 없습니다.");
            }
            
            // 도착했을 때만 대사 표시
            if (hasArrived && frontNPC != null && frontNPCComponent != null && frontNPCComponent.bohyunData != null)
            {
                // 무당 NPC인지 확인 (상호작용 완료 후 이벤트 트리거용)
                isCurrentNPCShaman = IsShamanNPC(frontNPCComponent.bohyunData);
                
                Debug.Log($"[NPCQueueSystem] 새 frontNPC 도착 및 대사 표시 - {frontNPC.name}, 신분: {frontNPCComponent.bohyunData.npcName}");
                ShowDialogue(frontNPCComponent.bohyunData);
            }
            else
            {
                if (!hasArrived)
                {
                    Debug.Log($"[NPCQueueSystem] 새 frontNPC 아직 도착하지 않음 - {frontNPC?.name}");
                }
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
    void ShowDialogue(BohyunNPCData bohyunData)
    {
        if (bohyunData == null) return;

        string dialogue = "";
        int refusalCount = 0;
        
        // 거절 횟수 확인 (고유 NPC 이름 사용 - 프리팹 이름 기반)
        if (NPCStateManager.Instance != null && frontNPC != null)
        {
            string uniqueNPCName = GetNPCName(frontNPC);
            refusalCount = NPCStateManager.Instance.GetRefusalCount(uniqueNPCName);
        }
        
        // 확률에 따라 요청 타입 결정
        bool requestedMedicine = DetermineNPCRequestType(bohyunData);
        currentNPCRequestedMedicine = requestedMedicine;
        
        // 요청 타입에 따라 대사 선택
        if (requestedMedicine)
        {
            // 한 번 거절 후 재요청 대사가 있으면 재요청 대사 표시
            if (refusalCount > 0 && bohyunData.medicineReRequestLines != null && bohyunData.medicineReRequestLines.Length > 0)
            {
                dialogue = bohyunData.medicineReRequestLines[Random.Range(0, bohyunData.medicineReRequestLines.Length)];
            }
            else if (bohyunData.medicineRequestLines != null && bohyunData.medicineRequestLines.Length > 0)
            {
                dialogue = bohyunData.medicineRequestLines[Random.Range(0, bohyunData.medicineRequestLines.Length)];
            }
            // NPC 상태 매니저에 약 요청 기록 (고유 NPC 이름 사용)
            if (NPCStateManager.Instance != null && frontNPC != null)
            {
                string uniqueNPCName = GetNPCName(frontNPC);
                NPCStateManager.Instance.RecordMedicineRequest(uniqueNPCName);
            }
        }
        else
        {
            // 한 번 거절 후 재요청 대사가 있으면 재요청 대사 표시
            if (refusalCount > 0 && bohyunData.foodReRequestLines != null && bohyunData.foodReRequestLines.Length > 0)
            {
                dialogue = bohyunData.foodReRequestLines[Random.Range(0, bohyunData.foodReRequestLines.Length)];
            }
            else if (bohyunData.foodRequestLines != null && bohyunData.foodRequestLines.Length > 0)
            {
                dialogue = bohyunData.foodRequestLines[Random.Range(0, bohyunData.foodRequestLines.Length)];
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
    /// 확률에 따라 NPC의 요청 타입을 결정합니다 (true = 약 요청, false = 밥 요청).
    /// </summary>
    bool DetermineNPCRequestType(BohyunNPCData bohyunData)
    {
        if (bohyunData == null) return false;
        
        float randomValue = Random.value;
        float totalProbability = bohyunData.foodRequestProbability + bohyunData.medicineRequestProbability;
        
        if (totalProbability <= 0f) return false; // 기본값: 밥 요청
        
        // 정규화된 확률로 결정
        float normalizedMedicineProb = bohyunData.medicineRequestProbability / totalProbability;
        return randomValue < normalizedMedicineProb;
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
        Debug.Log($"[NPCQueueSystem] RefuseFrontNPC() 호출됨 - activeNPCs.Count: {activeNPCs.Count}, frontNPC: {(frontNPC != null ? frontNPC.name : "null")}, isProcessingInteraction: {isProcessingInteraction}");
        
        if (activeNPCs.Count == 0)
        {
            Debug.LogWarning("[NPCQueueSystem] RefuseFrontNPC() 실패: activeNPCs가 비어있습니다.");
            return;
        }
        
        if (frontNPC == null)
        {
            Debug.LogWarning("[NPCQueueSystem] RefuseFrontNPC() 실패: frontNPC가 null입니다.");
            return;
        }
        
        // NPC가 목표 위치에 도착했는지 확인
        bool hasArrived = false;
        if (npcArrivedAtPosition.ContainsKey(frontNPC))
        {
            hasArrived = npcArrivedAtPosition[frontNPC];
        }
        
        if (!hasArrived)
        {
            Debug.LogWarning($"[NPCQueueSystem] RefuseFrontNPC() 실패: NPC가 아직 목표 위치에 도착하지 않았습니다. ({frontNPC.name})");
            return;
        }
        
        if (isProcessingInteraction)
        {
            Debug.LogWarning("[NPCQueueSystem] RefuseFrontNPC() 실패: 이미 상호작용 처리 중입니다.");
            return;
        }
        
        // 상호작용 시작
        isProcessingInteraction = true;
        Debug.Log("[NPCQueueSystem] RefuseFrontNPC() 상호작용 시작");

        BohyunNPCData bohyunData = frontNPCComponent != null ? frontNPCComponent.bohyunData : null;
        string npcName = GetNPCName(frontNPC); // 고유 NPC 이름
        bool requestedMedicine = false;
        
        // 거절 횟수 확인 (고유 NPC 이름 사용) - RecordRefusal 호출 전에 확인해야 함
        int refusalCount = 0;
        if (NPCStateManager.Instance != null)
        {
            refusalCount = NPCStateManager.Instance.GetRefusalCount(npcName);
        }
        
        Debug.Log($"[NPCQueueSystem] RefuseFrontNPC() - 현재 refusalCount: {refusalCount}");
        
        if (bohyunData != null)
        {
            requestedMedicine = currentNPCRequestedMedicine;
            
            // 재요청 가능 여부 확인 (re-accept나 re-reject 대사가 있으면 재요청 가능)
            bool hasReRequest = false;
            if (requestedMedicine)
            {
                hasReRequest = (bohyunData.medicineReAcceptLines != null && bohyunData.medicineReAcceptLines.Length > 0) ||
                               (bohyunData.medicineReRejectLines != null && bohyunData.medicineReRejectLines.Length > 0);
            }
            else
            {
                hasReRequest = (bohyunData.foodReAcceptLines != null && bohyunData.foodReAcceptLines.Length > 0) ||
                               (bohyunData.foodReRejectLines != null && bohyunData.foodReRejectLines.Length > 0);
            }
            
            // 재요청 가능하고 첫 거절이면, 랜덤으로 재요청 여부 결정
            // (약 요청 거절도 재요청 가능, 밤에 사망 처리됨)
            if (hasReRequest && refusalCount == 0)
            {
                // 랜덤으로 재요청할지 결정
                if (Random.value <= reRequestChance)
                {
                    // 상태 기록 (재요청하기 전에 기록)
                    if (NPCStateManager.Instance != null)
                    {
                        NPCStateManager.Instance.RecordRefusal(npcName, requestedMedicine);
                    }
                    
                    // 재요청하는 경우: 거절 대사 건너뛰고 바로 재요청 대사만 표시
                    // isProcessingInteraction은 ProcessRefusalAndReRequest에서 유지됨 (재요청 대사 표시 후에도 유지)
                    StartCoroutine(ProcessRefusalAndReRequest(frontNPC, skipRejectDialogue: true));
                    // NOLGAE HOOK — Re-Accept
                    if (npcName == "Nolgae")
                    {
                        NolgaeEffect.Instance.OnReAccept();
                    }

                    return; // 재요청 코루틴이 플래그를 관리하므로 여기서 리턴
                }
                else
                {
                    // 재요청하지 않는 경우: 상태 기록 후 거절 대사 표시 후 지나감
                    if (NPCStateManager.Instance != null)
                    {
                        NPCStateManager.Instance.RecordRefusal(npcName, requestedMedicine);
                    }
                    
                    string rejectDialogue = "";
                    if (requestedMedicine)
                    {
                        if (bohyunData.medicineRejectLines != null && bohyunData.medicineRejectLines.Length > 0)
                        {
                            rejectDialogue = bohyunData.medicineRejectLines[Random.Range(0, bohyunData.medicineRejectLines.Length)];
                        }
                    }
                    else
                    {
                        if (bohyunData.foodRejectLines != null && bohyunData.foodRejectLines.Length > 0)
                        {
                            rejectDialogue = bohyunData.foodRejectLines[Random.Range(0, bohyunData.foodRejectLines.Length)];
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(rejectDialogue))
                    {
                        ShowDialogueText(rejectDialogue);
                        // 타이핑 완료와 대사 표시 시간을 기다린 후 제거 시작
                        StartCoroutine(WaitForDialogueAndRemove(frontNPC));
                    }
                    else
                    {
                        // 대사가 없으면 바로 제거
                        Debug.Log($"[NPCQueueSystem] RefuseFrontNPC() ProcessInteractionAndRemove 시작 (재요청 없음, 대사 없음) - NPC: {(frontNPC != null ? frontNPC.name : "null")}");
                        StartCoroutine(ProcessInteractionAndRemove(frontNPC));
                    }
                    return;
                }
            }
            else
            {
                // 두 번째 거절이거나 재요청 불가능: 상태 기록 후 거절 대사 표시 후 제거
                if (NPCStateManager.Instance != null)
                {
                    NPCStateManager.Instance.RecordRefusal(npcName, requestedMedicine);
                    // RecordRefusal 호출 후 refusalCount가 증가했으므로 다시 확인
                    refusalCount = NPCStateManager.Instance.GetRefusalCount(npcName);
                }

                if (refusalCount >= 1 && npcName == "Nolgae")
                {
                    NolgaeEffect.Instance.OnReReject();
                }

                string rejectDialogue = "";
                if (requestedMedicine)
                {
                    // 두 번 거절당했을 때 (RecordRefusal 호출 후이므로 refusalCount >= 1)
                    if (refusalCount >= 1)
                    {
                        if (bohyunData.medicineReRejectLines != null && bohyunData.medicineReRejectLines.Length > 0)
                        {
                            rejectDialogue = bohyunData.medicineReRejectLines[Random.Range(0, bohyunData.medicineReRejectLines.Length)];
                        }
                        else
                        {
                            // re-reject 대사가 없으면 기본 메시지
                            rejectDialogue = "I'm gonna die tomorrow...";
                        }
                    }
                    // 첫 거절
                    else if (bohyunData.medicineRejectLines != null && bohyunData.medicineRejectLines.Length > 0)
                    {
                        rejectDialogue = bohyunData.medicineRejectLines[Random.Range(0, bohyunData.medicineRejectLines.Length)];
                    }
                }
                else
                {
                    // 두 번 거절당했을 때
                    if (refusalCount >= 1)
                    {
                        if (bohyunData.foodReRejectLines != null && bohyunData.foodReRejectLines.Length > 0)
                        {
                            rejectDialogue = bohyunData.foodReRejectLines[Random.Range(0, bohyunData.foodReRejectLines.Length)];
                        }
                        else
                        {
                            // re-reject 대사가 없으면 기본 메시지
                            rejectDialogue = "I'm gonna die tomorrow...";
                        }
                    }
                    // 첫 거절
                    else if (bohyunData.foodRejectLines != null && bohyunData.foodRejectLines.Length > 0)
                    {
                        rejectDialogue = bohyunData.foodRejectLines[Random.Range(0, bohyunData.foodRejectLines.Length)];
                    }
                }
                
                if (!string.IsNullOrEmpty(rejectDialogue))
                {
                    ShowDialogueText(rejectDialogue);
                    // 타이핑 완료와 대사 표시 시간을 기다린 후 제거 시작
                    StartCoroutine(WaitForDialogueAndRemove(frontNPC));
                }
                else
                {
                    // 대사가 없으면 바로 제거
                    Debug.Log($"[NPCQueueSystem] RefuseFrontNPC() ProcessInteractionAndRemove 시작 (재요청 없음, 대사 없음) - NPC: {(frontNPC != null ? frontNPC.name : "null")}");
                    StartCoroutine(ProcessInteractionAndRemove(frontNPC));
                }
            }
        }
        else
        {
            // NPC 데이터가 없으면 그냥 제거
            Debug.Log($"[NPCQueueSystem] RefuseFrontNPC() ProcessInteractionAndRemove 시작 (NPC 데이터 없음) - NPC: {(frontNPC != null ? frontNPC.name : "null")}");
            StartCoroutine(ProcessInteractionAndRemove(frontNPC));
        }
    }

    public void GiveLotusRice()
    {
        Debug.Log($"[NPCQueueSystem] GiveLotusRice() 호출됨 - activeNPCs.Count: {activeNPCs.Count}, frontNPC: {(frontNPC != null ? frontNPC.name : "null")}, isProcessingInteraction: {isProcessingInteraction}");
        
        if (activeNPCs.Count == 0)
        {
            Debug.LogWarning("[NPCQueueSystem] GiveLotusRice() 실패: activeNPCs가 비어있습니다.");
            return;
        }
        
        if (frontNPC == null)
        {
            Debug.LogWarning("[NPCQueueSystem] GiveLotusRice() 실패: frontNPC가 null입니다.");
            return;
        }
        
        // NPC가 목표 위치에 도착했는지 확인
        bool hasArrived = false;
        if (npcArrivedAtPosition.ContainsKey(frontNPC))
        {
            hasArrived = npcArrivedAtPosition[frontNPC];
        }
        
        if (!hasArrived)
        {
            Debug.LogWarning($"[NPCQueueSystem] GiveLotusRice() 실패: NPC가 아직 목표 위치에 도착하지 않았습니다. ({frontNPC.name})");
            return;
        }
        
        if (isProcessingInteraction)
        {
            Debug.LogWarning("[NPCQueueSystem] GiveLotusRice() 실패: 이미 상호작용 처리 중입니다.");
            return;
        }
        
        // Inventory 확인
        if (inventory != null && inventory.lotusRice <= 0)
        {
            Debug.LogWarning("[NPCQueueSystem] GiveLotusRice() 실패: 연밥이 부족합니다.");
            ShowDialogueText("연밥이 부족합니다.");
            return;
        }
        
        // 상호작용 시작
        isProcessingInteraction = true;
        Debug.Log("[NPCQueueSystem] GiveLotusRice() 상호작용 시작");

        BohyunNPCData bohyunData = frontNPCComponent != null ? frontNPCComponent.bohyunData : null;
        string npcName = GetNPCName(frontNPC);
        
        // 무당 NPC인지 확인 (상호작용 완료 후 이벤트 트리거용)
        isCurrentNPCShaman = IsShamanNPC(bohyunData);
        
        // 거절 횟수 확인
        int refusalCount = 0;
        if (NPCStateManager.Instance != null)
        {
            refusalCount = NPCStateManager.Instance.GetRefusalCount(npcName);
        }
        
        string dialogue = "";
        bool isAccept = false;
        
        if (bohyunData != null)
        {
            // NPC가 요청한 것과 일치하는지 확인
            if (!currentNPCRequestedMedicine) // 밥 요청
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
                    if (bohyunData.foodReAcceptLines != null && bohyunData.foodReAcceptLines.Length > 0)
                    {
                        dialogue = bohyunData.foodReAcceptLines[Random.Range(0, bohyunData.foodReAcceptLines.Length)];
                    }
                    else
                    {
                        // re-accept 대사가 없으면 기본 메시지
                        dialogue = "Thank you... Thank you...";
                    }
                }
                else if (bohyunData.foodAcceptLines != null && bohyunData.foodAcceptLines.Length > 0)
                {
                    dialogue = bohyunData.foodAcceptLines[Random.Range(0, bohyunData.foodAcceptLines.Length)];
                }
            }
            else // 약 요청
            {
                // 약을 요청했는데 밥을 줌 = 거절로 간주 (밤에 사망 처리됨)
                isAccept = false;
                if (NPCStateManager.Instance != null)
                {
                    NPCStateManager.Instance.RecordRefusal(npcName, true);
                }
                
                // 거절 대사 표시
                if (refusalCount >= 1 && bohyunData.medicineReRejectLines != null && bohyunData.medicineReRejectLines.Length > 0)
                {
                    // 두 번 거절당했을 때
                    dialogue = bohyunData.medicineReRejectLines[Random.Range(0, bohyunData.medicineReRejectLines.Length)];
                }
                else if (bohyunData.medicineRejectLines != null && bohyunData.medicineRejectLines.Length > 0)
                {
                    // 첫 거절
                    dialogue = bohyunData.medicineRejectLines[Random.Range(0, bohyunData.medicineRejectLines.Length)];
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

        // Inventory 소모 (실제로 NPC에게 줄 때만, isAccept이 true일 때만)
        if (isAccept && inventory != null)
        {
            inventory.UseLotusRice();
            Debug.Log("[NPCQueueSystem] GiveLotusRice() - 연밥 소모됨");
        }

        // 무당 이벤트 트리거 (상호작용 완료 후)
        TriggerShamanEvent();
        
        // 무당 플래그 리셋 (다음 NPC를 위해)
        isCurrentNPCShaman = false;
        
        // 대사 표시 후 페이드아웃 및 제거
        Debug.Log($"[NPCQueueSystem] GiveLotusRice() ProcessInteractionAndRemove 시작 - NPC: {(frontNPC != null ? frontNPC.name : "null")}");
        StartCoroutine(ProcessInteractionAndRemove(frontNPC));
    }

    public void GiveHerbalMedicine()
    {
        Debug.Log($"[NPCQueueSystem] GiveHerbalMedicine() 호출됨 - activeNPCs.Count: {activeNPCs.Count}, frontNPC: {(frontNPC != null ? frontNPC.name : "null")}, isProcessingInteraction: {isProcessingInteraction}");
        
        if (activeNPCs.Count == 0)
        {
            Debug.LogWarning("[NPCQueueSystem] GiveHerbalMedicine() 실패: activeNPCs가 비어있습니다.");
            return;
        }
        
        if (frontNPC == null)
        {
            Debug.LogWarning("[NPCQueueSystem] GiveHerbalMedicine() 실패: frontNPC가 null입니다.");
            return;
        }
        
        // NPC가 목표 위치에 도착했는지 확인
        bool hasArrived = false;
        if (npcArrivedAtPosition.ContainsKey(frontNPC))
        {
            hasArrived = npcArrivedAtPosition[frontNPC];
        }
        
        if (!hasArrived)
        {
            Debug.LogWarning($"[NPCQueueSystem] GiveHerbalMedicine() 실패: NPC가 아직 목표 위치에 도착하지 않았습니다. ({frontNPC.name})");
            return;
        }
        
        if (isProcessingInteraction)
        {
            Debug.LogWarning("[NPCQueueSystem] GiveHerbalMedicine() 실패: 이미 상호작용 처리 중입니다.");
            return;
        }
        
        // Inventory 확인
        if (inventory != null && inventory.herbalMedicine <= 0)
        {
            Debug.LogWarning("[NPCQueueSystem] GiveHerbalMedicine() 실패: 약초가 부족합니다.");
            ShowDialogueText("약초가 부족합니다.");
            return;
        }
        
        // 상호작용 시작
        isProcessingInteraction = true;
        Debug.Log("[NPCQueueSystem] GiveHerbalMedicine() 상호작용 시작");

        BohyunNPCData bohyunData = frontNPCComponent != null ? frontNPCComponent.bohyunData : null;
        string npcName = GetNPCName(frontNPC);
        
        // 무당 NPC인지 확인 (상호작용 완료 후 이벤트 트리거용)
        isCurrentNPCShaman = IsShamanNPC(bohyunData);
        
        // 거절 횟수 확인
        int refusalCount = 0;
        if (NPCStateManager.Instance != null)
        {
            refusalCount = NPCStateManager.Instance.GetRefusalCount(npcName);
        }
        
        string dialogue = "";
        bool isAccept = false;
        
        if (bohyunData != null)
        {
            // NPC가 요청한 것과 일치하는지 확인
            if (currentNPCRequestedMedicine) // 약 요청
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
                    if (bohyunData.medicineReAcceptLines != null && bohyunData.medicineReAcceptLines.Length > 0)
                    {
                        dialogue = bohyunData.medicineReAcceptLines[Random.Range(0, bohyunData.medicineReAcceptLines.Length)];
                    }
                    else
                    {
                        // re-accept 대사가 없으면 기본 메시지
                        dialogue = "Thank you... Thank you...";
                    }
                }
                else if (bohyunData.medicineAcceptLines != null && bohyunData.medicineAcceptLines.Length > 0)
                {
                    dialogue = bohyunData.medicineAcceptLines[Random.Range(0, bohyunData.medicineAcceptLines.Length)];
                }
            }
            else // 밥 요청
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
                    if (bohyunData.foodReRejectLines != null && bohyunData.foodReRejectLines.Length > 0)
                    {
                        dialogue = bohyunData.foodReRejectLines[Random.Range(0, bohyunData.foodReRejectLines.Length)];
                    }
                    else
                    {
                        // re-reject 대사가 없으면 기본 메시지
                        dialogue = "I'm gonna die tomorrow...";
                    }
                }
                else if (bohyunData.foodRejectLines != null && bohyunData.foodRejectLines.Length > 0)
                {
                    dialogue = bohyunData.foodRejectLines[Random.Range(0, bohyunData.foodRejectLines.Length)];
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

        // Inventory 소모 (실제로 NPC에게 줄 때만, isAccept이 true일 때만)
        if (isAccept && inventory != null)
        {
            inventory.UseHerbalMedicine();
            Debug.Log("[NPCQueueSystem] GiveHerbalMedicine() - 약초 소모됨");
        }

        // 무당 이벤트 트리거 (상호작용 완료 후)
        TriggerShamanEvent();
        
        // 무당 플래그 리셋 (다음 NPC를 위해)
        isCurrentNPCShaman = false;
        
        // 대사 표시 후 페이드아웃 및 제거
        Debug.Log($"[NPCQueueSystem] GiveHerbalMedicine() ProcessInteractionAndRemove 시작 - NPC: {(frontNPC != null ? frontNPC.name : "null")}");
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
        Debug.Log($"[NPCQueueSystem] ProcessRefusalAndReRequest() 시작 - NPC: {(npc != null ? npc.name : "null")}, skipRejectDialogue: {skipRejectDialogue}");
        
        // 거절 대사를 건너뛰지 않는 경우에만 대기
        if (!skipRejectDialogue)
        {
            // 거절 대사 표시 시간 대기
            yield return new WaitForSeconds(dialogueDisplayDuration);
        }
        
        // 재요청 대사 표시 (이미 RefuseFrontNPC()에서 재요청 확률로 결정되었으므로 무조건 재요청 대사 표시)
        BohyunNPCData bohyunData = frontNPCComponent != null ? frontNPCComponent.bohyunData : null;
        if (bohyunData != null)
        {
            string reRequestDialogue = "";
            if (currentNPCRequestedMedicine) // 약 요청
            {
                if (bohyunData.medicineReRequestLines != null && bohyunData.medicineReRequestLines.Length > 0)
                {
                    reRequestDialogue = bohyunData.medicineReRequestLines[Random.Range(0, bohyunData.medicineReRequestLines.Length)];
                }
            }
            else 
            {
                if (bohyunData.foodReRequestLines != null && bohyunData.foodReRequestLines.Length > 0)
                {
                    reRequestDialogue = bohyunData.foodReRequestLines[Random.Range(0, bohyunData.foodReRequestLines.Length)];
                }
            }
            
            if (!string.IsNullOrEmpty(reRequestDialogue))
            {
                ShowDialogueText(reRequestDialogue);
                Debug.Log($"[NPCQueueSystem] ProcessRefusalAndReRequest() 재요청 대사 표시: {reRequestDialogue}");
                
                // 타이핑이 완료될 때까지 대기
                // 재요청 대사 표시 후 바로 플래그 리셋 (버튼 클릭 가능하도록)
                // 타이핑은 백그라운드에서 계속 진행되지만, 상호작용은 즉시 가능해야 함
                isProcessingInteraction = false;
                Debug.Log("[NPCQueueSystem] ProcessRefusalAndReRequest() 재요청 대사 표시 완료, isProcessingInteraction 리셋됨 (즉시 상호작용 가능)");
            }
            else
            {
                Debug.LogWarning("[NPCQueueSystem] ProcessRefusalAndReRequest() 재요청 대사가 없습니다.");
                // 재요청 대사가 없으면 바로 플래그 리셋
                isProcessingInteraction = false;
            }
        }
        else
        {
            // NPC 데이터가 없으면 바로 플래그 리셋
            isProcessingInteraction = false;
        }
        
        // 무당 이벤트 트리거 (상호작용 완료 후)
        TriggerShamanEvent();
        
        Debug.Log("[NPCQueueSystem] ProcessRefusalAndReRequest() 완료 - 재요청 대사 표시 완료, NPC는 큐에 남아있음");
    }
    
    /// <summary>
    /// 대사 타이핑 완료와 표시 시간을 기다린 후 NPC를 제거합니다.
    /// </summary>
    System.Collections.IEnumerator WaitForDialogueAndRemove(GameObject npcToRemove)
    {
        Debug.Log($"[NPCQueueSystem] WaitForDialogueAndRemove() 시작 - NPC: {(npcToRemove != null ? npcToRemove.name : "null")}");
        
        // 타이핑이 완료될 때까지 대기
        float typingWaitTime = 0f;
        float maxTypingWaitTime = 10f; // 최대 10초 대기 (무한 대기 방지)
        while (!isTypingComplete && typingWaitTime < maxTypingWaitTime)
        {
            typingWaitTime += Time.deltaTime;
            yield return null;
        }
        
        if (typingWaitTime >= maxTypingWaitTime)
        {
            Debug.LogWarning("[NPCQueueSystem] WaitForDialogueAndRemove() 타이핑 완료 대기 시간 초과. 강제 진행합니다.");
            isTypingComplete = true;
        }
        
        // 대사 표시 시간 대기
        yield return new WaitForSeconds(dialogueDisplayDuration);
        
        Debug.Log($"[NPCQueueSystem] WaitForDialogueAndRemove() 대사 표시 완료, ProcessInteractionAndRemove 시작 - NPC: {(npcToRemove != null ? npcToRemove.name : "null")}");
        
        // 이제 NPC 제거 시작
        StartCoroutine(ProcessInteractionAndRemove(npcToRemove));
    }

    /// <summary>
    /// 상호작용을 처리하고 NPC를 제거합니다. (대사 표시 → 페이드아웃 → 제거 → 다음 NPC 이동)
    /// </summary>
    System.Collections.IEnumerator ProcessInteractionAndRemove(GameObject npcToRemove)
    {
        Debug.Log($"[NPCQueueSystem] ProcessInteractionAndRemove() 시작 - NPC: {(npcToRemove != null ? npcToRemove.name : "null")}");
        
        // 타이핑이 완료될 때까지 대기
        float typingWaitTime = 0f;
        float maxTypingWaitTime = 10f; // 최대 10초 대기 (무한 대기 방지)
        while (!isTypingComplete && typingWaitTime < maxTypingWaitTime)
        {
            typingWaitTime += Time.deltaTime;
            yield return null;
        }
        
        if (typingWaitTime >= maxTypingWaitTime)
        {
            Debug.LogWarning("[NPCQueueSystem] ProcessInteractionAndRemove() 타이핑 완료 대기 시간 초과. 강제 진행합니다.");
            isTypingComplete = true;
        }
        
        // 대사 표시 시간 대기
        yield return new WaitForSeconds(dialogueDisplayDuration);
        
        // center에 있는 NPC (activeNPCs[0]) 확인 및 제거
        if (activeNPCs.Count == 0)
        {
            Debug.LogWarning("[NPCQueueSystem] ProcessInteractionAndRemove() activeNPCs가 비어있습니다.");
            isProcessingInteraction = false;
            Debug.Log("[NPCQueueSystem] ProcessInteractionAndRemove() isProcessingInteraction 리셋됨 (activeNPCs 비어있음)");
            yield break;
        }

        // activeNPCs[0]이 제거할 NPC인지 확인
        GameObject centerNPC = activeNPCs[0];
        if (centerNPC == null || centerNPC != npcToRemove)
        {
            Debug.LogWarning($"[NPCQueueSystem] ProcessInteractionAndRemove() center NPC가 일치하지 않습니다. center={(centerNPC != null ? centerNPC.name : "null")}, toRemove={(npcToRemove != null ? npcToRemove.name : "null")}");
            isProcessingInteraction = false;
            Debug.Log("[NPCQueueSystem] ProcessInteractionAndRemove() isProcessingInteraction 리셋됨 (NPC 불일치)");
            yield break;
        }

        Debug.Log($"[NPCQueueSystem] ProcessInteractionAndRemove() NPC 제거 시작 - {npcToRemove.name}");

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
        
        // 무당 플래그 리셋 (다음 NPC를 위해)
        isCurrentNPCShaman = false;
        
        // frontNPC 초기화
        frontNPC = null;
        frontNPCComponent = null;
        
        // 상호작용 처리 완료
        isProcessingInteraction = false;
        Debug.Log("[NPCQueueSystem] ProcessInteractionAndRemove() 완료 및 isProcessingInteraction 리셋됨");
        
        // 모든 상호작용이 끝났는지 확인
        CheckAndTransitionToNighttime();
        
        // 다음 NPC들이 앞으로 이동 (UpdateNPCPositions가 자동으로 처리)
        // 끝에 새 NPC 생성 (Update에서 자동으로 처리)
    }
    
    
    [Tooltip("페이드아웃 오버레이 (자동 생성 가능)")]
    public Image fadeOverlay;
    
    [Tooltip("씬 전환 페이드아웃 시간 (초)")]
    public float sceneTransitionFadeDuration = 1f;
    
    [Tooltip("페이드아웃 색상")]
    public Color fadeColor = Color.black;
    
    private bool isTransitioning = false; // 전환 중인지 여부
    
    /// <summary>
    /// 모든 생존자와 상호작용이 끝났는지 확인하고 Nighttime으로 전환합니다.
    /// </summary>
    void CheckAndTransitionToNighttime()
    {
        // 이미 전환 중이면 무시
        if (isTransitioning) return;
        
        // 상호작용 처리 중이면 무시
        if (isProcessingInteraction) return;
        
        // 큐에 NPC가 없고, 모든 NPC가 스폰되었는지 확인
        if (randomQueuePrefabs == null || randomQueuePrefabs.Count == 0)
        {
            return;
        }
        
        // 생존자 수 계산
        int totalSurvivors = GetTotalSurvivorCount();
        int remainingSurvivors = GetRemainingSurvivorCount();
        
        Debug.Log($"[NPCQueueSystem] CheckAndTransitionToNighttime() - totalSurvivors: {totalSurvivors}, remainingSurvivors: {remainingSurvivors}, queueEmpty: {activeNPCs.Count == 0}, notProcessing: {!isProcessingInteraction}");
        
        // 모든 생존자와 상호작용 완료 (남은 생존자가 0명이고 큐가 비어있음)
        if (totalSurvivors > 0 && remainingSurvivors == 0 && activeNPCs.Count == 0)
        {
            // 모든 생존자와 상호작용 완료 - Nighttime으로 전환
            Debug.Log("[NPCQueueSystem] 모든 생존자와 상호작용이 완료되었습니다. Nighttime으로 전환합니다.");
            isTransitioning = true;
            StartCoroutine(TransitionToNighttime());
        }
    }
    
    /// <summary>
    /// 전체 생존자 수를 계산합니다 (죽지 않은 NPC 수).
    /// </summary>
    int GetTotalSurvivorCount()
    {
        if (NPCStateManager.Instance == null) return 0;
        
        List<string> allNPCNames = NPCStateManager.Instance.GetAllNPCNames();
        if (allNPCNames == null || allNPCNames.Count == 0) return 0;
        
        int survivorCount = 0;
        foreach (string npcName in allNPCNames)
        {
            if (string.IsNullOrEmpty(npcName)) continue;
            if (!NPCStateManager.Instance.IsDead(npcName))
            {
                survivorCount++;
            }
        }
        
        return survivorCount;
    }
    
    /// <summary>
    /// 아직 상호작용하지 않은 생존자 수를 계산합니다.
    /// </summary>
    int GetRemainingSurvivorCount()
    {
        if (NPCStateManager.Instance == null) return 0;
        
        List<string> allNPCNames = NPCStateManager.Instance.GetAllNPCNames();
        if (allNPCNames == null || allNPCNames.Count == 0) return 0;
        
        int remainingCount = 0;
        foreach (string npcName in allNPCNames)
        {
            if (string.IsNullOrEmpty(npcName)) continue;
            
            // 죽은 NPC는 제외
            if (NPCStateManager.Instance.IsDead(npcName)) continue;
            
            // 상호작용 완료 조건:
            // 1. 밥을 받았거나
            // 2. 약을 받았거나
            // 3. 거절당했거나 (refusalCount > 0이면 상호작용 완료)
            bool receivedFood = NPCStateManager.Instance.ReceivedFoodToday(npcName);
            bool receivedMedicine = NPCStateManager.Instance.ReceivedMedicineToday(npcName);
            int refusalCount = NPCStateManager.Instance.GetRefusalCount(npcName);
            
            // 상호작용 완료 여부 확인
            bool hasInteracted = receivedFood || receivedMedicine || refusalCount > 0;
            
            if (!hasInteracted)
            {
                remainingCount++;
            }
        }
        
        return remainingCount;
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

    IEnumerator TransitionToNighttime()
    {
        Debug.Log("[NPCQueueSystem] TransitionToNighttime() - Nighttime UI 표시 (카메라 전환)");
        
        // NighttimeUIManager 찾기 (Inspector에서 할당되었으면 사용, 아니면 자동으로 찾기)
        NighttimeUIManager nighttimeUI = nighttimeUIManager;
        
        if (nighttimeUI == null)
        {
            // Inspector에서 할당되지 않았으면 GameObject 이름으로 찾기
            GameObject nighttimeUIGameObject = GameObject.Find("NighttimeUIManager");
            if (nighttimeUIGameObject != null)
            {
                nighttimeUI = nighttimeUIGameObject.GetComponent<NighttimeUIManager>();
            }
        }
        
        if (nighttimeUI == null)
        {
            // GameObject 이름으로도 못 찾았으면 컴포넌트 타입으로 찾기 (최후의 수단)
            nighttimeUI = FindFirstObjectByType<NighttimeUIManager>();
        }
        
        // Day BGM 중지
        if (nighttimeUI != null && nighttimeUI.dayBGMAudioSource != null)
        {
            nighttimeUI.dayBGMAudioSource.Stop();
            Debug.Log("[NPCQueueSystem] Day BGM 중지됨");
        }
        
        if (nighttimeUI != null)
        {
            nighttimeUI.ShowNighttimeUI();
            Debug.Log("[NPCQueueSystem] NighttimeUIManager 활성화 및 UI 표시");
        }
        else
        {
            Debug.LogWarning("[NPCQueueSystem] NighttimeUIManager를 찾을 수 없습니다. 씬에 'NighttimeUIManager'라는 이름의 GameObject가 있는지 확인하세요.");
            isTransitioning = false; 
        }
        
        yield return null;
    }

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
            
            if (!typingAudioSource.isPlaying)
            {
                Debug.LogWarning($"NPCQueueSystem: 타이핑 소리 재생 실패. AudioSource.isPlaying = {typingAudioSource.isPlaying}, Volume: {typingAudioSource.volume}, Clip: {typingAudioSource.clip?.name ?? "null"}");
            }
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
    /// NPC 이름을 가져옵니다. (고유 ID로 사용 - 프리팹 이름만 사용)
    /// 프리팹 이름이 이미 고유하므로 (예: "Shaman 1", "Slave 2") 그대로 사용합니다.
    /// </summary>
    string GetNPCName(GameObject npc)
    {
        if (npc == null) return "Unknown";
        
        // 프리팹 이름을 고유 ID로 사용 (예: "Shaman 1", "Slave 2")
        // "(Clone)" 제거하고 공백 정리
        string prefabName = npc.name.Replace("(Clone)", "").Trim();
        
        return prefabName;
    }
    
    /// <summary>
    /// NPC 신분을 가져옵니다 (대사용).
    /// </summary>
    string GetNPCStatus(GameObject npc)
    {
        if (npc == null) return "";
        
        NPCComponent component = npc.GetComponent<NPCComponent>();
        if (component != null && component.bohyunData != null)
        {
            return component.bohyunData.npcName;
        }
        
        // 프리팹 이름에서 신분 추출
        string prefabName = npc.name.Replace("(Clone)", "").Trim();
        if (prefabName.StartsWith("King", System.StringComparison.OrdinalIgnoreCase)) return "King";
        if (prefabName.StartsWith("Yangban", System.StringComparison.OrdinalIgnoreCase)) return "Yangban";
        if (prefabName.StartsWith("Physician", System.StringComparison.OrdinalIgnoreCase)) return "Physician";
        if (prefabName.StartsWith("Merchant", System.StringComparison.OrdinalIgnoreCase)) return "Merchant";
        if (prefabName.StartsWith("Slave", System.StringComparison.OrdinalIgnoreCase)) return "Slave";
        if (prefabName.StartsWith("Shaman", System.StringComparison.OrdinalIgnoreCase)) return "Shaman";
        
        return "";
    }

    /// <summary>
    /// NPC가 무당인지 확인합니다.
    /// </summary>
    bool IsShamanNPC(BohyunNPCData bohyunData)
    {
        if (bohyunData == null) return false;
        return bohyunData.npcName != null && bohyunData.npcName.StartsWith("Shaman", System.StringComparison.OrdinalIgnoreCase);
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
    /// 하루를 시작합니다 (랜덤 큐 사용).
    /// </summary>
    public void StartDay()
    {
        StartDayWithRandomQueue();
    }
    
    /// <summary>
    /// 랜덤 큐로 하루를 시작합니다 (NPCRandomQueueManager 사용).
    /// </summary>
    public void StartDayWithRandomQueue()
    {
        // 기존 NPC들 모두 제거
        ClearAllNPCs();
        
        // NPCRandomQueueManager에서 새로운 큐 생성
        if (NPCRandomQueueManager.Instance != null)
        {
            randomQueuePrefabs = NPCRandomQueueManager.Instance.GenerateDayQueue();
            
            // NPCStateManager에 오늘 등장할 모든 NPC 이름 목록 설정
            if (randomQueuePrefabs != null && randomQueuePrefabs.Count > 0 && NPCStateManager.Instance != null)
            {
                List<string> npcNames = new List<string>();
                foreach (GameObject prefab in randomQueuePrefabs)
                {
                    if (prefab != null)
                    {
                        // 고유 ID 가져오기 (프리팹 이름 사용)
                        string npcUniqueName = GetNPCName(prefab);
                        if (!string.IsNullOrEmpty(npcUniqueName))
                        {
                            npcNames.Add(npcUniqueName);
                            Debug.Log($"NPCQueueSystem: OnSceneLoaded - NPC 이름 추가 - {npcUniqueName}");
                        }
                    }
                }
                Debug.Log($"NPCQueueSystem: OnSceneLoaded - NPC 이름 목록 설정 - 총 {npcNames.Count}개");
                NPCStateManager.Instance.SetAllNPCNames(npcNames);
            }
        }
        
        currentSpawnIndex = 0;
        timer = 0f;
        isInitialSpawnComplete = false;
        frontNPC = null;
        frontNPCComponent = null;
        
        // 말풍선 숨기기
        HideSpeechBubble();
        
        // 첫 NPC 스폰
        if (randomQueuePrefabs != null && randomQueuePrefabs.Count > 0)
        {
            SpawnNextNPC();
        }
        else
        {
            isInitialSpawnComplete = true;
        }
    }

    /// <summary>
    /// 현재 하루를 리셋합니다 (랜덤 큐로 다시 시작).
    /// </summary>
    public void ResetDay()
    {
        StartDayWithRandomQueue();
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
    
    /// <summary>
    /// 전체 NPC 수를 반환합니다 (랜덤 큐의 총 NPC 수).
    /// </summary>
    public int GetTotalNPCCount()
    {
        if (randomQueuePrefabs != null)
        {
            return randomQueuePrefabs.Count;
        }
        return 0;
    }
}

