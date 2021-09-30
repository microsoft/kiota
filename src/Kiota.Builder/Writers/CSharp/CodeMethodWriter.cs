using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers.Extensions;

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

            var returnType = conventions.GetTypeString(codeElement.ReturnType, codeElement);
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
                    WriteSerializerBody(inherits, codeElement, parentClass, writer);
                break;
                case CodeMethodKind.RequestGenerator:
                    WriteRequestGeneratorBody(codeElement, requestBodyParam, queryStringParam, headersParam, optionsParam, parentClass, writer);
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
                case CodeMethodKind.RequestBuilderWithParameters:
                    WriteRequestBuilderBody(parentClass, codeElement, writer);
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
        private void WriteRequestBuilderBody(CodeClass parentClass, CodeMethod codeElement, LanguageWriter writer)
        {
            var importSymbol = conventions.GetTypeString(codeElement.ReturnType, parentClass);
            var currentPathProperty = parentClass.Properties.FirstOrDefault(x => x.IsOfKind(CodePropertyKind.CurrentPath));
            conventions.AddRequestBuilderBody(currentPathProperty != null, importSymbol, writer, prefix: "return ", pathParameters: codeElement.Parameters.Where(x => x.IsOfKind(CodeParameterKind.Path)));
        }
        private static void WriteApiConstructorBody(CodeClass parentClass, CodeMethod method, LanguageWriter writer) {
            var httpCoreProperty = parentClass.Properties.FirstOrDefault(x => x.IsOfKind(CodePropertyKind.HttpCore));
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
                                            .Properties
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
            var property = parentClass.Properties.FirstOrDefault(x => x.IsOfKind(propertyKind));
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
                                            .Properties
                                            .Where(x => x.IsOfKind(CodePropertyKind.Custom))
                                            .OrderBy(x => x.Name)) {
                writer.WriteLine($"{{\"{otherProp.SerializationName ?? otherProp.Name.ToFirstCharacterLowerCase()}\", (o,n) => {{ (o as {parentClass.Name.ToFirstCharacterUpperCase()}).{otherProp.Name.ToFirstCharacterUpperCase()} = n.{GetDeserializationMethodName(otherProp.Type, codeElement)}(); }} }},");
            }
            writer.DecreaseIndent();
            writer.WriteLine("};");
        }
        private string GetDeserializationMethodName(CodeTypeBase propType, CodeMethod method) {
            var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
            var propertyType = conventions.GetTypeString(propType, method, false);
            if(propType is CodeType currentType) {
                if(isCollection)
                    if(currentType.TypeDefinition == null)
                        return $"GetCollectionOfPrimitiveValues<{propertyType}>().ToList";
                    else if (currentType.TypeDefinition is CodeEnum enumType)
                        return $"GetCollectionOfEnumValues<{enumType.Name.ToFirstCharacterUpperCase()}>().ToList";
                    else
                        return $"GetCollectionOfObjectValues<{propertyType}>().ToList";
                else if (currentType.TypeDefinition is CodeEnum enumType)
                    return $"GetEnumValue<{enumType.Name.ToFirstCharacterUpperCase()}>";
            }
            return propertyType switch
            {
                "byte[]" => "GetByteArrayValue",
                _ when conventions.IsPrimitiveType(propertyType) => $"Get{propertyType.TrimEnd(CSharpConventionService.NullableMarker).ToFirstCharacterUpperCase()}Value",
                _ => $"GetObjectValue<{propertyType.ToFirstCharacterUpperCase()}>",
            };
        }
        private void WriteRequestExecutorBody(CodeMethod codeElement, IEnumerable<CodeParameter> parameters, bool isVoid, string returnType, LanguageWriter writer) {
            if(codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");
            
            var isStream = conventions.StreamTypeName.Equals(returnType, StringComparison.OrdinalIgnoreCase);
            var generatorMethodName = (codeElement.Parent as CodeClass)
                                                .Methods
                                                .FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestGenerator) && x.HttpMethod == codeElement.HttpMethod)
                                                ?.Name;
            var parametersList = parameters.Select(x => x?.Name).Where(x => x != null).Aggregate((x,y) => $"{x}, {y}");
            writer.WriteLine($"var requestInfo = {generatorMethodName}({parametersList});");
            writer.WriteLine($"{(isVoid ? string.Empty : "return ")}await HttpCore.{GetSendRequestMethodName(isVoid, isStream, codeElement.ReturnType.IsCollection, returnType)}(requestInfo, responseHandler);");
        }
        private const string RequestInfoVarName = "requestInfo";
        private void WriteRequestGeneratorBody(CodeMethod codeElement, CodeParameter requestBodyParam, CodeParameter queryStringParam, CodeParameter headersParam, CodeParameter optionsParam, CodeClass currentClass, LanguageWriter writer) {
            if(codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");
            
            var operationName = codeElement.HttpMethod.ToString();
            var currentPathProperty = currentClass.GetPropertiesOfKind(CodePropertyKind.CurrentPath).FirstOrDefault();
            var pathSegmentProperty = currentClass.GetPropertiesOfKind(CodePropertyKind.PathSegment).FirstOrDefault();
            var rawUrlProperty = currentClass.GetPropertiesOfKind(CodePropertyKind.RawUrl).FirstOrDefault();
            var httpCoreProperty = currentClass.GetPropertiesOfKind(CodePropertyKind.HttpCore).FirstOrDefault();
            writer.WriteLine($"var {RequestInfoVarName} = new RequestInformation {{");
            writer.IncreaseIndent();
            writer.WriteLine($"HttpMethod = HttpMethod.{operationName?.ToUpperInvariant()},");
            writer.DecreaseIndent();
            writer.WriteLines("};",
                        $"{RequestInfoVarName}.SetURI({GetPropertyCall(currentPathProperty, "string.Empty")}, {GetPropertyCall(pathSegmentProperty, "string.Empty")}, {GetPropertyCall(rawUrlProperty, "false")});");
            if(requestBodyParam != null) {
                if(requestBodyParam.Type.Name.Equals(conventions.StreamTypeName, StringComparison.OrdinalIgnoreCase))
                    writer.WriteLine($"{RequestInfoVarName}.SetStreamContent({requestBodyParam.Name});");
                else
                    writer.WriteLine($"{RequestInfoVarName}.SetContentFromParsable({httpCoreProperty.Name.ToFirstCharacterUpperCase()}, \"{codeElement.ContentType}\", {requestBodyParam.Name});");
            }
            if(queryStringParam != null) {
                writer.WriteLine($"if ({queryStringParam.Name} != null) {{");
                writer.IncreaseIndent();
                writer.WriteLines($"var qParams = new {operationName?.ToFirstCharacterUpperCase()}QueryParameters();",
                            $"{queryStringParam.Name}.Invoke(qParams);",
                            $"qParams.AddQueryParameters({RequestInfoVarName}.QueryParameters);");
                writer.DecreaseIndent();
                writer.WriteLine("}");
            }
            if(headersParam != null)
                writer.WriteLine($"{headersParam.Name}?.Invoke({RequestInfoVarName}.Headers);");
            if(optionsParam != null)
                writer.WriteLine($"{RequestInfoVarName}.AddMiddlewareOptions({optionsParam.Name}?.ToArray());");
            writer.WriteLine($"return {RequestInfoVarName};");
        }
        private static string GetPropertyCall(CodeProperty property, string defaultValue) => property == null ? defaultValue : $"{property.Name.ToFirstCharacterUpperCase()}";
        private void WriteSerializerBody(bool shouldHide, CodeMethod method, CodeClass parentClass, LanguageWriter writer) {
            var additionalDataProperty = parentClass.Properties.FirstOrDefault(x => x.IsOfKind(CodePropertyKind.AdditionalData));
            if(shouldHide)
                writer.WriteLine("base.Serialize(writer);");
            foreach(var otherProp in parentClass
                                            .Properties
                                            .Where(x => x.IsOfKind(CodePropertyKind.Custom))
                                            .OrderBy(x => x.Name)) {
                writer.WriteLine($"writer.{GetSerializationMethodName(otherProp.Type, method)}(\"{otherProp.SerializationName ?? otherProp.Name.ToFirstCharacterLowerCase()}\", {otherProp.Name.ToFirstCharacterUpperCase()});");
            }
            if(additionalDataProperty != null)
                writer.WriteLine($"writer.WriteAdditionalData({additionalDataProperty.Name});");
        }
        private string GetSendRequestMethodName(bool isVoid, bool isStream, bool isCollection, string returnType) {
            if(isVoid) return "SendNoContentAsync";
            else if(isStream || conventions.IsPrimitiveType(returnType))
                if(isCollection)
                    return $"SendPrimitiveCollectionAsync<{returnType.StripArraySuffix()}>";
                else
                    return $"SendPrimitiveAsync<{returnType}>";
            else if (isCollection) return $"SendCollectionAsync<{returnType.StripArraySuffix()}>";
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
        private static readonly CodeParameterOrderComparer parameterOrderComparer = new();
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
            var parameters = string.Join(", ", code.Parameters.OrderBy(x => x, parameterOrderComparer).Select(p=> conventions.GetParameterSignature(p, code)).ToList());
            var methodName = isConstructor ? code.Parent.Name.ToFirstCharacterUpperCase() : code.Name;
            writer.WriteLine($"{conventions.GetAccessModifier(code.Access)} {staticModifier}{hideModifier}{completeReturnType}{methodName}({parameters}){baseSuffix} {{");
        }
        private string GetSerializationMethodName(CodeTypeBase propType, CodeMethod method) {
            var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
            var propertyType = conventions.GetTypeString(propType, method, false);
            if(propType is CodeType currentType) {
                if(isCollection)
                    if(currentType.TypeDefinition == null)
                        return $"WriteCollectionOfPrimitiveValues<{propertyType}>";
                    else if(currentType.TypeDefinition is CodeEnum enumType)
                        return $"WriteCollectionOfEnumValues<{enumType.Name.ToFirstCharacterUpperCase()}>";
                    else
                        return $"WriteCollectionOfObjectValues<{propertyType}>";
                else if (currentType.TypeDefinition is CodeEnum enumType)
                    return $"WriteEnumValue<{enumType.Name.ToFirstCharacterUpperCase()}>";
                
            }
            return propertyType switch
            {
                "byte[]" => "WriteByteArrayValue",
                _ when conventions.IsPrimitiveType(propertyType) => $"Write{propertyType.TrimEnd(CSharpConventionService.NullableMarker).ToFirstCharacterUpperCase()}Value",
                _ => $"WriteObjectValue<{propertyType.ToFirstCharacterUpperCase()}>",
            };
        }
    }
}
