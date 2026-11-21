classdef tColorSortingActivity < matlab.unittest.TestCase
%TCOLORSORTINGACTIVITY Issue #022 checks for the shipped Color Sorting data.
%
%   These tests use the real Shared_Data/ActivityData/color_sorting.json file,
%   not a small fixture, so the twelve-object activity and its first star are
%   verified exactly as the game uses them.

    properties (Access = private)
        Definition
        Registry
    end

    methods (TestClassSetup)
        function addProjectPath(testCase)
            root = fileparts(fileparts(mfilename('fullpath')));
            testCase.applyFixture(matlab.unittest.fixtures.PathFixture(root));
        end
    end

    methods (TestMethodSetup)
        function loadDefinition(testCase)
            root = fileparts(fileparts(mfilename('fullpath')));
            dataDirectory = fullfile(root, '..', 'Shared_Data', 'ActivityData');
            testCase.Registry = cmr.activities.Registry(dataDirectory);
            testCase.Definition = testCase.Registry.get("COLOR");
            testCase.verifyNotEmpty(testCase.Definition, ...
                'The COLOR activity definition was not loaded from Shared_Data.');
        end
    end

    methods (Test)
        function definitionHasSixTargetsAndTwelveObjects(testCase)
            definition = testCase.Definition;

            testCase.verifyEqual(definition.objectCount(), 12);
            testCase.verifyNumElements(definition.Targets, 6);
            testCase.verifyEqual(numel(unique(definition.Targets)), 6);
            testCase.verifyEqual(numel(unique(definition.objectIds())), 12);

            expected = ["basket_red", "basket_blue", "basket_green", ...
                "basket_yellow", "basket_orange", "basket_purple"];
            testCase.verifyEqual(sort(string(definition.Targets)), sort(expected));
        end

        function validatorAcceptsOnlyMatchingBaskets(testCase)
            definition = testCase.Definition;
            ids = definition.objectIds();

            for k = 1:numel(ids)
                target = definition.targetFor(ids(k));
                correct = cmr.activities.Validator.validate(definition, ids(k), target);
                testCase.verifyTrue(correct.valid);
                testCase.verifyTrue(correct.correct);

                wrongTarget = definition.Targets(find(definition.Targets ~= target, 1));
                incorrect = cmr.activities.Validator.validate(definition, ids(k), wrongTarget);
                testCase.verifyTrue(incorrect.valid);
                testCase.verifyFalse(incorrect.correct);
            end
        end

        function twelveCorrectPlacementsAwardExactlyOneStar(testCase)
            definition = testCase.Definition;
            session = cmr.game.Session(3);
            ids = definition.objectIds();

            for k = 1:11
                outcome = session.applyPlacement(definition, ids(k), true);
                testCase.verifyFalse(outcome.completed);
                testCase.verifyEqual(outcome.score, k);
                testCase.verifyEqual(outcome.stars, 0);
            end

            outcome = session.applyPlacement(definition, ids(12), true);
            testCase.verifyTrue(outcome.completed);
            testCase.verifyEqual(outcome.score, 12);
            testCase.verifyEqual(outcome.stars, 1);
            testCase.verifyTrue(session.isCompleted("COLOR"));
            testCase.verifyFalse(session.isGameCompleted());

            % Re-sorting an object keeps the same score and never adds a star.
            again = session.applyPlacement(definition, ids(1), true);
            testCase.verifyFalse(again.completed);
            testCase.verifyEqual(again.score, 12);
            testCase.verifyEqual(again.stars, 1);
        end

        function wrongPlacementDoesNotChangeProgress(testCase)
            definition = testCase.Definition;
            session = cmr.game.Session(3);
            ids = definition.objectIds();

            outcome = session.applyPlacement(definition, ids(1), false);
            testCase.verifyFalse(outcome.newlySolved);
            testCase.verifyEqual(outcome.score, 0);
            testCase.verifyEqual(outcome.stars, 0);
        end
    end
end
