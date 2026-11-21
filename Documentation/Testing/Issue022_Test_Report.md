# Issue #022 — Test Report

**Color Sorting Activity**

| Field | Value |
|---|---|
| Issue | #022 (GitHub #30) |
| Date | 2026-11-22 |
| Unity used | **6000.6.5f1 (3ff58d469c8a)**, Windows, URP |
| MATLAB used | `D:\stage3\bin\matlab.exe` (installed runner; project target remains MATLAB R2025b) |
| Scene build | **PASS** — saved `ColorSorting.unity` verified by EditMode scene tests (6 baskets, 12 toys, 151 objects) |
| EditMode tests | **66 passed, 0 failed, 1 skipped** (67 total, 4.55 s) |
| PlayMode tests | **14 passed, 0 failed, 0 skipped** (14 total, 2.70 s) |
| MATLAB tests | **204 passed, 0 failed, 0 incomplete** (204 total, 37.72 s) |
| New Issue #022 tests | 25 EditMode + 10 PlayMode + 6 MATLAB, all passed |
| Git operations | **NONE** (no stage, commit, push, merge, branch or history change) |

> No result below is claimed as passed unless it was actually executed. Raw
> runner XML is preserved in the two report files listed in §6, and the raw
> MATLAB runner log is preserved as `Issue022_MATLAB_TestResults.log`.

---

## 1. Checks executed

| # | Check | Platform | Result |
|---|---|---|---|
| 1 | Idempotent scene builder creates and saves `ColorSorting.unity` | Unity editor batch `-executeMethod` | **PASS** (exit 0) |
| 2 | Scene contains 6 labelled baskets and 12 sortable toys with colliders | Unity EditMode | **PASS** |
| 3 | Main Camera and Directional Light from Issue #011 are preserved | Unity EditMode | **PASS** |
| 4 | Build Settings and `SceneLoader` resolve `ColorSorting` | Unity EditMode | **PASS** |
| 5 | Correct/incorrect MATLAB replies are mirrored without local validation | Unity EditMode + PlayMode | **PASS** |
| 6 | Duplicate replies, invalid messages, errors, timeouts and disconnects do not score | Unity EditMode + PlayMode | **PASS** |
| 7 | Twelve validated placements plus MATLAB completion award exactly one star | Unity PlayMode + MATLAB | **PASS** |
| 8 | Return to Bedroom uses the existing `SceneLoader` path | Unity PlayMode | **PASS** |
| 9 | Real TCP protocol rejects a wrong basket and completes a twelve-object activity | MATLAB integration | **PASS** |
| 10 | MATLAB answer key defines all twelve scene objects and six targets | MATLAB unit | **PASS** |
| 11 | Three 1280×720 activity screenshots generated | Unity editor batch | **PASS** |

---

## 2. Unity EditMode results

Command used (batchmode, no graphics):

```text
Unity.exe -batchmode -nographics -disable-gpu -disable-assetdatabaseuds ^
  -projectPath D:\Matlab-Unity\Unity\CleanMyRoom ^
  -runTests -testPlatform EditMode ^
  -testResults D:\Matlab-Unity\Deliverables\Reports\Issue022_Unity_TestResults.xml ^
  -logFile C:\Temp\Issue022_Audit2_EditMode.log
```

Runner summary from the final audit rerun:

```text
testcasecount="67" result="Passed" total="67" passed="66"
failed="0" skipped="1" duration="4.550643"
```

Issue #022 EditMode coverage actually executed:

- `CleanMyRoom.Tests.ColorSorting.ColorSortingLogicTests` — 8 tests
  (MATLAB score mirroring, wrong/retryable placement, duplicate locks, invalid
  reply and protocol error, timeout/disconnect, completion gating, other-activity
  result ignored).
- `CleanMyRoom.Tests.ColorSorting.ColorSortingSceneTests` — 5 tests
  (scene asset/build registration, baskets, objects, controller/cursor/feedback
  wiring, reusable prefabs and materials).
- `CleanMyRoom.Tests.Issue022.ColorSortingEditModeTests` — 12 tests
  (catalogue, MATLAB answer-key agreement, progress state machine, GameManager
  mirroring, generated assets).

The single skipped test belongs to an earlier issue and is unchanged by this work.

---

## 3. Unity PlayMode results

Command used:

```text
Unity.exe -batchmode -nographics -disable-gpu -disable-assetdatabaseuds ^
  -projectPath D:\Matlab-Unity\Unity\CleanMyRoom ^
  -runTests -testPlatform PlayMode ^
  -testResults D:\Matlab-Unity\Deliverables\Reports\Issue022_PlayMode_TestResults.xml ^
  -logFile C:\Temp\Issue022_Audit2_PlayMode.log
```

Runner summary from the final audit rerun:

```text
testcasecount="14" result="Passed" total="14" passed="14"
failed="0" skipped="0" duration="2.6968554"
```

Issue #022 PlayMode coverage actually executed:

- `CleanMyRoom.Tests.ColorSorting.ColorSortingPlayModeTests` — 4 tests
  (real scene loads with 12 toys/6 baskets, wrong-then-correct retry, completion
  after twelve validated placements with one star, return to Bedroom).
- `CleanMyRoom.Tests.PlayMode.ColorSortingPlayModeTests` — 6 tests
  (controller wiring, correct/incorrect replies, disconnected gateway, completion
  event and GameManager star mirroring, keyboard/Escape navigation).

These tests use a deterministic `ISortingGateway` and inject protocol-shaped
MATLAB messages. They do **not** pretend that MATLAB is running and do not
validate answers locally.

---

## 4. MATLAB results

Command used:

```text
matlab.exe -batch "cd('D:\Matlab-Unity\MATLAB'); addpath('Tests'); runAllTests"
```

Summary:

```text
Total      : 204
Passed     : 204
Failed     : 0
Incomplete : 0
Duration   : 37.72 s
```

New Issue #022 MATLAB tests actually executed:

- `tColorSortingActivity` — 4 tests:
  real `color_sorting.json` has 6 targets/12 objects; validator accepts only the
  matching basket for every object; twelve correct placements award exactly one
  star; a wrong placement does not change progress.
- `tColorSortingProtocol` — 2 tests:
  a real `cmr.net.Server` on port 55102 and a real `tcpclient` verify that a wrong
  basket is rejected without score, that twelve validated placements send
  `PLACE_RES`, `ACTIVITY_RESULT` and `GAME_PROGRESS`, that the first star is
  awarded once, and that a duplicate request ID is ignored.

---

## 5. Important fix verified by these tests

MATLAB's `ACTIVITY_RESULT` message does **not** contain a `score` field; the
running score is carried by `PLACE_RES.score`. The completion gate now uses the
score mirrored from `PLACE_RES`, together with MATLAB's `completed` flag and
twelve confirmed object IDs. The architecture note and protocol example were
corrected to match the shipped wire format.

`ACTIVITY_RESULT` reaches `GameManager` only when `ColorSortingProgress` handled
the message and `activity == "COLOR"`. `GAME_PROGRESS` carries no request or
activity field, so the controller forwards it to
`GameManager.ApplyMatlabGameProgress`, whose `completedActivities` set is
idempotent. Together with the progress state's duplicate suppression, repeated
messages cannot award a second star.

---

## 6. Evidence files

| File | Contents |
|---|---|
| `Deliverables/Reports/Issue022_Unity_TestResults.xml` | Raw Unity EditMode runner XML (67 cases) |
| `Deliverables/Reports/Issue022_PlayMode_TestResults.xml` | Raw Unity PlayMode runner XML (14 cases) |
| `Deliverables/Reports/Issue022_MATLAB_TestResults.log` | Raw MATLAB `runAllTests` output (204 cases) |
| `Deliverables/Screenshots/Issue022_ColorSorting_Overview.png` | 1280×720 overview of the built activity |
| `Deliverables/Screenshots/Issue022_ColorSorting_Interaction.png` | 1280×720 drag/drop interaction state |
| `Deliverables/Screenshots/Issue022_ColorSorting_Completed.png` | 1280×720 completion state |
| `Documentation/Architecture/Issue022_Color_Sorting.md` | Architecture and responsibility distribution |

Screenshots were generated in editor batchmode with the scene builder's
`CaptureScreenshots` method. Their presence and 1280×720 PNG format were
verified; final visual review remains with the project owner.

---

## 7. Manual verification instructions (live MATLAB)

The automated Unity tests deliberately avoid a live socket. To repeat the full
game path manually:

1. Start the MATLAB server from the repository root:
   `matlab -batch "cd('MATLAB'); startServer('Verbose', true)"`
   (or run `startServer` in the MATLAB Command Window). It listens on
   `127.0.0.1:55001`.
2. Open `Assets/Scenes/ColorSorting.unity` in Unity and enter Play Mode.
3. Confirm the status label reads `MATLAB: CONNECTED`.
4. Drag each toy to the basket of the same colour, or use arrow keys + Space.
   The progress counter must advance from `0 / 12` to `12 / 12`.
5. Drag one toy to a wrong basket: the toy returns home, the message explains
   the mistake, and the counter does not advance.
6. Stop MATLAB while the scene is playing: the status becomes `DISCONNECTED`,
   dragging is disabled, and no answer is validated locally. Restart MATLAB and
   confirm the status and dragging recover.
7. After the twelfth correct placement, confirm the completion banner, one star,
   and the automatic or `Esc` return to the Bedroom scene.

---

## 8. Test status and limitations

- All automated suites listed above were executed and passed.
- The live Unity-to-MATLAB game path was **NOT RUN** as an automated test because
  it requires both applications to be started together; use the manual steps in
  §7. The real TCP protocol itself WAS tested independently in MATLAB §4.
- No camera, webcam, hand-tracking, gesture or pinch feature was introduced.
- No unresolved Issue #022 implementation defect is known at the time of this
  report. The Issue remains open for project-owner review and approval.
