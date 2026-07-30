using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SPTarkov.Core.Logging;

namespace SPTarkov.Core.Extensions;

public static class LoggingExtensions
{
    public static ILoggingBuilder AddFileLogger(this ILoggingBuilder builder)
    {
        builder.Services.AddSingleton<ILoggerProvider>(new FileLoggerProvider());
        return builder;
    }
}
