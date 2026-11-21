# Issue #015 — UI & Audio Foundation Test Report

Date: 2026-11-20  
Authoritative project: `C:\Users\zahraa\Documents\GitHub\Matlab-Unity\Unity\CleanMyRoom`  
Unity version used: `6000.6.5f1`

## 1. Scope verified

This report covers the approved Issue #015 scope only:

- Main Menu with Game Title, Start Game and Quit.
- Esc-only pause menu in the four gameplay scenes.
- Reusable instructions panel in ColorSorting, TasteSorting and MathActivity.
- Feedback panel driven only by authoritative MATLAB result messages/events.
- English UI text in a ScriptableObject catalog.
- Four approved Kenney sounds in a `Resources` audio catalog.
- Centralized `AudioManager` using `BeforeSceneLoad` bootstrap and safe lazy creation.
- New Input System Escape handling.
- `CleanMyRoom.UI` assembly.
- 1024×768 UI reference resolution.
- No new packages, no new scenes, no fade transitions.
- Interaction Hint, PR #12 scene content and existing Issue #10/#12/#14 functionality preserved.

MATLAB remains the sole authority for educational correctness and activity completion. Unity only displays results and plays cues in response to those results.

## 2. Test environment

Unity was executed from:

`C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe`

Automated commands used `-batchmode -nographics -runTests` for both suites. Result XML files were written outside the repository under:

`C:\Users\zahraa\AppData\Local\Temp\opencode\issue015\`

## 3. Baseline results (before changes)

| Suite | Result | Total | Passed | Failed | Skipped | Duration |
|---|---:|---:|---:|---:|---:|---:|
| EditMode | Passed | 64 | 63 | 0 | 1 | 3.5790891 s |
| PlayMode | Passed | 4 | 4 | 0 | 0 | 1.0986177 s |

Baseline artifacts:

- `baseline_editmode.xml`
- `baseline_editmode.log`
- `baseline_playmode.xml`
- `baseline_playmode.log`

## 4. Final results after all implementation changes

| Suite | Result | Total | Passed | Failed | Skipped | Duration |
|---|---:|---:|---:|---:|---:|---:|
| EditMode | Passed | 72 | 71 | 0 | 1 | 4.3332391 s |
| PlayMode | Passed | 9 | 9 | 0 | 0 | 1.1280834 s |

Final artifacts:

- `final_editmode.xml`
- `final_editmode.log`
- `final_playmode.xml`
- `final_playmode.log`

### Result interpretation

- EditMode increased from 63 passed to 71 passed: **8 new Issue #015 tests passed**.
- PlayMode increased from 4 passed to 9 passed: **5 new Issue #015 tests passed**.
- No previously passing test failed.
- The single skipped EditMode test was already skipped in the baseline.
- The complete existing suites, including the Issue #14 scene/progress/star tests, SceneArchitecture tests, network tests, player tests and bedroom environment tests, passed again after the changes. This is the automated regression evidence that Issue #10, #12 and #14 functionality remains intact.

## 5. New Issue #015 tests executed

### EditMode — `Assets/Tests/Editor/Issue15Tests.cs`

| Test | Verified |
|---|---|
| `UiAssets_ExistAndUseApprovedEnglishTextAndAudio` | English catalog values and the four approved clips (`Click`, `Success`, `Error`, `StarReward`). |
| `UiPrefabs_AreAt1024By768ReferenceResolution` | All three new UI prefabs use `ScaleWithScreenSize` at 1024×768. |
| `MainMenu_HasTitleStartQuitAndIsWiredToEnglishText` | MainMenu title/Start/Quit objects, `MainMenuView`, and the persistent `QuitGame` callback. |
| `GameplayScenes_HavePauseAndFeedbackOnlyWhereApproved` | Pause + feedback in all four gameplay scenes; instructions only in the three activity scenes with the correct text keys. |
| `PauseMenu_UsesNewInputSystemEscapeOnlyInGameplayScene` | New Input System Escape toggles pause in a gameplay scene and is ignored in MainMenu; `Time.timeScale` restored. |
| `FeedbackPanel_DisplaysOnlyAuthoritativeMatLabResults` | Invalid `PLACE_RES` displays MATLAB text; non-completing `ACTIVITY_RESULT` shows no success. |
| `FloatingStar_RaisesRewardEventOnlyOnNewCompletion` | Star reward event fires once on a new authoritative completion and not on duplicate completion. |
| `AudioManager_UsesBeforeSceneLoadBootstrap` | `Bootstrap` has `RuntimeInitializeOnLoadMethodAttribute` with `RuntimeInitializeLoadType.BeforeSceneLoad`. |

### PlayMode — `Assets/Tests/PlayMode/Issue15PlayModeTests.cs`

| Test | Verified |
|---|---|
| `AudioManager_IsCreatedBeforeScenesAndSurvivesTransitions` | Runtime instance exists, is placed in the persistent scene, and survives MainMenu → ColorSorting. |
| `DuplicateAudioManager_IsRemovedWithoutReplacingInstance` | A duplicate runtime manager is destroyed and `Instance` remains the original. |
| `AudioCatalog_ContainsTheFourApprovedKenneyClips` | `Resources/Audio/UiAudioCatalog` loads all four approved clips at runtime. |
| `MainMenu_LoadedDirectlyShowsEnglishTitleStartAndQuit` | Direct MainMenu entry displays catalog text at 1024×768. |
| `GameplayScenes_LoadDirectlyWithCorrectUiAndNoLocalValidation` | Direct entry to Bedroom, ColorSorting, TasteSorting and MathActivity; correct UI panels and instruction body text. |

## 6. Verification against acceptance criteria

| Requirement | Status | Evidence |
|---|---|---|
| Game title, Start Game, Quit | PASS | EditMode + PlayMode MainMenu tests; screenshot. |
| Pause only during gameplay on Esc | PASS | EditMode InputSystem test plus scene placement test. |
| Reusable instructions panel for three activities | PASS | Scene placement and direct-entry PlayMode tests. |
| Interaction Hint unchanged | PASS | No Interaction Hint source/scene content was edited; existing Issue #14 tests passed. |
| English UI text via ScriptableObject | PASS | Catalog asset, controller wiring and text assertions. |
| 1024×768 UI | PASS | Prefab and MainMenu PlayMode/EditMode assertions. |
| Separate `CleanMyRoom.UI` assembly | PASS | Assembly exists and all UI tests compile against it. |
| Centralized audio with approved files | PASS | Audio catalog tests; PlayMode persistence/duplicate tests. |
| `BeforeSceneLoad` bootstrap and safe lazy init | PASS | Attribute test, runtime persistence test and duplicate test. |
| `Instance` never silently returns null | PASS in runtime paths | Duplicate/persistence tests and reentrancy-safe creation logic; see limitations below. |
| New Input System Escape | PASS | InputTestFixture-driven Escape test. |
| MATLAB sole authority | PASS | Feedback test uses only `WireMessage`/`GameProgress`; no local validation code added. |
| Preserve PR #12 and Issues #10/#12/#14 | PASS (automated) | Full existing suites passed; no deletions or unrelated refactoring. |
| No packages/scenes/fade transitions added | PASS | No package manifest, scene asset or fade code changes. |

## 7. Screenshots

Generated at 1024×768 and saved in:

`C:\Users\zahraa\Documents\GitHub\Matlab-Unity\Deliverables\Screenshots\`

- `Issue015_MainMenu.png`
- `Issue015_PauseMenu.png`
- `Issue015_InstructionsPanel.png`
- `Issue015_FeedbackPanel.png`

Each PNG was programmatically verified as 1024×768 and non-blank (sampled unique colours: MainMenu 338, PauseMenu 211, InstructionsPanel 296, FeedbackPanel 232).

## 8. Warnings observed

The following non-fatal warnings appeared in the Unity logs. There were **no C# compile errors** in the final logs.

1. `LicensingClient has failed validation; ignoring` / `Access token is unavailable; failed to update` — Unity licensing service warning in batch mode. Tests still ran and passed.
2. `Error while loading assembly references for CleanMyRoom.Tests.BedroomEnvironment.PlayMode` with `EndOfStreamException` in Burst's hash cache. This appeared after an interrupted Unity import/build during asset generation. It is a generated-cache/Burst warning, not a source compile error; all EditMode and PlayMode tests, including that assembly's tests in the full run, completed successfully. It is documented rather than hidden.

## 9. NOT RUN / limitations

- **Live MATLAB audio/feedback integration with a running MATLAB TCP server:** NOT RUN. No MATLAB server was started for this UI/audio task. Audio and feedback behavior was verified with synthetic `WireMessage` objects and authoritative `GameProgress` test calls, not a live MATLAB session.
- **Human listening test for audio cue audibility:** NOT RUN. The headless runner cannot verify physical speaker output. Clip assets, catalog wiring and cue dispatch code were verified.
- **Visual human review of gameplay scenes:** NOT RUN beyond the generated 1024×768 UI screenshots and automated scene assertions.
- **Forced reentrant `AudioManager.Instance` stress test:** NOT RUN. The implementation removes the `creating` gate and adopts an already-created component through `FindAnyObjectByType`, so reentrant calls cannot return null while the first object is being created. This was verified by code review and the runtime duplicate test, not by a dedicated injected-reentrancy test.
- Fade transitions remain out of scope and were not implemented.

## 10. Manual verification suggested for the project owner

1. Open Unity 6000.6.5f1 and enter Play Mode from MainMenu.
2. Confirm title, Start Game and Quit button click sounds.
3. Start the game, press Esc in Bedroom, Resume, then Esc → Main Menu; confirm time freezes only while paused and resumes on every exit path.
4. Enter each activity directly; confirm the instructions text and Close button text.
5. With MATLAB running, produce a rejected `PLACE_REQ` and a completed activity; confirm error/success/star cues and feedback text.
6. Confirm the Interaction Hint prompt remains visually and behaviourally unchanged.

## 11. Git status

No Git or GitHub operations were performed. All approved changes were left locally uncommitted for the project owner to review.
