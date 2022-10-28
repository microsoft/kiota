using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder;
using Kiota.Builder.Configuration;
using Kiota.Builder.Lock;
using Microsoft.Extensions.Logging;

namespace kiota.Handlers;

internal class KiotaUpdateCommandHandler : BaseKiotaCommandHandler {
    public Option<string> OutputOption { get;set; }
    public Option<bool> CleanOutputOption { get;set; }
    public Option<bool> ClearCacheOption { get; set; }
    public override async Task<int> InvokeAsync(InvocationContext context) {
        string output = context.ParseResult.GetValueForOption(OutputOption);
        bool clearCache = context.ParseResult.GetValueForOption(ClearCacheOption);
        bool cleanOutput = context.ParseResult.GetValueForOption(CleanOutputOption);
        CancellationToken cancellationToken = (CancellationToken)context.BindingContext.GetService(typeof(CancellationToken));
        AssignIfNotNullOrEmpty(output, (c, s) => c.OutputPath = s);
        var searchPath = GetAbsolutePath(output);
        var lockService = new LockManagementService();
        var lockFileDirectoryPaths = lockService.GetDirectoriesContainingLockFile(searchPath);
        if (!lockFileDirectoryPaths.Any()) {
            DisplayError("No lock file found. Please run the generation command first.");
            return 1;
        }
        Configuration.Generation.ClearCache = clearCache;
        Configuration.Generation.CleanOutput = cleanOutput;
        var (loggerFactory, logger) = GetLoggerAndFactory<KiotaBuilder>(context);
        using (loggerFactory) {
            try {
                var locks = await Task.WhenAll(lockFileDirectoryPaths.Select(x => lockService.GetLockFromDirectoryAsync(x, cancellationToken)
                                                                                .ContinueWith(t => (lockInfo: t.Result, lockDirectoryPath: x), cancellationToken)));
                var configurations = locks.Select(x => {
                                            var config = Configuration.Generation.Clone() as GenerationConfiguration;
                                            x.lockInfo.UpdateGenerationConfigurationFromLock(config);
                                            config.OutputPath = x.lockDirectoryPath;
                                            return config;
                                        }).ToArray();
                await Task.WhenAll(configurations
                                        .Select(x => new KiotaBuilder(logger, x)
                                                    .GenerateClientAsync(cancellationToken)));
                DisplaySuccess($"Update of {locks.Length} clients completed successfully");
                foreach(var configuration in configurations)
                    DisplayInfoHint(configuration.Language, configuration.OpenAPIFilePath);
                return 0;
            } catch (Exception ex) {
    #if DEBUG
                logger.LogCritical(ex, "error updating the client: {exceptionMessage}", ex.Message);
                throw; // so debug tools go straight to the source of the exception when attached
    #else
                logger.LogCritical("error updating the client: {exceptionMessage}", ex.Message);
                return 1;
    #endif
            }
        }
    }

}
