function status = ColorSortingCompletion(solvedItemIds)
%COLORSORTINGCOMPLETION Check Color Sorting completion from solved item ids.
%
%   Only unique, known item ids count. The activity is complete when all twelve
%   MATLAB-defined items have been solved correctly.

    data = ColorSortingData();
    if nargin < 1 || isempty(solvedItemIds)
        solved = strings(1, 0);
    else
        solved = reshape(string(solvedItemIds), 1, []);
    end

    knownIds = strings(1, numel(data.items));
    for k = 1:numel(data.items)
        knownIds(k) = data.items(k).id;
    end

    valid = ~ismissing(solved) & strlength(solved) > 0 & ismember(solved, knownIds);
    solved = unique(solved(valid));

    status = struct( ...
        'solved', solved, ...
        'score', numel(solved), ...
        'total', numel(data.items), ...
        'completed', numel(solved) == numel(data.items));
end
