classdef tProjection < matlab.unittest.TestCase
%TPROJECTION Unit tests for Issue #010 orthographic and perspective projection.
%
%   Coverage:
%       view matrix             eye/target/up handling
%       orthographic matrix     textbook symmetric form, distance independence
%       perspective matrix      textbook form, radians option, depth mapping
%       projection pipeline     clip coordinates, homogeneous division, meshes
%       camera distance         Euclidean distance and mesh-centre distance
%       comparison              ortho versus perspective extent ratios
%       validation              clear identifiers for invalid inputs

    methods (TestClassSetup)
        function addProjectPath(testCase) %#ok<INUSD>
            root = fileparts(fileparts(mfilename('fullpath')));
            testCase.applyFixture( ...
                matlab.unittest.fixtures.PathFixture({ ...
                root, fullfile(root, 'Graphics')}));
        end
    end

    methods (Test)
        % ----------------------------------------------------- matrices
        function orthographicMatrixMatchesTextbookForm(testCase)
            P = cmr.graphics.Projection3D.orthographic(4, 2, 1, 5);

            expected = [ ...
                0.5, 0, 0, 0; ...
                0, 1, 0, 0; ...
                0, 0, -0.5, 0; ...
                0, 0, -1.5, 1];
            testCase.verifyEqual(P, expected, 'AbsTol', 1e-12);
        end

        function perspectiveMatrixMatchesTextbookForm(testCase)
            P = cmr.graphics.Projection3D.perspective(90, 1, 1, 10);

            expected = [ ...
                1, 0, 0, 0; ...
                0, 1, 0, 0; ...
                0, 0, -11 / 9, -1; ...
                0, 0, -20 / 9, 0];
            testCase.verifyEqual(P, expected, 'AbsTol', 1e-12);
        end

        function perspectiveAcceptsRadianFieldOfView(testCase)
            fromDegrees = cmr.graphics.Projection3D.perspective(90, 1.6, 0.1, 50);
            fromRadians = cmr.graphics.Projection3D.perspective( ...
                pi / 2, 1.6, 0.1, 50, 'radians');

            testCase.verifyEqual(fromRadians, fromDegrees, 'AbsTol', 1e-12);
        end

        function viewMatrixPlacesEyeAtOriginAndTargetOnNegativeZ(testCase)
            eye = [1 2 3];
            target = [4 5 6];
            V = cmr.graphics.Projection3D.viewMatrix(eye, target, [0 0 1]);

            cameraPoints = cmr.graphics.Transform3D.transformPoints( ...
                V, [eye; target]);

            testCase.verifySize(cameraPoints, [2 3]);
            testCase.verifyEqual(cameraPoints(1, :), [0 0 0], ...
                'AbsTol', 1e-10);
            testCase.verifyEqual(cameraPoints(2, :), ...
                [0 0 -norm(target - eye)], 'AbsTol', 1e-10);
        end

        function viewMatrixUsesDocumentedUpDirection(testCase)
            V = cmr.graphics.Projection3D.viewMatrix( ...
                [0 -8 0], [0 0 0], [0 0 1]);

            cameraPoints = cmr.graphics.Transform3D.transformPoints( ...
                V, [0 0 0; 0 0 1; 1 0 0]);

            testCase.verifyEqual(cameraPoints(:, 1), [0; 0; 1], ...
                'AbsTol', 1e-12);
            testCase.verifyEqual(cameraPoints(:, 2), [0; 1; 0], ...
                'AbsTol', 1e-12);
            testCase.verifyEqual(cameraPoints(:, 3), [-8; -8; -8], ...
                'AbsTol', 1e-12);
        end

        % --------------------------------------------------- pipeline
        function clipCoordinatesComputeHomogeneousValues(testCase)
            points = [-1 0 0; 1 0 0];
            P = cmr.graphics.Projection3D.orthographic(2, 2, 0.1, 10);

            clip = cmr.graphics.Projection3D.clipCoordinates(points, P);

            testCase.verifySize(clip, [2 4]);
            testCase.verifyEqual(clip(:, 1), [-1; 1], 'AbsTol', 1e-12);
            testCase.verifyEqual(clip(:, 4), [1; 1], 'AbsTol', 1e-12);
        end

        function projectPointsDividesByW(testCase)
            P = cmr.graphics.Projection3D.perspective(90, 1, 1, 10);
            points = [0 0 -2; 2 0 -4];

            ndc = cmr.graphics.Projection3D.projectPoints(points, P);

            testCase.verifyEqual(ndc(1, :), [0 0 1 / 9], ...
                'AbsTol', 1e-12);
            testCase.verifyEqual(ndc(2, 1), 2 / 4, 'AbsTol', 1e-12);
        end

        function nearAndFarPlanesMapToMinusOneAndPlusOne(testCase)
            P = cmr.graphics.Projection3D.perspective(60, 1.5, 1, 9);

            ndc = cmr.graphics.Projection3D.projectPoints( ...
                [0 0 -1; 0 0 -9], P);

            testCase.verifyEqual(ndc(:, 3), [-1; 1], 'AbsTol', 1e-12);
        end

        function pointBehindCameraIsRejected(testCase)
            P = cmr.graphics.Projection3D.perspective(90, 1, 1, 10);

            testCase.verifyError( ...
                @() cmr.graphics.Projection3D.projectPoints([0 0 1], P), ...
                'cmr:graphics:Projection3D:pointBehindCamera');
        end

        function projectMeshReturnsTrianglesAndExtent(testCase)
            cube = cmr.graphics.Cube3D.create(2);
            V = cmr.graphics.Projection3D.viewMatrix( ...
                [0 -8 0], [0 0 0], [0 0 1]);
            P = cmr.graphics.Projection3D.orthographic(4, 4, 0.1, 100);

            result = cmr.graphics.Projection3D.projectMesh(cube, V, P);

            testCase.verifyEqual(result.meshName, 'cube');
            testCase.verifySize(result.vertices, [8 3]);
            testCase.verifySize(result.faces, [12 3]);
            testCase.verifySize(result.cameraSpace, [8 3]);
            testCase.verifyTrue(all(isfinite(result.vertices(:))));
            testCase.verifyEqual(result.projectedExtent(1:2), [1 1], ...
                'AbsTol', 1e-10);
            testCase.verifyEqual(result.projectedExtent(3), 4 / (100 - 0.1), ...
                'AbsTol', 1e-10);
        end

        function allPrimitivesProjectToFiniteNdc(testCase)
            meshes = { ...
                cmr.graphics.Cube3D.create(2), ...
                cmr.graphics.Sphere3D.create(1, [0 0 0], 8, 12), ...
                cmr.graphics.Cylinder3D.create(1, 2, [0 0 0], 12)};
            V = cmr.graphics.Projection3D.viewMatrix( ...
                [8 -10 6], [0 0 0], [0 0 1]);
            P = cmr.graphics.Projection3D.perspective(60, 1.6, 0.1, 100);

            for index = 1:numel(meshes)
                result = cmr.graphics.Projection3D.projectMesh( ...
                    meshes{index}, V, P);
                testCase.verifyTrue(all(isfinite(result.vertices(:))));
                testCase.verifyGreaterThan(result.projectedExtent(1), 0);
                testCase.verifyGreaterThan(result.projectedExtent(2), 0);
            end
        end

        % ------------------------------------------ distance behaviour
        function distanceReturnsEuclideanDistance(testCase)
            eye = [1 2 3];
            points = [1 2 7; 4 6 3];

            distances = cmr.graphics.Projection3D.distance(eye, points);

            testCase.verifySize(distances, [2 1]);
            testCase.verifyEqual(distances, [4; 5], 'AbsTol', 1e-12);
        end

        function distanceToMeshUsesBoundingBoxCenter(testCase)
            eye = [0 -10 0];
            cube = cmr.graphics.Cube3D.create(2, [1 2 3]);

            distance = cmr.graphics.Projection3D.distanceToMesh(eye, cube);

            testCase.verifyEqual(distance, norm([1 2 3] - eye), ...
                'AbsTol', 1e-12);
        end

        function orthographicKeepsSizeWhenObjectMovesAway(testCase)
            square = tProjection.planarSquare();
            P = cmr.graphics.Projection3D.orthographic(4, 4, 0.1, 100);

            nearResult = cmr.graphics.Projection3D.projectMesh( ...
                square, tProjection.squareView(4), P);
            farResult = cmr.graphics.Projection3D.projectMesh( ...
                square, tProjection.squareView(12), P);

            testCase.verifyEqual(nearResult.projectedExtent(1:2), ...
                farResult.projectedExtent(1:2), 'AbsTol', 1e-12);
            testCase.verifyEqual(nearResult.projectedExtent(1), 1, ...
                'AbsTol', 1e-12);
        end

        function perspectiveShrinksInverselyWithDistance(testCase)
            square = tProjection.planarSquare();
            P = cmr.graphics.Projection3D.perspective(60, 1, 0.1, 100);

            nearResult = cmr.graphics.Projection3D.projectMesh( ...
                square, tProjection.squareView(4), P);
            farResult = cmr.graphics.Projection3D.projectMesh( ...
                square, tProjection.squareView(12), P);

            focal = 1 / tan(pi / 6);
            expectedNear = 2 * focal / 4;
            expectedFar = 2 * focal / 12;
            testCase.verifyEqual(nearResult.projectedExtent(1), ...
                expectedNear, 'AbsTol', 1e-12);
            testCase.verifyEqual(farResult.projectedExtent(1), ...
                expectedFar, 'AbsTol', 1e-12);
            testCase.verifyEqual(nearResult.projectedExtent(1) ...
                / farResult.projectedExtent(1), 3, 'AbsTol', 1e-12);
        end

        % -------------------------------------------------- comparison
        function compareReportsOrthoAndPerspectiveRatio(testCase)
            square = tProjection.planarSquare();
            V = tProjection.squareView(4);
            ortho = cmr.graphics.Projection3D.projectMesh( ...
                square, V, cmr.graphics.Projection3D.orthographic(4, 4, 0.1, 100));
            persp = cmr.graphics.Projection3D.projectMesh( ...
                square, V, cmr.graphics.Projection3D.perspective(90, 1, 0.1, 100));

            comparison = cmr.graphics.Projection3D.compare(ortho, persp);

            testCase.verifyEqual(comparison.meshName, 'square');
            testCase.verifyEqual(comparison.orthographicExtent, ...
                ortho.projectedExtent, 'AbsTol', 1e-12);
            testCase.verifyEqual(comparison.perspectiveExtent, ...
                persp.projectedExtent, 'AbsTol', 1e-12);
            testCase.verifyEqual(comparison.widthRatio, ...
                persp.projectedExtent(1) / ortho.projectedExtent(1), ...
                'AbsTol', 1e-12);
        end

        function compareRejectsDifferentMeshesAndCameras(testCase)
            square = tProjection.planarSquare();
            cube = cmr.graphics.Cube3D.create(2);
            V = tProjection.squareView(4);
            P = cmr.graphics.Projection3D.orthographic(4, 4, 0.1, 100);

            base = cmr.graphics.Projection3D.projectMesh(square, V, P);
            otherMesh = cmr.graphics.Projection3D.projectMesh(cube, V, P);
            otherCamera = cmr.graphics.Projection3D.projectMesh( ...
                square, tProjection.squareView(8), P);

            testCase.verifyError( ...
                @() cmr.graphics.Projection3D.compare(base, otherMesh), ...
                'cmr:graphics:Projection3D:meshMismatch');
            testCase.verifyError( ...
                @() cmr.graphics.Projection3D.compare(base, otherCamera), ...
                'cmr:graphics:Projection3D:cameraMismatch');
        end

        % --------------------------------------------------- validation
        function orthographicRejectsInvalidDimensions(testCase)
            testCase.verifyError( ...
                @() cmr.graphics.Projection3D.orthographic(0, 2, 1, 10), ...
                'cmr:graphics:Projection3D:invalidViewWidth');
            testCase.verifyError( ...
                @() cmr.graphics.Projection3D.orthographic(2, -1, 1, 10), ...
                'cmr:graphics:Projection3D:invalidViewHeight');
            testCase.verifyError( ...
                @() cmr.graphics.Projection3D.orthographic(2, 2, 10, 10), ...
                'cmr:graphics:Projection3D:invalidDepthRange');
            testCase.verifyError( ...
                @() cmr.graphics.Projection3D.orthographic(2, 2, 0, 10), ...
                'cmr:graphics:Projection3D:invalidNearPlane');
        end

        function perspectiveRejectsInvalidOptions(testCase)
            testCase.verifyError( ...
                @() cmr.graphics.Projection3D.perspective(180, 1, 1, 10), ...
                'cmr:graphics:Projection3D:invalidFieldOfView');
            testCase.verifyError( ...
                @() cmr.graphics.Projection3D.perspective(60, 0, 1, 10), ...
                'cmr:graphics:Projection3D:invalidAspectRatio');
            testCase.verifyError( ...
                @() cmr.graphics.Projection3D.perspective(60, 1, 10, 1), ...
                'cmr:graphics:Projection3D:invalidDepthRange');
            testCase.verifyError( ...
                @() cmr.graphics.Projection3D.perspective(60, 1, 1, 10, 'bananas'), ...
                'cmr:graphics:Projection3D:invalidFieldOfView');
        end

        function viewMatrixRejectsInvalidCameraSetup(testCase)
            testCase.verifyError( ...
                @() cmr.graphics.Projection3D.viewMatrix( ...
                    [0 0 0], [0 0 0], [0 0 1]), ...
                'cmr:graphics:Projection3D:coincidentCameraTarget');
            testCase.verifyError( ...
                @() cmr.graphics.Projection3D.viewMatrix( ...
                    [0 0 0], [0 0 1], [0 0 2]), ...
                'cmr:graphics:Projection3D:upVectorParallelToView');
            testCase.verifyError( ...
                @() cmr.graphics.Projection3D.viewMatrix( ...
                    [0 0], [0 0 1], [0 0 1]), ...
                'MATLAB:validation:IncompatibleSize');
        end

        function projectionRejectsMalformedInput(testCase)
            P = cmr.graphics.Projection3D.orthographic(2, 2, 0.1, 10);

            testCase.verifyError( ...
                @() cmr.graphics.Projection3D.clipCoordinates([1 2], P), ...
                'cmr:graphics:Projection3D:invalidPoints');
            testCase.verifyError( ...
                @() cmr.graphics.Projection3D.clipCoordinates([1 2 3], eye(3)), ...
                'cmr:graphics:Projection3D:invalidMatrix');
            testCase.verifyError( ...
                @() cmr.graphics.Projection3D.distance([0 0 0], [1 2 3; NaN 0 0]), ...
                'cmr:graphics:Projection3D:invalidPoints');
            testCase.verifyError( ...
                @() cmr.graphics.Projection3D.projectMesh(struct(), P, P), ...
                'cmr:graphics:SurfaceNormals:invalidMesh');
        end
    end

    methods (Static, Access = private)
        function mesh = planarSquare()
            % A square in the world x-z plane, one unit in front of nothing.
            mesh = cmr.graphics.Mesh3D.create( ...
                "square", ...
                [-1 0 -1; 1 0 -1; 1 0 1; -1 0 1], ...
                [1 2 3 4], [0.2 0.5 0.8]);
        end

        function V = squareView(distance)
            V = cmr.graphics.Projection3D.viewMatrix( ...
                [0 -distance 0], [0 0 0], [0 0 1]);
        end
    end
end
