function ValidateTasteDataset(data)
%VALIDATETASTEDATASET Verify the internal Taste Sorting dataset invariants.
%
%   ValidateTasteDataset(data) throws cmr:activities:TasteSorting:invalidDataset
%   when the dataset has the wrong size, missing fields, duplicate or invalid
%   food ids/keys/names, or a food whose category or target is invalid. It is
%   called by TasteSortingData so every consumer receives validated data.
%
%   This validator deliberately does not call TasteSortingData, avoiding
%   recursion when it is used to check a candidate dataset in tests.

    if ~isstruct(data) || ~isscalar(data)
        error('cmr:activities:TasteSorting:invalidDataset', ...
            'Taste dataset must be a scalar struct.');
    end

    required = {'activityId', 'categories', 'foods'};
    if ~all(isfield(data, required))
        error('cmr:activities:TasteSorting:invalidDataset', ...
            'Taste dataset is missing required fields.');
    end

    if ~(ischar(data.activityId) || isstring(data.activityId)) || ...
            ~isscalar(string(data.activityId)) || string(data.activityId) ~= "TASTE"
        error('cmr:activities:TasteSorting:invalidDataset', ...
            'Taste dataset activityId must be "TASTE".');
    end

    expectedCategories = ["Sweet", "Sour", "Bitter", "Salty"];
    categories = string(data.categories);
    if numel(categories) ~= numel(expectedCategories) || ...
            ~all(ismember(categories, expectedCategories)) || ...
            ~all(ismember(expectedCategories, categories))
        error('cmr:activities:TasteSorting:invalidDataset', ...
            'Taste dataset must define exactly Sweet, Sour, Bitter and Salty.');
    end

    foods = data.foods;
    if ~isstruct(foods) || numel(foods) ~= 8
        error('cmr:activities:TasteSorting:invalidDataset', ...
            'Taste dataset must contain exactly eight foods.');
    end

    foodFields = {'id', 'key', 'name', 'category', 'targetId'};
    if ~all(isfield(foods, foodFields))
        error('cmr:activities:TasteSorting:invalidDataset', ...
            'Every taste food needs id, key, name, category and targetId fields.');
    end

    ids = zeros(1, numel(foods));
    keys = strings(1, numel(foods));
    names = strings(1, numel(foods));
    for k = 1:numel(foods)
        id = foods(k).id;
        if ~isnumeric(id) || ~isscalar(id) || ~isfinite(id) || id ~= round(id) || id < 1
            error('cmr:activities:TasteSorting:invalidDataset', ...
                'Food %d has an invalid numeric id.', k);
        end
        ids(k) = double(id);

        key = string(foods(k).key);
        name = string(foods(k).name);
        category = string(foods(k).category);
        target = string(foods(k).targetId);

        if ~isscalar(key) || ismissing(key) || strlength(key) == 0 || ...
                isempty(regexp(char(key), '^[a-z][a-z0-9_]*$', 'once'))
            error('cmr:activities:TasteSorting:invalidDataset', ...
                'Food %d has an invalid protocol key.', k);
        end
        if ~isscalar(name) || ismissing(name) || strlength(strtrim(name)) == 0
            error('cmr:activities:TasteSorting:invalidDataset', ...
                'Food %d has an empty name.', k);
        end
        if ~isscalar(category) || ismissing(category) || ~any(categories == category)
            error('cmr:activities:TasteSorting:invalidDataset', ...
                'Food %d has an invalid category.', k);
        end
        if ~isscalar(target) || ismissing(target) || target ~= lower(category)
            error('cmr:activities:TasteSorting:invalidDataset', ...
                'Food %d targetId does not match its category.', k);
        end

        keys(k) = key;
        names(k) = strtrim(name);
    end

    if numel(unique(ids)) ~= numel(ids) || ~isequal(sort(ids), 1:8)
        error('cmr:activities:TasteSorting:invalidDataset', ...
            'Food ids must be the unique stable ids 1 through 8.');
    end
    if numel(unique(keys)) ~= numel(keys)
        error('cmr:activities:TasteSorting:invalidDataset', ...
            'Food protocol keys must be unique.');
    end
    if numel(unique(lower(names))) ~= numel(names)
        error('cmr:activities:TasteSorting:invalidDataset', ...
            'Food names must be unique (case insensitive).');
    end
end
