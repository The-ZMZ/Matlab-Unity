%DEMO2DTRANSFORMATIONS Standalone visualization of Issue #006 2D transforms.
%
%   Renders every transformation implemented by cmr.graphics.Transform2D and
%   demonstrates why composition order matters. Two PNG files are written to
%   Deliverables/Screenshots:
%
%       Issue006_2D_Transformations.png
%       Issue006_Composite_Order.png
%
%   Run from anywhere with:
%
%       demo2DTransformations
%
%   The script only calls the library class; it contains no transformation
%   mathematics of its own.

clear variables; clc;

%% Locate the project and put the graphics package on the path ---------------
here = fileparts(mfilename('fullpath'));    % MATLAB/Graphics
matlabRoot = fileparts(here);               % MATLAB
projectRoot = fileparts(matlabRoot);        % repository root

addpath(matlabRoot);
addpath(here);

outputDir = fullfile(projectRoot, 'Deliverables', 'Screenshots');
if ~isfolder(outputDir)
    mkdir(outputDir);
end

% Handles to the library so the panel table below stays readable.
identity       = @cmr.graphics.Transform2D.identity;
translation    = @cmr.graphics.Transform2D.translation;
rotation       = @cmr.graphics.Transform2D.rotation;
scaling        = @cmr.graphics.Transform2D.scaling;
reflectionY    = @cmr.graphics.Transform2D.reflectionY;
shearX         = @cmr.graphics.Transform2D.shearX;
reflectionLine = @cmr.graphics.Transform2D.reflectionAboutLine;
aboutPoint     = @cmr.graphics.Transform2D.aboutPoint;
compose        = @cmr.graphics.Transform2D.compose;
apply          = @cmr.graphics.Transform2D.apply;

%% Reference shape -----------------------------------------------------------
% Asymmetric L-shape: rotation, reflection and shear are unambiguous, unlike
% a square or a circle which hide several transformations.
shape = [0   0;
         2.5 0;
         2.5 1.2;
         1.1 1.2;
         1.1 3.0;
         0   3.0];

pivot = [1.1 1.2];                          % inner corner of the L
linePoint = [0 1];                          % arbitrary reflection line y = 0.5x + 1
lineTheta = atan(0.5);

%% Panel table ---------------------------------------------------------------
panels = makePanel('Original shape', identity(), '');
panels(end+1) = makePanel('Translation  t = (3, 1)', translation(3, 1), '');
panels(end+1) = makePanel('Rotation  theta = 45 deg (CCW)', rotation(pi/4), '');
panels(end+1) = makePanel('Scaling  s = (1.6, 0.6)', scaling(1.6, 0.6), '');
panels(end+1) = makePanel('Reflection about the y-axis', reflectionY(), '');
panels(end+1) = makePanel('Shear  kx = 0.8', shearX(0.8), '');
panels(end+1) = makePanel(sprintf('Rotation 90 deg about pivot (%.1f, %.1f)', ...
    pivot(1), pivot(2)), aboutPoint(rotation(pi/2), pivot), 'pivot');
panels(end+1) = makePanel('Reflection about the line y = 0.5x + 1', ...
    reflectionLine(linePoint, lineTheta), 'line');

for i = 1:numel(panels)
    panels(i).Q = apply(panels(i).M, shape);
end

% Shared limits so the panels can be compared directly.
allPoints = shape;
for i = 1:numel(panels)
    allPoints = [allPoints; panels(i).Q]; %#ok<AGROW>
end
pad = 0.9;
xl = [min(allPoints(:,1)) - pad, max(allPoints(:,1)) + pad];
yl = [min(allPoints(:,2)) - pad, max(allPoints(:,2)) + pad];

%% Figure 1 - one panel per transformation -----------------------------------
fig1 = figure('Visible', figureVisibility(), 'Color', 'w', ...
    'Name', 'Issue #006 - 2D transformations', ...
    'Position', [60 60 1760 860]);

for i = 1:numel(panels)
    ax = subplot(2, 4, i);
    extra = struct('Pivot', [], 'Line', []);
    if strcmp(panels(i).Extra, 'pivot')
        extra.Pivot = pivot;
    elseif strcmp(panels(i).Extra, 'line')
        xs = xl;
        extra.Line = [xs(1), 0.5 * xs(1) + 1; xs(2), 0.5 * xs(2) + 1];
        extra.Pivot = linePoint;
    end
    drawPanel(ax, shape, panels(i).Q, panels(i).Title, panels(i).M, xl, yl, extra);
end

sgtitle(fig1, {'Issue #006 - 2D geometric transformations (cmr.graphics.Transform2D)', ...
    'Grey dashed = original shape, blue = transformed shape, bottom-left text = 3x3 homogeneous matrix'}, ...
    'FontWeight', 'bold', 'FontSize', 11);

file1 = fullfile(outputDir, 'Issue006_2D_Transformations.png');
exportgraphics(fig1, file1, 'Resolution', 150);
reportSaved(file1);

%% Figure 2 - composition order ----------------------------------------------
T = translation(4, 1);
R = rotation(pi/3);
M_TR = compose(T, R);   % T applied first, then R:  M = R * T
M_RT = compose(R, T);   % R applied first, then T:  M = T * R

order = {
    'Original shape',                  identity(), 'identity';
    'Step 1: translate, step 2: rotate', M_TR,    'M = R * T';
    'Step 1: rotate, step 2: translate', M_RT,    'M = T * R'
};

orderPoints = shape;
for i = 1:size(order, 1)
    orderPoints = [orderPoints; apply(order{i,2}, shape)]; %#ok<AGROW>
end
pad2 = 1.0;
xl2 = [min(orderPoints(:,1)) - pad2, max(orderPoints(:,1)) + pad2];
yl2 = [min(orderPoints(:,2)) - pad2, max(orderPoints(:,2)) + pad2];

fig2 = figure('Visible', figureVisibility(), 'Color', 'w', ...
    'Name', 'Issue #006 - composite order', ...
    'Position', [80 80 1500 620]);

for i = 1:size(order, 1)
    ax = subplot(1, 3, i);
    Q = apply(order{i,2}, shape);
    drawPanel(ax, shape, Q, order{i,1}, order{i,2}, xl2, yl2, ...
        struct('Pivot', [], 'Line', []));
    % Track the vertex that starts at the origin so the different orders are
    % numerically comparable, not just visually different.
    hold(ax, 'on');
    plot(ax, Q(1,1), Q(1,2), 'o', 'MarkerSize', 8, ...
        'MarkerEdgeColor', 'k', 'MarkerFaceColor', [0.90 0.20 0.20]);
    text(ax, Q(1,1) + 0.25, Q(1,2) + 0.25, ...
        sprintf('v1 = (%.2f, %.2f)', Q(1,1), Q(1,2)), ...
        'FontSize', 8, 'Color', [0.70 0.10 0.10], 'FontWeight', 'bold');
    hold(ax, 'off');
end

sgtitle(fig2, {'Issue #006 - Composition order: compose(A, B) applies A first and returns M = B * A', ...
    'Same two operations, different order, different result (vertex v1 tracks the origin)'}, ...
    'FontWeight', 'bold', 'FontSize', 11);

file2 = fullfile(outputDir, 'Issue006_Composite_Order.png');
exportgraphics(fig2, file2, 'Resolution', 150);
reportSaved(file2);

%% Console summary -----------------------------------------------------------
fprintf('Translation matrix T(4,1):\n');
disp(T);
fprintf('Rotation matrix R(60 deg):\n');
disp(R);
fprintf('compose(T, R) = R*T  -> v1 = (%.4f, %.4f)\n', ...
    apply(M_TR, shape(1,:)));
fprintf('compose(R, T) = T*R  -> v1 = (%.4f, %.4f)\n', ...
    apply(M_RT, shape(1,:)));
fprintf('The two results differ, which is why the order of composition matters.\n');

%% Local functions -----------------------------------------------------------
function p = makePanel(titleText, M, extra)
    p = struct('Title', titleText, 'M', M, 'Extra', extra, 'Q', []);
end

function drawPanel(ax, shape, transformed, titleText, M, xl, yl, extra)
    cla(ax);
    hold(ax, 'on');
    box(ax, 'on');
    grid(ax, 'on');

    patch(ax, shape(:,1), shape(:,2), [0.88 0.88 0.88], ...
        'EdgeColor', [0.55 0.55 0.55], 'LineStyle', '--', 'LineWidth', 1.0);

    patch(ax, transformed(:,1), transformed(:,2), [0.29 0.56 0.89], ...
        'EdgeColor', [0.05 0.20 0.45], 'LineWidth', 1.5);

    if ~isempty(extra.Line)
        plot(ax, extra.Line(:,1), extra.Line(:,2), '-', ...
            'Color', [0.85 0.33 0.10], 'LineWidth', 1.2);
    end
    if ~isempty(extra.Pivot)
        plot(ax, extra.Pivot(1), extra.Pivot(2), 'o', ...
            'MarkerSize', 7, 'MarkerEdgeColor', 'k', ...
            'MarkerFaceColor', [0.95 0.75 0.10]);
    end

    axis(ax, 'equal');
    xlim(ax, xl);
    ylim(ax, yl);

    title(ax, titleText, 'FontWeight', 'bold', 'FontSize', 10);

    label = sprintf('%6.2f %6.2f %6.2f\n%6.2f %6.2f %6.2f\n%6.2f %6.2f %6.2f', M.');
    text(ax, 0.03, 0.05, label, 'Units', 'normalized', ...
        'FontName', 'FixedWidth', 'FontSize', 7, 'Color', [0.15 0.15 0.15], ...
        'BackgroundColor', [1 1 1], 'Margin', 1, ...
        'VerticalAlignment', 'bottom', 'Interpreter', 'none');

    hold(ax, 'off');
end

function v = figureVisibility()
    % Show figure windows in the desktop, stay headless in -batch.
    if usejava('desktop')
        v = 'on';
    else
        v = 'off';
    end
end

function reportSaved(path)
    info = dir(path);
    if isempty(info)
        error('demo2DTransformations:exportFailed', ...
            'Figure was not written: %s', path);
    end
    fprintf('Saved %s (%d bytes)\n', path, info.bytes);
end
