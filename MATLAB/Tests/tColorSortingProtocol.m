classdef tColorSortingProtocol < matlab.unittest.TestCase
%TCOLORSORTINGPROTOCOL Issue #022 end-to-end validation of all twelve toys.
%
%   A real TCP server loads the shipped COLOR definition and a real tcpclient
%   plays the Unity role. Port 55102 is used so the game's 55001 stays free.

    properties (Access = private)
        Server
        Client
        RxBuffer uint8 = uint8([])
        Pending = {}
    end

    methods (TestClassSetup)
        function addProjectPath(testCase)
            root = fileparts(fileparts(mfilename('fullpath')));
            testCase.applyFixture(matlab.unittest.fixtures.PathFixture(root));
        end
    end

    methods (TestMethodSetup)
        function startServer(testCase)
            root = fileparts(fileparts(mfilename('fullpath')));
            dataDirectory = fullfile(root, '..', 'Shared_Data', 'ActivityData');
            registry = cmr.activities.Registry(dataDirectory);
            session = cmr.game.Session(3);

            testCase.Server = cmr.net.Server(registry, session, ...
                'Port', 55102, 'Verbose', false);
            testCase.Server.start();
            testCase.Client = tcpclient("127.0.0.1", 55102);
            testCase.RxBuffer = uint8([]);
            testCase.Pending = {};

            deadline = tic;
            while ~testCase.Server.ClientConnected && toc(deadline) < 5
                pause(0.05);
            end
            testCase.verifyTrue(testCase.Server.ClientConnected, 'Client did not connect.');
        end
    end

    methods (TestMethodTeardown)
        function stopServer(testCase)
            if ~isempty(testCase.Client)
                try
                    delete(testCase.Client);
                catch
                    % Already closed.
                end
                testCase.Client = [];
            end
            if ~isempty(testCase.Server)
                testCase.Server.stop();
                testCase.Server = [];
            end
        end
    end

    methods (Test)
        function wrongBasketIsRejectedWithoutScore(testCase)
            testCase.place("req_wrong", "object_01", "basket_blue");
            messages = testCase.readMessages(2);
            testCase.verifyTrue(messages{1}.valid);
            testCase.verifyFalse(messages{1}.correct);
            testCase.verifyEqual(messages{1}.score, 0);
            testCase.verifyFalse(messages{2}.completed);
            testCase.verifyEqual(messages{2}.stars, 0);
        end

        function twelveValidatedPlacementsCompleteAndAwardFirstStar(testCase)
            root = fileparts(fileparts(mfilename('fullpath')));
            dataDirectory = fullfile(root, '..', 'Shared_Data', 'ActivityData');
            registry = cmr.activities.Registry(dataDirectory);
            definition = registry.get("COLOR");
            ids = definition.objectIds();

            for k = 1:11
                requestId = "req_ok_" + string(k);
                testCase.place(requestId, ids(k), definition.targetFor(ids(k)));
                messages = testCase.readMessages(2);
                testCase.verifyEqual(string(messages{1}.type), "PLACE_RES");
                testCase.verifyTrue(messages{1}.correct);
                testCase.verifyEqual(messages{1}.score, k);
                testCase.verifyFalse(messages{2}.completed);
                testCase.verifyEqual(messages{2}.stars, 0);
            end

            requestId = "req_ok_12";
            testCase.place(requestId, ids(12), definition.targetFor(ids(12)));
            messages = testCase.readMessages(3);

            testCase.verifyEqual(string(messages{1}.type), "PLACE_RES");
            testCase.verifyEqual(messages{1}.score, 12);

            result = messages{2};
            testCase.verifyEqual(string(result.type), "ACTIVITY_RESULT");
            testCase.verifyEqual(string(result.activity), "COLOR");
            testCase.verifyTrue(result.completed);
            testCase.verifyEqual(result.stars, 1);

            progress = messages{3};
            testCase.verifyEqual(string(progress.type), "GAME_PROGRESS");
            testCase.verifyEqual(progress.stars, 1);
            testCase.verifyEqual(string(progress.completedActivities), "COLOR");
            testCase.verifyFalse(progress.gameCompleted);

            % A duplicate request id is ignored by the server.
            testCase.place("req_ok_12", ids(12), definition.targetFor(ids(12)));
            testCase.assertNoMessages(0.5);
        end
    end

    methods (Access = private)
        function place(testCase, requestId, objectId, targetId)
            message = struct( ...
                'type', "PLACE_REQ", ...
                'requestId', string(requestId), ...
                'activity', "COLOR", ...
                'objectId', string(objectId), ...
                'targetId', string(targetId), ...
                'position', struct('x', 0.5, 'y', 0.5));
            writeline(testCase.Client, string(jsonencode(message)));
        end

        function messages = readMessages(testCase, count)
            deadline = tic;
            while numel(testCase.Pending) < count && toc(deadline) < 5
                testCase.pump();
                pause(0.02);
            end
            testCase.verifyGreaterThanOrEqual(numel(testCase.Pending), count, ...
                "Timed out waiting for " + string(count) + " MATLAB replies.");
            take = min(count, numel(testCase.Pending));
            messages = testCase.Pending(1:take);
            testCase.Pending(1:take) = [];
        end

        function assertNoMessages(testCase, waitSeconds)
            deadline = tic;
            while toc(deadline) < waitSeconds
                testCase.pump();
                pause(0.02);
            end
            testCase.verifyEmpty(testCase.Pending);
        end

        function pump(testCase)
            available = testCase.Client.NumBytesAvailable;
            if available <= 0
                return
            end
            data = read(testCase.Client, available, "uint8");
            testCase.RxBuffer = [testCase.RxBuffer, reshape(data, 1, [])];
            while true
                index = find(testCase.RxBuffer == uint8(10), 1, 'first');
                if isempty(index)
                    break
                end
                lineBytes = testCase.RxBuffer(1:index - 1);
                testCase.RxBuffer(1:index) = [];
                lineBytes = lineBytes(lineBytes ~= uint8(13));
                if ~isempty(lineBytes)
                    text = string(native2unicode(lineBytes, "UTF-8"));
                    testCase.Pending{end + 1} = jsondecode(text); %#ok<AGROW>
                end
            end
        end
    end
end
