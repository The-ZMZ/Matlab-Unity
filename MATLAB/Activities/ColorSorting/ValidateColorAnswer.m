function isCorrect = ValidateColorAnswer(itemColor, basketColor)
%VALIDATECOLORANSWER Compare an item color with a basket color.
%
%   isCorrect = ValidateColorAnswer(itemColor, basketColor) returns true for
%   matching text, ignoring case, and false for a mismatch or invalid input.
%   This is the equivalent of the original simple strcmpi validation, with
%   scalar text guards so malformed requests cannot produce a result array.

    isCorrect = false;
    if ~isScalarText(itemColor) || ~isScalarText(basketColor)
        return
    end

    isCorrect = strcmpi(char(string(itemColor)), char(string(basketColor)));
end

function tf = isScalarText(value)
    tf = (ischar(value) && isrow(value) && ~isempty(value)) || ...
         (isstring(value) && isscalar(value) && ~ismissing(value) && strlength(value) > 0);
end
