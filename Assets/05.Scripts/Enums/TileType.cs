public enum TileType
{
    Straight,      // 직선
    Corner,        // 코너
    DeadEnd,       // 막힌 길 (회전 불가)
    Start,         // 시작 타일 (회전 불가)
    Lose,          // 원하는 목표 (LOSE) - 회전 불가
    Clear          // 함정 도착 (CLEAR) - 회전 불가
}

