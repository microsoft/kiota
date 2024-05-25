using System;
using System.Collections.Generic;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Writers;

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

    public static OpenApiAiRespondingInstructionsExtension Parse(IOpenApiAny source)
    {
        if (source is not OpenApiArray rawArray) throw new ArgumentOutOfRangeException(nameof(source));
        var result = new OpenApiAiRespondingInstructionsExtension();
        
        foreach (var item in rawArray)
        {
            if (item is OpenApiString openApiString)
            {
                result.RespondingInstructions.Add(openApiString.Value);
            }
        }

        return result;
    }
}
