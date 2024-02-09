using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace VsTestRunner.Core
{
    public enum TraceLevel
    {
        Off,
        Error,
        Warning,
        Info,
        Verbose,
        Debug
    }

    public class VsTestRuntimeSettings
    {
        public const string Extension = ".runsettings";
        public TimeSpan TestSessionTimeout { get; }
        public TimeSpan TestTimeout { get; }
        [JsonConverter(typeof(StringEnumConverter))]
        public TraceLevel TraceLevel { get; }

        // Setting required by ArmadaTestRunner (path is different locally and on remote machine)
        public string RunSettings { get; set; }

        public VsTestRuntimeSettings(TimeSpan testSessionTimeout, TimeSpan testTimeout, string runSettingsFile = null, TraceLevel traceLevel = TraceLevel.Off)
        {
            RunSettings = runSettingsFile;
            TestSessionTimeout = testSessionTimeout;
            TestTimeout = testTimeout;
            TraceLevel = traceLevel;
        }

        public string GetArgument(FileInfo assemblyFile)
        {
            string argString = string.Empty;
            string runtimeSettings = null;

            if (TestSessionTimeout != TimeSpan.Zero)
            {
                runtimeSettings += $" RunConfiguration.TestSessionTimeout={(int)TestSessionTimeout.TotalMilliseconds}";
            }

            if (TestTimeout != TimeSpan.Zero)
            {
                // This only works in .NET Framework due to lack of Thread.Abort support in .net core
                runtimeSettings += $" NUnit.DefaultTimeout={(int)TestTimeout.TotalMilliseconds}";
            }

            if (!string.IsNullOrWhiteSpace(RunSettings))
            {
                argString += $" --settings:{Path.GetRelativePath(assemblyFile.DirectoryName, RunSettings)}";
            }

            if (!string.IsNullOrEmpty(runtimeSettings))
            {
                argString += $" -- {runtimeSettings}";
            }

            return argString;
        }
    }
}
