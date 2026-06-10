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
    public Button nextStageButton;
    public Button mainMenuButton;
    public Button leaderboardButton;

    [Header("Settings")]
    [SerializeField] private string nextStageButtonText = "다음 단계";
    [SerializeField] private string mainMenuButtonText = "메인 메뉴";
    [SerializeField] private string leaderboardButtonText = "리더보드";
    [SerializeField] private string messageFormat = "{0}";

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);

        // 버튼 이벤트 연결
        if (nextStageButton != null)
        {
            nextStageButton.onClick.RemoveAllListeners();
            nextStageButton.onClick.AddListener(OnNextStageClicked);
        }
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

    /// <summary>
    /// GameManager에서 리플렉션으로 호출하는 메서드
    /// </summary>
    public void ShowStageClear(int score, string reason)
    {
        if (panel != null) panel.SetActive(true);

        if (scoreText != null)
        {
            scoreText.text = string.Format("점수: {0}", score);
        }

        if (messageText != null)
        {
            string messageLabel = GetMessageLabel(reason);
            messageText.text = string.Format(messageFormat, messageLabel);
        }

        if (highScoreText != null && SaveManager.Instance != null)
        {
            highScoreText.text = string.Format("최고 점수: {0}", SaveManager.Instance.HighScore);
        }

        // 기록 점수 표시 (이전 게임 점수)
        if (lastScoreText != null && SaveManager.Instance != null)
        {
            int lastScore = GetLastRecordedScore();
            lastScoreText.text = string.Format("기록 점수: {0}", lastScore);
        }
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

    private void OnNextStageClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GoToNextStage();
        }
    }

    private void OnMainMenuClicked()
    {
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            player.ResetToStartState();
        }

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
