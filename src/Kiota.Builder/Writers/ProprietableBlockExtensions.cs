using System;
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
        string pathSegment = ".") where TBlockKind : Enum where TBlockDeclaration : ProprietableBlockDeclaration, new()
    {
        if (block is CodeInterface currentInterface)
        {
            if (currentInterface.Properties.FirstOrDefault(isPrimaryErrorMessage) is CodeProperty primaryErrorMessageProperty)
                return propertyNameNormalization(primaryErrorMessageProperty);
            else if (currentInterface.Methods.FirstOrDefault(x => isGetterMethod(x) && x.AccessedProperty is not null && isPrimaryErrorMessage(x.AccessedProperty)) is CodeMethod primaryErrorMessageMethod)
                return methodNameNormalization(primaryErrorMessageMethod);
            else if (currentInterface.Methods
                                        .Where(isGetterMethod)
                                        .Select(x => x.ReturnType is CodeType codeType && codeType.TypeDefinition is CodeInterface codeInterface && codeInterface.GetPrimaryMessageCodePath(propertyNameNormalization, methodNameNormalization, pathSegment) is string segment && !string.IsNullOrEmpty(segment) ? $"{methodNameNormalization(x)}{pathSegment}{segment}" : string.Empty)
                                        .Union(currentInterface.Properties
                                                .Where(isCustomProperty)
                                                .Select(x => x.Type is CodeType codeType && codeType.TypeDefinition is CodeInterface codeInterface && codeInterface.GetPrimaryMessageCodePath(propertyNameNormalization, methodNameNormalization, pathSegment) is string segment && !string.IsNullOrEmpty(segment) ? $"{propertyNameNormalization(x)}{pathSegment}{segment}" : string.Empty))
                                        .Order(StringComparer.OrdinalIgnoreCase)
                                        .FirstOrDefault(static x => !string.IsNullOrEmpty(x)) is string primaryMessageCodePath)
                return $"{pathSegment}{primaryMessageCodePath}";

        }
        else if (block is CodeClass currentClass)
        {
            if (currentClass.Properties.FirstOrDefault(isPrimaryErrorMessage) is CodeProperty primaryErrorMessageProperty)
                return propertyNameNormalization(primaryErrorMessageProperty);
            else if (currentClass.Methods.FirstOrDefault(x => isGetterMethod(x) && x.AccessedProperty is not null && isPrimaryErrorMessage(x.AccessedProperty)) is CodeMethod primaryErrorMessageMethod)
                return methodNameNormalization(primaryErrorMessageMethod);
            else if (currentClass.Methods
                                        .Where(isGetterMethod)
                                        .Select(x => x.ReturnType is CodeType codeType && codeType.TypeDefinition is CodeClass codeClass && codeClass.GetPrimaryMessageCodePath(propertyNameNormalization, methodNameNormalization, pathSegment) is string segment && !string.IsNullOrEmpty(segment) ? $"{methodNameNormalization(x)}{pathSegment}{segment}" : string.Empty)
                                        .Union(currentClass.Properties
                                                .Where(isCustomProperty)
                                                .Select(x => x.Type is CodeType codeType && codeType.TypeDefinition is CodeClass codeClass && codeClass.GetPrimaryMessageCodePath(propertyNameNormalization, methodNameNormalization, pathSegment) is string segment && !string.IsNullOrEmpty(segment) ? $"{propertyNameNormalization(x)}{pathSegment}{segment}" : string.Empty))
                                        .Order(StringComparer.OrdinalIgnoreCase)
                                        .FirstOrDefault(static x => !string.IsNullOrEmpty(x)) is string primaryMessageCodePath)
                return primaryMessageCodePath;
        }
        return string.Empty;
    }
}
