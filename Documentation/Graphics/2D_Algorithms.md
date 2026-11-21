# 2D Computer Graphics Algorithms (Issue #005)

## Purpose

This document describes the basic 2D computer graphics algorithms implemented
for the Clean My Room project. All drawing algorithms run in MATLAB
(`cmr.graphics.*`) and produce pixel coordinates or a pixel framebuffer.
MATLAB plotting is used only to display the precomputed framebuffer; no
built-in line, circle, rectangle, patch, fill or inpolygon function performs an
algorithm.

## File map

| File | Responsibility |
|---|---|
| `MATLAB/Graphics/+cmr/+graphics/Line2D.m` | DDA, generalized Bresenham, DDA-vs-Bresenham comparison |
| `MATLAB/Graphics/+cmr/+graphics/Circle2D.m` | Midpoint circle algorithm and eight symmetry points |
| `MATLAB/Graphics/+cmr/+graphics/Shape2D.m` | Rectangle, triangle and polygon outlines from Bresenham |
| `MATLAB/Graphics/+cmr/+graphics/Fill2D.m` | Even-odd scanline polygon fill |
| `MATLAB/Graphics/+cmr/+graphics/Canvas.m` | RGB framebuffer, clipping, display and PNG export |
| `MATLAB/Graphics/demo2DGraphics.m` | Standalone demonstration for requirements A-D |
| `MATLAB/Tests/tGraphics2D.m` | Automated unit tests |

## Coordinate convention

* Coordinates are integer pixel centres.
* `x` grows to the right, `y` grows downwards; `(0, 0)` is the top-left pixel.
* Fractional input coordinates are rounded to the nearest pixel before
  rasterisation. The line endpoint is forced to the rounded requested endpoint.
* Colour is a normalized RGB row `[r g b]` with each component in `[0, 1]`.

## A. DDA line drawing

`cmr.graphics.Line2D.dda(x0, y0, x1, y1)` implements the Digital Differential
Analyser.

1. `dx = x1 - x0`, `dy = y1 - y0`.
2. `steps = max(abs(dx), abs(dy))` (identical endpoints produce `steps = 0` and
   a single pixel).
3. `xIncrement = dx / steps`, `yIncrement = dy / steps`.
4. For `k = 0..steps`, the pixel is `(round(x0 + k*xIncrement),
   round(y0 + k*yIncrement))`.
5. The final pixel is set exactly to the requested endpoint.

The accumulation uses `x0 + k*xIncrement` rather than repeated addition to
avoid floating-point drift. Complexity is `O(max(|dx|, |dy|))`.

Example increments printed by the demonstration:

| Line | Steps | xInc | yInc | Pixels |
|---|---|---|---|---|
| Horizontal | 56 | 1.000 | 0.000 | 57 |
| Vertical | 36 | 0.000 | 1.000 | 37 |
| Diagonal 45 deg | 36 | 1.000 | 1.000 | 37 |
| Shallow positive | 56 | 1.000 | 0.143 | 57 |
| Steep positive | 36 | 0.222 | 1.000 | 37 |
| Shallow negative | 56 | 1.000 | -0.143 | 57 |
| Steep negative | 36 | -0.222 | 1.000 | 37 |

Returned fields: `algorithm`, `start`, `finish`, `steps`, `xIncrement`,
`yIncrement`, `points`.

## B. Bresenham line drawing

`cmr.graphics.Line2D.bresenham(x0, y0, x1, y1)` is an integer-only
implementation that handles all eight octants.

1. `dx = abs(x1 - x0)`, `dy = abs(y1 - y0)`, direction signs `stepX`, `stepY`.
2. If `dy > dx` the line is steep: x and y roles are swapped so the major axis
   drives the loop (`majorDelta`, `minorDelta`).
3. Initial decision parameter `p = 2*minorDelta - majorDelta`.
4. For each major-axis step:
   * emit `(x, y)`;
   * if `p < 0`, `p = p + 2*minorDelta`;
   * otherwise `p = p + 2*minorDelta - 2*majorDelta` and the minor coordinate
     advances;
   * the major coordinate always advances by its sign.
5. Steep lines swap only the major/minor axis roles; the emitted points are the
   true pixel coordinates, so no coordinate swapping or mirroring needs to be
   undone.

Complexity is `O(max(|dx|, |dy|))` and no division or floating point is used
inside the loop. Returned fields: `algorithm`, `start`, `finish`, `steep`,
`majorDelta`, `minorDelta`, `decisionParameters`, `points`.

Horizontal, vertical, both diagonals, all eight octants and reversed endpoints
are covered by tests. Bresenham and DDA may differ by one pixel on exact ties
(for example `(2,25) -> (48,6)`); both algorithms keep the same endpoint and
remain within one pixel of each other.

### DDA versus Bresenham comparison

`cmr.graphics.Line2D.compare(...)` runs both algorithms and reports:

* both pixel sets;
* pixel counts;
* shared pixel count;
* pixels produced only by DDA and only by Bresenham;
* `maxDeviation`, the largest nearest-pixel distance between the two sets;
* `endpointError`, the distance of each final pixel from the true endpoint;
* an `identical` flag.

Executed comparison from the demonstration:

| Line | DDA | Bresenham | Common | DDA only | Bresenham only | Max dev | Identical |
|---|---|---|---|---|---|---|---|
| (2,5)->(48,28) | 47 | 47 | 47 | 0 | 0 | 0.000 | yes |
| (2,2)->(48,2) | 47 | 47 | 47 | 0 | 0 | 0.000 | yes |
| (5,2)->(5,28) | 27 | 27 | 27 | 0 | 0 | 0.000 | yes |
| (2,2)->(28,28) | 27 | 27 | 27 | 0 | 0 | 0.000 | yes |
| (2,25)->(48,6) | 47 | 47 | 46 | 1 | 1 | 1.000 | no |
| (30,2)->(20,28) | 27 | 27 | 27 | 0 | 0 | 0.000 | yes |
| (2,28)->(48,8) | 47 | 47 | 47 | 0 | 0 | 0.000 | yes |

The single disagreement is the expected one-pixel tie difference;
`Deliverables/Screenshots/issue005_dda_vs_bresenham.png` highlights DDA-only
pixels in yellow and Bresenham-only pixels in green.

## C. Midpoint circle algorithm

`cmr.graphics.Circle2D.midpoint(cx, cy, radius)` requires a non-negative
integer radius.

1. Start with `x = 0`, `y = radius`, decision parameter `p = 1 - radius`.
2. While `x <= y`:
   * emit the eight symmetry points of `(x, y)`;
   * if `p < 0`: `p = p + 2x + 3`;
   * otherwise: `p = p + 2(x - y) + 5` and `y = y - 1`;
   * `x = x + 1`.
3. Duplicate points on the axes and for `radius = 0` are removed.

`cmr.graphics.Circle2D.symmetryPoints(cx, cy, x, y)` returns the eight points
`(+-x, +-y)` and `(+-y, +-x)` explicitly.

Executed circle counts:

| Radius | Pixels | Octant steps |
|---|---|---|
| 5 | 28 | 4 |
| 10 | 56 | 8 |
| 15 | 84 | 11 |
| 25 | 140 | 18 |
| 40 | 228 | 29 |

Eight symmetry points for radius 20, octant offset `(12, 16)`, centre
`(60, 60)`:

```
(72, 76)  (48, 76)  (72, 44)  (48, 44)
(76, 72)  (44, 72)  (76, 48)  (44, 48)
```

## D. Basic shapes and scanline filling

### Shape outlines

`cmr.graphics.Shape2D` builds every outline from the project's own Bresenham
implementation:

* `rectangle(x0, y0, x1, y1, color)` normalizes two opposite corners into four
  vertices.
* `triangle(p1, p2, p3, color)` uses three vertices.
* `polygon(vertices, color)` uses an ordered vertex list with at least three
  distinct vertices.

Each edge is rasterised, shared corners are emitted once, and the closing
duplicate of the first vertex is removed so the outline is a closed loop
without a repeated endpoint. Returned fields: `name`, `vertices`,
`vertexCount`, `outlinePoints`, `color`.

Executed outline sizes: rectangle 100 pixels (4 vertices), triangle 68 pixels
(3 vertices), hexagon polygon 96 pixels (6 vertices).

### Scanline polygon fill

`cmr.graphics.Fill2D.scanline(vertices)` uses the even-odd scanline rule:

1. Validate the vertex list, require at least three distinct vertices and
   round coordinates to the pixel lattice.
2. Test for degeneracy by exact collinearity: a polygon has no interior when
   every vertex lies on a single line, and only then does it return an empty
   fill. The check uses a cross product against the vertex farthest from the
   first vertex, so it stays exact for the rounded integer coordinates and
   still works when the first vertices are duplicated.
3. Build the edge list on the fly. Horizontal edges are skipped.
4. For each integer scanline `y` from `ceil(minY)` to `ceil(maxY) - 1`, use the
   half-open rule `yMin <= y < yMax` so a shared vertex is counted exactly
   once.
5. Intersections are `x = xAtYMin + (y - yMin) * dx/dy`; sort them ascending.
6. Fill the spans between intersection pairs (even-odd): from
   `ceil(xLeft)` to `floor(xRight)`, inclusive of boundary pixels.
7. Duplicate pixels are removed; self-intersecting polygons follow the
   even-odd rule.

The net shoelace area is deliberately not used as the degeneracy test.
Self-intersecting polygons can have zero net shoelace area and still contain
fillable even-odd regions: a symmetric bow-tie traverses one lobe positively
and the other negatively, so the signed areas cancel exactly while the even-odd
rule still fills both lobes. Zero net area therefore does not imply an empty
polygon; only collinear vertices do.

Complexity is `O(H * E log E + F)` for `H` scanlines, `E` edges and `F`
filled pixels. Returned fields: `algorithm`, `vertices`, `points`,
`pixelCount`, `scanlineCount`, `edgeCount`.

Executed fill sizes: rectangle 620 pixels, triangle 296 pixels, hexagon
polygon 724 pixels. Every filled pixel is independently checked against the
polygon boundary with a ray-casting parity test in `tGraphics2D`.

Boundary convention: under the y-down convention the fill includes the top
scanline (`y = minY`) and the side boundary pixels of the raster polygon but
excludes the bottom scanline (`y = maxY`); the shape outline covers that edge,
so the composed object has no visual gap.

### Canvas and display

`cmr.graphics.Canvas` stores an RGB `uint8` framebuffer. `drawPoints` sets
pixels and clips out-of-range points, returning the clipped count.
`pixel` reads a normalized colour, `render` displays the framebuffer on axes
with pixel-coordinate ticks, and `savePng` exports an invisible figure with
`exportgraphics`. Display uses `image` on the finished buffer only.

## Error handling

| Condition | Error identifier |
|---|---|
| Non-finite or complex line/rectangle/polygon coordinates | `cmr:graphics:Line2D:invalidCoordinates`, `cmr:graphics:Shape2D:invalidCoordinates`, `cmr:graphics:Fill2D:invalidCoordinates` |
| Negative, non-integer or non-finite circle radius | `cmr:graphics:Circle2D:invalidRadius` |
| Non-finite circle centre | `cmr:graphics:Circle2D:invalidCentre` |
| Fewer than three polygon vertices | `cmr:graphics:Shape2D:invalidPolygon`, `cmr:graphics:Fill2D:invalidPolygon` |
| Fewer than three distinct fill vertices | `cmr:graphics:Fill2D:degeneratePolygon` |
| Colour component outside `[0, 1]` | `cmr:graphics:Shape2D:invalidColor`, `cmr:graphics:Canvas:invalidColor` |
| Non-positive or fractional canvas size | `cmr:graphics:Canvas:invalidSize` |
| Non-finite canvas points | `cmr:graphics:Canvas:invalidPoints` |
| Pixel outside the canvas | `cmr:graphics:Canvas:outOfBounds` |

## Running the demonstration

From the project root:

```matlab
addpath(genpath(fullfile(pwd, 'MATLAB')));
outputs = demo2DGraphics();
```

The function writes five PNG files to `Deliverables/Screenshots/`:

* `issue005_dda.png`
* `issue005_dda_vs_bresenham.png`
* `issue005_circles.png`
* `issue005_shapes_outline.png`
* `issue005_shapes_filled.png`

It prints increment tables, comparison metrics, circle counts, symmetry
points and fill sizes to the command window.

## Scope note

This Issue implements basic 2D raster algorithms only. 2D geometric
transformations, clipping, 3D geometry, 3D transformations, projection and
lighting are separate future Issues and are not part of this implementation.
