using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 게임 시작 시 모든 UI를 자동으로 생성하고 연결합니다.
/// Singleton으로 GameManager와 함께 작동합니다.
/// </summary>
public class AutoUIBuilder : MonoBehaviour
{
    public static AutoUIBuilder Instance { get; private set; }

    [Header("Options")]
    [SerializeField] private bool buildMainMenu = true;
    [SerializeField] private bool buildLeaderboard = true;
    [SerializeField] private bool buildGameOver = true;
    [SerializeField] private bool buildStageClear = true;

    private Canvas _canvas;
    private LeaderboardUI _leaderboardUI;
    private MainMenuUI _mainMenuUI;
    private GameOverUI _gameOverUI;
    private StageClearUI _stageClearUI;
    private GameObject _mainMenuRoot;
    private GameObject _leaderboardPanel;
    private GameObject _gameOverPanel;
    private GameObject _stageClearPanel;
    private Text _highScoreText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log("[AutoUIBuilder] Starting UI setup...");
        EnsureCanvas();
        EnsureEventSystem();

        if (buildMainMenu) BuildMainMenu();
        if (buildLeaderboard) BuildLeaderboard();
        if (buildGameOver) BuildGameOver();
        if (buildStageClear) BuildStageClear();

        WireReferences();
        Debug.Log("[AutoUIBuilder] UI setup complete!");
    }

    private void EnsureCanvas()
    {
        _canvas = FindObjectOfType<Canvas>();
        if (_canvas == null)
        {
            GameObject canvasGO = new GameObject("HUD_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _canvas = canvasGO.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
        }
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }

    private void BuildMainMenu()
    {
        // Check if existing MenuRoot has buttons
        GameObject menuRoot = GameObject.Find("MenuRoot");
        bool hasStartButton = false;
        bool hasLeaderboardButton = false;

        if (menuRoot != null)
        {
            _mainMenuRoot = menuRoot;
            Button[] buttons = menuRoot.GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                if (b.name.Contains("Start")) hasStartButton = true;
                if (b.name.Contains("Leaderboard")) hasLeaderboardButton = true;
            }
        }
        else
        {
            _mainMenuRoot = new GameObject("MenuRoot", typeof(RectTransform));
            _mainMenuRoot.transform.SetParent(_canvas.transform, false);
            RectTransform rect = _mainMenuRoot.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        // High Score Text
        _highScoreText = FindChildText(_mainMenuRoot, "MenuHighScoreText");
        if (_highScoreText == null)
        {
            _highScoreText = CreateTextAtTop("MenuHighScoreText", _mainMenuRoot.transform,
                new Vector2(0f, -220f), new Vector2(500f, 50f), 32, Color.yellow, TextAnchor.MiddleCenter);
            _highScoreText.text = "최고 점수: 0";
            Debug.Log("[AutoUIBuilder] Created MenuHighScoreText");
        }

        // Leaderboard Button
        if (!hasLeaderboardButton)
        {
            GameObject lbBtn = CreateButtonAtTop("LeaderboardButton", _mainMenuRoot.transform,
                "리더보드", -520f, new Vector2(320f, 80f), new Color(0.3f, 0.3f, 0.3f, 0.9f), 32);
            Debug.Log("[AutoUIBuilder] Created LeaderboardButton");
        }

        // MainMenuUI component
        GameObject mmHolder = GameObject.Find("MainMenuUI");
        if (mmHolder == null)
        {
            mmHolder = new GameObject("MainMenuUI");
            mmHolder.transform.SetParent(_canvas.transform, false);
        }
        _mainMenuUI = mmHolder.GetComponent<MainMenuUI>();
        if (_mainMenuUI == null) _mainMenuUI = mmHolder.AddComponent<MainMenuUI>();
    }

    private void BuildLeaderboard()
    {
        _leaderboardPanel = GameObject.Find("LeaderboardPanel");
        bool needsSetup = _leaderboardPanel == null;

        if (needsSetup)
        {
            _leaderboardPanel = new GameObject("LeaderboardPanel", typeof(RectTransform), typeof(Image));
            _leaderboardPanel.transform.SetParent(_canvas.transform, false);
            RectTransform rect = _leaderboardPanel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(800f, 900f);
            rect.anchoredPosition = Vector2.zero;
            _leaderboardPanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.9f);

            // Title
            Text title = CreateText("Title", _leaderboardPanel.transform,
                new Vector2(0f, 400f), new Vector2(700f, 70f), 44, Color.cyan, TextAnchor.MiddleCenter);
            title.text = "리더보드";

            // Content
            GameObject content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(_leaderboardPanel.transform, false);
            RectTransform cRect = content.GetComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0f, 0f);
            cRect.anchorMax = new Vector2(1f, 1f);
            cRect.offsetMin = new Vector2(20f, 60f);
            cRect.offsetMax = new Vector2(-20f, 100f);
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10f;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.padding = new RectOffset(10, 10, 10, 10);
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Close button
            GameObject closeBtn = CreateButton("CloseButton", _leaderboardPanel.transform,
                "닫기", new Vector2(0f, -400f), new Vector2(240f, 70f), new Color(0.5f, 0f, 0f, 0.9f), 28);
        }

        _leaderboardUI = _leaderboardPanel.GetComponent<LeaderboardUI>();
        if (_leaderboardUI == null) _leaderboardUI = _leaderboardPanel.AddComponent<LeaderboardUI>();
    }

    private void BuildGameOver()
    {
        _gameOverPanel = GameObject.Find("GameOverPanel");
        bool needsSetup = _gameOverPanel == null;

        if (needsSetup)
        {
            _gameOverPanel = new GameObject("GameOverPanel", typeof(RectTransform), typeof(Image));
            _gameOverPanel.transform.SetParent(_canvas.transform, false);
            RectTransform rect = _gameOverPanel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(750f, 800f);
            rect.anchoredPosition = Vector2.zero;
            _gameOverPanel.GetComponent<Image>().color = new Color(0.3f, 0f, 0f, 0.92f);

            // Title
            CreateText("Title", _gameOverPanel.transform,
                new Vector2(0f, 330f), new Vector2(650f, 90f), 60, Color.red, TextAnchor.MiddleCenter).text = "GAME OVER";

            // Score
            CreateText("ScoreText", _gameOverPanel.transform,
                new Vector2(0f, 210f), new Vector2(550f, 60f), 38, Color.white, TextAnchor.MiddleCenter);

            // High Score
            CreateText("HighScoreText", _gameOverPanel.transform,
                new Vector2(0f, 140f), new Vector2(550f, 50f), 30, Color.yellow, TextAnchor.MiddleCenter);

            // Reason
            CreateText("ReasonText", _gameOverPanel.transform,
                new Vector2(0f, 70f), new Vector2(550f, 50f), 26, new Color(1f, 0.6f, 0.6f), TextAnchor.MiddleCenter);

            // Last Score Text
            CreateText("LastScoreText", _gameOverPanel.transform,
                new Vector2(0f, -10f), new Vector2(550f, 50f), 30, new Color(1f, 1f, 0.6f), TextAnchor.MiddleCenter);

            // Buttons
            CreateButton("MainMenuButton", _gameOverPanel.transform,
                "메인 메뉴", new Vector2(0f, -200f), new Vector2(300f, 65f), new Color(0.4f, 0.4f, 0.4f, 0.9f), 28);
            CreateButton("LeaderboardButton", _gameOverPanel.transform,
                "리더보드", new Vector2(0f, -290f), new Vector2(300f, 65f), new Color(0.3f, 0.3f, 0.3f, 0.9f), 28);
        }

        _gameOverUI = _gameOverPanel.GetComponent<GameOverUI>();
        if (_gameOverUI == null) _gameOverUI = _gameOverPanel.AddComponent<GameOverUI>();
    }

    private void BuildStageClear()
    {
        _stageClearPanel = GameObject.Find("StageClearPanel");
        bool needsSetup = _stageClearPanel == null;

        if (needsSetup)
        {
            _stageClearPanel = new GameObject("StageClearPanel", typeof(RectTransform), typeof(Image));
            _stageClearPanel.transform.SetParent(_canvas.transform, false);
            RectTransform rect = _stageClearPanel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(750f, 800f);
            rect.anchoredPosition = Vector2.zero;
            _stageClearPanel.GetComponent<Image>().color = new Color(0f, 0.3f, 0f, 0.92f);

            // Title
            CreateText("Title", _stageClearPanel.transform,
                new Vector2(0f, 330f), new Vector2(650f, 90f), 60, new Color(0f, 1f, 0.5f), TextAnchor.MiddleCenter).text = "STAGE CLEAR";

            // Score
            CreateText("ScoreText", _stageClearPanel.transform,
                new Vector2(0f, 210f), new Vector2(550f, 60f), 38, Color.white, TextAnchor.MiddleCenter);

            // High Score
            CreateText("HighScoreText", _stageClearPanel.transform,
                new Vector2(0f, 140f), new Vector2(550f, 50f), 30, Color.yellow, TextAnchor.MiddleCenter);

            // Message (positive)
            CreateText("MessageText", _stageClearPanel.transform,
                new Vector2(0f, 70f), new Vector2(550f, 50f), 26, new Color(0.6f, 1f, 0.6f), TextAnchor.MiddleCenter);

            // Last Score Text
            CreateText("LastScoreText", _stageClearPanel.transform,
                new Vector2(0f, -10f), new Vector2(550f, 50f), 30, new Color(1f, 1f, 0.6f), TextAnchor.MiddleCenter);

            // Buttons
            CreateButton("NextStageButton", _stageClearPanel.transform,
                "다음 단계", new Vector2(0f, -130f), new Vector2(300f, 65f), new Color(0f, 0.6f, 0.6f, 1f), 28);
            CreateButton("MainMenuButton", _stageClearPanel.transform,
                "메인 메뉴", new Vector2(0f, -220f), new Vector2(300f, 65f), new Color(0.4f, 0.4f, 0.4f, 0.9f), 28);
            CreateButton("LeaderboardButton", _stageClearPanel.transform,
                "리더보드", new Vector2(0f, -310f), new Vector2(300f, 65f), new Color(0.3f, 0.3f, 0.3f, 0.9f), 28);

            _stageClearPanel.SetActive(false);
        }

        _stageClearUI = _stageClearPanel.GetComponent<StageClearUI>();
        if (_stageClearUI == null) _stageClearUI = _stageClearPanel.AddComponent<StageClearUI>();
    }

    private void WireReferences()
    {
        Debug.Log("[AutoUIBuilder] Wiring references...");
        Debug.Log("[AutoUIBuilder] _leaderboardUI=" + (_leaderboardUI != null ? "OK" : "NULL"));
        Debug.Log("[AutoUIBuilder] _leaderboardPanel=" + (_leaderboardPanel != null ? "OK" : "NULL"));
        Debug.Log("[AutoUIBuilder] _mainMenuUI=" + (_mainMenuUI != null ? "OK" : "NULL"));
        Debug.Log("[AutoUIBuilder] _gameOverUI=" + (_gameOverUI != null ? "OK" : "NULL"));
        Debug.Log("[AutoUIBuilder] _highScoreText=" + (_highScoreText != null ? "OK" : "NULL"));
        Debug.Log("[AutoUIBuilder] GameManager=" + (GameManager.Instance != null ? "OK" : "NULL"));

        // LeaderboardUI
        if (_leaderboardUI != null)
        {
            _leaderboardUI.panel = _leaderboardPanel;
            _leaderboardUI.contentRoot = FindChildRectTransform(_leaderboardPanel, "Content");
            Debug.Log("[AutoUIBuilder] LeaderboardUI wired. contentRoot=" + (_leaderboardUI.contentRoot != null ? "OK" : "NULL"));
        }

        // MainMenuUI
        if (_mainMenuUI != null)
        {
            _mainMenuUI.leaderboardUI = _leaderboardUI;
            // Wire highScoreText via reflection (it's private serialized)
            var hsField = typeof(MainMenuUI).GetField("highScoreText",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (hsField != null) hsField.SetValue(_mainMenuUI, _highScoreText);
            Debug.Log("[AutoUIBuilder] MainMenuUI wired. leaderboardUI=" + (_mainMenuUI.leaderboardUI != null ? "OK" : "NULL"));
        }

        // Main menu root -> GameManager
        if (GameManager.Instance != null && _mainMenuRoot != null)
        {
            var mmField = typeof(GameManager).GetField("mainMenuUI",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (mmField != null) mmField.SetValue(GameManager.Instance, _mainMenuRoot);
        }

        // Game over panel -> GameManager
        if (GameManager.Instance != null && _gameOverPanel != null)
        {
            var goField = typeof(GameManager).GetField("gameOverUI",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (goField != null) goField.SetValue(GameManager.Instance, _gameOverPanel);
        }

        // Stage clear panel -> GameManager
        if (GameManager.Instance != null && _stageClearPanel != null)
        {
            var scField = typeof(GameManager).GetField("stageClearUI",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (scField != null) scField.SetValue(GameManager.Instance, _stageClearPanel);
        }

        // Wire button clicks
        WireButton("StartButton", () => GameManager.Instance?.StartGame(), _mainMenuRoot);
        WireButton("LeaderboardButton", OnLeaderboardClicked, _mainMenuRoot);
        WireButton("CloseButton", () => { if (_leaderboardPanel != null) _leaderboardPanel.SetActive(false); }, _leaderboardPanel);
        WireButton("MainMenuButton", () => GameManager.Instance?.GoToMainMenu(), _gameOverPanel);
        WireButton("LeaderboardButton", OnLeaderboardClicked, _gameOverPanel);
        WireButton("NextStageButton", () => GameManager.Instance?.GoToNextStage(), _stageClearPanel);
        WireButton("MainMenuButton", () => GameManager.Instance?.GoToMainMenu(), _stageClearPanel);
        WireButton("LeaderboardButton", OnLeaderboardClicked, _stageClearPanel);

        // GameOverUI wire
        if (_gameOverUI != null)
        {
            _gameOverUI.panel = _gameOverPanel;
            _gameOverUI.scoreText = FindChildText(_gameOverPanel, "ScoreText");
            _gameOverUI.highScoreText = FindChildText(_gameOverPanel, "HighScoreText");
            _gameOverUI.reasonText = FindChildText(_gameOverPanel, "ReasonText");
            _gameOverUI.lastScoreText = FindChildText(_gameOverPanel, "LastScoreText");
            _gameOverUI.mainMenuButton = FindChildButton(_gameOverPanel, "MainMenuButton");
            _gameOverUI.leaderboardButton = FindChildButton(_gameOverPanel, "LeaderboardButton");
        }

        // StageClearUI wire
        if (_stageClearUI != null)
        {
            _stageClearUI.panel = _stageClearPanel;
            _stageClearUI.scoreText = FindChildText(_stageClearPanel, "ScoreText");
            _stageClearUI.highScoreText = FindChildText(_stageClearPanel, "HighScoreText");
            _stageClearUI.messageText = FindChildText(_stageClearPanel, "MessageText");
            _stageClearUI.lastScoreText = FindChildText(_stageClearPanel, "LastScoreText");
            _stageClearUI.nextStageButton = FindChildButton(_stageClearPanel, "NextStageButton");
            _stageClearUI.mainMenuButton = FindChildButton(_stageClearPanel, "MainMenuButton");
            _stageClearUI.leaderboardButton = FindChildButton(_stageClearPanel, "LeaderboardButton");
        }

        // Update high score display
        UpdateHighScoreDisplay();
    }

    private void OnLeaderboardClicked()
    {
        Debug.Log("[AutoUIBuilder] OnLeaderboardClicked called!");
        Debug.Log("[AutoUIBuilder] _leaderboardUI=" + (_leaderboardUI != null ? "OK" : "NULL"));
        Debug.Log("[AutoUIBuilder] _leaderboardPanel=" + (_leaderboardPanel != null ? "OK" : "NULL"));

        if (_leaderboardUI != null)
        {
            _leaderboardUI.RefreshLeaderboard();
            _leaderboardUI.Show();
            Debug.Log("[AutoUIBuilder] Leaderboard shown!");
        }
        else
        {
            Debug.LogError("[AutoUIBuilder] _leaderboardUI is NULL!");
        }
    }

    private void UpdateHighScoreDisplay()
    {
        if (_highScoreText != null && SaveManager.Instance != null)
        {
            _highScoreText.text = string.Format("최고 점수: {0}", SaveManager.Instance.HighScore);
        }
    }

    private void WireButton(string name, System.Action action, GameObject parent)
    {
        if (parent == null || action == null) return;
        Button btn = FindChildButton(parent, name);
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => action());
            Debug.Log("[AutoUIBuilder] Wired button: " + name);
        }
        else
        {
            Debug.LogWarning("[AutoUIBuilder] Could not find button: " + name + " in " + parent.name);
        }
    }

    #region Helper Methods

    private static GameObject CreateButton(string name, Transform parent, string text, Vector2 anchoredPos, Vector2 size, Color bgColor, int fontSize)
    {
        // Check if already exists
        Transform existing = parent.Find(name);
        if (existing != null) return existing.gameObject;

        GameObject btnGO = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(parent, false);
        RectTransform rect = btnGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
        btnGO.GetComponent<Image>().color = bgColor;

        GameObject txtGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
        txtGO.transform.SetParent(btnGO.transform, false);
        RectTransform tRect = txtGO.GetComponent<RectTransform>();
        tRect.anchorMin = Vector2.zero;
        tRect.anchorMax = Vector2.one;
        tRect.offsetMin = Vector2.zero;
        tRect.offsetMax = Vector2.zero;
        Text txt = txtGO.GetComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = fontSize;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.text = text;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Overflow;

        return btnGO;
    }

    private static GameObject CreateButtonAtTop(string name, Transform parent, string text, float yFromTop, Vector2 size, Color bgColor, int fontSize)
    {
        // Check if already exists
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            Debug.Log("[AutoUIBuilder] Found existing button: " + name);
            return existing.gameObject;
        }

        GameObject btnGO = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(parent, false);
        RectTransform rect = btnGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, yFromTop);
        rect.sizeDelta = size;
        btnGO.GetComponent<Image>().color = bgColor;

        GameObject txtGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
        txtGO.transform.SetParent(btnGO.transform, false);
        RectTransform tRect = txtGO.GetComponent<RectTransform>();
        tRect.anchorMin = Vector2.zero;
        tRect.anchorMax = Vector2.one;
        tRect.offsetMin = Vector2.zero;
        tRect.offsetMax = Vector2.zero;
        Text txt = txtGO.GetComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = fontSize;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.text = text;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Overflow;

        Debug.Log("[AutoUIBuilder] Created button: " + name + " at y=" + yFromTop);
        return btnGO;
    }

    private static Text CreateText(string name, Transform parent, Vector2 anchoredPos, Vector2 size, int fontSize, Color color, TextAnchor alignment)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            Text txt = existing.GetComponent<Text>();
            if (txt != null) return txt;
        }

        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        Text txt2 = go.GetComponent<Text>();
        txt2.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt2.fontSize = fontSize;
        txt2.color = color;
        txt2.alignment = alignment;
        txt2.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt2.verticalOverflow = VerticalWrapMode.Overflow;

        return txt2;
    }

    private static Text CreateTextAtTop(string name, Transform parent, Vector2 anchoredPos, Vector2 size, int fontSize, Color color, TextAnchor alignment)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            Text txt = existing.GetComponent<Text>();
            if (txt != null) return txt;
        }

        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        Text txt2 = go.GetComponent<Text>();
        txt2.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt2.fontSize = fontSize;
        txt2.color = color;
        txt2.alignment = alignment;
        txt2.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt2.verticalOverflow = VerticalWrapMode.Overflow;

        return txt2;
    }

    private static Button FindChildButton(GameObject parent, string name)
    {
        if (parent == null) return null;
        // Try direct child first
        Transform t = parent.transform.Find(name);
        if (t != null) return t.GetComponent<Button>();
        // Then search all children
        Button[] buttons = parent.GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            if (btn.name == name) return btn;
        }
        return null;
    }

    private static Text FindChildText(GameObject parent, string name)
    {
        if (parent == null) return null;
        Transform t = parent.transform.Find(name);
        if (t != null) return t.GetComponent<Text>();
        Text[] texts = parent.GetComponentsInChildren<Text>(true);
        foreach (var txt in texts)
        {
            if (txt.name == name) return txt;
        }
        return null;
    }

    private static RectTransform FindChildRectTransform(GameObject parent, string name)
    {
        if (parent == null) return null;
        Transform t = parent.transform.Find(name);
        if (t != null) return t.GetComponent<RectTransform>();
        return null;
    }

    #endregion
}
