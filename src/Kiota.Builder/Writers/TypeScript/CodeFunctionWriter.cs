

using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Kiota.Builder.SearchProviders.GitHub.GitHubClient.Repos.Item;
using static Kiota.Builder.Refiners.TypeScriptRefiner;
using static Kiota.Builder.Writers.TypeScript.TypeScriptConventionService;

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

        var returnType = codeMethod.Kind is CodeMethodKind.Factory ?
            "((instance?: Parsable) => Record<string, (node: ParseNode) => void>)" :
            conventions.GetTypeString(codeMethod.ReturnType, codeElement);
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
            case CodeMethodKind.ComposedTypeFactory:
                WriteComposedTypeFactory(codeMethod, writer);
                break;
            case CodeMethodKind.ComposedTypeSerializer:
                WriteComposedTypeSerializer(codeMethod, writer);
                break;
            default: throw new InvalidOperationException("Invalid code method kind");
        }
    }

    private void WriteComposedTypeFactory(CodeMethod method, LanguageWriter writer)
    {
        // TODO: Add implementation for object types and collections
        if (GetOriginalComposedType(method.ReturnType) is CodeComposedTypeBase composedType)
        {
            writer.WriteLine("const nodeValue = node?.getNodeValue();");
            writer.StartBlock($"switch (typeof nodeValue) {{");
            foreach (var type in composedType.Types)
            {
                var nodeType = conventions.GetTypeString(type, method, false);
                writer.WriteLine($"case \"{nodeType}\":");
            }
            writer.IncreaseIndent();
            writer.WriteLine($"return nodeValue;");
            writer.DecreaseIndent();
            writer.StartBlock($"default:");
            writer.WriteLine($"return undefined;");
            writer.DecreaseIndent();
            writer.CloseBlock();
        }
    }

    private void WriteComposedTypeSerializer(CodeMethod method, LanguageWriter writer)
    {
        var composedParam = method.Parameters.FirstOrDefault(x => GetOriginalComposedType(x) is not null);
        if (composedParam == null) return;


        if (GetOriginalComposedType(composedParam) is CodeComposedTypeBase composedType)
        {
            var paramName = composedParam.Name.ToFirstCharacterLowerCase();
            writer.WriteLine($"if ({paramName} == undefined) return;");
            writer.StartBlock($"switch (typeof {paramName}) {{");

            foreach (var type in composedType.Types)
            {
                var nodeType = conventions.GetTypeString(type, method, false);
                var serializationName = GetSerializationMethodName(type);
                if (serializationName == null || nodeType == null) continue;

                writer.StartBlock($"case \"{nodeType}\":");
                writer.WriteLine($"writer.{serializationName}(key, {paramName});");
                writer.WriteLine($"break;");
                writer.DecreaseIndent();
            }
        }

        writer.CloseBlock();
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
                writer.WriteLine($"return {GetDeserializationFunction(codeElement, mappedType.Value.Name.ToFirstCharacterUpperCase())};");
                writer.DecreaseIndent();
            }
            writer.CloseBlock();
            writer.CloseBlock();
            writer.CloseBlock();
        }
        var s = GetDeserializationFunction(codeElement, returnType);
        writer.WriteLine($"return {s.ToFirstCharacterLowerCase()};");
    }

    private string GetDeserializationFunction(CodeElement codeElement, string returnType)
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
            if (composedType is not null && conventions.IsComposedOfPrimitives(composedType))
                writer.WriteLine($"{serializationName}(writer, \"{codeProperty.WireName}\", {spreadOperator}{modelParamName}.{codePropertyName}{defaultValueSuffix});");
            else
                writer.WriteLine($"writer.{serializationName}(\"{codeProperty.WireName}\", {spreadOperator}{modelParamName}.{codePropertyName}{defaultValueSuffix});");
        }
    }

    private string GetSerializationMethodName(CodeTypeBase propertyType)
    {
        var propertyTypeName = conventions.TranslateType(propertyType);
        var composedType = GetOriginalComposedType(propertyType);
        var currentType = propertyType as CodeType;

        return (composedType, currentType, propertyTypeName) switch
        {
            (CodeComposedTypeBase type, _, _) => GetComposedTypeSerializationMethodName(type),
            (_, CodeType type, string prop) when !string.IsNullOrEmpty(prop) && GetSerializationMethodNameForCodeType(type, prop) is string result && !string.IsNullOrWhiteSpace(result) => result,
            (_, _, "string" or "boolean" or "number" or "Guid" or "Date" or "DateOnly" or "TimeOnly" or "Duration") => $"write{propertyTypeName.ToFirstCharacterUpperCase()}Value",
            _ => "writeObjectValue"
        };
    }

    public static string GetComposedTypeSerializationMethodName(CodeComposedTypeBase composedType)
    {
        ArgumentNullException.ThrowIfNull(composedType);
        // handle union of primitive values
        if (composedType is CodeUnionType && ConventionServiceInstance.IsComposedOfPrimitives(composedType))
        {
            return $"write{composedType.Name}Value";
        }
        // throw unsupported exception
        throw new InvalidOperationException($"Serialization for this composed type :: {composedType} :: is not supported yet");
    }

    private string? GetSerializationMethodNameForCodeType(CodeType propType, string propertyType)
    {
        if (propType.TypeDefinition is CodeEnum currentEnum)
            return $"writeEnumValue<{currentEnum.Name.ToFirstCharacterUpperCase()}{(currentEnum.Flags && !propType.IsCollection ? "[]" : string.Empty)}>";
        if (conventions.StreamTypeName.Equals(propertyType, StringComparison.OrdinalIgnoreCase))
            return "writeByteArrayValue";
        if (propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None)
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
            WriteInheritsBlock(codeInterface, param, writer);
            var (primaryErrorMapping, primaryErrorMappingKey) = GetPrimaryErrorMapping(codeFunction, param);

            foreach (var otherProp in properties)
            {
                WritePropertyBlock(otherProp, param, primaryErrorMapping, primaryErrorMappingKey, codeFunction, writer);
            }

            writer.CloseBlock();
        }
        else
            throw new InvalidOperationException($"Model interface for deserializer function {codeFunction.Name} is not available");
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
        else if (GetOriginalComposedType(otherProp.Type) is not null)
        {
            writer.WriteLine($"\"{otherProp.WireName}\": n => {{ {param.Name.ToFirstCharacterLowerCase()}.{otherProp.Name.ToFirstCharacterLowerCase()} = {GetDeserializationMethodName(otherProp.Type, codeFunction)}(n); }},");
        }
        else
        {
            var defaultValueSuffix = GetDefaultValueLiteralForProperty(otherProp) is string dft && !string.IsNullOrEmpty(dft) ? $" ?? {dft}" : string.Empty;
            writer.WriteLine($"\"{otherProp.WireName}\": n => {{ {param.Name.ToFirstCharacterLowerCase()}.{otherProp.Name.ToFirstCharacterLowerCase()} = n.{GetDeserializationMethodName(otherProp.Type, codeFunction)}{defaultValueSuffix};{suffix} }},");
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
    private string GetDeserializationMethodName(CodeTypeBase codeType, CodeFunction codeFunction)
    {
        var isCollection = codeType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
        var propertyType = conventions.GetTypeString(codeType, codeFunction, false);
        if (GetOriginalComposedType(codeType) is CodeComposedTypeBase composedType)
        {
            return GetComposedTypeDeserializationMethodName(composedType);
        }
        if (codeType is CodeType currentType && !string.IsNullOrEmpty(propertyType))
        {
            return (currentType.TypeDefinition, isCollection, propertyType) switch
            {
                (CodeEnum currentEnum, _, _) when currentEnum.CodeEnumObject is not null => $"{(currentEnum.Flags || isCollection ? "getCollectionOfEnumValues" : "getEnumValue")}<{currentEnum.Name.ToFirstCharacterUpperCase()}>({currentEnum.CodeEnumObject.Name.ToFirstCharacterUpperCase()})",
                (_, _, string prop) when conventions.StreamTypeName.Equals(prop, StringComparison.OrdinalIgnoreCase) => "getByteArrayValue",
                (_, true, string prop) when currentType.TypeDefinition is null => $"getCollectionOfPrimitiveValues<{prop}>()",
                (_, true, string prop) => $"getCollectionOfObjectValues<{prop.ToFirstCharacterUpperCase()}>({GetFactoryMethodName(codeType, codeFunction.OriginalLocalMethod)})",
                _ => GetDeserializationMethodNameForPrimitiveOrObject(codeType, propertyType, codeFunction)
            };
        }
        return GetDeserializationMethodNameForPrimitiveOrObject(codeType, propertyType, codeFunction);
    }

    private string GetDeserializationMethodNameForPrimitiveOrObject(CodeTypeBase propType, string propertyTypeName, CodeFunction codeFunction)
    {
        return propertyTypeName switch
        {
            "string" or "boolean" or "number" or "Guid" or "Date" or "DateOnly" or "TimeOnly" or "Duration" => $"get{propertyTypeName.ToFirstCharacterUpperCase()}Value()",
            _ => $"getObjectValue<{propertyTypeName.ToFirstCharacterUpperCase()}>({GetFactoryMethodName(propType, codeFunction.OriginalLocalMethod)})"
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
                var factoryMethod = GetFactoryMethod(definitionClass, resultName);
                if (factoryMethod != null)
                {
                    var methodName = conventions.GetTypeString(new CodeType { Name = resultName, TypeDefinition = factoryMethod }, currentElement, false);
                    return methodName.ToFirstCharacterUpperCase();// static function is aliased
                }
            }
        }
        throw new InvalidOperationException($"Unable to find factory method for {targetClassType}");
    }

    private static T? GetParentOfTypeOrNull<T>(CodeInterface definitionClass) where T : class
    {
        try
        {
            return definitionClass.GetImmediateParentOfType<T>();
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static CodeFunction? GetFactoryMethod(CodeInterface definitionClass, string factoryFunctionName)
    {
        return GetParentOfTypeOrNull<CodeFile>(definitionClass)?.FindChildByName<CodeFunction>(factoryFunctionName)
            ?? GetParentOfTypeOrNull<CodeNamespace>(definitionClass)?.FindChildByName<CodeFunction>(factoryFunctionName);
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
