

using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
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

        var isComposedOfPrimitives = GetOriginalComposedType(codeMethod.ReturnType) is CodeComposedTypeBase composedType && IsComposedOfPrimitives(composedType);

        var returnType = codeMethod.Kind is CodeMethodKind.Factory || (codeMethod.Kind is CodeMethodKind.ComposedTypeFactory && !isComposedOfPrimitives) ?
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
            case CodeMethodKind.ComposedTypeFactory:
                WriteDiscriminatorFunction(codeElement, writer);
                break;
            case CodeMethodKind.ClientConstructor:
                WriteApiConstructorBody(parentFile, codeMethod, writer);
                break;
            case CodeMethodKind.ComposedTypeSerializer:
                WriteComposedTypeSerializer(codeElement, writer);
                break;
            case CodeMethodKind.ComposedTypeDeserializer:
                WriteComposedTypeDeserializer(codeElement, writer);
                break;
            default: throw new InvalidOperationException("Invalid code method kind");
        }
    }

    private void WriteFactoryMethodBodyForPrimitives(CodeComposedTypeBase composedType, CodeFunction codeElement, LanguageWriter writer, CodeParameter? parseNodeParameter)
    {
        ArgumentNullException.ThrowIfNull(parseNodeParameter);
        var parseNodeParameterName = parseNodeParameter.Name.ToFirstCharacterLowerCase();
        writer.StartBlock($"if ({parseNodeParameterName}) {{");

        string getPrimitiveValueString = string.Join(" || ", composedType.Types.Select(x => $"{parseNodeParameterName}." + conventions.GetDeserializationMethodName(x, codeElement.OriginalLocalMethod)));
        writer.WriteLine($"return {getPrimitiveValueString};");
        writer.CloseBlock();
        writer.WriteLine("return undefined;");
    }

    private void WriteComposedTypeDeserializer(CodeFunction codeElement, LanguageWriter writer)
    {
        var composedParam = codeElement.OriginalLocalMethod.Parameters.FirstOrDefault(x => GetOriginalComposedType(x) is not null);

        if (composedParam is null || GetOriginalComposedType(composedParam) is not { } composedType) return;

        writer.StartBlock("return {");
        foreach (var mappedType in composedType.Types.ToArray())
        {
            var mappedTypeName = mappedType.Name.ToFirstCharacterUpperCase();
            writer.WriteLine($"...{GetFunctionName(codeElement, mappedTypeName, CodeMethodKind.Deserializer)}({composedParam.Name.ToFirstCharacterLowerCase()}),");
        }
        writer.CloseBlock();
    }

    private void WriteComposedTypeSerializer(CodeFunction codeElement, LanguageWriter writer)
    {
        var composedParam = codeElement.OriginalLocalMethod.Parameters.FirstOrDefault(x => GetOriginalComposedType(x) is not null);

        if (composedParam is null || GetOriginalComposedType(composedParam) is not { } composedType) return;

        if (IsComposedOfPrimitives(composedType))
        {
            WriteComposedTypeSerializationForPrimitives(composedType, composedParam, codeElement, writer);
            return;
        }

        if (composedType is CodeIntersectionType)
        {
            WriteComposedTypeSerializationForCodeIntersectionType(composedType, composedParam, codeElement, writer);
            return;
        }

        WriteDefaultComposedTypeSerialization(composedParam, codeElement, writer);
    }

    private void WriteComposedTypeSerializationForCodeIntersectionType(CodeComposedTypeBase composedType, CodeParameter composedParam, CodeFunction method, LanguageWriter writer)
    {
        foreach (var mappedType in composedType.Types.ToArray())
        {
            var mappedTypeName = mappedType.Name.ToFirstCharacterUpperCase();
            writer.WriteLine($"{GetFunctionName(method, mappedTypeName, CodeMethodKind.Serializer)}(writer, {composedParam.Name.ToFirstCharacterLowerCase()});");
        }
    }

    private void WriteDefaultComposedTypeSerialization(CodeParameter composedParam, CodeFunction codeElement, LanguageWriter writer)
    {
        var discriminatorInfo = codeElement.OriginalMethodParentClass.DiscriminatorInformation;
        var discriminatorPropertyName = discriminatorInfo.DiscriminatorPropertyName;

        if (string.IsNullOrEmpty(discriminatorPropertyName))
        {
            WriteMissingDiscriminatorPropertyComment(composedParam, codeElement, writer);
            return;
        }

        var paramName = composedParam.Name.ToFirstCharacterLowerCase();
        writer.WriteLine($"if ({paramName} === undefined) return;");
        WriteDiscriminatorSwitchBlock(discriminatorInfo, paramName, codeElement, writer);
    }

    private void WriteMissingDiscriminatorPropertyComment(CodeParameter composedParam, CodeFunction codeElement, LanguageWriter writer)
    {
        var typeString = GetTypescriptTypeString(composedParam.Type, codeElement, inlineComposedTypeString: true);
        var comment = $"The composed parameter '{composedParam.Name}' consists of {typeString}. However, it lacks a discriminator property, which is necessary for proper type differentiation. Please update the OpenAPI specification to include a discriminator property to ensure correct method generation.";
        writer.WriteLine($"// {comment}");
        writer.WriteLine("return;");
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

    private void WriteComposedTypeSerializationForPrimitives(CodeComposedTypeBase composedType, CodeParameter composedParam, CodeFunction method, LanguageWriter writer)
    {
        var paramName = composedParam.Name.ToFirstCharacterLowerCase();
        writer.WriteLine($"if ({paramName} === undefined) return;");
        writer.StartBlock($"switch (typeof {paramName}) {{");

        foreach (var type in composedType.Types)
        {
            WriteTypeSerialization(type, paramName, method, writer);
        }

        writer.CloseBlock();
    }

    private void WriteTypeSerialization(CodeTypeBase type, string paramName, CodeFunction method, LanguageWriter writer)
    {
        var nodeType = conventions.GetTypeString(type, method, false);
        var serializationName = GetSerializationMethodName(type, method.OriginalLocalMethod);
        if (string.IsNullOrEmpty(serializationName) || string.IsNullOrEmpty(nodeType)) return;

        writer.StartBlock($"case \"{nodeType}\":");
        writer.WriteLine($"writer.{serializationName}(key, {paramName});");
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

    private void WriteDiscriminatorFunction(CodeFunction codeElement, LanguageWriter writer)
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
            case CodeComposedTypeBase type when IsComposedOfPrimitives(type):
                WriteFactoryMethodBodyForPrimitives(type, codeElement, writer, parseNodeParameter);
                break;
            case CodeUnionType _ when parseNodeParameter != null:
                WriteFactoryMethodBodyForCodeUnionType(codeElement, writer, parseNodeParameter);
                break;
            case CodeIntersectionType _ when parseNodeParameter != null:
                WriteDefaultDiscriminator(codeElement, returnType, writer, parseNodeParameter);
                break;
            default:
                WriteNormalFactoryMethodBody(codeElement, returnType, writer);
                break;
        }
    }

    private void WriteFactoryMethodBodyForCodeUnionType(CodeFunction codeElement, LanguageWriter writer, CodeParameter parseNodeParameter)
    {
        WriteDiscriminatorInformation(codeElement, parseNodeParameter, writer);
        // It's a composed type but there isn't a discriminator property
        writer.WriteLine($"throw new Error(\"A discriminator property is required to distinguish a union type\");");
    }

    private void WriteNormalFactoryMethodBody(CodeFunction codeElement, string returnType, LanguageWriter writer)
    {
        var parseNodeParameter = codeElement.OriginalLocalMethod.Parameters.OfKind(CodeParameterKind.ParseNode);
        if (ShouldWriteDiscriminatorInformation(codeElement, null) && parseNodeParameter != null)
        {
            WriteDiscriminatorInformation(codeElement, parseNodeParameter, writer);
        }
        WriteDefaultDiscriminator(codeElement, returnType, writer, parseNodeParameter);
    }

    private void WriteDefaultDiscriminator(CodeFunction codeElement, string returnType, LanguageWriter writer, CodeParameter? parseNodeParameter)
    {
        var deserializationFunction = GetFunctionName(codeElement, returnType, CodeMethodKind.Deserializer);
        var parseNodeParameterForPrimitiveValues = GetParseNodeParameterForPrimitiveValues(codeElement, parseNodeParameter);
        writer.WriteLine($"return {deserializationFunction.ToFirstCharacterLowerCase()}{parseNodeParameterForPrimitiveValues};");
    }

    private static bool ShouldWriteDiscriminatorInformation(CodeFunction codeElement, CodeComposedTypeBase? composedType)
    {
        return codeElement.OriginalMethodParentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForInheritedType || composedType is CodeUnionType;
    }

    private void WriteDiscriminatorInformation(CodeFunction codeElement, CodeParameter parseNodeParameter, LanguageWriter writer)
    {
        writer.WriteLines($"const mappingValueNode = {parseNodeParameter.Name.ToFirstCharacterLowerCase()}?.getChildNode(\"{codeElement.OriginalMethodParentClass.DiscriminatorInformation.DiscriminatorPropertyName}\");",
                            "if (mappingValueNode) {");
        writer.IncreaseIndent();
        writer.WriteLines("const mappingValue = mappingValueNode.getStringValue();",
                        "if (mappingValue) {");
        writer.IncreaseIndent();

        writer.StartBlock("switch (mappingValue) {");
        foreach (var mappedType in codeElement.OriginalMethodParentClass.DiscriminatorInformation.DiscriminatorMappings)
        {
            writer.StartBlock($"case \"{mappedType.Key}\":");
            writer.WriteLine($"return {GetFunctionName(codeElement, mappedType.Value.Name.ToFirstCharacterUpperCase(), CodeMethodKind.Deserializer)};");
            writer.DecreaseIndent();
        }
        writer.CloseBlock();
        writer.CloseBlock();
        writer.CloseBlock();
    }

    private string GetParseNodeParameterForPrimitiveValues(CodeFunction codeElement, CodeParameter? parseNodeParameter)
    {
        if (GetOriginalComposedType(codeElement.OriginalLocalMethod.ReturnType) is { } composedType && IsComposedOfPrimitives(composedType) && parseNodeParameter is not null)
        {
            return $"({parseNodeParameter.Name.ToFirstCharacterLowerCase()})";
        }
        return string.Empty;
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

    private static bool IsCodePropertyCollectionOfEnum(CodeProperty property)
    {
        return property.Type is CodeType cType && cType.IsCollection && cType.TypeDefinition is CodeEnum;
    }

    private void WritePropertySerializer(string modelParamName, CodeProperty codeProperty, LanguageWriter writer, CodeFunction codeFunction)
    {
        var isCollectionOfEnum = IsCodePropertyCollectionOfEnum(codeProperty);
        var spreadOperator = isCollectionOfEnum ? "..." : string.Empty;
        var codePropertyName = codeProperty.Name.ToFirstCharacterLowerCase();
        var propTypeName = GetTypescriptTypeString(codeProperty.Type, codeProperty.Parent!, false, inlineComposedTypeString: true);

        var serializationName = GetSerializationMethodName(codeProperty.Type, codeFunction.OriginalLocalMethod);
        var defaultValueSuffix = GetDefaultValueLiteralForProperty(codeProperty) is string dft && !string.IsNullOrEmpty(dft) ? $" ?? {dft}" : string.Empty;

        var composedType = GetOriginalComposedType(codeProperty.Type);

        if (customSerializationWriters.Contains(serializationName) && codeProperty.Type is CodeType propType && propType.TypeDefinition is not null)
        {
            var serializeName = GetSerializerAlias(propType, codeFunction, $"serialize{propType.TypeDefinition.Name}");
            writer.WriteLine($"writer.{serializationName}<{propTypeName}>(\"{codeProperty.WireName}\", {modelParamName}.{codePropertyName}{defaultValueSuffix}, {serializeName});");
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(spreadOperator))
            {
                writer.WriteLine($"if({modelParamName}.{codePropertyName})");
            }
            if (composedType is not null && IsComposedOfPrimitives(composedType))
                writer.WriteLine($"{serializationName}(writer, \"{codeProperty.WireName}\", {spreadOperator}{modelParamName}.{codePropertyName}{defaultValueSuffix});");
            else
                writer.WriteLine($"writer.{serializationName}(\"{codeProperty.WireName}\", {spreadOperator}{modelParamName}.{codePropertyName}{defaultValueSuffix});");
        }
    }


    public string GetSerializationMethodName(CodeTypeBase propertyType, CodeMethod method)
    {
        ArgumentNullException.ThrowIfNull(propertyType);
        ArgumentNullException.ThrowIfNull(method);

        var composedType = GetOriginalComposedType(propertyType);
        if (composedType is not null && IsComposedOfPrimitives(composedType))
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
        var param = codeFunction.OriginalLocalMethod.Parameters.FirstOrDefault();
        if (param?.Type is CodeType codeType && codeType.TypeDefinition is CodeInterface codeInterface)
        {
            var properties = codeInterface.Properties.Where(static x => x.IsOfKind(CodePropertyKind.Custom, CodePropertyKind.BackingStore) && !x.ExistsInBaseType);

            writer.StartBlock("return {");
            WriteInheritsBlock(codeInterface, param, writer);
            var (primaryErrorMapping, primaryErrorMappingKey) = GetPrimaryErrorMapping(codeFunction, param);

            foreach (var otherProp in properties)
            {
                WritePropertyBlock(otherProp, param, primaryErrorMapping, primaryErrorMappingKey, codeFunction, writer);
            }

            writer.CloseBlock();
        }
        else throw new InvalidOperationException($"Model interface for deserializer function {codeFunction.Name} is not available");
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

    private void WritePropertyBlock(CodeProperty otherProp, CodeParameter param, string primaryErrorMapping, string primaryErrorMappingKey, CodeFunction codeFunction, LanguageWriter writer)
    {
        var suffix = otherProp.Name.Equals(primaryErrorMappingKey, StringComparison.Ordinal) ? primaryErrorMapping : string.Empty;
        if (otherProp.Kind is CodePropertyKind.BackingStore)
            writer.WriteLine($"\"{BackingStoreEnabledKey}\": n => {{ {param.Name.ToFirstCharacterLowerCase()}.{otherProp.Name.ToFirstCharacterLowerCase()} = true;{suffix} }},");
        else if (GetOriginalComposedType(otherProp.Type) is { } composedType && IsComposedOfPrimitives(composedType))
        {
            writer.WriteLine($"\"{otherProp.WireName}\": n => {{ {param.Name.ToFirstCharacterLowerCase()}.{otherProp.Name.ToFirstCharacterLowerCase()} = {GetFactoryMethodName(otherProp.Type, codeFunction)}(n); }},");
        }
        else
        {
            var defaultValueSuffix = GetDefaultValueLiteralForProperty(otherProp) is string dft && !string.IsNullOrEmpty(dft) && !dft.EqualsIgnoreCase("null") ? $" ?? {dft}" : string.Empty;
            writer.WriteLine($"\"{otherProp.WireName}\": n => {{ {param.Name.ToFirstCharacterLowerCase()}.{otherProp.Name.ToFirstCharacterLowerCase()} = n.{conventions.GetDeserializationMethodName(otherProp.Type, codeFunction.OriginalLocalMethod)}{defaultValueSuffix};{suffix} }},");
        }
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
