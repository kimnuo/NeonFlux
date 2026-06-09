using UnityEngine;

// Minimal Main Menu UI glue: expose a method for the Leaderboard button
public class MainMenuUI : MonoBehaviour
{
    public LeaderboardUI leaderboardUI;

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
