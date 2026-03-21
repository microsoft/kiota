using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Kiota.Builder.OrderComparers;
using static Kiota.Builder.CodeDOM.CodeTypeBase;

namespace Kiota.Builder.Writers.Rust;

public class CodeMethodWriter : BaseElementWriter<CodeMethod, RustConventionService>
{
    private readonly HashSet<string> classesWithImplBlockOpened = new(StringComparer.Ordinal);

    public CodeMethodWriter(RustConventionService conventionService) : base(conventionService) { }

    internal bool HasImplBlockBeenOpened(string className) => classesWithImplBlockOpened.Contains(className);

    public override void WriteCodeElement(CodeMethod codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        if (codeElement.ReturnType == null) throw new InvalidOperationException($"{nameof(codeElement.ReturnType)} should not be null");
        ArgumentNullException.ThrowIfNull(writer);
        if (codeElement.Parent is not CodeClass parentClass) throw new InvalidOperationException("the parent of a method should be a class");

        var structName = parentClass.Name.ToFirstCharacterUpperCase();

        // Close struct block and open impl block on first method
        if (classesWithImplBlockOpened.Add(structName))
        {
            writer.CloseBlock(); // close pub struct { ... }
            writer.WriteLine();
            writer.StartBlock($"impl {structName} {{");
        }

        var returnType = conventions.GetTypeString(codeElement.ReturnType, codeElement);
        var inherits = parentClass.StartBlock.Inherits != null && !parentClass.IsErrorDefinition;
        var isVoid = conventions.VoidTypeName.Equals(returnType, StringComparison.OrdinalIgnoreCase) || returnType == "()";
        WriteMethodDocumentation(codeElement, writer);
        WriteMethodPrototype(codeElement, parentClass, writer, returnType, inherits, isVoid);

        HandleMethodKind(codeElement, writer, inherits, parentClass, isVoid);
        writer.CloseBlock();
    }

    protected virtual void HandleMethodKind(CodeMethod codeElement, LanguageWriter writer, bool doesInherit, CodeClass parentClass, bool isVoid)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(parentClass);
        var returnType = conventions.GetTypeString(codeElement.ReturnType, codeElement);
        var returnTypeWithoutCollectionInformation = conventions.GetTypeString(codeElement.ReturnType, codeElement, false);
        var requestBodyParam = codeElement.Parameters.OfKind(CodeParameterKind.RequestBody);
        var requestConfig = codeElement.Parameters.OfKind(CodeParameterKind.RequestConfiguration);
        var requestContentType = codeElement.Parameters.OfKind(CodeParameterKind.RequestBodyContentType);
        var requestParams = new RequestParams(requestBodyParam, requestConfig, requestContentType);

        switch (codeElement.Kind)
        {
            case CodeMethodKind.Serializer:
                WriteSerializerBody(doesInherit, codeElement, parentClass, writer);
                break;
            case CodeMethodKind.RequestGenerator:
                WriteRequestGeneratorBody(codeElement, requestParams, parentClass, writer);
                break;
            case CodeMethodKind.RequestExecutor:
                WriteRequestExecutorBody(codeElement, requestParams, parentClass, isVoid, returnTypeWithoutCollectionInformation, writer);
                break;
            case CodeMethodKind.Deserializer:
                WriteDeserializerBody(doesInherit, codeElement, parentClass, writer);
                break;
            case CodeMethodKind.ClientConstructor:
                WriteConstructorBody(parentClass, codeElement, writer);
                WriteApiConstructorBody(parentClass, codeElement, writer);
                break;
            case CodeMethodKind.Constructor:
            case CodeMethodKind.RawUrlConstructor:
                WriteConstructorBody(parentClass, codeElement, writer);
                break;
            case CodeMethodKind.IndexerBackwardCompatibility:
            case CodeMethodKind.RequestBuilderWithParameters:
                WriteRequestBuilderBody(parentClass, codeElement, writer);
                break;
            case CodeMethodKind.QueryParametersMapper:
                WriteQueryParametersBody(parentClass, writer);
                break;
            case CodeMethodKind.Getter:
            case CodeMethodKind.Setter:
                throw new InvalidOperationException("getters and setters are represented as struct fields in Rust");
            case CodeMethodKind.RequestBuilderBackwardCompatibility:
                throw new InvalidOperationException("RequestBuilderBackwardCompatibility is not supported as the request builders are implemented by methods.");
            case CodeMethodKind.ErrorMessageOverride:
                throw new InvalidOperationException("ErrorMessageOverride is not supported as the error message is implemented by Display trait.");
            case CodeMethodKind.CommandBuilder:
                throw new InvalidOperationException("CommandBuilder methods are not implemented for Rust.");
            case CodeMethodKind.Factory:
                WriteFactoryMethodBody(codeElement, parentClass, writer);
                break;
            case CodeMethodKind.ComposedTypeMarker:
                throw new InvalidOperationException("ComposedTypeMarker is not required for Rust.");
            default:
                writer.WriteLine("todo!()");
                break;
        }
    }

    private void WriteFactoryMethodBody(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer)
    {
        var parseNodeParameter = codeElement.Parameters.OfKind(CodeParameterKind.ParseNode) ?? throw new InvalidOperationException("Factory method should have a ParseNode parameter");

        if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForInheritedType)
        {
            var discriminatorPropertyName = parentClass.DiscriminatorInformation.DiscriminatorPropertyName;
            writer.WriteLine($"let mapping_value = {parseNodeParameter.Name.ToSnakeCase()}.get_child_node(\"{discriminatorPropertyName}\").and_then(|n| n.get_string_value());");
            writer.StartBlock("match mapping_value.as_deref() {");
            foreach (var mappedType in parentClass.DiscriminatorInformation.DiscriminatorMappings)
            {
                writer.WriteLine($"Some(\"{mappedType.Key}\") => Box::new({conventions.GetTypeString(mappedType.Value.AllTypes.First(), codeElement)}::default()),");
            }
            writer.WriteLine($"_ => Box::new({parentClass.Name}::default()),");
            writer.CloseBlock();
        }
        else
        {
            writer.WriteLine($"Box::new({parentClass.Name}::default())");
        }
    }

    private void WriteRequestBuilderBody(CodeClass parentClass, CodeMethod codeElement, LanguageWriter writer)
    {
        var importSymbol = conventions.GetTypeString(codeElement.ReturnType, parentClass);
        conventions.AddRequestBuilderBody(parentClass, importSymbol, writer, prefix: "", pathParameters: codeElement.Parameters.Where(static x => x.IsOfKind(CodeParameterKind.Path)), customParameters: codeElement.Parameters.Where(static x => x.IsOfKind(CodeParameterKind.Custom)));
    }

    private static void WriteApiConstructorBody(CodeClass parentClass, CodeMethod method, LanguageWriter writer)
    {
        if (parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter) is not CodeProperty requestAdapterProperty) return;
        var pathParametersProperty = parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters);
        var backingStoreParameter = method.Parameters.OfKind(CodeParameterKind.BackingStore);
        var requestAdapterPropertyName = requestAdapterProperty.Name.ToSnakeCase();

        WriteSerializationRegistration(method.SerializerModules, writer, "register_default_serializer");
        WriteSerializationRegistration(method.DeserializerModules, writer, "register_default_deserializer");

        if (!string.IsNullOrEmpty(method.BaseUrl))
        {
            writer.StartBlock($"if instance.{requestAdapterPropertyName}.get_base_url().is_empty() {{");
            writer.WriteLine($"instance.{requestAdapterPropertyName}.set_base_url(\"{method.BaseUrl}\");");
            writer.CloseBlock();
            if (pathParametersProperty != null)
                writer.WriteLine($"instance.{pathParametersProperty.Name.ToSnakeCase()}.insert(\"baseurl\".to_string(), instance.{requestAdapterPropertyName}.get_base_url().to_string());");
        }

        if (backingStoreParameter != null)
        {
            writer.StartBlock($"if let Some(ref store) = {backingStoreParameter.Name.ToSnakeCase()} {{");
            writer.WriteLine($"instance.{requestAdapterPropertyName}.enable_backing_store(store);");
            writer.CloseBlock();
        }
        writer.WriteLine("instance");
    }

    private static void WriteSerializationRegistration(HashSet<string> serializationClassNames, LanguageWriter writer, string methodName)
    {
        if (serializationClassNames != null)
            foreach (var serializationClassName in serializationClassNames)
                writer.WriteLine($"ApiClientBuilder::{methodName}::<{serializationClassName}>();");
    }

    private void WriteConstructorBody(CodeClass parentClass, CodeMethod currentMethod, LanguageWriter writer)
    {
        if (parentClass.IsOfKind(CodeClassKind.RequestBuilder))
        {
            WriteRequestBuilderConstructorBody(parentClass, currentMethod, writer);
        }
        else if (parentClass.IsOfKind(CodeClassKind.Model))
        {
            WriteModelConstructorBody(parentClass, currentMethod, writer);
        }
    }

    private void WriteRequestBuilderConstructorBody(CodeClass parentClass, CodeMethod currentMethod, LanguageWriter writer)
    {
        var pathParametersProp = parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters);
        var urlTemplateProp = parentClass.GetPropertyOfKind(CodePropertyKind.UrlTemplate);
        var requestAdapterParam = currentMethod.Parameters.OfKind(CodeParameterKind.RequestAdapter);
        var pathParametersParam = currentMethod.Parameters.OfKind(CodeParameterKind.PathParameters);
        var rawUrlParam = currentMethod.Parameters.OfKind(CodeParameterKind.RawUrl);

        writer.StartBlock($"let mut instance = Self {{");
        if (requestAdapterParam != null)
            writer.WriteLine($"request_adapter: {requestAdapterParam.Name.ToSnakeCase()},");
        if (urlTemplateProp != null && !string.IsNullOrEmpty(urlTemplateProp.DefaultValue))
            writer.WriteLine($"url_template: {urlTemplateProp.DefaultValue}.to_string(),");
        if (pathParametersProp != null)
        {
            if (pathParametersParam != null)
                writer.WriteLine($"path_parameters: {pathParametersParam.Name.ToSnakeCase()},");
            else if (rawUrlParam != null)
            {
                writer.WriteLine("path_parameters: {");
                writer.IncreaseIndent();
                writer.WriteLine("let mut m = std::collections::HashMap::new();");
                writer.WriteLine($"m.insert(RequestInformation::RAW_URL_KEY.to_string(), {rawUrlParam.Name.ToSnakeCase()}.to_string());");
                writer.WriteLine("m");
                writer.DecreaseIndent();
                writer.WriteLine("},");
            }
            else
                writer.WriteLine("path_parameters: std::collections::HashMap::new(),");
        }
        writer.CloseBlock("};");

        // Handle path parameters
        if (pathParametersProp != null && pathParametersParam != null)
        {
            var pathParameters = currentMethod.Parameters.Where(static x => x.IsOfKind(CodeParameterKind.Path)).ToArray();
            if (pathParameters.Length > 0)
            {
                foreach (var param in pathParameters)
                {
                    var serialName = string.IsNullOrEmpty(param.SerializationName) ? param.Name : param.SerializationName;
                    writer.WriteLine($"instance.path_parameters.insert(\"{serialName}\".to_string(), {param.Name.ToSnakeCase()}.to_string());");
                }
            }
        }
    }

    private void WriteModelConstructorBody(CodeClass parentClass, CodeMethod currentMethod, LanguageWriter writer)
    {
        var propWithDefaults = parentClass.Properties
            .Where(static x => !string.IsNullOrEmpty(x.DefaultValue) && !x.IsOfKind(CodePropertyKind.UrlTemplate, CodePropertyKind.PathParameters, CodePropertyKind.BackingStore))
            .Where(static x => x.Type is not CodeType propType || propType.TypeDefinition is not CodeClass propertyClass || propertyClass.OriginalComposedType is null)
            .OrderByDescending(static x => x.Kind)
            .ThenBy(static x => x.Name).ToArray();

        writer.StartBlock("Self {");
        foreach (var prop in propWithDefaults)
        {
            var defaultValue = GetDefaultValueForProperty(prop, currentMethod);
            writer.WriteLine($"{prop.Name.ToSnakeCase()}: {defaultValue},");
        }
        writer.WriteLine("..Default::default()");
        writer.CloseBlock();
    }

    private string GetDefaultValueForProperty(CodeProperty prop, CodeMethod method)
    {
        var defaultValue = prop.DefaultValue;
        if (prop.Type is CodeType propertyType && propertyType.TypeDefinition is CodeEnum)
        {
            var enumTypeName = conventions.GetTypeString(prop.Type, method).TrimStart("Option<").TrimEnd('>').TrimEnd('?');
            return $"{enumTypeName}::{defaultValue.Trim('"').ToFirstCharacterUpperCase()}";
        }
        if (prop.Type is CodeType pt && pt.Name.Equals("String", StringComparison.OrdinalIgnoreCase))
        {
            return $"\"{defaultValue.Trim('"')}\".to_string()";
        }
        return defaultValue;
    }

    private void WriteDeserializerBody(bool shouldHide, CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer)
    {
        var fieldToSerialize = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom, CodePropertyKind.ErrorMessageOverride).ToArray();
        writer.WriteLine("let mut map: HashMap<String, Box<dyn Fn(&dyn ParseNode, &mut Self)>> = HashMap::new();");

        if (fieldToSerialize.Length != 0)
        {
            foreach (var prop in fieldToSerialize
                .Where(x => !x.ExistsInBaseType && !conventions.ErrorClassPropertyExistsInSuperClass(x))
                .OrderBy(static x => x.Name))
            {
                var propName = prop.Name.ToSnakeCase();
                var deserMethod = GetDeserializationMethodName(prop.Type, codeElement);
                writer.WriteLine($"map.insert(\"{prop.WireName}\".to_string(), Box::new(|node, obj| {{ obj.{propName} = node.{deserMethod}; }}));");
            }
        }
        writer.WriteLine("map");
    }

    private void WriteSerializerBody(bool shouldHide, CodeMethod method, CodeClass parentClass, LanguageWriter writer)
    {
        foreach (var otherProp in parentClass
            .GetPropertiesOfKind(CodePropertyKind.Custom, CodePropertyKind.ErrorMessageOverride)
            .Where(x => !x.ExistsInBaseType && !x.ReadOnly && !conventions.ErrorClassPropertyExistsInSuperClass(x))
            .OrderBy(static x => x.Name))
        {
            var serializationMethodName = GetSerializationMethodName(otherProp.Type, method);
            var propName = otherProp.Name.ToSnakeCase();
            writer.WriteLine($"writer.{serializationMethodName}(\"{otherProp.WireName}\", &self.{propName})?;");
        }

        if (parentClass.GetPropertyOfKind(CodePropertyKind.AdditionalData) is CodeProperty additionalDataProperty)
            writer.WriteLine($"writer.write_additional_data(&self.{additionalDataProperty.Name.ToSnakeCase()})?;");
        writer.WriteLine("Ok(())");
    }

    protected void WriteRequestExecutorBody(CodeMethod codeElement, RequestParams requestParams, CodeClass parentClass, bool isVoid, string returnTypeWithoutCollectionInformation, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(requestParams);
        ArgumentNullException.ThrowIfNull(parentClass);
        ArgumentNullException.ThrowIfNull(writer);
        if (codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");

        var generatorMethodName = parentClass
            .Methods
            .FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestGenerator) && x.HttpMethod == codeElement.HttpMethod)
            ?.Name
            ?.ToSnakeCase();
        var parametersList = new CodeParameter?[] { requestParams.requestBody, requestParams.requestContentType, requestParams.requestConfiguration }
            .Where(static x => x != null)
            .Select(static x => x!.Name.ToSnakeCase())
            .Aggregate(static (x, y) => $"{x}, {y}");
        writer.WriteLine($"let request_info = self.{generatorMethodName}({parametersList})?;");

        if (codeElement.ErrorMappings.Any())
        {
            writer.StartBlock("let error_mapping: HashMap<String, Box<dyn Fn(&dyn ParseNode) -> Box<dyn std::error::Error>>> = [");
            foreach (var errorMapping in codeElement.ErrorMappings.Where(errorMapping => errorMapping.Value.AllTypes.FirstOrDefault()?.TypeDefinition is CodeClass))
            {
                writer.WriteLine($"(\"{errorMapping.Key.ToUpperInvariant()}\".to_string(), Box::new(|n| Box::new({conventions.GetTypeString(errorMapping.Value, codeElement, false)}::create_from_discriminator_value(n))) as Box<dyn Fn(&dyn ParseNode) -> Box<dyn std::error::Error>>),");
            }
            writer.CloseBlock("].into_iter().collect();");
        }
        else
        {
            writer.WriteLine("let error_mapping: HashMap<String, Box<dyn Fn(&dyn ParseNode) -> Box<dyn std::error::Error>>> = HashMap::new();");
        }

        var sendMethod = GetSendRequestMethodName(isVoid, codeElement, codeElement.ReturnType);
        var returnTypeFactory = codeElement.ReturnType is CodeType { TypeDefinition: CodeClass }
            ? $", {returnTypeWithoutCollectionInformation}::create_from_discriminator_value"
            : "";
        writer.WriteLine($"self.request_adapter.{sendMethod}(request_info{returnTypeFactory}, error_mapping).await");
    }

    private void WriteRequestGeneratorBody(CodeMethod codeElement, RequestParams requestParams, CodeClass currentClass, LanguageWriter writer)
    {
        if (codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");
        if (currentClass.GetPropertyOfKind(CodePropertyKind.PathParameters) is not CodeProperty urlTemplateParamsProperty) throw new InvalidOperationException("path parameters property cannot be null");
        if (currentClass.GetPropertyOfKind(CodePropertyKind.UrlTemplate) is not CodeProperty urlTemplateProperty) throw new InvalidOperationException("url template property cannot be null");

        var operationName = codeElement.HttpMethod.ToString()?.ToUpperInvariant();
        var urlTemplateValue = codeElement.HasUrlTemplateOverride ? $"\"{codeElement.UrlTemplateOverride}\"" : $"&self.{urlTemplateProperty.Name.ToSnakeCase()}";
        writer.WriteLine($"let mut request_info = RequestInformation::new(Method::{operationName}, {urlTemplateValue}.to_string(), self.{urlTemplateParamsProperty.Name.ToSnakeCase()}.clone());");

        if (requestParams.requestConfiguration != null)
        {
            writer.StartBlock($"if let Some(config) = {requestParams.requestConfiguration.Name.ToSnakeCase()} {{");
            writer.WriteLine("request_info.add_request_configuration(&config);");
            writer.CloseBlock();
        }

        if (codeElement.ShouldAddAcceptHeader)
            writer.WriteLine($"request_info.headers.insert(\"Accept\".to_string(), \"{codeElement.AcceptHeaderValue}\".to_string());");

        if (requestParams.requestBody != null)
        {
            var bodyParamName = requestParams.requestBody.Name.ToSnakeCase();
            if (requestParams.requestBody.Type.Name.Equals(conventions.StreamTypeName, StringComparison.OrdinalIgnoreCase))
            {
                if (requestParams.requestContentType is not null)
                    writer.WriteLine($"request_info.set_stream_content({bodyParamName}, \"{requestParams.requestContentType.Name}\");");
                else if (!string.IsNullOrEmpty(codeElement.RequestBodyContentType))
                    writer.WriteLine($"request_info.set_stream_content({bodyParamName}, \"{codeElement.RequestBodyContentType}\");");
            }
            else if (requestParams.requestBody.Type is CodeType bodyType && (bodyType.TypeDefinition is CodeClass || bodyType.Name.Equals("MultipartBody", StringComparison.OrdinalIgnoreCase)))
                writer.WriteLine($"request_info.set_content_from_parsable(&self.request_adapter, \"{codeElement.RequestBodyContentType}\", &{bodyParamName})?;");
            else
                writer.WriteLine($"request_info.set_content_from_scalar(&self.request_adapter, \"{codeElement.RequestBodyContentType}\", &{bodyParamName})?;");
        }

        writer.WriteLine("Ok(request_info)");
    }

    private void WriteQueryParametersBody(CodeClass parentClass, LanguageWriter writer)
    {
        writer.StartBlock("let mut map = std::collections::HashMap::new();");
        foreach (CodeProperty property in parentClass.Properties)
        {
            var key = property.IsNameEscaped ? property.SerializationName : property.Name;
            var propName = property.Name.ToSnakeCase();
            writer.StartBlock($"if let Some(ref val) = self.{propName} {{");
            writer.WriteLine($"map.insert(\"{key}\".to_string(), val.to_string());");
            writer.CloseBlock();
        }
        writer.WriteLine("map");
    }

    protected string GetSendRequestMethodName(bool isVoid, CodeElement currentElement, CodeTypeBase returnType)
    {
        ArgumentNullException.ThrowIfNull(returnType);
        var returnTypeName = conventions.GetTypeString(returnType, currentElement, false);
        var isStream = conventions.StreamTypeName.Equals(returnTypeName, StringComparison.OrdinalIgnoreCase);
        var isEnum = returnType is CodeType codeType && codeType.TypeDefinition is CodeEnum;
        if (isVoid) return "send_no_content";
        if (isStream || conventions.IsPrimitiveType(returnTypeName) || isEnum)
            return returnType.IsCollection ? "send_primitive_collection" : "send_primitive";
        if (returnType.IsCollection) return "send_collection";
        return "send";
    }

    private string GetDeserializationMethodName(CodeTypeBase propType, CodeMethod method)
    {
        var isCollection = propType.CollectionKind != CodeTypeCollectionKind.None;
        var propertyType = conventions.GetTypeString(propType, method, false, false);
        if (propType is CodeType currentType)
        {
            if (isCollection)
            {
                if (currentType.TypeDefinition == null)
                    return $"get_collection_of_primitive_values::<{propertyType}>()";
                if (currentType.TypeDefinition is CodeEnum)
                    return $"get_collection_of_enum_values::<{propertyType}>()";
                return $"get_collection_of_object_values::<{propertyType}>({propertyType}::create_from_discriminator_value)";
            }
            if (currentType.TypeDefinition is CodeEnum)
                return $"get_enum_value::<{propertyType}>()";
        }
        return propertyType switch
        {
            "Vec<u8>" => "get_byte_array_value()",
            "uuid::Uuid" => "get_uuid_value()",
            "String" => "get_string_value()",
            "bool" => "get_bool_value()",
            "i32" => "get_i32_value()",
            "i64" => "get_i64_value()",
            "f32" => "get_f32_value()",
            "f64" => "get_f64_value()",
            _ when conventions.IsPrimitiveType(propertyType) => $"get_{propertyType.ToSnakeCase()}_value()",
            _ => $"get_object_value::<{propertyType}>({propertyType}::create_from_discriminator_value)",
        };
    }

    private string GetSerializationMethodName(CodeTypeBase propType, CodeMethod method)
    {
        var isCollection = propType.CollectionKind != CodeTypeCollectionKind.None;
        var propertyType = conventions.GetTypeString(propType, method, false, false);
        if (propType is CodeType currentType)
        {
            if (isCollection)
            {
                if (currentType.TypeDefinition == null)
                    return $"write_collection_of_primitive_values::<{propertyType}>";
                if (currentType.TypeDefinition is CodeEnum)
                    return $"write_collection_of_enum_values::<{propertyType}>";
                return $"write_collection_of_object_values::<{propertyType}>";
            }
            if (currentType.TypeDefinition is CodeEnum)
                return $"write_enum_value::<{propertyType}>";
        }
        return propertyType switch
        {
            "Vec<u8>" => "write_byte_array_value",
            "uuid::Uuid" => "write_uuid_value",
            "String" => "write_string_value",
            "bool" => "write_bool_value",
            "i32" => "write_i32_value",
            "i64" => "write_i64_value",
            "f32" => "write_f32_value",
            "f64" => "write_f64_value",
            _ when conventions.IsPrimitiveType(propertyType) => $"write_{propertyType.ToSnakeCase()}_value",
            _ => $"write_object_value::<{propertyType}>",
        };
    }

    private void WriteMethodDocumentation(CodeMethod code, LanguageWriter writer)
    {
        conventions.WriteLongDescription(code, writer);
        foreach (var paramWithDescription in code.Parameters
            .Where(static x => x.Documentation.DescriptionAvailable)
            .OrderBy(static x => x.Name, StringComparer.OrdinalIgnoreCase))
            conventions.WriteParameterDescription(paramWithDescription, writer);
        conventions.WriteDeprecationAttribute(code, writer);
    }

    private static readonly BaseCodeParameterOrderComparer parameterOrderComparer = new();

    private void WriteMethodPrototype(CodeMethod code, CodeClass parentClass, LanguageWriter writer, string returnType, bool inherits, bool isVoid)
    {
        var isConstructor = code.IsOfKind(CodeMethodKind.Constructor, CodeMethodKind.ClientConstructor, CodeMethodKind.RawUrlConstructor);
        var asyncKeyword = code.IsAsync ? "async " : "";
        var methodName = isConstructor ? "new" : code.Name.ToSnakeCase();
        if (code.IsOfKind(CodeMethodKind.RawUrlConstructor))
            methodName = "with_raw_url";

        var selfParam = code.IsStatic ? "" : "&self, ";
        if (isConstructor)
            selfParam = "";

        var parameters = string.Join(", ", code.Parameters
            .OrderBy(static x => x, parameterOrderComparer)
            .Select(p => conventions.GetParameterSignature(p, code)));

        var allParams = string.IsNullOrEmpty(parameters) ? selfParam.TrimEnd(' ').TrimEnd(',') : $"{selfParam}{parameters}";

        string returnTypeStr;
        if (isConstructor)
        {
            returnTypeStr = " -> Self";
        }
        else if (isVoid && code.IsAsync)
        {
            returnTypeStr = " -> Result<(), Box<dyn std::error::Error>>";
        }
        else if (isVoid)
        {
            returnTypeStr = "";
        }
        else if (code.IsAsync)
        {
            returnTypeStr = $" -> Result<{returnType}, Box<dyn std::error::Error>>";
        }
        else
        {
            returnTypeStr = $" -> {returnType}";
        }

        var visibility = conventions.GetAccessModifier(code.Access);
        writer.StartBlock($"{visibility}{asyncKeyword}fn {methodName}({allParams}){returnTypeStr} {{");
    }
}
