---
name: Unity Runtime Auto-UI Builder
description: Create a runtime MonoBehaviour that auto-generates and wires all game UI screens at startup, ensuring buttons work regardless of scene setup
source: auto-skill
extracted_at: '2026-06-09T15:10:00.000Z'
---

## When to Use
- Scene has incomplete or missing UI (no buttons, no LeaderboardPanel, etc.)
- Editor wiring is unreliable or buttons don't respond despite existing in hierarchy
- Want guaranteed UI availability on Android builds without manual scene setup
- Need centralized control over all UI creation and cross-referencing

## Architecture Pattern

### 1. AutoUIBuilder MonoBehaviour

Create a runtime script that builds all UI on `Awake()`:

```csharp
public class AutoUIBuilder : MonoBehaviour
{
    public static AutoUIBuilder Instance { get; private set; }

    private Canvas _canvas;
    private LeaderboardUI _leaderboardUI;
    private MainMenuUI _mainMenuUI;
    private GameObject _leaderboardPanel;
    private GameObject _mainMenuRoot;
    private GameObject _gameOverPanel;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        EnsureCanvas();
        EnsureEventSystem();
        BuildMainMenu();
        BuildLeaderboard();
        BuildGameOver();
        WireReferences();
    }
```

### 2. GameManager Integration

Have GameManager auto-create AutoUIBuilder if missing:

```csharp
protected override void Awake()
{
    base.Awake();

    AutoUIBuilder uiBuilder = FindObjectOfType<AutoUIBuilder>();
    if (uiBuilder == null)
    {
        GameObject uiBuilderGO = new GameObject("AutoUIBuilder");
        uiBuilder = uiBuilderGO.AddComponent<AutoUIBuilder>();
    }

    CurrentState = GameState.MainMenu;
    ApplyStateVisuals();
}
```

### 3. Idempotent Build Pattern

Always check if UI elements exist before creating:

```csharp
private void BuildMainMenu()
{
    _mainMenuRoot = GameObject.Find("MenuRoot");
    bool hasLeaderboardButton = false;

    if (_mainMenuRoot != null)
    {
        Button[] buttons = _mainMenuRoot.GetComponentsInChildren<Button>(true);
        foreach (var b in buttons)
        {
            if (b.name.Contains("Leaderboard")) hasLeaderboardButton = true;
        }
    }
    else
    {
        _mainMenuRoot = new GameObject("MenuRoot", typeof(RectTransform));
        _mainMenuRoot.transform.SetParent(_canvas.transform, false);
        // Setup anchors to fill screen...
    }

    if (!hasLeaderboardButton)
    {
        CreateButton("LeaderboardButton", _mainMenuRoot.transform, ...);
    }
}
```

### 4. Deep Child Search for Wiring

Use `GetComponentsInChildren` when `transform.Find()` might not work:

```csharp
private static Button FindChildButton(GameObject parent, string name)
{
    if (parent == null) return null;

    // Try direct child first
    Transform t = parent.transform.Find(name);
    if (t != null) return t.GetComponent<Button>();

    // Search all descendants
    Button[] buttons = parent.GetComponentsInChildren<Button>(true);
    foreach (var btn in buttons)
    {
        if (btn.name == name) return btn;
    }
    return null;
}
```

### 5. Reflection for Private SerializedFields

```csharp
private void WireReferences()
{
    // Wire private [SerializeField] fields
    var hsField = typeof(MainMenuUI).GetField("highScoreText",
        BindingFlags.NonPublic | BindingFlags.Instance);
    if (hsField != null) hsField.SetValue(_mainMenuUI, _highScoreText);

    // Wire GameManager private fields
    if (GameManager.Instance != null && _gameOverPanel != null)
    {
        var goField = typeof(GameManager).GetField("gameOverUI",
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (goField != null) goField.SetValue(GameManager.Instance, _gameOverPanel);
    }
}
```

### 6. Button Wiring with Logging

```csharp
private void WireButton(string name, System.Action action, GameObject parent)
{
    if (parent == null || action == null) return;
    Button btn = FindChildButton(parent, name);
    if (btn != null)
    {
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => action());
        Debug.Log("[AutoUIBuilder] Wired button: " + name);
    }
    else
    {
        Debug.LogWarning("[AutoUIBuilder] Could not find button: " + name);
    }
}
```

## Key Considerations

| Aspect | Detail |
|--------|--------|
| **Singleton** | Use `Instance` pattern + `DontDestroyOnLoad` if scene loads change |
| **Idempotency** | Check `GameObject.Find()` and `GetComponentsInChildren` before creating |
| **Execution Order** | AutoUIBuilder `Awake` must run after GameManager creates it |
| **Deep Search** | Use `GetComponentsInChildren` not just `transform.Find` for nested hierarchies |
| **Debug Logging** | Log every wire operation to diagnose missing connections quickly |
| **Fallback** | If scene has partial UI, build only what's missing |
| **DateTime import** | `LeaderboardUI.cs` needs `using System;` for `DateTime.Parse` — missing import causes CS0246/CS0103 |
| **Button anchoring** | Check existing MenuRoot anchor style. If MenuRoot children use `anchorMin/Max = (0.5, 1)` (top-anchored), new buttons must use the same pattern with negative Y values, NOT `(0.5, 0.5)` (center-anchored) |

## Common Pitfalls

1. **Buttons not found**: `transform.Find()` only searches direct children. Use `GetComponentsInChildren` for nested UI
2. **Null references**: Log each component reference status during `WireReferences()` to pinpoint failures
3. **Duplicate creation**: Always check existence before creating to avoid duplicates on scene reloads
4. **Execution order**: If AutoUIBuilder runs before SaveManager, `UpdateHighScoreDisplay()` will fail — ensure load order or add null checks

## Verification Steps
1. Run game and check console for `[AutoUIBuilder]` logs
2. Verify all "Wired button" messages appear
3. Click each button and confirm action executes
4. Check leaderboard panel shows/hides correctly
5. Verify no duplicate UI elements in hierarchy
