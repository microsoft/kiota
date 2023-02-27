

using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.TypeScript;

public class CodeFunctionWriter : BaseElementWriter<CodeFunction, TypeScriptConventionService>
{

    public CodeFunctionWriter(TypeScriptConventionService conventionService, string clientNamespaceName) : base(conventionService)
    {
        _codeUsingWriter = new(clientNamespaceName);
    }
    private TypeScriptConventionService? localConventions;
    private readonly CodeUsingWriter _codeUsingWriter;
    private static HashSet<string> customSerializatioWriters = new HashSet<string> { "writeObjectValue", "writeCollectionOfObjectValues" };
    public override void WriteCodeElement(CodeFunction codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        if (codeElement.OriginalLocalMethod == null) throw new InvalidOperationException($"{nameof(codeElement.OriginalLocalMethod)} should not be null");
        ArgumentNullException.ThrowIfNull(writer);
        if (codeElement.Parent is not CodeNamespace) throw new InvalidOperationException("the parent of a function should be a namespace");
        _codeUsingWriter.WriteCodeElement(codeElement.StartBlock.Usings, codeElement.GetImmediateParentOfType<CodeNamespace>(), writer);
        var codeMethod = codeElement.OriginalLocalMethod;

        var returnType = codeMethod.Kind != CodeMethodKind.Factory ? conventions.GetTypeString(codeMethod.ReturnType, codeElement) : string.Empty;
        CodeMethodWriter.WriteMethodPrototypeInternal(codeElement.OriginalLocalMethod, writer, returnType, false, conventions, true);

        writer.IncreaseIndent();

        localConventions = new TypeScriptConventionService(writer);
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
            default: throw new InvalidOperationException("Invalid code method kind");
        }
    }

    private void WriteDiscriminatorFunction(CodeFunction codeElement, LanguageWriter writer)
    {
        var returnType = conventions.GetTypeString(codeElement.OriginalLocalMethod.ReturnType, codeElement);

        CodeMethodWriter.WriteDefensiveStatements(codeElement.OriginalLocalMethod, writer);
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

            writer.WriteLine("switch (mappingValue) {");
            writer.IncreaseIndent();
            foreach (var mappedType in codeElement.OriginalMethodParentClass.DiscriminatorInformation.DiscriminatorMappings)
            {
                writer.WriteLine($"case \"{mappedType.Key}\":");
                writer.IncreaseIndent();
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
        if (codeElement.Parent is not CodeNamespace codeNamespace)
        {
            throw new InvalidOperationException($"{codeElement.Name} does not have a parent namespace");
        }
        var parent = codeNamespace.FindChildByName<CodeFunction>($"deserializeInto{returnType}");

        return conventions.GetTypeString(new CodeType { TypeDefinition = parent }, codeElement, false);
    }

    private void WriteSerializerFunction(CodeFunction codeElement, LanguageWriter writer)
    {
        var param = codeElement.OriginalLocalMethod.Parameters.FirstOrDefault(x => x.Type is CodeType type && type.TypeDefinition is CodeInterface);
        if (param == null || param.Type is not CodeType codeType || codeType.TypeDefinition is not CodeInterface codeInterface)

            throw new InvalidOperationException("Interface parameter not found for code interface");

        writer.IncreaseIndent();

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
        writer.DecreaseIndent();
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

        var propertyTypeName = (codeProperty.Type as CodeType)?.TypeDefinition?.Name;

        var propType = localConventions?.GetTypeString(codeProperty.Type, codeProperty.Parent!, false);

        var serializationName = GetSerializationMethodName(codeProperty.Type);

        if (customSerializatioWriters.Contains(serializationName))
        {
            var serializeName = getSerializerAlias((codeProperty.Type as CodeType)!, codeFunction, $"serialize{propertyTypeName}");
            writer.WriteLine($"writer.{serializationName}<{propType}>(\"{codeProperty.WireName}\", {modelParamName}.{codePropertyName}, {serializeName});");
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(spreadOperator))
            {
                writer.WriteLine($"if({modelParamName}.{codePropertyName})");
            }
            writer.WriteLine($"writer.{serializationName}(\"{codeProperty.WireName}\", {spreadOperator}{modelParamName}.{codePropertyName});");
        }

    }

    private string GetSerializationMethodName(CodeTypeBase propType)
    {
        var propertyType = localConventions?.TranslateType(propType);
        if (!string.IsNullOrEmpty(propertyType) && propType is CodeType currentType && GetSerializationMethodNameForCodeType(currentType, propertyType) is string result && !String.IsNullOrWhiteSpace(result))
        {
            return result;
        }
        return propertyType switch
        {
            "string" or "boolean" or "number" or "Guid" or "Date" or "DateOnly" or "TimeOnly" or "Duration" => $"write{propertyType.ToFirstCharacterUpperCase()}Value",
            _ => $"writeObjectValue",
        };
    }

    private static string? GetSerializationMethodNameForCodeType(CodeType propType, string propertyType)
    {
        if (propType.TypeDefinition is CodeEnum currentEnum)
            return $"writeEnumValue<{currentEnum.Name.ToFirstCharacterUpperCase()}>";
        else if (propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None)
        {
            if (propType.TypeDefinition == null)
                return $"writeCollectionOfPrimitiveValues<{propertyType.ToFirstCharacterLowerCase()}>";
            else
                return $"writeCollectionOfObjectValues";
        }
        return null;
    }

    private void WriteDeserializerFunction(CodeFunction codeFunction, LanguageWriter writer)
    {
        if (codeFunction.OriginalLocalMethod.Parameters.FirstOrDefault() is CodeParameter param && param.Type is CodeType codeType && codeType.TypeDefinition is CodeInterface codeInterface)
        {


            var properties = codeInterface.Properties.Where(static x => x.Kind == CodePropertyKind.Custom && !x.ExistsInBaseType);

            writer.StartBlock("return {");
            if (codeInterface.StartBlock.Implements.FirstOrDefault(x => x.TypeDefinition is CodeInterface) is CodeType type && type.TypeDefinition is CodeInterface inherits)
            {
                writer.WriteLine($"...deserializeInto{inherits?.Name.ToFirstCharacterUpperCase()}({param.Name.ToFirstCharacterLowerCase()}),");
            }


            foreach (var otherProp in properties)
            {
                var keyName = !string.IsNullOrWhiteSpace(otherProp.SerializationName) ? otherProp.SerializationName.ToFirstCharacterLowerCase() : otherProp.Name.ToFirstCharacterLowerCase();
                writer.WriteLine($"\"{keyName}\": n => {{ {param.Name.ToFirstCharacterLowerCase()}.{otherProp.Name.ToFirstCharacterLowerCase()} = n.{GetDeserializationMethodName(otherProp.Type, codeFunction)}; }},");
            }

            writer.CloseBlock();
        }
        else
            throw new InvalidOperationException($"Model interface for deserializer function {codeFunction.Name} is not available");
    }

    private string GetDeserializationMethodName(CodeTypeBase propType, CodeFunction codeFunction)
    {
        var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
        var propertyType = localConventions?.GetTypeString(propType, codeFunction, false);
        if (!string.IsNullOrEmpty(propertyType) && propType is CodeType currentType)
        {
            if (currentType.TypeDefinition is CodeEnum currentEnum)
                return $"getEnumValue{(currentEnum.Flags || isCollection ? "s" : string.Empty)}<{currentEnum.Name.ToFirstCharacterUpperCase()}>({propertyType.ToFirstCharacterUpperCase()})";
            else if (isCollection)
                if (currentType.TypeDefinition == null)
                    return $"getCollectionOfPrimitiveValues<{propertyType.ToFirstCharacterLowerCase()}>()";
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
        var returnType = localConventions?.GetTypeString(targetClassType, currentElement, false);
        var targetClassName = localConventions?.TranslateType(targetClassType);
        if (targetClassName != null)
        {
            var resultName = $"create{targetClassName.ToFirstCharacterUpperCase()}FromDiscriminatorValue";
            if (targetClassName.Equals(returnType, StringComparison.OrdinalIgnoreCase))
                return resultName;
            if (targetClassType is CodeType currentType &&
                currentType.TypeDefinition is CodeInterface definitionClass &&
                definitionClass.GetImmediateParentOfType<CodeNamespace>() is CodeNamespace parentNamespace &&
                parentNamespace.FindChildByName<CodeFunction>(resultName) is CodeFunction factoryMethod)
            {
                var methodName = localConventions?.GetTypeString(new CodeType
                {
                    Name = resultName,
                    TypeDefinition = factoryMethod
                }, currentElement, false);
                return methodName.ToFirstCharacterUpperCase();// static function is aliased
            }
        }
        throw new InvalidOperationException($"Unable to find factory method for {targetClassName}");
    }

    private string? getSerializerAlias(CodeType propType, CodeFunction codeFunction, string propertySerializerName)
    {
        var parentNameSpace = propType.TypeDefinition?.Parent as CodeNamespace;
        var serializationFunction = parentNameSpace?.FindChildByName<CodeFunction>(propertySerializerName);
        return localConventions?.GetTypeString(new CodeType
        {
            TypeDefinition = serializationFunction
        }, codeFunction, false);
    }
}
