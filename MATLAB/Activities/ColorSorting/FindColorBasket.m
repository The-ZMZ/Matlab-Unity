function basket = FindColorBasket(basketId)
%FINDCOLORBASKET Return the MATLAB basket record for a protocol target id.

    basket = struct('id', {}, 'color', {});
    if ~(ischar(basketId) && isrow(basketId)) && ...
       ~(isstring(basketId) && isscalar(basketId) && ~ismissing(basketId))
        return
    end

    data = ColorSortingData();
    wanted = string(basketId);
    for k = 1:numel(data.baskets)
        if data.baskets(k).id == wanted
            basket = data.baskets(k);
            return
        end
    end
end
