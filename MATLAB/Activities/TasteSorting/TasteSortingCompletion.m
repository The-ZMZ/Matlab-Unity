function status = TasteSortingCompletion(solvedFoodRefs)
%TASTESORTINGCOMPLETION Check Taste Classification completion from solved foods.
%
%   Only known, unique foods count. Completion requires all eight foods to be
%   solved correctly. Numeric ids, protocol keys and display names are all
%   accepted and normalised to protocol keys; unknown or repeated references
%   are ignored.

    data = TasteSortingData();
    total = numel(data.foods);

    if nargin < 1
        solved = strings(1, 0);
    else
        solved = TasteNormalizeKeys(solvedFoodRefs);
    end

    status = struct( ...
        'solved', solved, ...
        'score', numel(solved), ...
        'total', total, ...
        'completionPercent', round(100 * numel(solved) / total), ...
        'completed', numel(solved) == total);
end
