function outputs = demo2DGraphics(outputDirectory)
%DEMO2DGRAPHICS Demonstrate the basic 2D computer graphics algorithms.
%
%   outputs = demo2DGraphics() renders the Issue #005 demonstrations and
%   writes the evidence PNG files to Deliverables/Screenshots.
%
%   outputs = demo2DGraphics(outputDirectory) writes the PNG files to the
%   requested folder instead. The returned string array lists every file.
%
%   The script demonstrates:
%       A. DDA line drawing with increments and intermediate pixels.
%       B. Bresenham lines across all slopes and a DDA comparison.
%       C. Midpoint circles with several radii and eight-way symmetry.
%       D. Coloured rectangle, triangle and polygon outlines plus scanline
%          filled interiors.
%
%   All algorithms live in cmr.graphics.*. MATLAB drawing primitives are
%   used only to display the precomputed raster framebuffer.

    arguments
        outputDirectory (1,1) string = ""
    end

    matlabRoot = fileparts(fileparts(mfilename('fullpath'))); % MATLAB/
    projectRoot = fileparts(matlabRoot);
    addpath(genpath(matlabRoot));

    if strlength(outputDirectory) == 0
        outputDirectory = string(fullfile(projectRoot, 'Deliverables', 'Screenshots'));
    end
    if ~isfolder(outputDirectory)
        mkdir(outputDirectory);
    end

    outputs = strings(0, 1);

    fprintf('Clean My Room - Issue #005 2D graphics demonstration\n');
    fprintf('MATLAB %s on %s\n', version, computer);

    % ---------------------------------------------------------------- A. DDA
    fprintf('\n=== A. DDA line drawing ===\n');
    ddaCanvas = cmr.graphics.Canvas(61, 41);
    ddaSpecs = { ...
        'horizontal',        2, 10, 58, 10, [0.10, 0.35, 0.85]; ...
        'vertical',         10,  2, 10, 38, [0.85, 0.25, 0.20]; ...
        'diagonal 45 deg',   2,  2, 38, 38, [0.15, 0.65, 0.30]; ...
        'shallow positive',  2, 20, 58, 28, [0.85, 0.55, 0.10]; ...
        'steep positive',   20,  2, 28, 38, [0.55, 0.20, 0.70]; ...
        'shallow negative',  2, 30, 58, 22, [0.00, 0.60, 0.60]; ...
        'steep negative',   30,  2, 22, 38, [0.40, 0.40, 0.40]};

    fprintf('%-18s %6s %9s %9s %7s\n', 'line', 'steps', 'xInc', 'yInc', 'pixels');
    for index = 1:size(ddaSpecs, 1)
        line = cmr.graphics.Line2D.dda( ...
            ddaSpecs{index, 2}, ddaSpecs{index, 3}, ...
            ddaSpecs{index, 4}, ddaSpecs{index, 5});
        ddaCanvas.drawPoints(line.points, ddaSpecs{index, 6});
        fprintf('%-18s %6d %9.3f %9.3f %7d\n', ...
            ddaSpecs{index, 1}, line.steps, ...
            line.xIncrement, line.yIncrement, size(line.points, 1));
    end

    ddaFile = fullfile(outputDirectory, 'issue005_dda.png');
    ddaCanvas.savePng(ddaFile, ...
        'A. DDA line drawing - increments and intermediate pixels');
    outputs(end + 1, 1) = ddaFile;

    % ---------------------------------------------- B. Bresenham vs DDA
    fprintf('\n=== B. Bresenham line drawing and DDA comparison ===\n');
    comparisonLine = [2, 5, 48, 28];
    ddaLine = cmr.graphics.Line2D.dda(comparisonLine(1), comparisonLine(2), ...
        comparisonLine(3), comparisonLine(4));
    bresenhamLine = cmr.graphics.Line2D.bresenham(comparisonLine(1), ...
        comparisonLine(2), comparisonLine(3), comparisonLine(4));
    comparison = cmr.graphics.Line2D.compare(comparisonLine(1), ...
        comparisonLine(2), comparisonLine(3), comparisonLine(4));

    ddaCanvas2 = cmr.graphics.Canvas(51, 31);
    ddaCanvas2.drawPoints(ddaLine.points, [0.10, 0.35, 0.85]);

    bresenhamCanvas = cmr.graphics.Canvas(51, 31);
    bresenhamCanvas.drawPoints(bresenhamLine.points, [0.85, 0.25, 0.20]);

    overlayCanvas = cmr.graphics.Canvas(51, 31);
    overlayCanvas.drawPoints(ddaLine.points, [0.10, 0.35, 0.85]);
    overlayCanvas.drawPoints(bresenhamLine.points, [0.85, 0.25, 0.20]);
    overlayCanvas.drawPoints(comparison.ddaOnly, [0.95, 0.75, 0.10]);
    overlayCanvas.drawPoints(comparison.bresenhamOnly, [0.45, 0.80, 0.20]);

    comparisonFigure = figure( ...
        'Visible', 'off', ...
        'Color', 'w', ...
        'Units', 'pixels', ...
        'Position', [100, 100, 1500, 560]);
    comparisonLayout = tiledlayout(comparisonFigure, 1, 3, ...
        'TileSpacing', 'compact', 'Padding', 'compact');

    ddaAxis = nexttile(comparisonLayout);
    ddaCanvas2.render(ddaAxis, 'DDA pixels (blue)');

    bresenhamAxis = nexttile(comparisonLayout);
    bresenhamCanvas.render(bresenhamAxis, 'Bresenham pixels (red)');

    overlayAxis = nexttile(comparisonLayout);
    overlayCanvas.render(overlayAxis, ...
        'Overlay: yellow = DDA only, green = Bresenham only');

    title(comparisonLayout, ...
        'B. Bresenham versus DDA: same endpoints, pixel-level comparison', ...
        'Interpreter', 'none');

    comparisonFile = fullfile(outputDirectory, ...
        'issue005_dda_vs_bresenham.png');
    exportgraphics(comparisonFigure, char(comparisonFile), 'Resolution', 144);
    close(comparisonFigure);
    outputs(end + 1, 1) = comparisonFile;

    fprintf('%-24s %5s %5s %8s %9s %9s %9s %10s\n', ...
        'line', 'DDA', 'Bres', 'common', 'DDA only', ...
        'Bres only', 'max dev', 'identical');
    compareSpecs = [ ...
        2, 5, 48, 28; ...
        2, 2, 48, 2; ...
        5, 2, 5, 28; ...
        2, 2, 28, 28; ...
        2, 25, 48, 6; ...
        30, 2, 20, 28; ...
        2, 28, 48, 8];
    for index = 1:size(compareSpecs, 1)
        result = cmr.graphics.Line2D.compare( ...
            compareSpecs(index, 1), compareSpecs(index, 2), ...
            compareSpecs(index, 3), compareSpecs(index, 4));
        label = sprintf('(%d,%d)->(%d,%d)', compareSpecs(index, :));
        if result.identical
            identicalText = 'yes';
        else
            identicalText = 'no';
        end
        fprintf('%-24s %5d %5d %8d %9d %9d %9.3f %10s\n', ...
            label, result.ddaCount, result.bresenhamCount, ...
            result.commonCount, result.ddaOnlyCount, ...
            result.bresenhamOnlyCount, result.maxDeviation, ...
            identicalText);
    end

    % ------------------------------------------------------------- C. Circle
    fprintf('\n=== C. Midpoint circle drawing ===\n');
    radii = [5, 10, 15, 25, 40];
    circleColors = [ ...
        0.10, 0.35, 0.85; ...
        0.85, 0.25, 0.20; ...
        0.15, 0.65, 0.30; ...
        0.85, 0.55, 0.10; ...
        0.55, 0.20, 0.70];

    circleCanvas = cmr.graphics.Canvas(121, 121);
    for index = 1:numel(radii)
        circle = cmr.graphics.Circle2D.midpoint(60, 60, radii(index));
        circleCanvas.drawPoints(circle.points, circleColors(index, :));
        fprintf('radius %2d: %3d pixels, %2d octant steps\n', ...
            circle.radius, circle.pixelCount, numel(circle.decisionParameters));
    end

    symmetryCanvas = cmr.graphics.Canvas(121, 121);
    symmetryCircle = cmr.graphics.Circle2D.midpoint(60, 60, 20);
    symmetryCanvas.drawPoints(symmetryCircle.points, [0.10, 0.35, 0.85]);

    symmetry = cmr.graphics.Circle2D.symmetryPoints(60, 60, 12, 16);
    fprintf('Eight symmetry points for octant offset (12, 16):\n');
    for index = 1:size(symmetry, 1)
        fprintf('  (%3d, %3d)\n', symmetry(index, 1), symmetry(index, 2));
        symmetryCanvas.drawPoints( ...
            blockPoints(symmetry(index, 1), symmetry(index, 2), 1), ...
            [0.05, 0.05, 0.05]);
    end

    circleFigure = figure( ...
        'Visible', 'off', ...
        'Color', 'w', ...
        'Units', 'pixels', ...
        'Position', [100, 100, 1250, 620]);
    circleLayout = tiledlayout(circleFigure, 1, 2, ...
        'TileSpacing', 'compact', 'Padding', 'compact');

    circleAxis = nexttile(circleLayout);
    circleCanvas.render(circleAxis, 'Circles with radii 5, 10, 15, 25, 40');

    symmetryAxis = nexttile(circleLayout);
    symmetryCanvas.render(symmetryAxis, ...
        'Radius 20 circle and one set of eight symmetry points');

    title(circleLayout, ...
        'C. Midpoint circle algorithm and eight-way symmetry', ...
        'Interpreter', 'none');

    circleFile = fullfile(outputDirectory, 'issue005_circles.png');
    exportgraphics(circleFigure, char(circleFile), 'Resolution', 144);
    close(circleFigure);
    outputs(end + 1, 1) = circleFile;

    % --------------------------------------------------- D. Shapes (outline)
    fprintf('\n=== D. Basic shapes and scanline fill ===\n');
    rectangleShape = cmr.graphics.Shape2D.rectangle( ...
        8, 8, 38, 28, [0.10, 0.35, 0.85]);
    triangleShape = cmr.graphics.Shape2D.triangle( ...
        [50, 8], [78, 8], [64, 28], [0.15, 0.65, 0.30]);
    polygonVertices = [ ...
        20, 40; ...
        36, 34; ...
        52, 40; ...
        52, 56; ...
        36, 62; ...
        20, 56];
    polygonShape = cmr.graphics.Shape2D.polygon( ...
        polygonVertices, [0.55, 0.20, 0.70]);

    outlineCanvas = cmr.graphics.Canvas(101, 71);
    outlineCanvas.drawPoints(rectangleShape.outlinePoints, rectangleShape.color);
    outlineCanvas.drawPoints(triangleShape.outlinePoints, triangleShape.color);
    outlineCanvas.drawPoints(polygonShape.outlinePoints, polygonShape.color);
    for shapeIndex = 1:3
        if shapeIndex == 1
            shape = rectangleShape;
        elseif shapeIndex == 2
            shape = triangleShape;
        else
            shape = polygonShape;
        end
        fprintf('%-10s outline: %4d pixels, %d vertices\n', ...
            char(shape.name), size(shape.outlinePoints, 1), shape.vertexCount);
    end

    outlineFile = fullfile(outputDirectory, ...
        'issue005_shapes_outline.png');
    outlineCanvas.savePng(outlineFile, ...
        'D. Coloured rectangle, triangle and polygon outlines');
    outputs(end + 1, 1) = outlineFile;

    % ---------------------------------------------------- D. Shapes (filled)
    rectangleFill = cmr.graphics.Fill2D.scanline(rectangleShape.vertices);
    triangleFill = cmr.graphics.Fill2D.scanline(triangleShape.vertices);
    polygonFill = cmr.graphics.Fill2D.scanline(polygonShape.vertices);

    filledCanvas = cmr.graphics.Canvas(101, 71);
    filledCanvas.drawPoints(rectangleFill.points, [0.75, 0.85, 1.00]);
    filledCanvas.drawPoints(triangleFill.points, [0.75, 0.95, 0.80]);
    filledCanvas.drawPoints(polygonFill.points, [0.95, 0.80, 0.95]);
    filledCanvas.drawPoints(rectangleShape.outlinePoints, rectangleShape.color);
    filledCanvas.drawPoints(triangleShape.outlinePoints, triangleShape.color);
    filledCanvas.drawPoints(polygonShape.outlinePoints, polygonShape.color);

    fprintf('rectangle fill: %4d pixels\n', rectangleFill.pixelCount);
    fprintf('triangle  fill: %4d pixels\n', triangleFill.pixelCount);
    fprintf('polygon   fill: %4d pixels\n', polygonFill.pixelCount);

    filledFile = fullfile(outputDirectory, ...
        'issue005_shapes_filled.png');
    filledCanvas.savePng(filledFile, ...
        'D. Scanline filled interiors with coloured outlines');
    outputs(end + 1, 1) = filledFile;

    fprintf('\nSaved %d evidence figures to:\n  %s\n', ...
        numel(outputs), outputDirectory);
    for index = 1:numel(outputs)
        fprintf('  %s\n', outputs(index));
    end
end

function points = blockPoints(x, y, halfSize)
%BLOCKPOINTS Pixel block used to highlight a symmetry point in the demo.
    [dx, dy] = meshgrid(-halfSize:halfSize, -halfSize:halfSize);
    points = [x + dx(:), y + dy(:)];
end
