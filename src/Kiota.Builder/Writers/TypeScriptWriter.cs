using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kiota.Builder.Extensions;

namespace Kiota.Builder
{
    public class TypeScriptWriter : LanguageWriter
    {
        public TypeScriptWriter(string rootPath, string clientNamespaceName)
        {
            segmenter = new TypeScriptPathSegmenter(rootPath,clientNamespaceName);
        }
        private readonly IPathSegmenter segmenter;
        public override IPathSegmenter PathSegmenter => segmenter;
        public override string GetParameterSignature(CodeParameter parameter)
        {
            return $"{parameter.Name}{(parameter.Optional ? "?" : string.Empty)}: {GetTypeString(parameter.Type)}{(parameter.Optional ? " | undefined": string.Empty)}";
        }

        public override string GetTypeString(CodeTypeBase code)
        {
            var collectionSuffix = code.CollectionKind == CodeType.CodeTypeCollectionKind.None ? string.Empty : "[]";
            if(code is CodeUnionType currentUnion && currentUnion.Types.Any())
                return currentUnion.Types.Select(x => GetTypeString(x)).Aggregate((x, y) => $"{x} | {y}") + collectionSuffix;
            else if(code is CodeType currentType) {
                var typeName = TranslateType(currentType.Name);
                if (code.ActionOf)
                {
                    IncreaseIndent(4);
                    var childElements = (currentType?.TypeDefinition as CodeClass)
                                                ?.InnerChildElements
                                                ?.OfType<CodeProperty>()
                                                ?.Select(x => $"{x.Name}?: {GetTypeString(x.Type)}");
                    var innerDeclaration = childElements?.Any() ?? false ? 
                                                    NewLine +
                                                    GetIndent() +
                                                    childElements
                                                    .Aggregate((x, y) => $"{x};{NewLine}{GetIndent()}{y}")
                                                    .Replace(';', ',') +
                                                    NewLine +
                                                    GetIndent()
                                                : string.Empty;
                    DecreaseIndent();
                    if(string.IsNullOrEmpty(innerDeclaration))
                        return "object";
                    else
                        return $"{{{innerDeclaration}}}";
                }
                else
                    return $"{typeName}{collectionSuffix}";
            }
            else throw new InvalidOperationException($"type of type {code.GetType()} is unknown");
        }

        public override string TranslateType(string typeName)
        {
            switch (typeName)
            {//TODO we're probably missing a bunch of type mappings
                case "integer": return "number";
                case "string": // little casing hack
                case "object":
                case "boolean":
                case "void":
                    return typeName; 
                default: return typeName.ToFirstCharacterUpperCase() ?? "object";
            } // string, boolean, object : same casing
        }

        public override void WriteCodeClassDeclaration(CodeClass.Declaration code)
        {
            foreach (var codeUsing in code.Usings
                                        .Where(x => x.Declaration?.IsExternal ?? false)
                                        .GroupBy(x => x.Declaration?.Name)
                                        .OrderBy(x => x.Key))
            {
                WriteLine($"import {{{codeUsing.Select(x => x.Name).Distinct().Aggregate((x,y) => x + ", " + y)}}} from '{codeUsing.Key}';");
            }
            foreach (var codeUsing in code.Usings
                                        .Where(x => (!x.Declaration?.IsExternal) ?? true)
                                        .Where(x => !x.Declaration.Name.Equals(code.Name, StringComparison.InvariantCultureIgnoreCase))
                                        .Select(x => {
                                            var relativeImportPath = GetRelativeImportPathForUsing(x, code.GetImmediateParentOfType<CodeNamespace>());
                                            return new {
                                                sourceSymbol = $"{relativeImportPath}{(string.IsNullOrEmpty(relativeImportPath) ? x.Name : x.Declaration.Name.ToFirstCharacterLowerCase())}",
                                                importSymbol = $"{x.Declaration?.Name?.ToFirstCharacterUpperCase() ?? x.Name}",
                                            };
                                        })
                                        .GroupBy(x => x.sourceSymbol)
                                        .OrderBy(x => x.Key))
            {
                                                    
                WriteLine($"import {{{codeUsing.Select(x => x.importSymbol).Distinct().Aggregate((x,y) => x + ", " + y)}}} from '{codeUsing.Key}';");
            }
            WriteLine();
            var derivation = (code.Inherits == null ? string.Empty : $" extends {code.Inherits.Name.ToFirstCharacterUpperCase()}") +
                            (!code.Implements.Any() ? string.Empty : $" implements {code.Implements.Select(x => x.Name).Aggregate((x,y) => x + " ," + y)}");
            WriteShortDescription((code.Parent as CodeClass).Description);
            WriteLine($"export class {code.Name.ToFirstCharacterUpperCase()}{derivation} {{");
            IncreaseIndent();
        }
        private static string GetRelativeImportPathForUsing(CodeUsing codeUsing, CodeNamespace currentNamespace) {
            if(codeUsing.Declaration == null)
                return string.Empty;//it's an external import, add nothing
            var typeDef = codeUsing.Declaration.TypeDefinition;
            if(typeDef == null) {
                // sometimes the definition is not attached to the declaration because it's generated after the fact, we need to search it
                typeDef = currentNamespace
                    .GetRootNamespace()
                    .GetChildElementOfType<CodeClass>(x => x.Name.Equals(codeUsing.Declaration.Name));
            }

            if(typeDef == null)
                return "./"; // it's relative to the folder, with no declaration (default failsafe)
            else
                return GetImportRelativePathFromNamespaces(currentNamespace, 
                                                        typeDef.GetImmediateParentOfType<CodeNamespace>());
        }
        private static char namespaceNameSeparator = '.';
        private static string GetImportRelativePathFromNamespaces(CodeNamespace currentNamespace, CodeNamespace importNamespace) {
            if(currentNamespace == null)
                throw new ArgumentNullException(nameof(currentNamespace));
            else if (importNamespace == null)
                throw new ArgumentNullException(nameof(importNamespace));
            else if(currentNamespace.Name.Equals(importNamespace.Name, StringComparison.InvariantCultureIgnoreCase)) // we're in the same namespace
                return "./";
            else {
                var currentNamespaceSegements = currentNamespace
                                    .Name
                                    .Split(namespaceNameSeparator, StringSplitOptions.RemoveEmptyEntries);
                var importNamespaceSegments = importNamespace
                                    .Name
                                    .Split(namespaceNameSeparator, StringSplitOptions.RemoveEmptyEntries);
                var importNamespaceSegmentsCount = importNamespaceSegments.Length;
                var currentNamespaceSegementsCount = currentNamespaceSegements.Length;
                var deeperMostSegmentIndex = 0;
                while(deeperMostSegmentIndex < Math.Min(importNamespaceSegmentsCount, currentNamespaceSegementsCount)) {
                    if(currentNamespaceSegements.ElementAt(deeperMostSegmentIndex).Equals(importNamespaceSegments.ElementAt(deeperMostSegmentIndex), StringComparison.InvariantCultureIgnoreCase))
                        deeperMostSegmentIndex++;
                    else
                        break;
                }
                if (deeperMostSegmentIndex == currentNamespaceSegementsCount) { // we're in a parent namespace and need to import with a relative path
                    return "./" + GetRemainingImportPath(importNamespaceSegments.Skip(deeperMostSegmentIndex));
                } else { // we're in a sub namespace and need to go "up" with dot dots
                    var upMoves = currentNamespaceSegementsCount - deeperMostSegmentIndex;
                    var upMovesBuilder = new StringBuilder();
                    for(var i = 0; i < upMoves; i++)
                        upMovesBuilder.Append("../");
                    return upMovesBuilder.ToString() + GetRemainingImportPath(importNamespaceSegments.Skip(deeperMostSegmentIndex));
                }
            }
        }
        private static string GetRemainingImportPath(IEnumerable<string> remainingSegments) {
            if(remainingSegments.Any())
                return remainingSegments.Select(x => x.ToFirstCharacterLowerCase()).Aggregate((x, y) => $"{x}/{y}") + '/';
            else
                return string.Empty;
        }

        public override void WriteCodeClassEnd(CodeClass.End code)
        {
            DecreaseIndent();
            WriteLine("}");
        }

        public override void WriteIndexer(CodeIndexer code)
        {
            throw new InvalidOperationException("indexers are not supported in TypeScript, the refiner should have removes those");
        }
        private string GetDeserializationMethodName(CodeTypeBase propType) {
            var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
            var propertyType = TranslateType(propType.Name);
            if(propType is CodeType currentType) {
                if(isCollection)
                    if(currentType.TypeDefinition == null)
                        return $"getCollectionOfPrimitiveValues<{propertyType.ToFirstCharacterLowerCase()}>()";
                    else
                        return $"getCollectionOfObjectValues<{propertyType.ToFirstCharacterUpperCase()}>({propertyType.ToFirstCharacterUpperCase()})";
                else if(currentType.TypeDefinition is CodeEnum currentEnum)
                    return $"getEnumValue{(currentEnum.Flags ? "s" : string.Empty)}<{currentEnum.Name.ToFirstCharacterUpperCase()}>({propertyType.ToFirstCharacterUpperCase()})";
            }
            switch(propertyType) {
                case "string":
                case "boolean":
                case "number":
                case "Guid":
                case "DateTimeOffset":
                    return $"get{propertyType.ToFirstCharacterUpperCase()}Value()";
                default:
                    return $"getObjectValue<{propertyType.ToFirstCharacterUpperCase()}>({propertyType.ToFirstCharacterUpperCase()})";
            }
        }
        private string GetSerializationMethodName(CodeTypeBase propType) {
            var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
            var propertyType = TranslateType(propType.Name);
            if(propType is CodeType currentType) {
                if(isCollection)
                    if(currentType.TypeDefinition == null)
                        return $"writeCollectionOfPrimitiveValues<{propertyType.ToFirstCharacterLowerCase()}>";
                    else
                        return $"writeCollectionOfObjectValues<{propertyType.ToFirstCharacterUpperCase()}>";
                else if(currentType.TypeDefinition is CodeEnum currentEnum)
                    return $"writeEnumValue<{currentEnum.Name.ToFirstCharacterUpperCase()}>";
            }
            switch(propertyType) {
                case "string":
                case "boolean":
                case "number":
                case "Guid":
                case "DateTimeOffset":
                    return $"write{propertyType.ToFirstCharacterUpperCase()}Value";
                default:
                    return $"writeObjectValue<{propertyType.ToFirstCharacterUpperCase()}>";
            }
        }
        private const string currentPathPropertyName = "currentPath";
        private const string pathSegmentPropertyName = "pathSegment";
        private const string httpCorePropertyName = "httpCore";
        private const string SerializerFactoryPropertyName = "serializerFactory";
        private void AddRequestBuilderBody(string returnType, string suffix = default) {
            WriteLines($"const builder = new {returnType}();",
                    $"builder.{currentPathPropertyName} = (this.{currentPathPropertyName} ?? '') + this.{pathSegmentPropertyName}{suffix};",
                    $"builder.{httpCorePropertyName} = this.{httpCorePropertyName};",
                    $"builder.{SerializerFactoryPropertyName} = this.{SerializerFactoryPropertyName};",
                    "return builder;");
        }
        private const string StreamType = "ReadableStream";
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
                    WriteLine($"{docCommentPrefix}@returns a Promise of {code.ReturnType.Name}");
                else
                    WriteLine($"{docCommentPrefix}@returns a {code.ReturnType.Name}");
                WriteLine(docCommentEnd);
            }
        }
        private static string RemoveInvalidDescriptionCharacters(string originalDescription) => originalDescription?.Replace("\\", "/");
        private void WriteShortDescription(string description) {
            if(!string.IsNullOrEmpty(description))
                WriteLine($"{docCommentStart} {RemoveInvalidDescriptionCharacters(description)} {docCommentEnd}");
        }
        private void WriteMethodPrototype(CodeMethod code, string returnType, bool isVoid) {
            var accessModifier = GetAccessModifier(code.Access);
            var methodName = code.Name.ToFirstCharacterLowerCase();
            var asyncPrefix = code.IsAsync && code.MethodKind != CodeMethodKind.RequestExecutor ? "async ": string.Empty;
            var parameters = string.Join(", ", code.Parameters.Select(p=> GetParameterSignature(p)).ToList());
            var asyncReturnTypePrefix = code.IsAsync ? "Promise<": string.Empty;
            var asyncReturnTypeSuffix = code.IsAsync ? ">": string.Empty;
            var nullableSuffix = code.ReturnType.IsNullable && !isVoid ? " | undefined" : string.Empty;
            WriteLine($"{accessModifier} {methodName} {asyncPrefix}({parameters}) : {asyncReturnTypePrefix}{returnType}{nullableSuffix}{asyncReturnTypeSuffix} {{");
        }
        public override void WriteMethod(CodeMethod code)
        {
            var returnType = GetTypeString(code.ReturnType);
            var isVoid = "void".Equals(returnType, StringComparison.InvariantCultureIgnoreCase);
            WriteMethodDocumentation(code);
            WriteMethodPrototype(code, returnType, isVoid);
            IncreaseIndent();
            var parentClass = code.Parent as CodeClass;
            var shouldHide = (parentClass.StartBlock as CodeClass.Declaration).Inherits != null && code.MethodKind == CodeMethodKind.Serializer;
            var requestBodyParam = code.Parameters.OfKind(CodeParameterKind.RequestBody);
            var queryStringParam = code.Parameters.OfKind(CodeParameterKind.QueryParameter);
            var headersParam = code.Parameters.OfKind(CodeParameterKind.Headers);
            switch(code.MethodKind) {
                case CodeMethodKind.IndexerBackwardCompatibility:
                    var pathSegment = code.GenerationProperties.ContainsKey(pathSegmentPropertyName) ? code.GenerationProperties[pathSegmentPropertyName] as string : string.Empty;
                    AddRequestBuilderBody(returnType, $" + \"/{(string.IsNullOrEmpty(pathSegment) ? string.Empty : pathSegment + "/" )}\" + id");
                    break;
                case CodeMethodKind.DeserializerBackwardCompatibility:
                    var inherits = (parentClass.StartBlock as CodeClass.Declaration).Inherits != null;
                    WriteLine($"return new Map<string, (item: {parentClass.Name.ToFirstCharacterUpperCase()}, node: ParseNode) => void>([{(inherits ? $"...super.{code.Name.ToFirstCharacterLowerCase()}()," : string.Empty)}");
                    IncreaseIndent();
                    foreach(var otherProp in parentClass
                                                    .InnerChildElements
                                                    .OfType<CodeProperty>()
                                                    .Where(x => x.PropertyKind == CodePropertyKind.Custom)) {
                        WriteLine($"[\"{otherProp.Name.ToFirstCharacterLowerCase()}\", (o, n) => {{ o.{otherProp.Name.ToFirstCharacterLowerCase()} = n.{GetDeserializationMethodName(otherProp.Type)}; }}],");
                    }
                    DecreaseIndent();
                    WriteLine("]);");
                    break;
                case CodeMethodKind.Serializer:
                    if(shouldHide)
                        WriteLine("super.serialize(writer);");
                    foreach(var otherProp in parentClass
                                                    .InnerChildElements
                                                    .OfType<CodeProperty>()
                                                    .Where(x => x.PropertyKind == CodePropertyKind.Custom)) {
                        WriteLine($"writer.{GetSerializationMethodName(otherProp.Type)}(\"{otherProp.Name.ToFirstCharacterLowerCase()}\", this.{otherProp.Name.ToFirstCharacterLowerCase()});");
                    }
                    break;
                case CodeMethodKind.RequestGenerator:
                    WriteLines("const requestInfo = new RequestInfo();",
                                $"requestInfo.URI = (this.{currentPathPropertyName} ?? '') + this.{pathSegmentPropertyName},",
                                $"requestInfo.httpMethod = HttpMethod.{code.HttpMethod.ToString().ToUpperInvariant()},");
                    if(headersParam != null)
                        WriteLine($"{headersParam.Name} && requestInfo.setHeadersFromRawObject(h);");
                    if(queryStringParam != null)
                        WriteLines($"{queryStringParam.Name} && requestInfo.setQueryStringParametersFromRawObject(q);");
                    if(requestBodyParam != null) {
                        if(requestBodyParam.Type.Name.Equals(StreamType, StringComparison.InvariantCultureIgnoreCase))
                            WriteLine($"requestInfo.setStreamContent({requestBodyParam.Name});");
                        else
                            WriteLine($"requestInfo.setJsonContentFromParsable({requestBodyParam.Name}, this.{SerializerFactoryPropertyName});"); //TODO we're making a big assumption here that everything will be json
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
                    WriteLine($"const requestInfo = this.{generatorMethodName}(");
                    var requestInfoParameters = new List<string> { requestBodyParam?.Name, queryStringParam?.Name, headersParam?.Name }.Where(x => x != null);
                    if(requestInfoParameters.Any()) {
                        IncreaseIndent();
                        WriteLine(requestInfoParameters.Aggregate((x,y) => $"{x}, {y}"));
                        DecreaseIndent();
                    }
                    WriteLine(");");
                    var isStream = StreamType.Equals(returnType, StringComparison.InvariantCultureIgnoreCase);
                    var genericTypeForSendMethod = GetSendRequestMethodName(isVoid, isStream, returnType);
                    var newFactoryParameter = GetTypeFactory(isVoid, isStream, returnType);
                    WriteLine($"return this.httpCore?.{genericTypeForSendMethod}(requestInfo,{newFactoryParameter} responseHandler) ?? Promise.reject(new Error('http core is null'));");
                    break;
                default:
                    WriteLine($"return {(code.IsAsync ? "Promise.resolve(" : string.Empty)}{(code.ReturnType.Name.Equals("string") ? "''" : "{} as any")}{(code.IsAsync ? ")" : string.Empty)};");
                    break;
            }
            DecreaseIndent();
            WriteLine("};");
        }
        private static string GetTypeFactory(bool isVoid, bool isStream, string returnType) {
            if(isVoid) return string.Empty;
            else if(isStream) return $" \"{returnType}\",";
            else return $" {returnType},";
        }
        private static string GetSendRequestMethodName(bool isVoid, bool isStream, string returnType) {
            if(isVoid) return "sendNoResponseContentAsync";
            else if(isStream) return $"sendPrimitiveAsync<{returnType}>";
            else return $"sendAsync<{returnType}>";
        }
        public override void WriteProperty(CodeProperty code)
        {
            var returnType = GetTypeString(code.Type);
            var isFlagEnum = code.Type is CodeType currentType && currentType.TypeDefinition is CodeEnum currentEnum && currentEnum.Flags;
            WriteShortDescription(code.Description);
            switch(code.PropertyKind) {
                case CodePropertyKind.Deserializer:
                    throw new InvalidOperationException("typescript uses methods for the deserializers and this property should have been converted to a method");
                case CodePropertyKind.RequestBuilder:
                    WriteLine($"{GetAccessModifier(code.Access)} get {code.Name.ToFirstCharacterLowerCase()}(): {returnType} {{");
                    IncreaseIndent();
                    AddRequestBuilderBody(returnType);
                    DecreaseIndent();
                    WriteLine("}");
                break;
                default:
                    var defaultValue = string.IsNullOrEmpty(code.DefaultValue) ? string.Empty : $" = {code.DefaultValue}";
                    var singleLiner = code.PropertyKind == CodePropertyKind.Custom;
                    WriteLine($"{GetAccessModifier(code.Access)}{(code.ReadOnly ? " readonly ": " ")}{code.Name.ToFirstCharacterLowerCase()}{(code.Type.IsNullable ? "?" : string.Empty)}: {returnType}{(isFlagEnum ? "[]" : string.Empty)}{(code.Type.IsNullable ? " | undefined" : string.Empty)}{defaultValue}{(singleLiner ? ";" : string.Empty)}");
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
            WriteShortDescription(code.Description);
            WriteLine($"export enum {code.Name.ToFirstCharacterUpperCase()} {{");
            IncreaseIndent();
            code.Options.ForEach(x => WriteLine($"{x.ToFirstCharacterUpperCase()} = \"{x}\","));
            DecreaseIndent();
            WriteLine("}");
        }
    }
}
