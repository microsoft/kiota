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
        RubyConventionService.WriteMemberSeparator(writer);
        WriteMethodDocumentation(codeElement, writer);
        var inherits = parentClass.StartBlock.Inherits != null;
        var requestBodyParam = codeElement.Parameters.OfKind(CodeParameterKind.RequestBody);
        var config = codeElement.Parameters.OfKind(CodeParameterKind.RequestConfiguration);
        var requestContentType = codeElement.Parameters.OfKind(CodeParameterKind.RequestBodyContentType);
        var requestParams = new RequestParams(requestBodyParam, config, requestContentType);
        WriteMethodPrototype(codeElement, writer);
        var wroteGuards = AddNullChecks(codeElement, writer);
        var body = writer.CaptureLines(() => WriteMethodBody(codeElement, parentClass, requestParams, returnType, inherits, writer));
        if (wroteGuards && body.Count != 0)
            writer.WriteLine();
        foreach (var line in body)
            writer.WriteLine(line, false);
        writer.CloseBlock("end");
    }
    private void WriteMethodBody(CodeMethod codeElement, CodeClass parentClass, RequestParams requestParams, string returnType, bool inherits, LanguageWriter writer)
    {
        switch (codeElement.Kind)
        {
            case CodeMethodKind.Serializer:
                WriteSerializerBody(parentClass, writer);
                break;
            case CodeMethodKind.Deserializer:
                WriteDeserializerBody(parentClass, writer);
                break;
            case CodeMethodKind.IndexerBackwardCompatibility:
                WriteIndexerBody(codeElement, parentClass, writer);
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
                writer.WriteLine("nil");
                break;
        }
    }
    private void WriteRawUrlBuilderBody(CodeClass parentClass, CodeMethod codeElement, LanguageWriter writer)
    {
        var rawUrlParameter = codeElement.Parameters.OfKind(CodeParameterKind.RawUrl) ?? throw new InvalidOperationException("RawUrlBuilder method should have a RawUrl parameter");
        var requestAdapterProperty = parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter) ?? throw new InvalidOperationException("RawUrlBuilder method should have a RequestAdapter property");
        writer.WriteLine($"{parentClass.Name.ToFirstCharacterUpperCase()}.new({RubyConventionService.GetParameterName(rawUrlParameter)}, @{requestAdapterProperty.Name.ToSnakeCase()})");
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
            writer.WriteLine($"{NodeVarName} = {RubyConventionService.GetParameterName(parseNodeParameter)}.get_child_node(\"{RubyConventionService.SanitizeRubyDoubleQuoteLiteral(parentClass.DiscriminatorInformation.DiscriminatorPropertyName)}\")");
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
        writer.WriteLine($"{parentClass.Name.ToFirstCharacterUpperCase()}.new");
    }
    private void WriteFactoryMethodBodyForUnionModel(CodeParameter parseNodeParameter, CodeClass parentClass, LanguageWriter writer)
    {
        var parseNodeParameterName = RubyConventionService.GetParameterName(parseNodeParameter);
        var customProperties = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)
                                          .OrderBy(static x => x, new CodePropertyTypeComparer())
                                          .ThenBy(static x => x.Name, StringComparer.OrdinalIgnoreCase)
                                          .ToArray();
        if (customProperties.Length == 0)
        {
            writer.WriteLine($"{parentClass.Name.ToFirstCharacterUpperCase()}.new");
            return;
        }
        var complexProperties = customProperties.Where(static x => x.Type is CodeType propType && propType.TypeDefinition is CodeClass && propType.CollectionKind == CodeTypeBase.CodeTypeCollectionKind.None).ToArray();
        var nonComplexProperties = customProperties.Except(complexProperties).ToArray();
        var discriminatorPropertyName = parentClass.DiscriminatorInformation.DiscriminatorPropertyName;
        var complexPropertiesWithMappings = string.IsNullOrEmpty(discriminatorPropertyName) ? [] : complexProperties
            .Select(p => (property: p, mappedKey: parentClass.DiscriminatorInformation.DiscriminatorMappings
                .FirstOrDefault(x => x.Value.Name.Equals(p.Type.Name, StringComparison.OrdinalIgnoreCase)).Key))
            .Where(static x => !string.IsNullOrEmpty(x.mappedKey))
            .ToArray();
        writer.WriteLine($"result = {parentClass.Name.ToFirstCharacterUpperCase()}.new");
        // safe navigation: the payload may not carry the discriminator, or carry it as null
        if (complexPropertiesWithMappings.Length > 0)
            writer.WriteLine($"{DiscriminatorMappingVarName} = {parseNodeParameterName}.get_child_node(\"{RubyConventionService.SanitizeRubyDoubleQuoteLiteral(discriminatorPropertyName)}\")&.get_string_value");
        foreach (var property in nonComplexProperties)
            writer.WriteLine($"{GetIntersectionValueVarName(property)} = {parseNodeParameterName}.{GetDeserializationMethodName(property.Type)}");
        string NewMember(CodeProperty property) => $"result.{property.Name.ToSnakeCase()} = {conventions.GetQualifiedTypeName(property.Type)}.new";
        var branches = complexPropertiesWithMappings
            .Select(x => (condition: $"{DiscriminatorMappingVarName}&.downcase == \"{RubyConventionService.SanitizeRubyDoubleQuoteLiteral(x.mappedKey)}\".downcase", body: NewMember(x.property)))
            .Concat(nonComplexProperties.Select(x => (condition: $"!{GetIntersectionValueVarName(x)}.nil?", body: $"result.{x.Name.ToSnakeCase()} = {GetIntersectionValueVarName(x)}")))
            .ToArray();
        // when nothing picked a member, every object member reads the payload, as TypeScript does
        var fallback = complexProperties.Select(NewMember).ToArray();
        if (branches.Length == 0)
            writer.WriteLines(fallback);
        else if (branches.Length == 1 && fallback.Length == 0)
            writer.WriteLine(branches[0].condition.StartsWith('!') ?
                $"{branches[0].body} unless {branches[0].condition[1..].Replace(".nil?", string.Empty, StringComparison.Ordinal)}.nil?" :
                $"{branches[0].body} if {branches[0].condition}");
        else
        {
            var elseIfPrefix = string.Empty;
            foreach (var (condition, body) in branches)
            {
                writer.StartBlock($"{elseIfPrefix}if {condition}");
                writer.WriteLine(body);
                writer.DecreaseIndent();
                elseIfPrefix = "els";
            }
            if (fallback.Length > 0)
            {
                writer.StartBlock("else");
                writer.WriteLines(fallback);
                writer.DecreaseIndent();
            }
            // the branches already restored the indent, so the chain's `end` must not decrease it again
            writer.CloseBlock("end", false);
        }
        writer.WriteLine("result");
    }
    private static string GetIntersectionValueVarName(CodeProperty property) => $"val_{property.Name.ToSnakeCase()}";
    private void WriteComposedTypeGuardedSerialization(CodeProperty property, LanguageWriter writer)
    {
        var propertyName = property.Name.ToSnakeCase();
        writer.WriteLine($"return if @{propertyName}.nil?");
        writer.WriteLine();
        writer.WriteLine($"writer.{GetSerializationMethodName(property.Type)}(nil, @{propertyName})");
    }
    private void WriteFactoryMethodBodyForIntersectionModel(CodeParameter parseNodeParameter, CodeClass parentClass, LanguageWriter writer)
    {
        var parseNodeParameterName = RubyConventionService.GetParameterName(parseNodeParameter);
        var customProperties = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)
                                          .OrderBy(static x => x, new CodePropertyTypeComparer(orderByDesc: true))
                                          .ThenBy(static x => x.Name, StringComparer.OrdinalIgnoreCase)
                                          .ToArray();
        var nonComplexProperties = customProperties.Where(static x => x.Type is not CodeType propType || propType.TypeDefinition is not CodeClass || propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None).ToArray();
        var complexProperties = customProperties.Where(static x => x.Type is CodeType propType && propType.TypeDefinition is CodeClass && propType.CollectionKind == CodeTypeBase.CodeTypeCollectionKind.None).ToArray();
        if (customProperties.Length == 0)
        {
            writer.WriteLine($"{parentClass.Name.ToFirstCharacterUpperCase()}.new");
            return;
        }
        writer.WriteLine($"result = {parentClass.Name.ToFirstCharacterUpperCase()}.new");
        // each property needs its own variable: a shared one would be reassigned inside the
        // previous branch of the if/elsif chain, so only the first property would ever be read
        foreach (var property in nonComplexProperties)
        {
            var methodName = GetDeserializationMethodName(property.Type);
            writer.WriteLine($"{GetIntersectionValueVarName(property)} = {parseNodeParameterName}.{methodName}");
        }
        // Ruby has no `elsunless`, so a chain has to open with `if !x.nil?`; a lone branch with no
        // else reads as `unless x.nil?` instead, which is also what RuboCop's Style/NegatedIf wants
        var factoryBranchesChain = nonComplexProperties.Length > 1 || complexProperties.Length > 0;
        var elseIfPrefix = string.Empty;
        foreach (var property in nonComplexProperties)
        {
            if (!factoryBranchesChain)
            {
                writer.WriteLine($"result.{property.Name.ToSnakeCase()} = {GetIntersectionValueVarName(property)} unless {GetIntersectionValueVarName(property)}.nil?");
                continue;
            }
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
                writer.WriteLine($"result.{property.Name.ToSnakeCase()} = {conventions.GetQualifiedTypeName(property.Type)}.new");
            }
            writer.DecreaseIndent();
        }
        else if (complexProperties.Length > 0)
        {
            foreach (var property in complexProperties)
            {
                writer.WriteLine($"result.{property.Name.ToSnakeCase()} = {conventions.GetQualifiedTypeName(property.Type)}.new");
            }
        }
        if (factoryBranchesChain && nonComplexProperties.Length > 0)
            writer.CloseBlock("end", false);
        writer.WriteLine("result");
    }
    private static bool AddNullChecks(CodeMethod codeElement, LanguageWriter writer)
    {
        if (codeElement.IsOverload) return false;
        var parameters = codeElement.Parameters
                                    .Where(static x => !x.Optional && !x.IsOfKind(CodeParameterKind.PathParameters, CodeParameterKind.RequestAdapter))
                                    .Select(static x => x.Name.ToSnakeCase())
                                    .OrderBy(static x => x)
                                    .ToArray();
        foreach (var parameter in parameters)
            writer.WriteLine($"raise StandardError, {RubyConventionService.ToRubyStringLiteral($"{parameter} cannot be null")} if {parameter}.nil?");
        return parameters.Length != 0;
    }
    private static void WriteQueryParametersMapper(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer)
    {
        var parameter = codeElement.Parameters.FirstOrDefault(static x => x.IsOfKind(CodeParameterKind.QueryParametersMapperParameter));
        if (parameter == null) throw new InvalidOperationException("QueryParametersMapper should have a parameter of type QueryParametersMapper");
        var parameterName = RubyConventionService.GetParameterName(parameter);
        writer.StartBlock($"case {parameterName}", false);
        var escapedProperties = parentClass.Properties.Where(static x => x.IsOfKind(CodePropertyKind.QueryParameter) && x.IsNameEscaped);
        foreach (var escapedProperty in escapedProperties)
        {
            writer.StartBlock($"when {RubyConventionService.ToRubyStringLiteral(escapedProperty.Name)}");
            writer.WriteLine($"\"{RubyConventionService.SanitizeRubyDoubleQuoteLiteral(escapedProperty.SerializationName)}\"");
            writer.DecreaseIndent();
        }
        writer.StartBlock("else");
        writer.WriteLine(parameterName);
        writer.DecreaseIndent();
        writer.CloseBlock("end", false);
    }
    private void WriteRequestBuilderBody(CodeClass parentClass, CodeMethod codeElement, LanguageWriter writer)
    {
        var importSymbol = conventions.GetQualifiedTypeName(codeElement.ReturnType);
        conventions.AddRequestBuilderBody(parentClass, importSymbol, writer, pathParameters: codeElement.Parameters.Where(static x => x.IsOfKind(CodeParameterKind.Path)));
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
            writer.WriteLine($"@{requestAdapterPropertyName}.set_base_url({RubyConventionService.ToRubyStringLiteral(method.BaseUrl)}) if @{requestAdapterPropertyName}.get_base_url.nil? || @{requestAdapterPropertyName}.get_base_url.empty?");
            if (pathParametersProperty != null)
                writer.WriteLine($"@{pathParametersProperty.Name.ToSnakeCase()}[\"baseurl\"] = @{requestAdapterPropertyName}.get_base_url");
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
                    writer.WriteLine($"super({RubyConventionService.GetParameterName(pathParametersParameter)}, {RubyConventionService.GetParameterName(requestAdapterParameter)}, {sanitizedUrlTemplate})");
                else
                    writer.WriteLine($"super({{}}, {RubyConventionService.GetParameterName(requestAdapterParameter)}, {sanitizedUrlTemplate})");
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
                defaultValue = $"{conventions.GetQualifiedTypeName(propWithDefault.Type)}[:{optionName}]";
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
        var parameterName = codeElement.Parameters.FirstOrDefault(static x => x.IsOfKind(CodeParameterKind.SetterValue)) is CodeParameter setterValue ? RubyConventionService.GetParameterName(setterValue) : null;
        if (codeElement.AccessedProperty is not null)
            writer.WriteLine($"@{codeElement.AccessedProperty.NamePrefix}{codeElement.AccessedProperty.Name.ToSnakeCase()} = {parameterName}");
    }
    private static void WriteGetterBody(CodeMethod codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        if (codeElement.AccessedProperty is not null)
            writer.WriteLine($"@{codeElement.AccessedProperty.NamePrefix}{codeElement.AccessedProperty.Name.ToSnakeCase()}");
    }
    private void WriteIndexerBody(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer)
    {
        if (parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters) is CodeProperty pathParametersProperty &&
            codeElement.OriginalIndexer != null)
            writer.WriteLines($"{conventions.TempDictionaryVarName} = @{pathParametersProperty.NamePrefix}{pathParametersProperty.Name.ToSnakeCase()}.clone",
                            $"{conventions.TempDictionaryVarName}[\"{RubyConventionService.SanitizeRubyDoubleQuoteLiteral(codeElement.OriginalIndexer.IndexParameter.SerializationName)}\"] = {RubyConventionService.GetParameterName(codeElement.OriginalIndexer.IndexParameter)}");
        conventions.AddRequestBuilderBody(parentClass, conventions.GetQualifiedTypeName(codeElement.ReturnType), writer, conventions.TempDictionaryVarName);
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
        var inherits = parentClass.StartBlock.Inherits != null;
        var entries = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)
                                .Where(static x => !x.ExistsInBaseType)
                                .OrderBy(static x => x.Name)
                                .Select(x => $"\"{RubyConventionService.SanitizeRubyDoubleQuoteLiteral(x.WireName)}\" => ->(n) {{ @{x.NamePrefix}{x.Name.ToSnakeCase()} = n.{GetDeserializationMethodName(x.Type)} }}")
                                .ToArray();
        if (entries.Length == 0)
        {
            writer.WriteLine(inherits ? "super" : "{}");
            return;
        }
        writer.WriteLine(inherits ? "super.merge(" : "{");
        writer.IncreaseIndent();
        for (var i = 0; i < entries.Length; i++)
            writer.WriteLine(i < entries.Length - 1 ? $"{entries[i]}," : entries[i]);
        writer.DecreaseIndent();
        writer.WriteLine(inherits ? ")" : "}");
    }
    // a union may hold several members when no discriminator picked one, so merge them like an intersection
    private static void WriteDeserializerBodyForUnionModel(CodeClass parentClass, LanguageWriter writer) =>
        WriteDeserializerBodyForIntersectionModel(parentClass, writer);
    private static void WriteDeserializerBodyForIntersectionModel(CodeClass parentClass, LanguageWriter writer)
    {
        var complexProperties = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)
                                           .Where(static x => x.Type is CodeType propType && propType.TypeDefinition is CodeClass && propType.CollectionKind == CodeTypeBase.CodeTypeCollectionKind.None)
                                           .OrderBy(static x => x.Name, StringComparer.OrdinalIgnoreCase)
                                           .ToArray();
        if (complexProperties.Length > 0)
        {
            var condition = string.Join(" || ", complexProperties.Select(x => $"@{x.Name.ToSnakeCase()}"));
            var propNames = string.Join(", ", complexProperties.Select(x => $"@{x.Name.ToSnakeCase()}"));
            writer.WriteLine($"return MicrosoftKiotaAbstractions::ParseNodeHelper.merge_deserializers_for_intersection_wrapper({propNames}) if {condition}");
            writer.WriteLine();
        }
        writer.WriteLine("{}");
    }
    private void WriteRequestExecutorBody(CodeMethod codeElement, RequestParams requestParams, CodeClass parentClass, string returnType, LanguageWriter writer)
    {
        if (codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");


        var generatorMethodName = parentClass
                                            .Methods
                                            .FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestGenerator) && x.HttpMethod == codeElement.HttpMethod)
                                            ?.Name
                                            ?.ToSnakeCase();
        var requestInfoParameters = new CodeParameter?[] { requestParams.requestBody, requestParams.requestContentType, requestParams.requestConfiguration }
            .OfType<CodeParameter>()
            .Select(RubyConventionService.GetParameterName)
            .ToArray();
        writer.WriteLine(requestInfoParameters.Length == 0 ?
            $"request_info = {generatorMethodName}" :
            $"request_info = {generatorMethodName}({string.Join(", ", requestInfoParameters)})");
        var (sendMethodName, responseArgument) = GetSendRequest(codeElement.ReturnType, returnType);
        var errorMappingVarName = "nil";
        if (codeElement.ErrorMappings.Any())
        {
            errorMappingVarName = "error_mapping";
            writer.WriteLine($"{errorMappingVarName} = {{}}");
            foreach (var errorMapping in codeElement.ErrorMappings)
            {
                writer.WriteLine($"{errorMappingVarName}[\"{errorMapping.Key.ToUpperInvariant()}\"] = {getDeserializationLambda(errorMapping.Value)}");
            }
        }
        writer.WriteLine($"@request_adapter.{sendMethodName}(request_info, {responseArgument}{errorMappingVarName})");
    }

    private void WriteRequestGeneratorBody(CodeMethod codeElement, RequestParams requestParams, CodeClass parentClass, LanguageWriter writer)
    {
        if (codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");

        writer.WriteLine("request_info = MicrosoftKiotaAbstractions::RequestInformation.new");
        if (requestParams.requestConfiguration != null)
        {
            var queryString = requestParams.QueryParameters;
            var headers = requestParams.Headers;
            var options = requestParams.Options;
            if (headers != null || queryString != null)
            {
                var configurationName = RubyConventionService.GetParameterName(requestParams.requestConfiguration);
                var statements = new[] {
                    headers is null ? null : $"request_info.add_headers_from_raw_object({configurationName}.{headers.Name.ToSnakeCase()})",
                    queryString is null ? null : $"request_info.set_query_string_parameters_from_raw_object({configurationName}.{queryString.Name.ToSnakeCase()})",
                    options is null ? null : $"request_info.add_request_options({configurationName}.{options.Name.ToSnakeCase()})",
                }.OfType<string>().ToArray();
                if (statements.Length == 1)
                    writer.WriteLine($"{statements[0]} unless {configurationName}.nil?");
                else
                {
                    writer.StartBlock($"unless {configurationName}.nil?");
                    writer.WriteLines(statements);
                    writer.CloseBlock("end");
                }
            }
        }
        if (requestParams.requestBody != null)
        {
            var requestBodyContentType = RubyConventionService.ToRubyStringLiteral(codeElement.RequestBodyContentType);
            // only a whole body is a stream; the refiner keeps the collection kind, and a list of
            // binary values is a payload of JSON strings rather than something to stream
            if (requestParams.requestBody.Type is CodeType { TypeDefinition: null, CollectionKind: CodeTypeBase.CodeTypeCollectionKind.None } &&
                requestParams.requestBody.Type.Name.Equals(conventions.StreamTypeName, StringComparison.OrdinalIgnoreCase))
            {
                if (requestParams.requestContentType is not null)
                    writer.WriteLine($"request_info.set_stream_content({RubyConventionService.GetParameterName(requestParams.requestBody)}, {RubyConventionService.GetParameterName(requestParams.requestContentType)})");
                else if (!string.IsNullOrEmpty(codeElement.RequestBodyContentType))
                    writer.WriteLine($"request_info.set_stream_content({RubyConventionService.GetParameterName(requestParams.requestBody)}, {requestBodyContentType})");
            }
            else if (parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter) is CodeProperty requestAdapterProperty)
            {
                var setMethodName = requestParams.requestBody.Type is CodeType bodyType &&
                    (bodyType.TypeDefinition is CodeClass || bodyType.Name.Equals("MultipartBody", StringComparison.OrdinalIgnoreCase)) ?
                    "set_content_from_parsable" :
                    "set_content_from_scalar";
                writer.WriteLine($"request_info.{setMethodName}(@{requestAdapterProperty.Name.ToSnakeCase()}, {requestBodyContentType}, {RubyConventionService.GetParameterName(requestParams.requestBody)})");
            }
        }
        if (parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters) is CodeProperty urlTemplateParamsProperty &&
            parentClass.GetPropertyOfKind(CodePropertyKind.UrlTemplate) is CodeProperty urlTemplateProperty)
        {
            var urlTemplateValue = codeElement.HasUrlTemplateOverride ? RubyConventionService.ToRubyStringLiteral(codeElement.UrlTemplateOverride) : GetPropertyCall(urlTemplateProperty, "\"\"");
            writer.WriteLines($"request_info.url_template = {urlTemplateValue}",
                            $"request_info.path_parameters = {GetPropertyCall(urlTemplateParamsProperty, "\"\"")}");
        }
        writer.WriteLine($"request_info.http_method = :{codeElement.HttpMethod.Value.ToString().ToUpperInvariant()}");
        if (codeElement.ShouldAddAcceptHeader)
            writer.WriteLine($"request_info.headers.try_add(\"Accept\", {RubyConventionService.ToRubyStringLiteral(codeElement.AcceptHeaderValue)})");
        writer.WriteLine("request_info");
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
    private void WriteSerializerBodyForUnionModel(CodeClass parentClass, LanguageWriter writer) =>
        WriteSerializerBodyForIntersectionModel(parentClass, writer);
    private void WriteSerializerBodyForIntersectionModel(CodeClass parentClass, LanguageWriter writer)
    {
        var customProperties = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)
                                          .OrderBy(static x => x, new CodePropertyTypeComparer(orderByDesc: true))
                                          .ThenBy(static x => x.Name, StringComparer.OrdinalIgnoreCase)
                                          .ToArray();
        var nonComplexProperties = customProperties.Where(static x => x.Type is not CodeType propType || propType.TypeDefinition is not CodeClass || propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None).ToArray();
        var complexProperties = customProperties.Where(static x => x.Type is CodeType propType && propType.TypeDefinition is CodeClass && propType.CollectionKind == CodeTypeBase.CodeTypeCollectionKind.None).ToArray();
        if (nonComplexProperties.Length == 1 && complexProperties.Length == 0)
        {
            WriteComposedTypeGuardedSerialization(nonComplexProperties[0], writer);
            return;
        }
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
                                                .Select(p => conventions.GetParameterSignature(p, code))
                                                .ToList());
        var staticPrefix = code.IsStatic ? "self." : string.Empty;
        var openParenthesis = parameters.Length == 0 ? string.Empty : "(";
        var closeParenthesis = parameters.Length == 0 ? string.Empty : ")";
        var equalsSign = code.IsOfKind(CodeMethodKind.Setter) ? "=" : string.Empty;
        writer.StartBlock($"def {staticPrefix}{methodName.ToSnakeCase()}{equalsSign}{openParenthesis}{parameters}{closeParenthesis}");
    }
    private void WriteMethodDocumentation(CodeMethod code, LanguageWriter writer)
    {
        var parametersWithDescription = code.Parameters.Where(static x => x.Documentation.DescriptionAvailable).OrderBy(static x => x.Name, StringComparer.OrdinalIgnoreCase).ToArray();
        if (code.Documentation.DescriptionAvailable || parametersWithDescription.Length != 0)
        {
            if (code.Documentation.DescriptionAvailable)
            {
                var description = code.Documentation.GetDescription(type => conventions.GetTypeString(type, code), normalizationFunc: RubyConventionService.RemoveInvalidDescriptionCharacters);
                if (!string.IsNullOrWhiteSpace(description))
                    writer.WriteLine($"{conventions.DocCommentPrefix}{description}");
            }
            foreach (var paramWithDescription in parametersWithDescription)
            {
                var description = paramWithDescription.Documentation.GetDescription(type => conventions.GetTypeString(type, code), normalizationFunc: RubyConventionService.RemoveInvalidDescriptionCharacters);
                writer.WriteLine($"{conventions.DocCommentPrefix}@param {paramWithDescription.Name.ToSnakeCase()} {description}".TrimEnd());
            }

            if (code.IsAsync)
                writer.WriteLine($"{conventions.DocCommentPrefix}@return a Fiber of {code.ReturnType.Name.ToSnakeCase()}");
            else
                writer.WriteLine($"{conventions.DocCommentPrefix}@return a {code.ReturnType.Name.ToSnakeCase()}");
        }
    }
    private string GetDeserializationMethodName(CodeTypeBase propType)
    {
        var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
        var propertyType = conventions.TranslateType(propType);
        if (propType is CodeType currentType)
        {
            if (currentType.TypeDefinition is CodeEnum currentEnum)
            {
                var enumName = conventions.GetQualifiedTypeName(currentType);
                if (isCollection)
                    return $"get_collection_of_enum_values({enumName})";
                return $"get_enum_value{(currentEnum.Flags ? "s" : string.Empty)}({enumName})";
            }
            // a resolved model is read by its factory whatever it is called, so the primitive map
            // below only decides for types the builder never resolved
            if (currentType.TypeDefinition is not null)
                return isCollection ?
                    $"get_collection_of_object_values({getDeserializationLambda(currentType)})" :
                    $"get_object_value({getDeserializationLambda(currentType)})";
            if (isCollection)
                return $"get_collection_of_primitive_values({RubyConventionService.GetPrimitiveConstant(propertyType)})";
        }
        return RubyConventionService.TryGetPrimitiveType(propertyType, out var primitive) ?
            primitive.Reader :
            $"get_object_value({getDeserializationLambda(propType)})";
    }
    private string getDeserializationLambda(CodeTypeBase targetTypeBase)
    {
        if (targetTypeBase is not CodeType targetType)
            return "->(_pn) { nil }";
        return $"->(pn) {{ {conventions.GetQualifiedTypeName(targetType)}.create_from_discriminator_value(pn) }}";
    }
    private string GetSerializationMethodName(CodeTypeBase propType)
    {
        var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
        var propertyType = conventions.TranslateType(propType);
        if (propType is CodeType currentType)
        {
            if (currentType.TypeDefinition is CodeEnum)
                return isCollection ? "write_collection_of_enum_values" : "write_enum_value";
            if (currentType.TypeDefinition is not null)
                return isCollection ? "write_collection_of_object_values" : "write_object_value";
            if (isCollection)
                return "write_collection_of_primitive_values";
        }
        return RubyConventionService.TryGetPrimitiveType(propertyType, out var primitive) ?
            primitive.Writer :
            "write_object_value";
    }
    /// <summary>
    /// Each response shape is read by a different request adapter method, and each of those takes a
    /// different second argument: a type for a primitive, an enum constant, a factory for a model,
    /// and nothing at all when there is no content.
    /// </summary>
    private (string methodName, string argument) GetSendRequest(CodeTypeBase returnTypeBase, string returnType)
    {
        var isResolved = returnTypeBase is CodeType { TypeDefinition: not null };
        if (!isResolved &&
            (returnType.Equals(conventions.VoidTypeName, StringComparison.OrdinalIgnoreCase) ||
             returnType.Equals("void", StringComparison.OrdinalIgnoreCase)))
            return ("send_no_response_content_async", string.Empty);

        var isCollection = returnTypeBase.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
        // an enum, a stream and a scalar are all read by type rather than by factory, so they only
        // differ in the type they name
        var byType = isCollection ? "send_collection_of_primitive_async" : "send_primitive_async";
        if (returnTypeBase is CodeType { TypeDefinition: CodeEnum } enumType)
            return (byType, $"{conventions.GetQualifiedTypeName(enumType)}, ");
        // a resolved model is read by its factory whatever it is called, so the name checks below
        // only decide for types the builder never resolved
        if (isResolved || returnTypeBase is not CodeType)
            return (isCollection ? "send_collection_async" : "send_async",
                    $"{getDeserializationLambda(returnTypeBase)}, ");
        if (conventions.StreamTypeName.Equals(returnType, StringComparison.OrdinalIgnoreCase))
            // only a whole body is a stream; a binary value inside a payload arrives as a JSON
            // string, and the runtime has no collection reader for a stream
            return (byType, isCollection ?
                $"{RubyConventionService.GetPrimitiveConstant("string")}, " :
                $"{conventions.StreamTypeName}, ");
        if (RubyConventionService.IsPrimitiveType(returnType))
            return (byType, $"{RubyConventionService.GetPrimitiveConstant(returnType)}, ");
        return (isCollection ? "send_collection_async" : "send_async",
                $"{getDeserializationLambda(returnTypeBase)}, ");
    }
}
