---
name: Unity Centralize Main Menu Reset in GameManager
description: Centralize player state reset (position, score, flags) inside GameManager.GoToMainMenu() so all UI buttons delegate to a single method instead of duplicating ResetToStartState() calls
source: auto-skill
extracted_at: '2026-06-10T12:15:00.000Z'
---

## When to Use
- Multiple UI components (GameOverUI, StageClearUI, etc.) each have their own "Main Menu" button handler
- Each handler independently calls `FindObjectOfType<PlayerController>().ResetToStartState()` before `GameManager.GoToMainMenu()`
- Want a single source of truth for "what happens when returning to main menu"
- Need to ensure score resets consistently when returning to main menu

## Problem

Before centralization, each UI component duplicates the reset logic:

```csharp
// GameOverUI.cs
private void OnMainMenuClicked()
{
    PlayerController player = FindObjectOfType<PlayerController>();
    if (player != null) player.ResetToStartState();  // DUPLICATE
    GameManager.Instance?.GoToMainMenu();
}

// StageClearUI.cs
private void OnMainMenuClicked()
{
    PlayerController player = FindObjectOfType<PlayerController>();
    if (player != null) player.ResetToStartState();  // DUPLICATE
    GameManager.Instance?.GoToMainMenu();
}
```

This means:
- Every new UI component that has a "Main Menu" button must remember to reset the player
- If reset logic changes, every button handler must be updated
- Easy to forget the reset in one place, causing stale state when returning to main menu

## Solution

Move the reset into `GameManager.GoToMainMenu()`:

```csharp
// GameManager.cs
public void GoToMainMenu()
{
    ResetStageObjects();

    PlayerController playerController = FindObjectOfType<PlayerController>();
    if (playerController != null)
    {
        playerController.ResetToStartState();
    }

    CurrentState = GameState.MainMenu;
    _currentGameScore = 0;  // Also reset the tracked score
    ApplyStateVisuals();
}
```

Then simplify ALL UI button handlers:

```csharp
// GameOverUI.cs - simplified
private void OnMainMenuClicked()
{
    if (GameManager.Instance != null)
    {
        GameManager.Instance.GoToMainMenu();
    }
}

// StageClearUI.cs - simplified
private void OnMainMenuClicked()
{
    if (GameManager.Instance != null)
    {
        GameManager.Instance.GoToMainMenu();
    }
}
```

## What GoToMainMenu() Should Handle

| Responsibility | Code |
|---------------|------|
| Reset stage objects (obstacles, pickups) | `ResetStageObjects()` |
| Reset player position, rotation, velocity | `playerController.ResetToStartState()` |
| Reset game state | `CurrentState = GameState.MainMenu` |
| Reset tracked score | `_currentGameScore = 0` |
| Apply UI visibility | `ApplyStateVisuals()` |

## PlayerController.ResetToStartState() Should Handle

| Responsibility | Code |
|---------------|------|
| Hide stage clear panel | `_stageClearPanel?.SetActive(false)` (if still owned by PlayerController) |
| Reset state flags | `_isOutOfBounds = false; _isStageCleared = false;` |
| Reset timers | `_noGroundTimer = 0f;` |
| Reset rigidbody | `_rb.velocity = Vector3.zero; _rb.angularVelocity = Vector3.zero; _rb.isKinematic = false;` |
| Restore position | `transform.position = _startPosition; transform.rotation = _startRotation;` |
| Restore yaw | `_yawAngle = _startRotation.eulerAngles.y;` |
| Reset score tracking | `ResetScoreTracking();` |

## Verification

1. Game Over → Main Menu → Start Game: player starts at initial position
2. Stage Clear → Main Menu → Start Game: player starts at initial position
3. Score shows 0 after returning to main menu and starting again
4. No stale `_isOutOfBounds` or `_isStageCleared` flags carry over
5. All "Main Menu" buttons behave identically regardless of which screen they're on
