classdef LightingModel
%LIGHTINGMODEL Ambient, diffuse and specular lighting components.
%
%   All methods are static. Inputs are a cmr.graphics.Material3D, a
%   cmr.graphics.Light3D, N-by-3 surface points, N-by-3 unit normals and an
%   N-by-3 (or 1-by-3) viewer position.
%
%   Equations
%   ---------
%       ambient  = material.ambient .* light.ambientColor
%       diffuse  = material.diffuse .* lightTerm .* max(N.L, 0)
%       specular = material.specular .* lightTerm .* (max(R.V, 0)^s)
%
%   where R = 2*(N.L)*N - L, s is material.shininess and
%   lightTerm = light.color * light.intensity * attenuation(distance).
%   Specular is zero when N.L <= 0, so a light behind a surface cannot create
%   a highlight on its front face.
%
%   Rows correspond to surface points. Ambient is returned as a 1-by-3 vector
%   by AMBIENT and expanded to N-by-3 by ILLUMINATE.

    methods (Static)
        function ambient = ambient(material, light)
            %AMBIENT Ambient contribution as a 1-by-3 RGB vector.
            cmr.graphics.LightingModel.checkMaterial(material);
            cmr.graphics.LightingModel.checkLight(light);
            ambient = material.ambient .* light.ambientColor();
        end

        function diffuse = diffuse(material, light, points, normals)
            %DIFFUSE Lambertian diffuse contribution at each point.
            diffuse = cmr.graphics.LightingModel.diffuseOrSpecular( ...
                material, light, points, normals, [], false);
        end

        function specular = specular(material, light, points, normals, viewer)
            %SPECULAR Phong specular contribution at each point.
            specular = cmr.graphics.LightingModel.diffuseOrSpecular( ...
                material, light, points, normals, viewer, true);
        end

        function components = illuminate(material, light, points, normals, viewer)
            %ILLUMINATE Compute all components and their sum.
            %
            %   components.ambient   N-by-3 repeated ambient colour.
            %   components.diffuse   N-by-3 Lambertian component.
            %   components.specular  N-by-3 Phong highlight component.
            %   components.total     N-by-3 unclipped sum of the three.
            ambientRow = cmr.graphics.LightingModel.ambient(material, light);
            ambient = repmat(ambientRow, size(points, 1), 1);
            diffuse = cmr.graphics.LightingModel.diffuse( ...
                material, light, points, normals);
            specular = cmr.graphics.LightingModel.specular( ...
                material, light, points, normals, viewer);

            components = struct();
            components.ambient = ambient;
            components.diffuse = diffuse;
            components.specular = specular;
            components.total = ambient + diffuse + specular;
        end
    end

    methods (Static)
        function checkMaterial(material)
            %CHECKMATERIAL Validate a Material3D instance. Public for reuse by Shading3D.
            if ~isa(material, 'cmr.graphics.Material3D')
                error('cmr:graphics:LightingModel:invalidMaterial', ...
                    'Material must be a cmr.graphics.Material3D object.');
            end
        end

        function checkLight(light)
            if ~isa(light, 'cmr.graphics.Light3D')
                error('cmr:graphics:LightingModel:invalidLight', ...
                    'Light must be a cmr.graphics.Light3D object.');
            end
        end

        function points = checkPoints(points, name)
            if ~(isnumeric(points) && ismatrix(points) ...
                    && size(points, 2) == 3 && isreal(points) ...
                    && all(isfinite(points(:))))
                error('cmr:graphics:LightingModel:invalidPoints', ...
                    '%s must be a finite real N-by-3 array.', name);
            end
            points = double(points);
        end

        function normals = checkNormals(normals)
            normals = cmr.graphics.LightingModel.checkPoints(normals, 'normals');
            magnitudes = sqrt(sum(normals .^ 2, 2));
            if any(magnitudes < 1e-12)
                error('cmr:graphics:LightingModel:zeroNormal', ...
                    'Every normal must have non-zero length.');
            end
            normals = bsxfun(@rdivide, normals, magnitudes);
        end

        function viewDirections = viewerDirections(viewer, points)
            viewer = cmr.graphics.LightingModel.checkPoints(viewer, 'viewer');
            if size(viewer, 1) == 1
                viewer = repmat(viewer, size(points, 1), 1);
            elseif size(viewer, 1) ~= size(points, 1)
                error('cmr:graphics:LightingModel:invalidViewer', ...
                    'Viewer must be a single 1-by-3 point or one point per surface point.');
            end

            delta = viewer - points;
            distances = sqrt(sum(delta .^ 2, 2));
            if any(distances < 1e-12)
                error('cmr:graphics:LightingModel:coincidentViewer', ...
                    'A viewer position coincides with a surface point; direction is undefined.');
            end
            viewDirections = bsxfun(@rdivide, delta, distances);
        end
    end

    methods (Static, Access = private)
        function result = diffuseOrSpecular(material, light, points, normals, viewer, isSpecular)
            cmr.graphics.LightingModel.checkMaterial(material);
            cmr.graphics.LightingModel.checkLight(light);
            points = cmr.graphics.LightingModel.checkPoints(points, 'points');
            normals = cmr.graphics.LightingModel.checkNormals(normals);

            lightDirections = light.directionTo(points);
            attenuation = light.attenuationAt(points);
            lightColor = bsxfun(@times, light.color, light.intensity .* attenuation);
            normalDotLight = sum(normals .* lightDirections, 2);
            illuminated = normalDotLight > 0;

            if isSpecular
                reflection = bsxfun(@times, 2 .* normalDotLight, normals) ...
                    - lightDirections;
                viewDirections = cmr.graphics.LightingModel.viewerDirections( ...
                    viewer, points);
                reflectionDotView = max(sum(reflection .* viewDirections, 2), 0);
                strength = illuminated .* (reflectionDotView .^ material.shininess);
                reflectance = material.specular;
            else
                strength = max(normalDotLight, 0);
                reflectance = material.diffuse;
            end

            result = bsxfun(@times, bsxfun(@times, lightColor, reflectance), strength);
        end
    end
end
