function key = TasteFoodKey(foodRef)
%TASTEFOODKEY Return the canonical protocol key for a food identifier.
%
%   Progress tracking is keyed by this stable string so numeric ids, display
%   names and protocol keys all resolve to the same food. An empty string is
%   returned when the identifier is unknown.

    key = "";
    food = FindTasteFood(foodRef);
    if ~isempty(food)
        key = food.key;
    end
end
