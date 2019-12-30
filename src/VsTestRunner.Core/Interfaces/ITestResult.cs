namespace VsTestRunner.Core.Interfaces
{
    public interface ITestResult
    {
        string AssemblyName { get; }

        string Framework { get; }

        VsTestResult VsTestResult { get; }

        Result Result { get; }
    }
}