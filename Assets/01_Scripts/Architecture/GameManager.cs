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

    [Header("UI References")]
    [SerializeField] private GameObject gameOverUI;
    [SerializeField] private GameObject mainMenuUI;

    private int _currentGameScore;
    private string _lastGameOverReason;

    private GameObject _mainMenuRoot;

    public void SetCurrentScore(int score)
    {
        _currentGameScore = score;
    }

    public void RecordGameScore(string reason)
    {
        _lastGameOverReason = ExtractReasonKey(reason);
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.RecordGameScore(_currentGameScore, _lastGameOverReason);
        }
    }

    private string ExtractReasonKey(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason)) return "unknown";
        string lower = reason.ToLowerInvariant();
        if (lower.Contains("장애물") || lower.Contains("obstacle")) return "obstacle";
        if (lower.Contains("이탈") || lower.Contains("outofbounds") || lower.Contains("outofbounds") || lower.Contains("out of bounds")) return "outOfBounds";
        if (lower.Contains("clear") || lower.Contains("클리어")) return "clear";
        return "other";
    }

    private void HideAllUI()
    {
        if (mainMenuUI != null) mainMenuUI.SetActive(false);
        if (gameOverUI != null) gameOverUI.SetActive(false);
    }

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
        _currentGameScore = 0;
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
            RecordGameScore("obstacle");
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
            RecordGameScore(reason);
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
            RecordGameScore("clear");
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
        _currentGameScore = 0;
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
        // Hide any dedicated UIs first
        HideAllUI();
        if (_mainMenuRoot == null)
        {
            CacheMainMenuRoot();
        }
        // MainMenu visibility
        if (CurrentState == GameState.MainMenu)
        {
            if (mainMenuUI != null)
                mainMenuUI.SetActive(true);
            else if (_mainMenuRoot != null)
                _mainMenuRoot.SetActive(true);
        }
        // Player HUD and stage UI
        PlayerController playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            playerController.SetGameplayUIVisible(CurrentState == GameState.Playing);
            playerController.SetStageClearUIVisible(CurrentState == GameState.StageClear);
        }
        // Game over handling: show GameOver UI and pass data if component supports it
        if (CurrentState == GameState.GameOver)
        {
            if (gameOverUI != null)
            {
                // Try to invoke ShowGameOver(int score, string reason) if present                
                var mb = gameOverUI.GetComponent<MonoBehaviour>();
                if (mb != null)
                {
                    var method = mb.GetType().GetMethod("ShowGameOver", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    if (method != null)
                    {
                        try { method.Invoke(mb, new object[] { _currentGameScore, _lastGameOverReason }); } catch { gameOverUI.SetActive(true); }
                    }
                    else
                    {
                        gameOverUI.SetActive(true);
                    }
                }
                else
                {
                    gameOverUI.SetActive(true);
                }
            }
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

