---
name: Unity UI Dynamic Panel Fallback
description: When a pre-created UI panel refuses SetActive(true) or has broken references, dynamically create the panel at runtime inside the Show* method as a reliable fallback
source: auto-skill
extracted_at: '2026-06-10T12:19:21.982Z'
---

## When to Use
- `panel.SetActive(true)` is called but `activeSelf` remains `false` (no error, just silently ignored)
- Panel hierarchy is corrupted or references are stale (text/button fields are null)
- AutoUIBuilder created the panel but it won't activate when the game state changes
- Need a reliable "always works" fallback for critical UI (Game Over, Stage Clear, etc.)

## Root Causes Observed
1. **Unity silently ignores SetActive(true)**: Even with Canvas and parents active, calling `SetActive(true)` on a specific child panel can fail with no error. The panel stays `activeSelf=false`.
2. **Stale references**: AutoUIBuilder wires references at startup, but if the panel GameObject is later modified (children renamed/removed), the wired `Text`/`Button` fields become null.
3. **Component order matters**: `GetComponent<MonoBehaviour>()` returns the first component (often `Image`), not the UI controller. Use `GetComponents<MonoBehaviour>()` and iterate.

## Solution: Dynamic Panel Creation Fallback

Add `autoCreatePanel` toggle and fallback logic to the UI component:

```csharp
public class StageClearUI : MonoBehaviour
{
    public GameObject panel;
    public Text scoreText;
    // ... other fields ...

    [SerializeField] private bool autoCreatePanel = true;

    public void ShowStageClear(int score, string reason)
    {
        // Step 1: Try to activate existing panel
        if (!TryActivatePanel())
        {
            // Step 2: Fallback — create panel dynamically
            if (autoCreatePanel)
                CreateDynamicPanel();
            else
                return;
        }

        // Step 3: Populate data
        if (scoreText != null) scoreText.text = $"점수: {score}";
        // ...
    }

    private bool TryActivatePanel()
    {
        if (panel == null) return false;

        // Activate all parents in hierarchy
        Transform t = panel.transform;
        while (t != null)
        {
            if (!t.gameObject.activeSelf)
                t.gameObject.SetActive(true);
            t = t.parent;
        }

        panel.SetActive(true);
        return panel.activeSelf && panel.activeInHierarchy;
    }

    private void CreateDynamicPanel()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        // Destroy stale panel if exists
        if (panel != null) Destroy(panel);

        panel = new GameObject("StageClearPanel_Dynamic", typeof(RectTransform), typeof(Image));
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.SetParent(canvas.transform, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(750f, 800f);
        panel.GetComponent<Image>().color = new Color(0f, 0.3f, 0f, 0.92f);

        // Create all UI elements dynamically
        CreateText("Title", "STAGE CLEAR", new Vector2(0f, 300f), new Vector2(600f, 90f), 60, new Color(0f, 1f, 0.5f));
        scoreText = CreateText("ScoreText", "", new Vector2(0f, 180f), new Vector2(500f, 60f), 38, Color.white);
        // ... more text elements ...

        mainMenuButton = CreateButton("MainMenuButton", "메인 메뉴", new Vector2(0f, -150f), new Color(0.4f, 0.4f, 0.4f));
        mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        // ... more buttons ...

        panel.SetActive(true);
    }

    private Text CreateText(string name, string text, Vector2 anchoredPos, Vector2 size, int fontSize, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(panel.transform, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        Text txt = go.GetComponent<Text>();
        txt.text = text;
        txt.fontSize = fontSize;
        txt.color = color;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        txt.raycastTarget = false;
        return txt;
    }

    private Button CreateButton(string name, string text, Vector2 anchoredPos, Color bgColor)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(panel.transform, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(280f, 60f);

        Image img = go.GetComponent<Image>();
        img.color = bgColor;

        Button btn = go.GetComponent<Button>();
        btn.targetGraphic = img;

        CreateText(name + "Text", text, Vector2.zero, new Vector2(280f, 60f), 26, Color.white);
        return btn;
    }
}
```

## GameManager Invocation Pattern

Use `FindObjectOfType<T>()` directly instead of relying on serialized field references:

```csharp
// In GameManager.ApplyStateVisuals():
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

This bypasses the `TryInvokeUIMethod` reflection approach entirely, which can fail if `GetComponent<MonoBehaviour>()` returns the wrong component.

## Key Principles

1. **Always have a fallback**: If the pre-wired panel fails, creating a new one at runtime guarantees the UI appears.
2. **FindObjectOfType > serialized field**: For UI components, `FindObjectOfType<StageClearUI>()` is more reliable than depending on `stageClearUI` field assignment, because it finds the actual component with the method.
3. **Destroy stale references**: If a panel exists but is broken, `Destroy(panel)` before creating a new one prevents duplicate panels.
4. **autoCreatePanel toggle**: Keep the toggle in Inspector so you can disable dynamic creation when the pre-built panel works correctly.

## Verification

1. UI shows correctly even when pre-built panel is broken
2. No duplicate panels created (stale one is destroyed)
3. Buttons function correctly (onClick wired after creation)
4. Text fields populated with correct data
5. Console shows `[StageClearUI] Creating panel dynamically...` when fallback triggers
