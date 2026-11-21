classdef Shape2D
%SHAPE2D Raster outlines for rectangles, triangles and polygons.
%
%   Shapes are built exclusively from the project's own Bresenham line
%   algorithm. Vertices are integer pixel centres; each shape stores its
%   closed outline as pixel coordinates and an RGB colour in [0, 1].

    methods (Static)
        function shape = rectangle(x0, y0, x1, y1, color)
            %RECTANGLE Build a rectangle outline from two opposite corners.
            arguments
                x0 (1,1) double
                y0 (1,1) double
                x1 (1,1) double
                y1 (1,1) double
                color (1,3) double = [0, 0, 0]
            end

            values = [x0, y0, x1, y1];
            if ~isreal(values) || any(~isfinite(values))
                error('cmr:graphics:Shape2D:invalidCoordinates', ...
                    'Rectangle corner coordinates must be finite real numbers.');
            end

            left = min(x0, x1);
            right = max(x0, x1);
            top = min(y0, y1);
            bottom = max(y0, y1);

            vertices = [ ...
                left, top; ...
                right, top; ...
                right, bottom; ...
                left, bottom];

            shape = cmr.graphics.Shape2D.buildShape("rectangle", vertices, color);
        end

        function shape = triangle(p1, p2, p3, color)
            %TRIANGLE Build a triangle outline from three vertices.
            arguments
                p1 (1,2) double
                p2 (1,2) double
                p3 (1,2) double
                color (1,3) double = [0, 0, 0]
            end

            vertices = [p1; p2; p3];
            if ~isreal(vertices) || any(~isfinite(vertices(:)))
                error('cmr:graphics:Shape2D:invalidCoordinates', ...
                    'Triangle coordinates must be finite real numbers.');
            end

            shape = cmr.graphics.Shape2D.buildShape("triangle", vertices, color);
        end

        function shape = polygon(vertices, color)
            %POLYGON Build a closed polygon outline from a vertex list.
            arguments
                vertices (:,2) double
                color (1,3) double = [0, 0, 0]
            end

            if size(vertices, 1) < 3
                error('cmr:graphics:Shape2D:invalidPolygon', ...
                    'A polygon needs at least three vertices.');
            end
            if ~isreal(vertices) || any(~isfinite(vertices(:)))
                error('cmr:graphics:Shape2D:invalidCoordinates', ...
                    'Polygon coordinates must be finite real numbers.');
            end

            shape = cmr.graphics.Shape2D.buildShape("polygon", vertices, color);
        end
    end

    methods (Static, Access = private)
        function shape = buildShape(name, vertices, color)
            cmr.graphics.Shape2D.checkColor(color);

            vertices = round(vertices);
            if size(unique(vertices, 'rows'), 1) < 3
                error('cmr:graphics:Shape2D:invalidPolygon', ...
                    'A polygon needs at least three distinct vertices.');
            end

            vertexCount = size(vertices, 1);
            closed = [vertices; vertices(1, :)];

            outlinePoints = zeros(0, 2);
            for index = 1:vertexCount
                segment = cmr.graphics.Line2D.bresenham( ...
                    closed(index, 1), closed(index, 2), ...
                    closed(index + 1, 1), closed(index + 1, 2));

                segmentPoints = segment.points;
                if index > 1
                    % The first pixel of a new edge repeats the shared corner.
                    segmentPoints = segmentPoints(2:end, :);
                end
                outlinePoints = [outlinePoints; segmentPoints]; %#ok<AGROW>
            end

            % Returning to the first vertex duplicates it; keep a closed loop
            % without a repeated endpoint.
            outlinePoints = unique(outlinePoints, 'rows', 'stable');

            shape = struct( ...
                'name', name, ...
                'vertices', vertices, ...
                'vertexCount', vertexCount, ...
                'outlinePoints', outlinePoints, ...
                'color', color);
        end

        function checkColor(color)
            if ~isreal(color) || any(~isfinite(color)) ...
                    || any(color < 0) || any(color > 1)
                error('cmr:graphics:Shape2D:invalidColor', ...
                    'Shape colour must contain three RGB components in [0, 1].');
            end
        end
    end
end
