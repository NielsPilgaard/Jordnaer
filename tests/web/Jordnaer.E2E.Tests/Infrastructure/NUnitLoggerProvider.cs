using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Jordnaer.E2E.Tests.Infrastructure;

/// <summary>
/// Routes server-side log messages to NUnit's TestContext.Out so they appear in test output.
/// Wire up via loggingBuilder.AddProvider(new NUnitLoggerProvider(minLevel)) in E2eWebApplicationFactory.
/// </summary>
public sealed class NUnitLoggerProvider(LogLevel minimumLevel = LogLevel.Warning) : ILoggerProvider
{
	public ILogger CreateLogger(string categoryName) => new NUnitLogger(categoryName, minimumLevel);

	public void Dispose() { }
}

file sealed class NUnitLogger(string categoryName, LogLevel minimumLevel) : ILogger
{
	public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

	public bool IsEnabled(LogLevel logLevel) => logLevel >= minimumLevel;

	public void Log<TState>(
		LogLevel logLevel,
		EventId eventId,
		TState state,
		Exception? exception,
		Func<TState, Exception?, string> formatter)
	{
		if (!IsEnabled(logLevel))
		{
			return;
		}

		var message = formatter(state, exception);
		var level = logLevel switch
		{
			LogLevel.Trace => "TRC",
			LogLevel.Debug => "DBG",
			LogLevel.Information => "INF",
			LogLevel.Warning => "WRN",
			LogLevel.Error => "ERR",
			LogLevel.Critical => "CRT",
			_ => "???"
		};

		var line = $"[{level}] {categoryName}: {message}";
		if (exception is not null)
		{
			line += $"\n{exception}";
		}

		TestContext.Out.WriteLine(line);
	}
}
