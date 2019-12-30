using System;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace VsTestRunner.Core
{
    public class LoggerEx<T> : ILogger<T>
    {
        private readonly ILogger _logger;

        public LoggerEx(ILoggerFactory factory)
        {
            var loggerName = typeof(T).Assembly == Assembly.GetExecutingAssembly()
                ? ""
                : typeof(T).FullName;

            _logger = factory.CreateLogger(loggerName);
        }

        IDisposable ILogger.BeginScope<TState>(TState state)
        {
            return _logger.BeginScope(state);
        }

        bool ILogger.IsEnabled(LogLevel logLevel)
        {
            return _logger.IsEnabled(logLevel);
        }

        void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            _logger.Log(logLevel, eventId, state, exception, formatter);
        }
    }
}
