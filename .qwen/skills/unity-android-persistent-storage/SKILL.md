---
name: Unity Android Persistent Storage
description: Implement persistent local data storage for Unity Android games using JSON serialization and Application.persistentDataPath
source: auto-skill
extracted_at: '2026-06-09T14:47:16.827Z'
---

## When to Use
- Need to persist player data (scores, settings, progress) across app restarts on Android
- Want a simple, no-server solution for single-player games
- Avoiding external storage permissions (Android 10+ scoped storage)

## Architecture Pattern

### 1. Data Structure (ScriptableObject or Serializable Class)
Create a serializable data class with `[Serializable]` attribute:

```csharp
[Serializable]
public class PlayerSaveData
{
    public int highScore;
    public int currentLevel;
    public List<GameEntry> history;
    
    public PlayerSaveData()
    {
        history = new List<GameEntry>();
    }
}
```

**Important**: All fields must be serializable by `JsonUtility` (no properties, only fields; no Dictionary, use lists instead).

### 2. SaveManager Singleton
Implement as a `Singleton<T>` with `DontDestroyOnLoad`:

```csharp
public class SaveManager : Singleton<SaveManager>
{
    private string _savePath;
    public PlayerSaveData CurrentData;
    
    protected override void Awake()
    {
        base.Awake();
        // Android maps this to: /storage/emulated/0/Android/data/<package>/files/
        _savePath = Path.Combine(Application.persistentDataPath, "user_data.json");
        LoadData();
    }
    
    public void SaveData()
    {
        string json = JsonUtility.ToJson(CurrentData);
        File.WriteAllText(_savePath, json);
    }
    
    public void LoadData()
    {
        if (File.Exists(_savePath))
        {
            string json = File.ReadAllText(_savePath);
            CurrentData = JsonUtility.FromJson<PlayerSaveData>(json);
        }
        else
        {
            CurrentData = new PlayerSaveData(); // New user defaults
        }
    }
}
```

### 3. Key Considerations for Android

| Aspect | Detail |
|--------|--------|
| **Path** | `Application.persistentDataPath` requires NO permissions |
| **Lifecycle** | Load on `Awake`, save after every meaningful state change |
| **Data Loss** | Data persists across app restarts but NOT across reinstall |
| **JsonUtility Limitations** | Cannot serialize properties, Dictionary, or nested interfaces |
| **Thread Safety** | File I/O is synchronous; for large data use async patterns |

### 4. Common Pitfalls to Avoid

1. **Don't forget to pass runtime data to the save system**
   - Example bug: PlayerController tracked `_score` but never called `GameManager.SetCurrentScore()` before game over → scores were always 0 in leaderboard
   - Fix: Call `GameManager.Instance.SetCurrentScore(_score)` before `EndGame()` or `CompleteStage()`

2. **Initialize collections in constructor or LoadData**
   - `JsonUtility.FromJson` returns `null` for missing lists, not empty lists
   - Always check: `if (CurrentData.history == null) CurrentData.history = new List<...>();`

3. **UTC vs Local Time for dates**
   - Store as ISO format (`DateTime.UtcNow.ToString("s")`)
   - Convert to local time on display: `DateTime.Parse(date).ToLocalTime()`

4. **ParticleSystem duration warning on Awake**
   - When creating ParticleSystem at runtime in `Awake`, call `Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear)` **before** setting `main.duration`
   - Unity 2022.3+ throws: "Setting the duration while system is still playing is not supported"
   - Fix:
     ```csharp
     ParticleSystem smoke = gameObject.AddComponent<ParticleSystem>();
     smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // Call BEFORE setting main module
     var main = smoke.main;
     main.duration = 1f; // Now safe
     ```

### 5. Verification Steps
- Test on actual Android device (emulator may have different path behavior)
- Verify file exists: `adb shell ls /data/data/<package>/files/`
- Check data persists: close app completely → reopen → verify data loaded
