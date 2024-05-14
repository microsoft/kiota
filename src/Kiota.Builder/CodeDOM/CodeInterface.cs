using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace Kiota.Builder.CodeDOM;

public enum CodeInterfaceKind
{
    Custom,
    Model,
    QueryParameters,
    RequestBuilder,
}

public class CodeInterface : ProprietableBlock<CodeInterfaceKind, InterfaceDeclaration>, ITypeDefinition, IDeprecableElement
{
    public required CodeClass OriginalClass
    {
        get; init;
    }
    [JsonIgnore]
    public DeprecationInformation? Deprecation
    {
        get; set;
    }

    public static CodeInterface FromRequestBuilder(CodeClass codeClass, CodeUsing[]? usingsToAdd = default)
    {
        ArgumentNullException.ThrowIfNull(codeClass);
        if (codeClass.Kind != CodeClassKind.RequestBuilder) throw new InvalidOperationException($"Cannot create a request builder interface from a non request builder class");
        var result = new CodeInterface
        {
            Name = codeClass.Name,
            Kind = CodeInterfaceKind.RequestBuilder,
            OriginalClass = codeClass,
            Documentation = codeClass.Documentation,
            Deprecation = codeClass.Deprecation,
        };

        if (codeClass.Methods
                .Where(static x => x.Kind is CodeMethodKind.RequestGenerator or
                                            CodeMethodKind.RequestExecutor or
                                            CodeMethodKind.IndexerBackwardCompatibility or
                                            CodeMethodKind.RequestBuilderWithParameters)
                .Select(static x => (CodeMethod)x.Clone()).ToArray() is { Length: > 0 } methods)
            result.AddMethod(methods);
        if (codeClass.Properties
                .Where(static x => x.Kind is CodePropertyKind.RequestBuilder)
                .Select(static x => (CodeProperty)x.Clone()).ToArray() is { Length: > 0 } properties)
            result.AddProperty(properties);

        if (codeClass.Usings.ToArray() is { Length: > 0 } usings)
        {
            foreach (var usingToCopy in usings.Where(static x => x.Declaration?.TypeDefinition is CodeInterface or CodeClass { Kind: CodeClassKind.RequestBuilder }))
            {
                usingToCopy.IsErasable = true;
            }
            result.AddUsing(usings);
        }
        if (usingsToAdd is { Length: > 0 } usingsToAddList)
            result.AddUsing(usingsToAddList);
        if (codeClass.StartBlock.Inherits is not null)
            result.StartBlock.AddImplements(codeClass.StartBlock.Inherits);
        return result;
    }
}
public class InterfaceDeclaration : ProprietableBlockDeclaration
{
}
