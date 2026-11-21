function [status, response] = ColorSortingApplyPlacement(solvedItemIds, itemId, basketId)
%COLORSORTINGAPPLYPLACEMENT Apply a validated placement to activity progress.
%
%   MATLAB decides correctness from its own color data, records only correct
%   unique items, and reports whether the activity is now complete.

    response = ColorPlacementResponse(itemId, basketId);
    status = ColorSortingCompletion(solvedItemIds);
    previousScore = status.score;
    status.correct = response.correct;
    status.newlySolved = false;

    if ~response.valid || ~response.correct
        return
    end

    solved = [status.solved, string(itemId)]; %#ok<AGROW>
    status = ColorSortingCompletion(solved);
    status.correct = true;
    status.newlySolved = status.score > previousScore;
end
