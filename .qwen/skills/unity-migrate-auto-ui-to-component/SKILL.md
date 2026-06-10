---
name: Unity Migrate Auto-Created UI to Dedicated Component
description: Migrate a UI panel from auto-creation code in a gameplay MonoBehaviour (e.g., PlayerController) to a dedicated UI component class managed by GameManager, keeping GameManager as the single source of UI state
source: auto-skill
extracted_at: '2026-06-10T10:42:05.344Z'
---

## When to Use
- A gameplay MonoBehaviour (e.g., `PlayerController`) contains 100+ lines of `Ensure*Panel()` code that creates UI GameObjects at runtime
- Want to centralize UI management in `GameManager` similar to how `GameOverUI` is handled
- Need a dedicated UI component class with clear public fields for inspector wiring
- Want AutoUIBuilder and editor scripts to create and wire the UI consistently

## Architecture

Before migration:
```
PlayerController
  ├── EnsureStageClearPanel()  ← 150 lines of GameObject creation
  ├── _stageClearPanel, _stageClearNextButton, _stageClearScoreText
  ├── SetStageClearUIVisible()
  └── TriggerStageClear() calls EnsureStageClearPanel + shows panel
```

After migration:
```
GameManager
  ├── stageClearUI (serialized field)
  └── ApplyStateVisuals() → calls ShowStageClear(score, reason) via reflection

StageClearUI (dedicated component)
  ├── panel, scoreText, messageText, highScoreText, lastScoreText
  ├── nextStageButton, mainMenuButton, leaderboardButton
  └── ShowStageClear(score, reason)

AutoUIBuilder
  └── BuildStageClear() + WireReferences() → creates and wires StageClearUI

PlayerController
  ├── TriggerStageClear()  ← only sets state, calls GameManager.CompleteStage()
  └── (no UI creation code)
```

## Procedure

### Step 1: Create the Dedicated UI Component

Create `StageClearUI.cs` mirroring the pattern of an existing UI component (e.g., `GameOverUI.cs`):

```csharp
using UnityEngine;
using UnityEngine.UI;

public class StageClearUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject panel;
    public Text scoreText;
    public Text messageText;      // positive message instead of death reason
    public Text highScoreText;
    public Text lastScoreText;

    [Header("Buttons")]
    public Button nextStageButton;
    public Button mainMenuButton;
    public Button leaderboardButton;

    [Header("Settings")]
    [SerializeField] private string nextStageButtonText = "다음 단계";
    [SerializeField] private string mainMenuButtonText = "메인 메뉴";
    [SerializeField] private string messageFormat = "{0}";

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);

        // Wire button events
        if (nextStageButton != null)
        {
            nextStageButton.onClick.RemoveAllListeners();
            nextStageButton.onClick.AddListener(OnNextStageClicked);
        }
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

    public void ShowStageClear(int score, string reason)
    {
        if (panel != null) panel.SetActive(true);

        if (scoreText != null)
            scoreText.text = string.Format("점수: {0}", score);

        if (messageText != null)
            messageText.text = string.Format(messageFormat, GetMessageLabel(reason));

        if (highScoreText != null && SaveManager.Instance != null)
            highScoreText.text = string.Format("최고 점수: {0}", SaveManager.Instance.HighScore);

        if (lastScoreText != null && SaveManager.Instance != null)
            lastScoreText.text = string.Format("기록 점수: {0}", GetLastRecordedScore());
    }

    private string GetMessageLabel(string reason)
    {
        if (string.IsNullOrEmpty(reason)) return "스테이지 클리어!";
        // Map reasons to positive messages
        return "스테이지 클리어!";
    }

    private int GetLastRecordedScore()
    {
        if (SaveManager.Instance == null) return 0;
        var lb = SaveManager.Instance.Leaderboard;
        if (lb != null && lb.Count > 0) return lb[0].score;
        return 0;
    }

    private void OnNextStageClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.GoToNextStage();
    }

    private void OnMainMenuClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.GoToMainMenu();
    }

    private void OnLeaderboardClicked()
    {
        LeaderboardUI lbUI = FindObjectOfType<LeaderboardUI>();
        if (lbUI != null) { lbUI.RefreshLeaderboard(); lbUI.Show(); }
    }
}
```

### Step 2: Update GameManager

Add the UI field and state tracking:

```csharp
// Add serialized field
[Header("UI References")]
[SerializeField] private GameObject gameOverUI;
[SerializeField] private GameObject stageClearUI;  // NEW
[SerializeField] private GameObject mainMenuUI;

// Add reason tracking
private string _lastGameOverReason;
private string _lastStageClearReason;  // NEW
```

Update `CompleteStage()` to store the reason:

```csharp
public void CompleteStage()
{
    if (CurrentState != GameState.Playing) return;

    CurrentState = GameState.StageClear;
    _lastStageClearReason = "clear";  // NEW

    if (SaveManager.Instance != null)
    {
        RecordGameScore("clear");
        SaveManager.Instance.CurrentData.currentStageLevel++;
        SaveManager.Instance.SaveData();
    }

    ApplyStateVisuals();
}
```

Update `HideAllUI()`:

```csharp
private void HideAllUI()
{
    if (mainMenuUI != null) mainMenuUI.SetActive(false);
    if (gameOverUI != null) gameOverUI.SetActive(false);
    if (stageClearUI != null) stageClearUI.SetActive(false);  // NEW
}
```

Remove PlayerController UI visibility call from `ApplyStateVisuals()`:

```csharp
// OLD:
playerController.SetStageClearUIVisible(CurrentState == GameState.StageClear);

// NEW - remove this line, StageClearUI is handled below
```

Add StageClear handling in `ApplyStateVisuals()` — prefer `FindObjectOfType<T>()` over reflection for reliability:

```csharp
// PREFERRED approach: FindObjectOfType (simple, reliable)
if (CurrentState == GameState.StageClear)
{
    var scUI = FindObjectOfType<StageClearUI>();
    if (scUI != null)
    {
        scUI.ShowStageClear(_currentGameScore, _lastStageClearReason);
    }
    else
    {
        Debug.LogError("No StageClearUI found in scene!");
    }
}
```

**Why FindObjectOfType over reflection?** `GetComponent<MonoBehaviour>()` returns the **first** MonoBehaviour on the GameObject, which is often `Image` (added when creating the panel). Even `GetComponents<MonoBehaviour>()` with iteration can fail if the panel is corrupted or won't activate. `FindObjectOfType<StageClearUI>()` directly finds the component with the method, bypassing all component-order issues.

**Fallback**: If the pre-built panel won't activate (`SetActive(true)` is silently ignored), see the [Unity UI Dynamic Panel Fallback](../unity-ui-dynamic-panel-fallback/SKILL.md) skill for creating the panel dynamically at runtime inside the `Show*` method.

### Step 3: Update AutoUIBuilder

Add fields:

```csharp
[SerializeField] private bool buildStageClear = true;
private StageClearUI _stageClearUI;
private GameObject _stageClearPanel;
```

Add `BuildStageClear()` method (mirroring `BuildGameOver()`):

```csharp
private void BuildStageClear()
{
    _stageClearPanel = GameObject.Find("StageClearPanel");
    bool needsSetup = _stageClearPanel == null;

    if (needsSetup)
    {
        _stageClearPanel = new GameObject("StageClearPanel", typeof(RectTransform), typeof(Image));
        _stageClearPanel.transform.SetParent(_canvas.transform, false);
        RectTransform rect = _stageClearPanel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(750f, 800f);
        rect.anchoredPosition = Vector2.zero;
        // Use green theme for positive feel
        _stageClearPanel.GetComponent<Image>().color = new Color(0f, 0.3f, 0f, 0.92f);

        CreateText("Title", _stageClearPanel.transform,
            new Vector2(0f, 330f), new Vector2(650f, 90f), 60, new Color(0f, 1f, 0.5f), TextAnchor.MiddleCenter).text = "STAGE CLEAR";

        CreateText("ScoreText", _stageClearPanel.transform,
            new Vector2(0f, 210f), new Vector2(550f, 60f), 38, Color.white, TextAnchor.MiddleCenter);

        CreateText("HighScoreText", _stageClearPanel.transform,
            new Vector2(0f, 140f), new Vector2(550f, 50f), 30, Color.yellow, TextAnchor.MiddleCenter);

        CreateText("MessageText", _stageClearPanel.transform,
            new Vector2(0f, 70f), new Vector2(550f, 50f), 26, new Color(0.6f, 1f, 0.6f), TextAnchor.MiddleCenter);

        CreateText("LastScoreText", _stageClearPanel.transform,
            new Vector2(0f, -10f), new Vector2(550f, 50f), 30, new Color(1f, 1f, 0.6f), TextAnchor.MiddleCenter);

        CreateButton("NextStageButton", _stageClearPanel.transform,
            "다음 단계", new Vector2(0f, -130f), new Vector2(300f, 65f), new Color(0f, 0.6f, 0.6f, 1f), 28);
        CreateButton("MainMenuButton", _stageClearPanel.transform,
            "메인 메뉴", new Vector2(0f, -220f), new Vector2(300f, 65f), new Color(0.4f, 0.4f, 0.4f, 0.9f), 28);
        CreateButton("LeaderboardButton", _stageClearPanel.transform,
            "리더보드", new Vector2(0f, -310f), new Vector2(300f, 65f), new Color(0.3f, 0.3f, 0.3f, 0.9f), 28);

        _stageClearPanel.SetActive(false);
    }

    _stageClearUI = _stageClearPanel.GetComponent<StageClearUI>();
    if (_stageClearUI == null) _stageClearUI = _stageClearPanel.AddComponent<StageClearUI>();
}
```

Call in `Awake()`:

```csharp
if (buildGameOver) BuildGameOver();
if (buildStageClear) BuildStageClear();  // NEW
WireReferences();
```

Add wiring in `WireReferences()`:

```csharp
// Stage clear panel -> GameManager
if (GameManager.Instance != null && _stageClearPanel != null)
{
    var scField = typeof(GameManager).GetField("stageClearUI",
        BindingFlags.NonPublic | BindingFlags.Instance);
    if (scField != null) scField.SetValue(GameManager.Instance, _stageClearPanel);
}

// Wire buttons
WireButton("NextStageButton", () => GameManager.Instance?.GoToNextStage(), _stageClearPanel);
WireButton("MainMenuButton", () => GameManager.Instance?.GoToMainMenu(), _stageClearPanel);
WireButton("LeaderboardButton", OnLeaderboardClicked, _stageClearPanel);

// StageClearUI wire
if (_stageClearUI != null)
{
    _stageClearUI.panel = _stageClearPanel;
    _stageClearUI.scoreText = FindChildText(_stageClearPanel, "ScoreText");
    _stageClearUI.highScoreText = FindChildText(_stageClearPanel, "HighScoreText");
    _stageClearUI.messageText = FindChildText(_stageClearPanel, "MessageText");
    _stageClearUI.lastScoreText = FindChildText(_stageClearPanel, "LastScoreText");
    _stageClearUI.nextStageButton = FindChildButton(_stageClearPanel, "NextStageButton");
    _stageClearUI.mainMenuButton = FindChildButton(_stageClearPanel, "MainMenuButton");
    _stageClearUI.leaderboardButton = FindChildButton(_stageClearPanel, "LeaderboardButton");
}
```

### Step 4: Update Editor Setup Script

Add `SetupStageClearUI()` method (mirroring `SetupGameOverUI()`):

```csharp
private static void SetupStageClearUI(Transform canvasT)
{
    GameObject panel = GameObject.Find("StageClearPanel");
    if (panel == null)
    {
        panel = new GameObject("StageClearPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasT, false);
        // ... create text/button children ...
        panel.SetActive(false);
    }

    StageClearUI scUI = panel.GetComponent<StageClearUI>();
    if (scUI == null) scUI = panel.AddComponent<StageClearUI>();

    scUI.panel = panel;
    scUI.scoreText = panel.transform.Find("ScoreText")?.GetComponent<Text>();
    scUI.messageText = panel.transform.Find("MessageText")?.GetComponent<Text>();
    // ... wire remaining fields ...
}
```

Call it in the setup method:

```csharp
SetupGameOverUI(canvasT);
SetupStageClearUI(canvasT);  // NEW
WireGameManager(lbPanel);
```

Update `WireGameManager()`:

```csharp
GameObject scPanel = GameObject.Find("StageClearPanel");
if (scPanel != null)
{
    var scField = typeof(GameManager).GetField("stageClearUI",
        BindingFlags.NonPublic | BindingFlags.Instance);
    if (scField != null) scField.SetValue(GameManager.Instance, scPanel);
}
```

### Step 5: Clean Up the Gameplay MonoBehaviour

In `PlayerController.cs` (or whichever MonoBehaviour had the auto-creation code):

**Remove serialized fields:**
```csharp
// REMOVE ALL:
// public bool autoCreateStageClearUI = true;
// public Vector2 stageClearPanelSize = ...
// public int stageClearTitleFontSize = 52;
// public string stageClearTitleText = "스테이지 클리어";
// etc.
```

**Remove private UI fields:**
```csharp
// REMOVE:
// private GameObject _stageClearPanel;
// private Button _stageClearNextButton;
// private Text _stageClearScoreText;
```

**Remove `Ensure*Panel()` method** (all 100+ lines)

**Remove `Set*UIVisible()` method**

**Remove helper methods** (`GoToNextStage`, `OnStageClearLeaderboardClicked`, `UpdateStageClearScoreText`)

**Simplify `TriggerStageClear()`:**
```csharp
// BEFORE:
private void TriggerStageClear()
{
    _isStageCleared = true;
    if (_rb != null) { /* zero velocity */ }
    GameManager.Instance?.CompleteStage();  // this now handles UI
}

// Keep as-is - GameManager.CompleteStage() + ApplyStateVisuals() handles everything
```

**Clean up `ResetToStartState()`:**
```csharp
// REMOVE: if (_stageClearPanel != null) _stageClearPanel.SetActive(false);
// KEEP: _isStageCleared = false;  // state flag still needed
```

**Remove from `Awake()` font clamping:**
```csharp
// REMOVE:
// stageClearTitleFontSize = Mathf.Max(12, stageClearTitleFontSize);
// stageClearButtonFontSize = Mathf.Max(12, stageClearButtonFontSize);
```

## Verification

1. Game reaches stage clear → StageClearPanel shows with score and positive message
2. "다음 단계" button calls `GameManager.GoToNextStage()`
3. "메인 메뉴" button resets player and returns to main menu
4. No compile errors for removed fields/methods
5. `_isStageCleared` flag still prevents re-triggering
6. No auto-created UI code remains in PlayerController
