using System.Runtime.CompilerServices;

namespace Jordnaer.Extensions;

public static class LoggerExtensions
{
	public static void LogFunctionBegan(this ILogger logger, LogLevel logLevel = LogLevel.Information, [CallerMemberName] string methodName = "") => logger.Log(logLevel, "Running {method_name}", methodName);

	public static void LogException(this ILogger logger, Exception exception, [CallerMemberName] string methodName = "") => logger.LogError(exception, "Exception occurred in {method_name}", methodName);
}
