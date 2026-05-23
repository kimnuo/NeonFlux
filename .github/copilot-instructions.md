# Copilot instructions for NeonFlux

## Build
- `dotnet build NeonFlux.sln -v minimal`

## Tests
- Full suite (EditMode): `"<UnityEditor>/Unity.exe" -runTests -projectPath . -testPlatform EditMode`
- Single test: `"<UnityEditor>/Unity.exe" -runTests -projectPath . -testPlatform EditMode -testFilter "Namespace.ClassName.TestName"`

## High-level architecture
- Unity 2022.3.62f3 project using URP; game logic lives in `Assets/01_Scripts`.
- `Architecture/Singleton<T>` provides global managers (`GameManager`, `InputManager`, `SaveManager`, `LevelDifficultyManager`) that persist across scenes.
- Input flow: `FixedSlideBar`/pointer input → `InputManager.SlideDirection` → `PlayerController` steering.
- Persistence: `SaveManager` serializes `PlayerSaveData` to JSON in `Application.persistentDataPath` and reads tuning values from `UpgradeStatsSO` ScriptableObjects.
- Scenes are under `Assets/05_Scenes` (e.g., `Roads.unity`); prefabs and ScriptableObjects are organized in numbered asset folders.
- Third-party mobile dependencies live under `Assets/ExternalDependencyManager` and `Assets/00_AsserStore/GoogleMobileAds`.

## Key conventions
- Keep assets in the numbered top-level folders (`00_` to `06_`) to match the project’s asset organization.
- Place gameplay code in `Assets/01_Scripts` and use the existing subfolders (`Architecture`, `Managers`, `Entities`, `Data`, `UI`).
- Managers should inherit `Singleton<T>` and be accessed via `.Instance` to avoid duplicate scene instances.
- Use `PlayerSaveData` + `JsonUtility` for save data and `UpgradeStatsSO` for designer-tuned upgrade values.
- Steering input should read from `InputManager` rather than directly from `Input`, to keep UI and touch/mouse handling centralized.
