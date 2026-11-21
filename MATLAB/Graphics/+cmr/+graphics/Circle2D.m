classdef Circle2D
%CIRCLE2D Raster circle drawing with the midpoint circle algorithm.
%
%   The algorithm evaluates one octant and mirrors every computed point into
%   the eight octants of the circle. Coordinates are integer pixel centres.

    methods (Static)
        function result = midpoint(cx, cy, radius)
            %MIDPOINT Draw an integer raster circle with the midpoint algorithm.
            %
            %   The initial decision parameter is p = 1 - radius. While
            %   x <= y the eight symmetry points are emitted and p is updated
            %   with p + 2x + 3 (inside) or p + 2(x - y) + 5 (outside).
            arguments
                cx (1,1) double
                cy (1,1) double
                radius (1,1) double
            end

            [cx, cy] = cmr.graphics.Circle2D.checkCentre(cx, cy);
            r = cmr.graphics.Circle2D.checkRadius(radius);

            x = 0;
            y = r;
            decisionParameter = 1 - r;

            octantPoints = zeros(0, 2);
            parameters = zeros(0, 1);
            allPoints = zeros(0, 2);

            while x <= y
                octantPoints(end + 1, :) = [x, y]; %#ok<AGROW>
                parameters(end + 1, 1) = decisionParameter; %#ok<AGROW>

                symmetry = cmr.graphics.Circle2D.symmetryPoints(cx, cy, x, y);
                allPoints = [allPoints; symmetry]; %#ok<AGROW>

                if decisionParameter < 0
                    decisionParameter = decisionParameter + 2 * x + 3;
                else
                    decisionParameter = decisionParameter + 2 * (x - y) + 5;
                    y = y - 1;
                end
                x = x + 1;
            end

            points = unique(allPoints, 'rows', 'stable');

            result = struct( ...
                'algorithm', "MIDPOINT", ...
                'centre', [cx, cy], ...
                'radius', r, ...
                'octantPoints', octantPoints, ...
                'decisionParameters', parameters, ...
                'pixelCount', size(points, 1), ...
                'points', points);
        end

        function points = symmetryPoints(cx, cy, x, y)
            %SYMMETRYPOINTS Return the eight symmetric points of one octant.
            %
            %   The eight points are (+-x, +-y) and (+-y, +-x) relative to
            %   the circle centre.
            arguments
                cx (1,1) double
                cy (1,1) double
                x (1,1) double
                y (1,1) double
            end

            points = [ ...
                cx + x, cy + y; ...
                cx - x, cy + y; ...
                cx + x, cy - y; ...
                cx - x, cy - y; ...
                cx + y, cy + x; ...
                cx - y, cy + x; ...
                cx + y, cy - x; ...
                cx - y, cy - x];
        end
    end

    methods (Static, Access = private)
        function [cx, cy] = checkCentre(cx, cy)
            if ~isreal(cx) || ~isreal(cy) || ~isfinite(cx) || ~isfinite(cy)
                error('cmr:graphics:Circle2D:invalidCentre', ...
                    'Circle centre coordinates must be finite real numbers.');
            end
            cx = round(cx);
            cy = round(cy);
        end

        function r = checkRadius(radius)
            if ~isreal(radius) || ~isfinite(radius) || radius < 0
                error('cmr:graphics:Circle2D:invalidRadius', ...
                    'Circle radius must be a finite, non-negative number.');
            end
            if radius ~= round(radius)
                error('cmr:graphics:Circle2D:invalidRadius', ...
                    'Circle radius must be an integer number of pixels.');
            end
            r = round(radius);
        end
    end
end
