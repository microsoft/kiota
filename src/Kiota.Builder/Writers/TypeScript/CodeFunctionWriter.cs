

using System;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.TypeScript;

public class CodeFunctionWriter : BaseElementWriter<CodeFunction, TypeScriptConventionService>
{
    private TypeScriptConventionService localConventions;
    private readonly CodeUsingWriter _codeUsingWriter;
    public CodeFunctionWriter(TypeScriptConventionService conventionService, string clientNamespaceName) : base(conventionService)
    {
        _codeUsingWriter = new(clientNamespaceName);
    }

    public override void WriteCodeElement(CodeFunction codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        if (codeElement.OriginalLocalMethod == null) throw new InvalidOperationException($"{nameof(codeElement.OriginalLocalMethod)} should not be null");
        ArgumentNullException.ThrowIfNull(writer);
        if (codeElement.Parent is not CodeNamespace) throw new InvalidOperationException("the parent of a function should be a namespace");
        _codeUsingWriter.WriteCodeElement(codeElement.StartBlock.Usings, codeElement.GetImmediateParentOfType<CodeNamespace>(), writer);

        var returnType = conventions.GetTypeString(codeElement.OriginalLocalMethod.ReturnType, codeElement);
        CodeMethodWriter.WriteMethodPrototypeInternal(codeElement.OriginalLocalMethod, writer, returnType, false, conventions, true);

        writer.IncreaseIndent();

        var codeMethod = codeElement.OriginalLocalMethod;
        localConventions = new TypeScriptConventionService(writer);
        if (codeMethod.Kind == CodeMethodKind.Deserializer)
        {
            WriteDeserializerMethod(codeElement, writer);
        }
        else if (codeMethod.Kind == CodeMethodKind.Serializer)
        {
            WriteSerializerMethod(codeElement, writer);
        }
    }

    private void WriteSerializerMethod(CodeFunction codeElement, LanguageWriter writer)
    {
        var param = codeElement.OriginalLocalMethod.Parameters.FirstOrDefault(x => (x.Type as CodeType).TypeDefinition is CodeInterface);
        var codeInterface = (param.Type as CodeType).TypeDefinition as CodeInterface;
        var inherits = codeInterface.StartBlock.Implements.FirstOrDefault(x => x.TypeDefinition is CodeInterface);
        writer.IncreaseIndent();

        if (inherits != null)
        {
            writer.WriteLine($"serialize{inherits.TypeDefinition.Name.ToFirstCharacterUpperCase()}(writer, {param.Name.ToFirstCharacterLowerCase()})");
        }

        foreach (var otherProp in codeInterface.Properties.Where(static x => x.Kind == CodePropertyKind.Custom && !x.ExistsInBaseType))
        {

            WritePropertySerializer(codeInterface.Name.ToFirstCharacterLowerCase(), otherProp, writer, codeElement);
        }

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
        var propType = localConventions.GetTypeString(codeProperty.Type, codeProperty.Parent, false);
        writer.IncreaseIndent();

        var serializationName = GetSerializationMethodName(codeProperty.Type, modelParamName);

        if (serializationName == "writeObjectValue" || serializationName == "writeCollectionOfObjectValues")
        {
            var serializeName = getSerializerAlias(codeProperty.Type as CodeType, codeFunction, $"serialize{propertyTypeName}");
            writer.WriteLine($"writer.{serializationName}<{propType}>(\"{codePropertyName}\", {modelParamName}.{codePropertyName}, {serializeName});");
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(spreadOperator))
            {
                writer.WriteLine($"if({modelParamName}.{codePropertyName})");
            }
            writer.WriteLine($"writer.{serializationName}(\"{codeProperty.SerializationName ?? codePropertyName}\", {spreadOperator}{modelParamName}.{codePropertyName});");
        }
        writer.DecreaseIndent();

    }

    private string GetSerializationMethodName(CodeTypeBase propType, string modelParamName)
    {
        var propertyType = localConventions.TranslateType(propType);
        if (propType is CodeType currentType)
        {
            var result = GetSerializationMethodNameForCodeType(currentType, propertyType, modelParamName);
            if (!String.IsNullOrWhiteSpace(result))
            {
                return result;
            }
        }
        return propertyType switch
        {
            "string" or "boolean" or "number" or "Guid" or "Date" or "DateOnly" or "TimeOnly" or "Duration" => $"write{propertyType.ToFirstCharacterUpperCase()}Value",
            _ => $"writeObjectValue",
        };
    }

    private static string GetSerializationMethodNameForCodeType(CodeType propType, string propertyType, string modelParamName)
    {
        var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
        if (propType.TypeDefinition is CodeEnum currentEnum)
            return $"writeEnumValue<{currentEnum.Name.ToFirstCharacterUpperCase()}>";
        else if (isCollection)
        {
            if (propType.TypeDefinition == null)
                return $"writeCollectionOfPrimitiveValues<{propertyType.ToFirstCharacterLowerCase()}>";
            else
                return $"writeCollectionOfObjectValues";
        }
        return null;
    }

    private void WriteDeserializerMethod(CodeFunction codeElement, LanguageWriter writer)
    {
        var param = codeElement.OriginalLocalMethod.Parameters.FirstOrDefault();

        var codeInterface = (param.Type as CodeType).TypeDefinition as CodeInterface;
        var inherits = codeInterface.StartBlock.Implements.FirstOrDefault(x => x.TypeDefinition is CodeInterface);

        var properties = codeInterface.Properties.Where(static x => x.Kind == CodePropertyKind.Custom && !x.ExistsInBaseType);

        writer.WriteLine("return {");
        writer.IncreaseIndent();
        if (inherits != null)
        {
            writer.WriteLine($"...deserializeInto{inherits.TypeDefinition.Name.ToFirstCharacterUpperCase()}({param.Name.ToFirstCharacterLowerCase()}),");
        }

        foreach (var otherProp in properties)
        {
            writer.WriteLine($"\"{otherProp.SerializationName.ToFirstCharacterLowerCase() ?? otherProp.Name.ToFirstCharacterLowerCase()}\": n => {{ {param.Name.ToFirstCharacterLowerCase()}.{otherProp.Name.ToFirstCharacterLowerCase()} = n.{GetDeserializationMethodName(otherProp.Type, codeElement)}; }},");
        }

        writer.DecreaseIndent();
        writer.WriteLine("}");

    }

    private string GetDeserializationMethodName(CodeTypeBase propType, CodeFunction codeFunction)
    {
        var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
        var propertyType = localConventions.GetTypeString(propType, codeFunction, false);
        if (propType is CodeType currentType)
        {
            if (currentType.TypeDefinition is CodeEnum currentEnum)
                return $"getEnumValue{(currentEnum.Flags || isCollection ? "s" : string.Empty)}<{currentEnum.Name.ToFirstCharacterUpperCase()}>({propertyType.ToFirstCharacterUpperCase()})";
            else if (isCollection)
                if (currentType.TypeDefinition == null)
                    return $"getCollectionOfPrimitiveValues<{propertyType.ToFirstCharacterLowerCase()}>()";
                else
                {
                    var name = getSerializerAlias(propType as CodeType, codeFunction, $"deserializeInto{(propType as CodeType).TypeDefinition.Name}");
                    return $"getCollectionOfObjectValues<{propertyType}>({name})";
                }
        }
        return propertyType switch
        {
            "string" or "boolean" or "number" or "Guid" or "Date" or "DateOnly" or "TimeOnly" or "Duration" => $"get{propertyType.ToFirstCharacterUpperCase()}Value()",
            _ => $"getObjectValue<{propertyType}>({getSerializerAlias(propType as CodeType, codeFunction, $"deserializeInto{(propType as CodeType).TypeDefinition.Name}")})",
        };
    }

    private string getSerializerAlias(CodeType propType, CodeFunction codeFunction, string propertySerializerName)
    {
        var serializationFunction = (propType.TypeDefinition.Parent as CodeNamespace).FindChildByName<CodeFunction>(propertySerializerName);
        return localConventions.GetTypeString(new CodeType
        {
            TypeDefinition = serializationFunction
        }, codeFunction, false);
    }
}
