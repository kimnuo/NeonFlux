using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Minimal leaderboard UI that populates a vertical list from SaveManager.Leaderboard
public class LeaderboardUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject panel; // root panel to enable/disable
    public RectTransform contentRoot; // container for entries (VerticalLayoutGroup recommended)
    public GameObject entryPrefab; // optional simple prefab containing a Text component

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
    }

    public void Show()
    {
        if (panel != null) panel.SetActive(true);
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }

    public void RefreshLeaderboard()
    {
        if (contentRoot == null)
        {
            Debug.LogWarning("LeaderboardUI: contentRoot is null");
            return;
        }

        // Clear existing entries
        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(contentRoot.GetChild(i).gameObject);
        }

        // Fetch leaderboard
        var list = SaveManager.Instance != null ? SaveManager.Instance.Leaderboard : null;
        if (list == null)
        {
            CreatePlaceholder("No leaderboard data");
            return;
        }

        int rank = 1;
        foreach (var e in list)
        {
            CreateEntry(rank, e.score, e.stageLevel, e.date, e.reason);
            rank++;
        }

        if (rank == 1)
        {
            CreatePlaceholder("No entries yet");
        }
    }

    private void CreatePlaceholder(string text)
    {
        GameObject go = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(contentRoot, false);
        Text t = go.GetComponent<Text>();
        t.font = GetBuiltinFont();
        t.fontSize = 30;
        t.text = text;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
    }

    private void CreateEntry(int rank, int score, int stage, string date, string reason)
    {
        if (entryPrefab != null)
        {
            GameObject go = Instantiate(entryPrefab, contentRoot);
            Text t = go.GetComponentInChildren<Text>();
            if (t != null)
            {
                t.fontSize = 30;
                t.text = FormatLine(rank, score, stage, date, reason);
            }
            return;
        }

        GameObject go2 = new GameObject($"Entry_{rank}", typeof(RectTransform), typeof(Text));
        go2.transform.SetParent(contentRoot, false);
        Text tt = go2.GetComponent<Text>();
        tt.font = GetBuiltinFont();
        tt.fontSize = 30;
        tt.text = FormatLine(rank, score, stage, date, reason);
        tt.alignment = TextAnchor.MiddleLeft;
        tt.color = Color.white;
    }

    private string FormatLine(int rank, int score, int stage, string date, string reason)
    {
        string formattedDate = date;
        if (!string.IsNullOrEmpty(date))
        {
            try
            {
                DateTime utcTime = DateTime.Parse(date, null, System.Globalization.DateTimeStyles.RoundtripKind);
                DateTime kstTime = utcTime.ToLocalTime();
                formattedDate = kstTime.ToString("MM/dd HH:mm");
            }
            catch
            {
                formattedDate = date;
            }
        }
        
        string reasonLabel = GetReasonLabel(reason);
        return string.Format("{0}위  점수:{1}  Stage:{2}  {3}  {4}", rank, score, stage, formattedDate, reasonLabel);
    }

    private string GetReasonLabel(string reason)
    {
        if (string.IsNullOrEmpty(reason)) return "";
        string lower = reason.ToLowerInvariant();
        if (lower.Contains("obstacle") || lower.Contains("장애물")) return "장애물";
        if (lower.Contains("outofbounds") || lower.Contains("이탈")) return "코스 이탈";
        if (lower.Contains("clear") || lower.Contains("클리어")) return "클리어";
        return reason;
    }

    private Font GetBuiltinFont()
    {
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }
}
