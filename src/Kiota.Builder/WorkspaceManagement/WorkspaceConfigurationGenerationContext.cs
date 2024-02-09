
using System.Text.Json.Serialization;

namespace Kiota.Builder.WorkspaceManagement;

[JsonSerializable(typeof(WorkspaceConfiguration))]
internal partial class WorkspaceConfigurationGenerationContext : JsonSerializerContext
{
}
