using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.WorkspaceManagement;
using Microsoft.Extensions.Logging;

namespace kiota.Handlers.Config;

internal class MigrateHandler : BaseKiotaCommandHandler
{
    public required Option<string> LockDirectoryOption
    {
        get;
        init;
    }
    public required Option<string> ClassOption
    {
        get; init;
    }

    public async override Task<int> InvokeAsync(InvocationContext context)
    {
        string lockDirectory = context.ParseResult.GetValueForOption(LockDirectoryOption) ?? Directory.GetCurrentDirectory();
        string className = context.ParseResult.GetValueForOption(ClassOption) ?? string.Empty;
        CancellationToken cancellationToken = context.BindingContext.GetService(typeof(CancellationToken)) is CancellationToken token ? token : CancellationToken.None;
        AssignIfNotNullOrEmpty(className, (c, s) => c.ClientClassName = s);
        var workspaceStorageService = new WorkspaceConfigurationStorageService(Directory.GetCurrentDirectory());
        var (loggerFactory, logger) = GetLoggerAndFactory<WorkspaceConfigurationStorageService>(context, Configuration.Generation.OutputPath);
        using (loggerFactory)
        {
            try
            {
                await Task.Delay(0).ConfigureAwait(false);
                // await workspaceStorageService.MigrateFromLockFileAsync(cancellationToken).ConfigureAwait(false);
                return 0;
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "error migrating the workspace configuration");
                return 1;
            }
        }
    }
}
