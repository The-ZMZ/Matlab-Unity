function keys = TasteNormalizeKeys(refs)
%TASTENORMALIZEKEYS Convert food references into ordered unique protocol keys.
%
%   Accepts numeric ids, protocol keys, display names or a mixture of them in
%   any array or cell array. Unknown and duplicate references are ignored.
%   Dataset order is preserved so progress output is deterministic.

    keys = strings(1, 0);
    if nargin < 1 || isempty(refs)
        return
    end

    items = reshape(refs, 1, []);
    for k = 1:numel(items)
        item = items(k);
        if iscell(item)
            if isempty(item{1})
                continue
            end
            item = item{1};
        end
        key = TasteFoodKey(item);
        if strlength(key) > 0 && ~any(keys == key)
            keys(end + 1) = key; %#ok<AGROW>
        end
    end
end
