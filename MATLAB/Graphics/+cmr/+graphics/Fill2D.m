classdef Fill2D
%FILL2D Raster polygon filling with the even-odd scanline algorithm.
%
%   The implementation builds the polygon edge list, intersects each integer
%   scanline with the active edges and fills the spans between sorted
%   intersection pairs. The half-open rule yMin <= y < yMax counts shared
%   vertices exactly once. No built-in fill, polygon or inpolygon function is
%   used to compute the filled pixels.

    methods (Static)
        function result = scanline(vertices)
            %SCANLINE Fill a closed polygon with the even-odd scanline rule.
            %
            %   Horizontal edges are skipped. Intersections are sorted per
            %   scanline and pixels from ceil(xLeft) to floor(xRight) are
            %   filled, so boundary pixels are included. Self-intersecting
            %   polygons follow the even-odd rule. A collinear polygon returns
            %   an empty fill instead of failing.
            arguments
                vertices (:,2) double
            end

            if size(vertices, 1) < 3
                error('cmr:graphics:Fill2D:invalidPolygon', ...
                    'Scanline fill needs at least three vertices.');
            end
            if ~isreal(vertices) || any(~isfinite(vertices(:)))
                error('cmr:graphics:Fill2D:invalidCoordinates', ...
                    'Polygon vertices must be finite real numbers.');
            end

            vertices = round(vertices);
            if size(unique(vertices, 'rows'), 1) < 3
                error('cmr:graphics:Fill2D:degeneratePolygon', ...
                    'Scanline fill needs at least three distinct vertices.');
            end

            % A polygon has no interior exactly when every vertex lies on a
            % single line, so the degeneracy test is collinearity, not area.
            % Coordinates are rounded integers, so this cross-product test is
            % exact. The net shoelace area must not be used here: a symmetric
            % self-intersecting polygon (for example a bow-tie) traverses one
            % lobe positively and the other negatively, so the signed areas
            % cancel and the net area is zero even though the even-odd rule
            % gives two fillable regions.
            centred = vertices - vertices(1, :);
            [~, pivot] = max(sum(centred .^ 2, 2));
            reference = centred(pivot, :);
            collinear = all(centred(:, 1) * reference(2) ...
                - centred(:, 2) * reference(1) == 0);
            if collinear
                result = struct( ...
                    'algorithm', "SCANLINE", ...
                    'vertices', vertices, ...
                    'points', zeros(0, 2), ...
                    'pixelCount', 0, ...
                    'scanlineCount', 0, ...
                    'edgeCount', size(vertices, 1));
                return
            end

            minY = min(vertices(:, 2));
            maxY = max(vertices(:, 2));
            maxSpan = max(vertices(:, 1)) - min(vertices(:, 1)) + 1;
            maxRows = max(0, maxY - minY);
            capacity = maxRows * max(1, maxSpan);

            filled = zeros(capacity, 2);
            filledCount = 0;
            scanlineCount = 0;

            for y = ceil(minY):(ceil(maxY) - 1)
                intersections = zeros(0, 1);
                vertexCount = size(vertices, 1);

                for index = 1:vertexCount
                    startPoint = vertices(index, :);
                    endPoint = vertices(mod(index, vertexCount) + 1, :);

                    if startPoint(2) == endPoint(2)
                        continue
                    end
                    if startPoint(2) > endPoint(2)
                        temporary = startPoint;
                        startPoint = endPoint;
                        endPoint = temporary;
                    end

                    % Half-open rule: shared vertices are counted once.
                    if y >= startPoint(2) && y < endPoint(2)
                        x = startPoint(1) ...
                            + (y - startPoint(2)) ...
                            * (endPoint(1) - startPoint(1)) ...
                            / (endPoint(2) - startPoint(2));
                        intersections(end + 1, 1) = x; %#ok<AGROW>
                    end
                end

                intersections = sort(intersections);
                scanlineCount = scanlineCount + 1;

                for pair = 1:2:(numel(intersections) - 1)
                    xStart = ceil(intersections(pair));
                    xEnd = floor(intersections(pair + 1));
                    if xEnd < xStart
                        continue
                    end
                    span = (xStart:xEnd)';
                    rowCount = numel(span);
                    filled(filledCount + (1:rowCount), 1) = span;
                    filled(filledCount + (1:rowCount), 2) = y;
                    filledCount = filledCount + rowCount;
                end
            end

            filled = filled(1:filledCount, :);
            points = unique(filled, 'rows', 'stable');

            result = struct( ...
                'algorithm', "SCANLINE", ...
                'vertices', vertices, ...
                'points', points, ...
                'pixelCount', size(points, 1), ...
                'scanlineCount', scanlineCount, ...
                'edgeCount', size(vertices, 1));
        end
    end
end
