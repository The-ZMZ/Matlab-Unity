classdef tTransform2D < matlab.unittest.TestCase
%TTRANSFORM2D Unit tests for 2D homogeneous transformations (Issue #006).

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
        function identityIsEye(testCase)
            T = cmr.graphics.Transform2D.identity();
            testCase.verifyEqual(T, eye(3));
            testCase.verifyEqual(T(3,:), [0 0 1]);
        end

        function identityLeavesPointsUnchanged(testCase)
            T = cmr.graphics.Transform2D.identity();
            P = [1 2; -3 4; 0 0];
            Q = cmr.graphics.Transform2D.apply(T, P);
            testCase.verifyEqual(Q, P);
        end

        function translationMatrixAndApplication(testCase)
            T = cmr.graphics.Transform2D.translation(3, -2);
            testCase.verifyEqual(T, [1 0 3; 0 1 -2; 0 0 1]);
            Q = cmr.graphics.Transform2D.apply(T, [1 1; 0 0]);
            testCase.verifyEqual(Q, [4 -1; 3 -2], 'AbsTol', 1e-12);
        end

        function rotationQuarterTurnMapsAxes(testCase)
            T = cmr.graphics.Transform2D.rotation(pi/2);
            Q = cmr.graphics.Transform2D.apply(T, [1 0; 0 1]);
            testCase.verifyEqual(Q, [0 1; -1 0], 'AbsTol', 1e-12);
        end

        function rotationDegreesMatchesRadians(testCase)
            Tr = cmr.graphics.Transform2D.rotation(pi/4);
            Td = cmr.graphics.Transform2D.rotationDegrees(45);
            testCase.verifyEqual(Td, Tr, 'AbsTol', 1e-12);
        end

        function rotationFullTurnIsIdentity(testCase)
            T = cmr.graphics.Transform2D.rotation(2*pi);
            testCase.verifyEqual(T, eye(3), 'AbsTol', 1e-12);
        end

        function rotationIsOrthonormalWithUnitDeterminant(testCase)
            T = cmr.graphics.Transform2D.rotation(pi/3);
            R = T(1:2,1:2);
            testCase.verifyEqual(R'*R, eye(2), 'AbsTol', 1e-12);
            testCase.verifyEqual(det(T), 1, 'AbsTol', 1e-12);
        end

        function scalingMatrixAndApplication(testCase)
            T = cmr.graphics.Transform2D.scaling(2, 0.5);
            testCase.verifyEqual(T, [2 0 0; 0 0.5 0; 0 0 1]);
            Q = cmr.graphics.Transform2D.apply(T, [3 4]);
            testCase.verifyEqual(Q, [6 2], 'AbsTol', 1e-12);
            testCase.verifyEqual(det(T), 1, 'AbsTol', 1e-12);
        end

        function reflectionXFlipsY(testCase)
            T = cmr.graphics.Transform2D.reflectionX();
            Q = cmr.graphics.Transform2D.apply(T, [2 3]);
            testCase.verifyEqual(Q, [2 -3], 'AbsTol', 1e-12);
            testCase.verifyEqual(det(T), -1, 'AbsTol', 1e-12);
        end

        function reflectionYFlipsX(testCase)
            T = cmr.graphics.Transform2D.reflectionY();
            Q = cmr.graphics.Transform2D.apply(T, [2 3]);
            testCase.verifyEqual(Q, [-2 3], 'AbsTol', 1e-12);
            testCase.verifyEqual(det(T), -1, 'AbsTol', 1e-12);
        end

        function reflectionOriginNegatesBoth(testCase)
            T = cmr.graphics.Transform2D.reflectionOrigin();
            Q = cmr.graphics.Transform2D.apply(T, [2 3]);
            testCase.verifyEqual(Q, [-2 -3], 'AbsTol', 1e-12);
        end

        function reflectionLineYXSwapsCoordinates(testCase)
            T = cmr.graphics.Transform2D.reflectionLineYX();
            Q = cmr.graphics.Transform2D.apply(T, [2 5]);
            testCase.verifyEqual(Q, [5 2], 'AbsTol', 1e-12);
        end

        function reflectionLineYNegXSwapsAndNegates(testCase)
            T = cmr.graphics.Transform2D.reflectionLineYNegX();
            Q = cmr.graphics.Transform2D.apply(T, [2 5]);
            testCase.verifyEqual(Q, [-5 -2], 'AbsTol', 1e-12);
        end

        function reflectionAboutArbitraryLine(testCase)
            % Reflect (3,0) across the vertical line x=1 (line at 90 degrees through (1,0))
            T = cmr.graphics.Transform2D.reflectionAboutLine([1 0], pi/2);
            Q = cmr.graphics.Transform2D.apply(T, [3 0]);
            testCase.verifyEqual(Q, [-1 0], 'AbsTol', 1e-12);
            % Point on the line remains fixed
            Q2 = cmr.graphics.Transform2D.apply(T, [1 5]);
            testCase.verifyEqual(Q2, [1 5], 'AbsTol', 1e-12);
        end

        function shearXMapsPoint(testCase)
            T = cmr.graphics.Transform2D.shearX(2);
            testCase.verifyEqual(T, [1 2 0; 0 1 0; 0 0 1]);
            Q = cmr.graphics.Transform2D.apply(T, [1 3]);
            testCase.verifyEqual(Q, [7 3], 'AbsTol', 1e-12); % x' = 1 + 2*3
        end

        function shearYMapsPoint(testCase)
            T = cmr.graphics.Transform2D.shearY(2);
            testCase.verifyEqual(T, [1 0 0; 2 1 0; 0 0 1]);
            Q = cmr.graphics.Transform2D.apply(T, [3 1]);
            testCase.verifyEqual(Q, [3 7], 'AbsTol', 1e-12); % y' = 2*3 + 1
        end

        function composeAppliesInOrder(testCase)
            A = cmr.graphics.Transform2D.translation(2, 0);   % first
            B = cmr.graphics.Transform2D.scaling(2, 2);       % second
            M = cmr.graphics.Transform2D.compose(A, B);       % B*A
            Q = cmr.graphics.Transform2D.apply(M, [1 0]);
            testCase.verifyEqual(Q, [6 0], 'AbsTol', 1e-12);  % (1+2)*2
            % order matters
            Mrev = cmr.graphics.Transform2D.compose(B, A);    % A*B
            Qrev = cmr.graphics.Transform2D.apply(Mrev, [1 0]);
            testCase.verifyEqual(Qrev, [4 0], 'AbsTol', 1e-12); % 1*2 + 2
            testCase.verifyNotEqual(M, Mrev);
        end

        function composeWithNoArgsIsIdentity(testCase)
            M = cmr.graphics.Transform2D.compose();
            testCase.verifyEqual(M, eye(3));
        end

        function rotationAboutPivotKeepsPivotFixed(testCase)
            pivot = [2 1];
            R = cmr.graphics.Transform2D.rotation(pi/2);
            T = cmr.graphics.Transform2D.aboutPoint(R, pivot);
            Qpivot = cmr.graphics.Transform2D.apply(T, pivot);
            testCase.verifyEqual(Qpivot, pivot, 'AbsTol', 1e-12);
            % point (3,1) is 1 unit right of pivot -> becomes (2,2)
            Q = cmr.graphics.Transform2D.apply(T, [3 1]);
            testCase.verifyEqual(Q, [2 2], 'AbsTol', 1e-12);
        end

        function scalingAboutPivot(testCase)
            pivot = [1 1];
            S = cmr.graphics.Transform2D.scaling(2, 2);
            T = cmr.graphics.Transform2D.aboutPoint(S, pivot);
            Qpivot = cmr.graphics.Transform2D.apply(T, pivot);
            testCase.verifyEqual(Qpivot, pivot, 'AbsTol', 1e-12);
            Q = cmr.graphics.Transform2D.apply(T, [2 1]);
            testCase.verifyEqual(Q, [3 1], 'AbsTol', 1e-12);
        end

        function applyHandlesMultiplePoints(testCase)
            T = cmr.graphics.Transform2D.translation(1, 2);
            P = [0 0; 1 0; 1 1; 0 1];
            Q = cmr.graphics.Transform2D.apply(T, P);
            testCase.verifySize(Q, [4 2]);
            testCase.verifyEqual(Q, P + [1 2], 'AbsTol', 1e-12);
        end

        function applyPointMatchesApply(testCase)
            T = cmr.graphics.Transform2D.rotation(pi/6);
            p = [3 4];
            q1 = cmr.graphics.Transform2D.applyPoint(T, p);
            q2 = cmr.graphics.Transform2D.apply(T, p);
            testCase.verifyEqual(q1, q2, 'AbsTol', 1e-12);
        end

        function buildersReturnAffineMatrices(testCase)
            mats = {
                cmr.graphics.Transform2D.identity(), ...
                cmr.graphics.Transform2D.translation(1,2), ...
                cmr.graphics.Transform2D.rotation(0.3), ...
                cmr.graphics.Transform2D.scaling(2,3), ...
                cmr.graphics.Transform2D.reflectionX(), ...
                cmr.graphics.Transform2D.shearX(0.5)
            };
            for i = 1:numel(mats)
                M = mats{i};
                testCase.verifyEqual(size(M), [3 3]);
                testCase.verifyTrue(all(isfinite(M(:))));
                testCase.verifyEqual(M(3,:), [0 0 1], 'AbsTol', 0);
            end
        end

        function invalidInputsThrow(testCase)
            testCase.verifyError(@() cmr.graphics.Transform2D.translation(inf, 0), 'cmr:graphics:Transform2D:invalidTranslation');
            testCase.verifyError(@() cmr.graphics.Transform2D.rotation(nan), 'cmr:graphics:Transform2D:invalidAngle');
            testCase.verifyError(@() cmr.graphics.Transform2D.scaling('a', 1), 'cmr:graphics:Transform2D:invalidScale');
            testCase.verifyError(@() cmr.graphics.Transform2D.shearX(inf), 'cmr:graphics:Transform2D:invalidShear');
            testCase.verifyError(@() cmr.graphics.Transform2D.apply(eye(3), [1 2 3]), 'cmr:graphics:Transform2D:invalidPoints');
            testCase.verifyError(@() cmr.graphics.Transform2D.aboutPoint(eye(3), [1 2 3]), 'cmr:graphics:Transform2D:invalidPivot');
            testCase.verifyError(@() cmr.graphics.Transform2D.apply(ones(2,2), [1 1]), 'cmr:graphics:Transform2D:invalidMatrix');
            testCase.verifyError(@() cmr.graphics.Transform2D.apply([1 0 0; 0 1 0; 1 0 1], [1 1]), 'cmr:graphics:Transform2D:invalidMatrix');
        end
    end
end
