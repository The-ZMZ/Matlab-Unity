classdef Transform2D
%TRANSFORM2D 2D homogeneous geometric transformations (3x3).
%
%   Convention:
%     - Points are row vectors [x y] or N-by-2 matrices. Internally they are
%       converted to homogeneous column vectors [x; y; 1] and transformed as
%       p' = M * p.
%     - Coordinate system: standard mathematical 2D, origin at bottom-left,
%       y-axis up, angles positive counter-clockwise.
%     - compose(A, B, C) applies A first, then B, then C and returns
%       M = C * B * A so that apply(M, P) == apply(C, apply(B, apply(A, P))).

    methods (Static)
        function T = identity()
            %IDENTITY Return the 3x3 identity matrix.
            T = eye(3);
        end

        function T = translation(tx, ty)
            %TRANSLATION Translation by (tx, ty).
            cmr.graphics.Transform2D.mustBeFiniteScalar(tx, 'tx', 'cmr:graphics:Transform2D:invalidTranslation');
            cmr.graphics.Transform2D.mustBeFiniteScalar(ty, 'ty', 'cmr:graphics:Transform2D:invalidTranslation');
            T = [1 0 tx;
                 0 1 ty;
                 0 0 1 ];
        end

        function T = rotation(thetaRadians)
            %ROTATION Counter-clockwise rotation about the origin (radians).
            cmr.graphics.Transform2D.mustBeFiniteScalar(thetaRadians, 'thetaRadians', 'cmr:graphics:Transform2D:invalidAngle');
            c = cos(thetaRadians);
            s = sin(thetaRadians);
            T = [c -s 0;
                 s  c 0;
                 0  0 1];
        end

        function T = rotationDegrees(thetaDegrees)
            %ROTATIONDEGREES Counter-clockwise rotation about the origin (degrees).
            cmr.graphics.Transform2D.mustBeFiniteScalar(thetaDegrees, 'thetaDegrees', 'cmr:graphics:Transform2D:invalidAngle');
            T = cmr.graphics.Transform2D.rotation(deg2rad(thetaDegrees));
        end

        function T = scaling(sx, sy)
            %SCALING Non-uniform scaling about the origin.
            cmr.graphics.Transform2D.mustBeFiniteScalar(sx, 'sx', 'cmr:graphics:Transform2D:invalidScale');
            cmr.graphics.Transform2D.mustBeFiniteScalar(sy, 'sy', 'cmr:graphics:Transform2D:invalidScale');
            T = [sx 0  0;
                 0  sy 0;
                 0  0  1];
        end

        function T = reflectionX()
            %REFLECTIONX Reflection about the x-axis (y -> -y).
            T = [1 0 0; 0 -1 0; 0 0 1];
        end

        function T = reflectionY()
            %REFLECTIONY Reflection about the y-axis (x -> -x).
            T = [-1 0 0; 0 1 0; 0 0 1];
        end

        function T = reflectionOrigin()
            %REFLECTIONORIGIN Reflection about the origin.
            T = [-1 0 0; 0 -1 0; 0 0 1];
        end

        function T = reflectionLineYX()
            %REFLECTIONLINEYX Reflection about the line y = x.
            T = [0 1 0; 1 0 0; 0 0 1];
        end

        function T = reflectionLineYNegX()
            %REFLECTIONLINEYNEGX Reflection about the line y = -x.
            T = [0 -1 0; -1 0 0; 0 0 1];
        end

        function T = reflectionAboutLine(point, thetaRadians)
            %REFLECTIONABOUTLINE Reflection about a line through point at angle theta.
            %   point is [x0 y0] on the line; thetaRadians is the line angle
            %   from the positive x-axis, counter-clockwise.
            p = cmr.graphics.Transform2D.mustBePoint(point, 'point', 'cmr:graphics:Transform2D:invalidPivot');
            cmr.graphics.Transform2D.mustBeFiniteScalar(thetaRadians, 'thetaRadians', 'cmr:graphics:Transform2D:invalidAngle');
            toOrigin = cmr.graphics.Transform2D.translation(-p(1), -p(2));
            back = cmr.graphics.Transform2D.translation(p(1), p(2));
            align = cmr.graphics.Transform2D.rotation(-thetaRadians);
            unalign = cmr.graphics.Transform2D.rotation(thetaRadians);
            % apply toOrigin, align, reflect-x, unalign, back
            T = back * unalign * cmr.graphics.Transform2D.reflectionX() * align * toOrigin;
        end

        function T = shearX(k)
            %SHEARX Shear in the x direction: x' = x + k*y.
            cmr.graphics.Transform2D.mustBeFiniteScalar(k, 'k', 'cmr:graphics:Transform2D:invalidShear');
            T = [1 k 0; 0 1 0; 0 0 1];
        end

        function T = shearY(k)
            %SHEARY Shear in the y direction: y' = k*x + y.
            cmr.graphics.Transform2D.mustBeFiniteScalar(k, 'k', 'cmr:graphics:Transform2D:invalidShear');
            T = [1 0 0; k 1 0; 0 0 1];
        end

        function T = aboutPoint(M, pivot)
            %ABOUTPOINT Apply transformation M about a pivot point instead of origin.
            %   Returns T(pivot)*M*T(-pivot) so that pivot remains fixed.
            cmr.graphics.Transform2D.mustBeTransform(M);
            p = cmr.graphics.Transform2D.mustBePoint(pivot, 'pivot', 'cmr:graphics:Transform2D:invalidPivot');
            toOrigin = cmr.graphics.Transform2D.translation(-p(1), -p(2));
            back = cmr.graphics.Transform2D.translation(p(1), p(2));
            T = back * M * toOrigin;
        end

        function M = compose(varargin)
            %COMPOSE Combine transformations in application order.
            %   compose(A, B, C) returns C*B*A, so A is applied first.
            if nargin == 0
                M = cmr.graphics.Transform2D.identity();
                return;
            end
            M = eye(3);
            for i = 1:nargin
                Ti = varargin{i};
                cmr.graphics.Transform2D.mustBeTransform(Ti);
                M = Ti * M;
            end
        end

        function Q = apply(T, P)
            %APPLY Apply transformation T to N-by-2 points, returning N-by-2.
            cmr.graphics.Transform2D.mustBeTransform(T);
            cmr.graphics.Transform2D.mustBePoints(P);
            n = size(P, 1);
            H = [P, ones(n, 1)]';   % 3-by-N homogeneous
            Hp = T * H;
            Q = Hp(1:2, :)';
        end

        function q = applyPoint(T, p)
            %APPLYPOINT Apply transformation T to a single [x y] point.
            Q = cmr.graphics.Transform2D.apply(T, p);
            q = Q;
        end
    end

    methods (Static, Access = private)
        function mustBeFiniteScalar(value, name, id)
            if ~(isscalar(value) && isnumeric(value) && isreal(value) && isfinite(value))
                error(id, '%s must be a finite numeric scalar.', name);
            end
        end

        function p = mustBePoint(value, name, id)
            if ~(isnumeric(value) && isreal(value) && numel(value) == 2 && all(isfinite(value(:))))
                error(id, '%s must be a finite numeric [x y] point.', name);
            end
            p = double(value(:))';
        end

        function mustBePoints(P)
            if ~(isnumeric(P) && isreal(P) && ismatrix(P) && size(P,2) == 2 && all(isfinite(P(:))))
                error('cmr:graphics:Transform2D:invalidPoints', ...
                    'P must be a finite numeric N-by-2 matrix of points.');
            end
        end

        function mustBeTransform(T)
            if ~(isnumeric(T) && isreal(T) && isequal(size(T), [3 3]) && all(isfinite(T(:))))
                error('cmr:graphics:Transform2D:invalidMatrix', ...
                    'T must be a finite numeric 3-by-3 matrix.');
            end
            if norm(T(3,:) - [0 0 1]) > 1e-12
                error('cmr:graphics:Transform2D:invalidMatrix', ...
                    'T must be an affine 3-by-3 matrix with last row [0 0 1].');
            end
        end
    end
end
