classdef tTransform3D < matlab.unittest.TestCase
%TTRANSFORM3D Unit tests for 3D homogeneous transformations (Issue #008).
%
%   The tests use the documented row-vector convention:
%
%       [X Y Z 1] = [x y z 1] * T
%
%   COMPOSE(T1,T2) returns T1*T2, so T1 is applied before T2.

    methods (TestClassSetup)
        function addProjectPath(testCase) %#ok<INUSD>
            root = fileparts(fileparts(mfilename('fullpath')));
            testCase.applyFixture( ...
                matlab.unittest.fixtures.PathFixture({ ...
                root, fullfile(root, 'Graphics')}));
        end
    end

    methods (Test)
        % ------------------------------------------------------------ basics
        function identityIsAffineFourByFour(testCase)
            T = cmr.graphics.Transform3D.identity();

            testCase.verifySize(T, [4 4]);
            testCase.verifyEqual(T, eye(4));
            testCase.verifyTrue(cmr.graphics.Transform3D.isAffine(T));
        end

        function identityLeavesPointsUnchanged(testCase)
            points = [1 2 3; -4 5 -6; 0 0 0];
            transformed = cmr.graphics.Transform3D.transformPoints( ...
                cmr.graphics.Transform3D.identity(), points);

            testCase.verifyEqual(transformed, points, 'AbsTol', 1e-12);
        end

        % ------------------------------------------------------- translation
        function translationMatrixUsesLastRowForRowVectors(testCase)
            T = cmr.graphics.Transform3D.translation(2, -3, 5);
            expected = [ ...
                 1  0  0  0; ...
                 0  1  0  0; ...
                 0  0  1  0; ...
                 2 -3  5  1];

            testCase.verifyEqual(T, expected);
        end

        function translationAcceptsVectorForm(testCase)
            separate = cmr.graphics.Transform3D.translation(2, -3, 5);
            vector = cmr.graphics.Transform3D.translation([2 -3 5]);

            testCase.verifyEqual(vector, separate);
        end

        function translationMovesEveryPointByTheSameOffset(testCase)
            points = [0 0 0; 1 2 3; -4 5 -6];
            expected = points + repmat([2 -3 5], size(points, 1), 1);
            T = cmr.graphics.Transform3D.translation(2, -3, 5);

            transformed = cmr.graphics.Transform3D.transformPoints(T, points);

            testCase.verifyEqual(transformed, expected, 'AbsTol', 1e-12);
        end

        function translationRejectsInvalidInput(testCase)
            testCase.verifyError( ...
                @() cmr.graphics.Transform3D.translation(1, 2), ...
                'cmr:graphics:Transform3D:invalidTranslation');
            testCase.verifyError( ...
                @() cmr.graphics.Transform3D.translation([1 2]), ...
                'cmr:graphics:Transform3D:invalidTranslation');
            testCase.verifyError( ...
                @() cmr.graphics.Transform3D.translation(1, NaN, 3), ...
                'cmr:graphics:Transform3D:invalidTranslation');
            testCase.verifyError( ...
                @() cmr.graphics.Transform3D.translation('a', 'b', 'c'), ...
                'cmr:graphics:Transform3D:invalidTranslation');
        end

        % ---------------------------------------------------------- rotation
        function rotationXQuarterTurnUsesRightHandRule(testCase)
            T = cmr.graphics.Transform3D.rotationX(pi / 2);
            expected = [ ...
                 1  0  0  0; ...
                 0  0  1  0; ...
                 0 -1  0  0; ...
                 0  0  0  1];

            testCase.verifyEqual(T, expected, 'AbsTol', 1e-12);
            testCase.verifyEqual( ...
                cmr.graphics.Transform3D.transformPoints(T, [0 1 0]), ...
                [0 0 1], 'AbsTol', 1e-12);
        end

        function rotationYQuarterTurnUsesRightHandRule(testCase)
            T = cmr.graphics.Transform3D.rotationY(pi / 2);
            expected = [ ...
                 0  0 -1  0; ...
                 0  1  0  0; ...
                 1  0  0  0; ...
                 0  0  0  1];

            testCase.verifyEqual(T, expected, 'AbsTol', 1e-12);
            testCase.verifyEqual( ...
                cmr.graphics.Transform3D.transformPoints(T, [0 0 1]), ...
                [1 0 0], 'AbsTol', 1e-12);
        end

        function rotationZQuarterTurnUsesRightHandRule(testCase)
            T = cmr.graphics.Transform3D.rotationZ(pi / 2);
            expected = [ ...
                 0  1  0  0; ...
                -1  0  0  0; ...
                 0  0  1  0; ...
                 0  0  0  1];

            testCase.verifyEqual(T, expected, 'AbsTol', 1e-12);
            testCase.verifyEqual( ...
                cmr.graphics.Transform3D.transformPoints(T, [1 0 0]), ...
                [0 1 0], 'AbsTol', 1e-12);
        end

        function rotationsAcceptDegrees(testCase)
            angles = pi / 2;

            testCase.verifyEqual( ...
                cmr.graphics.Transform3D.rotationX(90, 'degrees'), ...
                cmr.graphics.Transform3D.rotationX(angles), 'AbsTol', 1e-12);
            testCase.verifyEqual( ...
                cmr.graphics.Transform3D.rotationY(90, "deg"), ...
                cmr.graphics.Transform3D.rotationY(angles), 'AbsTol', 1e-12);
            testCase.verifyEqual( ...
                cmr.graphics.Transform3D.rotationZ(90, "DEGREES"), ...
                cmr.graphics.Transform3D.rotationZ(angles), 'AbsTol', 1e-12);
        end

        function rotationsRejectInvalidAnglesAndUnits(testCase)
            testCase.verifyError( ...
                @() cmr.graphics.Transform3D.rotationX(NaN), ...
                'cmr:graphics:Transform3D:invalidAngle');
            testCase.verifyError( ...
                @() cmr.graphics.Transform3D.rotationY([1 2]), ...
                'cmr:graphics:Transform3D:invalidAngle');
            testCase.verifyError( ...
                @() cmr.graphics.Transform3D.rotationZ(90, 'turns'), ...
                'cmr:graphics:Transform3D:invalidAngleUnit');
            testCase.verifyError( ...
                @() cmr.graphics.Transform3D.rotationX(90, 5), ...
                'cmr:graphics:Transform3D:invalidAngleUnit');
        end

        function rotationLinearPartsAreOrthonormal(testCase)
            constructors = {@() cmr.graphics.Transform3D.rotationX(0.7), ...
                            @() cmr.graphics.Transform3D.rotationY(-1.3), ...
                            @() cmr.graphics.Transform3D.rotationZ(2.1)};

            for index = 1:numel(constructors)
                R = constructors{index}();
                linear = R(1:3, 1:3);

                testCase.verifyEqual(linear * linear.', eye(3), ...
                    'AbsTol', 1e-12);
                testCase.verifyEqual(det(linear), 1, 'AbsTol', 1e-12);
            end
        end

        function rotationsPreserveOriginAndDistances(testCase)
            points = [3 4 0; 1 -2 5; -6 0 2.5];
            constructors = {@() cmr.graphics.Transform3D.rotationX(0.7), ...
                            @() cmr.graphics.Transform3D.rotationY(-1.3), ...
                            @() cmr.graphics.Transform3D.rotationZ(2.1)};

            for index = 1:numel(constructors)
                R = constructors{index}();
                transformed = cmr.graphics.Transform3D.transformPoints(R, points);

                testCase.verifyEqual( ...
                    cmr.graphics.Transform3D.transformPoints(R, [0 0 0]), ...
                    [0 0 0], 'AbsTol', 1e-12);
                testCase.verifyEqual(vecnorm(transformed, 2, 2), ...
                    vecnorm(points, 2, 2), 'AbsTol', 1e-12);
            end
        end

        function zeroAndFullTurnsAreIdentity(testCase)
            constructors = {@(a) cmr.graphics.Transform3D.rotationX(a), ...
                            @(a) cmr.graphics.Transform3D.rotationY(a), ...
                            @(a) cmr.graphics.Transform3D.rotationZ(a)};

            for index = 1:numel(constructors)
                testCase.verifyEqual(constructors{index}(0), eye(4), ...
                    'AbsTol', 1e-12);
                testCase.verifyEqual(constructors{index}(2 * pi), eye(4), ...
                    'AbsTol', 1e-12);
            end
        end

        % ----------------------------------------------------------- scaling
        function scalingMatrixIsDiagonal(testCase)
            T = cmr.graphics.Transform3D.scaling(2, -1, 0.5);
            expected = diag([2, -1, 0.5, 1]);

            testCase.verifyEqual(T, expected, 'AbsTol', 1e-12);
        end

        function scalingAcceptsUniformScalarAndVectorForms(testCase)
            scalar = cmr.graphics.Transform3D.scaling(3);
            threeArguments = cmr.graphics.Transform3D.scaling(3, 3, 3);
            vector = cmr.graphics.Transform3D.scaling([3 3 3]);

            testCase.verifyEqual(scalar, threeArguments);
            testCase.verifyEqual(scalar, vector);
        end

        function scalingMultipliesEachCoordinateByItsFactor(testCase)
            points = [1 2 3; -4 5 -6];
            expected = [2 2 1.5; -8 5 -3];
            T = cmr.graphics.Transform3D.scaling(2, 1, 0.5);

            transformed = cmr.graphics.Transform3D.transformPoints(T, points);

            testCase.verifyEqual(transformed, expected, 'AbsTol', 1e-12);
        end

        function negativeScalingProducesReflection(testCase)
            T = cmr.graphics.Transform3D.scaling(-1, 1, 1);

            testCase.verifyEqual( ...
                cmr.graphics.Transform3D.transformPoints(T, [2 3 4]), ...
                [-2 3 4], 'AbsTol', 1e-12);
            testCase.verifyEqual(det(T(1:3, 1:3)), -1, 'AbsTol', 1e-12);
        end

        function scalingRejectsInvalidFactors(testCase)
            testCase.verifyError( ...
                @() cmr.graphics.Transform3D.scaling(0), ...
                'cmr:graphics:Transform3D:invalidScale');
            testCase.verifyError( ...
                @() cmr.graphics.Transform3D.scaling(1, 2, 0), ...
                'cmr:graphics:Transform3D:invalidScale');
            testCase.verifyError( ...
                @() cmr.graphics.Transform3D.scaling(NaN), ...
                'cmr:graphics:Transform3D:invalidScale');
            testCase.verifyError( ...
                @() cmr.graphics.Transform3D.scaling([1 2]), ...
                'cmr:graphics:Transform3D:invalidScale');
        end

        % ----------------------------------------------------- homogeneous
        function toHomogeneousAppendsOnes(testCase)
            points = [1 2 3; -4 5 -6];
            H = cmr.graphics.Transform3D.toHomogeneous(points);
            expected = [1 2 3 1; -4 5 -6 1];

            testCase.verifySize(H, [2 4]);
            testCase.verifyEqual(H, expected);
        end

        function fromHomogeneousDividesByW(testCase)
            H = [2 4 6 2; -3 -6 -9 3];
            points = cmr.graphics.Transform3D.fromHomogeneous(H);

            testCase.verifyEqual(points, [1 2 3; -1 -2 -3], ...
                'AbsTol', 1e-12);
        end

        function fromHomogeneousHandlesNonCanonicalBottomRow(testCase)
            M = eye(4);
            M(4, 4) = 2;
            transformed = cmr.graphics.Transform3D.transformPoints(M, [1 2 3]);

            testCase.verifyEqual(transformed, [0.5 1 1.5], 'AbsTol', 1e-12);
            testCase.verifyFalse(cmr.graphics.Transform3D.isAffine(M));
        end

        function homogeneousHelpersValidateInput(testCase)
            testCase.verifyError( ...
                @() cmr.graphics.Transform3D.toHomogeneous([1 2]), ...
                'cmr:graphics:Transform3D:invalidPoints');
            testCase.verifyError( ...
                @() cmr.graphics.Transform3D.toHomogeneous([1 2 NaN]), ...
                'cmr:graphics:Transform3D:invalidPoints');
            testCase.verifyError( ...
                @() cmr.graphics.Transform3D.fromHomogeneous([1 2 3 0]), ...
                'cmr:graphics:Transform3D:zeroHomogeneousCoordinate');
            testCase.verifyError( ...
                @() cmr.graphics.Transform3D.fromHomogeneous([1 2 3]), ...
                'cmr:graphics:Transform3D:invalidHomogeneousPoints');
        end

        function transformPointsValidatesArguments(testCase)
            T = cmr.graphics.Transform3D.identity();

            testCase.verifyError( ...
                @() cmr.graphics.Transform3D.transformPoints(eye(3), [1 2 3]), ...
                'cmr:graphics:Transform3D:invalidMatrix');
            testCase.verifyError( ...
                @() cmr.graphics.Transform3D.transformPoints(T, [1 2]), ...
                'cmr:graphics:Transform3D:invalidPoints');
            testCase.verifyError( ...
                @() cmr.graphics.Transform3D.transformPoints(T, [1 2 Inf]), ...
                'cmr:graphics:Transform3D:invalidPoints');
        end

        % -------------------------------------------------------- composition
        function composeMatchesExplicitMatrixProduct(testCase)
            T = cmr.graphics.Transform3D.translation(1, 2, 3);
            R = cmr.graphics.Transform3D.rotationZ(pi / 3);
            S = cmr.graphics.Transform3D.scaling(2, 0.5, 4);

            combined = cmr.graphics.Transform3D.compose(T, R, S);

            testCase.verifyEqual(combined, T * R * S, 'AbsTol', 1e-12);
        end

        function composeAcceptsCellArrayAndSingleMatrix(testCase)
            T = cmr.graphics.Transform3D.translation(4, -1, 2);
            R = cmr.graphics.Transform3D.rotationX(0.5);
            fromList = cmr.graphics.Transform3D.compose(T, R);
            fromCell = cmr.graphics.Transform3D.compose({T, R});

            testCase.verifyEqual(fromCell, fromList, 'AbsTol', 1e-12);
            testCase.verifyEqual(cmr.graphics.Transform3D.compose(T), T, ...
                'AbsTol', 1e-12);
        end

        function composeAppliesTransformationsInArgumentOrder(testCase)
            T = cmr.graphics.Transform3D.translation(1, 2, 3);
            S = cmr.graphics.Transform3D.scaling(2, 1, 1);
            point = [1 0 0];

            translateThenScale = cmr.graphics.Transform3D.transformPoints( ...
                cmr.graphics.Transform3D.compose(T, S), point);
            scaleThenTranslate = cmr.graphics.Transform3D.transformPoints( ...
                cmr.graphics.Transform3D.compose(S, T), point);

            testCase.verifyEqual(translateThenScale, [4 2 3], ...
                'AbsTol', 1e-12);
            testCase.verifyEqual(scaleThenTranslate, [3 2 3], ...
                'AbsTol', 1e-12);
            testCase.verifyNotEqual(translateThenScale, scaleThenTranslate);
        end

        function rotationAndTranslationOrderChangesResult(testCase)
            T = cmr.graphics.Transform3D.translation(3, 0, 0);
            R = cmr.graphics.Transform3D.rotationZ(pi / 2);
            point = [1 0 0];

            rotateThenTranslate = cmr.graphics.Transform3D.transformPoints( ...
                cmr.graphics.Transform3D.compose(R, T), point);
            translateThenRotate = cmr.graphics.Transform3D.transformPoints( ...
                cmr.graphics.Transform3D.compose(T, R), point);

            testCase.verifyEqual(rotateThenTranslate, [3 1 0], ...
                'AbsTol', 1e-12);
            testCase.verifyEqual(translateThenRotate, [0 4 0], ...
                'AbsTol', 1e-12);
        end

        function composeRejectsEmptyAndInvalidMatrices(testCase)
            testCase.verifyError( ...
                @() cmr.graphics.Transform3D.compose(), ...
                'cmr:graphics:Transform3D:invalidSequence');
            testCase.verifyError( ...
                @() cmr.graphics.Transform3D.compose({}), ...
                'cmr:graphics:Transform3D:invalidSequence');
            testCase.verifyError( ...
                @() cmr.graphics.Transform3D.compose(eye(4), ones(3)), ...
                'cmr:graphics:Transform3D:invalidMatrix');
        end

        function combineAliasMatchesCompose(testCase)
            A = cmr.graphics.Transform3D.rotationX(0.4);
            B = cmr.graphics.Transform3D.translation(5, 6, 7);

            testCase.verifyEqual( ...
                cmr.graphics.Transform3D.combine(A, B), ...
                cmr.graphics.Transform3D.compose(A, B), 'AbsTol', 1e-12);
        end

        % ------------------------------------------------------------ inverse
        function inverseReversesCombinedTransformation(testCase)
            T = cmr.graphics.Transform3D.translation(2, -1, 4);
            R = cmr.graphics.Transform3D.rotationY(0.8);
            S = cmr.graphics.Transform3D.scaling(3, 2, 0.5);
            M = cmr.graphics.Transform3D.compose(T, R, S);
            points = [1 2 3; -4 0 5];

            transformed = cmr.graphics.Transform3D.transformPoints(M, points);
            restored = cmr.graphics.Transform3D.transformPoints( ...
                cmr.graphics.Transform3D.inverse(M), transformed);

            testCase.verifyEqual(restored, points, 'AbsTol', 1e-11);
        end

        function inverseRejectsSingularAndInvalidMatrices(testCase)
            singular = diag([1 1 1 0]);

            testCase.verifyError( ...
                @() cmr.graphics.Transform3D.inverse(singular), ...
                'cmr:graphics:Transform3D:singularMatrix');
            testCase.verifyError( ...
                @() cmr.graphics.Transform3D.inverse(eye(3)), ...
                'cmr:graphics:Transform3D:invalidMatrix');
        end

        function isAffineValidatesTolerance(testCase)
            testCase.verifyError( ...
                @() cmr.graphics.Transform3D.isAffine(eye(4), -1), ...
                'cmr:graphics:Transform3D:invalidTolerance');
        end

        % --------------------------------------------------------------- mesh
        function transformMeshMovesIssue007CubeVertices(testCase)
            cube = cmr.graphics.Cube3D.create(2, [0 0 0], [0.2 0.5 0.9]);
            originalVertices = cube.vertices;
            T = cmr.graphics.Transform3D.translation(4, -2, 1);

            moved = cmr.graphics.Transform3D.transformMesh(T, cube);

            testCase.verifyEqual(moved.vertices, ...
                originalVertices + repmat([4 -2 1], cube.vertexCount, 1), ...
                'AbsTol', 1e-12);
            testCase.verifyEqual(cube.vertices, originalVertices, ...
                'The input mesh must not be modified.');
        end

        function transformMeshPreservesMeshMetadata(testCase)
            cube = cmr.graphics.Cube3D.create(3, [1 1 1], [0.1 0.4 0.7]);
            R = cmr.graphics.Transform3D.rotationZ(pi / 2);

            rotated = cmr.graphics.Transform3D.transformMesh(R, cube);

            testCase.verifyEqual(rotated.name, cube.name);
            testCase.verifyEqual(rotated.faces, cube.faces);
            testCase.verifyEqual(rotated.color, cube.color);
            testCase.verifyEqual(rotated.vertexCount, cube.vertexCount);
            testCase.verifyEqual(rotated.faceCount, cube.faceCount);
            testCase.verifyEqual(rotated.parameters.sideLength, ...
                cube.parameters.sideLength);
            testCase.verifyEqual(rotated.parameters.center, [-1 1 1], ...
                'AbsTol', 1e-12);
        end

        function transformMeshKeepsFaceConnectivity(testCase)
            cylinder = cmr.graphics.Cylinder3D.create(1, 2, [0 0 0], 12);
            T = cmr.graphics.Transform3D.compose( ...
                cmr.graphics.Transform3D.scaling(2, 2, 2), ...
                cmr.graphics.Transform3D.rotationX(pi / 4));

            transformed = cmr.graphics.Transform3D.transformMesh(T, cylinder);
            [originalTriangles, originalFaces] = ...
                cmr.graphics.Mesh3D.triangulate(cylinder);
            [newTriangles, newFaces] = ...
                cmr.graphics.Mesh3D.triangulate(transformed);

            testCase.verifyEqual(newTriangles, originalTriangles);
            testCase.verifyEqual(newFaces, originalFaces);
            testCase.verifyTrue(all(isfinite(transformed.vertices(:))));
        end

        function transformMeshHandlesSphereWithoutCenterChange(testCase)
            sphere = cmr.graphics.Sphere3D.create(1, [0 0 0], 6, 8);
            M = cmr.graphics.Transform3D.scaling(2, 3, 4);

            stretched = cmr.graphics.Transform3D.transformMesh(M, sphere);
            distances = vecnorm(stretched.vertices, 2, 2);

            testCase.verifyEqual(stretched.parameters.center, [0 0 0], ...
                'AbsTol', 1e-12);
            testCase.verifyGreaterThan(max(distances), 4 - 1e-12);
            testCase.verifyLessThan(min(distances), 2 + 1e-12);
        end

        function transformMeshValidatesArguments(testCase)
            cube = cmr.graphics.Cube3D.create(2);

            testCase.verifyError( ...
                @() cmr.graphics.Transform3D.transformMesh(eye(3), cube), ...
                'cmr:graphics:Transform3D:invalidMatrix');
            testCase.verifyError( ...
                @() cmr.graphics.Transform3D.transformMesh(eye(4), struct()), ...
                'cmr:graphics:Transform3D:invalidMesh');
        end
    end
end
