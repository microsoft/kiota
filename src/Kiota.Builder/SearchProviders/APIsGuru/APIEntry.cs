using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Kiota.Builder.SearchProviders.APIsGuru;

public record ApiEntry(DateTimeOffset? added, string preferred, Dictionary<string, ApiVersion> versions);

public record ApiVersion(DateTimeOffset? added, ApiInformation info, DateTimeOffset? updated, Uri swaggerUrl, Uri swaggerYamlUrl, string openApiVer);

public record ApiInformation {
    public ApiContact contact { get; set;}
    public string description { get; set;}
    public string title { get; set;}
    public string version { get; set;}
    [JsonPropertyName("x-origin")]
    public List<ApiOrigin> origin { get; set;}
}

public record ApiContact(string email, string name, Uri url);

public record ApiOrigin(Uri url);
