function outputs = demo3DTransformations(outputDirectory)
%DEMO3DTRANSFORMATIONS Visualise Issue #008 3D geometric transformations.
%
%   outputs = demo3DTransformations() renders original and transformed Issue
%   #007 meshes and writes three evidence figures:
%
%       Deliverables/Screenshots/Issue008_Basic_Transformations.png
%       Deliverables/Screenshots/Issue008_Transformation_Order.png
%       Deliverables/Screenshots/Issue008_Transformed_Objects.png
%
%   outputs = demo3DTransformations(outputDirectory) writes the PNG files to
%   the requested folder instead. Figure windows are visible in the MATLAB
%   desktop and invisible under matlab -batch, matching demo3DObjects.m.
%
%   The demo uses base MATLAB only and does not modify the Issue #006 or
%   Issue #007 meshes, tests or documentation.

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

    fprintf('Clean My Room - Issue #008 3D geometric transformations\n');
    fprintf('MATLAB %s on %s\n', version, computer);

    %% Figure 1 - one primitive, six basic operations ----------------------
    cube = cmr.graphics.Cube3D.create(2, [0 0 0], [0.20 0.52 0.90]);

    operations = { ...
        'Translation  T([2.5 1.2 0.8])', ...
            cmr.graphics.Transform3D.translation(2.5, 1.2, 0.8), [0.20 0.52 0.90]; ...
        'Rotation X  45 degrees', ...
            cmr.graphics.Transform3D.rotationX(45, 'degrees'), [0.15 0.70 0.45]; ...
        'Rotation Y  45 degrees', ...
            cmr.graphics.Transform3D.rotationY(45, 'degrees'), [0.95 0.55 0.15]; ...
        'Rotation Z  45 degrees', ...
            cmr.graphics.Transform3D.rotationZ(45, 'degrees'), [0.62 0.35 0.90]; ...
        'Scaling  [1.8 0.8 1.2]', ...
            cmr.graphics.Transform3D.scaling(1.8, 0.8, 1.2), [0.90 0.25 0.35]; ...
        'Combined: scale, rotate Z, translate', ...
            cmr.graphics.Transform3D.compose( ...
                cmr.graphics.Transform3D.scaling(1, 1.2, 1), ...
                cmr.graphics.Transform3D.rotationZ(35, 'degrees'), ...
                cmr.graphics.Transform3D.translation(2, 0, 0)), ...
            [0.10 0.65 0.75]};

    fig1 = figure('Visible', figureVisibility(), 'Color', 'w', ...
        'Name', 'Issue #008 - basic 3D transformations', ...
        'Position', [50 50 1680 980]);
    layout1 = tiledlayout(fig1, 2, 3, ...
        'TileSpacing', 'compact', 'Padding', 'compact');

    for index = 1:size(operations, 1)
        ax = nexttile(layout1);
        matrix = operations{index, 2};
        changed = cmr.graphics.Transform3D.transformMesh(matrix, cube);
        changed.color = operations{index, 3};
        drawComparison(ax, cube, changed, operations{index, 1}, 3.6);
        verifyMesh(changed, sprintf('basic operation %d', index));
    end

    sgtitle(fig1, { ...
        'Issue #008 - homogeneous 4x4 geometric transformations', ...
        'Grey wireframe = original cube | Solid colour = transformed cube | Header row of T holds translation'}, ...
        'FontWeight', 'bold', 'FontSize', 11, 'Interpreter', 'none');

    file1 = fullfile(outputDirectory, 'Issue008_Basic_Transformations.png');
    exportFigure(fig1, file1);
    outputs(end + 1, 1) = file1;
    maybeCloseFigure(fig1);

    %% Figure 2 - transformation order -------------------------------------
    scaleTwice = cmr.graphics.Transform3D.scaling(2, 1, 1);
    shift = cmr.graphics.Transform3D.translation(1.5, 0, 0);

    translateThenScale = cmr.graphics.Transform3D.compose(shift, scaleTwice);
    scaleThenTranslate = cmr.graphics.Transform3D.compose(scaleTwice, shift);
    point = [1 0 0];
    firstResult = cmr.graphics.Transform3D.transformPoints(translateThenScale, point);
    secondResult = cmr.graphics.Transform3D.transformPoints(scaleThenTranslate, point);

    fig2 = figure('Visible', figureVisibility(), 'Color', 'w', ...
        'Name', 'Issue #008 - transformation order', ...
        'Position', [80 90 1360 560]);
    layout2 = tiledlayout(fig2, 1, 2, ...
        'TileSpacing', 'compact', 'Padding', 'compact');

    axOrder1 = nexttile(layout2);
    firstMesh = cmr.graphics.Transform3D.transformMesh(translateThenScale, cube);
    firstMesh.color = [0.15 0.55 0.90];
    drawComparison(axOrder1, cube, firstMesh, ...
        'Translate first, then scale: T*S', 5.6);
    text(axOrder1, 0, 0, 2.6, sprintf('[1 0 0] -> [%.1f %.0f %.0f]', ...
        firstResult(1), firstResult(2), firstResult(3)), ...
        'HorizontalAlignment', 'center', 'FontWeight', 'bold', ...
        'FontSize', 9, 'Color', [0.05 0.25 0.55]);

    axOrder2 = nexttile(layout2);
    secondMesh = cmr.graphics.Transform3D.transformMesh(scaleThenTranslate, cube);
    secondMesh.color = [0.95 0.45 0.10];
    drawComparison(axOrder2, cube, secondMesh, ...
        'Scale first, then translate: S*T', 5.6);
    text(axOrder2, 0, 0, 2.6, sprintf('[1 0 0] -> [%.1f %.0f %.0f]', ...
        secondResult(1), secondResult(2), secondResult(3)), ...
        'HorizontalAlignment', 'center', 'FontWeight', 'bold', ...
        'FontSize', 9, 'Color', [0.65 0.25 0.05]);

    sgtitle(fig2, { ...
        'Issue #008 - matrix multiplication order changes the result', ...
        'COMPOSE(T,S) applies T first; both panels combine the same two matrices in a different order'}, ...
        'FontWeight', 'bold', 'FontSize', 11, 'Interpreter', 'none');

    file2 = fullfile(outputDirectory, 'Issue008_Transformation_Order.png');
    exportFigure(fig2, file2);
    outputs(end + 1, 1) = file2;
    maybeCloseFigure(fig2);

    %% Figure 3 - all three Issue #007 primitives --------------------------
    sphere = cmr.graphics.Sphere3D.create( ...
        1.15, [0 0 0], 14, 20, [0.15 0.70 0.45]);
    cylinder = cmr.graphics.Cylinder3D.create( ...
        0.9, 2.1, [0 0 0], 20, [0.95 0.55 0.15]);

    sceneTransform = cmr.graphics.Transform3D.compose( ...
        cmr.graphics.Transform3D.scaling(1.15, 1.15, 1.15), ...
        cmr.graphics.Transform3D.rotationY(25, 'degrees'), ...
        cmr.graphics.Transform3D.rotationX(20, 'degrees'), ...
        cmr.graphics.Transform3D.translation(2.6, 0, 0));

    originals = {cube, sphere, cylinder};
    names = {'Cube', 'Sphere', 'Cylinder'};

    fig3 = figure('Visible', figureVisibility(), 'Color', 'w', ...
        'Name', 'Issue #008 - transformed primitive objects', ...
        'Position', [50 60 1680 650]);
    layout3 = tiledlayout(fig3, 2, 3, ...
        'TileSpacing', 'compact', 'Padding', 'compact');

    for index = 1:numel(originals)
        transformed = cmr.graphics.Transform3D.transformMesh( ...
            sceneTransform, originals{index});
        verifyMesh(transformed, names{index});

        axOriginal = nexttile(layout3);
        drawSingle(axOriginal, originals{index}, ...
            [0.55 0.58 0.62], 0.30, ...
            sprintf('Original %s', lower(names{index})), 4.4);

        axTransformed = nexttile(layout3, index + 3);
        drawSingle(axTransformed, transformed, ...
            originals{index}.color, 0.92, ...
            sprintf('Transformed %s', lower(names{index})), 4.4);
    end

    sgtitle(fig3, { ...
        'Issue #008 - original and transformed objects from Issue #007 meshes', ...
        'One shared 4x4 matrix: uniform scale -> rotate Y -> rotate X -> translate [2.6 0 0]'}, ...
        'FontWeight', 'bold', 'FontSize', 11);

    file3 = fullfile(outputDirectory, 'Issue008_Transformed_Objects.png');
    exportFigure(fig3, file3);
    outputs(end + 1, 1) = file3;
    maybeCloseFigure(fig3);

    %% Console verification summary ----------------------------------------
    fprintf('\nTransformation verification:\n');
    fprintf('  T*S on [1 0 0] : [%.1f %.0f %.0f]\n', ...
        firstResult(1), firstResult(2), firstResult(3));
    fprintf('  S*T on [1 0 0] : [%.1f %.0f %.0f]\n', ...
        secondResult(1), secondResult(2), secondResult(3));
    fprintf('  Orders differ  : %s\n', ...
        mat2str(~(norm(firstResult - secondResult) < 1e-12)));

    for index = 1:numel(originals)
        transformed = cmr.graphics.Transform3D.transformMesh( ...
            sceneTransform, originals{index});
        [minimum, maximum] = cmr.graphics.Mesh3D.bounds(transformed);
        fprintf('  %-8s : %4d vertices, %4d faces, bounds [%.2f %.2f %.2f] to [%.2f %.2f %.2f]\n', ...
            lower(names{index}), transformed.vertexCount, ...
            transformed.faceCount, minimum, maximum);
    end

    fprintf('\nSaved %d evidence figures:\n', numel(outputs));
    for index = 1:numel(outputs)
        fprintf('  %s\n', outputs(index));
    end
end

%% Local drawing helpers ----------------------------------------------------
function faceMatrix = meshFaces(mesh)
    % Patch needs equal-width face rows. Keep native quads where possible and
    % triangulate only the mixed-face sphere/cylinder meshes.
    widths = cellfun(@numel, mesh.faces);
    if all(widths == widths(1))
        faceMatrix = cell2mat(mesh.faces);
    else
        faceMatrix = cmr.graphics.Mesh3D.triangulate(mesh);
    end
end

function handle = drawMesh(ax, mesh, color, faceAlpha)
    handle = patch(ax, 'Vertices', mesh.vertices, 'Faces', meshFaces(mesh), ...
        'FaceColor', color, 'FaceAlpha', faceAlpha, ...
        'EdgeColor', [0.12 0.15 0.20], 'LineWidth', 0.8, ...
        'FaceLighting', 'flat');
end

function drawComparison(ax, original, transformed, titleText, axisLimit)
    wasHeld = ishold(ax);
    hold(ax, 'on');

    originalHandle = drawMesh(ax, original, [0.82 0.84 0.86], 0.22);
    transformedHandle = drawMesh(ax, transformed, transformed.color, 0.90);

    styleAxes(ax, titleText, axisLimit);
    legend(ax, [originalHandle, transformedHandle], ...
        {'Original', 'Transformed'}, 'Location', 'northwest', ...
        'FontSize', 8, 'Box', 'off');

    if ~wasHeld
        hold(ax, 'off');
    end
end

function drawSingle(ax, mesh, color, faceAlpha, titleText, axisLimit)
    drawMesh(ax, mesh, color, faceAlpha);
    styleAxes(ax, titleText, axisLimit);
end

function styleAxes(ax, titleText, axisLimit)
    axis(ax, 'equal');
    grid(ax, 'on');
    box(ax, 'on');
    xlim(ax, [-axisLimit, axisLimit]);
    ylim(ax, [-axisLimit, axisLimit]);
    zlim(ax, [-axisLimit, axisLimit]);
    xlabel(ax, 'x');
    ylabel(ax, 'y');
    zlabel(ax, 'z');
    view(ax, 45, 25);
    camlight(ax, 'headlight');
    material(ax, 'dull');
    title(ax, titleText, 'FontWeight', 'bold', 'FontSize', 10, ...
        'Interpreter', 'none');
end

function verifyMesh(mesh, label)
    if ~all(isfinite(mesh.vertices(:)))
        error('demo3DTransformations:nonFiniteVertices', ...
            '%s produced non-finite vertices.', label);
    end
    if mesh.vertexCount ~= size(mesh.vertices, 1) ...
            || mesh.faceCount ~= numel(mesh.faces)
        error('demo3DTransformations:invalidMeshCounts', ...
            '%s produced inconsistent mesh counts.', label);
    end
end

function visibility = figureVisibility()
    % Show windows in the desktop, stay headless in matlab -batch.
    if usejava('desktop')
        visibility = 'on';
    else
        visibility = 'off';
    end
end

function reportSaved(filePath)
    info = dir(filePath);
    if isempty(info)
        error('demo3DTransformations:exportFailed', ...
            'Figure was not written: %s', filePath);
    end
    fprintf('Saved %s (%d bytes)\n', filePath, info.bytes);
end

function exportFigure(fig, filePath)
    % One retry recovers the transient headless exportgraphics failure seen on
    % this machine; a repeated failure is rethrown rather than hidden.
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

function maybeCloseFigure(fig)
    if ~usejava('desktop')
        close(fig);
    end
end
