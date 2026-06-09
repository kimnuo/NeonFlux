using UnityEngine;
using UnityEngine.UI;

// Minimal Main Menu UI glue: expose a method for the Leaderboard button
public class MainMenuUI : MonoBehaviour
{
    public LeaderboardUI leaderboardUI;
    
    [Header("High Score Display")]
    [SerializeField] private Text highScoreText;

    private void Start()
    {
        UpdateHighScoreDisplay();
    }

    private void UpdateHighScoreDisplay()
    {
        if (highScoreText != null && SaveManager.Instance != null)
        {
            int highScore = SaveManager.Instance.HighScore;
            highScoreText.text = string.Format("최고 점수: {0}", highScore);
        }
    }

    public void OnLeaderboardClicked()
    {
        if (leaderboardUI != null)
        {
            leaderboardUI.RefreshLeaderboard();
            leaderboardUI.Show();
        }
        else
        {
            Debug.LogWarning("MainMenuUI: leaderboardUI not assigned");
        }
    }
}
