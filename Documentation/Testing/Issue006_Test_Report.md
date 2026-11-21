# Issue #006 — Test Report

**2D Geometric Transformations and Cohen–Sutherland Line Clipping**

| Field | Value |
|---|---|
| Issue | #006 |
| Date of execution | 2026-11-16 |
| MATLAB used | **R2026b (26.2.0.3386108)** |
| R2025b status | **NOT VERIFIED** — R2025b is not installed on the development machine |
| Test runner | `MATLAB/Tests/runAllTests.m` (unchanged) |
| Result | **99 / 99 tests passed, 0 failed, 0 incomplete** |
| Evidence | `Deliverables/Screenshots/Issue006_*.png` (3 files) |

> **Version caveat.** `AGENTS.md` §14.1 states MATLAB R2025b as the project target. Every result in
> this report was produced on R2026b, the only MATLAB installation available. R2025b compatibility
> is therefore **untested**, not "passing". Only base-MATLAB functions and syntax available since
> R2018b are used.

---

## 1. Commands executed

### 1.1 Transform2D suite alone

```powershell
& "D:\stage3\bin\matlab.exe" -batch "cd('D:/Matlab-Unity/MATLAB/Tests'); r = runtests('tTransform2D'); assertSuccess(r)"
```

```
Running tTransform2D
.......... .......... ....
Done tTransform2D
Totals:
   24 Passed, 0 Failed, 0 Incomplete.
   4.1064 seconds testing time.
```

### 1.2 Cohen–Sutherland suite alone

```powershell
& "D:\stage3\bin\matlab.exe" -batch "cd('D:/Matlab-Unity/MATLAB/Tests'); r = runtests('tCohenSutherland'); assertSuccess(r)"
```

```
Running tCohenSutherland
.......... .......... ........
Done tCohenSutherland
Totals:
   28 Passed, 0 Failed, 0 Incomplete.
   2.6665 seconds testing time.
```

### 1.3 Full regression suite

```powershell
& "D:\stage3\bin\matlab.exe" -batch "cd('D:/Matlab-Unity/MATLAB/Tests'); runAllTests"
```

```
=== MATLAB TEST SUMMARY ===
Total      : 99
Passed     : 99
Failed     : 0
Incomplete : 0
Duration   : 25.76 s
===========================
```

Per class:

| Test class | Tests | Passed |
|---|---|---|
| `tCodec` | 18 | 18 |
| `tServerIntegration` | 17 | 17 |
| `tSession` | 8 | 8 |
| `tValidator` | 4 | 4 |
| `tTransform2D` | 24 | 24 |
| `tCohenSutherland` | 28 | 28 |
| **Total** | **99** | **99** |

`47 pre-existing + 24 + 28 = 99`. No pre-existing test was modified, skipped or removed.

---

## 2. `tTransform2D` — 24 tests, all passed

| # | Test | Verifies |
|---|---|---|
| 1 | `identityIsEye` | `identity()` returns `eye(3)` with last row `[0 0 1]` |
| 2 | `identityLeavesPointsUnchanged` | identity preserves an `N×2` point set |
| 3 | `translationMatrixAndApplication` | matrix entries and `(1,1)→(4,−1)`, `(0,0)→(3,−2)` |
| 4 | `rotationQuarterTurnMapsAxes` | `R(π/2)`: `(1,0)→(0,1)`, `(0,1)→(−1,0)` (CCW) |
| 5 | `rotationDegreesMatchesRadians` | `rotationDegrees(45) == rotation(π/4)` |
| 6 | `rotationFullTurnIsIdentity` | `R(2π) ≈ I` |
| 7 | `rotationIsOrthonormalWithUnitDeterminant` | `RᵀR = I`, `det = 1` |
| 8 | `scalingMatrixAndApplication` | `S(2,0.5)`: `(3,4)→(6,2)`, `det = sx·sy` |
| 9 | `reflectionXFlipsY` | `(x,y)→(x,−y)`, `det = −1` |
| 10 | `reflectionYFlipsX` | `(x,y)→(−x,y)`, `det = −1` |
| 11 | `reflectionOriginNegatesBoth` | `(x,y)→(−x,−y)` |
| 12 | `reflectionLineYXSwapsCoordinates` | `(2,5)→(5,2)` |
| 13 | `reflectionLineYNegXSwapsAndNegates` | `(2,5)→(−5,−2)` |
| 14 | `reflectionAboutArbitraryLine` | `(3,0)` across `x = 1` gives `(−1,0)`; points on the line are fixed |
| 15 | `shearXMapsPoint` | `x' = x + k·y` |
| 16 | `shearYMapsPoint` | `y' = k·x + y` |
| 17 | `composeAppliesInOrder` | `compose(T,S)` → 6, `compose(S,T)` → 4; matrices differ |
| 18 | `composeWithNoArgsIsIdentity` | `compose()` → `eye(3)` |
| 19 | `rotationAboutPivotKeepsPivotFixed` | pivot fixed; `(3,1)` about `(2,1)` by 90° → `(2,2)` |
| 20 | `scalingAboutPivot` | pivot fixed; `(2,1)` about `(1,1)` by 2× → `(3,1)` |
| 21 | `applyHandlesMultiplePoints` | shape preserved, translation applied to all rows |
| 22 | `applyPointMatchesApply` | single-point wrapper agrees with `apply` |
| 23 | `buildersReturnAffineMatrices` | 6 builders: `3×3`, finite, last row exact `[0 0 1]` |
| 24 | `invalidInputsThrow` | all 7 documented error identifiers |

---

## 3. `tCohenSutherland` — 28 tests, all passed

| # | Test | Verifies |
|---|---|---|
| 1 | `outCodeCentreIsInside` | centre → `0000` |
| 2 | `outCodeOnBoundaryIsInside` | 4 edges + 4 corners → `0000` (closed window) |
| 3 | `outCodeSingleRegions` | LEFT `0001`, RIGHT `0010`, BOTTOM `0100`, TOP `1000` |
| 4 | `outCodeCornerRegions` | `0101`, `0110`, `1001`, `1010` |
| 5 | `outCodesVectorizedMatchesScalar` | `outCodes` agrees with `outCode` for 6 points |
| 6 | `fullyInsideSegmentUnchanged` | trivial accept, endpoints unchanged |
| 7 | `fullyOutsideOnEachSideRejected` | trivial reject on all four sides → NaN/NaN |
| 8 | `rejectsSegmentThatStraddlesACorner` | `(−5,3)→(3,15)` codes `0001`/`1000`: no shared bit, still rejected after clipping |
| 9 | `clipsLeftEdge` | `(−5,5)→(5,5)` → `(0,5)→(5,5)` |
| 10 | `clipsRightEdge` | `(5,5)→(15,5)` → `(5,5)→(10,5)` |
| 11 | `clipsBottomEdge` | `(5,5)→(5,−5)` → `(5,5)→(5,0)` |
| 12 | `clipsTopEdge` | `(5,5)→(5,15)` → `(5,5)→(5,10)` |
| 13 | `clipsDiagonalAcrossBothCorners` | `(−2,−2)→(12,12)` → `(0,0)→(10,10)` |
| 14 | `clipsDiagonalEnteringAtOppositeCorners` | `(−5,−5)→(5,5)` → `(0,0)→(5,5)` |
| 15 | `touchingCornerYieldsDegenerateVisibleSegment` | `(−10,0)→(0,10)` → `(0,10)→(0,10)`, accepted |
| 16 | `verticalLineOutsideWindowRejected` | vertical segment at `x = 15` rejected without division |
| 17 | `verticalLineClipsBothHorizontalEdges` | `(5,−3)→(5,13)` → `(5,0)→(5,10)` |
| 18 | `horizontalLineClipsBothVerticalEdges` | `(−3,5)→(13,5)` → `(0,5)→(10,5)` |
| 19 | `horizontalLineOutsideWindowRejected` | horizontal segment at `y = 15` rejected |
| 20 | `zeroLengthInsideAccepted` | degenerate point inside accepted |
| 21 | `zeroLengthOutsideRejected` | degenerate point outside rejected |
| 22 | `endpointOnBoundaryIsInclusive` | `(0,5)` is inside; segment clipped at `x = 10` only |
| 23 | `clipPolylineKeepsOnlyVisibleSegments` | 4 segments → 3 kept, exact coordinates |
| 24 | `clipPolylineFullyOutsideIsEmpty` | enclosing polygon → genuine `0×4` |
| 25 | `clipPolylinePartiallyOutsideTriangle` | triangle → 3 clipped rows, exact coordinates |
| 26 | `invalidWindowThrows` | 6 invalid windows (zero width, inverted, zero height, Inf, wrong length, non-numeric) |
| 27 | `invalidPointThrows` | 3-element point, NaN point, NaN endpoint |
| 28 | `invalidPolylineThrows` | single point, `N×3`, non-finite |

Tests 8, 15 and 24 target the three failure modes that a naive implementation gets wrong:
false accept near a corner, false reject on corner tangency, and NaN rows instead of empty output.

---

## 4. Demo execution

### 4.1 `demo2DTransformations`

```powershell
& "D:\stage3\bin\matlab.exe" -batch "cd('D:/Matlab-Unity/MATLAB/Graphics'); demo2DTransformations"
```

```
Saved D:\Matlab-Unity\Deliverables\Screenshots\Issue006_2D_Transformations.png (95976 bytes)
Saved D:\Matlab-Unity\Deliverables\Screenshots\Issue006_Composite_Order.png (65991 bytes)
Translation matrix T(4,1):
     1     0     4
     0     1     1
     0     0     1

Rotation matrix R(60 deg):
    0.5000   -0.8660         0
    0.8660    0.5000         0
         0         0    1.0000

compose(T, R) = R*T  -> v1 = (1.1340, 3.9641)
compose(R, T) = T*R  -> v1 = (4.0000, 1.0000)
The two results differ, which is why the order of composition matters.
```

The tracked vertex values match the hand calculation
`R(60°)·(4,1) = (4·0.5 − 1·0.8660, 4·0.8660 + 1·0.5) = (1.1340, 3.9641)`.

### 4.2 `demoCohenSutherland`

```powershell
& "D:\stage3\bin\matlab.exe" -batch "cd('D:/Matlab-Unity/MATLAB/Graphics'); demoCohenSutherland"
```

```
Cohen-Sutherland clipping, window = [0 0 10 10]
  # | p0            | p1            | c0   | c1   | result   | visible part
--------------------------------------------------------------------------------------------
  1 | (  2.0,  2.0) | (  8.0,  8.0) | 0000 | 0000 | ACCEPT   | (2.00, 2.00)-(8.00, 8.00)
  2 | ( -2.0,  4.0) | ( -1.0,  6.0) | 0001 | 0001 | REJECT   | -
  3 | ( 11.0,  4.0) | ( 12.0,  6.0) | 0010 | 0010 | REJECT   | -
  4 | (  3.0, 11.0) | (  7.0, 12.0) | 1000 | 1000 | REJECT   | -
  5 | (  3.0, -2.0) | (  7.0, -1.0) | 0100 | 0100 | REJECT   | -
  6 | ( -2.0,  5.0) | (  4.0,  5.0) | 0001 | 0000 | ACCEPT   | (0.00, 5.00)-(4.00, 5.00)
  7 | (  6.0,  5.0) | ( 12.0,  5.0) | 0000 | 0010 | ACCEPT   | (6.00, 5.00)-(10.00, 5.00)
  8 | (  5.0,  6.0) | (  5.0, 12.0) | 0000 | 1000 | ACCEPT   | (5.00, 6.00)-(5.00, 10.00)
  9 | (  5.0, -2.0) | (  5.0,  4.0) | 0100 | 0000 | ACCEPT   | (5.00, 0.00)-(5.00, 4.00)
 10 | ( -3.0, -3.0) | (  4.0,  4.0) | 0101 | 0000 | ACCEPT   | (0.00, 0.00)-(4.00, 4.00)
 11 | (  2.0, -2.0) | (  2.0, 12.0) | 0100 | 1000 | ACCEPT   | (2.00, 0.00)-(2.00, 10.00)
 12 | ( -2.0,  7.0) | ( 12.0,  7.0) | 0001 | 0010 | ACCEPT   | (0.00, 7.00)-(10.00, 7.00)
--------------------------------------------------------------------------------------------
Accepted: 8   Rejected: 4   Total: 12
Saved D:\Matlab-Unity\Deliverables\Screenshots\Issue006_Cohen_Sutherland_Clipping.png (104455 bytes)
```

Every value was checked against the intersection equations by hand, for example
segment 12 (`y = 7`, horizontal): codes `0001`/`0010` share no bit, so it is clipped at `x = 0`
and `x = 10` and the visible part is `(0,7)-(10,7)`.

---

## 5. Figure verification

Verification was **programmatic**, not visual: each PNG was read back with `imfinfo` and `imread`
to confirm existence, dimensions, colour type and that the image is not a blank canvas
(non-zero spread of grey levels plus the presence of the specific rendering colours used by the
plots).

| File | Dimensions | Depth | File size | Colour check |
|---|---|---|---|---|
| `Issue006_2D_Transformations.png` | 2163 × 1089 | 24-bit truecolour | 95,976 B | 63,241 blue-fill px, 57,090 ghost-grey px, 321 orange line px, 258 gold pivot px, 240 distinct levels |
| `Issue006_Composite_Order.png` | 1855 × 736 | 24-bit truecolour | 65,991 B | 43,652 blue-fill px, 354 red tracked-vertex px, 229 distinct levels |
| `Issue006_Cohen_Sutherland_Clipping.png` | 2123 × 1105 | 24-bit truecolour | 104,455 B | 733 red reject px, 455,550 window-fill px, 28,667 dark ink px, 256 distinct levels |

The presence of the ghost-grey, blue-fill, orange-line and gold-pivot colours confirms that each
panel of the transformation figure rendered its intended content; the red crosses and window fill
confirm the clipping figure rendered rejected segments and the clipping window.

> **Limitation of this check.** The agent could not *look* at the images. Content is verified by
> pixel statistics and by the code that produces it, not by human visual inspection. Human review
> of the three PNGs is recommended before they are used in the presentation.

---

## 6. MATLAB path setup used by the tests

`cmr.graphics` is a package under `MATLAB/Graphics/+cmr/+graphics`, so MATLAB resolves it from the
folder that contains `+cmr`:

```matlab
addpath('D:/Matlab-Unity/MATLAB/Graphics');  % resolves cmr.graphics.*
addpath('D:/Matlab-Unity/MATLAB');           % project root
```

Both new test classes register the path with:

```matlab
root = fileparts(fileparts(mfilename('fullpath')));   % MATLAB
graphicsRoot = fullfile(root, 'Graphics');            % MATLAB/Graphics
testCase.applyFixture(matlab.unittest.fixtures.PathFixture({root, graphicsRoot}));
```

`PathFixture` is **not recursive**: it adds only the folders supplied. There is no `+cmr` directly
inside `MATLAB` — each area (`Activities`, `Communication`, `GameLogic`, `Graphics`) is its own
package root — so adding `MATLAB` alone exposes no package, and 24 tests failed with
`MATLAB:undefinedVarOrClass` on the first attempt
(`Unable to resolve the name 'cmr.graphics.Transform2D.identity'`). Adding `MATLAB/Graphics` fixed
it; this is the setup that produced every result above.

`runAllTests.m` was **not** modified: it already calls `addpath(genpath(projectRoot))`, which
descends into `MATLAB/Graphics`, so a full run finds the package without extra setup. The demo
scripts add both folders themselves from `mfilename('fullpath')`.

---

## 7. Files created by Issue #006

Implementation and tests:

- `MATLAB/Graphics/+cmr/+graphics/Transform2D.m`
- `MATLAB/Graphics/+cmr/+graphics/CohenSutherland.m`
- `MATLAB/Tests/tTransform2D.m`
- `MATLAB/Tests/tCohenSutherland.m`

Demos and evidence:

- `MATLAB/Graphics/demo2DTransformations.m`
- `MATLAB/Graphics/demoCohenSutherland.m`
- `Deliverables/Screenshots/Issue006_2D_Transformations.png`
- `Deliverables/Screenshots/Issue006_Composite_Order.png`
- `Deliverables/Screenshots/Issue006_Cohen_Sutherland_Clipping.png`

Documentation:

- `Documentation/Graphics/2D_Transformations_and_Clipping.md`
- `Documentation/Testing/Issue006_Test_Report.md` (this file)

**No existing file was modified.** `git status` shows only new paths; nothing was staged,
committed or pushed. The working tree is left for the project owner to review.

```
$ git status --porcelain
?? Documentation/Graphics/2D_Transformations_and_Clipping.md
?? Documentation/Testing/Issue006_Test_Report.md
?? Deliverables/Screenshots/Issue006_2D_Transformations.png
?? Deliverables/Screenshots/Issue006_Composite_Order.png
?? Deliverables/Screenshots/Issue006_Cohen_Sutherland_Clipping.png
?? MATLAB/Graphics/+cmr/
?? MATLAB/Graphics/demo2DTransformations.m
?? MATLAB/Graphics/demoCohenSutherland.m
?? MATLAB/Tests/tCohenSutherland.m
?? MATLAB/Tests/tTransform2D.m
```

---

## 8. Manual verification steps

1. **Run the new suites**

   ```powershell
   & "D:\stage3\bin\matlab.exe" -batch "cd('D:/Matlab-Unity/MATLAB/Tests'); assertSuccess(runtests('tTransform2D'))"
   & "D:\stage3\bin\matlab.exe" -batch "cd('D:/Matlab-Unity/MATLAB/Tests'); assertSuccess(runtests('tCohenSutherland'))"
   ```

   Expect `24 Passed` and `28 Passed`.

2. **Run the full regression suite**

   ```powershell
   & "D:\stage3\bin\matlab.exe" -batch "cd('D:/Matlab-Unity/MATLAB/Tests'); runAllTests"
   ```

   Expect `Total 99 | Passed 99 | Failed 0 | Incomplete 0`.

3. **Regenerate the figures**

   ```matlab
   cd('D:/Matlab-Unity/MATLAB/Graphics')
   demo2DTransformations
   demoCohenSutherland
   ```

   In the MATLAB desktop both scripts open figure windows for inspection; under `matlab -batch`
   they render invisibly and only write PNGs.

4. **Inspect the evidence**

   Open the three PNGs in `Deliverables/Screenshots/`. Check in particular: the ghosted original
   versus the blue transformed shape in every panel, the gold pivot marker staying on the same
   point, the red tracked vertex `v1` differing between the two composition orders, the nine region
   labels and endpoint codes in the clipping figure, and the four red rejection crosses.

5. **Spot-check the API manually**

   ```matlab
   addpath('D:/Matlab-Unity/MATLAB'); addpath('D:/Matlab-Unity/MATLAB/Graphics');
   cmr.graphics.Transform2D.apply(cmr.graphics.Transform2D.translation(3,1), [1 1])   % [4 2]
   cmr.graphics.CohenSutherland.outCode([-1 11], [0 0 10 10])                        % 9
   [~,~,ok] = cmr.graphics.CohenSutherland.clipLine([-5 3], [3 15], [0 0 10 10]);     % ok = false
   ```

---

## 9. Items not run / outstanding

| Item | Status | Reason |
|---|---|---|
| MATLAB R2025b test run | **NOT RUN** | R2025b not installed; only R2026b (26.2.0.3386108) available |
| Human visual inspection of the three PNGs | **NOT DONE by agent** | The agent cannot view images; pixel statistics were used instead. Owner review requested. |
| Unity / MATLAB↔Unity protocol tests | Not applicable to Issue #006 | This issue is MATLAB-only and touches no communication file. The pre-existing `tServerIntegration` (17 tests) passed unchanged. |
| Issues #005, #007–#010 | Not started | Out of scope; require separate approval per `AGENTS.md` §12. |
| Git commit / push | Not performed | `AGENTS.md` §11 — the project owner performs all Git operations. |
