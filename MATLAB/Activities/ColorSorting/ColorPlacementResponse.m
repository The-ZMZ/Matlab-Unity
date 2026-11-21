function response = ColorPlacementResponse(itemId, basketId)
%COLORPLACEMENTRESPONSE Build the MATLAB response for one color placement.
%
%   The returned struct is activity data, not a new wire message. Its valid,
%   correct, code and message fields match the fields used by the existing
%   PLACE_RES / ERROR protocol responses.

    response = struct( ...
        'valid', false, ...
        'correct', false, ...
        'code', "", ...
        'message', "", ...
        'itemColor', "", ...
        'basketColor', "", ...
        'basketId', "");

    item = FindColorItem(itemId);
    if isempty(item)
        response.code = cmr.net.Protocol.ErrorUnknownObject;
        response.message = "Unknown color item '" + string(itemId) + "'.";
        return
    end

    basket = FindColorBasket(basketId);
    if isempty(basket)
        response.code = cmr.net.Protocol.ErrorUnknownTarget;
        response.message = "Unknown basket '" + string(basketId) + "'.";
        return
    end

    response.valid = true;
    response.itemColor = item.color;
    response.basketColor = basket.color;
    response.basketId = basket.id;
    response.correct = ValidateColorAnswer(item.color, basket.color);

    if response.correct
        response.message = "Correct placement";
    else
        response.message = "Incorrect placement. Try again.";
    end
end
