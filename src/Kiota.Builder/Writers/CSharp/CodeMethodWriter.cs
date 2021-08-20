using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.CSharp {
    public class CodeMethodWriter : BaseElementWriter<CodeMethod, CSharpConventionService>
    {
        public CodeMethodWriter(CSharpConventionService conventionService): base(conventionService) { }
        public override void WriteCodeElement(CodeMethod codeElement, LanguageWriter writer)
        {
            if(codeElement == null) throw new ArgumentNullException(nameof(codeElement));
            if(codeElement.ReturnType == null) throw new InvalidOperationException($"{nameof(codeElement.ReturnType)} should not be null");
            if(writer == null) throw new ArgumentNullException(nameof(writer));
            if(!(codeElement.Parent is CodeClass)) throw new InvalidOperationException("the parent of a method should be a class");

            var returnType = conventions.GetTypeString(codeElement.ReturnType);
            var parentClass = codeElement.Parent as CodeClass;
            var inherits = (parentClass.StartBlock as CodeClass.Declaration).Inherits != null;
            var isVoid = conventions.VoidTypeName.Equals(returnType, StringComparison.OrdinalIgnoreCase);
            WriteMethodDocumentation(codeElement, writer);
            WriteMethodPrototype(codeElement, writer, returnType, inherits, isVoid);
            writer.IncreaseIndent();
            var requestBodyParam = codeElement.Parameters.OfKind(CodeParameterKind.RequestBody);
            var queryStringParam = codeElement.Parameters.OfKind(CodeParameterKind.QueryParameter);
            var headersParam = codeElement.Parameters.OfKind(CodeParameterKind.Headers);
            var optionsParam = codeElement.Parameters.OfKind(CodeParameterKind.Options);
            foreach(var parameter in codeElement.Parameters.Where(x => !x.Optional).OrderBy(x => x.Name)) {
                if(nameof(String).Equals(parameter.Type.Name, StringComparison.OrdinalIgnoreCase))
                    writer.WriteLine($"if(string.IsNullOrEmpty({parameter.Name})) throw new ArgumentNullException(nameof({parameter.Name}));");
                else
                    writer.WriteLine($"_ = {parameter.Name} ?? throw new ArgumentNullException(nameof({parameter.Name}));");
            }
            switch(codeElement.MethodKind) {
                case CodeMethodKind.Serializer:
                    WriteSerializerBody(inherits, parentClass, writer);
                break;
                case CodeMethodKind.RequestGenerator:
                    WriteRequestGeneratorBody(codeElement, requestBodyParam, queryStringParam, headersParam, optionsParam, writer);
                    break;
                case CodeMethodKind.RequestExecutor:
                    WriteRequestExecutorBody(codeElement, new List<CodeParameter> { requestBodyParam, queryStringParam, headersParam, optionsParam }, isVoid, returnType, writer);
                    break;
                case CodeMethodKind.Deserializer:
                    WriteDeserializerBody(codeElement, parentClass, writer);
                    break;
                case CodeMethodKind.ClientConstructor:
                    WriteConstructorBody(parentClass, codeElement, writer);
                    WriteApiConstructorBody(parentClass, codeElement, writer);
                    break;
                case CodeMethodKind.Constructor:
                    WriteConstructorBody(parentClass, codeElement, writer);
                    break;
                case CodeMethodKind.Getter:
                case CodeMethodKind.Setter:
                    throw new InvalidOperationException("getters and setters are automatically added on fields in dotnet");
                case CodeMethodKind.RequestBuilderBackwardCompatibility:
                    throw new InvalidOperationException("RequestBuilderBackwardCompatibility is not supported as the request builders are implemented by properties.");
                default:
                    writer.WriteLine("return null;");
                break;
            }
            writer.DecreaseIndent();
            writer.WriteLine("}");
        }
        private static void WriteApiConstructorBody(CodeClass parentClass, CodeMethod method, LanguageWriter writer) {
            var httpCoreProperty = parentClass.GetChildElements(true).OfType<CodeProperty>().FirstOrDefault(x => x.IsOfKind(CodePropertyKind.HttpCore));
            var httpCoreParameter = method.Parameters.FirstOrDefault(x => x.IsOfKind(CodeParameterKind.HttpCore));
            var backingStoreParameter = method.Parameters.FirstOrDefault(x => x.IsOfKind(CodeParameterKind.BackingStore));
            var httpCorePropertyName = httpCoreProperty.Name.ToFirstCharacterUpperCase();
            writer.WriteLine($"{httpCorePropertyName} = {httpCoreParameter.Name};");
            WriteSerializationRegistration(method.SerializerModules, writer, "RegisterDefaultSerializer");
            WriteSerializationRegistration(method.DeserializerModules, writer, "RegisterDefaultDeserializer");
            if(backingStoreParameter != null)
                writer.WriteLine($"{httpCorePropertyName}.EnableBackingStore({backingStoreParameter.Name});");
        }
        private static void WriteSerializationRegistration(List<string> serializationClassNames, LanguageWriter writer, string methodName) {
            if(serializationClassNames != null)
                foreach(var serializationClassName in serializationClassNames)
                    writer.WriteLine($"ApiClientBuilder.{methodName}<{serializationClassName}>();");
        }
        private static void WriteConstructorBody(CodeClass parentClass, CodeMethod currentMethod, LanguageWriter writer) {
            foreach(var propWithDefault in parentClass
                                            .GetChildElements(true)
                                            .OfType<CodeProperty>()
                                            .Where(x => !string.IsNullOrEmpty(x.DefaultValue))
                                            .OrderByDescending(x => x.PropertyKind)
                                            .ThenBy(x => x.Name)) {
                writer.WriteLine($"{propWithDefault.Name.ToFirstCharacterUpperCase()} = {propWithDefault.DefaultValue};");
            }
            if(currentMethod.IsOfKind(CodeMethodKind.Constructor)) {
                AssignPropertyFromParameter(parentClass, currentMethod, CodeParameterKind.HttpCore, CodePropertyKind.HttpCore, writer);
                AssignPropertyFromParameter(parentClass, currentMethod, CodeParameterKind.CurrentPath, CodePropertyKind.CurrentPath, writer);
                AssignPropertyFromParameter(parentClass, currentMethod, CodeParameterKind.RawUrl, CodePropertyKind.RawUrl, writer);
            }
        }
        private static void AssignPropertyFromParameter(CodeClass parentClass, CodeMethod currentMethod, CodeParameterKind parameterKind, CodePropertyKind propertyKind, LanguageWriter writer) {
            var property = parentClass.GetChildElements(true).OfType<CodeProperty>().FirstOrDefault(x => x.IsOfKind(propertyKind));
            var parameter = currentMethod.Parameters.FirstOrDefault(x => x.IsOfKind(parameterKind));
            if(property != null && parameter != null) {
                writer.WriteLine($"{property.Name.ToFirstCharacterUpperCase()} = {parameter.Name};");
            }
        }
        private void WriteDeserializerBody(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer) {
            var hideParentMember = (parentClass.StartBlock as CodeClass.Declaration).Inherits != null;
            var parentSerializationInfo = hideParentMember ? $"(base.{codeElement.Name.ToFirstCharacterUpperCase()}())" : string.Empty;
            writer.WriteLine($"return new Dictionary<string, Action<T, {conventions.ParseNodeInterfaceName}>>{parentSerializationInfo} {{");
            writer.IncreaseIndent();
            foreach(var otherProp in parentClass
                                            .GetChildElements(true)
                                            .OfType<CodeProperty>()
                                            .Where(x => x.IsOfKind(CodePropertyKind.Custom))
                                            .OrderBy(x => x.Name)) {
                writer.WriteLine($"{{\"{otherProp.SerializationName ?? otherProp.Name.ToFirstCharacterLowerCase()}\", (o,n) => {{ (o as {parentClass.Name.ToFirstCharacterUpperCase()}).{otherProp.Name.ToFirstCharacterUpperCase()} = n.{GetDeserializationMethodName(otherProp.Type)}(); }} }},");
            }
            writer.DecreaseIndent();
            writer.WriteLine("};");
        }
        private string GetDeserializationMethodName(CodeTypeBase propType) {
            var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
            var propertyType = conventions.TranslateType(propType.Name);
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
        private void WriteRequestExecutorBody(CodeMethod codeElement, IEnumerable<CodeParameter> parameters, bool isVoid, string returnType, LanguageWriter writer) {
            if(codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");
            
            var isStream = conventions.StreamTypeName.Equals(returnType, StringComparison.OrdinalIgnoreCase);
            var generatorMethodName = (codeElement.Parent as CodeClass)
                                                .GetChildElements(true)
                                                .OfType<CodeMethod>()
                                                .FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestGenerator) && x.HttpMethod == codeElement.HttpMethod)
                                                ?.Name;
            var parametersList = parameters.Select(x => x?.Name).Where(x => x != null).Aggregate((x,y) => $"{x}, {y}");
            writer.WriteLine($"var requestInfo = {generatorMethodName}({parametersList});");
            writer.WriteLine($"{(isVoid ? string.Empty : "return ")}await HttpCore.{GetSendRequestMethodName(isVoid, isStream, codeElement.ReturnType.IsCollection, returnType)}(requestInfo, responseHandler);");
        }
        private const string _requestInfoVarName = "requestInfo";
        private void WriteRequestGeneratorBody(CodeMethod codeElement, CodeParameter requestBodyParam, CodeParameter queryStringParam, CodeParameter headersParam, CodeParameter optionsParam, LanguageWriter writer) {
            if(codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");
            
            var operationName = codeElement.HttpMethod.ToString();
            writer.WriteLine($"var {_requestInfoVarName} = new RequestInfo {{");
            writer.IncreaseIndent();
            writer.WriteLine($"HttpMethod = HttpMethod.{operationName?.ToUpperInvariant()},");
            writer.DecreaseIndent();
            writer.WriteLines("};",
                        $"{_requestInfoVarName}.SetURI({conventions.CurrentPathPropertyName}, {conventions.PathSegmentPropertyName}, {conventions.RawUrlPropertyName});");
            if(requestBodyParam != null) {
                if(requestBodyParam.Type.Name.Equals(conventions.StreamTypeName, StringComparison.OrdinalIgnoreCase))
                    writer.WriteLine($"{_requestInfoVarName}.SetStreamContent({requestBodyParam.Name});");
                else
                    writer.WriteLine($"{_requestInfoVarName}.SetContentFromParsable({conventions.HttpCorePropertyName}, \"{codeElement.ContentType}\", {requestBodyParam.Name});");
            }
            if(queryStringParam != null) {
                writer.WriteLine($"if ({queryStringParam.Name} != null) {{");
                writer.IncreaseIndent();
                writer.WriteLines($"var qParams = new {operationName?.ToFirstCharacterUpperCase()}QueryParameters();",
                            $"{queryStringParam.Name}.Invoke(qParams);",
                            $"qParams.AddQueryParameters({_requestInfoVarName}.QueryParameters);");
                writer.DecreaseIndent();
                writer.WriteLine("}");
            }
            if(headersParam != null)
                writer.WriteLine($"{headersParam.Name}?.Invoke({_requestInfoVarName}.Headers);");
            if(optionsParam != null)
                writer.WriteLine($"{_requestInfoVarName}.AddMiddlewareOptions({optionsParam.Name}?.ToArray());");
            writer.WriteLine($"return {_requestInfoVarName};");
        }
        private void WriteSerializerBody(bool shouldHide, CodeClass parentClass, LanguageWriter writer) {
            var additionalDataProperty = parentClass.GetChildElements(true).OfType<CodeProperty>().FirstOrDefault(x => x.IsOfKind(CodePropertyKind.AdditionalData));
            if(shouldHide)
                writer.WriteLine("base.Serialize(writer);");
            foreach(var otherProp in parentClass
                                            .GetChildElements(true)
                                            .OfType<CodeProperty>()
                                            .Where(x => x.IsOfKind(CodePropertyKind.Custom))
                                            .OrderBy(x => x.Name)) {
                writer.WriteLine($"writer.{GetSerializationMethodName(otherProp.Type)}(\"{otherProp.SerializationName ?? otherProp.Name.ToFirstCharacterLowerCase()}\", {otherProp.Name.ToFirstCharacterUpperCase()});");
            }
            if(additionalDataProperty != null)
                writer.WriteLine($"writer.WriteAdditionalData({additionalDataProperty.Name});");
        }
        private string GetSendRequestMethodName(bool isVoid, bool isStream, bool isCollection, string returnType) {
            if(isVoid) return "SendNoContentAsync";
            else if(isStream || conventions.IsPrimitiveType(returnType)) return $"SendPrimitiveAsync<{returnType}>";
            else if(isCollection) return $"SendCollectionAsync<{returnType.StripArraySuffix()}>";
            else return $"SendAsync<{returnType}>";
        }
        private void WriteMethodDocumentation(CodeMethod code, LanguageWriter writer) {
            var isDescriptionPresent = !string.IsNullOrEmpty(code.Description);
            var parametersWithDescription = code.Parameters.Where(x => !string.IsNullOrEmpty(code.Description));
            if (isDescriptionPresent || parametersWithDescription.Any()) {
                writer.WriteLine($"{conventions.DocCommentPrefix}<summary>");
                if(isDescriptionPresent)
                    writer.WriteLine($"{conventions.DocCommentPrefix}{code.Description}");
                foreach(var paramWithDescription in parametersWithDescription.OrderBy(x => x.Name))
                    writer.WriteLine($"{conventions.DocCommentPrefix}<param name=\"{paramWithDescription.Name}\">{paramWithDescription.Description}</param>");
                writer.WriteLine($"{conventions.DocCommentPrefix}</summary>");
            }
        }
        private static CodeParameterOrderComparer parameterOrderComparer = new CodeParameterOrderComparer();
        private void WriteMethodPrototype(CodeMethod code, LanguageWriter writer, string returnType, bool inherits, bool isVoid) {
            var staticModifier = code.IsStatic ? "static " : string.Empty;
            var hideModifier = inherits && code.IsSerializationMethod ? "new " : string.Empty;
            var genericTypePrefix = isVoid ? string.Empty : "<";
            var genericTypeSuffix = code.IsAsync && !isVoid ? ">": string.Empty;
            var isConstructor = code.IsOfKind(CodeMethodKind.Constructor, CodeMethodKind.ClientConstructor);
            var asyncPrefix = code.IsAsync ? "async Task" + genericTypePrefix : string.Empty;
            var voidCorrectedTaskReturnType = code.IsAsync && isVoid ? string.Empty : returnType;
            if(code.ReturnType.IsArray && code.IsOfKind(CodeMethodKind.RequestExecutor))
                voidCorrectedTaskReturnType = $"IEnumerable<{voidCorrectedTaskReturnType.StripArraySuffix()}>";
            // TODO: Task type should be moved into the refiner
            var completeReturnType = isConstructor ?
                string.Empty :
                $"{asyncPrefix}{voidCorrectedTaskReturnType}{genericTypeSuffix} ";
            var baseSuffix = string.Empty;
            if(isConstructor && inherits)
                baseSuffix = " : base()";
            var parameters = string.Join(", ", code.Parameters.OrderBy(x => x, parameterOrderComparer).Select(p=> conventions.GetParameterSignature(p)).ToList());
            var methodName = isConstructor ? code.Parent.Name.ToFirstCharacterUpperCase() : code.Name;
            writer.WriteLine($"{conventions.GetAccessModifier(code.Access)} {staticModifier}{hideModifier}{completeReturnType}{methodName}({parameters}){baseSuffix} {{");
        }
        private string GetSerializationMethodName(CodeTypeBase propType) {
            var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
            var propertyType = conventions.TranslateType(propType.Name);
            var nullableSuffix = conventions.ShouldTypeHaveNullableMarker(propType, propertyType) ? CSharpConventionService.NullableMarker : string.Empty;
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
    }
}
