# Issues #16 and #17 — Keyboard Input Foundation and Tracking

## Status

Issues #16 and #17 were originally written around webcam/hand input. `AGENTS.md` section 5 permanently excludes camera, webcam, hand tracking, hand detection, gesture recognition and pinch control, and directs older issues to preserve their valid objectives while adapting interaction to Keyboard and Mouse.

This implementation therefore keeps the valid objectives:

- #16: establish reusable input detection.
- #17: track input state for gameplay interaction without repeated actions.

Both are implemented in Unity using the Input System. No MATLAB, TCP/protocol, scene, UI, audio or gameplay logic was changed for these Issues.

## Controls actually used by the game

| Action | Keys | Consumer |
|---|---|---|
| Move child character | `W` `A` `S` `D` and arrow keys | `PlayerController` |
| Interact with nearby activity station | `E` | `InteractionController` |
| Mouse select / drag / confirm | Mouse | Existing activity code, unchanged |

No new key bindings were introduced. Movement and interaction were already established by Issues #13 and #14; this work centralises their detection and adds explicit state tracking.

## Files

| File | Purpose |
|---|---|
| `Unity/CleanMyRoom/Assets/Scripts/Player/Input/IKeyboardInputSource.cs` | Reusable keyboard snapshot source interface |
| `Unity/CleanMyRoom/Assets/Scripts/Player/Input/UnityKeyboardInputSource.cs` | Input System implementation of the source |
| `Unity/CleanMyRoom/Assets/Scripts/Player/Input/KeyboardInputSnapshot.cs` | Immutable key-down / held / key-up and movement state |
| `Unity/CleanMyRoom/Assets/Scripts/Player/Input/KeyboardInput.cs` | Single entry point and shared movement normalisation |
| `Unity/CleanMyRoom/Assets/Scripts/Player/Input/InteractionInputTracker.cs` | One interaction per E press, released by key-up |
| `Unity/CleanMyRoom/Assets/Scripts/Player/PlayerController.cs` | Existing movement component, now reads the shared input source |
| `Unity/CleanMyRoom/Assets/Scripts/Gameplay/InteractionController.cs` | Existing interaction component, now uses the latched E tracker |
| `Unity/CleanMyRoom/Assets/Tests/Editor/Issue16_KeyboardInputTests.cs` | Issue #16 EditMode tests |
| `Unity/CleanMyRoom/Assets/Tests/Editor/Issue17_InteractionTrackingTests.cs` | Issue #17 EditMode tests |

The new input code lives inside the existing `CleanMyRoom.Player` assembly. `CleanMyRoom.Gameplay` already references that assembly, so no assembly definition or package changes were needed.

## Issue #16 — input detection foundation

`KeyboardInput` exposes:

- `Read()` — one immutable `KeyboardInputSnapshot` per call.
- `ReadMovement()` — combined WASD/arrow vector.
- `ReadInteractPressed()` — raw E key-down state for simple tooling and tests.
- `NormalizeMovement()` — prevents diagonal movement from being faster.
- `Source` / `ResetToUnitySource()` — deterministic source injection for EditMode tests.

`KeyboardInputSnapshot` reports:

- `InteractDown` — E pressed this frame.
- `InteractHeld` — E currently held.
- `InteractReleased` — E released this frame.
- `MoveLeftHeld`, `MoveRightHeld`, `MoveDownHeld`, `MoveUpHeld`.
- `Movement` — normalised combined movement vector.
- `HeldGameKeyCount`, `HasInput`, `IsIdle`, `IsMoving`.

When no keyboard device is present, or no approved key is held, the snapshot is empty and `IsIdle` is true. Movement keys can be combined; opposing keys cancel. Missing keyboard hardware never throws.

`PlayerController.ReadKeyboardMovement()` now delegates to `KeyboardInput.ReadMovement()`. Its public API and normalized diagonal rule are unchanged, so Issue #13 tests remain valid.

## Issue #17 — interaction tracking

`InteractionInputTracker.ConsumePress()` converts the E key edge into one interaction request:

1. A key-down produces one `true` result.
2. Additional polls in the same frame produce `false` while the press is latched.
3. A held key never repeats the interaction.
4. A key-up clears the latch; the next deliberate press works again.
5. Movement keys and idle frames never produce an interaction.

`InteractionController.HandleInteractionInput()` now uses the tracker before calling `TryInteract()`. The existing nearest-target selection, prompt text, activity request event and scene navigation are unchanged. The tracker is also reset in `OnDisable()` so a transition cannot carry a stale latch into the next scene.

No cursor is derived from hand coordinates. In-game selection and drag/drop remain mouse-based and are owned by the existing activity code.

No `AudioManager` or `MainMenu` script exists in the current working tree. All existing Unity assets, including the scene, prefabs, materials, meshes, animations and teammate-owned Issue #13/#14 files, were left in place.

## Architecture boundaries

- Unity collects keyboard input.
- MATLAB continues to own coordinate mapping, hit testing and educational validation over the existing TCP/JSON protocol.
- No new message type was added.
- No camera, webcam, hand landmark, gesture, pinch or computer-vision code remains.
- No gameplay, UI, audio, communication, scene or MATLAB file was repurposed for these Issues.

## Manual verification in the Editor

1. Open `Assets/Scenes/Bedroom.unity`.
2. Enter Play mode.
3. Confirm `W/A/S/D` and arrow keys move the child.
4. Hold two movement keys and confirm diagonal movement is not faster.
5. Stand near a furniture prompt and confirm `E` opens the configured activity once.
6. Hold `E` and confirm it does not repeatedly request the activity.
7. Release `E`, press again, and confirm a new request is accepted.
8. Release all keys and confirm the character stops and the interaction state returns to idle.
9. Confirm no camera or hand-tracking panel appears anywhere.
