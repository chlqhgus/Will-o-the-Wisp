# START GAME 버튼 동작 흐름

## 동작 순서

1. **메인 메뉴에서 START GAME 버튼 클릭**
   - `MenuManager.OnStartGameClicked()` 호출

2. **첫 플레이 여부 확인**
   - `PlayerPrefs`에서 `HasPlayedBefore` 확인
   - 첫 플레이면 스테이지 1로 설정

3. **GameScene으로 씬 전환**
   - `SceneManager.LoadScene("GameScene")` 실행

4. **GameScene 로드 후**
   - `GameManager.Start()` → `LoadCurrentStage()` 호출
   - `PlayerPrefs`에서 현재 스테이지 가져오기 (기본값: 1)
   - `StageManager`에서 스테이지 데이터 가져오기
   - `GridGenerator.GenerateGrid()`로 타일 그리드 생성
   - 인간 캐릭터를 시작 위치에 배치

5. **게임 준비 완료**
   - 타일 회전 준비 단계 시작
   - UI 업데이트 (STAGE, MOVES 표시)

## 중요 사항

### 스테이지 데이터 설정 필요

게임이 제대로 작동하려면 `StageManager`에 스테이지 데이터를 추가해야 합니다:

1. Unity 에디터에서 `StageManager` 오브젝트 선택
2. Inspector에서 `Stages` 리스트에 새 항목 추가
3. 각 스테이지의 설정:
   - `Stage Number`: 스테이지 번호
   - `Tile Layout`: 4x4 타일 배열 (각 위치에 타일 타입 설정)
   - `Start Position`: 시작 위치 (x, y)
   - `Clear Position`: CLEAR 목표 위치
   - `Lose Position`: LOSE 목표 위치
   - `Moves Limit`: 타일 회전 제한 (-1 = 무제한)

### 예시: Stage 1 설정

기획서를 참고하여:
- `Start Position`: (0, 0) - H 위치
- `Clear Position`: (3, 1) - B 위치
- `Lose Position`: (1, 3) - A 위치 (초기 도달 지점)
- `Moves Limit`: -1 (무제한)

### 다음 단계

스테이지 선택 화면을 추가하려면:
1. `StageSelectScene` 씬 생성
2. `MenuManager.OnStartGameClicked()`에서 스테이지 선택 화면으로 이동하도록 수정
3. 스테이지 선택 화면에서 선택한 스테이지로 GameScene 이동

