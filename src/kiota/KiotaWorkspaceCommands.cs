using System.CommandLine;
using kiota.Handlers.Workspace;

namespace kiota;

public static class KiotaWorkspaceCommands
{
    public static Command GetWorkspaceNodeCommand(IServiceProvider? serviceProvider)
    {
        var command = new Command("workspace", "Manages the Kiota workspace configuration");
        command.Add(GetInitCommand(serviceProvider));
        command.Add(GetMigrateCommand(serviceProvider));
        return command;
    }
    private static Command GetInitCommand(IServiceProvider? serviceProvider)
    {
        var logLevelOption = KiotaHost.GetLogLevelOption();
        var command = new Command("init", "Initializes the Kiota workspace configuration"){
            logLevelOption,
        };
        command.Action = new InitHandler
        {
            LogLevelOption = logLevelOption,
            ServiceProvider = serviceProvider,
        };
        return command;
    }
    private static Command GetMigrateCommand(IServiceProvider? serviceProvider)
    {
        var logLevelOption = KiotaHost.GetLogLevelOption();
        var lockDirectoryOption = GetLockDirectoryOption();
        var classOption = KiotaClientCommands.GetClientNameOption(false);
        var command = new Command("migrate", "Migrates a kiota lock file to a Kiota workspace configuration")
        {
            logLevelOption,
            lockDirectoryOption,
            classOption,
        };
        command.Action = new MigrateHandler
        {
            LogLevelOption = logLevelOption,
            LockDirectoryOption = lockDirectoryOption,
            ClassOption = classOption,
            ServiceProvider = serviceProvider,
        };
        return command;
    }
    private static Option<string> GetLockDirectoryOption()
    {
        var option = new Option<string>("--lock-directory")
        {
            Description = "The directory containing a kiota-lock.json file",
        };
        option.Aliases.Add("--ld");
        return option;
    }
}
