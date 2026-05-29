using System.Reflection;
using UnityEngine;

public enum GameState { MainMenu, Playing, StageClear, GameOver }

public class GameManager : Singleton<GameManager>
{
    [Header("Main Menu")]
    [SerializeField] private string mainMenuCanvasName = "MainMenuCanvas";
    [SerializeField] private string mainMenuRootName = "MenuRoot";

#if UNITY_STANDALONE_WIN || UNITY_EDITOR
    [Header("Standalone Resolution")]
    [SerializeField] private bool applyStandaloneResolution = true;
    [SerializeField] private bool forceWindowed = true;
    [SerializeField] private int standaloneTargetWidth = 1920;
    [SerializeField] private int standaloneTargetHeight = 1080;
    [SerializeField] private bool clampToDisplay = true;
#endif

    public GameState CurrentState { get; private set; }

    private GameObject _mainMenuRoot;

    protected override void Awake()
    {
        base.Awake();
#if UNITY_STANDALONE_WIN || UNITY_EDITOR
        ApplyStandaloneResolution();
#endif
        CurrentState = GameState.MainMenu;
        CacheMainMenuRoot();
        ApplyStateVisuals();
    }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR
    private void ApplyStandaloneResolution()
    {
        if (Application.isEditor)
        {
            return;
        }

        if (!applyStandaloneResolution)
        {
            return;
        }

        int targetWidth = Mathf.Max(1, standaloneTargetWidth);
        int targetHeight = Mathf.Max(1, standaloneTargetHeight);
        float targetAspect = targetWidth / (float)targetHeight;

        if (clampToDisplay)
        {
            Resolution current = Screen.currentResolution;
            int maxWidth = Mathf.Max(1, current.width);
            int maxHeight = Mathf.Max(1, current.height);

            int adjustedWidth = Mathf.Min(targetWidth, maxWidth);
            int adjustedHeight = Mathf.RoundToInt(adjustedWidth / targetAspect);
            if (adjustedHeight > maxHeight)
            {
                adjustedHeight = Mathf.Min(targetHeight, maxHeight);
                adjustedWidth = Mathf.RoundToInt(adjustedHeight * targetAspect);
            }

            targetWidth = Mathf.Max(1, adjustedWidth);
            targetHeight = Mathf.Max(1, adjustedHeight);
        }

        FullScreenMode mode = forceWindowed ? FullScreenMode.Windowed : Screen.fullScreenMode;
        Screen.SetResolution(targetWidth, targetHeight, mode);
    }
#endif

    public void StartGame()
    {
        ResetStageObjects();

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
        ResetStageObjects();
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

    public void GoToNextStage()
    {
        if (CurrentState != GameState.StageClear) return;

        ResetStageObjects();

        PlayerController playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            playerController.ResetToStartState();
        }

        Debug.Log("임시 처리: 다음 스테이지 대신 현재 씬을 재시작합니다.");
        CurrentState = GameState.Playing;
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
            playerController.SetStageClearUIVisible(CurrentState == GameState.StageClear);
        }
    }

    private void ResetStageObjects()
    {
        MonoBehaviour[] behaviours = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            GameObject obj = behaviour.gameObject;
            if (!obj.scene.IsValid() || !obj.scene.isLoaded)
            {
                continue;
            }

            MethodInfo method = behaviour.GetType().GetMethod(
                "ResetForReplay",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method != null && method.GetParameters().Length == 0)
            {
                method.Invoke(behaviour, null);
            }
        }
    }
}
