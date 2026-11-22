function definition = TasteSortingDefinition()
%TASTESORTINGDEFINITION Build a generic activity definition for TASTE.
%
%   This adapter maps the MATLAB-owned taste dataset onto the existing
%   cmr.activities.Definition used by the generic Validator, Registry and
%   Session classes. The correct food-to-target mapping stays in MATLAB;
%   Unity receives only opaque object and target identifiers.

    data = TasteSortingData();

    objects = repmat(struct('id', "", 'target', ""), 1, numel(data.foods));
    targets = strings(1, numel(data.categories));
    for k = 1:numel(data.foods)
        objects(k).id = data.foods(k).key;
        objects(k).target = data.foods(k).targetId;
    end
    for k = 1:numel(data.categories)
        targets(k) = lower(data.categories(k));
    end

    definition = cmr.activities.Definition( ...
        data.activityId, objects, targets, "Taste Classification");
end
