using System;
using System.Collections.Generic;

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

    public DeprecationInformation? Deprecation
    {
        get; set;
    }

    public static CodeInterface FromRequestBuilder(CodeClass codeClass, CodeUsing[]? usingsToAdd = default)
    {
        ArgumentNullException.ThrowIfNull(codeClass);
        if (codeClass.Kind != CodeClassKind.RequestBuilder)
            throw new InvalidOperationException($"Cannot create a request builder interface from a non request builder class");

        var result = new CodeInterface
        {
            Name = codeClass.Name,
            Kind = CodeInterfaceKind.RequestBuilder,
            OriginalClass = codeClass,
            Documentation = codeClass.Documentation,
            Deprecation = codeClass.Deprecation,
        };

        // Copy methods
        var methodsToCopy = new List<CodeMethod>();
        foreach (var method in codeClass.Methods)
        {
            if (method.Kind is CodeMethodKind.RequestGenerator or
                CodeMethodKind.RequestExecutor or
                CodeMethodKind.IndexerBackwardCompatibility or
                CodeMethodKind.RequestBuilderWithParameters)
            {
                methodsToCopy.Add((CodeMethod)method.Clone());
            }
        }
        if (methodsToCopy.Count > 0)
        {
            result.AddMethod(methodsToCopy.ToArray());
        }

        // Copy properties
        var propertiesToCopy = new List<CodeProperty>();
        foreach (var property in codeClass.Properties)
        {
            if (property.Kind is CodePropertyKind.RequestBuilder)
            {
                propertiesToCopy.Add((CodeProperty)property.Clone());
            }
        }
        if (propertiesToCopy.Count > 0)
        {
            result.AddProperty(propertiesToCopy.ToArray());
        }

        // Copy usings
        var usingsToCopy = new List<CodeUsing>();
        foreach (var usingToCopy in codeClass.Usings)
        {
            if (usingToCopy.Declaration?.TypeDefinition is CodeInterface or CodeClass { Kind: CodeClassKind.RequestBuilder })
            {
                usingToCopy.IsErasable = true;
                usingsToCopy.Add(usingToCopy);
            }
        }
        if (usingsToCopy.Count > 0)
        {
            result.AddUsing(usingsToCopy.ToArray());
        }

        // Add additional usings
        if (usingsToAdd is { Length: > 0 } usingsToAddList)
        {
            result.AddUsing(usingsToAddList);
        }

        // Add inherited interface
        if (codeClass.StartBlock.Inherits is not null)
        {
            result.StartBlock.AddImplements(codeClass.StartBlock.Inherits);
        }

        return result;
    }

}

public class InterfaceDeclaration : ProprietableBlockDeclaration
{
}
