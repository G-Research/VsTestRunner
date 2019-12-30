using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace VsTestRunner.Core.Tests
{
    public class TestLogger : ILogger
    {
        private readonly LogLevel _minimumLogLevel;
        private ConcurrentQueue<(LogLevel, string)> _messages = new ConcurrentQueue<(LogLevel, string)>();

        public IEnumerable<(LogLevel, string)> Messages => _messages;

        public TestLogger(LogLevel minimumLogLevel)
        {
            _minimumLogLevel = minimumLogLevel;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            _messages.Enqueue((logLevel, formatter(state, exception)));
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= _minimumLogLevel;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }
    }

    public class TestLogger<T> : TestLogger, ILogger<T>
    {
        public TestLogger(LogLevel minimumLogLevel) : base(minimumLogLevel)
        {
        }
    }
}
