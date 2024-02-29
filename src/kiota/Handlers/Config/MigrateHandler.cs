using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
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
    public override async Task<int> InvokeAsync(InvocationContext context)
    {
        var workingDirectory = NormalizeSlashesInPath(Directory.GetCurrentDirectory());
        string lockDirectory = context.ParseResult.GetValueForOption(LockDirectoryOption) ?? workingDirectory;
        string clientName = context.ParseResult.GetValueForOption(ClassOption) ?? string.Empty;
        CancellationToken cancellationToken = context.BindingContext.GetService(typeof(CancellationToken)) is CancellationToken token ? token : CancellationToken.None;
        lockDirectory = NormalizeSlashesInPath(lockDirectory);
        var (loggerFactory, logger) = GetLoggerAndFactory<WorkspaceManagementService>(context, Configuration.Generation.OutputPath);
        using (loggerFactory)
        {
            try
            {
                var workspaceManagementService = new WorkspaceManagementService(logger, true, workingDirectory);
                var clientNames = await workspaceManagementService.MigrateFromLockFileAsync(clientName, lockDirectory, cancellationToken).ConfigureAwait(false);
                if (!clientNames.Any())
                {
                    logger.LogWarning("no client configuration was migrated");
                    return 1;
                }
                logger.LogInformation("client configurations migrated successfully: {clientNames}", string.Join(", ", clientNames));
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
