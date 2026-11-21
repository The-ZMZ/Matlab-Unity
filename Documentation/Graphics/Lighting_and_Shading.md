# Lighting and Shading

**Issue #009 — implementation reference**

| Item | Value |
|---|---|
| Scope | MATLAB normal computation, lighting equations and shading algorithms (no Unity changes). |
| Package | `cmr.graphics` |
| Classes | `SurfaceNormals.m`, `Light3D.m`, `Material3D.m`, `LightingModel.m`, `Shading3D.m` |
| Location | `MATLAB/Graphics/+cmr/+graphics/` |
| Demo | `MATLAB/Graphics/demoLighting.m` |
| Tests | `MATLAB/Tests/tLighting.m` (29 tests) |
| Evidence | `Deliverables/Screenshots/Issue009_Normals.png`, `Issue009_Lambertian_vs_Phong.png`, `Issue009_Lighting_Components.png` |
| Test report | `Documentation/Testing/Issue009_Test_Report.md` |
| Base branch | `feature/issue-008-transformations` (Issue #007 and #008 files are unchanged). |

---

## 1. Purpose and scope

`AGENTS.md` §3 and §7 assign lighting and shading calculations to MATLAB. This
module completes the graphics chain that started with the Issue #007 meshes and
Issue #008 transformations:

```
Mesh3D (Issue #007)
      |
      +--> SurfaceNormals  -> face centres, face areas, unit face/vertex normals
                                  |
Light3D  ---> direction + distance attenuation ---+
Material3D -> ambient / diffuse / specular / shininess
                                                   |
                                          LightingModel.illuminate
                                                   |
                                  +----------------+------------------+
                                  |                                   |
                         Shading3D.lambertian                Shading3D.phong
                         one colour per triangle             one colour per vertex
                                  |                                   |
                               patch (FaceColor flat)          patch (FaceColor interp)
```

All colour values shown in the evidence figures are computed by these classes.
MATLAB's `camlight` and `lighting` commands are **not** used for the evidence;
the rendered patches have `FaceLighting = 'none'`.

No Issue #007 or Issue #008 source file is modified. `MATLAB/Tests/runAllTests.m`
is unchanged because it discovers the new test class automatically.

---

## 2. Geometry: normal fields (`SurfaceNormals`)

Meshes use the Issue #007 convention: vertices are `N×3` rows, faces are a
1×`F` cell array of counter-clockwise index lists as seen from outside.

### 2.1 Face normals and areas

`Mesh3D.faceNormals` uses Newell's method. For face vertices `p_i`:

```
Nx = sum( (y_i - y_{i+1})(z_i + z_{i+1}) )
Ny = sum( (z_i - z_{i+1})(x_i + x_{i+1}) )
Nz = sum( (x_i - x_{i+1})(y_i + y_{i+1}) )
n  = [Nx Ny Nz] / |[Nx Ny Nz]|
```

`SurfaceNormals.faceAreas` uses `|Newell vector| / 2`, which is exact for planar
faces and a consistent weight for the slightly non-planar sphere quads.
`faceCenters` returns the arithmetic mean of each face's vertices.

### 2.2 Area-weighted vertex normals

```
N(v) = normalize( sum_{faces f incident to v} area(f) * n(f) )
```

Large faces therefore contribute more than small ones. On a cube all incident
face areas are equal, so each corner normal is `sign(vertex)/sqrt(3)`.

### 2.3 Outward orientation

`orientOutwards` compares each normal with the radial vector from the mesh
vertex mean to the sample point (face centre for face normals, vertex position
for vertex normals) and flips the normal when the dot product is negative:

```
r = normalize(samplePoint - mean(vertices))
if dot(n, r) < 0, n = -n
```

This test is valid for the closed, star-shaped primitives used here (cube,
sphere, cylinder). `outwardFaceNormals`, `outwardVertexNormals`, `isOutward`
and `visualize` build on this step. `visualize` draws the mesh translucently and
quiver arrows for either normal type.

---

## 3. Light and material

### 3.1 Point light (`Light3D`)

A light stores a finite world position `p_L`, RGB colour `c_L`, intensity `I`
and ambient intensity `a`. For a surface point `p`:

```
L(p)   = normalize(p_L - p)                 direction toward the light
d(p)   = |p_L - p|
atten  = 1 / (1 + 0.05 * d^2)               smooth inverse-square falloff
ambient light colour = c_L .* (I * a)
```

A point coincident with the light is rejected because its direction is
undefined. The attenuation denominator keeps the value finite and equal to 1 at
the light position.

### 3.2 Phong material (`Material3D`)

| Property | Meaning | Default |
|---|---|---|
| `ambient` | ambient RGB reflectance | `[0.05 0.05 0.05]` |
| `diffuse` | Lambertian RGB reflectance | `[0.60 0.60 0.60]` |
| `specular` | specular RGB reflectance | `[0.10 0.10 0.10]` |
| `shininess` | Phong exponent `s >= 1` | `16` |

`fromColor(name, rgb, 'Shininess', s, 'SpecularScale', k)` is the convenience
constructor: `ambient = 0.15*rgb`, `diffuse = rgb`, `specular = k*[1 1 1]`.
Larger `shininess` produces a smaller, tighter highlight.

---

## 4. Lighting equations (`LightingModel`)

For each surface point with unit normal `N`, light direction `L`, viewer
direction `V = normalize(p_V - p)` and light term
`C = c_L * I * atten(d)`:

```
ambient  = material.ambient  .* (c_L * I * a)
diffuse  = material.diffuse  .* C .* max(dot(N,L), 0)
R        = 2 * dot(N,L) * N - L
specular = material.specular .* C .* (max(dot(R,V), 0)^s)   when dot(N,L) > 0
specular = 0                                                 when dot(N,L) <= 0
total    = ambient + diffuse + specular
```

`illuminate` returns all three components plus their unclipped sum. Input
normals are normalized internally; a zero-length normal is rejected. The viewer
may be one `1×3` position or one position per surface point.

---

## 5. Shading algorithms (`Shading3D`)

### 5.1 Flat Lambertian shading

1. Triangulate the polygon mesh with `Mesh3D.triangulate`, carrying a
   `sourceFace` map back to the original polygon.
2. Use the outward face normal of the source polygon and the triangle centre.
3. Evaluate ambient + diffuse once per output triangle.
4. Paint every triangle of a polygon with the same colour
   (`patch` with `FaceColor = 'flat'`). The specular component is explicitly
   zero for this method, matching the ambient+diffuse contract.

### 5.2 Smooth Phong shading

1. Triangulate the mesh.
2. Evaluate ambient + diffuse + specular at every vertex using the
   area-weighted outward vertex normal.
3. Interpolate vertex colours across each triangle
   (`patch` with `FaceColor = 'interp'` and `FaceVertexCData`).
4. Provide the mean of the three vertex colours as `faceColors` for numerical
   comparison with the flat method.

### 5.3 Result structure and clipping

Both methods return a scalar struct containing `method`, `colorLocation`,
`meshName`, `vertices`, `faces`, `sourceFace`, `normals`, `faceCenters`,
`faceColors`, `vertexColors`, `components` and a `brightestFace` /
`brightestVertex` index. Colour channels are clipped to `[0,1]` for display;
the raw lighting terms remain available in `result.components`.

`shade(mesh, light, material, viewer, method)` dispatches by name
(`lambertian`/`lambert`/`flat` or `phong`/`smooth`). `render` draws a result
into the supplied axes (or the current axes) and sets an equal-aspect 3D view.

---

## 6. Demo and evidence

Run from the repository root:

```matlab
addpath(genpath('MATLAB'));
outputs = demoLighting();
```

`demoLighting` writes three figures to `Deliverables/Screenshots/` and closes
each figure immediately after export:

| Figure | Content |
|---|---|
| `Issue009_Normals.png` | Face normals (top row) and area-weighted vertex normals (bottom row) for cube, sphere and cylinder. |
| `Issue009_Lambertian_vs_Phong.png` | Same light, viewer and per-object materials; flat Lambertian above, smooth Phong below. |
| `Issue009_Lighting_Components.png` | Isolated ambient, diffuse and specular terms, their sum, and low vs high shininess highlights. |

The demo prints a verification summary (light configuration, mean component
values, brightest vertex and saved file names). An optional output directory
can be passed: `demoLighting("C:/tmp/lighting")`.

---

## 7. Compatibility fixes made during Issue #009

Three defects were found by the new tests and corrected in place; no class was
recreated and no public API changed:

| Defect | Fix | File |
|---|---|---|
| `patch(...,'VertexColors',...)` — property does not exist | use `FaceVertexCData` | `Shading3D.render` |
| `patch(...,'Lighting','none')` — `Lighting` is an axes property | use `FaceLighting` | `Shading3D.render`, `SurfaceNormals.visualize` |
| Lambertian result inherited a non-zero specular term from `illuminate` | zero `components.specular` and recompute `total` for flat shading | `Shading3D.lambertian` |

---

## 8. Limitations

- One point light, no shadows, no gamma correction and no multiple-light
  accumulation. The equations are intentionally explicit and base-MATLAB only.
- The centre-based outward test assumes convex, star-shaped meshes. It is used
  only for the cube, sphere and cylinder produced by Issues #007/#008.
- Flat shading is per output triangle, so a non-planar sphere quad can show a
  subtle seam between its two triangles. This is expected for a flat method.
- The environment MATLAB is R2026b; R2025b compatibility is documented as
  **NOT VERIFIED** in the test report.
