classdef Projection3D
%PROJECTION3D Orthographic and perspective projection matrices (Issue #010).
%
%   This class implements the mathematical camera used to turn the Issue #007
%   polygon meshes into 2D coordinates. All algorithms are written in base
%   MATLAB; MATLAB's camera commands are not used for the calculations.
%
%   Row-vector convention
%   ---------------------
%   The class follows cmr.graphics.Transform3D: a point is a row [x y z] and a
%   4x4 matrix acts by right multiplication.
%
%       [X Y Z W] = [x y z 1] * M
%       [xn yn zn] = [X/W Y/W Z/W]
%
%   Camera space
%   ------------
%   After VIEW matrix the camera is at the origin, +x points right, +y points
%   up and -z points into the scene. A visible point therefore has z < 0 in
%   camera space. PROJECTPOINTS rejects points with W <= 0 instead of silently
%   returning a mirrored position.
%
%   This is a purely mathematical viewpoint. It does not capture images and it
%   is not a physical camera or input device (see AGENTS.md section 5).
%
%   Example:
%       cube = cmr.graphics.Cube3D.create(2);
%       V = cmr.graphics.Projection3D.viewMatrix([0 -8 0], [0 0 0], [0 0 1]);
%       P = cmr.graphics.Projection3D.perspective(60, 1.6, 0.1, 100);
%       projected = cmr.graphics.Projection3D.projectMesh(cube, V, P);

    methods (Static)
        function V = viewMatrix(eyePoint, target, up)
            %VIEWMATRIX Build the world-to-camera 4x4 matrix.
            %
            %   V = Projection3D.viewMatrix(eye, target, up)
            %
            %   EYE, TARGET and UP are 1-by-3 points. UP defaults to [0 0 1],
            %   matching the Z-up convention of the Issue #007/#008 meshes.
            arguments
                eyePoint (1,3) double
                target (1,3) double
                up (1,3) double = [0 0 1]
            end

            eyePoint = cmr.graphics.Projection3D.checkPoint( ...
                double(eyePoint), 'eye point');
            target = cmr.graphics.Projection3D.checkPoint( ...
                double(target), 'target point');
            up = cmr.graphics.Projection3D.checkPoint(double(up), 'up vector');

            forward = target - eyePoint;
            distance = norm(forward);
            if distance < 1e-12
                error('cmr:graphics:Projection3D:coincidentCameraTarget', ...
                    'The eye point and target point must be different.');
            end
            forward = forward / distance;

            right = cross(forward, up);
            rightLength = norm(right);
            if rightLength < 1e-12
                error('cmr:graphics:Projection3D:upVectorParallelToView', ...
                    'The up vector must not be parallel to the view direction.');
            end
            right = right / rightLength;

            % Recompute up from the other two axes so it is exactly orthogonal.
            trueUp = cross(right, forward);

            % This is the transpose of the familiar column-vector look-at
            % matrix because points are stored as homogeneous rows.
            V = [ ...
                 right(1),  trueUp(1), -forward(1), 0; ...
                 right(2),  trueUp(2), -forward(2), 0; ...
                 right(3),  trueUp(3), -forward(3), 0; ...
                -dot(right, eyePoint), -dot(trueUp, eyePoint), ...
                 dot(forward, eyePoint), 1];
        end

        function P = orthographic(width, height, near, far)
            %ORTHOGRAPHIC Symmetric orthographic projection matrix.
            %
            %   P = Projection3D.orthographic(width, height, near, far)
            %
            %   WIDTH and HEIGHT are the visible rectangle in camera-space
            %   units. NEAR and FAR are positive distances from the eye.
            %   Parallel lines stay parallel and object size does not change
            %   with distance.
            arguments
                width (1,1) double
                height (1,1) double
                near (1,1) double
                far (1,1) double
            end

            if ~isfinite(width) || width <= 0
                error('cmr:graphics:Projection3D:invalidViewWidth', ...
                    'Orthographic width must be a finite positive number.');
            end
            if ~isfinite(height) || height <= 0
                error('cmr:graphics:Projection3D:invalidViewHeight', ...
                    'Orthographic height must be a finite positive number.');
            end
            cmr.graphics.Projection3D.checkDepthRange(near, far);

            P = [ ...
                2 / width, 0, 0, 0; ...
                0, 2 / height, 0, 0; ...
                0, 0, -2 / (far - near), 0; ...
                0, 0, -(far + near) / (far - near), 1];
        end

        function P = perspective(fieldOfView, aspect, near, far, angleUnit)
            %PERSPECTIVE Perspective projection matrix.
            %
            %   P = Projection3D.perspective(fov, aspect, near, far)
            %   P = Projection3D.perspective(fov, aspect, near, far, "radians")
            %
            %   FOV is the vertical field of view in degrees unless the
            %   optional ANGLEUNIT is "radians"/"rad". ASPECT is width/height.
            %   Objects shrink as their distance from the eye increases.
            if nargin < 5 || isempty(angleUnit)
                angleUnit = "degrees";
            end

            if ~(isnumeric(fieldOfView) && isscalar(fieldOfView) ...
                    && isreal(fieldOfView) && isfinite(fieldOfView))
                error('cmr:graphics:Projection3D:invalidFieldOfView', ...
                    'Field of view must be a finite real scalar.');
            end
            if ~(isnumeric(aspect) && isscalar(aspect) && isreal(aspect) ...
                    && isfinite(aspect) && aspect > 0)
                error('cmr:graphics:Projection3D:invalidAspectRatio', ...
                    'Aspect ratio must be a finite positive number.');
            end
            cmr.graphics.Projection3D.checkDepthRange(near, far);

            theta = cmr.graphics.Projection3D.parseAngle( ...
                fieldOfView, angleUnit, 'invalidFieldOfView');
            if theta <= 0 || theta >= pi
                error('cmr:graphics:Projection3D:invalidFieldOfView', ...
                    'Field of view must be greater than 0 and less than 180 degrees.');
            end

            focal = 1 / tan(theta / 2);
            P = [ ...
                focal / aspect, 0, 0, 0; ...
                0, focal, 0, 0; ...
                0, 0, -(far + near) / (far - near), -1; ...
                0, 0, -2 * far * near / (far - near), 0];
        end

        function clip = clipCoordinates(points, matrix)
            %CLIPCOORDINATES Apply a 4x4 matrix without dividing by W.
            %
            %   clip = Projection3D.clipCoordinates(points, matrix) returns
            %   an N-by-4 array [X Y Z W]. This is useful when teaching the
            %   projection pipeline step by step.
            points = cmr.graphics.Projection3D.checkPoints(points, 'points');
            matrix = cmr.graphics.Projection3D.checkMatrix(matrix, 'matrix');

            homogeneous = cmr.graphics.Transform3D.toHomogeneous(points);
            clip = homogeneous * matrix;
        end

        function ndc = projectPoints(points, matrix)
            %PROJECTPOINTS Project N-by-3 world points into normalised space.
            %
            %   ndc = Projection3D.projectPoints(points, matrix) returns
            %   N-by-3 coordinates in the range [-1, 1] for visible points.
            %   The homogeneous division reuses Transform3D.fromHomogeneous.
            clip = cmr.graphics.Projection3D.clipCoordinates(points, matrix);

            if any(clip(:, 4) <= 1e-12)
                error('cmr:graphics:Projection3D:pointBehindCamera', ...
                    ['One or more points have W <= 0 after projection. ' ...
                     'Only points in front of the camera can be displayed.']);
            end
            ndc = cmr.graphics.Transform3D.fromHomogeneous(clip);
        end

        function result = projectMesh(mesh, viewMatrix, projectionMatrix)
            %PROJECTMESH Project an Issue #007 mesh into normalised space.
            %
            %   result = Projection3D.projectMesh(mesh, V, P) applies the view
            %   matrix first and the projection matrix second. RESULT.FACES is
            %   the triangulated index list from Mesh3D.TRIANGULATE, so the
            %   result can be drawn with a 2D patch.
            cmr.graphics.SurfaceNormals.checkMesh(mesh);
            viewMatrix = cmr.graphics.Projection3D.checkMatrix( ...
                viewMatrix, 'view matrix');
            projectionMatrix = cmr.graphics.Projection3D.checkMatrix( ...
                projectionMatrix, 'projection matrix');

            combined = viewMatrix * projectionMatrix;
            ndc = cmr.graphics.Projection3D.projectPoints( ...
                mesh.vertices, combined);
            triangles = cmr.graphics.Mesh3D.triangulate(mesh);
            [extentWidth, extentHeight, extentDepth] = ...
                cmr.graphics.Projection3D.projectedExtent(ndc);

            result = struct();
            result.meshName = char(mesh.name);
            result.vertices = ndc;
            result.faces = triangles;
            result.cameraSpace = cmr.graphics.Transform3D.transformPoints( ...
                viewMatrix, mesh.vertices);
            result.viewMatrix = viewMatrix;
            result.projectionMatrix = projectionMatrix;
            result.projectedExtent = [extentWidth, extentHeight, extentDepth];
        end

        function [width, height, depth] = projectedExtent(points)
            %PROJECTEDEXTENT Width, height and depth of NDC coordinates.
            %
            %   [width, height] = Projection3D.projectedExtent(points)
            %   [width, height, depth] = Projection3D.projectedExtent(points)
            %
            %   POINTS may be an N-by-3 NDC array or a PROJECTMESH result.
            if isstruct(points) && isfield(points, 'vertices')
                points = points.vertices;
            end
            points = cmr.graphics.Projection3D.checkPoints(points, 'NDC points');

            spans = max(points, [], 1) - min(points, [], 1);
            width = spans(1);
            height = spans(2);
            if nargout > 2
                depth = spans(3);
            end
        end

        function distance = distance(eyePoint, points)
            %DISTANCE Euclidean distance from a camera eye point to points.
            %
            %   distance = Projection3D.distance(eye, points) returns an
            %   N-by-1 column with one world-space distance per point.
            eyePoint = cmr.graphics.Projection3D.checkPoint( ...
                eyePoint, 'eye point');
            points = cmr.graphics.Projection3D.checkPoints(points, 'points');

            offset = bsxfun(@minus, points, eyePoint);
            distance = sqrt(sum(offset .^ 2, 2));
        end

        function distance = distanceToMesh(eyePoint, mesh)
            %DISTANCETOMESH Distance from a camera eye point to a mesh centre.
            %
            %   The centre of the axis-aligned bounding box is used as a
            %   simple, repeatable proxy for the object's position.
            cmr.graphics.SurfaceNormals.checkMesh(mesh);
            [minimum, maximum] = cmr.graphics.Mesh3D.bounds(mesh);
            center = (minimum + maximum) / 2;
            distance = cmr.graphics.Projection3D.distance(eyePoint, center);
        end

        function comparison = compare(orthographicResult, perspectiveResult)
            %COMPARE Compare an orthographic and a perspective projection.
            %
            %   Both results must come from PROJECTMESH with the same mesh and
            %   the same view matrix; only the projection type may differ.
            %   The returned struct contains both extents and their ratios.
            cmr.graphics.Projection3D.checkProjectionResult( ...
                orthographicResult, 'orthographic result');
            cmr.graphics.Projection3D.checkProjectionResult( ...
                perspectiveResult, 'perspective result');

            if ~strcmp(orthographicResult.meshName, perspectiveResult.meshName)
                error('cmr:graphics:Projection3D:meshMismatch', ...
                    'Both projections must use the same mesh.');
            end
            if ~isequal(orthographicResult.viewMatrix, perspectiveResult.viewMatrix)
                error('cmr:graphics:Projection3D:cameraMismatch', ...
                    'Both projections must use the same view matrix.');
            end

            ortho = orthographicResult.projectedExtent;
            persp = perspectiveResult.projectedExtent;

            comparison = struct();
            comparison.meshName = orthographicResult.meshName;
            comparison.orthographicExtent = ortho;
            comparison.perspectiveExtent = persp;
            comparison.widthRatio = persp(1) / ortho(1);
            comparison.heightRatio = persp(2) / ortho(2);
        end
    end

    methods (Static, Access = private)
        function theta = parseAngle(value, unit, errorIdentifier)
            if isstring(unit) && isscalar(unit)
                unit = lower(char(unit));
            elseif ischar(unit) && isrow(unit)
                unit = lower(unit);
            else
                error(['cmr:graphics:Projection3D:' errorIdentifier], ...
                    'Angle unit must be "degrees" or "radians".');
            end

            theta = double(value);
            switch unit
                case {'deg', 'degree', 'degrees'}
                    theta = theta * pi / 180;
                case {'rad', 'radian', 'radians'}
                    % Already in radians.
                otherwise
                    error(['cmr:graphics:Projection3D:' errorIdentifier], ...
                        'Angle unit must be "degrees" or "radians".');
            end
        end

        function checkDepthRange(near, far)
            if ~(isnumeric(near) && isscalar(near) && isreal(near) ...
                    && isfinite(near) && near > 0)
                error('cmr:graphics:Projection3D:invalidNearPlane', ...
                    'Near plane distance must be a finite positive number.');
            end
            if ~(isnumeric(far) && isscalar(far) && isreal(far) ...
                    && isfinite(far) && far > 0)
                error('cmr:graphics:Projection3D:invalidFarPlane', ...
                    'Far plane distance must be a finite positive number.');
            end
            if far <= near
                error('cmr:graphics:Projection3D:invalidDepthRange', ...
                    'Far plane distance must be greater than near plane distance.');
            end
        end

        function point = checkPoint(point, name)
            if ~(isnumeric(point) && isreal(point) && numel(point) == 3 ...
                    && all(isfinite(point(:))))
                error('cmr:graphics:Projection3D:invalidPoint', ...
                    '%s must be a finite real 1-by-3 point.', name);
            end
            point = double(point(:))';
        end

        function points = checkPoints(points, name)
            if ~(isnumeric(points) && ismatrix(points) ...
                    && size(points, 2) == 3 && isreal(points) ...
                    && ~isempty(points) && all(isfinite(points(:))))
                error('cmr:graphics:Projection3D:invalidPoints', ...
                    '%s must be a non-empty finite real N-by-3 array.', name);
            end
            points = double(points);
        end

        function matrix = checkMatrix(matrix, name)
            if ~(isnumeric(matrix) && ismatrix(matrix) ...
                    && all(size(matrix) == [4, 4]) && isreal(matrix) ...
                    && all(isfinite(matrix(:))))
                error('cmr:graphics:Projection3D:invalidMatrix', ...
                    '%s must be a finite real 4-by-4 matrix.', name);
            end
            matrix = double(matrix);
        end

        function checkProjectionResult(result, name)
            required = {'meshName', 'vertices', 'viewMatrix', 'projectedExtent'};
            if ~isstruct(result) || ~isscalar(result)
                error('cmr:graphics:Projection3D:invalidResult', ...
                    'Expected a scalar projection result struct for %s.', name);
            end
            for index = 1:numel(required)
                if ~isfield(result, required{index})
                    error('cmr:graphics:Projection3D:invalidResult', ...
                        '%s is missing the "%s" field.', name, required{index});
                end
            end
        end
    end
end
