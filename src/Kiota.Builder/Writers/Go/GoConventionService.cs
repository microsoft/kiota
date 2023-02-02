﻿using System;
using System.Collections.Generic;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

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
        return $"{parameter.Name.ToFirstCharacterLowerCase()} {GetTypeString(parameter.Type, targetElement)}";
    }
    private static readonly char dot = '.';
    public string GetImportedStaticMethodName(CodeTypeBase code, CodeElement targetElement, string methodPrefix = "New", string methodSuffix = "", string trimEnd = "")
    {
        var typeString = GetTypeString(code, targetElement, false, false)?.Split(dot);
        var importSymbol = typeString == null || typeString.Length < 2 ? string.Empty : typeString.First() + dot;
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
        if (code is CodeComposedTypeBase)
            throw new InvalidOperationException($"Go does not support union types, the union type {code.Name} should have been filtered out by the refiner");
        if (code is CodeType currentType)
        {
            var importSymbol = GetImportSymbol(code, targetElement);
            if (!string.IsNullOrEmpty(importSymbol))
                importSymbol += ".";
            var typeName = TranslateType(currentType, includeImportSymbol);
            var nullableSymbol = addPointerSymbol &&
                                 currentType.IsNullable &&
                                 currentType.TypeDefinition is not CodeInterface &&
                                 currentType.CollectionKind == CodeTypeBase.CodeTypeCollectionKind.None &&
                                 !IsScalarType(currentType.Name) ? "*"
                : string.Empty;
            var collectionPrefix = currentType.CollectionKind switch
            {
                CodeTypeBase.CodeTypeCollectionKind.Array or CodeTypeBase.CodeTypeCollectionKind.Complex when includeCollectionInformation => "[]",
                _ => string.Empty,
            };
            if (currentType.ActionOf)
                return $"func (value {nullableSymbol}{collectionPrefix}{importSymbol}{typeName}) (err error)";
            return $"{nullableSymbol}{collectionPrefix}{importSymbol}{typeName}";
        }

        throw new InvalidOperationException($"type of type {code.GetType()} is unknown");
    }

    public override string TranslateType(CodeType type) => throw new InvalidOperationException("use the overload instead.");
#pragma warning disable CA1822 // Method should be static
    public string TranslateType(CodeTypeBase type, bool includeImportSymbol)
    {
        if (type.Name?.StartsWith("map[") ?? false) return type.Name; //casing hack

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
            "binary" => "[]byte",
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
        return typeName.TrimCollectionAndPointerSymbols() switch
        {
            "void" or "string" or "float" or "integer" or "long" or "double" or "boolean" or "Guid" or "DateTimeOffset"
            or "bool" or "int32" or "int64" or "float32" or "float64" or "UUID" or "Time" or "decimal" or "TimeOnly"
            or "DateOnly" or "ISODuration" or "uint8" => true,
            "byte" when !typeName.StartsWith("[]") => true,
            _ => false,
        };
    }
    public bool IsScalarType(string typeName)
    {
        if (typeName?.StartsWith("map[") ?? false) return true;
        return typeName?.ToLowerInvariant() switch
        {
            "binary" or "void" or "[]byte" => true,
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

    public override void WriteShortDescription(string description, LanguageWriter writer)
    {
        writer.WriteLine($"{DocCommentPrefix}{description}");
    }
    public void WriteLinkDescription(CodeDocumentation documentation, LanguageWriter writer)
    {
        if (documentation is null) return;
        if (documentation.ExternalDocumentationAvailable)
        {
            WriteShortDescription($"[{documentation.DocumentationLabel}]", writer);
            WriteShortDescription(string.Empty, writer);
            WriteShortDescription($"[{documentation.DocumentationLabel}]: {documentation.DocumentationLink}", writer);
        }
    }
#pragma warning disable CA1822 // Method should be static
    internal void AddRequestBuilderBody(CodeClass parentClass, string returnType, LanguageWriter writer, string? urlTemplateVarName = default, IEnumerable<CodeParameter>? pathParameters = default)
    {
        if (parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter) is not CodeProperty requestAdapterProp) return;
        var urlTemplateParams = string.IsNullOrEmpty(urlTemplateVarName) &&
                                parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters) is CodeProperty urlTemplateParamsProp ?
                                    $"m.{urlTemplateParamsProp.Name}" :
                                    (urlTemplateVarName ?? string.Empty);
        var splatImport = returnType.Split('.');
        var constructorName = splatImport.Last().TrimCollectionAndPointerSymbols().ToFirstCharacterUpperCase();
        var moduleName = splatImport.Length > 1 ? $"{splatImport.First().TrimStart('*')}." : string.Empty;
        var pathParametersSuffix = (pathParameters?.Any() ?? false) ? $", {string.Join(", ", pathParameters.Select(static x => $"{x.Name.ToFirstCharacterLowerCase()}"))}" : string.Empty;
        writer.WriteLines($"return {moduleName}New{constructorName}Internal({urlTemplateParams}, m.{requestAdapterProp.Name}{pathParametersSuffix});");
    }
    public override string TempDictionaryVarName => "urlTplParams";
    internal void AddParametersAssignment(LanguageWriter writer, CodeTypeBase? pathParametersType, string pathParametersReference, params (CodeTypeBase, string, string)[] parameters)
    {
        if (pathParametersType == null) return;
        var mapTypeName = pathParametersType.Name;
        writer.WriteLine($"{TempDictionaryVarName} := make({mapTypeName})");
        writer.StartBlock($"for idx, item := range {pathParametersReference} {{");
        writer.WriteLine($"{TempDictionaryVarName}[idx] = item");
        writer.CloseBlock();
        foreach (var p in parameters)
        {
            var isStringStruct = !p.Item1.IsNullable && p.Item1.Name.Equals("string", StringComparison.OrdinalIgnoreCase);
            var defaultValue = isStringStruct ? "\"\"" : "nil";
            var pointerDereference = isStringStruct ? string.Empty : "*";
            writer.StartBlock($"if {p.Item3} != {defaultValue} {{");
            writer.WriteLine($"{TempDictionaryVarName}[\"{p.Item2}\"] = {GetValueStringConversion(p.Item1.Name, pointerDereference + p.Item3)}");
            writer.CloseBlock();
        }
    }
#pragma warning restore CA1822 // Method should be static
    private const string StrConvHash = "i53ac87e8cb3cc9276228f74d38694a208cacb99bb8ceb705eeae99fb88d4d274";
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
            _ => reference,
        };
    }
}
