using System.CommandLine;
using kiota.Handlers.Config;

namespace kiota;

public static class KiotaConfigCommands
{
    public static Command GetConfigNodeCommand()
    {
        var command = new Command("config", "Manages the Kiota configuration");
        command.AddCommand(GetInitCommand());
        command.AddCommand(GetMigrateCommand());
        return command;
    }
    private static Command GetInitCommand()
    {
        var logLevelOption = KiotaHost.GetLogLevelOption();
        var command = new Command("init", "Initializes the Kiota configuration"){
            logLevelOption,
        };
        command.Handler = new InitHandler
        {
            LogLevelOption = logLevelOption,
        };
        return command;
    }
    private static Command GetMigrateCommand()
    {
        var logLevelOption = KiotaHost.GetLogLevelOption();
        var lockDirectoryOption = GetLockDirectoryOption();
        var classOption = KiotaClientCommands.GetClientNameOption(false);
        var command = new Command("migrate", "Migrates a kiota lock file to a Kiota configuration")
        {
            logLevelOption,
            lockDirectoryOption,
            classOption,
        };
        command.Handler = new MigrateHandler
        {
            LogLevelOption = logLevelOption,
            LockDirectoryOption = lockDirectoryOption,
            ClassOption = classOption,
        };
        return command;
    }
    private static Option<string> GetLockDirectoryOption()
    {
        var option = new Option<string>("--lock-directory", "The directory containing a kiota-lock.json file");
        option.AddAlias("--ld");
        return option;
    }
}
