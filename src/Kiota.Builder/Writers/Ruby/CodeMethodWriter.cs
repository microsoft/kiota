﻿using System;
using System.Collections.Generic;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

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
        WriteMethodDocumentation(codeElement, writer);
        var inherits = parentClass.StartBlock.Inherits != null;
        var requestBodyParam = codeElement.Parameters.OfKind(CodeParameterKind.RequestBody);
        var config = codeElement.Parameters.OfKind(CodeParameterKind.RequestConfiguration);
        var requestParams = new RequestParams(requestBodyParam, config);
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
            default:
                writer.WriteLine("return nil;");
                break;
        }
        writer.CloseBlock("end");
    }
    private const string DiscriminatorMappingVarName = "mapping_value";
    private const string NodeVarName = "mapping_value_node";
    private static void WriteFactoryMethodBody(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer)
    {
        var parseNodeParameter = codeElement.Parameters.OfKind(CodeParameterKind.ParseNode) ?? throw new InvalidOperationException("Factory method should have a ParseNode parameter");
        var writeDiscriminatorValueRead = parentClass.DiscriminatorInformation.ShouldWriteParseNodeCheck && !parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForIntersectionType;
        if (writeDiscriminatorValueRead)
        {
            writer.WriteLine($"{NodeVarName} = {parseNodeParameter.Name.ToSnakeCase()}.get_child_node(\"{parentClass.DiscriminatorInformation.DiscriminatorPropertyName}\")");
            writer.StartBlock($"unless {NodeVarName}.nil? then");
            writer.WriteLine($"{DiscriminatorMappingVarName} = {NodeVarName}.get_string_value");
            writer.StartBlock($"case {DiscriminatorMappingVarName}");
            foreach (var mappedType in parentClass.DiscriminatorInformation.DiscriminatorMappings.OrderBy(static x => x.Key))
            {
                writer.StartBlock($"when \"{mappedType.Key}\"");
                writer.WriteLine($"return {mappedType.Value.AllTypes.First().Name.ToFirstCharacterUpperCase()}.new");
                writer.DecreaseIndent();
            }
            writer.CloseBlock("end");
            writer.CloseBlock("end");
        }
        writer.WriteLine($"return {parentClass.Name.ToFirstCharacterUpperCase()}.new");
    }
    private static void AddNullChecks(CodeMethod codeElement, LanguageWriter writer)
    {
        if (!codeElement.IsOverload)
            foreach (var parameter in codeElement.Parameters
                                                .Where(static x => !x.Optional)
                                                .Select(static x => x.Name.ToSnakeCase())
                                                .OrderBy(static x => x))
                writer.WriteLine($"raise StandardError, '{parameter} cannot be null' if {parameter}.nil?");
    }
    private static void WriteQueryParametersMapper(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer)
    {
        var parameter = codeElement.Parameters.FirstOrDefault(static x => x.IsOfKind(CodeParameterKind.QueryParametersMapperParameter));
        if (parameter == null) throw new InvalidOperationException("QueryParametersMapper should have a parameter of type QueryParametersMapper");
        var parameterName = parameter.Name.ToSnakeCase();
        writer.StartBlock($"case {parameterName}");
        var escapedProperties = parentClass.Properties.Where(static x => x.IsOfKind(CodePropertyKind.QueryParameter) && x.IsNameEscaped);
        foreach (var escapedProperty in escapedProperties)
        {
            writer.StartBlock($"when \"{escapedProperty.Name}\"");
            writer.WriteLine($"return \"{escapedProperty.SerializationName}\"");
            writer.DecreaseIndent();
        }
        writer.StartBlock("else");
        writer.WriteLine($"return {parameterName}");
        writer.DecreaseIndent();
        writer.CloseBlock("end");
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
        if (method.Parameters.FirstOrDefault(static x => x.IsOfKind(CodeParameterKind.RequestAdapter)) is CodeParameter requestAdapterParameter)
            writer.WriteLine($"@{requestAdapterPropertyName} = {requestAdapterParameter.Name.ToSnakeCase()}");
        if (!string.IsNullOrEmpty(method.BaseUrl))
        {
            writer.StartBlock($"if @{requestAdapterPropertyName}.get_base_url.nil? || @{requestAdapterPropertyName}.get_base_url.empty?");
            writer.WriteLine($"@{requestAdapterPropertyName}.set_base_url('{method.BaseUrl}')");
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
                    prefix = $"{prefix.ToCamelCase(new char[] { '_' }).ToFirstCharacterUpperCase()}::";
                writer.WriteLine($"MicrosoftKiotaAbstractions::ApiClientBuilder.{methodName}({prefix}{serializationClassName})");
            }
    }
    private static void WriteConstructorBody(CodeClass parentClass, CodeMethod currentMethod, LanguageWriter writer, bool inherits)
    {
        if (inherits)
            writer.WriteLine("super");
        foreach (var propWithDefault in parentClass.GetPropertiesOfKind(CodePropertyKind.BackingStore,
                                                                        CodePropertyKind.RequestBuilder,
                                                                        CodePropertyKind.UrlTemplate,
                                                                        CodePropertyKind.PathParameters)
                                        .Where(static x => !string.IsNullOrEmpty(x.DefaultValue))
                                        .OrderBy(static x => x.Name))
        {
            writer.WriteLine($"@{propWithDefault.NamePrefix}{propWithDefault.Name.ToSnakeCase()} = {propWithDefault.DefaultValue}");
        }
        foreach (var propWithDefault in parentClass.GetPropertiesOfKind(CodePropertyKind.AdditionalData,
                                                                        CodePropertyKind.Custom) //additional data and custom properties rely on accessors
                                        .Where(static x => !string.IsNullOrEmpty(x.DefaultValue))
                                        // do not apply the default value if the type is composed as the default value may not necessarily which type to use
                                        .Where(static x => x.Type is not CodeType propType || propType.TypeDefinition is not CodeClass propertyClass || propertyClass.OriginalComposedType is null)
                                        .OrderBy(static x => x.Name))
        {
            writer.WriteLine($"@{propWithDefault.NamePrefix}{propWithDefault.Name.ToSnakeCase()} = {propWithDefault.DefaultValue}");
        }
        if (currentMethod.IsOfKind(CodeMethodKind.Constructor))
        {
            AssignPropertyFromParameter(parentClass, currentMethod, CodeParameterKind.RequestAdapter, CodePropertyKind.RequestAdapter, writer);
            var pathParametersParamName = currentMethod.Parameters.OfKind(CodeParameterKind.PathParameters)?.Name.ToSnakeCase();
            if (parentClass.IsOfKind(CodeClassKind.RequestBuilder))
                writer.WriteLine($"{pathParametersParamName} = {{ \"request-raw-url\" => {pathParametersParamName} }} if {pathParametersParamName}.is_a? String");
            AssignPropertyFromParameter(parentClass, currentMethod, CodeParameterKind.PathParameters, CodePropertyKind.PathParameters, writer, $" if {pathParametersParamName}.is_a? Hash");
        }
    }
    private static void AssignPropertyFromParameter(CodeClass parentClass, CodeMethod currentMethod, CodeParameterKind parameterKind, CodePropertyKind propertyKind, LanguageWriter writer, string? controlSuffix = default)
    {
        if (parentClass.GetPropertyOfKind(propertyKind) is CodeProperty property &&
             currentMethod.Parameters.FirstOrDefault(x => x.IsOfKind(parameterKind)) is CodeParameter parameter)
        {
            writer.WriteLine($"@{property.NamePrefix}{property.Name.ToSnakeCase()} = {parameter.Name.ToSnakeCase()}{controlSuffix}");
        }
    }
    private static void WriteSetterBody(CodeMethod codeElement, LanguageWriter writer)
    {
        var parameterName = codeElement.Parameters.FirstOrDefault(static x => x.IsOfKind(CodeParameterKind.SetterValue))?.Name.ToSnakeCase();
        if (codeElement.AccessedProperty is not null)
            writer.WriteLine($"@{codeElement.AccessedProperty.NamePrefix}{codeElement.AccessedProperty?.Name?.ToSnakeCase()} = {parameterName}");
    }
    private static void WriteGetterBody(CodeMethod codeElement, LanguageWriter writer)
    {
        if (codeElement.AccessedProperty is not null)
            writer.WriteLine($"return @{codeElement.AccessedProperty.NamePrefix}{codeElement.AccessedProperty?.Name?.ToSnakeCase()}");
    }
    private void WriteIndexerBody(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer, string returnType)
    {
        var prefix = conventions.GetNormalizedNamespacePrefixForType(codeElement.ReturnType);
        if (parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters) is CodeProperty pathParametersProperty &&
            codeElement.OriginalIndexer != null)
            writer.WriteLines($"{conventions.TempDictionaryVarName} = @{pathParametersProperty.NamePrefix}{pathParametersProperty.Name.ToSnakeCase()}.clone",
                            $"{conventions.TempDictionaryVarName}[\"{codeElement.OriginalIndexer.SerializationName}\"] = id");
        conventions.AddRequestBuilderBody(parentClass, returnType, writer, conventions.TempDictionaryVarName, $"return {prefix}");
    }
    private void WriteDeserializerBody(CodeClass parentClass, LanguageWriter writer)
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
            writer.WriteLine($"\"{otherProp.WireName}\" => lambda {{|n| @{otherProp.NamePrefix}{otherProp.Name.ToSnakeCase()} = n.{GetDeserializationMethodName(otherProp.Type)} }},");
        }
        writer.DecreaseIndent();
        if (parentClass.StartBlock.Inherits != null)
            writer.WriteLine("})");
        else
            writer.WriteLine("}");
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
        var requestInfoParameters = new[] { requestParams.requestBody, requestParams.requestConfiguration }
            .Where(static x => x != null)
            .Select(static x => x!.Name.ToSnakeCase());
        if (requestInfoParameters.Any())
        {
            writer.IncreaseIndent();
            writer.WriteLine(requestInfoParameters.Aggregate(static (x, y) => $"{x}, {y}"));
            writer.DecreaseIndent();
        }
        writer.WriteLine(")");
        var isStream = conventions.StreamTypeName.Equals(StringComparison.OrdinalIgnoreCase);
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
        if (parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters) is CodeProperty urlTemplateParamsProperty &&
            parentClass.GetPropertyOfKind(CodePropertyKind.UrlTemplate) is CodeProperty urlTemplateProperty)
            writer.WriteLines($"request_info.url_template = {GetPropertyCall(urlTemplateProperty, "''")}",
                                $"request_info.path_parameters = {GetPropertyCall(urlTemplateParamsProperty, "''")}");
        writer.WriteLine($"request_info.http_method = :{codeElement.HttpMethod?.ToString().ToUpperInvariant()}");
        if (codeElement.AcceptedResponseTypes.Any())
            writer.WriteLine($"request_info.headers.add('Accept', '{string.Join(", ", codeElement.AcceptedResponseTypes)}')");
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
                if (requestParams.requestBody.Type.Name.Equals(conventions.StreamTypeName, StringComparison.OrdinalIgnoreCase))
                    writer.WriteLine($"request_info.set_stream_content({requestParams.requestBody.Name})");
                else if (parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter) is CodeProperty requestAdapterProperty)
                    writer.WriteLine($"request_info.set_content_from_parsable(self.{requestAdapterProperty.Name.ToSnakeCase()}, \"{codeElement.RequestBodyContentType}\", {requestParams.requestBody.Name})");
            }
        }
        writer.WriteLine("return request_info");
    }
    private static string GetPropertyCall(CodeProperty property, string defaultValue) => property == null ? defaultValue : $"@{property.NamePrefix}{property.Name.ToSnakeCase()}";
    private void WriteSerializerBody(CodeClass parentClass, LanguageWriter writer)
    {
        var additionalDataProperty = parentClass.GetPropertyOfKind(CodePropertyKind.AdditionalData);
        if (parentClass.StartBlock.Inherits != null)
            writer.WriteLine("super");
        foreach (var otherProp in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)
                                            .Where(static x => !x.ExistsInBaseType && !x.ReadOnly)
                                            .OrderBy(static x => x.Name))
        {
            writer.WriteLine($"writer.{GetSerializationMethodName(otherProp.Type)}(\"{otherProp.WireName}\", @{otherProp.Name.ToSnakeCase()})");
        }
        if (additionalDataProperty != null)
            writer.WriteLine($"writer.write_additional_data(@{additionalDataProperty.NamePrefix}{additionalDataProperty.Name.ToSnakeCase()})");
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
        var isDescriptionPresent = !string.IsNullOrEmpty(code.Documentation.Description);
        var parametersWithDescription = code.Parameters.Where(x => !string.IsNullOrEmpty(code.Documentation.Description));
        if (isDescriptionPresent || parametersWithDescription.Any())
        {
            writer.WriteLine(conventions.DocCommentStart);
            if (isDescriptionPresent)
                writer.WriteLine($"{conventions.DocCommentPrefix}{RubyConventionService.RemoveInvalidDescriptionCharacters(code.Documentation.Description)}");
            foreach (var paramWithDescription in parametersWithDescription.OrderBy(static x => x.Name))
                writer.WriteLine($"{conventions.DocCommentPrefix}@param {paramWithDescription.Name} {RubyConventionService.RemoveInvalidDescriptionCharacters(paramWithDescription.Documentation.Description)}");

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
            "binary" or "Binary" => "get_string_value()", //TODO: add support for binary
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
            _ => typeName.ToFirstCharacterUpperCase() ?? "Object",
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
            "binary" => "write_string_value", //TODO: add support for binary
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
