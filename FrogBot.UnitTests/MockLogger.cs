using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;

namespace FrogBot.UnitTests;

public class MockLogger<T> : ILogger<T>
{
    private readonly List<LogMessage> _logMessages = new();

    public IEnumerable<LogMessage> this[LogLevel level] => _logMessages.Where(message => message.LogLevel == level);

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _logMessages.Add(
            new LogMessage
            {
                LogLevel = logLevel,
                Exception = exception,
                Message = formatter(state, exception)
            });
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => Mock.Of<IDisposable>();

    public class LogMessage
    {
        public LogLevel LogLevel { get; init; }
        public Exception? Exception { get; set; }
        public string Message { get; init; } = null!;
    }
}