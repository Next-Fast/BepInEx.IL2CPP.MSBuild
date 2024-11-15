using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace NextBepLoader.BepInEx.IL2CPP.MSBuild.Runner;

public class TaskLogger(TaskLoggingHelper logger) : ILogger
{
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        logger.LogMessage(logLevel switch
        {
            LogLevel.None => MessageImportance.Low,
            LogLevel.Trace => MessageImportance.Low,
            LogLevel.Debug => MessageImportance.Low,
            LogLevel.Information => MessageImportance.Normal,
            LogLevel.Warning => MessageImportance.High,
            LogLevel.Error => MessageImportance.High,
            LogLevel.Critical => MessageImportance.High,
            _ => throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null),
        }, formatter(state, exception));
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        throw new NotSupportedException();
    }
}
