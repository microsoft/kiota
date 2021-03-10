using System;
using System.Collections.Generic;
using System.Linq;

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
                case "integer": return "Integer";
                case "boolean": return "Boolean";
                case "string": return "String";
                case "object": return "Object";
                default: return typeName.ToFirstCharacterUpperCase() ?? "Object";
            }
        }

        public override void WriteCodeClassDeclaration(CodeClass.Declaration code)
        {
            if(code?.Parent?.Parent is CodeNamespace ns) {
                WriteLine($"package {ns.Name};");
                WriteLine();
                code.Usings
                    .Select(x => $"import {x.Name}.{x.Declaration.Name.ToFirstCharacterUpperCase()};")
                    .Distinct()
                    .ToList()
                    .ForEach(x => WriteLine(x));
            }
            var derivation = (code.Inherits == null ? string.Empty : $" extends {code.Inherits.Name.ToFirstCharacterUpperCase()}") +
                            (!code.Implements.Any() ? string.Empty : $" implements {code.Implements.Select(x => x.Name).Aggregate((x,y) => x + " ," + y)}");
            //TODO: missing javadoc
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

        public override void WriteMethod(CodeMethod code)
        {
            //TODO javadoc
            WriteLine(code.ReturnType.IsNullable && !code.IsAsync ? "@javax.annotation.Nullable" : "@javax.annotation.Nonnull");
            WriteLine($"{GetAccessModifier(code.Access)} {(code.IsAsync ? "java.util.concurrent.CompletableFuture<" : string.Empty)}{GetTypeString(code.ReturnType).ToFirstCharacterUpperCase()}{(code.IsAsync ? ">" : string.Empty)} {code.Name.ToFirstCharacterLowerCase()}({string.Join(", ", code.Parameters.Select(p=> GetParameterSignature(p)).ToList())}) {(code.MethodKind == CodeMethodKind.RequestGenerator ? "throws URISyntaxException ": string.Empty)}{{");
            IncreaseIndent();
            var requestBodyParam = code.Parameters.FirstOrDefault(x => x.ParameterKind == CodeParameterKind.RequestBody);
            var queryStringParam = code.Parameters.FirstOrDefault(x => x.ParameterKind == CodeParameterKind.QueryParameter);
            var headersParam = code.Parameters.FirstOrDefault(x => x.ParameterKind == CodeParameterKind.Headers);
            foreach(var parameter in code.Parameters.Where(x => !x.Optional)) {
                WriteLine($"Objects.requireNonNull({parameter.Name});");
            }
            switch(code.MethodKind) {
                case CodeMethodKind.IndexerBackwardCompatibility:
                    var pathSegment = code.GenerationProperties.ContainsKey(pathSegmentPropertyName) ? code.GenerationProperties[pathSegmentPropertyName] as string : string.Empty;
                    var returnType = GetTypeString(code.ReturnType);
                    AddRequestBuilderBody(returnType, $" + \"/{(string.IsNullOrEmpty(pathSegment) ? string.Empty : pathSegment + "/" )}\" + id");
                break;
                case CodeMethodKind.RequestGenerator:
                    WriteLine("final RequestInfo requestInfo = new RequestInfo() {{");
                    IncreaseIndent();
                    WriteLines($"uri = new URI({currentPathPropertyName} + {pathSegmentPropertyName});",
                                $"httpMethod = HttpMethod.{code.HttpMethod?.ToString().ToUpperInvariant()};");
                    if(requestBodyParam != null)
                        WriteLine($"content = (InputStream)(Object){requestBodyParam.Name};"); //TODO remove cast when serialization is available
                    DecreaseIndent();
                    WriteLine("}};");
                    if(queryStringParam != null)
                        WriteLines($"final {code.HttpMethod.ToString().ToFirstCharacterUpperCase()}QueryParameters qParams = new {code.HttpMethod?.ToString().ToFirstCharacterUpperCase()}QueryParameters();",
                                   $"{queryStringParam.Name}.accept(qParams);",
                                   "qParams.AddQueryParameters(requestInfo.queryParameters);");
                    if(headersParam != null)
                        WriteLine($"{headersParam.Name}.accept(requestInfo.headers);");
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
                    if(code.Parameters.Any(x => x.ParameterKind == CodeParameterKind.ResponseHandler))
                        WriteLine("return this.httpCore.sendAsync(requestInfo, responseHandler);");
                    else
                        WriteLine("return this.httpCore.sendAsync(requestInfo, null);");
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
        private const string pathSegmentPropertyName = "pathSegment";
        private const string currentPathPropertyName = "currentPath";
        private void AddRequestBuilderBody(string returnType, string suffix = default) {
            // we're assigning this temp variable because java doesn't have a way to differentiate references with same names in properties initializers
            // and because if currentPath is null it'll add "null" to the string...
            WriteLine($"final String parentPath = ({currentPathPropertyName} == null ? \"\" : {currentPathPropertyName}) + {pathSegmentPropertyName}{suffix};");
            WriteLine($"return new {returnType}() {{{{ {currentPathPropertyName} = parentPath; }}}};");
        }
        public override void WriteProperty(CodeProperty code)
        {
            //TODO: missing javadoc
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
                default:
                    var defaultValue = string.IsNullOrEmpty(code.DefaultValue) ? string.Empty : $" = {code.DefaultValue}";
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
            return (access == AccessModifier.Public ? "public" : (access == AccessModifier.Protected ? "protected" : "private"));
        }
    }
}
