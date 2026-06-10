#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Editor helper to create all UI screens with leaderboard buttons
public static class NeonFluxUISetupEditor
{
    [MenuItem("NeonFlux/Setup Complete UI")]
    public static void SetupCompleteUI()
    {
        // Ensure canvas
        GameObject canvasGO = GameObject.Find("HUD_Canvas");
        if (canvasGO == null)
        {
            canvasGO = GameObject.Find("MainMenuCanvas");
        }
        if (canvasGO == null)
        {
            canvasGO = new GameObject("HUD_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas c = canvasGO.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        // Ensure EventSystem
        if (GameObject.FindObjectOfType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        Transform canvasT = canvasGO.transform;

        // 1. Setup Main Menu UI
        SetupMainMenu(canvasT);

        // 2. Setup Leaderboard Panel
        GameObject lbPanel = SetupLeaderboardPanel(canvasT);

        // 3. Setup Game Over UI
        SetupGameOverUI(canvasT);

        // 4. Setup Stage Clear UI
        SetupStageClearUI(canvasT);

        // 5. Wire GameManager
        WireGameManager(lbPanel);

        EditorSceneManager.MarkAllScenesDirty();
        Debug.Log("NeonFlux UI setup complete: Main Menu + Leaderboard + Game Over + Stage Clear");
    }

    private static void SetupMainMenu(Transform canvasT)
    {
        GameObject menuRoot = GameObject.Find("MainMenuRoot");
        if (menuRoot == null)
        {
            menuRoot = new GameObject("MainMenuRoot", typeof(RectTransform));
            menuRoot.transform.SetParent(canvasT, false);
            RectTransform rect = menuRoot.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        // Title text
        if (GameObject.Find("MenuTitle") == null)
        {
            GameObject title = new GameObject("MenuTitle", typeof(RectTransform), typeof(Text));
            title.transform.SetParent(menuRoot.transform, false);
            RectTransform rect = title.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -100f);
            rect.sizeDelta = new Vector2(600f, 100f);

            Text txt = title.GetComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 64;
            txt.color = new Color(0f, 1f, 1f, 1f);
            txt.alignment = TextAnchor.MiddleCenter;
            txt.text = "NEON FLUX";
        }

        // High Score text
        GameObject highScoreText = GameObject.Find("MenuHighScoreText");
        if (highScoreText == null)
        {
            highScoreText = new GameObject("MenuHighScoreText", typeof(RectTransform), typeof(Text));
            highScoreText.transform.SetParent(menuRoot.transform, false);
            RectTransform rect = highScoreText.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -200f);
            rect.sizeDelta = new Vector2(400f, 50f);

            Text txt = highScoreText.GetComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 32;
            txt.color = Color.yellow;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.text = "최고 점수: 0";
        }

        // Start Button
        GameObject startBtn = CreateButton("StartButton", menuRoot.transform, "게임 시작", new Vector2(0f, -320f), new Vector2(300f, 80f), new Color(0f, 0.8f, 0.8f, 1f));

        // Leaderboard Button
        GameObject lbBtn = CreateButton("LeaderboardButton", menuRoot.transform, "리더보드", new Vector2(0f, -420f), new Vector2(300f, 70f), new Color(0.3f, 0.3f, 0.3f, 0.9f));

        // MainMenuUI component
        GameObject mmHolder = GameObject.Find("MainMenuUIHolder");
        if (mmHolder == null)
        {
            mmHolder = new GameObject("MainMenuUIHolder");
            mmHolder.transform.SetParent(canvasT, false);
        }

        MainMenuUI mmUI = mmHolder.GetComponent<MainMenuUI>();
        if (mmUI == null) mmUI = mmHolder.AddComponent<MainMenuUI>();

        // Wire high score text
        var hsField = typeof(MainMenuUI).GetField("highScoreText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (hsField != null) hsField.SetValue(mmUI, highScoreText.GetComponent<Text>());

        // Wire button clicks
        var startComp = startBtn.GetComponent<Button>();
        startComp.onClick.RemoveAllListeners();
        startComp.onClick.AddListener(() =>
        {
            if (GameManager.Instance != null) GameManager.Instance.StartGame();
        });

        var lbComp = lbBtn.GetComponent<Button>();
        lbComp.onClick.RemoveAllListeners();
        lbComp.onClick.AddListener(mmUI.OnLeaderboardClicked);
    }

    private static GameObject SetupLeaderboardPanel(Transform canvasT)
    {
        GameObject panel = GameObject.Find("LeaderboardPanel");
        if (panel == null)
        {
            panel = new GameObject("LeaderboardPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvasT, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(760f, 800f);
            rect.anchoredPosition = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);

            // Title
            GameObject title = new GameObject("Title", typeof(RectTransform), typeof(Text));
            title.transform.SetParent(panel.transform, false);
            RectTransform tRect = title.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0.5f, 1f);
            tRect.anchorMax = new Vector2(0.5f, 1f);
            tRect.pivot = new Vector2(0.5f, 1f);
            tRect.anchoredPosition = new Vector2(0f, -20f);
            tRect.sizeDelta = new Vector2(700f, 60f);
            Text t = title.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 40;
            t.color = Color.cyan;
            t.alignment = TextAnchor.MiddleCenter;
            t.text = "리더보드";

            // Content
            GameObject content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(panel.transform, false);
            RectTransform cRect = content.GetComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0f, 0f);
            cRect.anchorMax = new Vector2(1f, 1f);
            cRect.offsetMin = new Vector2(20f, 80f);
            cRect.offsetMax = new Vector2(-20f, -100f);
            content.AddComponent<VerticalLayoutGroup>().spacing = 8f;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ((VerticalLayoutGroup)content.GetComponent<VerticalLayoutGroup>()).childForceExpandHeight = false;

            // Close button
            GameObject closeBtn = CreateButton("CloseButton", panel.transform, "닫기", new Vector2(0f, 30f), new Vector2(200f, 60f), new Color(0.5f, 0f, 0f, 0.9f));
            closeBtn.GetComponent<Button>().onClick.AddListener(() => panel.SetActive(false));

            panel.SetActive(false);
        }

        // LeaderboardUI component
        LeaderboardUI lbUI = panel.GetComponent<LeaderboardUI>();
        if (lbUI == null) lbUI = panel.AddComponent<LeaderboardUI>();
        lbUI.panel = panel;
        lbUI.contentRoot = panel.transform.Find("Content")?.GetComponent<RectTransform>();

        return panel;
    }

    private static void SetupGameOverUI(Transform canvasT)
    {
        GameObject panel = GameObject.Find("GameOverPanel");
        if (panel == null)
        {
            panel = new GameObject("GameOverPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvasT, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(700f, 700f);
            rect.anchoredPosition = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.3f, 0f, 0f, 0.9f);

            // Title
            GameObject title = CreateTextObject("Title", panel.transform, new Vector2(0f, 250f), new Vector2(600f, 80f), 56, Color.red, TextAnchor.MiddleCenter, "GAME OVER");

            // Score text
            GameObject scoreText = CreateTextObject("ScoreText", panel.transform, new Vector2(0f, 150f), new Vector2(500f, 60f), 36, Color.white, TextAnchor.MiddleCenter, "");

            // High score text
            GameObject highScoreText = CreateTextObject("HighScoreText", panel.transform, new Vector2(0f, 90f), new Vector2(500f, 50f), 28, Color.yellow, TextAnchor.MiddleCenter, "");

            // Reason text
            GameObject reasonText = CreateTextObject("ReasonText", panel.transform, new Vector2(0f, 30f), new Vector2(500f, 50f), 24, new Color(1f, 0.6f, 0.6f), TextAnchor.MiddleCenter, "");

            // Last score text
            CreateTextObject("LastScoreText", panel.transform, new Vector2(0f, -40f), new Vector2(500f, 50f), 28, new Color(1f, 1f, 0.6f), TextAnchor.MiddleCenter, "");

            // Buttons
            CreateButton("MainMenuButton", panel.transform, "메인 메뉴", new Vector2(0f, -130f), new Vector2(280f, 60f), new Color(0.4f, 0.4f, 0.4f, 0.9f));
            CreateButton("LeaderboardButton", panel.transform, "리더보드", new Vector2(0f, -290f), new Vector2(280f, 60f), new Color(0.3f, 0.3f, 0.3f, 0.9f));

            panel.SetActive(false);
        }

        // GameOverUI component
        GameOverUI goUI = panel.GetComponent<GameOverUI>();
        if (goUI == null) goUI = panel.AddComponent<GameOverUI>();

        goUI.panel = panel;
        goUI.scoreText = panel.transform.Find("ScoreText")?.GetComponent<Text>();
        goUI.highScoreText = panel.transform.Find("HighScoreText")?.GetComponent<Text>();
        goUI.reasonText = panel.transform.Find("ReasonText")?.GetComponent<Text>();
        goUI.lastScoreText = panel.transform.Find("LastScoreText")?.GetComponent<Text>();
        goUI.mainMenuButton = panel.transform.Find("MainMenuButton")?.GetComponent<Button>();
        goUI.leaderboardButton = panel.transform.Find("LeaderboardButton")?.GetComponent<Button>();
    }

    private static void SetupStageClearUI(Transform canvasT)
    {
        GameObject panel = GameObject.Find("StageClearPanel");
        if (panel == null)
        {
            panel = new GameObject("StageClearPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvasT, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(700f, 700f);
            rect.anchoredPosition = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0f, 0.3f, 0f, 0.9f);

            // Title
            GameObject title = CreateTextObject("Title", panel.transform, new Vector2(0f, 250f), new Vector2(600f, 80f), 56, new Color(0f, 1f, 0.5f), TextAnchor.MiddleCenter, "STAGE CLEAR");

            // Score text
            GameObject scoreText = CreateTextObject("ScoreText", panel.transform, new Vector2(0f, 150f), new Vector2(500f, 60f), 36, Color.white, TextAnchor.MiddleCenter, "");

            // High score text
            GameObject highScoreText = CreateTextObject("HighScoreText", panel.transform, new Vector2(0f, 90f), new Vector2(500f, 50f), 28, Color.yellow, TextAnchor.MiddleCenter, "");

            // Message text (positive)
            CreateTextObject("MessageText", panel.transform, new Vector2(0f, 30f), new Vector2(500f, 50f), 24, new Color(0.6f, 1f, 0.6f), TextAnchor.MiddleCenter, "");

            // Last score text
            CreateTextObject("LastScoreText", panel.transform, new Vector2(0f, -40f), new Vector2(500f, 50f), 28, new Color(1f, 1f, 0.6f), TextAnchor.MiddleCenter, "");

            // Buttons
            CreateButton("MainMenuButton", panel.transform, "메인 메뉴", new Vector2(0f, -150f), new Vector2(280f, 60f), new Color(0.4f, 0.4f, 0.4f, 0.9f));
            CreateButton("LeaderboardButton", panel.transform, "리더보드", new Vector2(0f, -290f), new Vector2(280f, 60f), new Color(0.3f, 0.3f, 0.3f, 0.9f));

            panel.SetActive(false);
        }

        // StageClearUI component
        StageClearUI scUI = panel.GetComponent<StageClearUI>();
        if (scUI == null) scUI = panel.AddComponent<StageClearUI>();

        scUI.panel = panel;
        scUI.scoreText = panel.transform.Find("ScoreText")?.GetComponent<Text>();
        scUI.highScoreText = panel.transform.Find("HighScoreText")?.GetComponent<Text>();
        scUI.messageText = panel.transform.Find("MessageText")?.GetComponent<Text>();
        scUI.lastScoreText = panel.transform.Find("LastScoreText")?.GetComponent<Text>();
        scUI.mainMenuButton = panel.transform.Find("MainMenuButton")?.GetComponent<Button>();
        scUI.leaderboardButton = panel.transform.Find("LeaderboardButton")?.GetComponent<Button>();
    }

    private static void WireGameManager(GameObject lbPanel)
    {
        if (GameManager.Instance != null)
        {
            // Find main menu root
            GameObject menuRoot = GameObject.Find("MainMenuRoot");
            if (menuRoot != null)
            {
                var mainMenuField = typeof(GameManager).GetField("mainMenuUI", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (mainMenuField != null) mainMenuField.SetValue(GameManager.Instance, menuRoot);
            }

            // Find game over panel
            GameObject goPanel = GameObject.Find("GameOverPanel");
            if (goPanel != null)
            {
                var goField = typeof(GameManager).GetField("gameOverUI", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (goField != null) goField.SetValue(GameManager.Instance, goPanel);
            }

            // Find stage clear panel
            GameObject scPanel = GameObject.Find("StageClearPanel");
            if (scPanel != null)
            {
                var scField = typeof(GameManager).GetField("stageClearUI", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (scField != null) scField.SetValue(GameManager.Instance, scPanel);
            }
        }

        // Wire leaderboard to MainMenuUI
        MainMenuUI mmUI = GameObject.FindObjectOfType<MainMenuUI>();
        if (mmUI != null)
        {
            LeaderboardUI lbUI = lbPanel?.GetComponent<LeaderboardUI>();
            if (lbUI != null) mmUI.leaderboardUI = lbUI;
        }
    }

    private static GameObject CreateButton(string name, Transform parent, string text, Vector2 anchoredPos, Vector2 size, Color bgColor)
    {
        GameObject btnGO = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(parent, false);
        RectTransform rect = btnGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        Image img = btnGO.GetComponent<Image>();
        img.color = bgColor;

        GameObject txtGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
        txtGO.transform.SetParent(btnGO.transform, false);
        RectTransform tRect = txtGO.GetComponent<RectTransform>();
        tRect.anchorMin = Vector2.zero;
        tRect.anchorMax = Vector2.one;
        tRect.offsetMin = Vector2.zero;
        tRect.offsetMax = Vector2.zero;
        Text txt = txtGO.GetComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 28;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.text = text;

        return btnGO;
    }

    private static GameObject CreateTextObject(string name, Transform parent, Vector2 anchoredPos, Vector2 size, int fontSize, Color color, TextAnchor alignment, string text)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        Text txt = go.GetComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = fontSize;
        txt.color = color;
        txt.alignment = alignment;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        txt.text = text;

        return go;
    }
}
#endif
