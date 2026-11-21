function outputs = demoProjectionExport(outputDirectory, meshDirectory)
%DEMPROJECTIONEXPORT Visualise Issue #010 projection and geometry export.
%
%   outputs = demoProjectionExport() writes three evidence figures to
%   Deliverables/Screenshots and four geometry files plus a manifest to
%   Shared_Data/Meshes/Issue010:
%
%       Issue010_Orthographic_vs_Perspective.png
%       Issue010_Distance_Comparison.png
%       Issue010_OBJ_Export.png
%       Issue010_Box_MATLAB.obj / .mtl
%       Issue010_Box_Unity.obj / .mtl
%       Export_Manifest.txt
%
%   The functions called here are the cmr.graphics classes implemented for
%   Issue #010: Projection3D for the matrices and GeometryExporter for the
%   OBJ/MTL files. Earlier Issue #007-#009 classes are reused, not changed.
%
%   Optional arguments override the two output folders:
%       outputs = demoProjectionExport(screenshotFolder, meshFolder)

    arguments
        outputDirectory (1,1) string = ""
        meshDirectory (1,1) string = ""
    end

    here = fileparts(mfilename('fullpath'));     % MATLAB/Graphics
    matlabRoot = fileparts(here);                % MATLAB
    projectRoot = fileparts(matlabRoot);         % repository root

    addpath(genpath(matlabRoot));

    if strlength(outputDirectory) == 0
        outputDirectory = string(fullfile( ...
            projectRoot, 'Deliverables', 'Screenshots'));
    end
    if strlength(meshDirectory) == 0
        meshDirectory = string(fullfile( ...
            projectRoot, 'Shared_Data', 'Meshes', 'Issue010'));
    end
    if ~isfolder(outputDirectory)
        mkdir(outputDirectory);
    end
    if ~isfolder(meshDirectory)
        mkdir(meshDirectory);
    end

    outputs = strings(0, 1);

    fprintf('Clean My Room - Issue #010 projection and geometry export\n');
    fprintf('MATLAB %s on %s\n', version, computer);

    %% Figure 1 - orthographic and perspective side by side -----------------
    meshes = { ...
        cmr.graphics.Cube3D.create(2, [-3 0 0], [0.20 0.52 0.90]), ...
        cmr.graphics.Sphere3D.create(1.2, [0 0 0], 10, 16, [0.15 0.70 0.45]), ...
        cmr.graphics.Cylinder3D.create(0.9, 2.2, [3 0 0], 14, [0.95 0.55 0.15])};
    names = {'Cube', 'Sphere', 'Cylinder'};

    eye = [0 -10 3];
    target = [0 0 0];
    view = cmr.graphics.Projection3D.viewMatrix(eye, target, [0 0 1]);
    orthographic = cmr.graphics.Projection3D.orthographic(6, 4.5, 0.1, 100);
    perspective = cmr.graphics.Projection3D.perspective(35, 6 / 4.5, 0.1, 100);

    fig1 = figure('Visible', figureVisibility(), 'Color', 'w', ...
        'Name', 'Issue #010 - orthographic versus perspective', ...
        'Position', [50 50 1680 980]);
    layout1 = tiledlayout(fig1, 2, 3, ...
        'TileSpacing', 'compact', 'Padding', 'compact');

    fprintf('\nGallery projection extents (NDC width x height):\n');
    for index = 1:numel(meshes)
        orthoResult = cmr.graphics.Projection3D.projectMesh( ...
            meshes{index}, view, orthographic);
        perspResult = cmr.graphics.Projection3D.projectMesh( ...
            meshes{index}, view, perspective);
        comparison = cmr.graphics.Projection3D.compare( ...
            orthoResult, perspResult);

        axOrtho = nexttile(layout1, index);
        drawProjected(axOrtho, orthoResult, meshes{index}.color, ...
            sprintf('%s - orthographic (%.3f x %.3f)', ...
            names{index}, orthoResult.projectedExtent(1), ...
            orthoResult.projectedExtent(2)));

        axPersp = nexttile(layout1, index + 3);
        drawProjected(axPersp, perspResult, meshes{index}.color, ...
            sprintf('%s - perspective (%.3f x %.3f)', ...
            names{index}, perspResult.projectedExtent(1), ...
            perspResult.projectedExtent(2)));

        fprintf('  %-8s ortho %.4f x %.4f | perspective %.4f x %.4f | ratio %.3f\n', ...
            names{index}, comparison.orthographicExtent(1), ...
            comparison.orthographicExtent(2), ...
            comparison.perspectiveExtent(1), ...
            comparison.perspectiveExtent(2), comparison.widthRatio);
    end

    sgtitle(fig1, { ...
        'Issue #010 - orthographic (top) versus perspective (bottom) projection', ...
        'All matrices and homogeneous divisions computed by cmr.graphics.Projection3D'}, ...
        'FontWeight', 'bold', 'FontSize', 11, 'Interpreter', 'none');

    file1 = fullfile(outputDirectory, 'Issue010_Orthographic_vs_Perspective.png');
    exportFigure(fig1, file1);
    outputs(end + 1, 1) = file1;
    close(fig1);

    %% Figure 2 - camera and object distance --------------------------------
    distances = [4, 8, 12];
    distanceView = cell(1, numel(distances));
    orthoSizes = zeros(1, numel(distances));
    perspSizes = zeros(1, numel(distances));
    distanceMesh = cmr.graphics.Cube3D.create(2);
    orthographicDistance = cmr.graphics.Projection3D.orthographic(4, 4, 0.1, 100);
    perspectiveDistance = cmr.graphics.Projection3D.perspective(60, 1, 0.1, 100);

    fig2 = figure('Visible', figureVisibility(), 'Color', 'w', ...
        'Name', 'Issue #010 - camera distance test', ...
        'Position', [50 60 1680 980]);
    layout2 = tiledlayout(fig2, 2, 3, ...
        'TileSpacing', 'compact', 'Padding', 'compact');

    fprintf('\nDistance test (cube side 2, camera distance d):\n');
    for index = 1:numel(distances)
        d = distances(index);
        distanceView{index} = cmr.graphics.Projection3D.viewMatrix( ...
            [0 -d 0], [0 0 0], [0 0 1]);

        orthoResult = cmr.graphics.Projection3D.projectMesh( ...
            distanceMesh, distanceView{index}, orthographicDistance);
        perspResult = cmr.graphics.Projection3D.projectMesh( ...
            distanceMesh, distanceView{index}, perspectiveDistance);
        orthoSizes(index) = orthoResult.projectedExtent(1);
        perspSizes(index) = perspResult.projectedExtent(1);

        ax = nexttile(layout2, index);
        drawProjected(ax, orthoResult, distanceMesh.color, ...
            sprintf('Orthographic, d = %d (width %.3f)', d, orthoSizes(index)));

        ax = nexttile(layout2, index + 3);
        drawProjected(ax, perspResult, distanceMesh.color, ...
            sprintf('Perspective, d = %d (width %.3f)', d, perspSizes(index)));

        measured = cmr.graphics.Projection3D.distanceToMesh( ...
            [0 -d 0], distanceMesh);
        fprintf('  d = %2d | measured distance %.2f | ortho width %.4f | perspective width %.4f\n', ...
            d, measured, orthoSizes(index), perspSizes(index));
    end

    sgtitle(fig2, { ...
        'Issue #010 - orthographic size is constant; perspective size falls with distance', ...
        'Same cube and same projection settings in every column'}, ...
        'FontWeight', 'bold', 'FontSize', 11, 'Interpreter', 'none');

    file2 = fullfile(outputDirectory, 'Issue010_Distance_Comparison.png');
    exportFigure(fig2, file2);
    outputs(end + 1, 1) = file2;
    close(fig2);

    %% Geometry export ------------------------------------------------------
    baseCube = cmr.graphics.Cube3D.create(1, [0 0 0], [0.20 0.55 0.90]);
    transform = cmr.graphics.Transform3D.compose( ...
        cmr.graphics.Transform3D.scaling(2, 3, 5), ...
        cmr.graphics.Transform3D.translation(1, 2, 3));
    boxMesh = cmr.graphics.Transform3D.transformMesh(transform, baseCube);
    boxMaterial = cmr.graphics.Material3D.fromColor( ...
        'CleanMyRoom box', boxMesh.color, ...
        'Shininess', 48, 'SpecularScale', 0.55);

    matlabExport = cmr.graphics.GeometryExporter.export( ...
        boxMesh, fullfile(meshDirectory, 'Issue010_Box_MATLAB.obj'), ...
        'Material', boxMaterial, 'CoordinateSystem', 'matlab');
    unityExport = cmr.graphics.GeometryExporter.export( ...
        boxMesh, fullfile(meshDirectory, 'Issue010_Box_Unity.obj'), ...
        'Material', boxMaterial, 'CoordinateSystem', 'unity');

    matlabOBJ = cmr.graphics.GeometryExporter.readOBJ(matlabExport.objPath);
    unityOBJ = cmr.graphics.GeometryExporter.readOBJ(unityExport.objPath);
    matlabSize = max(matlabOBJ.vertices, [], 1) - min(matlabOBJ.vertices, [], 1);
    unitySize = max(unityOBJ.vertices, [], 1) - min(unityOBJ.vertices, [], 1);

    writeManifest(meshDirectory, boxMesh, matlabExport, unityExport, ...
        matlabSize, unitySize);

    fprintf('\nExport verification:\n');
    fprintf('  MATLAB Z-up bounds : [%.3f %.3f %.3f], %d vertices, %d triangles\n', ...
        matlabSize, matlabExport.vertexCount, matlabExport.faceCount);
    fprintf('  Unity  Y-up bounds : [%.3f %.3f %.3f], centre [%.3f %.3f %.3f]\n', ...
        unitySize, mean(unityOBJ.vertices, 1));
    fprintf('  material           : Ka/Kd/Ks/Ns written to %s\n', ...
        matlabExport.mtlPath);

    %% Figure 3 - exported geometry evidence --------------------------------
    fig3 = figure('Visible', figureVisibility(), 'Color', 'w', ...
        'Name', 'Issue #010 - OBJ and MTL export', ...
        'Position', [50 70 1680 980]);
    layout3 = tiledlayout(fig3, 2, 2, ...
        'TileSpacing', 'compact', 'Padding', 'compact');

    ax = nexttile(layout3, 1);
    drawMesh(ax, boxMesh.vertices, cmr.graphics.Mesh3D.triangulate(boxMesh), ...
        boxMesh.color, 'Source mesh (MATLAB Z-up)');

    ax = nexttile(layout3, 2);
    drawMesh(ax, unityOBJ.vertices, unityOBJ.faces, ...
        boxMaterial.diffuse, 'Re-imported OBJ (Unity Y-up axes)');

    objLines = splitlines(string(fileread(char(unityExport.objPath))));
    objExcerpt = objLines(1:min(numel(objLines), 14));
    objExcerpt(end + 1) = '...';
    faceLines = objLines(startsWith(objLines, 'f '));
    objExcerpt(end + 1) = char(faceLines(1));
    objExcerpt(end + 1) = char(faceLines(end));
    showText(nexttile(layout3, 3), objExcerpt, 'OBJ excerpt (triangles + normals + material)');

    mtlText = splitlines(string(fileread(char(unityExport.mtlPath))));
    showText(nexttile(layout3, 4), mtlText, 'MTL excerpt (supported Phong fields)');

    sgtitle(fig3, { ...
        'Issue #010 - MATLAB mesh converted to OBJ + MTL', ...
        'Unity variant maps (x,y,z) -> (x,z,y); the .obj was read back with GeometryExporter.readOBJ'}, ...
        'FontWeight', 'bold', 'FontSize', 11, 'Interpreter', 'none');

    file3 = fullfile(outputDirectory, 'Issue010_OBJ_Export.png');
    exportFigure(fig3, file3);
    outputs(end + 1, 1) = file3;
    close(fig3);

    fprintf('\nSaved %d evidence figures:\n', numel(outputs));
    for index = 1:numel(outputs)
        fprintf('  %s\n', outputs(index));
    end
    fprintf('Saved geometry folder: %s\n', meshDirectory);
end

%% Local helpers -------------------------------------------------------------
function visibility = figureVisibility()
    if usejava('desktop')
        visibility = 'on';
    else
        visibility = 'off';
    end
end

function reportSaved(filePath)
    info = dir(char(filePath));
    if isempty(info) || info.bytes == 0
        error('demoProjectionExport:exportFailed', ...
            'Figure was not written or is empty: %s', filePath);
    end
    fprintf('Saved %s (%d bytes)\n', filePath, info.bytes);
end

function exportFigure(fig, filePath)
    drawnow;
    try
        exportgraphics(fig, char(filePath), 'Resolution', 130);
    catch firstError
        drawnow;
        try
            exportgraphics(fig, char(filePath), 'Resolution', 130);
        catch
            rethrow(firstError);
        end
    end
    reportSaved(filePath);
end

function drawProjected(ax, result, color, titleText)
    % Draw the projected triangles in the NDC x-y plane. Farther triangles
    % are drawn first so nearer faces stay visible.
    faces = result.faces;
    vertices = result.vertices;
    depths = mean(reshape(vertices(faces, 3), size(faces)), 2);
    [~, order] = sort(depths, 'descend');
    faces = faces(order, :);

    flatVertices = [vertices(:, 1:2), zeros(size(vertices, 1), 1)];
    patch(ax, 'Vertices', flatVertices, 'Faces', faces, ...
        'FaceColor', color, 'FaceAlpha', 0.94, ...
        'EdgeColor', [0.16 0.18 0.22], 'LineWidth', 0.45);
    axis(ax, 'equal');
    grid(ax, 'on');
    box(ax, 'on');
    xlim(ax, [-1 1]);
    ylim(ax, [-1 1]);
    xlabel(ax, 'NDC x');
    ylabel(ax, 'NDC y');
    title(ax, titleText, 'FontWeight', 'bold', 'FontSize', 9);
end

function drawMesh(ax, vertices, faces, color, titleText)
    patch(ax, 'Vertices', vertices, 'Faces', faces, ...
        'FaceColor', color, 'EdgeColor', [0.16 0.18 0.22], ...
        'LineWidth', 0.4, 'FaceLighting', 'none');
    axis(ax, 'equal');
    grid(ax, 'on');
    box(ax, 'on');
    xlabel(ax, 'x');
    ylabel(ax, 'y');
    zlabel(ax, 'z');
    view(ax, 38, 26);
    title(ax, titleText, 'FontWeight', 'bold', 'FontSize', 9);
end

function showText(ax, lines, titleText)
    axis(ax, 'off');
    if ~iscell(lines)
        lines = cellstr(lines);
    end
    text(ax, 0.02, 0.98, strjoin(lines, newline), ...
        'VerticalAlignment', 'top', 'HorizontalAlignment', 'left', ...
        'FontName', 'Consolas', 'FontSize', 9);
    title(ax, titleText, 'FontWeight', 'bold', 'FontSize', 9);
end

function writeManifest(folder, mesh, matlabExport, unityExport, ...
        matlabSize, unitySize)
    path = fullfile(folder, 'Export_Manifest.txt');
    fileID = fopen(path, 'w');
    if fileID == -1
        error('demoProjectionExport:manifestFailed', ...
            'Could not write %s', path);
    end
    cleanup = onCleanup(@() fclose(fileID)); %#ok<NASGU>

    fprintf(fileID, 'Clean My Room - Issue #010 geometry export\n');
    fprintf(fileID, 'Generated by MATLAB/Graphics/demoProjectionExport.m\n\n');
    fprintf(fileID, 'Source mesh      : %s, %d vertices, %d triangles\n', ...
        mesh.name, mesh.vertexCount, matlabExport.faceCount);
    fprintf(fileID, 'Source colour    : %.3f %.3f %.3f (RGB in [0,1])\n', mesh.color);
    fprintf(fileID, 'Triangulation    : cmr.graphics.Mesh3D.triangulate (fan)\n');
    fprintf(fileID, 'Normals          : outward vertex normals (vn, one per vertex)\n');
    fprintf(fileID, 'Faces            : f v//vn, 1-based indices\n\n');
    fprintf(fileID, '%s\n', matlabExport.objPath);
    fprintf(fileID, '  coordinate system: MATLAB Z-up right-handed\n');
    fprintf(fileID, '  bounds           : [%.3f %.3f %.3f]\n', matlabSize);
    fprintf(fileID, '  material         : %s\n', matlabExport.mtlPath);
    fprintf(fileID, '  Ka/Kd/Ks/Ns/d/illum from cmr.graphics.Material3D\n\n');
    fprintf(fileID, '%s\n', unityExport.objPath);
    fprintf(fileID, '  coordinate system: Unity Y-up, (x,y,z) -> (x,z,y)\n');
    fprintf(fileID, '  bounds           : [%.3f %.3f %.3f]\n', unitySize);
    fprintf(fileID, '  winding          : reversed because the remap is a reflection\n');
    fprintf(fileID, '  material         : %s\n', unityExport.mtlPath);
    fprintf(fileID, '\nUnity import: see Documentation/Testing/Issue010_Test_Report.md\n');
end
