using UnityEngine;
using UnityEngine.EventSystems;

public class TileController : MonoBehaviour, IPointerClickHandler
{
    [Header("Tile Settings")]
    public TileType tileType;
    [SerializeField] private int rotation = 0; // 0, 1, 2, 3 (0도, 90도, 180도, 270도)
    
    [Header("Debug")]
    public bool showConnections = false; // 연결 방향 시각화 (에디터/디버그용)
    
    private GameManager gameManager;
    private SpriteRenderer spriteRenderer;
    
    // 타일의 연결 방향 (상, 우, 하, 좌)
    private bool[] connections = new bool[4];
    
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        gameManager = FindObjectOfType<GameManager>();
    }
    
    void Start()
    {
        UpdateConnections();
        UpdateRotation();
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (gameManager != null && gameManager.CurrentPhase == GamePhase.Preparation)
        {
            if (CanRotate())
            {
                RotateTile();
            }
        }
    }
    
    public bool CanRotate()
    {
        // 시작 타일, 목표 타일, 막힌 길은 회전 불가
        return tileType != TileType.Start && 
               tileType != TileType.Lose && 
               tileType != TileType.Clear && 
               tileType != TileType.DeadEnd;
    }
    
    public void RotateTile()
    {
        if (!CanRotate()) return;
        
        rotation = (rotation + 1) % 4;
        UpdateRotation();
        UpdateConnections();
        
        if (gameManager != null)
        {
            gameManager.OnTileRotated();
        }
    }
    
    private void UpdateRotation()
    {
        transform.rotation = Quaternion.Euler(0, 0, -rotation * 90);
    }
    
    private void UpdateConnections()
    {
        // 타일 타입에 따라 기본 연결 설정
        switch (tileType)
        {
            case TileType.Straight:
                // 직선: 상하 또는 좌우
                if (rotation % 2 == 0)
                {
                    connections[0] = true; // 상
                    connections[2] = true; // 하
                    connections[1] = false; // 우
                    connections[3] = false; // 좌
                }
                else
                {
                    connections[0] = false; // 상
                    connections[2] = false; // 하
                    connections[1] = true; // 우
                    connections[3] = true; // 좌
                }
                break;
                
            case TileType.Corner:
                // 코너: 회전에 따라 연결 방향 변경
                connections[0] = (rotation == 0 || rotation == 3); // 상
                connections[1] = (rotation == 0 || rotation == 1); // 우
                connections[2] = (rotation == 1 || rotation == 2); // 하
                connections[3] = (rotation == 2 || rotation == 3); // 좌
                break;
                
            case TileType.DeadEnd:
                // 막힌 길: 한 방향만 연결
                connections[0] = (rotation == 0);
                connections[1] = (rotation == 1);
                connections[2] = (rotation == 2);
                connections[3] = (rotation == 3);
                break;
                
            case TileType.Start:
                // 시작 타일: 모든 방향 연결 (인간이 시작 지점에서 이동 시작)
                connections[0] = true;
                connections[1] = true;
                connections[2] = true;
                connections[3] = true;
                break;
                
            case TileType.Lose:
            case TileType.Clear:
                // 목표 타일: 모든 방향 연결 (인간이 도착 가능)
                connections[0] = true;
                connections[1] = true;
                connections[2] = true;
                connections[3] = true;
                break;
        }
    }
    
    public bool HasConnection(Direction dir)
    {
        int dirIndex = (int)dir;
        return connections[dirIndex];
    }
    
    // 인접한 타일과 연결되어 있는지 확인하는 메서드
    public bool IsConnectedTo(TileController otherTile, Direction dir)
    {
        if (otherTile == null) return false;
        
        // 현재 타일이 dir 방향으로 연결되어 있고
        // 다른 타일이 반대 방향으로 연결되어 있어야 함
        Direction oppositeDir = GetOppositeDirection(dir);
        return HasConnection(dir) && otherTile.HasConnection(oppositeDir);
    }
    
    private Direction GetOppositeDirection(Direction dir)
    {
        switch (dir)
        {
            case Direction.Up: return Direction.Down;
            case Direction.Right: return Direction.Left;
            case Direction.Down: return Direction.Up;
            case Direction.Left: return Direction.Right;
            default: return Direction.Up;
        }
    }
    
    public void ResetRotation()
    {
        rotation = 0;
        UpdateRotation();
        UpdateConnections();
    }
    
    public int GetRotation()
    {
        return rotation;
    }
    
    // 디버그용: 연결 방향 시각화 (Scene 뷰에서만 보임)
    void OnDrawGizmos()
    {
        if (!showConnections) return;
        
        float lineLength = 0.3f;
        Vector3 center = transform.position;
        
        // 각 방향의 연결 상태를 선으로 표시
        Gizmos.color = Color.green;
        
        if (connections[0]) // 상
            Gizmos.DrawLine(center, center + Vector3.up * lineLength);
        if (connections[1]) // 우
            Gizmos.DrawLine(center, center + Vector3.right * lineLength);
        if (connections[2]) // 하
            Gizmos.DrawLine(center, center + Vector3.down * lineLength);
        if (connections[3]) // 좌
            Gizmos.DrawLine(center, center + Vector3.left * lineLength);
    }
}

