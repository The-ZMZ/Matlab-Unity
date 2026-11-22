# Task #24 — Taste Sorting: Test Plan

## 1. Scope

Task #24 adds the **TASTE** educational activity to the existing Clean My Room
project. The child drags eight food cards into four taste areas (Sweet, Sour,
Bitter, Salty) using the **mouse only**. MATLAB owns the food-to-taste mapping,
all answer validation, scoring and completion. Unity only collects pointer
input, sends `PLACE_REQ` and displays the returned `PLACE_RES`.

This plan covers only Task #24. Task #25 (Number Matching) is planned
separately and is not touched by these tests.

## 2. Files under test

### New files

- `Shared_Data/ActivityData/taste.json`
- `MATLAB/Tests/tTasteActivity.m`
- `Unity/CleanMyRoom/Assets/Scripts/UI/Taste/TasteActivityController.cs`
- `Unity/CleanMyRoom/Assets/Scripts/UI/Taste/TasteFoodItem.cs`
- `Unity/CleanMyRoom/Assets/Scripts/UI/Taste/TasteDropZone.cs`
- `Unity/CleanMyRoom/Assets/Tests/Editor/TasteActivityTests.cs`

### Modified files

- `Unity/CleanMyRoom/Assets/Scenes/TasteSorting.unity`
- `Unity/CleanMyRoom/Assets/UI/Text/EnglishUiText.asset`
- `Unity/CleanMyRoom/Assets/Scripts/UI/UiTextKeys.cs`

## 3. Environment

- MATLAB R2025b: `C:\Program Files\MATLAB\R2025b\bin\matlab.exe`
- Unity 6000.6.5f1: `C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe`
- Project root: `C:\Users\zahraa\Documents\GitHub\Matlab-Unity`
- Tests were executed in an isolated project copy at
  `C:\Users\zahraa\AppData\Local\Temp\opencode\task24_scene_build`, created from
  the repository's `Assets`, `Packages` and `ProjectSettings`. The verified
  scene and generated `.meta` files were copied back to the repository.

## 4. Test levels

### 4.1 MATLAB — authoritative data and rules

Runner: `MATLAB/Tests/runAllTests` (includes the new `tTasteActivity` class).

| ID | Test | Expected result |
|---|---|---|
| M1 | `datasetHasEightObjectsAndFourTargets` | `TASTE` definition has 8 unique object IDs and the four targets `sweet`, `sour`, `bitter`, `salty` |
| M2 | `everyDeclaredTasteMappingValidates` | Each declared food/target pair returns `valid = true`, `correct = true` |
| M3 | `wrongTasteIsIncorrect` | Honey → sour returns `valid = true`, `correct = false` |
| M4 | `unknownFoodIsRejected` | Unknown object returns `valid = false` with `ErrorUnknownObject` |
| M5 | `completionOnlyAfterEightDistinctCorrectPlacements` | Score rises 1…8; completion and one star occur only on the 8th distinct solved item |
| M6 | `incorrectAndDuplicateDoNotAdvanceCompletion` | Wrong and duplicate placements do not increase score or award a star |

Command:

```powershell
& 'C:\Program Files\MATLAB\R2025b\bin\matlab.exe' -batch `
  "cd('C:\Users\zahraa\Documents\GitHub\Matlab-Unity\MATLAB\Tests'); runAllTests"
```

Pass condition: zero failures, zero incomplete tests.

### 4.2 Unity EditMode — interaction and display layer

Runner: Unity Test Runner, platform `EditMode`, filter
`CleanMyRoom.Tests.TasteActivityTests`.

| ID | Test | Expected result |
|---|---|---|
| U1 | `NormalizedCoordinates_UseTopLeftOriginAndClamp` | Top-left origin, y-down, clamped to `[0,1]` |
| U2 | `TasteScene_HasFourZonesAndEightFoodItems` | Scene has a controller, 4 drop zones and 8 food items |
| U3 | `DragSelection_IsSingleAndZoneDetectionUsesLocalRectangles` | Only one drag at a time; all four zone rectangles are hit-testable |
| U4 | `DropOutsideAnyZone_ReturnsItemHome` | No request is sent; card returns to its start position |
| U5 | `CorrectResponse_MarksItemOnceAndNeverCompletesLocally` | `correct = true` marks one card; Unity does not complete the activity locally |
| U6 | `IncorrectResponse_ReturnsItemAndDoesNotMarkIt` | `correct = false` returns the card and marks nothing |
| U7 | `RepeatedResponse_IsIgnored` | Duplicate response IDs do not double-count |

Command (use `Start-Process -Wait`; plain `& Unity.exe` detaches in this shell):

```powershell
$exe = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe'
$args = @(
  '-batchmode','-nographics',
  '-projectPath','<isolated project copy>',
  '-runTests','-testPlatform','EditMode',
  '-testFilter','CleanMyRoom.Tests.TasteActivityTests',
  '-testResults','<temp>\task24_focused_results3.xml',
  '-logFile','<temp>\task24_focused4.log'
)
Start-Process -FilePath $exe -ArgumentList $args -Wait -PassThru
```

### 4.3 Unity regression suites

- Full EditMode suite: all previously passing tests must still pass; the
  pre-existing skipped test remains the only skip.
- Full PlayMode suite: all tests must pass.

The `MeshImportTests` fixture resolves an external path three directories
above the project (`Shared_Data/Meshes/Issue010`). When running from an
isolated copy, that fixture must be mirrored to the matching temp location;
this is test-environment setup, not a product change.

### 4.4 End-to-end TASTE over TCP — manual only

Not automated in this pass. Manual steps:

1. Start MATLAB on `127.0.0.1:55001` with the project's server entry point.
2. Start the game and open the taste activity from the bedroom.
3. Drag Honey onto Sweet; confirm MATLAB returns `valid=true, correct=true`,
   the card is marked and success feedback/audio plays.
4. Drag Lemon onto Salty; confirm MATLAB returns `correct=false`, the card
   returns home and error feedback/audio plays.
5. Solve all eight items; confirm the star/activity-completion message comes
   from MATLAB's `ACTIVITY_RESULT`/`GAME_PROGRESS`, not from Unity.
6. Stop MATLAB while the activity is open; confirm the connection status is
   shown, activities are disabled, and no answer can be completed locally.
7. Restart MATLAB and confirm the client reconnects and the activity works.

## 5. Acceptance mapping

| Acceptance item | Covered by |
|---|---|
| Mouse-only drag and drop | U3, U4, manual step 3 |
| MATLAB owns correctness and completion | M2–M6, U5, U7, manual step 5 |
| Eight-food / four-taste dataset | M1, U2 |
| No hand/gesture/camera input | Source inspection + manual step 3 |
| Disconnection/reconnection behaviour | Manual steps 6–7 |
| No regression in Issue #14/#15 work | Full EditMode + PlayMode suites |
