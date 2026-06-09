---
name: Unity UI Auto-Create Editor Tool
description: Create an editor script that auto-generates complete game UI screens (main menu, game over, leaderboard) with proper component wiring
source: auto-skill
extracted_at: '2026-06-09T15:05:00.000Z'
---

## When to Use
- Need to quickly create game UI screens in Unity without manual inspector wiring
- Building a mobile game with multiple screens (main menu, game over, stage clear, leaderboard)
- Want to ensure consistent UI structure across the project

## Architecture Pattern

### 1. Editor Script Setup
Create an editor script in `Assets/Editor/` with `#if UNITY_EDITOR` guard:

```csharp
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public static class NeonFluxUISetupEditor
{
    [MenuItem("NeonFlux/Setup Complete UI")]
    public static void SetupCompleteUI()
    {
        // Ensure canvas and EventSystem
        GameObject canvasGO = EnsureCanvas("HUD_Canvas");
        EnsureEventSystem();
        
        Transform canvasT = canvasGO.transform;
        
        // Create each screen
        SetupMainMenu(canvasT);
        SetupLeaderboardPanel(canvasT);
        SetupGameOverUI(canvasT);
        
        // Wire GameManager and cross-references
        WireGameManager();
        
        EditorSceneManager.MarkAllScenesDirty();
        Debug.Log("UI setup complete.");
    }
}
#endif
```

### 2. Helper Methods Pattern

```csharp
private static GameObject CreateButton(string name, Transform parent, string text, Vector2 anchoredPos, Vector2 size, Color bgColor)
{
    GameObject btnGO = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
    btnGO.transform.SetParent(parent, false);
    
    RectTransform rect = btnGO.GetComponent<RectTransform>();
    rect.anchorMin = new Vector2(0.5f, 0.5f);
    rect.anchorMax = new Vector2(0.5f, 0.5f);
    rect.anchoredPosition = anchoredPos;
    rect.sizeDelta = size;
    btnGO.GetComponent<Image>().color = bgColor;
    
    // Button text
    GameObject txtGO = CreateTextObject("Text", btnGO.transform, Vector2.zero, size, 28, Color.white, TextAnchor.MiddleCenter, text);
    
    return btnGO;
}

private static GameObject CreateTextObject(string name, Transform parent, Vector2 anchoredPos, Vector2 size, int fontSize, Color color, TextAnchor alignment, string text)
{
    GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
    go.transform.SetParent(parent, false);
    
    RectTransform rect = go.GetComponent<RectTransform>();
    rect.anchorMin = new Vector2(0.5f, 1f);
    rect.anchorMax = new Vector2(0.5f, 1f);
    rect.pivot = new Vector2(0.5f, 1f);
    rect.anchoredPosition = anchoredPos;
    rect.sizeDelta = size;
    
    Text txt = go.GetComponent<Text>();
    txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    txt.fontSize = fontSize;
    txt.color = color;
    txt.alignment = alignment;
    txt.text = text;
    
    return go;
}
```

### 3. Component Wiring Pattern

```csharp
private static void WireGameManager()
{
    if (GameManager.Instance != null)
    {
        var goField = typeof(GameManager).GetField("gameOverUI", BindingFlags.NonPublic | BindingFlags.Instance);
        if (goField != null) goField.SetValue(GameManager.Instance, GameObject.Find("GameOverPanel"));
    }
    
    // Wire leaderboard to MainMenuUI
    MainMenuUI mmUI = FindObjectOfType<MainMenuUI>();
    if (mmUI != null)
    {
        mmUI.leaderboardUI = FindObjectOfType<LeaderboardUI>();
    }
}
```

### 4. Runtime UI Creation Pattern (for StageClear in PlayerController)

For UI created at runtime (not via editor), use the same pattern but in `EnsureXXX()` methods:

```csharp
private void EnsureStageClearPanel()
{
    if (_stageClearPanel != null) return;
    
    Canvas canvas = GetOrCreateHudCanvas();
    EnsureEventSystem();
    
    GameObject panelGo = new GameObject("StageClearPanel", typeof(RectTransform), typeof(Image));
    // ... setup RectTransform, Image, child elements ...
    
    // Leaderboard button
    GameObject lbButtonGo = new GameObject("LeaderboardButton", typeof(RectTransform), typeof(Image), typeof(Button));
    // ... setup ...
    lbButtonGo.GetComponent<Button>().onClick.AddListener(OnLeaderboardClicked);
    
    _stageClearPanel = panelGo;
    _stageClearPanel.SetActive(false);
}
```

## Key Considerations

| Aspect | Detail |
|--------|--------|
| **Canvas Setup** | Use ScreenSpaceOverlay with ScaleWithScreenSize for mobile |
| **Reference Resolution** | 1080x1920 for portrait mobile games |
| **EventSystem** | Must exist for button clicks to work |
| **Font** | `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")` works without external assets |
| **Idempotency** | Check if objects exist before creating (`GameObject.Find("Name") == null`) |
| **Scene Dirty** | Call `EditorSceneManager.MarkAllScenesDirty()` after editor changes |

## Common Pitfalls to Avoid

1. **Buttons not responding**: Ensure EventSystem exists in scene
2. **UI not visible**: Check Canvas render mode and RectTransform anchors
3. **Text not showing**: Verify font is assigned and color is not transparent
4. **Inspector wiring forgotten**: Editor script should auto-wire all cross-references
5. **Private field access**: Use reflection for `[SerializeField] private` fields:
   ```csharp
   var field = typeof(GameManager).GetField("gameOverUI", BindingFlags.NonPublic | BindingFlags.Instance);
   if (field != null) field.SetValue(GameManager.Instance, value);
   ```

## Verification Steps
1. Run editor menu command: `NeonFlux → Setup Complete UI`
2. Verify all panels created in hierarchy
3. Enter Play mode and test each button
4. Check that GameManager fields are properly assigned
5. Test leaderboard shows/hides correctly