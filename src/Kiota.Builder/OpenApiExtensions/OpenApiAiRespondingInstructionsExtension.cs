using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.OpenApi;

namespace Kiota.Builder.OpenApiExtensions;

public class OpenApiAiRespondingInstructionsExtension : IOpenApiExtension
{
    public static string Name => "x-ai-responding-instructions";
#pragma warning disable CA1002 // Do not expose generic lists
    public List<string> RespondingInstructions { get; init; } = [];
#pragma warning restore CA1002 // Do not expose generic lists
    public void Write(IOpenApiWriter writer, OpenApiSpecVersion specVersion)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (RespondingInstructions != null &&
            RespondingInstructions.Count != 0)
        {
            writer.WriteStartArray();
            foreach (var instruction in RespondingInstructions)
            {
                writer.WriteValue(instruction);
            }
            writer.WriteEndArray();
        }
    }
    public static OpenApiAiRespondingInstructionsExtension Parse(JsonNode source)
    {
        if (source is not JsonArray rawArray) throw new ArgumentOutOfRangeException(nameof(source));
        var result = new OpenApiAiRespondingInstructionsExtension();
        result.RespondingInstructions.AddRange(rawArray.OfType<JsonValue>().Where(static x => x.GetValueKind() is JsonValueKind.String).Select(static x => x.GetValue<string>()));
        return result;
    }
}
