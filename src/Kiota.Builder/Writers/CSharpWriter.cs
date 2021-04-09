using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder
{
    public class CSharpWriter : LanguageWriter
    {
        public CSharpWriter(string rootPath, string clientNamespaceName)
        {
            segmenter = new CSharpPathSegmenter(rootPath, clientNamespaceName);
        }
        private readonly IPathSegmenter segmenter;
        public override IPathSegmenter PathSegmenter => segmenter;

        public override void WriteCodeClassDeclaration(CodeClass.Declaration code)
        {
            foreach (var codeUsing in code.Usings.Where(x => !string.IsNullOrEmpty(x.Name) && x.Declaration == null)
                                                .Select(x => x.Name)
                                                .Distinct()
                                                .OrderBy(x => x))
                WriteLine($"using {codeUsing};");
            foreach (var codeUsing in code.Usings.Where(x => !string.IsNullOrEmpty(x.Name) && x.Declaration != null)
                                                .Select(x => x.Name)
                                                .Distinct()
                                                .OrderBy(x => x))
                WriteLine($"using {codeUsing.Split('.').Select(x => x.ToFirstCharacterUpperCase()).Aggregate((x,y) => x + "." + y)};");
            if(code?.Parent?.Parent is CodeNamespace) {
                WriteLine($"namespace {code.Parent.Parent.Name} {{");
                IncreaseIndent();
            }

            var derivedTypes = code.Implements.Select(x => x.Name)
                                            .Union(new List<string>{code.Inherits?.Name})
                                            .Where(x => x != null);
            var derivation = derivedTypes.Any() ? ": " +  derivedTypes.Select(x => x.ToFirstCharacterUpperCase()).Aggregate((x, y) => $"{x}, {y}") + " " : string.Empty;
            WriteLine($"public class {code.Name.ToFirstCharacterUpperCase()} {derivation}{{");
            IncreaseIndent();
        }

        public override void WriteCodeClassEnd(CodeClass.End code)
        {
            DecreaseIndent();
            WriteLine("}");
            if(code?.Parent?.Parent is CodeNamespace) {
                DecreaseIndent();
                WriteLine("}");
            }
        }

        public override void WriteProperty(CodeProperty code)
        {
            var simpleBody = "get;";
            if (!code.ReadOnly)
            {
                simpleBody = "get; set;";
            }
            var defaultValue = string.Empty;
            if (code.DefaultValue != null)
            {
                defaultValue = " = " + code.DefaultValue + ";";
            }
            var propertyType = GetTypeString(code.Type);
            switch(code.PropertyKind) {
                case CodePropertyKind.RequestBuilder:
                    WriteLine($"{GetAccessModifier(code.Access)} {propertyType} {code.Name.ToFirstCharacterUpperCase()} {{ get =>");
                    IncreaseIndent();
                    AddRequestBuilderBody(propertyType);
                    DecreaseIndent();
                    WriteLine("}");
                break;
                default:
                    WriteLine($"{GetAccessModifier(code.Access)} {propertyType} {code.Name.ToFirstCharacterUpperCase()} {{ {simpleBody} }}{defaultValue}");
                break;
            }
        }
        private const string pathSegmentPropertyName = "PathSegment";
        private const string currentPathPropertyName = "CurrentPath";
        private const string httpCorePropertyName = "HttpCore";
        private void AddRequestBuilderBody(string returnType, string suffix = default, string prefix = default) {
            WriteLine($"{prefix}new {returnType} {{ {httpCorePropertyName} = {httpCorePropertyName}, {currentPathPropertyName} = {currentPathPropertyName} + {pathSegmentPropertyName} {suffix}}};");
        }
        public override void WriteIndexer(CodeIndexer code)
        {
            var returnType = GetTypeString(code.ReturnType);
            WriteLine($"public {returnType} this[{GetTypeString(code.IndexType)} position] {{ get {{");
            IncreaseIndent();
            AddRequestBuilderBody(returnType, " + \"/\" + position", "return ");
            DecreaseIndent();
            WriteLine("} }");
        }

        public override void WriteMethod(CodeMethod code)
        {
            var staticModifier = code.IsStatic ? "static " : string.Empty;
            var returnType = GetTypeString(code.ReturnType);
            // Task type should be moved into the refiner
            WriteLine($"{GetAccessModifier(code.Access)} {staticModifier}{(code.IsAsync ? "async Task<": string.Empty)}{returnType}{( code.IsAsync ? ">": string.Empty)} {code.Name}({string.Join(", ", code.Parameters.Select(p=> GetParameterSignature(p)).ToList())}) {{");
            IncreaseIndent();
            var requestBodyParam = code.Parameters.FirstOrDefault(x => x.ParameterKind == CodeParameterKind.RequestBody);
            var queryStringParam = code.Parameters.FirstOrDefault(x => x.ParameterKind == CodeParameterKind.QueryParameter);
            var headersParam = code.Parameters.FirstOrDefault(x => x.ParameterKind == CodeParameterKind.Headers);
            switch(code.MethodKind) {
                case CodeMethodKind.RequestGenerator:
                    var operationName = code.HttpMethod?.ToString();
                    WriteLine("var requestInfo = new RequestInfo {");
                    IncreaseIndent();
                    WriteLines($"HttpMethod = HttpMethod.{operationName?.ToUpperInvariant()},",
                               $"URI = new Uri({currentPathPropertyName} + {pathSegmentPropertyName}),");
                    if(requestBodyParam != null)
                        WriteLine($"Content = {requestBodyParam.Name} as object as Stream"); //TODO remove cast and call serialization once in place
                    DecreaseIndent();
                    WriteLine("};");
                    if(queryStringParam != null) {
                        WriteLine($"if ({queryStringParam.Name} != null) {{");
                        IncreaseIndent();
                        WriteLines($"var qParams = new {operationName?.ToFirstCharacterUpperCase()}QueryParameters();",
                                    $"{queryStringParam.Name}.Invoke(qParams);",
                                    "qParams.AddQueryParameters(requestInfo.QueryParameters);");
                        DecreaseIndent();
                        WriteLine("}");
                    }
                    if(headersParam != null) {
                        WriteLines($"{headersParam.Name}?.Invoke(requestInfo.Headers);",
                                "return requestInfo;");
                    }
                    break;
                case CodeMethodKind.RequestExecutor:
                    var generatorMethodName = (code.Parent as CodeClass)
                                                .InnerChildElements
                                                .OfType<CodeMethod>()
                                                .FirstOrDefault(x => x.MethodKind == CodeMethodKind.RequestGenerator && x.HttpMethod == code.HttpMethod)
                                                ?.Name;
                    WriteLine($"var requestInfo = {generatorMethodName}(");
                    IncreaseIndent();
                    WriteLine(new List<string> { requestBodyParam?.Name, queryStringParam?.Name, headersParam?.Name }.Where(x => x != null).Aggregate((x,y) => $"{x}, {y}"));
                    DecreaseIndent();
                    WriteLines(");",
                                $"return await HttpCore.SendAsync<{returnType}>(requestInfo, responseHandler);");
                break;
                default:
                    WriteLine("return null;");
                break;
            }
            DecreaseIndent();
            WriteLine("}");

        }

        public override void WriteType(CodeType code)
        {
            Write(GetTypeString(code), includeIndent: false);

        }

        public override string GetTypeString(CodeTypeBase code)
        {
            if(code is CodeUnionType) 
                throw new InvalidOperationException($"CSharp does not support union types, the union type {code.Name} should have been filtered out by the refiner");
            else if (code is CodeType currentType) {
                var typeName = TranslateType(currentType.Name);
                var collectionPrefix = currentType.CollectionKind == CodeType.CodeTypeCollectionKind.Complex ? "List<" : string.Empty;
                var collectionSuffix = currentType.CollectionKind == CodeType.CodeTypeCollectionKind.Complex ? ">" : 
                                            (currentType.CollectionKind == CodeType.CodeTypeCollectionKind.Array ? "[]" : string.Empty);
                if (currentType.ActionOf)
                    return $"Action<{collectionPrefix}{typeName}{collectionSuffix}>";
                else
                    return $"{collectionPrefix}{typeName}{collectionSuffix}";
            }
            else throw new InvalidOperationException($"type of type {code.GetType()} is unknown");
        }

        public override string TranslateType(string typeName)
        {
            switch (typeName)
            {
                case "integer": return "int";
                case "boolean": return "bool";
                case "string": return "string"; // little casing hack
                case "object": return "object";
                default: return typeName?.ToFirstCharacterUpperCase() ?? "object";
            }
        }

        public override string GetParameterSignature(CodeParameter parameter)
        {
            var parameterType = GetTypeString(parameter.Type);
            return $"{parameterType} {parameter.Name}{(parameter.Optional ? $" = default": string.Empty)}";
        }

        public override string GetAccessModifier(AccessModifier access)
        {
            return (access == AccessModifier.Public ? "public" : (access == AccessModifier.Protected ? "protected" : "private"));
        }
    }
}
