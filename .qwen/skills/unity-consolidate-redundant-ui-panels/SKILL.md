---
name: Unity Consolidate Redundant UI Panels
description: Remove duplicate runtime UI panels (e.g., OutOfBoundsPanel + GameOverPanel) and route all triggers through a single panel while preserving trigger conditions
source: auto-skill
extracted_at: '2026-06-10T09:33:34.027Z'
---

## When to Use
- Multiple UI panels show for the same or similar game states (e.g., separate "death" and "out of bounds" popups)
- Want to consolidate to a single panel (e.g., GameOverPanel) while keeping distinct trigger conditions
- Need to remove auto-created UI panels from a MonoBehaviour while preserving underlying game logic

## Procedure

### Step 1: Identify the Common Trigger Flow

In the project, out-of-bounds detection follows this flow:

```
PlayerController.UpdateOutOfBounds(hasGround)
  → TriggerOutOfBounds()
    → GameManager.SetGameOver("이탈했습니다.")
    → ShowOutOfBoundsUI()  // REMOVE THIS
```

`GameManager.SetGameOver()` already triggers `GameOverUI.ShowGameOver()`, so `ShowOutOfBoundsUI()` is redundant.

### Step 2: Remove UI Panel Creation Code

In the MonoBehaviour that auto-creates the panel (e.g., `PlayerController`):

**Remove serialized UI fields** (keep only trigger-related settings):
```csharp
// KEEP - these control WHEN the trigger fires
public float outOfBoundsNoGroundTime = 0.8f;
public float outOfBoundsBelowStartY = -5f;

// REMOVE - these control HOW the panel looks
// public bool autoCreateOutOfBoundsUI = true;
// public Vector2 outOfBoundsPanelSize = ...
// public int outOfBoundsTitleFontSize = 52;
// public string outOfBoundsTitleText = "이탈";
// etc.
```

**Remove private UI fields:**
```csharp
// REMOVE
// private GameObject _outOfBoundsPanel;
// private Text _outOfBoundsScoreText;
// private Button _outOfBoundsHomeButton;
```

**Remove the `Ensure*Panel()` method entirely** (can be 100+ lines of GameObject creation)

**Remove the `Show*UI()` method**

**Remove the `Awake()` call** to `Ensure*Panel()`

### Step 3: Keep the Trigger Logic Intact

The following should remain unchanged:

```csharp
// In FixedUpdate - detection still runs
UpdateOutOfBounds(hasGround);
if (_isOutOfBounds) return;  // still block input

// Trigger method - keep all logic EXCEPT the UI show call
private void TriggerOutOfBounds()
{
    _isOutOfBounds = true;
    _noGroundTimer = 0f;

    if (_rb != null)
    {
        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.isKinematic = true;
    }

    // GameManager.SetGameOver will show the GameOverPanel
    GameManager.Instance?.SetGameOver("플레이어가 코스를 이탈했습니다.");
    // REMOVED: ShowOutOfBoundsUI();
}
```

### Step 4: Clean Up References

Remove all `_outOfBoundsPanel` references from other methods:

```csharp
// ResetToStartState - remove panel hide
public void ResetToStartState()
{
    // REMOVE: if (_outOfBoundsPanel != null) _outOfBoundsPanel.SetActive(false);
    // KEEP: _isOutOfBounds = false;  // state flag still needed
    ...
}

// SetGameplayUIVisible - remove panel visibility
public void SetGameplayUIVisible(bool visible)
{
    // REMOVE: if (_outOfBoundsPanel != null) _outOfBoundsPanel.SetActive(visible && _isOutOfBounds);
    ...
}

// AddScore - remove score text update
private void AddScore(int amount)
{
    ...
    // REMOVE: if (_outOfBoundsScoreText != null) ...
}
```

### Step 5: Centralize Main Menu Reset in GameManager

If the removed panel had button behavior that resets the player before going to main menu, centralize this in `GameManager.GoToMainMenu()` instead of duplicating in each UI component:

```csharp
// In GameManager.cs
public void GoToMainMenu()
{
    ResetStageObjects();

    PlayerController playerController = FindObjectOfType<PlayerController>();
    if (playerController != null)
    {
        playerController.ResetToStartState();
    }

    CurrentState = GameState.MainMenu;
    _currentGameScore = 0;
    ApplyStateVisuals();
}
```

Then ALL UI "Main Menu" button handlers become identical and simple:

```csharp
// GameOverUI.cs, StageClearUI.cs, etc. - all the same
private void OnMainMenuClicked()
{
    if (GameManager.Instance != null)
    {
        GameManager.Instance.GoToMainMenu();
    }
}
```

This avoids duplicating `FindObjectOfType<PlayerController>().ResetToStartState()` in every UI component's main menu handler.

## What to Keep vs Remove

| Keep | Remove |
|------|--------|
| Trigger detection logic (`UpdateOutOfBounds`) | `Ensure*Panel()` method |
| State flag (`_isOutOfBounds`) | Private UI fields (`_panel`, `_text`, `_button`) |
| Trigger condition fields (`noGroundTime`, `belowStartY`) | UI styling fields (font sizes, colors, panel size) |
| `GameManager.SetGameOver()` call | `Show*UI()` method |
| `_isOutOfBounds` early-return in FixedUpdate | `_panel.SetActive()` calls everywhere |
| `_isOutOfBounds` check in scoring | Score text update for removed panel |

## Verification

1. Player goes out of bounds → GameOverPanel shows with "코스 이탈" reason
2. GameOverPanel's "메인 메뉴" button resets player and returns to main menu
3. No compile errors for removed fields/methods
4. `_isOutOfBounds` flag still prevents input and scoring when triggered
5. No duplicate panels appear in hierarchy at runtime
