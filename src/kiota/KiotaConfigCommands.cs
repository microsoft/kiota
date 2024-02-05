using System.CommandLine;

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
        var command = new Command("init", "Initializes the Kiota configuration");
        //TODO map the handler
        return command;
    }
    private static Command GetMigrateCommand()
    {
        var command = new Command("migrate", "Migrates a kiota lock file to a Kiota configuration");
        //TODO map the handler
        return command;
    }
}
