using System;
using System.Collections.Generic;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Writers;

namespace Kiota.Builder.Configuration;

public class LanguagesInformation : Dictionary<string, LanguageInformation>, IOpenApiSerializable, ICloneable
{
    public void SerializeAsV2(IOpenApiWriter writer) => SerializeAsV3(writer);
    public void SerializeAsV3(IOpenApiWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStartObject();

        var sortedEntries = new SortedDictionary<string, LanguageInformation>(this, StringComparer.OrdinalIgnoreCase);
        foreach (var entry in sortedEntries)
        {
            writer.WriteRequiredObject(entry.Key, entry.Value, (w, x) => x.SerializeAsV3(w));
        }

        writer.WriteEndObject();
    }
    public static LanguagesInformation Parse(IOpenApiAny source)
    {
        if (source is not OpenApiObject rawObject) throw new ArgumentOutOfRangeException(nameof(source));
        var extension = new LanguagesInformation();
        foreach (var property in rawObject)
            extension.Add(property.Key, LanguageInformation.Parse(property.Value));
        return extension;
    }

    public object Clone()
    {
        var result = new LanguagesInformation();
        foreach (var entry in this)
            result.Add(entry.Key, entry.Value);// records don't need to be cloned as they are immutable
        return result;
    }
}
