using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Bulldog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using VsTestRunner.Core;
using VsTestRunner.Core.Interfaces;

namespace VsTestRunner
{
    public class TestRunner : ToolBase<Options>
    {
        protected override bool MonitorForTaskKill => true;

        protected override string SerilogOutputTemplate => "[{Timestamp:HH:mm:ss.fff} {Level:u3}] {SourceContext}  {Message:lj}{NewLine}{Exception}";

        protected override void ConfigureServices(IServiceCollection serviceCollection, Options options)
        {
            serviceCollection.AddSingleton(typeof(ILogger<>), typeof(LoggerEx<>));
            serviceCollection.AddTransient<Core.VsTestRunner>();
            serviceCollection.AddSingleton<TestResultPrinter>();
            serviceCollection.AddSingleton<ITestFactory, TestFactory>();
            serviceCollection.AddSingleton<ICommandLineTestOptions>(options);
            serviceCollection.AddSingleton<IEnvironmentOptions>(options);
            serviceCollection.AddSingleton<TestOptions>();
            serviceCollection.AddSingleton<EnvironmentOptions>();
        }

        private IEnumerable<FileInfo> GetTestAssemblies(Options options)
        {
            if (options.TestAssemblies != null)
            {
                foreach (var assembly in options.TestAssemblies)
                {
                    yield return new FileInfo(assembly);
                }
            }

            if (!string.IsNullOrEmpty(options.TestAssemblyFile))
            {
                using (FileStream fileStream = new FileStream(options.TestAssemblyFile, FileMode.Open, FileAccess.Read))
                using (StreamReader streamReader = new StreamReader(fileStream))
                {
                    while (!streamReader.EndOfStream)
                    {
                        var assembly = streamReader.ReadLine();

                        if (!string.IsNullOrEmpty(assembly))
                        {
                            yield return new FileInfo(assembly);
                        }
                    }
                }
            }
        }

        protected override async Task<int> Run(Options options)
        {
            options.PrintOptions();
            Log.Information($"Environment.UserName is {Environment.UserName}");
            var userNameFromKerberos = Kerberos.UserName;
            if (Environment.OSVersion.Platform == PlatformID.Unix &&
                Environment.UserName != userNameFromKerberos &&
                !string.IsNullOrEmpty(userNameFromKerberos))
            {
                Log.Warning("As you ar running on Linux please note that if the tests use 'Environment.UserName', its value will be " +
                            $"'{Environment.UserName}' - this might not not be what you want. " +
                            "There seems to be a kerberos ticket for " +
                            $"'{userNameFromKerberos}' - if you need to use this user instead, you can use 'KerberosUtils' library " +
                            "and replace the 'Environment.UserName' call  with 'Kerberos.UserName'");
            }

            DateTime startTime = DateTime.Now;

            var testAssemblies = GetTestAssemblies(options).ToList();

            if (testAssemblies.Count == 0)
            {
                Logger.LogError("No assemblies found for testing.");
                return ExitCode.NoTestAssemblies;
            }

            if (options.Timeout > 0)
            {
                var timeout = TimeSpan.FromMinutes(options.Timeout);
                var _ = Task.Run(() => Task.Delay(timeout, CancellationTokenSource.Token).ContinueWith(
                    delayTask =>
                    {
                        if (!delayTask.IsCanceled)
                        {
                            Log.Error("Timeout {timeout} exceeded. Cancelling any outstanding test executions.", timeout);
                            CancellationTokenSource.Cancel();
                        }
                    }));

                Log.Information("Timeout enabled. All jobs will be cancelled after {timeout}", timeout);
            }

            int maxConcurrency = options.MaxDegreeOfParallelism > 0 ? (int)options.MaxDegreeOfParallelism : Environment.ProcessorCount;

            var testRunner = ServiceProvider.GetService<Core.VsTestRunner>();
            var testResults = await testRunner.RunTests(Logger, testAssemblies, maxConcurrency, CancellationTokenSource.Token);

            // Cancel anything outstanding...
            CancellationTokenSource.Cancel();

            // check that test Count == testResult count
            // then check that all the test results are successes
            bool success = testAssemblies.Count == testResults.Count &&
                testResults.All(r => r.Result == Result.Success);

            ServiceProvider.GetService<TestResultPrinter>().ProcessResults(testResults.ToList());

            DateTime endTime = DateTime.Now;

            Log.Information("Total time for testing: {duration}", endTime - startTime);

            if (!options.NoMetrics)
            {
                var metrics = new Metrics(startTime, endTime, success, testRunner.EnvironmentOptions, testRunner.TestOptions, testResults.ToList());

                string metricsDirectory = options.ResultsDirectory ?? Environment.CurrentDirectory;

                string fileName;
                if (string.IsNullOrEmpty(options.MetricsFile))
                {
                    fileName = Path.Combine(metricsDirectory, $"{Guid.NewGuid()}.metrics");
                }
                else
                {
                    if (Path.IsPathRooted(options.MetricsFile) || options.MetricsFile.StartsWith("."))
                    {
                        fileName = options.MetricsFile;
                    }
                    else
                    {
                        fileName = Path.Combine(metricsDirectory, options.MetricsFile);
                    }
                }

                Log.Information("Writing metric information to {fileName}", fileName);
                metrics.Save(fileName);
            }

            if (!success)
            {
                Log.Error($"Returning {ExitCode.FailedTests}.");
                return ExitCode.FailedTests;
            }

            Log.Information($"Returning {ExitCode.Success}.");
            return ExitCode.Success;
        }
    }
}
