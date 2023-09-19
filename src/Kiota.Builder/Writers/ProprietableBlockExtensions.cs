using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers;

internal static class ProprietableBlockExtensions
{
    private static readonly Func<CodeProperty, bool> isCustomProperty = x => x.Kind is CodePropertyKind.Custom;
    private static readonly Func<CodeProperty, bool> isPrimaryErrorMessage = x => isCustomProperty(x) && x.IsPrimaryErrorMessage;
    private static readonly Func<CodeMethod, bool> isGetterMethod = x => x.Kind is CodeMethodKind.Getter;
    internal static string GetPrimaryMessageCodePath<TBlockKind, TBlockDeclaration>(this ProprietableBlock<TBlockKind, TBlockDeclaration> block,
        Func<CodeProperty, string> propertyNameNormalization,
        Func<CodeMethod, string> methodNameNormalization,
        string pathSegment = ".",
        HashSet<CodeElement>? visitedElements = default) where TBlockKind : Enum where TBlockDeclaration : ProprietableBlockDeclaration, new()
    {
        visitedElements ??= new();
        if (visitedElements.Contains(block))
            return string.Empty;
        else
            visitedElements.Add(block);
        if (block is CodeInterface currentInterface)
        {
            if (currentInterface.Methods.FirstOrDefault(static x => isGetterMethod(x) && x.AccessedProperty is not null && isPrimaryErrorMessage(x.AccessedProperty)) is CodeMethod primaryErrorMessageMethod)
                return methodNameNormalization(primaryErrorMessageMethod);
            else if (currentInterface.Properties.FirstOrDefault(isPrimaryErrorMessage) is CodeProperty primaryErrorMessageProperty)
                return propertyNameNormalization(primaryErrorMessageProperty);
            else if (currentInterface.Methods
                                        .Where(isGetterMethod)
                                        .Select(x => new { Value = x.ReturnType is CodeType codeType && codeType.TypeDefinition is CodeInterface codeInterface && codeInterface.GetPrimaryMessageCodePath(propertyNameNormalization, methodNameNormalization, pathSegment, visitedElements) is string segment && !string.IsNullOrEmpty(segment) ? $"{methodNameNormalization(x)}{pathSegment}{segment}" : string.Empty, IsMethod = true })
                                        .Union(currentInterface.Properties
                                                .Where(isCustomProperty)
                                                .Select(x => new { Value = x.Type is CodeType codeType && codeType.TypeDefinition is CodeInterface codeInterface && codeInterface.GetPrimaryMessageCodePath(propertyNameNormalization, methodNameNormalization, pathSegment, visitedElements) is string segment && !string.IsNullOrEmpty(segment) ? $"{propertyNameNormalization(x)}{pathSegment}{segment}" : string.Empty, IsMethod = false }))
                                        .OrderBy(static x => x.IsMethod)
                                        .ThenBy(static x => x.Value, StringComparer.OrdinalIgnoreCase)
                                        .FirstOrDefault(static x => !string.IsNullOrEmpty(x.Value)) is { } primaryMessageCodePath)
                return primaryMessageCodePath.Value;

        }
        else if (block is CodeClass currentClass)
        {
            if (currentClass.Methods.FirstOrDefault(static x => isGetterMethod(x) && x.AccessedProperty is not null && isPrimaryErrorMessage(x.AccessedProperty)) is CodeMethod primaryErrorMessageMethod)
                return methodNameNormalization(primaryErrorMessageMethod);
            else if (currentClass.Properties.FirstOrDefault(isPrimaryErrorMessage) is CodeProperty primaryErrorMessageProperty)
                return propertyNameNormalization(primaryErrorMessageProperty);
            else if (currentClass.Methods
                                        .Where(isGetterMethod)
                                        .Select(x => new { Value = x.ReturnType is CodeType codeType && codeType.TypeDefinition is CodeClass codeClass && codeClass.GetPrimaryMessageCodePath(propertyNameNormalization, methodNameNormalization, pathSegment, visitedElements) is string segment && !string.IsNullOrEmpty(segment) ? $"{methodNameNormalization(x)}{pathSegment}{segment}" : string.Empty, IsMethod = true })
                                        .Union(currentClass.Properties
                                                .Where(isCustomProperty)
                                                .Select(x => new { Value = x.Type is CodeType codeType && codeType.TypeDefinition is CodeClass codeClass && codeClass.GetPrimaryMessageCodePath(propertyNameNormalization, methodNameNormalization, pathSegment, visitedElements) is string segment && !string.IsNullOrEmpty(segment) ? $"{propertyNameNormalization(x)}{pathSegment}{segment}" : string.Empty, IsMethod = false }))
                                        .OrderBy(static x => x.IsMethod)
                                        .ThenBy(static x => x.Value, StringComparer.OrdinalIgnoreCase)
                                        .FirstOrDefault(static x => !string.IsNullOrEmpty(x.Value)) is { } primaryMessageCodePath)
                return primaryMessageCodePath.Value;
        }
        return string.Empty;
    }
}
