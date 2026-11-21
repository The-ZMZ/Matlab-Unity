# Issue #008 — Test Report

**3D Geometric Transformations**

| Field | Value |
|---|---|
| Issue | #008 |
| Branch | `feature/issue-008-transformations` |
| Base branch | `feature/issue-007-graphics` (Issue #007 PR #16 is open/unmerged) |
| Date of final verification | 2026-11-17 |
| MATLAB used | **R2026b (26.2.0.3386108)**, PCWIN64 |
| R2025b status | **NOT VERIFIED** — R2025b is not installed on this machine |
| Test runner | `MATLAB/Tests/runAllTests.m` (unchanged) |
| New tests | **38** (`tTransform3D`) |
| Focused new-suite result | **38 / 38 passed, 0 failed** |
| Full suite on this branch | **132 / 132 passed, 0 failed, 0 incomplete** |
| Evidence | 3 PNG figures in `Deliverables/Screenshots/Issue008_*.png` |

> **Version caveat.** `AGENTS.md` §14.1 names MATLAB R2025b as the project target.
> Every result below was produced on R2026b, the only MATLAB installation
> available here. R2025b compatibility is **untested**, not "passing".

---

## 1. Commands executed

### 1.1 New transformation suite

```powershell
& "D:\stage3\bin\matlab.exe" -batch "cd('D:/Matlab-Unity/MATLAB/Tests'); r = runtests('tTransform3D'); assertSuccess(r); fprintf('RESULT %d/%d passed\n', sum([r.Passed]), numel(r));"
```

```
RESULT 38/38 passed
```

### 1.2 Full regression suite on this branch

```powershell
& "D:\stage3\bin\matlab.exe" -batch "cd('D:/Matlab-Unity/MATLAB/Tests'); runAllTests"
```

```
=== MATLAB TEST SUMMARY ===
Total      : 132
Passed     : 132
Failed     : 0
Incomplete : 0
Duration   : 30.88 s
===========================
```

Per class from the same run:

| Test class | Tests | Passed | Duration |
|---|---:|---:|---:|
| `t3DObjects` | 25 | 25 | 4.77 s |
| `tCodec` | 18 | 18 | 1.32 s |
| `tMesh3D` | 22 | 22 | 1.41 s |
| `tServerIntegration` | 17 | 17 | 20.02 s |
| `tSession` | 8 | 8 | 0.80 s |
| `tTransform3D` | 38 | 38 | 1.98 s |
| `tValidator` | 4 | 4 | 0.59 s |
| **Total** | **132** | **132** | **30.88 s** |

`94` pre-existing tests on `feature/issue-007-graphics` plus `38` new tests =
`132`. No pre-existing test was modified, skipped or removed, and
`runAllTests.m` was not changed. The older 190-test figure belongs to a tree
that also contains the Issue #006 2D test classes, which are not present on this
branch.

---

## 2. `tTransform3D` — 38 tests, all passed

### 2.1 Basics and translation (6)

| # | Test | Verifies |
|---:|---|---|
| 1 | `identityIsAffineFourByFour` | identity is finite real 4×4 and affine |
| 2 | `identityLeavesPointsUnchanged` | identity maps several points to themselves |
| 3 | `translationMatrixUsesLastRowForRowVectors` | offset appears in the last row for row-vector convention |
| 4 | `translationAcceptsVectorForm` | `translation([dx dy dz])` equals three scalar arguments |
| 5 | `translationMovesEveryPointByTheSameOffset` | all points move by the same `[dx dy dz]` |
| 6 | `translationRejectsInvalidInput` | wrong argument count, bad vector length, `NaN`, char input |

### 2.2 Rotation (8)

| # | Test | Verifies |
|---:|---|---|
| 7 | `rotationXQuarterTurnUsesRightHandRule` | exact `Rx(pi/2)` and `+y → +z` |
| 8 | `rotationYQuarterTurnUsesRightHandRule` | exact `Ry(pi/2)` and `+z → +x` |
| 9 | `rotationZQuarterTurnUsesRightHandRule` | exact `Rz(pi/2)` and `+x → +y` |
| 10 | `rotationsAcceptDegrees` | `"degrees"`, `"deg"` and `"DEGREES"` equal radian values |
| 11 | `rotationsRejectInvalidAnglesAndUnits` | `NaN`, vector angle and unsupported units rejected |
| 12 | `rotationLinearPartsAreOrthonormal` | `R(1:3,1:3)*R(1:3,1:3)' = I`, determinant `1` |
| 13 | `rotationsPreserveOriginAndDistances` | origin fixed and point norms preserved |
| 14 | `zeroAndFullTurnsAreIdentity` | `0` and `2*pi` produce identity for all three axes |

### 2.3 Scaling (5)

| # | Test | Verifies |
|---:|---|---|
| 15 | `scalingMatrixIsDiagonal` | `diag([2 -1 0.5 1])` |
| 16 | `scalingAcceptsUniformScalarAndVectorForms` | scalar, three-argument and vector forms agree |
| 17 | `scalingMultipliesEachCoordinateByItsFactor` | each coordinate scaled independently |
| 18 | `negativeScalingProducesReflection` | negative x factor reflects and determinant is `-1` |
| 19 | `scalingRejectsInvalidFactors` | zero, `NaN` and malformed vectors rejected |

### 2.4 Homogeneous coordinates and validation (5)

| # | Test | Verifies |
|---:|---|---|
| 20 | `toHomogeneousAppendsOnes` | N×3 points become N×4 with final column `1` |
| 21 | `fromHomogeneousDividesByW` | `[2 4 6 2] → [1 2 3]`, `[-3 -6 -9 3] → [-1 -2 -3]` |
| 22 | `fromHomogeneousHandlesNonCanonicalBottomRow` | general 4×4 with `w = 2` divides correctly and is not affine |
| 23 | `homogeneousHelpersValidateInput` | bad N×3, `NaN`, zero `w` and bad N×4 rejected |
| 24 | `transformPointsValidatesArguments` | bad matrix and bad point shapes rejected |

### 2.5 Composition, order and inverse (9)

| # | Test | Verifies |
|---:|---|---|
| 25 | `composeMatchesExplicitMatrixProduct` | `compose(T,R,S)` equals `T*R*S` |
| 26 | `composeAcceptsCellArrayAndSingleMatrix` | cell input and single-matrix pass-through |
| 27 | `composeAppliesTransformationsInArgumentOrder` | `T*S` gives `[4 2 3]`, `S*T` gives `[3 2 3]` |
| 28 | `rotationAndTranslationOrderChangesResult` | `R*T` gives `[3 1 0]`, `T*R` gives `[0 4 0]` |
| 29 | `composeRejectsEmptyAndInvalidMatrices` | empty sequence and malformed matrix rejected |
| 30 | `combineAliasMatchesCompose` | alias returns the same product |
| 31 | `inverseReversesCombinedTransformation` | inverse restores points after `T`, `R` and `S` |
| 32 | `inverseRejectsSingularAndInvalidMatrices` | singular and non-4×4 matrices rejected |
| 33 | `isAffineValidatesTolerance` | negative tolerance rejected |

### 2.6 Mesh integration (5)

| # | Test | Verifies |
|---:|---|---|
| 34 | `transformMeshMovesIssue007CubeVertices` | cube vertices translate; input mesh unchanged |
| 35 | `transformMeshPreservesMeshMetadata` | name, faces, colour, counts and side length preserved; centre rotates |
| 36 | `transformMeshKeepsFaceConnectivity` | cylinder triangulation indices unchanged after scale + rotate |
| 37 | `transformMeshHandlesSphereWithoutCenterChange` | sphere scales with finite vertices and centre `[0 0 0]` |
| 38 | `transformMeshValidatesArguments` | bad matrix and malformed mesh rejected |

---

## 3. Demonstration run

The final evidence was generated with the documented default output folder:

```powershell
& "D:\stage3\bin\matlab.exe" -batch "cd('D:/Matlab-Unity/MATLAB/Graphics'); demo3DTransformations;"
```

Final output (complete, no warnings, exit code 0):

```
Clean My Room - Issue #008 3D geometric transformations
MATLAB 26.2.0.3386108 (R2026b) on PCWIN64
Saved D:\Matlab-Unity\Deliverables\Screenshots\Issue008_Basic_Transformations.png (186197 bytes)
Saved D:\Matlab-Unity\Deliverables\Screenshots\Issue008_Transformation_Order.png (77871 bytes)
Saved D:\Matlab-Unity\Deliverables\Screenshots\Issue008_Transformed_Objects.png (104934 bytes)

Transformation verification:
  T*S on [1 0 0] : [5.0 0 0]
  S*T on [1 0 0] : [3.5 0 0]
  Orders differ  : true
  cube     :    8 vertices,    6 faces, bounds [1.07 -1.60 -1.83] to [4.13 1.60 1.83]
  sphere   :  262 vertices,  280 faces, bounds [1.28 -1.30 -1.31] to [3.92 1.30 1.31]
  cylinder :   40 vertices,   22 faces, bounds [1.15 -1.35 -1.57] to [4.05 1.35 1.57]

Saved 3 evidence figures:
  (the three paths listed above)
```

### 3.1 PNG verification

**Human visual inspection was not performed.** The exported files were analysed
numerically with `imread`: non-white pixel fraction overall and per panel, plus
colour masks on the order figure.

| Figure | Pixels | Bytes | Overall non-white |
|---|---:|---:|---:|
| `Issue008_Basic_Transformations.png` | 1994 × 1284 | 186 197 | 9.80 % |
| `Issue008_Transformation_Order.png` | 1537 × 739 | 77 871 | 9.05 % |
| `Issue008_Transformed_Objects.png` | 1692 × 865 | 104 934 | 7.65 % |

Per-panel non-white fractions:

| Figure | Panel values |
|---|---|
| Basic transformations (2×3, row-wise) | 10.80, 10.74, 8.85, 8.49, 9.67, 10.27 % |
| Transformation order (1×2) | 9.26, 8.85 % |
| Transformed objects (2×3, row-wise) | 7.17, 9.95, 7.21, 7.29, 7.12, 7.17 % |

Order-figure colour masks: **12 794 blue pixels** and **12 414 orange pixels**,
confirming that both order results are rendered. Every panel contains rendered
content; no panel is blank.

---

## 4. Defects found and fixed during verification

1. **Row-vector translation placement.** The first smoke test showed that a
   translation offset in the last column does not move a row-vector point. The
   matrix was corrected to place the offset in the last row, and
   `translationMovesEveryPointByTheSameOffset` plus the demo now verify the fix.
2. **Incorrect test expectation.** `scalingMultipliesEachCoordinateByItsFactor`
   initially expected an erroneous y/z result. The implementation was correct;
   the expected values were corrected to `[2 2 1.5; -8 5 -3]` before the final
   run.
3. **Demo order labels.** The order-comparison panel initially named the two
   compositions backwards. Titles and variables were corrected so `T*S` is
   labelled "translate first, then scale" and `S*T` is labelled "scale first,
   then translate".
4. **Transient headless MATLAB exit.** One intermediate demo run exited with
   status 1 and emitted no MATLAB diagnostic on this memory-constrained
   machine (about 1 GB free RAM). An immediate rerun succeeded, and a `drawnow`
   plus the existing one-retry `exportgraphics` logic were retained. The final
   demo run shown in §3 completed with exit code 0 and no warnings.
5. **Image-check command syntax.** The first ad-hoc image-analysis command
   failed because two MATLAB string rows of different lengths were vertically
   concatenated. The command was rewritten with a cell array; the successful
   results are reported in §3.1. This did not affect repository code.

---

## 5. Warnings and errors

| Run | Warnings | Errors | Result |
|---|---|---|---|
| `runtests('tTransform3D')` | none | none | 38/38 passed |
| `runAllTests` | none | none | 132/132 passed |
| `demo3DTransformations` (final) | none | none | 3 PNG files written |
| PNG numeric analysis | none | none | all panels non-blank |

No warning or error from a final run is hidden. The intermediate issues above
are listed in §4 for transparency.

---

## 6. Git state

The Issue #008 change set consists of exactly these 8 new files. No tracked
file was modified or deleted, and no Issue #006 or Issue #007 file was touched.

```
Deliverables/Screenshots/Issue008_Basic_Transformations.png
Deliverables/Screenshots/Issue008_Transformation_Order.png
Deliverables/Screenshots/Issue008_Transformed_Objects.png
Documentation/Graphics/3D_Geometric_Transformations.md
Documentation/Testing/Issue008_Test_Report.md
MATLAB/Graphics/+cmr/+graphics/Transform3D.m
MATLAB/Graphics/demo3DTransformations.m
MATLAB/Tests/tTransform3D.m
```

The feature branch was created from `feature/issue-007-graphics` after
confirming GitHub PR #16 is open and unmerged. The PR base is therefore
`feature/issue-007-graphics`; no PR was merged.

---

## 7. Manual verification steps

1. **Run the new suite**

   ```powershell
   & "D:\stage3\bin\matlab.exe" -batch "cd('D:/Matlab-Unity/MATLAB/Tests'); assertSuccess(runtests('tTransform3D'))"
   ```

   Expect 38 passed.

2. **Run the full available suite**

   ```powershell
   & "D:\stage3\bin\matlab.exe" -batch "cd('D:/Matlab-Unity/MATLAB/Tests'); runAllTests"
   ```

   Expect `Total 132 | Passed 132 | Failed 0 | Incomplete 0` on this branch.

3. **Regenerate evidence**

   ```matlab
   cd('D:/Matlab-Unity/MATLAB/Graphics')
   demo3DTransformations
   ```

   Expect three PNG files under `Deliverables/Screenshots/` and a console
   summary showing `T*S = [5 0 0]`, `S*T = [3.5 0 0]` and `Orders differ: true`.

4. **Human visual inspection of the three PNGs** — **NOT DONE** by the agent.
   Owner review is requested. Expected content:

   - figure 1: six panels, each with a translucent grey original cube and a
     solid coloured transformed cube;
   - figure 2: translate-then-scale cube ends farther right than scale-then-
     translate, with matching coordinate annotations;
   - figure 3: original and transformed cube, sphere and cylinder, each with
     labelled axes, grid, lighting and consistent proportions.

5. **Spot-check the API**

   ```matlab
   addpath('D:/Matlab-Unity/MATLAB/Graphics');
   T = cmr.graphics.Transform3D.translation(1, 2, 3);
   cmr.graphics.Transform3D.transformPoints(T, [0 0 0])      % [1 2 3]
   R = cmr.graphics.Transform3D.rotationZ(90, 'degrees');
   cmr.graphics.Transform3D.transformPoints(R, [1 0 0])      % [0 1 0]
   cube = cmr.graphics.Cube3D.create(2);
   moved = cmr.graphics.Transform3D.transformMesh(T, cube);  % input unchanged
   ```

---

## 8. Items not run / outstanding

| Item | Status | Reason |
|---|---|---|
| MATLAB R2025b test run | **NOT RUN** | R2025b not installed; only R2026b tested |
| Human visual inspection of the three PNGs | **NOT DONE** | Numeric image analysis only; owner review requested |
| Full 190-test merge suite (incl. Issue #006) | **NOT RUN on this branch** | Issue #006 2D test classes are not present here |
| Unity import / rendering of transformed meshes | **NOT RUN** | Out of scope for this MATLAB-only issue |
| MATLAB↔Unity protocol changes | Not applicable | No protocol or C# file was changed |
| Merge of any pull request | **NOT PERFORMED** | Explicitly prohibited for this task |
