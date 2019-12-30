using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ProcessV2;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace VsTestRunner.Core
{
    public class Test
    {
        private readonly ILogger _logger;
        private readonly TestOutputLogger _outputWriter;

        public string AssemblyName { get; }

        public int Id { get; }

        public string Framework { get; }

        public Test(ILogger logger, TestOutputLogger outputWriter, int id, FileInfo assembly)
        {
            _logger = logger;
            _outputWriter = outputWriter;
            Id = id;
            Assembly = assembly;
            AssemblyName = assembly.FileNameWithoutExtension();
            RunDirectory = Assembly.Directory.FullName;

            Framework = GetFramework(Assembly.Directory, AssemblyName);
        }

        private FileInfo Assembly { get; }
        public string RunDirectory { get; }

        public async Task<TestResult> Execute(EnvironmentOptions environmentOptions, TestOptions testOptions, CancellationToken cancellationToken)
        {
            string outputFileName;
            string resultsFile;
            string resultsDirectory;
            if (string.IsNullOrEmpty(environmentOptions.BaseResultsDirectory))
            {
                resultsDirectory = ".";
                outputFileName = AssemblyName;
                resultsFile = Path.Combine(RunDirectory, $"{outputFileName}.trx");
            }
            else
            {
                resultsDirectory = environmentOptions.WriteToBaseResultsDirectory ?
                    Path.GetFullPath(environmentOptions.BaseResultsDirectory) :
                    Path.GetFullPath(Path.Combine(environmentOptions.BaseResultsDirectory, Framework, AssemblyName));

                if (!Directory.Exists(resultsDirectory))
                {
                    _logger.LogDebug("Creating results directory {resultsDirectory}.", resultsDirectory);
                    Directory.CreateDirectory(resultsDirectory);
                }


                outputFileName = environmentOptions.WriteToBaseResultsDirectory ?
                    $"{Framework}-{AssemblyName}" :
                    AssemblyName;

                resultsFile = Path.Combine(resultsDirectory, $"{outputFileName}.trx");
            }

            var stopwatch = Stopwatch.StartNew();
            try
            {
                if (File.Exists(resultsFile))
                {
                    _logger.LogDebug("Removing existing test result file '{trxFile}'.", resultsFile);
                    // Prevent false positives by deleting the previous file.
                    File.Delete(resultsFile);
                }

                var processRunner = GetProcessRunner(environmentOptions, testOptions, resultsDirectory, outputFileName);

                ProcessResult vsTestProcessResult;
                try
                {
                    vsTestProcessResult = await processRunner.Run(cancellationToken);

                    _logger.LogInformation("Tests completed. ExitCode: {exitCode}", vsTestProcessResult.ExitCode);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogError("Test execution cancelled. vstest output follows:\n{output}", _outputWriter.GetOutputLines());

                    return new TestResult(this, -1, stopwatch.Elapsed, VsTestResultProcessor.GetResultsForFailure(resultsFile, VsTestFailureReason.Cancelled, AssemblyName, "Test execution cancelled."));
                }

                var vsTestResult = new VsTestResult(AssemblyName, vsTestProcessResult.ExitCode, resultsFile, testOptions.TreatZeroTestsAsSuccess);
                _logger.LogDebug("TestResultFile {resultsFile} Result={result} Message={message}'.", resultsFile, vsTestResult.Result, vsTestResult.Message);

                var testResult = new TestResult(this, vsTestProcessResult.ExitCode, vsTestProcessResult.Duration, vsTestResult);

                if (testResult.Result == Result.Success)
                {
                    _logger.LogInformation("Test execution succeeded in {duration}", testResult.Duration);
                }
                else
                {
                    _logger.LogError("Test execution failed in {duration}. vstest output follows:\n{output}", testResult.Duration, _outputWriter.GetOutputLines());
                }

                return testResult;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Exception during test execution.");
                return new TestResult(this, -1, stopwatch.Elapsed, VsTestResultProcessor.GetResultsForFailure(resultsFile, VsTestFailureReason.Error, AssemblyName, "Test execution failed."));
            }
        }

        private ProcessRunner GetProcessRunner(EnvironmentOptions environmentOptions, TestOptions testOptions, string resultsDirectory, string outputFileName)
        {
            if (environmentOptions.RunTestInDocker)
            {
                string commandLineArgs = string.Empty;
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    commandLineArgs += "--debug ";
                }

                commandLineArgs += $"run -e DOTNET_CLI_HOME=/workdir -e DOTNET_NOLOGO=1 -e DOTNET_CLI_TELEMETRY_OPTOUT=1 --rm --workdir /workdir -v \"{RunDirectory}:/workdir\"";

                foreach (var mount in environmentOptions.AdditionalMounts)
                {
                    commandLineArgs += $" -v \"{Path.GetFullPath(mount.LocalDirectory)}:{mount.MountDirectory}\"";
                }

                if (resultsDirectory != ".")
                {
                    commandLineArgs += $" -v \"{Path.GetFullPath(resultsDirectory)}:/resultsdir\"";
                    resultsDirectory = "/resultsdir";
                }

                string vsTestCommandLineArgs = $"vstest {Assembly.Name} {VsTestCommandLineHelper.GetVsTestCommandLineArguments(outputFileName, testOptions, resultsDirectory, Assembly)}";

                foreach (var environmentVariable in environmentOptions.EnvironmentVariables)
                {
                    commandLineArgs += $" -e \"{environmentVariable.Key}={environmentVariable.Value}\"";
                }

                if (!environmentOptions.TryGetDockerImage(Framework, out string dockerImage))
                {
                    throw new ApplicationException($"Unable to find suitable docker image to run test for '{AssemblyName}' and TargetFramework='{Framework}'");
                }

                commandLineArgs += $" --entrypoint dotnet {dockerImage} {vsTestCommandLineArgs}\"";

                _outputWriter.WriteOutput($">>> Executing: docker {commandLineArgs}\n>>> Working dir: {RunDirectory}\n");
                return new ProcessRunner("docker", commandLineArgs, workingDirectory: RunDirectory, outputWriter: _outputWriter);
            }
            else
            {
                string commandLineArgs = $"vstest {Assembly.Name} {VsTestCommandLineHelper.GetVsTestCommandLineArguments(outputFileName, testOptions, resultsDirectory, Assembly)}";

                _outputWriter.WriteOutput($">>> Executing: dotnet {commandLineArgs}\n>>> Working dir: {RunDirectory}\n");
                return new ProcessRunner("dotnet", commandLineArgs, workingDirectory: RunDirectory, outputWriter: _outputWriter, clearEnvironmentVariables: environmentOptions.ClearEnvironmentVariables,
                    environmentVariables: environmentOptions.EnvironmentVariables);
            }
        }

        public string GetFramework(DirectoryInfo testFolder, string assemblyName)
        {
            if (testFolder.Name.StartsWith("net"))
            {
                return testFolder.Name;
            }

            if (testFolder.Name == "publish")
            {
                if (testFolder.Parent.Name.StartsWith("net"))
                {
                    return testFolder.Parent.Name;
                }
            }

            FileInfo runtimeFile = testFolder.GetFiles($"{assemblyName}.runtimeconfig.json").SingleOrDefault();
            if (runtimeFile != null)
            {
                _logger.LogInformation("TargetFramework cannot be determined from folder name. Checking runtimeconfig.");

                JToken jToken = JToken.Parse(File.ReadAllText(runtimeFile.FullName));
                string targetFramework = jToken["runtimeOptions"]?["tfm"]?.Value<string>();

                if (string.IsNullOrEmpty(targetFramework))
                {
                    targetFramework = "netcoreapp2.1";
                    _logger.LogWarning("Failed to find target framework from runtimeconfig.json. Defaulting to '{targetFramework}'", targetFramework);
                }
                else
                {
                    _logger.LogInformation("'{assemblyName}.runtimeconfig.json' TargetFramework = '{targetFramework}'.", assemblyName, targetFramework);
                }

                return targetFramework;
            }
            else
            {
                _logger.LogInformation("No runtimeconfig.json present - defaulting TargetFramework to 'net461'");
                return "net461";
            }
        }
    }
}
