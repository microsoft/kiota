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
    public required Option<string> OutputOption { get;init; }
    public required Option<bool> CleanOutputOption { get;init; }
    public required Option<bool> ClearCacheOption { get; init; }
    public override async Task<int> InvokeAsync(InvocationContext context) {
        string output = context.ParseResult.GetValueForOption(OutputOption) ?? string.Empty;
        bool clearCache = context.ParseResult.GetValueForOption(ClearCacheOption);
        bool cleanOutput = context.ParseResult.GetValueForOption(CleanOutputOption);
        CancellationToken cancellationToken = context.BindingContext.GetService(typeof(CancellationToken)) is CancellationToken token ? token : CancellationToken.None;
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
                                            var config = (GenerationConfiguration)Configuration.Generation.Clone();
                                            x.lockInfo?.UpdateGenerationConfigurationFromLock(config);
                                            config.OutputPath = x.lockDirectoryPath;
                                            return config;
                                        }).ToArray();
                var results = await Task.WhenAll(configurations
                                        .Select(x => new KiotaBuilder(logger, x, httpClient)
                                                    .GenerateClientAsync(cancellationToken)));
                foreach (var (lockInfo, lockDirectoryPath) in locks)
                    DisplaySuccess($"Update of {lockInfo?.ClientClassName} client for {lockInfo?.Language} at {lockDirectoryPath} completed");
                DisplaySuccess($"Update of {locks.Length} clients completed successfully");
                foreach(var configuration in configurations)
                    DisplayInfoHint(configuration.Language, configuration.OpenAPIFilePath);
                if(results.Any(x => x))
                    DisplayCleanHint("update");
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
