# Unity Scene Architecture (Issue #011)

## 1. Overview

The game is split into six Unity scenes. Each scene is a clean starting point:
a `Main Camera` and a `Directional Light`. No gameplay, UI, activity content or
artwork is added by Issue #011. The existing `Bedroom.unity` is reused as-is and
was **not** modified or duplicated.

| Build index | Scene name | Asset path | Purpose |
|---:|---|---|---|
| 0 | `MainMenu` | `Assets/Scenes/MainMenu.unity` | Startup scene and future menu entry point. |
| 1 | `Bedroom` | `Assets/Scenes/Bedroom.unity` | Existing 3D bedroom / hub scene. |
| 2 | `ColorSorting` | `Assets/Scenes/ColorSorting.unity` | Bed / colour-sorting activity host. |
| 3 | `TasteSorting` | `Assets/Scenes/TasteSorting.unity` | Dining-table / taste-classification activity host. |
| 4 | `MathActivity` | `Assets/Scenes/MathActivity.unity` | Study-desk / mathematics activity host. |
| 5 | `Ending` | `Assets/Scenes/Ending.unity` | Father's congratulations and game end host. |

`SampleScene.unity` remains enabled at the end of the list because it existed in
the project before this Issue. It is not part of the game flow, and future work
may disable or remove it once an owner-approved cleanup issue exists.

Build configuration lives in
`Unity/CleanMyRoom/ProjectSettings/EditorBuildSettings.asset`. This project does
not store a Build Profile under `ProjectSettings/`; the generated profiles under
`Library/BuildProfiles/` are Unity-managed and gitignored. The active profile
therefore reads this build-scene list.

## 2. Naming conventions

- Scene names use **PascalCase** with no spaces or suffixes: `MainMenu`, not
  `main_menu` or `MainMenu_v2`.
- All game scenes live directly in `Assets/Scenes/`.
- The file name, the scene name, the value in `SceneNames`, the build-settings
  path and the build-settings GUID must agree.
- `SceneNames` is the only place where names are declared. Code should call
  `SceneNames.MainMenu` rather than repeat a string literal.
- Lookup is **case-sensitive** by design so a mistyped name fails immediately
  instead of silently resolving to something else.

## 3. Scene-loading workflow

`CleanMyRoom.Core.SceneLoader` is the single entry point for transitions.

```csharp
SceneLoader.LoadMainMenu();     // or LoadBedroom(), LoadColorSorting(), ...
SceneLoader.Load(SceneNames.MathActivity);

AsyncOperation operation;
SceneLoader.LoadAsync(SceneNames.Ending, out operation);
```

Each request is processed as follows:

1. `SceneNames.IsKnown` checks the name against the six registered names.
2. `SceneLoader.TryResolve` converts the name to `Assets/Scenes/<Name>.unity`
   and queries `SceneUtility.GetBuildIndexByScenePath`. A scene that exists on
   disk but is missing from the build list produces a clear `Debug.LogError`.
3. `IsSameScene` compares the resolved name with the active scene. Loading the
   current scene is a no-op, which avoids unnecessary reloads.
4. Loading uses the **build index**, not a free-form string, so name/path
   ambiguity cannot cause the wrong scene to open.

Invalid requests are never silent: `Load` and `LoadAsync` log an error that lists
the valid names, return `false`, and leave the active scene unchanged.

Scene loading performs **no** answer validation and does **not** change stars or
progress. MATLAB remains authoritative for all activity results.

## 4. Persistent Game Manager lifecycle

`CleanMyRoom.Core.GameManager` follows the existing `NetworkManager` pattern:

1. `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)] EnsureInstance()` creates a
   root `GameObject` named `Game Manager` if none exists. It may also be placed
   explicitly in a scene.
2. `Awake` calls `Register`. If another instance is already registered, the new
   object is destroyed (with a warning) and `Instance` is unchanged.
3. `PersistAcrossScenes` calls `DontDestroyOnLoad`, so the manager is not
   destroyed when the active scene changes. A parented object is rejected with
   a warning because Unity only persists root objects.
4. `AttachSceneEvents` subscribes to `SceneManager.sceneLoaded`. It removes the
   handler before adding it, so repeated registration cannot create duplicate
   subscriptions.
5. `OnDestroy` calls `Unregister`, which detaches the event and clears the
   static reference only when it points at that exact instance.

The manager records `CurrentSceneName` and raises `SceneChanged`. It holds no
scores, stars, hints, difficulty or correctness state — those belong to MATLAB.

## 5. Adding a future scene

1. Create the scene in `Assets/Scenes/` using PascalCase, with the same clean
   camera + light starting structure.
2. Add a `const string` to `SceneNames` and include it in the `All` list. The
   list order defines the build-order contract used by the tests.
3. Add the scene to `EditorBuildSettings.asset` with the GUID from its `.meta`
   file, in the same position as in `SceneNames.All`.
4. Optionally add a `Load<Name>()` convenience method to `SceneLoader`.
5. Update the expected count/order in
   `Assets/Tests/Editor/SceneArchitecture/SceneArchitectureTests.cs` and run
   the EditMode tests.
6. Document the new scene in this file.

Do not call `SceneManager.LoadScene` directly from new code; route through
`SceneLoader` so validation and error reporting stay consistent.

## 6. Tests

EditMode tests live in the separate assembly
`CleanMyRoom.Tests.SceneArchitecture` under
`Assets/Tests/Editor/SceneArchitecture/`. They verify asset existence and
validity, GUID uniqueness, build-settings registration and ordering, name/path
resolution, invalid-name errors, no-reload detection, and GameManager
registration/unregistration plus the `CanPersist` root precondition.

Runtime persistence is covered by PlayMode tests in the separate assembly
`CleanMyRoom.Tests.PlayMode` under `Assets/Tests/PlayMode/`. These are required
because `DontDestroyOnLoad` is a runtime-only Unity call and throws in EditMode.
They load `MainMenu` and `Bedroom` for real and confirm that the same
`GameManager.Instance` stays in the `DontDestroyOnLoad` scene, and that a second
manager added at runtime is destroyed. See the Issue #011 test report for exact
commands and results.

---

## 7. Integration note — MainMenu start button

After the four issues were integrated, a minimal Start Game button was added to
`MainMenu.unity` so the startup scene is playable without manual scene switching.
It is a uGUI `Button` on a `MainMenuCanvas`, with an `EventSystem`; its persistent
onClick calls `CleanMyRoom.Core.MainMenuController.StartGame()`, which routes
through `SceneLoader.LoadBedroom()`.

`MainMenu` remains build index 0, and all other scene names, paths, GUIDs and
build indices are unchanged.

---

## 8. Integration note — Bedroom ceiling visibility fix

The Bedroom is a closed box: the walls end at `y = 5` and `Ceiling` is a solid
white slab at `y = 5.1`. `FollowCamera` intentionally keeps the player's camera
above the walls (`minimumHeight = 5.6`, offset height `6.5`) for a room-wide
view. With a solid ceiling, that camera ended up **above** the ceiling and could
only see its white underside, so the room appeared for about a second while the
camera moved into place and then "vanished" into a near-white screen.

`PlayModeCeilingHider` (attached to the `Ceiling` GameObject in `Bedroom.unity`)
disables only the ceiling's `MeshRenderer` in `Awake()` during Play Mode. The
ceiling GameObject, transform and light remain in the scene at edit time and for
all environment tests; no scene, camera or gameplay values were changed.

Verified with rendered captures at 0 s / 2 s / 5 s: the room stays fully visible
throughout, and the EditMode/PlayMode suites remain green.
