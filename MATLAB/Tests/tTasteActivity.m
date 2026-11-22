classdef tTasteActivity < matlab.unittest.TestCase
%TTASTEACTIVITY Unit tests for the authoritative TASTE activity data.
%
%   The food-to-taste mapping lives only in MATLAB. Unity never receives the
%   correct answers; it sends a placement request and displays this verdict.

    properties
        Definition
        ObjectIds
    end

    methods (TestClassSetup)
        function addProjectPath(testCase) %#ok<INUSD>
            root = fileparts(fileparts(mfilename('fullpath')));
            testCase.applyFixture(matlab.unittest.fixtures.PathFixture(root));
        end
    end

    methods (TestMethodSetup)
        function loadTasteDefinition(testCase)
            root = fileparts(fileparts(mfilename('fullpath')));
            jsonPath = fullfile(root, '..', 'Shared_Data', 'ActivityData', 'taste.json');
            testCase.assertThat(exist(jsonPath, 'file') == 2, ...
                matlab.unittest.constraints.IsTrue(), ...
                'Taste activity definition is missing.');
            raw = jsondecode(fileread(jsonPath));
            testCase.Definition = cmr.activities.Definition.fromStruct(raw);
            testCase.ObjectIds = testCase.Definition.objectIds();
        end
    end

    methods (Test)
        function datasetHasEightObjectsAndFourTargets(testCase)
            testCase.verifyEqual(string(testCase.Definition.Id), "TASTE");
            testCase.verifyEqual(testCase.Definition.objectCount(), 8);
            testCase.verifyEqual(testCase.Definition.Targets, ...
                ["sweet", "sour", "bitter", "salty"]);
            testCase.verifyEqual(numel(unique(testCase.ObjectIds)), 8);
        end

        function everyDeclaredTasteMappingValidates(testCase)
            ids = testCase.ObjectIds;
            for k = 1:numel(ids)
                target = testCase.Definition.targetFor(ids(k));
                result = cmr.activities.Validator.validate( ...
                    testCase.Definition, ids(k), target);
                testCase.verifyTrue(result.valid, ...
                    sprintf('Declared target must be valid for %s.', string(ids(k))));
                testCase.verifyTrue(result.correct, ...
                    sprintf('Declared target must be correct for %s.', string(ids(k))));
            end
        end

        function wrongTasteIsIncorrect(testCase)
            result = cmr.activities.Validator.validate( ...
                testCase.Definition, "food_honey", "sour");
            testCase.verifyTrue(result.valid);
            testCase.verifyFalse(result.correct);
        end

        function unknownFoodIsRejected(testCase)
            result = cmr.activities.Validator.validate( ...
                testCase.Definition, "food_unknown", "sweet");
            testCase.verifyFalse(result.valid);
            testCase.verifyEqual(result.code, cmr.net.Protocol.ErrorUnknownObject);
        end

        function completionOnlyAfterEightDistinctCorrectPlacements(testCase)
            session = cmr.game.Session(3);
            ids = testCase.ObjectIds;

            for k = 1:7
                outcome = session.applyPlacement(testCase.Definition, ids(k), true);
                testCase.verifyEqual(outcome.score, k);
                testCase.verifyFalse(outcome.completed);
                testCase.verifyEqual(outcome.stars, 0);
            end

            outcome = session.applyPlacement(testCase.Definition, ids(8), true);
            testCase.verifyTrue(outcome.completed);
            testCase.verifyEqual(outcome.score, 8);
            testCase.verifyEqual(outcome.stars, 1);
            testCase.verifyTrue(session.isCompleted("TASTE"));
        end

        function incorrectAndDuplicateDoNotAdvanceCompletion(testCase)
            session = cmr.game.Session(3);
            ids = testCase.ObjectIds;

            wrong = session.applyPlacement(testCase.Definition, ids(1), false);
            testCase.verifyFalse(wrong.newlySolved);
            testCase.verifyEqual(wrong.score, 0);
            testCase.verifyFalse(wrong.completed);

            first = session.applyPlacement(testCase.Definition, ids(1), true);
            duplicate = session.applyPlacement(testCase.Definition, ids(1), true);
            testCase.verifyTrue(first.newlySolved);
            testCase.verifyFalse(duplicate.newlySolved);
            testCase.verifyEqual(duplicate.score, 1);
            testCase.verifyFalse(duplicate.completed);

            for k = 2:7
                session.applyPlacement(testCase.Definition, ids(k), true);
            end
            testCase.verifyFalse(session.isCompleted("TASTE"));

            last = session.applyPlacement(testCase.Definition, ids(8), true);
            testCase.verifyTrue(last.completed);
            testCase.verifyEqual(session.Stars, 1);
        end
    end
end
