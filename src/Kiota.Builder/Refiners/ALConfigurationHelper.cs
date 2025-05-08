using System;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;

namespace Kiota.Builder.Refiners;
public static class ALConfigurationHelper
{
    internal static CodeNamespace GetClientNamespace(CodeElement currentElement, GenerationConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var root = GetRootNamespace(currentElement);
        var clientNamespace = root.FindNamespaceByName($"{configuration.ClientNamespaceName}");
        ArgumentNullException.ThrowIfNull(clientNamespace);
        return clientNamespace;
    }
    internal static CodeNamespace GetModelNamespace(CodeElement currentElement, GenerationConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var root = GetRootNamespace(currentElement);
        var modelNamespace = root.FindNamespaceByName($"{configuration.ClientNamespaceName}.{GenerationConfiguration.ModelsNamespaceSegmentName}");
        ArgumentNullException.ThrowIfNull(modelNamespace);
        return modelNamespace;
    }
    internal static CodeNamespace GetRootNamespace(CodeElement currentElement)
    {
        if (currentElement is CodeNamespace currentNamespace)
        {
            var root = currentNamespace.GetRootNamespace();
            return root;
        }
        else
        {
            var ns = currentElement.GetImmediateParentOfType<CodeNamespace>();
            return GetRootNamespace(ns);
        }
    }
    internal static string? GetBaseUrl(CodeElement element, GenerationConfiguration configuration)
    {
        return element.GetImmediateParentOfType<CodeNamespace>()
                      .GetRootNamespace()?
                      .FindChildByName<CodeClass>(configuration.ClientClassName)?
                      .Methods?
                      .FirstOrDefault(static x => x.IsOfKind(CodeMethodKind.ClientConstructor))?
                      .BaseUrl;
    }
}