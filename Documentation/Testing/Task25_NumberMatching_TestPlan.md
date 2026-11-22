# Task #25 — Number Matching: Test Plan

## 1. Scope

Task #25 adds the **MATH** educational activity: numbers 1–10 are matched with
groups containing the same number of dots (`README.md:30`). The child uses the
**mouse only** to drag ten number cards onto ten dot-group zones. MATLAB owns
the dataset, answer validation, scoring and completion. Unity collects pointer
input, sends `PLACE_REQ` and displays the returned `PLACE_RES`.

Approved decisions:

1. Minimal approach: MATLAB owns `math.json` and all validation/completion;
   Unity renders dots from the target ids (`dots_1`…`dots_10`). The TCP protocol
   is unchanged — no new fields or message types.
2. All ten number cards and all ten dot-group zones are displayed
   simultaneously.

Task #24 (Taste Sorting) is untouched by this work.

## 2. Files under test

### Created

- `Shared_Data/ActivityData/math.json`
- `MATLAB/Tests/tMathActivity.m`
- `Unity/CleanMyRoom/Assets/Scripts/UI/Math/MathActivityController.cs`
- `Unity/CleanMyRoom/Assets/Scripts/UI/Math/NumberCard.cs`
- `Unity/CleanMyRoom/Assets/Scripts/UI/Math/DotGroupZone.cs`
- `Unity/CleanMyRoom/Assets/Tests/Editor/MathActivityTests.cs`
- `Documentation/Testing/Task25_NumberMatching_TestPlan.md`
- `Documentation/Testing/Task25_NumberMatching_Report.md`

### Modified

- `Unity/CleanMyRoom/Assets/Scenes/MathActivity.unity` — added the activity
  canvas, controller, ten cards and ten zones; preserved the camera, light and
  all three Issue #15 UI prefab instances.

No shared scripts, protocol files, text catalog files, or Task #24 files were
modified.

## 3. Environment

- MATLAB R2025b: `C:\Program Files\MATLAB\R2025b\bin\matlab.exe`
- Unity 6000.6.5f1: `C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe`
- Project root: `C:\Users\zahraa\Documents\GitHub\Matlab-Unity`
- Tests executed in an isolated copy:
  `C:\Users\zahraa\AppData\Local\Temp\opencode\task25_scene_build`
  (created from `Assets`, `Packages` and `ProjectSettings`). The verified scene
  and generated `.meta` files were copied back to the repository.
- The path-dependent `MeshImportTests` fixture was mirrored to
  `C:\Users\zahraa\AppData\Local\Temp\Shared_Data\Meshes\Issue010` for the
  isolated copy. This is test-environment setup only.

## 4. Test levels

### 4.1 MATLAB — authoritative data and rules

Runner: `MATLAB/Tests/runAllTests` (includes the new `tMathActivity` class).

| ID | Test | Expected result |
|---|---|---|
| M1 | `datasetHasTenNumbersAndTenDotGroups` | `MATH` has 10 unique number objects and 10 unique `dots_N` targets |
| M2 | `everyDeclaredMappingValidates` | Every declared mapping returns `valid=true`, `correct=true` |
| M3 | `numberMatchesItsOwnDotGroupOnly` | 3→dots_3 correct; 3→dots_4 incorrect |
| M4 | `unknownNumberIsRejected` | `number_11` returns `valid=false` with `ErrorUnknownObject` |
| M5 | `completionOnlyAfterTenDistinctCorrectPlacements` | Score rises 1…10; completion and one star only on the 10th distinct solved number |
| M6 | `incorrectAndDuplicateDoNotAdvanceCompletion` | Wrong/duplicate placements do not increase score or award a star |

Command:

```powershell
& 'C:\Program Files\MATLAB\R2025b\bin\matlab.exe' -batch `
  "cd('C:\Users\zahraa\Documents\GitHub\Matlab-Unity\MATLAB\Tests'); runAllTests"
```

Pass condition: zero failures, zero incomplete tests.

### 4.2 Unity EditMode — interaction and display layer

Filter: `CleanMyRoom.Tests.MathActivityTests` (`[Category("Task25")]`).

| ID | Test | Expected result |
|---|---|---|
| U1 | `NormalizedCoordinates_UseTopLeftOriginAndClamp` | Top-left origin, y-down, clamped `[0,1]` |
| U2 | `MathScene_HasTenZonesAndTenNumberCards` | Scene has controller, 10 zones and 10 cards; each zone has a unique dot count 1–10 |
| U3 | `DotGroupZone_ParsesCountFromTargetId` | `dots_7`→7, `dots_1`→1, `dots_11`/`invalid`→0; no container means no rendering |
| U4 | `DragSelection_IsSingleAndZoneDetectionUsesLocalRectangles` | One drag at a time; all zones hit-test correctly |
| U5 | `DropOutsideAnyZone_ReturnsCardHome` | No request sent; card returns home |
| U6 | `CorrectResponse_MarksCardOnceAndNeverCompletesLocally` | `correct=true` marks one card; Unity does not complete locally |
| U7 | `IncorrectResponse_ReturnsCardAndDoesNotMarkIt` | `correct=false` returns card and marks nothing |
| U8 | `RepeatedResponse_IsIgnored` | Duplicate response IDs do not double-count |

Command (use `Start-Process -Wait`; plain `& Unity.exe` detaches; do not pass
`-quit`):

```powershell
$exe = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe'
$args = @(
  '-batchmode','-nographics',
  '-projectPath','<isolated project copy>',
  '-runTests','-testPlatform','EditMode',
  '-testFilter','CleanMyRoom.Tests.MathActivityTests',
  '-testResults','<temp>\task25_focused_results.xml',
  '-logFile','<temp>\task25_focused.log'
)
Start-Process -FilePath $exe -ArgumentList $args -Wait -PassThru
```

### 4.3 Regression suites

- Full Unity EditMode: all previously passing tests must still pass; the one
  pre-existing skip remains the only skip.
- Full Unity PlayMode: all tests must pass.
- Task #24's `TasteActivityTests` and all Issue #14/#15 tests are included in
  these suites.

### 4.4 End-to-end MATH over TCP — manual only

Not automated in this pass. Manual steps:

1. Start MATLAB on `127.0.0.1:55001` with the default data directory.
2. Open the Bedroom scene and press E at the Study Desk.
3. Confirm 10 number cards and 10 dot groups (55 dots total).
4. Drag 3 onto the 3-dot group; expect MATLAB `valid=true, correct=true`,
   success feedback/audio and a marked card.
5. Drag 3 onto the 4-dot group; expect `correct=false`, card return and error cue.
6. Stop MATLAB: expect disconnected status, disabled activities, no local completion.
7. Restart MATLAB and confirm automatic reconnection.
8. Solve all ten: confirm the star/completion message comes from MATLAB.
