function category = TasteCategoryForFood(foodRef)
%TASTECATEGORYFORFOOD Return the canonical correct category for a food.
%
%   MATLAB derives this value from its own dataset. An empty string is
%   returned for an unknown food identifier; Unity never receives this
%   mapping for local validation.

    category = "";
    food = FindTasteFood(foodRef);
    if ~isempty(food)
        category = food.category;
    end
end
