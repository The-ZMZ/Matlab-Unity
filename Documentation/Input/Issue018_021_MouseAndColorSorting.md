# Issues #18–#21 — Mouse Input, Placement Bridge, Drag & Drop and Color Sorting

## 1. Scope and permanent decisions

This document describes the implementation of project Issues **#18, #19, #20 and #21** in the order requested.

It follows the permanent project decisions in `AGENTS.md`:

- Keyboard and mouse are the only approved input methods.
- No camera, webcam, hand tracking, gesture recognition, pinch detection, virtual hand cursor or computer-vision dependency was added.
- MATLAB remains the TCP server and the only authority for activity validation, scoring and completion.
- Unity only collects input, sends identifiers/positions, displays MATLAB decisions and applies presentation changes.
- The existing TCP/JSON protocol is reused unchanged. No new message type, field, socket or error code was introduced.

### Correction to earlier documentation

`Documentation/Input/Issue016_017_KeyboardInput.md` states that mouse select/drag/confirm is "owned by the existing activity code". At the inspection that preceded this work there was **no mouse input source, mouse state machine, drag/drop component, activity scene, activity prefab or configured drop target** in the repository. That sentence described an intended division of responsibility, not code that existed at the time. This document and the code listed below are the authoritative record for #18–#21.

## 2. State before this work

- Keyboard input (WASD/arrows/E), `InteractionInputTracker`, player movement, nearest-furniture interaction and TCP/JSON messaging already existed as prior uncommitted work.
- `NetworkManager.SendPlaceRequest(...)`, `WireMessage`, `JsonCodec`, `ProtocolConstants`, MATLAB `Server`, `Registry`, `Validator` and `Session` already existed.
- The existing protocol already contained `PING`, `PONG`, `PLACE_REQ`, `PLACE_RES`, `ACTIVITY_RESULT` and `GAME_PROGRESS`.
- There was no mouse abstraction, no reusable mouse drag/drop, no color dataset and no Color Sorting activity implementation.
- Bedroom furniture `scenePath` values remained empty. No activity scenes or prefabs were invented.

## 3. Issue #18 — Mouse input and interaction state machine

### Files created (Unity)

- `Assets/Scripts/Player/Input/MouseInteractionState.cs`
- `Assets/Scripts/Player/Input/MouseInputSnapshot.cs`
- `Assets/Scripts/Player/Input/IMouseInputSource.cs`
- `Assets/Scripts/Player/Input/UnityMouseInputSource.cs`
- `Assets/Scripts/Player/Input/MouseInput.cs`
- `Assets/Scripts/Player/Input/MouseInteractionStateMachine.cs`

### Behavior

- `MouseInputSnapshot` exposes only the system pointer position and left-button `PressDown`, `PressHeld` and `PressReleased` flags. It has no camera, hand or gesture data.
- `UnityMouseInputSource` reads the Unity Input System mouse. `MouseInput.Source` is injectable so EditMode tests can supply deterministic snapshots and restore the live source.
- `MouseInteractionStateMachine` moves through `IDLE → PRESSED → HOLDING → RELEASE → IDLE`.
- Press, hold and release events are latched one-shot events with matching `Consume...()` methods. They survive until consumed and are never re-emitted by themselves.
- A physical button-down produces at most one press event. It re-arms only after a full release.
- `Update(snapshot, pressTargetsInteractable)` rejects presses that did not hit an interactable object, so a click on empty space cannot become a grab.
- `Reset()` clears all state. `Interrupt()` resets and additionally requires a physical release before a new press can start; this prevents a stale held button from starting a new drag after a pause or disable.

## 4. Issue #19 — Keyboard/mouse communication adaptation

### Files created (Unity)

- `Assets/Scripts/Player/Input/MouseCoordinates.cs`
- `Assets/Scripts/Gameplay/Placement/IPlacementTransport.cs`
- `Assets/Scripts/Gameplay/Placement/NetworkPlacementTransport.cs`
- `Assets/Scripts/Gameplay/Placement/PlacementOutcome.cs`
- `Assets/Scripts/Gameplay/Placement/ActivityPlacementGateway.cs`

### Behavior

- `MouseCoordinates.Normalize(...)` converts a screen point into normalized activity-canvas coordinates with a top-left origin and y growing downward. Results are clamped to `[0, 1]`; a degenerate rectangle returns zero.
- `IPlacementTransport` is a narrow seam over the existing connection. `NetworkPlacementTransport` forwards directly to `NetworkManager` events and `SendPlaceRequest(...)`; it opens no new socket.
- `ActivityPlacementGateway` sends only `activityId`, `itemId`, `targetId` and normalized `x`/`y`. These map exactly to the existing `PLACE_REQ` fields.
- Incoming `PLACE_RES` and `ERROR` messages are correlated by `requestId`. `valid`/`correct`/`score`/`message`/`code` are copied into a `PlacementOutcome`; Unity never computes correctness.
- The gateway allows one pending placement at a time, exposes non-blocking `Update()` timeout handling, and raises `Interrupted` on connection loss.
- Disconnect, timeout and protocol error outcomes all let the caller safely restore a dragged object.
- `ProtocolConstants.cs` was intentionally not modified. Tests use the existing wire literal `"UNKNOWN_OBJECT"` rather than adding a Unity-only error-code constant.

## 5. Issue #20 — Reusable drag-and-drop

### Files created (Unity)

- `Assets/Scripts/Gameplay/Activities/DraggableItem.cs`
- `Assets/Scripts/Gameplay/Activities/DropTarget.cs`
- `Assets/Scripts/Gameplay/Activities/DragPlacementResult.cs`
- `Assets/Scripts/Gameplay/Activities/MouseDragPlacementController.cs`

### Behavior

- `DraggableItem` stores only a protocol `itemId` and the original pose. It contains no correct-target mapping.
- `DropTarget` stores only a protocol `targetId`. It contains no correctness logic.
- `MouseDragPlacementController` is activity-agnostic:
  - raycasts from the approved mouse cursor and selects the nearest `DraggableItem`;
  - moves it on a horizontal plane at the grabbed object's original height;
  - resolves the nearest non-draggable `DropTarget` under the release point;
  - normalizes the release point and sends a `PLACE_REQ` through the gateway;
  - keeps the object at the released position only when MATLAB returns a valid, correct `PLACE_RES`;
  - restores the original pose for incorrect, rejected, timed-out, interrupted, unavailable or targetless releases.
- Only one item can be dragged at a time. A second drag is refused while an item is held or a MATLAB request is pending.
- Disconnection while dragging cancels and restores the item.
- `OnDisable()` delegates to `HandleActivityDisabled()`, a public deterministic equivalent used by tests because Unity EditMode does not invoke `OnDisable` synchronously for `SetActive(false)`. It cancels the drag, interrupts the mouse latch and detaches transport listeners. `OnEnable()`/`ProcessFrame()` reattach them.
- `ProcessFrame(snapshot)` is the deterministic test entry point; the normal `Update()` reads `MouseInput.Source` and calls it.

## 6. Issue #21 — MATLAB Color Sorting validation

### Files created (MATLAB / data / Unity IDs)

- `MATLAB/Activities/ColorSorting/ColorSortingData.m`
- `MATLAB/Activities/ColorSorting/ValidateColorAnswer.m`
- `MATLAB/Activities/ColorSorting/FindColorItem.m`
- `MATLAB/Activities/ColorSorting/FindColorBasket.m`
- `MATLAB/Activities/ColorSorting/ColorBasketForItem.m`
- `MATLAB/Activities/ColorSorting/ColorPlacementResponse.m`
- `MATLAB/Activities/ColorSorting/ColorSortingCompletion.m`
- `MATLAB/Activities/ColorSorting/ColorSortingApplyPlacement.m`
- `MATLAB/Activities/ColorSorting/ColorSortingDefinition.m`
- `Shared_Data/ActivityData/COLOR.json`
- `Unity/CleanMyRoom/Assets/Scripts/Gameplay/Activities/ColorSortingIds.cs`

### Dataset

Six colors, two objects per color, twelve items total:

| Color / basket | Objects |
|---|---|
| Red / `basket_red` | `apple`, `strawberry` |
| Orange / `basket_orange` | `carrot`, `basketball` |
| Yellow / `basket_yellow` | `duck`, `star` |
| Green / `basket_green` | `leaf`, `frog` |
| Blue / `basket_blue` | `blue_car`, `balloon` |
| Purple / `basket_purple` | `grapes`, `butterfly` |

### Behavior

- `ColorSortingData` owns the item names, colors and basket identifiers in MATLAB.
- `ValidateColorAnswer` performs case-insensitive scalar-text comparison. It is the simple color comparison required by the original activity objective, with guards against malformed input.
- `FindColorItem`, `FindColorBasket` and `ColorBasketForItem` are MATLAB-side lookup helpers. Unity never receives the correct mapping for validation.
- `ColorPlacementResponse` returns the existing `valid`, `correct`, `code` and `message` fields plus descriptive color data for MATLAB-side use. Unknown ids use the existing `UNKNOWN_OBJECT` / `UNKNOWN_TARGET` protocol error codes.
- `ColorSortingCompletion` counts unique, known solved item ids; completion requires all twelve.
- `ColorSortingApplyPlacement` records only correct, unique placements and reports `newlySolved` and `completed`.
- `ColorSortingDefinition` adapts the dataset to the existing `cmr.activities.Definition` so the generic `Validator`/`Session` path also works.
- `Shared_Data/ActivityData/COLOR.json` is loaded by the existing `startServer.m` registry from `Shared_Data/ActivityData`.
- Unity's `ColorSortingIds` contains only stable string identifiers. It deliberately contains no item-to-basket answer map.

## 7. TCP/JSON protocol reuse

No protocol change was made.

- `PLACE_REQ` fields used: `type`, `requestId`, `activity`, `objectId`, `targetId`, `position.x`, `position.y`.
- `PLACE_RES` fields consumed: `type`, `requestId`, `valid`, `correct`, `score`, `message`.
- `ERROR` fields consumed: `type`, `requestId`, `code`, `message`.
- Completion still arrives through existing activity/session logic; Unity does not award stars locally.
- `ProtocolConstants.cs`, `Documentation/Communication/Protocol.md` and the MATLAB protocol class were left unchanged.

## 8. Files created for tests and documentation

### Unity EditMode tests

- `Assets/Tests/Editor/Issue18_MouseInteractionStateTests.cs` — 9 tests
- `Assets/Tests/Editor/Issue19_ActivityPlacementGatewayTests.cs` — 10 tests
- `Assets/Tests/Editor/Issue20_MouseDragPlacementTests.cs` — 9 tests
- `Assets/Tests/Editor/Issue21_ColorSortingIdsTests.cs` — 3 tests

### MATLAB tests

- `MATLAB/Tests/tColorSorting.m` — 15 tests

### Documentation

- `Documentation/Input/Issue018_021_MouseAndColorSorting.md` — this file
- `Documentation/Testing/Issue018_021_Test_Report.md`

Unity generated `.meta` files for every new C# asset and for the new `Placement/` and `Activities/` folders. No tracked teammate source file was overwritten.

## 9. Integration instructions for the project owner

The implementation is reusable but is not yet wired into activity scenes, because no activity scenes, item prefabs, basket prefabs or target configurations exist.

To integrate an activity later:

1. Add `MouseDragPlacementController` to the activity root.
2. Assign `activityCamera` (or leave `Camera.main`), set `activityId`, and choose screen-bounds or custom-rect normalization.
3. Add `DraggableItem` to each movable item and set its `itemId` from `ColorSortingIds.Items` (or a future activity's IDs).
4. Add `DropTarget` to each drop area and set its `targetId` from `ColorSortingIds.Baskets` (or the relevant constants).
5. Ensure items and targets have colliders. The controller ignores the dragged item's collider when searching for a drop target.
6. Keep `NetworkManager` in the scene so the default transport can reuse the existing connection.
7. Run MATLAB via `startServer.m` before interacting; Unity must not validate locally while disconnected.

No scene or prefab files were created or modified for these issues.

## 10. Known limitations / not run

- No activity-level drag/drop demonstration was possible: required scenes, prefabs and configured targets do not exist.
- Play Mode and physical mouse testing were not run.
- `NetworkIntegrationTests.ConnectsPingsPlacesAndReconnects` remains explicit/skipped; it requires a separately running MATLAB TCP server.
- Color Sorting completion is implemented and unit-tested in MATLAB, but no in-game star/father ending flow was added for this issue.

## 11. Git state

No Git write operations were performed. No staging, commit, push, merge, branch change, reset or rebase was executed. All changes remain in the working directory for the owner to review. The branch remained `dev`, ahead 3 / behind 4 relative to `origin/dev` during read-only inspection.
