using Serilog;
using Serilog.Exceptions;

namespace Jordnaer.Server.Extensions;

public static class SerilogExtensions
{
    public static WebApplicationBuilder AddSerilog(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, loggerConfiguration) =>
        {
            loggerConfiguration.ReadFrom.Configuration(context.Configuration)
                .Enrich.WithProperty("Application", builder.Environment.ApplicationName)
                .Enrich.FromLogContext()
                .Enrich.WithExceptionDetails();

            loggerConfiguration.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss}] [{Level}] {SourceContext}: {Message:lj}{NewLine}{Exception}");
        });

        return builder;
    }
}
