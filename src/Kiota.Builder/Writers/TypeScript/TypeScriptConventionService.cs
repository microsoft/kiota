﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using static Kiota.Builder.CodeDOM.CodeTypeBase;
using static Kiota.Builder.Refiners.TypeScriptRefiner;

namespace Kiota.Builder.Writers.TypeScript;
public class TypeScriptConventionService : CommonLanguageConventionService
{
#pragma warning disable CA1707 // Remove the underscores
    public const string TYPE_INTEGER = "integer";
    public const string TYPE_INT64 = "int64";
    public const string TYPE_FLOAT = "float";
    public const string TYPE_DOUBLE = "double";
    public const string TYPE_BYTE = "byte";
    public const string TYPE_SBYTE = "sbyte";
    public const string TYPE_DECIMAL = "decimal";
    public const string TYPE_BINARY = "binary";
    public const string TYPE_BASE64 = "base64";
    public const string TYPE_BASE64URL = "base64url";
    public const string TYPE_GUID = "Guid";
    public const string TYPE_STRING = "String";
    public const string TYPE_OBJECT = "Object";
    public const string TYPE_BOOLEAN = "Boolean";
    public const string TYPE_VOID = "Void";
    public const string TYPE_LOWERCASE_STRING = "string";
    public const string TYPE_LOWERCASE_OBJECT = "object";
    public const string TYPE_LOWERCASE_BOOLEAN = "boolean";
    public const string TYPE_LOWERCASE_VOID = "void";
    public const string TYPE_BYTE_ARRAY = "byte[]";
    public const string TYPE_NUMBER = "number";
    public const string TYPE_DATE = "Date";
    public const string TYPE_DATE_ONLY = "DateOnly";
    public const string TYPE_TIME_ONLY = "TimeOnly";
    public const string TYPE_DURATION = "Duration";
#pragma warning restore CA1707 // Remove the underscores

    internal void WriteAutoGeneratedStart(LanguageWriter writer)
    {
        writer.WriteLine("/* tslint:disable */");
        writer.WriteLine("/* eslint-disable */");
        writer.WriteLine("// Generated by Microsoft Kiota");
    }
    internal void WriteAutoGeneratedEnd(LanguageWriter writer)
    {
        writer.WriteLine("/* tslint:enable */");
        writer.WriteLine("/* eslint-enable */");
    }
    public override string StreamTypeName => "ArrayBuffer";
    public override string VoidTypeName => "void";
    public override string DocCommentPrefix => " * ";
    public override string ParseNodeInterfaceName => "ParseNode";
    internal string DocCommentStart = "/**";
    internal string DocCommentEnd = " */";
    public override string TempDictionaryVarName => "urlTplParams";
    internal void AddParametersAssignment(LanguageWriter writer, CodeTypeBase pathParametersType, string pathParametersReference, string varName = "", params (CodeTypeBase, string, string)[] parameters)
    {
        if (pathParametersType == null) return;
        if (string.IsNullOrEmpty(varName))
        {
            varName = TempDictionaryVarName;
            writer.WriteLine($"const {varName} = getPathParameters({pathParametersReference});");
        }
        if (parameters.Length != 0)
            writer.WriteLines(parameters.Select(p =>
                $"{varName}[\"{p.Item2}\"] = {p.Item3}"
            ).ToArray());
    }
    public override string GetAccessModifier(AccessModifier access)
    {
        return access switch
        {
            AccessModifier.Public => "public",
            AccessModifier.Protected => "protected",
            _ => "private",
        };
    }

    private static bool ShouldIncludeCollectionInformationForParameter(CodeParameter parameter)
    {
        return !(GetOriginalComposedType(parameter) is not null
            && parameter.Parent is CodeMethod codeMethod
            && (codeMethod.IsOfKind(CodeMethodKind.Serializer) || codeMethod.IsOfKind(CodeMethodKind.Deserializer)));
    }

    public override string GetParameterSignature(CodeParameter parameter, CodeElement targetElement, LanguageWriter? writer = null)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        var includeCollectionInformation = ShouldIncludeCollectionInformationForParameter(parameter);
        var paramType = GetTypescriptTypeString(parameter.Type, targetElement, includeCollectionInformation: includeCollectionInformation, inlineComposedTypeString: true);
        var isComposedOfPrimitives = GetOriginalComposedType(parameter.Type) is CodeComposedTypeBase composedType && composedType.IsComposedOfPrimitives(IsPrimitiveType);
        var defaultValueSuffix = (string.IsNullOrEmpty(parameter.DefaultValue), parameter.Kind, isComposedOfPrimitives) switch
        {
            (false, CodeParameterKind.DeserializationTarget, false) when parameter.Parent is CodeMethod codeMethod && codeMethod.Kind is CodeMethodKind.Serializer
                => $" | null = {parameter.DefaultValue}",
            (false, CodeParameterKind.DeserializationTarget, false) => $" = {parameter.DefaultValue}",
            (false, _, false) => $" = {parameter.DefaultValue} as {paramType}",
            _ => string.Empty,
        };
        var (partialPrefix, partialSuffix) = (isComposedOfPrimitives, parameter.Kind) switch
        {
            (false, CodeParameterKind.DeserializationTarget) => ("Partial<", ">"),
            _ => (string.Empty, string.Empty),
        };
        return $"{parameter.Name.ToFirstCharacterLowerCase()}{(parameter.Optional && parameter.Type.IsNullable ? "?" : string.Empty)}: {partialPrefix}{paramType}{partialSuffix}{(parameter.Type.IsNullable ? " | undefined" : string.Empty)}{defaultValueSuffix}";
    }

    public override string GetTypeString(CodeTypeBase code, CodeElement targetElement, bool includeCollectionInformation = true, LanguageWriter? writer = null)
    {
        return GetTypescriptTypeString(code, targetElement, includeCollectionInformation);
    }

    public static string GetTypescriptTypeString(CodeTypeBase code, CodeElement targetElement, bool includeCollectionInformation = true, bool inlineComposedTypeString = false)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(targetElement);

        var collectionSuffix = code.CollectionKind == CodeTypeCollectionKind.None || !includeCollectionInformation ? string.Empty : "[]";

        var composedType = GetOriginalComposedType(code);

        if (inlineComposedTypeString && composedType?.Types.Any() == true)
        {
            return GetComposedTypeTypeString(composedType, targetElement, collectionSuffix, includeCollectionInformation: includeCollectionInformation);
        }

        CodeTypeBase codeType = composedType is not null ? new CodeType() { TypeDefinition = composedType } : code;

        if (codeType is not CodeType currentType)
        {
            throw new InvalidOperationException($"type of type {code.GetType()} is unknown");
        }

        var typeName = GetTypeAlias(currentType, targetElement) is string alias && !string.IsNullOrEmpty(alias)
            ? alias
            : TranslateTypescriptType(currentType);

        var genericParameters = currentType.GenericTypeParameterValues.Count != 0
            ? $"<{string.Join(", ", currentType.GenericTypeParameterValues.Select(x => GetTypescriptTypeString(x, targetElement, includeCollectionInformation)))}>"
            : string.Empty;

        return $"{typeName}{collectionSuffix}{genericParameters}";
    }

    /**
    * Gets the composed type string representation e.g `type1 | type2 | type3[]` or `(type1 & type2 & type3)[]`
    * @param composedType The composed type to get the string representation for
    * @param targetElement The target element
    * @returns The composed type string representation 
    */
    private static string GetComposedTypeTypeString(CodeComposedTypeBase composedType, CodeElement targetElement, string collectionSuffix, bool includeCollectionInformation = true)
    {
        if (!composedType.Types.Any())
            throw new InvalidOperationException("Composed type should be comprised of at least one type");

        var typesDelimiter = composedType is CodeUnionType or CodeIntersectionType ? " | " :
            throw new InvalidOperationException("Unknown composed type");

        var returnTypeString = string.Join(typesDelimiter, composedType.Types.Select(x => GetTypescriptTypeString(x, targetElement, includeCollectionInformation: includeCollectionInformation)));
        return collectionSuffix.Length > 0 ? $"({returnTypeString}){collectionSuffix}" : returnTypeString;
    }

    private static string GetTypeAlias(CodeType targetType, CodeElement targetElement)
    {
        var block = targetElement.GetImmediateParentOfType<IBlock>();
        var usings = block is CodeFile cf ? cf.GetChildElements(true).SelectMany(GetUsingsFromCodeElement) : block?.Usings ?? Array.Empty<CodeUsing>();
        return GetTypeAlias(targetType, usings);
    }

    private static string GetTypeAlias(CodeType targetType, IEnumerable<CodeUsing> usings)
    {
        var aliasedUsing = usings.FirstOrDefault(x => !x.IsExternal &&
                                                      x.Declaration?.TypeDefinition != null &&
                                                      x.Declaration.TypeDefinition == targetType.TypeDefinition &&
                                                      !string.IsNullOrEmpty(x.Alias));

        return aliasedUsing != null ? aliasedUsing.Alias : string.Empty;
    }

    public override string TranslateType(CodeType type)
    {
        return TranslateTypescriptType(type);
    }

    public static string TranslateTypescriptType(CodeTypeBase type)
    {
        return type?.Name switch
        {
            TYPE_INTEGER or TYPE_INT64 or TYPE_FLOAT or TYPE_DOUBLE or TYPE_BYTE or TYPE_SBYTE or TYPE_DECIMAL => TYPE_NUMBER,
            TYPE_BINARY or TYPE_BASE64 or TYPE_BASE64URL => TYPE_STRING,
            TYPE_GUID => TYPE_GUID,
            TYPE_STRING or TYPE_OBJECT or TYPE_BOOLEAN or TYPE_VOID or TYPE_LOWERCASE_STRING or TYPE_LOWERCASE_OBJECT or TYPE_LOWERCASE_BOOLEAN or TYPE_LOWERCASE_VOID => type.Name.ToFirstCharacterLowerCase(),
            null => TYPE_OBJECT,
            _ when type is CodeComposedTypeBase composedType => composedType.Name.ToFirstCharacterUpperCase(),
            _ when type is CodeType codeType => GetCodeTypeName(codeType) is string typeName && !string.IsNullOrEmpty(typeName) ? typeName : TYPE_OBJECT,
            _ => throw new InvalidOperationException($"Unable to translate type {type.Name}")
        };
    }

    private static string GetCodeTypeName(CodeType codeType)
    {
        if (codeType.TypeDefinition is CodeFunction codeFunction)
        {
            return !string.IsNullOrEmpty(codeFunction.Name) ? codeFunction.Name : string.Empty;
        }

        return (!string.IsNullOrEmpty(codeType.TypeDefinition?.Name) ? codeType.TypeDefinition.Name : codeType.Name).ToFirstCharacterUpperCase();
    }

    public static bool IsPrimitiveType(string typeName)
    {
        return typeName switch
        {
            TYPE_NUMBER or
            TYPE_LOWERCASE_STRING or
            TYPE_STRING or
            TYPE_BYTE_ARRAY or
            TYPE_LOWERCASE_BOOLEAN or
            TYPE_BOOLEAN or
            TYPE_VOID or
            TYPE_LOWERCASE_VOID => true,
            _ => false,
        };
    }

    public static bool IsPrimitiveType(CodeType codeType, CodeComposedTypeBase codeComposedTypeBase) => IsPrimitiveType(GetTypescriptTypeString(codeType, codeComposedTypeBase));

    internal static string RemoveInvalidDescriptionCharacters(string originalDescription) => originalDescription?.Replace("\\", "/", StringComparison.OrdinalIgnoreCase) ?? string.Empty;
    public override bool WriteShortDescription(IDocumentedElement element, LanguageWriter writer, string prefix = "", string suffix = "")
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(element);
        if (!element.Documentation.DescriptionAvailable) return false;
        if (element is not CodeElement codeElement) return false;

        var description = element.Documentation.GetDescription(type => GetTypeString(type, codeElement), ReferenceTypePrefix, ReferenceTypeSuffix, RemoveInvalidDescriptionCharacters);
        writer.WriteLine($"{DocCommentStart} {description}{DocCommentEnd}");

        return true;
    }
    internal const string ReferenceTypePrefix = "{@link ";
    internal const string ReferenceTypeSuffix = "}";
    public void WriteLongDescription(IDocumentedElement documentedElement, LanguageWriter writer, IEnumerable<string>? additionalRemarks = default)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(documentedElement);
        if (documentedElement.Documentation is null) return;
        if (documentedElement is not CodeElement codeElement) return;
        additionalRemarks ??= [];
        var remarks = additionalRemarks.Where(static x => !string.IsNullOrEmpty(x)).ToArray();
        if (documentedElement.Documentation.DescriptionAvailable || documentedElement.Documentation.ExternalDocumentationAvailable || remarks.Length != 0)
        {
            writer.WriteLine(DocCommentStart);
            if (documentedElement.Documentation.DescriptionAvailable)
            {
                var description = documentedElement.Documentation.GetDescription(type => GetTypeString(type, codeElement), ReferenceTypePrefix, ReferenceTypeSuffix, RemoveInvalidDescriptionCharacters);
                writer.WriteLine($"{DocCommentPrefix}{description}");
            }
            foreach (var additionalRemark in remarks)
                writer.WriteLine($"{DocCommentPrefix}{additionalRemark}");

            if (documentedElement is IDeprecableElement deprecableElement && GetDeprecationComment(deprecableElement) is string deprecationComment && !string.IsNullOrEmpty(deprecationComment))
                writer.WriteLine($"{DocCommentPrefix}{deprecationComment}");

            if (documentedElement.Documentation.ExternalDocumentationAvailable)
                writer.WriteLine($"{DocCommentPrefix}@see {{@link {documentedElement.Documentation.DocumentationLink}|{documentedElement.Documentation.DocumentationLabel}}}");
            writer.WriteLine(DocCommentEnd);
        }
    }
    private string GetDeprecationComment(IDeprecableElement element)
    {
        if (element.Deprecation is null || !element.Deprecation.IsDeprecated) return string.Empty;

        var versionComment = string.IsNullOrEmpty(element.Deprecation.Version) ? string.Empty : $" as of {element.Deprecation.Version}";
        var dateComment = element.Deprecation.Date is null ? string.Empty : $" on {element.Deprecation.Date.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";
        var removalComment = element.Deprecation.RemovalDate is null ? string.Empty : $" and will be removed {element.Deprecation.RemovalDate.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";
        return $"@deprecated {element.Deprecation.GetDescription(type => GetTypeString(type, (element as CodeElement)!))}{versionComment}{dateComment}{removalComment}";
    }

    public static string GetFactoryMethodName(CodeTypeBase targetClassType, CodeElement currentElement, LanguageWriter? writer = null)
    {
        var composedType = GetOriginalComposedType(targetClassType);
        string targetClassName = TranslateTypescriptType(composedType ?? targetClassType);
        var resultName = $"create{targetClassName.ToFirstCharacterUpperCase()}FromDiscriminatorValue";
        if (GetTypescriptTypeString(targetClassType, currentElement, false) is string returnType && targetClassName.EqualsIgnoreCase(returnType)) return resultName;
        if (targetClassType is CodeType currentType && currentType.TypeDefinition is CodeInterface definitionClass && GetFactoryMethod(definitionClass, resultName) is { } factoryMethod)
        {
            var methodName = GetTypescriptTypeString(new CodeType { Name = resultName, TypeDefinition = factoryMethod }, currentElement, false);
            return methodName.ToFirstCharacterUpperCase();// static function is aliased
        }
        throw new InvalidOperationException($"Unable to find factory method for {targetClassType}");
    }

    private static CodeFunction? GetFactoryMethod(CodeInterface definitionClass, string factoryMethodName)
    {
        return definitionClass.GetImmediateParentOfType<CodeNamespace>(definitionClass)?.FindChildByName<CodeFunction>(factoryMethodName);
    }

    public string GetDeserializationMethodName(CodeTypeBase codeType, CodeMethod method, bool? IsCollection = null)
    {
        ArgumentNullException.ThrowIfNull(codeType);
        ArgumentNullException.ThrowIfNull(method);
        var isCollection = IsCollection == true || codeType.IsCollection;
        var propertyType = GetTypescriptTypeString(codeType, method, false);

        CodeTypeBase _codeType = GetOriginalComposedType(codeType) is CodeComposedTypeBase composedType ? new CodeType() { Name = composedType.Name, TypeDefinition = composedType } : codeType;

        if (_codeType is CodeType currentType && !string.IsNullOrEmpty(propertyType))
        {
            return (currentType.TypeDefinition, isCollection, propertyType) switch
            {
                (CodeEnum currentEnum, _, _) when currentEnum.CodeEnumObject is not null => $"{(currentEnum.Flags || isCollection ? "getCollectionOfEnumValues" : "getEnumValue")}<{currentEnum.Name.ToFirstCharacterUpperCase()}>({currentEnum.CodeEnumObject.Name.ToFirstCharacterUpperCase()})",
                (_, _, _) when StreamTypeName.Equals(propertyType, StringComparison.OrdinalIgnoreCase) => "getByteArrayValue",
                (_, true, _) when currentType.TypeDefinition is null => $"getCollectionOfPrimitiveValues<{propertyType}>()",
                (_, true, _) => $"getCollectionOfObjectValues<{propertyType.ToFirstCharacterUpperCase()}>({GetFactoryMethodName(_codeType, method)})",
                _ => GetDeserializationMethodNameForPrimitiveOrObject(_codeType, propertyType, method)
            };
        }
        return GetDeserializationMethodNameForPrimitiveOrObject(_codeType, propertyType, method);
    }

    private static string GetDeserializationMethodNameForPrimitiveOrObject(CodeTypeBase propType, string propertyTypeName, CodeMethod method)
    {
        return propertyTypeName switch
        {
            TYPE_LOWERCASE_STRING or TYPE_LOWERCASE_BOOLEAN or TYPE_NUMBER or TYPE_GUID or TYPE_DATE or TYPE_DATE_ONLY or TYPE_TIME_ONLY or TYPE_DURATION => $"get{propertyTypeName.ToFirstCharacterUpperCase()}Value()",
            _ => $"getObjectValue<{propertyTypeName.ToFirstCharacterUpperCase()}>({GetFactoryMethodName(propType, method)})"
        };
    }
}
