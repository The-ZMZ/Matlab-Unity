function [tf, text] = TasteScalarText(value)
%TASTESCALARTEXT Return trimmed scalar text for taste input validation.
%
%   [tf, text] = TasteScalarText(value) is true only for a non-empty char row
%   or a non-missing string scalar. Whitespace-only text is rejected. This
%   shared guard prevents malformed requests from producing array results.

    tf = false;
    text = "";

    if ischar(value) && isrow(value) && ~isempty(value)
        tf = true;
        text = string(value);
    elseif isstring(value) && isscalar(value) && ~ismissing(value) && strlength(value) > 0
        tf = true;
        text = string(value);
    end

    if tf
        text = strtrim(text);
        tf = strlength(text) > 0;
    end
end
