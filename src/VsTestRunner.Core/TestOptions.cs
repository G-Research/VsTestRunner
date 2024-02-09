using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using VsTestRunner.Core.Interfaces;

namespace VsTestRunner.Core
{
    public enum CodeCoverageCollector
    {
        None,
        Coverlet,
        VisualStudio
    }

    public class TestOptions
    {
        public TestOptions(ICommandLineTestOptions commandLineOptions)
        {
            Platform = commandLineOptions.TestPlatform;
            Framework = commandLineOptions.TestFramework;
            ExcludeCategories = commandLineOptions.ExcludeCategories;
            IncludeCategories = commandLineOptions.IncludeCategories;
            Filter = commandLineOptions.Filter;

            if (commandLineOptions.Tests.Any() &&
                (ExcludeCategories.Any() || IncludeCategories.Any() || !string.IsNullOrEmpty(Filter)))
            {
                throw new ArgumentException(nameof(commandLineOptions.Tests), "Test and Filter options are mutually exclusive. Please rerun with single option selected.");
            }

            Tests = commandLineOptions.Tests;
            TreatZeroTestsAsSuccess = commandLineOptions.TreatZeroTestsAsSuccess;
            Blame = commandLineOptions.Blame;
            Diagnostics = commandLineOptions.Diagnostics;
            CodeCoverageCollector = commandLineOptions.CodeCoverageCollector;
            VsTestRuntimeSettings = new VsTestRuntimeSettings(TimeSpan.FromMinutes(commandLineOptions.TestSessionTimeout), TimeSpan.FromSeconds(commandLineOptions.TestTimeout), runSettingsFile: commandLineOptions.RunSettings, TraceLevel.Off);
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public VsTestPlatform Platform { get; }
        public string Framework { get; }
        /// <summary>
        /// Not currently supported by VsTest despite documentation indicating otherwise.
        /// </summary>
        public bool Blame { get; }
        public CodeCoverageCollector CodeCoverageCollector { get; }
        public bool Diagnostics { get; }
        public string Filter { get; }
        public IList<string> IncludeCategories { get; }
        public IList<string> ExcludeCategories { get; }

        public IList<string> Tests { get; }
        public bool TreatZeroTestsAsSuccess { get; }
        public VsTestRuntimeSettings VsTestRuntimeSettings { get; }
    }
}
