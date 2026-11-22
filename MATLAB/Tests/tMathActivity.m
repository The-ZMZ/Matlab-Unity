classdef tMathActivity < matlab.unittest.TestCase
%TMATHACTIVITY Unit tests for the authoritative MATH activity data.
%
%   Numbers 1-10 are matched to dot groups containing the same number of
%   dots. The mapping lives only in MATLAB; Unity sends a placement request
%   and displays the verdict returned by MATLAB.

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
        function loadMathDefinition(testCase)
            root = fileparts(fileparts(mfilename('fullpath')));
            jsonPath = fullfile(root, '..', 'Shared_Data', 'ActivityData', 'math.json');
            testCase.assertThat(exist(jsonPath, 'file') == 2, ...
                matlab.unittest.constraints.IsTrue(), ...
                'Math activity definition is missing.');
            raw = jsondecode(fileread(jsonPath));
            testCase.Definition = cmr.activities.Definition.fromStruct(raw);
            testCase.ObjectIds = testCase.Definition.objectIds();
        end
    end

    methods (Test)
        function datasetHasTenNumbersAndTenDotGroups(testCase)
            testCase.verifyEqual(string(testCase.Definition.Id), "MATH");
            testCase.verifyEqual(testCase.Definition.objectCount(), 10);
            testCase.verifyEqual(numel(testCase.Definition.Targets), 10);
            testCase.verifyEqual(numel(unique(testCase.ObjectIds)), 10);

            for n = 1:10
                testCase.verifyTrue(any(testCase.ObjectIds == "number_" + n));
                testCase.verifyTrue(any(testCase.Definition.Targets == "dots_" + n));
            end
        end

        function everyDeclaredMappingValidates(testCase)
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

        function numberMatchesItsOwnDotGroupOnly(testCase)
            result = cmr.activities.Validator.validate( ...
                testCase.Definition, "number_3", "dots_3");
            testCase.verifyTrue(result.valid);
            testCase.verifyTrue(result.correct);

            wrong = cmr.activities.Validator.validate( ...
                testCase.Definition, "number_3", "dots_4");
            testCase.verifyTrue(wrong.valid);
            testCase.verifyFalse(wrong.correct);
        end

        function unknownNumberIsRejected(testCase)
            result = cmr.activities.Validator.validate( ...
                testCase.Definition, "number_11", "dots_1");
            testCase.verifyFalse(result.valid);
            testCase.verifyEqual(result.code, cmr.net.Protocol.ErrorUnknownObject);
        end

        function completionOnlyAfterTenDistinctCorrectPlacements(testCase)
            session = cmr.game.Session(3);
            ids = testCase.ObjectIds;

            for k = 1:9
                outcome = session.applyPlacement(testCase.Definition, ids(k), true);
                testCase.verifyEqual(outcome.score, k);
                testCase.verifyFalse(outcome.completed);
                testCase.verifyEqual(outcome.stars, 0);
            end

            outcome = session.applyPlacement(testCase.Definition, ids(10), true);
            testCase.verifyTrue(outcome.completed);
            testCase.verifyEqual(outcome.score, 10);
            testCase.verifyEqual(outcome.stars, 1);
            testCase.verifyTrue(session.isCompleted("MATH"));
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

            for k = 2:9
                session.applyPlacement(testCase.Definition, ids(k), true);
            end
            testCase.verifyFalse(session.isCompleted("MATH"));

            last = session.applyPlacement(testCase.Definition, ids(10), true);
            testCase.verifyTrue(last.completed);
            testCase.verifyEqual(session.Stars, 1);
        end
    end
end
