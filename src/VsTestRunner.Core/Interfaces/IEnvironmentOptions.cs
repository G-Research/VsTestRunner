using System.Collections.Generic;

namespace VsTestRunner.Core.Interfaces
{
    public interface IEnvironmentOptions
    {
        uint MaxDegreeOfParallelism { get; }
        bool RemoveAllEnvironmentVariables { get; }
        IEnumerable<string> EnvironmentVariables { get; }
        IEnumerable<string> AdditionalMounts { get; }
        IEnumerable<string> DockerImage { get; }
        string ResultsDirectory { get; }
        bool WriteToBaseResultsDirectory { get; }
    }
}
