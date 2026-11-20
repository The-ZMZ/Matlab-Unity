classdef tCohenSutherland < matlab.unittest.TestCase
%TCOHENSUTHERLAND Unit tests for manual Cohen-Sutherland line clipping.
%
%   Window used by almost every test: [0 0 10 10] (xmin ymin xmax ymax).
%   Coordinate system is y-up, so BOTTOM is y < ymin and TOP is y > ymax.

    properties (Constant)
        Window = [0 0 10 10]
    end

    methods (TestClassSetup)
        function addProjectPath(testCase)
            % The cmr.graphics package lives under MATLAB/Graphics, so that
            % folder's parent must also be on the path for +cmr resolution.
            root = fileparts(fileparts(mfilename('fullpath')));
            graphicsRoot = fullfile(root, 'Graphics');
            testCase.applyFixture( ...
                matlab.unittest.fixtures.PathFixture({root, graphicsRoot}));
        end
    end

    methods (Test)
        % ---------------------------------------------------------------
        % Region codes
        % ---------------------------------------------------------------
        function outCodeCentreIsInside(testCase)
            code = cmr.graphics.CohenSutherland.outCode([5 5], testCase.Window);
            testCase.verifyEqual(code, cmr.graphics.CohenSutherland.Inside);
        end

        function outCodeOnBoundaryIsInside(testCase)
            % Edges and corners are part of the closed window.
            boundary = [0 5; 10 5; 5 0; 5 10; 0 0; 10 0; 0 10; 10 10];
            for i = 1:size(boundary, 1)
                code = cmr.graphics.CohenSutherland.outCode(boundary(i,:), testCase.Window);
                testCase.verifyEqual(code, 0, ...
                    sprintf('Boundary point (%g,%g) must be INSIDE.', ...
                        boundary(i,1), boundary(i,2)));
            end
        end

        function outCodeSingleRegions(testCase)
            cs = cmr.graphics.CohenSutherland;
            testCase.verifyEqual(cs.outCode([-1 5], testCase.Window), cs.Left);
            testCase.verifyEqual(cs.outCode([11 5], testCase.Window), cs.Right);
            testCase.verifyEqual(cs.outCode([5 -1], testCase.Window), cs.Bottom);
            testCase.verifyEqual(cs.outCode([5 11], testCase.Window), cs.Top);
        end

        function outCodeCornerRegions(testCase)
            cs = cmr.graphics.CohenSutherland;
            testCase.verifyEqual(cs.outCode([-1 -1], testCase.Window), cs.Left + cs.Bottom);
            testCase.verifyEqual(cs.outCode([11 -1], testCase.Window), cs.Right + cs.Bottom);
            testCase.verifyEqual(cs.outCode([-1 11], testCase.Window), cs.Left + cs.Top);
            testCase.verifyEqual(cs.outCode([11 11], testCase.Window), cs.Right + cs.Top);
        end

        function outCodesVectorizedMatchesScalar(testCase)
            cs = cmr.graphics.CohenSutherland;
            points = [5 5; -1 5; 11 5; 5 -1; 5 11; -1 -1];
            expected = [0; 1; 2; 4; 8; 5];
            codes = cs.outCodes(points, testCase.Window);
            testCase.verifySize(codes, [6 1]);
            testCase.verifyEqual(codes, expected);
            for i = 1:size(points, 1)
                testCase.verifyEqual(codes(i), cs.outCode(points(i,:), testCase.Window));
            end
        end

        % ---------------------------------------------------------------
        % Trivial accept / trivial reject
        % ---------------------------------------------------------------
        function fullyInsideSegmentUnchanged(testCase)
            [a, b, accepted] = cmr.graphics.CohenSutherland.clipLine( ...
                [2 2], [8 8], testCase.Window);
            testCase.verifyTrue(accepted);
            testCase.verifyEqual(a, [2 2]);
            testCase.verifyEqual(b, [8 8]);
        end

        function fullyOutsideOnEachSideRejected(testCase)
            cases = {
                [-10 2], [-1 8];     % entirely left
                [11 2],  [20 8];     % entirely right
                [2 -12], [8 -5];     % entirely below (BOTTOM)
                [2 12],  [8 15]      % entirely above (TOP)
            };
            for i = 1:size(cases, 1)
                [a, b, accepted] = cmr.graphics.CohenSutherland.clipLine( ...
                    cases{i,1}, cases{i,2}, testCase.Window);
                testCase.verifyFalse(accepted, ...
                    sprintf('Segment %d must be rejected.', i));
                testCase.verifyEqual(a, [NaN NaN]);
                testCase.verifyEqual(b, [NaN NaN]);
            end
        end

        function rejectsSegmentThatStraddlesACorner(testCase)
            % Codes LEFT (1) and TOP (8) share no bit, so trivial reject does
            % not fire. The line passes above the window corner (0,10) and
            % must still be rejected after the LEFT clip.
            [a, b, accepted] = cmr.graphics.CohenSutherland.clipLine( ...
                [-5 3], [3 15], testCase.Window);
            testCase.verifyFalse(accepted);
            testCase.verifyEqual(a, [NaN NaN]);
            testCase.verifyEqual(b, [NaN NaN]);
        end

        % ---------------------------------------------------------------
        % Intersections with the four boundaries
        % ---------------------------------------------------------------
        function clipsLeftEdge(testCase)
            [a, b, accepted] = cmr.graphics.CohenSutherland.clipLine( ...
                [-5 5], [5 5], testCase.Window);
            testCase.verifyTrue(accepted);
            testCase.verifyEqual(a, [0 5], 'AbsTol', 1e-12);
            testCase.verifyEqual(b, [5 5], 'AbsTol', 1e-12);
        end

        function clipsRightEdge(testCase)
            [a, b, accepted] = cmr.graphics.CohenSutherland.clipLine( ...
                [5 5], [15 5], testCase.Window);
            testCase.verifyTrue(accepted);
            testCase.verifyEqual(a, [5 5], 'AbsTol', 1e-12);
            testCase.verifyEqual(b, [10 5], 'AbsTol', 1e-12);
        end

        function clipsBottomEdge(testCase)
            % y-up convention: BOTTOM is y = ymin.
            [a, b, accepted] = cmr.graphics.CohenSutherland.clipLine( ...
                [5 5], [5 -5], testCase.Window);
            testCase.verifyTrue(accepted);
            testCase.verifyEqual(a, [5 5], 'AbsTol', 1e-12);
            testCase.verifyEqual(b, [5 0], 'AbsTol', 1e-12);
        end

        function clipsTopEdge(testCase)
            [a, b, accepted] = cmr.graphics.CohenSutherland.clipLine( ...
                [5 5], [5 15], testCase.Window);
            testCase.verifyTrue(accepted);
            testCase.verifyEqual(a, [5 5], 'AbsTol', 1e-12);
            testCase.verifyEqual(b, [5 10], 'AbsTol', 1e-12);
        end

        function clipsDiagonalAcrossBothCorners(testCase)
            [a, b, accepted] = cmr.graphics.CohenSutherland.clipLine( ...
                [-2 -2], [12 12], testCase.Window);
            testCase.verifyTrue(accepted);
            testCase.verifyEqual(a, [0 0], 'AbsTol', 1e-12);
            testCase.verifyEqual(b, [10 10], 'AbsTol', 1e-12);
        end

        function clipsDiagonalEnteringAtOppositeCorners(testCase)
            [a, b, accepted] = cmr.graphics.CohenSutherland.clipLine( ...
                [-5 -5], [5 5], testCase.Window);
            testCase.verifyTrue(accepted);
            testCase.verifyEqual(a, [0 0], 'AbsTol', 1e-12);
            testCase.verifyEqual(b, [5 5], 'AbsTol', 1e-12);
        end

        function touchingCornerYieldsDegenerateVisibleSegment(testCase)
            % Line y = x + 10 only touches the window at corner (0,10).
            [a, b, accepted] = cmr.graphics.CohenSutherland.clipLine( ...
                [-10 0], [0 10], testCase.Window);
            testCase.verifyTrue(accepted);
            testCase.verifyEqual(a, [0 10], 'AbsTol', 1e-12);
            testCase.verifyEqual(b, [0 10], 'AbsTol', 1e-12);
        end

        % ---------------------------------------------------------------
        % Vertical / horizontal segments (division guards)
        % ---------------------------------------------------------------
        function verticalLineOutsideWindowRejected(testCase)
            [a, b, accepted] = cmr.graphics.CohenSutherland.clipLine( ...
                [15 0], [15 10], testCase.Window);
            testCase.verifyFalse(accepted);
            testCase.verifyEqual(a, [NaN NaN]);
            testCase.verifyEqual(b, [NaN NaN]);
        end

        function verticalLineClipsBothHorizontalEdges(testCase)
            [a, b, accepted] = cmr.graphics.CohenSutherland.clipLine( ...
                [5 -3], [5 13], testCase.Window);
            testCase.verifyTrue(accepted);
            testCase.verifyEqual(a, [5 0], 'AbsTol', 1e-12);
            testCase.verifyEqual(b, [5 10], 'AbsTol', 1e-12);
        end

        function horizontalLineClipsBothVerticalEdges(testCase)
            [a, b, accepted] = cmr.graphics.CohenSutherland.clipLine( ...
                [-3 5], [13 5], testCase.Window);
            testCase.verifyTrue(accepted);
            testCase.verifyEqual(a, [0 5], 'AbsTol', 1e-12);
            testCase.verifyEqual(b, [10 5], 'AbsTol', 1e-12);
        end

        function horizontalLineOutsideWindowRejected(testCase)
            [a, b, accepted] = cmr.graphics.CohenSutherland.clipLine( ...
                [-5 15], [15 15], testCase.Window);
            testCase.verifyFalse(accepted);
            testCase.verifyEqual(a, [NaN NaN]);
            testCase.verifyEqual(b, [NaN NaN]);
        end

        % ---------------------------------------------------------------
        % Degenerate and boundary segments
        % ---------------------------------------------------------------
        function zeroLengthInsideAccepted(testCase)
            [a, b, accepted] = cmr.graphics.CohenSutherland.clipLine( ...
                [5 5], [5 5], testCase.Window);
            testCase.verifyTrue(accepted);
            testCase.verifyEqual(a, [5 5]);
            testCase.verifyEqual(b, [5 5]);
        end

        function zeroLengthOutsideRejected(testCase)
            [~, ~, accepted] = cmr.graphics.CohenSutherland.clipLine( ...
                [50 50], [50 50], testCase.Window);
            testCase.verifyFalse(accepted);
        end

        function endpointOnBoundaryIsInclusive(testCase)
            [a, b, accepted] = cmr.graphics.CohenSutherland.clipLine( ...
                [0 5], [20 5], testCase.Window);
            testCase.verifyTrue(accepted);
            testCase.verifyEqual(a, [0 5], 'AbsTol', 1e-12);
            testCase.verifyEqual(b, [10 5], 'AbsTol', 1e-12);
        end

        % ---------------------------------------------------------------
        % Polyline clipping
        % ---------------------------------------------------------------
        function clipPolylineKeepsOnlyVisibleSegments(testCase)
            points = [1 1; 5 5; 9 9; 15 15; 15 5];
            segments = cmr.graphics.CohenSutherland.clipPolyline( ...
                points, testCase.Window);
            expected = [1 1 5 5; 5 5 9 9; 9 9 10 10];
            testCase.verifySize(segments, [3 4]);
            testCase.verifyEqual(segments, expected, 'AbsTol', 1e-12);
        end

        function clipPolylineFullyOutsideIsEmpty(testCase)
            % A polygon that completely encloses the window: every edge lies
            % outside, so no segment is visible.
            points = [-5 -5; 15 -5; 15 15; -5 15; -5 -5];
            segments = cmr.graphics.CohenSutherland.clipPolyline( ...
                points, testCase.Window);
            testCase.verifySize(segments, [0 4]);
            testCase.verifyEmpty(segments);
        end

        function clipPolylinePartiallyOutsideTriangle(testCase)
            points = [0 0; 12 0; 0 12; 0 0];
            segments = cmr.graphics.CohenSutherland.clipPolyline( ...
                points, testCase.Window);
            expected = [0 0 10 0; 10 2 2 10; 0 10 0 0];
            testCase.verifySize(segments, [3 4]);
            testCase.verifyEqual(segments, expected, 'AbsTol', 1e-12);
        end

        % ---------------------------------------------------------------
        % Validation
        % ---------------------------------------------------------------
        function invalidWindowThrows(testCase)
            cs = cmr.graphics.CohenSutherland;
            testCase.verifyError(@() cs.outCode([1 1], [0 0 0 10]), ...
                'cmr:graphics:CohenSutherland:invalidWindow');       % xmin == xmax
            testCase.verifyError(@() cs.outCode([1 1], [10 0 5 10]), ...
                'cmr:graphics:CohenSutherland:invalidWindow');       % xmin > xmax
            testCase.verifyError(@() cs.outCode([1 1], [0 5 10 5]), ...
                'cmr:graphics:CohenSutherland:invalidWindow');       % ymin == ymax
            testCase.verifyError(@() cs.outCode([1 1], [0 0 10 Inf]), ...
                'cmr:graphics:CohenSutherland:invalidWindow');       % non-finite
            testCase.verifyError(@() cs.outCode([1 1], [0 0 10 20 30]), ...
                'cmr:graphics:CohenSutherland:invalidWindow');       % wrong length
            testCase.verifyError(@() cs.clipLine([1 1], [2 2], "abc"), ...
                'cmr:graphics:CohenSutherland:invalidWindow');       % non-numeric
        end

        function invalidPointThrows(testCase)
            cs = cmr.graphics.CohenSutherland;
            testCase.verifyError(@() cs.outCode([1 2 3], testCase.Window), ...
                'cmr:graphics:CohenSutherland:invalidPoint');
            testCase.verifyError(@() cs.outCode([NaN 2], testCase.Window), ...
                'cmr:graphics:CohenSutherland:invalidPoint');
            testCase.verifyError(@() cs.clipLine([1 1], [2 NaN], testCase.Window), ...
                'cmr:graphics:CohenSutherland:invalidPoint');
        end

        function invalidPolylineThrows(testCase)
            cs = cmr.graphics.CohenSutherland;
            testCase.verifyError(@() cs.clipPolyline([1 1], testCase.Window), ...
                'cmr:graphics:CohenSutherland:invalidPoint');          % single point
            testCase.verifyError(@() cs.clipPolyline([1 1 1; 2 2 2], testCase.Window), ...
                'cmr:graphics:CohenSutherland:invalidPoint');          % N-by-3
            testCase.verifyError(@() cs.clipPolyline([1 1; Inf 2], testCase.Window), ...
                'cmr:graphics:CohenSutherland:invalidPoint');          % non-finite
        end
    end
end
