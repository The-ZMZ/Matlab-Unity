# Issue #011 — Test Report

**Unity Scene Architecture**

| Field | Value |
|---|---|
| Issue | #011 |
| Branch | `feature/issue-011-scene-architecture` |
| Date of final verification | 2026-11-18 / 2026-11-19 |
| Unity used | **6000.6.5f1 (3ff58d469c8a)**, Windows |
| Test framework | `com.unity.test-framework` 1.8.0 (already present; no package change) |
| EditMode tests | **35 passed, 0 failed, 1 skipped** (36 total, 3.13 s) |
| PlayMode tests | **2 passed, 0 failed, 0 skipped** (2 total, 0.22 s) |
| Scenes | **6** registered and verified (5 created, 1 reused unchanged) |
| Git operations | **NONE** (no stage, commit, push, merge, branch or history changes) |

> No test result in this report is claimed as passed unless it was actually
> executed by the Unity Test Runner in batchmode. Raw runner output is stored in
> the XML files listed in section 6.

---

## 1. Command and result summary

| # | Check | Platform | Result |
|---|---|---|---|
| 1 | Scene assets, build settings, name resolution, GameManager helpers | EditMode | **35 passed, 0 failed, 1 skipped** |
| 2 | Runtime `DontDestroyOnLoad` persistence and duplicate prevention | PlayMode | **2 passed, 0 failed** |
| 3 | Full existing Unity regression (`CleanMyRoom.Tests.Editor`) | EditMode | **25 passed, 0 failed, 1 skipped** |
| 4 | New scene-architecture suite (`CleanMyRoom.Tests.SceneArchitecture`) | EditMode | **10 passed, 0 failed** |
| 5 | Build-settings order (`MainMenu` at index 0) | EditMode | PASS (asserted by test) |
| 6 | MATLAB interaction / validation | — | **NOT RUN** — unchanged by this Issue |

The single skipped test is the pre-existing explicit integration test
`CleanMyRoom.Tests.NetworkIntegrationTests.ConnectsPingsPlacesAndReconnects`,
reason: "Requires the MATLAB server on 127.0.0.1:55001 with the test fixture
loaded." It is skipped automatically by the test framework, not by this Issue.

---

## 2. EditMode test run (authoritative, final)

Exact command:

```powershell
& 'D:\Unity\Unity Hub\6000.6.5f1\Editor\Unity.exe' -batchmode -nographics `
  -projectPath 'D:\Matlab-Unity\Unity\CleanMyRoom' -runTests `
  -testPlatform EditMode `
  -testResults 'D:\Matlab-Unity\Deliverables\Reports\Issue011_Unity_TestResults.xml' `
  -logFile 'D:\Matlab-Unity\Unity\Logs\Issue011_TestRun3.log'
```

Unity log ended with: `Test run completed. Exiting with code 0 (Ok).`

Result XML header:

```xml
<test-run id="2" testcasecount="36" result="Passed" total="36" passed="35"
          failed="0" inconclusive="0" skipped="1" engine-version="3.5.0.0"
          start-time="2026-11-18 20:59:08Z" end-time="2026-11-18 20:59:11Z"
          duration="3.1288244">
```

### 2.1 Existing suite — `CleanMyRoom.Tests.Editor.dll` (unchanged)

26 cases: 25 passed, 0 failed, 1 explicit skip. This confirms Issues #007–#010
tests still compile and pass after the new assemblies were added.

| Fixture | Cases | Result |
|---|---:|---|
| `JsonCodecTests` | 11 | 11 passed |
| `MeshImportTests` | 5 | 5 passed |
| `MessageFramerTests` | 9 | 9 passed |
| `NetworkIntegrationTests` | 1 | skipped (explicit, requires MATLAB) |

### 2.2 New suite — `CleanMyRoom.Tests.SceneArchitecture.dll`

10 cases, all passed:

| Test | Result | Duration (s) |
|---|---|---:|
| `AllSixScenesExistAsValidUnitySceneAssets` | Passed | 0.011 |
| `SceneMetaGuidsAreUniqueAndPresentInBuildSettings` | Passed | 0.007 |
| `BuildSettingsEnableAllSixScenesWithMainMenuFirst` | Passed | 0.006 |
| `RegisteredSceneNamesResolveToBuildIndices` | Passed | 0.004 |
| `UnknownSceneNamesAreRejectedWithMeaningfulErrors` | Passed | 0.002 |
| `ActiveSceneComparisonPreventsUnnecessaryReloads` | Passed | 0.003 |
| `GameManagerRegistrationKeepsOneInstance` | Passed | 0.013 |
| `GameManagerUnregisterClearsStaticReference` | Passed | 0.001 |
| `PersistenceRequiresAnUnparentedRootObject` | Passed | 0.005 |
| `SceneChangedEventReportsLoadedScene` | Passed | 0.003 |

What these cover: all six `.unity` assets and `.meta` files exist; each scene is
valid Unity YAML with a camera and light; all six GUIDs are unique and present in
build settings; `MainMenu` is at index 0 and the other five appear in the agreed
order; `SampleScene` is retained at the end; registered names resolve to valid
build indices; invalid names are rejected with meaningful errors; loading the
active scene is detected as a no-op; GameManager register/unregister keeps one
instance; only unparented roots are allowed to persist; and the scene-changed
event reports the loaded scene.

---

## 3. PlayMode test run (runtime persistence)

`DontDestroyOnLoad` is a runtime-only Unity call; calling it in EditMode throws
`InvalidOperationException`. Runtime persistence is therefore verified by a
separate PlayMode assembly instead of by the EditMode suite.

Exact command:

```powershell
$unity = 'D:\Unity\Unity Hub\6000.6.5f1\Editor\Unity.exe'
$args = @(
  '-batchmode','-nographics',
  '-projectPath','D:\Matlab-Unity\Unity\CleanMyRoom',
  '-runTests','-testPlatform','PlayMode',
  '-testResults','D:\Matlab-Unity\Deliverables\Reports\Issue011_PlayMode_TestResults.xml',
  '-logFile','D:\Matlab-Unity\Unity\Logs\Issue011_PlayMode.log'
)
$p = Start-Process -FilePath $unity -ArgumentList $args -Wait -PassThru
$p.ExitCode   # 0
```

Result XML header:

```xml
<test-run id="2" testcasecount="2" result="Passed" total="2" passed="2"
          failed="0" inconclusive="0" skipped="0" engine-version="3.5.0.0"
          start-time="2026-11-18 20:59:59Z" end-time="2026-11-18 20:59:59Z"
          duration="0.220202">
```

| Test | Result | Duration (s) |
|---|---|---:|
| `GameManagerSurvivesSceneLoadsInPersistentScene` | Passed | 0.118 |
| `DuplicateGameManagerIsRemovedAtRuntime` | Passed | 0.045 |

Verified behaviours:

1. The startup `GameManager` root is placed in Unity's `DontDestroyOnLoad`
   scene.
2. Loading `MainMenu` and then `Bedroom` leaves the **same** `GameManager`
   instance in that persistent scene.
3. `Bedroom` becomes the active scene after the transition.
4. A second `GameManager` created at runtime is destroyed and does not replace
   `GameManager.Instance` (log confirms `[GameManager] Duplicate GameManager
   removed.`).

---

## 4. Earlier failing run and its resolution (transparency)

The first EditMode run in this session produced 34 passed / 1 failed. The
failure was:

```
System.InvalidOperationException : The following game object is invoking the
DontDestroyOnLoad method: Game Manager Persistence Probe. Notice that
DontDestroyOnLoad can only be used in play mode and, as such, cannot be part of
an editor script.
```

Cause: the EditMode test called `PersistAcrossScenes()` directly. Fix: the test
now checks the `CanPersist` root precondition in EditMode, and the real
`DontDestroyOnLoad` behaviour is covered by the PlayMode tests in section 3.
The final re-run (section 2) is the authoritative result. The superseded logs
`Issue011_TestRun.log` and `Issue011_TestRun2.log` are retained for traceability.

---

## 5. Files created

### Scenes (`Unity/CleanMyRoom/Assets/Scenes/`)

| File | Note |
|---|---|
| `MainMenu.unity` (+ `.meta`) | New, build index 0. |
| `ColorSorting.unity` (+ `.meta`) | New, build index 2. |
| `TasteSorting.unity` (+ `.meta`) | New, build index 3. |
| `MathActivity.unity` (+ `.meta`) | New, build index 4. |
| `Ending.unity` (+ `.meta`) | New, build index 5. |

`Bedroom.unity` (build index 1) already existed and was **not modified,
duplicated or replaced**. Each new scene contains only a `Main Camera` and a
`Directional Light`.

### Runtime code (`Unity/CleanMyRoom/Assets/Scripts/Core/`)

| File | Purpose |
|---|---|
| `CleanMyRoom.Core.asmdef` (+ `.meta`) | Isolates Issue #011 runtime code. |
| `SceneNames.cs` (+ `.meta`) | Single source of truth for the six scene names. |
| `SceneLoader.cs` (+ `.meta`) | Validated, centralized scene loading API. |
| `GameManager.cs` (+ `.meta`) | Persistent lifecycle owner; no progress state. |
| `AssemblyInfo.cs` (+ `.meta`) | `InternalsVisibleTo` for the EditMode test assembly. |

Parent folder `Unity/CleanMyRoom/Assets/Scripts/Core.meta` was also created.

### Tests

| File | Purpose |
|---|---|
| `Assets/Tests/Editor/SceneArchitecture/CleanMyRoom.Tests.SceneArchitecture.asmdef` (+ `.meta`) | New nested EditMode test assembly; existing test files/asmdefs untouched. |
| `Assets/Tests/Editor/SceneArchitecture/SceneArchitectureTests.cs` (+ `.meta`) | 10 asset/build/name/lifecycle tests. |
| `Assets/Tests/PlayMode/CleanMyRoom.Tests.PlayMode.asmdef` (+ `.meta`) | New PlayMode test assembly. |
| `Assets/Tests/PlayMode/GameManagerPersistencePlayModeTests.cs` (+ `.meta`) | 2 runtime persistence tests. |

Folder metas: `Assets/Tests/Editor/SceneArchitecture.meta`, `Assets/Tests/PlayMode.meta`.

### Documentation and evidence

| File | Purpose |
|---|---|
| `Documentation/Architecture/Scene_Architecture.md` | Scene/loading/lifecycle design. |
| `Documentation/Testing/Issue011_Test_Report.md` | This report. |
| `Deliverables/Reports/Issue011_Unity_TestResults.xml` | Final EditMode runner output. |
| `Deliverables/Reports/Issue011_PlayMode_TestResults.xml` | PlayMode runner output. |
| `Unity/Logs/Issue011_*.log` | Batchmode Unity logs (4 files). |

---

## 6. Files modified

| File | Change |
|---|---|
| `Unity/CleanMyRoom/ProjectSettings/EditorBuildSettings.asset` | Added the six game scenes in order; retained the pre-existing `SampleScene` entry. |
| `Unity/CleanMyRoom/CleanMyRoom.slnx` | Unity regenerated the solution to include the three new assembly projects (`Core`, `Tests.PlayMode`, `Tests.SceneArchitecture`). |

Two other settings files (`ProjectSettings.asset`, `ProjectAuditorSettings.asset`)
were rewritten by Unity with LF line endings during the test runs. They were
normalized back to the repository's CRLF convention, so `git diff` reports **no
content change** for them.

**No files belonging to Issues #007, #008, #009 or #010 were edited.** Their
tests were re-run and still pass.

---

## 7. Tests NOT run

| Check | Status | Reason / manual step |
|---|---|---|
| MATLAB server integration / activity validation | NOT RUN | Out of scope for Issue #011; no MATLAB code changed. The pre-existing `NetworkIntegrationTests` remains explicitly skipped. |
| Interactive Editor visual inspection of each scene | NOT RUN | Batchmode has no graphics display. Manual step: open each of the six scenes in Unity and confirm the clean camera + light structure. |
| Player navigation from MainMenu through all activities to Ending | NOT RUN | No gameplay, UI or activity content exists yet (future Issues). |

---

## 8. Limitations and unresolved items

1. `SampleScene.unity` remains enabled at the end of the build list to avoid
   changing pre-existing configuration. Removing it requires owner approval.
2. The new scenes are intentionally empty starting points; they contain no
   menu, furniture, characters, UI or activity logic yet.
3. `GameManager` deliberately stores no stars, score or progress; those remain
   MATLAB's responsibility and will be added by later Issues.
4. The PlayMode tests load real scenes and change the active scene at runtime;
   Unity batchmode resets this afterwards, so no scene asset is modified.
5. All changes are left **uncommitted and unstaged** for the project owner. No
   Git write operation was performed.
6. Issue closure awaits human review and approval; no Issue was closed.
