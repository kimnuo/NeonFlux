using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Stage Clear 화면 UI. GameManager에서 ShowStageClear(score, reason) 호출로 활성화됩니다.
/// </summary>
public class StageClearUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject panel;
    public Text scoreText;
    public Text messageText;
    public Text highScoreText;
    public Text lastScoreText;

    [Header("Buttons")]
    public Button mainMenuButton;
    public Button leaderboardButton;

    [Header("Settings")]
    [SerializeField] private string mainMenuButtonText = "메인 메뉴";
    [SerializeField] private string leaderboardButtonText = "리더보드";
    [SerializeField] private string messageFormat = "{0}";
    [SerializeField] private bool autoCreatePanel = true;

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }
        if (leaderboardButton != null)
        {
            leaderboardButton.onClick.RemoveAllListeners();
            leaderboardButton.onClick.AddListener(OnLeaderboardClicked);
        }
    }

    public void ShowStageClear(int score, string reason)
    {
        Debug.Log($"[StageClearUI] ShowStageClear called! score={score}, reason={reason}, panel={panel?.name}");

        if (!TryActivatePanel())
        {
            if (autoCreatePanel)
            {
                Debug.Log("[StageClearUI] Creating panel dynamically...");
                CreateDynamicPanel();
            }
            else
            {
                Debug.LogError("[StageClearUI] Cannot activate or create panel!");
                return;
            }
        }

        if (scoreText != null) scoreText.text = $"점수: {score}";
        if (messageText != null) messageText.text = GetMessageLabel(reason);
        if (highScoreText != null && SaveManager.Instance != null) highScoreText.text = $"최고 점수: {SaveManager.Instance.HighScore}";
        if (lastScoreText != null && SaveManager.Instance != null) lastScoreText.text = $"기록 점수: {GetLastRecordedScore()}";
    }

    private bool TryActivatePanel()
    {
        if (panel == null) return false;

        // Activate all parents first
        Transform t = panel.transform;
        while (t != null)
        {
            if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
            t = t.parent;
        }

        panel.SetActive(true);
        bool success = panel.activeSelf && panel.activeInHierarchy;
        Debug.Log($"[StageClearUI] TryActivatePanel: activeSelf={panel.activeSelf}, activeInHierarchy={panel.activeInHierarchy}, success={success}");
        return success;
    }

    private void CreateDynamicPanel()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[StageClearUI] No Canvas found!");
            return;
        }

        // Destroy old panel if exists
        if (panel != null) Destroy(panel);

        panel = new GameObject("StageClearPanel_Dynamic", typeof(RectTransform), typeof(Image));
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.SetParent(canvas.transform, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(750f, 800f);
        rect.anchoredPosition = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0f, 0.3f, 0f, 0.92f);

        // Title
        CreateText("Title", "STAGE CLEAR", new Vector2(0f, 300f), new Vector2(600f, 90f), 60, new Color(0f, 1f, 0.5f));
        // Score
        scoreText = CreateText("ScoreText", "", new Vector2(0f, 180f), new Vector2(500f, 60f), 38, Color.white);
        // High Score
        highScoreText = CreateText("HighScoreText", "", new Vector2(0f, 120f), new Vector2(500f, 50f), 30, Color.yellow);
        // Message
        messageText = CreateText("MessageText", "", new Vector2(0f, 50f), new Vector2(500f, 50f), 26, new Color(0.6f, 1f, 0.6f));
        // Last Score
        lastScoreText = CreateText("LastScoreText", "", new Vector2(0f, -10f), new Vector2(500f, 50f), 30, new Color(1f, 1f, 0.6f));
        // Main Menu Button
        mainMenuButton = CreateButton("MainMenuButton", mainMenuButtonText, new Vector2(0f, -150f), new Color(0.4f, 0.4f, 0.4f));
        // Leaderboard Button
        leaderboardButton = CreateButton("LeaderboardButton", leaderboardButtonText, new Vector2(0f, -240f), new Color(0.3f, 0.3f, 0.3f));

        // Wire buttons
        mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        leaderboardButton.onClick.AddListener(OnLeaderboardClicked);

        panel.SetActive(true);
        Debug.Log($"[StageClearUI] Dynamic panel created and activated!");
    }

    private Text CreateText(string name, string text, Vector2 anchoredPos, Vector2 size, int fontSize, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(panel.transform, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        Text txt = go.GetComponent<Text>();
        txt.text = text;
        txt.fontSize = fontSize;
        txt.color = color;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        txt.raycastTarget = false;
        return txt;
    }

    private Button CreateButton(string name, string text, Vector2 anchoredPos, Color bgColor)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(panel.transform, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(280f, 60f);

        Image img = go.GetComponent<Image>();
        img.color = bgColor;

        Button btn = go.GetComponent<Button>();
        btn.targetGraphic = img;

        CreateText(name + "Text", text, Vector2.zero, new Vector2(280f, 60f), 26, Color.white);
        return btn;
    }

    private int GetLastRecordedScore()
    {
        if (SaveManager.Instance == null) return 0;
        var leaderboard = SaveManager.Instance.Leaderboard;
        if (leaderboard != null && leaderboard.Count > 0)
        {
            return leaderboard[0].score;
        }
        return 0;
    }

    private string GetMessageLabel(string reason)
    {
        if (string.IsNullOrEmpty(reason)) return "스테이지 클리어!";
        string lower = reason.ToLowerInvariant();
        if (lower.Contains("clear") || lower.Contains("클리어")) return "완벽한 주행!";
        return "스테이지 클리어!";
    }

    private void OnMainMenuClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GoToMainMenu();
        }
    }

    private void OnLeaderboardClicked()
    {
        LeaderboardUI lbUI = FindObjectOfType<LeaderboardUI>();
        if (lbUI != null)
        {
            lbUI.RefreshLeaderboard();
            lbUI.Show();
        }
    }
}
