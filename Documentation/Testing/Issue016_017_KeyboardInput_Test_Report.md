# Issues #16 and #17 — Keyboard Input Test Report

## Environment

- Date: 2026-11-21
- MATLAB: R2025b `25.2.0.2998904` (Windows, PCWIN64)
- Unity project version: `6000.6.5f1`
- Unity CLI: `1.0.0-beta.12`
- Input package: `com.unity.inputsystem` `1.20.0`
- Branch: `dev` (read-only Git inspection only; no Git write operations)
- No camera, webcam, hand-tracking, gesture or pinch implementation remains in the source tree.

## Commands executed

### Unity EditMode suite

```text
unity test "D:\3th year\Projects\Matlab-Unity\Unity\CleanMyRoom" ^
  --mode EditMode ^
  --output "C:\Users\manal\AppData\Local\Temp\opencode\unity-editmode-results.xml" ^
  --timeout 900 --non-interactive --format json
```

### MATLAB regression suite

```text
matlab -batch "addpath('D:\3th year\Projects\Matlab-Unity\MATLAB\Tests'); runAllTests();"
```

## Results

### Unity EditMode — final run

```text
testcasecount : 53
passed        : 52
failed        : 0
skipped       : 1
duration      : 0.729186 s
result        : Passed
```

The single skipped case is `NetworkIntegrationTests.ConnectsPingsPlacesAndReconnects`. It is explicitly marked and requires a running MATLAB TCP server; it was not run in this EditMode pass.

### New Issue #16 tests

| Fixture | Tests | Passed | Result |
|---|---:|---:|---|
| `Issue16_KeyboardInputTests` | 6 | 6 | Passed |

Covered: idle/no-device snapshot, WASD and arrow keys, simultaneous movement keys, opposing-key cancellation, key-down/held/released for E, and movement plus interaction in one frame.

### New Issue #17 tests

| Fixture | Tests | Passed | Result |
|---|---:|---:|---|
| `Issue17_InteractionTrackingTests` | 4 | 4 | Passed |

Covered: no-input interaction, movement keys not triggering interaction, one action per press with release/re-press, and `InteractionController` integration that fires exactly once per deliberate E press.

### Existing Unity suites preserved

| Fixture | Tests | Passed | Result |
|---|---:|---:|---|
| `Issue14Tests` | 11 | 11 | Passed |
| `PlayerInputTests` | 3 | 3 | Passed |
| `PlayerTests` | 8 | 8 | Passed |
| `JsonCodecTests` | 11 | 11 | Passed |
| `MessageFramerTests` | 9 | 9 | Passed |
| `NetworkIntegrationTests` | 1 | 0 | Explicit / skipped |

Issue #13 player movement and Issue #14 interaction, prompt, progress/stars and scene-navigation behaviour all remain green after the input refactor.

### MATLAB regression suite

```text
Total      : 94
Passed     : 94
Failed     : 0
Incomplete : 0
Duration   : 30.83 s
```

The previous hand-tracking MATLAB tests were removed, so the suite returned from 171 to the 94 pre-existing tests. All remaining MATLAB tests pass.

## Verification notes

- An initial Unity run found one failure in the new `InteractionControllerFiresOncePerPress` test: the test sampled a fresh E press without first sampling the release frame, so the tracker latch correctly suppressed it. The test was corrected to sample the release frame like a real frame loop. The final run above is the actual passing result.
- Keyboard input was simulated with Unity's `InputTestFixture` and a real `Keyboard` device added to the test input system. No physical keyboard hardware was used.
- No test result was fabricated. The Unity results come from the generated NUnit XML report; the MATLAB result comes from `runAllTests()`.
- Source searches of `MATLAB`, `Unity/CleanMyRoom/Assets/Scripts`, `Documentation` and `Deliverables` found no webcam, hand tracking, hand detection, landmark, gesture, pinch or `HAND_STATE` implementation. Documentation mentions these only to state that they are excluded.

## Not run / not verified

- Unity Play mode in the Editor (runtime input with a real hardware keyboard).
- Physical keyboard and mouse behaviour on the target machine.
- Mouse drag/drop activity interactions beyond the existing automated suites.
- The explicit MATLAB–Unity network integration test (requires the server to be started separately).
- Any UI/Audio runtime behaviour. No `AudioManager` or `MainMenu` script exists in the current working tree; all existing Unity assets were left untouched.

## CLI notification

The Unity CLI reported an available update: `1.0.0-beta.12` → `1.0.1`. The remediation requires user approval, so it was not applied.
