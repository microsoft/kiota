

using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using static Kiota.Builder.Refiners.TypeScriptRefiner;

namespace Kiota.Builder.Writers.TypeScript;

public class CodeFunctionWriter : BaseElementWriter<CodeFunction, TypeScriptConventionService>
{
    public CodeFunctionWriter(TypeScriptConventionService conventionService) : base(conventionService)
    {
    }
    private static readonly HashSet<string> customSerializationWriters = new(StringComparer.OrdinalIgnoreCase) { "writeObjectValue", "writeCollectionOfObjectValues" };

    public override void WriteCodeElement(CodeFunction codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        if (codeElement.OriginalLocalMethod == null) throw new InvalidOperationException($"{nameof(codeElement.OriginalLocalMethod)} should not be null");
        ArgumentNullException.ThrowIfNull(writer);

        if (codeElement.Parent is not CodeFile parentFile) throw new InvalidOperationException("the parent of a function should be a file");

        var codeMethod = codeElement.OriginalLocalMethod;

        var returnType = codeMethod.Kind != CodeMethodKind.Factory ? conventions.GetTypeString(codeMethod.ReturnType, codeElement) : string.Empty;
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
                WriteDiscriminatorFunction(codeElement, writer);
                break;
            case CodeMethodKind.ClientConstructor:
                WriteApiConstructorBody(parentFile, codeMethod, writer);
                break;
            default: throw new InvalidOperationException("Invalid code method kind");
        }
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
            var uriTemplateConstantName = $"{codeInterface.Name.ToFirstCharacterUpperCase()}{CodeConstant.UriTemplateSuffix}";
            var navigationMetadataConstantName = parentFile.FindChildByName<CodeConstant>($"{codeInterface.Name.ToFirstCharacterUpperCase()}{CodeConstant.NavigationMetadataSuffix}", false) is { } navConstant ? navConstant.Name.ToFirstCharacterUpperCase() : "undefined";
            var requestsMetadataConstantName = parentFile.FindChildByName<CodeConstant>($"{codeInterface.Name.ToFirstCharacterUpperCase()}{CodeConstant.RequestsMetadataSuffix}", false) is { } reqConstant ? reqConstant.Name.ToFirstCharacterUpperCase() : "undefined";
            writer.WriteLine($"return apiClientProxifier<{codeInterface.Name.ToFirstCharacterUpperCase()}>({requestAdapterArgumentName}, pathParameters, {uriTemplateConstantName}, {navigationMetadataConstantName}, {requestsMetadataConstantName});");
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
        if (codeElement.OriginalMethodParentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForInheritedType && parseNodeParameter != null)
        {
            writer.WriteLines($"const mappingValueNode = {parseNodeParameter.Name.ToFirstCharacterLowerCase()}.getChildNode(\"{codeElement.OriginalMethodParentClass.DiscriminatorInformation.DiscriminatorPropertyName}\");",
                                "if (mappingValueNode) {");
            writer.IncreaseIndent();
            writer.WriteLines("const mappingValue = mappingValueNode.getStringValue();",
                            "if (mappingValue) {");
            writer.IncreaseIndent();

            writer.StartBlock("switch (mappingValue) {");
            foreach (var mappedType in codeElement.OriginalMethodParentClass.DiscriminatorInformation.DiscriminatorMappings)
            {
                writer.StartBlock($"case \"{mappedType.Key}\":");
                writer.WriteLine($"return {getDeserializationFunction(codeElement, mappedType.Value.Name.ToFirstCharacterUpperCase())};");
                writer.DecreaseIndent();
            }
            writer.CloseBlock();
            writer.CloseBlock();
            writer.CloseBlock();
        }
        var s = getDeserializationFunction(codeElement, returnType);
        writer.WriteLine($"return {s.ToFirstCharacterLowerCase()};");
    }

    private string getDeserializationFunction(CodeElement codeElement, string returnType)
    {
        var codeNamespace = codeElement.GetImmediateParentOfType<CodeNamespace>();
        var parent = codeNamespace.FindChildByName<CodeFunction>($"deserializeInto{returnType}")!;

        return conventions.GetTypeString(new CodeType { TypeDefinition = parent }, codeElement, false);
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
        var propTypeName = conventions.GetTypeString(codeProperty.Type, codeProperty.Parent!, false);

        var serializationName = GetSerializationMethodName(codeProperty.Type);
        var defaultValueSuffix = GetDefaultValueLiteralForProperty(codeProperty) is string dft && !string.IsNullOrEmpty(dft) ? $" ?? {dft}" : string.Empty;

        if (customSerializationWriters.Contains(serializationName) && codeProperty.Type is CodeType propType && propType.TypeDefinition is not null)
        {
            var serializeName = getSerializerAlias(propType, codeFunction, $"serialize{propType.TypeDefinition.Name}");
            writer.WriteLine($"writer.{serializationName}<{propTypeName}>(\"{codeProperty.WireName}\", {modelParamName}.{codePropertyName}{defaultValueSuffix}, {serializeName});");
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(spreadOperator))
            {
                writer.WriteLine($"if({modelParamName}.{codePropertyName})");
            }
            writer.WriteLine($"writer.{serializationName}(\"{codeProperty.WireName}\", {spreadOperator}{modelParamName}.{codePropertyName}{defaultValueSuffix});");
        }
    }

    private string GetSerializationMethodName(CodeTypeBase propType)
    {
        var propertyType = conventions.TranslateType(propType);
        if (!string.IsNullOrEmpty(propertyType) && propType is CodeType currentType && GetSerializationMethodNameForCodeType(currentType, propertyType) is string result && !string.IsNullOrWhiteSpace(result))
        {
            return result;
        }
        return propertyType switch
        {
            "string" or "boolean" or "number" or "Guid" or "Date" or "DateOnly" or "TimeOnly" or "Duration" => $"write{propertyType.ToFirstCharacterUpperCase()}Value",
            _ => $"writeObjectValue",
        };
    }

    private string? GetSerializationMethodNameForCodeType(CodeType propType, string propertyType)
    {
        if (propType.TypeDefinition is CodeEnum currentEnum)
            return $"writeEnumValue<{currentEnum.Name.ToFirstCharacterUpperCase()}{(currentEnum.Flags && !propType.IsCollection ? "[]" : string.Empty)}>";
        else if (conventions.StreamTypeName.Equals(propertyType, StringComparison.OrdinalIgnoreCase))
            return "writeByteArrayValue";
        else if (propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None)
        {
            if (propType.TypeDefinition == null)
                return $"writeCollectionOfPrimitiveValues<{propertyType}>";
            else
                return "writeCollectionOfObjectValues";
        }
        return null;
    }

    private void WriteDeserializerFunction(CodeFunction codeFunction, LanguageWriter writer)
    {
        if (codeFunction.OriginalLocalMethod.Parameters.FirstOrDefault() is CodeParameter param && param.Type is CodeType codeType && codeType.TypeDefinition is CodeInterface codeInterface)
        {
            var properties = codeInterface.Properties.Where(static x => x.IsOfKind(CodePropertyKind.Custom, CodePropertyKind.BackingStore) && !x.ExistsInBaseType);

            writer.StartBlock("return {");
            if (codeInterface.StartBlock.Implements.FirstOrDefault(static x => x.TypeDefinition is CodeInterface) is CodeType type && type.TypeDefinition is CodeInterface inherits)
            {
                writer.WriteLine($"...deserializeInto{inherits.Name.ToFirstCharacterUpperCase()}({param.Name.ToFirstCharacterLowerCase()}),");
            }

            var primaryErrorMapping = string.Empty;
            var primaryErrorMappingKey = string.Empty;
            var parentClass = codeFunction.OriginalMethodParentClass;

            if (parentClass.IsErrorDefinition && parentClass.AssociatedInterface is not null && parentClass.AssociatedInterface.GetPrimaryMessageCodePath(static x => x.Name.ToFirstCharacterLowerCase(), static x => x.Name.ToFirstCharacterLowerCase(), "?.") is string primaryMessageCodePath && !string.IsNullOrEmpty(primaryMessageCodePath))
            {
                primaryErrorMapping = $" {param.Name.ToFirstCharacterLowerCase()}.message = {param.Name.ToFirstCharacterLowerCase()}.{primaryMessageCodePath} ?? \"\";";
                primaryErrorMappingKey = primaryMessageCodePath.Split("?.", StringSplitOptions.RemoveEmptyEntries)[0];
            }

            foreach (var otherProp in properties)
            {
                var keyName = !string.IsNullOrWhiteSpace(otherProp.SerializationName) ? otherProp.SerializationName.ToFirstCharacterLowerCase() : otherProp.Name.ToFirstCharacterLowerCase();
                var suffix = otherProp.Name.Equals(primaryErrorMappingKey, StringComparison.Ordinal) ? primaryErrorMapping : string.Empty;
                if (keyName.Equals(BackingStoreEnabledKey, StringComparison.Ordinal))
                    writer.WriteLine($"\"{keyName}\": n => {{ {param.Name.ToFirstCharacterLowerCase()}.{otherProp.Name.ToFirstCharacterLowerCase()} = true;{suffix} }},");
                else
                {
                    var defaultValueSuffix = GetDefaultValueLiteralForProperty(otherProp) is string dft && !string.IsNullOrEmpty(dft) ? $" ?? {dft}" : string.Empty;
                    writer.WriteLine($"\"{keyName}\": n => {{ {param.Name.ToFirstCharacterLowerCase()}.{otherProp.Name.ToFirstCharacterLowerCase()} = n.{GetDeserializationMethodName(otherProp.Type, codeFunction)}{defaultValueSuffix};{suffix} }},");
                }
            }

            writer.CloseBlock();
        }
        else
            throw new InvalidOperationException($"Model interface for deserializer function {codeFunction.Name} is not available");
    }
    private static string GetDefaultValueLiteralForProperty(CodeProperty codeProperty)
    {
        if (string.IsNullOrEmpty(codeProperty.DefaultValue)) return string.Empty;
        if (codeProperty.Type is CodeType propertyType && propertyType.TypeDefinition is CodeEnum enumDefinition)
            return $"{enumDefinition.Name.ToFirstCharacterUpperCase()}.{codeProperty.DefaultValue.Trim('"').CleanupSymbolName().ToFirstCharacterUpperCase()}";
        return codeProperty.DefaultValue;
    }
    private static void WriteDefensiveStatements(CodeMethod codeElement, LanguageWriter writer)
    {
        if (codeElement.IsOfKind(CodeMethodKind.Setter)) return;

        var isRequestExecutor = codeElement.IsOfKind(CodeMethodKind.RequestExecutor);

        foreach (var parameter in codeElement.Parameters
                                        .Where(x => !x.Optional && !x.IsOfKind(CodeParameterKind.RequestAdapter, CodeParameterKind.PathParameters) &&
                                                !(isRequestExecutor && x.IsOfKind(CodeParameterKind.RequestBody)))
                                        .OrderBy(static x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            var parameterName = parameter.Name.ToFirstCharacterLowerCase();
            writer.WriteLine($"if(!{parameterName}) throw new Error(\"{parameterName} cannot be undefined\");");
        }
    }
    private string GetDeserializationMethodName(CodeTypeBase propType, CodeFunction codeFunction)
    {
        var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
        var propertyType = conventions.GetTypeString(propType, codeFunction, false);
        if (!string.IsNullOrEmpty(propertyType) && propType is CodeType currentType)
        {
            if (currentType.TypeDefinition is CodeEnum currentEnum && currentEnum.CodeEnumObject is not null)
                return $"{(currentEnum.Flags || isCollection ? "getCollectionOfEnumValues" : "getEnumValue")}<{currentEnum.Name.ToFirstCharacterUpperCase()}>({currentEnum.CodeEnumObject.Name.ToFirstCharacterUpperCase()})";
            else if (conventions.StreamTypeName.Equals(propertyType, StringComparison.OrdinalIgnoreCase))
                return "getByteArrayValue";
            else if (isCollection)
                if (currentType.TypeDefinition == null)
                    return $"getCollectionOfPrimitiveValues<{propertyType}>()";
                else
                {
                    return $"getCollectionOfObjectValues<{propertyType.ToFirstCharacterUpperCase()}>({GetFactoryMethodName(propType, codeFunction.OriginalLocalMethod)})";
                }
        }
        return propertyType switch
        {
            "string" or "boolean" or "number" or "Guid" or "Date" or "DateOnly" or "TimeOnly" or "Duration" => $"get{propertyType.ToFirstCharacterUpperCase()}Value()",
            _ => $"getObjectValue<{propertyType.ToFirstCharacterUpperCase()}>({GetFactoryMethodName(propType, codeFunction.OriginalLocalMethod)})"
        };
    }

    private string GetFactoryMethodName(CodeTypeBase targetClassType, CodeMethod currentElement)
    {
        if (conventions.TranslateType(targetClassType) is string targetClassName)
        {
            var resultName = $"create{targetClassName.ToFirstCharacterUpperCase()}FromDiscriminatorValue";
            if (conventions.GetTypeString(targetClassType, currentElement, false) is string returnType && targetClassName.EqualsIgnoreCase(returnType)) return resultName;
            if (targetClassType is CodeType currentType && currentType.TypeDefinition is CodeInterface definitionClass)
            {
                var factoryMethod = definitionClass.GetImmediateParentOfType<CodeFile>()?.FindChildByName<CodeFunction>(resultName) ??
                                    definitionClass.GetImmediateParentOfType<CodeNamespace>()?.FindChildByName<CodeFunction>(resultName);
                if (factoryMethod != null)
                {
                    var methodName = conventions.GetTypeString(new CodeType { Name = resultName, TypeDefinition = factoryMethod }, currentElement, false);
                    return methodName.ToFirstCharacterUpperCase();// static function is aliased
                }
            }
        }
        throw new InvalidOperationException($"Unable to find factory method for {targetClassType}");
    }

    private string? getSerializerAlias(CodeType propType, CodeFunction codeFunction, string propertySerializerName)
    {
        if (propType.TypeDefinition?.GetImmediateParentOfType<CodeFile>() is not CodeFile parentFile ||
            parentFile.FindChildByName<CodeFunction>(propertySerializerName, false) is not CodeFunction serializationFunction)
            return string.Empty;
        return conventions.GetTypeString(new CodeType { TypeDefinition = serializationFunction }, codeFunction, false);
    }
}
