# 2D Geometric Transformations and Cohen–Sutherland Line Clipping

**Issue #006 — implementation reference**

| Item | Value |
|---|---|
| Scope | 2D only (MATLAB). 3D transformations belong to Issue #008. |
| Package | `cmr.graphics` |
| Files | `MATLAB/Graphics/+cmr/+graphics/Transform2D.m`, `MATLAB/Graphics/+cmr/+graphics/CohenSutherland.m` |
| Demos | `MATLAB/Graphics/demo2DTransformations.m`, `MATLAB/Graphics/demoCohenSutherland.m` |
| Tests | `MATLAB/Tests/tTransform2D.m` (24), `MATLAB/Tests/tCohenSutherland.m` (28) |
| Evidence | `Deliverables/Screenshots/Issue006_2D_Transformations.png`, `Issue006_Composite_Order.png`, `Issue006_Cohen_Sutherland_Clipping.png` |
| Test report | `Documentation/Testing/Issue006_Test_Report.md` |

---

## 1. Purpose and scope

This module provides the 2D computer-graphics layer required by `AGENTS.md` §7:

- 2D geometric transformations — translation, rotation, scaling, reflection, shearing, and their composition — expressed as **3×3 homogeneous matrices**;
- **Cohen–Sutherland line clipping** implemented manually (region codes, trivial acceptance, trivial rejection, edge intersection), with no toolbox clipping function.

In scope:

- transformation matrices and their application to points;
- composition, including order of application and pivot-relative transformations;
- clipping a single segment and clipping a polyline against an axis-aligned rectangular window;
- unit tests, standalone demonstration scripts, and figure evidence.

Out of scope (deliberately):

- 3D transformations, 4×4 matrices, projection, lighting — Issues #007–#010;
- rasterisation (DDA/Bresenham/midpoint) — Issue #005;
- the game's normalized canvas coordinates and the MATLAB↔Unity wire protocol — Issue #004 (untouched);
- any Unity file, any communication, game-logic or activity file.

The module is additive: no existing file in the repository was modified.

---

## 2. Coordinate conventions

### 2.1 Points and homogeneous matrices

A 2D point is a row vector `[x y]`. Internally the library lifts it to a homogeneous **column** vector and multiplies on the left:

```
p = [x; y; 1]            p' = M · p
```

For `N` points the implementation stacks them into a 3×N matrix and transforms all of them in one matrix product:

```
H  = [P, ones(N,1)]'          % 3×N homogeneous coordinates
H' = M * H                    % one multiply for all points
Q  = H'(1:2, :)'              % back to N×2 Cartesian points
```

Because the last row of every matrix is `[0 0 1]`, the (constant) third coordinate needs no perspective division. The library still validates that invariant: `apply` rejects any matrix whose last row differs from `[0 0 1]`, which catches accidental use of a 4×4 or a projective matrix.

### 2.2 Axis convention

The module uses the **standard mathematical 2D coordinate system**:

- origin at the bottom-left of the plot,
- **x to the right, y upwards**,
- angles measured **counter-clockwise**, positive rotation turns `+x` toward `+y`.

This is a deliberate, documented decision. The game canvas in `Documentation/Communication/Protocol.md` uses a *different*, screen-style convention (origin top-left, y downwards) for normalised activity coordinates. That convention belongs to the protocol layer and is **not** changed or reinterpreted here; the two systems never meet in this issue.

Consequence for clipping: "BOTTOM" means `y < ymin` and "TOP" means `y > ymax`.

### 2.3 Composition order

```
compose(A, B, C)  ≡  C · B · A          % A is applied first
```

so that

```
apply(compose(A,B,C), P)  ==  apply(C, apply(B, apply(A, P)))
```

`compose()` with no arguments returns the identity matrix.

---

## 3. Transformation matrices

All matrices are 3×3 and affine.

### 3.1 Identity

```
I = [ 1  0  0 ]
    [ 0  1  0 ]
    [ 0  0  1 ]
```

`x' = x`, `y' = y`. `det(I) = 1`.

### 3.2 Translation

```
T(tx, ty) = [ 1  0  tx ]
            [ 0  1  ty ]
            [ 0  0  1  ]
```

```
x' = x + tx
y' = y + ty
```

`det(T) = 1`. Translation cannot be expressed as a 2×2 matrix; this is the reason homogeneous coordinates are used at all.

### 3.3 Rotation

```
R(θ) = [ cos θ  −sin θ  0 ]
       [ sin θ   cos θ  0 ]
       [   0       0    1 ]
```

```
x' = x cos θ − y sin θ
y' = x sin θ + y cos θ
```

`det(R) = cos²θ + sin²θ = 1`, and the upper-left 2×2 block is orthonormal (`Rᵀ R = I`), so rotation preserves lengths and angles. The angle is in **radians**; `rotationDegrees(deg)` converts degrees with `deg2rad` and delegates to `rotation`.

Derivation: rotating the basis vectors gives `e₁ → (cos θ, sin θ)` and `e₂ → (−sin θ, cos θ)`, and those images are exactly the columns of the 2×2 block.

### 3.4 Scaling

```
S(sx, sy) = [ sx  0   0 ]
            [ 0   sy  0 ]
            [ 0   0   1 ]
```

```
x' = sx · x
y' = sy · y
```

`det(S) = sx · sy`. Scaling is uniform when `sx = sy`. Negative factors are permitted (they turn the scaling into a reflection composed with a scale); non-finite factors are rejected. The pivot for scaling is the origin unless `aboutPoint` is used.

### 3.5 Reflection

Every reflection has `det = −1`, which is the algebraic statement that it reverses orientation.

| Reflection | Method | Matrix |
|---|---|---|
| about the x-axis (`y = 0`) | `reflectionX` | `[1 0 0; 0 −1 0; 0 0 1]` |
| about the y-axis (`x = 0`) | `reflectionY` | `[−1 0 0; 0 1 0; 0 0 1]` |
| about the origin | `reflectionOrigin` | `[−1 0 0; 0 −1 0; 0 0 1]` |
| about the line `y = x` | `reflectionLineYX` | `[0 1 0; 1 0 0; 0 0 1]` |
| about the line `y = −x` | `reflectionLineYNegX` | `[0 −1 0; −1 0 0; 0 0 1]` |
| about an arbitrary line | `reflectionAboutLine(point, theta)` | built by composition, see below |

Effects: `reflectionX: (x, y) → (x, −y)`; `reflectionY: (x, y) → (−x, y)`; `reflectionOrigin: (x, y) → (−x, −y)`; `reflectionLineYX: (x, y) → (y, x)`; `reflectionLineYNegX: (x, y) → (−y, −x)`.

#### Reflection about an arbitrary line

Given a point `p₀ = (x₀, y₀)` on the line and the line's angle `θ` from the positive x-axis, the reflection is composed from primitives in five steps:

```
F = T(x₀, y₀) · R(θ) · F_x · R(−θ) · T(−x₀, −y₀)
```

1. `T(−x₀, −y₀)` moves the line so that it passes through the origin;
2. `R(−θ)` rotates the line onto the x-axis;
3. `F_x` reflects across the x-axis;
4. `R(θ)` rotates the line back to its original direction;
5. `T(x₀, y₀)` returns it to its original position.

The implementation is a literal transcription of that product, so the function also serves as a worked example of composition:

```matlab
T = back * unalign * reflectionX() * align * toOrigin
```

Points on the line are fixed points (asserted by a test).

### 3.6 Shearing

```
Sh_x(kx) = [ 1  kx  0 ]        Sh_y(ky) = [ 1   0  0 ]
           [ 0  1   0 ]                   [ ky  1  0 ]
           [ 0  0   1 ]                   [ 0   0  1 ]
```

```
x' = x + kx · y                y' = ky · x + y
```

Both have `det = 1` (area preserving), but they are **not** length preserving and not orthonormal. Under `Sh_x` the y coordinate is untouched, so horizontal lines stay horizontal while vertical lines become sloped — that tilt is the visual signature of a shear (`Sh_y` is the mirror image: vertical lines stay vertical, horizontal lines tilt).

### 3.7 Rotation and scaling about a pivot

```
aboutPoint(M, p) = T(p) · M · T(−p)
```

Applying `M` about the origin and then translating would move the pivot; conjugating with `T(∓p)` keeps `p` fixed:

```
aboutPoint(R(θ), p) · p = p
```

Typical uses: rotate a shape about its own centre, scale about a corner, or reflect about a horizontal line `y = c` via `aboutPoint(reflectionX(), [0 c])`.

### 3.8 Determinant summary

| Transformation | `det` | Preserves |
|---|---|---|
| translation | 1 | orientation, area, lengths |
| rotation | 1 | orientation, area, lengths, angles |
| uniform scaling `s` | `s²` | orientation, angles |
| general scaling | `sx·sy` | orientation, parallel lines |
| reflection | −1 | area magnitude, lengths (reverses orientation) |
| shear | 1 | orientation, area, parallel lines |

---

## 4. Composition and why order matters

Composition is matrix multiplication, which is associative but **not commutative**: `R·T ≠ T·R` in general. Two concrete consequences follow.

### 4.1 Rotation and translation

With `T(4, 1)` and `R(60°)`:

| Expression | Matrix | Image of the origin |
|---|---|---|
| `compose(T, R)` — translate first | `R · T` | `(1.1340, 3.9641)` |
| `compose(R, T)` — rotate first | `T · R` | `(4.0000, 1.0000)` |

Both values were produced by the demo (see `Issue006_Composite_Order.png`): translating first puts the point at `(4,1)` and the subsequent rotation swings it round the origin; rotating first leaves the origin at the origin, and the translation then moves it along the unrotated `x` axis.

### 4.2 Scaling and translation

`compose(T(2,0), S(2,2))` maps `(1,0) → ((1+2)·2) = (6,0)`, while `compose(S(2,2), T(2,0))` maps `(1,0) → (1·2 + 2) = (4,0)`. Both cases are asserted by tests, so the documented order rule is verified, not assumed.

### 4.3 Reading a composite matrix

Because `p' = M·p` with column vectors, the rightmost factor acts first. When reading a composite left-to-right, remember that it executes right-to-left. The helper `compose(A, B, C)` exists so that source code reads in **execution** order and the library reverses it.

---

## 5. Cohen–Sutherland line clipping

### 5.1 Clipping window

```
window = [xmin ymin xmax ymax],   xmin < xmax,   ymin < ymax
```

For `window = [0 0 10 10]`, the visible region is the axis-aligned rectangle `0 ≤ x ≤ 10`, `0 ≤ y ≤ 10`. The window is **closed**: a point exactly on an edge or corner counts as inside.

### 5.2 Region codes

Each endpoint is classified with a 4-bit code, one bit per infinite half-plane:

| Bit | Value | Name | Condition (y-up) | Meaning |
|---|---|---|---|---|
| — | 0 (`0000`) | INSIDE | `xmin ≤ x ≤ xmax` and `ymin ≤ y ≤ ymax` | visible region |
| 0 | 1 (`0001`) | LEFT | `x < xmin` | left of the window |
| 1 | 2 (`0010`) | RIGHT | `x > xmax` | right of the window |
| 2 | 4 (`0100`) | BOTTOM | `y < ymin` | below the window |
| 3 | 8 (`1000`) | TOP | `y > ymax` | above the window |

The four half-planes divide the plane into **nine** regions. The four corner regions carry two bits each:

| Region | Code | Region | Code |
|---|---|---|---|
| left+bottom | `0101` (5) | left+top | `1001` (9) |
| right+bottom | `0110` (6) | right+top | `1010` (10) |

Bits are assigned with strict inequalities, which is what makes the window boundary inclusive: a point with `x = xmin` is not LEFT, so it is INSIDE unless another bit is set.

### 5.3 Algorithm

```
clipLine(p0, p1, window):
    c0 ← outCode(p0); c1 ← outCode(p1)
    repeat at most 4 times:
        if (c0 | c1) = 0        → trivial accept: the whole segment is visible
        if (c0 & c1) ≠ 0        → trivial reject: both endpoints share an
                                  outside half-plane, so the segment cannot
                                  enter the window
        pick an endpoint whose code ≠ 0 (the "chosen" endpoint)
        pick one set bit of its code, priority TOP, BOTTOM, RIGHT, LEFT
        replace the chosen endpoint with its intersection against that edge
        recompute only the chosen endpoint's code
    return the (possibly shortened) endpoints
```

**Trivial acceptance.** `0000 | 0000 = 0000` means both endpoints are inside; a straight segment between two points of a convex region stays inside, so nothing needs clipping.

**Trivial rejection.** `c0 & c1 ≠ 0` means one bit is set in both codes, i.e. both endpoints lie in the same outside half-plane. That half-plane is convex and disjoint from the window, so the whole segment is invisible. This rejects the four "entirely left/right/above/below" cases in one test, without any division.

**The interesting case.** When the codes share no bit (e.g. `0001` and `1000`) the segment may or may not cross the window, so one boundary is clipped and the test repeats. A segment can pass near a corner without touching the window; it is rejected only after the first clip.

### 5.4 Intersection equations

Clipping against a vertical edge `x = xe` uses the parametric form of the segment:

```
y = y0 + (y1 − y0) · (xe − x0) / (x1 − x0)
x = xe
```

Clipping against a horizontal edge `y = ye`:

```
x = x0 + (x1 − x0) · (ye − y0) / (y1 − y0)
y = ye
```

The coordinate on the boundary is assigned **exactly** (`y0 = ye`, `x0 = xe`) rather than computed, so floating-point error cannot leave the endpoint a fraction outside and trigger the same edge again.

### 5.5 Degenerate and edge cases

| Case | Behaviour |
|---|---|
| vertical segment | never clipped against a vertical edge (both endpoints are LEFT or both RIGHT → trivial reject), so `x1 − x0 = 0` cannot reach a division |
| horizontal segment | symmetric: never clipped against a horizontal edge |
| zero denominator (guard) | an explicit `denominator == 0` check aborts the clip instead of dividing by zero; with valid input it is unreachable |
| endpoint exactly on the boundary | code `0000` on that axis; accepted and returned unchanged on that side |
| segment through a corner | clipped to the corner exactly, e.g. `(−2,−2)→(12,12)` becomes `(0,0)→(10,10)` |
| segment touching one corner only | accepted as a degenerate segment whose two endpoints coincide, e.g. `(−10,0)→(0,10)` becomes `(0,10)→(0,10)` |
| zero-length segment inside | accepted immediately (`0000 \| 0000`) |
| zero-length segment outside | rejected immediately (`c0 & c0 ≠ 0`) |
| segment entirely outside but with no shared bit | rejected after the first clip (the corner-straddle case in §5.6) |

### 5.6 Worked example — the corner straddle

Window `[0 0 10 10]`, segment `(−5, 3) → (3, 15)`.

1. Codes: `0001` (LEFT) and `1000` (TOP). `AND = 0`, so trivial rejection does not apply.
2. Chosen endpoint: `(−5,3)`, bit LEFT. Clip to `x = 0`:
   `y = 3 + (15 − 3) · (0 − (−5)) / (3 − (−5)) = 3 + 12 · 5/8 = 10.5`
   New endpoint `(0, 10.5)`, code now `1000` (TOP).
3. Codes: `1000` and `1000`. `AND ≠ 0` → **reject**.

The line passes above the corner `(0, 10)` and never enters the window; the algorithm needs two iterations and one intersection to prove it. This exact case is a unit test (`rejectsSegmentThatStraddlesACorner`).

### 5.7 Termination and complexity

Each iteration removes one bit from one endpoint (the chosen endpoint leaves the half-plane that was just clipped, and can never re-enter it because the segment is straight). With four possible half-planes per endpoint, at most four clips are needed, then one final accept/reject test. Therefore:

- single segment: **O(1)** time, at most 4 intersections, constant memory (`maxIterations = 12` is a safety bound that also absorbs floating-point re-entry; exceeding it raises `cmr:graphics:CohenSutherland:clippingFailed`);
- polyline of `n` vertices: **O(n)** time, one row per visible segment.

### 5.8 Polyline clipping

`clipPolyline(points, window)` clips each consecutive pair independently and returns one row per visible segment:

```
segments (K×4) = [x0 y0 x1 y1]     K ≤ N−1
```

Invisible segments are dropped rather than returned as NaN, so the result can be plotted directly: `segments(:,1:2)` and `segments(:,3:4)` are the start and end points of each visible piece. A fully invisible polyline returns a genuine `0×4` matrix.

---

## 6. API reference

### 6.1 `cmr.graphics.Transform2D`

All methods are `static`; the class is never instantiated. Every builder returns a finite 3×3 affine matrix.

| Method | Returns | Notes |
|---|---|---|
| `identity()` | `3×3` | identity matrix |
| `translation(tx, ty)` | `3×3` | translation by `(tx, ty)` |
| `rotation(thetaRadians)` | `3×3` | CCW about the origin |
| `rotationDegrees(thetaDegrees)` | `3×3` | convenience wrapper around `rotation` |
| `scaling(sx, sy)` | `3×3` | about the origin; negative factors allowed |
| `reflectionX()` | `3×3` | about the x-axis |
| `reflectionY()` | `3×3` | about the y-axis |
| `reflectionOrigin()` | `3×3` | about the origin |
| `reflectionLineYX()` | `3×3` | about `y = x` |
| `reflectionLineYNegX()` | `3×3` | about `y = −x` |
| `reflectionAboutLine(point, thetaRadians)` | `3×3` | `point` is `[x0 y0]` on the line |
| `shearX(k)` | `3×3` | `x' = x + k·y` |
| `shearY(k)` | `3×3` | `y' = k·x + y` |
| `aboutPoint(M, pivot)` | `3×3` | `T(p)·M·T(−p)`; `pivot` stays fixed |
| `compose(A, B, ...)` | `3×3` | `A` applied first; `compose()` → identity |
| `apply(T, P)` | `N×2` | `P` is `N×2`; a `1×2` point is also accepted |
| `applyPoint(T, p)` | `1×2` | single point, convenience wrapper around `apply` |

Single-point arguments (`pivot` in `aboutPoint`, `point` in `reflectionAboutLine`) accept either `[x y]` or `[x; y]` and are normalised to rows. Matrix arguments (`P` in `apply`) must be `N×2`.

**Errors** (`identifier` → cause):

| Identifier | Cause |
|---|---|
| `cmr:graphics:Transform2D:invalidTranslation` | `tx` or `ty` not a finite real scalar |
| `cmr:graphics:Transform2D:invalidAngle` | angle not a finite real scalar |
| `cmr:graphics:Transform2D:invalidScale` | `sx` or `sy` not a finite real scalar |
| `cmr:graphics:Transform2D:invalidShear` | shear factor not a finite real scalar |
| `cmr:graphics:Transform2D:invalidPivot` | pivot/point not a finite numeric `[x y]` |
| `cmr:graphics:Transform2D:invalidPoints` | `P` not a finite real `N×2` matrix |
| `cmr:graphics:Transform2D:invalidMatrix` | `T` not finite `3×3`, or last row ≠ `[0 0 1]` |

### 6.2 `cmr.graphics.CohenSutherland`

Constant properties: `Inside = 0`, `Left = 1`, `Right = 2`, `Bottom = 4`, `Top = 8`.

| Method | Returns | Notes |
|---|---|---|
| `outCode(point, window)` | scalar | region code of one point |
| `outCodes(points, window)` | `N×1` | vectorised codes |
| `clipLine(p0, p1, window)` | `[p0, p1, accepted]` | rejected → both points `[NaN NaN]`, `accepted = false` |
| `clipPolyline(points, window)` | `K×4` | visible segments `[x0 y0 x1 y1]`; `N ≥ 2` required |

`p0`, `p1` and `point` accept `[x y]` or `[x; y]`; `points` must be `N×2`.

**Errors:**

| Identifier | Cause |
|---|---|
| `cmr:graphics:CohenSutherland:invalidWindow` | not a finite `[xmin ymin xmax ymax]`, or `xmin ≥ xmax` / `ymin ≥ ymax` |
| `cmr:graphics:CohenSutherland:invalidPoint` | not a finite `[x y]` point, or a polyline that is not finite `N×2` with `N ≥ minRows` |
| `cmr:graphics:CohenSutherland:clippingFailed` | defensive: the clipped endpoints did not converge within the iteration bound |

---

## 7. MATLAB path setup (required)

`cmr.graphics` is a **package** (`+cmr/+graphics`). MATLAB resolves it from the folder that *contains* `+cmr`, so the folder to add is `MATLAB/Graphics` — not `MATLAB/Graphics/+cmr` and not the repository root.

```
D:\Matlab-Unity\
└── MATLAB\
    ├── Activities\+cmr\+activities\...
    ├── Communication\+cmr\+net\...
    ├── GameLogic\+cmr\+game\...
    └── Graphics\                 <-- must be on the path for cmr.graphics.*
        ├── +cmr\
        │   └── +graphics\
        │       ├── Transform2D.m        -> cmr.graphics.Transform2D
        │       └── CohenSutherland.m    -> cmr.graphics.CohenSutherland
        ├── demo2DTransformations.m
        └── demoCohenSutherland.m
```

Interactive session:

```matlab
addpath('D:/Matlab-Unity/MATLAB/Graphics');  % resolves cmr.graphics.*
addpath('D:/Matlab-Unity/MATLAB');           % project root (non-package helpers)

% Equivalent single call when every cmr.* package is needed:
% addpath(genpath('D:/Matlab-Unity/MATLAB'));
```

### 7.1 Why the unit tests add two folders

The new test classes register their path with a `PathFixture`:

```matlab
root = fileparts(fileparts(mfilename('fullpath')));   % MATLAB
graphicsRoot = fullfile(root, 'Graphics');            % MATLAB/Graphics
testCase.applyFixture(matlab.unittest.fixtures.PathFixture({root, graphicsRoot}));
```

`PathFixture` is **not recursive**: it adds exactly the folders it is given, without subfolders (unlike `addpath(genpath(...))`). This matters because there is no single package root for the whole project — `+cmr` appears once per area:

```
MATLAB\Activities\+cmr\        MATLAB\Communication\+cmr\
MATLAB\GameLogic\+cmr\         MATLAB\Graphics\+cmr\        <- needs its parent on the path
```

Adding `MATLAB` alone therefore exposes **no** package at all (`MATLAB` contains no `+cmr` folder directly), and every `cmr.graphics.*` call fails with `MATLAB:undefinedVarOrClass` (`Unable to resolve the name 'cmr.graphics.Transform2D.identity'`). The entry that actually resolves the class is `MATLAB/Graphics`; the `MATLAB` entry is kept alongside it for consistency with the existing test classes and to make non-package helpers under `MATLAB` reachable. Both entries are supplied as a cell array to the `PathFixture` constructor.

Note that this is a *test-only* concern: `runAllTests` and `startServer` use `addpath(genpath(...))`, which descends into every area folder, so they resolve all four package roots without any per-area entry.

Any *future* graphics test class must repeat this two-folder setup.

### 7.2 Why the test runner needs no change

`MATLAB/Tests/runAllTests.m` already calls

```matlab
addpath(genpath(projectRoot));   % projectRoot = MATLAB
```

and `genpath` descends into subfolders, so `MATLAB/Graphics` is on the path during a full run. `runAllTests.m` was therefore left untouched, and it discovers `tTransform2D` / `tCohenSutherland` automatically because it builds its suite with `TestSuite.fromFolder(..., 'IncludingSubfolders', true)`.

`MATLAB/startServer.m` likewise uses `addpath(genpath(root))`, so the graphics classes load harmlessly alongside the TCP server and cannot affect the protocol.

The demo scripts add both folders themselves from `mfilename('fullpath')`, so they run from any working directory with no setup.

---

## 8. Usage examples

All outputs below were executed on MATLAB R2026b (see §10) and are reproduced verbatim. Values such as `6.12323e-17` are `cos(pi/2)` in floating point; they are zero to within machine precision, which is why the tests assert with `AbsTol = 1e-12`.

### 8.1 Basic transformations

```matlab
apply = @cmr.graphics.Transform2D.apply;

apply(cmr.graphics.Transform2D.translation(3, 1), [1 1])
% ans = [4 2]

apply(cmr.graphics.Transform2D.rotation(pi/2), [1 0])
% ans = [6.12323e-17 1]        i.e. (0, 1)

apply(cmr.graphics.Transform2D.scaling(2, 0.5), [3 4])
% ans = [6 2]

apply(cmr.graphics.Transform2D.reflectionLineYX(), [2 5])
% ans = [5 2]
```

### 8.2 Composition

```matlab
M = cmr.graphics.Transform2D.compose( ...
        cmr.graphics.Transform2D.translation(2, 0), ...   % applied first
        cmr.graphics.Transform2D.rotationDegrees(90));    % applied second
apply(M, [1 0])
% ans = [1.83697e-16 3]        i.e. (0, 3):  translate (0,0)->(3,0), then rotate 90 CCW
```

The reverse order gives a different point, which is the point of the composition test and of `Issue006_Composite_Order.png`.

### 8.3 Pivot and arbitrary-line reflection

```matlab
P = cmr.graphics.Transform2D.aboutPoint( ...
        cmr.graphics.Transform2D.rotation(pi/2), [1 1]);
apply(P, [2 1])
% ans = [1 2]                  the pivot-relative rotation of (2,1) about (1,1)

apply(cmr.graphics.Transform2D.reflectionAboutLine([1 0], pi/2), [3 0])
% ans = [-1 2.44929e-16]       i.e. (-1, 0): reflection across the vertical line x = 1
```

### 8.4 Clipping one segment

```matlab
[out0, out1, ok] = cmr.graphics.CohenSutherland.clipLine([-2 5], [4 5], [0 0 10 10]);
% ok = 1,  out0 = (0,5),  out1 = (4,5)      left edge intersection

[out0, out1, ok] = cmr.graphics.CohenSutherland.clipLine([-2 4], [-1 6], [0 0 10 10]);
% ok = 0,  out0 = [NaN NaN]                 entirely left -> trivial reject
```

### 8.5 Region codes and polyline clipping

```matlab
cmr.graphics.CohenSutherland.outCode([-1 11], [0 0 10 10])
% ans = 9        LEFT (0001) + TOP (1000)

segments = cmr.graphics.CohenSutherland.clipPolyline( ...
    [0 0; 12 0; 0 12; 0 0], [0 0 10 10])
% segments =
%      0     0    10     0
%     10     2     2    10
%      0    10     0     0
```

The triangle's first edge is cut at `x = 10`, its hypotenuse enters at `(10,2)` and leaves at `(2,10)`, and its vertical edge is cut at `y = 10`.

---

## 9. Demonstration scripts and figures

### 9.1 `demo2DTransformations.m`

Standalone script; run `demo2DTransformations`. It resolves the project root from `mfilename('fullpath')`, adds the paths, and calls only the library — it contains no transformation mathematics of its own.

- Panel set (2×4): original shape, translation `(3,1)`, rotation 45° CCW, scaling `(1.6, 0.6)`, reflection about the y-axis, shear `kx = 0.8`, rotation 90° about the pivot `(1.1, 1.2)`, and reflection about the arbitrary line `y = 0.5x + 1`.
- Reference shape: an asymmetric L-shaped polygon, chosen so that rotation, reflection and shear are visually distinguishable (a square or circle would hide them). The original is ghosted in grey dashed behind each transformed copy, all panels share one set of axis limits, and each panel prints its 3×3 matrix.
- Second figure: 1×3 — original, translate-then-rotate, rotate-then-translate — both using `T(4,1)` and `R(60°)`, with the vertex that starts at the origin tracked by a red marker and labelled with its coordinates.
- Output: `Issue006_2D_Transformations.png`, `Issue006_Composite_Order.png` at 150 dpi.

### 9.2 `demoCohenSutherland.m`

Standalone script; run `demoCohenSutherland`.

- 12 labelled segments covering: 1 fully inside, 4 fully outside (one per side), 4 single-edge crossings, 1 diagonal entering exactly through the corner `(0,0)`, 1 vertical span, 1 horizontal span.
- Left panel: the input segments with both endpoint region codes (`0000`…`1010`), the extended region grid, and all nine region labels (INSIDE, LEFT, RIGHT, BOTTOM, TOP and the four corners).
- Right panel: the clipped result — derived segments drawn as thick solid lines, rejected segments marked with a red cross and a "rejected" label, and an accepted/rejected count in the corner.
- The console prints a 12-row table of endpoints, codes, verdicts and visible spans: 8 accepted, 4 rejected.
- Output: `Issue006_Cohen_Sutherland_Clipping.png` at 150 dpi.

### 9.3 Figure evidence

| File | Size (px) | Content |
|---|---|---|
| `Deliverables/Screenshots/Issue006_2D_Transformations.png` | 2163 × 1089 | all six transformation families + pivot + arbitrary-line reflection |
| `Deliverables/Screenshots/Issue006_Composite_Order.png` | 1855 × 736 | composition order, tracked vertex, resulting matrices |
| `Deliverables/Screenshots/Issue006_Cohen_Sutherland_Clipping.png` | 2123 × 1105 | region codes, trivial accept/reject, four-boundary clipping |

Both scripts create `Deliverables/Screenshots` if it is missing, and raise an error if `exportgraphics` produced no file. Figure windows are shown when running in the MATLAB desktop and kept invisible under `matlab -batch` (detected with `usejava('desktop')`).

---

## 10. Tests and verification

| Suite | Tests | Result |
|---|---|---|
| `tTransform2D` | 24 | 24 passed |
| `tCohenSutherland` | 28 | 28 passed |
| **New total** | **52** | **52 passed** |
| Full regression (`runAllTests`) | 99 | **99 passed**, 0 failed, 0 incomplete |

The 99 total = 47 pre-existing tests (`tCodec` 18, `tServerIntegration` 17, `tSession` 8, `tValidator` 4) + 24 + 28. No pre-existing test regressed, and no existing test file was modified. Full details, commands and per-test results are in `Documentation/Testing/Issue006_Test_Report.md`.

**Environment.** All tests and demos were executed on **MATLAB R2026b (26.2.0.3386108)**. The project's stated target is **R2025b** (`AGENTS.md` §14.1), but R2025b is not installed on the development machine, so **R2025b compatibility was NOT verified**. Only base-MATLAB functionality is used (`eye`, `cos`, `sin`, `deg2rad`, `bitand`, `bitor`, `isfinite`, `zeros`, `patch`, `rectangle`, `lines`, `text`), with no toolbox dependency. The newest functions relied on are `sgtitle` (R2018b) and `exportgraphics` (R2020a), both present in R2025b, so no R2025b incompatibility is expected — but it remains untested.

---

## 11. Limitations and non-goals

- **2D only.** No 4×4 matrices, no projection, no lighting: those are Issues #008–#010.
- **Affine only.** The last row must be `[0 0 1]`; projective/perspective 2D transforms are rejected by design.
- **Axis-aligned rectangular clipping window only.** This is what Cohen–Sutherland requires; polygon/region clipping (Sutherland–Hodgman) is not part of Issue #006.
- **No rasterisation.** Clipping works in continuous coordinates; line rasterisation is Issue #005.
- **Floating point.** `cos(pi/2)` evaluates to `6.12e-17`, not `0`, so exact axis alignment leaves machine-epsilon residues in transformed coordinates. Tests and documented examples state this explicitly instead of rounding silently.
- **Not wired into the game.** The module is a graphics algorithm library plus demos. It does not change the wire protocol, and Unity never calls it in this issue.
- **No R2025b verification**, as noted in §10.

---

## 12. Traceability to Issue #006 acceptance criteria

| Requirement | Where it is satisfied |
|---|---|
| 2D translation | `Transform2D.translation`, tests, figure panel 2 |
| 2D rotation | `Transform2D.rotation` / `rotationDegrees`, tests, figure panel 3 |
| 2D scaling | `Transform2D.scaling`, tests, figure panel 4 |
| Reflection | five canonical methods + `reflectionAboutLine`, tests, figure panels 5 and 8 |
| Shearing | `Transform2D.shearX` / `shearY`, tests, figure panel 6 |
| Homogeneous 3×3 matrices | every builder; `apply` validates the affine last row |
| Combine transformations | `compose`, `aboutPoint`, `reflectionAboutLine`; composition tests; `Issue006_Composite_Order.png` |
| Cohen–Sutherland clipping | `cmr.graphics.CohenSutherland` (manual region codes) |
| Define clipping window | `window = [xmin ymin xmax ymax]`, validated |
| Test inside/outside lines | `tCohenSutherland`: trivial accept, four outside rejects, four boundary crossings |
| Display clipped result | `demoCohenSutherland.m` → `Issue006_Cohen_Sutherland_Clipping.png` |
