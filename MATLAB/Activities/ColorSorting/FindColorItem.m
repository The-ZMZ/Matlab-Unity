function item = FindColorItem(itemId)
%FINDCOLORITEM Return the MATLAB item record for a protocol item id.
%
%   An empty struct is returned for missing or invalid ids; the caller must
%   convert that into an ERROR response through the existing protocol.

    item = struct('id', {}, 'name', {}, 'color', {}, 'basketId', {});
    if ~(ischar(itemId) && isrow(itemId)) && ...
       ~(isstring(itemId) && isscalar(itemId) && ~ismissing(itemId))
        return
    end

    data = ColorSortingData();
    wanted = string(itemId);
    for k = 1:numel(data.items)
        if data.items(k).id == wanted
            item = data.items(k);
            return
        end
    end
end
