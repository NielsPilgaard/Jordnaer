using System.Runtime.CompilerServices;

namespace Jordnaer.Server.Extensions;

public static class LoggerExtensions
{
    public static void LogFunctionBegan(this ILogger logger, LogLevel logLevel = LogLevel.Information, [CallerMemberName] string methodName = "") => logger.Log(logLevel, "Running {method_name}", methodName);
}
