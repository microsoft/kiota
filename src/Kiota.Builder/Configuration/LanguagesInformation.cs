using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Writers;

namespace Kiota.Builder.Configuration;

public class LanguagesInformation : Dictionary<string, LanguageInformation>, IOpenApiSerializable
{
    public void SerializeAsV2(IOpenApiWriter writer)
    {
        SerializeAsV3(writer);
    }

    public void SerializeAsV3(IOpenApiWriter writer)
    {
        writer.WriteStartObject();
        foreach(var entry in this.OrderBy(static x => x.Key)) {
            writer.WriteRequiredObject(entry.Key, entry.Value, (w, x) => x.SerializeAsV3(w));
        }
        writer.WriteEndObject();
    }
    public static LanguagesInformation Parse(IOpenApiAny source)
    {
        if (source is not OpenApiObject rawObject) throw new ArgumentOutOfRangeException(nameof(source));
        var extension = new LanguagesInformation();
        foreach(var property in rawObject)
            extension.Add(property.Key, LanguageInformation.Parse(property.Value));
        return extension;
    }
}
