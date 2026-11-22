# Issue #022 — Color Sorting Activity

| Item | Value |
|---|---|
| Issue | #022 (GitHub #30) |
| Scene | `Assets/Scenes/ColorSorting.unity` |
| Activity id | `COLOR` |
| MATLAB answer key | `Shared_Data/ActivityData/color_sorting.json` |
| Unity controller | `Assets/Scripts/ColorSorting/ColorSortingController.cs` |
| Builder | `Assets/Scripts/Editor/Issue022_ColorSortingBuilder.cs` |
| Input | Mouse drag-and-drop and keyboard fallback |
| Validation authority | MATLAB only |

---

## 1. Scope and approved adaptation

This Issue makes the existing Color Sorting scene playable: twelve toys are sorted
into six colour baskets, feedback is displayed, and one star is recorded when
MATLAB reports that the activity is complete.

The original request used the phrase “virtual hand cursor / hand-controlled
interaction”. The permanent project rules exclude camera, webcam, hand tracking,
hand detection, gesture recognition and pinch detection. The implementation
therefore uses the approved **mouse and keyboard** input methods and provides a
visible in-scene **pointer cursor** as the non-camera equivalent of a virtual
hand. No camera or hand-tracking code, package or protocol message was added.

The agreed story and activity structure are unchanged. This Issue implements only
the first bedroom activity; Taste Sorting, Mathematics and the father's ending
remain separate work.

---

## 2. Files and responsibilities

### 2.1 Unity runtime

| File | Responsibility |
|---|---|
| `ColorSortingCatalog.cs` | Presentation-only basket/toy ids, labels, colours and shapes. It deliberately contains no answer key. |
| `ColorSortingController.cs` | Collects mouse/keyboard input, maps the drop point to normalized coordinates, sends `PLACE_REQ`, subscribes to `NetworkManager`, and displays MATLAB replies. |
| `ColorSortingProgress.cs` | Plain C# state machine that mirrors MATLAB messages: pending requests, duplicate suppression, score/star values, timeouts and completion event. It never computes correctness. |
| `SortingGateway.cs` | `ISortingGateway` seam and production `MatlabSortingGateway` wrapper around the existing `NetworkManager.SendPlaceRequest`. It only forwards requests. |
| `SortableObject.cs` | Toy identity, home position, drag movement and snap-to-basket presentation. |
| `ColorBasket.cs` | Basket identity, hover highlight and snap point. It never accepts or rejects an object. |
| `ColorSortingFeedback.cs` | Text, progress, connection status, badges and completion banner. |
| `ActivityPointerCursor.cs` | Visible pointer disc/tip driven by mouse or keyboard movement. |
| `ActivityAudioManager.cs` | Runtime-generated correct, wrong and completion tones; no external audio assets were available. |
| `CleanMyRoom.ColorSorting.asmdef` | Runtime assembly referencing the existing Core and Net assemblies. |

### 2.2 Existing systems reused

- `SceneLoader.LoadColorSorting()` / `SceneLoader.LoadBedroom()`.
- `NetworkManager` TCP client, `ConnectionState`, `WireMessage` and
  `ProtocolConstants`.
- Persistent `GameManager` from Issue #011. It was extended only to mirror
  MATLAB-reported progress and to expose a completion event; it still performs no
  validation.
- The existing Main Camera and Directional Light in the activity scene.

### 2.3 MATLAB-owned data

`Shared_Data/ActivityData/color_sorting.json` was added because the existing
MATLAB validator loads activity definitions from that folder and the folder was
otherwise empty. It defines:

- activity id `COLOR`;
- twelve object ids, `object_01` … `object_12`;
- six target ids, `basket_red`, `basket_blue`, `basket_green`, `basket_yellow`,
  `basket_orange`, `basket_purple`;
- the object-to-basket answer key used by MATLAB.

No MATLAB validation code was copied into Unity. The builder reads this file only
to arrange the still image for the completion screenshot; that editor-only staging
is not part of gameplay.

---

## 3. Scene construction

`CleanMyRoom.Editor.Issue022.ColorSortingBuilder.Build` is idempotent. It opens
the existing `ColorSorting.unity`, removes only its previous activity root, and
recreates:

- a soft-coloured sorting room with floor, walls and bunting;
- a large sorting table;
- six labelled baskets with coloured rims;
- twelve toys, two per colour, with sphere/cube shapes and labels;
- instruction and feedback text;
- a visible pointer cursor;
- an activity controller wired to the camera, cursor, feedback, toys and baskets;
- warm point lights and presentation settings.

Generated assets live in:

| Folder | Contents |
|---|---|
| `Assets/Materials/ColorSorting/` | URP/Lit materials for the room, board, baskets, cursor and six colours |
| `Assets/Prefabs/ColorSorting/` | `PF_ColorBasket`, `PF_SortableToy`, `PF_ActivityPointer` |

The builder preserves the pre-existing Main Camera and Directional Light objects
and updates their activity-specific framing and lighting.

---

## 4. Interaction flow

1. Unity checks `ISortingGateway.ActivitiesEnabled`. When MATLAB is disconnected,
   dragging is disabled, the status text says so, and no answer can be submitted.
2. Mouse: a ray from `Input.mousePosition` is intersected with the drag plane.
   The nearest unplaced toy under the pointer is picked up, moved with the
   pointer, and dropped on the basket under the pointer.
3. Keyboard: arrow keys move the same pointer; `Space` picks up the nearest toy
   and drops it on the nearest basket; `Esc` returns to the Bedroom.
4. The drop point is clamped to the activity board and normalized:
   `x = InverseLerp(boardMinX, boardMaxX, worldX)`,
   `y = InverseLerp(boardMaxZ, boardMinZ, worldZ)` so the protocol origin remains
   top-left.
5. Unity sends `PLACE_REQ` with a unique `requestId`, `activity = "COLOR"`,
   `objectId`, `targetId` and the normalized `position`.
6. MATLAB validates against the authoritative answer key and replies with
   `PLACE_RES`, then `ACTIVITY_RESULT`, then `GAME_PROGRESS` on completion.
7. Unity displays the returned correctness, feedback text, score and stars. It
   never sets `correct`, `score`, `completed` or `stars` locally.

Repeated drops of the same toy, late/duplicate replies, invalid responses,
protocol errors, timeouts and disconnects are handled without increasing score
or confirming the object.

---

## 5. Progress and star rules

`ColorSortingProgress` changes state only from MATLAB messages:

- `PLACE_RES.valid = true` and `correct = true` confirms the object id;
- `PLACE_RES.score` raises the displayed matched count. The shipped MATLAB server
  sends the running score only in `PLACE_RES`; `ACTIVITY_RESULT` and
  `GAME_PROGRESS` carry no score field. `ColorSortingProgress` also tolerates an
  optional score field on a MATLAB `ACTIVITY_RESULT`, but no score is computed
  or invented in Unity;
- `ACTIVITY_RESULT.stars` and `GAME_PROGRESS.stars` raise the displayed star total;
- completion is raised only when **all** of the following are true:
  - `ACTIVITY_RESULT.activity == "COLOR"`;
  - `ACTIVITY_RESULT.completed == true`;
  - the MATLAB-reported score (`PLACE_RES.score` in the shipped protocol) is at least 12;
  - all twelve distinct objects were confirmed by correct `PLACE_RES` replies;
  - the completion has not already been raised.

Each of the first eleven placements produces an `ACTIVITY_RESULT` with
`completed = false`; only the twelfth produces `completed = true` and one star.
A repeated completed result for the same request is treated as a duplicate, and
the controller skips mirroring any result that the progress state rejected, so a
repeated message cannot increase `GameManager.TotalStars`.

`GameManager.ApplyMatlabActivityResult` and `ApplyMatlabGameProgress` mirror the
same MATLAB values into the persistent session and raise
`ActivityCompletedFromMatlab` once per activity. The star displayed by this
activity is therefore reported by MATLAB, not calculated in Unity.

---

## 6. Protocol messages used

Only the existing message types are used:

```json
{"type":"PLACE_REQ","requestId":"issue022_req_001","activity":"COLOR",
 "objectId":"object_01","targetId":"basket_red","position":{"x":0.45,"y":0.62}}
```

```json
{"type":"PLACE_RES","requestId":"issue022_req_001","activity":"COLOR",
 "valid":true,"correct":true,"position":{"x":0.45,"y":0.62},
 "score":1,"message":"Correct placement"}
```

```json
{"type":"ACTIVITY_RESULT","requestId":"issue022_req_012","activity":"COLOR",
 "correct":true,"completed":true,"stars":1,
 "message":"Correct placement"}
```

```json
{"type":"GAME_PROGRESS","stars":1,
 "completedActivities":["COLOR"],"gameCompleted":false}
```

`ERROR` messages with a matching `requestId` clear the pending placement and show
MATLAB's error. No camera or hand-tracking message type exists.

---

## 7. Feedback, audio and navigation

- Correct/incorrect MATLAB replies drive coloured text, badges and a short tone.
- Completion shows the green banner, completion tone and MATLAB's star count.
- The connection label shows `MATLAB: CONNECTED` or `DISCONNECTED`; while
  disconnected, activity input is disabled and in-flight drops are cancelled.
- `Esc` and the automatic post-completion delay use
  `SceneLoader.LoadBedroom()`. Unity never ends the activity locally.

---

## 8. Test seams

- `ColorSortingController.Gateway` accepts a test `ISortingGateway`, so tests can
  send requests without opening a socket.
- `ColorSortingController.HandleIncomingMessage(WireMessage)` accepts real
  protocol-shaped MATLAB messages, so tests can inject replies without a server.
- `ColorSortingProgress` is a plain C# class, so the state machine is testable in
  EditMode.
- PlayMode tests load the real scene and assert the real controller wiring,
  counters, feedback and navigation.

The existing explicit `NetworkIntegrationTests` remains the separate end-to-end
check against a running MATLAB server.

---

## 9. Responsibility distribution

This Issue preserves the project-wide approximate 70/30 computational split:

- **MATLAB**: answer key, placement validation, score, activity completion, stars,
  game progress and error responses.
- **Unity**: scene presentation, input collection, coordinate conversion, request
  transport, state mirroring, feedback, audio, screenshots and navigation.

Unity's coordinate math supports interaction only; it does not participate in the
educational decision.

---

## 10. Known limitations

- No player-character walkthrough is part of this Issue; entry to the activity
  uses the existing scene navigation API. Bedroom-to-activity station wiring is
  separate follow-on work.
- Re-entering the scene after MATLAB has already marked `COLOR` complete in the
  same session is outside this Issue. MATLAB remains authoritative and the
  controller does not locally reconstruct a completed banner.
- No external audio clips existed; feedback sounds are generated at runtime.
- Live MATLAB integration is not exercised by the deterministic tests and must be
  verified separately with the MATLAB server running on `127.0.0.1:55001`.
- Camera, webcam and hand-tracking features remain permanently excluded.
