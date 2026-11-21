function result = ValidateTasteAnswer(foodTaste, selectedCategory)
%VALIDATETASTEANSWER Validate one Taste Classification answer in MATLAB.
%
%   result = ValidateTasteAnswer(foodTaste, selectedCategory) compares the
%   correct taste of a food with the category chosen by the player. It never
%   throws for malformed input: the same result structure is returned in
%   every case.
%
%   foodTaste may be:
%     - a stable numeric id (1-8) from the MATLAB dataset;
%     - a protocol key, for example "ice_cream" (case insensitive);
%     - a display name, for example "Ice Cream" (case insensitive);
%     - a taste category itself, for example "Sweet" (case insensitive).
%
%   selectedCategory must be Sweet, Sour, Bitter or Salty (case insensitive).
%
%   result fields (always present):
%     valid            logical - inputs resolved to a known food/taste and a
%                                valid selected category
%     correct          logical - MATLAB's authoritative answer
%     code             string  - "" on success, otherwise a protocol error
%                                code from cmr.net.Protocol
%     message          string  - human-readable feedback
%     foodId           double  - stable id 1-8, or 0 for direct taste input
%     foodKey          string  - protocol key, or "" for direct taste input
%     foodName         string  - display name, or "" for direct taste input
%     expectedCategory string  - canonical correct category
%     selectedCategory string  - canonical selected category, or "" if invalid
%
%   Error codes: INVALID_REQUEST for empty/malformed input, UNKNOWN_OBJECT for
%   an unknown food identifier and UNKNOWN_TARGET for an invalid category.

    result = struct( ...
        'valid', false, ...
        'correct', false, ...
        'code', "", ...
        'message', "", ...
        'foodId', 0, ...
        'foodKey', "", ...
        'foodName', "", ...
        'expectedCategory', "", ...
        'selectedCategory', "");

    [hasCategoryText, categoryText] = TasteScalarText(selectedCategory);
    if ~hasCategoryText
        result.code = cmr.net.Protocol.ErrorInvalidRequest;
        result.message = "Selected category must be non-empty text.";
        return
    end

    category = FindTasteCategory(categoryText);
    if isempty(category)
        result.code = cmr.net.Protocol.ErrorUnknownTarget;
        result.message = "Unknown taste category '" + categoryText + "'.";
        return
    end
    result.selectedCategory = category.name;

    if isempty(foodTaste)
        result.code = cmr.net.Protocol.ErrorInvalidRequest;
        result.message = "Food identifier must not be empty.";
        return
    end

    [hasFoodText, foodText] = TasteScalarText(foodTaste);
    if ~hasFoodText && ~(isnumeric(foodTaste) && isscalar(foodTaste) && isreal(foodTaste))
        result.code = cmr.net.Protocol.ErrorInvalidRequest;
        result.message = "Food identifier must be a scalar id, key, name or taste.";
        return
    end

    directTaste = [];
    if hasFoodText
        directTaste = FindTasteCategory(foodText);
    end

    if ~isempty(directTaste)
        result.expectedCategory = directTaste.name;
    else
        food = FindTasteFood(foodTaste);
        if isempty(food)
            result.code = cmr.net.Protocol.ErrorUnknownObject;
            result.message = "Unknown food '" + string(foodTaste) + "'.";
            return
        end
        result.foodId = food.id;
        result.foodKey = food.key;
        result.foodName = food.name;
        result.expectedCategory = food.category;
    end

    result.valid = true;
    result.correct = strcmpi(char(result.expectedCategory), char(result.selectedCategory));
    if result.correct
        result.message = "Correct classification";
    else
        result.message = "Incorrect classification. Try again.";
    end
end
