classdef Canvas < handle
%CANVAS Off-screen RGB raster used to display and export algorithm output.
%
%   The canvas only stores and displays pixels. All line, circle, shape and
%   fill coordinates are produced by the other cmr.graphics classes; this
%   class never draws with MATLAB line, rectangle, circle, patch or fill
%   primitives. Pixel (0, 0) is the top-left pixel and y grows downwards,
%   matching the activity-space convention used by the project.

    properties (SetAccess = private)
        Width (1,1) double
        Height (1,1) double
        Background (1,3) double
    end

    properties (Access = private)
        Buffer uint8
    end

    methods
        function obj = Canvas(width, height, background)
            %CANVAS Create a raster canvas with a uniform background colour.
            arguments
                width (1,1) double
                height (1,1) double
                background (1,3) double = [1, 1, 1]
            end

            cmr.graphics.Canvas.checkSize(width, height);
            cmr.graphics.Canvas.checkColor(background);

            obj.Width = round(width);
            obj.Height = round(height);
            obj.Background = background;

            rgb = uint8(round(background * 255));
            obj.Buffer = repmat(reshape(rgb, 1, 1, 3), obj.Height, obj.Width, 1);
        end

        function count = drawPoints(obj, points, color)
            %DRAWPOINTS Set the given pixels to an RGB colour.
            %
            %   Pixels outside the canvas are clipped. The returned count is
            %   the number of clipped points.
            arguments
                obj
                points (:,2) double
                color (1,3) double
            end

            if ~isreal(points) || any(~isfinite(points(:)))
                error('cmr:graphics:Canvas:invalidPoints', ...
                    'Canvas points must be finite real coordinates.');
            end
            cmr.graphics.Canvas.checkColor(color);

            x = round(points(:, 1));
            y = round(points(:, 2));

            inside = x >= 0 & x < obj.Width & y >= 0 & y < obj.Height;
            count = sum(~inside);
            if ~any(inside)
                return
            end

            indices = sub2ind( ...
                [obj.Height, obj.Width], ...
                y(inside) + 1, ...
                x(inside) + 1);

            rgb = uint8(round(color * 255));
            for channel = 1:3
                slice = obj.Buffer(:, :, channel);
                slice(indices) = rgb(channel);
                obj.Buffer(:, :, channel) = slice;
            end
        end

        function rgb = pixel(obj, x, y)
            %PIXEL Read one pixel as normalized RGB in [0, 1].
            arguments
                obj
                x (1,1) double
                y (1,1) double
            end

            if ~isreal(x) || ~isreal(y) || ~isfinite(x) || ~isfinite(y)
                error('cmr:graphics:Canvas:invalidPoints', ...
                    'Canvas coordinates must be finite real numbers.');
            end
            x = round(x);
            y = round(y);
            if x < 0 || x >= obj.Width || y < 0 || y >= obj.Height
                error('cmr:graphics:Canvas:outOfBounds', ...
                    'Pixel (%d, %d) is outside the %d x %d canvas.', ...
                    x, y, obj.Width, obj.Height);
            end

            rgb = reshape(double(obj.Buffer(y + 1, x + 1, :)), 1, 3) / 255;
        end

        function data = imageData(obj)
            %IMAGEDATA Return the current framebuffer as an RGB uint8 image.
            data = obj.Buffer;
        end

        function clear(obj)
            %CLEAR Reset every pixel to the background colour.
            rgb = uint8(round(obj.Background * 255));
            obj.Buffer = repmat(reshape(rgb, 1, 1, 3), obj.Height, obj.Width, 1);
        end

        function fig = render(obj, ax, titleText)
            %RENDER Display the framebuffer on the given axes.
            %
            %   The axes receive an image of the precomputed buffer plus pixel
            %   coordinate ticks. This is presentation only; no drawing
            %   primitive performs the algorithms.
            arguments
                obj
                ax
                titleText (1,1) string = ""
            end

            image(ax, 0:(obj.Width - 1), 0:(obj.Height - 1), obj.imageData());
            set(ax, 'YDir', 'reverse');
            axis(ax, 'image');
            xlim(ax, [-0.5, obj.Width - 0.5]);
            ylim(ax, [-0.5, obj.Height - 0.5]);

            step = max(1, round(max(obj.Width, obj.Height) / 10));
            xticks(ax, 0:step:(obj.Width - 1));
            yticks(ax, 0:step:(obj.Height - 1));

            xlabel(ax, 'x (pixels)');
            ylabel(ax, 'y (pixels)');
            title(ax, titleText, 'Interpreter', 'none');
            box(ax, 'on');
            set(ax, 'Color', [0.98, 0.98, 0.98]);

            fig = ancestor(ax, 'figure');
        end

        function filePath = savePng(obj, filePath, titleText)
            %SAVEPNG Render the framebuffer into a PNG evidence file.
            %
            %   The figure is created invisibly so the export works in batch
            %   mode. Missing parent folders are created.
            arguments
                obj
                filePath (1,1) string
                titleText (1,1) string = ""
            end

            filePath = string(filePath);
            folderPath = string(fileparts(char(filePath)));
            if strlength(folderPath) > 0 && ~isfolder(folderPath)
                mkdir(folderPath);
            end

            fig = figure( ...
                'Visible', 'off', ...
                'Color', 'w', ...
                'Units', 'pixels', ...
                'Position', [100, 100, 960, 720]);
            cleanup = onCleanup(@() close(fig)); %#ok<NASGU>

            ax = axes(fig);
            obj.render(ax, titleText);
            exportgraphics(fig, char(filePath), 'Resolution', 144);
        end
    end

    methods (Static, Access = private)
        function checkSize(width, height)
            valid = @(value) isreal(value) && isfinite(value) ...
                && value >= 1 && value == round(value);
            if ~valid(width) || ~valid(height)
                error('cmr:graphics:Canvas:invalidSize', ...
                    'Canvas width and height must be positive integer values.');
            end
        end

        function checkColor(color)
            if ~isreal(color) || any(~isfinite(color)) ...
                    || any(color < 0) || any(color > 1)
                error('cmr:graphics:Canvas:invalidColor', ...
                    'Canvas colour must contain three RGB components in [0, 1].');
            end
        end
    end
end
