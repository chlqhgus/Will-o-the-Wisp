using UnityEngine;
using System.Collections;

public class HumanController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float moveDelay = 0.3f;
    
    private GameManager gameManager;
    private Vector2Int currentGridPosition;
    private bool isMoving = false;
    private Vector2Int startPosition;
    private Direction? previousDirection = null; // 이전 이동 방향
    
    void Awake()
    {
        gameManager = FindObjectOfType<GameManager>();
    }
    
    void Start()
    {
        FindStartPosition();
    }
    
    private void FindStartPosition()
    {
        // 시작 타일 찾기
        for (int x = 0; x < 4; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                TileController tile = gameManager.GetTileAt(x, y);
                if (tile != null && tile.tileType == TileType.Start)
                {
                    startPosition = new Vector2Int(x, y);
                    currentGridPosition = startPosition;
                    SetWorldPosition(currentGridPosition);
                    return;
                }
            }
        }
    }
    
    public void StartMovement()
    {
        if (isMoving) return;
        
        StartCoroutine(MoveCoroutine());
    }
    
    private IEnumerator MoveCoroutine()
    {
        isMoving = true;
        
        while (isMoving)
        {
            TileController currentTile = gameManager.GetTileAt(currentGridPosition.x, currentGridPosition.y);
            
            if (currentTile == null)
            {
                break;
            }
            
            // 목표 타일에 도착했는지 확인
            if (currentTile.tileType == TileType.Clear || currentTile.tileType == TileType.Lose)
            {
                gameManager.OnHumanReachedDestination(currentTile.tileType);
                isMoving = false;
                break;
            }
            
            // 다음 이동 방향 찾기
            Direction? nextDirection = FindNextDirection(currentTile, previousDirection);
            
            if (nextDirection == null)
            {
                // 막다른 길
                gameManager.OnHumanReachedDeadEnd();
                isMoving = false;
                break;
            }
            
            // 이동
            Vector2Int nextPosition = GetNextPosition(currentGridPosition, nextDirection.Value);
            
            yield return StartCoroutine(MoveToPosition(nextPosition));
            
            previousDirection = nextDirection.Value;
            currentGridPosition = nextPosition;
            yield return new WaitForSeconds(moveDelay);
        }
    }
    
    private Direction? FindNextDirection(TileController tile, Direction? excludeDirection)
    {
        // 타일의 연결 방향 확인
        Direction[] directions = { Direction.Up, Direction.Right, Direction.Down, Direction.Left };
        
        foreach (Direction dir in directions)
        {
            // 이전에 온 방향으로 되돌아가지 않음
            if (excludeDirection != null && dir == GetOppositeDirection(excludeDirection.Value))
            {
                continue;
            }
            
            // 현재 타일이 dir 방향으로 연결되어 있는지 확인
            if (tile.HasConnection(dir))
            {
                Vector2Int nextPos = GetNextPosition(currentGridPosition, dir);
                TileController nextTile = gameManager.GetTileAt(nextPos.x, nextPos.y);
                
                if (nextTile != null)
                {
                    // 두 타일이 서로 연결되어 있는지 확인
                    // 타일 A가 dir 방향으로 연결되어 있고, 타일 B가 반대 방향으로 연결되어 있어야 함
                    Direction oppositeDir = GetOppositeDirection(dir);
                    if (nextTile.HasConnection(oppositeDir))
                    {
                        // 이동 가능한 방향 발견
                        return dir;
                    }
                }
            }
        }
        
        return null; // 이동할 수 없음
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
    
    private Vector2Int GetNextPosition(Vector2Int current, Direction dir)
    {
        switch (dir)
        {
            case Direction.Up:
                return new Vector2Int(current.x, current.y - 1);
            case Direction.Right:
                return new Vector2Int(current.x + 1, current.y);
            case Direction.Down:
                return new Vector2Int(current.x, current.y + 1);
            case Direction.Left:
                return new Vector2Int(current.x - 1, current.y);
            default:
                return current;
        }
    }
    
    private IEnumerator MoveToPosition(Vector2Int targetGridPos)
    {
        Vector3 targetWorldPos = GetWorldPosition(targetGridPos);
        
        while (Vector3.Distance(transform.position, targetWorldPos) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, moveSpeed * Time.deltaTime);
            yield return null;
        }
        
        transform.position = targetWorldPos;
    }
    
    private Vector3 GetWorldPosition(Vector2Int gridPos)
    {
        // 타일의 위치를 기준으로 계산
        TileController tile = gameManager.GetTileAt(gridPos.x, gridPos.y);
        if (tile != null)
        {
            return tile.transform.position;
        }
        return Vector3.zero;
    }
    
    private void SetWorldPosition(Vector2Int gridPos)
    {
        transform.position = GetWorldPosition(gridPos);
    }
    
    public void ResetPosition()
    {
        StopAllCoroutines();
        isMoving = false;
        previousDirection = null;
        currentGridPosition = startPosition;
        SetWorldPosition(startPosition);
    }
}

