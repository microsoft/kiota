using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Kiota.Builder.CodeDOM;

public class CodeFile : CodeBlock<CodeFileDeclaration, CodeFileBlockEnd>
{
    [JsonPropertyName("interfaces")]
    public IDictionary<string, CodeInterface>? InterfacesJSON
    {
        get => InnerChildElements.Values.OfType<CodeInterface>().ToDictionary(static x => x.Name, static x => x) is { Count: > 0 } expanded ? expanded : null;
    }
    [JsonIgnore]
    public IEnumerable<CodeInterface> Interfaces => InnerChildElements.Values.OfType<CodeInterface>().OrderBy(static x => x.Name, StringComparer.Ordinal);
    [JsonPropertyName("classes")]
    public IDictionary<string, CodeClass>? ClassesJSON
    {
        get => InnerChildElements.Values.OfType<CodeClass>().ToDictionary(static x => x.Name, static x => x) is { Count: > 0 } expanded ? expanded : null;
    }
    [JsonIgnore]
    public IEnumerable<CodeClass> Classes => InnerChildElements.Values.OfType<CodeClass>().OrderBy(static x => x.Name, StringComparer.Ordinal);
    [JsonPropertyName("enums")]
    public IDictionary<string, CodeEnum>? EnumsJSON
    {
        get => InnerChildElements.Values.OfType<CodeEnum>().ToDictionary(static x => x.Name, static x => x) is { Count: > 0 } expanded ? expanded : null;
    }
    [JsonIgnore]
    public IEnumerable<CodeEnum> Enums => InnerChildElements.Values.OfType<CodeEnum>().OrderBy(static x => x.Name, StringComparer.Ordinal);
    [JsonPropertyName("constants")]
    public IDictionary<string, CodeConstant>? ConstantsJSON
    {
        get => InnerChildElements.Values.OfType<CodeConstant>().ToDictionary(static x => x.Name, static x => x) is { Count: > 0 } expanded ? expanded : null;
    }
    [JsonIgnore]
    public IEnumerable<CodeConstant> Constants => InnerChildElements.Values.OfType<CodeConstant>().OrderBy(static x => x.Name, StringComparer.Ordinal);
    [JsonPropertyName("functions")]
    public IDictionary<string, CodeFunction>? FunctionsJSON
    {
        get => InnerChildElements.Values.OfType<CodeFunction>().ToDictionary(static x => x.Name, static x => x) is { Count: > 0 } expanded ? expanded : null;
    }
    [JsonIgnore]
    public IEnumerable<CodeFunction> Functions => InnerChildElements.Values.OfType<CodeFunction>().OrderBy(static x => x.Name, StringComparer.Ordinal);
    public IEnumerable<T> AddElements<T>(params T[] elements) where T : CodeElement
    {
        if (elements == null || elements.Any(static x => x == null))
            throw new ArgumentNullException(nameof(elements));
        if (elements.Length == 0)
            throw new ArgumentOutOfRangeException(nameof(elements));

        return AddRange(elements);
    }

    public IEnumerable<CodeUsing> AllUsingsFromChildElements => GetChildElements(true)
        .SelectMany(static x => x.GetChildElements(false))
        .OfType<ProprietableBlockDeclaration>()
        .SelectMany(static x => x.Usings);
}
public class CodeFileDeclaration : ProprietableBlockDeclaration
{
}

public class CodeFileBlockEnd : BlockEnd
{
}
