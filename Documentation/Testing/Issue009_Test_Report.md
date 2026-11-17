# Issue #009 — Test Report

**Lighting and Shading**

| Field | Value |
|---|---|
| Issue | #009 |
| Branch | `feature/issue-009-lighting` |
| Base branch | `feature/issue-008-transformations` (Issue #007 and #008 files unchanged) |
| Date of final verification | 2026-11-18 |
| MATLAB used | **R2026b (26.2.0.3386108)**, PCWIN64 |
| R2025b status | **NOT VERIFIED** — R2025b is not installed on this machine |
| Test runner | `MATLAB/Tests/runAllTests.m` (unchanged) |
| New tests | **29** (`tLighting`) |
| Focused new-suite result | **29 / 29 passed, 0 failed** (included in the full run below) |
| Full suite on this branch | **161 / 161 passed, 0 failed, 0 incomplete, 42.43 s** |
| Demo result | **PASS** — 3 PNG figures exported and programmatically verified |
| Evidence | `Deliverables/Screenshots/Issue009_*.png` |

> **Version caveat.** `AGENTS.md` §14.1 names MATLAB R2025b as the project target.
> Every result below was produced on R2026b, the only MATLAB installation
> available here. R2025b compatibility is **untested**, not "passing".

---

## 1. Commands executed

### 1.1 Full suite + demo + output check (final run)

```powershell
& 'D:\stage3\bin\matlab.exe' -batch "cd('D:\Matlab-Unity'); addpath(genpath('MATLAB')); results=runAllTests(); outputs=demoLighting(); assert(numel(outputs)==3); for k=1:numel(outputs); info=dir(char(outputs(k))); assert(~isempty(info) && info.bytes>0, 'Missing or empty output'); end; disp('VERIFY_OK')"
```

```
=== MATLAB TEST SUMMARY ===
Total      : 161
Passed     : 161
Failed     : 0
Incomplete : 0
Duration   : 42.43 s
===========================
Saved D:\Matlab-Unity\Deliverables\Screenshots\Issue009_Normals.png (282236 bytes)
Saved D:\Matlab-Unity\Deliverables\Screenshots\Issue009_Lambertian_vs_Phong.png (306889 bytes)
Saved D:\Matlab-Unity\Deliverables\Screenshots\Issue009_Lighting_Components.png (213139 bytes)
VERIFY_OK: tests completed and three screenshots verified
```

The same command was run once before the compatibility fixes were applied; it
reported **159 / 161 passed** and two errors in `tLighting` (see §4).

### 1.2 Screenshot content verification

```powershell
& 'D:\stage3\bin\matlab.exe' -batch "cd('D:\Matlab-Unity\Deliverables\Screenshots'); files=dir('Issue009_*.png'); for k=1:numel(files); img=imread(files(k).name); rgb=reshape(img,[],size(img,3)); sample=rgb(1:97:end,:); fprintf('%s: %dx%d px, %d sampled unique colours, channel std %.1f, bytes %d\n', files(k).name, size(img,1), size(img,2), size(unique(sample,'rows'),1), std(double(rgb(:))), files(k).bytes); end"
```

| File | Dimensions | Sampled unique colours | Channel σ | Bytes |
|---|---|---:|---:|---:|
| `Issue009_Normals.png` | 1286 × 2086 | 141 | 40.6 | 282 236 |
| `Issue009_Lambertian_vs_Phong.png` | 1283 × 2096 | 977 | 103.9 | 306 889 |
| `Issue009_Lighting_Components.png` | 1286 × 1945 | 513 | 89.8 | 213 139 |

All three files are valid PNGs with substantial non-uniform content, so they
are not blank. The agent could not view the images directly (the image-reading
tool is unavailable for this model); a visual review by the project owner is
still recommended.

### 1.3 Demo console summary (final run)

```
Clean My Room - Issue #009 lighting and shading
MATLAB 26.2.0.3386108 (R2026b) on PCWIN64
Lighting verification:
  light position     : [4 -5 6]
  light intensity    : 1.20, ambient 0.18
  mean RGB terms     : ambient 0.0143, diffuse 0.0281, specular 0.0003
  brightest vertex   : 95 of 362
  specular present   : true
Saved 3 evidence figures: ...
```

---

## 2. Full-suite breakdown

| Test class | Tests | Passed | Duration |
|---|---:|---:|---:|
| `t3DObjects` (Issue #007) | 25 | 25 | 3.78 s |
| `tCodec` | 18 | 18 | 1.09 s |
| `tLighting` (**new**) | **29** | **29** | 12.63 s |
| `tMesh3D` | 22 | 22 | 1.37 s |
| `tServerIntegration` | 17 | 17 | 20.23 s |
| `tSession` | 8 | 8 | 0.75 s |
| `tTransform3D` (Issue #008) | 38 | 38 | 1.99 s |
| `tValidator` | 4 | 4 | 0.58 s |
| **Total** | **161** | **161** | **42.43 s** |

`132` pre-existing tests on the base branch plus `29` new tests = `161`. No
pre-existing test was modified, skipped or removed. `runAllTests.m` was not
changed; it discovers `tLighting` automatically.

---

## 3. `tLighting` — 29 tests, all passed

| # | Test | Verifies |
|---:|---|---|
| 1 | `light3DUsesDocumentedDefaults` | position stored; white light, intensity 1, ambient 0.15 |
| 2 | `light3DRejectsInvalidValues` | non-finite position, RGB out of `[0,1]`, negative intensity, ambient > 1 |
| 3 | `directionToReturnsOutwardUnitVectors` | unit directions for two points toward `[3 4 0]` |
| 4 | `directionToRejectsBadAndCoincidentPoints` | coincident light point and malformed point array |
| 5 | `attenuationFollowsDocumentedDistanceFormula` | `1/(1+0.05 d^2)` at `d=3` and `d=0` |
| 6 | `ambientColorCombinesColorIntensityAndAmbient` | `colour .* (I*a)` |
| 7 | `material3DUsesNeutralDefaults` | name and four documented default properties |
| 8 | `fromColorDerivesAllComponents` | ambient `0.15*c`, diffuse `c`, specular `0.2`, shininess 16 |
| 9 | `fromColorHonoursOptionalArguments` | custom shininess and specular scale |
| 10 | `material3DRejectsInvalidValues` | empty/non-text name, bad RGB, shininess < 1, bad scale |
| 11 | `ambientMatchesPublishedEquation` | `material.ambient .* light.ambientColor` |
| 12 | `diffuseIsLambertianAndVanishesOnBackFaces` | cosine law and zero on back/perpendicular faces |
| 13 | `specularDependsOnReflectionAndViewer` | highlight at view=light, zero behind and on back face |
| 14 | `illuminateReturnsConsistentComponents` | shapes, sum, repeated ambient row |
| 15 | `normalsAreNormalisedBeforeLighting` | `[0 0 25]` equals `[0 0 1]` |
| 16 | `lightingModelRejectsInvalidInputs` | material/light type, points, zero normal, viewer cases |
| 17 | `faceNormalsGatewayMatchesMesh3D` | `SurfaceNormals` delegates to `Mesh3D.faceNormals` |
| 18 | `faceCentersAndAreasOfCubeAreExact` | six centres at radius 1, every face area 4 |
| 19 | `cubeVertexNormalsAreAreaWeightedAndUnitLength` | `sign(v)/sqrt(3)`, unit length |
| 20 | `outwardNormalsPointAwayFromMeshCentre` | cube and sphere, face and vertex normals |
| 21 | `orientOutwardsFlipsInwardNormals` | all flipped flags true, result equals outward normals |
| 22 | `isOutwardDetectsNormalOrientation` | true for outward, false for reversed |
| 23 | `visualizeDrawsQuiverOverMesh` | returns a `quiver` graphics object |
| 24 | `surfaceNormalsRejectInvalidInputs` | mesh, sample points, axes handle, normal type |
| 25 | `lambertianProducesFlatPerFaceColors` | per-face colours, zero vertex colours, no specular term |
| 26 | `phongProducesSmoothPerVertexColors` | per-vertex colours, specular present, face mean, total sum |
| 27 | `shadeDispatchesByMethodName` | `flat`/`SMOOTH` aliases and invalid method |
| 28 | `renderCreatesPatchWithComputedColors` | flat and interp patches, `FaceVertexCData`, bad result |
| 29 | `allPrimitivesShadeWithFiniteColors` | cube, sphere, cylinder all finite and clipped |

Figures created by tests 23 and 28 are invisible and closed by `onCleanup`.

---

## 4. Defects found and fixed

All fixes were made **only** to the five Issue #009 classes; no class was
recreated and no public signature changed.

| # | Defect found by | Symptom | Fix |
|---:|---|---|---|
| 1 | API check on installed MATLAB | `patch` has no `VertexColors` property | `Shading3D.render`: use `FaceVertexCData` for interpolated vertex colours |
| 2 | Tests 23 and 28, first full run (159/161) | `MATLAB:hg:InvalidProperty — Unrecognized property Lighting for class Patch` | `Shading3D.render` and `SurfaceNormals.visualize`: use `FaceLighting` (the `Lighting` property belongs to axes) |
| 3 | Test 25 / code review | Lambertian result contained a non-zero specular term despite its ambient+diffuse contract | `Shading3D.lambertian`: zero `components.specular` and set `components.total = ambient + diffuse` |

After these fixes, tests 23 and 28 passed and the full suite reached 161/161.

---

## 5. Test plan status

| Area | Planned check | Status |
|---|---|---|
| Light geometry | defaults, validation, direction, attenuation, ambient | PASS |
| Material properties | defaults, factory derivation, options, validation | PASS |
| Lighting equations | ambient, diffuse, specular, component sum, normalisation | PASS |
| Normal fields | face normals, centres, areas, vertex weights, outward test, visualisation | PASS |
| Shading algorithms | flat vs smooth, dispatch, colour ranges, rendering | PASS |
| Primitive integration | all three Issue #007 meshes shade to finite colours | PASS |
| Regression | all pre-existing MATLAB tests | PASS (161/161) |
| Demo | three figures generated, exported, closed | PASS |
| Screenshot evidence | valid PNG, non-blank, expected size | PASS (programmatic) |
| Visual review of figures | human inspection of composition/labels | NOT RUN — project owner action |
| MATLAB R2025b execution | run suite and demo on R2025b | NOT RUN — not installed |

---

## 6. Manual verification instructions

1. Start MATLAB R2025b (when available) from the repository root.
2. Run `addpath(genpath('MATLAB')); runAllTests` and confirm 0 failures.
3. Run `outputs = demoLighting()` and open the three PNG files listed in
   `Documentation/Graphics/Lighting_and_Shading.md` §6. Check that:
   - normal arrows point outwards on all three primitives;
   - flat shading shows constant colour per polygon and Phong shows a smooth
     highlight;
   - the specular-only panel is dark except near the highlight, and shininess
     96 gives a smaller highlight than shininess 4.
4. Confirm no MATLAB figure windows remain open after the demo.

---

## 7. Remaining work / unresolved items

- R2025b has not been executed; only the installed R2026b result is recorded.
- No Unity change is part of Issue #009; the MATLAB results are not yet wired
  into the game scene (belongs to a later issue).
- Human visual review of the three screenshots is outstanding.
- Git operations remain the project owner's responsibility: all changes are
  left uncommitted in the working directory on `feature/issue-009-lighting`.
