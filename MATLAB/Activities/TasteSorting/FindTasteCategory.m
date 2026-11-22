function category = FindTasteCategory(categoryRef)
%FINDTASTECATEGORY Resolve a taste category name without case sensitivity.
%
%   category = FindTasteCategory(categoryRef) returns a scalar struct with
%   canonical 'name' ("Sweet", "Sour", "Bitter" or "Salty") and lowercase
%   protocol 'id' for a valid input. An empty struct is returned for an
%   empty, malformed or unknown category.

    category = struct('id', {}, 'name', {});

    [hasText, text] = TasteScalarText(categoryRef);
    if ~hasText
        return
    end

    data = TasteSortingData();
    for k = 1:numel(data.categories)
        candidate = data.categories(k);
        if strcmpi(char(text), char(candidate))
            category = struct('id', lower(candidate), 'name', candidate);
            return
        end
    end
end
