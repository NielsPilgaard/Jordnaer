using System.Runtime.CompilerServices;
using OneOf.Types;

namespace Jordnaer.Extensions;

public static class LoggerExtensions
{
	public static void LogFunctionBegan(this ILogger logger, LogLevel logLevel = LogLevel.Information, [CallerMemberName] string methodName = "") => logger.Log(logLevel, "Running {method_name}", methodName);

	public static void LogException(this ILogger logger, Exception exception, [CallerMemberName] string methodName = "") => logger.LogError(exception, "Exception occurred in {method_name}", methodName);

	public static Error<string> LogAndReturnErrorResult(this ILogger logger, string error,
		[CallerMemberName] string methodName = "")
	{
		logger.LogError("{MethodName}: {Error}", methodName, error);
		return new Error<string>(error);
	}

	public static Error<string> LogAndReturnErrorResult(this ILogger logger, Exception exception, string error,
		[CallerMemberName] string methodName = "")
	{
		logger.LogError(exception, "{MethodName}: {Error}", methodName, error);
		return new Error<string>(error);
	}
}
