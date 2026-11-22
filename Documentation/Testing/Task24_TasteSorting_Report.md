# Task #24 — Taste Sorting: Implementation & Test Report

## 1. Scope and outcome

Task #24 is implemented within the approved file list. The child uses the mouse
to drag eight food cards into four taste areas (Sweet, Sour, Bitter, Salty).
MATLAB owns the dataset, coordinate validation, answer validation, scoring and
completion. Unity collects input, sends `PLACE_REQ` and displays the returned
`PLACE_RES`; it never decides whether an answer is correct and never awards a
star locally.

Task #25 (Number Matching) was **not** modified. Its Number Board reference is
now approved (`README.md:30`: numbers 1–10 matched with equal-sized dot
groups), but its implementation checklist still requires explicit approval.

## 2. Files changed

### Created

| File | Purpose |
|---|---|
| `Shared_Data/ActivityData/taste.json` | Authoritative 8-food / 4-target dataset |
| `MATLAB/Tests/tTasteActivity.m` | MATLAB unit tests for dataset, validation and completion |
| `Unity/CleanMyRoom/Assets/Scripts/UI/Taste/TasteActivityController.cs` | Mouse input, coordinate conversion, request/response handling |
| `Unity/CleanMyRoom/Assets/Scripts/UI/Taste/TasteFoodItem.cs` | Draggable food card, identity and visual state |
| `Unity/CleanMyRoom/Assets/Scripts/UI/Taste/TasteDropZone.cs` | Drop-area identity and hit-test rectangle |
| `Unity/CleanMyRoom/Assets/Tests/Editor/TasteActivityTests.cs` | EditMode tests for interaction, mapping and response display |
| `Documentation/Testing/Task24_TasteSorting_TestPlan.md` | Test plan |
| `Documentation/Testing/Task24_TasteSorting_Report.md` | This report |

### Modified

| File | Purpose |
|---|---|
| `Unity/CleanMyRoom/Assets/Scenes/TasteSorting.unity` | Added 4 taste zones, 8 food cards and the activity controller |
| `Unity/CleanMyRoom/Assets/UI/Text/EnglishUiText.asset` | Added `taste.sweet/sour/bitter/salty` labels |
| `Unity/CleanMyRoom/Assets/Scripts/UI/UiTextKeys.cs` | Added the four matching key constants |

Unity-generated `.meta` files for the new script/test/folder were copied from
the isolated build project back into the repository so references remain valid.

## 3. Dependency status

| Item | Status | Evidence / note |
|---|---|---|
| Issue #14 (furniture interaction / stars) | LOCALLY VERIFIED | `Documentation/Testing/Issue14_Interaction_Tests.md`; Bedroom scene already points to `Assets/Scenes/TasteSorting.unity` |
| Issue #15 (UI/palette work) | PRESERVED | Previous local/uncommitted files untouched; full Unity suites still pass |
| PR #14 | REQUIRES CONFIRMATION | No local evidence; no GitHub operations performed |
| Issue #19 | REQUIRES CONFIRMATION | No local evidence found; no GitHub operations performed |
| Issue #22 | REQUIRES CONFIRMATION | No local evidence found; no GitHub operations performed |
| Issue #3 | REQUIRES CONFIRMATION | No local evidence found; no GitHub operations performed |

These statuses are reported honestly; no dependency was assumed satisfied.

## 4. MATLAB server integration

Read-only inspection confirmed that `cmr.activities.Registry` scans
`Shared_Data/ActivityData/*.json` at startup (`MATLAB/Activities/+cmr/+activities/Registry.m:63`)
and keys each definition by the JSON `id`. Therefore `taste.json` with
`"id": "TASTE"` is discovered automatically; `Unity` uses the matching
`ActivityIds.Taste = "TASTE"`. A server started before the file was added must
be restarted.

The live default registry currently contains only `TASTE`. `COLOR` and `MATH`
have no JSON files yet, so live requests for those activities would return
`UNKNOWN_ACTIVITY` — a pre-existing condition outside Task #24 scope.

## 5. Test environment

- MATLAB R2025b: `C:\Program Files\MATLAB\R2025b\bin\matlab.exe`
- Unity 6000.6.5f1: `C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe`
- Tests executed in isolated copy:
  `C:\Users\zahraa\AppData\Local\Temp\opencode\task24_scene_build`
- Raw result files remain in that temp folder (not inside the repository).

## 6. Actual test results

| Suite | Command / filter | Total | Passed | Failed | Skipped | Duration |
|---|---|---:|---:|---:|---:|---:|
| MATLAB `runAllTests` | full suite (includes `tTasteActivity`) | 300 | 300 | 0 | 0 | 71.0626 s |
| Unity EditMode focused | `CleanMyRoom.Tests.TasteActivityTests` | 7 | 7 | 0 | 0 | 0.4216755 s |
| Unity EditMode full | all EditMode tests | 79 | 78 | 0 | 1 | 7.401094 s |
| Unity PlayMode full | all PlayMode tests | 9 | 9 | 0 | 0 | 1.6359014 s |

Result XML headers:

- `task24_focused_results3.xml` — `testcasecount="7" result="Passed"`
- `task24_editmode_full3.xml` — `testcasecount="79" result="Passed"`
- `task24_playmode_full2.xml` — `testcasecount="9" result="Passed"`

The single EditMode skip is pre-existing (Issue #15 baseline also reported one
skipped EditMode test). No previously passing test failed.

### Issues found and fixed during implementation

1. Initial MATLAB run: one test errored because `verifyTrue` was called with
   printf-style extra arguments. Replaced with `sprintf(...)`; full suite then
   passed 300/300.
2. Initial Unity focused run: 5 tests failed because the synthetic food-card
   hit point was between two cards. Corrected the fixture coordinate from
   `(-200, 100)` to `(-270, 100)`; focused suite then passed 7/7.
3. First Unity invocation with `-quit` exited before running tests; the test
   command was corrected to the documented form without `-quit`.
4. Full EditMode in the isolated copy initially failed 5 `MeshImportTests`
   because that fixture resolves a path relative to the project's grandparent.
   Mirroring `Shared_Data/Meshes/Issue010` into the matching temp location made
   those tests pass; this was test-environment setup only.

## 7. Acceptance checklist

| # | Acceptance item | Status | Evidence |
|---|---|---|---|
| 1 | Task #24 implemented with 8 foods and 4 taste targets | PASS | M1, U2; `taste.json` |
| 2 | Mouse-only drag and drop, no hand/gesture/camera input | PASS | U3, U4; source inspection; no new input packages |
| 3 | MATLAB is the only authority for correctness | PASS | M2–M4, U5, U6; `Validator.m` used through `PLACE_RES` |
| 4 | MATLAB is the only authority for scoring/completion/star | PASS | M5, M6, U5; Unity tests assert no local completion |
| 5 | Unique request IDs and newline-delimited JSON TCP protocol | PASS | `NetworkManager.SendPlaceRequest`, U7; existing protocol reused |
| 6 | Unity displays feedback/audio from MATLAB verdict | PASS | U5, U6; controller calls `ShowSuccess/ShowError` and `PlaySuccess/PlayError` |
| 7 | Disconnection/reconnection behaviour unchanged | PASS (regression) | Existing suites pass; see manual step 6 |
| 8 | Task #24 independent from Task #25 | PASS | No Task #25 files created or modified |
| 9 | Previous Issue #14/#15 work preserved | PASS | Full EditMode 78/0/1 and PlayMode 9/0/0 |
| 10 | Automated MATLAB and Unity tests pass | PASS | Section 6 |
| 11 | End-to-end live TASTE game run with MATLAB TCP server | NOT RUN | Requires interactive GUI/server session; manual steps in test plan §4.4 |
| 12 | PR #14 / Issue #19 / Issue #22 / Issue #3 satisfaction | BLOCKED | REQUIRES CONFIRMATION; no GitHub operations allowed |

## 8. Manual verification instructions

1. Start MATLAB: `startServer` with default data directory (port 55001).
2. Start Unity and open the Bedroom scene; press E at the dining table.
3. Confirm the TasteSorting scene shows 4 labelled zones and 8 food cards.
4. Drag Honey to Sweet: expect MATLAB `correct=true`, green card state, success cue.
5. Drag Lemon to Salty: expect `correct=false`, card returns, error cue.
6. Stop MATLAB: expect clear disconnected status, activities disabled, no local completion.
7. Restart MATLAB: expect automatic reconnect and working activity.
8. Solve all eight: expect the star/completion message from MATLAB, not Unity.

## 9. Unresolved items

- Dependencies in §3 remain REQUIRES CONFIRMATION.
- The live end-to-end TASTE run (§7 item 11) was not executed; no GUI was opened for final display.
- `COLOR` and `MATH` activity JSON files are absent; live requests for those IDs return `UNKNOWN_ACTIVITY`. This is outside Task #24 scope and is not changed here.
- Note on file count: the approval message referenced “7 new files”, while the approved checklist enumerated 8 create paths (including both the test plan and this report). Both listed documentation files were created; please confirm if a different count was intended.

## 10. Git

No Git or GitHub operations were performed. All changes remain in the working directory for owner review.
