%DEMOCOHENSUTHERLAND Standalone visualization of Issue #006 line clipping.
%
%   Demonstrates the manual Cohen-Sutherland implementation in
%   cmr.graphics.CohenSutherland:
%     - the 4-bit region codes and the nine regions around the window;
%     - trivial acceptance of segments that are fully inside;
%     - trivial rejection of segments that are fully outside;
%     - intersection with each of the four window boundaries;
%     - a diagonal that enters exactly through a corner, a vertical span and
%       a horizontal span.
%
%   Writes Deliverables/Screenshots/Issue006_Cohen_Sutherland_Clipping.png.
%
%   Run from anywhere with:
%
%       demoCohenSutherland
%
%   The script only calls the library class; it contains no clipping
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

outCode  = @cmr.graphics.CohenSutherland.outCode;
clipLine = @cmr.graphics.CohenSutherland.clipLine;

window = [0 0 10 10];                       % [xmin ymin xmax ymax]
xl = [-4.5 13.5];
yl = [-4.5 13.5];

%% Test segments -------------------------------------------------------------
segments = makeSegment([2 2], [8 8], 'inside');
segments(end+1) = makeSegment([-2 4], [-1 6], 'outside left');
segments(end+1) = makeSegment([11 4], [12 6], 'outside right');
segments(end+1) = makeSegment([3 11], [7 12], 'outside top');
segments(end+1) = makeSegment([3 -2], [7 -1], 'outside bottom');
segments(end+1) = makeSegment([-2 5], [4 5], 'crosses left edge');
segments(end+1) = makeSegment([6 5], [12 5], 'crosses right edge');
segments(end+1) = makeSegment([5 6], [5 12], 'crosses top edge');
segments(end+1) = makeSegment([5 -2], [5 4], 'crosses bottom edge');
segments(end+1) = makeSegment([-3 -3], [4 4], 'diagonal through corner (0,0)');
segments(end+1) = makeSegment([2 -2], [2 12], 'vertical span');
segments(end+1) = makeSegment([-2 7], [12 7], 'horizontal span');

%% Clip every segment with the library ---------------------------------------
for i = 1:numel(segments)
    segments(i).code0 = outCode(segments(i).p0, window);
    segments(i).code1 = outCode(segments(i).p1, window);
    [a, b, accepted] = clipLine(segments(i).p0, segments(i).p1, window);
    segments(i).a = a;
    segments(i).b = b;
    segments(i).accepted = accepted;
end

acceptedMask = [segments.accepted];
nAccepted = sum(acceptedMask);
nRejected = numel(segments) - nAccepted;

%% Console table -------------------------------------------------------------
fprintf('\nCohen-Sutherland clipping, window = [%g %g %g %g]\n', window);
fprintf('%3s | %-13s | %-13s | %-4s | %-4s | %-8s | %s\n', ...
    '#', 'p0', 'p1', 'c0', 'c1', 'result', 'visible part');
fprintf('%s\n', repmat('-', 1, 92));
for i = 1:numel(segments)
    s = segments(i);
    if s.accepted
        result = 'ACCEPT';
        visible = sprintf('(%.2f, %.2f)-(%.2f, %.2f)', s.a(1), s.a(2), s.b(1), s.b(2));
    else
        result = 'REJECT';
        visible = '-';
    end
    fprintf('%3d | (%5.1f,%5.1f) | (%5.1f,%5.1f) | %-4s | %-4s | %-8s | %s\n', ...
        i, s.p0(1), s.p0(2), s.p1(1), s.p1(2), ...
        dec2bin(s.code0, 4), dec2bin(s.code1, 4), result, visible);
end
fprintf('%s\n', repmat('-', 1, 92));
fprintf('Accepted: %d   Rejected: %d   Total: %d\n', ...
    nAccepted, nRejected, numel(segments));

%% Figure --------------------------------------------------------------------
fig = figure('Visible', figureVisibility(), 'Color', 'w', ...
    'Name', 'Issue #006 - Cohen-Sutherland clipping', ...
    'Position', [60 60 1700 850]);

colors = lines(numel(segments));

ax1 = subplot(1, 2, 1);
drawInputPanel(ax1, segments, window, colors, xl, yl);

ax2 = subplot(1, 2, 2);
drawResultPanel(ax2, segments, window, colors, xl, yl, nAccepted, nRejected);

sgtitle(fig, {'Issue #006 - Cohen-Sutherland line clipping (cmr.graphics.CohenSutherland)', ...
    'Region code bits: LEFT=0001  RIGHT=0010  BOTTOM=0100  TOP=1000  (y-up convention, boundary inclusive)'}, ...
    'FontWeight', 'bold', 'FontSize', 11);

file = fullfile(outputDir, 'Issue006_Cohen_Sutherland_Clipping.png');
exportgraphics(fig, file, 'Resolution', 150);
reportSaved(file);

%% Local functions -----------------------------------------------------------
function s = makeSegment(p0, p1, label)
    s = struct('p0', p0, 'p1', p1, 'label', label, ...
        'code0', 0, 'code1', 0, 'a', [], 'b', [], 'accepted', false);
end

function drawWindow(ax, window)
    rectangle(ax, 'Position', [window(1), window(2), ...
        window(3) - window(1), window(4) - window(2)], ...
        'FaceColor', [0.94 0.97 1.00], 'EdgeColor', [0 0 0], 'LineWidth', 1.8);
end

function drawRegionGrid(ax, window, xl, yl)
    plot(ax, [window(1) window(1)], yl, ':', 'Color', [0.65 0.65 0.65], 'LineWidth', 0.8);
    plot(ax, [window(3) window(3)], yl, ':', 'Color', [0.65 0.65 0.65], 'LineWidth', 0.8);
    plot(ax, xl, [window(2) window(2)], ':', 'Color', [0.65 0.65 0.65], 'LineWidth', 0.8);
    plot(ax, xl, [window(4) window(4)], ':', 'Color', [0.65 0.65 0.65], 'LineWidth', 0.8);
end

function drawRegionLabels(ax, window, xl, yl)
    xmin = window(1); ymin = window(2); xmax = window(3); ymax = window(4);
    gx = 0.05 * (xl(2) - xl(1));
    gy = 0.05 * (yl(2) - yl(1));
    xm = 0.5 * (xmin + xmax);
    ym = 0.5 * (ymin + ymax);

    places = {
        xl(1) + gx,  ym, '0001 LEFT';
        xmax + gx,   ym, '0010 RIGHT';
        xm,  yl(1) + gy, '0100 BOTTOM';
        xm,  ymax + gy,  '1000 TOP';
        xl(1) + gx, yl(1) + gy, '0101';
        xmax + gx,  yl(1) + gy, '0110';
        xl(1) + gx, ymax + gy,  '1001';
        xmax + gx,  ymax + gy,  '1010';
        xmin + 0.62 * (xmax - xmin), ymin + 0.78 * (ymax - ymin), '0000 INSIDE'};

    for i = 1:size(places, 1)
        text(ax, places{i,1}, places{i,2}, places{i,3}, ...
            'FontSize', 7, 'Color', [0.45 0.45 0.45], ...
            'FontAngle', 'italic', 'HorizontalAlignment', 'left');
    end
end

function drawInputPanel(ax, segments, window, colors, xl, yl)
    cla(ax);
    hold(ax, 'on');
    box(ax, 'on');

    drawWindow(ax, window);
    drawRegionGrid(ax, window, xl, yl);
    drawRegionLabels(ax, window, xl, yl);

    for i = 1:numel(segments)
        s = segments(i);
        plot(ax, [s.p0(1) s.p1(1)], [s.p0(2) s.p1(2)], '--', ...
            'Color', colors(i,:), 'LineWidth', 1.6);
        plot(ax, s.p0(1), s.p0(2), 'o', 'MarkerSize', 4, ...
            'MarkerEdgeColor', 'k', 'MarkerFaceColor', colors(i,:));
        plot(ax, s.p1(1), s.p1(2), 's', 'MarkerSize', 4, ...
            'MarkerEdgeColor', 'k', 'MarkerFaceColor', colors(i,:));

        mid = 0.5 * (s.p0 + s.p1);
        text(ax, mid(1), mid(2), sprintf('%d', i), 'FontSize', 9, ...
            'FontWeight', 'bold', 'HorizontalAlignment', 'center', ...
            'BackgroundColor', [1 1 1], 'Margin', 0.5);

        text(ax, s.p0(1), s.p0(2) - 0.45, dec2bin(s.code0, 4), ...
            'FontSize', 6, 'Color', [0.20 0.20 0.20], 'HorizontalAlignment', 'center');
        text(ax, s.p1(1), s.p1(2) + 0.45, dec2bin(s.code1, 4), ...
            'FontSize', 6, 'Color', [0.20 0.20 0.20], 'HorizontalAlignment', 'center');
    end

    axis(ax, 'equal');
    xlim(ax, xl);
    ylim(ax, yl);
    title(ax, 'Input: segments with endpoint region codes', 'FontWeight', 'bold', 'FontSize', 10);
    xlabel(ax, 'x');
    ylabel(ax, 'y');
    hold(ax, 'off');
end

function drawResultPanel(ax, segments, window, colors, xl, yl, nAccepted, nRejected)
    cla(ax);
    hold(ax, 'on');
    box(ax, 'on');

    drawWindow(ax, window);

    for i = 1:numel(segments)
        s = segments(i);

        % Original segment left as a faint dotted guide.
        plot(ax, [s.p0(1) s.p1(1)], [s.p0(2) s.p1(2)], ':', ...
            'Color', [0.72 0.72 0.72], 'LineWidth', 1.0);

        if s.accepted
            plot(ax, [s.a(1) s.b(1)], [s.a(2) s.b(2)], '-', ...
                'Color', colors(i,:), 'LineWidth', 3.0);
            mid = 0.5 * (s.a + s.b);
            text(ax, mid(1), mid(2), sprintf('%d', i), 'FontSize', 9, ...
                'FontWeight', 'bold', 'HorizontalAlignment', 'center', ...
                'BackgroundColor', [1 1 1], 'Margin', 0.5);
        else
            mid = 0.5 * (s.p0 + s.p1);
            plot(ax, mid(1), mid(2), 'x', 'MarkerSize', 10, ...
                'Color', [0.80 0.10 0.10], 'LineWidth', 2.0);
            text(ax, mid(1) + 0.25, mid(2) + 0.55, sprintf('%d rejected', i), ...
                'FontSize', 7, 'Color', [0.80 0.10 0.10]);
        end
    end

    axis(ax, 'equal');
    xlim(ax, xl);
    ylim(ax, yl);
    title(ax, 'Output: visible parts after clipping', 'FontWeight', 'bold', 'FontSize', 10);
    xlabel(ax, 'x');
    ylabel(ax, 'y');

    text(ax, 0.02, 0.02, sprintf('accepted = %d   rejected = %d', nAccepted, nRejected), ...
        'Units', 'normalized', 'FontSize', 9, 'FontWeight', 'bold', ...
        'BackgroundColor', [1 1 1], 'Margin', 2);

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
        error('demoCohenSutherland:exportFailed', ...
            'Figure was not written: %s', path);
    end
    fprintf('Saved %s (%d bytes)\n', path, info.bytes);
end
