# Unity 에디터 설정 가이드

## 자동 설정 도구 사용하기

Unity 에디터에서 **Window > Will-O-The-Wisp > Game Setup Helper** 메뉴를 열어 자동 설정 도구를 사용할 수 있습니다.

### 단계별 설정

1. **Create Folder Structure** 클릭
   - 필요한 폴더 구조를 자동으로 생성합니다.
   - `Assets/Prefabs/Tiles`, `Assets/Prefabs/Player`, `Assets/Scenes` 등

2. **Create Tile Prefabs** 클릭
   - 6가지 타일 프리팹을 자동 생성합니다:
     - StraightTile (직선)
     - CornerTile (코너)
     - DeadEndTile (막힌 길)
     - StartTile (시작)
     - LoseTile (LOSE 목표)
     - ClearTile (CLEAR 목표)
   - 각 프리팹에 적절한 스프라이트가 자동 할당됩니다.

3. **Create Human Prefab** 클릭
   - 인간 캐릭터 프리팹을 생성합니다.

4. **Setup Game Scene** 클릭
   - 게임 플레이 씬을 자동으로 설정합니다.
   - GameManager, UIManager, GridGenerator 등 필요한 컴포넌트를 생성합니다.
   - UI 요소들(버튼, 텍스트)을 자동 배치합니다.

5. **Setup Main Menu Scene** 클릭
   - 메인 메뉴 씬을 자동으로 설정합니다.
   - START GAME, SETTINGS, EXIT 버튼을 생성합니다.

## 수동 설정 (필요시)

### 타일 프리팹 설정

각 타일 프리팹에는 다음 컴포넌트가 필요합니다:
- **SpriteRenderer**: 타일 스프라이트 표시
- **TileController**: 타일 로직 (자동 추가됨)
- **BoxCollider2D**: 클릭 감지용 (자동 추가됨)

### 스프라이트 할당

리소스 파일들이 이미 `Assets/03.Resources/` 폴더에 있으므로, 자동으로 할당됩니다:
- `road_1.png` → StraightTile
- `road_02.png` → CornerTile
- `ground.png` → DeadEndTile
- `StartPoint.png` → StartTile
- `LosePoint.png` → LoseTile
- `ClearPoint.png` → ClearTile
- `person_front.png` → Human

### 게임 씬 설정 확인

게임 씬에는 다음 오브젝트들이 있어야 합니다:
- **GameManager**: 게임 상태 관리
- **UIManager**: UI 관리
- **GridGenerator**: 타일 그리드 생성
- **StageManager**: 스테이지 데이터 관리
- **Canvas**: UI 요소들의 부모

### 스테이지 데이터 생성

`StageManager`에 스테이지 데이터를 추가하려면:
1. Hierarchy에서 `StageManager` 선택
2. Inspector에서 `Stages` 리스트에 새 항목 추가
3. 각 스테이지의 타일 배치 설정

## 문제 해결

### 스프라이트가 보이지 않는 경우
- 리소스 파일이 올바른 경로에 있는지 확인 (`Assets/03.Resources/`)
- Texture Import Settings에서 Sprite로 설정되어 있는지 확인

### 타일 클릭이 안 되는 경우
- 타일 프리팹에 `BoxCollider2D`가 있는지 확인
- `EventSystem`이 씬에 있는지 확인 (Canvas 생성 시 자동 추가됨)

### UI가 보이지 않는 경우
- Canvas가 올바르게 설정되어 있는지 확인
- Camera의 Render Mode가 Screen Space - Overlay인지 확인

## 다음 단계

1. 스테이지 데이터를 `StageManager`에 추가
2. 게임 테스트 및 디버깅
3. 스프라이트 및 UI 디자인 개선

