using Kiota.Builder.Extensions;
using Microsoft.DeclarativeAgents.Manifest;
using Microsoft.OpenApi;

namespace kiota.Rpc
{
    internal class PluginResultMapper
    {
        internal static ShowPluginResult FromPluginManifesValidationResult(DocumentValidationResults<PluginManifestDocument> manifest)
        {
            var functions = BuildPluginFunctionsFromManifest(manifest);
            var runtimes = BuildRuntimesFromManifest(manifest);

            return new ShowPluginResult(
                isValid: manifest.IsValid,
                schema_version: manifest.Document?.SchemaVersion ?? string.Empty,
                name_for_human: manifest.Document?.NameForHuman ?? string.Empty,
                functions: functions.ToArray(),
                runtime: runtimes.ToArray());
        }

        private static List<PluginRuntime> BuildRuntimesFromManifest(DocumentValidationResults<PluginManifestDocument> manifest)
        {
            var runtimes = new List<PluginRuntime>();
            if (manifest.Document?.Runtimes != null)
            {
                foreach (var runtime in manifest.Document.Runtimes)
                {
                    var referenceId = runtime.Auth.GetReferenceId();
                    var pluginAuth = new PluginAuth(
                        type: runtime.Auth?.Type ?? AuthType.None,
                        reference_id: referenceId);
                    var runForFunctions = runtime.RunForFunctions?.ToArray() ?? Array.Empty<string>();
                    var pluginRuntime = new PluginRuntime(
                        type: runtime.Type?.GetDisplayName() ?? String.Empty,
                        auth: pluginAuth,
                        run_for_functions: runForFunctions);
                    runtimes.Add(pluginRuntime);
                }
            }

            return runtimes;
        }

        private static List<PluginFunction> BuildPluginFunctionsFromManifest(DocumentValidationResults<PluginManifestDocument> manifest)
        {
            var functions = new List<PluginFunction>();
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
            return functions;
        }
    }
}
