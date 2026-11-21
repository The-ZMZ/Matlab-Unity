# Issues #18–#21 — Test Report

## 1. Environment and date

- Date: 2026-11-21
- Project: Clean My Room
- Repository branch: `dev` (read-only Git inspection only)
- Unity project version: `6000.6.5f1`
- Unity CLI: `unity` on PATH (`C:\Users\manal\AppData\Local\Unity\bin\unity.exe`)
- MATLAB: R2025b `25.2.0.2998904` (`D:\3th year\bin\matlab.exe`)
- Input package: `com.unity.inputsystem` (existing project dependency)
- No camera, webcam, hand tracking, gesture, pinch or CV dependency was introduced.

## 2. Commands actually executed

### Unity EditMode — final run

```text
unity test "D:\3th year\Projects\Matlab-Unity\Unity\CleanMyRoom" ^
  --mode EditMode ^
  --output "C:\Users\manal\AppData\Local\Temp\opencode\unity-editmode-results-18-21.xml" ^
  --timeout 900 --non-interactive --format json
```

CLI result: `success: true`, no errors, no warnings.

### MATLAB full suite — final run

```text
& 'D:\3th year\bin\matlab.exe' -batch "addpath('MATLAB/Tests'); runAllTests();"
```

Run from repository root `D:\3th year\Projects\Matlab-Unity`.

## 3. Final Unity EditMode results

NUnit XML:
`C:\Users\manal\AppData\Local\Temp\opencode\unity-editmode-results-18-21.xml`

| Metric | Value |
|---|---|
| `testcasecount` | 84 |
| `total` | 84 |
| `passed` | 83 |
| `failed` | 0 |
| `inconclusive` | 0 |
| `skipped` | 1 |
| `duration` | 0.7913195 s |
| overall `result` | `Passed` |

### Per-fixture results

| Fixture | Tests | Passed | Failed | Skipped |
|---|---:|---:|---:|---:|
| `Issue14Tests` | 11 | 11 | 0 | 0 |
| `Issue16_KeyboardInputTests` | 6 | 6 | 0 | 0 |
| `Issue17_InteractionTrackingTests` | 4 | 4 | 0 | 0 |
| `Issue18_MouseInteractionStateTests` | 9 | 9 | 0 | 0 |
| `Issue19_ActivityPlacementGatewayTests` | 10 | 10 | 0 | 0 |
| `Issue20_MouseDragPlacementTests` | 9 | 9 | 0 | 0 |
| `Issue21_ColorSortingIdsTests` | 3 | 3 | 0 | 0 |
| `JsonCodecTests` | 11 | 11 | 0 | 0 |
| `MessageFramerTests` | 9 | 9 | 0 | 0 |
| `NetworkIntegrationTests` | 1 | 0 | 0 | 1 |
| `PlayerInputTests` | 3 | 3 | 0 | 0 |
| `PlayerTests` | 8 | 8 | 0 | 0 |

The only skipped test is `NetworkIntegrationTests.ConnectsPingsPlacesAndReconnects`. It is marked `Explicit` and requires a separately running MATLAB TCP server, so it was not executed in this automated EditMode run.

## 4. Final MATLAB results

| Metric | Value |
|---|---|
| Total | 109 |
| Passed | 109 |
| Failed | 0 |
| Incomplete | 0 |
| Duration | 15.49 s |

`tColorSorting` contributed 15 passed tests. A focused `tColorSorting` run was also executed successfully with 15 dots and no failures before the final full-suite run.

## 5. Test coverage by issue

### Issue #18 — mouse input/state machine

Executed and passed `Issue18_MouseInteractionStateTests` (9/9):

- starts idle without pending events;
- `PRESSED → HOLDING → RELEASE → IDLE` sequence;
- repeated down/hold frames do not duplicate press or hold events;
- rapid press/release sequences re-arm correctly;
- same-frame press and release emit both events;
- presses outside an interactable are ignored until a later deliberate press;
- `Reset()` clears state and latches;
- `Interrupt()` requires a physical release before another press;
- injectable `MouseInput.Source` and restoration to the Unity source.

### Issue #19 — placement gateway

Executed and passed `Issue19_ActivityPlacementGatewayTests` (10/10):

- top-left normalized coordinates, clamping and degenerate rect handling;
- offline gateway refuses placement with no local validation;
- `PLACE_REQ` fields and request-id correlation;
- correct, incorrect and error responses;
- responses for other request ids ignored;
- disconnect interruption;
- timeout without blocking;
- second request refused while one is pending.

### Issue #20 — drag/drop

Executed and passed `Issue20_MouseDragPlacementTests` (9/9):

- press selects and holding moves the item;
- release sends existing `PLACE_REQ` fields and a correct MATLAB reply keeps the item;
- incorrect reply restores the original pose;
- `ERROR` reply restores the pose and is reported as rejected;
- release without a target cancels and restores;
- second object cannot be dragged while one is held;
- communication failure at release restores;
- disconnect while dragging cancels and restores;
- deterministic disable handler cancels, restores and requires a fresh press.

Synthetic Unity physics fixtures (primitive colliders plus a deterministic ray provider) were used. No activity scene or prefab was fabricated.

### Issue #21 — Color Sorting

Executed and passed:

- `Issue21_ColorSortingIdsTests` (Unity): 3/3 — stable 12 item IDs and 6 basket IDs, uniqueness and key constants.
- `tColorSorting` (MATLAB): 15/15 — dataset structure, unique IDs, all object colors, correct/incorrect/case-insensitive comparison, malformed inputs, placement responses, unknown IDs, 12-item completion, duplicate/unknown filtering, apply-placement progress, generic `Definition`/`Validator`/`Session` compatibility, JSON consistency and Unity/MATLAB ID consistency.

## 6. Failures encountered and resolved

These were real executed failures, not hypothetical cases.

### Unity compile failure — first run

The first Unity run stopped before tests executed with three `CS0117` errors for `ProtocolConstants.ErrorUnknownObject`. `ProtocolConstants.cs` intentionally has no such constant. All three test references were replaced with the existing wire literal `"UNKNOWN_OBJECT"`; no protocol file was changed.

### Unity behavior failures — second run

- `Issue18_MouseInteractionStateTests.RapidPressReleaseSequencesRearmCorrectly` failed because one-shot events were cleared on every `Update()`, so a press event could be lost before the consumer read it. The state machine now latches press/hold/release events until consumed.
- `Issue20_MouseDragPlacementTests.DisablingControllerCancelsDragAndRequiresFreshPress` failed because Unity EditMode does not call `OnDisable` synchronously after `GameObject.SetActive(false)`. `OnDisable()` now delegates to a public `HandleActivityDisabled()` method that performs the same cancellation/interruption/detach logic, and the test calls that deterministic method.

Both fixes were followed by the final full run above: 83 passed, 0 failed, 1 skipped.

### MATLAB fixture failure — first full run

The first MATLAB full run failed only because `tColorSorting` initially passed a cell array to `PathFixture`. The fixture was corrected to:

```matlab
testCase.applyFixture(matlab.unittest.fixtures.PathFixture( ...
    root, 'IncludingSubfolders', true));
```

A detailed-output focused run then exceeded the shell timeout; the two lingering MATLAB processes were force-stopped. Subsequent focused and full runs passed. The final full run is the authoritative result above.

## 7. Not run / manual verification still required

| Item | Status | Reason / next step |
|---|---|---|
| Unity Play Mode | NOT RUN | Automated EditMode coverage passed; physical frame loop not executed. |
| Physical mouse drag/drop | NOT RUN | No hardware interaction in this environment. Use the manual steps below. |
| Activity scene-level drag/drop | NOT RUN | No activity scenes, item/basket prefabs or configured targets exist. Do not fabricate them. |
| `NetworkIntegrationTests.ConnectsPingsPlacesAndReconnects` | SKIPPED | Explicit test requiring an independently running MATLAB TCP server. |
| Full game ending / father star sequence | NOT RUN | Outside the scope of #18–#21; no activity scenes exist to drive it. |

### Manual verification instructions for the owner

1. Start MATLAB: `startServer()` from the repository MATLAB directory.
2. Open Unity and enter Play Mode with `NetworkManager` present.
3. Confirm the connection HUD reports connected and activities are enabled.
4. In a scene that has `DraggableItem` and `DropTarget` components with colliders, press and hold the left mouse button over an item, move it over a target and release.
5. Confirm:
   - a correct MATLAB answer keeps the item at the target;
   - an incorrect answer restores the original pose;
   - releasing over empty space restores the item;
   - disconnecting MATLAB during a drag restores the item;
   - a second item cannot be grabbed while a placement is pending.
6. Stop MATLAB and confirm Unity shows disconnected state and refuses placements without locally validating.

## 8. Git and artifact notes

- No Git write operations were performed: no staging, commit, push, merge, branch change, reset or rebase.
- No tracked teammate source file was overwritten. All implementation and tests are new untracked files; the pre-existing modified/untracked teammate work was left intact.
- Unity generated `.meta` files for all new C# files and the new `Placement/` and `Activities/` folders.
- No webcam, camera, hand, gesture, pinch, virtual cursor or CV code/dependency was added.

## 9. Conclusion

Issues #18, #19, #20 and #21 have automated Unity and MATLAB coverage, and the final executed suites are green: **Unity 83/83 executed EditMode tests passed (1 explicit skip)** and **MATLAB 109/109 tests passed**. Activity-level scene demonstration and physical mouse testing remain pending because the required scenes/prefabs are not present in the repository.
