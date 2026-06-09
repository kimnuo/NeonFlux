using System;
using System.Collections.Generic;
using System.IO;using UnityEngine;

// JSON으로 디스크에 직렬화될 동적 유저 데이터 구조체 
[Serializable]
public class PlayerSaveData
{
    public int currentCoins;
    public int currentSpeedLevel;
    public int currentDriftLevel;
    public int currentStageLevel;

    // Leaderboard support
    [Serializable]
    public class LeaderboardEntry
    {
        public int score;
        public int stageLevel;
        public string date;
        public string reason;

        public LeaderboardEntry() { }
        public LeaderboardEntry(int score, int stageLevel, string date, string reason)
        {
            this.score = score;
            this.stageLevel = stageLevel;
            this.date = date;
            this.reason = reason;
        }
    }

    public int highScore;
    public List<LeaderboardEntry> scoreHistory;

    public PlayerSaveData()
    {
        scoreHistory = new List<LeaderboardEntry>();
        highScore = 0;
    }
}
