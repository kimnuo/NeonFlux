---
name: Unity Capture Start Position In Start Not Awake
description: Capture player start position in Start() instead of Awake() because other systems may move the player between Awake and the first frame
source: auto-skill
extracted_at: '2026-06-10T12:19:21.982Z'
---

## When to Use
- `PlayerController.ResetToStartState()` resets player to (0,0,0) instead of the actual scene position
- Player prefab is placed at a non-origin position in the scene but starts at origin at runtime
- `_startPosition = transform.position` in `Awake()` captures (0,0,0) but the player is later moved by another system

## Root Cause

Unity's execution order:
1. **Awake()** runs — `transform.position` may still be (0,0,0) if another script hasn't placed the player yet
2. **Other Awake()** runs — another script moves the player to actual position
3. **Start()** runs — player is now at correct position

If you capture `_startPosition` in `Awake()`, you get (0,0,0). The fix is to capture in `Start()`.

## Solution

Move start position capture from `Awake()` to `Start()`:

```csharp
// Awake() — REMOVE these lines:
// _startPosition = transform.position;
// _startRotation = transform.rotation;

// Start() — ADD here:
private void Start()
{
    // Capture actual position after scene setup (Awake runs too early)
    _startPosition = transform.position;
    _startRotation = transform.rotation;
    Debug.Log($"[PlayerController] Start: captured startPos={_startPosition}");

    // ... rest of Start() logic ...
}
```

## Why This Matters

`ResetToStartState()` uses these captured values:

```csharp
public void ResetToStartState()
{
    transform.position = _startPosition;  // ← If this is (0,0,0), player resets to wrong place
    transform.rotation = _startRotation;
    // ...
}
```

When `GoToMainMenu()` → `StartGame()` → `ResetToStartState()` is called, the player must return to the **actual** spawn point, not (0,0,0).

## Verification

1. On game start, console shows `[PlayerController] Start: captured startPos=(27.55, -196.50, 674.62)` (actual scene position, not 0,0,0)
2. After game over → main menu → start game, player spawns at the correct position
3. `ResetToStartState()` moves player to the captured start position, not origin

## Related: Out of Bounds Forced Reset

When the player goes out of bounds, force them to (0,0,0) to prevent the "stuck at out-of-bounds position" problem on restart:

```csharp
private void TriggerOutOfBounds()
{
    _isOutOfBounds = true;

    if (_rb != null)
    {
        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.isKinematic = true;
    }

    // Force reset to origin so restart doesn't spawn at out-of-bounds position
    transform.position = Vector3.zero;
    transform.rotation = Quaternion.identity;
    _yawAngle = 0f;

    GameManager.Instance?.SetGameOver("플레이어가 코스를 이탈했습니다.");
}
```

Combined with `_startPosition` captured in `Start()`, this ensures:
- First play: start at actual scene position (captured in Start)
- Out of bounds: forced to (0,0,0), which becomes the new effective start for subsequent restarts
