# 스테이지 데이터 생성 가이드

## 자동 생성 방법

Unity 에디터에서:
1. **Window > Will-O-The-Wisp > Create Stage Data** 또는 **Tools > Create Stage Data** 선택
2. GameScene을 열고 StageManager 오브젝트를 선택한 상태에서
3. "Create Stage 1 & 2" 버튼 클릭

## 수동 설정 방법

Unity 에디터에서:
1. GameScene 열기
2. Hierarchy에서 `StageManager` 선택
3. Inspector에서 `Stages` 리스트에 새 항목 추가
4. 각 스테이지 설정:
   - **Stage Number**: 스테이지 번호
   - **Tile Layout**: 4x4 배열 (각 위치의 타일 타입)
   - **Start Position**: 시작 위치 (x, y)
   - **Clear Position**: CLEAR 목표 위치
   - **Lose Position**: LOSE 목표 위치
   - **Moves Limit**: 타일 회전 제한 (-1 = 무제한)

## 타일 타입

- **Straight**: 직선 타일 (상하 또는 좌우 연결)
- **Corner**: 코너 타일 (L자 모양)
- **DeadEnd**: 막힌 길 (회전 불가)
- **Start**: 시작 타일 (회전 불가)
- **Lose**: LOSE 목표 타일 (회전 불가)
- **Clear**: CLEAR 목표 타일 (회전 불가)

## 좌표 시스템

그리드 좌표는 (x, y) = (열, 행) 형식입니다.
- x: 0~3 (왼쪽에서 오른쪽)
- y: 0~3 (위에서 아래)

예시:
- (0, 0): 왼쪽 위
- (3, 0): 오른쪽 위
- (0, 3): 왼쪽 아래
- (3, 3): 오른쪽 아래

## Stage 1 & 2 설정

### Stage 1
- **Moves Limit**: -1 (무제한)
- **Start**: (0, 0)
- **Clear**: (3, 1)
- **Lose**: (1, 3)

### Stage 2
- **Moves Limit**: -1 (무제한)
- **Start**: (0, 0)
- **Clear**: (3, 1)
- **Lose**: (1, 3)

## 스테이지 수정

생성된 스테이지는 Unity 에디터의 Inspector에서 언제든지 수정할 수 있습니다.
StageManager의 Stages 리스트에서 각 스테이지를 선택하여 수정하세요.

