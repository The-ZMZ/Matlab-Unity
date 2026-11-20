classdef tLighting < matlab.unittest.TestCase
%TLIGHTING Unit tests for Issue #009 normals, lighting and shading.
%
%   Coverage:
%       Light3D          point-light geometry and validation
%       Material3D       Phong material properties and validation
%       LightingModel    ambient / diffuse / specular equations
%       SurfaceNormals   face, vertex and outward normal fields
%       Shading3D        flat Lambertian, smooth Phong and rendering
%
%   No test uses MATLAB's built-in lighting or shading commands; all colours
%   under test are produced by the cmr.graphics classes.

    methods (TestClassSetup)
        function addProjectPath(testCase) %#ok<INUSD>
            root = fileparts(fileparts(mfilename('fullpath')));
            testCase.applyFixture( ...
                matlab.unittest.fixtures.PathFixture({ ...
                root, fullfile(root, 'Graphics')}));
        end
    end

    methods (Test)
        % ------------------------------------------------------------ Light3D
        function light3DUsesDocumentedDefaults(testCase)
            light = cmr.graphics.Light3D([4 -5 6]);

            testCase.verifyEqual(light.position, [4 -5 6]);
            testCase.verifyEqual(light.color, [1 1 1]);
            testCase.verifyEqual(light.intensity, 1);
            testCase.verifyEqual(light.ambientIntensity, 0.15);
        end

        function light3DRejectsInvalidValues(testCase)
            testCase.verifyError( ...
                @() cmr.graphics.Light3D([0 0 NaN]), ...
                'cmr:graphics:Light3D:invalidPosition');
            testCase.verifyError( ...
                @() cmr.graphics.Light3D([0 0 0], 'Color', [1.2 0 0]), ...
                'cmr:graphics:Light3D:invalidColor');
            testCase.verifyError( ...
                @() cmr.graphics.Light3D([0 0 0], 'Intensity', -1), ...
                'cmr:graphics:Light3D:invalidIntensity');
            testCase.verifyError( ...
                @() cmr.graphics.Light3D([0 0 0], 'AmbientIntensity', 1.2), ...
                'cmr:graphics:Light3D:invalidAmbientIntensity');
        end

        function directionToReturnsOutwardUnitVectors(testCase)
            light = cmr.graphics.Light3D([3 4 0]);
            directions = light.directionTo([0 0 0; 3 -4 0]);

            testCase.verifySize(directions, [2 3]);
            testCase.verifyEqual(directions, [0.6 0.8 0; 0 1 0], ...
                'AbsTol', 1e-12);
            testCase.verifyEqual(vecnorm(directions, 2, 2), [1; 1], ...
                'AbsTol', 1e-12);
        end

        function directionToRejectsBadAndCoincidentPoints(testCase)
            light = cmr.graphics.Light3D([3 4 0]);

            testCase.verifyError( ...
                @() light.directionTo([3 4 0]), ...
                'cmr:graphics:Light3D:coincidentPoint');
            testCase.verifyError( ...
                @() light.directionTo([1 2]), ...
                'cmr:graphics:Light3D:invalidPoints');
        end

        function attenuationFollowsDocumentedDistanceFormula(testCase)
            light = cmr.graphics.Light3D([0 0 0]);
            attenuation = light.attenuationAt([1 2 2; 0 0 0]);

            expected = [1 / (1 + 0.05 * 3^2); 1];
            testCase.verifyEqual(attenuation, expected, 'AbsTol', 1e-12);
        end

        function ambientColorCombinesColorIntensityAndAmbient(testCase)
            light = cmr.graphics.Light3D([0 0 0], ...
                'Color', [1 0.5 0], 'Intensity', 2, ...
                'AmbientIntensity', 0.1);

            testCase.verifyEqual(light.ambientColor(), [0.2 0.1 0], ...
                'AbsTol', 1e-12);
        end

        % --------------------------------------------------------- Material3D
        function material3DUsesNeutralDefaults(testCase)
            material = cmr.graphics.Material3D();

            testCase.verifyEqual(material.name, 'material');
            testCase.verifyEqual(material.ambient, [0.05 0.05 0.05]);
            testCase.verifyEqual(material.diffuse, [0.60 0.60 0.60]);
            testCase.verifyEqual(material.specular, [0.10 0.10 0.10]);
            testCase.verifyEqual(material.shininess, 16);
        end

        function fromColorDerivesAllComponents(testCase)
            color = [0.2 0.55 0.9];
            material = cmr.graphics.Material3D.fromColor('Blue', color);

            testCase.verifyEqual(material.name, 'Blue');
            testCase.verifyEqual(material.diffuse, color, 'AbsTol', 1e-12);
            testCase.verifyEqual(material.ambient, 0.15 * color, ...
                'AbsTol', 1e-12);
            testCase.verifyEqual(material.specular, 0.20 * ones(1, 3), ...
                'AbsTol', 1e-12);
            testCase.verifyEqual(material.shininess, 16);
        end

        function fromColorHonoursOptionalArguments(testCase)
            material = cmr.graphics.Material3D.fromColor( ...
                "Glossy", [0.3 0.4 0.5], ...
                'Shininess', 64, 'SpecularScale', 0.5);

            testCase.verifyEqual(material.name, 'Glossy');
            testCase.verifyEqual(material.shininess, 64);
            testCase.verifyEqual(material.specular, 0.5 * ones(1, 3), ...
                'AbsTol', 1e-12);
        end

        function material3DRejectsInvalidValues(testCase)
            testCase.verifyError( ...
                @() cmr.graphics.Material3D(""), ...
                'cmr:graphics:Material3D:invalidName');
            testCase.verifyError( ...
                @() cmr.graphics.Material3D(5), ...
                'cmr:graphics:Material3D:invalidName');
            testCase.verifyError( ...
                @() cmr.graphics.Material3D('X', 'Ambient', [1.2 0 0]), ...
                'cmr:graphics:Material3D:invalidColor');
            testCase.verifyError( ...
                @() cmr.graphics.Material3D('X', 'Shininess', 0), ...
                'cmr:graphics:Material3D:invalidShininess');
            testCase.verifyError( ...
                @() cmr.graphics.Material3D.fromColor( ...
                    'X', [0 0 1], 'SpecularScale', 2), ...
                'cmr:graphics:Material3D:invalidSpecularScale');
        end

        % ------------------------------------------------------ LightingModel
        function ambientMatchesPublishedEquation(testCase)
            material = cmr.graphics.Material3D( ...
                'M', 'Ambient', [0.1 0.2 0.3]);
            light = cmr.graphics.Light3D([0 0 5], ...
                'Intensity', 2, 'AmbientIntensity', 0.5);

            ambient = cmr.graphics.LightingModel.ambient(material, light);

            testCase.verifySize(ambient, [1 3]);
            testCase.verifyEqual(ambient, [0.1 0.2 0.3], ...
                'AbsTol', 1e-12);
        end

        function diffuseIsLambertianAndVanishesOnBackFaces(testCase)
            material = cmr.graphics.Material3D( ...
                'M', 'Diffuse', [0.5 0.4 0.3]);
            light = cmr.graphics.Light3D([0 0 5], 'Intensity', 2);
            points = [0 0 0; 0 0 0; 0 0 0];
            normals = [0 0 1; 0 0 -1; 1 0 0];

            diffuse = cmr.graphics.LightingModel.diffuse( ...
                material, light, points, normals);

            lightTerm = 2 / (1 + 0.05 * 5^2);
            expected = [0.5 0.4 0.3] * lightTerm;
            testCase.verifySize(diffuse, [3 3]);
            testCase.verifyEqual(diffuse(1, :), expected, 'AbsTol', 1e-12);
            testCase.verifyEqual(diffuse(2, :), [0 0 0], 'AbsTol', 1e-12);
            testCase.verifyEqual(diffuse(3, :), [0 0 0], 'AbsTol', 1e-12);
        end

        function specularDependsOnReflectionAndViewer(testCase)
            material = cmr.graphics.Material3D( ...
                'M', 'Specular', [0.8 0.8 0.8], 'Shininess', 4);
            light = cmr.graphics.Light3D([0 0 5], 'Intensity', 2);
            point = [0 0 0];
            normal = [0 0 1];

            facing = cmr.graphics.LightingModel.specular( ...
                material, light, point, normal, [0 0 5]);
            behind = cmr.graphics.LightingModel.specular( ...
                material, light, point, normal, [0 0 -5]);
            backFace = cmr.graphics.LightingModel.specular( ...
                material, light, point, [0 0 -1], [0 0 -5]);

            lightTerm = 2 / (1 + 0.05 * 5^2);
            expected = 0.8 * lightTerm * ones(1, 3);
            testCase.verifyEqual(facing, expected, 'AbsTol', 1e-12);
            testCase.verifyEqual(behind, [0 0 0], 'AbsTol', 1e-12);
            testCase.verifyEqual(backFace, [0 0 0], 'AbsTol', 1e-12);
        end

        function illuminateReturnsConsistentComponents(testCase)
            material = cmr.graphics.Material3D.fromColor( ...
                'M', [0.2 0.5 0.8], 'Shininess', 16);
            light = cmr.graphics.Light3D([4 -5 6], 'Intensity', 1.5);
            points = [0 0 0; 1 1 1];
            normals = [0 0 1; 1 0 0];

            components = cmr.graphics.LightingModel.illuminate( ...
                material, light, points, normals, [6 -8 7]);

            testCase.verifySize(components.ambient, [2 3]);
            testCase.verifySize(components.diffuse, [2 3]);
            testCase.verifySize(components.specular, [2 3]);
            testCase.verifySize(components.total, [2 3]);
            testCase.verifyEqual(components.total, ...
                components.ambient + components.diffuse ...
                + components.specular, 'AbsTol', 1e-12);
            testCase.verifyEqual(components.ambient(1, :), ...
                components.ambient(2, :), 'AbsTol', 1e-12);
        end

        function normalsAreNormalisedBeforeLighting(testCase)
            material = cmr.graphics.Material3D.fromColor('M', [0.2 0.5 0.8]);
            light = cmr.graphics.Light3D([0 0 5]);

            vertical = cmr.graphics.LightingModel.diffuse( ...
                material, light, [0 0 0], [0 0 1]);
            scaled = cmr.graphics.LightingModel.diffuse( ...
                material, light, [0 0 0], [0 0 25]);

            testCase.verifyEqual(scaled, vertical, 'AbsTol', 1e-12);
        end

        function lightingModelRejectsInvalidInputs(testCase)
            light = cmr.graphics.Light3D([0 0 5]);
            material = cmr.graphics.Material3D();

            testCase.verifyError( ...
                @() cmr.graphics.LightingModel.ambient(struct(), light), ...
                'cmr:graphics:LightingModel:invalidMaterial');
            testCase.verifyError( ...
                @() cmr.graphics.LightingModel.ambient(material, struct()), ...
                'cmr:graphics:LightingModel:invalidLight');
            testCase.verifyError( ...
                @() cmr.graphics.LightingModel.diffuse( ...
                    material, light, [1 2], [0 0 1]), ...
                'cmr:graphics:LightingModel:invalidPoints');
            testCase.verifyError( ...
                @() cmr.graphics.LightingModel.diffuse( ...
                    material, light, [0 0 0], [0 0 0]), ...
                'cmr:graphics:LightingModel:zeroNormal');
            testCase.verifyError( ...
                @() cmr.graphics.LightingModel.specular( ...
                    material, light, [0 0 0], [0 0 1], [0 0 0 0]), ...
                'cmr:graphics:LightingModel:invalidPoints');
            testCase.verifyError( ...
                @() cmr.graphics.LightingModel.specular( ...
                    material, light, [0 0 0], [0 0 1], [0 0 0]), ...
                'cmr:graphics:LightingModel:coincidentViewer');
            testCase.verifyError( ...
                @() cmr.graphics.LightingModel.specular( ...
                    material, light, [0 0 0], [0 0 1], ...
                    [0 0 5; 1 1 1]), ...
                'cmr:graphics:LightingModel:invalidViewer');
        end

        % ------------------------------------------------------ SurfaceNormals
        function faceNormalsGatewayMatchesMesh3D(testCase)
            cube = cmr.graphics.Cube3D.create(2, [1 2 3]);

            testCase.verifyEqual( ...
                cmr.graphics.SurfaceNormals.faceNormals(cube), ...
                cmr.graphics.Mesh3D.faceNormals(cube), 'AbsTol', 1e-12);
        end

        function faceCentersAndAreasOfCubeAreExact(testCase)
            center = [1 2 3];
            cube = cmr.graphics.Cube3D.create(2, center);

            faceCenters = cmr.graphics.SurfaceNormals.faceCenters(cube);
            faceAreas = cmr.graphics.SurfaceNormals.faceAreas(cube);

            testCase.verifySize(faceCenters, [6 3]);
            testCase.verifySize(faceAreas, [6 1]);
            testCase.verifyEqual(faceAreas, 4 * ones(6, 1), ...
                'AbsTol', 1e-12);
            testCase.verifyEqual( ...
                vecnorm(faceCenters - repmat(center, 6, 1), 2, 2), ...
                ones(6, 1), 'AbsTol', 1e-12);
        end

        function cubeVertexNormalsAreAreaWeightedAndUnitLength(testCase)
            cube = cmr.graphics.Cube3D.create(2, [0 0 0]);
            normals = cmr.graphics.SurfaceNormals.vertexNormals(cube);

            testCase.verifySize(normals, [8 3]);
            testCase.verifyEqual(vecnorm(normals, 2, 2), ones(8, 1), ...
                'AbsTol', 1e-12);
            testCase.verifyEqual(normals, sign(cube.vertices) / sqrt(3), ...
                'AbsTol', 1e-12);
        end

        function outwardNormalsPointAwayFromMeshCentre(testCase)
            meshes = { ...
                cmr.graphics.Cube3D.create(2), ...
                cmr.graphics.Sphere3D.create(1, [0 0 0], 8, 12)};

            for index = 1:numel(meshes)
                mesh = meshes{index};
                center = mean(mesh.vertices, 1);
                faceCenters = cmr.graphics.SurfaceNormals.faceCenters(mesh);
                faceNormals = ...
                    cmr.graphics.SurfaceNormals.outwardFaceNormals(mesh);
                vertexNormals = ...
                    cmr.graphics.SurfaceNormals.outwardVertexNormals(mesh);

                faceDot = sum(faceNormals .* (faceCenters - center), 2);
                vertexDot = sum(vertexNormals .* (mesh.vertices - center), 2);
                testCase.verifyTrue(all(faceDot > 0), ...
                    'Face normals must point away from the centre.');
                testCase.verifyTrue(all(vertexDot > 0), ...
                    'Vertex normals must point away from the centre.');
            end
        end

        function orientOutwardsFlipsInwardNormals(testCase)
            cube = cmr.graphics.Cube3D.create(2);
            outward = cmr.graphics.SurfaceNormals.outwardFaceNormals(cube);
            inward = -outward;
            centers = cmr.graphics.SurfaceNormals.faceCenters(cube);

            [oriented, flipped] = cmr.graphics.SurfaceNormals.orientOutwards( ...
                inward, centers, cube);

            testCase.verifyTrue(all(flipped));
            testCase.verifyEqual(oriented, outward, 'AbsTol', 1e-12);
        end

        function isOutwardDetectsNormalOrientation(testCase)
            cube = cmr.graphics.Cube3D.create(2);
            outward = cmr.graphics.SurfaceNormals.outwardFaceNormals(cube);
            centers = cmr.graphics.SurfaceNormals.faceCenters(cube);

            testCase.verifyTrue(all(cmr.graphics.SurfaceNormals.isOutward( ...
                outward, centers, cube)));
            testCase.verifyFalse(any(cmr.graphics.SurfaceNormals.isOutward( ...
                -outward, centers, cube)));
        end

        function visualizeDrawsQuiverOverMesh(testCase)
            figureHandle = figure('Visible', 'off');
            cleanup = onCleanup(@() close(figureHandle)); %#ok<NASGU>
            axesHandle = axes('Parent', figureHandle);
            cube = cmr.graphics.Cube3D.create(2);

            handle = cmr.graphics.SurfaceNormals.visualize( ...
                axesHandle, cube, 'face');

            testCase.verifyTrue(isgraphics(handle));
            testCase.verifyEqual(get(handle, 'Type'), 'quiver');
        end

        function surfaceNormalsRejectInvalidInputs(testCase)
            cube = cmr.graphics.Cube3D.create(2);

            testCase.verifyError( ...
                @() cmr.graphics.SurfaceNormals.checkMesh(struct()), ...
                'cmr:graphics:SurfaceNormals:invalidMesh');
            testCase.verifyError( ...
                @() cmr.graphics.SurfaceNormals.orientOutwards( ...
                    ones(6, 3), [0 0 0], cube), ...
                'cmr:graphics:SurfaceNormals:invalidSamplePoints');

            figureHandle = figure('Visible', 'off');
            cleanup = onCleanup(@() close(figureHandle)); %#ok<NASGU>
            testCase.verifyError( ...
                @() cmr.graphics.SurfaceNormals.visualize( ...
                    figureHandle, cube, 'face'), ...
                'cmr:graphics:SurfaceNormals:invalidAxes');
            testCase.verifyError( ...
                @() cmr.graphics.SurfaceNormals.visualize( ...
                    axes(figureHandle), cube, 'unknown'), ...
                'cmr:graphics:SurfaceNormals:invalidNormalType');
        end

        % ---------------------------------------------------------- Shading3D
        function lambertianProducesFlatPerFaceColors(testCase)
            [mesh, light, material, viewer] = tLighting.shadingFixture();
            triangles = cmr.graphics.Mesh3D.triangulate(mesh);

            result = cmr.graphics.Shading3D.lambertian( ...
                mesh, light, material, viewer);

            testCase.verifyEqual(result.method, 'Lambertian');
            testCase.verifyEqual(result.colorLocation, 'face');
            testCase.verifySize(result.faceColors, [size(triangles, 1) 3]);
            testCase.verifySize(result.vertexColors, [mesh.vertexCount 3]);
            testCase.verifyTrue(all(result.vertexColors(:) == 0));
            testCase.verifyTrue(all(isfinite(result.faceColors(:))));
            testCase.verifyTrue(all(result.faceColors(:) >= 0));
            testCase.verifyTrue(all(result.faceColors(:) <= 1));
            testCase.verifyTrue(all(result.components.specular(:) == 0), ...
                'Lambertian shading must not contain a specular term.');
            testCase.verifyEqual(result.components.total, ...
                result.components.ambient + result.components.diffuse, ...
                'AbsTol', 1e-12);
            testCase.verifyGreaterThanOrEqual(result.brightestFace, 1);
            testCase.verifyLessThanOrEqual( ...
                result.brightestFace, size(triangles, 1));
        end

        function phongProducesSmoothPerVertexColors(testCase)
            [mesh, light, material, viewer] = tLighting.shadingFixture();
            triangles = cmr.graphics.Mesh3D.triangulate(mesh);

            result = cmr.graphics.Shading3D.phong( ...
                mesh, light, material, viewer);

            testCase.verifyEqual(result.method, 'Phong');
            testCase.verifyEqual(result.colorLocation, 'vertex');
            testCase.verifySize(result.vertexColors, [mesh.vertexCount 3]);
            testCase.verifySize(result.faceColors, [size(triangles, 1) 3]);
            testCase.verifyTrue(all(isfinite(result.vertexColors(:))));
            testCase.verifyTrue(all(result.vertexColors(:) >= 0));
            testCase.verifyTrue(all(result.vertexColors(:) <= 1));
            testCase.verifyEqual(result.components.total, ...
                result.components.ambient + result.components.diffuse ...
                + result.components.specular, 'AbsTol', 1e-12);
            testCase.verifyTrue(any(result.components.specular(:) > 0), ...
                'A glossy material must produce a specular highlight.');
            expectedFaces = (result.vertexColors(triangles(:, 1), :) ...
                + result.vertexColors(triangles(:, 2), :) ...
                + result.vertexColors(triangles(:, 3), :)) / 3;
            testCase.verifyEqual(result.faceColors, expectedFaces, ...
                'AbsTol', 1e-12);
            testCase.verifyGreaterThanOrEqual(result.brightestVertex, 1);
            testCase.verifyLessThanOrEqual( ...
                result.brightestVertex, mesh.vertexCount);
        end

        function shadeDispatchesByMethodName(testCase)
            [mesh, light, material, viewer] = tLighting.shadingFixture();

            flat = cmr.graphics.Shading3D.shade( ...
                mesh, light, material, viewer, 'flat');
            expectedFlat = cmr.graphics.Shading3D.lambertian( ...
                mesh, light, material, viewer);
            testCase.verifyEqual(flat.faceColors, expectedFlat.faceColors, ...
                'AbsTol', 1e-12);

            smooth = cmr.graphics.Shading3D.shade( ...
                mesh, light, material, viewer, 'SMOOTH');
            expectedSmooth = cmr.graphics.Shading3D.phong( ...
                mesh, light, material, viewer);
            testCase.verifyEqual( ...
                smooth.vertexColors, expectedSmooth.vertexColors, ...
                'AbsTol', 1e-12);

            testCase.verifyError( ...
                @() cmr.graphics.Shading3D.shade( ...
                    mesh, light, material, viewer, 'unknown'), ...
                'cmr:graphics:Shading3D:invalidMethod');
        end

        function renderCreatesPatchWithComputedColors(testCase)
            [mesh, light, material, viewer] = tLighting.shadingFixture();
            figureHandle = figure('Visible', 'off');
            cleanup = onCleanup(@() close(figureHandle)); %#ok<NASGU>
            axesHandle = axes('Parent', figureHandle);

            flat = cmr.graphics.Shading3D.lambertian( ...
                mesh, light, material, viewer);
            flatHandle = cmr.graphics.Shading3D.render(axesHandle, flat);

            testCase.verifyTrue(isgraphics(flatHandle));
            testCase.verifyEqual(get(flatHandle, 'FaceColor'), 'flat');
            testCase.verifySize(get(flatHandle, 'FaceVertexCData'), ...
                size(flat.faceColors));

            smooth = cmr.graphics.Shading3D.phong( ...
                mesh, light, material, viewer);
            smoothHandle = cmr.graphics.Shading3D.render(axesHandle, smooth);

            testCase.verifyTrue(isgraphics(smoothHandle));
            testCase.verifyEqual(get(smoothHandle, 'FaceColor'), 'interp');
            testCase.verifySize(get(smoothHandle, 'FaceVertexCData'), ...
                size(smooth.vertexColors));

            testCase.verifyError( ...
                @() cmr.graphics.Shading3D.render(struct()), ...
                'cmr:graphics:Shading3D:invalidResult');
        end

        function allPrimitivesShadeWithFiniteColors(testCase)
            meshes = { ...
                cmr.graphics.Cube3D.create(2), ...
                cmr.graphics.Sphere3D.create(1, [0 0 0], 8, 12), ...
                cmr.graphics.Cylinder3D.create(1, 2, [0 0 0], 12)};
            light = cmr.graphics.Light3D([4 -5 6], 'Intensity', 1.2);
            material = cmr.graphics.Material3D.fromColor( ...
                'M', [0.2 0.5 0.8], 'Shininess', 32);

            for index = 1:numel(meshes)
                flat = cmr.graphics.Shading3D.lambertian( ...
                    meshes{index}, light, material, [6 -8 7]);
                smooth = cmr.graphics.Shading3D.phong( ...
                    meshes{index}, light, material, [6 -8 7]);

                testCase.verifyTrue(all(isfinite(flat.faceColors(:))));
                testCase.verifyTrue(all(isfinite(smooth.vertexColors(:))));
                testCase.verifyTrue(all(smooth.faceColors(:) >= 0));
                testCase.verifyTrue(all(smooth.faceColors(:) <= 1));
            end
        end
    end

    methods (Static, Access = private)
        function [mesh, light, material, viewer] = shadingFixture()
            mesh = cmr.graphics.Sphere3D.create( ...
                1, [0 0 0], 8, 12, [0.2 0.5 0.8]);
            light = cmr.graphics.Light3D([4 -5 6], ...
                'Color', [1 0.95 0.85], 'Intensity', 1.2, ...
                'AmbientIntensity', 0.18);
            material = cmr.graphics.Material3D.fromColor( ...
                'Test gloss', [0.2 0.5 0.8], ...
                'Shininess', 32, 'SpecularScale', 0.6);
            viewer = [6 -8 7];
        end
    end
end
