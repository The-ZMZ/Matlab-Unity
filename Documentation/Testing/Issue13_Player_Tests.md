# Issue #13 — Child Character & Basic Movement: Test Plan

## Scope

Tests for the child placeholder character, keyboard movement, Idle/Walk animation
configuration, camera following, play-area limits and collision in the Bedroom scene.

Environment used for the automated run:

- Unity 6000.6.5f1 (Windows, PCWIN64)
- Project: `Unity/CleanMyRoom`
- Test platform: EditMode

No MATLAB functionality was changed or tested as part of this issue.

## Automated tests

Added two EditMode fixtures:

### `CleanMyRoom.Tests.PlayerTests`

| Test | What it verifies |
|---|---|
| `Prefab_HasOneControllerAnimatorAndAnimatedLimbs` | `ChildCharacter.prefab` has one `CharacterController`, no extra primitive colliders, a `PlayerController`, an `Animator` with a controller, and the animated arm/leg pivots. |
| `AnimatorController_HasIdleAndWalkWithSpeedDrivenTransitions` | `ChildCharacter.controller` has a float `Speed` parameter, looped Idle and Walk clips, Idle as default, and smooth (0.15 s) non-exit-time transitions driven by `Speed` thresholds. |
| `BedroomScene_HasPlayerFollowCameraAndRoomColliders` | `Bedroom.unity` contains the player at the room origin, the Main Camera has `FollowCamera` targeting the player with a wall-clearing minimum height, and floor/wall colliders still exist. |
| `BedroomScene_IsTheFirstEnabledBuildScene` | The bedroom is the first enabled scene in Build Settings, so the player appears when the game starts. |
| `DiagonalInputIsNormalised` | Combined input `(1,1)` is reduced to unit length. |
| `Movement_StaysInsideConfiguredPlayArea` | Movement is clamped to the configured X/Z bounds. |
| `SolidWallCollider_BlocksPlayerMovement` | A `CharacterController` moves toward a wall and is stopped by its collider. |
| `FollowCamera_SmoothlyFollowsAndStaysAboveWalls` | The camera moves toward a displaced target over successive frames and never drops below the wall height. |

### `CleanMyRoom.Tests.PlayerInputTests`

Uses the Input System test fixture with a simulated keyboard device.

| Test | What it verifies |
|---|---|
| `WasdKeysProduceExpectedMovementVectors` | W/S/A/D each produce the expected forward/back/left/right vector. |
| `ArrowKeysProduceExpectedMovementVectors` | Arrow keys produce the same four vectors as WASD. |
| `MixedInputCancelsOpposingKeysAndNormalisesDiagonals` | Opposing keys cancel, and the remaining diagonal is normalised. |

## Automated result (executed 2026-11-20)

Command (from the project root):

```text
Unity.exe -batchmode -nographics -projectPath Unity/CleanMyRoom ^
  -runTests -testPlatform EditMode
```

Result: **32 total, 31 passed, 0 failed, 1 skipped**.

The single skipped test is the pre-existing explicit
`NetworkIntegrationTests.ConnectsPingsPlacesAndReconnects`, which requires a running
MATLAB server and is outside the scope of Issue #13.

An earlier focused run of only the Issue #13 fixtures reported **11 total, 11 passed,
0 failed**.

## Manual verification steps (not yet executed in PlayMode)

1. Open `Unity/CleanMyRoom` in Unity 6000.6.5f1 and load `Assets/Scenes/Bedroom.unity`.
2. Enter Play Mode. The child character should be visible near the centre of the room.
3. Hold W/S/A/D and confirm movement in the matching directions; repeat with the arrow keys.
4. Hold two movement keys at once (for example W+D) and confirm diagonal movement is not faster.
5. Release all keys. The character should settle into the Idle animation; holding a key should show the Walk animation.
6. Move toward the side walls, back wall and front edge. The character should stop at the walls/floor boundary without passing through.
7. Observe the camera during movement: it should follow smoothly, keep the character framed and stay above the walls.
8. Confirm no MATLAB/network or unrelated Unity behaviour changed.

## Known limitations

- The character is a deliberately simple placeholder built from Unity primitives because no suitable free/CC0 child character asset was available locally. No external asset was downloaded, so there is no third-party licence to record.
- The current bedroom scene contains the floor and three walls but no furniture yet. Collision with future furniture will work when those objects have colliders, but could not be visually exercised in this issue.
- Animation and camera configuration were verified by EditMode tests; visual PlayMode confirmation is listed above for the project owner.
- Camera and hand tracking remain permanently excluded. `FollowCamera` is the in-game scene camera only.
