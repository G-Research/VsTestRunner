using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;

namespace VsTestRunner.Nuke
{
    public enum CodeCoverageCollector
    {
        None,
        Coverlet,
        VisualStudio
    }

    public static class ArgumentBuilderExtensions
    {
        public static Arguments AddArgumentIfNotNull<T>(this Arguments arguments, string argumentName, T? value)
        {
            if (value != null)
            {
                arguments.Add(argumentName, value);
            }

            return arguments;
        }

        public static Arguments AddArgumentIfNotNullOrDefault<T>(this Arguments arguments, string argumentName, T? value, T defaultValue) where T : IEquatable<T>
        {
            if (value != null && (defaultValue == null || !value.Equals(defaultValue)))
            {
                arguments.Add(argumentName);
                arguments.Add(value.ToString());
            }

            return arguments;
        }

        public static Arguments AddIfNotEmptyOrNull(this Arguments arguments, string argumentName, IEnumerable<string>? value, char separator = ' ')
        {
            if (value != null && value.Any())
            {
                arguments.Add(argumentName);
                arguments.Add(string.Join(separator, value)); // With some escaping??
            }

            return arguments;
        }
    }

    [Serializable]
    public class VsTestRunnerSettings : ToolSettings
    {
        //
        // Summary:
        //     Path to the DotNet executable.
        public override string ProcessToolPath => base.ProcessToolPath ?? DotNetTasks.DotNetPath;

        public VsTestRunnerSettings()
        {
            VsTestRunnerPath = ToolPathResolver.GetPackageExecutable("vstestrunner", "VsTestRunner.dll",
#if NET7_0
                framework: "net7.0"
#elif NET6_0
                framework: "net6.0"
#elif NET5_0
                framework: "net5.0"
#endif
    ) ?? "vstest-runner";
        }

        public VsTestRunnerSettings(string vstestRunnerPath)
        {
            VsTestRunnerPath = vstestRunnerPath;
        }

        public string VsTestRunnerPath { get; }


        public override Action<OutputType, string> ProcessCustomLogger => DotNetTasks.DotNetLogger;

        public IEnumerable<string>? TestAssemblies { get; set; }

        public string? TestAssembliesFile { get; set; }

        public int MaxConcurrentTests { get; set; }

        public IEnumerable<string>? DockerImage { get; set; }

        public string? Filter { get; set; }

        public IList<string>? IncludeCategories { get; set; }

        public IList<string>? ExcludeCategories { get; set; }

        public double Timeout { get; set; }

        public double TestSessionTimeout { get; set; }

        public double TestTimeout { get; set; }

        public CodeCoverageCollector CodeCoverageCollector { get; set; }

        public bool NoMetrics { get; set; }

        public string? MetricsFile { get; set; }

        public string? RunSettings { get; set; }

        public bool TreatZeroTestsAsSuccess { get; set; }

        public string? ResultsDirectory { get; set; }

        public bool WriteToBaseResultsDirectory { get; set; }

        protected override Arguments ConfigureProcessArguments(Arguments arguments)
        {
            arguments.Add(VsTestRunnerPath).AddIfNotEmptyOrNull("--test-assemblies", TestAssemblies)
                                .AddArgumentIfNotNullOrDefault("--test-assemblies-file", TestAssembliesFile, "")
                                .AddArgumentIfNotNullOrDefault("--max-concurrent-tests", MaxConcurrentTests, 0)
                                .AddIfNotEmptyOrNull("--docker-image", DockerImage)
                                .AddIfNotEmptyOrNull("--include-categories", IncludeCategories)
                                .AddIfNotEmptyOrNull("--exclude-categories", ExcludeCategories)
                                .AddArgumentIfNotNullOrDefault("--filter", Filter, "")
                                .AddArgumentIfNotNullOrDefault("--timeout", Timeout, 0)
                                .AddArgumentIfNotNullOrDefault("--session-timeout", TestSessionTimeout, 0)
                                .AddArgumentIfNotNullOrDefault("--test-timeout", TestTimeout, 0)
                                .AddArgumentIfNotNullOrDefault("--results-directory", ResultsDirectory, "")
                                .AddArgumentIfNotNullOrDefault("--no-metrics", NoMetrics, false)
                                .AddArgumentIfNotNullOrDefault("--metrics-file", MetricsFile, "")
                                .AddArgumentIfNotNullOrDefault("--write-to-base-results-directory", WriteToBaseResultsDirectory, false)
                                .AddArgumentIfNotNullOrDefault("--treat-zero-tests-as-success", TreatZeroTestsAsSuccess, false);

            return base.ConfigureProcessArguments(arguments);
        }
    }

    public class VsTestRunnerTasks
    {
        public static IReadOnlyCollection<Output> VsTestRunner(VsTestRunnerSettings vsTestRunnerSettings)
        {
            using var process = ProcessTasks.StartProcess(vsTestRunnerSettings);
            process.AssertZeroExitCode();
            return process.Output;
        }

    }
}
