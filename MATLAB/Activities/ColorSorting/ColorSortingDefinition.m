function definition = ColorSortingDefinition()
%COLORSORTINGDEFINITION Build the existing activity definition from MATLAB data.
%
%   This adapter lets the generic Registry/Validator/Session path validate the
%   twelve-item color activity without moving answer data into Unity.

    data = ColorSortingData();

    objects = repmat(struct('id', "", 'target', ""), 1, numel(data.items));
    targets = strings(1, numel(data.baskets));
    for k = 1:numel(data.items)
        objects(k).id = data.items(k).id;
        objects(k).target = data.items(k).basketId;
    end
    for k = 1:numel(data.baskets)
        targets(k) = data.baskets(k).id;
    end

    definition = cmr.activities.Definition( ...
        data.activityId, objects, targets, "Color Sorting");
end
