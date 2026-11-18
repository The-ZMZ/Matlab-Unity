classdef SurfaceNormals
%SURFACENORMALS Face and vertex normals for Issue #007 polygon meshes.
%
%   All methods are static. The class reuses cmr.graphics.Mesh3D.FACENORMALS
%   for face normals and adds area-weighted vertex normals, outward-direction
%   checks and normal-vector visualisation required by Issue #009.
%
%   Coordinate convention
%   ---------------------
%   Faces are stored counter-clockwise when seen from outside, as documented
%   by Mesh3D. Face normals therefore point outwards. OUTWARDFACENORMALS and
%   OUTWARDVERTEXNORMALS additionally check each normal against the vector
%   from the mesh centre to the sample point and flip it when necessary. This
%   centre-based test is intended for the closed, star-shaped primitives used
%   by this project (cube, sphere and cylinder).
%
%   Example:
%       sphere = cmr.graphics.Sphere3D.create(1, [0 0 0], 12, 18);
%       normals = cmr.graphics.SurfaceNormals.outwardVertexNormals(sphere);
%       cmr.graphics.SurfaceNormals.visualize(gca, sphere, "vertex");

    methods (Static)
        function normals = faceNormals(mesh)
            %FACENORMALS Unit normal of each polygon face.
            %
            %   This is a thin, documented gateway to the existing Issue #007
            %   implementation. Mesh3D.faceNormals uses Newell's method, so
            %   non-planar quads are handled without triangulation.
            normals = cmr.graphics.Mesh3D.faceNormals(mesh);
        end

        function centers = faceCenters(mesh)
            %FACECENTERS Centroid of each polygon face.
            %
            %   centers = SurfaceNormals.faceCenters(mesh) returns an F-by-3
            %   matrix. The centroid is the arithmetic mean of the face's
            %   vertex coordinates.
            cmr.graphics.SurfaceNormals.checkMesh(mesh);

            centers = zeros(numel(mesh.faces), 3);
            for faceIndex = 1:numel(mesh.faces)
                faceVertices = mesh.vertices(mesh.faces{faceIndex}, :);
                centers(faceIndex, :) = mean(faceVertices, 1);
            end
        end

        function areas = faceAreas(mesh)
            %FACEAREAS Area of each polygon face.
            %
            %   Newell's vector has magnitude 2*A for a planar polygon. Using
            %   the same summation as Mesh3D.faceNormals keeps area weights
            %   consistent with the stored face normals.
            cmr.graphics.SurfaceNormals.checkMesh(mesh);

            areas = zeros(numel(mesh.faces), 1);
            for faceIndex = 1:numel(mesh.faces)
                faceVertices = mesh.vertices(mesh.faces{faceIndex}, :);
                newell = cmr.graphics.SurfaceNormals.newellVector(faceVertices);
                areas(faceIndex) = norm(newell) / 2;
            end
        end

        function normals = vertexNormals(mesh)
            %VERTEXNORMALS Area-weighted smooth normal at each mesh vertex.
            %
            %   For vertex v, N(v) is the normalised sum of the normals of all
            %   incident faces, weighted by face area. Large faces therefore
            %   contribute more than small faces. This is the normal field
            %   used by Phong shading.
            cmr.graphics.SurfaceNormals.checkMesh(mesh);

            faceNormals = cmr.graphics.Mesh3D.faceNormals(mesh);
            faceAreas = cmr.graphics.SurfaceNormals.faceAreas(mesh);
            normals = zeros(size(mesh.vertices, 1), 3);

            for faceIndex = 1:numel(mesh.faces)
                face = mesh.faces{faceIndex};
                contribution = repmat( ...
                    faceAreas(faceIndex) * faceNormals(faceIndex, :), ...
                    numel(face), 1);
                normals(face, :) = normals(face, :) + contribution;
            end

            for vertexIndex = 1:size(normals, 1)
                magnitude = norm(normals(vertexIndex, :));
                if magnitude < 1e-12
                    error('cmr:graphics:SurfaceNormals:degenerateVertexNormal', ...
                        'Vertex %d has no well-defined normal.', vertexIndex);
                end
                normals(vertexIndex, :) = normals(vertexIndex, :) / magnitude;
            end
        end

        function outward = outwardFaceNormals(mesh)
            %OUTWARDFACENORMALS Face normals oriented away from the mesh centre.
            centers = cmr.graphics.SurfaceNormals.faceCenters(mesh);
            normals = cmr.graphics.Mesh3D.faceNormals(mesh);
            outward = cmr.graphics.SurfaceNormals.orientOutwards( ...
                normals, centers, mesh);
        end

        function outward = outwardVertexNormals(mesh)
            %OUTWARDVERTEXNORMALS Vertex normals oriented away from the centre.
            normals = cmr.graphics.SurfaceNormals.vertexNormals(mesh);
            outward = cmr.graphics.SurfaceNormals.orientOutwards( ...
                normals, mesh.vertices, mesh);
        end

        function [oriented, flipped] = orientOutwards(normals, samplePoints, mesh)
            %ORIENTOUTWARDS Flip normals that point toward the mesh centre.
            %
            %   [oriented, flipped] = orientOutwards(normals, samplePoints, mesh)
            %
            %   SAMPLEPOINTS must have one row per normal. For face normals use
            %   FACE CENTERS; for vertex normals use MESH.VERTICES. FLIPPED is
            %   a logical vector identifying rows that were reversed.
            cmr.graphics.SurfaceNormals.checkMesh(mesh);
            normals = cmr.graphics.SurfaceNormals.checkNormals(normals);

            if ~(isnumeric(samplePoints) && ismatrix(samplePoints) ...
                    && size(samplePoints, 2) == 3 ...
                    && size(samplePoints, 1) == size(normals, 1) ...
                    && isreal(samplePoints) && all(isfinite(samplePoints(:))))
                error('cmr:graphics:SurfaceNormals:invalidSamplePoints', ...
                    'Sample points must be a finite real N-by-3 array with one row per normal.');
            end
            samplePoints = double(samplePoints);

            center = mean(mesh.vertices, 1);
            radial = bsxfun(@minus, samplePoints, center);
            distances = sqrt(sum(radial .^ 2, 2));
            if any(distances < 1e-12)
                error('cmr:graphics:SurfaceNormals:ambiguousOrientation', ...
                    'A sample point coincides with the mesh centre; its outward direction is undefined.');
            end
            radial = bsxfun(@rdivide, radial, distances);

            facing = sum(normals .* radial, 2);
            flipped = facing < 0;
            oriented = normals;
            oriented(flipped, :) = -oriented(flipped, :);
        end

        function tf = isOutward(normals, samplePoints, mesh)
            %ISOUTWARD True where a normal points away from the mesh centre.
            oriented = cmr.graphics.SurfaceNormals.orientOutwards( ...
                normals, samplePoints, mesh);
            tf = all(oriented == normals, 2);
        end

        function handle = visualize(ax, mesh, normalType, varargin)
            %VISUALIZE Draw normal vectors over a translucent mesh.
            %
            %   handle = SurfaceNormals.visualize(ax, mesh, "face")
            %   handle = SurfaceNormals.visualize(ax, mesh, "vertex", ...
            %       'Scale', 0.4, 'Color', [1 0 0], 'ShowMesh', true)
            %
            %   NORMALTYPE is "face" or "vertex". SCALE is the quiver arrow
            %   length. The returned handle is a quiver object.
            if ~isgraphics(ax, 'axes')
                error('cmr:graphics:SurfaceNormals:invalidAxes', ...
                    'The first argument must be an axes handle.');
            end
            cmr.graphics.SurfaceNormals.checkMesh(mesh);

            parser = inputParser();
            parser.addParameter('Scale', 0.45, @(value) isnumeric(value) ...
                && isscalar(value) && isreal(value) && isfinite(value) && value > 0);
            parser.addParameter('Color', [0.90 0.15 0.10], @(value) isnumeric(value) ...
                && numel(value) == 3 && isreal(value) && all(isfinite(value(:))) ...
                && all(value(:) >= 0) && all(value(:) <= 1));
            parser.addParameter('ShowMesh', true, @(value) islogical(value) && isscalar(value));
            parser.parse(varargin{:});
            options = parser.Results;

            switch lower(char(string(normalType)))
                case {'face', 'faces', 'facenormals'}
                    normals = cmr.graphics.SurfaceNormals.outwardFaceNormals(mesh);
                    origins = cmr.graphics.SurfaceNormals.faceCenters(mesh);
                case {'vertex', 'vertices', 'vertexnormals'}
                    normals = cmr.graphics.SurfaceNormals.outwardVertexNormals(mesh);
                    origins = mesh.vertices;
                otherwise
                    error('cmr:graphics:SurfaceNormals:invalidNormalType', ...
                        'Normal type must be "face" or "vertex".');
            end

            wasHeld = ishold(ax);
            hold(ax, 'on');

            if options.ShowMesh
                patch(ax, 'Vertices', mesh.vertices, ...
                    'Faces', cmr.graphics.SurfaceNormals.faceMatrix(mesh), ...
                    'FaceColor', mesh.color, 'FaceAlpha', 0.22, ...
                    'EdgeColor', [0.20 0.24 0.30], 'LineWidth', 0.6, ...
                    'FaceLighting', 'none');
            end

            handle = quiver3(ax, origins(:, 1), origins(:, 2), origins(:, 3), ...
                normals(:, 1), normals(:, 2), normals(:, 3), options.Scale, ...
                'Color', options.Color, 'LineWidth', 1.4, ...
                'MaxHeadSize', 0.65, 'AutoScale', 'off');

            axis(ax, 'equal');
            grid(ax, 'on');
            box(ax, 'on');
            xlabel(ax, 'x');
            ylabel(ax, 'y');
            zlabel(ax, 'z');
            view(ax, 38, 26);

            if ~wasHeld
                hold(ax, 'off');
            end
        end
    end

    methods (Static)
        function checkMesh(mesh)
            %CHECKMESH Validate the mesh fields used by this class.
            %   This validator is public so Shading3D can reuse it.
            required = {'vertices', 'faces'};
            if ~isstruct(mesh) || ~isscalar(mesh)
                error('cmr:graphics:SurfaceNormals:invalidMesh', ...
                    'Expected a scalar mesh struct.');
            end
            for index = 1:numel(required)
                if ~isfield(mesh, required{index})
                    error('cmr:graphics:SurfaceNormals:invalidMesh', ...
                        'Mesh struct is missing the "%s" field.', required{index});
                end
            end
            if ~(isnumeric(mesh.vertices) && ismatrix(mesh.vertices) ...
                    && size(mesh.vertices, 2) == 3 && isreal(mesh.vertices) ...
                    && all(isfinite(mesh.vertices(:))))
                error('cmr:graphics:SurfaceNormals:invalidMesh', ...
                    'Mesh vertices must be a finite real N-by-3 matrix.');
            end
            if ~iscell(mesh.faces) || isempty(mesh.faces)
                error('cmr:graphics:SurfaceNormals:invalidMesh', ...
                    'Mesh faces must be a non-empty cell array.');
            end
        end
    end

    methods (Static, Access = private)
        function normals = checkNormals(normals)
            if ~(isnumeric(normals) && ismatrix(normals) ...
                    && size(normals, 2) == 3 && isreal(normals) ...
                    && all(isfinite(normals(:))))
                error('cmr:graphics:SurfaceNormals:invalidNormals', ...
                    'Normals must be a finite real N-by-3 array.');
            end
            normals = double(normals);
        end

        function vector = newellVector(vertices)
            vector = [0, 0, 0];
            vertexCount = size(vertices, 1);
            for corner = 1:vertexCount
                current = vertices(corner, :);
                next = vertices(mod(corner, vertexCount) + 1, :);
                vector(1) = vector(1) ...
                    + (current(2) - next(2)) * (current(3) + next(3));
                vector(2) = vector(2) ...
                    + (current(3) - next(3)) * (current(1) + next(1));
                vector(3) = vector(3) ...
                    + (current(1) - next(1)) * (current(2) + next(2));
            end
        end

        function faces = faceMatrix(mesh)
            widths = cellfun(@numel, mesh.faces);
            if all(widths == widths(1))
                faces = cell2mat(mesh.faces);
            else
                faces = cmr.graphics.Mesh3D.triangulate(mesh);
            end
        end
    end
end
