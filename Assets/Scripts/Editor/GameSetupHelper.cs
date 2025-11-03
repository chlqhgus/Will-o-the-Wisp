using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameSetupHelper : EditorWindow
{
    // 메뉴 항목: 상단 메뉴 바에서 "Will-O-The-Wisp" 메뉴를 찾으면 됩니다
    // 또는 "Tools" 메뉴에서도 찾을 수 있습니다
    [MenuItem("Will-O-The-Wisp/Game Setup Helper", false, 1)]
    [MenuItem("Tools/Game Setup Helper", false, 1)]
    public static void ShowWindow()
    {
        GetWindow<GameSetupHelper>("Game Setup Helper");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Will-O-The-Wisp Game Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        if (GUILayout.Button("Create Folder Structure", GUILayout.Height(30)))
        {
            CreateFolderStructure();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Create Tile Prefabs", GUILayout.Height(30)))
        {
            CreateTilePrefabs();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Create Human Prefab", GUILayout.Height(30)))
        {
            CreateHumanPrefab();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Setup Game Scene", GUILayout.Height(30)))
        {
            SetupGameScene();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Setup Main Menu Scene", GUILayout.Height(30)))
        {
            SetupMainMenuScene();
        }
        
        GUILayout.Space(20);
        
        EditorGUILayout.HelpBox("1. Create Folder Structure 먼저 실행\n2. 그 다음 각 프리팹 생성\n3. 마지막으로 씬 설정", MessageType.Info);
    }
    
    private void CreateFolderStructure()
    {
        string[] folders = {
            "Assets/Prefabs",
            "Assets/Prefabs/Tiles",
            "Assets/Prefabs/Player",
            "Assets/Prefabs/UI",
            "Assets/Scenes",
            "Assets/Scripts/Editor"
        };
        
        foreach (string folder in folders)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                string parentFolder = folder.Substring(0, folder.LastIndexOf('/'));
                string folderName = folder.Substring(folder.LastIndexOf('/') + 1);
                AssetDatabase.CreateFolder(parentFolder, folderName);
            }
        }
        
        AssetDatabase.Refresh();
        Debug.Log("Folder structure created!");
    }
    
    private void CreateTilePrefabs()
    {
        CreateTilePrefab("StraightTile", TileType.Straight);
        CreateTilePrefab("CornerTile", TileType.Corner);
        CreateTilePrefab("DeadEndTile", TileType.DeadEnd);
        CreateTilePrefab("StartTile", TileType.Start);
        CreateTilePrefab("LoseTile", TileType.Lose);
        CreateTilePrefab("ClearTile", TileType.Clear);
        
        AssetDatabase.Refresh();
        Debug.Log("Tile prefabs created!");
    }
    
    private void CreateTilePrefab(string name, TileType tileType)
    {
        GameObject tileObj = new GameObject(name);
        
        // SpriteRenderer 추가
        SpriteRenderer sr = tileObj.AddComponent<SpriteRenderer>();
        sr.color = Color.white;
        
        // 스프라이트 할당 시도
        Sprite sprite = GetSpriteForTileType(tileType);
        if (sprite != null)
        {
            sr.sprite = sprite;
        }
        
        // TileController 추가
        TileController tileController = tileObj.AddComponent<TileController>();
        tileController.tileType = tileType;
        
        // BoxCollider2D 추가 (클릭을 위해)
        BoxCollider2D col = tileObj.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1, 1);
        
        // 프리팹 저장
        string path = $"Assets/Prefabs/Tiles/{name}.prefab";
        PrefabUtility.SaveAsPrefabAsset(tileObj, path);
        
        DestroyImmediate(tileObj);
    }
    
    private Sprite GetSpriteForTileType(TileType tileType)
    {
        string spritePath = "";
        
        switch (tileType)
        {
            case TileType.Straight:
                spritePath = "Assets/03.Resources/road_1.png";
                break;
            case TileType.Corner:
                spritePath = "Assets/03.Resources/road_02.png";
                break;
            case TileType.DeadEnd:
                spritePath = "Assets/03.Resources/ground.png";
                break;
            case TileType.Start:
                spritePath = "Assets/03.Resources/StartPoint.png";
                break;
            case TileType.Lose:
                spritePath = "Assets/03.Resources/LosePoint.png";
                break;
            case TileType.Clear:
                spritePath = "Assets/03.Resources/ClearPoint.png";
                break;
        }
        
        if (!string.IsNullOrEmpty(spritePath))
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(spritePath);
            if (texture != null)
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (sprite == null)
                {
                    sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), 
                        new Vector2(0.5f, 0.5f), 100);
                }
                return sprite;
            }
        }
        
        return null;
    }
    
    private void CreateHumanPrefab()
    {
        GameObject humanObj = new GameObject("Human");
        
        // SpriteRenderer 추가
        SpriteRenderer sr = humanObj.AddComponent<SpriteRenderer>();
        sr.color = Color.white;
        sr.sortingOrder = 10;
        
        // 인간 스프라이트 할당 시도
        Sprite humanSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/03.Resources/person_front.png");
        if (humanSprite == null)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/03.Resources/person_front.png");
            if (texture != null)
            {
                humanSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), 
                    new Vector2(0.5f, 0.5f), 100);
            }
        }
        if (humanSprite != null)
        {
            sr.sprite = humanSprite;
        }
        
        // HumanController 추가
        HumanController humanController = humanObj.AddComponent<HumanController>();
        
        // 프리팹 저장
        string path = "Assets/Prefabs/Player/Human.prefab";
        PrefabUtility.SaveAsPrefabAsset(humanObj, path);
        
        DestroyImmediate(humanObj);
        
        AssetDatabase.Refresh();
        Debug.Log("Human prefab created!");
    }
    
    private void SetupGameScene()
    {
        UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.name == "GameScene")
        {
            // 기존 씬 사용
        }
        else
        {
            scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/GameScene.unity");
        }
        
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.transform.position = new Vector3(2.4f, -2.4f, -10);
            mainCamera.orthographicSize = 4;
        }
        
        GameObject gameManagerObj = new GameObject("GameManager");
        GameManager gameManager = gameManagerObj.AddComponent<GameManager>();
        gameManager.currentStage = 1;
        gameManager.movesRemaining = -1;
        
        GameObject uiManagerObj = new GameObject("UIManager");
        UIManager uiManager = uiManagerObj.AddComponent<UIManager>();
        CreateUIElements(uiManager);
        
        GameObject gridGeneratorObj = new GameObject("GridGenerator");
        GridGenerator gridGenerator = gridGeneratorObj.AddComponent<GridGenerator>();
        
        gridGenerator.straightTilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tiles/StraightTile.prefab");
        gridGenerator.cornerTilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tiles/CornerTile.prefab");
        gridGenerator.deadEndTilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tiles/DeadEndTile.prefab");
        gridGenerator.startTilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tiles/StartTile.prefab");
        gridGenerator.loseTilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tiles/LoseTile.prefab");
        gridGenerator.clearTilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tiles/ClearTile.prefab");
        gridGenerator.humanPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Human.prefab");
        
        GameObject stageManagerObj = new GameObject("StageManager");
        StageManager stageManager = stageManagerObj.AddComponent<StageManager>();
        
        gameManager.uiManager = uiManager;
        
        CreateCanvas();
        
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        
        Debug.Log("Game scene setup complete!");
    }
    
    private void CreateUIElements(UIManager uiManager)
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        GameObject stageTextObj = new GameObject("StageText");
        stageTextObj.transform.SetParent(canvas.transform, false);
        TextMeshProUGUI stageText = stageTextObj.AddComponent<TextMeshProUGUI>();
        stageText.text = "STAGE 1";
        stageText.fontSize = 24;
        stageText.alignment = TextAlignmentOptions.TopLeft;
        RectTransform stageRect = stageTextObj.GetComponent<RectTransform>();
        stageRect.anchorMin = new Vector2(0, 1);
        stageRect.anchorMax = new Vector2(0, 1);
        stageRect.pivot = new Vector2(0, 1);
        stageRect.anchoredPosition = new Vector2(20, -20);
        uiManager.stageText = stageText;
        
        GameObject movesTextObj = new GameObject("MovesText");
        movesTextObj.transform.SetParent(canvas.transform, false);
        TextMeshProUGUI movesText = movesTextObj.AddComponent<TextMeshProUGUI>();
        movesText.text = "MOVES: ∞";
        movesText.fontSize = 24;
        movesText.alignment = TextAlignmentOptions.TopRight;
        RectTransform movesRect = movesTextObj.GetComponent<RectTransform>();
        movesRect.anchorMin = new Vector2(1, 1);
        movesRect.anchorMax = new Vector2(1, 1);
        movesRect.pivot = new Vector2(1, 1);
        movesRect.anchoredPosition = new Vector2(-20, -20);
        uiManager.movesText = movesText;
        
        GameObject startButtonObj = new GameObject("StartButton");
        startButtonObj.transform.SetParent(canvas.transform, false);
        Image startImage = startButtonObj.AddComponent<Image>();
        startImage.color = new Color(0.2f, 0.6f, 0.2f);
        Button startButton = startButtonObj.AddComponent<Button>();
        
        GameObject startTextObj = new GameObject("Text");
        startTextObj.transform.SetParent(startButtonObj.transform, false);
        TextMeshProUGUI startText = startTextObj.AddComponent<TextMeshProUGUI>();
        startText.text = "START";
        startText.fontSize = 20;
        startText.alignment = TextAlignmentOptions.Center;
        startText.color = Color.white;
        RectTransform startTextRect = startTextObj.GetComponent<RectTransform>();
        startTextRect.anchorMin = Vector2.zero;
        startTextRect.anchorMax = Vector2.one;
        startTextRect.sizeDelta = Vector2.zero;
        
        RectTransform startRect = startButtonObj.GetComponent<RectTransform>();
        startRect.anchorMin = new Vector2(1, 0);
        startRect.anchorMax = new Vector2(1, 0);
        startRect.pivot = new Vector2(1, 0);
        startRect.anchoredPosition = new Vector2(-20, 20);
        startRect.sizeDelta = new Vector2(120, 50);
        uiManager.startButton = startButton;
        
        GameObject resetButtonObj = new GameObject("ResetButton");
        resetButtonObj.transform.SetParent(canvas.transform, false);
        Image resetImage = resetButtonObj.AddComponent<Image>();
        resetImage.color = new Color(0.6f, 0.2f, 0.2f);
        Button resetButton = resetButtonObj.AddComponent<Button>();
        
        GameObject resetTextObj = new GameObject("Text");
        resetTextObj.transform.SetParent(resetButtonObj.transform, false);
        TextMeshProUGUI resetText = resetTextObj.AddComponent<TextMeshProUGUI>();
        resetText.text = "RESET";
        resetText.fontSize = 20;
        resetText.alignment = TextAlignmentOptions.Center;
        resetText.color = Color.white;
        RectTransform resetTextRect = resetTextObj.GetComponent<RectTransform>();
        resetTextRect.anchorMin = Vector2.zero;
        resetTextRect.anchorMax = Vector2.one;
        resetTextRect.sizeDelta = Vector2.zero;
        
        RectTransform resetRect = resetButtonObj.GetComponent<RectTransform>();
        resetRect.anchorMin = new Vector2(0, 0);
        resetRect.anchorMax = new Vector2(0, 0);
        resetRect.pivot = new Vector2(0, 0);
        resetRect.anchoredPosition = new Vector2(20, 20);
        resetRect.sizeDelta = new Vector2(120, 50);
        uiManager.resetButton = resetButton;
        
        GameObject resultPanelObj = new GameObject("ResultPanel");
        resultPanelObj.transform.SetParent(canvas.transform, false);
        Image resultPanelImage = resultPanelObj.AddComponent<Image>();
        resultPanelImage.color = new Color(0, 0, 0, 0.8f);
        RectTransform resultRect = resultPanelObj.GetComponent<RectTransform>();
        resultRect.anchorMin = Vector2.zero;
        resultRect.anchorMax = Vector2.one;
        resultRect.sizeDelta = Vector2.zero;
        resultPanelObj.SetActive(false);
        
        GameObject resultTextObj = new GameObject("ResultText");
        resultTextObj.transform.SetParent(resultPanelObj.transform, false);
        TextMeshProUGUI resultText = resultTextObj.AddComponent<TextMeshProUGUI>();
        resultText.text = "CLEAR";
        resultText.fontSize = 48;
        resultText.alignment = TextAlignmentOptions.Center;
        resultText.color = Color.white;
        RectTransform resultTextRect = resultTextObj.GetComponent<RectTransform>();
        resultTextRect.anchorMin = new Vector2(0, 0.5f);
        resultTextRect.anchorMax = new Vector2(1, 0.5f);
        resultTextRect.pivot = new Vector2(0.5f, 0.5f);
        resultTextRect.anchoredPosition = Vector2.zero;
        resultTextRect.sizeDelta = new Vector2(0, 100);
        uiManager.resultText = resultText;
        
        GameObject nextStageButtonObj = new GameObject("NextStageButton");
        nextStageButtonObj.transform.SetParent(resultPanelObj.transform, false);
        Image nextStageImage = nextStageButtonObj.AddComponent<Image>();
        nextStageImage.color = new Color(0.2f, 0.6f, 0.2f);
        Button nextStageButton = nextStageButtonObj.AddComponent<Button>();
        
        GameObject nextStageTextObj = new GameObject("Text");
        nextStageTextObj.transform.SetParent(nextStageButtonObj.transform, false);
        TextMeshProUGUI nextStageText = nextStageTextObj.AddComponent<TextMeshProUGUI>();
        nextStageText.text = "NEXT STAGE";
        nextStageText.fontSize = 20;
        nextStageText.alignment = TextAlignmentOptions.Center;
        nextStageText.color = Color.white;
        RectTransform nextStageTextRect = nextStageTextObj.GetComponent<RectTransform>();
        nextStageTextRect.anchorMin = Vector2.zero;
        nextStageTextRect.anchorMax = Vector2.one;
        nextStageTextRect.sizeDelta = Vector2.zero;
        
        RectTransform nextStageRect = nextStageButtonObj.GetComponent<RectTransform>();
        nextStageRect.anchorMin = new Vector2(0.5f, 0);
        nextStageRect.anchorMax = new Vector2(0.5f, 0);
        nextStageRect.pivot = new Vector2(0.5f, 0);
        nextStageRect.anchoredPosition = new Vector2(0, 80);
        nextStageRect.sizeDelta = new Vector2(150, 50);
        uiManager.nextStageButton = nextStageButton;
        
        GameObject restartButtonObj = new GameObject("RestartButton");
        restartButtonObj.transform.SetParent(resultPanelObj.transform, false);
        Image restartImage = restartButtonObj.AddComponent<Image>();
        restartImage.color = new Color(0.6f, 0.2f, 0.2f);
        Button restartButton = restartButtonObj.AddComponent<Button>();
        
        GameObject restartTextObj = new GameObject("Text");
        restartTextObj.transform.SetParent(restartButtonObj.transform, false);
        TextMeshProUGUI restartText = restartTextObj.AddComponent<TextMeshProUGUI>();
        restartText.text = "RESTART";
        restartText.fontSize = 20;
        restartText.alignment = TextAlignmentOptions.Center;
        restartText.color = Color.white;
        RectTransform restartTextRect = restartTextObj.GetComponent<RectTransform>();
        restartTextRect.anchorMin = Vector2.zero;
        restartTextRect.anchorMax = Vector2.one;
        restartTextRect.sizeDelta = Vector2.zero;
        
        RectTransform restartRect = restartButtonObj.GetComponent<RectTransform>();
        restartRect.anchorMin = new Vector2(0.5f, 0);
        restartRect.anchorMax = new Vector2(0.5f, 0);
        restartRect.pivot = new Vector2(0.5f, 0);
        restartRect.anchoredPosition = new Vector2(0, 20);
        restartRect.sizeDelta = new Vector2(150, 50);
        uiManager.restartButton = restartButton;
        
        uiManager.resultPanel = resultPanelObj;
    }
    
    private void CreateCanvas()
    {
        // Canvas는 이미 CreateUIElements에서 생성됨
    }
    
    private void SetupMainMenuScene()
    {
        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.backgroundColor = new Color(0.1f, 0.15f, 0.1f);
        }
        
        GameObject menuManagerObj = new GameObject("MenuManager");
        MenuManager menuManager = menuManagerObj.AddComponent<MenuManager>();
        
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvas.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.05f, 0.1f, 0.05f);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(canvas.transform, false);
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "MYSTERY PATH";
        titleText.fontSize = 48;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1);
        titleRect.anchorMax = new Vector2(0.5f, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, -50);
        titleRect.sizeDelta = new Vector2(400, 80);
        
        CreateMenuButton(canvas.transform, "StartGameButton", "START GAME", 
            new Vector2(0.5f, 0.5f), new Vector2(200, 60), 
            new Color(0.2f, 0.6f, 0.2f));
        
        CreateMenuButton(canvas.transform, "SettingsButton", "SETTINGS", 
            new Vector2(0.5f, 0.4f), new Vector2(200, 60), 
            new Color(0.4f, 0.4f, 0.4f));
        
        CreateMenuButton(canvas.transform, "ExitGameButton", "EXIT GAME", 
            new Vector2(0.5f, 0.3f), new Vector2(200, 60), 
            new Color(0.6f, 0.2f, 0.2f));
        
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity");
        
        Debug.Log("Main menu scene setup complete!");
    }
    
    private void CreateMenuButton(Transform parent, string name, string text, 
        Vector2 anchorPos, Vector2 size, Color color)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent, false);
        
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = color;
        
        Button button = buttonObj.AddComponent<Button>();
        
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = text;
        buttonText.fontSize = 24;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.color = Color.white;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        
        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchorMin = anchorPos;
        buttonRect.anchorMax = anchorPos;
        buttonRect.pivot = anchorPos;
        buttonRect.anchoredPosition = new Vector2(0, anchorPos.y * Screen.height - Screen.height / 2);
        buttonRect.sizeDelta = size;
        
        if (name == "StartGameButton")
        {
            button.onClick.AddListener(() => {
                MenuManager menuManager = FindObjectOfType<MenuManager>();
                if (menuManager != null) menuManager.OnStartGameClicked();
            });
        }
        else if (name == "SettingsButton")
        {
            button.onClick.AddListener(() => {
                MenuManager menuManager = FindObjectOfType<MenuManager>();
                if (menuManager != null) menuManager.OnSettingsClicked();
            });
        }
        else if (name == "ExitGameButton")
        {
            button.onClick.AddListener(() => {
                MenuManager menuManager = FindObjectOfType<MenuManager>();
                if (menuManager != null) menuManager.OnExitGameClicked();
            });
        }
    }
}
