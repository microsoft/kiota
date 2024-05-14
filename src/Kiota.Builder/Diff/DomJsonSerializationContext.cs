using System.Text.Json.Serialization;
using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Diff;

[JsonSerializable(typeof(CodeNamespace))]
internal partial class DomJsonSerializationContext : JsonSerializerContext
{
}
