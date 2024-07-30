﻿using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers.Go;
using static Kiota.Builder.Refiners.TypeScriptRefiner;
using static Kiota.Builder.Writers.TypeScript.TypeScriptConventionService;

namespace Kiota.Builder.Writers.TypeScript;

public class CodeFunctionWriter(TypeScriptConventionService conventionService) : BaseElementWriter<CodeFunction, TypeScriptConventionService>(conventionService)
{
    private static readonly HashSet<string> customSerializationWriters = new(StringComparer.OrdinalIgnoreCase) { "writeObjectValue", "writeCollectionOfObjectValues" };
    private const string FactoryMethodReturnType = "((instance?: Parsable) => Record<string, (node: ParseNode) => void>)";

    public override void WriteCodeElement(CodeFunction codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        if (codeElement.OriginalLocalMethod == null) throw new InvalidOperationException($"{nameof(codeElement.OriginalLocalMethod)} should not be null");
        ArgumentNullException.ThrowIfNull(writer);

        if (codeElement.Parent is not CodeFile parentFile) throw new InvalidOperationException("the parent of a function should be a file");

        var codeMethod = codeElement.OriginalLocalMethod;
        var composedType = GetOriginalComposedType(codeMethod.ReturnType);
        var isComposedOfPrimitives = composedType is not null && composedType.IsComposedOfPrimitives();

        var returnType = codeMethod.Kind is CodeMethodKind.Factory && !isComposedOfPrimitives ?
            FactoryMethodReturnType :
            GetTypescriptTypeString(codeMethod.ReturnType, codeElement, inlineComposedTypeString: true);
        var isVoid = "void".EqualsIgnoreCase(returnType);
        CodeMethodWriter.WriteMethodDocumentationInternal(codeElement.OriginalLocalMethod, writer, isVoid, conventions);
        CodeMethodWriter.WriteMethodPrototypeInternal(codeElement.OriginalLocalMethod, writer, returnType, isVoid, conventions, true);

        writer.IncreaseIndent();

        switch (codeMethod.Kind)
        {
            case CodeMethodKind.Deserializer:
                WriteDeserializerFunction(codeElement, writer);
                break;
            case CodeMethodKind.Serializer:
                WriteSerializerFunction(codeElement, writer);
                break;
            case CodeMethodKind.Factory:
                WriteFactoryMethod(codeElement, writer);
                break;
            case CodeMethodKind.ClientConstructor:
                WriteApiConstructorBody(parentFile, codeMethod, writer);
                break;
            default: throw new InvalidOperationException("Invalid code method kind");
        }
    }

    private string GetSerializationMethodsForPrimitiveUnionTypes(CodeComposedTypeBase composedType, string parseNodeParameterName, CodeFunction codeElement, bool nodeParameterCanBeNull = true)
    {
        var optionalChainingSymbol = nodeParameterCanBeNull ? "?" : string.Empty;
        return string.Join(" ?? ", composedType.GetPrimitiveTypes().Select(x => $"{parseNodeParameterName}{optionalChainingSymbol}." + conventions.GetDeserializationMethodName(x, codeElement.OriginalLocalMethod)));
    }

    private void WriteFactoryMethodBodyForPrimitives(CodeComposedTypeBase composedType, CodeFunction codeElement, LanguageWriter writer, CodeParameter? parseNodeParameter)
    {
        ArgumentNullException.ThrowIfNull(parseNodeParameter);
        string primitiveValuesUnionString = GetSerializationMethodsForPrimitiveUnionTypes(composedType, parseNodeParameter.Name.ToFirstCharacterLowerCase(), codeElement);
        writer.WriteLine($"return {primitiveValuesUnionString};");
    }

    private static CodeParameter? GetComposedTypeParameter(CodeFunction codeElement)
    {
        return codeElement.OriginalLocalMethod.Parameters.FirstOrDefault(x => GetOriginalComposedType(x) is not null);
    }

    private void WriteComposedTypeDeserializer(CodeFunction codeElement, LanguageWriter writer, CodeParameter composedParam)
    {

        if (composedParam is null || GetOriginalComposedType(composedParam) is not { } composedType) return;

        writer.StartBlock("return {");
        // Serialization/Deserialization functions can be called for object types only
        foreach (var mappedType in composedType.GetNonPrimitiveTypes().ToArray())
        {
            var mappedTypeName = mappedType.Name.ToFirstCharacterUpperCase();
            writer.WriteLine($"...{GetFunctionName(codeElement, mappedTypeName, CodeMethodKind.Deserializer)}({composedParam.Name.ToFirstCharacterLowerCase()} as {mappedTypeName}),");
        }
        writer.CloseBlock();
    }

    private void WriteComposedTypeSerializer(CodeFunction codeElement, LanguageWriter writer, CodeParameter composedParam)
    {
        if (composedParam is null || GetOriginalComposedType(composedParam) is not { } composedType) return;

        if (composedType.IsComposedOfPrimitives())
        {
            WriteSerializationFunctionForTypeComposedOfPrimitives(composedType, composedParam, codeElement, writer);
            return;
        }

        if (composedType is CodeIntersectionType)
        {
            WriteSerializationFunctionForCodeIntersectionType(composedType, composedParam, codeElement, writer);
            return;
        }

        WriteSerializationFunctionForCodeUnionTypes(composedType, composedParam, codeElement, writer);
    }

    private void WriteSerializationFunctionForCodeIntersectionType(CodeComposedTypeBase composedType, CodeParameter composedParam, CodeFunction method, LanguageWriter writer)
    {
        // Serialization/Deserialization functions can be called for object types only
        foreach (var mappedType in composedType.GetNonPrimitiveTypes().ToArray())
        {
            var mappedTypeName = mappedType.Name.ToFirstCharacterUpperCase();
            writer.WriteLine($"{GetFunctionName(method, mappedTypeName, CodeMethodKind.Serializer)}(writer, {composedParam.Name.ToFirstCharacterLowerCase()} as {mappedTypeName});");
        }
    }

    private void WriteSerializationFunctionForCodeUnionTypes(CodeComposedTypeBase composedType, CodeParameter composedParam, CodeFunction codeElement, LanguageWriter writer)
    {
        var discriminatorInfo = codeElement.OriginalMethodParentClass.DiscriminatorInformation;
        var discriminatorPropertyName = discriminatorInfo.DiscriminatorPropertyName;

        if (string.IsNullOrEmpty(discriminatorPropertyName))
        {
            WriteBruteForceSerializationFunctionForCodeUnionType(composedType, composedParam, codeElement, writer);
            return;
        }

        var paramName = composedParam.Name.ToFirstCharacterLowerCase();
        writer.WriteLine($"if ({paramName} === undefined) return;");
        WriteDiscriminatorSwitchBlock(discriminatorInfo, paramName, codeElement, writer);
    }

    /// <summary>
    /// Writes the brute-force serialization function for a union type.
    /// </summary>
    /// <param name="composedType">The composed type representing the union.</param>
    /// <param name="composedParam">The parameter associated with the composed type.</param>
    /// <param name="codeElement">The function code element where serialization is performed.</param>
    /// <param name="writer">The language writer used to generate the code.</param>
    /// <remarks>
    /// This method handles serialization for union types when the discriminator property is missing. 
    /// In the absence of a discriminator, all possible types in the union are serialized. For example, 
    /// a Pet union defined as Cat | Dog would result in the serialization of both Cat and Dog types. 
    /// It delegates the task to the method responsible for intersection types, treating the union 
    /// similarly to an intersection in this context.
    /// </remarks>
    private void WriteBruteForceSerializationFunctionForCodeUnionType(CodeComposedTypeBase composedType, CodeParameter composedParam, CodeFunction codeElement, LanguageWriter writer)
    {
        // Delegate the serialization logic to the method handling intersection types,
        // as both require serializing all possible type variations.
        WriteSerializationFunctionForCodeIntersectionType(composedType, composedParam, codeElement, writer);
    }

    private void WriteDiscriminatorSwitchBlock(DiscriminatorInformation discriminatorInfo, string paramName, CodeFunction codeElement, LanguageWriter writer)
    {
        writer.StartBlock($"switch ({paramName}.{discriminatorInfo.DiscriminatorPropertyName}) {{");

        foreach (var mappedType in discriminatorInfo.DiscriminatorMappings)
        {
            writer.StartBlock($"case \"{mappedType.Key}\":");
            var mappedTypeName = mappedType.Value.Name.ToFirstCharacterUpperCase();
            writer.WriteLine($"{GetFunctionName(codeElement, mappedTypeName, CodeMethodKind.Serializer)}(writer, {paramName});");
            writer.WriteLine("break;");
            writer.DecreaseIndent();
        }

        writer.CloseBlock();
    }

    private void WriteSerializationFunctionForTypeComposedOfPrimitives(CodeComposedTypeBase composedType, CodeParameter composedParam, CodeFunction method, LanguageWriter writer)
    {
        var paramName = composedParam.Name.ToFirstCharacterLowerCase();
        writer.WriteLine($"if ({paramName} === undefined) return;");
        writer.StartBlock($"switch (typeof {paramName}) {{");

        foreach (var type in composedType.GetPrimitiveTypes())
        {
            WriteCaseStatementForPrimitiveTypeSerialization(type, "key", paramName, method, writer);
        }

        writer.CloseBlock();
    }

    private void WriteCaseStatementForPrimitiveTypeSerialization(CodeTypeBase type, string key, string value, CodeFunction method, LanguageWriter writer)
    {
        var nodeType = conventions.GetTypeString(type, method, false);
        var serializationName = GetSerializationMethodName(type, method.OriginalLocalMethod);
        if (string.IsNullOrEmpty(serializationName) || string.IsNullOrEmpty(nodeType)) return;

        writer.StartBlock($"case \"{nodeType}\":");
        writer.WriteLine($"writer.{serializationName}({key}, {value});");
        writer.WriteLine($"break;");
        writer.DecreaseIndent();
    }

    private static void WriteApiConstructorBody(CodeFile parentFile, CodeMethod method, LanguageWriter writer)
    {
        WriteSerializationRegistration(method.SerializerModules, writer, "registerDefaultSerializer");
        WriteSerializationRegistration(method.DeserializerModules, writer, "registerDefaultDeserializer");
        if (method.Parameters.OfKind(CodeParameterKind.RequestAdapter)?.Name.ToFirstCharacterLowerCase() is not string requestAdapterArgumentName) return;
        if (!string.IsNullOrEmpty(method.BaseUrl))
        {
            writer.StartBlock($"if ({requestAdapterArgumentName}.baseUrl === undefined || {requestAdapterArgumentName}.baseUrl === \"\") {{");
            writer.WriteLine($"{requestAdapterArgumentName}.baseUrl = \"{method.BaseUrl}\";");
            writer.CloseBlock();
        }
        writer.StartBlock($"const pathParameters: Record<string, unknown> = {{");
        writer.WriteLine($"\"baseurl\": {requestAdapterArgumentName}.baseUrl,");
        writer.CloseBlock("};");
        if (method.Parameters.OfKind(CodeParameterKind.BackingStore)?.Name is string backingStoreParameterName)
            writer.WriteLine($"{requestAdapterArgumentName}.enableBackingStore({backingStoreParameterName.ToFirstCharacterLowerCase()});");
        if (parentFile.Interfaces.FirstOrDefault(static x => x.Kind is CodeInterfaceKind.RequestBuilder) is CodeInterface codeInterface)
        {
            var navigationMetadataConstantName = parentFile.FindChildByName<CodeConstant>($"{codeInterface.Name.ToFirstCharacterUpperCase()}{CodeConstant.NavigationMetadataSuffix}", false) is { } navConstant ? navConstant.Name.ToFirstCharacterUpperCase() : "undefined";
            var requestsMetadataConstantName = parentFile.FindChildByName<CodeConstant>($"{codeInterface.Name.ToFirstCharacterUpperCase()}{CodeConstant.RequestsMetadataSuffix}", false) is { } reqConstant ? reqConstant.Name.ToFirstCharacterUpperCase() : "undefined";
            writer.WriteLine($"return apiClientProxifier<{codeInterface.Name.ToFirstCharacterUpperCase()}>({requestAdapterArgumentName}, pathParameters, {navigationMetadataConstantName}, {requestsMetadataConstantName});");
        }
    }
    private static void WriteSerializationRegistration(HashSet<string> serializationModules, LanguageWriter writer, string methodName)
    {
        if (serializationModules != null)
            foreach (var module in serializationModules)
                writer.WriteLine($"{methodName}({module});");
    }

    private void WriteFactoryMethod(CodeFunction codeElement, LanguageWriter writer)
    {
        var returnType = conventions.GetTypeString(codeElement.OriginalLocalMethod.ReturnType, codeElement);

        if (codeElement.OriginalMethodParentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForInheritedType)
            WriteDefensiveStatements(codeElement.OriginalLocalMethod, writer);
        WriteFactoryMethodBody(codeElement, returnType, writer);
    }

    private void WriteFactoryMethodBody(CodeFunction codeElement, string returnType, LanguageWriter writer)
    {
        var parseNodeParameter = codeElement.OriginalLocalMethod.Parameters.OfKind(CodeParameterKind.ParseNode);
        var composedType = GetOriginalComposedType(codeElement.OriginalLocalMethod.ReturnType);

        switch (composedType)
        {
            case CodeComposedTypeBase type when type.IsComposedOfPrimitives():
                WriteFactoryMethodBodyForPrimitives(type, codeElement, writer, parseNodeParameter);
                break;
            case CodeUnionType _ when parseNodeParameter != null:
                WriteFactoryMethodBodyForCodeUnionType(codeElement, returnType, writer, parseNodeParameter);
                break;
            case CodeIntersectionType _ when parseNodeParameter != null:
                WriteDefaultDiscriminator(codeElement, returnType, writer);
                break;
            default:
                WriteNormalFactoryMethodBody(codeElement, returnType, writer);
                break;
        }
    }

    private void WriteFactoryMethodBodyForCodeUnionType(CodeFunction codeElement, string returnType, LanguageWriter writer, CodeParameter parseNodeParameter)
    {
        WriteDiscriminatorInformation(codeElement, parseNodeParameter, writer);
        // The default discriminator is useful when the discriminator information is not provided.
        WriteDefaultDiscriminator(codeElement, returnType, writer);
    }

    private void WriteNormalFactoryMethodBody(CodeFunction codeElement, string returnType, LanguageWriter writer)
    {
        var parseNodeParameter = codeElement.OriginalLocalMethod.Parameters.OfKind(CodeParameterKind.ParseNode);
        if (ShouldWriteDiscriminatorInformation(codeElement, null) && parseNodeParameter != null)
        {
            WriteDiscriminatorInformation(codeElement, parseNodeParameter, writer);
        }
        WriteDefaultDiscriminator(codeElement, returnType, writer);
    }

    private void WriteDefaultDiscriminator(CodeFunction codeElement, string returnType, LanguageWriter writer)
    {
        var deserializationFunction = GetFunctionName(codeElement, returnType, CodeMethodKind.Deserializer);
        writer.WriteLine($"return {deserializationFunction.ToFirstCharacterLowerCase()};");
    }

    private static bool ShouldWriteDiscriminatorInformation(CodeFunction codeElement, CodeComposedTypeBase? composedType)
    {
        return codeElement.OriginalMethodParentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForInheritedType || composedType is CodeUnionType;
    }

    private void WriteDiscriminatorInformation(CodeFunction codeElement, CodeParameter parseNodeParameter, LanguageWriter writer)
    {
        var discriminatorInfo = codeElement.OriginalMethodParentClass.DiscriminatorInformation;
        var discriminatorPropertyName = discriminatorInfo.DiscriminatorPropertyName;

        if (!string.IsNullOrEmpty(discriminatorPropertyName))
        {
            writer.WriteLines($"const mappingValueNode = {parseNodeParameter.Name.ToFirstCharacterLowerCase()}?.getChildNode(\"{discriminatorPropertyName}\");",
                                "if (mappingValueNode) {");
            writer.IncreaseIndent();
            writer.WriteLines("const mappingValue = mappingValueNode.getStringValue();",
                            "if (mappingValue) {");
            writer.IncreaseIndent();

            writer.StartBlock("switch (mappingValue) {");
            foreach (var mappedType in discriminatorInfo.DiscriminatorMappings)
            {
                writer.StartBlock($"case \"{mappedType.Key}\":");
                writer.WriteLine($"return {GetFunctionName(codeElement, mappedType.Value.Name.ToFirstCharacterUpperCase(), CodeMethodKind.Deserializer)};");
                writer.DecreaseIndent();
            }
            writer.CloseBlock();
            writer.CloseBlock();
            writer.CloseBlock();
        }
    }

    private string GetFunctionName(CodeElement codeElement, string returnType, CodeMethodKind kind)
    {
        var functionName = CreateSerializationFunctionNameFromType(returnType, kind);
        var parentNamespace = codeElement.GetImmediateParentOfType<CodeNamespace>();
        var codeFunction = FindCodeFunctionInParentNamespaces(functionName, parentNamespace);
        return conventions.GetTypeString(new CodeType { TypeDefinition = codeFunction }, codeElement, false);
    }

    private CodeFunction? FindCodeFunctionInParentNamespaces(string functionName, CodeNamespace? parentNamespace)
    {
        CodeFunction? codeFunction = null;

        for (var currentNamespace = parentNamespace;
            currentNamespace is not null && !functionName.Equals(codeFunction?.Name, StringComparison.Ordinal);
            currentNamespace = currentNamespace.Parent?.GetImmediateParentOfType<CodeNamespace>())
        {
            codeFunction = currentNamespace.FindChildByName<CodeFunction>(functionName);
        }

        return codeFunction;
    }

    private static string CreateSerializationFunctionNameFromType(string returnType, CodeMethodKind functionKind)
    {
        return functionKind switch
        {
            CodeMethodKind.Serializer => $"serialize{returnType}",
            CodeMethodKind.Deserializer => $"deserializeInto{returnType}",
            _ => throw new InvalidOperationException($"Unsupported function kind :: {functionKind}")
        };
    }

    private void WriteSerializerFunction(CodeFunction codeElement, LanguageWriter writer)
    {
        // Determine if the function serializes a composed type
        var composedParam = GetComposedTypeParameter(codeElement);
        if (composedParam is not null)
        {
            WriteComposedTypeSerializer(codeElement, writer, composedParam);
            return;
        }

        if (codeElement.OriginalLocalMethod.Parameters.FirstOrDefault(static x => x.Type is CodeType type && type.TypeDefinition is CodeInterface) is not
            {
                Type: CodeType
                {
                    TypeDefinition: CodeInterface codeInterface
                }
            } param)
            throw new InvalidOperationException("Interface parameter not found for code interface");

        if (codeInterface.StartBlock.Implements.FirstOrDefault(static x => x.TypeDefinition is CodeInterface) is CodeType inherits)
        {
            writer.WriteLine($"serialize{inherits.TypeDefinition!.Name.ToFirstCharacterUpperCase()}(writer, {param.Name.ToFirstCharacterLowerCase()})");
        }

        foreach (var otherProp in codeInterface.Properties.Where(static x => x.IsOfKind(CodePropertyKind.Custom) && !x.ExistsInBaseType && !x.ReadOnly))
        {
            WritePropertySerializer(codeInterface.Name.ToFirstCharacterLowerCase(), otherProp, writer, codeElement);
        }

        if (codeInterface.GetPropertyOfKind(CodePropertyKind.AdditionalData) is CodeProperty additionalDataProperty)
            writer.WriteLine($"writer.writeAdditionalData({codeInterface.Name.ToFirstCharacterLowerCase()}.{additionalDataProperty.Name.ToFirstCharacterLowerCase()});");
    }

    private static bool IsCollectionOfEnum(CodeProperty property)
    {
        return property.Type is CodeType codeType && codeType.IsCollection && codeType.TypeDefinition is CodeEnum;
    }

    private void WritePropertySerializer(string modelParamName, CodeProperty codeProperty, LanguageWriter writer, CodeFunction codeFunction)
    {
        var isCollectionOfEnum = IsCollectionOfEnum(codeProperty);
        var spreadOperator = isCollectionOfEnum ? "..." : string.Empty;
        var codePropertyName = codeProperty.Name.ToFirstCharacterLowerCase();
        var propTypeName = GetTypescriptTypeString(codeProperty.Type, codeProperty.Parent!, false, inlineComposedTypeString: true);

        var serializationName = GetSerializationMethodName(codeProperty.Type, codeFunction.OriginalLocalMethod);
        var defaultValueSuffix = GetDefaultValueLiteralForProperty(codeProperty) is string dft && !string.IsNullOrEmpty(dft) && !dft.EqualsIgnoreCase("\"null\"") ? $" ?? {dft}" : string.Empty;

        var composedType = GetOriginalComposedType(codeProperty.Type);

        if (customSerializationWriters.Contains(serializationName) && codeProperty.Type is CodeType propType && propType.TypeDefinition is not null)
        {
            var serializeName = GetSerializerAlias(propType, codeFunction, $"serialize{propType.TypeDefinition.Name}");
            if (GetOriginalComposedType(propType.TypeDefinition) is { } ct && (ct.IsComposedOfPrimitives() || ct.IsComposedOfObjectsAndPrimitives()))
                WriteSerializationStatementForComposedTypeProperty(ct, modelParamName, codeFunction, writer, codeProperty, serializeName);
            else
                writer.WriteLine($"writer.{serializationName}<{propTypeName}>(\"{codeProperty.WireName}\", {modelParamName}.{codePropertyName}{defaultValueSuffix}, {serializeName});");
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(spreadOperator))
                writer.WriteLine($"if({modelParamName}.{codePropertyName})");
            if (composedType is not null && (composedType.IsComposedOfPrimitives() || composedType.IsComposedOfObjectsAndPrimitives()))
                WriteSerializationStatementForComposedTypeProperty(composedType, modelParamName, codeFunction, writer, codeProperty, string.Empty);
            else
                writer.WriteLine($"writer.{serializationName}(\"{codeProperty.WireName}\", {spreadOperator}{modelParamName}.{codePropertyName}{defaultValueSuffix});");
        }
    }

    private void WriteSerializationStatementForComposedTypeProperty(CodeComposedTypeBase composedType, string modelParamName, CodeFunction method, LanguageWriter writer, CodeProperty codeProperty, string? serializeName)
    {
        var isCollectionOfEnum = IsCollectionOfEnum(codeProperty);
        var spreadOperator = isCollectionOfEnum ? "..." : string.Empty;
        var codePropertyName = codeProperty.Name.ToFirstCharacterLowerCase();
        var propTypeName = GetTypescriptTypeString(codeProperty.Type, codeProperty.Parent!, false, inlineComposedTypeString: true);

        var serializationName = GetSerializationMethodName(codeProperty.Type, method.OriginalLocalMethod);
        var defaultValueSuffix = GetDefaultValueLiteralForProperty(codeProperty) is string dft && !string.IsNullOrEmpty(dft) && !dft.EqualsIgnoreCase("\"null\"") ? $" ?? {dft}" : string.Empty;

        writer.StartBlock($"switch (typeof {modelParamName}.{codePropertyName}) {{");

        foreach (var type in composedType.GetPrimitiveTypes())
        {
            WriteCaseStatementForPrimitiveTypeSerialization(type, $"\"{codeProperty.WireName}\"", $"{spreadOperator}{modelParamName}.{codePropertyName}{defaultValueSuffix}", method, writer);
        }

        if (composedType.IsComposedOfObjectsAndPrimitives())
        {
            // write the default statement serialization statement for the object
            writer.StartBlock($"default:");
            writer.WriteLine($"writer.{serializationName}<{propTypeName}>(\"{codeProperty.WireName}\", {modelParamName}.{codePropertyName}{defaultValueSuffix}, {serializeName});");
            writer.WriteLine($"break;");
            writer.DecreaseIndent();
        }

        writer.CloseBlock();
    }

    public string GetSerializationMethodName(CodeTypeBase propertyType, CodeMethod method)
    {
        ArgumentNullException.ThrowIfNull(propertyType);
        ArgumentNullException.ThrowIfNull(method);

        var composedType = GetOriginalComposedType(propertyType);
        if (composedType is not null && composedType.IsComposedOfPrimitives())
        {
            return $"serialize{composedType.Name.ToFirstCharacterUpperCase()}";
        }

        var propertyTypeName = TranslateTypescriptType(propertyType);
        CodeType? currentType = composedType is not null ? GetCodeTypeForComposedType(composedType) : propertyType as CodeType;

        if (currentType != null && !string.IsNullOrEmpty(propertyTypeName))
        {
            var result = GetSerializationMethodNameForCodeType(currentType, propertyTypeName);
            if (!string.IsNullOrWhiteSpace(result))
            {
                return result;
            }
        }

        if (propertyTypeName is TYPE_LOWERCASE_STRING or TYPE_LOWERCASE_BOOLEAN or TYPE_NUMBER or TYPE_GUID or TYPE_DATE or TYPE_DATE_ONLY or TYPE_TIME_ONLY or TYPE_DURATION)
        {
            return $"write{propertyTypeName.ToFirstCharacterUpperCase()}Value";
        }

        return "writeObjectValue";
    }

    private static CodeType GetCodeTypeForComposedType(CodeComposedTypeBase composedType)
    {
        ArgumentNullException.ThrowIfNull(composedType);
        return new CodeType
        {
            Name = composedType.Name,
            TypeDefinition = composedType,
            CollectionKind = composedType.CollectionKind
        };
    }

    private string? GetSerializationMethodNameForCodeType(CodeType propType, string propertyType)
    {
        return propType switch
        {
            _ when propType.TypeDefinition is CodeEnum currentEnum => $"writeEnumValue<{currentEnum.Name.ToFirstCharacterUpperCase()}{(currentEnum.Flags && !propType.IsCollection ? "[]" : string.Empty)}>",
            _ when conventions.StreamTypeName.Equals(propertyType, StringComparison.OrdinalIgnoreCase) => "writeByteArrayValue",
            _ when propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None => propType.TypeDefinition == null ? $"writeCollectionOfPrimitiveValues<{propertyType}>" : "writeCollectionOfObjectValues",
            _ => null
        };
    }

    private void WriteDeserializerFunction(CodeFunction codeFunction, LanguageWriter writer)
    {
        // handle composed types
        var composedParam = GetComposedTypeParameter(codeFunction);
        if (composedParam is not null)
        {
            WriteComposedTypeDeserializer(codeFunction, writer, composedParam);
            return;
        }

        var param = codeFunction.OriginalLocalMethod.Parameters.FirstOrDefault();
        if (param?.Type is CodeType codeType && codeType.TypeDefinition is CodeInterface codeInterface)
        {
            WriteDeserializerFunctionProperties(param, codeInterface, codeFunction, writer);
        }
        else
        {
            throw new InvalidOperationException($"Model interface for deserializer function {codeFunction.Name} is not available");
        }
    }

    private void WriteDeserializerFunctionProperties(CodeParameter param, CodeInterface codeInterface, CodeFunction codeFunction, LanguageWriter writer)
    {
        var properties = codeInterface.Properties.Where(static x => x.IsOfKind(CodePropertyKind.Custom, CodePropertyKind.BackingStore) && !x.ExistsInBaseType);

        writer.StartBlock("return {");
        WriteInheritsBlock(codeInterface, param, writer);
        var (primaryErrorMapping, primaryErrorMappingKey) = GetPrimaryErrorMapping(codeFunction, param);

        foreach (var otherProp in properties)
        {
            WritePropertyDeserializationBlock(otherProp, param, primaryErrorMapping, primaryErrorMappingKey, codeFunction, writer);
        }

        writer.CloseBlock();
    }

    private void WriteInheritsBlock(CodeInterface codeInterface, CodeParameter param, LanguageWriter writer)
    {
        if (codeInterface.StartBlock.Implements.FirstOrDefault(static x => x.TypeDefinition is CodeInterface) is CodeType type && type.TypeDefinition is CodeInterface inherits)
        {
            writer.WriteLine($"...deserializeInto{inherits.Name.ToFirstCharacterUpperCase()}({param.Name.ToFirstCharacterLowerCase()}),");
        }
    }

    private (string, string) GetPrimaryErrorMapping(CodeFunction codeFunction, CodeParameter param)
    {
        var primaryErrorMapping = string.Empty;
        var primaryErrorMappingKey = string.Empty;
        var parentClass = codeFunction.OriginalMethodParentClass;

        if (parentClass.IsErrorDefinition && parentClass.AssociatedInterface is not null && parentClass.AssociatedInterface.GetPrimaryMessageCodePath(static x => x.Name.ToFirstCharacterLowerCase(), static x => x.Name.ToFirstCharacterLowerCase(), "?.") is string primaryMessageCodePath && !string.IsNullOrEmpty(primaryMessageCodePath))
        {
            primaryErrorMapping = $" {param.Name.ToFirstCharacterLowerCase()}.message = {param.Name.ToFirstCharacterLowerCase()}.{primaryMessageCodePath} ?? \"\";";
            primaryErrorMappingKey = primaryMessageCodePath.Split("?.", StringSplitOptions.RemoveEmptyEntries)[0];
        }

        return (primaryErrorMapping, primaryErrorMappingKey);
    }

    private void WritePropertyDeserializationBlock(CodeProperty otherProp, CodeParameter param, string primaryErrorMapping, string primaryErrorMappingKey, CodeFunction codeFunction, LanguageWriter writer)
    {
        var suffix = GetSuffix(otherProp, primaryErrorMapping, primaryErrorMappingKey);
        var paramName = param.Name.ToFirstCharacterLowerCase();
        var propName = otherProp.Name.ToFirstCharacterLowerCase();

        if (IsBackingStoreProperty(otherProp))
        {
            WriteBackingStoreProperty(writer, paramName, propName, suffix);
        }
        else if (GetOriginalComposedType(otherProp.Type) is { } composedType)
        {
            WriteComposedTypePropertyDeserialization(writer, otherProp, paramName, propName, composedType, codeFunction, suffix);
        }
        else
        {
            WriteDefaultPropertyDeserialization(writer, otherProp, paramName, propName, codeFunction, suffix);
        }
    }

    private string GetSuffix(CodeProperty otherProp, string primaryErrorMapping, string primaryErrorMappingKey)
    {
        return otherProp.Name.Equals(primaryErrorMappingKey, StringComparison.Ordinal) ? primaryErrorMapping : string.Empty;
    }

    private bool IsBackingStoreProperty(CodeProperty otherProp)
    {
        return otherProp.Kind is CodePropertyKind.BackingStore;
    }

    private void WriteBackingStoreProperty(LanguageWriter writer, string paramName, string propName, string suffix)
    {
        writer.WriteLine($"\"{BackingStoreEnabledKey}\": n => {{ {paramName}.{propName} = true;{suffix} }},");
    }

    private void WriteComposedTypePropertyDeserialization(LanguageWriter writer, CodeProperty otherProp, string paramName, string propName, CodeComposedTypeBase composedType, CodeFunction codeFunction, string suffix)
    {
        if (composedType.IsComposedOfPrimitives())
        {
            writer.WriteLine($"\"{otherProp.WireName}\": n => {{ {paramName}.{propName} = {GetFactoryMethodName(otherProp.Type, codeFunction)}(n); }},");
        }
        else if (composedType.IsComposedOfObjectsAndPrimitives())
        {
            var objectSerializationMethodName = conventions.GetDeserializationMethodName(otherProp.Type, codeFunction.OriginalLocalMethod);
            var primitiveValuesUnionString = GetSerializationMethodsForPrimitiveUnionTypes(composedType, "n", codeFunction, false);
            var defaultValueSuffix = GetDefaultValueSuffix(otherProp);
            writer.WriteLine($"\"{otherProp.WireName}\": n => {{ {paramName}.{propName} = {primitiveValuesUnionString} ?? n.{objectSerializationMethodName}{defaultValueSuffix};{suffix} }},");
        }
    }

    private void WriteDefaultPropertyDeserialization(LanguageWriter writer, CodeProperty otherProp, string paramName, string propName, CodeFunction codeFunction, string suffix)
    {
        var objectSerializationMethodName = conventions.GetDeserializationMethodName(otherProp.Type, codeFunction.OriginalLocalMethod);
        var defaultValueSuffix = GetDefaultValueSuffix(otherProp);
        writer.WriteLine($"\"{otherProp.WireName}\": n => {{ {paramName}.{propName} = n.{objectSerializationMethodName}{defaultValueSuffix};{suffix} }},");
    }

    private string GetDefaultValueSuffix(CodeProperty otherProp)
    {
        var defaultValue = GetDefaultValueLiteralForProperty(otherProp);
        return !string.IsNullOrEmpty(defaultValue) && !defaultValue.EqualsIgnoreCase("\"null\"") ? $" ?? {defaultValue}" : string.Empty;
    }

    private static string GetDefaultValueLiteralForProperty(CodeProperty codeProperty)
    {
        if (string.IsNullOrEmpty(codeProperty.DefaultValue)) return string.Empty;
        if (codeProperty.Type is CodeType propertyType && propertyType.TypeDefinition is CodeEnum enumDefinition && enumDefinition.CodeEnumObject is not null)
            return $"{enumDefinition.CodeEnumObject.Name.ToFirstCharacterUpperCase()}.{codeProperty.DefaultValue.Trim('"').CleanupSymbolName().ToFirstCharacterUpperCase()}";
        return codeProperty.DefaultValue;
    }
    private void WriteDefensiveStatements(CodeMethod codeElement, LanguageWriter writer)
    {
        if (codeElement.IsOfKind(CodeMethodKind.Setter)) return;

        var isRequestExecutor = codeElement.IsOfKind(CodeMethodKind.RequestExecutor);

        foreach (var parameter in codeElement.Parameters
                                        .Where(x => !x.Optional && !x.IsOfKind(CodeParameterKind.RequestAdapter, CodeParameterKind.PathParameters) &&
                                                !(isRequestExecutor && x.IsOfKind(CodeParameterKind.RequestBody)))
                                        .OrderBy(static x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            var parameterName = parameter.Name.ToFirstCharacterLowerCase();
            if (!"boolean".Equals(conventions.TranslateType(parameter.Type), StringComparison.OrdinalIgnoreCase))
                writer.WriteLine($"if(!{parameterName}) throw new Error(\"{parameterName} cannot be undefined\");");
        }
    }

    private string? GetSerializerAlias(CodeType propType, CodeFunction codeFunction, string propertySerializerName)
    {
        CodeFunction serializationFunction;

        if (GetOriginalComposedType(propType) is not null)
        {
            if (codeFunction.GetImmediateParentOfType<CodeFile>() is not CodeFile functionParentFile ||
                functionParentFile.FindChildByName<CodeFunction>(propertySerializerName, false) is not CodeFunction composedTypeSerializationFunction)
            {
                return string.Empty;
            }
            serializationFunction = composedTypeSerializationFunction;
        }
        else
        {
            if (propType.TypeDefinition?.GetImmediateParentOfType<CodeFile>() is not CodeFile parentFile ||
                parentFile.FindChildByName<CodeFunction>(propertySerializerName, false) is not CodeFunction foundSerializationFunction)
            {
                return string.Empty;
            }
            serializationFunction = foundSerializationFunction;
        }

        return conventions.GetTypeString(new CodeType { TypeDefinition = serializationFunction }, codeFunction, false);
    }
}
