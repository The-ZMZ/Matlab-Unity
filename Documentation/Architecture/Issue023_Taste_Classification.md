# Issue #023 — MATLAB Taste Classification Logic

## 1. Scope and permanent decisions

This document records the MATLAB implementation of the Taste Classification
activity logic for Issue #023.

It follows `AGENTS.md` and the repository architecture:

- MATLAB is the TCP server and the **only** authority for answer validation,
  progress, completion and stars.
- Unity collects keyboard/mouse input, sends `PLACE_REQ` messages and displays
  MATLAB results. It does not decide whether an answer is correct.
- Keyboard and mouse are the only input methods. No camera, webcam, hand
  tracking, gesture recognition or pinch detection is used.
- TCP/IP + UTF-8 JSON, one object per line, and the existing message catalogue
  (`PING`, `PONG`, `PLACE_REQ`, `PLACE_RES`, `ACTIVITY_RESULT`,
  `GAME_PROGRESS`, `ERROR`) are reused without change.
- The computational work stays in MATLAB, preserving the approximately
  70% MATLAB / 30% Unity responsibility split.

Inspected dependencies before implementation:

- **Issue #019** — `ActivityPlacementGateway` / placement transport. It sends
  `activity`, `objectId`, `targetId` and normalized `x`/`y`, and consumes
  `valid`, `correct`, `score`, `message` and `code` from MATLAB.
- **Issue #020** — reusable `DraggableItem` / `DropTarget` /
  `MouseDragPlacementController`. These components carry opaque IDs only and
  contain no correctness mapping.
- **Issue #021** — the MATLAB Color Sorting implementation. Its data,
  validation, response, completion, definition, shared JSON and test layout
  is the pattern mirrored here.
- Existing interfaces inspected: `cmr.activities.Definition`, `Registry`,
  `Validator`, `cmr.game.Session`, `cmr.net.Protocol`, `Codec`, `Server`,
  `startServer.m`, `Shared_Data/ActivityData/COLOR.json`,
  `Documentation/Communication/Protocol.md`, `MATLAB/Tests/runAllTests.m`,
  `tColorSorting.m`, `tServerIntegration.m`, Unity `ActivityIds.cs`,
  `TasteSorting.unity` and the Unity networking scripts.

No new wire message type, socket, field or error code was introduced.

## 2. Architecture overview

```
MATLAB/Activities/TasteSorting/
  TasteSortingData.m              single source of truth (8 foods)
  ValidateTasteDataset.m          dataset invariant checks (throws)
  TasteScalarText.m               shared scalar-text guard
  FindTasteFood.m                 id / key / name -> food record
  FindTasteCategory.m             category text -> canonical category
  TasteCategoryForFood.m          food -> correct category
  TasteFoodKey.m                  food -> stable protocol key
  TasteNormalizeKeys.m            mixed refs -> ordered unique keys
  ValidateTasteAnswer.m           required public validation function
  TastePlacementResponse.m        MATLAB response struct
  TasteSortingCompletion.m        completion/percentage from solved foods
  TasteSortingProgress.m          answered/correct/incorrect/remaining state
  TasteSortingApplyPlacement.m    applies one answer to progress
  TasteSortingDefinition.m        adapter to generic Definition/Validator/Session

Shared_Data/ActivityData/TASTE.json   registry-loadable activity definition
MATLAB/Tests/tTasteSorting.m          26 unit/protocol tests
MATLAB/Tests/runIssue023Tests.m       focused runner producing actual JUnit XML
```

Layers:

1. **Data** — `TasteSortingData` constructs the dataset and immediately calls
   `ValidateTasteDataset`, so no consumer can use a malformed dataset.
2. **Lookup** — small reusable functions resolve numeric IDs, protocol keys,
   display names and category text without duplicating comparison logic.
3. **Validation** — `ValidateTasteAnswer` returns a fixed result structure;
   `TastePlacementResponse` maps it onto the existing protocol field names.
4. **Progress** — functional, struct-based progress with enforced invariants.
5. **Integration** — `TasteSortingDefinition` and `TASTE.json` plug the
   activity into the existing `Registry` → `Validator` → `Session` → `Server`
   path unchanged.

## 3. Dataset structure

`TasteSortingData()` returns a scalar struct:

| Field | Type | Meaning |
|---|---|---|
| `activityId` | string | `"TASTE"` |
| `categories` | string array | `["Sweet","Sour","Bitter","Salty"]` |
| `foods` | struct array (8) | food records, each with `id`, `key`, `name`, `category`, `targetId` |

Every food record:

| Field | Type | Meaning |
|---|---|---|
| `id` | double | stable unique ID, 1–8 |
| `key` | string | lowercase snake-case protocol `objectId` |
| `name` | string | display name |
| `category` | string | canonical correct category |
| `targetId` | string | lowercase category, used as protocol `targetId` |

### Food–category mapping

| ID | Key (`objectId`) | Food | Correct category | `targetId` |
|---:|---|---|---|---|
| 1 | `ice_cream` | Ice Cream | Sweet | `sweet` |
| 2 | `lemon` | Lemon | Sour | `sour` |
| 3 | `tomato` | Tomato | Sour | `sour` |
| 4 | `garlic` | Garlic | Bitter | `bitter` |
| 5 | `pizza` | Pizza | Salty | `salty` |
| 6 | `coffee` | Coffee | Bitter | `bitter` |
| 7 | `chips` | Chips | Salty | `salty` |
| 8 | `donuts` | Donuts | Sweet | `sweet` |

`ValidateTasteDataset` verifies: exactly eight foods, IDs exactly 1–8 with no
duplicates, unique valid protocol keys, non-empty unique names, categories
limited to the four declared values, and `targetId == lower(category)`. It
throws `cmr:activities:TasteSorting:invalidDataset` on any violation.

`Shared_Data/ActivityData/TASTE.json` mirrors this table so the existing
`Registry` can discover the activity at server startup. A test compares every
field in that file against the MATLAB dataset.

## 4. Function interfaces

### `result = ValidateTasteAnswer(foodTaste, selectedCategory)`

Required Issue #023 entry point. It never throws for bad caller input.

`foodTaste` accepts:

- a numeric stable ID (`1`–`8`);
- a protocol key (`"ice_cream"`, case insensitive);
- a display name (`"Ice Cream"`, case insensitive);
- a category itself (`"Sweet"`, case insensitive), for direct taste comparison.

`selectedCategory` accepts `Sweet`, `Sour`, `Bitter` or `Salty`, case
insensitive. Text must be a non-empty char row or string scalar.

The result struct always has the same fields:

| Field | Type | Meaning |
|---|---|---|
| `valid` | logical | inputs resolved to a known food/taste and a valid category |
| `correct` | logical | MATLAB's authoritative judgement |
| `code` | string | `""` on success, otherwise a `cmr.net.Protocol` error code |
| `message` | string | human-readable feedback |
| `foodId` | double | stable ID 1–8, or `0` for direct taste input |
| `foodKey` | string | protocol key, or `""` for direct taste input |
| `foodName` | string | display name, or `""` for direct taste input |
| `expectedCategory` | string | canonical correct category |
| `selectedCategory` | string | canonical selected category, or `""` if invalid |

### Supporting interfaces

| Function | Interface | Purpose |
|---|---|---|
| `TasteSortingData` | `data = TasteSortingData()` | validated dataset |
| `ValidateTasteDataset` | `ValidateTasteDataset(data)` | throws on invalid dataset |
| `FindTasteFood` | `food = FindTasteFood(ref)` | food record or empty struct |
| `FindTasteCategory` | `category = FindTasteCategory(text)` | canonical category or empty struct |
| `TasteCategoryForFood` | `category = TasteCategoryForFood(ref)` | correct category or `""` |
| `TasteFoodKey` | `key = TasteFoodKey(ref)` | stable protocol key or `""` |
| `TasteNormalizeKeys` | `keys = TasteNormalizeKeys(refs)` | ordered unique known keys |
| `TastePlacementResponse` | `response = TastePlacementResponse(food, category)` | protocol-shaped result |
| `TasteSortingCompletion` | `status = TasteSortingCompletion(solvedRefs)` | solved/score/total/percent/completed |
| `TasteSortingProgress` | `progress = TasteSortingProgress(answered, correct, incorrect)` | full consistent progress struct |
| `TasteSortingApplyPlacement` | `[next, response] = TasteSortingApplyPlacement(progress, food, category)` | apply one answer |
| `TasteSortingDefinition` | `definition = TasteSortingDefinition()` | generic `cmr.activities.Definition` |

`TasteSortingProgress` fields:

`answered`, `correct`, `incorrect`, `remaining` (string arrays of protocol
keys); `answeredCount`, `correctCount`, `incorrectCount`, `remainingCount`,
`total` (doubles); `completionPercent`, `answeredPercent` (0–100 doubles);
`allAnswered`, `completed` (logical).

`TasteSortingApplyPlacement` extends `TastePlacementResponse` with
`newlyAnswered`, `newlySolved` and `duplicate` logical flags.

## 5. Validation behavior

| Input case | `valid` | `correct` | `code` |
|---|---|---|---|
| Known food/key/name + correct category | true | true | `""` |
| Known food/key/name + wrong valid category | true | false | `""` |
| Direct taste + matching category | true | true | `""` |
| Direct taste + different category | true | false | `""` |
| Empty food or category | false | false | `INVALID_REQUEST` |
| Whitespace-only text, numeric category, non-scalar text | false | false | `INVALID_REQUEST` |
| Unknown food ID/key/name | false | false | `UNKNOWN_OBJECT` |
| Unknown category name | false | false | `UNKNOWN_TARGET` |

Case is ignored for food keys, food names and category names. Malformed input
is never silently accepted: the result is always scalar and carries an error
code. The only `error()` calls in the activity code are for internal
invariants (invalid dataset, invalid progress struct, inconsistent progress
lists), not for caller input.

## 6. Progress-tracking logic

- **answered** — unique foods with at least one valid submission (correct or
  incorrect).
- **correct** — unique foods whose latest valid submission was correct.
- **incorrect** — unique foods whose latest valid submission was incorrect.
- **remaining** — dataset foods not present in `answered`, in dataset order.
- The enforced invariants are
  `answeredCount == correctCount + incorrectCount` and
  `remainingCount == total - answeredCount`.
- `completionPercent = round(100 * correctCount / total)`;
  `answeredPercent = round(100 * answeredCount / total)`.
- `allAnswered` becomes true when all eight foods have been attempted.
- `completed` becomes true only when all eight foods are correct. This is the
  same rule as Color Sorting: an incorrect attempt cannot earn completion or
  a star.

Duplicate and correction rules:

1. A new food submission adds the food once to `answered` and to `correct` or
   `incorrect`. `newlyAnswered` is true.
2. Repeating a correct answer for an already solved food changes nothing;
   `duplicate` is true, `newlySolved` is false and no count increases.
3. A later correct answer for a previously incorrect food moves that food from
   `incorrect` to `correct`. `answeredCount` does not increase, the food is
   counted once, and `newlySolved` is true.
4. Invalid answers and unknown foods leave progress unchanged.

`TasteSortingProgress` throws
`cmr:activities:TasteSorting:inconsistentProgress` if `correct` or `incorrect`
contains a food not in `answered`, or if the two lists overlap.

## 7. MATLAB–Unity integration

No Unity file was modified for this issue. The activity is exposed through the
existing protocol exactly as Color Sorting is:

```json
{"type":"PLACE_REQ","requestId":"req_000001","activity":"TASTE",
 "objectId":"ice_cream","targetId":"sweet","position":{"x":0.42,"y":0.63}}
```

- `activity` must be `"TASTE"`, matching `ActivityIds.Taste` in Unity and the
  Bedroom Dining Table station.
- `objectId` is one of the eight lowercase keys from the table above.
- `targetId` is the lowercase category (`sweet`, `sour`, `bitter`, `salty`).
  The generic `Validator` compares IDs case-sensitively; the direct
  `ValidateTasteAnswer` API is case-insensitive by requirement.
- `position` keeps the existing normalized top-left-origin convention.

MATLAB replies with the existing `PLACE_RES` and `ACTIVITY_RESULT` messages.
`score` is the number of distinct correctly placed foods from
`cmr.game.Session`. Star awarding and `GAME_PROGRESS` remain in the existing
session/server code. The richer `TasteSortingProgress` struct is MATLAB-side
activity state; its extra fields are not new wire fields.

Verified at protocol level: `tTasteSorting/serverHandlesTastePlacementsOverTcp`
starts a real `cmr.net.Server` on port 55102 with the default activity data
directory, connects a real `tcpclient`, sends all eight correct `TASTE`
placements and checks every `PLACE_RES`/`ACTIVITY_RESULT`, the score sequence
1–8, and completion/star state.

Not verified: a real Unity client, Play Mode, physical mouse interaction,
the configured TasteSorting scene flow and the father/ending sequence.
Inspection showed that Unity already declares `ActivityIds.Taste`, the Dining
Table station and `DiningTable_Star` with `activityId = TASTE`, but
`TasteSorting.unity` is a UI shell with no food
objects, no `DraggableItem`/`DropTarget`, no `TasteSortingIds` and no
configured `MouseDragPlacementController`. The scene is also not registered in
Build Settings. Those Unity-side gaps are outside the scope of Issue #023 and
were deliberately left unchanged.

## 8. Error-handling behavior

| Code | Trigger |
|---|---|
| `INVALID_REQUEST` | empty/whitespace food or category, non-text category, non-scalar/cell/struct food input |
| `UNKNOWN_OBJECT` | food numeric ID outside 1–8, non-integer/non-finite ID, unknown key or name |
| `UNKNOWN_TARGET` | category not in Sweet/Sour/Bitter/Salty |
| `""` | valid inputs; `correct` then carries the educational result |

At the TCP boundary, the existing `Server` sends `ERROR` replies for unknown
activities/objects/targets and for malformed requests, and keeps the
connection, framing, duplicate-request and reconnection behavior unchanged.
Internal invariant violations throw typed MATLAB errors rather than returning
silent partial data.
