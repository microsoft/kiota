using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder
{
    public class JavaWriter : LanguageWriter
    {
        private readonly IPathSegmenter segmenter;

        public JavaWriter(string rootPath, string clientNamespaceName)
        {
            segmenter = new JavaPathSegmenter(rootPath, clientNamespaceName);
        }
        public override IPathSegmenter PathSegmenter => segmenter;

        public override string GetParameterSignature(CodeParameter parameter)
        {
            return $"@javax.annotation.{(parameter.Optional ? "Nullable" : "Nonnull")} final {GetTypeString(parameter.Type)} {parameter.Name}";
        }

        public override string GetTypeString(CodeTypeBase code)
        {
            if(code is CodeUnionType) 
                throw new InvalidOperationException($"Java does not support union types, the union type {code.Name} should have been filtered out by the refiner");
            else if (code is CodeType currentType) {
                var typeName = TranslateType(currentType.Name);
                var collectionPrefix = currentType.CollectionKind == CodeType.CodeTypeCollectionKind.Complex ? "List<" : string.Empty;
                var collectionSuffix = currentType.CollectionKind == CodeType.CodeTypeCollectionKind.Complex ? ">" : 
                                            (currentType.CollectionKind == CodeType.CodeTypeCollectionKind.Array ? "[]" : string.Empty);
                if (currentType.ActionOf)
                    return $"java.util.function.Consumer<{collectionPrefix}{typeName}{collectionSuffix}>";
                else
                    return $"{collectionPrefix}{typeName}{collectionSuffix}";
            }
            else throw new InvalidOperationException($"type of type {code.GetType()} is unknown");
        }

        public override string TranslateType(string typeName)
        {
            switch (typeName)
            {//TODO we're probably missing a bunch of type mappings
                case "void": return typeName.ToFirstCharacterLowerCase(); //little casing hack
                default: return typeName.ToFirstCharacterUpperCase() ?? "Object";
            }
        }

        public override void WriteCodeClassDeclaration(CodeClass.Declaration code)
        {
            if(code?.Parent?.Parent is CodeNamespace ns) {
                WriteLine($"package {ns.Name};");
                WriteLine();
                code.Usings
                    .Select(x => x.Declaration.IsExternal ?
                                     $"import {x.Declaration.Name}.{x.Name.ToFirstCharacterUpperCase()};" :
                                     $"import {x.Name}.{x.Declaration.Name.ToFirstCharacterUpperCase()};")
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList()
                    .ForEach(x => WriteLine(x));
            }
            var derivation = (code.Inherits == null ? string.Empty : $" extends {code.Inherits.Name.ToFirstCharacterUpperCase()}") +
                            (!code.Implements.Any() ? string.Empty : $" implements {code.Implements.Select(x => x.Name).Aggregate((x,y) => x + " ," + y)}");
            WriteShortDescription((code.Parent as CodeClass)?.Description);
            WriteLine($"public class {code.Name.ToFirstCharacterUpperCase()}{derivation} {{");
            IncreaseIndent();
        }

        public override void WriteCodeClassEnd(CodeClass.End code)
        {
            DecreaseIndent();
            WriteLine("}");
        }

        public override void WriteIndexer(CodeIndexer code)
        {
            throw new InvalidOperationException("indexers are not supported in Java, the refiner should have replaced those by methods");
        }
        private const string serializerFactoryParamName = "serializerFactory";
        private const string streamType = "InputStream";
        private const string docCommentStart = "/**";
        private const string docCommentPrefix = " * ";
        private const string docCommentEnd = " */";
        private void WriteMethodDocumentation(CodeMethod code) {
            var isDescriptionPresent = !string.IsNullOrEmpty(code.Description);
            var parametersWithDescription = code.Parameters.Where(x => !string.IsNullOrEmpty(code.Description));
            if (isDescriptionPresent || parametersWithDescription.Any()) {
                WriteLine(docCommentStart);
                if(isDescriptionPresent)
                    WriteLine($"{docCommentPrefix}{RemoveInvalidDescriptionCharacters(code.Description)}");
                foreach(var paramWithDescription in parametersWithDescription)
                    WriteLine($"{docCommentPrefix}@param {paramWithDescription.Name} {RemoveInvalidDescriptionCharacters(paramWithDescription.Description)}");
                
                if(code.IsAsync)
                    WriteLine($"{docCommentPrefix}@return a CompletableFuture of {code.ReturnType.Name}");
                else
                    WriteLine($"{docCommentPrefix}@return a {code.ReturnType.Name}");
                WriteLine(docCommentEnd);
            }
        }
        private static string RemoveInvalidDescriptionCharacters(string originalDescription) => originalDescription?.Replace("\\", "/");
        private void WriteShortDescription(string description) {
            if(!string.IsNullOrEmpty(description))
                WriteLine($"{docCommentStart} {RemoveInvalidDescriptionCharacters(description)} {docCommentEnd}");
        }
        private void WriteMethodPrototype(CodeMethod code, string returnType) {
            var accessModifier = GetAccessModifier(code.Access);
            var genericTypeParameterDeclaration = code.MethodKind == CodeMethodKind.DeserializerBackwardCompatibility ? "<T> ": string.Empty;
            var returnTypeAsyncPrefix = code.IsAsync ? "java.util.concurrent.CompletableFuture<" : string.Empty;
            var returnTypeAsyncSuffix = code.IsAsync ? ">" : string.Empty;
            var methodName = code.Name.ToFirstCharacterLowerCase();
            var parameters = string.Join(", ", code.Parameters.Select(p=> GetParameterSignature(p)).ToList());
            var throwableDeclarations = code.MethodKind == CodeMethodKind.RequestGenerator ? "throws URISyntaxException ": string.Empty;
            WriteLine($"{accessModifier} {genericTypeParameterDeclaration}{returnTypeAsyncPrefix}{returnType}{returnTypeAsyncSuffix} {methodName}({parameters}) {throwableDeclarations}{{");
        }
        public override void WriteMethod(CodeMethod code)
        {
            var returnType = GetTypeString(code.ReturnType);
            var parentClass = code.Parent as CodeClass;
            WriteMethodDocumentation(code);
            if(returnType.Equals("void", StringComparison.InvariantCultureIgnoreCase))
            {
                if(code.MethodKind == CodeMethodKind.RequestExecutor)
                    returnType = "Void"; //generic type for the future
            } else if(!code.IsAsync)
                WriteLine(code.ReturnType.IsNullable && !code.IsAsync ? "@javax.annotation.Nullable" : "@javax.annotation.Nonnull");
            WriteMethodPrototype(code, returnType);
            IncreaseIndent();
            var requestBodyParam = code.Parameters.OfKind(CodeParameterKind.RequestBody);
            var queryStringParam = code.Parameters.OfKind(CodeParameterKind.QueryParameter);
            var headersParam = code.Parameters.OfKind(CodeParameterKind.Headers);
            foreach(var parameter in code.Parameters.Where(x => !x.Optional)) {
                WriteLine($"Objects.requireNonNull({parameter.Name});");
            }
            switch(code.MethodKind) {
                case CodeMethodKind.Serializer:
                    if((parentClass.StartBlock as CodeClass.Declaration).Inherits != null)
                        WriteLine("super.serialize(writer);");
                    foreach(var otherProp in parentClass
                                                    .InnerChildElements
                                                    .OfType<CodeProperty>()
                                                    .Where(x => x.PropertyKind == CodePropertyKind.Custom)) {
                        WriteLine($"writer.{GetSerializationMethodName(otherProp.Type)}(\"{otherProp.Name.ToFirstCharacterLowerCase()}\", {otherProp.Name.ToFirstCharacterLowerCase()});");
                    }
                break;
                case CodeMethodKind.DeserializerBackwardCompatibility:
                    var inherits = (parentClass.StartBlock as CodeClass.Declaration).Inherits != null;
                    var fieldToSerialize = parentClass
                            .InnerChildElements
                            .OfType<CodeProperty>()
                            .Where(x => x.PropertyKind == CodePropertyKind.Custom);
                    WriteLine($"final Map<String, BiConsumer<T, ParseNode>> fields = new HashMap<>({(inherits ? "super." + code.Name+ "()" : fieldToSerialize.Count())});");
                    if(fieldToSerialize.Any())
                        fieldToSerialize
                                .Select(x => 
                                    $"fields.put(\"{x.Name.ToFirstCharacterLowerCase()}\", (o, n) -> {{ (({parentClass.Name.ToFirstCharacterUpperCase()})o).{x.Name.ToFirstCharacterLowerCase()} = {GetDeserializationMethodName(x.Type)}; }});")
                                .ToList()
                                .ForEach(x => WriteLine(x));
                    WriteLine("return fields;");
                    break;
                case CodeMethodKind.IndexerBackwardCompatibility:
                    var pathSegment = code.GenerationProperties.ContainsKey(pathSegmentPropertyName) ? code.GenerationProperties[pathSegmentPropertyName] as string : string.Empty;
                    AddRequestBuilderBody(returnType, $" + \"/{(string.IsNullOrEmpty(pathSegment) ? string.Empty : pathSegment + "/" )}\" + id");
                break;
                case CodeMethodKind.RequestGenerator:
                    WriteLine("final RequestInfo requestInfo = new RequestInfo() {{");
                    IncreaseIndent();
                    WriteLines($"uri = new URI({currentPathPropertyName} + {pathSegmentPropertyName});",
                                $"httpMethod = HttpMethod.{code.HttpMethod?.ToString().ToUpperInvariant()};");
                    DecreaseIndent();
                    WriteLine("}};");
                    if(requestBodyParam != null)
                        if(requestBodyParam.Type.Name.Equals(streamType, StringComparison.InvariantCultureIgnoreCase))
                            WriteLine($"requestInfo.setStreamContent({requestBodyParam.Name});");
                        else
                            WriteLine($"requestInfo.setJsonContentFromParsable({requestBodyParam.Name}, {serializerFactoryParamName});"); //TODO we're making a big assumption here that the request is json
                    if(queryStringParam != null) {
                        var httpMethodPrefix = code.HttpMethod.ToString().ToFirstCharacterUpperCase();
                        WriteLine($"if ({queryStringParam.Name} != null) {{");
                        IncreaseIndent();
                        WriteLines($"final {httpMethodPrefix}QueryParameters qParams = new {httpMethodPrefix}QueryParameters();",
                                    $"{queryStringParam.Name}.accept(qParams);",
                                    "qParams.AddQueryParameters(requestInfo.queryParameters);");
                        DecreaseIndent();
                        WriteLine("}");
                    }
                    if(headersParam != null) {
                        WriteLine($"if ({headersParam.Name} != null) {{");
                        IncreaseIndent();
                        WriteLine($"{headersParam.Name}.accept(requestInfo.headers);");
                        DecreaseIndent();
                        WriteLine("}");
                    }
                    WriteLine("return requestInfo;");
                break;
                case CodeMethodKind.RequestExecutor:
                    var generatorMethodName = (code.Parent as CodeClass)
                                                .InnerChildElements
                                                .OfType<CodeMethod>()
                                                .FirstOrDefault(x => x.MethodKind == CodeMethodKind.RequestGenerator && x.HttpMethod == code.HttpMethod)
                                                ?.Name
                                                ?.ToFirstCharacterLowerCase();
                    WriteLine("try {");
                    IncreaseIndent();
                    WriteLine($"final RequestInfo requestInfo = {generatorMethodName}(");
                    var requestInfoParameters = new List<string> { requestBodyParam?.Name, queryStringParam?.Name, headersParam?.Name }.Where(x => x != null);
                    if(requestInfoParameters.Any()) {
                        IncreaseIndent();
                        WriteLine(requestInfoParameters.Aggregate((x,y) => $"{x}, {y}"));
                        DecreaseIndent();
                    }
                    WriteLine(");");
                    var sendMethodName = primitiveTypes.Contains(returnType) ? "sendPrimitiveAsync" : "sendAsync";
                    if(code.Parameters.Any(x => x.ParameterKind == CodeParameterKind.ResponseHandler))
                        WriteLine($"return this.httpCore.{sendMethodName}(requestInfo, {returnType}.class, responseHandler);");
                    else
                        WriteLine($"return this.httpCore.{sendMethodName}(requestInfo, {returnType}.class, null);");
                    DecreaseIndent();
                    WriteLine("} catch (URISyntaxException ex) {");
                    IncreaseIndent();
                    WriteLine("return java.util.concurrent.CompletableFuture.failedFuture(ex);");
                    DecreaseIndent();
                    WriteLine("}");
                break;
                default:
                    WriteLine("return null;");
                break;
            }
            DecreaseIndent();
            WriteLine("}");
        }
        private static HashSet<string> primitiveTypes = new() {"String", "Boolean", "Integer", "Float", "Long", "Guid", "OffsetDateTime", "Void", streamType };
        private string GetDeserializationMethodName(CodeTypeBase propType) {
            var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
            var propertyType = TranslateType(propType.Name);
            if(propType is CodeType currentType) {
                if(isCollection)
                    if(currentType.TypeDefinition == null)
                        return $"n.getCollectionOfPrimitiveValues({propertyType.ToFirstCharacterUpperCase()}.class)";
                    else
                        return $"n.getCollectionOfObjectValues({propertyType.ToFirstCharacterUpperCase()}.class)";
                else if (currentType.TypeDefinition is CodeEnum currentEnum)
                    return $"n.getEnum{(currentEnum.Flags ? "Set" : string.Empty)}Value({propertyType.ToFirstCharacterUpperCase()}.class)";
            }
            switch(propertyType) {
                case "String":
                case "Boolean":
                case "Integer":
                case "Float":
                case "Long":
                case "Guid":
                case "OffsetDateTime":
                    return $"n.get{propertyType.ToFirstCharacterUpperCase()}Value()";
                default:
                    return $"n.getObjectValue({propertyType.ToFirstCharacterUpperCase()}.class)";
            }
        }
        private string GetSerializationMethodName(CodeTypeBase propType) {
            var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
            var propertyType = TranslateType(propType.Name);
            if(propType is CodeType currentType) {
                if(isCollection)
                    if(currentType.TypeDefinition == null)
                        return $"writeCollectionOfPrimitiveValues";
                    else
                        return $"writeCollectionOfObjectValues";
                else if (currentType.TypeDefinition is CodeEnum currentEnum)
                    return $"writeEnum{(currentEnum.Flags ? "Set" : string.Empty)}Value";
            }
            switch(propertyType) {
                case "String":
                case "Boolean":
                case "Integer":
                case "Float":
                case "Long":
                case "Guid":
                case "OffsetDateTime":
                    return $"write{propertyType}Value";
                default:
                    return $"writeObjectValue";
            }
        }
        private const string pathSegmentPropertyName = "pathSegment";
        private const string currentPathPropertyName = "currentPath";
        private const string httpCorePropertyName = "httpCore";
        private void AddRequestBuilderBody(string returnType, string suffix = default) {
            // we're assigning this temp variable because java doesn't have a way to differentiate references with same names in properties initializers
            // and because if currentPath is null it'll add "null" to the string...
            WriteLines($"final String parentPath = ({currentPathPropertyName} == null ? \"\" : {currentPathPropertyName}) + {pathSegmentPropertyName}{suffix};",
                        $"final HttpCore parentCore = {httpCorePropertyName};", //this variable naming is because Java can't tell the difference in terms of scopes priority in property initializers
                        $"return new {returnType}() {{{{ {currentPathPropertyName} = parentPath; {httpCorePropertyName} = parentCore; }}}};");
        }
        public override void WriteProperty(CodeProperty code)
        {
            WriteShortDescription(code.Description);
            var returnType = GetTypeString(code.Type);
            switch(code.PropertyKind) {
                case CodePropertyKind.RequestBuilder:
                    WriteLine("@javax.annotation.Nonnull");
                    WriteLine($"{GetAccessModifier(code.Access)} {returnType} {code.Name.ToFirstCharacterLowerCase()}() {{");
                    IncreaseIndent();
                    AddRequestBuilderBody(returnType);
                    DecreaseIndent();
                    WriteLine("}");
                break;
                case CodePropertyKind.Deserializer:
                    throw new InvalidOperationException("java uses methods for the deserializer and this property should have been converted by the refiner");
                default:
                    var defaultValue = string.IsNullOrEmpty(code.DefaultValue) ? string.Empty : $" = {code.DefaultValue}";
                    if(code.Type is CodeType currentType && currentType.TypeDefinition is CodeEnum enumType && enumType.Flags)
                        returnType = $"EnumSet<{returnType}>";
                    WriteLine(code.Type.IsNullable ? "@javax.annotation.Nullable" : "@javax.annotation.Nonnull");
                    WriteLine($"{GetAccessModifier(code.Access)}{(code.ReadOnly ? " final " : " ")}{returnType} {code.Name.ToFirstCharacterLowerCase()}{defaultValue};");
                break;
            }
        }

        public override void WriteType(CodeType code)
        {
            Write(GetTypeString(code), includeIndent: false);
        }
        public override string GetAccessModifier(AccessModifier access)
        {
            switch(access) {
                case AccessModifier.Public: return "public";
                case AccessModifier.Protected: return "protected";
                default: return "private";
            }
        }

        public override void WriteEnum(CodeEnum code)
        {
            if(!code.Options.Any())
                return;
            var enumName = code.Name.ToFirstCharacterUpperCase();
            WriteLines($"package {(code.Parent as CodeNamespace)?.Name};",
                string.Empty,
                "import com.microsoft.kiota.serialization.ValuedEnum;",
                string.Empty);
            WriteShortDescription(code.Description);
            WriteLine($"public enum {enumName} implements ValuedEnum {{");
            IncreaseIndent();
            Write(code.Options
                        .Select(x => $"{x.ToFirstCharacterUpperCase()}(\"{x}\")")
                        .Aggregate((x, y) => $"{x},{NewLine}{GetIndent()}{y}") + ";" + NewLine);
            WriteLines("public final String value;",
                $"{enumName}(final String value) {{");
            IncreaseIndent();
            WriteLine("this.value = value;");
            DecreaseIndent();
            WriteLines("}",
                        "@javax.annotation.Nonnull",
                        "public String getValue() { return this.value; }",
                        "@javax.annotation.Nullable",
                        $"public static {enumName} forValue(@javax.annotation.Nonnull final String searchValue) {{");
            IncreaseIndent();
            WriteLine("switch(searchValue) {");
            IncreaseIndent();
            Write(code.Options
                        .Select(x => $"case \"{x}\": return {x.ToFirstCharacterUpperCase()};")
                        .Aggregate((x, y) => $"{x}{NewLine}{GetIndent()}{y}") + NewLine);
            WriteLine("default: return null;");
            DecreaseIndent();
            WriteLine("}");
            DecreaseIndent();
            WriteLine("}");
            DecreaseIndent();
            WriteLine("}");
        }
    }
}
