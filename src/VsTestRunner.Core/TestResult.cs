using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using VsTestRunner.Core.Interfaces;

namespace VsTestRunner.Core
{
    public class TestResult : ITestResult
    {
        public string AssemblyName { get; }

        public string Framework { get; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Result Result { get; }

        public int ExitCode { get; }

        public VsTestResult VsTestResult { get; }

        public string RunDirectory { get; }

        public TimeSpan Duration { get; }

        public TestResult(Test test, int exitCode, TimeSpan duration, VsTestResult testResult)
        {
            AssemblyName = test.AssemblyName;
            Framework = test.Framework;
            Duration = duration;
            ExitCode = exitCode;
            RunDirectory = test.RunDirectory;

            VsTestResult = testResult;

            if (ExitCode == 0 && VsTestResult.Result == Result.Success)
            {
                Result = Result.Success;
            }
            else
            {
                Result = Result.Failure;
            }
        }
    }
}
