function basketId = ColorBasketForItem(itemId)
%COLORBASKETFORITEM Identify the target basket for a Color Sorting item.
%
%   This is MATLAB-side target identification derived from the MATLAB-owned
%   item data. Unity does not receive or use this mapping to decide answers.

    item = FindColorItem(itemId);
    if isempty(item)
        basketId = "";
        return
    end

    basketId = item.basketId;
end
