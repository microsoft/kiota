using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
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
        var requestParams = new RequestParams(requestBodyParam, config);

        WriteMethodPhpDocs(codeElement, writer);
        WriteMethodsAndParameters(codeElement, writer, codeElement.IsOfKind(CodeMethodKind.Constructor, CodeMethodKind.ClientConstructor));

        switch (codeElement.Kind)
        {
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

    private const string UrlTemplateTempVarName = "$urlTplParams";
    private const string RawUrlParameterKey = "request-raw-url";
    private static readonly Dictionary<CodeParameterKind, CodePropertyKind> propertiesToAssign = new Dictionary<CodeParameterKind, CodePropertyKind>()
    {
        { CodeParameterKind.QueryParameter, CodePropertyKind.QueryParameters }, // Handles query parameter object as a constructor param in request config classes
    };

    private static void WriteConstructorParentCall(CodeClass parentClass, CodeMethod currentMethod, LanguageWriter writer)
    {
        var requestAdapterParameter = currentMethod.Parameters.OfKind(CodeParameterKind.RequestAdapter);
        var requestOptionParameter = currentMethod.Parameters.OfKind(CodeParameterKind.Options);
        var requestHeadersParameter = currentMethod.Parameters.OfKind(CodeParameterKind.Headers);
        var pathParametersProperty = parentClass.Properties.OfKind(CodePropertyKind.PathParameters);
        var urlTemplateProperty = parentClass.Properties.OfKind(CodePropertyKind.UrlTemplate);

        if (parentClass.IsOfKind(CodeClassKind.RequestBuilder))
        {
            writer.WriteLine($"parent::__construct(${(requestAdapterParameter?.Name ?? "requestAdapter")}, {(pathParametersProperty?.DefaultValue ?? "[]")}, {(urlTemplateProperty?.DefaultValue.ReplaceDoubleQuoteWithSingleQuote() ?? "")});");
        }
        else if (parentClass.IsOfKind(CodeClassKind.RequestConfiguration))
            writer.WriteLine($"parent::__construct(${(requestHeadersParameter?.Name ?? "headers")} ?? [], ${(requestOptionParameter?.Name ?? "options")} ?? []);");
        else
            writer.WriteLine("parent::__construct();");

    }
    private void WriteConstructorBody(CodeClass parentClass, CodeMethod currentMethod, LanguageWriter writer, bool inherits)
    {
        if (inherits)
        {
            WriteConstructorParentCall(parentClass, currentMethod, writer);
        }
        var backingStoreProperty = parentClass.GetPropertyOfKind(CodePropertyKind.BackingStore);
        if (backingStoreProperty != null && !string.IsNullOrEmpty(backingStoreProperty.DefaultValue))
            writer.WriteLine($"$this->{backingStoreProperty.Name.ToFirstCharacterLowerCase()} = {backingStoreProperty.DefaultValue};");
        foreach (var propWithDefault in parentClass.GetPropertiesOfKind(
                CodePropertyKind.RequestBuilder)
            .Where(x => !string.IsNullOrEmpty(x.DefaultValue))
            .OrderByDescending(x => x.Kind)
            .ThenBy(x => x.Name))
        {
            var isPathSegment = propWithDefault.IsOfKind(CodePropertyKind.PathParameters);
            writer.WriteLine($"$this->{propWithDefault.Name.ToFirstCharacterLowerCase()} = {(isPathSegment ? "[]" : propWithDefault.DefaultValue.ReplaceDoubleQuoteWithSingleQuote())};");
        }
        foreach (var propWithDefault in parentClass.GetPropertiesOfKind(CodePropertyKind.AdditionalData, CodePropertyKind.Custom) //additional data and custom properties rely on accessors
            .Where(x => !string.IsNullOrEmpty(x.DefaultValue))
            // do not apply the default value if the type is composed as the default value may not necessarily which type to use
            .Where(static x => x.Type is not CodeType propType || propType.TypeDefinition is not CodeClass propertyClass || propertyClass.OriginalComposedType is null)
            .OrderBy(x => x.Name))
        {
            var setterName = propWithDefault.SetterFromCurrentOrBaseType?.Name.ToFirstCharacterLowerCase() is string sName && !string.IsNullOrEmpty(sName) ? sName : $"set{propWithDefault.SymbolName.ToFirstCharacterUpperCase()}";
            writer.WriteLine($"$this->{setterName}({propWithDefault.DefaultValue.ReplaceDoubleQuoteWithSingleQuote()});");
        }
        foreach (var parameterKind in propertiesToAssign.Keys)
        {
            AssignPropertyFromParameter(parentClass, currentMethod, parameterKind, propertiesToAssign[parameterKind], writer);
        }
        // Handles various query parameter properties in query parameter classes
        // Separate call because CodeParameterKind.QueryParameter key is already used in map initialization
        AssignPropertyFromParameter(parentClass, currentMethod, CodeParameterKind.QueryParameter, CodePropertyKind.QueryParameter, writer);

        if (parentClass.IsOfKind(CodeClassKind.RequestBuilder) &&
            currentMethod.IsOfKind(CodeMethodKind.Constructor, CodeMethodKind.ClientConstructor) &&
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
                var key = String.IsNullOrEmpty(parameter.SerializationName)
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
        if (parameters.Any() && parameters.Count.Equals(properties.Count))
        {
            for (var i = 0; i < parameters.Count; i++)
            {
                writer.WriteLine($"$this->{properties[i].Name.ToFirstCharacterLowerCase()} = ${parameters[i].Name};");
            }
        }
    }

    private void WriteMethodPhpDocs(CodeMethod codeMethod, LanguageWriter writer)
    {
        var methodDescription = codeMethod.Documentation.Description;

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
            var nullableSuffix = (codeMethod.ReturnType.IsNullable ? "|null" : "");
            returnDocString = (codeMethod.Kind == CodeMethodKind.RequestExecutor)
                ? "@return Promise"
                : $"@return {returnDocString}{nullableSuffix}";
        }
        else returnDocString = String.Empty;
        conventions.WriteLongDescription(codeMethod.Documentation,
            writer,
            parametersWithOrWithoutDescription.Union(new[] { returnDocString })
            );

    }

    private string GetDocCommentReturnType(CodeMethod codeMethod)
    {
        return codeMethod.Kind switch
        {
            CodeMethodKind.Deserializer => "array<string, callable>",
            CodeMethodKind.Getter when codeMethod.AccessedProperty?.IsOfKind(CodePropertyKind.AdditionalData) ?? false => "array<string, mixed>",
            CodeMethodKind.Getter when codeMethod.AccessedProperty?.Type.IsCollection ?? false => $"array<{conventions.TranslateType(codeMethod.AccessedProperty.Type)}>",
            _ => conventions.GetTypeString(codeMethod.ReturnType, codeMethod)
        };
    }

    private string GetParameterDocString(CodeMethod codeMethod, CodeParameter x)
    {
        if (codeMethod.IsOfKind(CodeMethodKind.Setter)
            && (codeMethod.AccessedProperty?.IsOfKind(CodePropertyKind.AdditionalData) ?? false))
        {
            return $"@param array<string,mixed> $value {x?.Documentation.Description}";
        }
        return $"@param {conventions.GetParameterDocNullable(x, x)} {x?.Documentation.Description}";
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
        if (complexProperties.Any())
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
        else if (otherProps.Any())
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
        if (otherProps.Any())
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
    private string GetDeserializationMethodName(CodeTypeBase propType, CodeMethod method)
    {
        var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
        var propertyType = conventions.GetTypeString(propType, method, false);
        var parseNodeMethod = string.Empty;
        if (propType is CodeType currentType)
        {
            if (isCollection)
                parseNodeMethod = currentType.TypeDefinition switch
                {
                    CodeEnum enumType => $"getCollectionOfEnumValues({enumType.Name.ToFirstCharacterUpperCase()}::class)",
                    _ => $"getCollectionOfObjectValues([{conventions.TranslateType(propType)}::class, '{CreateDiscriminatorMethodName}'])"
                };
            else if (currentType.TypeDefinition is CodeEnum)
                parseNodeMethod = $"getEnumValue({propertyType.ToFirstCharacterUpperCase()}::class)";
        }

        var lowerCaseType = propertyType.ToLowerInvariant();
        return string.IsNullOrEmpty(parseNodeMethod) ? lowerCaseType switch
        {
            "int" => "getIntegerValue()",
            "bool" => "getBooleanValue()",
            "number" => "getIntegerValue()",
            "decimal" or "double" => "getFloatValue()",
            "streaminterface" => "getBinaryContent()",
            "byte" => "getByteValue()",
            _ when conventions.PrimitiveTypes.Contains(lowerCaseType) => $"get{propertyType.ToFirstCharacterUpperCase()}Value()",
            _ => $"getObjectValue([{propertyType.ToFirstCharacterUpperCase()}::class, '{CreateDiscriminatorMethodName}'])",
        } : parseNodeMethod;
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
        if (UseBackingStore && !isBackingStoreGetter && parentClass.GetBackingStoreProperty() is CodeProperty backingStoreProperty && backingStoreProperty.Getter != null)
            writer.WriteLine($"return $this->{backingStoreProperty.Getter!.Name}()->get('{propertyName}');");
        else
            writer.WriteLine($"return $this->{propertyName};");
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
            writer.WriteLines($"{RequestInfoVarName}->urlTemplate = {GetPropertyCall(urlTemplateProperty, "''")};",
                            $"{RequestInfoVarName}->pathParameters = {GetPropertyCall(pathParametersProperty, "''")};");
        writer.WriteLine($"{RequestInfoVarName}->httpMethod = HttpMethod::{codeElement.HttpMethod.Value.ToString().ToUpperInvariant()};");
        WriteAcceptHeaderDef(codeElement, writer);
        WriteRequestConfiguration(requestParams, writer);
        if (requestParams.requestBody != null)
        {
            var suffix = requestParams.requestBody.Type.IsCollection ? "Collection" : string.Empty;
            if (requestParams.requestBody.Type.Name.Equals(conventions.StreamTypeName, StringComparison.OrdinalIgnoreCase))
                writer.WriteLine($"{RequestInfoVarName}->setStreamContent({conventions.GetParameterName(requestParams.requestBody)});");
            else if (currentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter) is CodeProperty requestAdapterProperty)
                if (requestParams.requestBody.Type is CodeType bodyType && bodyType.TypeDefinition is CodeClass)
                    writer.WriteLine($"{RequestInfoVarName}->setContentFromParsable{suffix}($this->{requestAdapterProperty.Name.ToFirstCharacterLowerCase()}, \"{codeElement.RequestBodyContentType}\", {conventions.GetParameterName(requestParams.requestBody)});");
                else
                    writer.WriteLine($"{RequestInfoVarName}->setContentFromScalar{suffix}($this->{requestAdapterProperty.Name.ToFirstCharacterLowerCase()}, \"{codeElement.RequestBodyContentType}\", {conventions.GetParameterName(requestParams.requestBody)});");
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
        if (codeMethod.AcceptedResponseTypes.Any())
            writer.WriteLine($"{RequestInfoVarName}->addHeader('Accept', \"{string.Join(", ", codeMethod.AcceptedResponseTypes)}\");");
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
        if (codeProperties.Any())
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
            writer.WriteLine($"'{property.WireName}' => function (ParseNode $n) {{");
            writer.IncreaseIndent();
            writer.WriteLine("$val = $n->getCollectionOfPrimitiveValues();");
            writer.WriteLine($"if (is_array($val)) {{");
            writer.IncreaseIndent();
            var type = conventions.TranslateType(property.Type);
            writer.WriteLine($"TypeUtils::validateCollectionValues($val, '{type}');");
            writer.DecreaseIndent();
            writer.WriteLine("}");
            writer.WriteLine($"/** @var array<{type}>|null $val */");
            writer.WriteLine($"$this->{property.Setter!.Name.ToFirstCharacterLowerCase()}($val);");
            writer.DecreaseIndent();
            writer.WriteLine("},");
            return;
        }
        writer.WriteLine($"'{property.WireName}' => fn(ParseNode $n) => $o->{property.Setter!.Name.ToFirstCharacterLowerCase()}($n->{GetDeserializationMethodName(property.Type, method)}),");
    }

    private static void WriteDeserializerBodyForIntersectionModel(CodeClass parentClass, LanguageWriter writer)
    {
        var complexProperties = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)
            .Where(static x => x.Type is CodeType propType && propType.TypeDefinition is CodeClass && !x.Type.IsCollection)
            .ToArray();
        if (complexProperties.Any())
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
        if (otherPropGetters.Any())
            writer.CloseBlock(decreaseIndent: false);
        writer.WriteLine($"return [];");
    }

    private void WriteIndexerBody(CodeMethod codeElement, CodeClass parentClass, string returnType, LanguageWriter writer)
    {
        var pathParameters = codeElement.Parameters.Where(static x => x.IsOfKind(CodeParameterKind.Path));
        if (parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters) is CodeProperty pathParametersProperty &&
            codeElement.OriginalIndexer != null)
            conventions.AddParametersAssignment(writer, pathParametersProperty.Type, $"$this->{pathParametersProperty.Name}",
                (codeElement.OriginalIndexer.IndexType, codeElement.OriginalIndexer.SerializationName, $"${codeElement.OriginalIndexer.IndexParameterName.ToFirstCharacterLowerCase()}"));
        conventions.AddRequestBuilderBody(parentClass, returnType, writer, conventions.TempDictionaryVarName, pathParameters);
    }

    private void WriteRequestExecutorBody(CodeMethod codeElement, CodeClass parentClass, RequestParams requestParams, LanguageWriter writer)
    {
        var generatorMethod = parentClass
            .Methods
            .FirstOrDefault(x =>
                x.IsOfKind(CodeMethodKind.RequestGenerator) && x.HttpMethod == codeElement.HttpMethod);
        var generatorMethodName = generatorMethod?.Name.ToFirstCharacterLowerCase();
        var requestInfoParameters = new[] { requestParams.requestBody, requestParams.requestConfiguration }
            .Where(static x => x?.Name != null)
            .Select(static x => x!);
        var callParams = requestInfoParameters.Select(conventions.GetParameterName);
        var joinedParams = string.Empty;
        if (requestInfoParameters.Any())
        {
            joinedParams = string.Join(", ", callParams);
        }

        var returnType = conventions.TranslateType(codeElement.ReturnType);
        writer.WriteLine($"$requestInfo = $this->{generatorMethodName}({joinedParams});");
        writer.WriteLine("try {");
        writer.IncreaseIndent();
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
                writer.WriteLine($"'{errorMapping.Key}' => [{errorMapping.Value.Name}::class, '{CreateDiscriminatorMethodName}'],");
            });
            writer.DecreaseIndent();
            writer.WriteLine("];");
        }

        var returnsVoid = returnType.Equals("void", StringComparison.OrdinalIgnoreCase);
        var isStream = returnType.Equals(conventions.StreamTypeName, StringComparison.OrdinalIgnoreCase);
        var isCollection = codeElement.ReturnType.IsCollection;
        var methodName = GetSendRequestMethodName(returnsVoid, isStream, isCollection, returnType);
        var returnTypeFactory = codeElement.ReturnType is CodeType rt && rt.TypeDefinition is CodeClass
            ? $", [{returnType}::class, '{CreateDiscriminatorMethodName}']"
            : string.Empty;
        var returnWithCustomType =
            !returnsVoid && string.IsNullOrEmpty(returnTypeFactory) && conventions.CustomTypes.Contains(returnType)
                ? $", {returnType}::class"
                : returnTypeFactory;
        var finalReturn = string.IsNullOrEmpty(returnWithCustomType) && !returnsVoid
            ? $", '{returnType}'"
            : returnWithCustomType;
        var requestAdapterProperty = parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter) ?? throw new InvalidOperationException("Request adapter property not found");
        writer.WriteLine(
            $"return {GetPropertyCall(requestAdapterProperty, string.Empty)}->{methodName}({RequestInfoVarName}{finalReturn}, {(hasErrorMappings ? $"{errorMappingsVarName}" : "null")});");

        writer.DecreaseIndent();
        writer.WriteLine("} catch(Exception $ex) {");
        writer.IncreaseIndent();
        writer.WriteLine("return new RejectedPromise($ex);");
        writer.DecreaseIndent();
        writer.WriteLine("}");
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

    protected string GetSendRequestMethodName(bool isVoid, bool isStream, bool isCollection, string returnType)
    {
        if (isVoid) return "sendNoContentAsync";
        if (isStream || conventions.PrimitiveTypes.Contains(returnType))
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
        var writeDiscriminatorValueRead = parentClass.DiscriminatorInformation.ShouldWriteParseNodeCheck && !parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForIntersectionType;

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
        else if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForUnionType && parentClass.DiscriminatorInformation.HasBasicDiscriminatorInformation)
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
                var deserializationMethodName = $"{ParseNodeVarName}->{GetDeserializationMethodName(propertyType, codeElement)}";
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
        if (complexProperties.Any())
        {
            if (includeElse)
                writer.StartBlock("} else {");
            foreach (var property in complexProperties)
                writer.WriteLine($"{ResultVarName}->{property.Item1.Setter!.Name.ToFirstCharacterLowerCase()}(new {conventions.GetTypeString(property.Item2, codeElement, false)}());");
            if (includeElse)
                writer.CloseBlock();
        }
        else if (otherProps.Any())
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
        if (otherProps.Any())
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
            var serializationMethodName = $"{ParseNodeVarName}->{GetDeserializationMethodName(property.Type, currentElement)}";
            writer.StartBlock($"{(includeElse ? "} else " : string.Empty)}if ({serializationMethodName} !== null) {{");
            writer.WriteLine($"{ResultVarName}->{property.Setter!.Name.ToFirstCharacterLowerCase()}({serializationMethodName});");
            writer.DecreaseIndent();
            if (!includeElse)
                includeElse = true;
        }
        if (otherProps.Any())
            writer.CloseBlock(decreaseIndent: false);
    }

    private void WriteFactoryMethodBodyForInheritedModel(IOrderedEnumerable<KeyValuePair<string, CodeTypeBase>> discriminatorMappings, LanguageWriter writer, CodeMethod method, string? varName = default)
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
