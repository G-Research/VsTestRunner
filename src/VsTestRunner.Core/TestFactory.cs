using System.IO;
using Microsoft.Extensions.Logging;
using VsTestRunner.Core.Interfaces;

namespace VsTestRunner.Core
{
    public class TestFactory : ITestFactory
    {
        private readonly ILoggerFactory _loggerFactory;

        public TestFactory(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public Test Create(int testId, FileInfo testAssembly)
        {
            var logger = _loggerFactory.CreateLogger(testAssembly.Name);
            var outputWriter = new TestOutputLogger(logger);
            return new Test(logger, outputWriter, testId, testAssembly);
        }
    }
}
