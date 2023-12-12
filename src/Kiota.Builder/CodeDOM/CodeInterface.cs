using System;
using System.Linq;

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
    public CodeClass? OriginalClass
    {
        get; set;
    }
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
            result.AddUsing(usings); //TODO pass a list of external imports to remove as we create the interface
        if (usingsToAdd is { Length: > 0 } usingsToAddList)
            result.AddUsing(usingsToAddList);
        return result;
    }
}
public class InterfaceDeclaration : ProprietableBlockDeclaration
{
}
