# Issue #005 - 2D Graphics Test Report

## Environment

| Item | Value |
|---|---|
| MATLAB version | 25.2.0.2998904 (R2025b) |
| Platform | PCWIN64 |
| Command mode | `matlab -batch` (headless) |
| Project root | `C:\Users\zahraa\Desktop\Matlab-Unity-main` |
| External dependencies added | None (no toolboxes or packages required) |

## Test plan

| Area | What is verified |
|---|---|
| DDA | Horizontal, vertical, 45 degree, steep, reversed and fractional-input lines; increments; step count; single-pixel line; invalid coordinates |
| Bresenham | Horizontal, vertical, single point; all eight octants; reversed lines; decision parameters; adjacency and bounding box; invalid coordinates |
| Comparison | Identical lines; shared/differing pixel counts; endpoint error; deviation at most one pixel |
| Midpoint circle | Radius 0 and 1; eight-way symmetry; points on radius; several radii; eight distinct symmetry points; invalid radius and centre |
| Shapes | Rectangle closure and boundary; triangle edges; polygon corners and closure; too few vertices; invalid colour; non-finite coordinates |
| Scanline fill | Rectangle span, triangle, concave polygon, symmetric bow-tie, pixel-inside-polygon check, collinear-degenerate polygon, too few vertices, non-finite vertices |
| Canvas | Drawing and clipping, pixel readback, image data size, invalid size/colour/points, clear, PNG export |

Every filled pixel is independently checked with a ray-casting parity test
including boundary detection. This verifies that filled pixels stay inside the
polygon boundary. The symmetric bow-tie regression test
(`scanlineFillsSymmetricBowTie`) additionally verifies that both even-odd lobes
are filled (60 pixels), the per-scanline widths and translation invariance.

## Commands executed

Targeted graphics suite:

```powershell
& "C:\Program Files\MATLAB\R2025b\bin\matlab.exe" -batch "addpath(genpath(fullfile(pwd,'MATLAB'))); addpath(fullfile(pwd,'MATLAB','Tests')); results = runtests('MATLAB/Tests/tGraphics2D.m'); disp(table(results)); if ~all([results.Passed]), error('cmr:tests:graphicsFailed','Graphics tests failed.'); end"
```

Full suite:

```powershell
& "C:\Program Files\MATLAB\R2025b\bin\matlab.exe" -batch "addpath(genpath(fullfile(pwd,'MATLAB'))); addpath(fullfile(pwd,'MATLAB','Tests')); results = runAllTests();"
```

Demonstration:

```powershell
& "C:\Program Files\MATLAB\R2025b\bin\matlab.exe" -batch "addpath(genpath(fullfile(pwd,'MATLAB'))); outputs = demo2DGraphics(); disp(outputs);"
```

## Results

### Graphics suite

| Metric | Result |
|---|---|
| Tests | 44 |
| Passed | 44 |
| Failed | 0 |
| Incomplete | 0 |
| Duration | 11.6 s (full-suite run) |

**Status: PASSED**

### Full project suite

| Test class | Tests | Result |
|---|---|---|
| `tCodec` | 18 | PASSED |
| `tGraphics2D` | 44 | PASSED |
| `tServerIntegration` | 17 | PASSED |
| `tSession` | 8 | PASSED |
| `tValidator` | 4 | PASSED |
| **Total** | **91** | **91 passed, 0 failed, 0 incomplete** |

Full-suite duration: 31.78 s. **Status: PASSED**

### Failures during development (reported for transparency)

The first graphics run had 2 failures; both were corrected and the final run
above is the actual executed result.

1. `circleHasEightWaySymmetry`: the test used an 8x1 logical result with
   `verifyTrue`. Fixed the assertion with `all(ismember(...))`. Test defect,
   not an algorithm defect.
2. `scanlineDegenerateCollinearPolygonReturnsEmpty`: a collinear triangle
   produced 4 spine pixels instead of an empty fill. The degeneracy check in
   `Fill2D.scanline` is now an exact collinearity test (a cross-product test
   against the vertex farthest from the first vertex) so the documented
   empty-fill contract holds. A net-shoelace zero-area guard is deliberately
   not used: a symmetric bow-tie has zero net signed area but still has two
   fillable even-odd lobes, which `scanlineFillsSymmetricBowTie` verifies.
   After the fix the test passes.

No failures were hidden and no test result was fabricated.

## Demonstration output (actual)

DDA increments and pixel counts:

```
horizontal         56 steps, xInc 1.000,  yInc 0.000,  57 pixels
vertical           36 steps, xInc 0.000,  yInc 1.000,  37 pixels
diagonal 45 deg    36 steps, xInc 1.000,  yInc 1.000,  37 pixels
shallow positive   56 steps, xInc 1.000,  yInc 0.143,  57 pixels
steep positive     36 steps, xInc 0.222,  yInc 1.000,  37 pixels
shallow negative   56 steps, xInc 1.000,  yInc -0.143, 57 pixels
steep negative     36 steps, xInc -0.222, yInc 1.000,  37 pixels
```

DDA versus Bresenham:

```
(2,5)->(48,28)    DDA 47, Bres 47, common 47, max deviation 0.000, identical yes
(2,2)->(48,2)     DDA 47, Bres 47, common 47, max deviation 0.000, identical yes
(5,2)->(5,28)     DDA 27, Bres 27, common 27, max deviation 0.000, identical yes
(2,2)->(28,28)    DDA 27, Bres 27, common 27, max deviation 0.000, identical yes
(2,25)->(48,6)    DDA 47, Bres 47, common 46, max deviation 1.000, identical no
(30,2)->(20,28)   DDA 27, Bres 27, common 27, max deviation 0.000, identical yes
(2,28)->(48,8)    DDA 47, Bres 47, common 47, max deviation 0.000, identical yes
```

Circles: radius 5 -> 28 pixels, 10 -> 56, 15 -> 84, 25 -> 140, 40 -> 228.

Fills: rectangle 620 pixels, triangle 296 pixels, hexagon polygon 724 pixels.

## Evidence files

All files were produced by the executed demonstration and validated with
`imfinfo`.

| File | Dimensions | Bytes |
|---|---|---|
| `Deliverables/Screenshots/issue005_dda.png` | 1181 x 834 truecolour | 28,330 |
| `Deliverables/Screenshots/issue005_dda_vs_bresenham.png` | 2078 x 498 truecolour | 46,385 |
| `Deliverables/Screenshots/issue005_circles.png` | 1739 x 888 truecolour | 51,709 |
| `Deliverables/Screenshots/issue005_shapes_outline.png` | 1189 x 868 truecolour | 29,706 |
| `Deliverables/Screenshots/issue005_shapes_filled.png` | 1189 x 868 truecolour | 29,330 |

## Manual verification steps

1. Open MATLAB R2025b in the project root.
2. Run `addpath(genpath(fullfile(pwd, 'MATLAB')));`.
3. Run `outputs = demo2DGraphics();`. Confirm five PNG paths are printed and
   the five files exist in `Deliverables/Screenshots/`.
4. Open each PNG and confirm:
   * DDA figure shows seven coloured lines with visible pixel steps;
   * comparison figure shows DDA-only and Bresenham-only pixels highlighted;
   * circle figure shows five radii and eight highlighted symmetry points;
   * outline figure shows coloured rectangle, triangle and hexagon;
   * filled figure shows filled interiors with coloured outlines.
5. Run `addpath(fullfile(pwd, 'MATLAB', 'Tests')); runAllTests` and confirm
   91 passed, 0 failed, 0 incomplete.
6. Inspect a drop-target style placement request flow in the existing server
   tests if a regression is suspected; the graphics code does not modify the
   protocol or activity validation.

## Items not executed / limitations

* The PNG evidence was validated as readable truecolour images with
  `imfinfo`, but automated pixel-level inspection of the rendered PNG files was
  not performed; visual review is left to the project owner/reviewer.
* No Unity test was run; this Issue introduces no Unity changes and no
  protocol changes.
* The build was executed on PCWIN64 with MATLAB R2025b only. Other platforms
  and MATLAB versions were not tested.
* No GitHub or Git operation was performed at any point.
