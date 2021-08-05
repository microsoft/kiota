using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers.Extensions;

namespace Kiota.Builder.Writers.Go {
    public class CodeMethodWriter : BaseElementWriter<CodeMethod, GoConventionService>
    {
        private readonly bool _usesBackingStore;
        public CodeMethodWriter(GoConventionService conventionService, bool usesBackingStore) : base(conventionService){
            _usesBackingStore = usesBackingStore;
        }
        public override void WriteCodeElement(CodeMethod codeElement, LanguageWriter writer)
        {
            if(codeElement == null) throw new ArgumentNullException(nameof(codeElement));
            if(codeElement.ReturnType == null) throw new InvalidOperationException($"{nameof(codeElement.ReturnType)} should not be null");
            if(writer == null) throw new ArgumentNullException(nameof(writer));
            if(!(codeElement.Parent is CodeClass)) throw new InvalidOperationException("the parent of a method should be a class");
            
            var parentClass = codeElement.Parent as CodeClass;
            var returnType = conventions.GetTypeString(codeElement.ReturnType, parentClass);
            WriteMethodPrototype(codeElement, writer, returnType, parentClass);
            writer.IncreaseIndent();
            var requestBodyParam = codeElement.Parameters.OfKind(CodeParameterKind.RequestBody);
            var queryStringParam = codeElement.Parameters.OfKind(CodeParameterKind.QueryParameter);
            var headersParam = codeElement.Parameters.OfKind(CodeParameterKind.Headers);
            var optionsParam = codeElement.Parameters.OfKind(CodeParameterKind.Options);
            switch(codeElement.MethodKind) {
                // case CodeMethodKind.Serializer:
                //     WriteSerializerBody(parentClass, writer);
                // break;
                // case CodeMethodKind.Deserializer:
                //     WriteDeserializerBody(codeElement, parentClass, writer);
                // break;
                case CodeMethodKind.IndexerBackwardCompatibility:
                    WriteIndexerBody(codeElement, writer, returnType);
                break;
                case CodeMethodKind.RequestGenerator:
                    WriteRequestGeneratorBody(codeElement, requestBodyParam, queryStringParam, headersParam, writer, parentClass, returnType);
                break;
                case CodeMethodKind.RequestExecutor:
                    WriteRequestExecutorBody(codeElement, requestBodyParam, queryStringParam, headersParam, optionsParam, returnType, writer);
                break;
                case CodeMethodKind.Getter:
                    WriteGetterBody(codeElement, writer, parentClass);
                    break;
                case CodeMethodKind.Setter:
                    WriteSetterBody(codeElement, writer, parentClass);
                    break;
                // case CodeMethodKind.ClientConstructor:
                //     WriteConstructorBody(parentClass, codeElement, writer, inherits);
                //     WriteApiConstructorBody(parentClass, codeElement, writer);
                // break;
                // case CodeMethodKind.Constructor:
                //     WriteConstructorBody(parentClass, codeElement, writer, inherits);
                //     break;
                case CodeMethodKind.Deserializer:
                    writer.WriteLine("return nil, nil");
                    break;
                default:
                    writer.WriteLine("return nil");
                break;
            }
            writer.DecreaseIndent();
            writer.WriteLine("}");
        }
        private void WriteMethodPrototype(CodeMethod code, LanguageWriter writer, string returnType, CodeClass parentClass) {
            var genericTypeParameterDeclaration = code.IsOfKind(CodeMethodKind.Deserializer) ? " <T>": string.Empty;
            var returnTypeAsyncPrefix = code.IsAsync ? "func() (" : string.Empty;
            var returnTypeAsyncSuffix = code.IsAsync ? "error)" : string.Empty;
            if(!string.IsNullOrEmpty(returnType) && code.IsAsync)
                returnTypeAsyncSuffix = $", {returnTypeAsyncSuffix}";
            var isConstructor = code.IsOfKind(CodeMethodKind.Constructor, CodeMethodKind.ClientConstructor);
            var methodName = (code.MethodKind switch {
                (CodeMethodKind.Constructor or CodeMethodKind.ClientConstructor) => $"New{code.Parent.Name.ToFirstCharacterUpperCase()}",
                (CodeMethodKind.Getter) => $"get{code.AccessedProperty?.Name?.ToFirstCharacterUpperCase()}",
                (CodeMethodKind.Setter) => $"set{code.AccessedProperty?.Name?.ToFirstCharacterUpperCase()}",
                _ => code.Name.ToFirstCharacterUpperCase()
            });
            var parameters = string.Join(", ", code.Parameters.Select(p => conventions.GetParameterSignature(p, parentClass)).ToList());
            var classType = conventions.GetTypeString(new CodeType(parentClass) { Name = parentClass.Name, TypeDefinition = parentClass }, parentClass);
            var associatedTypePrefix = isConstructor ? string.Empty : $" (m {classType})";
            var finalReturnType = isConstructor ? classType : $"{returnTypeAsyncPrefix}{returnType}{returnTypeAsyncSuffix}";
            var errorDeclaration = code.IsOfKind(CodeMethodKind.ClientConstructor, 
                                                CodeMethodKind.Constructor, 
                                                CodeMethodKind.Getter, 
                                                CodeMethodKind.Setter,
                                                CodeMethodKind.IndexerBackwardCompatibility) || code.IsAsync ? 
                                                    string.Empty :
                                                    "error";
            if(!string.IsNullOrEmpty(finalReturnType) && !string.IsNullOrEmpty(errorDeclaration))
                finalReturnType += ", ";
            writer.WriteLine($"func{associatedTypePrefix} {methodName}({parameters})({finalReturnType}{errorDeclaration}) {{");
        }
        private void WriteGetterBody(CodeMethod codeElement, LanguageWriter writer, CodeClass parentClass) {
            var backingStore = parentClass.GetBackingStoreProperty();
            if(backingStore == null || (codeElement.AccessedProperty?.IsOfKind(CodePropertyKind.BackingStore) ?? false))
                writer.WriteLine($"return m.{codeElement.AccessedProperty?.Name?.ToFirstCharacterLowerCase()}");
            else 
                if(!(codeElement.AccessedProperty?.Type?.IsNullable ?? true) &&
                   !(codeElement.AccessedProperty?.ReadOnly ?? true) && //TODO implement backing store getter when definition available in abstractions
                    !string.IsNullOrEmpty(codeElement.AccessedProperty?.DefaultValue)) {
                    writer.WriteLines($"{conventions.GetTypeString(codeElement.AccessedProperty.Type)} value = m.{backingStore.NamePrefix}{backingStore.Name.ToFirstCharacterLowerCase()}.get(\"{codeElement.AccessedProperty.Name.ToFirstCharacterLowerCase()}\");",
                        "if(value == null) {");
                    writer.IncreaseIndent();
                    writer.WriteLines($"value = {codeElement.AccessedProperty.DefaultValue};",
                        $"m.set{codeElement.AccessedProperty?.Name?.ToFirstCharacterUpperCase()}(value);");
                    writer.DecreaseIndent();
                    writer.WriteLines("}", "return value;");
                } else
                    writer.WriteLine($"return m.get{backingStore.Name.ToFirstCharacterUpperCase()}().get(\"{codeElement.AccessedProperty?.Name?.ToFirstCharacterLowerCase()}\");");

        }
        private static void WriteSetterBody(CodeMethod codeElement, LanguageWriter writer, CodeClass parentClass) {
            var backingStore = parentClass.GetBackingStoreProperty();
            if(backingStore == null)
                writer.WriteLine($"m.{codeElement.AccessedProperty?.Name?.ToFirstCharacterLowerCase()} = value");
            else //TODO implement backing store setter when definition available in abstractions
                writer.WriteLine($"m.get{backingStore.Name.ToFirstCharacterUpperCase()}().set(\"{codeElement.AccessedProperty?.Name?.ToFirstCharacterLowerCase()}\", value);");
        }
        private void WriteIndexerBody(CodeMethod codeElement, LanguageWriter writer, string returnType) {
            var currentPathProperty = codeElement.Parent.GetChildElements(true).OfType<CodeProperty>().FirstOrDefault(x => x.IsOfKind(CodePropertyKind.CurrentPath));
            var pathSegment = codeElement.PathSegment;
            conventions.AddRequestBuilderBody(currentPathProperty != null, returnType, writer, $" + \"/{(string.IsNullOrEmpty(pathSegment) ? string.Empty : pathSegment + "/" )}\" + id");
        }
        private void WriteRequestExecutorBody(CodeMethod codeElement, CodeParameter requestBodyParam, CodeParameter queryStringParam, CodeParameter headersParam, CodeParameter optionsParam, string returnType, LanguageWriter writer) {
            if(codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");
            var sendMethodName = returnType switch {
                "void" => "SendNoContentAsync",
                _ when string.IsNullOrEmpty(returnType) => "SendNoContentAsync",
                _ when conventions.IsScalarType(returnType) => "SendPrimitiveAsync",
                _ => "SendAsync"
            };
            var responseHandlerParam = codeElement.Parameters.FirstOrDefault(x => x.IsOfKind(CodeParameterKind.ResponseHandler));
            var typeShortName = returnType.Split('.').Last().ToFirstCharacterUpperCase();
            var isVoid = string.IsNullOrEmpty(typeShortName);
            WriteGeneratorMethodCall(codeElement, requestBodyParam, queryStringParam, headersParam, optionsParam, writer, $"{rInfoVarName}, err := ");
            WriteAsyncReturnError(writer, returnType);
            var constructorFunction = isVoid ?
                        string.Empty :
                        $"{conventions.GetTypeString(codeElement.ReturnType, codeElement.Parent, false)}.New{typeShortName.Replace("*", string.Empty)}, ";
            var returnTypeDeclaration = isVoid ?
                        string.Empty :
                        $"{returnType}, ";
            writer.WriteLine($"return func() ({returnTypeDeclaration}error) {{");
            writer.IncreaseIndent();
            var assignmentPrefix = isVoid ?
                        string.Empty :
                        "res, ";
            if(responseHandlerParam != null)
                writer.WriteLine($"{assignmentPrefix}err := m.httpCore.{sendMethodName}(*{rInfoVarName}, {constructorFunction}*{responseHandlerParam.Name})()");
            else
                writer.WriteLine($"{assignmentPrefix}err := m.httpCore.{sendMethodName}(*{rInfoVarName}, {constructorFunction}nil)()");
            WriteReturnError(writer, returnType);
            var resultReturnCast = isVoid ?
                        string.Empty :
                        $"res.({returnType}), ";
            writer.WriteLine($"return {resultReturnCast}nil");
            writer.DecreaseIndent();
            writer.WriteLine("}");
        }
        private static void WriteGeneratorMethodCall(CodeMethod codeElement, CodeParameter requestBodyParam, CodeParameter queryStringParam, CodeParameter headersParam, CodeParameter optionsParam, LanguageWriter writer, string prefix) {
            var generatorMethodName = (codeElement.Parent as CodeClass)
                                                .GetChildElements(true)
                                                .OfType<CodeMethod>()
                                                .FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestGenerator) && x.HttpMethod == codeElement.HttpMethod)
                                                ?.Name
                                                ?.ToFirstCharacterUpperCase();
            var paramsList = new List<CodeParameter> { requestBodyParam, queryStringParam, headersParam, optionsParam };
            var requestInfoParameters = paramsList.Where(x => x != null)
                                                .Select(x => x.Name)
                                                .ToList();
            var shouldSkipBodyParam = requestBodyParam == null && (codeElement.HttpMethod == HttpMethod.Get || codeElement.HttpMethod == HttpMethod.Delete);
            var skipIndex = shouldSkipBodyParam ? 1 : 0;
            if(codeElement.IsOverload && !codeElement.OriginalMethod.Parameters.Any(x => x.IsOfKind(CodeParameterKind.QueryParameter)) || // we're on an overload and the original method has no query parameters
                !codeElement.IsOverload && queryStringParam == null) // we're on the original method and there is no query string parameter
                skipIndex++;// we skip the query string parameter null value
            requestInfoParameters.AddRange(paramsList.Where(x => x == null).Skip(skipIndex).Select(x => "null"));
            var paramsCall = requestInfoParameters.Any() ? requestInfoParameters.Aggregate((x,y) => $"{x}, {y}") : string.Empty;
            writer.WriteLine($"{prefix}m.{generatorMethodName}({paramsCall});");
        }
        private const string rInfoVarName = "requestInfo";
        private void WriteRequestGeneratorBody(CodeMethod codeElement, CodeParameter requestBodyParam, CodeParameter queryStringParam, CodeParameter headersParam, LanguageWriter writer, CodeClass parentClass, string returnType) {
            if(codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");
            
            writer.WriteLine($"{rInfoVarName} := new({conventions.AbstractionsHash}.RequestInfo)");
            writer.WriteLines($"uri, err := url.Parse(*m.{conventions.CurrentPathPropertyName} + *m.{conventions.PathSegmentPropertyName})",
                        $"{rInfoVarName}.URI = *uri",
                        $"{rInfoVarName}.Method = {conventions.AbstractionsHash}.{codeElement.HttpMethod?.ToString().ToUpperInvariant()}");
            WriteReturnError(writer, returnType);
            if(requestBodyParam != null)
                if(requestBodyParam.Type.Name.Equals("binary", StringComparison.OrdinalIgnoreCase))
                    writer.WriteLine($"{rInfoVarName}.SetStreamContent({requestBodyParam.Name})");
                else
                    writer.WriteLine($"{rInfoVarName}.SetContentFromParsable({requestBodyParam.Name}, m.{conventions.HttpCorePropertyName}, \"{codeElement.ContentType}\")");
            if(queryStringParam != null) {
                var httpMethodPrefix = codeElement.HttpMethod.ToString().ToFirstCharacterUpperCase();
                writer.WriteLine($"if {queryStringParam.Name} != nil {{");
                writer.IncreaseIndent();
                writer.WriteLines($"qParams := new({parentClass.Name}{httpMethodPrefix}QueryParameters)",
                            $"err = {queryStringParam.Name}(qParams)");
                WriteReturnError(writer, returnType);
                writer.WriteLine("err := qParams.AddQueryParameters(requestInfo.QueryParameters)");
                WriteReturnError(writer, returnType);
                writer.DecreaseIndent();
                writer.WriteLine("}");
            }
            if(headersParam != null) {
                writer.WriteLine($"if {headersParam.Name} != nil {{");
                writer.IncreaseIndent();
                writer.WriteLine($"err = {headersParam.Name}({rInfoVarName}.Headers)");
                WriteReturnError(writer, returnType);
                writer.DecreaseIndent();
                writer.WriteLine("}");
            }
            writer.WriteLine($"return {rInfoVarName}, err");
        }
        private void WriteReturnError(LanguageWriter writer, params string[] returnTypes) {
            writer.WriteLine("if err != nil {");
            writer.IncreaseIndent();
            var nilsPrefix = GetNilsErrorPrefix(returnTypes);
            writer.WriteLine($"return {nilsPrefix}err");
            writer.DecreaseIndent();
            writer.WriteLine("}");
        }
        private static string GetNilsErrorPrefix(params string[] returnTypes) {
            var sanitizedTypes = returnTypes.Where(x => !string.IsNullOrEmpty(x));
            return !sanitizedTypes.Any() ?
                            string.Empty :
                            sanitizedTypes.Select(_ => "nil").Aggregate((x,y) => $"{x}, {y}") + ", ";
        }
        private void WriteAsyncReturnError(LanguageWriter writer, params string[] returnTypes) {
            writer.WriteLine("if err != nil {");
            writer.IncreaseIndent();
            var sanitizedTypes = returnTypes.Where(x => !string.IsNullOrEmpty(x));
            var typeDeclarationPrefix = !sanitizedTypes.Any() ?
                            string.Empty :
                            sanitizedTypes.Aggregate((x,y) => $"{x}, {y}") + ", ";
            var nilsPrefix = GetNilsErrorPrefix(returnTypes);
            writer.WriteLine($"return func() ({typeDeclarationPrefix}error) {{ return {nilsPrefix}err }}");
            writer.DecreaseIndent();
            writer.WriteLine("}");
        }
    }
}
