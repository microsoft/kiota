using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Kiota.Builder.OrderComparers;
namespace Kiota.Builder.Writers.Php;

public class CodeMethodWriter : BaseElementWriter<CodeMethod, PhpConventionService>
{

    protected bool UseBackingStore
    {
        get; init;
    }
    public CodeMethodWriter(PhpConventionService conventionService, bool useBackingStore = false) : base(conventionService)
    {
        UseBackingStore = useBackingStore;
    }

    private const string RequestInfoVarName = "$requestInfo";
    private const string CreateDiscriminatorMethodName = "createFromDiscriminatorValue";
    public override void WriteCodeElement(CodeMethod codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        if (codeElement.Parent is not CodeClass parentClass) throw new InvalidOperationException("the parent of a method should be a class");
        var returnType = codeElement.Kind == CodeMethodKind.Constructor ? "void" : conventions.GetTypeString(codeElement.ReturnType, codeElement);
        var inherits = parentClass.StartBlock.Inherits != null;
        var extendsModelClass = inherits && parentClass.StartBlock.Inherits?.TypeDefinition is CodeClass codeClass &&
                                    codeClass.IsOfKind(CodeClassKind.Model);
        var requestBodyParam = codeElement.Parameters.OfKind(CodeParameterKind.RequestBody);
        var config = codeElement.Parameters.OfKind(CodeParameterKind.RequestConfiguration);
        var requestContentType = codeElement.Parameters.OfKind(CodeParameterKind.RequestBodyContentType);
        var requestParams = new RequestParams(requestBodyParam, config, requestContentType);

        WriteMethodPhpDocs(codeElement, writer);
        WriteMethodsAndParameters(codeElement, writer, codeElement.IsOfKind(CodeMethodKind.Constructor, CodeMethodKind.ClientConstructor));

        switch (codeElement.Kind)
        {
            case CodeMethodKind.ErrorMessageOverride:
                WriteErrorMessageOverride(parentClass, writer);
                break;
            case CodeMethodKind.Constructor:
                WriteConstructorBody(parentClass, codeElement, writer, inherits);
                break;
            case CodeMethodKind.Serializer:
                WriteSerializerBody(parentClass, writer, extendsModelClass);
                break;
            case CodeMethodKind.Setter:
                WriteSetterBody(writer, codeElement, parentClass);
                break;
            case CodeMethodKind.Getter:
                WriteGetterBody(writer, codeElement, parentClass);
                break;
            case CodeMethodKind.Deserializer:
                WriteDeserializerBody(parentClass, writer, codeElement, extendsModelClass);
                break;
            case CodeMethodKind.RequestBuilderWithParameters:
                WriteRequestBuilderWithParametersBody(returnType, writer, codeElement);
                break;
            case CodeMethodKind.RequestGenerator:
                WriteRequestGeneratorBody(codeElement, requestParams, parentClass, writer);
                break;
            case CodeMethodKind.RawUrlBuilder:
                WriteRawUrlBuilderBody(parentClass, codeElement, writer);
                break;
            case CodeMethodKind.ClientConstructor:
                WriteConstructorBody(parentClass, codeElement, writer, inherits);
                WriteApiConstructorBody(parentClass, codeElement, writer);
                break;
            case CodeMethodKind.IndexerBackwardCompatibility:
                WriteIndexerBody(codeElement, parentClass, returnType, writer);
                break;
            case CodeMethodKind.RequestExecutor:
                WriteRequestExecutorBody(codeElement, parentClass, requestParams, writer);
                break;
            case CodeMethodKind.Factory:
                WriteFactoryMethodBody(codeElement, parentClass, writer);
                break;
        }
        writer.CloseBlock();
        writer.WriteLine();
    }

    private void WriteErrorMessageOverride(CodeClass parentClass, LanguageWriter writer)
    {
        if (parentClass.IsErrorDefinition && parentClass.GetPrimaryMessageCodePath(static x => x.Name.ToFirstCharacterLowerCase(), static x => x.Name.ToFirstCharacterLowerCase() + "()", pathSegment: "->") is { } primaryMessageCodePath && !string.IsNullOrEmpty(primaryMessageCodePath))
        {
            var withoutMessage = primaryMessageCodePath.TrimEnd("->getMessage()".ToCharArray()) + "()";
            var primaryErrorVariableName = "$primaryError";
            writer.WriteLine($"{primaryErrorVariableName} = $this->{withoutMessage};");
            writer.WriteLine($"if ({primaryErrorVariableName} !== null) {{");
            writer.IncreaseIndent();
            writer.WriteLine($"return {primaryErrorVariableName}->getMessage() ?? '';");
            writer.CloseBlock();
            writer.WriteLine("return '';");
        }
        else
        {
            writer.WriteLine("return parent::getMessage();");
        }
    }
    private const string UrlTemplateTempVarName = "$urlTplParams";
    private const string RawUrlParameterKey = "request-raw-url";
    private static readonly Dictionary<CodeParameterKind, CodePropertyKind> propertiesToAssign = new Dictionary<CodeParameterKind, CodePropertyKind>()
    {
        { CodeParameterKind.QueryParameter, CodePropertyKind.QueryParameters }, // Handles query parameter object as a constructor param in request config classes
    };
    private void WriteRawUrlBuilderBody(CodeClass parentClass, CodeMethod codeElement, LanguageWriter writer)
    {
        var rawUrlParameter = codeElement.Parameters.OfKind(CodeParameterKind.RawUrl) ?? throw new InvalidOperationException("RawUrlBuilder method should have a RawUrl parameter");
        var requestAdapterProperty = parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter) ?? throw new InvalidOperationException("RawUrlBuilder method should have a RequestAdapter property");
        writer.WriteLine($"return new {parentClass.Name.ToFirstCharacterUpperCase()}(${rawUrlParameter.Name.ToFirstCharacterLowerCase()}, $this->{requestAdapterProperty.Name.ToFirstCharacterLowerCase()});");
    }

    private static void WriteConstructorParentCall(CodeClass parentClass, CodeMethod currentMethod, LanguageWriter writer)
    {
        var requestAdapterParameter = currentMethod.Parameters.OfKind(CodeParameterKind.RequestAdapter);
        var requestOptionParameter = currentMethod.Parameters.OfKind(CodeParameterKind.Options);
        var requestHeadersParameter = currentMethod.Parameters.OfKind(CodeParameterKind.Headers);
        var pathParametersProperty = parentClass.Properties.FirstOrDefaultOfKind(CodePropertyKind.PathParameters);
        var urlTemplateProperty = parentClass.Properties.FirstOrDefaultOfKind(CodePropertyKind.UrlTemplate);

        if (parentClass.IsOfKind(CodeClassKind.RequestBuilder))
        {
            writer.WriteLine($"parent::__construct(${(requestAdapterParameter?.Name ?? "requestAdapter")}, {(pathParametersProperty?.DefaultValue ?? "[]")}, {(urlTemplateProperty?.DefaultValue.ReplaceDoubleQuoteWithSingleQuote() ?? "")});");
        }
        else if (parentClass.IsOfKind(CodeClassKind.RequestConfiguration))
            writer.WriteLine($"parent::__construct(${(requestHeadersParameter?.Name ?? "headers")} ?? [], ${(requestOptionParameter?.Name ?? "options")} ?? []);");
        else
            writer.WriteLine("parent::__construct();");

    }

    private void WriteModelConstructorBody(CodeClass parentClass, LanguageWriter writer)
    {
        var backingStoreProperty = parentClass.GetPropertyOfKind(CodePropertyKind.BackingStore);
        if (backingStoreProperty != null && !string.IsNullOrEmpty(backingStoreProperty.DefaultValue))
            writer.WriteLine($"$this->{backingStoreProperty.Name.ToFirstCharacterLowerCase()} = {backingStoreProperty.DefaultValue};");
        foreach (var propWithDefault in parentClass.GetPropertiesOfKind(CodePropertyKind.AdditionalData, CodePropertyKind.Custom) //additional data and custom properties rely on accessors
            .Where(static x => !string.IsNullOrEmpty(x.DefaultValue))
            // do not apply the default value if the type is composed as the default value may not necessarily which type to use
            .Where(static x => x.Type is not CodeType propType || propType.TypeDefinition is not CodeClass propertyClass || propertyClass.OriginalComposedType is null)
            .OrderBy(static x => x.Name))
        {
            var setterName = propWithDefault.SetterFromCurrentOrBaseType?.Name.ToFirstCharacterLowerCase() is string sName && !string.IsNullOrEmpty(sName) ? sName : $"set{propWithDefault.Name.ToFirstCharacterUpperCase()}";
            var defaultValue = propWithDefault.DefaultValue.ReplaceDoubleQuoteWithSingleQuote();
            if (propWithDefault.Type is CodeType codeType && codeType.TypeDefinition is CodeEnum enumDefinition)
            {
                defaultValue = $"new {enumDefinition.Name.ToFirstCharacterUpperCase()}({defaultValue})";
            }
            else if (propWithDefault.Type.IsNullable &&
                defaultValue.TrimQuotes().Equals(NullValueString, StringComparison.OrdinalIgnoreCase))
            { // avoid setting null as a string.
                defaultValue = NullValueString;
            }
            else if (propWithDefault.Type is CodeType propType && propType.Name.Equals("boolean", StringComparison.OrdinalIgnoreCase))
            {
                defaultValue = defaultValue.TrimQuotes();
            }
            writer.WriteLine($"$this->{setterName}({defaultValue});");
        }
    }

    private const string NullValueString = "null";
    private void WriteRequestBuilderConstructorBody(CodeClass parentClass, CodeMethod currentMethod, LanguageWriter writer)
    {
        foreach (var propWithDefault in parentClass.GetPropertiesOfKind(
                CodePropertyKind.RequestBuilder)
            .Where(x => !string.IsNullOrEmpty(x.DefaultValue))
            .OrderByDescending(x => x.Kind)
            .ThenBy(x => x.Name))
        {
            var isPathSegment = propWithDefault.IsOfKind(CodePropertyKind.PathParameters);
            writer.WriteLine($"$this->{propWithDefault.Name.ToFirstCharacterLowerCase()} = {(isPathSegment ? "[]" : propWithDefault.DefaultValue.ReplaceDoubleQuoteWithSingleQuote())};");
        }
        // Set path parameters property
        if (currentMethod.IsOfKind(CodeMethodKind.Constructor) &&
                currentMethod.Parameters.OfKind(CodeParameterKind.PathParameters) is CodeParameter pathParametersParameter &&
                parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters) is CodeProperty pathParametersProperty
            )
        {
            var pathParametersParameterName = conventions.GetParameterName(pathParametersParameter);
            writer.StartBlock($"if (is_array({pathParametersParameterName})) {{");
            WritePathParametersOptions(currentMethod, parentClass, pathParametersParameter, writer);
            writer.CloseBlock("} else {");
            writer.IncreaseIndent();
            writer.WriteLine($"{GetPropertyCall(pathParametersProperty, "[]")} = ['{RawUrlParameterKey}' => {conventions.GetParameterName(pathParametersParameter)}];");
            writer.CloseBlock();
        }
    }

    private void WriteRequestConfigurationConstructorBody(CodeClass parentClass, CodeMethod currentMethod, LanguageWriter writer)
    {
        foreach (var parameterKind in propertiesToAssign.Keys)
        {
            AssignPropertyFromParameter(parentClass, currentMethod, parameterKind, propertiesToAssign[parameterKind], writer);
        }
    }
    private void WriteQueryParameterConstructorBody(CodeClass parentClass, CodeMethod currentMethod, LanguageWriter writer)
    {
        // Handles various query parameter properties in query parameter classes
        // Not in propertiesToAssign because CodeParameterKind.QueryParameter key is already used
        AssignPropertyFromParameter(parentClass, currentMethod, CodeParameterKind.QueryParameter, CodePropertyKind.QueryParameter, writer);
    }

    private void WriteConstructorBody(CodeClass parentClass, CodeMethod currentMethod, LanguageWriter writer, bool inherits)
    {
        if (inherits)
        {
            WriteConstructorParentCall(parentClass, currentMethod, writer);
        }
        switch (parentClass.Kind)
        {
            case CodeClassKind.Model:
                WriteModelConstructorBody(parentClass, writer);
                break;
            case CodeClassKind.RequestBuilder:
                WriteRequestBuilderConstructorBody(parentClass, currentMethod, writer);
                break;
            case CodeClassKind.RequestConfiguration:
                WriteRequestConfigurationConstructorBody(parentClass, currentMethod, writer);
                break;
            case CodeClassKind.QueryParameters:
                WriteQueryParameterConstructorBody(parentClass, currentMethod, writer);
                break;
            default:
                writer.WriteLine("");
                break;
        }
    }
    private void WritePathParametersOptions(CodeMethod currentMethod, CodeClass parentClass, CodeParameter pathParameter, LanguageWriter writer)
    {
        var pathParametersProperty = parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters);

        if (pathParametersProperty != null && !currentMethod.Parameters.Any(static x => x.IsOfKind(CodeParameterKind.Path)))
        {
            writer.WriteLine($"{GetPropertyCall(pathParametersProperty, "[]")} = {conventions.GetParameterName(pathParameter)};");
            return;
        }

        writer.WriteLine($"{UrlTemplateTempVarName} = {conventions.GetParameterName(pathParameter)};");
        currentMethod.Parameters.Where(static parameter => parameter.IsOfKind(CodeParameterKind.Path)).ToList()
            .ForEach(parameter =>
            {
                var key = string.IsNullOrEmpty(parameter.SerializationName)
                    ? parameter.Name
                    : parameter.SerializationName;
                writer.WriteLine($"{UrlTemplateTempVarName}['{key}'] = ${parameter.Name.ToFirstCharacterLowerCase()};");
            });
        if (pathParametersProperty != null)
            writer.WriteLine(
                $"{GetPropertyCall(pathParametersProperty, "[]")} = {UrlTemplateTempVarName};");
    }
    private static void AssignPropertyFromParameter(CodeClass parentClass, CodeMethod currentMethod, CodeParameterKind parameterKind, CodePropertyKind propertyKind, LanguageWriter writer)
    {
        var parameters = currentMethod.Parameters.Where(x => x.IsOfKind(parameterKind)).ToList();
        var properties = parentClass.GetPropertiesOfKind(propertyKind).ToList();
        if (parameters.Count != 0 && parameters.Count.Equals(properties.Count))
        {
            for (var i = 0; i < parameters.Count; i++)
            {
                var isNonNullableCollection = !parameters[i].Type.IsNullable && parameters[i].Type.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
                writer.WriteLine($"$this->{properties[i].Name.ToFirstCharacterLowerCase()} = ${parameters[i].Name.ToFirstCharacterLowerCase()}{(isNonNullableCollection ? "?? []" : string.Empty)};");
            }
        }
    }

    private void WriteMethodPhpDocs(CodeMethod codeMethod, LanguageWriter writer)
    {
        var methodDescription = codeMethod.Documentation.GetDescription(x => conventions.GetTypeString(x, codeMethod), normalizationFunc: PhpConventionService.RemoveInvalidDescriptionCharacters);
        var methodThrows = codeMethod.IsOfKind(CodeMethodKind.RequestExecutor);
        var hasMethodDescription = !string.IsNullOrEmpty(methodDescription.Trim());
        if (!hasMethodDescription && !codeMethod.Parameters.Any())
        {
            return;
        }
        var isVoidable = "void".Equals(conventions.GetTypeString(codeMethod.ReturnType, codeMethod),
            StringComparison.OrdinalIgnoreCase) && !codeMethod.IsOfKind(CodeMethodKind.RequestExecutor);

        var parametersWithOrWithoutDescription = codeMethod.Parameters
            .Select(x => GetParameterDocString(codeMethod, x))
            .ToList();
        var returnDocString = GetDocCommentReturnType(codeMethod);
        if (!isVoidable)
        {
            var nullableSuffix = codeMethod.ReturnType.IsNullable ? "|null" : "";
            returnDocString = (codeMethod.Kind == CodeMethodKind.RequestExecutor)
                ? $"@return Promise<{returnDocString}|null>"
                : $"@return {returnDocString}{nullableSuffix}";
        }
        else returnDocString = string.Empty;

        var throwsArray = methodThrows ? ["@throws Exception"] : Array.Empty<string>();
        conventions.WriteLongDescription(codeMethod,
            writer,
            parametersWithOrWithoutDescription.Union(new[] { returnDocString }).Union(throwsArray)
            );

    }

    private string GetDocCommentReturnType(CodeMethod codeMethod)
    {
        return codeMethod.Kind switch
        {
            CodeMethodKind.Deserializer => "array<string, callable(ParseNode): void>",
            CodeMethodKind.Getter when codeMethod.AccessedProperty?.IsOfKind(CodePropertyKind.AdditionalData) ?? false => "array<string, mixed>",
            CodeMethodKind.Getter when codeMethod.AccessedProperty?.Type.IsCollection ?? false => $"array<{conventions.TranslateType(codeMethod.AccessedProperty.Type)}>",
            CodeMethodKind.RequestExecutor when codeMethod.ReturnType.IsCollection => $"array<{conventions.TranslateType(codeMethod.ReturnType)}>",
            _ => conventions.GetTypeString(codeMethod.ReturnType, codeMethod)
        };
    }

    private string GetParameterDocString(CodeMethod codeMethod, CodeParameter x)
    {
        if (codeMethod.IsOfKind(CodeMethodKind.Setter)
            && (codeMethod.AccessedProperty?.IsOfKind(CodePropertyKind.AdditionalData) ?? false))
        {
            return $"@param array<string,mixed> $value {x?.Documentation.GetDescription(x => conventions.GetTypeString(x, codeMethod), normalizationFunc: PhpConventionService.RemoveInvalidDescriptionCharacters)}";
        }
        return $"@param {conventions.GetParameterDocNullable(x, x)} {x?.Documentation.GetDescription(x => conventions.GetTypeString(x, codeMethod), normalizationFunc: PhpConventionService.RemoveInvalidDescriptionCharacters)}";
    }

    private static readonly BaseCodeParameterOrderComparer parameterOrderComparer = new();
    private void WriteMethodsAndParameters(CodeMethod codeMethod, LanguageWriter writer, bool isConstructor = false)
    {
        var methodParameters = string.Join(", ", codeMethod.Parameters
                                                            .Order(parameterOrderComparer)
                                                            .Select(x => conventions.GetParameterSignature(x, codeMethod))
                                                            .ToList());

        var methodName = codeMethod.Kind switch
        {
            CodeMethodKind.Constructor or CodeMethodKind.ClientConstructor => "__construct",
            _ => codeMethod.Name.ToFirstCharacterLowerCase()
        };
        var isVoid = "void".Equals(conventions.GetTypeString(codeMethod.ReturnType, codeMethod), StringComparison.OrdinalIgnoreCase);
        var optionalCharacterReturn = (codeMethod.ReturnType.IsNullable && !isVoid) ? "?" : "";
        var returnValue = (codeMethod.Kind == CodeMethodKind.RequestExecutor) ? "Promise" : $"{optionalCharacterReturn}{conventions.GetTypeString(codeMethod.ReturnType, codeMethod)}";
        writer.WriteLine($"{conventions.GetAccessModifier(codeMethod.Access)} {(codeMethod.IsStatic ? "static " : string.Empty)}"
            + $"function {methodName}({methodParameters}){(isConstructor ? "" : $": {returnValue}")} {{");
        writer.IncreaseIndent();
    }

    private void WriteSerializerBody(CodeClass parentClass, LanguageWriter writer, bool extendsModelClass = false)
    {
        if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForUnionType)
            WriteSerializerBodyForUnionModel(parentClass, writer);
        else if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForIntersectionType)
            WriteSerializerBodyForIntersectionModel(parentClass, writer);
        else
            WriteSerializerBodyForInheritedModel(parentClass, writer, extendsModelClass);

        if (parentClass.GetPropertiesOfKind(CodePropertyKind.AdditionalData).FirstOrDefault() is CodeProperty additionalDataProperty &&
            additionalDataProperty.Getter != null)
            writer.WriteLine($"$writer->writeAdditionalData($this->{additionalDataProperty.Getter.Name}());");
    }

    private void WriteSerializerBodyForIntersectionModel(CodeClass parentClass, LanguageWriter writer)
    {
        var includeElse = false;
        var otherProps = parentClass
                                .GetPropertiesOfKind(CodePropertyKind.Custom)
                                .Where(static x => !x.ExistsInBaseType && x.Getter != null)
                                .Where(static x => x.Type is not CodeType propertyType || propertyType.IsCollection || propertyType.TypeDefinition is not CodeClass)
                                .Order(CodePropertyTypeBackwardComparer)
                                .ThenBy(static x => x.Name)
                                .ToArray();
        foreach (var otherProp in otherProps)
        {
            writer.StartBlock($"{(includeElse ? "} else " : string.Empty)}if ($this->{otherProp.Getter!.Name.ToFirstCharacterLowerCase()}() !== null) {{");
            WriteSerializationMethodCall(otherProp, writer, "null");
            writer.DecreaseIndent();
            if (!includeElse)
                includeElse = true;
        }
        var complexProperties = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)
                                            .Where(static x => x.Getter != null)
                                            .Where(static x => x.Type is CodeType { TypeDefinition: CodeClass } && !x.Type.IsCollection)
                                            .ToArray();
        if (complexProperties.Length != 0)
        {
            if (includeElse)
            {
                writer.StartBlock("} else {");
            }
            var propertiesNames = complexProperties
                                .Select(static x => $"$this->{x.Getter!.Name.ToFirstCharacterLowerCase()}()")
                                .Order(StringComparer.OrdinalIgnoreCase)
                                .Aggregate(static (x, y) => $"{x}, {y}");
            WriteSerializationMethodCall(complexProperties.First(), writer, "null", propertiesNames);
            if (includeElse)
            {
                writer.CloseBlock();
            }
        }
        else if (otherProps.Length != 0)
        {
            writer.CloseBlock(decreaseIndent: false);
        }
    }

    private void WriteSerializerBodyForUnionModel(CodeClass parentClass, LanguageWriter writer)
    {
        var includeElse = false;
        var otherProps = parentClass
            .GetPropertiesOfKind(CodePropertyKind.Custom)
            .Where(static x => !x.ExistsInBaseType && x.Getter != null)
            .Order(CodePropertyTypeForwardComparer)
            .ThenBy(static x => x.Name)
            .ToArray();
        foreach (var otherProp in otherProps)
        {
            writer.StartBlock($"{(includeElse ? "} else " : string.Empty)}if ($this->{otherProp.Getter!.Name.ToFirstCharacterLowerCase()}() !== null) {{");
            WriteSerializationMethodCall(otherProp, writer, "null");
            writer.DecreaseIndent();
            if (!includeElse)
                includeElse = true;
        }
        if (otherProps.Length != 0)
            writer.CloseBlock(decreaseIndent: false);
    }

    private void WriteSerializationMethodCall(CodeProperty otherProp, LanguageWriter writer, string serializationKey, string? dataToSerialize = default)
    {
        if (string.IsNullOrEmpty(dataToSerialize))
            dataToSerialize = $"$this->{(otherProp.Getter?.Name?.ToFirstCharacterLowerCase() is string gName && !string.IsNullOrEmpty(gName) ? gName : "get" + otherProp.Name.ToFirstCharacterUpperCase())}()";
        writer.WriteLine($"$writer->{GetSerializationMethodName(otherProp.Type)}({serializationKey}, {dataToSerialize});");
    }

    private void WriteSerializerBodyForInheritedModel(CodeClass parentClass, LanguageWriter writer, bool extendsModelClass = false)
    {
        if (extendsModelClass)
            writer.WriteLine("parent::serialize($writer);");
        foreach (var otherProp in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom).Where(static x => !x.ExistsInBaseType && !x.ReadOnly))
            WriteSerializationMethodCall(otherProp, writer, $"'{otherProp.WireName}'");
    }

    private string GetSerializationMethodName(CodeTypeBase propType)
    {
        var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
        var propertyType = conventions.TranslateType(propType);
        if (propType is CodeType currentType)
        {
            if (isCollection)
            {
                if (currentType.TypeDefinition is null)
                {
                    return "writeCollectionOfPrimitiveValues";
                }
                return currentType.TypeDefinition is CodeEnum ? "writeCollectionOfEnumValues" : "writeCollectionOfObjectValues";
            }

            if (currentType.TypeDefinition is CodeEnum)
            {
                return "writeEnumValue";
            }
        }

        var lowerCaseProp = propertyType.ToLowerInvariant();
        return lowerCaseProp switch
        {
            "string" or "guid" => "writeStringValue",
            "enum" or "float" or "date" or "time" or "byte" => $"write{lowerCaseProp.ToFirstCharacterUpperCase()}Value",
            "bool" or "boolean" => "writeBooleanValue",
            "double" or "decimal" => "writeFloatValue",
            "datetime" or "datetimeoffset" => "writeDateTimeValue",
            "duration" or "timespan" or "dateinterval" => "writeDateIntervalValue",
            "int" or "number" => "writeIntegerValue",
            "streaminterface" => "writeBinaryContent",
            _ when conventions.PrimitiveTypes.Contains(lowerCaseProp) => $"write{lowerCaseProp.ToFirstCharacterUpperCase()}Value",
            _ => "writeObjectValue"
        };
    }

    private const string ParseNodeVarName = "$parseNode";
    private (string, string) GetDeserializationMethodName(CodeTypeBase propType, CodeMethod method)
    {
        var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
        var propertyType = conventions.GetTypeString(propType, method, false);
        var parseNodeMethod = (string.Empty, string.Empty);
        if (propType is CodeType currentType)
        {
            if (isCollection)
                parseNodeMethod = currentType.TypeDefinition switch
                {
                    CodeEnum enumType => (string.Empty, $"getCollectionOfEnumValues({enumType.Name.ToFirstCharacterUpperCase()}::class)"),
                    CodeClass => (string.Empty, $"getCollectionOfObjectValues([{conventions.TranslateType(propType)}::class, '{CreateDiscriminatorMethodName}'])"),
                    _ => (conventions.TranslateType(propType), $"getCollectionOfPrimitiveValues('{conventions.TranslateType(propType)}')")
                };
            else if (currentType.TypeDefinition is CodeEnum)
                parseNodeMethod = (string.Empty, $"getEnumValue({propertyType.ToFirstCharacterUpperCase()}::class)");
        }

        var lowerCaseType = propertyType.ToLowerInvariant();
        var res = parseNodeMethod;
        if (string.IsNullOrEmpty(parseNodeMethod.Item2))
            res = (string.Empty, lowerCaseType switch
            {
                "int" => "getIntegerValue()",
                "bool" => "getBooleanValue()",
                "number" => "getIntegerValue()",
                "decimal" or "double" => "getFloatValue()",
                "streaminterface" => "getBinaryContent()",
                "byte" => "getByteValue()",
                _ when conventions.PrimitiveTypes.Contains(lowerCaseType) =>
                    $"get{propertyType.ToFirstCharacterUpperCase()}Value()",
                _ =>
                    $"getObjectValue([{propertyType.ToFirstCharacterUpperCase()}::class, '{CreateDiscriminatorMethodName}'])",
            });

        return res;
    }

    private void WriteSetterBody(LanguageWriter writer, CodeMethod codeElement, CodeClass parentClass)
    {
        var propertyName = codeElement.AccessedProperty?.Name.ToFirstCharacterLowerCase();
        var isBackingStoreSetter = codeElement.AccessedProperty?.Kind == CodePropertyKind.BackingStore;
        if (UseBackingStore && !isBackingStoreSetter && parentClass.GetBackingStoreProperty() is CodeProperty backingStoreProperty && backingStoreProperty.Getter != null)
            writer.WriteLine($"$this->{backingStoreProperty.Getter!.Name}()->set('{propertyName.ToFirstCharacterLowerCase()}', $value);");
        else
            writer.WriteLine($"$this->{propertyName.ToFirstCharacterLowerCase()} = $value;");
    }

    private void WriteGetterBody(LanguageWriter writer, CodeMethod codeMethod, CodeClass parentClass)
    {
        var propertyName = codeMethod.AccessedProperty?.Name.ToFirstCharacterLowerCase();
        var isBackingStoreGetter = codeMethod.AccessedProperty?.Kind == CodePropertyKind.BackingStore;
        if (UseBackingStore
            && !isBackingStoreGetter
            && parentClass.GetBackingStoreProperty() is CodeProperty backingStoreProperty
            && backingStoreProperty.Getter != null
            && codeMethod.AccessedProperty is CodeProperty accessedProperty
            && accessedProperty.Type is CodeType propertyType)
        {
            writer.WriteLine($"$val = $this->{backingStoreProperty.Getter!.Name}()->get('{propertyName}');");
            var propertyTypeName = conventions.TranslateType(propertyType);
            var isScalarType = conventions.ScalarTypes.Contains(propertyTypeName);
            if (propertyType.CollectionKind == CodeTypeBase.CodeTypeCollectionKind.None)
            {
                writer.StartBlock($"if (is_null($val) || {(isScalarType ? $"is_{propertyTypeName}($val)" : $"$val instanceof {propertyTypeName}")}) {{");
            }
            else if (accessedProperty.Kind == CodePropertyKind.AdditionalData)
            {
                writer.StartBlock($"if (is_null($val) || is_array($val)) {{");
                writer.WriteLine($"/** @var array<string, mixed>|null $val */");
            }
            else
            {
                writer.StartBlock("if (is_array($val) || is_null($val)) {");
                writer.WriteLine($"TypeUtils::validateCollectionValues($val, {(isScalarType ? $"'{propertyTypeName}'" : $"{propertyTypeName}::class")});");
                writer.WriteLine($"/** @var array<{propertyTypeName}>|null $val */");
            }
            writer.WriteLine("return $val;");
            writer.CloseBlock();
            writer.WriteLine($"throw new \\UnexpectedValueException(\"Invalid type found in backing store for '{propertyName}'\");");
        }
        else
        {
            writer.WriteLine($"return $this->{propertyName};");
        }
    }

    private void WriteRequestBuilderWithParametersBody(string returnType, LanguageWriter writer, CodeMethod codeMethod)
    {
        conventions.AddRequestBuilderBody(returnType, writer, pathParameters: codeMethod.Parameters.Where(static x => x.IsOfKind(CodeParameterKind.Path)));
    }

    private static string GetPropertyCall(CodeProperty property, string defaultValue) => property == null ? defaultValue : $"$this->{property.Name}";
    private void WriteRequestGeneratorBody(CodeMethod codeElement, RequestParams requestParams, CodeClass currentClass, LanguageWriter writer)
    {
        if (codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");
        var requestInformationClass = "RequestInformation";
        writer.WriteLine($"{RequestInfoVarName} = new {requestInformationClass}();");
        if (currentClass.GetPropertyOfKind(CodePropertyKind.PathParameters) is CodeProperty pathParametersProperty &&
            currentClass.GetPropertyOfKind(CodePropertyKind.UrlTemplate) is CodeProperty urlTemplateProperty)
        {
            var urlTemplateValue = codeElement.HasUrlTemplateOverride ? $"'{codeElement.UrlTemplateOverride.SanitizeSingleQuote()}'" : GetPropertyCall(urlTemplateProperty, "''");
            writer.WriteLines($"{RequestInfoVarName}->urlTemplate = {urlTemplateValue};",
                            $"{RequestInfoVarName}->pathParameters = {GetPropertyCall(pathParametersProperty, "''")};");
        }
        writer.WriteLine($"{RequestInfoVarName}->httpMethod = HttpMethod::{codeElement.HttpMethod.Value.ToString().ToUpperInvariant()};");
        WriteRequestConfiguration(requestParams, writer);
        WriteAcceptHeaderDef(codeElement, writer);
        if (requestParams.requestBody != null)
        {
            var suffix = requestParams.requestBody.Type.IsCollection ? "Collection" : string.Empty;
            var sanitizedRequestBodyContentType = codeElement.RequestBodyContentType.SanitizeDoubleQuote();
            if (requestParams.requestBody.Type.Name.Equals(conventions.StreamTypeName, StringComparison.OrdinalIgnoreCase))
            {
                if (requestParams.requestContentType is not null)
                    writer.WriteLine($"{RequestInfoVarName}->setStreamContent({conventions.GetParameterName(requestParams.requestBody)}, {conventions.GetParameterName(requestParams.requestContentType)});");
                else if (!string.IsNullOrEmpty(codeElement.RequestBodyContentType))
                    writer.WriteLine($"{RequestInfoVarName}->setStreamContent({conventions.GetParameterName(requestParams.requestBody)}, \"{sanitizedRequestBodyContentType}\");");
            }
            else if (currentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter) is CodeProperty requestAdapterProperty)
                if (requestParams.requestBody.Type is CodeType bodyType && (bodyType.TypeDefinition is CodeClass || bodyType.Name.Equals("MultiPartBody", StringComparison.OrdinalIgnoreCase)))
                    writer.WriteLine($"{RequestInfoVarName}->setContentFromParsable{suffix}($this->{requestAdapterProperty.Name.ToFirstCharacterLowerCase()}, \"{sanitizedRequestBodyContentType}\", {conventions.GetParameterName(requestParams.requestBody)});");
                else
                    writer.WriteLine($"{RequestInfoVarName}->setContentFromScalar{suffix}($this->{requestAdapterProperty.Name.ToFirstCharacterLowerCase()}, \"{sanitizedRequestBodyContentType}\", {conventions.GetParameterName(requestParams.requestBody)});");
        }

        writer.WriteLine($"return {RequestInfoVarName};");
    }

    private void WriteRequestConfiguration(RequestParams requestParams, LanguageWriter writer)
    {
        if (requestParams.requestConfiguration != null)
        {
            var queryString = requestParams.QueryParameters;
            var headers = requestParams.Headers;
            var options = requestParams.Options;
            var requestConfigParamName = conventions.GetParameterName(requestParams.requestConfiguration);
            writer.StartBlock($"if ({requestConfigParamName} !== null) {{");
            var headersName = $"{requestConfigParamName}->{headers?.Name.ToFirstCharacterLowerCase() ?? "headers"}";
            writer.WriteLine($"{RequestInfoVarName}->addHeaders({headersName});");
            if (queryString != null)
            {
                var queryStringName = $"{requestConfigParamName}->{queryString.Name.ToFirstCharacterLowerCase()}";
                writer.StartBlock($"if ({queryStringName} !== null) {{");
                writer.WriteLine($"{RequestInfoVarName}->setQueryParameters({queryStringName});");
                writer.CloseBlock();
            }
            var optionsName = $"{requestConfigParamName}->{(options?.Name.ToFirstCharacterLowerCase() ?? "options")}";
            writer.WriteLine($"{RequestInfoVarName}->addRequestOptions(...{optionsName});");
            writer.CloseBlock();
        }
    }

    private void WriteAcceptHeaderDef(CodeMethod codeMethod, LanguageWriter writer)
    {
        if (codeMethod.ShouldAddAcceptHeader)
            writer.WriteLine($"{RequestInfoVarName}->tryAddHeader('Accept', \"{codeMethod.AcceptHeaderValue.SanitizeDoubleQuote()}\");");
    }
    private void WriteDeserializerBody(CodeClass parentClass, LanguageWriter writer, CodeMethod method, bool extendsModelClass = false)
    {
        if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForUnionType)
            WriteDeserializerBodyForUnionModel(method, parentClass, writer);
        else if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForIntersectionType)
            WriteDeserializerBodyForIntersectionModel(parentClass, writer);
        else
            WriteDeserializerBodyForInheritedModel(method, parentClass, writer, extendsModelClass);
    }
    private void WriteDeserializerBodyForInheritedModel(CodeMethod method, CodeClass parentClass, LanguageWriter writer, bool extendsModelClass = false)
    {
        var codeProperties = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom).ToArray();
        writer.WriteLine("$o = $this;");
        writer.WriteLines(
            $"return {((extendsModelClass) ? $"array_merge(parent::{method.Name.ToFirstCharacterLowerCase()}(), [" : " [")}");
        writer.IncreaseIndent();
        if (codeProperties.Length != 0)
        {
            codeProperties
                .Where(static x => !x.ExistsInBaseType && x.Setter != null)
                .OrderBy(static x => x.Name)
                .ToList()
                .ForEach(x => WriteDeserializerPropertyCallback(x, method, writer));
        }
        writer.DecreaseIndent();
        writer.WriteLine(extendsModelClass ? "]);" : "];");
    }

    private void WriteDeserializerPropertyCallback(CodeProperty property, CodeMethod method, LanguageWriter writer)
    {
        if (property.Type.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None
            && property.Type is CodeType currentType
            && currentType.TypeDefinition == null)
        {
            writer.StartBlock($"'{property.WireName}' => function (ParseNode $n) {{");
            writer.WriteLine("$val = $n->getCollectionOfPrimitiveValues();");
            writer.StartBlock($"if (is_array($val)) {{");
            var type = conventions.TranslateType(property.Type);
            writer.WriteLine($"TypeUtils::validateCollectionValues($val, '{type}');");
            writer.CloseBlock();
            writer.WriteLine($"/** @var array<{type}>|null $val */");
            writer.WriteLine($"$this->{property.Setter!.Name.ToFirstCharacterLowerCase()}($val);");
            writer.DecreaseIndent();
            writer.WriteLine("},");
            return;
        }
        writer.WriteLine($"'{property.WireName}' => fn(ParseNode $n) => $o->{property.Setter!.Name.ToFirstCharacterLowerCase()}($n->{GetDeserializationMethodName(property.Type, method).Item2}),");
    }

    private static void WriteDeserializerBodyForIntersectionModel(CodeClass parentClass, LanguageWriter writer)
    {
        var complexProperties = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)
            .Where(static x => x.Type is CodeType propType && propType.TypeDefinition is CodeClass && !x.Type.IsCollection)
            .ToArray();
        if (complexProperties.Length != 0)
        {
            var propertiesNames = complexProperties
                .Where(static x => x.Getter != null)
                .Select(static x => x.Getter!.Name.ToFirstCharacterLowerCase())
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var propertiesNamesAsConditions = propertiesNames
                .Select(static x => $"$this->{x}() !== null")
                .Aggregate(static (x, y) => $"{x} || {y}");
            writer.StartBlock($"if ({propertiesNamesAsConditions}) {{");
            var propertiesNamesAsArgument = propertiesNames
                .Select(static x => $"$this->{x}()")
                .Aggregate(static (x, y) => $"{x}, {y}");
            writer.WriteLine($"return ParseNodeHelper::mergeDeserializersForIntersectionWrapper({propertiesNamesAsArgument});");
            writer.CloseBlock();
        }
        writer.WriteLine($"return [];");
    }

    private static void WriteDeserializerBodyForUnionModel(CodeMethod method, CodeClass parentClass, LanguageWriter writer)
    {
        var includeElse = false;
        var otherPropGetters = parentClass
            .GetPropertiesOfKind(CodePropertyKind.Custom)
            .Where(static x => !x.ExistsInBaseType && x.Getter != null)
            .Where(static x => x.Type is CodeType propertyType && !propertyType.IsCollection && propertyType.TypeDefinition is CodeClass)
            .Order(CodePropertyTypeForwardComparer)
            .ThenBy(static x => x.Name)
            .Select(static x => x.Getter!.Name.ToFirstCharacterLowerCase())
            .ToArray();
        foreach (var otherPropGetter in otherPropGetters)
        {
            writer.StartBlock($"{(includeElse ? "} else " : string.Empty)}if ($this->{otherPropGetter}() !== null) {{");
            writer.WriteLine($"return $this->{otherPropGetter}()->{method.Name.ToFirstCharacterLowerCase()}();");
            writer.DecreaseIndent();
            if (!includeElse)
                includeElse = true;
        }
        if (otherPropGetters.Length != 0)
            writer.CloseBlock(decreaseIndent: false);
        writer.WriteLine($"return [];");
    }

    private void WriteIndexerBody(CodeMethod codeElement, CodeClass parentClass, string returnType, LanguageWriter writer)
    {
        var pathParameters = codeElement.Parameters.Where(static x => x.IsOfKind(CodeParameterKind.Path));
        if (parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters) is CodeProperty pathParametersProperty &&
            codeElement.OriginalIndexer != null)
            conventions.AddParametersAssignment(writer, pathParametersProperty.Type, $"$this->{pathParametersProperty.Name}",
                (codeElement.OriginalIndexer.IndexParameter.Type, codeElement.OriginalIndexer.IndexParameter.SerializationName, $"${codeElement.OriginalIndexer.IndexParameter.Name.ToFirstCharacterLowerCase()}"));
        conventions.AddRequestBuilderBody(parentClass, returnType, writer, conventions.TempDictionaryVarName, pathParameters);
    }

    private void WriteRequestExecutorBody(CodeMethod codeElement, CodeClass parentClass, RequestParams requestParams, LanguageWriter writer)
    {
        var generatorMethod = parentClass
            .Methods
            .FirstOrDefault(x =>
                x.IsOfKind(CodeMethodKind.RequestGenerator) && x.HttpMethod == codeElement.HttpMethod);
        var generatorMethodName = generatorMethod?.Name.ToFirstCharacterLowerCase();
        var requestInfoParameters = new CodeParameter?[] { requestParams.requestBody, requestParams.requestContentType, requestParams.requestConfiguration }
            .OfType<CodeParameter>()
            .Select(conventions.GetParameterName)
            .ToArray();
        var joinedParams = string.Empty;
        if (requestInfoParameters.Length != 0)
        {
            joinedParams = string.Join(", ", requestInfoParameters);
        }

        var returnTypeName = conventions.GetTypeString(codeElement.ReturnType, codeElement, false);
        writer.WriteLine($"$requestInfo = $this->{generatorMethodName}({joinedParams});");
        var errorMappings = codeElement.ErrorMappings;
        var hasErrorMappings = false;
        var errorMappingsVarName = "$errorMappings";
        if (errorMappings != null && errorMappings.Any())
        {
            hasErrorMappings = true;
            writer.WriteLine($"{errorMappingsVarName} = [");
            writer.IncreaseIndent(2);
            errorMappings.ToList().ForEach(errorMapping =>
            {
                writer.WriteLine($"'{errorMapping.Key}' => [{errorMapping.Value.Name.ToFirstCharacterUpperCase()}::class, '{CreateDiscriminatorMethodName}'],");
            });
            writer.DecreaseIndent();
            writer.WriteLine("];");
        }

        var returnsVoid = returnTypeName.Equals("void", StringComparison.OrdinalIgnoreCase);
        var isStream = returnTypeName.Equals(conventions.StreamTypeName, StringComparison.OrdinalIgnoreCase);
        var isCollection = codeElement.ReturnType.IsCollection;
        var isEnum = codeElement.ReturnType is CodeType returnType && returnType.TypeDefinition is CodeEnum;
        var methodName = GetSendRequestMethodName(returnsVoid, isStream, isCollection, isEnum, returnTypeName);
        var returnTypeFactory = codeElement.ReturnType is CodeType rt && rt.TypeDefinition is CodeClass
            ? $", [{returnTypeName}::class, '{CreateDiscriminatorMethodName}']"
            : string.Empty;
        var returnWithCustomType =
            !returnsVoid && string.IsNullOrEmpty(returnTypeFactory) && conventions.CustomTypes.Contains(returnTypeName)
                ? $", {returnTypeName}::class"
                : returnTypeFactory;
        var returnEnumType = codeElement.ReturnType is CodeType codeType && codeType.TypeDefinition is CodeEnum
            ? $", {returnTypeName}::class"
            : returnWithCustomType;
        var finalReturn = string.IsNullOrEmpty(returnEnumType) && !returnsVoid
            ? $", '{returnTypeName}'"
            : returnEnumType;
        var requestAdapterProperty = parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter) ?? throw new InvalidOperationException("Request adapter property not found");

        var methodCall = $"{GetPropertyCall(requestAdapterProperty, string.Empty)}->{methodName}({RequestInfoVarName}{finalReturn}, {(hasErrorMappings ? $"{errorMappingsVarName}" : "null")});";
        if (methodName.Contains("sendPrimitive", StringComparison.OrdinalIgnoreCase))
        {
            writer.WriteLines($"/** @var Promise<{GetDocCommentReturnType(codeElement)}|null> $result */",
                $"$result = {methodCall}",
                "return $result;"
                );
        }
        else writer.WriteLines(
            $"return {methodCall}");
    }

    private static void WriteApiConstructorBody(CodeClass parentClass, CodeMethod codeMethod, LanguageWriter writer)
    {
        WriteSerializationRegistration(codeMethod.SerializerModules, writer, "registerDefaultSerializer");
        WriteSerializationRegistration(codeMethod.DeserializerModules, writer, "registerDefaultDeserializer");
        if (parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter) is not CodeProperty requestAdapterProperty) return;
        if (!string.IsNullOrEmpty(codeMethod.BaseUrl))
        {
            writer.StartBlock($"if (empty({GetPropertyCall(requestAdapterProperty, string.Empty)}->getBaseUrl())) {{");
            writer.WriteLine($"{GetPropertyCall(requestAdapterProperty, string.Empty)}->setBaseUrl('{codeMethod.BaseUrl}');");
            writer.CloseBlock();
            if (parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters) is CodeProperty pathParametersProperty)
                writer.WriteLine($"{GetPropertyCall(pathParametersProperty, string.Empty)}['baseurl'] = {GetPropertyCall(requestAdapterProperty, string.Empty)}->getBaseUrl();");
        }
        if (codeMethod.Parameters.OfKind(CodeParameterKind.BackingStore) is CodeParameter backingStoreParam)
            writer.WriteLine($"{GetPropertyCall(requestAdapterProperty, string.Empty)}->enableBackingStore(${backingStoreParam.Name} ?? BackingStoreFactorySingleton::getInstance());");
    }

    private static void WriteSerializationRegistration(HashSet<string> serializationModules, LanguageWriter writer, string methodName)
    {
        if (serializationModules != null)
            foreach (var module in serializationModules)
                writer.WriteLine($"ApiClientBuilder::{methodName}({module}::class);");
    }

    protected string GetSendRequestMethodName(bool isVoid, bool isStream, bool isCollection, bool isEnum, string returnType)
    {
        if (isVoid) return "sendNoContentAsync";
        if (isStream || isEnum || conventions.PrimitiveTypes.Contains(returnType))
            if (isCollection)
                return "sendPrimitiveCollectionAsync";
            else
                return "sendPrimitiveAsync";
        if (isCollection) return "sendCollectionAsync";
        return "sendAsync";
    }

    private const string DiscriminatorMappingVarName = "$mappingValue";
    private const string ResultVarName = "$result";

    private void WriteFactoryMethodBody(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer)
    {
        switch (parentClass.Kind)
        {
            case CodeClassKind.Model:
                WriteModelFactoryMethodBody(codeElement, parentClass, writer);
                break;
            default:
                var parameterNames = string.Join(", ", codeElement.Parameters.Order(parameterOrderComparer).Select(x => $"${x.Name.ToFirstCharacterLowerCase()}"));
                writer.WriteLine($"return new {conventions.GetTypeString(codeElement.ReturnType, codeElement)}({parameterNames});");
                break;
        }
    }

    private void WriteModelFactoryMethodBody(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer)
    {
        if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForUnionType || parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForIntersectionType)
            writer.WriteLine($"{ResultVarName} = new {parentClass.Name.ToFirstCharacterUpperCase()}();");
        var writeDiscriminatorValueRead = parentClass.DiscriminatorInformation is { ShouldWriteParseNodeCheck: true, ShouldWriteDiscriminatorForIntersectionType: false, HasBasicDiscriminatorInformation: true };

        if (writeDiscriminatorValueRead &&
            codeElement.Parameters.OfKind(CodeParameterKind.ParseNode) is CodeParameter parseNodeParameter)
        {
            writer.WriteLines($"$mappingValueNode = ${parseNodeParameter.Name.ToFirstCharacterLowerCase()}->getChildNode(\"{parentClass.DiscriminatorInformation.DiscriminatorPropertyName}\");",
                "if ($mappingValueNode !== null) {");
            writer.IncreaseIndent();
            writer.WriteLines($"{DiscriminatorMappingVarName} = $mappingValueNode->getStringValue();");
        }

        if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForInheritedType)
            WriteFactoryMethodBodyForInheritedModel(parentClass.DiscriminatorInformation.DiscriminatorMappings, writer, codeElement);
        else if (parentClass.DiscriminatorInformation is { ShouldWriteDiscriminatorForUnionType: true, HasBasicDiscriminatorInformation: true })
            WriteFactoryMethodBodyForUnionModelForDiscriminatedTypes(codeElement, parentClass, writer);
        else if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForIntersectionType)
            WriteFactoryMethodBodyForIntersectionModel(codeElement, parentClass, writer);

        if (writeDiscriminatorValueRead)
        {
            writer.CloseBlock();
        }
        if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForUnionType || parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForIntersectionType)
        {
            if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForUnionType)
                WriteFactoryMethodBodyForUnionModelForUnDiscriminatedTypes(codeElement, parentClass, writer);
            writer.WriteLine($"return {ResultVarName};");
        }
        else
            writer.WriteLine($"return new {parentClass.Name.ToFirstCharacterUpperCase()}();");
    }

    private void WriteFactoryMethodBodyForIntersectionModel(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer)
    {
        var includeElse = false;
        var otherProps = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)
                                .Where(static x => x.Setter != null)
                                .Where(static x => x.Type is not CodeType propertyType || propertyType.IsCollection || propertyType.TypeDefinition is not CodeClass)
                                .Order(CodePropertyTypeBackwardComparer)
                                .ThenBy(static x => x.Name)
                                .ToArray();
        foreach (var property in otherProps)
        {
            if (property.Type is CodeType propertyType)
            {
                var methodName = GetDeserializationMethodName(propertyType, codeElement);
                var deserializationMethodName = $"{ParseNodeVarName}->{methodName.Item2}";
                writer.StartBlock($"{(includeElse ? "} else " : string.Empty)}if ({deserializationMethodName} !== null) {{");
                writer.WriteLine($"{ResultVarName}->{property.Setter!.Name.ToFirstCharacterLowerCase()}({deserializationMethodName});");
                writer.DecreaseIndent();
            }
            if (!includeElse)
                includeElse = true;
        }
        var complexProperties = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)
                                        .Where(static x => x.Setter != null && x.Type is CodeType)
                                        .Select(static x => new Tuple<CodeProperty, CodeType>(x, (CodeType)x.Type))
                                        .Where(static x => x.Item2.TypeDefinition is CodeClass && !x.Item2.IsCollection)
                                        .ToArray();
        if (complexProperties.Length != 0)
        {
            if (includeElse)
                writer.StartBlock("} else {");
            foreach (var property in complexProperties)
                writer.WriteLine($"{ResultVarName}->{property.Item1.Setter!.Name.ToFirstCharacterLowerCase()}(new {conventions.GetTypeString(property.Item2, codeElement, false)}());");
            if (includeElse)
                writer.CloseBlock();
        }
        else if (otherProps.Length != 0)
            writer.CloseBlock(decreaseIndent: false);
    }

    private void WriteFactoryMethodBodyForUnionModelForDiscriminatedTypes(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer)
    {
        var includeElse = false;
        var otherProps = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)
            .Where(static x => x.Setter != null && x.Type is CodeType)
            .Where(static x => x.Type is CodeType { IsCollection: false, TypeDefinition: CodeClass or CodeInterface })
            .Order(CodePropertyTypeForwardComparer)
            .ThenBy(static x => x.Name)
            .ToArray();
        foreach (var property in otherProps)
        {
            var propertyType = (CodeType)property.Type;
            if (propertyType.TypeDefinition is CodeInterface { OriginalClass: { } } typeInterface)
                propertyType = new CodeType
                {
                    Name = typeInterface.OriginalClass.Name,
                    TypeDefinition = typeInterface.OriginalClass,
                    CollectionKind = propertyType.CollectionKind,
                    IsNullable = propertyType.IsNullable,
                };
            var mappedType = parentClass.DiscriminatorInformation.DiscriminatorMappings.FirstOrDefault(x => x.Value.Name.Equals(propertyType.Name, StringComparison.OrdinalIgnoreCase));
            writer.StartBlock($"{(includeElse ? "} else " : string.Empty)}if ('{mappedType.Key}' === {DiscriminatorMappingVarName}) {{");
            writer.WriteLine($"{ResultVarName}->{property.Setter!.Name.ToFirstCharacterLowerCase()}(new {conventions.GetTypeString(propertyType, codeElement, false)}());");
            writer.DecreaseIndent();
            if (!includeElse)
                includeElse = true;
        }
        if (otherProps.Length != 0)
            writer.CloseBlock(decreaseIndent: false);
    }

    private static readonly CodePropertyTypeComparer CodePropertyTypeForwardComparer = new();
    private static readonly CodePropertyTypeComparer CodePropertyTypeBackwardComparer = new(true);
    private void WriteFactoryMethodBodyForUnionModelForUnDiscriminatedTypes(CodeMethod currentElement, CodeClass parentClass, LanguageWriter writer)
    {
        var includeElse = false;
        var otherProps = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)
            .Where(static x => x.Setter != null)
            .Where(static x => x.Type is CodeType xType && (xType.IsCollection || xType.TypeDefinition is null or CodeEnum))
            .Order(CodePropertyTypeForwardComparer)
            .ThenBy(static x => x.Name)
            .ToArray();
        foreach (var property in otherProps)
        {
            var methodName = GetDeserializationMethodName(property.Type, currentElement);
            var serializationMethodName = $"{ParseNodeVarName}->{methodName.Item2}";
            const string finalValueName = "$finalValue";
            writer.StartBlock($"{(includeElse ? "} else " : string.Empty)}if ({serializationMethodName} !== null) {{");
            if (!string.IsNullOrEmpty(methodName.Item1))
            {
                writer.WriteLine($"/** @var array<{methodName.Item1}> {finalValueName} */");
            }
            writer.WriteLine($"{finalValueName} = {serializationMethodName};");
            writer.WriteLine($"{ResultVarName}->{property.Setter!.Name.ToFirstCharacterLowerCase()}({finalValueName});");
            writer.DecreaseIndent();
            if (!includeElse)
                includeElse = true;
        }
        if (otherProps.Length != 0)
            writer.CloseBlock(decreaseIndent: false);
    }

    private void WriteFactoryMethodBodyForInheritedModel(IOrderedEnumerable<KeyValuePair<string, CodeType>> discriminatorMappings, LanguageWriter writer, CodeMethod method, string? varName = default)
    {
        if (string.IsNullOrEmpty(varName))
            varName = DiscriminatorMappingVarName;
        writer.StartBlock($"switch ({varName}) {{");
        foreach (var mappedType in discriminatorMappings)
        {
            writer.WriteLine($"case '{mappedType.Key}': return new {conventions.GetTypeString(mappedType.Value.AllTypes.First(), method, false, writer)}();");
        }
        writer.CloseBlock();
    }
}
