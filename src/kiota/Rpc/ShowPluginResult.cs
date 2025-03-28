namespace kiota.Rpc;

public record ShowPluginResult(
    bool isValid,
    string schema_version,
    string name_for_human,
    PluginFunction[] functions,
    PluginRuntime[] runtime);
