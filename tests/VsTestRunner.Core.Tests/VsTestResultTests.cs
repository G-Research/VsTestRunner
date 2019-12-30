using System.IO;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace VsTestRunner.Core.Tests
{
    [TestFixture]
    public class VsTestResultTests
    {
        [Test]
        public void ResultParsedCorrectly()
        {
            var results = new VsTestResult("UniverseGenerator", VsTestResult.SuccessExitCode, Path.Combine(TestContext.CurrentContext.TestDirectory, "UniverseGenerator.trx"), false);

            results.Result.Should().Be(Result.Success);
            results.ExitCode.Should().Be(0);
            results.ResultsParsed.Should().BeTrue();
        }
    }
}
