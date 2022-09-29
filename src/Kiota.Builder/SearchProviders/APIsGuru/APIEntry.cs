using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Kiota.Builder.SearchProviders.APIsGuru;

public record APIEntry(DateTimeOffset? added, string preferred, Dictionary<string, APIVersion> versions);

public record APIVersion(DateTimeOffset? added, APIInformation info, DateTimeOffset? updated, Uri swaggerUrl, Uri swaggerYamlUrl, string openApiVer);

public record APIInformation {
    public APIContact contact { get; set;}
    public string description { get; set;}
    public string title { get; set;}
    public string version { get; set;}
    [JsonPropertyName("x-origin")]
    public List<APIOrigin> origin { get; set;}
}

public record APIContact(string email, string name, Uri url);

public record APIOrigin(Uri url);
