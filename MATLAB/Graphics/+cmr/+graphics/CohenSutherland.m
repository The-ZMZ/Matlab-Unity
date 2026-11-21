classdef CohenSutherland
%COHENSUTHERLAND Manual Cohen-Sutherland line clipping (Issue #006).
%
%   Implements the classic Cohen-Sutherland algorithm with explicit 4-bit
%   region codes. No toolbox clipping function is used; every step (code
%   generation, trivial accept, trivial reject, edge intersection) is
%   implemented here.
%
%   Window and coordinates:
%     window = [xmin ymin xmax ymax]
%     The coordinate system is the standard mathematical 2D one used by
%     cmr.graphics.Transform2D: x right, y up. "Bottom" therefore means
%     y < ymin and "Top" means y > ymax.
%
%   Region codes (bit values):
%     0000 (0)  INSIDE  xmin <= x <= xmax and ymin <= y <= ymax
%     0001 (1)  LEFT    x < xmin
%     0010 (2)  RIGHT   x > xmax
%     0100 (4)  BOTTOM  y < ymin
%     1000 (8)  TOP     y > ymax
%
%   Points exactly on a boundary edge or corner are INSIDE (code 0), so the
%   window is treated as a closed region.
%
%   Algorithm per segment (at most a few iterations):
%     1. compute both region codes;
%     2. code0 OR code1 == 0        -> trivially accept, segment is visible;
%     3. code0 AND code1 ~= 0       -> trivially reject, segment is invisible;
%     4. otherwise pick one outside endpoint and one set bit
%        (priority TOP, BOTTOM, RIGHT, LEFT), replace that endpoint with the
%        intersection against the corresponding window edge, recompute its
%        code and repeat.
%
%   Intersections use the parametric form of the segment:
%     vertical edge   x = xe : y = y0 + (y1 - y0) * (xe - x0) / (x1 - x0)
%     horizontal edge y = ye : x = x0 + (x1 - x0) * (ye - y0) / (y1 - y0)
%   A chosen endpoint always differs from its partner on the clipped axis
%   (otherwise the segment would have been trivially rejected), and a zero
%   denominator is still guarded explicitly so no division-by-zero warning
%   can ever escape for vertical or horizontal segments.
%
%   Complexity: O(1) per segment, O(n) for an n-vertex polyline.

    properties (Constant)
        Inside = 0;
        Left   = 1;
        Right  = 2;
        Bottom = 4;
        Top    = 8;
    end

    methods (Static)
        function code = outCode(point, window)
            %OUTCODE Region code of one [x y] point against the window.
            w = cmr.graphics.CohenSutherland.mustBeWindow(window);
            p = cmr.graphics.CohenSutherland.mustBePoint(point, 'point');
            code = cmr.graphics.CohenSutherland.codeFor(p(1), p(2), w);
        end

        function codes = outCodes(points, window)
            %OUTCODES Region codes of an N-by-2 point matrix (N-by-1 result).
            w = cmr.graphics.CohenSutherland.mustBeWindow(window);
            P = cmr.graphics.CohenSutherland.mustBePointList(points, 'points', 1);
            codes = zeros(size(P, 1), 1);
            for i = 1:size(P, 1)
                codes(i) = cmr.graphics.CohenSutherland.codeFor(P(i,1), P(i,2), w);
            end
        end

        function [p0, p1, accepted] = clipLine(p0, p1, window)
            %CLIPLINE Clip one segment against the window.
            %
            %   [p0, p1, accepted] = clipLine(p0, p1, window) returns the
            %   visible part of the segment p0-p1. When the segment is fully
            %   outside, accepted is false and both points are [NaN NaN].
            %   A segment that only touches the window at one point or lies
            %   entirely inside is accepted.
            w = cmr.graphics.CohenSutherland.mustBeWindow(window);
            a = cmr.graphics.CohenSutherland.mustBePoint(p0, 'p0');
            b = cmr.graphics.CohenSutherland.mustBePoint(p1, 'p1');

            [x0, y0, x1, y1, accepted] = ...
                cmr.graphics.CohenSutherland.clipCore(a(1), a(2), b(1), b(2), w);

            if accepted
                p0 = [x0 y0];
                p1 = [x1 y1];
            else
                p0 = [NaN NaN];
                p1 = [NaN NaN];
            end
        end

        function segments = clipPolyline(points, window)
            %CLIPPOLYLINE Clip an N-by-2 polyline, returning visible segments.
            %
            %   segments = clipPolyline(points, window) clips every segment
            %   points(i,:)-points(i+1,:) and returns a K-by-4 matrix
            %   [x0 y0 x1 y1], one row per visible segment, in input order.
            %   Fully invisible segments are dropped, so K <= N-1. An empty
            %   result is 0-by-4.
            w = cmr.graphics.CohenSutherland.mustBeWindow(window);
            P = cmr.graphics.CohenSutherland.mustBePointList(points, 'points', 2);

            segments = zeros(0, 4);
            for i = 1:(size(P, 1) - 1)
                [x0, y0, x1, y1, accepted] = ...
                    cmr.graphics.CohenSutherland.clipCore(P(i,1), P(i,2), P(i+1,1), P(i+1,2), w);
                if accepted
                    segments(end + 1, :) = [x0 y0 x1 y1]; %#ok<AGROW>
                end
            end
        end
    end

    methods (Static, Access = private)
        function code = codeFor(x, y, w)
            %CODEFOR Build the 4-bit region code of a single coordinate pair.
            code = cmr.graphics.CohenSutherland.Inside;
            if x < w(1)
                code = code + cmr.graphics.CohenSutherland.Left;
            elseif x > w(3)
                code = code + cmr.graphics.CohenSutherland.Right;
            end
            if y < w(2)
                code = code + cmr.graphics.CohenSutherland.Bottom;
            elseif y > w(4)
                code = code + cmr.graphics.CohenSutherland.Top;
            end
        end

        function [x0, y0, x1, y1, accepted] = clipCore(x0, y0, x1, y1, w)
            %CLIPCORE Validate-free clipping loop shared by line and polyline.
            code0 = cmr.graphics.CohenSutherland.codeFor(x0, y0, w);
            code1 = cmr.graphics.CohenSutherland.codeFor(x1, y1, w);

            accepted = false;
            converged = false;
            % Four boundary clips per segment is the theoretical maximum; the
            % extra iterations only absorb floating-point re-entry.
            maxIterations = 12;

            for iteration = 1:maxIterations %#ok<NASGU>
                if bitor(code0, code1) == cmr.graphics.CohenSutherland.Inside
                    accepted = true;
                    converged = true;
                    break
                end

                if bitand(code0, code1) ~= 0
                    accepted = false;
                    converged = true;
                    break
                end

                if code0 ~= 0
                    useFirst = true;
                    code = code0;
                else
                    useFirst = false;
                    code = code1;
                end

                % Priority TOP -> BOTTOM -> RIGHT -> LEFT.
                if bitand(code, cmr.graphics.CohenSutherland.Top) ~= 0
                    denominator = y1 - y0;
                    if denominator == 0
                        converged = true;
                        break
                    end
                    yEdge = w(4);
                    x = x0 + (x1 - x0) * (yEdge - y0) / denominator;
                    if useFirst
                        x0 = x; y0 = yEdge;
                        code0 = cmr.graphics.CohenSutherland.codeFor(x0, y0, w);
                    else
                        x1 = x; y1 = yEdge;
                        code1 = cmr.graphics.CohenSutherland.codeFor(x1, y1, w);
                    end
                elseif bitand(code, cmr.graphics.CohenSutherland.Bottom) ~= 0
                    denominator = y1 - y0;
                    if denominator == 0
                        converged = true;
                        break
                    end
                    yEdge = w(2);
                    x = x0 + (x1 - x0) * (yEdge - y0) / denominator;
                    if useFirst
                        x0 = x; y0 = yEdge;
                        code0 = cmr.graphics.CohenSutherland.codeFor(x0, y0, w);
                    else
                        x1 = x; y1 = yEdge;
                        code1 = cmr.graphics.CohenSutherland.codeFor(x1, y1, w);
                    end
                elseif bitand(code, cmr.graphics.CohenSutherland.Right) ~= 0
                    denominator = x1 - x0;
                    if denominator == 0
                        converged = true;
                        break
                    end
                    xEdge = w(3);
                    y = y0 + (y1 - y0) * (xEdge - x0) / denominator;
                    if useFirst
                        x0 = xEdge; y0 = y;
                        code0 = cmr.graphics.CohenSutherland.codeFor(x0, y0, w);
                    else
                        x1 = xEdge; y1 = y;
                        code1 = cmr.graphics.CohenSutherland.codeFor(x1, y1, w);
                    end
                else
                    % Only LEFT remains.
                    denominator = x1 - x0;
                    if denominator == 0
                        converged = true;
                        break
                    end
                    xEdge = w(1);
                    y = y0 + (y1 - y0) * (xEdge - x0) / denominator;
                    if useFirst
                        x0 = xEdge; y0 = y;
                        code0 = cmr.graphics.CohenSutherland.codeFor(x0, y0, w);
                    else
                        x1 = xEdge; y1 = y;
                        code1 = cmr.graphics.CohenSutherland.codeFor(x1, y1, w);
                    end
                end
            end

            if ~converged
                error('cmr:graphics:CohenSutherland:clippingFailed', ...
                    'Cohen-Sutherland clipping did not converge.');
            end
        end

        function w = mustBeWindow(window)
            %MUSTBEWINDOW Validate [xmin ymin xmax ymax] and return a 1x4 row.
            if ~(isnumeric(window) && isreal(window) && numel(window) == 4 ...
                    && all(isfinite(window(:))))
                error('cmr:graphics:CohenSutherland:invalidWindow', ...
                    'window must be a finite numeric [xmin ymin xmax ymax].');
            end
            w = double(window(:))';
            if ~(w(1) < w(3))
                error('cmr:graphics:CohenSutherland:invalidWindow', ...
                    'window requires xmin < xmax.');
            end
            if ~(w(2) < w(4))
                error('cmr:graphics:CohenSutherland:invalidWindow', ...
                    'window requires ymin < ymax.');
            end
        end

        function p = mustBePoint(value, name)
            %MUSTBEPOINT Validate a single [x y] point and return a 1x2 row.
            if ~(isnumeric(value) && isreal(value) && numel(value) == 2 ...
                    && all(isfinite(value(:))))
                error('cmr:graphics:CohenSutherland:invalidPoint', ...
                    '%s must be a finite numeric [x y] point.', name);
            end
            p = double(value(:))';
        end

        function P = mustBePointList(points, name, minRows)
            %MUSTBEPOINTLIST Validate an N-by-2 point matrix.
            if ~(isnumeric(points) && isreal(points) && ismatrix(points) ...
                    && size(points, 2) == 2 && size(points, 1) >= minRows ...
                    && all(isfinite(points(:))))
                error('cmr:graphics:CohenSutherland:invalidPoint', ...
                    '%s must be a finite numeric N-by-2 matrix with at least %d row(s).', ...
                    name, minRows);
            end
            P = double(points);
        end
    end
end
