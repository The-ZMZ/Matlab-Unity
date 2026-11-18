classdef Transform3D
%TRANSFORM3D Homogeneous 4x4 matrices for 3D geometric transformations.
%
%   This class implements the Issue #008 affine transformations on the polygon
%   meshes produced by cmr.graphics.Cube3D, Sphere3D and Cylinder3D. All methods
%   are static; the class is never instantiated.
%
%   Row-vector convention
%   --------------------
%   A point [x y z] is written as the homogeneous row [x y z 1] and is
%   transformed by right multiplication:
%
%       [X Y Z 1] = [x y z 1] * T
%
%   COMPOSE(T1,T2,...,Tn) returns T1*T2*...*Tn. With the row-vector convention
%   this means T1 is applied first, then T2, and so on. Transformation order
%   therefore matters, and the same sequence written in the reverse order is
%   not generally equivalent.
%
%   Angle convention
%   ----------------
%   Rotation angles are in radians by default. Pass "degrees" (or "deg") as
%   the second argument to use degrees.
%
%   Example:
%       cube = cmr.graphics.Cube3D.create(2);
%       T = cmr.graphics.Transform3D.compose( ...
%           cmr.graphics.Transform3D.translation(3, 0, 0), ...
%           cmr.graphics.Transform3D.rotationZ(90, "degrees"));
%       moved = cmr.graphics.Transform3D.transformMesh(T, cube);

    methods (Static)
        function T = identity()
            %IDENTITY Return the 4x4 identity transformation.
            T = eye(4);
        end

        function T = translation(varargin)
            %TRANSLATION Build a 4x4 translation matrix.
            %
            %   T = Transform3D.translation(dx, dy, dz)
            %   T = Transform3D.translation([dx dy dz])
            %
            %   Applying T maps (x, y, z) to (x+dx, y+dy, z+dz).

            offset = cmr.graphics.Transform3D.parseTranslation(varargin{:});
            T = eye(4);
            T(4, 1:3) = offset;
        end

        function T = rotationX(angle, angleUnit)
            %ROTATIONX Right-handed rotation about the x axis.
            %
            %   T = Transform3D.rotationX(theta)          % theta in radians
            %   T = Transform3D.rotationX(90, "degrees")
            %
            %   Positive angles turn +y toward +z.

            if nargin < 2
                theta = cmr.graphics.Transform3D.parseAngle(angle);
            else
                theta = cmr.graphics.Transform3D.parseAngle(angle, angleUnit);
            end
            c = cos(theta);
            s = sin(theta);

            % Transpose of the usual column-vector matrix because points are
            % stored as rows and transformed by right multiplication.
            T = [ ...
                 1,  0,  0,  0; ...
                 0,  c,  s,  0; ...
                 0, -s,  c,  0; ...
                 0,  0,  0,  1];
        end

        function T = rotationY(angle, angleUnit)
            %ROTATIONY Right-handed rotation about the y axis.
            %
            %   Positive angles turn +z toward +x.

            if nargin < 2
                theta = cmr.graphics.Transform3D.parseAngle(angle);
            else
                theta = cmr.graphics.Transform3D.parseAngle(angle, angleUnit);
            end
            c = cos(theta);
            s = sin(theta);

            T = [ ...
                 c,  0, -s,  0; ...
                 0,  1,  0,  0; ...
                 s,  0,  c,  0; ...
                 0,  0,  0,  1];
        end

        function T = rotationZ(angle, angleUnit)
            %ROTATIONZ Right-handed rotation about the z axis.
            %
            %   Positive angles turn +x toward +y.

            if nargin < 2
                theta = cmr.graphics.Transform3D.parseAngle(angle);
            else
                theta = cmr.graphics.Transform3D.parseAngle(angle, angleUnit);
            end
            c = cos(theta);
            s = sin(theta);

            T = [ ...
                 c,  s,  0,  0; ...
                -s,  c,  0,  0; ...
                 0,  0,  1,  0; ...
                 0,  0,  0,  1];
        end

        function T = scaling(varargin)
            %SCALING Build a 4x4 scaling matrix.
            %
            %   T = Transform3D.scaling(s)          % uniform scaling
            %   T = Transform3D.scaling(sx, sy, sz)
            %   T = Transform3D.scaling([sx sy sz])
            %
            %   Negative factors are allowed and produce a reflection. Zero
            %   factors are rejected because they make the transform singular.

            factors = cmr.graphics.Transform3D.parseScaling(varargin{:});
            T = eye(4);
            T(1:3, 1:3) = diag(factors);
        end

        function T = compose(varargin)
            %COMPOSE Multiply transformations into one 4x4 matrix.
            %
            %   T = Transform3D.compose(T1, T2, ..., Tn)
            %   T = Transform3D.compose({T1, T2, ..., Tn})
            %
            %   The returned matrix is T1*T2*...*Tn, so T1 is applied first.
            %   A single matrix is returned unchanged after validation.

            matrices = cmr.graphics.Transform3D.parseMatrixList(varargin);
            T = matrices{1};
            for index = 2:numel(matrices)
                T = T * matrices{index};
            end
        end

        function T = combine(varargin)
            %COMBINE Alias for COMPOSE.
            T = cmr.graphics.Transform3D.compose(varargin{:});
        end

        function T = inverse(matrix)
            %INVERSE Return the inverse homogeneous transformation.
            matrix = cmr.graphics.Transform3D.checkMatrix(matrix, 'matrix');
            if rcond(matrix) < 1e-12
                error('cmr:graphics:Transform3D:singularMatrix', ...
                    'Transformation matrix is singular and cannot be inverted.');
            end
            T = inv(matrix);
        end

        function H = toHomogeneous(points)
            %TOHOMOGENEOUS Append a column of ones to N-by-3 points.
            points = cmr.graphics.Transform3D.checkPoints(points, 'points');
            H = [points, ones(size(points, 1), 1)];
        end

        function points = fromHomogeneous(H)
            %FROMHOMOGENEOUS Divide an N-by-4 homogeneous array by w.
            if ~(isnumeric(H) && ismatrix(H) && size(H, 2) == 4 ...
                    && isreal(H) && all(isfinite(H(:))))
                error('cmr:graphics:Transform3D:invalidHomogeneousPoints', ...
                    'Homogeneous points must be a finite real N-by-4 array.');
            end

            w = H(:, 4);
            if any(abs(w) < 1e-12)
                error('cmr:graphics:Transform3D:zeroHomogeneousCoordinate', ...
                    'All homogeneous coordinates w must be non-zero.');
            end

            points = H(:, 1:3) ./ repmat(w, 1, 3);
            if ~all(isfinite(points(:)))
                error('cmr:graphics:Transform3D:nonFiniteCoordinate', ...
                    'Homogeneous division produced non-finite coordinates.');
            end
        end

        function transformed = transformPoints(matrix, points)
            %TRANSFORMPOINTS Apply a 4x4 matrix to N-by-3 points.
            matrix = cmr.graphics.Transform3D.checkMatrix(matrix, 'matrix');
            H = cmr.graphics.Transform3D.toHomogeneous(points);
            transformed = cmr.graphics.Transform3D.fromHomogeneous(H * matrix);
        end

        function mesh = transformMesh(matrix, mesh)
            %TRANSFORMMESH Apply a 4x4 matrix to an Issue #007 mesh struct.
            %
            %   The transformed mesh keeps the original name, faces, colour
            %   and parameter struct. A stored parameters.center, when present,
            %   is transformed as a point so translated or rotated meshes keep
            %   a consistent centre. Shape parameters such as radius and
            %   sideLength are deliberately left unchanged: they describe how
            %   the mesh was constructed, not its current world orientation.
            %
            %   The input mesh is never modified.

            matrix = cmr.graphics.Transform3D.checkMatrix(matrix, 'matrix');
            cmr.graphics.Transform3D.checkMesh(mesh);

            vertices = cmr.graphics.Transform3D.transformPoints( ...
                matrix, mesh.vertices);
            parameters = mesh.parameters;

            if isfield(parameters, 'center')
                center = parameters.center;
                if isnumeric(center) && isreal(center) ...
                        && numel(center) == 3 && all(isfinite(center(:)))
                    parameters.center = cmr.graphics.Transform3D.transformPoints( ...
                        matrix, double(center(:))');
                end
            end

            mesh = cmr.graphics.Mesh3D.create( ...
                mesh.name, vertices, mesh.faces, mesh.color, parameters);
        end

        function tf = isAffine(matrix, tolerance)
            %ISAFFINE Check whether the bottom row is [0 0 0 1].
            matrix = cmr.graphics.Transform3D.checkMatrix(matrix, 'matrix');
            if nargin < 2 || isempty(tolerance)
                tolerance = 1e-12;
            end
            if ~(isnumeric(tolerance) && isscalar(tolerance) ...
                    && isreal(tolerance) && isfinite(tolerance) ...
                    && tolerance >= 0)
                error('cmr:graphics:Transform3D:invalidTolerance', ...
                    'Tolerance must be a finite non-negative scalar.');
            end
            tf = norm(matrix(4, :) - [0, 0, 0, 1], Inf) <= tolerance;
        end
    end

    methods (Static, Access = private)
        function matrix = checkMatrix(matrix, name)
            if ~(isnumeric(matrix) && ismatrix(matrix) ...
                    && all(size(matrix) == [4, 4]) && isreal(matrix) ...
                    && all(isfinite(matrix(:))))
                error('cmr:graphics:Transform3D:invalidMatrix', ...
                    '%s must be a finite real 4-by-4 matrix.', name);
            end
            matrix = double(matrix);
        end

        function points = checkPoints(points, name)
            if ~(isnumeric(points) && ismatrix(points) ...
                    && size(points, 2) == 3 && isreal(points) ...
                    && all(isfinite(points(:))))
                error('cmr:graphics:Transform3D:invalidPoints', ...
                    '%s must be a finite real N-by-3 array.', name);
            end
            points = double(points);
        end

        function checkMesh(mesh)
            if ~isstruct(mesh) || ~isscalar(mesh)
                error('cmr:graphics:Transform3D:invalidMesh', ...
                    'Expected a scalar mesh struct.');
            end

            required = {'name', 'vertices', 'faces', 'color', 'parameters'};
            for index = 1:numel(required)
                if ~isfield(mesh, required{index})
                    error('cmr:graphics:Transform3D:invalidMesh', ...
                        'Mesh is missing the "%s" field.', required{index});
                end
            end

            cmr.graphics.Transform3D.checkPoints(mesh.vertices, 'mesh.vertices');

            if ~iscell(mesh.faces) || isempty(mesh.faces)
                error('cmr:graphics:Transform3D:invalidMesh', ...
                    'Mesh faces must be a non-empty cell array.');
            end
            if ~(isnumeric(mesh.color) && numel(mesh.color) == 3 ...
                    && isreal(mesh.color) && all(isfinite(mesh.color(:))))
                error('cmr:graphics:Transform3D:invalidMesh', ...
                    'Mesh colour must contain three finite RGB components.');
            end
            if ~(isstruct(mesh.parameters) && isscalar(mesh.parameters))
                error('cmr:graphics:Transform3D:invalidMesh', ...
                    'Mesh parameters must be a scalar struct.');
            end
        end

        function offset = parseTranslation(varargin)
            if numel(varargin) == 1
                offset = varargin{1};
                if ~(isnumeric(offset) && isreal(offset) ...
                        && numel(offset) == 3 && all(isfinite(offset(:))))
                    error('cmr:graphics:Transform3D:invalidTranslation', ...
                        'Translation must be a finite [dx dy dz] point.');
                end
                offset = double(offset(:))';
            elseif numel(varargin) == 3
                values = [varargin{:}];
                if ~(isnumeric(values) && isreal(values) ...
                        && all(isfinite(values(:))))
                    error('cmr:graphics:Transform3D:invalidTranslation', ...
                        'Each translation component must be finite.');
                end
                offset = double(values(:))';
            else
                error('cmr:graphics:Transform3D:invalidTranslation', ...
                    'Use translation(dx,dy,dz) or translation([dx dy dz]).');
            end
        end

        function factors = parseScaling(varargin)
            if numel(varargin) == 1
                factors = varargin{1};
                if ~(isnumeric(factors) && isreal(factors) ...
                        && (isscalar(factors) || numel(factors) == 3) ...
                        && all(isfinite(factors(:))))
                    error('cmr:graphics:Transform3D:invalidScale', ...
                        'Scaling must be a finite scalar or [sx sy sz].');
                end
                if isscalar(factors)
                    factors = repmat(double(factors), 1, 3);
                else
                    factors = double(factors(:))';
                end
            elseif numel(varargin) == 3
                values = [varargin{:}];
                if ~(isnumeric(values) && isreal(values) ...
                        && all(isfinite(values(:))))
                    error('cmr:graphics:Transform3D:invalidScale', ...
                        'Each scaling factor must be finite.');
                end
                factors = double(values(:))';
            else
                error('cmr:graphics:Transform3D:invalidScale', ...
                    'Use scaling(s) or scaling(sx,sy,sz).');
            end

            if any(factors == 0)
                error('cmr:graphics:Transform3D:invalidScale', ...
                    'Scaling factors must be non-zero.');
            end
        end

        function theta = parseAngle(angle, angleUnit)
            if ~(isnumeric(angle) && isscalar(angle) ...
                    && isreal(angle) && isfinite(angle))
                error('cmr:graphics:Transform3D:invalidAngle', ...
                    'Rotation angle must be a finite real scalar.');
            end
            theta = double(angle);

            if nargin < 2 || isempty(angleUnit)
                angleUnit = "radians";
            end
            if isstring(angleUnit) && isscalar(angleUnit)
                unit = lower(char(angleUnit));
            elseif ischar(angleUnit) && isrow(angleUnit)
                unit = lower(angleUnit);
            else
                error('cmr:graphics:Transform3D:invalidAngleUnit', ...
                    'Angle unit must be "radians" or "degrees".');
            end

            switch unit
                case {'rad', 'radian', 'radians'}
                    % No conversion needed.
                case {'deg', 'degree', 'degrees'}
                    theta = theta * pi / 180;
                otherwise
                    error('cmr:graphics:Transform3D:invalidAngleUnit', ...
                        'Angle unit must be "radians" or "degrees".');
            end
        end

        function matrices = parseMatrixList(arguments)
            if numel(arguments) == 1 && iscell(arguments{1})
                matrices = arguments{1};
            else
                matrices = arguments;
            end

            if isempty(matrices)
                error('cmr:graphics:Transform3D:invalidSequence', ...
                    'At least one transformation matrix is required.');
            end

            for index = 1:numel(matrices)
                matrices{index} = cmr.graphics.Transform3D.checkMatrix( ...
                    matrices{index}, sprintf('matrix %d', index));
            end
        end
    end
end
