using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using ProcessV2;

namespace VsTestRunner.Core
{
    public class TestOutputLogger : IOutputWriter
    {
        private readonly ILogger _logger;
        private readonly ConcurrentQueue<string> _output = new ConcurrentQueue<string>();
        private readonly bool _isDebugLogEnabled;

        private bool _vsTestStarted;
        private int _messagesLogged;

        public TestOutputLogger(ILogger logger)
        {
            _logger = logger;
            _isDebugLogEnabled = logger.IsEnabled(LogLevel.Debug);
        }

        public void WriteOutput(string message)
        {
            _output.Enqueue(message);
        }

        public void WriteOutput(OutputType outputType, string message)
        {
            if (message.StartsWith("Microsoft (R) Test Execution Command Line Tool"))
            {
                _vsTestStarted = true;

                if (_messagesLogged > 0 && !_isDebugLogEnabled)
                {
                    _logger.LogInformation("vstest started: pausing live output");
                }
            }

            _output.Enqueue(message);

            // Live log all output prior to vstest actually starting
            // This includes useful information like Docker image download that may be slow
            if (_isDebugLogEnabled || !_vsTestStarted)
            {
                Interlocked.Increment(ref _messagesLogged);

                var logLevel = _vsTestStarted
                    ? LogLevel.Debug
                    : outputType == OutputType.StdOut ? LogLevel.Information : LogLevel.Error;
                var prefix = outputType == OutputType.StdOut ? "stdout" : "stderr";
                _logger.Log(logLevel, prefix + ": {Message}", message);
            }
        }

        public string GetOutputLines()
        {
            var sb = new StringBuilder();
            foreach (var message in _output)
            {
                sb.AppendLine(message);
            }
            return sb.ToString();
        }
    }
}
