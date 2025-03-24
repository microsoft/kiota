namespace kiota.Rpc;

public record PluginRuntime(
    string type,
    string auth, // None, OAuthPluginVault, ApiKeyPluginVault
    string[] run_for_functions);
