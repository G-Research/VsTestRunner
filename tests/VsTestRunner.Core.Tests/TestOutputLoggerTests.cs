using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using ProcessV2;

namespace VsTestRunner.Core.Tests
{
    [TestFixture]
    public class TestOutputLoggerTests
    {
        private static readonly string ExpectedOutputLogs = String.Join(Environment.NewLine,
            ">>> Executing: docker run arguments-here",
            ">>> Working dir: E:\\directory-here",
            "Unable to find image 'mcr.microsoft.com/dotnet/sdk:5.0.102-alpine3.12-amd64' locally",
            "5768ec22eb25: Pull complete",
            "Status: Downloaded newer image for mcr.microsoft.com/dotnet/sdk:5.0.102-alpine3.12-amd64",
            "Microsoft (R) Test Execution Command Line Tool Version 16.8.3",
            "Copyright (c) Microsoft Corporation.  All rights reserved.",
            "Testhost process exited with error: It was not possible to find any compatible framework version",
            "");

        [Test]
        public void RealWorldDockerOutput()
        {
            var testLogger = new TestLogger(LogLevel.Information);
            var testOutputLogger = new TestOutputLogger(testLogger);

            testOutputLogger.WriteOutput(@">>> Executing: docker run arguments-here");
            testOutputLogger.WriteOutput(@">>> Working dir: E:\directory-here");

            // Expect no messages yet
            Assert.AreEqual(0, testLogger.Messages.Count());

            testOutputLogger.WriteOutput(OutputType.StdErr, "Unable to find image 'mcr.microsoft.com/dotnet/sdk:5.0.102-alpine3.12-amd64' locally");
            testOutputLogger.WriteOutput(OutputType.StdErr, "5768ec22eb25: Pull complete");
            testOutputLogger.WriteOutput(OutputType.StdOut, "Status: Downloaded newer image for mcr.microsoft.com/dotnet/sdk:5.0.102-alpine3.12-amd64");

            CollectionAssert.AreEqual(new[]
            {
                (LogLevel.Error, "stderr: Unable to find image 'mcr.microsoft.com/dotnet/sdk:5.0.102-alpine3.12-amd64' locally"),
                (LogLevel.Error, "stderr: 5768ec22eb25: Pull complete"),
                (LogLevel.Information, "stdout: Status: Downloaded newer image for mcr.microsoft.com/dotnet/sdk:5.0.102-alpine3.12-amd64")
            }, testLogger.Messages.ToArray());

            testOutputLogger.WriteOutput(OutputType.StdOut,
                "Microsoft (R) Test Execution Command Line Tool Version 16.8.3");

            Assert.AreEqual(4, testLogger.Messages.Count());
            Assert.AreEqual((LogLevel.Information, "vstest started: pausing live output"), testLogger.Messages.Last());

            testOutputLogger.WriteOutput(OutputType.StdOut, "Copyright (c) Microsoft Corporation.  All rights reserved.");
            testOutputLogger.WriteOutput(OutputType.StdOut, "Testhost process exited with error: It was not possible to find any compatible framework version");

            Assert.AreEqual(4, testLogger.Messages.Count());
            Assert.AreEqual(ExpectedOutputLogs, testOutputLogger.GetOutputLines());
        }

        [Test]
        public void AlwaysLiveLogsWhenDebugLogIsEnabled()
        {
            var testLogger = new TestLogger(LogLevel.Debug);
            var testOutputLogger = new TestOutputLogger(testLogger);

            testOutputLogger.WriteOutput(@">>> Executing: docker run arguments-here");
            testOutputLogger.WriteOutput(@">>> Working dir: E:\directory-here");

            // Expect no messages yet
            Assert.AreEqual(0, testLogger.Messages.Count());

            testOutputLogger.WriteOutput(OutputType.StdErr, "Unable to find image 'mcr.microsoft.com/dotnet/sdk:5.0.102-alpine3.12-amd64' locally");
            testOutputLogger.WriteOutput(OutputType.StdErr, "5768ec22eb25: Pull complete");
            testOutputLogger.WriteOutput(OutputType.StdOut, "Status: Downloaded newer image for mcr.microsoft.com/dotnet/sdk:5.0.102-alpine3.12-amd64");

            Assert.AreEqual(3, testLogger.Messages.Count());

            testOutputLogger.WriteOutput(OutputType.StdOut,
                "Microsoft (R) Test Execution Command Line Tool Version 16.8.3");

            Assert.AreEqual(4, testLogger.Messages.Count());
            Assert.AreEqual((LogLevel.Debug, "stdout: Microsoft (R) Test Execution Command Line Tool Version 16.8.3"), testLogger.Messages.Last());

            testOutputLogger.WriteOutput(OutputType.StdOut, "Copyright (c) Microsoft Corporation.  All rights reserved.");
            testOutputLogger.WriteOutput(OutputType.StdOut, "Testhost process exited with error: It was not possible to find any compatible framework version");

            Assert.AreEqual(6, testLogger.Messages.Count());

            var expectedOutput = String.Join(Environment.NewLine,
                ">>> Executing: docker run arguments-here",
                ">>> Working dir: E:\\directory-here",
                "Unable to find image 'mcr.microsoft.com/dotnet/sdk:5.0.102-alpine3.12-amd64' locally",
                "5768ec22eb25: Pull complete",
                "Status: Downloaded newer image for mcr.microsoft.com/dotnet/sdk:5.0.102-alpine3.12-amd64",
                "Microsoft (R) Test Execution Command Line Tool Version 16.8.3",
                "Copyright (c) Microsoft Corporation.  All rights reserved.",
                "Testhost process exited with error: It was not possible to find any compatible framework version",
                "");
            Assert.AreEqual(ExpectedOutputLogs, testOutputLogger.GetOutputLines());
        }
    }
}
