using UnityEngine;

public enum GameState { MainMenu, Playing, StageClear, GameOver }

public class GameManager : Singleton<GameManager>
{
    [Header("Main Menu")]
    [SerializeField] private string mainMenuCanvasName = "MainMenuCanvas";
    [SerializeField] private string mainMenuRootName = "MenuRoot";

    public GameState CurrentState { get; private set; }

    private GameObject _mainMenuRoot;

    protected override void Awake()
    {
        base.Awake();
        CurrentState = GameState.MainMenu;
        CacheMainMenuRoot();
        ApplyStateVisuals();
    }

    public void StartGame()
    {
        PlayerController playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            playerController.ResetToStartState();
        }

        CurrentState = GameState.Playing;
        ApplyStateVisuals();
    }

    public void GoToMainMenu()
    {
        CurrentState = GameState.MainMenu;
        ApplyStateVisuals();
    }

    public void EndGame()
    {
        if (CurrentState != GameState.Playing) return;

        CurrentState = GameState.GameOver;
        Debug.Log("Game Over! 장애물에 충돌했습니다.");

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveData();
        }

        ApplyStateVisuals();
    }

    public void SetGameOver(string reason)
    {
        if (CurrentState != GameState.Playing) return;

        CurrentState = GameState.GameOver;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            Debug.Log(reason);
        }

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveData();
        }

        ApplyStateVisuals();
    }

    public void CompleteStage()
    {
        if (CurrentState != GameState.Playing) return;

        CurrentState = GameState.StageClear;
        Debug.Log("Stage Cleared! 결승선을 통과했습니다.");

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.CurrentData.currentStageLevel++;
            SaveManager.Instance.SaveData();
        }

        ApplyStateVisuals();
    }

    private void CacheMainMenuRoot()
    {
        GameObject canvasObject = GameObject.Find(mainMenuCanvasName);
        if (canvasObject == null)
        {
            return;
        }

        Transform menuRoot = canvasObject.transform.Find(mainMenuRootName);
        if (menuRoot != null)
        {
            _mainMenuRoot = menuRoot.gameObject;
        }
    }

    private void ApplyStateVisuals()
    {
        if (_mainMenuRoot == null)
        {
            CacheMainMenuRoot();
        }

        if (_mainMenuRoot != null)
        {
            _mainMenuRoot.SetActive(CurrentState == GameState.MainMenu);
        }

        PlayerController playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            playerController.SetGameplayUIVisible(CurrentState == GameState.Playing);
        }
    }
}
