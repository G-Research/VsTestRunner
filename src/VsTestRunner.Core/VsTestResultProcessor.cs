using System;
using System.IO;
using Serilog;

namespace VsTestRunner.Core
{
    public static class VsTestResultProcessor
    {
        public const int FailedExitCode = 1;
        public const int CancelledExitCode = -999;

        public static VsTestResult GetResultsForFailure(string trxFile, VsTestFailureReason failureReason, string assemblyName, string message)
        {
            CreateDummyResultFileForFailure(trxFile, failureReason, assemblyName, message);

            return new VsTestResult(assemblyName, failureReason == VsTestFailureReason.Cancelled ? CancelledExitCode : FailedExitCode, trxFile, false);
        }

        private static bool CanWriteResultsFile(string trxFile)
        {
            if (string.IsNullOrEmpty(trxFile))
            {
                Log.Error("Result file specified is invalid.");
                return false;
            }

            if (File.Exists(trxFile))
            {
                Log.Error("Result file {trxFile} already exists.", trxFile);
                return false;
            }

            var directory = Path.GetDirectoryName(trxFile);
            if (!Directory.Exists(Path.GetDirectoryName(trxFile)))
            {
                Log.Error("Result directory {directory} does not exist.", directory);
                return false;
            }

            return true;
        }

        public static void CreateDummyResultFileForFailure(string trxFile, VsTestFailureReason failureReason, string assemblyName, string message)
        {
            if (!CanWriteResultsFile(trxFile))
            {
                return;
            }

            var executionId = Guid.NewGuid();
            var testId = Guid.NewGuid();
            var testListId = Guid.NewGuid();

            try
            {
                // Could be fancy or just hard code a failed test...
                File.WriteAllText(trxFile,
$@"<?xml version=""1.0"" encoding=""utf-8""?>
<TestRun id=""{Guid.NewGuid().ToString()}"" name="""" runUser = ""{Environment.UserName}"" xmlns=""http://microsoft.com/schemas/VisualStudio/TeamTest/2010"" >
  <Times creation=""{DateTime.Now}"" queuing=""00:00:00.0000000"" start=""{DateTime.Now}"" finish=""{DateTime.Now}""/>
  <TestSettings name=""default"" id=""{Guid.NewGuid()}"">
    <Deployment runDeploymentRoot="""" />
  </TestSettings>
  <Results>
    <UnitTestResult executionId=""{executionId}"" testId=""{testId}"" testName=""{assemblyName}"" computerName=""UNKOWN"" duration=""00:00:00.0000000"" startTime=""{DateTime.UtcNow}"" endTime=""2020-03-16T18:29:59.0000000+00:00"" testType=""13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b"" outcome=""Failed"" testListId = ""{testListId}"" relativeResultsDirectory=""{executionId}""/></Results>
  <TestDefinitions>
    <UnitTest name=""{assemblyName}"" storage=""{assemblyName}.dll"" id=""{testId}"" >
      <Execution id=""{executionId}"" />
      <!-- While this setup is obviously a bit stupid and setting the class name and unit test name in this fashion doesn't look right, bamboo is rubbish and this produces the best result in the UI-->
      <TestMethod codeBase=""{assemblyName}.dll"" adapterTypeName=""executor://nunit3testexecutor/"" className=""{failureReason}"" name=""{assemblyName}"" />
    </UnitTest>
  </TestDefinitions>
  <TestEntries>
    <TestEntry testId=""{testId}"" executionId=""{executionId}"" testListId=""{testListId}"" />
  </TestEntries>
  <TestLists>
    <TestList name=""Results Not in a List"" id=""{testListId}"" />
    <TestList name=""All Loaded Results"" id=""{Guid.NewGuid()}"" />
  </TestLists>
  <ResultSummary outcome=""Aborted"" >
    <Counters total=""1"" executed=""1"" passed=""0"" failed=""0"" error=""0"" timeout=""0"" aborted=""1"" inconclusive=""0"" passedButRunAborted=""0"" notRunnable=""0"" notExecuted=""0"" disconnected=""0"" warning=""0"" completed=""0"" inProgress=""0"" pending=""0"" />
    <Output>
      <StdOut>Test execution failed. Failure reason: {failureReason}</StdOut>
    </Output>
    <RunInfos>
      <RunInfo computerName=""{Environment.MachineName}"" outcome=""Error"" timestamp=""{DateTime.Now}"">
        <Text>Test execution failed. Failure reason: {message}.</Text>
      </RunInfo>
    </RunInfos>
  </ResultSummary>
</TestRun>
");
                Log.Information("Written dummy cancelled trx results file for {assemblyName}.", assemblyName);
            }
            catch (Exception exception)
            {
                Log.Error(exception, "Error while attempting to generate dummy results file {trxFile}", trxFile);
            }
        }

        public static void CreateDummyResultFileForNoTests(string trxFile, string assemblyName)
        {
            if (!CanWriteResultsFile(trxFile))
            {
                return;
            }

            var executionId = Guid.NewGuid();
            var testId = Guid.NewGuid();
            var testListId = Guid.NewGuid();

            try
            {
                // Could be fancy or just hard code a failed test...
                File.WriteAllText(trxFile,
$@"<?xml version=""1.0"" encoding=""utf-8""?>
<TestRun id=""{Guid.NewGuid().ToString()}"" name="""" runUser = ""{Environment.UserName}"" xmlns=""http://microsoft.com/schemas/VisualStudio/TeamTest/2010"" >
  <Times creation=""{DateTime.Now}"" queuing=""00:00:00.0000000"" start=""{DateTime.Now}"" finish=""{DateTime.Now}""/>
  <TestSettings name=""default"" id=""{Guid.NewGuid()}"">
    <Deployment runDeploymentRoot="""" />
  </TestSettings>
  <TestLists>
    <TestList name=""Results Not in a List"" id=""{testListId}"" />
    <TestList name=""All Loaded Results"" id=""{Guid.NewGuid()}"" />
  </TestLists>
  <ResultSummary outcome=""Aborted"" >
    <Counters total=""0"" executed=""0"" passed=""0"" failed=""0"" error=""0"" timeout=""0"" aborted=""0"" inconclusive=""0"" passedButRunAborted=""0"" notRunnable=""0"" notExecuted=""0"" disconnected=""0"" warning=""0"" completed=""0"" inProgress=""0"" pending=""0"" />
    <Output>
      <StdOut>Failed to load or no tests found.</StdOut>
    </Output>
    <RunInfos>
      <RunInfo computerName=""CZWWILLIAMRO2"" outcome=""Warning"" timestamp=""2020-06-30T20:42:18.0581515+01:00"">
        <Text>No test is available in {assemblyName}. Tests either filtered or </Text>
      </RunInfo>
    </RunInfos>
  </ResultSummary>
</TestRun>
");
                Log.Information("Written dummy results file for {assemblyName} with no tests in.", assemblyName);
            }
            catch (Exception exception)
            {
                Log.Error(exception, "Error while attempting to generate dummy results file {trxFile}", trxFile);
            }
        }
    }
}