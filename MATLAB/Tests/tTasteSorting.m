classdef tTasteSorting < matlab.unittest.TestCase
%TTASTESORTING Unit and protocol tests for MATLAB Taste Classification logic.
%
%   Covers the reusable food dataset, authoritative answer validation,
%   progress tracking (including duplicate prevention), completion, and the
%   existing TCP/JSON PLACE_REQ path for activity "TASTE".

    properties (Access = private)
        Data
        ProjectRoot
        Client = []
        RxBuffer uint8 = uint8([])
        PendingMessages = {}
    end

    methods (TestClassSetup)
        function addProjectPath(testCase)
            root = fileparts(fileparts(mfilename('fullpath')));
            testCase.applyFixture(matlab.unittest.fixtures.PathFixture( ...
                root, 'IncludingSubfolders', true));
        end
    end

    methods (TestMethodSetup)
        function loadData(testCase)
            testCase.Data = TasteSortingData();
            testCase.ProjectRoot = fileparts(fileparts(fileparts(mfilename('fullpath'))));
            testCase.Client = [];
            testCase.RxBuffer = uint8([]);
            testCase.PendingMessages = {};
        end
    end

    methods (Test)
        function datasetContainsExactlyEightValidFoods(testCase)
            testCase.verifyEqual(string(testCase.Data.activityId), "TASTE");
            testCase.verifyEqual(numel(testCase.Data.foods), 8);
            testCase.verifyEqual(string(testCase.Data.categories), ...
                ["Sweet", "Sour", "Bitter", "Salty"]);

            required = {'id', 'key', 'name', 'category', 'targetId'};
            for k = 1:numel(testCase.Data.foods)
                testCase.verifyTrue(all(isfield(testCase.Data.foods(k), required)));
                testCase.verifyNotEmpty(char(testCase.Data.foods(k).key));
                testCase.verifyNotEmpty(char(testCase.Data.foods(k).name));
                testCase.verifyEqual(string(testCase.Data.foods(k).targetId), ...
                    lower(string(testCase.Data.foods(k).category)));
            end
        end

        function allFoodIdsAreUniqueAndStable(testCase)
            ids = [testCase.Data.foods.id];
            keys = strings(1, numel(testCase.Data.foods));
            for k = 1:numel(testCase.Data.foods)
                keys(k) = testCase.Data.foods(k).key;
            end

            testCase.verifyEqual(sort(ids), 1:8);
            testCase.verifyEqual(numel(unique(ids)), 8);
            testCase.verifyEqual(numel(unique(keys)), 8);
            for k = 1:8
                testCase.verifyEqual( ...
                    string(TasteFoodKey(k)), testCase.Data.foods(k).key);
            end
        end

        function allFoodCategoryMappingsAreCorrect(testCase)
            expectedNames = [ ...
                "Ice Cream", "Lemon", "Tomato", "Garlic", ...
                "Pizza", "Coffee", "Chips", "Donuts"];
            expectedCategories = [ ...
                "Sweet", "Sour", "Sour", "Bitter", ...
                "Salty", "Bitter", "Salty", "Sweet"];

            for k = 1:8
                food = testCase.Data.foods(k);
                testCase.verifyEqual(string(food.name), expectedNames(k));
                testCase.verifyEqual(string(food.category), expectedCategories(k));
                testCase.verifyEqual( ...
                    TasteCategoryForFood(food.id), expectedCategories(k));
                testCase.verifyEqual( ...
                    TasteCategoryForFood(food.key), expectedCategories(k));
                testCase.verifyEqual( ...
                    TasteCategoryForFood(char(food.name)), expectedCategories(k));
            end
        end

        function datasetValidatorRejectsInvalidEntries(testCase)
            duplicateId = testCase.Data;
            duplicateId.foods(2).id = duplicateId.foods(1).id;
            testCase.verifyError(@() ValidateTasteDataset(duplicateId), ...
                'cmr:activities:TasteSorting:invalidDataset');

            duplicateKey = testCase.Data;
            duplicateKey.foods(2).key = duplicateKey.foods(1).key;
            testCase.verifyError(@() ValidateTasteDataset(duplicateKey), ...
                'cmr:activities:TasteSorting:invalidDataset');

            wrongCategory = testCase.Data;
            wrongCategory.foods(1).category = "Umami";
            testCase.verifyError(@() ValidateTasteDataset(wrongCategory), ...
                'cmr:activities:TasteSorting:invalidDataset');

            missingName = testCase.Data;
            missingName.foods(3).name = "";
            testCase.verifyError(@() ValidateTasteDataset(missingName), ...
                'cmr:activities:TasteSorting:invalidDataset');

            wrongCount = testCase.Data;
            wrongCount.foods = wrongCount.foods(1:7);
            testCase.verifyError(@() ValidateTasteDataset(wrongCount), ...
                'cmr:activities:TasteSorting:invalidDataset');
        end

        function correctAnswersAreAcceptedForEveryFood(testCase)
            categories = ["Sweet", "Sour", "Sour", "Bitter", ...
                "Salty", "Bitter", "Salty", "Sweet"];
            for k = 1:8
                key = testCase.Data.foods(k).key;
                result = ValidateTasteAnswer(key, char(categories(k)));
                testCase.verifyTrue(result.valid, "Food " + key);
                testCase.verifyTrue(result.correct, "Food " + key);
                testCase.verifyEqual(result.code, "");
                testCase.verifyEqual(result.foodId, k);
                testCase.verifyEqual(result.expectedCategory, categories(k));
            end
        end

        function directTasteComparisonIsSupported(testCase)
            result = ValidateTasteAnswer("Sweet", "sweet");
            testCase.verifyTrue(result.valid);
            testCase.verifyTrue(result.correct);
            testCase.verifyEqual(result.foodId, 0);
            testCase.verifyEqual(result.foodKey, "");
            testCase.verifyEqual(result.expectedCategory, "Sweet");
        end

        function incorrectAnswersAreRejectedForEveryFood(testCase)
            allCategories = string(testCase.Data.categories);
            for k = 1:8
                food = testCase.Data.foods(k);
                wrong = allCategories(allCategories ~= food.category);
                result = ValidateTasteAnswer(food.key, wrong(1));
                testCase.verifyTrue(result.valid);
                testCase.verifyFalse(result.correct, "Food " + food.key);
                testCase.verifyEqual(result.code, "");
                testCase.verifyEqual(result.message, ...
                    "Incorrect classification. Try again.");
            end
        end

        function categoryMatchingIsCaseInsensitive(testCase)
            testCase.verifyTrue(ValidateTasteAnswer("ice_cream", "SWEET").correct);
            testCase.verifyTrue(ValidateTasteAnswer('LEMON', 'sour').correct);
            testCase.verifyTrue(ValidateTasteAnswer("COFFEE", "BiTtEr").correct);
            testCase.verifyTrue(ValidateTasteAnswer('bitter', 'BITTER').correct);
            testCase.verifyTrue(ValidateTasteAnswer("Ice Cream", "sWeEt").correct);
        end

        function emptyInputIsHandledWithoutThrowing(testCase)
            emptyFood = ValidateTasteAnswer([], "Sweet");
            testCase.verifyFalse(emptyFood.valid);
            testCase.verifyFalse(emptyFood.correct);
            testCase.verifyEqual(emptyFood.code, cmr.net.Protocol.ErrorInvalidRequest);

            emptyCategory = ValidateTasteAnswer("ice_cream", []);
            testCase.verifyFalse(emptyCategory.valid);
            testCase.verifyEqual(emptyCategory.code, cmr.net.Protocol.ErrorInvalidRequest);

            blank = ValidateTasteAnswer("   ", "Sweet");
            testCase.verifyFalse(blank.valid);
            testCase.verifyEqual(blank.code, cmr.net.Protocol.ErrorInvalidRequest);
        end

        function invalidCategoriesAreRejected(testCase)
            result = ValidateTasteAnswer("ice_cream", "Umami");
            testCase.verifyFalse(result.valid);
            testCase.verifyFalse(result.correct);
            testCase.verifyEqual(result.code, cmr.net.Protocol.ErrorUnknownTarget);
            testCase.verifyEqual(result.selectedCategory, "");

            numeric = ValidateTasteAnswer("ice_cream", 7);
            testCase.verifyEqual(numeric.code, cmr.net.Protocol.ErrorInvalidRequest);

            array = ValidateTasteAnswer("ice_cream", ["Sweet", "Sour"]);
            testCase.verifyEqual(array.code, cmr.net.Protocol.ErrorInvalidRequest);
        end

        function unknownFoodIdentifiersAreRejected(testCase)
            unknownId = ValidateTasteAnswer(99, "Sweet");
            testCase.verifyFalse(unknownId.valid);
            testCase.verifyEqual(unknownId.code, cmr.net.Protocol.ErrorUnknownObject);

            unknownKey = ValidateTasteAnswer("pineapple", "Sweet");
            testCase.verifyEqual(unknownKey.code, cmr.net.Protocol.ErrorUnknownObject);

            invalidId = ValidateTasteAnswer(1.5, "Sweet");
            testCase.verifyEqual(invalidId.code, cmr.net.Protocol.ErrorUnknownObject);

            nonFinite = ValidateTasteAnswer(NaN, "Sweet");
            testCase.verifyEqual(nonFinite.code, cmr.net.Protocol.ErrorUnknownObject);
        end

        function malformedFoodInputIsRejectedWithoutArrayResults(testCase)
            cellInput = ValidateTasteAnswer({'ice_cream'}, "Sweet");
            testCase.verifyFalse(cellInput.valid);
            testCase.verifyEqual(cellInput.code, cmr.net.Protocol.ErrorInvalidRequest);
            testCase.verifyTrue(isscalar(cellInput.valid));

            structInput = ValidateTasteAnswer(struct('id', 1), "Sweet");
            testCase.verifyEqual(structInput.code, cmr.net.Protocol.ErrorInvalidRequest);

            numericArray = ValidateTasteAnswer([1, 2], "Sweet");
            testCase.verifyEqual(numericArray.code, cmr.net.Protocol.ErrorInvalidRequest);
        end

        function placementResponseUsesExistingProtocolErrorCodes(testCase)
            unknown = TastePlacementResponse("pineapple", "Sweet");
            testCase.verifyFalse(unknown.valid);
            testCase.verifyEqual(unknown.code, cmr.net.Protocol.ErrorUnknownObject);

            badCategory = TastePlacementResponse("ice_cream", "Umami");
            testCase.verifyEqual(badCategory.code, cmr.net.Protocol.ErrorUnknownTarget);

            correct = TastePlacementResponse("lemon", "SOUR");
            testCase.verifyTrue(correct.valid);
            testCase.verifyTrue(correct.correct);
            testCase.verifyEqual(correct.foodName, "Lemon");
            testCase.verifyEqual(correct.selectedCategory, "Sour");
        end

        function completionRequiresAllEightUniqueFoods(testCase)
            keys = testCase.foodKeys();

            empty = TasteSortingCompletion(strings(1, 0));
            testCase.verifyEqual(empty.score, 0);
            testCase.verifyEqual(empty.total, 8);
            testCase.verifyEqual(empty.completionPercent, 0);
            testCase.verifyFalse(empty.completed);

            partial = TasteSortingCompletion(keys(1:7));
            testCase.verifyEqual(partial.score, 7);
            testCase.verifyEqual(partial.completionPercent, 88);
            testCase.verifyFalse(partial.completed);

            complete = TasteSortingCompletion(keys);
            testCase.verifyEqual(complete.score, 8);
            testCase.verifyEqual(complete.completionPercent, 100);
            testCase.verifyTrue(complete.completed);
        end

        function completionIgnoresDuplicatesAndUnknownFoods(testCase)
            keys = testCase.foodKeys();
            status = TasteSortingCompletion([keys(1), keys(1), "pineapple", 5]);
            testCase.verifyEqual(status.score, 2);
            testCase.verifyFalse(status.completed);
            testCase.verifyEqual(numel(status.solved), 2);
        end

        function completionPercentageIsCalculatedCorrectly(testCase)
            keys = testCase.foodKeys();
            testCase.verifyEqual(TasteSortingCompletion(keys(1:4)).completionPercent, 50);
            testCase.verifyEqual(TasteSortingCompletion(keys(1:2)).completionPercent, 25);
            testCase.verifyEqual(TasteSortingCompletion(keys).completionPercent, 100);
        end

        function progressTracksAnsweredCorrectIncorrectAndRemaining(testCase)
            progress = TasteSortingProgress();
            testCase.verifyEqual(progress.total, 8);
            testCase.verifyEqual(progress.answeredCount, 0);
            testCase.verifyEqual(progress.correctCount, 0);
            testCase.verifyEqual(progress.incorrectCount, 0);
            testCase.verifyEqual(progress.remainingCount, 8);
            testCase.verifyEqual(progress.completionPercent, 0);
            testCase.verifyFalse(progress.completed);

            [progress, ~] = TasteSortingApplyPlacement(progress, 1, "Sweet");
            [progress, ~] = TasteSortingApplyPlacement(progress, 2, "Sweet");
            [progress, ~] = TasteSortingApplyPlacement(progress, "unanswered_food", "Sweet");

            testCase.verifyEqual(string(progress.answered), ["ice_cream", "lemon"]);
            testCase.verifyEqual(string(progress.correct), "ice_cream");
            testCase.verifyEqual(string(progress.incorrect), "lemon");
            testCase.verifyEqual(progress.answeredCount, 2);
            testCase.verifyEqual(progress.correctCount, 1);
            testCase.verifyEqual(progress.incorrectCount, 1);
            testCase.verifyEqual(progress.remainingCount, 6);
            testCase.verifyEqual(progress.answeredPercent, 25);
            testCase.verifyEqual(progress.completionPercent, 13);
            testCase.verifyFalse(progress.allAnswered);
            testCase.verifyFalse(progress.completed);
        end

        function invalidAnswersDoNotChangeProgress(testCase)
            progress = TasteSortingProgress();
            [after, response] = TasteSortingApplyPlacement(progress, "ice_cream", "Umami");
            testCase.verifyFalse(response.valid);
            testCase.verifyFalse(response.newlyAnswered);
            testCase.verifyEqual(after.answeredCount, 0);
            testCase.verifyEqual(after.completionPercent, 0);

            [after, response] = TasteSortingApplyPlacement(progress, "pineapple", "Sweet");
            testCase.verifyFalse(response.valid);
            testCase.verifyEqual(after.answeredCount, 0);
        end

        function duplicateSubmissionsDoNotIncreaseProgress(testCase)
            progress = TasteSortingProgress();
            [progress, first] = TasteSortingApplyPlacement(progress, "ice_cream", "Sweet");
            testCase.verifyTrue(first.newlyAnswered);
            testCase.verifyTrue(first.newlySolved);
            testCase.verifyFalse(first.duplicate);

            [progress, repeat] = TasteSortingApplyPlacement(progress, 1, "sweet");
            testCase.verifyTrue(repeat.duplicate);
            testCase.verifyFalse(repeat.newlyAnswered);
            testCase.verifyFalse(repeat.newlySolved);
            testCase.verifyEqual(progress.answeredCount, 1);
            testCase.verifyEqual(progress.correctCount, 1);
            testCase.verifyEqual(progress.completionPercent, 13);
        end

        function correctingWrongAnswerCountsFoodOnce(testCase)
            progress = TasteSortingProgress();
            [progress, wrong] = TasteSortingApplyPlacement(progress, "lemon", "Sweet");
            testCase.verifyFalse(wrong.correct);
            testCase.verifyEqual(progress.incorrectCount, 1);

            [progress, right] = TasteSortingApplyPlacement(progress, "lemon", "SOUR");
            testCase.verifyTrue(right.correct);
            testCase.verifyTrue(right.duplicate);
            testCase.verifyTrue(right.newlySolved);
            testCase.verifyEqual(progress.answeredCount, 1);
            testCase.verifyEqual(progress.correctCount, 1);
            testCase.verifyEqual(progress.incorrectCount, 0);
            testCase.verifyEqual(progress.remainingCount, 7);
        end

        function progressValuesStayConsistentAfterMixedAnswers(testCase)
            progress = TasteSortingProgress();
            answers = {1, "Sweet"; 2, "Sweet"; 3, "Sour"; 4, "Bitter";
                5, "Salty"; 6, "Bitter"; 7, "Salty"; 8, "Sweet"};
            for k = 1:size(answers, 1)
                [progress, ~] = TasteSortingApplyPlacement( ...
                    progress, answers{k, 1}, answers{k, 2});
                testCase.verifyEqual(progress.answeredCount, ...
                    progress.correctCount + progress.incorrectCount);
                testCase.verifyEqual(progress.remainingCount, ...
                    progress.total - progress.answeredCount);
                testCase.verifyGreaterThanOrEqual(progress.completionPercent, 0);
                testCase.verifyLessThanOrEqual(progress.completionPercent, 100);
                testCase.verifyEqual(progress.completed, progress.correctCount == 8);
            end
        end

        function allEightFoodsCanBeProcessedSuccessfully(testCase)
            progress = TasteSortingProgress();
            for k = 1:8
                food = testCase.Data.foods(k);
                [progress, response] = TasteSortingApplyPlacement( ...
                    progress, food.key, food.category);
                testCase.verifyTrue(response.correct, "Food " + food.key);
                testCase.verifyTrue(response.newlyAnswered);
            end

            testCase.verifyEqual(progress.answeredCount, 8);
            testCase.verifyEqual(progress.correctCount, 8);
            testCase.verifyEqual(progress.incorrectCount, 0);
            testCase.verifyEqual(progress.remainingCount, 0);
            testCase.verifyEmpty(progress.remaining);
            testCase.verifyEqual(progress.answeredPercent, 100);
            testCase.verifyEqual(progress.completionPercent, 100);
            testCase.verifyTrue(progress.allAnswered);
            testCase.verifyTrue(progress.completed);

            completion = TasteSortingCompletion(progress.correct);
            testCase.verifyTrue(completion.completed);
            testCase.verifyEqual(completion.score, 8);
        end

        function definitionWorksWithGenericValidatorAndSession(testCase)
            definition = TasteSortingDefinition();
            testCase.verifyEqual(definition.Id, "TASTE");
            testCase.verifyEqual(definition.objectCount(), 8);

            session = cmr.game.Session(3);
            for k = 1:8
                food = testCase.Data.foods(k);
                result = cmr.activities.Validator.validate( ...
                    definition, food.key, food.targetId);
                testCase.verifyTrue(result.valid);
                testCase.verifyTrue(result.correct);
                session.applyPlacement(definition, food.key, true);
            end

            wrong = cmr.activities.Validator.validate( ...
                definition, "ice_cream", "salty");
            testCase.verifyTrue(wrong.valid);
            testCase.verifyFalse(wrong.correct);

            unknown = cmr.activities.Validator.validate( ...
                definition, "ice_cream", "umami");
            testCase.verifyEqual(unknown.code, cmr.net.Protocol.ErrorUnknownTarget);

            testCase.verifyTrue(session.isCompleted("TASTE"));
            testCase.verifyEqual(session.Stars, 1);
        end

        function sharedJsonMatchesMatlabDataset(testCase)
            path = fullfile(testCase.ProjectRoot, 'Shared_Data', ...
                'ActivityData', 'TASTE.json');
            raw = jsondecode(fileread(path));

            testCase.verifyEqual(string(raw.id), "TASTE");
            testCase.verifyEqual(numel(raw.objects), 8);
            testCase.verifyEqual(numel(raw.targets), 4);
            testCase.verifyEqual(reshape(string(raw.targets), 1, []), ...
                ["sweet", "sour", "bitter", "salty"]);

            for k = 1:8
                food = testCase.Data.foods(k);
                testCase.verifyEqual(string(raw.objects(k).id), string(food.key));
                testCase.verifyEqual(string(raw.objects(k).name), string(food.name));
                testCase.verifyEqual(string(raw.objects(k).category), string(food.category));
                testCase.verifyEqual(string(raw.objects(k).target), string(food.targetId));
            end
        end

        function registryLoadsTasteFromDefaultDataDirectory(testCase)
            directory = fullfile(testCase.ProjectRoot, 'Shared_Data', 'ActivityData');
            registry = cmr.activities.Registry(directory);

            testCase.verifyTrue(registry.has("TASTE"));
            testCase.verifyTrue(registry.has("COLOR"));
            testCase.verifyEqual(registry.get("TASTE").objectCount(), 8);
        end

        function serverHandlesTastePlacementsOverTcp(testCase)
            registry = cmr.activities.Registry(fullfile( ...
                testCase.ProjectRoot, 'Shared_Data', 'ActivityData'));
            session = cmr.game.Session(3);
            server = cmr.net.Server(registry, session, ...
                'Port', 55102, 'Verbose', false);
            server.start();
            serverCleanup = onCleanup(@() server.stop());

            testCase.Client = tcpclient("127.0.0.1", 55102);
            clientCleanup = onCleanup(@() delete(testCase.Client));

            deadline = tic;
            while ~server.ClientConnected && toc(deadline) < 5
                pause(0.02);
            end
            testCase.verifyTrue(server.ClientConnected);

            for k = 1:8
                food = testCase.Data.foods(k);
                request = struct( ...
                    'type', cmr.net.Protocol.TypePlaceRequest, ...
                    'requestId', "req_taste_" + string(k), ...
                    'activity', "TASTE", ...
                    'objectId', food.key, ...
                    'targetId', food.targetId, ...
                    'position', struct('x', 0.2 + 0.05 * k, 'y', 0.5));
                writeline(testCase.Client, cmr.net.Codec.encode(request));

                replies = testCase.readMessages(2);
                testCase.verifyEqual(string(replies{1}.type), ...
                    cmr.net.Protocol.TypePlaceResponse);
                testCase.verifyTrue(replies{1}.valid);
                testCase.verifyTrue(replies{1}.correct);
                testCase.verifyEqual(replies{1}.score, k);
                testCase.verifyEqual(string(replies{2}.type), ...
                    cmr.net.Protocol.TypeActivityResult);
                testCase.verifyEqual(replies{2}.completed, k == 8);
                testCase.verifyEqual(replies{2}.stars, double(k == 8));
            end

            testCase.verifyTrue(session.isCompleted("TASTE"));
            testCase.verifyEqual(session.Stars, 1);
        end
    end

    methods (Access = private)
        function keys = foodKeys(~)
            keys = [ ...
                "ice_cream", "lemon", "tomato", "garlic", ...
                "pizza", "coffee", "chips", "donuts"];
        end

        function messages = readMessages(testCase, count, timeoutSeconds)
            if nargin < 3
                timeoutSeconds = 5;
            end

            deadline = tic;
            while numel(testCase.PendingMessages) < count && toc(deadline) < timeoutSeconds
                testCase.pumpSocket();
                pause(0.02);
            end
            testCase.verifyGreaterThanOrEqual(numel(testCase.PendingMessages), count, ...
                'Timed out waiting for MATLAB server replies.');

            take = min(count, numel(testCase.PendingMessages));
            messages = testCase.PendingMessages(1:take);
            testCase.PendingMessages(1:take) = [];
        end

        function pumpSocket(testCase)
            available = testCase.Client.NumBytesAvailable;
            if available <= 0
                return
            end
            data = read(testCase.Client, available, "uint8");
            testCase.RxBuffer = [testCase.RxBuffer, reshape(data, 1, [])];
            while true
                delimiter = find(testCase.RxBuffer == uint8(10), 1, 'first');
                if isempty(delimiter)
                    break
                end
                lineBytes = testCase.RxBuffer(1:delimiter - 1);
                testCase.RxBuffer(1:delimiter) = [];
                lineBytes = lineBytes(lineBytes ~= uint8(13));
                if isempty(lineBytes)
                    continue
                end
                text = string(native2unicode(lineBytes, "UTF-8"));
                testCase.PendingMessages{end + 1} = jsondecode(char(text));
            end
        end
    end
end
