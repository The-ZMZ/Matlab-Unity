function results = runIssue023Tests()
%RUNISSUE023TESTS Run the Issue #023 MATLAB suite and write JUnit XML.
%
%   This runner writes Deliverables/Reports/Issue023_MATLAB_TestResults.xml
%   from the actual matlab.unittest results produced by tTasteSorting. It
%   throws when any test fails so the batch-process exit code is meaningful.

    testsDirectory = fileparts(mfilename('fullpath'));
    matlabRoot = fileparts(testsDirectory);
    projectRoot = fileparts(matlabRoot);

    addpath(genpath(matlabRoot));

    suite = matlab.unittest.TestSuite.fromFile( ...
        fullfile(testsDirectory, 'tTasteSorting.m'));

    runner = matlab.unittest.TestRunner.withTextOutput( ...
        'OutputDetail', matlab.unittest.Verbosity.Detailed);

    import matlab.unittest.plugins.XMLPlugin;
    reportsDirectory = fullfile(projectRoot, 'Deliverables', 'Reports');
    if ~isfolder(reportsDirectory)
        mkdir(reportsDirectory);
    end
    xmlPath = fullfile(reportsDirectory, 'Issue023_MATLAB_TestResults.xml');
    runner.addPlugin(XMLPlugin.producingJUnitFormat(xmlPath));

    results = runner.run(suite);

    passed = sum([results.Passed]);
    failed = sum([results.Failed]);
    incomplete = sum([results.Incomplete]);
    duration = sum([results.Duration]);

    fprintf('\n=== ISSUE 023 MATLAB TEST SUMMARY ===\n');
    fprintf('Total      : %d\n', numel(results));
    fprintf('Passed     : %d\n', passed);
    fprintf('Failed     : %d\n', failed);
    fprintf('Incomplete : %d\n', incomplete);
    fprintf('Duration   : %.2f s\n', duration);
    fprintf('JUnit XML  : %s\n', xmlPath);
    fprintf('====================================\n');

    if failed > 0
        names = string({results([results.Failed]).Name});
        error('cmr:tests:issue023Failed', ...
            '%d Issue #023 test(s) failed: %s', failed, strjoin(names, ', '));
    end
end
