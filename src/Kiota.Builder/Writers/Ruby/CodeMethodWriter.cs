using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Kiota.Builder.OrderComparers;

namespace Kiota.Builder.Writers.Ruby;

public class CodeMethodWriter : BaseElementWriter<CodeMethod, RubyConventionService>
{
    public CodeMethodWriter(RubyConventionService conventionService) : base(conventionService)
    {
    }
    public override void WriteCodeElement(CodeMethod codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        if (codeElement.Parent is not CodeClass parentClass) throw new InvalidOperationException("the parent of a method should be a class");
        var returnType = conventions.GetTypeString(codeElement.ReturnType, codeElement);
        if (parentClass.Properties.Any(static x => x.IsOfKind(CodePropertyKind.QueryParameter, CodePropertyKind.QueryParameters, CodePropertyKind.Headers, CodePropertyKind.Options)))
            writer.WriteLine();
        WriteMethodDocumentation(codeElement, writer);
        var inherits = parentClass.StartBlock.Inherits != null;
        var requestBodyParam = codeElement.Parameters.OfKind(CodeParameterKind.RequestBody);
        var config = codeElement.Parameters.OfKind(CodeParameterKind.RequestConfiguration);
        var requestContentType = codeElement.Parameters.OfKind(CodeParameterKind.RequestBodyContentType);
        var requestParams = new RequestParams(requestBodyParam, config, requestContentType);
        WriteMethodPrototype(codeElement, writer);
        AddNullChecks(codeElement, writer);
        switch (codeElement.Kind)
        {
            case CodeMethodKind.Serializer:
                WriteSerializerBody(parentClass, writer);
                break;
            case CodeMethodKind.Deserializer:
                WriteDeserializerBody(parentClass, writer);
                break;
            case CodeMethodKind.IndexerBackwardCompatibility:
                WriteIndexerBody(codeElement, parentClass, writer, returnType);
                break;
            case CodeMethodKind.RequestGenerator:
                WriteRequestGeneratorBody(codeElement, requestParams, parentClass, writer);
                break;
            case CodeMethodKind.RequestExecutor:
                WriteRequestExecutorBody(codeElement, requestParams, parentClass, returnType, writer);
                break;
            case CodeMethodKind.Getter:
                WriteGetterBody(codeElement, writer);
                break;
            case CodeMethodKind.Setter:
                WriteSetterBody(codeElement, writer);
                break;
            case CodeMethodKind.ClientConstructor:
                WriteConstructorBody(parentClass, codeElement, writer, inherits);
                WriteApiConstructorBody(parentClass, codeElement, writer);
                break;
            case CodeMethodKind.RawUrlBuilder:
                WriteRawUrlBuilderBody(parentClass, codeElement, writer);
                break;
            case CodeMethodKind.Constructor:
                WriteConstructorBody(parentClass, codeElement, writer, inherits);
                break;
            case CodeMethodKind.QueryParametersMapper:
                WriteQueryParametersMapper(codeElement, parentClass, writer);
                break;
            case CodeMethodKind.RequestBuilderWithParameters:
                WriteRequestBuilderBody(parentClass, codeElement, writer);
                break;
            case CodeMethodKind.Factory:
                WriteFactoryMethodBody(codeElement, parentClass, writer);
                break;
            case CodeMethodKind.RequestBuilderBackwardCompatibility:
                throw new InvalidOperationException("RequestBuilderBackwardCompatibility is not supported as the request builders are implemented by properties.");
            case CodeMethodKind.ComposedTypeMarker:
                throw new InvalidOperationException("ComposedTypeMarker is not required as the wrapper is implemented directly.");
            default:
                writer.WriteLine("return nil;");
                break;
        }
        writer.CloseBlock("end");
    }
    private void WriteRawUrlBuilderBody(CodeClass parentClass, CodeMethod codeElement, LanguageWriter writer)
    {
        var rawUrlParameter = codeElement.Parameters.OfKind(CodeParameterKind.RawUrl) ?? throw new InvalidOperationException("RawUrlBuilder method should have a RawUrl parameter");
        var requestAdapterProperty = parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter) ?? throw new InvalidOperationException("RawUrlBuilder method should have a RequestAdapter property");
        writer.WriteLine($"return {parentClass.Name.ToFirstCharacterUpperCase()}.new({rawUrlParameter.Name.ToSnakeCase()}, @{requestAdapterProperty.Name.ToSnakeCase()})");
    }
    private const string DiscriminatorMappingVarName = "mapping_value";
    private const string NodeVarName = "mapping_value_node";
    private void WriteFactoryMethodBody(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer)
    {
        var parseNodeParameter = codeElement.Parameters.OfKind(CodeParameterKind.ParseNode) ?? throw new InvalidOperationException("Factory method should have a ParseNode parameter");
        if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForUnionType)
            WriteFactoryMethodBodyForUnionModel(parseNodeParameter, parentClass, writer);
        else if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForIntersectionType)
            WriteFactoryMethodBodyForIntersectionModel(parseNodeParameter, parentClass, writer);
        else
            WriteFactoryMethodBodyForInheritedModel(parseNodeParameter, parentClass, writer);
    }
    private static void WriteFactoryMethodBodyForInheritedModel(CodeParameter parseNodeParameter, CodeClass parentClass, LanguageWriter writer)
    {
        var writeDiscriminatorValueRead = parentClass.DiscriminatorInformation.ShouldWriteParseNodeCheck && !parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForIntersectionType;
        var discriminatorMappings = parentClass.DiscriminatorInformation.DiscriminatorMappings.OrderBy(static x => x.Key).ToArray();
        if (writeDiscriminatorValueRead && discriminatorMappings.Length > 0)
        {
            writer.WriteLine($"{NodeVarName} = {parseNodeParameter.Name.ToSnakeCase()}.get_child_node(\"{RubyConventionService.SanitizeRubyDoubleQuoteLiteral(parentClass.DiscriminatorInformation.DiscriminatorPropertyName)}\")");
            writer.StartBlock($"unless {NodeVarName}.nil?");
            writer.WriteLine($"{DiscriminatorMappingVarName} = {NodeVarName}.get_string_value");
            writer.StartBlock($"case {DiscriminatorMappingVarName}", false);
            foreach (var mappedType in discriminatorMappings)
            {
                writer.StartBlock($"when \"{RubyConventionService.SanitizeRubyDoubleQuoteLiteral(mappedType.Key)}\"");
                writer.WriteLine($"return {mappedType.Value.AllTypes.First().Name.ToFirstCharacterUpperCase()}.new");
                writer.DecreaseIndent();
            }
            writer.CloseBlock("end", false);
            writer.CloseBlock("end");
        }
        writer.WriteLine($"return {parentClass.Name.ToFirstCharacterUpperCase()}.new");
    }
    private void WriteFactoryMethodBodyForUnionModel(CodeParameter parseNodeParameter, CodeClass parentClass, LanguageWriter writer)
    {
        writer.WriteLine($"result = {parentClass.Name.ToFirstCharacterUpperCase()}.new");
        var parseNodeParameterName = parseNodeParameter.Name.ToSnakeCase();
        var customProperties = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)
                                          .OrderBy(static x => x, new CodePropertyTypeComparer())
                                          .ThenBy(static x => x.Name, StringComparer.OrdinalIgnoreCase)
                                          .ToArray();
        var complexPropertiesWithMappings = customProperties
            .Where(static x => x.Type is CodeType propType && propType.TypeDefinition is CodeClass && propType.CollectionKind == CodeTypeBase.CodeTypeCollectionKind.None)
            .Select(p => (property: p, mappedKey: parentClass.DiscriminatorInformation.DiscriminatorMappings
                .FirstOrDefault(x => x.Value.Name.Equals(p.Type.Name, StringComparison.OrdinalIgnoreCase)).Key))
            .Where(static x => !string.IsNullOrEmpty(x.mappedKey))
            .ToArray();
        if (complexPropertiesWithMappings.Length > 0)
        {
            writer.WriteLine($"{NodeVarName} = {parseNodeParameterName}.get_child_node(\"{RubyConventionService.SanitizeRubyDoubleQuoteLiteral(parentClass.DiscriminatorInformation.DiscriminatorPropertyName)}\")");
            writer.StartBlock($"unless {NodeVarName}.nil?");
            writer.WriteLine($"{DiscriminatorMappingVarName} = {NodeVarName}.get_string_value");
            var elseIfPrefix = string.Empty;
            foreach (var (property, mappedKey) in complexPropertiesWithMappings)
            {
                // safe navigation: a ParseNode may yield a nil discriminator value, and the
                // inherited factory's `case` path tolerates that, so this one must too
                writer.StartBlock($"{elseIfPrefix}if {DiscriminatorMappingVarName}&.downcase == \"{RubyConventionService.SanitizeRubyDoubleQuoteLiteral(mappedKey)}\".downcase");
                writer.WriteLine($"result.{property.Name.ToSnakeCase()} = {property.Type.Name.ToFirstCharacterUpperCase()}.new");
                writer.DecreaseIndent();
                elseIfPrefix = "els";
            }
            // the loop already restored the indent, so the chain's `end` must not decrease it again
            writer.CloseBlock("end", false);
            writer.CloseBlock("end");
        }
        foreach (var property in customProperties.Where(static x => x.Type is not CodeType propType || propType.TypeDefinition is not CodeClass || propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None))
        {
            var methodName = GetDeserializationMethodName(property.Type);
            writer.WriteLine($"val = {parseNodeParameterName}.{methodName}");
            writer.StartBlock("unless val.nil?");
            writer.WriteLine($"result.{property.Name.ToSnakeCase()} = val");
            writer.CloseBlock("end");
        }
        writer.WriteLine("return result");
    }
    private static string GetIntersectionValueVarName(CodeProperty property) => $"val_{property.Name.ToSnakeCase()}";
    private void WriteFactoryMethodBodyForIntersectionModel(CodeParameter parseNodeParameter, CodeClass parentClass, LanguageWriter writer)
    {
        writer.WriteLine($"result = {parentClass.Name.ToFirstCharacterUpperCase()}.new");
        var parseNodeParameterName = parseNodeParameter.Name.ToSnakeCase();
        var customProperties = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)
                                          .OrderBy(static x => x, new CodePropertyTypeComparer(orderByDesc: true))
                                          .ThenBy(static x => x.Name, StringComparer.OrdinalIgnoreCase)
                                          .ToArray();
        var nonComplexProperties = customProperties.Where(static x => x.Type is not CodeType propType || propType.TypeDefinition is not CodeClass || propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None).ToArray();
        var complexProperties = customProperties.Where(static x => x.Type is CodeType propType && propType.TypeDefinition is CodeClass && propType.CollectionKind == CodeTypeBase.CodeTypeCollectionKind.None).ToArray();
        // each property needs its own variable: a shared one would be reassigned inside the
        // previous branch of the if/elsif chain, so only the first property would ever be read
        foreach (var property in nonComplexProperties)
        {
            var methodName = GetDeserializationMethodName(property.Type);
            writer.WriteLine($"{GetIntersectionValueVarName(property)} = {parseNodeParameterName}.{methodName}");
        }
        var elseIfPrefix = string.Empty;
        foreach (var property in nonComplexProperties)
        {
            writer.StartBlock($"{elseIfPrefix}if !{GetIntersectionValueVarName(property)}.nil?");
            writer.WriteLine($"result.{property.Name.ToSnakeCase()} = {GetIntersectionValueVarName(property)}");
            writer.DecreaseIndent();
            elseIfPrefix = "els";
        }
        if (complexProperties.Length > 0 && nonComplexProperties.Length > 0)
        {
            writer.StartBlock("else");
            foreach (var property in complexProperties)
            {
                writer.WriteLine($"result.{property.Name.ToSnakeCase()} = {property.Type.Name.ToFirstCharacterUpperCase()}.new");
            }
            writer.DecreaseIndent();
        }
        else if (complexProperties.Length > 0)
        {
            foreach (var property in complexProperties)
            {
                writer.WriteLine($"result.{property.Name.ToSnakeCase()} = {property.Type.Name.ToFirstCharacterUpperCase()}.new");
            }
        }
        if (nonComplexProperties.Length > 0)
            writer.CloseBlock("end", false);
        writer.WriteLine("return result");
    }
    private static void AddNullChecks(CodeMethod codeElement, LanguageWriter writer)
    {
        if (!codeElement.IsOverload)
            foreach (var parameter in codeElement.Parameters
                                                .Where(static x => !x.Optional && !x.IsOfKind(CodeParameterKind.PathParameters, CodeParameterKind.RequestAdapter))
                                                .Select(static x => x.Name.ToSnakeCase())
                                                .OrderBy(static x => x))
                writer.WriteLine($"raise StandardError, '{parameter} cannot be null' if {parameter}.nil?");
    }
    private static void WriteQueryParametersMapper(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer)
    {
        var parameter = codeElement.Parameters.FirstOrDefault(static x => x.IsOfKind(CodeParameterKind.QueryParametersMapperParameter));
        if (parameter == null) throw new InvalidOperationException("QueryParametersMapper should have a parameter of type QueryParametersMapper");
        var parameterName = parameter.Name.ToSnakeCase();
        writer.StartBlock($"case {parameterName}", false);
        var escapedProperties = parentClass.Properties.Where(static x => x.IsOfKind(CodePropertyKind.QueryParameter) && x.IsNameEscaped);
        foreach (var escapedProperty in escapedProperties)
        {
            writer.StartBlock($"when \"{escapedProperty.Name}\"");
            writer.WriteLine($"return \"{RubyConventionService.SanitizeRubyDoubleQuoteLiteral(escapedProperty.SerializationName)}\"");
            writer.DecreaseIndent();
        }
        writer.StartBlock("else");
        writer.WriteLine($"return {parameterName}");
        writer.DecreaseIndent();
        writer.CloseBlock("end", false);
    }
    private void WriteRequestBuilderBody(CodeClass parentClass, CodeMethod codeElement, LanguageWriter writer)
    {
        var importSymbol = conventions.GetTypeString(codeElement.ReturnType, parentClass);
        conventions.AddRequestBuilderBody(parentClass, importSymbol, writer, prefix: "return ", pathParameters: codeElement.Parameters.Where(static x => x.IsOfKind(CodeParameterKind.Path)));
    }
    private static void WriteApiConstructorBody(CodeClass parentClass, CodeMethod method, LanguageWriter writer)
    {
        var requestAdapterProperty = parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter);
        var pathParametersProperty = parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters);
        var requestAdapterPropertyName = $"{requestAdapterProperty?.NamePrefix}{requestAdapterProperty?.Name.ToSnakeCase()}";
        WriteSerializationRegistration(parentClass, method.SerializerModules, writer, "register_default_serializer");
        WriteSerializationRegistration(parentClass, method.DeserializerModules, writer, "register_default_deserializer");
        if (!string.IsNullOrEmpty(method.BaseUrl))
        {
            writer.StartBlock($"if @{requestAdapterPropertyName}.get_base_url.nil? || @{requestAdapterPropertyName}.get_base_url.empty?");
            writer.WriteLine($"@{requestAdapterPropertyName}.set_base_url('{method.BaseUrl.SanitizeSingleQuote()}')");
            writer.CloseBlock("end");
            if (pathParametersProperty != null)
                writer.WriteLine($"@{pathParametersProperty.Name.ToSnakeCase()}['baseurl'] = @{requestAdapterPropertyName}.get_base_url");
        }
    }
    private static void WriteSerializationRegistration(CodeClass parentClass, HashSet<string> serializationClassNames, LanguageWriter writer, string methodName)
    {
        if (serializationClassNames != null)
            foreach (var serializationClassName in serializationClassNames)
            {
                var prefix = parentClass.Usings.FirstOrDefault(x => x.IsExternal && x.Name.Equals(serializationClassName, StringComparison.OrdinalIgnoreCase))?.Declaration?.Name;
                if (!string.IsNullOrEmpty(prefix))
                    prefix = $"{prefix.ToPascalCase(['_'])}::";
                writer.WriteLine($"MicrosoftKiotaAbstractions::ApiClientBuilder.{methodName}({prefix}{serializationClassName})");
            }
    }
    private void WriteConstructorBody(CodeClass parentClass, CodeMethod currentMethod, LanguageWriter writer, bool inherits)
    {
        if (inherits)
            if (parentClass.IsOfKind(CodeClassKind.RequestBuilder) &&
                currentMethod.Parameters.OfKind(CodeParameterKind.RequestAdapter) is CodeParameter requestAdapterParameter &&
                parentClass.Properties.FirstOrDefaultOfKind(CodePropertyKind.UrlTemplate) is CodeProperty urlTemplateProperty &&
                !string.IsNullOrEmpty(urlTemplateProperty.DefaultValue))
            {
                var sanitizedUrlTemplate = RubyConventionService.SanitizeRubyDoubleQuoteLiteral(urlTemplateProperty.DefaultValue);
                if (currentMethod.Parameters.OfKind(CodeParameterKind.PathParameters) is CodeParameter pathParametersParameter)
                    writer.WriteLine($"super({pathParametersParameter.Name.ToSnakeCase()}, {requestAdapterParameter.Name.ToSnakeCase()}, {sanitizedUrlTemplate})");
                else
                    writer.WriteLine($"super(Hash.new, {requestAdapterParameter.Name.ToSnakeCase()}, {sanitizedUrlTemplate})");
            }
            else
                writer.WriteLine("super");
        foreach (var propWithDefault in parentClass.GetPropertiesOfKind(CodePropertyKind.BackingStore,
                                                                        CodePropertyKind.RequestBuilder)
                                        .Where(static x => !string.IsNullOrEmpty(x.DefaultValue))
                                        .OrderBy(static x => x.Name))
        {
            writer.WriteLine($"@{propWithDefault.NamePrefix}{propWithDefault.Name.ToSnakeCase()} = {RubyConventionService.SanitizeRubyDoubleQuoteLiteral(propWithDefault.DefaultValue)}");
        }
        foreach (var propWithDefault in parentClass.GetPropertiesOfKind(CodePropertyKind.AdditionalData,
                                                                        CodePropertyKind.Custom) //additional data and custom properties rely on accessors
                                        .Where(static x => !string.IsNullOrEmpty(x.DefaultValue))
                                        // do not apply the default value if the type is composed as the default value may not necessarily which type to use
                                        .Where(static x => x.Type is not CodeType propType || propType.TypeDefinition is not CodeClass propertyClass || propertyClass.OriginalComposedType is null)
                                        .OrderBy(static x => x.Name))
        {
            string defaultValue = RubyConventionService.SanitizeRubyDoubleQuoteLiteral(propWithDefault.DefaultValue);
            if (propWithDefault.Type is CodeType propertyType && propertyType.TypeDefinition is CodeEnum enumDefinition)
            {
                var trimmedDefault = defaultValue.TrimQuotes();
                var matchingOption = enumDefinition.Options.FirstOrDefault(x => x.WireName.Equals(trimmedDefault, StringComparison.OrdinalIgnoreCase));
                var optionName = (matchingOption?.Name ?? trimmedDefault).CleanupSymbolName().ToFirstCharacterUpperCase();
                defaultValue = $"{conventions.GetNormalizedNamespacePrefixForType(propWithDefault.Type)}{conventions.GetTypeString(propWithDefault.Type, currentMethod)}[:{optionName}]";
            }
            else
            {
                if (propWithDefault.Type is CodeType propertyType2 &&
                    TryNormalizePrimitiveDefaultValue(defaultValue, propertyType2, out var normalizedDefaultValue))
                {
                    if (normalizedDefaultValue is null)
                        continue;
                    defaultValue = normalizedDefaultValue;
                }
                else
                    defaultValue = propWithDefault.Type.Name.ToLowerInvariant() switch
                    {
                        "datetime" => $"DateTime.parse({defaultValue})",
                        "date" => $"Date.parse({defaultValue})",
                        "time" => $"Time.parse({defaultValue})",
                        "guid" => $"UUIDTools::UUID.parse({defaultValue})",
                        _ => defaultValue
                    };
            }
            writer.WriteLine($"@{propWithDefault.NamePrefix}{propWithDefault.Name.ToSnakeCase()} = {defaultValue}");
        }
    }
    private static bool TryNormalizePrimitiveDefaultValue(string defaultValue, CodeType propertyType, out string? normalizedDefaultValue)
    {
        if (propertyType.Name.Equals("boolean", StringComparison.OrdinalIgnoreCase))
        {
            normalizedDefaultValue = PrimitiveDefaultValueUtils.TryNormalizeBooleanLiteral(defaultValue, out var booleanDefaultValue) ?
                booleanDefaultValue :
                null;
            return true;
        }
        if (PrimitiveDefaultValueUtils.IsNumericType(propertyType.Name))
        {
            normalizedDefaultValue = PrimitiveDefaultValueUtils.TryNormalizeNumericLiteral(defaultValue.TrimQuotes(), propertyType.Name, out var numericDefaultValue) ?
                numericDefaultValue :
                null;
            return true;
        }
        normalizedDefaultValue = null;
        return false;
    }
    private static void WriteSetterBody(CodeMethod codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        var parameterName = codeElement.Parameters.FirstOrDefault(static x => x.IsOfKind(CodeParameterKind.SetterValue))?.Name.ToSnakeCase();
        if (codeElement.AccessedProperty is not null)
            writer.WriteLine($"@{codeElement.AccessedProperty.NamePrefix}{codeElement.AccessedProperty.Name.ToSnakeCase()} = {parameterName}");
    }
    private static void WriteGetterBody(CodeMethod codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        if (codeElement.AccessedProperty is not null)
            writer.WriteLine($"return @{codeElement.AccessedProperty.NamePrefix}{codeElement.AccessedProperty.Name.ToSnakeCase()}");
    }
    private void WriteIndexerBody(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer, string returnType)
    {
        var prefix = conventions.GetNormalizedNamespacePrefixForType(codeElement.ReturnType);
        if (parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters) is CodeProperty pathParametersProperty &&
            codeElement.OriginalIndexer != null)
            writer.WriteLines($"{conventions.TempDictionaryVarName} = @{pathParametersProperty.NamePrefix}{pathParametersProperty.Name.ToSnakeCase()}.clone",
                            $"{conventions.TempDictionaryVarName}[\"{RubyConventionService.SanitizeRubyDoubleQuoteLiteral(codeElement.OriginalIndexer.IndexParameter.SerializationName)}\"] = {codeElement.OriginalIndexer.IndexParameter.Name.ToSnakeCase()}");
        conventions.AddRequestBuilderBody(parentClass, returnType, writer, conventions.TempDictionaryVarName, $"return {prefix}");
    }
    private void WriteDeserializerBody(CodeClass parentClass, LanguageWriter writer)
    {
        if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForUnionType)
            WriteDeserializerBodyForUnionModel(parentClass, writer);
        else if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForIntersectionType)
            WriteDeserializerBodyForIntersectionModel(parentClass, writer);
        else
            WriteDeserializerBodyForInheritedModel(parentClass, writer);
    }
    private void WriteDeserializerBodyForInheritedModel(CodeClass parentClass, LanguageWriter writer)
    {
        if (parentClass.StartBlock.Inherits != null)
            writer.WriteLine("return super.merge({");
        else
            writer.WriteLine("return {");
        writer.IncreaseIndent();
        foreach (var otherProp in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)
                                            .Where(static x => !x.ExistsInBaseType)
                                            .OrderBy(static x => x.Name))
        {
            writer.WriteLine($"\"{RubyConventionService.SanitizeRubyDoubleQuoteLiteral(otherProp.WireName)}\" => lambda {{|n| @{otherProp.NamePrefix}{otherProp.Name.ToSnakeCase()} = n.{GetDeserializationMethodName(otherProp.Type)} }},");
        }
        writer.DecreaseIndent();
        if (parentClass.StartBlock.Inherits != null)
            writer.WriteLine("})");
        else
            writer.WriteLine("}");
    }
    private static void WriteDeserializerBodyForUnionModel(CodeClass parentClass, LanguageWriter writer)
    {
        var complexProperties = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)
                                           .Where(static x => x.Type is CodeType propType && propType.TypeDefinition is CodeClass && propType.CollectionKind == CodeTypeBase.CodeTypeCollectionKind.None)
                                           .OrderBy(static x => x.Name, StringComparer.OrdinalIgnoreCase)
                                           .ToArray();
        foreach (var property in complexProperties)
        {
            writer.StartBlock($"unless @{property.Name.ToSnakeCase()}.nil?");
            writer.WriteLine($"return @{property.Name.ToSnakeCase()}.get_field_deserializers()");
            writer.CloseBlock("end");
        }
        writer.WriteLine("return {}");
    }
    private static void WriteDeserializerBodyForIntersectionModel(CodeClass parentClass, LanguageWriter writer)
    {
        var complexProperties = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)
                                           .Where(static x => x.Type is CodeType propType && propType.TypeDefinition is CodeClass && propType.CollectionKind == CodeTypeBase.CodeTypeCollectionKind.None)
                                           .OrderBy(static x => x.Name, StringComparer.OrdinalIgnoreCase)
                                           .ToArray();
        if (complexProperties.Length > 0)
        {
            var condition = string.Join(" || ", complexProperties.Select(x => $"@{x.Name.ToSnakeCase()}"));
            writer.StartBlock($"if {condition}");
            var propNames = string.Join(", ", complexProperties.Select(x => $"@{x.Name.ToSnakeCase()}"));
            writer.WriteLine($"return MicrosoftKiotaAbstractions::ParseNodeHelper.merge_deserializers_for_intersection_wrapper({propNames})");
            writer.CloseBlock("end");
        }
        writer.WriteLine("return {}");
    }
    private void WriteRequestExecutorBody(CodeMethod codeElement, RequestParams requestParams, CodeClass parentClass, string returnType, LanguageWriter writer)
    {
        if (returnType.Equals("void", StringComparison.OrdinalIgnoreCase))
            returnType = "nil"; //generic type for the future
        else if (codeElement.ReturnType is CodeType returnT && returnT.TypeDefinition is not null)
            returnType = getDeserializationLambda(returnT);
        if (codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");


        var generatorMethodName = parentClass
                                            .Methods
                                            .FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestGenerator) && x.HttpMethod == codeElement.HttpMethod)
                                            ?.Name
                                            ?.ToSnakeCase();
        writer.WriteLine($"request_info = self.{generatorMethodName}(");
        var requestInfoParameters = new CodeParameter?[] { requestParams.requestBody, requestParams.requestContentType, requestParams.requestConfiguration }
            .OfType<CodeParameter>()
            .Select(static x => x.Name.ToSnakeCase())
            .ToArray();
        if (requestInfoParameters.Length != 0)
        {
            writer.IncreaseIndent();
            writer.WriteLine(requestInfoParameters.Aggregate(static (x, y) => $"{x}, {y}"));
            writer.DecreaseIndent();
        }
        writer.WriteLine(")");
        var isStream = conventions.StreamTypeName.Equals(returnType, StringComparison.OrdinalIgnoreCase);
        var genericTypeForSendMethod = GetSendRequestMethodName(isStream);
        var errorMappingVarName = "nil";
        if (codeElement.ErrorMappings.Any())
        {
            errorMappingVarName = "error_mapping";
            writer.WriteLine($"{errorMappingVarName} = Hash.new");
            foreach (var errorMapping in codeElement.ErrorMappings)
            {
                writer.WriteLine($"{errorMappingVarName}[\"{errorMapping.Key.ToUpperInvariant()}\"] = {getDeserializationLambda(errorMapping.Value)}");
            }
        }
        writer.WriteLine($"return @request_adapter.{genericTypeForSendMethod}(request_info, {returnType}, {errorMappingVarName})");
    }

    private void WriteRequestGeneratorBody(CodeMethod codeElement, RequestParams requestParams, CodeClass parentClass, LanguageWriter writer)
    {
        if (codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");

        writer.WriteLine("request_info = MicrosoftKiotaAbstractions::RequestInformation.new()");
        if (requestParams.requestConfiguration != null)
        {
            var queryString = requestParams.QueryParameters;
            var headers = requestParams.Headers;
            var options = requestParams.Options;
            if (headers != null || queryString != null)
            {
                writer.WriteLine($"unless {requestParams.requestConfiguration.Name.ToSnakeCase()}.nil?");
                writer.IncreaseIndent();
                if (headers != null)
                    writer.WriteLine($"request_info.add_headers_from_raw_object({requestParams.requestConfiguration.Name.ToSnakeCase()}.{headers.Name.ToSnakeCase()})");
                if (queryString != null)
                    writer.WriteLine($"request_info.set_query_string_parameters_from_raw_object({requestParams.requestConfiguration.Name.ToSnakeCase()}.{queryString.Name.ToSnakeCase()})");
                if (options != null)
                    writer.WriteLine($"request_info.add_request_options({requestParams.requestConfiguration.Name.ToSnakeCase()}.{options.Name.ToSnakeCase()})");
                writer.CloseBlock("end");
            }
            if (requestParams.requestBody != null)
            {
                var sanitizedRequestBodyContentType = codeElement.RequestBodyContentType.SanitizeSingleQuote();
                if (requestParams.requestBody.Type.Name.Equals(conventions.StreamTypeName, StringComparison.OrdinalIgnoreCase))
                {
                    if (requestParams.requestContentType is not null)
                        writer.WriteLine($"request_info.set_stream_content({requestParams.requestBody.Name}, {requestParams.requestContentType.Name})");
                    else if (!string.IsNullOrEmpty(sanitizedRequestBodyContentType))
                        writer.WriteLine($"request_info.set_stream_content({requestParams.requestBody.Name}, '{sanitizedRequestBodyContentType}')");
                }
                else if (parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter) is CodeProperty requestAdapterProperty)
                    writer.WriteLine($"request_info.set_content_from_parsable(@{requestAdapterProperty.Name.ToSnakeCase()}, '{sanitizedRequestBodyContentType}', {requestParams.requestBody.Name})");
            }
        }
        if (parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters) is CodeProperty urlTemplateParamsProperty &&
            parentClass.GetPropertyOfKind(CodePropertyKind.UrlTemplate) is CodeProperty urlTemplateProperty)
        {
            var urlTemplateValue = codeElement.HasUrlTemplateOverride ? $"'{codeElement.UrlTemplateOverride.SanitizeSingleQuote()}'" : GetPropertyCall(urlTemplateProperty, "''");
            writer.WriteLines($"request_info.url_template = {urlTemplateValue}",
                            $"request_info.path_parameters = {GetPropertyCall(urlTemplateParamsProperty, "''")}");
        }
        writer.WriteLine($"request_info.http_method = :{codeElement.HttpMethod.Value.ToString().ToUpperInvariant()}");
        if (codeElement.ShouldAddAcceptHeader)
            writer.WriteLine($"request_info.headers.try_add('Accept', '{codeElement.AcceptHeaderValue.SanitizeSingleQuote()}')");
        writer.WriteLine("return request_info");
    }
    private static string GetPropertyCall(CodeProperty property, string defaultValue) => property == null ? defaultValue : $"@{property.NamePrefix}{property.Name.ToSnakeCase()}";
    private void WriteSerializerBody(CodeClass parentClass, LanguageWriter writer)
    {
        if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForUnionType)
            WriteSerializerBodyForUnionModel(parentClass, writer);
        else if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForIntersectionType)
            WriteSerializerBodyForIntersectionModel(parentClass, writer);
        else
            WriteSerializerBodyForInheritedModel(parentClass, writer);
    }
    private void WriteSerializerBodyForInheritedModel(CodeClass parentClass, LanguageWriter writer)
    {
        var additionalDataProperty = parentClass.GetPropertyOfKind(CodePropertyKind.AdditionalData);
        if (parentClass.StartBlock.Inherits != null)
            writer.WriteLine("super");
        foreach (var otherProp in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)
                                            .Where(static x => !x.ExistsInBaseType && !x.ReadOnly)
                                            .OrderBy(static x => x.Name))
        {
            writer.WriteLine($"writer.{GetSerializationMethodName(otherProp.Type)}(\"{RubyConventionService.SanitizeRubyDoubleQuoteLiteral(otherProp.WireName)}\", @{otherProp.Name.ToSnakeCase()})");
        }
        if (additionalDataProperty != null)
            writer.WriteLine($"writer.write_additional_data(@{additionalDataProperty.NamePrefix}{additionalDataProperty.Name.ToSnakeCase()})");
    }
    private void WriteSerializerBodyForUnionModel(CodeClass parentClass, LanguageWriter writer)
    {
        var customProperties = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)
                                          .OrderBy(static x => x, new CodePropertyTypeComparer())
                                          .ThenBy(static x => x.Name, StringComparer.OrdinalIgnoreCase)
                                          .ToArray();
        var elseIfPrefix = string.Empty;
        foreach (var property in customProperties)
        {
            writer.StartBlock($"{elseIfPrefix}if !@{property.Name.ToSnakeCase()}.nil?");
            writer.WriteLine($"writer.{GetSerializationMethodName(property.Type)}(nil, @{property.Name.ToSnakeCase()})");
            writer.DecreaseIndent();
            elseIfPrefix = "els";
        }
        // the loop already restored the indent, so the chain's `end` must not decrease it again
        if (customProperties.Length > 0)
            writer.CloseBlock("end", false);
    }
    private void WriteSerializerBodyForIntersectionModel(CodeClass parentClass, LanguageWriter writer)
    {
        var customProperties = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)
                                          .OrderBy(static x => x, new CodePropertyTypeComparer(orderByDesc: true))
                                          .ThenBy(static x => x.Name, StringComparer.OrdinalIgnoreCase)
                                          .ToArray();
        var nonComplexProperties = customProperties.Where(static x => x.Type is not CodeType propType || propType.TypeDefinition is not CodeClass || propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None).ToArray();
        var complexProperties = customProperties.Where(static x => x.Type is CodeType propType && propType.TypeDefinition is CodeClass && propType.CollectionKind == CodeTypeBase.CodeTypeCollectionKind.None).ToArray();
        var elseIfPrefix = string.Empty;
        foreach (var property in nonComplexProperties)
        {
            writer.StartBlock($"{elseIfPrefix}if !@{property.Name.ToSnakeCase()}.nil?");
            writer.WriteLine($"writer.{GetSerializationMethodName(property.Type)}(nil, @{property.Name.ToSnakeCase()})");
            writer.DecreaseIndent();
            elseIfPrefix = "els";
        }
        if (complexProperties.Length > 0)
        {
            if (nonComplexProperties.Length > 0)
                writer.StartBlock("else");
            // write_object_value returns early when its first argument is nil, which would drop
            // every remaining member, so compact the list and skip the call when nothing is set
            var complexPropNames = string.Join(", ", complexProperties.Select(x => $"@{x.Name.ToSnakeCase()}"));
            writer.WriteLine($"composed_values = [{complexPropNames}].compact");
            writer.WriteLine("writer.write_object_value(nil, *composed_values) unless composed_values.empty?");
            if (nonComplexProperties.Length > 0)
                writer.DecreaseIndent();
        }
        // the branches above already restored the indent, so the chain's `end` must not decrease it again
        if (nonComplexProperties.Length > 0)
            writer.CloseBlock("end", false);
    }
    private static readonly BaseCodeParameterOrderComparer parameterOrderComparer = new();
    private void WriteMethodPrototype(CodeMethod code, LanguageWriter writer)
    {
        var methodName = code.Kind switch
        {
            CodeMethodKind.Constructor or CodeMethodKind.ClientConstructor => "initialize",
            CodeMethodKind.Getter => $"{code.AccessedProperty?.Name?.ToSnakeCase()}",
            CodeMethodKind.Setter => $"{code.AccessedProperty?.Name?.ToSnakeCase()}",
            _ => code.Name.ToSnakeCase()
        };
        var parameters = string.Join(", ", code.Parameters
                                                .OrderBy(static x => x, parameterOrderComparer)
                                                .Select(p => conventions.GetParameterSignature(p, code).ToSnakeCase())
                                                .ToList());
        var staticPrefix = code.IsStatic ? "self." : string.Empty;
        var openParenthesis = code.IsOfKind(CodeMethodKind.Getter) ? string.Empty : "(";
        var closeParenthesis = code.IsOfKind(CodeMethodKind.Getter) ? string.Empty : ")";
        var equalsSign = code.IsOfKind(CodeMethodKind.Setter) ? "=" : string.Empty;
        writer.StartBlock($"def {staticPrefix}{methodName.ToSnakeCase()}{equalsSign}{openParenthesis}{parameters}{closeParenthesis}");
    }
    private void WriteMethodDocumentation(CodeMethod code, LanguageWriter writer)
    {
        var parametersWithDescription = code.Parameters.Where(static x => x.Documentation.DescriptionAvailable).OrderBy(static x => x.Name, StringComparer.OrdinalIgnoreCase).ToArray();
        if (code.Documentation.DescriptionAvailable || parametersWithDescription.Length != 0)
        {
            writer.WriteLine(conventions.DocCommentStart);
            if (code.Documentation.DescriptionAvailable)
            {
                var description = code.Documentation.GetDescription(type => conventions.GetTypeString(type, code), normalizationFunc: RubyConventionService.RemoveInvalidDescriptionCharacters);
                writer.WriteLine($"{conventions.DocCommentPrefix}{description}");
            }
            foreach (var paramWithDescription in parametersWithDescription)
            {
                var description = paramWithDescription.Documentation.GetDescription(type => conventions.GetTypeString(type, code), normalizationFunc: RubyConventionService.RemoveInvalidDescriptionCharacters);
                writer.WriteLine($"{conventions.DocCommentPrefix}@param {paramWithDescription.Name.ToSnakeCase()} {description}");
            }

            if (code.IsAsync)
                writer.WriteLine($"{conventions.DocCommentPrefix}@return a Fiber of {code.ReturnType.Name.ToSnakeCase()}");
            else
                writer.WriteLine($"{conventions.DocCommentPrefix}@return a {code.ReturnType.Name.ToSnakeCase()}");
            writer.WriteLine(conventions.DocCommentEnd);
        }
    }
    private string GetDeserializationMethodName(CodeTypeBase propType)
    {
        var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
        var propertyType = conventions.TranslateType(propType);
        if (propType is CodeType currentType)
        {
            if (isCollection)
                if (currentType.TypeDefinition == null)
                    return $"get_collection_of_primitive_values({TranslateObjectType(propertyType.ToFirstCharacterUpperCase())})";
                else
                    return $"get_collection_of_object_values({getDeserializationLambda(currentType)})";
            if (currentType.TypeDefinition is CodeEnum currentEnum)
                return $"get_enum_value{(currentEnum.Flags ? "s" : string.Empty)}({currentType.TypeDefinition.Parent?.Name.NormalizeNameSpaceName("::").ToFirstCharacterUpperCase()}::{propertyType.ToFirstCharacterUpperCase()})";
        }
        return propertyType switch
        {
            "string" or "boolean" or "number" or "float" or "Guid" => $"get_{propertyType.ToSnakeCase()}_value()",
            "binary" or "Binary" or "base64" or "base64url" => "get_string_value()", //TODO: add support for binary
            "DateTimeOffset" or "DateTime" => "get_date_time_value()",
            "TimeSpan" or "MicrosoftKiotaAbstractions::ISODuration" => "get_duration_value()",
            "DateOnly" or "Date" => "get_date_value()",
            "TimeOnly" or "Time" => "get_time_value()",
            _ => $"get_object_value({getDeserializationLambda(propType)})",
        };
    }
    private static string getDeserializationLambda(CodeTypeBase targetTypeBase)
    {
        if (targetTypeBase is not CodeType targetType)
            return "lambda {|pn| nil }";
        var nsPrefix = targetType.TypeDefinition?.Parent?.Name.NormalizeNameSpaceName("::").ToFirstCharacterUpperCase();
        if (!string.IsNullOrEmpty(nsPrefix))
            nsPrefix += "::";
        return $"lambda {{|pn| {nsPrefix}{targetType.Name.ToFirstCharacterUpperCase()}.create_from_discriminator_value(pn) }}";
    }
    private static string TranslateObjectType(string typeName)
    {
        return typeName switch
        {
            "String" or "Float" or "Object" => typeName,
            "Boolean" => "\"boolean\"",
            "Number" => "Integer",
            "Guid" => "UUIDTools::UUID",
            "Date" => "Time",
            "DateTimeOffset" => "Time",
            _ => typeName.ToFirstCharacterUpperCase() is string tName && !string.IsNullOrEmpty(tName) ? tName : "Object",
        };
    }
    private string GetSerializationMethodName(CodeTypeBase propType)
    {
        var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
        var propertyType = conventions.TranslateType(propType);
        if (propType is CodeType currentType)
        {
            if (isCollection)
                if (currentType.TypeDefinition == null)
                    return "write_collection_of_primitive_values";
                else
                    return "write_collection_of_object_values";
            if (currentType.TypeDefinition is CodeEnum)
                return "write_enum_value";
        }
        return propertyType switch
        {
            "string" or "boolean" or "number" or "float" or "Guid" => $"write_{propertyType.ToSnakeCase()}_value",
            "binary" or "base64" or "base64url" => "write_string_value", //TODO: add support for binary
            "DateTimeOffset" or "DateTime" => "write_date_time_value",
            "TimeSpan" or "MicrosoftKiotaAbstractions::ISODuration" => "write_duration_value",
            "DateOnly" or "Date" => "write_date_value",
            "TimeOnly" or "Time" => "write_time_value",
            _ => "write_object_value",
        };
    }
    private static string GetSendRequestMethodName(bool isStream)
    {
        if (isStream) return "send_primitive_async";
        return "send_async";
    }
}
