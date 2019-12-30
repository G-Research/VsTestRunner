using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using VsTestRunner.Core.Interfaces;

namespace VsTestRunner.Core
{
    public class TestResultPrinter
    {
        private ILogger<TestResultPrinter> _logger;

        public TestResultPrinter(ILogger<TestResultPrinter> logger)
        {
            _logger = logger;
        }

        public bool ProcessResults(IReadOnlyCollection<ITestResult> results)
        {
            _logger.LogInformation(FormatResultsOutput(results));

            if (results.All(nr => nr.Result == Result.Success))
            {
                _logger.LogInformation($"All tests have completed successfully in {results.Count} test project(s).");
                return true;
            }

            var failedAssemblies = results.Count(nr => nr.Result == Result.Failure);
            _logger.LogError($"Unit test failures. Tested {results.Count} assemblies and {failedAssemblies} failed.");
            return false;
        }

        private static string FormatResultsOutput(IReadOnlyCollection<ITestResult> results)
        {
            var passedResults = results.Where(r => r.VsTestResult.Result == Result.Success).ToList();
            var unknownResults = results.Where(r => !r.VsTestResult.ResultsParsed).ToList();
            var failedResults = results.Where(r => r.VsTestResult.ResultsParsed).ToList()
                                       .Where(r => r.VsTestResult.Result == Result.Failure).ToList();

            var outputBuilder = new StringBuilder();
            outputBuilder.AppendLine();
            outputBuilder.AppendLine("******************* TEST SUMMARY ********************");
            outputBuilder.AppendLine();
            if (unknownResults.Any() || failedResults.Any())
            {
                outputBuilder.AppendLine($" TEST RUN FAILED:");
                outputBuilder.AppendLine($" - ASSEMBLIES TESTED: {results.Count}");
                outputBuilder.AppendLine($" - ASSEMBLIES PASSED: {passedResults.Count}");
                outputBuilder.AppendLine($" - ASSEMBLIES FAILED: {results.Count - passedResults.Count}");
            }
            else
            {
                outputBuilder.AppendLine($" TEST RUN PASSED: {results.Count} ASSEMBLIES TESTED/PASSED");
            }

            outputBuilder.AppendLine();
            outputBuilder.AppendLine($" SUMMARY OF RESULTS FROM '{results.Count(r => r.VsTestResult.ResultsParsed)}' ASSEMBLIES'");
            outputBuilder.AppendLine();

            outputBuilder.AppendLine($"TOTAL TESTS:               {results.Select(r => r.VsTestResult.Total).Sum()}");
            outputBuilder.AppendLine($"TOTAL PASSED:              {results.Select(r => r.VsTestResult.Passed).Sum()}");

            var passedButRunAborted = results.Select(r => r.VsTestResult.PassedButRunAborted).Sum();
            if (passedButRunAborted > 0)
            {
                outputBuilder.AppendLine($"TOTAL PASSEDBUTRUNABORTED: {passedButRunAborted}");
            }

            var inconclusive = results.Select(r => r.VsTestResult.Inconclusive).Sum();
            if (inconclusive > 0)
            {
                outputBuilder.AppendLine($"TOTAL INCONCLUSIVE:        {inconclusive}");
            }

            var skipped = results.Select(r => r.VsTestResult.Skipped).Sum();
            if (skipped > 0)
            {
                outputBuilder.AppendLine($"TOTAL SKIPPED:             {skipped}");
            }

            var aborted = results.Select(r => r.VsTestResult.Aborted).Sum();
            if (aborted > 0)
            {
                outputBuilder.AppendLine($"TOTAL ABORTED:             {aborted}");
            }

            outputBuilder.AppendLine($"TOTAL FAILED:              {results.Select(r => r.VsTestResult.Failed).Sum()}");
            outputBuilder.AppendLine();

            outputBuilder.AppendLine("*************** TEST ASSEMBLY SUMMARY ***************");
            PrintSectionSummary(outputBuilder, "PASSED:", passedResults.Where(r => r.VsTestResult.HasTests).OrderBy(r => r.VsTestResult.Duration).ToList());
            PrintSectionSummary(outputBuilder, "NO TESTS:", passedResults.Where(r => !r.VsTestResult.HasTests).ToList());
            PrintSectionSummary(outputBuilder, "FAILED:", failedResults);
            PrintSectionSummary(outputBuilder, "UNKNOWN:", unknownResults);
            outputBuilder.AppendLine();
            outputBuilder.AppendLine("*****************************************************");

            return outputBuilder.ToString();
        }

        private static void PrintSectionSummary(StringBuilder builder, string sectionHeader, IReadOnlyCollection<ITestResult> testResultsForSection)
        {
            if (testResultsForSection.Any())
            {
                builder.AppendLine();
                builder.AppendLine(sectionHeader);
            }
            foreach (var testResultsForFramework in testResultsForSection.GroupBy(r => r.Framework))
            {
                builder.AppendLine($"({testResultsForFramework.Key})");
                foreach (var testResult in testResultsForFramework)
                {
                    builder.AppendLine(GetSummaryLine(testResult));
                }
            }
        }

        private static string GetSummaryLine(ITestResult testResult)
        {
            if (testResult.VsTestResult.ResultsParsed)
            {
                if (testResult.VsTestResult.HasTests)
                {
                    if (testResult.Result == Result.Success)
                    {
                        return $" * {testResult.AssemblyName,-55}: {$"Total={testResult.VsTestResult.Total}",-11} {$"Passed={testResult.VsTestResult.Passed}",-12} {$"Duration=[{testResult.VsTestResult.Duration:hh\\:mm\\:ss}]",19}";
                    }
                    else
                    {
                        // Interest in the number of failures more than the number that have passed
                        return $" * {testResult.AssemblyName,-55}: {$"Total={testResult.VsTestResult.Total}",-11} {$"Failed={testResult.VsTestResult.Failed}",-12} {$"Duration=[{testResult.VsTestResult.Duration:hh\\:mm\\:ss}]",19} {$"Aborted={testResult.VsTestResult.Aborted + testResult.VsTestResult.PassedButRunAborted}",11}  {$"ExitCode={testResult.VsTestResult.ExitCode}",20} Message=[{testResult.VsTestResult.Message}]";
                    }
                }

                return $" * {testResult.AssemblyName,-55}: Zero tests found";
            }

            return $" * {testResult.AssemblyName,-55}: Failed to parse results file. {testResult.VsTestResult.Message}.";
        }
    }
}
