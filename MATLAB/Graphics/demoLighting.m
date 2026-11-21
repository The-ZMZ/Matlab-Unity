function outputs = demoLighting(outputDirectory)
%DEMOLIGHTING Visualise Issue #009 normals, lighting and shading.
%
%   outputs = demoLighting() builds the lighting and shading evidence for
%   Issue #009 and writes three figures:
%
%       Deliverables/Screenshots/Issue009_Normals.png
%       Deliverables/Screenshots/Issue009_Lambertian_vs_Phong.png
%       Deliverables/Screenshots/Issue009_Lighting_Components.png
%
%   outputs = demoLighting(outputDirectory) writes the PNG files to the
%   requested folder instead. Every figure is closed immediately after
%   export to keep MATLAB's graphics memory use low.
%
%   All surface colours shown here are computed by cmr.graphics classes:
%
%       SurfaceNormals  face / vertex normal fields
%       Light3D         point light direction and distance attenuation
%       Material3D      ambient, diffuse, specular and shininess
%       LightingModel   ambient + Lambert diffuse + Phong specular
%       Shading3D       flat (Lambertian) and smooth (Phong) rasterisation
%
%   MATLAB's own camlight / lighting commands are deliberately NOT used for
%   the evidence: the patch objects are rendered with Lighting = 'none'.

    arguments
        outputDirectory (1,1) string = ""
    end

    here = fileparts(mfilename('fullpath'));     % MATLAB/Graphics
    matlabRoot = fileparts(here);                % MATLAB
    projectRoot = fileparts(matlabRoot);         % repository root

    addpath(genpath(matlabRoot));

    if strlength(outputDirectory) == 0
        outputDirectory = string(fullfile( ...
            projectRoot, 'Deliverables', 'Screenshots'));
    end
    if ~isfolder(outputDirectory)
        mkdir(outputDirectory);
    end

    outputs = strings(0, 1);

    fprintf('Clean My Room - Issue #009 lighting and shading\n');
    fprintf('MATLAB %s on %s\n', version, computer);

    % Shared scene definitions ----------------------------------------------
    cube = cmr.graphics.Cube3D.create( ...
        2.2, [0 0 0], [0.20 0.52 0.90]);
    sphere = cmr.graphics.Sphere3D.create( ...
        1.25, [0 0 0], 10, 16, [0.15 0.70 0.45]);
    cylinder = cmr.graphics.Cylinder3D.create( ...
        0.9, 2.2, [0 0 0], 14, [0.95 0.55 0.15]);

    meshes = {cube, sphere, cylinder};
    names = {'Cube', 'Sphere', 'Cylinder'};

    light = cmr.graphics.Light3D([4 -5 6], ...
        'Color', [1 0.95 0.85], 'Intensity', 1.2, ...
        'AmbientIntensity', 0.18);
    viewer = [6 -8 7];

    objectMaterials = { ...
        cmr.graphics.Material3D.fromColor('Cube paint', cube.color, ...
            'Shininess', 24, 'SpecularScale', 0.50), ...
        cmr.graphics.Material3D.fromColor('Sphere paint', sphere.color, ...
            'Shininess', 48, 'SpecularScale', 0.65), ...
        cmr.graphics.Material3D.fromColor('Cylinder paint', cylinder.color, ...
            'Shininess', 12, 'SpecularScale', 0.40)};

    %% Figure 1 - computed face and vertex normals --------------------------
    fig1 = figure('Visible', figureVisibility(), 'Color', 'w', ...
        'Name', 'Issue #009 - surface normals', ...
        'Position', [50 50 1680 980]);
    layout1 = tiledlayout(fig1, 2, 3, ...
        'TileSpacing', 'compact', 'Padding', 'compact');

    for index = 1:numel(meshes)
        axFace = nexttile(layout1, index);
        cmr.graphics.SurfaceNormals.visualize( ...
            axFace, meshes{index}, 'face', ...
            'Scale', 0.42, 'Color', [0.85 0.12 0.10]);
        title(axFace, sprintf('%s: face normals (%d)', ...
            names{index}, meshes{index}.faceCount), ...
            'FontWeight', 'bold', 'FontSize', 10);

        axVertex = nexttile(layout1, index + 3);
        cmr.graphics.SurfaceNormals.visualize( ...
            axVertex, meshes{index}, 'vertex', ...
            'Scale', 0.42, 'Color', [0.10 0.35 0.90]);
        title(axVertex, sprintf('%s: area-weighted vertex normals (%d)', ...
            names{index}, meshes{index}.vertexCount), ...
            'FontWeight', 'bold', 'FontSize', 10);
    end

    sgtitle(fig1, { ...
        'Issue #009 - normal fields computed in MATLAB', ...
        'Top: one outward normal per polygon face | Bottom: smooth vertex normals from incident face areas'}, ...
        'FontWeight', 'bold', 'FontSize', 11, 'Interpreter', 'none');

    file1 = fullfile(outputDirectory, 'Issue009_Normals.png');
    exportFigure(fig1, file1);
    outputs(end + 1, 1) = file1;
    close(fig1);

    %% Figure 2 - flat Lambertian versus smooth Phong -----------------------
    fig2 = figure('Visible', figureVisibility(), 'Color', 'w', ...
        'Name', 'Issue #009 - Lambertian and Phong shading', ...
        'Position', [50 60 1680 980]);
    layout2 = tiledlayout(fig2, 2, 3, ...
        'TileSpacing', 'compact', 'Padding', 'compact');

    for index = 1:numel(meshes)
        flat = cmr.graphics.Shading3D.lambertian( ...
            meshes{index}, light, objectMaterials{index}, viewer);
        axFlat = nexttile(layout2, index);
        cmr.graphics.Shading3D.render(axFlat, flat);
        title(axFlat, sprintf('Lambertian (flat): %s', lower(names{index})), ...
            'FontWeight', 'bold', 'FontSize', 10);
        verifyResult(flat, sprintf('Lambertian %s', names{index}));

        smooth = cmr.graphics.Shading3D.phong( ...
            meshes{index}, light, objectMaterials{index}, viewer);
        axSmooth = nexttile(layout2, index + 3);
        cmr.graphics.Shading3D.render(axSmooth, smooth);
        title(axSmooth, sprintf('Phong (smooth): %s', lower(names{index})), ...
            'FontWeight', 'bold', 'FontSize', 10);
        verifyResult(smooth, sprintf('Phong %s', names{index}));
    end

    sgtitle(fig2, { ...
        'Issue #009 - ambient + diffuse (flat) versus ambient + diffuse + specular (smooth)', ...
        'One shared point light and viewer; colours are computed per face or per vertex in MATLAB'}, ...
        'FontWeight', 'bold', 'FontSize', 11, 'Interpreter', 'none');

    file2 = fullfile(outputDirectory, 'Issue009_Lambertian_vs_Phong.png');
    exportFigure(fig2, file2);
    outputs(end + 1, 1) = file2;
    close(fig2);

    %% Figure 3 - lighting components and shininess -------------------------
    componentMesh = cmr.graphics.Sphere3D.create( ...
        1.4, [0 0 0], 16, 24, [0.18 0.42 0.88]);
    componentMaterial = cmr.graphics.Material3D.fromColor( ...
        'Glossy sphere', [0.18 0.42 0.88], ...
        'Shininess', 40, 'SpecularScale', 0.80);
    componentNormals = cmr.graphics.SurfaceNormals.outwardVertexNormals( ...
        componentMesh);
    components = cmr.graphics.LightingModel.illuminate( ...
        componentMaterial, light, componentMesh.vertices, ...
        componentNormals, viewer);
    phongResult = cmr.graphics.Shading3D.phong( ...
        componentMesh, light, componentMaterial, viewer);

    fig3 = figure('Visible', figureVisibility(), 'Color', 'w', ...
        'Name', 'Issue #009 - lighting components', ...
        'Position', [60 70 1680 980]);
    layout3 = tiledlayout(fig3, 2, 3, ...
        'TileSpacing', 'compact', 'Padding', 'compact');

    componentPanels = { ...
        'Ambient only', components.ambient; ...
        'Diffuse only', components.diffuse; ...
        'Specular only', components.specular; ...
        'Full Phong sum', components.total};
    for index = 1:size(componentPanels, 1)
        ax = nexttile(layout3, index);
        panel = componentPanel(phongResult, componentPanels{index, 2});
        cmr.graphics.Shading3D.render(ax, panel);
        title(ax, componentPanels{index, 1}, ...
            'FontWeight', 'bold', 'FontSize', 10);
    end

    shininessValues = [4, 96];
    for index = 1:numel(shininessValues)
        wide = cmr.graphics.Material3D.fromColor( ...
            'Broad highlight', [0.18 0.42 0.88], ...
            'Shininess', shininessValues(1), 'SpecularScale', 0.80);
        tight = cmr.graphics.Material3D.fromColor( ...
            'Tight highlight', [0.18 0.42 0.88], ...
            'Shininess', shininessValues(2), 'SpecularScale', 0.80);
        materials = {wide, tight};
        ax = nexttile(layout3, index + 4);
        result = cmr.graphics.Shading3D.phong( ...
            componentMesh, light, materials{index}, viewer);
        cmr.graphics.Shading3D.render(ax, result);
        title(ax, sprintf('Shininess = %d', shininessValues(index)), ...
            'FontWeight', 'bold', 'FontSize', 10);
        verifyResult(result, sprintf('Shininess %d', shininessValues(index)));
    end

    sgtitle(fig3, { ...
        'Issue #009 - Phong lighting components and material shininess', ...
        'First three panels isolate the computed terms; last panel combines them; right: wide vs tight highlight'}, ...
        'FontWeight', 'bold', 'FontSize', 11, 'Interpreter', 'none');

    file3 = fullfile(outputDirectory, 'Issue009_Lighting_Components.png');
    exportFigure(fig3, file3);
    outputs(end + 1, 1) = file3;
    close(fig3);

    %% Console verification summary -----------------------------------------
    ambientMean = mean(components.ambient(:));
    diffuseMean = mean(components.diffuse(:));
    specularMean = mean(components.specular(:));
    [~, fullBrightest] = max(mean(components.total, 2));

    fprintf('\nLighting verification:\n');
    fprintf('  light position     : [%g %g %g]\n', light.position);
    fprintf('  light intensity    : %.2f, ambient %.2f\n', ...
        light.intensity, light.ambientIntensity);
    fprintf('  mean RGB terms     : ambient %.4f, diffuse %.4f, specular %.4f\n', ...
        ambientMean, diffuseMean, specularMean);
    fprintf('  brightest vertex   : %d of %d\n', ...
        fullBrightest, componentMesh.vertexCount);
    fprintf('  specular present   : %s\n', ...
        mat2str(specularMean > 0));

    fprintf('\nSaved %d evidence figures:\n', numel(outputs));
    for index = 1:numel(outputs)
        fprintf('  %s\n', outputs(index));
    end
end

%% Local helpers -------------------------------------------------------------
function panel = componentPanel(base, colors)
    % Build a renderable result from one lighting component.
    colors = min(max(colors, 0), 1);
    faces = base.faces;

    panel = base;
    panel.method = 'phong';
    panel.colorLocation = 'vertex';
    panel.vertexColors = colors;
    panel.faceColors = (colors(faces(:, 1), :) ...
        + colors(faces(:, 2), :) + colors(faces(:, 3), :)) / 3;
end

function verifyResult(result, label)
    if ~all(isfinite(result.faceColors(:))) ...
            || any(result.faceColors(:) < 0) ...
            || any(result.faceColors(:) > 1)
        error('demoLighting:invalidFaceColors', ...
            '%s produced invalid face colours.', label);
    end
    if ~all(isfinite(result.vertexColors(:))) ...
            || any(result.vertexColors(:) < 0) ...
            || any(result.vertexColors(:) > 1)
        error('demoLighting:invalidVertexColors', ...
            '%s produced invalid vertex colours.', label);
    end
end

function visibility = figureVisibility()
    % Show windows in the MATLAB desktop, stay headless in matlab -batch.
    if usejava('desktop')
        visibility = 'on';
    else
        visibility = 'off';
    end
end

function reportSaved(filePath)
    info = dir(char(filePath));
    if isempty(info)
        error('demoLighting:exportFailed', ...
            'Figure was not written: %s', filePath);
    end
    fprintf('Saved %s (%d bytes)\n', filePath, info.bytes);
end

function exportFigure(fig, filePath)
    % One retry recovers the transient headless export failure seen on this
    % machine; a repeated failure is rethrown rather than hidden.
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
