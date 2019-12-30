using System.Collections.Generic;

namespace VsTestRunner.Core.Interfaces
{
    public interface ICommandLineTestOptions
    {
        VsTestPlatform TestPlatform { get; }
        string TestFramework { get; }
        bool Blame { get; }
        CodeCoverageCollector CodeCoverageCollector { get; }
        bool Diagnostics { get; }
        string Filter { get; }
        IList<string> IncludeCategories { get; }
        IList<string> ExcludeCategories { get; }
        IList<string> Tests { get; }
        double TestTimeout { get; }
        double TestSessionTimeout { get; }
        bool TreatZeroTestsAsSuccess { get; }
        string RunSettings { get; }
    }
}
