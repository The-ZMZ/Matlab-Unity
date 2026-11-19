# 3D Projection and Geometry Export

**Issue #010 — implementation reference**

| Item | Value |
|---|---|
| Scope | MATLAB 3D projection matrices, camera-distance tests, comparison, and OBJ/MTL geometry export. |
| Package | `cmr.graphics` |
| Classes | `MATLAB/Graphics/+cmr/+graphics/Projection3D.m`, `GeometryExporter.m` |
| Demo | `MATLAB/Graphics/demoProjectionExport.m` |
| Tests | `MATLAB/Tests/tProjection.m` (21), `MATLAB/Tests/tGeometryExporter.m` (16) |
| Unity import test | `Unity/CleanMyRoom/Assets/Tests/Editor/MeshImportTests.cs` (5 tests, executed) |
| Evidence | `Deliverables/Screenshots/Issue010_Orthographic_vs_Perspective.png`, `Issue010_Distance_Comparison.png`, `Issue010_OBJ_Export.png` |
| Exports | `Shared_Data/Meshes/Issue010/*.obj`, `*.mtl`, `Export_Manifest.txt` |
| Import evidence | `Deliverables/Reports/Issue010_Unity_Import_Result.txt`, `Issue010_Unity_TestResults.xml` |
| Test report | `Documentation/Testing/Issue010_Test_Report.md` |
| Base branch | `feature/issue-010-projection-export` (Issue #007/#008/#009 files unchanged). |

---

## 1. Purpose and scope

`AGENTS.md` §3 and §7 assign projection calculations and geometry export to
MATLAB. This module completes the graphics chain:

```
Mesh3D (Issue #007)  ─┐
Transform3D (#008)   ─┼──> Projection3D ──> normalised device coordinates
Lighting/Material(#009)┘          │
                                  └──> GeometryExporter ──> OBJ + MTL ──> Unity
```

The module reuses the existing polygon meshes, transforms, normals, lights and
materials. **No file from Issues #007–#009 was modified.**

> **Camera clarification.** "Camera" in this Issue means a mathematical
> viewpoint (eye point, target and up vector) used to build projection
> matrices. No image capture, webcam, hand tracking or gesture input exists in
> this module or anywhere in this project (`AGENTS.md` §5).

---

## 2. Coordinate and multiplication conventions

All existing `cmr.graphics` code stores a point as a **row** `[x y z]` and uses
homogeneous **right** multiplication (`Transform3D`, Issue #008):

```matlab
[X Y Z W] = [x y z 1] * M;      % 4x4 matrix M
[xn yn zn] = [X/W Y/W Z/W];     % perspective divide
```

`Projection3D` follows that convention exactly, so view and projection matrices
can be combined as `M = V * P` and passed to `Transform3D.fromHomogeneous`.

**Camera space** after applying the view matrix:

- eye at `(0, 0, 0)`;
- `+x` points right;
- `+y` points up;
- `−z` points into the scene (a visible point has `z < 0`);
- up defaults to `[0 0 1]`, matching the Z-up meshes from Issue #007.

Points with `W <= 0` are rejected with
`cmr:graphics:Projection3D:pointBehindCamera` rather than being silently
mirrored.

---

## 3. Projection matrix definitions

### 3.1 View matrix

```matlab
V = cmr.graphics.Projection3D.viewMatrix(eye, target, up);
```

Given `f = normalise(target − eye)`, `r = normalise(f × up)` and `u = r × f`:

```
      [ rx  ux  -fx  0 ]
V  =  [ ry  uy  -fy  0 ]
      [ rz  uz  -fz  0 ]
      [-r·e -u·e  f·e 1 ]
```

This is the transpose of the column-vector look-at matrix. It places the eye at
the origin and the target on the negative camera Z axis. Coincident eye/target
or an up vector parallel to the view direction produces a clear error.

### 3.2 Orthographic projection

```matlab
P = cmr.graphics.Projection3D.orthographic(width, height, near, far);
```

```
      [ 2/width        0           0             0 ]
P  =  [    0        2/height       0             0 ]
      [    0           0      -2/(far-near)      0 ]
      [    0           0    -(far+near)/(far-near) 1 ]
```

Visible size is independent of distance. `near` and `far` are positive eye
distances with `far > near`.

### 3.3 Perspective projection

```matlab
P = cmr.graphics.Projection3D.perspective(fov, aspect, near, far);
P = cmr.graphics.Projection3D.perspective(pi/3, 1.6, 0.1, 100, "radians");
```

With vertical field of view `θ` and `q = 1/tan(θ/2)`:

```
      [ q/aspect   0           0                   0 ]
P  =  [    0       q           0                   0 ]
      [    0       0    -(far+near)/(far-near)    -1 ]
      [    0       0   -2*far*near/(far-near)      0 ]
```

`W` after projection equals the positive camera-space distance, so the
perspective divide produces the expected `1/distance` size falloff. The near
and far planes map to NDC `z = −1` and `z = +1`.

### 3.4 Pipeline helpers

| Function | Result |
|---|---|
| `clipCoordinates(points, M)` | N-by-4 `[X Y Z W]`, no divide (useful for teaching). |
| `projectPoints(points, M)` | N-by-3 NDC after the homogeneous divide. |
| `projectMesh(mesh, V, P)` | Struct with `vertices`, triangulated `faces`, `cameraSpace`, both matrices and `projectedExtent`. |
| `projectedExtent(points)` | Width, height and optional depth of an NDC point set. |
| `distance(eye, points)` | N-by-1 Euclidean distances. |
| `distanceToMesh(eye, mesh)` | Distance to the mesh's bounding-box centre. |
| `compare(orthoResult, perspResult)` | Extents of both projections and their width/height ratios; rejects different meshes or cameras. |

---

## 4. Measured distance behaviour

The demo projects a cube of side 2 from `[0 −d 0]` with a 60° vertical field of
view. The measured `distanceToMesh` matches `d`, orthographic width stays fixed
at 1.0000, and perspective width falls with distance:

| d (world units) | measured distance | orthographic width | perspective width |
|---:|---:|---:|---:|
| 4 | 4.00 | 1.0000 | 1.1547 |
| 8 | 8.00 | 1.0000 | 0.4949 |
| 12 | 12.00 | 1.0000 | 0.3149 |

The gallery comparison (cube, sphere, cylinder) shows the same effect:
perspective/orthographic width ratios of 0.941, 0.684 and 0.776 respectively.

---

## 5. OBJ / MTL export format

`cmr.graphics.GeometryExporter.export` writes a deliberately small, readable
subset of OBJ that Unity imports without a toolbox.

### 5.1 OBJ lines

```
# Clean My Room - Issue #010
# Generated by cmr.graphics.GeometryExporter
# Coordinate system: unity (x, y, z)
mtllib Issue010_Box_Unity.mtl
o cube
# 8 vertices, 12 triangles
v 0 0.5 0.5 0.2 0.55 0.9      <- x y z [r g b]
vn 0.789474 -0.315789 0.526316
usemtl CleanMyRoom_box
f 2//2 3//3 4//4              <- 1-based vertex//normal indices
```

- Faces are triangulated with `Mesh3D.triangulate` (fan triangulation).
- Indices are **1-based**, as required by OBJ.
- Vertex colours are written after each `v` as RGB in `[0, 1]`.
- One outward vertex normal (`vn`) is written per vertex using
  `SurfaceNormals.outwardVertexNormals`.
- Colours, normals and material lines can each be disabled with
  `WriteColors`, `WriteNormals` and `WriteMaterial`.

### 5.2 MTL lines

```
newmtl CleanMyRoom_box
Ka 0.03 0.0825 0.135
Kd 0.2 0.55 0.9
Ks 0.55 0.55 0.55
Ns 48
d 1
illum 2
```

`Ka` (ambient), `Kd` (diffuse), `Ks` (specular), `Ns` (Phong shininess) come
directly from the existing `cmr.graphics.Material3D`. `d` (opacity) and
`illum 2` (Phong) are written as fixed supported values. If no material object
is supplied, one is derived from `mesh.color`. Material and object names are
sanitised to letters, digits, `_`, `-` and `.`.

### 5.3 Verification reader

`GeometryExporter.readOBJ(path)` reads the subset written above (and the plain
`v`/`f` files produced by Issue #007's `Mesh3D.exportOBJ`). It returns
`objectName`, `materialName`, `materialFile`, `vertices`, `vertexColors`,
`normals` and `faces`, and is used by the tests and the demo to verify exports
without any toolbox.

---

## 6. Unity coordinate convention

MATLAB meshes are Z-up right-handed. Unity is Y-up left-handed. Two variants of
every demo export are provided:

| Variant | Mapping | Bounds of the demo box | Purpose |
|---|---|---|---|
| `Issue010_Box_MATLAB.obj` | none (Z-up) | `[2 3 5]` | Fidelity check / original convention. |
| `Issue010_Box_Unity.obj` | `(x,y,z) → (x,z,y)` | `[2 5 3]` | Direct import into Unity. |

The remap swaps Y and Z, so MATLAB's `+z` becomes Unity's up axis and MATLAB's
`+y` becomes Unity's forward axis. A swap is a **reflection**, therefore every
face winding is reversed (`fliplr`) to keep normals pointing outwards. The
mesh centre `[1 2 3]` becomes `[1 3 2]`.

### 6.1 Observed Unity importer behaviour

A real import into Unity `6000.6.5f1` was performed (see the test report). The
results are recorded in `Deliverables/Reports/Issue010_Unity_Import_Result.txt`:

- the Unity-variant file imports with size `[2 5 3]`; the raw MATLAB file
  imports with size `[2 3 5]` — the remap is required for correct height;
- Unity's OBJ importer **mirrors X** to convert right-handed OBJ data into its
  left-handed space: file `x ∈ [0, 2]` imports as `x ∈ [−2, 0]`, so the imported
  centre is `[−1 3 2]`. The size is unchanged and this is expected;
- normals remain outward after the mirror (minimum outward dot product
  `0.768417`);
- the `CleanMyRoom_box` material is imported. MTL `Kd` is treated as linear
  colour, so `Kd 0.2 0.55 0.9` appears in `Material.color` as approximately
  `[0.4845 0.7674 0.9547]` after the linear→sRGB conversion.

The Unity EditMode test `MeshImportTests.cs` asserts all of these values.

---

## 7. Usage

### 7.1 MATLAB

```matlab
addpath(genpath('MATLAB'));

cube = cmr.graphics.Cube3D.create(2, [0 0 0], [0.2 0.55 0.9]);
V = cmr.graphics.Projection3D.viewMatrix([0 -8 0], [0 0 0], [0 0 1]);
Po = cmr.graphics.Projection3D.orthographic(4, 4, 0.1, 100);
Pp = cmr.graphics.Projection3D.perspective(60, 1, 0.1, 100);

ortho = cmr.graphics.Projection3D.projectMesh(cube, V, Po);
persp = cmr.graphics.Projection3D.projectMesh(cube, V, Pp);
comparison = cmr.graphics.Projection3D.compare(ortho, persp);

material = cmr.graphics.Material3D.fromColor('Box paint', cube.color);
exported = cmr.graphics.GeometryExporter.export( ...
    cube, 'Shared_Data/Meshes/box.obj', ...
    'Material', material, 'CoordinateSystem', 'unity');
```

### 7.2 Demo

```matlab
outputs = demoProjectionExport();       % default Deliverables and Shared_Data
outputs = demoProjectionExport("myScreenshots", "myMeshes");
```

### 7.3 Unity import test

Copy the files from `Shared_Data/Meshes/Issue010` next to the project (the test
resolves the repository root from `Application.dataPath`) and run:

```
Unity.exe -runTests -batchmode -projectPath Unity\CleanMyRoom ^
  -testPlatform EditMode -testFilter CleanMyRoom.Tests.MeshImportTests ^
  -testResults Deliverables\Reports\Issue010_Unity_TestResults.xml
```

The test copies the OBJ/MTL files into a temporary `Assets/Issue010ImportTest`
folder, imports them with `AssetDatabase.ImportAsset`, and removes the folder
in `TearDown`.

---

## 8. Limitations

- MATLAB R2025b is the project target, but only R2026b was installed here, so
  R2025b compatibility is **NOT VERIFIED**.
- The MTL subset is intentionally limited to ambient, diffuse, specular,
  shininess, opacity and illumination model. Textures and normal maps are not
  exported.
- OBJ files use one material per object and vertex colours copied from the
  mesh; per-face colour palettes are not supported.
- `GeometryExporter.readOBJ` reads only the subset this project writes. It is
  a verification helper, not a general-purpose OBJ parser.
- Unity's importer may change its axis handling between editor versions; the
  Unity test pins the behaviour observed on `6000.6.5f1`.
