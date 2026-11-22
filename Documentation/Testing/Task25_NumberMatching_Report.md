# Task #25 — Number Matching: Implementation & Test Report

## 1. Scope and outcome

Task #25 is implemented within the approved checklist. Numbers 1–10 are
matched to dot groups of the same size (`README.md:30`). The child uses the
mouse to drag ten number cards onto ten dot-group zones. MATLAB owns
`math.json`, validation, scoring and completion; Unity collects pointer input,
sends `PLACE_REQ` and displays `PLACE_RES`. It never validates an answer or
awards a star locally.

Approved decisions were followed exactly: Unity renders dots from target ids
(`dots_1`…`dots_10`), the TCP protocol is unchanged (no new fields or message
types), and all ten cards/zones are shown simultaneously.

Task #24 and all Issue #14/#15 work were preserved.

## 2. Files changed

### Created

| File | Purpose |
|---|---|
| `Shared_Data/ActivityData/math.json` | Authoritative 10-number / 10-dot-group dataset |
| `MATLAB/Tests/tMathActivity.m` | MATLAB tests for dataset, validation and completion |
| `Unity/CleanMyRoom/Assets/Scripts/UI/Math/MathActivityController.cs` | Mouse input, coordinate conversion, request/response handling |
| `Unity/CleanMyRoom/Assets/Scripts/UI/Math/NumberCard.cs` | Draggable number card; identity and visual state only |
| `Unity/CleanMyRoom/Assets/Scripts/UI/Math/DotGroupZone.cs` | Dot-group zone; parses count from target id and renders dots |
| `Unity/CleanMyRoom/Assets/Tests/Editor/MathActivityTests.cs` | EditMode tests for interaction, rendering data and response display |
| `Documentation/Testing/Task25_NumberMatching_TestPlan.md` | Test plan |
| `Documentation/Testing/Task25_NumberMatching_Report.md` | This report |

### Modified

| File | Purpose |
|---|---|
| `Unity/CleanMyRoom/Assets/Scenes/MathActivity.unity` | Added `MathActivityCanvas`, controller, 10 number cards and 10 dot-group zones; preserved camera, light and the three UI prefab instances |

Unity-generated `.meta` files for the new Math folder, scripts and test were
copied from the isolated build project back into the repository.

No shared scripts, protocol files, text catalog files, or Task #24 files were
modified. No new message types or fields were introduced.

## 3. Dependency status

| Item | Status | Evidence / note |
|---|---|---|
| Task #24 (Taste Sorting) | LOCALLY VERIFIED | Preserved; its tests still pass in the full suites |
| Issue #14 navigation / Study Desk → `MATH` | LOCALLY VERIFIED | Bedroom scene and Issue #14 tests unchanged |
| Issue #15 UI tests on Math scene | LOCALLY VERIFIED | Three prefab instances preserved; PlayMode UI test still passes |
| PR #14 | REQUIRES CONFIRMATION | No local evidence; no GitHub operations performed |
| Issue #19 | REQUIRES CONFIRMATION | No local evidence; no GitHub operations performed |
| Issue #22 | REQUIRES CONFIRMATION | No local evidence; no GitHub operations performed |
| Issue #3 | REQUIRES CONFIRMATION | No local evidence; no GitHub operations performed |

## 4. Server integration

`cmr.activities.Registry.loadDirectory()` scans `Shared_Data/ActivityData/*.json`
at startup and registers definitions by JSON `id`. Adding `math.json` with
`"id": "MATH"` therefore registers MATH automatically; Unity's
`ActivityIds.Math = "MATH"` matches. A server started before the file was added
must be restarted.

No protocol changes were made. `PLACE_REQ`/`PLACE_RES`/`ACTIVITY_RESULT`/
`GAME_PROGRESS` are reused unchanged, and dot counts are display data only.

## 5. Test environment

- MATLAB R2025b: `C:\Program Files\MATLAB\R2025b\bin\matlab.exe`
- Unity 6000.6.5f1: `C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe`
- Isolated project copy:
  `C:\Users\zahraa\AppData\Local\Temp\opencode\task25_scene_build`
- Raw result files remain in that temp folder (not inside the repository).

## 6. Actual test results

| Suite | Command / filter | Total | Passed | Failed | Skipped | Duration |
|---|---|---:|---:|---:|---:|---:|
| MATLAB `runAllTests` | full suite (includes `tMathActivity`) | 306 | 306 | 0 | 0 | 58.8815 s |
| Unity EditMode focused | `CleanMyRoom.Tests.MathActivityTests` | 8 | 8 | 0 | 0 | 1.0961845 s |
| Unity EditMode full | all EditMode tests | 87 | 86 | 0 | 1 | 7.4159735 s |
| Unity PlayMode full | all PlayMode tests | 9 | 9 | 0 | 0 | 1.5018103 s |

Result XML headers:

- `task25_focused_results.xml` — `testcasecount="8" result="Passed"`
- `task25_editmode_full.xml` — `testcasecount="87" result="Passed"`
- `task25_playmode_full.xml` — `testcasecount="9" result="Passed"`

The single EditMode skip is pre-existing (also present in the Issue #15 and
Task #24 baselines). No previously passing test failed. MATLAB increased from
300 to 306 tests: the six new `tMathActivity` tests all passed. Unity EditMode
increased from 79 to 87 tests: the eight new `MathActivityTests` all passed.

### Scene verification (batch-mode inspection)

`MathActivity.unity` contains 10 `Zone_dots_*` objects, 10 `Number_*` cards,
55 `Dot_*` visuals (1+2+…+10), one `MathActivityController`, and all three
Issue #15 UI prefab instances with `instructions.math` intact.

### Issues found and fixed

No implementation defects were found during testing. One proactive correction
was made before the test run: the scene-count assertion was made order-independent
because `FindObjectsByType` does not guarantee ordering. One namespace ambiguity
was avoided by fully qualifying `System.Math.Min` in `DotGroupZone.cs`.

## 7. Acceptance checklist

| # | Acceptance item | Status | Evidence |
|---|---|---|---|
| 1 | Numbers 1–10 matched with equal dot groups | PASS | M1–M3; `math.json`; 55 rendered dots |
| 2 | All 10 cards and 10 zones shown simultaneously | PASS | U2; scene inspection |
| 3 | Mouse-only drag and drop; no hand/gesture/camera input | PASS | U4, U5; source inspection; no new packages |
| 4 | MATLAB is the only authority for correctness | PASS | M2–M4, U6, U7; `Validator.m` via `PLACE_RES` |
| 5 | MATLAB is the only authority for scoring/completion/star | PASS | M5, M6, U6; Unity asserts no local completion |
| 6 | Protocol unchanged; no new fields or message types | PASS | Source inspection; existing constants reused |
| 7 | Unity renders dots from target ids only | PASS | U3; `DotGroupZone` parses `dots_N` |
| 8 | Task #24 and Issue #14/#15 work preserved | PASS | Full EditMode 86/0/1 and PlayMode 9/0/0 |
| 9 | Automated MATLAB and Unity tests pass | PASS | Section 6 |
| 10 | End-to-end live MATH game run with MATLAB TCP server | NOT RUN | Requires interactive GUI/server session; manual steps in test plan §4.4 |
| 11 | PR #14 / Issue #19 / Issue #22 / Issue #3 satisfaction | BLOCKED | REQUIRES CONFIRMATION; no GitHub operations allowed |

## 8. Manual verification instructions

1. Start MATLAB: `startServer` with the default data directory (port 55001).
2. Start Unity, open Bedroom, press E at the Study Desk.
3. Confirm 10 number cards and 10 dot groups containing 1–10 dots.
4. Drag 3 to the 3-dot group: expect MATLAB `correct=true`, success cue, green card.
5. Drag 3 to the 4-dot group: expect `correct=false`, card returns, error cue.
6. Stop MATLAB: expect disconnected status, disabled activities, no local completion.
7. Restart MATLAB: expect automatic reconnect and working activity.
8. Solve all ten: expect star/completion message from MATLAB, not Unity.

## 9. Unresolved items

- Dependencies in §3 remain REQUIRES CONFIRMATION.
- Live end-to-end MATH run (§7 item 10) was not executed; no Unity GUI was
  opened for final display. No manual E2E result is claimed.
- `COLOR` has no activity JSON, so a live three-star playthrough still cannot
  complete. This is outside Task #25 scope and was not changed.
- Dot rendering is display-only in Unity, as approved; no MATLAB-generated
  layout data was added.

## 10. Git

No Git or GitHub operations were performed. All changes remain in the working directory for owner review.
