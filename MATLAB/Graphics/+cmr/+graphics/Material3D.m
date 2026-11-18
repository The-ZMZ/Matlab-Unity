classdef Material3D
%MATERIAL3D Phong material properties for shaded meshes.
%
%   The class stores the four parameters used by the ambient, diffuse and
%   specular calculations in cmr.graphics.LightingModel:
%
%       ambient     Ambient RGB reflectance.
%       diffuse     Diffuse (Lambertian) RGB reflectance.
%       specular    Specular RGB reflectance.
%       shininess   Positive Phong exponent; larger values give tighter
%                   highlights.
%
%   Example:
%       glossy = cmr.graphics.Material3D('Glossy plastic', ...
%           'Diffuse', [0.20 0.55 0.90], ...
%           'Specular', [0.95 0.95 0.95], 'Shininess', 48);

    properties (SetAccess = private)
        name
        ambient
        diffuse
        specular
        shininess
    end

    methods
        function obj = Material3D(name, options)
            %MATERIAL3D Construct a material.
            %
            %   Material3D() uses the name "material" and neutral defaults.
            %   The first positional argument, when supplied, names the
            %   material. Remaining properties are name-value pairs.
            arguments
                name = "material"
                options.Ambient (1,3) double = [0.05, 0.05, 0.05]
                options.Diffuse (1,3) double = [0.60, 0.60, 0.60]
                options.Specular (1,3) double = [0.10, 0.10, 0.10]
                options.Shininess (1,1) double = 16
            end

            if ~(isstring(name) || ischar(name))
                error('cmr:graphics:Material3D:invalidName', ...
                    'Material name must be a string scalar or character vector.');
            end
            name = string(name);
            if ~isscalar(name) || strlength(name) == 0
                error('cmr:graphics:Material3D:invalidName', ...
                    'Material name must be a non-empty string scalar.');
            end

            obj.name = char(name);
            obj.ambient = double(options.Ambient);
            obj.diffuse = double(options.Diffuse);
            obj.specular = double(options.Specular);
            obj.shininess = double(options.Shininess);

            componentNames = {'Ambient', 'Diffuse', 'Specular'};
            values = {obj.ambient, obj.diffuse, obj.specular};
            for index = 1:numel(values)
                value = values{index};
                if ~(isreal(value) && all(isfinite(value(:))) ...
                        && all(value(:) >= 0) && all(value(:) <= 1))
                    error('cmr:graphics:Material3D:invalidColor', ...
                        '%s material property must contain three RGB components in [0, 1].', ...
                        componentNames{index});
                end
            end

            if ~(isreal(obj.shininess) && isfinite(obj.shininess) ...
                    && obj.shininess >= 1)
                error('cmr:graphics:Material3D:invalidShininess', ...
                    'Shininess must be a finite real scalar greater than or equal to 1.');
            end
        end
    end

    methods (Static)
        function material = fromColor(name, color, options)
            %FROMCOLOR Build a material from one base RGB colour.
            %
            %   Material3D.fromColor(name, color) uses COLOR as the diffuse
            %   component, a small ambient component and a neutral specular
            %   component. Optional Shininess and SpecularScale name-value
            %   pairs are accepted.
            arguments
                name
                color (1,3) double
                options.Shininess (1,1) double = 16
                options.SpecularScale (1,1) double = 0.20
            end

            if ~(isnumeric(options.SpecularScale) && isreal(options.SpecularScale) ...
                    && isfinite(options.SpecularScale) ...
                    && options.SpecularScale >= 0 && options.SpecularScale <= 1)
                error('cmr:graphics:Material3D:invalidSpecularScale', ...
                    'SpecularScale must be a finite scalar in [0, 1].');
            end

            ambient = 0.15 * color;
            specular = options.SpecularScale * ones(1, 3);
            material = cmr.graphics.Material3D(name, ...
                'Ambient', ambient, 'Diffuse', color, ...
                'Specular', specular, 'Shininess', options.Shininess);
        end
    end
end
