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

            var derivedTypes = new List<string>{code.Inherits?.Name}
                                            .Union(code.Implements.Select(x => x.Name))
                                            .Where(x => x != null);
            var derivation = derivedTypes.Any() ? ": " +  derivedTypes.Select(x => x.ToFirstCharacterUpperCase()).Aggregate((x, y) => $"{x}, {y}") + " " : string.Empty;
            if(code.Parent is CodeClass parentClass)
                WriteShortDescription(parentClass.Description);
            WriteLine($"public class {code.Name.ToFirstCharacterUpperCase()} {derivation}{{");
            IncreaseIndent();
        }
        private void WriteShortDescription(string description) {
            if(!string.IsNullOrEmpty(description))
                WriteLine($"{docCommentPrefix}<summary>{description}</summary>");
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
        private const string parseNodeInterfaceName = "IParseNode";
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
            WriteShortDescription(code.Description);
            switch(code.PropertyKind) {
                case CodePropertyKind.RequestBuilder:
                    WriteLine($"{GetAccessModifier(code.Access)} {propertyType} {code.Name.ToFirstCharacterUpperCase()} {{ get =>");
                    IncreaseIndent();
                    AddRequestBuilderBody(propertyType);
                    DecreaseIndent();
                    WriteLine("}");
                break;
                case CodePropertyKind.Deserializer:
                    var parentClass = code.Parent as CodeClass;
                    var hideParentMember = (parentClass.StartBlock as CodeClass.Declaration).Inherits != null;
                    WriteLine($"{GetAccessModifier(code.Access)} {(hideParentMember ? "new " : string.Empty)}{code.Type.Name} {code.Name.ToFirstCharacterUpperCase()} => new Dictionary<string, Action<{parentClass.Name.ToFirstCharacterUpperCase()}, {parseNodeInterfaceName}>> {{");
                    IncreaseIndent();
                    foreach(var otherProp in parentClass
                                                    .InnerChildElements
                                                    .OfType<CodeProperty>()
                                                    .Where(x => x.PropertyKind == CodePropertyKind.Custom)) {
                        WriteLine("{");
                        IncreaseIndent();
                        WriteLine($"\"{otherProp.Name.ToFirstCharacterLowerCase()}\", (o,n) => {{ o.{otherProp.Name.ToFirstCharacterUpperCase()} = n.{GetDeserializationMethodName(otherProp.Type)}(); }}");
                        DecreaseIndent();
                        WriteLine("},");
                    }
                    DecreaseIndent();
                    WriteLine("};");
                break;
                default:
                    WriteLine($"{GetAccessModifier(code.Access)} {propertyType} {code.Name.ToFirstCharacterUpperCase()} {{ {simpleBody} }}{defaultValue}");
                break;
            }
        }
        private string GetDeserializationMethodName(CodeTypeBase propType) {
            var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
            var propertyType = TranslateType(propType.Name);
            if(propType is CodeType currentType) {
                if(isCollection)
                    if(currentType.TypeDefinition == null)
                        return $"GetCollectionOfPrimitiveValues<{propertyType}>().ToList";
                    else
                        return $"GetCollectionOfObjectValues<{propertyType}>().ToList";
                else if (currentType.TypeDefinition is CodeEnum enumType)
                    return $"GetEnumValue<{enumType.Name.ToFirstCharacterUpperCase()}>";
            }
            switch(propertyType) {
                case "string":
                case "bool":
                case "int":
                case "float":
                case "double":
                case "Guid":
                case "DateTimeOffset":
                    return $"Get{propertyType.ToFirstCharacterUpperCase()}Value";
                default:
                    return $"GetObjectValue<{propertyType.ToFirstCharacterUpperCase()}>";
            }
        }
        private readonly Func<CodeTypeBase, string, bool> shouldTypeHaveNullableMarker = (propType, propTypeName) => propType.IsNullable && (nullableTypes.Contains(propTypeName) || (propType is CodeType codeType && codeType.TypeDefinition is CodeEnum));
        private string GetSerializationMethodName(CodeTypeBase propType) {
            var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
            var propertyType = TranslateType(propType.Name);
            var nullableSuffix = shouldTypeHaveNullableMarker(propType, propertyType) ? nullableMarker : string.Empty;
            if(propType is CodeType currentType) {
                if(isCollection)
                    if(currentType.TypeDefinition == null)
                        return $"WriteCollectionOfPrimitiveValues<{propertyType}{nullableSuffix}>";
                    else
                        return $"WriteCollectionOfObjectValues<{propertyType}{nullableSuffix}>";
                else if (currentType.TypeDefinition is CodeEnum enumType)
                    return $"WriteEnumValue<{enumType.Name.ToFirstCharacterUpperCase()}>";
                
            }
            switch(propertyType) {
                case "string":
                case "bool":
                case "int":
                case "float":
                case "double":
                case "Guid":
                case "DateTimeOffset":
                    return $"Write{propertyType.ToFirstCharacterUpperCase()}Value";
                default:
                    return $"WriteObjectValue<{propertyType.ToFirstCharacterUpperCase()}>";
            }
        }
        private const string pathSegmentPropertyName = "PathSegment";
        private const string currentPathPropertyName = "CurrentPath";
        private const string httpCorePropertyName = "HttpCore";
        private void AddRequestBuilderBody(string returnType, string suffix = default, string prefix = default) {
            WriteLine($"{prefix}new {returnType} {{ {httpCorePropertyName} = {httpCorePropertyName}, {SerializerFactoryPropertyName} = {SerializerFactoryPropertyName}, {currentPathPropertyName} = {currentPathPropertyName} + {pathSegmentPropertyName} {suffix}}};");
        }
        public override void WriteIndexer(CodeIndexer code)
        {
            var returnType = GetTypeString(code.ReturnType);
            WriteShortDescription(code.Description);
            WriteLine($"public {returnType} this[{GetTypeString(code.IndexType)} position] {{ get {{");
            IncreaseIndent();
            AddRequestBuilderBody(returnType, " + \"/\" + position", "return ");
            DecreaseIndent();
            WriteLine("} }");
        }
        private const string SerializerFactoryPropertyName = "SerializerFactory";
        private const string StreamTypeName = "stream";
        private const string VoidTypeName = "void";
        private const string docCommentPrefix = "/// ";
        private void WriteMethodDocumentation(CodeMethod code) {
            var isDescriptionPresent = !string.IsNullOrEmpty(code.Description);
            var parametersWithDescription = code.Parameters.Where(x => !string.IsNullOrEmpty(code.Description));
            if (isDescriptionPresent || parametersWithDescription.Any()) {
                WriteLine($"{docCommentPrefix}<summary>");
                if(isDescriptionPresent)
                    WriteLine($"{docCommentPrefix}{code.Description}");
                foreach(var paramWithDescription in parametersWithDescription)
                    WriteLine($"{docCommentPrefix}<param name=\"{paramWithDescription.Name}\">{paramWithDescription.Description}</param>");
                WriteLine($"{docCommentPrefix}</summary>");
            }
        }
        private void WriteMethodPrototype(CodeMethod code, string returnType, bool shouldHide, bool isVoid) {
            var staticModifier = code.IsStatic ? "static " : string.Empty;
            var hideModifier = shouldHide ? "new " : string.Empty;
            var genericTypePrefix = isVoid ? string.Empty : "<";
            var genricTypeSuffix = code.IsAsync && !isVoid ? ">": string.Empty;
            // TODO: Task type should be moved into the refiner
            var completeReturnType = $"{(code.IsAsync ? "async Task" + genericTypePrefix : string.Empty)}{(code.IsAsync && isVoid ? string.Empty : returnType)}{genricTypeSuffix}";
            var parameters = string.Join(", ", code.Parameters.Select(p=> GetParameterSignature(p)).ToList());
            WriteLine($"{GetAccessModifier(code.Access)} {staticModifier}{hideModifier}{completeReturnType} {code.Name}({parameters}) {{");
        }
        public override void WriteMethod(CodeMethod code)
        {
            var returnType = GetTypeString(code.ReturnType);
            var parentClass = code.Parent as CodeClass;
            var shouldHide = (parentClass.StartBlock as CodeClass.Declaration).Inherits != null && code.MethodKind == CodeMethodKind.Serializer;
            var isVoid = VoidTypeName.Equals(returnType, StringComparison.InvariantCultureIgnoreCase);
            var isStream = StreamTypeName.Equals(returnType, StringComparison.InvariantCultureIgnoreCase);
            WriteMethodDocumentation(code);
            WriteMethodPrototype(code, returnType, shouldHide, isVoid);
            IncreaseIndent();
            var requestBodyParam = code.Parameters.OfKind(CodeParameterKind.RequestBody);
            var queryStringParam = code.Parameters.OfKind(CodeParameterKind.QueryParameter);
            var headersParam = code.Parameters.OfKind(CodeParameterKind.Headers);
            switch(code.MethodKind) {
                case CodeMethodKind.Serializer:
                    if(shouldHide)
                        WriteLine("base.Serialize(writer);");
                    foreach(var otherProp in parentClass
                                                    .InnerChildElements
                                                    .OfType<CodeProperty>()
                                                    .Where(x => x.PropertyKind == CodePropertyKind.Custom)) {
                        WriteLine($"writer.{GetSerializationMethodName(otherProp.Type)}(\"{otherProp.Name.ToFirstCharacterLowerCase()}\", {otherProp.Name.ToFirstCharacterUpperCase()});");
                    }
                break;
                case CodeMethodKind.RequestGenerator:
                    var operationName = code.HttpMethod?.ToString();
                    WriteLine("var requestInfo = new RequestInfo {");
                    IncreaseIndent();
                    WriteLines($"HttpMethod = HttpMethod.{operationName?.ToUpperInvariant()},",
                               $"URI = new Uri({currentPathPropertyName} + {pathSegmentPropertyName}),");
                    DecreaseIndent();
                    WriteLine("};");
                    if(requestBodyParam != null) {
                        if(requestBodyParam.Type.Name.Equals(StreamTypeName, StringComparison.InvariantCultureIgnoreCase))
                            WriteLine($"requestInfo.SetStreamContent({requestBodyParam.Name});");
                        else
                            WriteLine($"requestInfo.SetJsonContentFromParsable({requestBodyParam.Name}, {SerializerFactoryPropertyName});"); //TODO we're making a big assumption here that everything will be json
                    }
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
                                $"{(isVoid ? string.Empty : "return ")}await HttpCore.{GetSendRequestMethodName(isVoid, isStream, returnType)}(requestInfo, responseHandler);");
                break;
                default:
                    WriteLine("return null;");
                break;
            }
            DecreaseIndent();
            WriteLine("}");

        }
        private static string GetSendRequestMethodName(bool isVoid, bool isStream, string returnType) {
            if(isVoid) return "SendNoContentAsync";
            else if(isStream) return $"SendPrimitiveAsync<{returnType}>";
            else return $"SendAsync<{returnType}>";
        }

        public override void WriteType(CodeType code)
        {
            Write(GetTypeString(code), includeIndent: false);

        }
        private static string[] nullableTypes = { "int", "bool", "float", "double", "decimal", "Guid", "DateTimeOffset" };
        private const string nullableMarker = "?";
        public override string GetTypeString(CodeTypeBase code)
        {
            if(code is CodeUnionType) 
                throw new InvalidOperationException($"CSharp does not support union types, the union type {code.Name} should have been filtered out by the refiner");
            else if (code is CodeType currentType) {
                var typeName = TranslateType(currentType.Name);
                var nullableSuffix = shouldTypeHaveNullableMarker(code, typeName) ? nullableMarker : string.Empty;
                var collectionPrefix = currentType.CollectionKind == CodeType.CodeTypeCollectionKind.Complex ? "List<" : string.Empty;
                var collectionSuffix = currentType.CollectionKind == CodeType.CodeTypeCollectionKind.Complex ? ">" : 
                                            (currentType.CollectionKind == CodeType.CodeTypeCollectionKind.Array ? "[]" : string.Empty);
                if (currentType.ActionOf)
                    return $"Action<{collectionPrefix}{typeName}{nullableSuffix}{collectionSuffix}>";
                else
                    return $"{collectionPrefix}{typeName}{nullableSuffix}{collectionSuffix}";
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
                case "void": return "void";
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
            switch(access) {
                case AccessModifier.Public: return "public";
                case AccessModifier.Protected: return "protected";
                default: return "private";
            }
        }

        private readonly Func<int, string> GetEnumIndex = (idx) => (idx == 0 ? 0 : 2^(idx -1)).ToString();
        public override void WriteEnum(CodeEnum code)
        {
            var codeNamespace = code?.Parent as CodeNamespace;
            if(codeNamespace != null) {
                WriteLine($"namespace {codeNamespace.Name} {{");
                IncreaseIndent();
            }
            if(code.Flags)
                WriteLine("[Flags]");
            WriteShortDescription(code.Description);
            WriteLine($"public enum {code.Name.ToFirstCharacterUpperCase()} {{"); //TODO docs
            IncreaseIndent();
            WriteLines(code.Options
                            .Select(x => x.ToFirstCharacterUpperCase())
                            .Select((x, idx) => $"{x}{(code.Flags ? " = " + GetEnumIndex(idx) : string.Empty)},")
                            .ToArray());
            DecreaseIndent();
            WriteLine("}");
            if(codeNamespace != null) {
                DecreaseIndent();
                WriteLine("}");
            }
        }
    }
}
