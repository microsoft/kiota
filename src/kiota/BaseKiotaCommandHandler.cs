using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using Kiota.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace kiota;

internal abstract class BaseKiotaCommandHandler : ICommandHandler
{
    public Option<LogLevel> LogLevelOption { get;set; }
    protected KiotaConfiguration Configuration { get => ConfigurationFactory.Value; }
    private readonly Lazy<KiotaConfiguration> ConfigurationFactory = new (() => {
        var builder = new ConfigurationBuilder();
        var configuration = builder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables(prefix: "KIOTA_")
                .Build();
        var configObject = new KiotaConfiguration();
        configuration.Bind(configObject);
        return configObject;
    });
    public int Invoke(InvocationContext context)
    {
        return InvokeAsync(context).GetAwaiter().GetResult();
    }
    public abstract Task<int> InvokeAsync(InvocationContext context);
    protected (ILoggerFactory, ILogger<T>) GetLoggerAndFactory<T>(InvocationContext context) {
        LogLevel logLevel = context.ParseResult.GetValueForOption(LogLevelOption);
#if DEBUG
        logLevel = logLevel > LogLevel.Debug ? LogLevel.Debug : logLevel;
#endif

        var loggerFactory = LoggerFactory.Create(builder => {
            builder
                .AddConsole()
#if DEBUG
                .AddDebug()
#endif
                .SetMinimumLevel(logLevel);
        });
        var logger = loggerFactory.CreateLogger<T>();
        return (loggerFactory, logger);
    }
}
