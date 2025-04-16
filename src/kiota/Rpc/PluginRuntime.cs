namespace kiota.Rpc;

public record PluginAuth(
    string type, // None, OAuthPluginVault, ApiKeyPluginVault
    string? reference_id);

public record PluginRuntime(
    string type,
    PluginAuth auth,
    string[] run_for_functions);
