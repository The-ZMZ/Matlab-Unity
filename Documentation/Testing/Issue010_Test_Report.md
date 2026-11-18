# Issue #010 — Test Report

**3D Projection and Geometry Export**

| Field | Value |
|---|---|
| Issue | #010 |
| Branch | `feature/issue-010-projection-export` |
| Working tree before changes | clean (verified with `git status --porcelain`) |
| Date of final verification | 2026-11-18 |
| MATLAB used | **R2026b (26.2.0.3386108)**, PCWIN64 |
| R2025b status | **NOT VERIFIED** — R2025b is not installed on this machine |
| Unity used | **6000.6.5f1 (3ff58d469c8a)** |
| New MATLAB tests | **37** (`tProjection` 21, `tGeometryExporter` 16) |
| Focused MATLAB result | **37 / 37 passed, 0 failed** |
| Full MATLAB suite | **198 / 198 passed, 0 failed, 0 incomplete, 37.36 s** |
| Demo result | **PASS** — 3 PNG figures, 4 geometry files and a manifest written and verified |
| Unity import | **PERFORMED** — real `ModelImporter` run in Unity batchmode |
| Unity EditMode tests | **5 / 5 passed, 0 failed, 0 skipped, 3.595 s** |
| Evidence | `Deliverables/Screenshots/Issue010_*.png`, `Deliverables/Reports/Issue010_Unity_Import_Result.txt`, `Deliverables/Reports/Issue010_Unity_TestResults.xml` |

> **Version caveat.** `AGENTS.md` §14.1 names MATLAB R2025b as the target. All
> MATLAB results below were produced with R2026b, the only installed version.
> R2025b compatibility is **untested**, not "passing".

---

## 1. Command and result summary

| # | Test / check | Command (abridged) | Result |
|---|---|---|---|
| 1 | Focused new MATLAB tests | `matlab -batch` with `TestSuite.fromClass(?tProjection)` and `?tGeometryExporter` | **37 passed, 0 failed** |
| 2 | Full regression suite | `matlab -batch "cd('D:\Matlab-Unity'); addpath(genpath('MATLAB')); results=runAllTests();"` | **198 passed, 0 failed, 0 incomplete, 37.36 s** |
| 3 | Demo | `matlab -batch "…outputs=demoProjectionExport();…"` | **PASS**, 3 figures + 5 export files |
| 4 | Screenshot integrity | `imread` on every `Issue010_*.png` | **PASS**, non-empty, non-uniform |
| 5 | Real Unity import | `Unity.exe -batchmode -executeMethod …` (temporary diagnostic) | **COMPLETED**, values recorded |
| 6 | Unity EditMode tests | `Unity.exe -runTests -batchmode … -testFilter CleanMyRoom.Tests.MeshImportTests` | **5 passed, 0 failed, 0 skipped, 3.595 s** |

No test or check was reported as passed without being executed.

---

## 2. Focused MATLAB tests (new functionality)

Exact command:

```powershell
& 'D:\stage3\bin\matlab.exe' -batch "cd('D:\Matlab-Unity'); addpath(genpath('MATLAB')); import matlab.unittest.*; suite=[TestSuite.fromClass(?tProjection), TestSuite.fromClass(?tGeometryExporter)]; runner=TestRunner.withTextOutput(); results=runner.run(suite); fprintf('\nFOCUSED SUMMARY: total=%d passed=%d failed=%d\n', numel(results), sum([results.Passed]), sum([results.Failed]));"
```

Output:

```
Running tProjection
.......... .......... .
Done tProjection
__________

Running tGeometryExporter
.......... ......
Done tGeometryExporter
__________

FOCUSED SUMMARY: total=37 passed=37 failed=0
```

Coverage in `tProjection` (21 tests): textbook orthographic and perspective
matrices, radian option, view-matrix placement of eye/target/up, clip
coordinates, homogeneous division, near/far mapping, behind-camera rejection,
mesh projection for all three primitives, Euclidean and mesh-centre distance,
constant orthographic size with distance, inverse perspective size falloff,
ortho/perspective comparison, and validation errors.

Coverage in `tGeometryExporter` (16 tests): vertex/face/normal/colour writing,
MTL Phong fields, optional-data switches, subset OBJ reader, reading Issue #007
plain OBJ, Unity axis remap and winding reversal, MATLAB passthrough, exported
Unity bounds, coordinate-system validation, folder creation and extension
handling, derived material, transformed-mesh fidelity, deterministic output,
and invalid inputs.

---

## 3. Full MATLAB regression

Exact command:

```powershell
& 'D:\stage3\bin\matlab.exe' -batch "cd('D:\Matlab-Unity'); addpath(genpath('MATLAB')); results=runAllTests();"
```

`MATLAB/Tests/runAllTests.m` was **not modified**; it auto-discovers the two new
test classes. Result:

```
Total     : 198
Passed    : 198
Failed    : 0
Incomplete: 0
Duration  : 37.36 s
```

This includes all Issue #007 (47), Issue #008 (38), Issue #009 (29) and
communication/activity tests, plus the 37 new Issue #010 tests.

---

## 4. Demo and generated evidence

Exact command:

```powershell
& 'D:\stage3\bin\matlab.exe' -batch "cd('D:\Matlab-Unity'); addpath(genpath('MATLAB')); outputs=demoProjectionExport(); fprintf('\nDEMO_OUTPUTS=%d\n', numel(outputs)); for k=1:numel(outputs); info=dir(char(outputs(k))); fprintf('%s %d bytes\n', outputs(k), info.bytes); end"
```

Result: **PASS**. Key output:

```
Gallery projection extents (NDC width x height):
  Cube     ortho 0.6667 x 1.1068 | perspective 0.6276 x 0.7597 | ratio 0.941
  Sphere   ortho 0.8000 x 1.0664 | perspective 0.5468 x 0.7289 | ratio 0.684
  Cylinder ortho 0.6000 x 1.1607 | perspective 0.4653 x 0.7953 | ratio 0.776

Distance test (cube side 2, camera distance d):
  d =  4 | measured distance 4.00 | ortho width 1.0000 | perspective width 1.1547
  d =  8 | measured distance 8.00 | ortho width 1.0000 | perspective width 0.4949
  d = 12 | measured distance 12.00 | ortho width 1.0000 | perspective width 0.3149

Export verification:
  MATLAB Z-up bounds : [2.000 3.000 5.000], 8 vertices, 12 triangles
  Unity  Y-up bounds : [2.000 5.000 3.000], centre [1.000 3.000 2.000]
```

The demo writes the following files (all verified non-empty):

| File | Bytes |
|---|---:|
| `Deliverables/Screenshots/Issue010_Orthographic_vs_Perspective.png` | 169233 |
| `Deliverables/Screenshots/Issue010_Distance_Comparison.png` | 69880 |
| `Deliverables/Screenshots/Issue010_OBJ_Export.png` | 105661 |
| `Shared_Data/Meshes/Issue010/Issue010_Box_MATLAB.obj` / `.mtl` | — |
| `Shared_Data/Meshes/Issue010/Issue010_Box_Unity.obj` / `.mtl` | — |
| `Shared_Data/Meshes/Issue010/Export_Manifest.txt` | — |

Screenshot integrity check with `imread`:

| Image | Resolution | Sampled unique colours | Channel std |
|---|---|---:|---:|
| `Issue010_Orthographic_vs_Perspective.png` | 1306 × 2040 | 105 | 46.8 |
| `Issue010_Distance_Comparison.png` | 1306 × 2040 | 81 | 52.8 |
| `Issue010_OBJ_Export.png` | 1129 × 1762 | 66 | 36.9 |

These programmatic checks confirm the figures are real, non-empty rendered
images. No human visual review was performed by the AI agent.

---

## 5. Unity import verification (actually performed)

Unity was available, so the import was **not** left as a manual-only step.

### 5.1 Real import diagnostic

A temporary Editor script copied the MATLAB-generated OBJ/MTL files into the
project, imported them through Unity's real `ModelImporter`, and wrote
`Deliverables/Reports/Issue010_Unity_Import_Result.txt`. The script was deleted
afterwards; the permanent test is `MeshImportTests.cs`.

Recorded results (Unity 6000.6.5f1, `ModelImporter.globalScale = 1`):

| Property | MATLAB Z-up OBJ | Unity Y-up OBJ |
|---|---|---|
| Imported object | `Issue010_Box_MATLAB` | `Issue010_Box_Unity` |
| Vertices | 8 | 8 |
| Triangle indices | 36 | 36 |
| Normals | 8 | 8 |
| Bounds centre | `[-1, 2, 3]` | `[-1, 3, 2]` |
| Bounds size | `[2, 3, 5]` | `[2, 5, 3]` |
| Minimum outward normal dot | 0.768417 | 0.768417 |
| Material | `CleanMyRoom_box` | `CleanMyRoom_box` |
| `Material.color` | `[0.484529, 0.767376, 0.954687, 1]` | same |

Findings:

1. The Unity-variant file imports with the correct Y-up height of 5; the raw
   MATLAB file imports with height 3. The documented remap is necessary.
2. Unity's OBJ importer mirrors X (right-handed → left-handed), so file
   `x ∈ [0, 2]` becomes imported `x ∈ [−2, 0]` and the centre X is `−1`. Size is
   unchanged; this is expected importer behaviour, not an export defect.
3. Face winding and normals survive the remap: all normals still point outwards.
4. MTL `Kd` is treated as linear colour; `Material.color` shows the display
   (sRGB) value `[0.4845, 0.7674, 0.9547]`, matching the linear→sRGB conversion
   of `[0.2, 0.55, 0.9]`.

### 5.2 Unity EditMode test run

Exact command:

```
Unity.exe -runTests -batchmode -nographics -projectPath D:\Matlab-Unity\Unity\CleanMyRoom ^
  -testPlatform EditMode -testFilter CleanMyRoom.Tests.MeshImportTests ^
  -testResults D:\Matlab-Unity\Deliverables\Reports\Issue010_Unity_TestResults.xml
```

An earlier attempt included `-quit`; Unity exited before the test runner
started and produced no results file. Removing `-quit` produced the valid run
below. Result XML header:

```xml
<test-run id="2" testcasecount="5" result="Passed" total="5" passed="5"
          failed="0" inconclusive="0" skipped="0" engine-version="3.5.0.0"
          start-time="2026-11-18 19:29:28Z" end-time="2026-11-18 19:29:31Z"
          duration="3.5950941">
```

Individual results:

| Test | Result | Duration (s) |
|---|---|---:|
| `CleanMyRoom.Tests.MeshImportTests.ImportsAllVerticesTrianglesAndNormals` | Passed | 2.053663 |
| `CleanMyRoom.Tests.MeshImportTests.ImportsDocumentedPhongMaterial` | Passed | 0.381428 |
| `CleanMyRoom.Tests.MeshImportTests.MatlabVariantKeepsZUpDimensions` | Passed | 0.365066 |
| `CleanMyRoom.Tests.MeshImportTests.ObjFilesUseDocumentedAxisConventions` | Passed | 0.326117 |
| `CleanMyRoom.Tests.MeshImportTests.UnityVariantImportsWithDocumentedDimensionsAndOrientation` | Passed | 0.374819 |

The test assembly `CleanMyRoom.Tests.Editor` compiled successfully. The test
framework is resolved transitively in `Packages/packages-lock.json`
(`com.unity.test-framework` 1.8.0, builtin), so **no package manifest change was
needed**. The test class copies files into a temporary `Assets/Issue010ImportTest`
folder and removes it in `TearDown`; no temporary asset remains in the project.

---

## 6. Files created

| Path | Purpose |
|---|---|
| `MATLAB/Graphics/+cmr/+graphics/Projection3D.m` | View/ortho/perspective matrices, projection pipeline, distance, comparison, validation. |
| `MATLAB/Graphics/+cmr/+graphics/GeometryExporter.m` | OBJ/MTL export, Unity remap, verification OBJ reader. |
| `MATLAB/Tests/tProjection.m` | 21 focused projection tests. |
| `MATLAB/Tests/tGeometryExporter.m` | 16 focused export tests. |
| `MATLAB/Graphics/demoProjectionExport.m` | Working demo, screenshots, exports, manifest. |
| `Unity/CleanMyRoom/Assets/Tests/Editor/MeshImportTests.cs` (+ `.meta`) | Unity EditMode import test. |
| `Documentation/Graphics/Projection_and_Geometry_Export.md` | Matrices, formats and conventions. |
| `Documentation/Testing/Issue010_Test_Report.md` | This report. |
| `Deliverables/Screenshots/Issue010_*.png` | Three evidence figures. |
| `Deliverables/Reports/Issue010_Unity_Import_Result.txt` | Real import measurements. |
| `Deliverables/Reports/Issue010_Unity_TestResults.xml` | Unity test runner output. |
| `Shared_Data/Meshes/Issue010/*` | Four OBJ/MTL files and export manifest. |

**Modified files:** none. (Unity's first-open normalized four `.asset` YAML files
by changing line endings only; `git diff` reported zero content changes and
the files were restored to CRLF so the working tree contains no incidental
modifications.)

---

## 7. Limitations and unresolved items

1. **MATLAB R2025b not verified** — only R2026b is installed. The code uses base
   MATLAB and R2025b-compatible syntax, but the target version was not run.
2. **Visual review** — screenshot evidence was verified programmatically
   (resolution, colour diversity, byte size); an AI agent did not perform a
   subjective visual quality review.
3. **MTL scope** — only ambient, diffuse, specular, shininess, opacity and
   illumination model are exported; textures and normal maps are out of scope.
4. **Unity axis handling is version-specific** — the import test pins the
   behaviour observed on Unity 6000.6.5f1 and may need updating if the importer
   changes.
5. **Git operations** — none performed, as required by `AGENTS.md` §11. All
   changes are left uncommitted for the project owner.
6. **Issue closure** — awaiting human review and approval; no Issue was closed.
