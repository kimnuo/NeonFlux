using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Game Over 화면 UI. GameManager에서 ShowGameOver(score, reason) 호출로 활성화됩니다.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject panel;
    public Text scoreText;
    public Text reasonText;
    public Text highScoreText;
    public Text lastScoreText;

    [Header("Buttons")]
    public Button mainMenuButton;
    public Button leaderboardButton;

    [Header("Settings")]
    [SerializeField] private string mainMenuButtonText = "메인 메뉴";
    [SerializeField] private string leaderboardButtonText = "리더보드";
    [SerializeField] private string reasonFormat = "사망: {0}";
    [SerializeField] private string lastScoreFormat = "기록 점수: {0}";

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);

        // 버튼 이벤트 연결
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
    public void ShowGameOver(int score, string reason)
    {
        if (panel != null) panel.SetActive(true);

        if (scoreText != null)
        {
            scoreText.text = string.Format("점수: {0}", score);
        }

        if (reasonText != null)
        {
            string reasonLabel = GetReasonLabel(reason);
            reasonText.text = string.Format(reasonFormat, reasonLabel);
        }

        if (highScoreText != null && SaveManager.Instance != null)
        {
            highScoreText.text = string.Format("최고 점수: {0}", SaveManager.Instance.HighScore);
        }

        // 기록 점수 표시 (이전 게임 점수)
        if (lastScoreText != null && SaveManager.Instance != null)
        {
            int lastScore = GetLastRecordedScore();
            lastScoreText.text = string.Format(lastScoreFormat, lastScore);
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

    private string GetReasonLabel(string reason)
    {
        if (string.IsNullOrEmpty(reason)) return "알 수 없음";
        string lower = reason.ToLowerInvariant();
        if (lower.Contains("obstacle") || lower.Contains("장애물")) return "장애물 충돌";
        if (lower.Contains("outofbounds") || lower.Contains("이탈")) return "코스 이탈";
        if (lower.Contains("clear") || lower.Contains("클리어")) return "클리어";
        return reason;
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
