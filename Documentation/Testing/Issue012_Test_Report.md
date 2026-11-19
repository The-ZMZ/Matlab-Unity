# Issue #012 — Test Report

**3D Bedroom Environment**

| Field | Value |
|---|---|
| Issue | #012 (GitHub #20) |
| Branch | `feature/issue-012-bedroom-environment` |
| Date | 2026-11-19 |
| Unity used | **6000.6.5f1 (3ff58d469c8a)**, Windows, URP |
| EditMode tests | **41 passed, 0 failed, 1 skipped** (42 total, 3.12 s) |
| PlayMode tests | **4 passed, 0 failed, 0 skipped** (4 total, 0.37 s) |
| New Issue #012 tests | 6 EditMode + 2 PlayMode, all passed |
| Regression | Issues #007–#011 suites re-run and still pass |
| Git operations | **NONE** (no stage, commit, push, merge, branch or history change) |

> No result below is claimed as passed unless it was produced by the Unity Test
> Runner in batchmode. Raw runner output is in the XML files listed in §6.

---

## 1. Checks executed

| # | Check | Platform | Result |
|---|---|---|---|
| 1 | Scene builder executes and saves `Bedroom.unity` | Editor batch `-executeMethod` | **PASS** (exit 0) |
| 2 | MATLAB OBJ/MTL import into `Assets/Models/MATLAB` | Editor batch | **PASS** — 4 models imported, vertex counts logged |
| 3 | Full Unity EditMode suite | EditMode | **41 passed, 0 failed, 1 skipped** |
| 4 | Full Unity PlayMode suite | PlayMode | **4 passed, 0 failed, 0 skipped** |
| 5 | Screenshot capture to `Deliverables/Screenshots` | Editor batch, Camera.Render | **PASS** — 3 PNGs, 1280 × 720 |
| 6 | Scene architecture regression (#011) | EditMode | **10 passed** |
| 7 | Existing mesh-import regression (#010) | EditMode | **5 passed** |
| 8 | Existing codec/framer regression (#006/#007) | EditMode | **20 passed, 1 explicitly skipped** |
| 9 | MATLAB server integration / activity validation | — | **NOT RUN** — out of scope; no MATLAB code changed |
| 10 | Interactive keyboard/mouse navigation walkthrough | — | **NOT RUN** — no player controller exists yet |

The single skipped EditMode test is the pre-existing explicit
`CleanMyRoom.Tests.NetworkIntegrationTests.ConnectsPingsPlacesAndReconnects`,
which requires a MATLAB server on `127.0.0.1:55001`. It is skipped by the
test framework, not by this Issue.

---

## 2. Builder and import run

Exact command:

```powershell
$unity = 'D:\Unity\Unity Hub\6000.6.5f1\Editor\Unity.exe'
$args = @('-batchmode','-nographics','-quit',
  '-projectPath','D:\Matlab-Unity\Unity\CleanMyRoom',
  '-executeMethod','CleanMyRoom.Editor.Issue012.BedroomEnvironmentBuilder.Build',
  '-logFile','C:\Temp\Issue012_Build.log')
Start-Process -FilePath $unity -ArgumentList $args -Wait -PassThru
```

Exit code 0. Relevant log lines:

```
[Issue012] Imported Assets/Models/MATLAB/Issue010/Issue010_Box_Unity.obj (8 vertices)
[Issue012] Imported Assets/Models/MATLAB/Issue007/MATLAB_Cube.obj (24 vertices)
[Issue012] Imported Assets/Models/MATLAB/Issue007/MATLAB_Sphere.obj (2186 vertices)
[Issue012] Imported Assets/Models/MATLAB/Issue007/MATLAB_Cylinder.obj (192 vertices)
[Issue012] Bedroom built: 57 objects, 42 materials, scene saved to Assets/Scenes/Bedroom.unity
```

The first build attempt exposed two editor-script API errors (`Camera.updateMatrixWorld`
and `ModelImporter.importMaterials`, removed/renamed in Unity 6000); both were
fixed. A second attempt exposed a `??`/fake-null issue when adding the ceiling
`Light`, also fixed. The final run above is the authoritative one.

---

## 3. EditMode run (authoritative)

Exact command:

```powershell
$args = @('-batchmode','-nographics',
  '-projectPath','D:\Matlab-Unity\Unity\CleanMyRoom',
  '-runTests','-testPlatform','EditMode',
  '-testResults','D:\Matlab-Unity\Deliverables\Reports\Issue012_Unity_TestResults.xml',
  '-logFile','C:\Temp\Issue012_EditMode.log')
Start-Process -FilePath $unity -ArgumentList $args -Wait -PassThru
```

Result XML header:

```xml
<test-run id="2" testcasecount="42" result="Passed" total="42" passed="41"
          failed="0" inconclusive="0" skipped="1" ... duration="3.1243329">
```

| Assembly | Cases | Passed | Failed | Skipped |
|---|---:|---:|---:|---:|
| `CleanMyRoom.Tests.BedroomEnvironment.dll` (new) | 6 | 6 | 0 | 0 |
| `CleanMyRoom.Tests.Editor.dll` (#006–#010, unchanged) | 26 | 25 | 0 | 1 |
| `CleanMyRoom.Tests.SceneArchitecture.dll` (#011, unchanged) | 10 | 10 | 0 | 0 |

New tests and what they verify:

| Test | Result |
|---|---|
| `BedroomSceneAssetExistsAndPreservesArchitecture` | Passed |
| `RequiredEnvironmentElementsExistWithValidTransforms` | Passed |
| `RequiredFurnitureExistsWithReasonableDimensionsAndColliders` | Passed |
| `MatlabGeneratedMeshesAreImportedAndUsedInScene` | Passed |
| `NavigationWaypointsAreNotBlockedByFurniture` | Passed |
| `AllBedroomMaterialsAndPrefabAssetsExist` | Passed |

Coverage: scene asset/meta and YAML identity, preserved camera/light/floor/walls,
Build Settings registration and `SceneLoader` resolution; four walls, ceiling,
door, three windows, glass renderers and three active lights; bed, table, desk,
wardrobe, bookshelf, rug, divider, nightstand with valid positions/scales and
colliders; prefab linkage for chairs; all five MATLAB source copies present and
four imported meshes with non-zero geometry; seven placed MATLAB objects with
renderers and materials; five clear navigation waypoints; prefab and material
assets exist.

An earlier EditMode run produced 3 failures, all in the **new test code**: two
wrong absolute-path calculations (`Scenes`/`Assets/Models` resolved one folder
too high) and one object-name mismatch (`Room_Divider` vs `Room Divider`).
The tests were corrected and the final run above is the authoritative result.
No production scene code was changed for those failures.

---

## 4. PlayMode run

Exact command:

```powershell
$args = @('-batchmode','-nographics',
  '-projectPath','D:\Matlab-Unity\Unity\CleanMyRoom',
  '-runTests','-testPlatform','PlayMode',
  '-testResults','D:\Matlab-Unity\Deliverables\Reports\Issue012_PlayMode_TestResults.xml',
  '-logFile','C:\Temp\Issue012_PlayMode.log')
Start-Process -FilePath $unity -ArgumentList $args -Wait -PassThru
```

Result XML header:

```xml
<test-run id="2" testcasecount="4" result="Passed" total="4" passed="4"
          failed="0" inconclusive="0" skipped="0" ... duration="0.3702441">
```

| Assembly | Cases | Passed | Failed |
|---|---:|---:|---:|
| `CleanMyRoom.Tests.BedroomEnvironment.PlayMode.dll` (new) | 2 | 2 | 0 |
| `CleanMyRoom.Tests.PlayMode.dll` (#011, unchanged) | 2 | 2 | 0 |

| Test | Result |
|---|---|
| `BedroomLoadsThroughSceneLoaderWithEnvironmentIntact` | Passed |
| `MatlabImportedObjectsRenderWithMaterialsAtRuntime` | Passed |

These load the real Bedroom scene at runtime through the Issue #011
`SceneLoader`, confirm the active scene, all environment/furniture objects,
three enabled lights, more than 15 enabled colliders, and that every placed
MATLAB object has an enabled renderer with a material.

An initial PlayMode run failed the second new test because `SceneLoader.Load`
correctly treats the already-active scene as a no-op; the test now skips the
reload when Bedroom is already active. The final run above is authoritative.

---

## 5. Screenshots

Generated with `BedroomEnvironmentBuilder.CaptureScreenshots` using
`Camera.Render()` into a 1280 × 720 `RenderTexture`:

| File | View |
|---|---|
| `Deliverables/Screenshots/Issue012_Bedroom_Overview.png` | Fixed overview camera; bed, dining table, study desk, wardrobe, windows and toys |
| `Deliverables/Screenshots/Issue012_Bedroom_DoorAndWardrobe.png` | Entrance door, wardrobe and left wall |
| `Deliverables/Screenshots/Issue012_Bedroom_ActivityStations.png` | The three activity locations from the room centre |

The first capture attempt produced a nearly flat Overview frame because URP had
not finished warming its shaders/materials after the batchmode scene load. A
throwaway warm-up render was added to the capture method and all three PNGs were
regenerated. Pixel sanity checks on the final files show normal mid-tone
luminance, no near-black frames, no magenta missing-material pixels and varied
colour content in all three images.

**Visual limitation:** this agent's model cannot display image input, so no AI
visual inspection of the PNGs was possible. The project owner should open the
three files in an image viewer as the final visual acceptance step. This is an
environment limitation, not a claimed visual pass.

---

## 6. Evidence files

| File | Note |
|---|---|
| `Deliverables/Reports/Issue012_Unity_TestResults.xml` | Final EditMode runner output |
| `Deliverables/Reports/Issue012_PlayMode_TestResults.xml` | Final PlayMode runner output |
| `Deliverables/Screenshots/Issue012_Bedroom_*.png` | Three rendered views |
| `Documentation/Architecture/Issue012_Bedroom_Environment.md` | Layout, assets, lighting, navigation |

Builder logs were written outside the repository (`C:\Temp\Issue012_*.log`) so no
`Unity/Logs/` content was added by this Issue.

---

## 7. Tests NOT run / manual verification

| Check | Status | Manual step |
|---|---|---|
| MATLAB server and activity validation | NOT RUN | Out of scope; no MATLAB code changed. The existing explicit network test remains skipped. |
| Keyboard/mouse walkthrough with a player character | NOT RUN | No player controller exists yet (future Issue). Verify colliders and waypoints in the Editor instead. |
| Human visual review of screenshots and scene | NOT RUN by this agent | Open `Bedroom.unity` and the three PNGs; confirm colours, absence of clipping and clear paths. |
| MATLAB R2025b compatibility | NOT VERIFIED | No MATLAB code was added or changed in this Issue. |

---

## 8. Files created

| File | Purpose |
|---|---|
| `Unity/CleanMyRoom/Assets/Scripts/Editor/Issue012_BedroomBuilder.cs` (+ `.meta`) | Idempotent scene builder and screenshot capture |
| `Unity/CleanMyRoom/Assets/Materials/Bedroom/*.mat` (42 + metas) | URP materials |
| `Unity/CleanMyRoom/Assets/Models/MATLAB/Issue010/*` (OBJ, MTL + metas) | MATLAB #010 export copy |
| `Unity/CleanMyRoom/Assets/Models/MATLAB/Issue007/*` (3 OBJs + metas) | MATLAB #007 export copies |
| `Unity/CleanMyRoom/Assets/Prefabs/Bedroom/PF_Chair.prefab` (+ `.meta`) | Reused seating |
| `Unity/CleanMyRoom/Assets/Prefabs/Bedroom/PF_WallPicture.prefab` (+ `.meta`) | Reused framed art |
| `Unity/CleanMyRoom/Assets/Prefabs/Bedroom/PF_MatlabToyBlock.prefab` (+ `.meta`) | Reused MATLAB toy block |
| `Assets/Tests/Editor/BedroomEnvironment/*` (asmdef, tests + metas) | 6 EditMode tests |
| `Assets/Tests/PlayMode/BedroomEnvironment/*` (asmdef, tests + metas) | 2 PlayMode tests |
| Documentation and evidence files listed in §6 | Architecture, testing, XML and PNGs |

## 9. Files modified

| File | Change |
|---|---|
| `Unity/CleanMyRoom/Assets/Scenes/Bedroom.unity` | Bedroom environment built into the existing scene; identity, camera and light names preserved |
| `Unity/CleanMyRoom/CleanMyRoom.slnx` | Unity regenerated it to include the two new test assemblies |

Three `ProjectSettings/*.asset` files were rewritten by Unity with LF endings
during batch runs and were normalised back to CRLF; `git diff` reports no content
change for them.

**No file belonging to Issues #007, #008, #009, #010 or #011 was edited.**
Their tests were re-run and still pass. No staging, commit, push, merge, branch
or history operation was performed. The pre-existing untracked `Unity/Logs/`
directory was left untouched.

---

## 10. Limitations and unresolved items

1. No player character, movement controller or interactive activity exists yet;
   navigation is validated by colliders and clear waypoints, not by play.
2. Materials are solid URP Lit colours; the repository has no texture assets.
3. The entrance leaf is static; no door-opening animation is included.
4. `MATLAB_Cube.obj` is imported and verified but not instantiated; the other
   four MATLAB objects are placed in the room.
5. The front wall has an architectural opening rather than a full solid panel,
   so the camera can see into the room from its fixed viewpoint.
6. Human visual acceptance of the three PNGs is still required (see §7).
7. Issue closure awaits owner review and approval; no Issue was closed.
