using UnityEngine;

public class GridGenerator : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridWidth = 4;
    public int gridHeight = 4;
    public float tileSpacing = 1.2f;
    
    [Header("Tile Prefabs")]
    public GameObject straightTilePrefab;
    public GameObject cornerTilePrefab;
    public GameObject deadEndTilePrefab;
    public GameObject startTilePrefab;
    public GameObject loseTilePrefab;
    public GameObject clearTilePrefab;
    
    [Header("Other Prefabs")]
    public GameObject humanPrefab;
    
    private GameManager gameManager;
    private GameObject currentHuman;
    
    void Awake()
    {
        gameManager = FindObjectOfType<GameManager>();
    }
    
    public void GenerateGrid(StageData stageData)
    {
        if (gameManager == null)
        {
            Debug.LogError("GameManager not found!");
            return;
        }
        
        // 기존 타일 및 인간 제거
        ClearGrid();
        
        // 타일 그리드 생성
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                TileType tileType = stageData.tileLayout[x, y];
                GameObject tilePrefab = GetTilePrefab(tileType);
                
                if (tilePrefab != null)
                {
                    Vector3 position = new Vector3(x * tileSpacing, -y * tileSpacing, 0);
                    GameObject tileObj = Instantiate(tilePrefab, position, Quaternion.identity, transform);
                    
                    TileController tileController = tileObj.GetComponent<TileController>();
                    if (tileController != null)
                    {
                        tileController.tileType = tileType;
                        gameManager.tileGrid[x, y] = tileController;
                    }
                }
            }
        }
        
        // 인간 캐릭터 생성 (기존 인간이 있으면 제거)
        if (currentHuman != null)
        {
            Destroy(currentHuman);
            currentHuman = null;
        }
        
        if (humanPrefab != null)
        {
            Vector3 humanPosition = new Vector3(
                stageData.startPosition.x * tileSpacing,
                -stageData.startPosition.y * tileSpacing,
                -1
            );
            GameObject humanObj = Instantiate(humanPrefab, humanPosition, Quaternion.identity);
            HumanController humanController = humanObj.GetComponent<HumanController>();
            if (humanController != null && gameManager != null)
            {
                gameManager.humanController = humanController;
                currentHuman = humanObj;
            }
        }
    }
    
    private GameObject GetTilePrefab(TileType tileType)
    {
        switch (tileType)
        {
            case TileType.Straight:
                return straightTilePrefab;
            case TileType.Corner:
                return cornerTilePrefab;
            case TileType.DeadEnd:
                return deadEndTilePrefab;
            case TileType.Start:
                return startTilePrefab;
            case TileType.Lose:
                return loseTilePrefab;
            case TileType.Clear:
                return clearTilePrefab;
            default:
                return null;
        }
    }
    
    public void ClearGrid()
    {
        // 자식 오브젝트 제거 (런타임과 에디터 모두 지원)
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }
            else
            {
                Destroy(transform.GetChild(i).gameObject);
            }
            #else
            Destroy(transform.GetChild(i).gameObject);
            #endif
        }
        
        // 기존 인간 제거
        if (currentHuman != null)
        {
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(currentHuman);
            }
            else
            {
                Destroy(currentHuman);
            }
            #else
            Destroy(currentHuman);
            #endif
            currentHuman = null;
        }
        
        // GameManager의 그리드 초기화
        if (gameManager != null)
        {
            gameManager.tileGrid = new TileController[4, 4];
            gameManager.humanController = null;
        }
    }
}

