using UnityEngine;

public enum GameState { MainMenu, Playing, GameOver }

public class GameManager : Singleton<GameManager>
{
    public GameState CurrentState { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        CurrentState = GameState.MainMenu;
    }

    public void StartGame()
    {
        CurrentState = GameState.Playing;
        // 향후 UI 갱신 이벤트 호출 및 차량 움직임 활성화
    }

    public void EndGame()
    {
        if (CurrentState == GameState.GameOver) return;

        CurrentState = GameState.GameOver;
        Debug.Log("Game Over!");

        // 디스크에 영구 데이터 보존
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveData();
        }

        // 보상형 광고 팝업 띄우기 (AdMobManager 연동)
        if (AdMobManager.Instance != null)
        {
            AdMobManager.Instance.ShowRewardedAd();
        }
    }
}