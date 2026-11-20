classdef tGraphics2D < matlab.unittest.TestCase
%TGRAPHICS2D Unit tests for the basic 2D computer graphics algorithms.
%
%   Covers DDA, Bresenham (all eight octants, horizontal, vertical and
%   reversed lines), the midpoint circle, shape outlines, scanline polygon
%   filling and the raster canvas. Filled pixels are independently checked
%   against the polygon boundary with a ray-casting parity test.

    methods (TestClassSetup)
        function addProjectPath(testCase)
            root = fileparts(fileparts(mfilename('fullpath')));
            testCase.applyFixture(matlab.unittest.fixtures.PathFixture(root));
            testCase.applyFixture( ...
                matlab.unittest.fixtures.PathFixture(fullfile(root, 'Graphics')));
        end
    end

    methods (Test)
        function ddaHorizontalLine(testCase)
            result = cmr.graphics.Line2D.dda(2, 3, 8, 3);

            testCase.verifyEqual(result.algorithm, "DDA");
            testCase.verifyEqual(result.steps, 6);
            testCase.verifyEqual(result.xIncrement, 1);
            testCase.verifyEqual(result.yIncrement, 0);
            testCase.verifyRasterLine(result.points, [2, 3], [8, 3]);
            testCase.verifyEqual(result.points(:, 2), repmat(3, 7, 1));
        end

        function ddaVerticalLine(testCase)
            result = cmr.graphics.Line2D.dda(5, 1, 5, 9);

            testCase.verifyEqual(result.steps, 8);
            testCase.verifyEqual(result.xIncrement, 0);
            testCase.verifyEqual(result.yIncrement, 1);
            testCase.verifyRasterLine(result.points, [5, 1], [5, 9]);
        end

        function ddaDiagonalLine(testCase)
            result = cmr.graphics.Line2D.dda(0, 0, 4, 4);

            testCase.verifyRasterLine(result.points, [0, 0], [4, 4]);
            testCase.verifyEqual(result.points(:, 1), result.points(:, 2));
        end

        function ddaSinglePoint(testCase)
            result = cmr.graphics.Line2D.dda(3, 3, 3, 3);

            testCase.verifyEqual(result.steps, 0);
            testCase.verifyEqual(result.xIncrement, 0);
            testCase.verifyEqual(result.yIncrement, 0);
            testCase.verifyEqual(result.points, [3, 3]);
        end

        function ddaSteepLine(testCase)
            result = cmr.graphics.Line2D.dda(0, 0, 2, 5);

            testCase.verifyEqual(result.steps, 5);
            testCase.verifyRasterLine(result.points, [0, 0], [2, 5]);
        end

        function ddaReversedLine(testCase)
            result = cmr.graphics.Line2D.dda(8, 3, 2, 3);

            testCase.verifyRasterLine(result.points, [8, 3], [2, 3]);
            testCase.verifyTrue(all(diff(result.points(:, 1)) <= 0));
        end

        function ddaRoundsFractionalInput(testCase)
            result = cmr.graphics.Line2D.dda(1.4, 2.6, 5.6, 2.6);

            testCase.verifyEqual(result.start, [1, 3]);
            testCase.verifyEqual(result.finish, [6, 3]);
            testCase.verifyRasterLine(result.points, [1, 3], [6, 3]);
        end

        function ddaRejectsNonFiniteCoordinates(testCase)
            testCase.verifyError( ...
                @() cmr.graphics.Line2D.dda(NaN, 0, 4, 4), ...
                'cmr:graphics:Line2D:invalidCoordinates');
            testCase.verifyError( ...
                @() cmr.graphics.Line2D.dda(0, 0, Inf, 4), ...
                'cmr:graphics:Line2D:invalidCoordinates');
        end

        function bresenhamHorizontalLine(testCase)
            result = cmr.graphics.Line2D.bresenham(2, 3, 8, 3);

            testCase.verifyEqual(result.algorithm, "BRESENHAM");
            testCase.verifyFalse(result.steep);
            testCase.verifyEqual(result.majorDelta, 6);
            testCase.verifyEqual(result.minorDelta, 0);
            testCase.verifyEqual(numel(result.decisionParameters), 7);
            testCase.verifyRasterLine(result.points, [2, 3], [8, 3]);
        end

        function bresenhamVerticalLine(testCase)
            result = cmr.graphics.Line2D.bresenham(5, 1, 5, 9);

            testCase.verifyTrue(result.steep);
            testCase.verifyEqual(result.majorDelta, 8);
            testCase.verifyEqual(result.minorDelta, 0);
            testCase.verifyRasterLine(result.points, [5, 1], [5, 9]);
        end

        function bresenhamSinglePoint(testCase)
            result = cmr.graphics.Line2D.bresenham(3, 3, 3, 3);

            testCase.verifyEqual(result.majorDelta, 0);
            testCase.verifyEqual(result.points, [3, 3]);
        end

        function bresenhamAllEightOctants(testCase)
            lines = { ...
                "octant 1: shallow, x+, y+", [0, 0], [10, 3]; ...
                "octant 2: steep, x+, y+", [0, 0], [3, 10]; ...
                "octant 3: steep, x-, y+", [0, 0], [-3, 10]; ...
                "octant 4: shallow, x-, y+", [10, 0], [0, 3]; ...
                "octant 5: shallow, x-, y-", [0, 0], [-10, -3]; ...
                "octant 6: steep, x-, y-", [0, 0], [-3, -10]; ...
                "octant 7: steep, x+, y-", [0, 0], [3, -10]; ...
                "octant 8: shallow, x+, y-", [0, 0], [10, -3]};

            for index = 1:size(lines, 1)
                label = lines{index, 1};
                startPoint = lines{index, 2};
                finishPoint = lines{index, 3};

                result = cmr.graphics.Line2D.bresenham( ...
                    startPoint(1), startPoint(2), ...
                    finishPoint(1), finishPoint(2));

                testCase.verifyRasterLine( ...
                    result.points, startPoint, finishPoint);
                testCase.verifyEqual(result.start, startPoint, label);
                testCase.verifyEqual(result.finish, finishPoint, label);

                comparison = cmr.graphics.Line2D.compare( ...
                    startPoint(1), startPoint(2), ...
                    finishPoint(1), finishPoint(2));
                testCase.verifyEqual( ...
                    comparison.ddaCount, comparison.bresenhamCount, label);
                testCase.verifyLessThanOrEqual( ...
                    comparison.maxDeviation, 1, label);
            end
        end

        function bresenhamReversedLineProperties(testCase)
            forward = cmr.graphics.Line2D.bresenham(2, 3, 17, 9);
            reverse = cmr.graphics.Line2D.bresenham(17, 9, 2, 3);

            testCase.verifyEqual(size(forward.points, 1), size(reverse.points, 1));
            testCase.verifyEqual(reverse.points(1, :), [17, 9]);
            testCase.verifyEqual(reverse.points(end, :), [2, 3]);
            testCase.verifyRasterLine(reverse.points, [17, 9], [2, 3]);

            comparison = cmr.graphics.Line2D.compare(17, 9, 2, 3);
            testCase.verifyLessThanOrEqual(comparison.maxDeviation, 1);
        end

        function bresenhamRejectsNonFiniteCoordinates(testCase)
            testCase.verifyError( ...
                @() cmr.graphics.Line2D.bresenham(0, NaN, 4, 4), ...
                'cmr:graphics:Line2D:invalidCoordinates');
        end

        function comparisonOfIdenticalLines(testCase)
            result = cmr.graphics.Line2D.compare(2, 3, 8, 3);

            testCase.verifyTrue(result.identical);
            testCase.verifyEqual(result.ddaCount, 7);
            testCase.verifyEqual(result.bresenhamCount, 7);
            testCase.verifyEqual(result.commonCount, 7);
            testCase.verifyEqual(result.ddaOnlyCount, 0);
            testCase.verifyEqual(result.bresenhamOnlyCount, 0);
            testCase.verifyEqual(result.maxDeviation, 0);
            testCase.verifyEqual(result.endpointError, 0);
        end

        function comparisonFieldsAreConsistent(testCase)
            result = cmr.graphics.Line2D.compare(2, 5, 48, 28);

            testCase.verifyEqual( ...
                result.commonCount + result.ddaOnlyCount, result.ddaCount);
            testCase.verifyEqual( ...
                result.commonCount + result.bresenhamOnlyCount, ...
                result.bresenhamCount);
            testCase.verifyEqual(size(result.ddaOnly, 1), result.ddaOnlyCount);
            testCase.verifyEqual( ...
                size(result.bresenhamOnly, 1), result.bresenhamOnlyCount);
            testCase.verifyLessThanOrEqual(result.maxDeviation, 1);
            testCase.verifyEqual(result.endpointError, 0);
        end

        function circleRadiusZero(testCase)
            result = cmr.graphics.Circle2D.midpoint(4, 7, 0);

            testCase.verifyEqual(result.points, [4, 7]);
            testCase.verifyEqual(result.radius, 0);
        end

        function circleRadiusOne(testCase)
            result = cmr.graphics.Circle2D.midpoint(0, 0, 1);

            expected = [0, 1; 0, -1; 1, 0; -1, 0];
            testCase.verifyEqual(sortrows(result.points), sortrows(expected));
        end

        function circleHasEightWaySymmetry(testCase)
            centre = [30, 40];
            result = cmr.graphics.Circle2D.midpoint(centre(1), centre(2), 10);
            points = result.points;

            for index = 1:size(points, 1)
                dx = points(index, 1) - centre(1);
                dy = points(index, 2) - centre(2);
                expected = [ ...
                    centre(1) + dx, centre(2) + dy; ...
                    centre(1) - dx, centre(2) + dy; ...
                    centre(1) + dx, centre(2) - dy; ...
                    centre(1) - dx, centre(2) - dy; ...
                    centre(1) + dy, centre(2) + dx; ...
                    centre(1) - dy, centre(2) + dx; ...
                    centre(1) + dy, centre(2) - dx; ...
                    centre(1) - dy, centre(2) - dx];
                testCase.verifyTrue(all(ismember(expected, points, 'rows')));
            end
        end

        function circlePointsLieOnRadius(testCase)
            result = cmr.graphics.Circle2D.midpoint(50, 50, 25);

            distances = hypot( ...
                result.points(:, 1) - 50, result.points(:, 2) - 50);
            testCase.verifyTrue(all(abs(distances - 25) <= 0.5 + 1e-9));
        end

        function circleMultipleRadii(testCase)
            radii = [5, 10, 15, 25, 40];
            counts = zeros(1, numel(radii));

            for index = 1:numel(radii)
                result = cmr.graphics.Circle2D.midpoint(60, 60, radii(index));
                distances = hypot( ...
                    result.points(:, 1) - 60, result.points(:, 2) - 60);
                testCase.verifyTrue(all( ...
                    abs(distances - radii(index)) <= 0.5 + 1e-9));
                counts(index) = result.pixelCount;
            end

            testCase.verifyTrue(all(diff(counts) > 0));
            testCase.verifyTrue(all(counts > 0));
        end

        function symmetryPointsReturnsEightDistinctPoints(testCase)
            points = cmr.graphics.Circle2D.symmetryPoints(10, 20, 12, 16);

            testCase.verifySize(points, [8, 2]);
            testCase.verifyEqual(size(unique(points, 'rows'), 1), 8);
        end

        function circleRejectsInvalidRadius(testCase)
            testCase.verifyError( ...
                @() cmr.graphics.Circle2D.midpoint(0, 0, -1), ...
                'cmr:graphics:Circle2D:invalidRadius');
            testCase.verifyError( ...
                @() cmr.graphics.Circle2D.midpoint(0, 0, 2.5), ...
                'cmr:graphics:Circle2D:invalidRadius');
            testCase.verifyError( ...
                @() cmr.graphics.Circle2D.midpoint(0, 0, NaN), ...
                'cmr:graphics:Circle2D:invalidRadius');
        end

        function circleRejectsInvalidCentre(testCase)
            testCase.verifyError( ...
                @() cmr.graphics.Circle2D.midpoint(NaN, 0, 5), ...
                'cmr:graphics:Circle2D:invalidCentre');
        end

        function rectangleOutlineIsClosedAndOnBoundary(testCase)
            shape = cmr.graphics.Shape2D.rectangle(4, 2, 12, 12, [0.2, 0.4, 0.6]);
            points = shape.outlinePoints;

            testCase.verifyEqual(shape.vertexCount, 4);
            testCase.verifyEqual(shape.color, [0.2, 0.4, 0.6]);
            testCase.verifyFalse(isequal(points(1, :), points(end, :)));

            corners = [4, 2; 12, 2; 12, 12; 4, 12];
            for index = 1:size(corners, 1)
                testCase.verifyTrue(ismember(corners(index, :), points, 'rows'));
            end

            onBoundary = points(:, 1) == 4 | points(:, 1) == 12 ...
                | points(:, 2) == 2 | points(:, 2) == 12;
            testCase.verifyTrue(all(onBoundary));
        end

        function triangleOutlineUsesThreeEdges(testCase)
            vertices = [2, 2; 18, 2; 10, 16];
            shape = cmr.graphics.Shape2D.triangle( ...
                vertices(1, :), vertices(2, :), vertices(3, :), [0.1, 0.5, 0.2]);
            points = shape.outlinePoints;

            testCase.verifyEqual(shape.name, "triangle");
            testCase.verifyEqual(shape.vertexCount, 3);

            distances = zeros(size(points, 1), 3);
            for index = 1:size(points, 1)
                for edge = 1:3
                    a = vertices(edge, :);
                    b = vertices(mod(edge, 3) + 1, :);
                    distances(index, edge) = ...
                        tGraphics2D.pointSegmentDistance(points(index, :), a, b);
                end
            end
            testCase.verifyTrue(all(min(distances, [], 2) <= 0.75));
        end

        function polygonOutlineClosesAndKeepsCorners(testCase)
            vertices = [20, 40; 36, 34; 52, 40; 52, 56; 36, 62; 20, 56];
            shape = cmr.graphics.Shape2D.polygon(vertices);
            points = shape.outlinePoints;

            testCase.verifyEqual(shape.name, "polygon");
            testCase.verifyEqual(shape.vertexCount, 6);
            for index = 1:size(vertices, 1)
                testCase.verifyTrue(ismember(vertices(index, :), points, 'rows'));
            end
            testCase.verifyFalse(isequal(points(1, :), points(end, :)));
        end

        function polygonRejectsTooFewVertices(testCase)
            testCase.verifyError( ...
                @() cmr.graphics.Shape2D.polygon([0, 0; 4, 4]), ...
                'cmr:graphics:Shape2D:invalidPolygon');
        end

        function shapeRejectsInvalidColor(testCase)
            testCase.verifyError( ...
                @() cmr.graphics.Shape2D.rectangle(0, 0, 4, 4, [1.2, 0, 0]), ...
                'cmr:graphics:Shape2D:invalidColor');
        end

        function rectangleRejectsNonFiniteCoordinates(testCase)
            testCase.verifyError( ...
                @() cmr.graphics.Shape2D.rectangle(0, 0, NaN, 4), ...
                'cmr:graphics:Shape2D:invalidCoordinates');
        end

        function scanlineFillsRectangle(testCase)
            vertices = [0, 0; 4, 0; 4, 3; 0, 3];
            result = cmr.graphics.Fill2D.scanline(vertices);

            testCase.verifyEqual(result.algorithm, "SCANLINE");
            testCase.verifyEqual(result.pixelCount, 15);
            testCase.verifyEqual(unique(result.points(:, 2))', 0:2);
            testCase.verifyTrue(all(result.points(:, 1) >= 0 & result.points(:, 1) <= 4));
            testCase.verifyTrue(all(result.points(:, 2) >= 0 & result.points(:, 2) <= 3));
        end

        function scanlineFillsTriangle(testCase)
            vertices = [0, 0; 6, 0; 3, 6];
            result = cmr.graphics.Fill2D.scanline(vertices);

            testCase.verifyEqual(result.pixelCount, 24);
            testCase.verifyTrue(ismember([3, 2], result.points, 'rows'));
            for index = 1:size(result.points, 1)
                testCase.verifyTrue(testCase.isInsideOrOnPolygon( ...
                    result.points(index, :), vertices));
            end
        end

        function scanlineFillsConcavePolygon(testCase)
            vertices = [0, 0; 6, 0; 6, 2; 2, 2; 2, 6; 0, 6];
            result = cmr.graphics.Fill2D.scanline(vertices);

            testCase.verifyTrue(ismember([1, 1], result.points, 'rows'));
            testCase.verifyFalse(ismember([4, 4], result.points, 'rows'));
            for index = 1:size(result.points, 1)
                testCase.verifyTrue(testCase.isInsideOrOnPolygon( ...
                    result.points(index, :), vertices));
            end
        end

        function scanlineFillsSymmetricBowTie(testCase)
            % A symmetric bow-tie has zero net signed area because its two
            % lobes are traversed in opposite directions, but the even-odd
            % rule still gives two fillable triangular regions. The fill must
            % not be discarded as a degenerate polygon.
            vertices = [0, 0; 10, 0; 0, 10; 10, 10];
            result = cmr.graphics.Fill2D.scanline(vertices);

            testCase.verifyEqual(result.pixelCount, 60, ...
                'Both even-odd lobes of the bow-tie must be filled.');

            rowWidths = accumarray( ...
                result.points(:, 2) - min(vertices(:, 2)) + 1, ...
                1, [10, 1])';
            testCase.verifyEqual(rowWidths, [11, 9, 7, 5, 3, 1, 3, 5, 7, 9], ...
                'Each scanline must span exactly between the two crossing edges.');

            testCase.verifyTrue(ismember([5, 0], result.points, 'rows'), ...
                'The bottom lobe must be filled.');
            testCase.verifyTrue(ismember([5, 5], result.points, 'rows'), ...
                'The crossing pixel must be filled.');
            testCase.verifyTrue(ismember([5, 9], result.points, 'rows'), ...
                'The top lobe must be filled.');
            testCase.verifyFalse(ismember([0, 5], result.points, 'rows'), ...
                'Pixels left of the lobes must stay empty.');
            testCase.verifyFalse(ismember([10, 5], result.points, 'rows'), ...
                'Pixels right of the lobes must stay empty.');

            for index = 1:size(result.points, 1)
                testCase.verifyTrue(testCase.isInsideOrOnPolygon( ...
                    result.points(index, :), vertices));
            end

            % The old area guard was translation invariant, so a shifted
            % bow-tie failed in exactly the same way.
            translated = cmr.graphics.Fill2D.scanline(vertices + [5, 5]);
            testCase.verifyEqual(translated.pixelCount, 60, ...
                'Translation must not change the filled pixel count.');
        end

        function fillStaysInsidePolygonBoundary(testCase)
            polygons = { ...
                [0, 0; 4, 0; 4, 3; 0, 3], ...
                [0, 0; 6, 0; 3, 6], ...
                [0, 0; 6, 0; 6, 2; 2, 2; 2, 6; 0, 6], ...
                [20, 40; 36, 34; 52, 40; 52, 56; 36, 62; 20, 56]};

            for polygonIndex = 1:numel(polygons)
                vertices = polygons{polygonIndex};
                result = cmr.graphics.Fill2D.scanline(vertices);
                testCase.verifyGreaterThan(result.pixelCount, 0);

                for index = 1:size(result.points, 1)
                    testCase.verifyTrue(testCase.isInsideOrOnPolygon( ...
                        result.points(index, :), vertices), ...
                        sprintf('Fill pixel (%d, %d) left polygon %d.', ...
                        result.points(index, 1), result.points(index, 2), ...
                        polygonIndex));
                end
            end
        end

        function scanlineDegenerateCollinearPolygonReturnsEmpty(testCase)
            result = cmr.graphics.Fill2D.scanline([0, 0; 2, 2; 4, 4]);

            testCase.verifyEqual(result.pixelCount, 0);
            testCase.verifyEmpty(result.points);
        end

        function scanlineRejectsTooFewVertices(testCase)
            testCase.verifyError( ...
                @() cmr.graphics.Fill2D.scanline([0, 0; 4, 4]), ...
                'cmr:graphics:Fill2D:invalidPolygon');
        end

        function scanlineRejectsNonFiniteVertices(testCase)
            testCase.verifyError( ...
                @() cmr.graphics.Fill2D.scanline([0, 0; 4, 0; NaN, 4]), ...
                'cmr:graphics:Fill2D:invalidCoordinates');
        end

        function canvasDrawsAndClipsPixels(testCase)
            canvas = cmr.graphics.Canvas(10, 8);

            clipped = canvas.drawPoints( ...
                [-1, 0; 10, 0; 0, 8; 9, -1; 0, 0; 9, 7], [1, 0, 0]);

            testCase.verifyEqual(clipped, 4);
            testCase.verifyEqual(canvas.pixel(0, 0), [1, 0, 0]);
            testCase.verifyEqual(canvas.pixel(9, 7), [1, 0, 0]);
            testCase.verifyEqual(canvas.pixel(5, 5), [1, 1, 1]);
        end

        function canvasReadsPixels(testCase)
            canvas = cmr.graphics.Canvas(6, 5, [0, 0, 0]);
            canvas.drawPoints([2, 3], [0.2, 0.4, 0.6]);

            testCase.verifyEqual( ...
                canvas.pixel(2, 3), [0.2, 0.4, 0.6], 'AbsTol', 1 / 255);
            testCase.verifyEqual(size(canvas.imageData()), [5, 6, 3]);
            testCase.verifyError( ...
                @() canvas.pixel(6, 0), 'cmr:graphics:Canvas:outOfBounds');
        end

        function canvasRejectsInvalidSize(testCase)
            testCase.verifyError( ...
                @() cmr.graphics.Canvas(0, 5), ...
                'cmr:graphics:Canvas:invalidSize');
            testCase.verifyError( ...
                @() cmr.graphics.Canvas(2.5, 5), ...
                'cmr:graphics:Canvas:invalidSize');
        end

        function canvasRejectsInvalidColor(testCase)
            canvas = cmr.graphics.Canvas(5, 5);

            testCase.verifyError( ...
                @() canvas.drawPoints([0, 0], [1.2, 0, 0]), ...
                'cmr:graphics:Canvas:invalidColor');
            testCase.verifyError( ...
                @() canvas.drawPoints([NaN, 0], [0, 0, 0]), ...
                'cmr:graphics:Canvas:invalidPoints');
        end

        function canvasClearRestoresBackground(testCase)
            canvas = cmr.graphics.Canvas(4, 4, [1, 1, 1]);
            canvas.drawPoints([1, 1; 2, 2], [0, 0, 0]);
            canvas.clear();

            testCase.verifyEqual(canvas.pixel(1, 1), [1, 1, 1]);
            testCase.verifyEqual(canvas.pixel(2, 2), [1, 1, 1]);
        end

        function canvasRendersAndSavesPng(testCase)
            folder = string(tempname);
            mkdir(folder);
            cleanup = onCleanup(@() rmdir(folder, 's')); %#ok<NASGU>
            filePath = fullfile(folder, 'canvas_test.png');

            canvas = cmr.graphics.Canvas(12, 8);
            line = cmr.graphics.Line2D.bresenham(1, 1, 10, 6);
            canvas.drawPoints(line.points, [0, 0, 0]);
            canvas.savePng(filePath, "Canvas export test");

            testCase.verifyTrue(isfile(filePath));
            info = dir(filePath);
            testCase.verifyGreaterThan(info.bytes, 0);
        end
    end

    methods (Access = private)
        function verifyRasterLine(testCase, points, startPoint, finishPoint)
            testCase.verifyEqual(points(1, :), startPoint, ...
                'Raster line must begin at the requested start pixel.');
            testCase.verifyEqual(points(end, :), finishPoint, ...
                'Raster line must end at the requested end pixel.');

            expectedCount = max(abs(finishPoint - startPoint)) + 1;
            testCase.verifyEqual(size(points, 1), expectedCount, ...
                'Raster line must contain one pixel per major-axis step.');

            steps = diff(points, 1, 1);
            testCase.verifyTrue(all(abs(steps(:)) <= 1), ...
                'Consecutive raster pixels must be 8-connected.');
            testCase.verifyTrue(all(any(steps ~= 0, 2)), ...
                'Consecutive raster pixels must not repeat.');

            testCase.verifyTrue(all( ...
                points(:, 1) >= min(startPoint(1), finishPoint(1)) ...
                & points(:, 1) <= max(startPoint(1), finishPoint(1))), ...
                'Raster pixels must stay inside the x bounding box.');
            testCase.verifyTrue(all( ...
                points(:, 2) >= min(startPoint(2), finishPoint(2)) ...
                & points(:, 2) <= max(startPoint(2), finishPoint(2))), ...
                'Raster pixels must stay inside the y bounding box.');

            delta = finishPoint - startPoint;
            if abs(delta(1)) >= abs(delta(2))
                if delta(1) ~= 0
                    testCase.verifyTrue(all( ...
                        sign(diff(points(:, 1))) == sign(delta(1))), ...
                        'The major x axis must advance monotonically.');
                end
            elseif delta(2) ~= 0
                testCase.verifyTrue(all( ...
                    sign(diff(points(:, 2))) == sign(delta(2))), ...
                    'The major y axis must advance monotonically.');
            end
        end

        function tf = isInsideOrOnPolygon(~, point, vertices)
            x = point(1);
            y = point(2);
            vertexCount = size(vertices, 1);

            for index = 1:vertexCount
                a = vertices(index, :);
                b = vertices(mod(index, vertexCount) + 1, :);
                if tGraphics2D.onSegment(x, y, a, b)
                    tf = true;
                    return
                end
            end

            inside = false;
            for index = 1:vertexCount
                a = vertices(index, :);
                b = vertices(mod(index, vertexCount) + 1, :);
                if (a(2) > y) ~= (b(2) > y)
                    xIntersection = a(1) ...
                        + (y - a(2)) * (b(1) - a(1)) / (b(2) - a(2));
                    if x < xIntersection
                        inside = ~inside;
                    end
                end
            end
            tf = inside;
        end
    end

    methods (Static, Access = private)
        function tf = onSegment(x, y, a, b)
            cross = (b(1) - a(1)) * (y - a(2)) - (b(2) - a(2)) * (x - a(1));
            if abs(cross) > 1e-9
                tf = false;
                return
            end

            dot = (x - a(1)) * (b(1) - a(1)) + (y - a(2)) * (b(2) - a(2));
            if dot < -1e-9
                tf = false;
                return
            end

            lengthSquared = (b(1) - a(1))^2 + (b(2) - a(2))^2;
            tf = dot <= lengthSquared + 1e-9;
        end

        function distance = pointSegmentDistance(point, a, b)
            ab = b - a;
            ap = point - a;
            lengthSquared = dot(ab, ab);

            if lengthSquared == 0
                distance = norm(ap);
                return
            end

            projection = max(0, min(1, dot(ap, ab) / lengthSquared));
            distance = norm(point - (a + projection * ab));
        end
    end
end
