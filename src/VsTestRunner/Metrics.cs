using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using VsTestRunner.Core;
using VsTestRunner.Core.Interfaces;

namespace VsTestRunner
{
    public class Metrics
    {
        public int AssemblyCount { get; }
        public DateTime StartTime { get; }
        [JsonConverter(typeof(StringEnumConverter))]
        public Result Result { get; }
        public DateTime EndTime { get; }
        public TestOptions TestOptions { get; }
        public EnvironmentOptions EnvironmentOptions { get; }
        public IEnumerable<ITestResult> TestResults { get; }

        public Metrics(DateTime startTime, DateTime endTime, bool success, EnvironmentOptions environmentOptions, TestOptions options, IList<TestResult> testResults)
        {
            StartTime = startTime;
            EndTime = endTime;
            Result = success ? Result.Success : Result.Failure;
            EnvironmentOptions = environmentOptions;
            TestOptions = options;
            AssemblyCount = testResults.Count;
            TestResults = testResults;
        }

        public void Save(string fileName)
        {
            File.WriteAllText(fileName, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
    }
}
