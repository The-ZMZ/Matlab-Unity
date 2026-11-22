function progress = TasteSortingProgress(answeredRefs, correctRefs, incorrectRefs)
%TASTESORTINGPROGRESS Build consistent Taste Classification progress.
%
%   progress = TasteSortingProgress(answered, correct, incorrect) normalises
%   the three food lists to unique protocol keys and derives all counts,
%   remaining foods, percentages and completion flags from them.
%
%   answered   foods with at least one valid submission
%   correct    foods whose latest valid submission was correct
%   incorrect  foods whose latest valid submission was incorrect
%
%   Invariants enforced here: correct and incorrect are disjoint subsets of
%   answered. Violations throw cmr:activities:TasteSorting:inconsistentProgress
%   so progress values can never silently become invalid.

    data = TasteSortingData();
    allKeys = strings(1, numel(data.foods));
    for k = 1:numel(data.foods)
        allKeys(k) = data.foods(k).key;
    end
    total = numel(allKeys);

    if nargin < 1
        answered = strings(1, 0);
    else
        answered = TasteNormalizeKeys(answeredRefs);
    end
    if nargin < 2
        correct = strings(1, 0);
    else
        correct = TasteNormalizeKeys(correctRefs);
    end
    if nargin < 3
        incorrect = strings(1, 0);
    else
        incorrect = TasteNormalizeKeys(incorrectRefs);
    end

    if ~all(ismember(correct, answered)) || ~all(ismember(incorrect, answered)) || ...
            any(ismember(correct, incorrect))
        error('cmr:activities:TasteSorting:inconsistentProgress', ...
            'Correct and incorrect foods must be disjoint subsets of answered foods.');
    end

    remaining = allKeys(~ismember(allKeys, answered));
    answeredCount = numel(answered);
    correctCount = numel(correct);
    incorrectCount = numel(incorrect);
    remainingCount = numel(remaining);

    progress = struct( ...
        'answered', answered, ...
        'correct', correct, ...
        'incorrect', incorrect, ...
        'remaining', remaining, ...
        'answeredCount', answeredCount, ...
        'correctCount', correctCount, ...
        'incorrectCount', incorrectCount, ...
        'remainingCount', remainingCount, ...
        'total', total, ...
        'completionPercent', round(100 * correctCount / total), ...
        'answeredPercent', round(100 * answeredCount / total), ...
        'allAnswered', answeredCount == total, ...
        'completed', correctCount == total);
end
