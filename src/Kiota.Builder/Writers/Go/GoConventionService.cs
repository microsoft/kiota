using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Kiota.Builder.Refiners;

namespace Kiota.Builder.Writers.Go;

public class GoConventionService : CommonLanguageConventionService
{
    public override string StreamTypeName => "[]byte";

    public override string VoidTypeName => string.Empty;

    public override string DocCommentPrefix => "// ";
    public override string ParseNodeInterfaceName => "ParseNode";
#pragma warning disable CA1822 // Method should be static
    public string AbstractionsHash => "i2ae4187f7daee263371cb1c977df639813ab50ffa529013b7437480d1ec0158f";
    public string SerializationHash => "i878a80d2330e89d26896388a3f487eef27b0a0e6c010c493bf80be1452208f91";
    public string StoreHash => "ie8677ce2c7e1b4c22e9c3827ecd078d41185424dd9eeb92b7d971ed2d49a392e";
    public string StringsHash => "ie967d16dae74a49b5e0e051225c5dac0d76e5e38f13dd1628028cbce108c25b6";

    public string ContextVarTypeName => "context.Context";

#pragma warning restore CA1822 // Method should be static
    public override string GetAccessModifier(AccessModifier access)
    {
        throw new InvalidOperationException("go uses a naming convention for access modifiers");
    }
    public override string GetParameterSignature(CodeParameter parameter, CodeElement targetElement, LanguageWriter? writer = null)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        return $"{parameter.Name.ToFirstCharacterLowerCase()} {GetTypeString(parameter.Type, targetElement)}";
    }
    private const char Dot = '.';
    public string GetImportedStaticMethodName(CodeTypeBase code, CodeElement targetElement, string methodPrefix = "New", string methodSuffix = "", string trimEnd = "")
    {
        var typeString = GetTypeString(code, targetElement, false, false)?.Split(Dot);
        var importSymbol = typeString == null || typeString.Length < 2 ? string.Empty : typeString[0] + Dot;
        var methodName = typeString?.Last().ToFirstCharacterUpperCase();
        if (!string.IsNullOrEmpty(trimEnd) && (methodName?.EndsWith(trimEnd, StringComparison.OrdinalIgnoreCase) ?? false))
        {
            methodName = methodName[..^trimEnd.Length];
        }
        return $"{importSymbol}{methodPrefix}{methodName}{methodSuffix}";
    }
    public override string GetTypeString(CodeTypeBase code, CodeElement targetElement, bool includeCollectionInformation = true, LanguageWriter? writer = null) =>
        GetTypeString(code, targetElement, includeCollectionInformation, true);
    public string GetTypeString(CodeTypeBase code, CodeElement targetElement, bool includeCollectionInformation, bool addPointerSymbol, bool includeImportSymbol = true)
    {
        ArgumentNullException.ThrowIfNull(targetElement);
        if (code is CodeComposedTypeBase)
            throw new InvalidOperationException($"Go does not support union types, the union type {code.Name} should have been filtered out by the refiner");
        if (code is CodeType currentType)
        {
            var importSymbol = includeImportSymbol ? GetImportSymbol(code, targetElement) : string.Empty;
            if (!string.IsNullOrEmpty(importSymbol))
                importSymbol += ".";
            var typeName = TranslateType(currentType, includeImportSymbol);
            var nullableSymbol = addPointerSymbol &&
                                 currentType.IsNullable &&
                                 currentType.TypeDefinition is not CodeInterface &&
                                 currentType.CollectionKind == CodeTypeBase.CodeTypeCollectionKind.None &&
                                 !currentType.Name.Equals(GoRefiner.UntypedNodeName, StringComparison.OrdinalIgnoreCase) &&
                                 !IsScalarType(currentType.Name) ? "*"
                : string.Empty;
            var collectionPrefix = currentType.CollectionKind switch
            {
                CodeTypeBase.CodeTypeCollectionKind.Array or CodeTypeBase.CodeTypeCollectionKind.Complex when includeCollectionInformation => "[]",
                _ => string.Empty,
            };
            var genericTypeParameters = currentType.GenericTypeParameterValues.Any() ?
                            $"[{string.Join(",", currentType.GenericTypeParameterValues.Select(x => GetTypeString(x, targetElement, true, false, true)))}]" :
                            string.Empty;
            if (currentType.ActionOf)
                return $"func (value {nullableSymbol}{collectionPrefix}{importSymbol}{typeName}{genericTypeParameters}) (err error)";
            return $"{nullableSymbol}{collectionPrefix}{importSymbol}{typeName}{genericTypeParameters}";
        }

        throw new InvalidOperationException($"type of type {code?.GetType()} is unknown");
    }

    public override string TranslateType(CodeType type) => throw new InvalidOperationException("use the overload instead.");
#pragma warning disable CA1822 // Method should be static
    public string TranslateType(CodeTypeBase type, bool includeImportSymbol)
    {
        ArgumentNullException.ThrowIfNull(type);
        if (type.Name.StartsWith("map[", StringComparison.Ordinal)) return type.Name; //casing hack

        return type.Name switch
        {
            "void" => string.Empty,
            "float" => "float32",
            "integer" => "int32",
            "long" => "int64",
            "double" or "decimal" => "float64", //decimal should be float128
            "byte" => "byte",
            "sbyte" => "int8",
            "boolean" => "bool",
            "Guid" or "UUID" when includeImportSymbol => "i561e97a8befe7661a44c8f54600992b4207a3a0cf6770e5559949bc276de2e22.UUID",
            "Guid" or "UUID" when !includeImportSymbol => "UUID",
            "DateTimeOffset" or "Time" when includeImportSymbol => "i336074805fc853987abe6f7fe3ad97a6a6f3077a16391fec744f671a015fbd7e.Time",
            "DateTimeOffset" or "Time" when !includeImportSymbol => "Time",
            "DateOnly" or "TimeOnly" or "ISODuration" when includeImportSymbol => $"{SerializationHash}.{type.Name}",
            "DateOnly" or "TimeOnly" or "ISODuration" when !includeImportSymbol => type.Name,
            "binary" or "base64" or "base64url" => "[]byte",
            "string" or "float32" or "float64" or "int32" or "int64" => type.Name,
            "String" or "Int64" or "Int32" or "Float32" or "Float64" => type.Name.ToFirstCharacterLowerCase(), //casing hack
            "context.Context" => "context.Context",
            "BackedModel" => $"{StoreHash}.BackedModel",
            "" or null => "Object",
            _ => type.Name.ToFirstCharacterUpperCase(),
        };
    }
    public bool IsPrimitiveType(string typeName)
    {
        return typeName.TrimCollectionAndPointerSymbols().TrimPackageReference() switch
        {
            "void" or "string" or "float" or "integer" or "long" or "double" or "boolean" or "Guid" or "DateTimeOffset"
            or "bool" or "int32" or "int64" or "float32" or "float64" or "UUID" or "Time" or "decimal" or "TimeOnly"
            or "DateOnly" or "ISODuration" or "uint8" => true,
            "byte" when !typeName.StartsWith("[]", StringComparison.OrdinalIgnoreCase) => true,
            _ => false,
        };
    }
    public bool IsScalarType(string typeName)
    {
        if (typeName?.StartsWith("map[", StringComparison.Ordinal) ?? false) return true;
        return typeName?.ToLowerInvariant() switch
        {
            "binary" or "base64" or "base64url" or "void" or "[]byte" => true,
            _ => false,
        };
    }
#pragma warning restore CA1822 // Method should be static
    private string GetImportSymbol(CodeTypeBase currentBaseType, CodeElement targetElement)
    {
        if (currentBaseType == null || IsPrimitiveType(currentBaseType.Name)) return string.Empty;
        var targetNamespace = targetElement.GetImmediateParentOfType<CodeNamespace>();
        if (currentBaseType is CodeType currentType)
        {
            if (currentType.TypeDefinition is IProprietableBlock currentTypDefinition &&
               currentTypDefinition.Parent is CodeNamespace typeDefNS &&
               targetNamespace != typeDefNS)
                return typeDefNS.GetNamespaceImportSymbol();
            if (currentType.TypeDefinition is IProprietableBlock typeDefinition &&
                typeDefinition.Parent is CodeFile codeFile &&
                codeFile.Parent is CodeNamespace fileTypeDefNS &&
                targetNamespace != fileTypeDefNS)
                return fileTypeDefNS.GetNamespaceImportSymbol();
            if (currentType.TypeDefinition is CodeEnum currentEnumDefinition &&
               currentEnumDefinition.Parent is CodeNamespace enumNS &&
               targetNamespace != enumNS)
                return enumNS.GetNamespaceImportSymbol();
            if (currentType.TypeDefinition is null &&
               targetElement is IProprietableBlock targetTypeDef)
            {
                var symbolUsing = ((targetTypeDef.Parent as CodeClass)?.StartBlock as BlockDeclaration ??
                                   (targetTypeDef as CodeClass)?.StartBlock as BlockDeclaration ??
                                   (targetTypeDef as CodeInterface)?.StartBlock)?
                    .Usings
                    .FirstOrDefault(x => currentBaseType.Name?.Equals(x.Name, StringComparison.OrdinalIgnoreCase) ?? false);
                return symbolUsing?.Declaration?.Name.GetNamespaceImportSymbol() ?? string.Empty;
            }
        }
        return string.Empty;
    }

    public override bool WriteShortDescription(IDocumentedElement element, LanguageWriter writer, string prefix = "", string suffix = "")
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(element);
        if (!element.Documentation.DescriptionAvailable) return false;
        if (element is not CodeElement codeElement) return false;

        var description = element.Documentation.GetDescription(x => GetTypeString(x, codeElement, true, false));
        if (!string.IsNullOrEmpty(prefix))
        {
            description = description.ToFirstCharacterLowerCase();
        }
        WriteDescriptionItem($"{prefix}{description}{suffix}", writer);
        return true;
    }
    public void WriteGeneratorComment(LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteLine($"{DocCommentPrefix}Code generated by Microsoft Kiota - DO NOT EDIT.");
        writer.WriteLine($"{DocCommentPrefix}Changes may cause incorrect behavior and will be lost if the code is regenerated.");
        writer.WriteLine(string.Empty);
    }
    public void WriteDescriptionItem(string description, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteLine($"{DocCommentPrefix}{description}");
    }
    public void WriteLinkDescription(CodeDocumentation documentation, LanguageWriter writer)
    {
        if (documentation is null) return;
        if (documentation.ExternalDocumentationAvailable)
        {
            WriteDescriptionItem($"[{documentation.DocumentationLabel}]", writer);
            WriteDescriptionItem(string.Empty, writer);
            WriteDescriptionItem($"[{documentation.DocumentationLabel}]: {documentation.DocumentationLink}", writer);
        }
    }
#pragma warning disable CA1822 // Method should be static
    internal void AddRequestBuilderBody(CodeClass parentClass, string returnType, LanguageWriter writer, string? urlTemplateVarName = default, IEnumerable<CodeParameter>? pathParameters = default)
    {
        if (parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter) is not CodeProperty requestAdapterProp) return;
        var urlTemplateParams = string.IsNullOrEmpty(urlTemplateVarName) &&
                                parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters) is CodeProperty urlTemplateParamsProp ?
                                    $"m.BaseRequestBuilder.{urlTemplateParamsProp.Name.ToFirstCharacterUpperCase()}" :
                                    (urlTemplateVarName ?? string.Empty);
        var splatImport = returnType.Split('.');
        var constructorName = splatImport.Last().TrimCollectionAndPointerSymbols().ToFirstCharacterUpperCase();
        var moduleName = splatImport.Length > 1 ? $"{splatImport.First().TrimStart('*')}." : string.Empty;
        pathParameters ??= Enumerable.Empty<CodeParameter>();
        var pathParametersSuffix = pathParameters.Any() ? $", {string.Join(", ", pathParameters.Select(x => $"{x.Name.ToFirstCharacterLowerCase()}"))}" : string.Empty;
        writer.WriteLines($"return {moduleName}New{constructorName}Internal({urlTemplateParams}, m.BaseRequestBuilder.{requestAdapterProp.Name.ToFirstCharacterUpperCase()}{pathParametersSuffix})");
    }
    public override string TempDictionaryVarName => "urlTplParams";
    internal void AddParametersAssignment(LanguageWriter writer, CodeTypeBase? pathParametersType, string pathParametersReference, string pathParametersTarget, bool copyMap, params (CodeTypeBase, string, string)[] parameters)
    {
        if (pathParametersType == null) return;
        if (string.IsNullOrEmpty(pathParametersTarget))
        {
            pathParametersTarget = TempDictionaryVarName;
            writer.WriteLine($"{pathParametersTarget} := make({pathParametersType.Name})");
        }
        if (copyMap)
        {
            writer.StartBlock($"for idx, item := range {pathParametersReference} {{");
            writer.WriteLine($"{pathParametersTarget}[idx] = item");
            writer.CloseBlock();
        }
        foreach (var p in parameters)
        {
            var isStringStruct = !p.Item1.IsNullable && p.Item1.Name.Equals("string", StringComparison.OrdinalIgnoreCase);
            var (defaultValue, pointerDereference, shouldCheckNullability) = (isStringStruct, p.Item1.IsNullable) switch
            {
                (true, _) => ("\"\"", string.Empty, true),
                (_, true) => ("nil", "*", true),
                (_, false) => (string.Empty, string.Empty, false),
            };
            if (shouldCheckNullability)
                writer.StartBlock($"if {p.Item3} != {defaultValue} {{");
            writer.WriteLine($"{pathParametersTarget}[\"{p.Item2}\"] = {GetValueStringConversion(p.Item1.Name, pointerDereference + p.Item3)}");
            if (shouldCheckNullability)
                writer.CloseBlock();
        }
    }
#pragma warning restore CA1822 // Method should be static
    internal const string StrConvHash = "i53ac87e8cb3cc9276228f74d38694a208cacb99bb8ceb705eeae99fb88d4d274";
    private const string TimeFormatHash = "i336074805fc853987abe6f7fe3ad97a6a6f3077a16391fec744f671a015fbd7e";
    private static string GetValueStringConversion(string typeName, string reference)
    {
        return typeName switch
        {
            "boolean" => $"{StrConvHash}.FormatBool({reference})",
            "int64" => $"{StrConvHash}.FormatInt({reference}, 10)",
            "integer" or "int32" => $"{StrConvHash}.FormatInt(int64({reference}), 10)",
            "long" => $"{StrConvHash}.FormatInt({reference}, 10)",
            "float" or "double" or "decimal" or "float64" or "float32" => $"{StrConvHash}.FormatFloat({reference}, 'E', -1, 64)",
            "DateTimeOffset" or "Time" => $"({reference}).Format({TimeFormatHash}.RFC3339)", // default to using ISO 8601
            "ISODuration" or "TimeSpan" or "TimeOnly" or "DateOnly" => $"({reference}).String()",
            "Guid" or "UUID" => $"{reference}.String()",
            _ => reference,
        };
    }
    internal void WriteDeprecation(IDeprecableElement element, LanguageWriter writer)
    {
        if (element.Deprecation is null || !element.Deprecation.IsDeprecated) return;

        var versionComment = string.IsNullOrEmpty(element.Deprecation.Version) ? string.Empty : $" as of {element.Deprecation.Version}";
        var dateComment = element.Deprecation.Date is null ? string.Empty : $" on {element.Deprecation.Date.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";
        var removalComment = element.Deprecation.RemovalDate is null ? string.Empty : $" and will be removed {element.Deprecation.RemovalDate.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";
        WriteDescriptionItem($"Deprecated: {element.Deprecation.GetDescription(type => GetTypeString(type, (element as CodeElement)!).TrimStart('*'))}{versionComment}{dateComment}{removalComment}", writer);
    }
}
