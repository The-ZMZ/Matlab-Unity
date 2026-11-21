function food = FindTasteFood(foodRef)
%FINDTASTEFOOD Resolve a food identifier against the MATLAB-owned dataset.
%
%   food = FindTasteFood(foodRef) accepts a numeric stable id (1-8), a string
%   protocol key (for example "ice_cream"), a display name (for example
%   "Ice Cream"), or the numeric id written as text ("1"). Comparisons for
%   text inputs ignore case. An empty struct is returned for missing,
%   unknown or malformed identifiers; callers convert that into a structured
%   protocol error.

    food = struct('id', {}, 'key', {}, 'name', {}, 'category', {}, 'targetId', {});

    if isempty(foodRef)
        return
    end

    [hasText, text] = TasteScalarText(foodRef);
    if ~hasText && ~(isnumeric(foodRef) && isscalar(foodRef))
        return
    end

    data = TasteSortingData();

    if ~hasText
        wantedId = double(foodRef);
        if ~isfinite(wantedId) || wantedId ~= round(wantedId)
            return
        end
        for k = 1:numel(data.foods)
            if data.foods(k).id == wantedId
                food = data.foods(k);
                return
            end
        end
        return
    end

    for k = 1:numel(data.foods)
        entry = data.foods(k);
        if strcmpi(char(text), char(entry.key)) || ...
                strcmpi(char(text), char(entry.name)) || ...
                strcmpi(char(text), num2str(entry.id))
            food = entry;
            return
        end
    end
end
