using Microsoft.DeclarativeAgents.Manifest;
using Microsoft.OpenApi.Extensions;

namespace kiota.Rpc
{
    internal class PluginResultMapper
    {
        public static ShowPluginResult FromPluginManifesValidationResult(DocumentValidationResults<PluginManifestDocument> manifest)
        {
            var functions = new List<PluginFunction>();
            var runtimes = new List<PluginRuntime>();

            if (manifest.Document?.Functions != null)
            {
                foreach (var function in manifest.Document.Functions)
                {
                    var pluginFunction = new PluginFunction(
                        name: function.Name,
                        description: function.Description);
                    functions.Add(pluginFunction);
                }
            }

            if (manifest.Document?.Runtimes != null)
            {
                foreach (var runtime in manifest.Document.Runtimes)
                {
                    var runForFunctions = runtime.RunForFunctions?.ToArray() ?? Array.Empty<string>();
                    var pluginRuntime = new PluginRuntime(
                        type: runtime.Type?.GetDisplayName() ?? String.Empty,
                        auth: runtime.Auth?.Type ?? AuthType.None,
                        run_for_functions: runForFunctions);
                    runtimes.Add(pluginRuntime);
                }
            }

            return new ShowPluginResult(
                isValid: manifest.IsValid,
                schema_version: manifest.Document?.SchemaVersion ?? string.Empty,
                name_for_human: manifest.Document?.NameForHuman ?? string.Empty,
                functions: functions.ToArray(),
                runtime: runtimes.ToArray());
        }
    }
}
