using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace VsTestRunner.Tests
{
    public class TestRunnerTests : TestRunner
    {
        protected override Task<int> Run(Options options)
        {
            return Task.FromResult(0);
        }

        [Test]
        public void ValidateDependencyInjectionConfiguration()
        {
            // Yes to all intents and purposes this looks like a giant waste of time!
            // BUT it is a really important test because it validates that the DI container correctly builds.
            // Well to a point - not sure it validates open generics but I think this is all I need.
            Run(new string[0]).Result.Should().Be(0);
        }
    }
}
