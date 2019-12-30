using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace VsTestRunner.Core
{
    public enum VsTestFailureReason
    {
        Cancelled,
        Error,
        NotRun,
        NoResultsFile
    }

    /// <summary>
    ///    The results from testing a single assembly.
    /// </summary>
    public class VsTestResult
    {
        public const int FailedExitCode = 1;
        public const int CancelledExitCode = -999;
        public const int SuccessExitCode = 0;

        public string AssemblyName { get; }

        /// <summary>
        /// Must always have an exit-code.
        /// </summary>
        public int ExitCode { get; }
        public bool ResultsParsed { get; }

        public int Total { get; private set; }
        public int Passed { get; private set; }
        public int Failed { get; private set; }
        public int Skipped { get; private set; }
        public int Inconclusive { get; private set; }
        public int Aborted { get; private set; }
        public int PassedButRunAborted { get; private set; }
        public string Message { get; private set; }
        public DateTime StartTime { get; private set; }
        public DateTime FinishTime { get; private set; }
        public TimeSpan Duration => FinishTime - StartTime;

        public string ResultsFile { get; private set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Result Result { get; }

        public bool HasTests => ResultsParsed && Total > 0;

        public VsTestResult(string assemblyName, int exitCode, string resultsFile, bool treatZeroTestsAsSuccess)
        {
            AssemblyName = assemblyName;
            ExitCode = exitCode;
            ResultsFile = resultsFile;
            Result = Result.Failure;

            if (resultsFile == null || !File.Exists(resultsFile))
            {
                Message = "No results file.";
                Failed = 1;
                return;
            }

            try
            {
                XDocument document;
                using (var reader = new StreamReader(resultsFile))
                {
                    if (reader.EndOfStream)
                    {
                        Message = "Empty results file. Please check logs. Either tests have failed to execute or there were no tests to execute.";
                    }

                    document = XDocument.Load(reader);
                }

                var resultsSummary = document.Root.Descendants().SingleOrDefault(e => e.Name.LocalName == "ResultSummary");
                var times = document.Root.Descendants().SingleOrDefault(e => e.Name.LocalName == "Times");

                if (resultsSummary == null || times == null)
                {
                    Message = "Failed to find required document elements.";
                }

                var counters = resultsSummary.Descendants().Single(e => e.Name.LocalName == "Counters");

                var text = resultsSummary.Descendants().FirstOrDefault(e => e.Name.LocalName == "Text")?.Value;

                StartTime = DateTime.Parse(times.Attribute("start").Value);
                FinishTime = DateTime.Parse(times.Attribute("finish").Value);
                Total = int.Parse(counters.Attribute("total").Value);
                Passed = int.Parse(counters.Attribute("passed").Value);
                Failed = int.Parse(counters.Attribute("failed").Value) + int.Parse(counters.Attribute("error").Value);
                Inconclusive = int.Parse(counters.Attribute("inconclusive").Value);
                Skipped = int.Parse(counters.Attribute("notExecuted").Value);
                Aborted = int.Parse(counters.Attribute("aborted").Value);
                PassedButRunAborted = int.Parse(counters.Attribute("passedButRunAborted").Value);
                ResultsParsed = true;
                Message = text;
            }
            catch (Exception exception)
            {
                Message = $"Parsing trx file failed: {exception.Message}";
            }

            if (ExitCode == SuccessExitCode
                && ResultsParsed
                && Failed == 0
                && Inconclusive == 0
                && Aborted == 0
                && PassedButRunAborted == 0
                && (Passed > 0 || treatZeroTestsAsSuccess))
            {
                Result = Result.Success;
            }
        }
    }
}
