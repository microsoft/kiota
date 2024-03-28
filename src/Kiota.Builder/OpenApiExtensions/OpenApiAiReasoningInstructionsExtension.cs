using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Writers;

namespace Kiota.Builder.OpenApiExtensions;

public class OpenApiAiReasoningInstructionsExtension : IOpenApiExtension
{
    public static string Name => "x-ai-reasoning-instructions";
#pragma warning disable CA1002 // Do not expose generic lists
    public List<string> ReasoningInstructions { get; init; } = [];
#pragma warning restore CA1002 // Do not expose generic lists
    public void Write(IOpenApiWriter writer, OpenApiSpecVersion specVersion)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (ReasoningInstructions != null &&
            ReasoningInstructions.Count != 0)
        {
            writer.WriteStartArray();
            foreach (var instruction in ReasoningInstructions)
            {
                writer.WriteValue(instruction);
            }
            writer.WriteEndArray();
        }
    }
    public static OpenApiAiReasoningInstructionsExtension Parse(IOpenApiAny source)
    {
        if (source is not OpenApiArray rawArray) throw new ArgumentOutOfRangeException(nameof(source));
        var result = new OpenApiAiReasoningInstructionsExtension();
        result.ReasoningInstructions.AddRange(rawArray.OfType<OpenApiString>().Select(x => x.Value));
        return result;
    }
}
