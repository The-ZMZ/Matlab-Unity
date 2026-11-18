classdef Shading3D
%SHADING3D Lambertian (flat) and Phong (smooth) shading for Issue #007 meshes.
%
%   LAMBERTIAN evaluates ambient plus diffuse lighting once per polygon face
%   and paints every triangle of that polygon with the same colour. PHONG
%   evaluates ambient, diffuse and specular lighting at every vertex using
%   area-weighted vertex normals, then relies on patch interpolation to
%   produce a smooth highlight.
%
%   Both methods triangulate the mesh with cmr.graphics.Mesh3D.TRIANGULATE
%   and return a result struct suitable for SHADING3D.RENDER. Colour channels
%   are clipped to [0, 1] only for display; the raw lighting components stay
%   available in RESULT.COMPONENTS.
%
%   Example:
%       sphere = cmr.graphics.Sphere3D.create(1, [0 0 0], 14, 20);
%       light = cmr.graphics.Light3D([4 -5 6]);
%       material = cmr.graphics.Material3D.fromColor('Blue', [0.2 0.55 0.9]);
%       result = cmr.graphics.Shading3D.phong(sphere, light, material, [5 -7 5]);
%       cmr.graphics.Shading3D.render(gca, result);

    methods (Static)
        function result = lambertian(mesh, light, material, viewer)
            %LAMBERTIAN Flat Lambertian shading with ambient lighting.
            %
            %   result = Shading3D.lambertian(mesh, light, material, viewer)
            %
            %   RESULT.FACECOLORS is an M-by-3 matrix, one colour per output
            %   triangle. RESULT.COMPONENTS contains the same M rows before
            %   display clipping. RESULT.SPECULAR is zero by design.
            cmr.graphics.SurfaceNormals.checkMesh(mesh);
            cmr.graphics.LightingModel.checkLight(light);
            cmr.graphics.LightingModel.checkMaterial(material);
            viewer = cmr.graphics.LightingModel.checkPoints(viewer, 'viewer');

            [triangles, sourceFace] = cmr.graphics.Mesh3D.triangulate(mesh);
            faceNormals = cmr.graphics.SurfaceNormals.outwardFaceNormals(mesh);
            centers = cmr.graphics.Shading3D.triangleCenters(mesh.vertices, triangles);
            normals = faceNormals(sourceFace, :);

            components = cmr.graphics.LightingModel.illuminate( ...
                material, light, centers, normals, viewer);
            % Lambertian shading is ambient plus diffuse only. Illuminate also
            % returns a Phong specular term, so discard it here to honour the
            % documented flat-shading contract.
            components.specular = zeros(size(components.diffuse));
            components.total = components.ambient + components.diffuse;
            colors = cmr.graphics.Shading3D.clamp(components.total);
            meanBrightness = mean(colors, 2);
            [~, brightest] = max(meanBrightness);

            result = cmr.graphics.Shading3D.baseResult( ...
                mesh, triangles, sourceFace, 'Lambertian', 'face');
            result.normals = normals;
            result.faceCenters = centers;
            result.faceColors = colors;
            result.vertexColors = zeros(size(mesh.vertices, 1), 3);
            result.components = components;
            result.brightestFace = brightest;
        end

        function result = phong(mesh, light, material, viewer)
            %PHONG Smooth per-vertex ambient, diffuse and specular shading.
            %
            %   RESULT.VERTEXCOLORS holds the N-by-3 colour at each mesh
            %   vertex. RESULT.FACECOLORS is the mean of the three vertex
            %   colours of each output triangle and is supplied for numerical
            %   comparison with Lambertian results.
            cmr.graphics.SurfaceNormals.checkMesh(mesh);
            cmr.graphics.LightingModel.checkLight(light);
            cmr.graphics.LightingModel.checkMaterial(material);
            viewer = cmr.graphics.LightingModel.checkPoints(viewer, 'viewer');

            [triangles, sourceFace] = cmr.graphics.Mesh3D.triangulate(mesh);
            normals = cmr.graphics.SurfaceNormals.outwardVertexNormals(mesh);

            components = cmr.graphics.LightingModel.illuminate( ...
                material, light, mesh.vertices, normals, viewer);
            colors = cmr.graphics.Shading3D.clamp(components.total);
            faceColors = cmr.graphics.Shading3D.averageVertexColors( ...
                colors, triangles);
            meanBrightness = mean(colors, 2);
            [~, brightest] = max(meanBrightness);

            result = cmr.graphics.Shading3D.baseResult( ...
                mesh, triangles, sourceFace, 'Phong', 'vertex');
            result.normals = normals;
            result.faceCenters = cmr.graphics.Shading3D.triangleCenters( ...
                mesh.vertices, triangles);
            result.faceColors = faceColors;
            result.vertexColors = colors;
            result.components = components;
            result.brightestVertex = brightest;
        end

        function result = shade(mesh, light, material, viewer, method)
            %SHADE Dispatch to LAMBERTIAN or PHONG by name.
            %
            %   Accepted method names are "lambertian", "lambert", "flat",
            %   "phong" and "smooth" (case-insensitive).
            switch lower(char(string(method)))
                case {'lambertian', 'lambert', 'flat'}
                    result = cmr.graphics.Shading3D.lambertian( ...
                        mesh, light, material, viewer);
                case {'phong', 'smooth'}
                    result = cmr.graphics.Shading3D.phong( ...
                        mesh, light, material, viewer);
                otherwise
                    error('cmr:graphics:Shading3D:invalidMethod', ...
                        'Shading method must be "lambertian" or "phong".');
            end
        end

        function handle = render(ax, result)
            %RENDER Draw a shading result with flat or interpolated colours.
            %
            %   handle = Shading3D.render(ax, result) draws into AX.
            %   handle = Shading3D.render(result) draws into the current axes.
            if isgraphics(ax, 'axes')
                target = ax;
            else
                result = ax;
                target = gca;
            end
            cmr.graphics.Shading3D.checkResult(result);

            switch lower(char(string(result.method)))
                case 'lambertian'
                    handle = patch(target, ...
                        'Vertices', result.vertices, ...
                        'Faces', result.faces, ...
                        'FaceVertexCData', result.faceColors, ...
                        'FaceColor', 'flat', ...
                        'EdgeColor', [0.14 0.16 0.20], ...
                        'LineWidth', 0.5, ...
                        'FaceLighting', 'none');
                case 'phong'
                    handle = patch(target, ...
                        'Vertices', result.vertices, ...
                        'Faces', result.faces, ...
                        'FaceVertexCData', result.vertexColors, ...
                        'FaceColor', 'interp', ...
                        'EdgeColor', 'none', ...
                        'FaceLighting', 'none');
                otherwise
                    error('cmr:graphics:Shading3D:invalidMethod', ...
                        'Shading result has an unsupported method.');
            end

            axis(target, 'equal');
            grid(target, 'on');
            box(target, 'on');
            xlabel(target, 'x');
            ylabel(target, 'y');
            zlabel(target, 'z');
            view(target, 38, 26);
        end
    end

    methods (Static, Access = private)
        function result = baseResult(mesh, faces, sourceFace, method, colorLocation)
            result = struct();
            result.method = method;
            result.colorLocation = colorLocation;
            result.meshName = char(mesh.name);
            result.vertices = mesh.vertices;
            result.faces = faces;
            result.sourceFace = sourceFace;
        end

        function centers = triangleCenters(vertices, triangles)
            centers = (vertices(triangles(:, 1), :) ...
                + vertices(triangles(:, 2), :) ...
                + vertices(triangles(:, 3), :)) / 3;
        end

        function colors = averageVertexColors(vertexColors, triangles)
            colors = (vertexColors(triangles(:, 1), :) ...
                + vertexColors(triangles(:, 2), :) ...
                + vertexColors(triangles(:, 3), :)) / 3;
        end

        function colors = clamp(colors)
            colors = min(max(colors, 0), 1);
        end

        function checkResult(result)
            required = {'method', 'vertices', 'faces', 'faceColors', ...
                'vertexColors', 'components'};
            if ~isstruct(result) || ~isscalar(result)
                error('cmr:graphics:Shading3D:invalidResult', ...
                    'Expected a scalar shading result struct.');
            end
            for index = 1:numel(required)
                if ~isfield(result, required{index})
                    error('cmr:graphics:Shading3D:invalidResult', ...
                        'Shading result is missing the "%s" field.', ...
                        required{index});
                end
            end
        end
    end
end
