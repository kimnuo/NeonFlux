#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Editor helper to create a basic Leaderboard UI and hook it to MainMenuUI
public static class LeaderboardSetupEditor
{
    [MenuItem("NeonFlux/Setup Leaderboard UI")]
    public static void SetupLeaderboardUI()
    {
        // Ensure a scene canvas exists
        GameObject canvasGO = GameObject.Find("MainMenuCanvas");
        if (canvasGO == null)
        {
            canvasGO = new GameObject("MainMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas c = canvasGO.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            Debug.Log("Created MainMenuCanvas");
        }

        // Ensure an EventSystem exists
        if (GameObject.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
            Debug.Log("Created EventSystem");
        }

        // Create LeaderboardPanel
        Transform canvasT = canvasGO.transform;
        GameObject panel = new GameObject("LeaderboardPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasT, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(760f, 420f);
        rect.anchoredPosition = Vector2.zero;
        var img = panel.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.7f);

        // Create ContentRoot
        GameObject contentRoot = new GameObject("LeaderboardContent", typeof(RectTransform));
        contentRoot.transform.SetParent(panel.transform, false);
        RectTransform crect = contentRoot.GetComponent<RectTransform>();
        crect.anchorMin = new Vector2(0f, 0f);
        crect.anchorMax = new Vector2(1f, 1f);
        crect.offsetMin = new Vector2(10f, 10f);
        crect.offsetMax = new Vector2(-10f, -10f);

        // Add VerticalLayoutGroup and ContentSizeFitter
        var vlg = contentRoot.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.spacing = 6f;
        var csf = contentRoot.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Create a simple entry prefab in Assets/01_Prefabs
        string prefabDir = "Assets/01_Prefabs";
        if (!System.IO.Directory.Exists(prefabDir)) System.IO.Directory.CreateDirectory(prefabDir);

        GameObject entry = new GameObject("LeaderboardEntryPrefab", typeof(RectTransform));
        var et = entry.GetComponent<RectTransform>();
        et.sizeDelta = new Vector2(720f, 28f);
        GameObject textGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textGO.transform.SetParent(entry.transform, false);
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0f, 0f);
        textRT.anchorMax = new Vector2(1f, 1f);
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
        var txt = textGO.GetComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 20;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleLeft;

        // Save prefab
        string prefabPath = prefabDir + "/LeaderboardEntry.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(entry, prefabPath);
        GameObject.DestroyImmediate(entry);

        if (prefab == null)
        {
            Debug.LogError("Failed to create prefab at " + prefabPath);
            return;
        }

        // Attach LeaderboardUI script to panel and set fields
        var lbScript = AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/01_Scripts/UI/LeaderboardUI.cs");
        if (lbScript != null && lbScript.GetClass() != null)
        {
            var lbComp = panel.AddComponent(lbScript.GetClass());
            var compObj = (Component)lbComp;
            var panelField = compObj.GetType().GetField("panel");
            var contentField = compObj.GetType().GetField("contentRoot");
            var prefabField = compObj.GetType().GetField("entryPrefab");
            if (panelField != null) panelField.SetValue(compObj, panel);
            if (contentField != null) contentField.SetValue(compObj, contentRoot.transform);
            if (prefabField != null) prefabField.SetValue(compObj, prefab);
        }
        else
        {
            Debug.LogWarning("Could not find LeaderboardUI.cs to attach. Please attach LeaderboardUI manually and assign fields.");
        }

        // Find or create MainMenuUI holder and assign leaderboardUI reference
        GameObject mainMenuGO = GameObject.Find("MainMenuUI");
        if (mainMenuGO == null)
        {
            mainMenuGO = new GameObject("MainMenuUI");
            mainMenuGO.transform.SetParent(canvasT, false);
        }

        var mmScript = AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/01_Scripts/UI/MainMenuUI.cs");
        if (mmScript != null && mmScript.GetClass() != null)
        {
            var mmComp = mainMenuGO.AddComponent(mmScript.GetClass());
            var lbCompOnPanel = panel.GetComponent(lbScript != null ? lbScript.GetClass() : typeof(MonoBehaviour));
            var lbFieldOnMM = mmComp.GetType().GetField("leaderboardUI");
            if (lbFieldOnMM != null && lbCompOnPanel != null)
            {
                lbFieldOnMM.SetValue(mmComp, lbCompOnPanel);
            }
        }
        else
        {
            Debug.LogWarning("Could not find MainMenuUI.cs to attach. Please add MainMenuUI and assign LeaderboardUI reference.");
        }

        // Hide panel by default
        panel.SetActive(false);

        // Mark scene dirty
        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();

        Debug.Log("Leaderboard UI setup complete. Panel created under MainMenuCanvas. Assign MainMenu button to MainMenuUI.OnLeaderboardClicked().");
    }
}
#endif
