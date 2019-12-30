using VsTestRunner.Nuke;
using FluentAssertions;

namespace VsxTestRunner.Nuke.Tests
{
    public class VsTestRunnerSettingsTests
    {
        [Fact]
        public void TestArgumentBuilding()
        {
            string testAssemlyPath = "base-dir/tests/SomeTest.dll";
            VsTestRunnerSettings settings = new VsTestRunnerSettings("vstest-runner")
            {
                TestAssemblies = new string[] { testAssemlyPath },
                DockerImage = new string[] { "mcr.microsoft.com/dotnet/sdk:5.0.100-focal-amd64" },
                NoMetrics = true,
                ResultsDirectory = "test-results/net5.0"
            };

            var args = settings.GetProcessArguments();
            args.ToString().Should().Contain($"--test-assemblies {testAssemlyPath}");
        }
    }
}