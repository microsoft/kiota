using System;
using System.Collections.Generic;
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
        var propertiesOfKind = parentClass.GetPropertiesOfKind(CodePropertyKind.AdditionalData, CodePropertyKind.Custom);
        var filteredProperties = new List<CodeProperty>();

        foreach (var prop in propertiesOfKind)
        {
            if (!string.IsNullOrEmpty(prop.DefaultValue) && 
                (!(prop.Type is CodeType propType) || 
                !(propType.TypeDefinition is CodeClass propertyClass) || 
                propertyClass.OriginalComposedType == null))
            {
                filteredProperties.Add(prop);
            }
        }

        filteredProperties.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));

        foreach (var propWithDefault in filteredProperties)
        {
            var setterName = propWithDefault.SetterFromCurrentOrBaseType?.Name.ToFirstCharacterLowerCase() is string sName && !string.IsNullOrEmpty(sName) ? sName : $"set{propWithDefault.Name.ToFirstCharacterUpperCase()}";
            var defaultValue = propWithDefault.DefaultValue.ReplaceDoubleQuoteWithSingleQuote();
            if (propWithDefault.Type is CodeType codeType && codeType.TypeDefinition is CodeEnum enumDefinition)
            {
                defaultValue = $"new {enumDefinition.Name.ToFirstCharacterUpperCase()}({defaultValue})";
            }
            // avoid setting null as a string.
            if (propWithDefault.Type.IsNullable &&
                defaultValue.TrimQuotes().Equals(NullValueString, StringComparison.OrdinalIgnoreCase))
            {
                defaultValue = NullValueString;
            }
            writer.WriteLine($"$this->{setterName}({defaultValue});");
        }
    }

    private const string NullValueString = "null";
    private void WriteRequestBuilderConstructorBody(CodeClass parentClass, CodeMethod currentMethod, LanguageWriter writer)
    {
        var propertiesOfKind = parentClass.GetPropertiesOfKind(CodePropertyKind.RequestBuilder);
        var filteredProperties = new List<CodeProperty>();

        foreach (var prop in propertiesOfKind)
        {
            if (!string.IsNullOrEmpty(prop.DefaultValue))
            {
                filteredProperties.Add(prop);
            }
        }

        filteredProperties.Sort((x, y) =>
        {
            int kindComparison = y.Kind.CompareTo(x.Kind); // Descending order for Kind
            if (kindComparison != 0) return kindComparison;
            return string.Compare(x.Name, y.Name, StringComparison.Ordinal); // Ascending order for Name
        });

        foreach (var propWithDefault in filteredProperties)
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

        bool hasPathParameter = false;
        foreach (var param in currentMethod.Parameters)
        {
            if (param.IsOfKind(CodeParameterKind.Path))
            {
                hasPathParameter = true;
                break;
            }
        }

        if (pathParametersProperty != null && !hasPathParameter)
        {
            writer.WriteLine($"{GetPropertyCall(pathParametersProperty, "[]")} = {conventions.GetParameterName(pathParameter)};");
            return;
        }

        writer.WriteLine($"{UrlTemplateTempVarName} = {conventions.GetParameterName(pathParameter)};");

        var pathParameters = new List<CodeParameter>();
        foreach (var parameter in currentMethod.Parameters)
        {
            if (parameter.IsOfKind(CodeParameterKind.Path))
            {
                pathParameters.Add(parameter);
            }
        }

        foreach (var parameter in pathParameters)
        {
            var key = string.IsNullOrEmpty(parameter.SerializationName) ? parameter.Name : parameter.SerializationName;
            writer.WriteLine($"{UrlTemplateTempVarName}['{key}'] = ${parameter.Name.ToFirstCharacterLowerCase()};");
        }
        if (pathParametersProperty != null)
            writer.WriteLine(
                $"{GetPropertyCall(pathParametersProperty, "[]")} = {UrlTemplateTempVarName};");
    }
    private static void AssignPropertyFromParameter(CodeClass parentClass, CodeMethod currentMethod, CodeParameterKind parameterKind, CodePropertyKind propertyKind, LanguageWriter writer)
    {
        var parameters = new List<CodeParameter>();
        foreach (var param in currentMethod.Parameters)
        {
            if (param.IsOfKind(parameterKind))
            {
                parameters.Add(param);
            }
        }

        var properties = new List<CodeProperty>();
        foreach (var prop in parentClass.GetPropertiesOfKind(propertyKind))
        {
            properties.Add(prop);
        }

        if (parameters.Count != 0 && parameters.Count == properties.Count)
        {
            for (var i = 0; i < parameters.Count; i++)
            {
                var isNonNullableCollection = !parameters[i].Type.IsNullable && parameters[i].Type.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
                writer.WriteLine($"$this->{properties[i].Name.ToFirstCharacterLowerCase()} = ${parameters[i].Name.ToFirstCharacterLowerCase()}{(isNonNullableCollection ? " ?? []" : string.Empty)};");
            }
        }
    }

    private void WriteMethodPhpDocs(CodeMethod codeMethod, LanguageWriter writer)
    {
        var methodDescription = codeMethod.Documentation.GetDescription(x => conventions.GetTypeString(x, codeMethod), normalizationFunc: PhpConventionService.RemoveInvalidDescriptionCharacters);
        var methodThrows = codeMethod.IsOfKind(CodeMethodKind.RequestExecutor);
        var hasMethodDescription = !string.IsNullOrEmpty(methodDescription.Trim());
        using var enumerator = codeMethod.Parameters.GetEnumerator();
        if (!hasMethodDescription && !enumerator.MoveNext())
        {
            return;
        }

        var isVoidable = "void".Equals(conventions.GetTypeString(codeMethod.ReturnType, codeMethod),
            StringComparison.OrdinalIgnoreCase) && !codeMethod.IsOfKind(CodeMethodKind.RequestExecutor);

        var parametersWithOrWithoutDescription = new List<string>();

        using (var paramEnumerator = codeMethod.Parameters.GetEnumerator())
        {
            while (paramEnumerator.MoveNext())
            {
                var parameter = paramEnumerator.Current;
                parametersWithOrWithoutDescription.Add(GetParameterDocString(codeMethod, parameter));
            }
        }
        var returnDocString = GetDocCommentReturnType(codeMethod);
        if (!isVoidable)
        {
            var nullableSuffix = codeMethod.ReturnType.IsNullable ? "|null" : "";
            returnDocString = (codeMethod.Kind == CodeMethodKind.RequestExecutor)
                ? $"@return Promise<{returnDocString}|null>"
                : $"@return {returnDocString}{nullableSuffix}";
        }
        else returnDocString = string.Empty;

        string[] throwsArray = methodThrows ? ["@throws Exception"] : [];

        var allDescriptions = new List<string>(parametersWithOrWithoutDescription.Count + 1 + throwsArray.Length);
        allDescriptions.AddRange(parametersWithOrWithoutDescription);
        allDescriptions.Add(returnDocString);
        allDescriptions.AddRange(throwsArray);

        conventions.WriteLongDescription(codeMethod, writer, allDescriptions);
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
        var orderedParameters = new List<CodeParameter>(codeMethod.Parameters);
        orderedParameters.Sort(parameterOrderComparer);

        var methodParametersList = new List<string>();
        foreach (var parameter in orderedParameters)
        {
            methodParametersList.Add(conventions.GetParameterSignature(parameter, codeMethod));
        }

        var methodParameters = string.Join(", ", methodParametersList);

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

        CodeProperty? additionalDataProperty = null;
        foreach (var property in parentClass.GetPropertiesOfKind(CodePropertyKind.AdditionalData))
        {
            additionalDataProperty = property;
            break;
        }

        if (additionalDataProperty != null && additionalDataProperty.Getter != null)
        {
            writer.WriteLine($"$writer->writeAdditionalData($this->{additionalDataProperty.Getter.Name}());");
        }
    }

    private void WriteSerializerBodyForIntersectionModel(CodeClass parentClass, LanguageWriter writer)
    {
        var includeElse = false;
        var otherProps = new List<CodeProperty>();
        foreach (var property in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom))
        {
            if (!property.ExistsInBaseType && property.Getter != null &&
                (!(property.Type is CodeType propertyType) || propertyType.IsCollection || !(propertyType.TypeDefinition is CodeClass)))
            {
                otherProps.Add(property);
            }
        }

        otherProps.Sort(CodePropertyTypeBackwardComparer);
        otherProps.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));

        foreach (var otherProp in otherProps)
        {
            writer.StartBlock($"{(includeElse ? "} else " : string.Empty)}if ($this->{otherProp.Getter!.Name.ToFirstCharacterLowerCase()}() !== null) {{");
            WriteSerializationMethodCall(otherProp, writer, "null");
            writer.DecreaseIndent();
            if (!includeElse)
                includeElse = true;
        }

        var complexProperties = new List<CodeProperty>();
        foreach (var property in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom))
        {
            if (property.Getter != null && property.Type is CodeType { TypeDefinition: CodeClass } propertyType && !property.Type.IsCollection)
            {
                complexProperties.Add(property);
            }
        }

        if (complexProperties.Count != 0)
        {
            if (includeElse)
            {
                writer.StartBlock("} else {");
            }

            // Sort the complexProperties list by name
            complexProperties.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));

            // Find the first property
            CodeProperty firstProperty = complexProperties[0];


            // Construct propertiesNames
            string? propertiesNames = null;
            foreach (var property in complexProperties)
            {
                if (propertiesNames is not null) propertiesNames += ", ";
                propertiesNames += $"$this->{property.Getter!.Name.ToFirstCharacterLowerCase()}()";
            }

            // Call WriteSerializationMethodCall with the first property and propertiesNames
            WriteSerializationMethodCall(firstProperty, writer, "null", propertiesNames);


            // Call WriteSerializationMethodCall with the first property and propertiesNames
            WriteSerializationMethodCall(firstProperty, writer, "null", propertiesNames);

            if (includeElse)
            {
                writer.CloseBlock();
            }
        }
        else if (otherProps.Count != 0)
        {
            writer.CloseBlock(decreaseIndent: false);
        }
    }

    private void WriteSerializerBodyForUnionModel(CodeClass parentClass, LanguageWriter writer)
    {
        var includeElse = false;
        var otherProps = new List<CodeProperty>();

        // Iterate over all properties of kind CodePropertyKind.Custom
        foreach (var property in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom))
        {
            // Filter properties that meet the conditions
            if (!property.ExistsInBaseType && property.Getter != null)
            {
                otherProps.Add(property);
            }
        }

        // Sort the filtered properties
        otherProps.Sort((x, y) =>
        {
            int compareType = CodePropertyTypeForwardComparer.Compare(x, y);
            if (compareType != 0)
                return compareType;
            return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
        });

        // Iterate over the sorted properties and write serialization method calls
        foreach (var otherProp in otherProps)
        {
            writer.StartBlock($"{(includeElse ? "} else " : string.Empty)}if ($this->{otherProp.Getter!.Name.ToFirstCharacterLowerCase()}() !== null) {{");
            WriteSerializationMethodCall(otherProp, writer, "null");
            writer.DecreaseIndent();
            if (!includeElse)
                includeElse = true;
        }

        // Close the block if at least one property was processed
        if (includeElse)
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
        foreach (var otherProp in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom))
        {
            if (!otherProp.ExistsInBaseType && !otherProp.ReadOnly)
                WriteSerializationMethodCall(otherProp, writer, $"'{otherProp.WireName}'");
        }
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
        var pathParameters = new List<CodeParameter>();

        // Iterate over codeMethod.Parameters and filter parameters of kind CodeParameterKind.Path
        foreach (var parameter in codeMethod.Parameters)
        {
            if (parameter.IsOfKind(CodeParameterKind.Path))
            {
                pathParameters.Add(parameter);
            }
        }

        // Call AddRequestBuilderBody with the filtered path parameters
        conventions.AddRequestBuilderBody(returnType, writer, pathParameters: pathParameters);
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
        var codeProperties = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom);
        writer.WriteLine("$o = $this;");
        writer.WriteLines(
            $"return {((extendsModelClass) ? $"array_merge(parent::{method.Name.ToFirstCharacterLowerCase()}(), [" : " [")}");
        writer.IncreaseIndent();
        var propertiesToWrite = new List<CodeProperty>();

        bool hasCustomProperties = false;
        foreach (var property in codeProperties)
        {
            if (!property.ExistsInBaseType && property.Setter != null)
            {
                propertiesToWrite.Add(property);
                hasCustomProperties = true;
            }
        }

        if (hasCustomProperties)
        {
            propertiesToWrite.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));

            foreach (var property in propertiesToWrite)
            {
                WriteDeserializerPropertyCallback(property, method, writer);
            }
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
        writer.WriteLine($"'{property.WireName}' => fn(ParseNode $n) => $o->{property.Setter!.Name.ToFirstCharacterLowerCase()}($n->{GetDeserializationMethodName(property.Type, method)}),");
    }

    private static void WriteDeserializerBodyForIntersectionModel(CodeClass parentClass, LanguageWriter writer)
    {
        var complexProperties = new List<CodeProperty>();

        // Iterate over properties of kind CodePropertyKind.Custom and filter based on the condition
        foreach (var property in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom))
        {
            if (property.Type is CodeType propType && propType.TypeDefinition is CodeClass && !property.Type.IsCollection && property.Getter != null)
            {
                complexProperties.Add(property);
            }
        }

        if (complexProperties.Count != 0)
        {
            var propertiesNames = new List<string>();

            // Filter complex properties with getters and store their names
            foreach (var property in complexProperties)
            {
                propertiesNames.Add(property.Getter!.Name.ToFirstCharacterLowerCase());
            }

            // Sort property names
            propertiesNames.Sort(StringComparer.OrdinalIgnoreCase);

            var propertiesNamesAsConditions = "";
            for (int i = 0; i < propertiesNames.Count; i++)
            {
                if (i > 0)
                {
                    propertiesNamesAsConditions += " || ";
                }
                propertiesNamesAsConditions += $"$this->{propertiesNames[i]}() !== null";
            }

            writer.StartBlock($"if ({propertiesNamesAsConditions}) {{");

            var propertiesNamesAsArgument = "";
            for (int i = 0; i < propertiesNames.Count; i++)
            {
                if (i > 0)
                {
                    propertiesNamesAsArgument += ", ";
                }
                propertiesNamesAsArgument += $"$this->{propertiesNames[i]}()";
            }
            writer.WriteLine($"return ParseNodeHelper::mergeDeserializersForIntersectionWrapper({propertiesNamesAsArgument});");

            writer.CloseBlock();
        }

        writer.WriteLine($"return [];");
    }

    private static void WriteDeserializerBodyForUnionModel(CodeMethod method, CodeClass parentClass, LanguageWriter writer)
    {
        var includeElse = false;
        var otherPropGetters = new List<string>();

        // Iterate over properties of kind CodePropertyKind.Custom and filter based on the condition
        foreach (var property in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom))
        {
            if (!property.ExistsInBaseType && property.Getter != null &&
                property.Type is CodeType propertyType && !propertyType.IsCollection && propertyType.TypeDefinition is CodeClass)
            {
                otherPropGetters.Add(property.Getter!.Name.ToFirstCharacterLowerCase());
            }
        }

        // Sort property getters
        otherPropGetters.Sort((x, y) => string.Compare(x, y, StringComparison.Ordinal));

        foreach (var otherPropGetter in otherPropGetters)
        {
            writer.StartBlock($"{(includeElse ? "} else " : string.Empty)}if ($this->{otherPropGetter}() !== null) {{");
            writer.WriteLine($"return $this->{otherPropGetter}()->{method.Name.ToFirstCharacterLowerCase()}();");
            writer.DecreaseIndent();
            if (!includeElse)
                includeElse = true;
        }

        if (otherPropGetters.Count != 0)
            writer.CloseBlock(decreaseIndent: false);

        writer.WriteLine($"return [];");
    }

    private void WriteIndexerBody(CodeMethod codeElement, CodeClass parentClass, string returnType, LanguageWriter writer)
    {
        // Find path parameters
        var pathParameters = new List<CodeParameter>();
        foreach (var parameter in codeElement.Parameters)
        {
            if (parameter.IsOfKind(CodeParameterKind.Path))
            {
                pathParameters.Add(parameter);
            }
        }

        // Find path parameters property
        CodeProperty? pathParametersProperty = null;
        foreach (var property in parentClass.Properties)
        {
            if (property.Kind == CodePropertyKind.PathParameters)
            {
                pathParametersProperty = property;
                break;
            }
        }

        // Add parameters assignment if path parameters property exists and original indexer is not null
        if (pathParametersProperty != null && codeElement.OriginalIndexer != null)
        {
            conventions.AddParametersAssignment(writer, pathParametersProperty.Type, $"$this->{pathParametersProperty.Name}",
                (codeElement.OriginalIndexer.IndexParameter.Type, codeElement.OriginalIndexer.IndexParameter.SerializationName, $"${codeElement.OriginalIndexer.IndexParameter.Name.ToFirstCharacterLowerCase()}"));
        }

        // Add request builder body
        conventions.AddRequestBuilderBody(parentClass, returnType, writer, conventions.TempDictionaryVarName, pathParameters);
    }

    private void WriteRequestExecutorBody(CodeMethod codeElement, CodeClass parentClass, RequestParams requestParams, LanguageWriter writer)
    {
        // Find the generator method
        CodeMethod? generatorMethod = null;
        foreach (var method in parentClass.Methods)
        {
            if (method.IsOfKind(CodeMethodKind.RequestGenerator) && method.HttpMethod == codeElement.HttpMethod)
            {
                generatorMethod = method;
                break;
            }
        }

        // Get the generator method name
        var generatorMethodName = generatorMethod?.Name.ToFirstCharacterLowerCase();

        // Prepare request info parameters
        var requestInfoParameters = new List<string>();
        foreach (var parameter in new CodeParameter?[] { requestParams.requestBody, requestParams.requestContentType, requestParams.requestConfiguration })
        {
            if (parameter is CodeParameter codeParameter)
            {
                requestInfoParameters.Add(conventions.GetParameterName(codeParameter));
            }
        }
        var joinedParams = string.Empty;
        if (requestInfoParameters.Count != 0)
        {
            joinedParams = string.Join(", ", requestInfoParameters);
        }

        // Get return type name
        var returnTypeName = conventions.GetTypeString(codeElement.ReturnType, codeElement, false);

        // Write request info assignment
        writer.WriteLine($"$requestInfo = $this->{generatorMethodName}({joinedParams});");

        // Write error mappings if available
        IEnumerable<KeyValuePair<string, CodeTypeBase>>? errorMappings = codeElement.ErrorMappings;
        var hasErrorMappings = false;
        var errorMappingsVarName = "$errorMappings";
        if (errorMappings != null)
        {
            // Check if the collection contains any elements
            var enumerator = errorMappings.GetEnumerator();
            if (enumerator.MoveNext())
            {
                hasErrorMappings = true;
                writer.WriteLine($"{errorMappingsVarName} = [");
                writer.IncreaseIndent(2);

                do
                {
                    var errorMapping = enumerator.Current;
                    writer.WriteLine($"'{errorMapping.Key}' => [{errorMapping.Value.Name.ToFirstCharacterUpperCase()}::class, '{CreateDiscriminatorMethodName}'],");
                }
                while (enumerator.MoveNext());

                writer.DecreaseIndent();
                writer.WriteLine("];");
            }
        }

        // Determine method details
        var returnsVoid = returnTypeName.Equals("void", StringComparison.OrdinalIgnoreCase);
        var isStream = returnTypeName.Equals(conventions.StreamTypeName, StringComparison.OrdinalIgnoreCase);
        var isCollection = codeElement.ReturnType.IsCollection;
        var isEnum = codeElement.ReturnType is CodeType returnType && returnType.TypeDefinition is CodeEnum;
        var methodName = GetSendRequestMethodName(returnsVoid, isStream, isCollection, isEnum, returnTypeName);

        // Determine return type factory and custom type handling
        var returnTypeFactory = string.Empty;
        if (codeElement.ReturnType is CodeType rt && rt.TypeDefinition is CodeClass)
        {
            returnTypeFactory = $", [{returnTypeName}::class, '{CreateDiscriminatorMethodName}']";
        }
        var returnWithCustomType = !returnsVoid && string.IsNullOrEmpty(returnTypeFactory) && conventions.CustomTypes.Contains(returnTypeName)
            ? $", {returnTypeName}::class"
            : returnTypeFactory;
        var returnEnumType = string.Empty;
        if (codeElement.ReturnType is CodeType codeType && codeType.TypeDefinition is CodeEnum)
        {
            returnEnumType = $", {returnTypeName}::class";
        }
        else
        {
            returnEnumType = returnWithCustomType;
        }
        var finalReturn = string.IsNullOrEmpty(returnEnumType) && !returnsVoid
            ? $", '{returnTypeName}'"
            : returnEnumType;

        // Get request adapter property
        var requestAdapterProperty = parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter) ?? throw new InvalidOperationException("Request adapter property not found");

        // Construct method call
        var methodCall = $"{GetPropertyCall(requestAdapterProperty, string.Empty)}->{methodName}({RequestInfoVarName}{finalReturn}, {(hasErrorMappings ? $"{errorMappingsVarName}" : "null")});";

        // Write method call
        if (methodName.Contains("sendPrimitive", StringComparison.OrdinalIgnoreCase))
        {
            writer.WriteLines($"/** @var Promise<{GetDocCommentReturnType(codeElement)}|null> $result */",
                $"$result = {methodCall}",
                "return $result;");
        }
        else
        {
            writer.WriteLines($"return {methodCall}");
        }
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
                // Collect parameter names respecting parameterOrderComparer
                var parameterNames = new List<string>();

                // Sort parameters using parameterOrderComparer
                var sortedParameters = new List<CodeParameter>(codeElement.Parameters);
                sortedParameters.Sort(parameterOrderComparer);

                // Collect sorted parameter names
                foreach (var parameter in sortedParameters)
                {
                    parameterNames.Add($"${parameter.Name.ToFirstCharacterLowerCase()}");
                }

                // Join parameter names
                var joinedParameterNames = string.Join(", ", parameterNames);

                // Write factory method body
                writer.WriteLine($"return new {conventions.GetTypeString(codeElement.ReturnType, codeElement)}({joinedParameterNames});");
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
        var otherProps = new List<CodeProperty>();
        foreach (var property in parentClass.Properties)
        {
            if (property.Kind == CodePropertyKind.Custom && property.Setter != null)
            {
                if (!(property.Type is CodeType propertyType) || propertyType.IsCollection || !(propertyType.TypeDefinition is CodeClass))
                {
                    otherProps.Add(property);
                }
            }
        }

        // Sort otherProps by backward comparer
        otherProps.Sort(CodePropertyTypeBackwardComparer);

        // Iterate through otherProps
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

        // Find complex properties
        var complexProperties = new List<Tuple<CodeProperty, CodeType>>();
        foreach (var property in parentClass.Properties)
        {
            if (property.Kind == CodePropertyKind.Custom && property.Setter != null && property.Type is CodeType propertyType)
            {
                if (propertyType.TypeDefinition is CodeClass && !propertyType.IsCollection)
                {
                    complexProperties.Add(new Tuple<CodeProperty, CodeType>(property, propertyType));
                }
            }
        }

        // Iterate through complexProperties
        if (complexProperties.Count != 0)
        {
            if (includeElse)
                writer.StartBlock("} else {");
            foreach (var property in complexProperties)
            {
                writer.WriteLine($"{ResultVarName}->{property.Item1.Setter!.Name.ToFirstCharacterLowerCase()}(new {conventions.GetTypeString(property.Item2, codeElement, false)}());");
            }
            if (includeElse)
                writer.CloseBlock();
        }
        else if (otherProps.Count != 0)
            writer.CloseBlock(decreaseIndent: false);
    }

private void WriteFactoryMethodBodyForUnionModelForDiscriminatedTypes(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer)
{
    var includeElse = false;
    var otherProps = new List<CodeProperty>();

    foreach (var prop in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom))
    {
        if (prop.Setter != null && prop.Type is CodeType codeType && !codeType.IsCollection && (codeType.TypeDefinition is CodeClass || codeType.TypeDefinition is CodeInterface))
        {
            otherProps.Add(prop);
        }
    }

    otherProps.Sort(static (x, y) => {
        int compare = CodePropertyTypeForwardComparer.Compare(x, y);
        return compare == 0 && x.Name == y.Name ? 0 : compare;
    });

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

        KeyValuePair<string, CodeType>? mappedType = null;
        foreach (var mapping in parentClass.DiscriminatorInformation.DiscriminatorMappings)
        {
            if (mapping.Value.Name.Equals(propertyType.Name, StringComparison.OrdinalIgnoreCase))
            {
                mappedType = mapping;
                break;
            }
        }

        if (mappedType != null)
        {
            writer.StartBlock($"{(includeElse ? "} else " : string.Empty)}if ('{mappedType.Value.Key}' === {DiscriminatorMappingVarName}) {{");
            writer.WriteLine($"{ResultVarName}->{property.Setter!.Name.ToFirstCharacterLowerCase()}(new {conventions.GetTypeString(propertyType, codeElement, false)}());");
            writer.DecreaseIndent();
            if (!includeElse)
                includeElse = true;
        }
    }

    if (otherProps.Count != 0)
        writer.CloseBlock(decreaseIndent: false);
}
    private static readonly CodePropertyTypeComparer CodePropertyTypeForwardComparer = new();
    private static readonly CodePropertyTypeComparer CodePropertyTypeBackwardComparer = new(true);
    private void WriteFactoryMethodBodyForUnionModelForUnDiscriminatedTypes(CodeMethod currentElement, CodeClass parentClass, LanguageWriter writer)
    {
        var includeElse = false;
        var otherProps = new List<CodeProperty>();
        foreach (var property in parentClass.Properties)
        {
            if (property.Setter != null && property.Type is CodeType xType)
            {
                if (xType.IsCollection || xType.TypeDefinition == null || xType.TypeDefinition is CodeEnum)
                {
                    otherProps.Add(property);
                }
            }
        }

        // Sort otherProps by forward comparer
        otherProps.Sort(CodePropertyTypeForwardComparer);

        // Iterate through otherProps
        foreach (var property in otherProps)
        {
            var serializationMethodName = $"{ParseNodeVarName}->{GetDeserializationMethodName(property.Type, currentElement)}";
            writer.StartBlock($"{(includeElse ? "} else " : string.Empty)}if ({serializationMethodName} !== null) {{");
            writer.WriteLine($"{ResultVarName}->{property.Setter!.Name.ToFirstCharacterLowerCase()}({serializationMethodName});");
            writer.DecreaseIndent();
            if (!includeElse)
                includeElse = true;
        }

        if (otherProps.Count != 0)
            writer.CloseBlock(decreaseIndent: false);
    }

    private void WriteFactoryMethodBodyForInheritedModel(IEnumerable<KeyValuePair<string, CodeType>> discriminatorMappings, LanguageWriter writer, CodeMethod method, string? varName = default)
    {
        if (string.IsNullOrEmpty(varName))
            varName = DiscriminatorMappingVarName;

        // Start switch block
        writer.WriteLine($"switch ({varName}) {{");

        // Iterate through discriminator mappings
        foreach (var mappedType in discriminatorMappings)
        {
            // Get the first type from AllTypes
            CodeType? firstType = null;
            foreach (var type in mappedType.Value.AllTypes)
            {
                firstType = type;
                break;
            }

            // Write case statement
            if (firstType != null)
            {
                writer.WriteLine($"case '{mappedType.Key}': return new {conventions.GetTypeString(firstType, method, false, writer)}();");
            }
        }

        // Close switch block
        writer.CloseBlock();
    }
}
