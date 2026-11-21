classdef tColorSorting < matlab.unittest.TestCase
%TCOLORSORTING Unit tests for the MATLAB-owned Color Sorting dataset and logic.

    properties
        Data
    end

    methods (TestClassSetup)
        function addProjectPath(testCase) %#ok<MANU>
            root = fileparts(fileparts(mfilename('fullpath')));
            testCase.applyFixture(matlab.unittest.fixtures.PathFixture( ...
                root, 'IncludingSubfolders', true));
        end
    end

    methods (TestMethodSetup)
        function loadData(testCase)
            testCase.Data = ColorSortingData();
        end
    end

    methods (Test)
        function datasetHasRequiredStructure(testCase)
            testCase.verifyEqual(string(testCase.Data.activityId), "COLOR");
            testCase.verifyEqual(numel(testCase.Data.colors), 6);
            testCase.verifyEqual(numel(testCase.Data.baskets), 6);
            testCase.verifyEqual(numel(testCase.Data.items), 12);
        end

        function everyItemHasUniqueIdNameColorAndBasket(testCase)
            ids = strings(1, numel(testCase.Data.items));
            for k = 1:numel(testCase.Data.items)
                item = testCase.Data.items(k);
                ids(k) = item.id;
                testCase.verifyNotEmpty(char(item.id));
                testCase.verifyNotEmpty(char(item.name));
                testCase.verifyTrue(any(testCase.Data.colors == item.color));
                testCase.verifyTrue(any([testCase.Data.baskets(:).id] == item.basketId));
                testCase.verifyEqual( ...
                    item.basketId, "basket_" + lower(item.color));
            end
            testCase.verifyEqual(numel(unique(ids)), 12);
        end

        function allTwelveObjectsHaveExpectedColors(testCase)
            expectedIds = [ ...
                "apple", "carrot", "duck", "leaf", "blue_car", "grapes", ...
                "strawberry", "basketball", "star", "frog", "balloon", "butterfly"];
            expectedNames = [ ...
                "Apple", "Carrot", "Duck", "Leaf", "Blue Car", "Grapes", ...
                "Strawberry", "Basketball", "Star", "Frog", "Balloon", "Butterfly"];
            expectedColors = [ ...
                "Red", "Orange", "Yellow", "Green", "Blue", "Purple", ...
                "Red", "Orange", "Yellow", "Green", "Blue", "Purple"];

            for k = 1:numel(expectedIds)
                item = FindColorItem(expectedIds(k));
                testCase.verifyNotEmpty(item, "Missing item " + expectedIds(k));
                testCase.verifyEqual(item.name, expectedNames(k));
                testCase.verifyEqual(item.color, expectedColors(k));
                testCase.verifyEqual( ...
                    ColorBasketForItem(expectedIds(k)), ...
                    "basket_" + lower(expectedColors(k)));
            end
        end

        function correctAnswersReturnTrue(testCase)
            for k = 1:numel(testCase.Data.items)
                item = testCase.Data.items(k);
                basket = FindColorBasket(item.basketId);
                testCase.verifyTrue(ValidateColorAnswer(item.color, basket.color));
                testCase.verifyTrue(ValidateColorAnswer( ...
                    char(item.color), char(basket.color)));
            end
        end

        function incorrectAnswersReturnFalse(testCase)
            for k = 1:numel(testCase.Data.items)
                item = testCase.Data.items(k);
                wrongColor = testCase.Data.colors(mod(k, numel(testCase.Data.colors)) + 1);
                testCase.verifyFalse(ValidateColorAnswer(item.color, wrongColor));
            end
        end

        function validationIsCaseInsensitive(testCase)
            testCase.verifyTrue(ValidateColorAnswer("red", "RED"));
            testCase.verifyTrue(ValidateColorAnswer('Blue', 'blue'));
        end

        function invalidInputsReturnFalse(testCase)
            testCase.verifyFalse(ValidateColorAnswer([], "Red"));
            testCase.verifyFalse(ValidateColorAnswer("Red", []));
            testCase.verifyFalse(ValidateColorAnswer(7, "Red"));
            testCase.verifyFalse(ValidateColorAnswer("Red", 7));
            testCase.verifyFalse(ValidateColorAnswer(["Red", "Blue"], "Red"));
            testCase.verifyFalse(ValidateColorAnswer("Red", {"Red"}));
        end

        function placementResponseReportsCorrectAndIncorrect(testCase)
            correct = ColorPlacementResponse("apple", "basket_red");
            testCase.verifyTrue(correct.valid);
            testCase.verifyTrue(correct.correct);
            testCase.verifyEqual(correct.itemColor, "Red");
            testCase.verifyEqual(correct.basketColor, "Red");
            testCase.verifyEqual(correct.message, "Correct placement");

            incorrect = ColorPlacementResponse("apple", "basket_blue");
            testCase.verifyTrue(incorrect.valid);
            testCase.verifyFalse(incorrect.correct);
            testCase.verifyEqual(incorrect.itemColor, "Red");
            testCase.verifyEqual(incorrect.basketColor, "Blue");
            testCase.verifyEqual(incorrect.message, "Incorrect placement. Try again.");
        end

        function placementResponseRejectsUnknownIds(testCase)
            unknownItem = ColorPlacementResponse("pineapple", "basket_red");
            testCase.verifyFalse(unknownItem.valid);
            testCase.verifyEqual(unknownItem.code, cmr.net.Protocol.ErrorUnknownObject);

            unknownBasket = ColorPlacementResponse("apple", "basket_gold");
            testCase.verifyFalse(unknownBasket.valid);
            testCase.verifyEqual(unknownBasket.code, cmr.net.Protocol.ErrorUnknownTarget);
        end

        function completionRequiresAllTwelveItems(testCase)
            ids = testCase.itemIds();

            empty = ColorSortingCompletion(strings(1, 0));
            testCase.verifyFalse(empty.completed);
            testCase.verifyEqual(empty.score, 0);
            testCase.verifyEqual(empty.total, 12);

            partial = ColorSortingCompletion(ids(1:11));
            testCase.verifyFalse(partial.completed);
            testCase.verifyEqual(partial.score, 11);

            complete = ColorSortingCompletion(ids);
            testCase.verifyTrue(complete.completed);
            testCase.verifyEqual(complete.score, 12);
        end

        function completionIgnoresUnknownAndDuplicateIds(testCase)
            ids = testCase.itemIds();
            status = ColorSortingCompletion([ids(1), ids(1), "not_an_item"]);
            testCase.verifyEqual(status.score, 1);
            testCase.verifyFalse(status.completed);
        end

        function applyingPlacementsTracksProgressAndCompletion(testCase)
            solved = strings(1, 0);
            ids = testCase.itemIds();

            [firstState, firstResponse] = ColorSortingApplyPlacement( ...
                solved, "apple", "basket_red");
            testCase.verifyTrue(firstResponse.correct);
            testCase.verifyTrue(firstState.correct);
            testCase.verifyTrue(firstState.newlySolved);
            testCase.verifyEqual(firstState.score, 1);

            [repeatState, ~] = ColorSortingApplyPlacement( ...
                firstState.solved, "apple", "basket_red");
            testCase.verifyFalse(repeatState.newlySolved);
            testCase.verifyEqual(repeatState.score, 1);

            [wrongState, wrongResponse] = ColorSortingApplyPlacement( ...
                repeatState.solved, "apple", "basket_blue");
            testCase.verifyFalse(wrongResponse.correct);
            testCase.verifyFalse(wrongState.correct);
            testCase.verifyEqual(wrongState.score, 1);

            state = wrongState;
            for k = 1:numel(ids)
                [state, ~] = ColorSortingApplyPlacement( ...
                    state.solved, ids(k), ColorBasketForItem(ids(k)));
            end
            testCase.verifyTrue(state.completed);
            testCase.verifyEqual(state.score, 12);
        end

        function definitionWorksWithGenericValidatorAndSession(testCase)
            definition = ColorSortingDefinition();
            testCase.verifyEqual(definition.Id, "COLOR");
            testCase.verifyEqual(definition.objectCount(), 12);

            session = cmr.game.Session(3);
            ids = testCase.itemIds();
            for k = 1:numel(ids)
                result = cmr.activities.Validator.validate( ...
                    definition, ids(k), ColorBasketForItem(ids(k)));
                testCase.verifyTrue(result.valid);
                testCase.verifyTrue(result.correct);
                session.applyPlacement(definition, ids(k), true);
            end

            testCase.verifyTrue(session.isCompleted("COLOR"));
            testCase.verifyEqual(session.Stars, 1);
        end

        function sharedJsonMatchesMatlabDataset(testCase)
            projectRoot = fileparts(fileparts(fileparts(mfilename('fullpath'))));
            jsonPath = fullfile(projectRoot, 'Shared_Data', 'ActivityData', 'COLOR.json');
            raw = jsondecode(fileread(jsonPath));

            testCase.verifyEqual(string(raw.id), "COLOR");
            testCase.verifyEqual(numel(raw.objects), 12);
            testCase.verifyEqual(numel(raw.targets), 6);

            ids = testCase.itemIds();
            for k = 1:numel(ids)
                testCase.verifyEqual(string(raw.objects(k).id), ids(k));
                testCase.verifyEqual( ...
                    string(raw.objects(k).target), ColorBasketForItem(ids(k)));
                testCase.verifyEqual( ...
                    string(raw.objects(k).color), FindColorItem(ids(k)).color);
            end
        end

        function unityIdentifiersMatchDataset(testCase)
            projectRoot = fileparts(fileparts(fileparts(mfilename('fullpath'))));
            sourcePath = fullfile(projectRoot, 'Unity', 'CleanMyRoom', 'Assets', ...
                'Scripts', 'Gameplay', 'Activities', 'ColorSortingIds.cs');
            source = fileread(sourcePath);
            ids = testCase.itemIds();

            for k = 1:numel(ids)
                testCase.verifyTrue( ...
                    contains(source, ['"', char(ids(k)), '"']), ...
                    "Unity item id missing: " + ids(k));
            end
            for k = 1:numel(testCase.Data.baskets)
                id = char(testCase.Data.baskets(k).id);
                testCase.verifyTrue( ...
                    contains(source, ['"', id, '"']), ...
                    "Unity basket id missing: " + string(id));
            end
        end
    end

    methods (Access = private)
        function ids = itemIds(~)
            ids = [ ...
                "apple", "carrot", "duck", "leaf", "blue_car", "grapes", ...
                "strawberry", "basketball", "star", "frog", "balloon", "butterfly"];
        end
    end
end
