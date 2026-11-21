# 3D Geometric Transformations

**Issue #008 — implementation reference**

| Item | Value |
|---|---|
| Scope | MATLAB 3D affine transformations and visualisation (no Unity changes). |
| Package | `cmr.graphics` |
| Class | `MATLAB/Graphics/+cmr/+graphics/Transform3D.m` |
| Demo | `MATLAB/Graphics/demo3DTransformations.m` |
| Tests | `MATLAB/Tests/tTransform3D.m` (38 tests) |
| Evidence | `Deliverables/Screenshots/Issue008_Basic_Transformations.png`, `Issue008_Transformation_Order.png`, `Issue008_Transformed_Objects.png` |
| Test report | `Documentation/Testing/Issue008_Test_Report.md` |
| Base branch | `feature/issue-007-graphics` (Issue #007 PR #16 is open and unmerged). |

---

## 1. Purpose and scope

`AGENTS.md` §3 and §7 assign 3D transformations to MATLAB. This module adds the
Issue #008 algorithms on top of the Issue #007 polygon-mesh classes:

- 3D translation;
- rotation around the X, Y and Z axes;
- 3D scaling, including uniform and non-uniform factors;
- 4×4 homogeneous transformation matrices;
- composition of multiple transformations;
- inverse transformations;
- transformation-order verification;
- visual comparison of original and transformed cube, sphere and cylinder meshes.

No Issue #006 or Issue #007 file is modified. `MATLAB/Tests/runAllTests.m` is
unchanged because it discovers the new test class automatically.

---

## 2. Coordinate and matrix convention

The class uses **homogeneous row vectors**. A point `(x, y, z)` is written as
`[x y z 1]`, and a transformation matrix `T` is applied by right multiplication:

```
[X Y Z W] = [x y z 1] * T
x' = X / W,  y' = Y / W,  z' = Z / W
```

All constructors return affine matrices whose bottom row is `[0 0 0 1]`, so
`W = 1` for ordinary points. `transformPoints` still performs the homogeneous
division, so a caller may also pass a general 4×4 matrix with a non-canonical
bottom row.

### 2.1 Translation

For offset `d = (dx, dy, dz)`:

```
T(d) = [ 1  0  0  0 ]
       [ 0  1  0  0 ]
       [ 0  0  1  0 ]
       [ dx dy dz 1 ]
```

The offset is in the **last row** because points are row vectors. With the more
common column-vector convention it would be in the last column; both forms
describe the same geometric translation.

### 2.2 Rotation

With `c = cos(theta)` and `s = sin(theta)`, the right-handed rotations are:

```
Rx(theta) = [ 1  0  0 0 ]   Ry(theta) = [  c 0 -s 0 ]
            [ 0  c  s 0 ]               [  0 1  0 0 ]
            [ 0 -s  c 0 ]               [  s 0  c 0 ]
            [ 0  0  0 1 ]               [  0 0  0 1 ]

Rz(theta) = [ c  s 0 0 ]
            [-s  c 0 0 ]
            [ 0  0 1 0 ]
            [ 0  0 0 1 ]
```

Positive angles follow the right-hand rule:

- `Rx`: `+y` turns toward `+z`;
- `Ry`: `+z` turns toward `+x`;
- `Rz`: `+x` turns toward `+y`.

Angles are radians by default. Pass `"degrees"`, `"degree"` or `"deg"` as the
second argument to use degrees.

### 2.3 Scaling

```
S(sx, sy, sz) = diag(sx, sy, sz, 1)
```

Negative factors are accepted and produce a reflection. Zero factors are
rejected because they make the matrix singular and collapse the object.

---

## 3. Combining transformations

`compose(T1, T2, ..., Tn)` returns the matrix product `T1*T2*...*Tn`. With row
vectors this means **T1 is applied first**, then T2, and so on. This is the
opposite reading order from nested column-vector notation, so the order must be
chosen deliberately.

Example: scale by `S`, then rotate by `R`, then translate by `T`:

```matlab
M = cmr.graphics.Transform3D.compose(S, R, T);
```

`combine` is an alias for `compose`. A cell array is also accepted:

```matlab
M = cmr.graphics.Transform3D.compose({S, R, T});
```

### 3.1 Demonstrated order difference

Using `T = translation(1.5, 0, 0)` and `S = scaling(2, 1, 1)`:

| Composition | Matrix | Applied order | `[1 0 0]` becomes |
|---|---|---|---|
| `compose(T, S)` | `T*S` | translate, then scale | `[5 0 0]` |
| `compose(S, T)` | `S*T` | scale, then translate | `[3.5 0 0]` |

The results differ, which is asserted by `tTransform3D` and shown in
`Issue008_Transformation_Order.png`.

---

## 4. API reference

All methods are static; `cmr.graphics.Transform3D` is never instantiated.

| Method | Returns | Notes |
|---|---|---|
| `identity()` | `4×4` identity | affine; leaves points unchanged |
| `translation(dx,dy,dz)` / `translation([dx dy dz])` | translation matrix | finite components required |
| `rotationX(angle, unit)` | rotation about X | radians by default; `unit` optional |
| `rotationY(angle, unit)` | rotation about Y | radians by default; `unit` optional |
| `rotationZ(angle, unit)` | rotation about Z | radians by default; `unit` optional |
| `scaling(s)` / `scaling(sx,sy,sz)` / `scaling([sx sy sz])` | scaling matrix | scalar form is uniform; zero rejected |
| `compose(T1,...,Tn)` | combined matrix | `T1*T2*...*Tn`; first argument applied first |
| `combine(T1,...,Tn)` | combined matrix | alias for `compose` |
| `inverse(T)` | inverse matrix | rejects singular matrices using `rcond` |
| `toHomogeneous(points)` | `N×4` | appends a column of ones |
| `fromHomogeneous(H)` | `N×3` | divides by `w`; rejects zero/non-finite `w` |
| `transformPoints(T, points)` | `N×3` | applies `T` to finite `N×3` points |
| `transformMesh(T, mesh)` | mesh struct | transformed Issue #007 mesh; input unchanged |
| `isAffine(T, tolerance)` | logical | checks bottom row against `[0 0 0 1]` |

### 4.1 Error identifiers

| Identifier | Cause |
|---|---|
| `cmr:graphics:Transform3D:invalidMatrix` | matrix is not a finite real 4×4 array |
| `cmr:graphics:Transform3D:invalidPoints` | points are not a finite real N×3 array |
| `cmr:graphics:Transform3D:invalidHomogeneousPoints` | homogeneous input is not finite real N×4 |
| `cmr:graphics:Transform3D:zeroHomogeneousCoordinate` | any `w` is zero |
| `cmr:graphics:Transform3D:nonFiniteCoordinate` | division produced `Inf`/`NaN` |
| `cmr:graphics:Transform3D:invalidTranslation` | bad translation argument form or value |
| `cmr:graphics:Transform3D:invalidAngle` | non-scalar, non-real or non-finite angle |
| `cmr:graphics:Transform3D:invalidAngleUnit` | unsupported angle unit |
| `cmr:graphics:Transform3D:invalidScale` | bad scaling form, non-finite or zero factor |
| `cmr:graphics:Transform3D:invalidSequence` | `compose` called without matrices |
| `cmr:graphics:Transform3D:singularMatrix` | `inverse` called on a singular matrix |
| `cmr:graphics:Transform3D:invalidTolerance` | bad `isAffine` tolerance |
| `cmr:graphics:Transform3D:invalidMesh` | `transformMesh` called with a malformed mesh |

---

## 5. Applying transformations to Issue #007 meshes

```matlab
cube = cmr.graphics.Cube3D.create(2, [0 0 0], [0.20 0.52 0.90]);

T = cmr.graphics.Transform3D.compose( ...
    cmr.graphics.Transform3D.scaling(1, 1.2, 1), ...
    cmr.graphics.Transform3D.rotationZ(35, 'degrees'), ...
    cmr.graphics.Transform3D.translation(2, 0, 0));

moved = cmr.graphics.Transform3D.transformMesh(T, cube);
```

`transformMesh`:

- transforms every vertex with `transformPoints`;
- preserves `name`, faces, colour, counts and the parameter struct;
- transforms a stored `parameters.center` as a point, so a translated or
  rotated mesh keeps a consistent centre;
- leaves construction parameters such as `sideLength`, `radius` and `height`
  unchanged because they describe how the mesh was originally built;
- never modifies the input struct (MATLAB value semantics plus validation).

The mesh still satisfies `cmr.graphics.Mesh3D` validation, so `triangulate`,
`faceNormals`, `bounds` and `exportOBJ` continue to work.

---

## 6. Demonstration and evidence

Run:

```matlab
cd('D:/Matlab-Unity/MATLAB/Graphics')
demo3DTransformations
```

or use an alternate output folder:

```matlab
outputs = demo3DTransformations('C:/temp/issue008');
```

The demo resolves the project root from `mfilename('fullpath')`, adds the MATLAB
paths, prints a verification summary and writes three PNG files. Figure windows
are visible in the desktop and hidden under `matlab -batch`.

### 6.1 Figure 1 — basic transformations

`Issue008_Basic_Transformations.png` is a 2×3 tiled comparison of one cube:

1. translation;
2. rotation around X;
3. rotation around Y;
4. rotation around Z;
5. non-uniform scaling;
6. a combined scale → rotate → translate sequence.

Each panel shows the original cube as a translucent grey wireframe and the
transformed cube as a solid coloured mesh, with fixed axes, grid, labels,
lighting and a legend.

### 6.2 Figure 2 — transformation order

`Issue008_Transformation_Order.png` compares `T*S` (translate, then scale) with
`S*T` (scale, then translate). The translated cube ends in a different position
and the on-panel coordinates show `[1 0 0]` mapping to `[5 0 0]` versus
`[3.5 0 0]`.

### 6.3 Figure 3 — transformed primitives

`Issue008_Transformed_Objects.png` shows the original and transformed cube,
sphere and cylinder from Issue #007. All three use the same matrix:
uniform scale → rotate Y → rotate X → translate.

### 6.4 Evidence summary

| File | Pixels | Bytes | Non-white pixels |
|---|---:|---:|---:|
| `Issue008_Basic_Transformations.png` | 1994 × 1284 | 186 197 | 9.80 % |
| `Issue008_Transformation_Order.png` | 1537 × 739 | 77 871 | 9.05 % |
| `Issue008_Transformed_Objects.png` | 1692 × 865 | 104 934 | 7.65 % |

All panels contain rendered content; see the test report for per-panel values
and colour-mask counts.

---

## 7. Acceptance-criteria traceability

| Issue #008 task | Where it is satisfied |
|---|---|
| 3D Translation | `Transform3D.translation`; `translationMovesEveryPointByTheSameOffset`; demo figure 1 |
| Rotation around X | `Transform3D.rotationX`; quarter-turn, right-hand-rule and orthonormality tests |
| Rotation around Y | `Transform3D.rotationY`; quarter-turn, right-hand-rule and orthonormality tests |
| Rotation around Z | `Transform3D.rotationZ`; quarter-turn, right-hand-rule and orthonormality tests |
| 3D Scaling | `Transform3D.scaling`; uniform, non-uniform and reflection tests |
| Build 4×4 matrices | Every constructor and `identity`; matrix-shape and affine-bottom-row tests |
| Combine transformations | `compose` / `combine`; product, cell-array and alias tests |
| Test transformation order | `composeAppliesTransformationsInArgumentOrder`, `rotationAndTranslationOrderChangesResult`; demo figure 2 |
| Visualise original and transformed objects | `demo3DTransformations`; three PNG evidence files |
| Comprehensive unit tests | 38 new tests in `tTransform3D`; full suite 132/132 passed |
| Reuse Issue #007 objects | `transformMesh` operates directly on `Cube3D`, `Sphere3D` and `Cylinder3D` mesh structs |

---

## 8. Limitations and notes

- **MATLAB R2025b is NOT installed on this machine.** All execution used
  R2026b (26.2.0.3386108). R2025b compatibility is **NOT VERIFIED**. Only base
  MATLAB functions are used (`eye`, `diag`, `sin`, `cos`, `inv`, `rcond`,
  `patch`, `tiledlayout`, `exportgraphics`); `tiledlayout` and `exportgraphics`
  are available in R2025b, so no incompatibility is expected.
- **Row-vector convention.** The translation offset is placed in the last row
  and rotation matrices are transposed relative to column-vector textbooks.
  This is documented above and tested explicitly.
- **General 4×4 matrices.** `transformPoints` divides by `w`, so non-affine
  matrices can be applied. The constructors themselves create affine matrices.
- **Construction metadata.** `transformMesh` updates `parameters.center` but not
  shape parameters such as radius, side length or volume, which remain the
  original construction values.
- **No projection or full lighting model.** The demo uses MATLAB's camera and a
  headlight for readable evidence; projection and lighting calculations belong
  to later issues.
- **No Unity or protocol changes.** This is a MATLAB-only graphics issue.
  PR base is `feature/issue-007-graphics` until Issue #007 PR #16 is merged.
