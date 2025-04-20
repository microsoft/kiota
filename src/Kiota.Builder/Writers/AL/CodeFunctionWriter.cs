using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using static Kiota.Builder.Refiners.ALRefiner;
using static Kiota.Builder.Writers.AL.ALConventionService;

namespace Kiota.Builder.Writers.AL;

public class CodeFunctionWriter(ALConventionService conventionService) : BaseElementWriter<CodeFunction, ALConventionService>(conventionService)
{
    //private static readonly HashSet<string> customSerializationWriters = new(StringComparer.OrdinalIgnoreCase) { "writeObjectValue", "writeCollectionOfObjectValues" };
    //private const string FactoryMethodReturnType = "((instance?: Parsable) => Record<string, (node: ParseNode) => void>)";    
    public override void WriteCodeElement(CodeFunction codeElement, LanguageWriter writer)
    {
        var alWriter = (writer as ALWriter);
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(alWriter);
        var method = codeElement.OriginalLocalMethod;
        var startRange = alWriter.ObjectIdProvider.StartRange;
        var idRange = new AppJsonIdRange(startRange, startRange + int.Parse(GetPropertyValueFromUsing(codeElement, "HighestObjectID"), System.Globalization.CultureInfo.InvariantCulture)); // TODO-SF: move to custom property
        var template = new AppJsonTemplate(codeElement, idRange,
                                    GetPropertyValueFromUsing(codeElement, "Name"),
                                    GetPropertyValueFromUsing(codeElement, "Publisher"),
                                    GetPropertyValueFromUsing(codeElement, "Version"),
                                    GetPropertyValueFromUsing(codeElement, "Description"),
                                    GetPropertyValueFromUsing(codeElement, "Brief"));

        string jsonString = JsonSerializer.Serialize(template, AppJsonTemplateContext.Default.AppJsonTemplate);
        writer.Write(jsonString);
    }
    private static string GetPropertyValueFromUsing(CodeFunction codeElement, string propertyIdentifer)
    {
        var property = codeElement.Usings.FirstOrDefault(x => x.Name.StartsWith(propertyIdentifer, StringComparison.OrdinalIgnoreCase));
        ArgumentNullException.ThrowIfNull(property);
        if (!property.Name.Contains('=', StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Property {propertyIdentifer} is not in the correct format");
        return property?.Name.Split("=")[1] ?? string.Empty;
    }
    private static int GetPropertyValueFromUsingAsInt(CodeFunction codeElement, string propertyIdentifer)
    {
        var property = codeElement.Usings.FirstOrDefault(x => x.Name.StartsWith(propertyIdentifer, StringComparison.OrdinalIgnoreCase));
        ArgumentNullException.ThrowIfNull(property);
        if (!property.Name.Contains('=', StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Property {propertyIdentifer} is not in the correct format");
        return int.Parse(property?.Name.Split("=")[1] ?? string.Empty, System.Globalization.CultureInfo.InvariantCulture);
    }
}

public partial class AppJsonTemplate
{
    [JsonPropertyName("id")]
    public Guid Id
    {
        get; set;
    }
    [JsonPropertyName("name")]
    public string Name
    {
        get; set;
    }
    [JsonPropertyName("publisher")]
    public string Publisher
    {
        get; set;
    }
    [JsonPropertyName("version")]
    public string Version
    {
        get; set;
    }
    [JsonPropertyName("description")]
    public string Description
    {
        get; set;
    }
    [JsonPropertyName("brief")]
    public string Brief
    {
        get; set;
    }
    [JsonPropertyName("privacyStatement")]
    public Uri PrivacyStatement
    {
        get; set;
    }
    [JsonPropertyName("EULA")]
    public Uri Eula
    {
        get; set;
    }
    [JsonPropertyName("help")]
    public Uri Help
    {
        get; set;
    }
    [JsonPropertyName("url")]
    public Uri Url
    {
        get; set;
    }
    [JsonPropertyName("contextSensitiveHelpUrl")]
    public Uri ContextSensitiveHelpUrl
    {
        get; set;
    }
    [JsonPropertyName("logo")]
    public string Logo
    {
        get; set;
    }
    [JsonPropertyName("propagateDependencies")]
    public bool PropagateDependencies
    {
        get; set;
    }
    [JsonPropertyName("dependencies")]
    public ReadOnlyCollection<AppJsonDependency> Dependencies
    {
        get; set;
    }
    [JsonPropertyName("screenshots")]
    public ReadOnlyCollection<string> Screenshots
    {
        get;
    }
    [JsonPropertyName("idRanges")]
    public ReadOnlyCollection<AppJsonIdRange> IdRanges
    {
        get; set;
    }
    [JsonPropertyName("application")]
    public string Application
    {
        get; set;
    }
    [JsonPropertyName("platform")]
    public string Platform
    {
        get; set;
    }
    [JsonPropertyName("features")]
    public ReadOnlyCollection<string> Features
    {
        get; set;
    }
    [JsonPropertyName("runtime")]
    public string Runtime
    {
        get; set;
    }
    [JsonPropertyName("target")]
    public string Target
    {
        get; set;
    }
    [JsonPropertyName("supportedLocales")]
    public ReadOnlyCollection<string> SupportedLocales
    {
        get; set;
    }
    [JsonPropertyName("resourceExposurePolicy")]
    public AppJsonResourceExposurePolicy ResourceExposurePolicy
    {
        get; set;
    }


    public AppJsonTemplate(CodeFunction codeElement, AppJsonIdRange idRange, string name, string publisher, string version, string description, string brief)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        Id = Guid.NewGuid();
        Id = Guid.Parse("55e8e560-32f8-4124-ae9d-1c45c6a8be2b"); // TODO-SF: remove, only for debugging
        Name = name;
        Publisher = publisher;
        Version = version;
        Description = description;
        Brief = brief;
        PrivacyStatement = new Uri("https://www.providersample.com/data-protection");
        Eula = new Uri("https://www.providersample.com/eula");
        Help = new Uri("https://www.providersample.com/help");
        Url = new Uri("https://www.providersample.com");
        ContextSensitiveHelpUrl = new Uri("https://www.providersample.com/help");
        Logo = "";
        PropagateDependencies = false;
        Dependencies = new ReadOnlyCollection<AppJsonDependency>(new List<AppJsonDependency>() { new AppJsonDependency() });
        Screenshots = new ReadOnlyCollection<string>(new List<string>());
        IdRanges = new ReadOnlyCollection<AppJsonIdRange>(new List<AppJsonIdRange> { idRange });
        Application = "25.0.0.0";
        Platform = "25.0.0.0";
        Features = new ReadOnlyCollection<string>(new List<string>() { "NoImplicitWith" });
        Runtime = "14.0";
        Target = "Cloud";
        SupportedLocales = new ReadOnlyCollection<string>(new List<string>() { "en-US" });
        ResourceExposurePolicy = new AppJsonResourceExposurePolicy();
    }
}
public partial class AppJsonIdRange
{
    [JsonPropertyName("from")]
    public int From
    {
        get; set;
    }
    [JsonPropertyName("to")]
    public int To
    {
        get; set;
    }
    public AppJsonIdRange(int from, int to)
    {
        From = from;
        To = to;
    }
}
public partial class AppJsonDependency
{
    [JsonPropertyName("id")]
    public Guid Id
    {
        get; set;
    }
    [JsonPropertyName("name")]
    public string Name
    {
        get; set;
    }
    [JsonPropertyName("publisher")]
    public string Publisher
    {
        get; set;
    }
    [JsonPropertyName("version")]
    public string Version
    {
        get; set;
    }
    public AppJsonDependency()
    {
        // default dependency
        Id = System.Guid.Parse("c24a2609-e5c2-4702-b734-db13e5a6594c");
        Name = "Kiota.Abstractions";
        Publisher = "SimonOfHH";
        Version = "1.0.0.0";
    }
}
public partial class AppJsonResourceExposurePolicy
{
    [JsonPropertyName("allowDebugging")]
    public bool AllowDebugging
    {
        get; set;
    }
    [JsonPropertyName("allowDownloadingSource")]
    public bool AllowDownloadingSource
    {
        get; set;
    }
    [JsonPropertyName("includeSourceInSymbolFile")]
    public bool IncludeSourceInSymbolFile
    {
        get; set;
    }
    public AppJsonResourceExposurePolicy()
    {
        AllowDebugging = true;
        AllowDownloadingSource = true;
        IncludeSourceInSymbolFile = false;
    }
}
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(AppJsonTemplate))]
public partial class AppJsonTemplateContext : JsonSerializerContext
{

}