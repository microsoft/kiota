using System;
using System.Collections.Generic;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Rust;

public class CodeMethodWriter(RustConventionService conventionService) : BaseElementWriter<CodeMethod, RustConventionService>(conventionService)
{
    public override void WriteCodeElement(CodeMethod codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);

        if (codeElement.ReturnType is null)
            throw new InvalidOperationException($"Method {codeElement.Name} has no return type");

        var parentClass = codeElement.Parent as CodeClass;

        // skip methods on nested classes (config/query params — composed types were promoted by refiner)
        if (parentClass?.Parent is CodeClass)
            return;

        switch (codeElement.Kind)
        {
            case CodeMethodKind.Serializer:
            case CodeMethodKind.Deserializer:
                // handled by CodeClassDeclarationWriter in the Parsable impl block
                break;
            case CodeMethodKind.Constructor:
                WriteConstructorBody(codeElement, parentClass!, writer);
                break;
            case CodeMethodKind.ClientConstructor:
                WriteClientConstructorBody(codeElement, parentClass!, writer);
                break;
            case CodeMethodKind.RawUrlConstructor:
                WriteRawUrlConstructorBody(parentClass!, writer);
                break;
            case CodeMethodKind.Factory:
                WriteFactoryMethodBody(codeElement, parentClass!, writer);
                break;
            case CodeMethodKind.RequestGenerator:
                WriteRequestGeneratorBody(codeElement, parentClass!, writer);
                break;
            case CodeMethodKind.RequestExecutor:
                WriteRequestExecutorBody(codeElement, parentClass!, writer);
                break;
            case CodeMethodKind.Getter:
                WriteGetterBody(codeElement, writer);
                break;
            case CodeMethodKind.Setter:
                WriteSetterBody(codeElement, writer);
                break;
            case CodeMethodKind.RequestBuilderWithParameters:
            case CodeMethodKind.IndexerBackwardCompatibility:
                WriteIndexerBody(codeElement, writer);
                break;
            case CodeMethodKind.RequestBuilderBackwardCompatibility:
                WriteNavBody(codeElement, writer);
                break;
            default:
                // skip unknown method kinds
                break;
        }
    }

    private void WriteConstructorBody(CodeMethod method, CodeClass parentClass, LanguageWriter writer)
    {
        if (parentClass.IsOfKind(CodeClassKind.RequestBuilder) && method.Parameters.Any())
        {
            var urlTemplate = parentClass.GetPropertyOfKind(CodePropertyKind.UrlTemplate)
                ?.DefaultValue?.Trim('"') ?? string.Empty;
            writer.WriteLine("pub fn new(path_parameters: std::collections::HashMap<String, String>, request_adapter: std::sync::Arc<dyn RequestAdapter>) -> Self {");
            writer.IncreaseIndent();
            writer.WriteLine("Self {");
            writer.IncreaseIndent();
            writer.WriteLine($"base: BaseRequestBuilder::new(request_adapter, \"{urlTemplate}\".to_string(), path_parameters),");
            writer.DecreaseIndent();
            writer.WriteLine("}");
        }
        else
        {
            writer.WriteLine("pub fn new() -> Self {");
            writer.IncreaseIndent();
            writer.WriteLine("Self::default()");
        }
        writer.DecreaseIndent();
        writer.WriteLine("}");
    }

    private void WriteClientConstructorBody(CodeMethod method, CodeClass parentClass, LanguageWriter writer)
    {
        var urlTemplate = parentClass.GetPropertyOfKind(CodePropertyKind.UrlTemplate)
            ?.DefaultValue?.Trim('"') ?? "{+baseurl}";
        var baseUrl = method.BaseUrl ?? string.Empty;

        writer.WriteLine("pub fn new(request_adapter: std::sync::Arc<dyn RequestAdapter>) -> Self {");
        writer.IncreaseIndent();
        writer.WriteLine("let mut path_parameters = std::collections::HashMap::new();");

        if (!string.IsNullOrEmpty(baseUrl))
        {
            writer.WriteLine($"if request_adapter.base_url().is_empty() {{");
            writer.IncreaseIndent();
            writer.WriteLine($"path_parameters.insert(\"baseurl\".to_string(), \"{baseUrl}\".to_string());");
            writer.DecreaseIndent();
            writer.WriteLine("} else {");
            writer.IncreaseIndent();
            writer.WriteLine("path_parameters.insert(\"baseurl\".to_string(), request_adapter.base_url().to_string());");
            writer.DecreaseIndent();
            writer.WriteLine("}");
        }

        writer.WriteLine("Self {");
        writer.IncreaseIndent();
        writer.WriteLine($"base: BaseRequestBuilder::new(request_adapter, \"{urlTemplate}\".to_string(), path_parameters),");
        writer.DecreaseIndent();
        writer.WriteLine("}");
        writer.DecreaseIndent();
        writer.WriteLine("}");
    }

    private static void WriteRawUrlConstructorBody(CodeClass parentClass, LanguageWriter writer)
    {
        writer.WriteLine("pub fn with_url(raw_url: &str, request_adapter: std::sync::Arc<dyn RequestAdapter>) -> Self {");
        writer.IncreaseIndent();
        writer.WriteLine("let mut path_parameters = std::collections::HashMap::new();");
        writer.WriteLine("path_parameters.insert(\"request-raw-url\".to_string(), raw_url.to_string());");
        writer.WriteLine("Self {");
        writer.IncreaseIndent();
        writer.WriteLine("base: BaseRequestBuilder::new(request_adapter, String::new(), path_parameters),");
        writer.DecreaseIndent();
        writer.WriteLine("}");
        writer.DecreaseIndent();
        writer.WriteLine("}");
    }

    private void WriteFactoryMethodBody(CodeMethod method, CodeClass parentClass, LanguageWriter writer)
    {
        writer.WriteLine("pub fn create_from_discriminator_value(_parse_node: &dyn ParseNode) -> Result<Self, KiotaError> {");
        writer.IncreaseIndent();

        var disc = parentClass.DiscriminatorInformation;
        if (disc?.ShouldWriteDiscriminatorForInheritedType == true &&
            !string.IsNullOrEmpty(disc.DiscriminatorPropertyName))
        {
            writer.WriteLine($"if let Ok(Some(child)) = _parse_node.get_child_node(\"{disc.DiscriminatorPropertyName}\") {{");
            writer.IncreaseIndent();
            writer.WriteLine("if let Ok(Some(val)) = child.get_string_value() {");
            writer.IncreaseIndent();
            writer.WriteLine("match val.as_str() {");
            writer.IncreaseIndent();
            foreach (var m in disc.DiscriminatorMappings.OrderBy(static x => x.Key, StringComparer.OrdinalIgnoreCase))
            {
                var t = m.Value.AllTypes.FirstOrDefault()?.TypeDefinition?.Name?.ToFirstCharacterUpperCase();
                if (!string.IsNullOrEmpty(t))
                    writer.WriteLine($"\"{m.Key}\" => return Ok({t}::default()),");
            }
            writer.WriteLine("_ => {}");
            writer.DecreaseIndent();
            writer.WriteLine("}");
            writer.DecreaseIndent();
            writer.WriteLine("}");
            writer.DecreaseIndent();
            writer.WriteLine("}");
        }

        writer.WriteLine("Ok(Self::default())");
        writer.DecreaseIndent();
        writer.WriteLine("}");
    }

    private void WriteRequestGeneratorBody(CodeMethod method, CodeClass parentClass, LanguageWriter writer)
    {
        var httpMethod = method.HttpMethod?.ToString() ?? "Get";
        httpMethod = char.ToUpperInvariant(httpMethod[0]) + httpMethod[1..].ToLowerInvariant();
        var name = method.Name.ToSnakeCase();
        var configParam = method.Parameters.OfKind(CodeParameterKind.RequestConfiguration);
        var bodyParam = method.Parameters.OfKind(CodeParameterKind.RequestBody);

        // find the query parameters type for this method
        var qpTypeName = GetQueryParamsTypeName(configParam);

        var sig = new List<string> { "&self" };
        if (bodyParam != null)
            sig.Add($"body: &{conventions.GetTypeString(bodyParam.Type, method)}");
        if (configParam != null)
            sig.Add($"config: Option<&RequestConfiguration<{qpTypeName}>>");

        conventions.WriteShortDescription(method, writer);
        writer.WriteLine($"pub fn {name}({string.Join(", ", sig)}) -> Result<RequestInformation, KiotaError> {{");
        writer.IncreaseIndent();
        writer.WriteLine($"let mut request_info = RequestInformation::new_with_method_and_url_template(HttpMethod::{httpMethod}, &self.base.url_template, self.base.path_parameters.clone());");

        if (configParam != null)
        {
            writer.WriteLine("if let Some(c) = config {");
            writer.IncreaseIndent();
            writer.WriteLine("request_info.configure(c);");
            writer.DecreaseIndent();
            writer.WriteLine("}");
        }

        if (method.AcceptedResponseTypes?.Any() == true)
            writer.WriteLine($"request_info.headers.try_add(\"Accept\", \"{string.Join(", ", method.AcceptedResponseTypes)}\");");

        if (bodyParam != null)
        {
            var contentType = method.RequestBodyContentType ?? "application/json";
            var isBodyCollection = bodyParam.Type.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
            if (isBodyCollection)
            {
                writer.WriteLine($"// TODO: serialize collection body for \"{contentType}\"");
            }
            else if (bodyParam.Type.IsNullable)
            {
                writer.WriteLine("if let Some(ref b) = body {");
                writer.IncreaseIndent();
                writer.WriteLine($"request_info.set_content_from_parsable(self.base.request_adapter.serialization_writer_factory(), \"{contentType}\", b as &dyn Parsable)?;");
                writer.DecreaseIndent();
                writer.WriteLine("}");
            }
            else
            {
                writer.WriteLine($"request_info.set_content_from_parsable(self.base.request_adapter.serialization_writer_factory(), \"{contentType}\", body as &dyn Parsable)?;");
            }
        }

        writer.WriteLine("Ok(request_info)");
        writer.DecreaseIndent();
        writer.WriteLine("}");
    }

    private void WriteRequestExecutorBody(CodeMethod method, CodeClass parentClass, LanguageWriter writer)
    {
        var name = method.Name.ToSnakeCase();
        var returnType = conventions.GetTypeString(method.ReturnType, method);
        var configParam = method.Parameters.OfKind(CodeParameterKind.RequestConfiguration);
        var bodyParam = method.Parameters.OfKind(CodeParameterKind.RequestBody);
        var isVoid = method.ReturnType.Name.Equals("void", StringComparison.OrdinalIgnoreCase);
        var isStream = method.ReturnType.Name.Equals("binary", StringComparison.OrdinalIgnoreCase);
        var isCollection = method.ReturnType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;

        var generatorMethod = parentClass.Methods
            .FirstOrDefault(m => m.IsOfKind(CodeMethodKind.RequestGenerator) && m.HttpMethod == method.HttpMethod);
        var genName = generatorMethod?.Name.ToSnakeCase() ?? $"to_{name}_request_information";

        var qpTypeName = GetQueryParamsTypeName(configParam);

        var sig = new List<string> { "&self" };
        if (bodyParam != null)
            sig.Add($"body: &{conventions.GetTypeString(bodyParam.Type, method)}");
        if (configParam != null)
            sig.Add($"config: Option<&RequestConfiguration<{qpTypeName}>>");

        var args = new List<string>();
        if (bodyParam != null) args.Add("body");
        if (configParam != null) args.Add("config");

        conventions.WriteShortDescription(method, writer);
        writer.WriteLine($"pub async fn {name}({string.Join(", ", sig)}) -> Result<{returnType}, KiotaError> {{");
        writer.IncreaseIndent();
        writer.WriteLine($"let request_info = self.{genName}({string.Join(", ", args)})?;");

        if (isVoid)
        {
            writer.WriteLine("self.base.request_adapter.send_no_content(&request_info, None).await");
        }
        else if (isStream)
        {
            writer.WriteLine("self.base.request_adapter.send_primitive(&request_info, None).await");
        }
        else
        {
            // figure out the model type for the factory
            var modelType = method.ReturnType is CodeType ct && ct.TypeDefinition is CodeClass modelClass
                ? modelClass.Name.ToFirstCharacterUpperCase()
                : null;

            if (modelType != null)
            {
                writer.WriteLine($"let factory: Box<dyn Fn(&dyn ParseNode) -> Result<Box<dyn Parsable>, KiotaError> + Send + Sync> =");
                writer.IncreaseIndent();
                writer.WriteLine($"Box::new(|node| Ok(Box::new({modelType}::create_from_discriminator_value(node)?)));");
                writer.DecreaseIndent();

                if (isCollection)
                {
                    writer.WriteLine("let results = self.base.request_adapter.send_collection(&request_info, &factory, None).await?;");
                    writer.WriteLine($"let typed: Vec<{modelType}> = results.into_iter().filter_map(|r| {{");
                    writer.IncreaseIndent();
                    writer.WriteLine($"r.as_any().downcast::<{modelType}>().ok().map(|b| *b)");
                    writer.DecreaseIndent();
                    writer.WriteLine("}).collect();");
                    writer.WriteLine("Ok(typed)");
                }
                else if (method.ReturnType.IsNullable)
                {
                    writer.WriteLine("let result = self.base.request_adapter.send(&request_info, &factory, None).await?;");
                    writer.WriteLine($"Ok(result.and_then(|r| r.as_any().downcast::<{modelType}>().ok().map(|b| *b)))");
                }
                else
                {
                    writer.WriteLine("let result = self.base.request_adapter.send(&request_info, &factory, None).await?;");
                    writer.WriteLine($"result.ok_or_else(|| KiotaError::General(\"empty response\".to_string()))");
                    writer.WriteLine($"    .and_then(|r| r.as_any().downcast::<{modelType}>().map(|b| *b)");
                    writer.WriteLine($"        .map_err(|_| KiotaError::Deserialization(\"type mismatch\".to_string())))");
                }
            }
            else
            {
                writer.WriteLine("todo!(\"unknown return type\")");
            }
        }

        writer.DecreaseIndent();
        writer.WriteLine("}");
    }

    private void WriteGetterBody(CodeMethod method, LanguageWriter writer)
    {
        var prop = method.AccessedProperty;
        if (prop == null) return;
        var fieldName = prop.Name.ToSnakeCase();
        var cleanName = StripRawPrefix(fieldName);
        var returnType = conventions.GetTypeString(prop.Type, method);

        writer.WriteLine($"pub fn get_{cleanName}(&self) -> &{returnType} {{");
        writer.IncreaseIndent();
        writer.WriteLine($"&self.{fieldName}");
        writer.DecreaseIndent();
        writer.WriteLine("}");
    }

    private void WriteSetterBody(CodeMethod method, LanguageWriter writer)
    {
        var prop = method.AccessedProperty;
        if (prop == null) return;
        var fieldName = prop.Name.ToSnakeCase();
        var cleanName = StripRawPrefix(fieldName);
        var paramType = conventions.GetTypeString(prop.Type, method);

        writer.WriteLine($"pub fn set_{cleanName}(&mut self, value: {paramType}) {{");
        writer.IncreaseIndent();
        writer.WriteLine($"self.{fieldName} = value;");
        writer.DecreaseIndent();
        writer.WriteLine("}");
    }

    private static string GetQueryParamsTypeName(CodeParameter? configParam)
    {
        if (configParam?.Type is CodeType configType)
        {
            var genericArg = configType.GenericTypeParameterValues.FirstOrDefault();
            if (genericArg is CodeType qpType && qpType.TypeDefinition is CodeClass qpClass)
                return qpClass.Name.ToFirstCharacterUpperCase();
        }
        return "DefaultQueryParameters";
    }

    private static string StripRawPrefix(string name)
    {
        return name.StartsWith("r#", StringComparison.Ordinal) ? name[2..] : name;
    }

    private void WriteIndexerBody(CodeMethod method, LanguageWriter writer)
    {
        var returnType = conventions.GetTypeString(method.ReturnType, method);
        var name = method.Name.ToSnakeCase();

        var sig = new List<string> { "&self" };
        foreach (var p in method.Parameters.Where(static p => p.IsOfKind(CodeParameterKind.Custom, CodeParameterKind.Path)))
            sig.Add($"{p.Name.ToSnakeCase()}: {conventions.GetTypeString(p.Type, method)}");

        conventions.WriteShortDescription(method, writer);
        writer.WriteLine($"pub fn {name}({string.Join(", ", sig)}) -> {returnType} {{");
        writer.IncreaseIndent();
        writer.WriteLine("let mut url_tpl_params = self.base.path_parameters.clone();");
        foreach (var p in method.Parameters.Where(static p => p.IsOfKind(CodeParameterKind.Custom, CodeParameterKind.Path)))
            writer.WriteLine($"url_tpl_params.insert(\"{p.SerializationName ?? p.Name}\".to_string(), {p.Name.ToSnakeCase()}.to_string());");
        writer.WriteLine($"{returnType}::new(url_tpl_params, self.base.request_adapter.clone())");
        writer.DecreaseIndent();
        writer.WriteLine("}");
    }

    private void WriteNavBody(CodeMethod method, LanguageWriter writer)
    {
        var returnType = conventions.GetTypeString(method.ReturnType, method);
        var name = method.Name.ToSnakeCase();

        conventions.WriteShortDescription(method, writer);
        writer.WriteLine($"pub fn {name}(&self) -> {returnType} {{");
        writer.IncreaseIndent();
        writer.WriteLine($"{returnType}::new(self.base.path_parameters.clone(), self.base.request_adapter.clone())");
        writer.DecreaseIndent();
        writer.WriteLine("}");
    }
}
