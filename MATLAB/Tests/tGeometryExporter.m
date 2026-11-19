classdef tGeometryExporter < matlab.unittest.TestCase
%TGEOMETRYEXPORTER Unit tests for Issue #010 OBJ/MTL geometry export.
%
%   Coverage:
%       OBJ writing        vertices, triangle indices, normals, colours
%       MTL writing        Ka/Kd/Ks/Ns/d/illum from Material3D
%       options            normals, colours and material can be disabled
%       coordinate systems MATLAB Z-up and Unity Y-up conversion
%       verification       subset OBJ reader, bounds and winding checks
%       validation         clear identifiers for invalid inputs

    methods (TestClassSetup)
        function addProjectPath(testCase) %#ok<INUSD>
            root = fileparts(fileparts(mfilename('fullpath')));
            testCase.applyFixture( ...
                matlab.unittest.fixtures.PathFixture({ ...
                root, fullfile(root, 'Graphics')}));
        end
    end

    methods (Test)
        % --------------------------------------------------------- OBJ
        function exportWritesVerticesFacesAndMaterialReferences(testCase)
            folder = tGeometryExporter.tempFolder(testCase);
            mesh = cmr.graphics.Cube3D.create(2, [0 0 0], [0.2 0.55 0.9]);
            material = cmr.graphics.Material3D.fromColor('Cube paint', mesh.color);

            outputs = cmr.graphics.GeometryExporter.export( ...
                mesh, fullfile(folder, 'cube.obj'), 'Material', material);

            text = string(fileread(char(outputs.objPath)));
            lines = splitlines(text);
            testCase.verifyTrue(isfile(outputs.objPath));
            testCase.verifyTrue(isfile(outputs.mtlPath));
            testCase.verifyTrue(contains(text, 'mtllib cube.mtl'));
            testCase.verifyTrue(contains(text, 'o cube'));
            testCase.verifyTrue(contains(text, 'usemtl Cube_paint'));
            faceLines = lines(startsWith(lines, 'f '));
            testCase.verifyEqual(sum(startsWith(lines, 'v ')), 8);
            testCase.verifyEqual(sum(startsWith(lines, 'vn ')), 8);
            testCase.verifyEqual(numel(faceLines), 12);
            testCase.verifyTrue(all(contains(faceLines, '//')), ...
                'Every face must reference a vertex normal.');
        end

        function exportWritesMaterialFileWithPhongProperties(testCase)
            folder = tGeometryExporter.tempFolder(testCase);
            mesh = cmr.graphics.Cube3D.create(2);
            material = cmr.graphics.Material3D('Test material', ...
                'Ambient', [0.1 0.2 0.3], ...
                'Diffuse', [0.4 0.5 0.6], ...
                'Specular', [0.7 0.8 0.9], 'Shininess', 32);

            outputs = cmr.graphics.GeometryExporter.export( ...
                mesh, fullfile(folder, 'box.obj'), 'Material', material);
            text = string(fileread(char(outputs.mtlPath)));

            testCase.verifyTrue(contains(text, 'newmtl Test_material'));
            testCase.verifyTrue(contains(text, 'Ka 0.1 0.2 0.3'));
            testCase.verifyTrue(contains(text, 'Kd 0.4 0.5 0.6'));
            testCase.verifyTrue(contains(text, 'Ks 0.7 0.8 0.9'));
            testCase.verifyTrue(contains(text, 'Ns 32'));
            testCase.verifyTrue(contains(text, 'd 1'));
            testCase.verifyTrue(contains(text, 'illum 2'));
        end

        function exportWritesVertexColorsWhenRequested(testCase)
            folder = tGeometryExporter.tempFolder(testCase);
            mesh = cmr.graphics.Cube3D.create(2, [0 0 0], [0.2 0.55 0.9]);

            outputs = cmr.graphics.GeometryExporter.export( ...
                mesh, fullfile(folder, 'coloured.obj'));
            data = cmr.graphics.GeometryExporter.readOBJ(outputs.objPath);

            testCase.verifySize(data.vertexColors, [mesh.vertexCount 3]);
            testCase.verifyEqual(data.vertexColors, ...
                repmat(mesh.color, mesh.vertexCount, 1), 'AbsTol', 1e-9);
        end

        function exportOmitsOptionalDataWhenDisabled(testCase)
            folder = tGeometryExporter.tempFolder(testCase);
            mesh = cmr.graphics.Cube3D.create(2);

            outputs = cmr.graphics.GeometryExporter.export( ...
                mesh, fullfile(folder, 'plain.obj'), ...
                'WriteNormals', false, 'WriteColors', false, ...
                'WriteMaterial', false);
            text = string(fileread(char(outputs.objPath)));
            data = cmr.graphics.GeometryExporter.readOBJ(outputs.objPath);

            testCase.verifyFalse(contains(text, 'vn '));
            testCase.verifyFalse(contains(text, 'mtllib'));
            testCase.verifyFalse(contains(text, 'usemtl'));
            testCase.verifyEqual(outputs.mtlPath, "");
            testCase.verifyTrue(isempty(data.normals));
            testCase.verifyTrue(isempty(data.vertexColors));
            lines = splitlines(text);
            faceLines = lines(startsWith(lines, 'f '));
            testCase.verifyEqual(numel(faceLines), 12);
            testCase.verifyTrue(all(~contains(faceLines, '/')));
        end

        function readOBJRecoversExportedGeometry(testCase)
            folder = tGeometryExporter.tempFolder(testCase);
            mesh = cmr.graphics.Sphere3D.create(1, [0 0 0], 8, 12, ...
                [0.15 0.70 0.45]);

            outputs = cmr.graphics.GeometryExporter.export( ...
                mesh, fullfile(folder, 'sphere.obj'));
            data = cmr.graphics.GeometryExporter.readOBJ(outputs.objPath);

            testCase.verifyEqual(data.objectName, 'sphere');
            testCase.verifyEqual(data.materialName, 'sphere_material');
            testCase.verifyEqual(data.materialFile, 'sphere.mtl');
            testCase.verifySize(data.vertices, [mesh.vertexCount 3]);
            testCase.verifyEqual(data.vertices, mesh.vertices, 'AbsTol', 1e-9);
            testCase.verifySize(data.normals, [mesh.vertexCount 3]);
            testCase.verifySize(data.faces, [outputs.faceCount 3]);
            testCase.verifyLessThanOrEqual(max(data.faces(:)), mesh.vertexCount);
            testCase.verifyGreaterThanOrEqual(min(data.faces(:)), 1);
            testCase.verifyLessThanOrEqual( ...
                abs(vecnorm(data.normals, 2, 2) - 1), 1e-9);
        end

        function readOBJHandlesPlainFacesFromIssue007(testCase)
            folder = tGeometryExporter.tempFolder(testCase);
            mesh = cmr.graphics.Cube3D.create(2);
            path = cmr.graphics.Mesh3D.exportOBJ( ...
                mesh, fullfile(folder, 'plain007.obj'));

            data = cmr.graphics.GeometryExporter.readOBJ(path);

            testCase.verifySize(data.vertices, [8 3]);
            testCase.verifySize(data.faces, [12 3]);
            testCase.verifyTrue(isempty(data.normals));
            testCase.verifyTrue(isempty(data.vertexColors));
        end

        % -------------------------------------------- coordinate systems
        function unityConversionRemapsAxesAndDimensions(testCase)
            scaled = cmr.graphics.Transform3D.transformMesh( ...
                cmr.graphics.Transform3D.compose( ...
                    cmr.graphics.Transform3D.scaling(2, 3, 5), ...
                    cmr.graphics.Transform3D.translation(1, 2, 3)), ...
                cmr.graphics.Cube3D.create(1));

            converted = cmr.graphics.GeometryExporter.convertMesh( ...
                scaled, 'unity');
            [minimum, maximum] = cmr.graphics.Mesh3D.bounds(converted);

            testCase.verifyEqual(maximum - minimum, [2 5 3], ...
                'AbsTol', 1e-12);
            testCase.verifyEqual(mean(converted.vertices, 1), [1 3 2], ...
                'AbsTol', 1e-12);
            testCase.verifyEqual(converted.parameters.center, [1 3 2], ...
                'AbsTol', 1e-12);
        end

        function unityConversionReversesFaceWinding(testCase)
            mesh = cmr.graphics.Cube3D.create(2);

            converted = cmr.graphics.GeometryExporter.convertMesh(mesh, 'unity');

            for index = 1:numel(mesh.faces)
                testCase.verifyEqual(converted.faces{index}, ...
                    fliplr(mesh.faces{index}));
            end

            center = mean(converted.vertices, 1);
            faceCenters = cmr.graphics.SurfaceNormals.faceCenters(converted);
            normals = cmr.graphics.SurfaceNormals.outwardFaceNormals(converted);
            radial = faceCenters - repmat(center, size(faceCenters, 1), 1);
            testCase.verifyTrue(all(sum(normals .* radial, 2) > 0), ...
                'Converted normals must point away from the mesh centre.');
        end

        function matlabConversionLeavesMeshUnchanged(testCase)
            mesh = cmr.graphics.Cylinder3D.create(1, 2, [1 2 3], 12);

            converted = cmr.graphics.GeometryExporter.convertMesh( ...
                mesh, 'matlab');

            testCase.verifyEqual(converted, mesh);
        end

        function exportedUnityFileUsesRemappedCoordinates(testCase)
            folder = tGeometryExporter.tempFolder(testCase);
            mesh = cmr.graphics.Transform3D.transformMesh( ...
                cmr.graphics.Transform3D.scaling(2, 3, 5), ...
                cmr.graphics.Cube3D.create(1));

            outputs = cmr.graphics.GeometryExporter.export( ...
                mesh, fullfile(folder, 'unity.obj'), ...
                'CoordinateSystem', 'unity');
            data = cmr.graphics.GeometryExporter.readOBJ(outputs.objPath);
            sizeInFile = max(data.vertices, [], 1) - min(data.vertices, [], 1);

            testCase.verifyEqual(outputs.coordinateSystem, "unity");
            testCase.verifyEqual(sizeInFile, [2 5 3], 'AbsTol', 1e-9);
            testCase.verifyEqual(size(data.normals), [mesh.vertexCount 3]);
        end

        function convertMeshRejectsUnknownSystem(testCase)
            mesh = cmr.graphics.Cube3D.create(2);

            testCase.verifyError( ...
                @() cmr.graphics.GeometryExporter.convertMesh(mesh, 'blender'), ...
                'cmr:graphics:GeometryExporter:invalidCoordinateSystem');
            testCase.verifyError( ...
                @() cmr.graphics.GeometryExporter.export( ...
                    mesh, tempname, 'CoordinateSystem', 42), ...
                'cmr:graphics:GeometryExporter:invalidCoordinateSystem');
        end

        % -------------------------------------------------------- files
        function exportAddsMissingExtensionAndCreatesFolders(testCase)
            root = string(tempname);
            cleanup = onCleanup(@() rmdir(root, 's')); %#ok<NASGU>
            mesh = cmr.graphics.Cube3D.create(2);

            outputs = cmr.graphics.GeometryExporter.export( ...
                mesh, fullfile(root, 'nested', 'box'));

            testCase.verifyTrue(endsWith(outputs.objPath, '.obj'));
            testCase.verifyTrue(isfile(outputs.objPath));
            testCase.verifyTrue(endsWith(outputs.mtlPath, 'box.mtl'));
            testCase.verifyTrue(isfile(outputs.mtlPath));
        end

        function exportUsesDerivedMaterialWhenNoneProvided(testCase)
            folder = tGeometryExporter.tempFolder(testCase);
            mesh = cmr.graphics.Cube3D.create(2, [0 0 0], [0.2 0.55 0.9]);

            outputs = cmr.graphics.GeometryExporter.export( ...
                mesh, fullfile(folder, 'derived.obj'));
            mtl = string(fileread(char(outputs.mtlPath)));

            testCase.verifyTrue(contains(mtl, 'newmtl cube_material'));
            testCase.verifyTrue(contains(mtl, 'Kd 0.2 0.55 0.9'));
        end

        function exportFromTransformedMeshPreservesDimensions(testCase)
            folder = tGeometryExporter.tempFolder(testCase);
            transform = cmr.graphics.Transform3D.compose( ...
                cmr.graphics.Transform3D.scaling(2, 3, 5), ...
                cmr.graphics.Transform3D.rotationZ(30, 'degrees'), ...
                cmr.graphics.Transform3D.translation(1, 2, 3));
            mesh = cmr.graphics.Transform3D.transformMesh( ...
                transform, cmr.graphics.Cube3D.create(1));
            [expectedMinimum, expectedMaximum] = cmr.graphics.Mesh3D.bounds(mesh);

            outputs = cmr.graphics.GeometryExporter.export( ...
                mesh, fullfile(folder, 'moved.obj'));
            data = cmr.graphics.GeometryExporter.readOBJ(outputs.objPath);

            testCase.verifyEqual(min(data.vertices, [], 1), expectedMinimum, ...
                'AbsTol', 1e-9);
            testCase.verifyEqual(max(data.vertices, [], 1), expectedMaximum, ...
                'AbsTol', 1e-9);
        end

        function exportIsDeterministic(testCase)
            first = tGeometryExporter.tempFolder(testCase);
            second = tGeometryExporter.tempFolder(testCase);
            mesh = cmr.graphics.Cube3D.create(2);

            a = cmr.graphics.GeometryExporter.export( ...
                mesh, fullfile(first, 'same.obj'));
            b = cmr.graphics.GeometryExporter.export( ...
                mesh, fullfile(second, 'same.obj'));

            testCase.verifyEqual( ...
                string(fileread(char(a.objPath))), ...
                string(fileread(char(b.objPath))));
            testCase.verifyEqual( ...
                string(fileread(char(a.mtlPath))), ...
                string(fileread(char(b.mtlPath))));
        end

        % --------------------------------------------------- validation
        function exportRejectsInvalidInputs(testCase)
            mesh = cmr.graphics.Cube3D.create(2);

            testCase.verifyError( ...
                @() cmr.graphics.GeometryExporter.export(mesh, 123), ...
                'cmr:graphics:GeometryExporter:invalidFilePath');
            testCase.verifyError( ...
                @() cmr.graphics.GeometryExporter.export(mesh, ''), ...
                'cmr:graphics:GeometryExporter:invalidFilePath');
            testCase.verifyError( ...
                @() cmr.graphics.GeometryExporter.export(struct(), tempname), ...
                'cmr:graphics:GeometryExporter:invalidMesh');
            testCase.verifyError( ...
                @() cmr.graphics.GeometryExporter.export( ...
                    mesh, tempname, 'Material', struct()), ...
                'cmr:graphics:LightingModel:invalidMaterial');
            testCase.verifyError( ...
                @() cmr.graphics.GeometryExporter.readOBJ(tempname), ...
                'cmr:graphics:GeometryExporter:fileNotFound');
        end
    end

    methods (Static, Access = private)
        function folder = tempFolder(testCase)
            fixture = testCase.applyFixture( ...
                matlab.unittest.fixtures.TemporaryFolderFixture);
            folder = string(fixture.Folder);
        end
    end
end
