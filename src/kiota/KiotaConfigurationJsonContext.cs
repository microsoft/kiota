
using System.Text.Json.Serialization;
using Kiota.Builder.Configuration;

namespace kiota;

[JsonSerializable(typeof(KiotaConfiguration))]
internal partial class KiotaConfigurationJsonContext : JsonSerializerContext
{

}
