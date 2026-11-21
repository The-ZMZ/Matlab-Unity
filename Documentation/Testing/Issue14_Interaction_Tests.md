# Issue #14 — Bedroom Furniture Interaction & Floating Stars: Test Plan

## Scope

Issue #14 adds furniture interaction, a deterministic E-key prompt, activity
navigation requests, session completion tracking and reusable floating stars.
It integrates with the Issue #13 `PlayerController` and the existing
`CleanMyRoom.Net` protocol. No MATLAB functionality was changed.

Environment used for the automated run:

- Unity 6000.6.5f1 (Windows, PCWIN64)
- Project: `Unity/CleanMyRoom`
- Test platform: EditMode, batch mode
- Test run date: 2026-11-20

The Unity GUI was left open during development, so Unity asset generation and
tests were executed against an isolated copy of the project and the verified
`Assets/` changes were copied back.

## What was implemented safely

The project had no Bed, Dining Table or Study Desk objects, no activity scenes,
no activity scripts and no progression system before this issue. Therefore the
implementation deliberately stops short of loading an activity:

- Furniture, interaction colliders, prompt logic and stars are fully configured.
- `GameProgress` accepts completion only from authoritative MATLAB messages.
- `ActivityNavigation` and `SceneTransitionController` provide a guarded scene
  transition path.
- Each furniture object has an empty `scenePath`. Pressing E on such an object
  logs a warning and loads nothing.
- No placeholder or guessed activity scene was created.

## Furniture-to-activity mapping

The mapping was taken from `AGENTS.md` / `README.md`, not guessed:

| Furniture | Activity id | Activity label |
|---|---|---|
| Bed | `COLOR` | Color Sorting |
| Dining Table | `TASTE` | Taste Classification |
| Study Desk | `MATH` | Mathematics Activity |

**Decision requiring approval:** the real activity scene paths are still
missing. Please supply the scene asset paths (and add them to Build Settings),
or approve creating dedicated placeholder activity scenes. The `scenePath`
fields can then be filled without changing the interaction code.

## Automated tests

Added `Assets/Tests/Editor/Issue14Tests.cs` (11 tests):

| Test | What it verifies |
|---|---|
| `BedroomScene_HasThreeFurnitureWithSolidAndTriggerColliders` | Bed, Dining Table and Study Desk exist with the correct activity ids, a solid non-trigger collider, a trigger collider and an interaction anchor. Scene paths are still empty. |
| `BedroomScene_HasReusableStarsAboveEachFurniture` | One reusable `FloatingStar.prefab` with mesh, URP material and glow light; three configured instances float above the correct furniture. |
| `Prompt_AppearsForNearestTargetAndHidesOutsideRange` | Prompt appears for the nearest object, updates when moving to another object and disappears outside range. |
| `SelectNearest_TiesAreBrokenByActivityId` | Equal-distance targets are resolved deterministically by activity id, independent of enumeration order. |
| `EKey_OpensOnlyTheSelectedFurnitureActivity` | Simulated E press emits exactly one navigation request with the selected object's activity id and path; the other nearby activity is never opened. |
| `UnconfiguredFurniture_DoesNotRequestAnyScene` | E on an object with an empty scene path emits no navigation request. |
| `GameProgress_AcceptsOnlyAuthoritativeCompletion` | `PLACE_RES`, failed `ACTIVITY_RESULT` and duplicate results do not complete an activity; successful `ACTIVITY_RESULT` and `GAME_PROGRESS` do. |
| `ProgressState_SurvivesSceneTransitionStyleRecreation` | Completion state survives destruction/recreation of the scene-owned progress object. |
| `FloatingStar_HidesOnlyForItsOwnCompletedActivity` | A star stays visible for other completions, hides for its own completion and is restored on session reset. |
| `Completion_RequestsReturnOnlyForTheActiveActivity` | A return-to-bedroom request is raised only for the active activity's successful completion. |
| `Issue13_PlayerMovementRemainsFunctional` | Issue #13 diagonal normalisation still works and the player prefab has both `PlayerController` and `InteractionController`. |

## Automated results

Focused Issue #14 run:

```text
Unity.exe -batchmode -nographics -projectPath <project> \
  -runTests -testPlatform EditMode \
  -testFilter CleanMyRoom.Tests.Issue14Tests
```

Result: **11 total, 11 passed, 0 failed, 0 skipped**.

Full EditMode suite after all changes:

```text
Unity.exe -batchmode -nographics -projectPath <project> \
  -runTests -testPlatform EditMode
```

Result: **43 total, 42 passed, 0 failed, 1 skipped**.

The skipped test is the pre-existing explicit
`NetworkIntegrationTests.ConnectsPingsPlacesAndReconnects`, which requires a
running MATLAB server and is outside Issue #14.

## PlayMode status

**PlayMode was not run.** No visual verification of walking up to furniture,
reading the on-screen prompt, or watching the stars animate was performed.
The IMGUI prompt and star motion are configured in code and scene assets, but
only their state logic and configuration were covered by EditMode tests.

Manual PlayMode checks for the project owner:

1. Open `Assets/Scenes/Bedroom.unity` and enter Play Mode.
2. Move with WASD/arrows to each furniture object.
3. Confirm the prompt appears only in range and changes between objects.
4. Press E. Until scene paths are configured, confirm the warning is logged and
   no wrong scene loads.
5. After configuring real activity scenes, confirm each E press opens only its
   mapped activity.
6. Send a successful `ACTIVITY_RESULT` from MATLAB and confirm the player
   returns to the bedroom and only the matching star disappears.

## Limitations

- Activity scene paths are intentionally empty. Opening activities and returning
  after real completion cannot be visually verified until the scenes are supplied.
- Completion persistence is session-only. It is held in a static set on
  `GameProgress` and survives scene reloads, but not application restarts.
- `ACTIVITY_RESULT.stars` is not used for local gameplay decisions; MATLAB's
  `completed`/`completedActivities` fields are authoritative.
- The stars use a URP/Lit emissive material plus a small point light. This is a
  lightweight glow; there is no Bloom/post-processing dependency.
- No camera/webcam, hand tracking, gesture or pinch functionality was added.
- No Git write operations were performed.
