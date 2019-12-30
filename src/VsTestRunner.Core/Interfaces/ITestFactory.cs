using System.IO;

namespace VsTestRunner.Core.Interfaces
{
    public interface ITestFactory
    {
        Test Create(int testId, FileInfo testAssembly);
    }
}