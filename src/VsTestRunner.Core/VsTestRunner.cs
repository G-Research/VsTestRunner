using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ProcessV2;
using VsTestRunner.Core.Interfaces;

namespace VsTestRunner.Core
{
    public class VsTestRunner
    {
        private readonly ILogger _logger;
        private readonly ITestFactory _testFactory;

        public TestOptions TestOptions { get; }

        public EnvironmentOptions EnvironmentOptions { get; }

        public VsTestRunner(ILogger<VsTestRunner> logger, ITestFactory testFactory, TestOptions testOptions, EnvironmentOptions environmentOptions)
        {
            _logger = logger;
            _testFactory = testFactory;
            TestOptions = testOptions;
            EnvironmentOptions = environmentOptions;
        }

        public async Task<IList<TestResult>> RunTests(ILogger outputLogger, IList<FileInfo> testAssemblies, int maxDegreeOfParallelism, CancellationToken cancellationToken)
        {
            SemaphoreSlim concurrencyLimiter = new SemaphoreSlim(maxDegreeOfParallelism);

            IOutputWriter outputWriter = new OutputLogger(outputLogger);
            ConcurrentBag<TestResult> testResults = new ConcurrentBag<TestResult>();
            List<Task> tests = new List<Task>();

            var periodicLoggingTask = Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken: cancellationToken);

                        _logger.LogInformation("Summary: Total: {total} Completed: {completed} Executing: {executing}", testAssemblies.Count, testResults.Count, maxDegreeOfParallelism - concurrencyLimiter.CurrentCount);
                    }
                    catch (OperationCanceledException)
                    {
                        // Nothing to do... 
                    }
                }
            });

            int testId = 0;
            foreach (var testAssembly in testAssemblies)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogError("Cancellation request. Stopping further test execution.");
                    break;
                }

                try
                {
                    await concurrencyLimiter.WaitAsync(cancellationToken);

                    Test test = _testFactory.Create(testId++, testAssembly);

                    var testTask = Task.Run(async () =>
                    {
                        TestResult testResult = null;
                        try
                        {
                            _logger.LogInformation("Executing tests in '{testAssembly}'...", testAssembly);
                            testResult = await test.Execute(EnvironmentOptions, TestOptions, cancellationToken);
                            _logger.LogDebug("Completed executing '{testAssembly}'. Add to results.", testAssembly);
                            testResults.Add(testResult);
                        }
                        finally
                        {
                            _logger.LogDebug("Releasing semaphore. Current count = {currentCount}", concurrencyLimiter.CurrentCount);
                            concurrencyLimiter.Release();
                        }
                    });

                    tests.Add(testTask);

                    _logger.LogInformation("Running {count} jobs.", maxDegreeOfParallelism - concurrencyLimiter.CurrentCount);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogError("Task execution has been cancelled.");
                }
            }

            Task.WaitAll(tests.ToArray());

            return testResults.ToArray();
        }
    }
}
