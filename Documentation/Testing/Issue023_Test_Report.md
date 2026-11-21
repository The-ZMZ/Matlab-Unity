# Issue #023 — MATLAB Taste Classification Test Report

## 1. Test environment

| Item | Value |
|---|---|
| Date executed | 2026-11-22 |
| Repository | Clean My Room (worktree `a7a8f5/witty-moon`) |
| MATLAB actually used | **R2026b, 26.2.0.3386108** |
| MATLAB executable | `D:\stage3\bin\matlab.exe` |
| Project target version | R2025b (`AGENTS.md` §14.1) — **NOT installed, NOT verified** |
| Operating system | Windows |
| MATLAB toolboxes | Base MATLAB only (`matlab.unittest`, `tcpserver`/`tcpclient`, `jsondecode`/`jsonencode`) |
| JUnit XML plugin | `matlab.unittest.plugins.XMLPlugin` (present in the installed MATLAB) |

No Python, camera, webcam, hand-tracking, gesture or pinch dependency was used.

## 2. Commands actually executed

All commands were run from the repository root.

Focused Issue #023 suite with JUnit XML generation:

```powershell
& 'D:\stage3\bin\matlab.exe' -batch "addpath('MATLAB/Tests'); runIssue023Tests();"
```

Full MATLAB regression suite:

```powershell
& 'D:\stage3\bin\matlab.exe' -batch "addpath('MATLAB/Tests'); runAllTests();"
```

Version/plugin probe (read-only):

```powershell
& 'D:\stage3\bin\matlab.exe' -batch "fprintf('VERSION=%s\n', version); fprintf('XMLPLUGIN=%d\n', exist('matlab.unittest.plugins.XMLPlugin','class')); fprintf('TCPSERVER=%d\n', exist('tcpserver','file'));"
```

## 3. Final actual results

### Focused Issue #023 suite (authoritative)

| Metric | Value |
|---|---|
| Total | 26 |
| Passed | 26 |
| Failed | 0 |
| Incomplete / errored | 0 |
| Skipped | 0 |
| Duration | 10.51 s |
| Process exit status | success |

XML evidence:
`Deliverables/Reports/Issue023_MATLAB_TestResults.xml`

The file was produced by `matlab.unittest.plugins.XMLPlugin.producingJUnitFormat`
during the final focused run. Its root suite entry reports
`errors="0" failures="0" skipped="0" tests="26" time="10.5115"`, and it contains
26 individual `<testcase>` elements matching the tests below. It was not
hand-written.

### Full MATLAB regression suite

| Metric | Value |
|---|---|
| Total | 335 |
| Passed | 335 |
| Failed | 0 |
| Incomplete / errored | 0 |
| Duration | 59.13 s |
| Process exit status | success |

The full suite includes the new `tTasteSorting` class (26 tests) and all
existing classes: `t3DObjects`, `tCodec`, `tCohenSutherland`, `tColorSorting`,
`tGeometryExporter`, `tGraphics2D`, `tLighting`, `tMesh3D`, `tProjection`,
`tServerIntegration`, `tSession`, `tTransform2D`, `tTransform3D` and
`tValidator`. Issues #019, #020, #021 and #022 files were not modified.

## 4. Test cases and requirement coverage

| # | Test (`tTasteSorting/...`) | Requirement covered |
|---:|---|---|
| 1 | `datasetContainsExactlyEightValidFoods` | Exactly eight foods; valid structure; four categories |
| 2 | `allFoodIdsAreUniqueAndStable` | Unique stable IDs; key resolution |
| 3 | `allFoodCategoryMappingsAreCorrect` | All eight mappings correct; names consistent |
| 4 | `datasetValidatorRejectsInvalidEntries` | Missing, duplicate and invalid dataset entries |
| 5 | `correctAnswersAreAcceptedForEveryFood` | Correct answers accepted for all foods |
| 6 | `directTasteComparisonIsSupported` | Direct taste-vs-category comparison |
| 7 | `incorrectAnswersAreRejectedForEveryFood` | Incorrect answers rejected |
| 8 | `categoryMatchingIsCaseInsensitive` | Case-insensitive comparison |
| 9 | `emptyInputIsHandledWithoutThrowing` | Empty input handling |
| 10 | `invalidCategoriesAreRejected` | Invalid categories rejected |
| 11 | `unknownFoodIdentifiersAreRejected` | Unknown / non-integer / non-finite IDs rejected |
| 12 | `malformedFoodInputIsRejectedWithoutArrayResults` | Malformed input never silently accepted |
| 13 | `placementResponseUsesExistingProtocolErrorCodes` | Consistent result structure; existing error codes |
| 14 | `completionRequiresAllEightUniqueFoods` | Completion detection; score/total |
| 15 | `completionIgnoresDuplicatesAndUnknownFoods` | Duplicates and unknown IDs ignored |
| 16 | `completionPercentageIsCalculatedCorrectly` | 0/25/50/100 percentages |
| 17 | `progressTracksAnsweredCorrectIncorrectAndRemaining` | All progress fields and counts |
| 18 | `invalidAnswersDoNotChangeProgress` | Invalid answers do not advance progress |
| 19 | `duplicateSubmissionsDoNotIncreaseProgress` | Duplicate prevention; each food counted once |
| 20 | `correctingWrongAnswerCountsFoodOnce` | Correction after a wrong answer counted once |
| 21 | `progressValuesStayConsistentAfterMixedAnswers` | Progress values remain valid/consistent |
| 22 | `allEightFoodsCanBeProcessedSuccessfully` | All eight foods processed; completion |
| 23 | `definitionWorksWithGenericValidatorAndSession` | Compatibility with existing Definition/Validator/Session |
| 24 | `sharedJsonMatchesMatlabDataset` | `TASTE.json` matches the MATLAB dataset |
| 25 | `registryLoadsTasteFromDefaultDataDirectory` | Server registry integration |
| 26 | `serverHandlesTastePlacementsOverTcp` | Real TCP `PLACE_REQ`/`PLACE_RES`/`ACTIVITY_RESULT` flow |

## 5. Failures encountered and resolved

The first executed focused run did not pass; it produced real failures and
errors. The exact pass/fail counts of that initial run were not retained, so
they are not claimed here. Every issue observed was in the new test code or
runner paths, not in the production implementation:

1. `completionIgnoresDuplicatesAndUnknownFoods` expected a score of 2 but
   actual was 1. The test passed numeric `1` alongside key `ice_cream`;
   `TasteNormalizeKeys` correctly resolves both to the same food. The test now
   uses numeric ID `5` as the second distinct food.
2. `sharedJsonMatchesMatlabDataset` errored with `MATLAB:fileread:cannotOpenFile`
   because `ProjectRoot` was computed with two `fileparts` calls, pointing at
   `MATLAB/Shared_Data/...` instead of the repository root. A third `fileparts`
   fixed it.
3. `registryLoadsTasteFromDefaultDataDirectory` failed for the same path error
   and then errored while dot-indexing the empty registry result. Fixed with
   the same path correction.
4. `serverHandlesTastePlacementsOverTcp` errored with
   `MATLAB:UndefinedFunction` for `client` after the socket was changed to a
   test-case property. The call now uses `testCase.Client`.
5. The first runner invocation also wrote the XML under `MATLAB/Deliverables/...`
   because the runner's project root was one directory too shallow. The runner
   now writes to `Deliverables/Reports/...` at the repository root, and the
   misplaced artifact was removed.

Each fix was followed by the final focused run (26/26) and the full regression
run (335/335) recorded in section 3. No production-code changes were needed
after those runs.

## 6. Skipped and not-run items

| Item | Status | Reason |
|---|---|---|
| Skipped MATLAB tests | 0 | None; all 26 focused tests executed |
| Unity EditMode / Play Mode tests | NOT RUN | Issue #023 is MATLAB-only; no Unity file was changed |
| Real Unity client ↔ MATLAB TASTE interaction | NOT RUN | Unity has no taste IDs, configured drag/drop objects or wired TasteSorting scene |
| Physical mouse drag/drop | NOT RUN | No hardware interaction and no configured activity objects |
| MATLAB R2025b execution | NOT RUN | Only R2026b is installed; base-MATLAB functions used, but R2025b remains unverified |
| Father/ending sequence with three stars | NOT RUN | Outside Issue #023 scope; no Unity integration performed |

The MATLAB server side of the protocol was exercised with a real `tcpclient`
in `serverHandlesTastePlacementsOverTcp`; that verifies MATLAB behavior only,
not Unity behavior.

## 7. Known limitations

- The generic server path (`cmr.game.Session`) tracks distinct correctly
  placed foods. The richer answered/incorrect breakdown from
  `TasteSortingProgress` is MATLAB activity state and is not sent as new wire
  fields; no protocol change was authorized.
- `ValidateTasteAnswer` accepts a direct category as `foodTaste` for simple
  comparison, but progress functions require a food ID, key or name. A direct
  category cannot advance progress because it does not identify a food.
- Completion requires all eight foods to be **correct**, not merely
  attempted, so a wrong answer cannot complete the activity.
- `targetId` on the wire is lowercase (`sweet`, etc.) because the generic
  `Validator` is case-sensitive. The `ValidateTasteAnswer` API itself is
  case-insensitive as required.
- Unity inspection found `TasteSorting.unity` as a UI shell only, no
  `TasteSortingIds.cs`, no food/drag/drop objects and no Build Settings entry
  for the scene. These are Unity-side gaps outside this issue; the existing
  `ActivityIds.Taste = "TASTE"` value is compatible with the MATLAB activity
  ID.

## 8. Unresolved issues

No unresolved MATLAB defects remain from the executed tests. The Unity-side
scene/ID wiring and R2025b verification listed above remain open limitations,
not failures of this implementation.

## 9. Artifact and Git notes

Created in this issue:

- 14 MATLAB activity functions under `MATLAB/Activities/TasteSorting/`
- `Shared_Data/ActivityData/TASTE.json`
- `MATLAB/Tests/tTasteSorting.m`
- `MATLAB/Tests/runIssue023Tests.m`
- `Deliverables/Reports/Issue023_MATLAB_TestResults.xml` (generated from the
  actual final focused run)
- `Documentation/Architecture/Issue023_Taste_Classification.md`
- `Documentation/Testing/Issue023_Test_Report.md`

No Unity asset, Unity `ProjectSettings`, protocol constant, existing MATLAB
source or existing test file was modified. No Git write operation (stage,
commit, push, merge, rebase, reset, clean or branch change) was performed.
