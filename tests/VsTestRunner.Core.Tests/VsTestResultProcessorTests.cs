using System.IO;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace VsTestRunner.Core.Tests
{
    public class VsTestResultProcessorTests
    {
        [Test]
        public void GenerateDummyTestResultWhenNonePresent()
        {
            string trxFile = Path.Combine(TestContext.CurrentContext.TestDirectory, "DummyTest1.trx");

            if (File.Exists(trxFile))
            {
                File.Delete(trxFile);
            }

            VsTestResultProcessor.CreateDummyResultFileForNoTests(trxFile, "DummyTest");

            VsTestResult vsTestResult = new VsTestResult("DummyTest", 0, trxFile, false);

            vsTestResult.ExitCode.Should().Be(0);
            vsTestResult.ResultsParsed.Should().BeTrue();
            vsTestResult.Result.Should().Be(Result.Failure);

            vsTestResult = new VsTestResult("DummyTest", 0, trxFile, true);
            vsTestResult.ExitCode.Should().Be(0);
            vsTestResult.ResultsParsed.Should().BeTrue();
            vsTestResult.Result.Should().Be(Result.Success);

            File.Delete(trxFile);
        }

        [Test]
        public void GeneratedCancelledResultParsesCorrectly()
        {
            string trxFile = Path.Combine(TestContext.CurrentContext.TestDirectory, "DummyTest2.trx");

            if (File.Exists(trxFile))
            {
                File.Delete(trxFile);
            }

            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

            var vsTestResult = VsTestResultProcessor.GetResultsForFailure(trxFile, VsTestFailureReason.Cancelled, "DummyTest", "Test execution cancelled");

            vsTestResult.Aborted.Should().Be(1);

            vsTestResult.ExitCode.Should().Be(VsTestResult.CancelledExitCode);
            vsTestResult.ResultsParsed.Should().BeTrue();
            vsTestResult.Result.Should().Be(Result.Failure);
            vsTestResult.Message.Should().Be("Test execution failed. Failure reason: Test execution cancelled.");

            vsTestResult = VsTestResultProcessor.GetResultsForFailure(trxFile, VsTestFailureReason.Error, "DummyTest", "Error in test execution");

            vsTestResult.Aborted.Should().Be(1);
            vsTestResult.ExitCode.Should().Be(VsTestResult.FailedExitCode);
            vsTestResult.ResultsParsed.Should().BeTrue();
            vsTestResult.Result.Should().Be(Result.Failure);
            vsTestResult.Message.Should().Be("Test execution failed. Failure reason: Test execution cancelled.", "The original trx file should not have be deleted...");

            File.Delete(trxFile);

            vsTestResult = VsTestResultProcessor.GetResultsForFailure(trxFile, VsTestFailureReason.Error, "DummyTest", "Error in test execution");

            vsTestResult.Aborted.Should().Be(1);
            vsTestResult.ExitCode.Should().Be(VsTestResult.FailedExitCode);
            vsTestResult.ResultsParsed.Should().BeTrue();
            vsTestResult.Result.Should().Be(Result.Failure);
            vsTestResult.Message.Should().Be("Test execution failed. Failure reason: Error in test execution.");

            File.Delete(trxFile);
        }
    }
}
