function [next, response] = TasteSortingApplyPlacement(progress, foodRef, selectedCategory)
%TASTESORTINGAPPLYPLACEMENT Apply one validated answer to activity progress.
%
%   [next, response] = TasteSortingApplyPlacement(progress, foodRef,
%   selectedCategory) returns the updated progress struct and an extended
%   placement response. MATLAB decides correctness from its own dataset.
%
%   Progress rules:
%     - an invalid answer or unknown food changes nothing;
%     - a new food is added once to 'answered' and to 'correct' or 'incorrect';
%     - repeating a submitted food never increases answeredCount;
%     - a later correct answer for a previously incorrect food replaces the
%       incorrect record, so the food is counted exactly once as correct;
%     - completion requires all eight foods to be correct.
%
%   Extended response fields in addition to TastePlacementResponse:
%     newlyAnswered logical - this food had not been answered before
%     newlySolved   logical - this food became correct because of this answer
%     duplicate     logical - this food had been submitted before

    if nargin < 1 || isempty(progress)
        progress = TasteSortingProgress();
    end
    if ~isstruct(progress) || ~isscalar(progress) || ...
            ~all(isfield(progress, {'answered', 'correct', 'incorrect'}))
        error('cmr:activities:TasteSorting:invalidProgress', ...
            'Progress must be a TasteSortingProgress struct.');
    end

    current = TasteSortingProgress(progress.answered, progress.correct, progress.incorrect);
    response = TastePlacementResponse(foodRef, selectedCategory);
    response.newlyAnswered = false;
    response.newlySolved = false;
    response.duplicate = false;

    key = "";
    if response.valid
        key = TasteFoodKey(foodRef);
    end
    if strlength(key) == 0
        next = current;
        return
    end

    answered = current.answered;
    correct = current.correct;
    incorrect = current.incorrect;

    if any(answered == key)
        response.duplicate = true;
        if response.correct && ~any(correct == key)
            incorrect = incorrect(incorrect ~= key);
            correct = TasteNormalizeKeys([correct, key]);
            response.newlySolved = true;
        end
    else
        answered = TasteNormalizeKeys([answered, key]);
        if response.correct
            correct = TasteNormalizeKeys([correct, key]);
            response.newlySolved = true;
        else
            incorrect = TasteNormalizeKeys([incorrect, key]);
        end
        response.newlyAnswered = true;
    end

    next = TasteSortingProgress(answered, correct, incorrect);
end
