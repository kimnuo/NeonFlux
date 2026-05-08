using UnityEngine;

public enum GameState { MainMenu, Playing, StageClear, GameOver }

public class GameManager : Singleton<GameManager>
{
    public GameState CurrentState { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        CurrentState = GameState.Playing;
    }

    public void StartGame()
    {
        CurrentState = GameState.Playing;
        // 향후 UI 갱신 이벤트 호출 및 차량 움직임 활성화
    }

    public void EndGame()
    {
        if (CurrentState != GameState.Playing) return;

        CurrentState = GameState.GameOver;
        Debug.Log("Game Over! 장애물에 충돌했습니다.");

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

    public void CompleteStage()
    {
        if (CurrentState != GameState.Playing) return;

        CurrentState = GameState.StageClear;
        Debug.Log("Stage Cleared! 결승선을 통과했습니다.");

        // 데이터 저장 로직
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.CurrentData.currentStageLevel++;
            SaveManager.Instance.SaveData();
        }

        // 여기에 승리 UI를 띄우는 코드를 추가할 예정입니다.
    }
}
