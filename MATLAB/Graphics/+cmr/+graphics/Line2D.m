classdef Line2D
%LINE2D Raster line drawing algorithms (DDA and Bresenham).
%
%   Coordinates are integer pixel centres on a raster grid whose x axis grows
%   to the right and whose y axis grows downwards. Fractional input
%   coordinates are rounded to the nearest pixel before rasterisation, so the
%   generated points always refer to whole pixels.
%
%   Example:
%       line = cmr.graphics.Line2D.bresenham(2, 3, 18, 9);
%       plot(line.points(:, 1), line.points(:, 2), '.')

    methods (Static)
        function result = dda(x0, y0, x1, y1)
            %DDA Draw a raster line with the Digital Differential Analyser.
            %
            %   The line is driven by its major axis. The increments per step
            %   are dx/steps and dy/steps with steps = max(abs(dx), abs(dy)).
            %   Every intermediate position is rounded to a pixel centre.
            arguments
                x0 (1,1) double
                y0 (1,1) double
                x1 (1,1) double
                y1 (1,1) double
            end

            [x0, y0, x1, y1] = cmr.graphics.Line2D.checkCoordinates(x0, y0, x1, y1);

            dx = x1 - x0;
            dy = y1 - y0;
            steps = max(abs(dx), abs(dy));

            if steps == 0
                xIncrement = 0;
                yIncrement = 0;
                points = [x0, y0];
            else
                xIncrement = dx / steps;
                yIncrement = dy / steps;

                k = (0:steps)';
                points = [ ...
                    round(x0 + k .* xIncrement), ...
                    round(y0 + k .* yIncrement)];
                points(end, :) = [x1, y1];
            end

            result = struct( ...
                'algorithm', "DDA", ...
                'start', [x0, y0], ...
                'finish', [x1, y1], ...
                'steps', steps, ...
                'xIncrement', xIncrement, ...
                'yIncrement', yIncrement, ...
                'points', points);
        end

        function result = bresenham(x0, y0, x1, y1)
            %BRESENHAM Draw a raster line with the Bresenham algorithm.
            %
            %   The implementation is integer-only and handles all eight
            %   octants. When the line is steep the x and y roles are swapped
            %   so that the major axis always drives the loop. The decision
            %   parameter p starts at 2*minorDelta - majorDelta.
            arguments
                x0 (1,1) double
                y0 (1,1) double
                x1 (1,1) double
                y1 (1,1) double
            end

            [x0, y0, x1, y1] = cmr.graphics.Line2D.checkCoordinates(x0, y0, x1, y1);

            dx = abs(x1 - x0);
            dy = abs(y1 - y0);
            stepX = sign(x1 - x0);
            stepY = sign(y1 - y0);
            if stepX == 0
                stepX = 1;
            end
            if stepY == 0
                stepY = 1;
            end

            steep = dy > dx;
            if steep
                majorDelta = dy;
                minorDelta = dx;
            else
                majorDelta = dx;
                minorDelta = dy;
            end

            decisionParameter = 2 * minorDelta - majorDelta;

            x = x0;
            y = y0;
            points = zeros(majorDelta + 1, 2);
            parameters = zeros(majorDelta + 1, 1);

            for index = 1:(majorDelta + 1)
                points(index, :) = [x, y];
                parameters(index) = decisionParameter;

                if steep
                    if decisionParameter < 0
                        decisionParameter = decisionParameter + 2 * minorDelta;
                    else
                        decisionParameter = decisionParameter ...
                            + 2 * minorDelta - 2 * majorDelta;
                        x = x + stepX;
                    end
                    y = y + stepY;
                else
                    if decisionParameter < 0
                        decisionParameter = decisionParameter + 2 * minorDelta;
                    else
                        decisionParameter = decisionParameter ...
                            + 2 * minorDelta - 2 * majorDelta;
                        y = y + stepY;
                    end
                    x = x + stepX;
                end
            end

            result = struct( ...
                'algorithm', "BRESENHAM", ...
                'start', [x0, y0], ...
                'finish', [x1, y1], ...
                'steep', steep, ...
                'majorDelta', majorDelta, ...
                'minorDelta', minorDelta, ...
                'decisionParameters', parameters, ...
                'points', points);
        end

        function result = compare(x0, y0, x1, y1)
            %COMPARE Compare the DDA and Bresenham rasterisation of one line.
            %
            %   The comparison reports the two pixel sets, the number of
            %   shared and differing pixels, the largest nearest-pixel
            %   deviation between the sets and the endpoint error.
            arguments
                x0 (1,1) double
                y0 (1,1) double
                x1 (1,1) double
                y1 (1,1) double
            end

            ddaLine = cmr.graphics.Line2D.dda(x0, y0, x1, y1);
            bresenhamLine = cmr.graphics.Line2D.bresenham(x0, y0, x1, y1);

            ddaPoints = unique(ddaLine.points, 'rows', 'stable');
            bresenhamPoints = unique(bresenhamLine.points, 'rows', 'stable');

            common = intersect(ddaPoints, bresenhamPoints, 'rows');
            ddaOnly = setdiff(ddaPoints, bresenhamPoints, 'rows');
            bresenhamOnly = setdiff(bresenhamPoints, ddaPoints, 'rows');

            deviation = max( ...
                cmr.graphics.Line2D.maxNearestDistance(ddaPoints, bresenhamPoints), ...
                cmr.graphics.Line2D.maxNearestDistance(bresenhamPoints, ddaPoints));

            endpointError = max( ...
                norm(ddaPoints(end, :) - ddaLine.finish), ...
                norm(bresenhamPoints(end, :) - bresenhamLine.finish));

            result = struct( ...
                'start', ddaLine.start, ...
                'finish', ddaLine.finish, ...
                'ddaPoints', ddaPoints, ...
                'bresenhamPoints', bresenhamPoints, ...
                'ddaCount', size(ddaPoints, 1), ...
                'bresenhamCount', size(bresenhamPoints, 1), ...
                'commonCount', size(common, 1), ...
                'ddaOnlyCount', size(ddaOnly, 1), ...
                'bresenhamOnlyCount', size(bresenhamOnly, 1), ...
                'ddaOnly', ddaOnly, ...
                'bresenhamOnly', bresenhamOnly, ...
                'maxDeviation', deviation, ...
                'endpointError', endpointError, ...
                'identical', isempty(ddaOnly) && isempty(bresenhamOnly));
        end
    end

    methods (Static, Access = private)
        function [x0, y0, x1, y1] = checkCoordinates(x0, y0, x1, y1)
            values = [x0, y0, x1, y1];
            if ~isreal(values) || any(~isfinite(values))
                error('cmr:graphics:Line2D:invalidCoordinates', ...
                    'Line coordinates must be finite real numbers.');
            end
            x0 = round(x0);
            y0 = round(y0);
            x1 = round(x1);
            y1 = round(y1);
        end

        function value = maxNearestDistance(pointsA, pointsB)
            if isempty(pointsA) || isempty(pointsB)
                value = 0;
                return
            end

            value = 0;
            for index = 1:size(pointsA, 1)
                dx = pointsB(:, 1) - pointsA(index, 1);
                dy = pointsB(:, 2) - pointsA(index, 2);
                nearest = min(hypot(dx, dy));
                if nearest > value
                    value = nearest;
                end
            end
        end
    end
end
