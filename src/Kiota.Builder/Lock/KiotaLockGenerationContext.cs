using System.Text.Json.Serialization;

namespace Kiota.Builder.Lock;

[JsonSerializable(typeof(KiotaLock))]
internal partial class KiotaLockGenerationContext : JsonSerializerContext
{
}
