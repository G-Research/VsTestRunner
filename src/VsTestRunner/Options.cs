using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Bulldog;
using CommandLine;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Serilog;
using VsTestRunner.Core;
using VsTestRunner.Core.Interfaces;

namespace VsTestRunner
{
    public class Options : SettableOptions, ICommandLineTestOptions, IEnvironmentOptions
    {
        private string _settingsFile;

        [Option("settings-file", HelpText = "Settings file")]
        public string SettingsFile
        {
            get => _settingsFile;
            set
            {
                _settingsFile = value;
                SettableOptions options;
                using (var stream = File.OpenRead(_settingsFile))
                {
                    options = JsonSerializer.Create().Deserialize<SettableOptions>(new JsonTextReader(new StreamReader(stream)));
                }

                foreach (PropertyInfo property in typeof(SettableOptions).GetProperties().Where(p => p.CanWrite))
                {
                    var propertyValue = property.GetValue(options, null);

                    if (propertyValue != null)
                    {
                        property.SetValue(this, property.GetValue(options, null), null);
                    }
                }
            }
        }

        public void PrintOptions()
        {
            Log.Information($@"Command line options:
 * TestAssembliesCount    : {TestAssemblies.Count()}
 * TestAssembliesFile     : {TestAssemblyFile}
 - Tests                  : {String.Join(',', Tests)}
 = Filter                 : {Filter}
 = IncludeCategories      : {String.Join(',', IncludeCategories)}
 = ExcludeCategories      : {String.Join(',', ExcludeCategories)}
 * MaxConcurrency         : {MaxDegreeOfParallelism}
 * EnvironmentVariables   : {String.Join(',', EnvironmentVariables)}
 * Timeout                : {(Timeout == 0 ? "Infinite" : $"{Timeout:G}min")}
 * SessionTimeout         : {(TestSessionTimeout == 0 ? "Infinite" : $"{TestSessionTimeout:G}min")}
 * TestTimeout            : {(TestTimeout == 0 ? "Infinite" : $"{TestTimeout:G}s")}
 * Diagnostics            : {Diagnostics}
 * NoMetrics              : {NoMetrics}
 * MetricsFile            : {(NoMetrics ? "" : MetricsFile)}
 * CodeCoverageCollector  : {CodeCoverageCollector}
 * ResultsDirectory       : {ResultsDirectory}
 * Platform               : {TestPlatform}
 * Framework              : {TestFramework}
 * DockerImage            : {String.Join(' ', DockerImage)}
 * RunSettings            : {RunSettings}");
        }
    }

    public class SettableOptions : OptionsBase
    {
        public SettableOptions()
        {
            MaxDegreeOfParallelism = (uint)Environment.ProcessorCount;
            ResultsDirectory = Environment.CurrentDirectory;
            MetricsFile = $"{Guid.NewGuid()}.metrics";
        }

        [Option("test-assemblies", HelpText = "List of test-assemblies to run")]
        [JsonProperty("test-assemblies")]
        public IEnumerable<string> TestAssemblies
        {
            get { return _testAssemblies; }
            set
            {
                if (value.Count() == 1)
                {
                    _testAssemblies = value.First().Split(',');
                }
                else
                {
                    _testAssemblies = value;
                }
            }
        }

        [JsonIgnore]
        private IEnumerable<string> _testAssemblies = new string[0];

        [Option("test-assemblies-file", HelpText = "File containing the test assemblies to run tests for.")]
        [JsonProperty("test-assemblies-file")]
        public string TestAssemblyFile { get; set; }

        [Option("max-concurrent-tests", HelpText = "Maximum number of concurrent tests to run.")]
        [JsonProperty("max-concurrent-tests")]
        public uint MaxDegreeOfParallelism { get; set; }

        [Option("clear-env-vars", SetName = "Local", HelpText = "Indicates whether to clear all environment variables prior to test execution.")]
        [JsonProperty("clear-env-vars")]
        public bool RemoveAllEnvironmentVariables { get; set; }

        [Option("environment-variables", HelpText = "List of environment variables to set for test assemblies, delimited by space of comma, e.g. 'VAR1=val1,VAR2=val2' or 'VAR1=val1' 'VAR2=val2'")]
        [JsonProperty("environment-variables")]
        public IEnumerable<string> EnvironmentVariables
        {
            get { return _environmentVariables; }
            set
            {
                if (value.Count() == 1)
                {
                    _environmentVariables = value.First().Split(',');
                }
                else
                {
                    _environmentVariables = value;
                }
            }
        }

        [JsonIgnore]
        private IEnumerable<string> _environmentVariables = new string[0];

        [Option("additional-mounts", SetName = "Docker", HelpText = "Space or colon delimited list of additional directories to mount into the docker image")]
        [JsonProperty("additional-mounts")]
        public IEnumerable<string> AdditionalMounts
        {
            get { return _additionalMounts; }
            set
            {
                if (value.Count() == 1)
                {
                    _additionalMounts = value.First().Split(',');
                }
                else
                {
                    _additionalMounts = value;
                }
            }
        }

        [JsonIgnore]
        private IEnumerable<string> _additionalMounts = new string[0];

        [Option("docker-image", SetName = "Docker", HelpText = "Docker SDK image name to run tests on. If specified will run the tests locally in that image.")]
        [JsonProperty("docker-image")]
        public IEnumerable<string> DockerImage { get; set; }

        [Option('i', "include-categories", HelpText = "Test Inclusion Categories", SetName = "TestFilter")]
        [JsonProperty("include-categories")]
        public IList<string> IncludeCategories
        {
            get { return _includeCategories; }
            set
            {
                if (value.Count() == 1)
                {
                    _includeCategories = value.First().Split(',');
                }
                else
                {
                    _includeCategories = value;
                }
            }
        }
        public IList<string> _includeCategories = new string[0];


        [Option('e', "exclude-categories", HelpText = "Test Exclustion Categories", SetName = "TestFilter")]
        [JsonProperty("exclude-categories")]
        public IList<string> ExcludeCategories
        {
            get { return _excludeCategories; }
            set
            {
                if (value.Count() == 1)
                {
                    _excludeCategories = value.First().Split(',');
                }
                else
                {
                    _excludeCategories = value;
                }
            }
        }
        public IList<string> _excludeCategories = new string[0];

        [Option("filter", Required = false, HelpText = "VsTest TestCaseFilter", SetName = "TestFilter")]
        [JsonProperty("filter")]
        public string Filter { get; set; }

        [Option("tests", HelpText = "Test Names to run", SetName = "TestName")]
        [JsonProperty("tests")]
        public IList<string> Tests
        {
            get { return _tests; }
            set
            {
                if (value.Count() == 1)
                {
                    _tests = value.First().Split(',');
                }
                else
                {
                    _tests = value;
                }
            }
        }
        public IList<string> _tests = new string[0];

        [Option("timeout", HelpText = "Timeout in minutes for all tests to complete.")]
        [JsonProperty("timeout")]
        public double Timeout { get; set; }

        [Option("session-timeout", HelpText = "Timeout in minutes for all tests in an assembly to complete.", Default = 0)]
        [JsonProperty("session-timeout")]
        public double TestSessionTimeout { get; set; }

        [Option("test-timeout", HelpText = "Timeout in seconds for an individual test to complete.")]
        [JsonProperty("test-timeout")]
        public double TestTimeout { get; set; }

        [Option("code-coverage-collector", HelpText = "Code coverage collector", Default = CodeCoverageCollector.None)]
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty("code-coverage-collector")]
        public CodeCoverageCollector CodeCoverageCollector { get; set; }

        [Option("diagnostics", HelpText = "Vstest diagnostics")]
        [JsonProperty("diagnostics")]
        public bool Diagnostics { get; set; }

        [Option("blame", HelpText = "VsTest blame")]
        [JsonProperty("blame")]
        public bool Blame { get; set; }

        [Option("no-metrics", HelpText = "Skip writing of execution metrics")]
        [JsonProperty("no-metrics")]
        public bool NoMetrics { get; set; }

        [Option("metrics-file", HelpText = "Specifies output file to write metrics to. If not specified new file will be created.")]
        [JsonProperty("metrics-file")]
        public string MetricsFile { get; set; }

        [Option("test-platform", HelpText = "Target platform architecture to be used for test execution")]
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty("test-platform")]
        public VsTestPlatform TestPlatform { get; set; }

        [Option("test-framework", HelpText = "Target framework to be used for test execution.")]
        [JsonProperty("test-framework")]
        public string TestFramework { get; set; }

        [Option("run-settings", HelpText = "Run settings file to be passed to vstest")]
        [JsonProperty("run-settings")]
        public string RunSettings { get; set; }

        [Option("treat-zero-tests-as-success", HelpText = "Flag to allow test assemblies with zero tests to be treated as successful")]
        [JsonProperty("treat-zero-tests-as-success")]
        public bool TreatZeroTestsAsSuccess { get; set; }

        [Option("results-directory", HelpText = "Base folder for writing test results to")]
        [JsonProperty("results-directory")]
        public string ResultsDirectory { get; set; }

        [Option('b', "write-to-base-results-directory", HelpText = "Output all test results directly to the results-directory instead of within subfolders")]
        [JsonProperty("write-to-base-results-directory")]
        public bool WriteToBaseResultsDirectory { get; set; }
    }

}
