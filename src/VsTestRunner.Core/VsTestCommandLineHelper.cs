using System.IO;
using System.Linq;
using Serilog;

namespace VsTestRunner.Core
{
    public class VsTestCommandLineHelper
    {
        //TODO: Should this return string[]
        public static string GetVsTestCommandLineArguments(string outputFilename, TestOptions testOptions, string resultsDirectory, FileInfo assemblyFile)
        {
            string arguments = "";

            if (testOptions.Platform != VsTestPlatform.AUTO)
            {
                arguments += $" --platform:{testOptions.Platform}";
            }

            if (!string.IsNullOrEmpty(testOptions.Framework))
            {
                arguments += $" --framework:\"{testOptions.Framework}\"";
            }

            if (testOptions.Blame)
            {
                arguments += $" --blame:{outputFilename}.blame";
            }

            if (testOptions.CodeCoverageCollector == CodeCoverageCollector.Coverlet)
            {
                arguments += @" --collect:""XPlat Code Coverage""";
            }
            else if (testOptions.CodeCoverageCollector == CodeCoverageCollector.VisualStudio)
            {
                arguments += @" --collect:""Code Coverage""";
            }

            if (testOptions.Diagnostics)
            {
                arguments += $" --diag:{outputFilename}.diag";
            }

            var filterClause = GetFilterClause(testOptions);
            if (filterClause != null)
            {
                arguments += $" \"--TestCaseFilter:{filterClause}\"";
            }

            if (testOptions.Tests.Any())
            {
                arguments += $" \"--Tests:{string.Join(",", testOptions.Tests)}\"";
            }

            arguments += $" \"--logger:trx;LogFileName={outputFilename}.trx\" --ResultsDirectory:{resultsDirectory}";

            // This needs to be the last argument to dotnet vstest
            if (testOptions.VsTestRuntimeSettings != null)
            {
                arguments += testOptions.VsTestRuntimeSettings.GetArgument(assemblyFile);
            }

            return arguments;
        }

        public static string GetFilterClause(TestOptions testOptions)
        {
            string filterClause = null;

            if (testOptions.ExcludeCategories.Any())
            {
                var excludedCategories = testOptions.ExcludeCategories;

                filterClause += "(";

                for (int i = 0; i < excludedCategories.Count; i++)
                {
                    filterClause += $"TestCategory!={excludedCategories[i]}";

                    if (i < excludedCategories.Count - 1)
                    {
                        filterClause += "&";
                    }
                }

                filterClause += ")";
            }

            if (testOptions.IncludeCategories.Any())
            {
                var includedCategories = testOptions.IncludeCategories;

                if (filterClause != null)
                {
                    filterClause += "&";
                }
                filterClause += "(";

                for (int i = 0; i < includedCategories.Count; i++)
                {
                    filterClause += $"TestCategory={includedCategories[i]}";
                    if (i < includedCategories.Count - 1)
                    {
                        filterClause += "|";
                    }
                }

                filterClause += ")";
            }

            if (!string.IsNullOrEmpty(testOptions.Filter))
            {
                if (string.IsNullOrEmpty(filterClause))
                {
                    filterClause = testOptions.Filter;
                }
                else if (testOptions.Filter.StartsWith("|") || testOptions.Filter.StartsWith("&"))
                {
                    filterClause = $"({filterClause}){testOptions.Filter}";
                }
                else
                {
                    filterClause = $"({filterClause})&{testOptions.Filter}";
                }
            }

            return filterClause;
        }
    }
}
