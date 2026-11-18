classdef Light3D
%LIGHT3D Point light source for MATLAB lighting calculations.
%
%   A light has a finite world-space POSITION, an RGB COLOR, an INTENSITY and
%   an AMBIENTINTENSITY. The position defines the direction from each surface
%   point toward the source and a simple distance attenuation. All values are
%   stored as doubles; the class is a value object and can be copied safely.
%
%   Example:
%       light = cmr.graphics.Light3D([4 -5 6], ...
%           'Color', [1 0.95 0.85], 'Intensity', 1.2, ...
%           'AmbientIntensity', 0.18);
%       direction = light.directionTo([0 0 0]);  % unit vector toward light

    properties (SetAccess = private)
        position
        color
        intensity
        ambientIntensity
    end

    methods
        function obj = Light3D(position, options)
            %LIGHT3D Construct a point light.
            %
            %   Light3D(position) uses white light, intensity 1 and ambient
            %   intensity 0.15. Name-value options override the defaults.
            arguments
                position (1,3) double
                options.Color (1,3) double = [1, 1, 1]
                options.Intensity (1,1) double = 1
                options.AmbientIntensity (1,1) double = 0.15
            end

            obj.position = double(position);
            obj.color = double(options.Color);

            if ~(isreal(obj.position) && all(isfinite(obj.position(:))))
                error('cmr:graphics:Light3D:invalidPosition', ...
                    'Light position must contain finite real coordinates.');
            end
            if ~(isreal(obj.color) && all(isfinite(obj.color(:))) ...
                    && all(obj.color(:) >= 0) && all(obj.color(:) <= 1))
                error('cmr:graphics:Light3D:invalidColor', ...
                    'Light colour must contain three RGB components in [0, 1].');
            end
            if ~(isreal(options.Intensity) && isfinite(options.Intensity) ...
                    && options.Intensity >= 0)
                error('cmr:graphics:Light3D:invalidIntensity', ...
                    'Light intensity must be a finite non-negative scalar.');
            end
            if ~(isreal(options.AmbientIntensity) ...
                    && isfinite(options.AmbientIntensity) ...
                    && options.AmbientIntensity >= 0 ...
                    && options.AmbientIntensity <= 1)
                error('cmr:graphics:Light3D:invalidAmbientIntensity', ...
                    'Ambient intensity must be a finite scalar in [0, 1].');
            end

            obj.intensity = double(options.Intensity);
            obj.ambientIntensity = double(options.AmbientIntensity);
        end

        function ambient = ambientColor(obj)
            %AMBIENTCOLOR Effective ambient light colour.
            %
            %   ambient = light.color * intensity * ambientIntensity.
            ambient = obj.color .* (obj.intensity * obj.ambientIntensity);
        end

        function directions = directionTo(obj, points)
            %DIRECTIONTO Unit vectors from surface points toward the light.
            %
            %   directions = light.directionTo(points) returns an N-by-3
            %   matrix for points stored as rows. A point coincident with the
            %   light source is rejected because its direction is undefined.
            points = cmr.graphics.Light3D.checkPoints(points, 'points');

            delta = bsxfun(@minus, obj.position, points);
            distances = sqrt(sum(delta .^ 2, 2));
            if any(distances < 1e-12)
                error('cmr:graphics:Light3D:coincidentPoint', ...
                    'A surface point coincides with the light source; direction is undefined.');
            end
            directions = bsxfun(@rdivide, delta, distances);
        end

        function attenuation = attenuationAt(obj, points)
            %ATTENUATIONAT Distance attenuation for each surface point.
            %
            %   attenuation = 1 / (1 + 0.05 * distance^2). The denominator
            %   keeps the value finite and normalised at the light position.
            points = cmr.graphics.Light3D.checkPoints(points, 'points');

            delta = bsxfun(@minus, obj.position, points);
            distances = sqrt(sum(delta .^ 2, 2));
            attenuation = 1 ./ (1 + 0.05 * (distances .^ 2));
        end
    end

    methods (Static, Access = private)
        function points = checkPoints(points, name)
            if ~(isnumeric(points) && ismatrix(points) ...
                    && size(points, 2) == 3 && isreal(points) ...
                    && all(isfinite(points(:))))
                error('cmr:graphics:Light3D:invalidPoints', ...
                    '%s must be a finite real N-by-3 array.', name);
            end
            points = double(points);
        end
    end
end
