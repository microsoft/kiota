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
            var shouldHide = (parentClass.StartBlock as CodeClass.Declaration).Inherits != null && 
                            (codeElement.MethodKind == CodeMethodKind.Serializer ||
                            codeElement.MethodKind == CodeMethodKind.Deserializer);
            var isVoid = conventions.VoidTypeName.Equals(returnType, StringComparison.OrdinalIgnoreCase);
            WriteMethodDocumentation(codeElement, writer);
            WriteMethodPrototype(codeElement, writer, returnType, shouldHide, isVoid);
            writer.IncreaseIndent();
            var requestBodyParam = codeElement.Parameters.OfKind(CodeParameterKind.RequestBody);
            var queryStringParam = codeElement.Parameters.OfKind(CodeParameterKind.QueryParameter);
            var headersParam = codeElement.Parameters.OfKind(CodeParameterKind.Headers);
            switch(codeElement.MethodKind) {
                case CodeMethodKind.Serializer:
                    WriteSerializerBody(shouldHide, parentClass, writer);
                break;
                case CodeMethodKind.RequestGenerator:
                    WriteRequestGeneratorBody(codeElement, requestBodyParam, queryStringParam, headersParam, writer);
                    break;
                case CodeMethodKind.RequestExecutor:
                    WriteRequestExecutorBody(codeElement, requestBodyParam, queryStringParam, headersParam, isVoid, returnType, writer);
                    break;
                case CodeMethodKind.Deserializer:
                    var hideParentMember = (parentClass.StartBlock as CodeClass.Declaration).Inherits != null;
                    var parentSerializationInfo = hideParentMember ? $"(base.{codeElement.Name.ToFirstCharacterUpperCase()}())" : string.Empty;
                    writer.WriteLine($"return new Dictionary<string, Action<T, {conventions.ParseNodeInterfaceName}>>{parentSerializationInfo} {{");
                    writer.IncreaseIndent();
                    foreach(var otherProp in parentClass
                                                    .GetChildElements(true)
                                                    .OfType<CodeProperty>()
                                                    .Where(x => x.PropertyKind == CodePropertyKind.Custom)
                                                    .OrderBy(x => x.Name)) {
                        writer.WriteLine($"{{\"{otherProp.SerializationName ?? otherProp.Name.ToFirstCharacterLowerCase()}\", (o,n) => {{ (o as {parentClass.Name.ToFirstCharacterUpperCase()}).{otherProp.Name.ToFirstCharacterUpperCase()} = n.{GetDeserializationMethodName(otherProp.Type)}(); }} }},");
                    }
                    writer.DecreaseIndent();
                    writer.WriteLine("};");
                break;
                default:
                    writer.WriteLine("return null;");
                break;
            }
            writer.DecreaseIndent();
            writer.WriteLine("}");
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
        private void WriteRequestExecutorBody(CodeMethod codeElement, CodeParameter requestBodyParam, CodeParameter queryStringParam, CodeParameter headersParam, bool isVoid, string returnType, LanguageWriter writer) {
            if(codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");
            
            var isStream = conventions.StreamTypeName.Equals(returnType, StringComparison.OrdinalIgnoreCase);
            var generatorMethodName = (codeElement.Parent as CodeClass)
                                                .GetChildElements(true)
                                                .OfType<CodeMethod>()
                                                .FirstOrDefault(x => x.MethodKind == CodeMethodKind.RequestGenerator && x.HttpMethod == codeElement.HttpMethod)
                                                ?.Name;
                    writer.WriteLine($"var requestInfo = {generatorMethodName}(");
                    writer.IncreaseIndent();
                    writer.WriteLine(new List<string> { requestBodyParam?.Name, queryStringParam?.Name, headersParam?.Name }.Where(x => x != null).Aggregate((x,y) => $"{x}, {y}"));
                    writer.DecreaseIndent();
                    writer.WriteLines(");",
                                $"{(isVoid ? string.Empty : "return ")}await HttpCore.{GetSendRequestMethodName(isVoid, isStream, returnType)}(requestInfo, responseHandler);");

        }
        private void WriteRequestGeneratorBody(CodeMethod codeElement, CodeParameter requestBodyParam, CodeParameter queryStringParam, CodeParameter headersParam, LanguageWriter writer) {
            if(codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");
            
            var operationName = codeElement.HttpMethod.ToString();
            writer.WriteLine("var requestInfo = new RequestInfo {");
            writer.IncreaseIndent();
            writer.WriteLines($"HttpMethod = HttpMethod.{operationName?.ToUpperInvariant()},",
                        $"URI = new Uri({conventions.CurrentPathPropertyName} + {conventions.PathSegmentPropertyName}),");
            writer.DecreaseIndent();
            writer.WriteLine("};");
            if(requestBodyParam != null) {
                if(requestBodyParam.Type.Name.Equals(conventions.StreamTypeName, StringComparison.OrdinalIgnoreCase))
                    writer.WriteLine($"requestInfo.SetStreamContent({requestBodyParam.Name});");
                else
                    writer.WriteLine($"requestInfo.SetContentFromParsable({requestBodyParam.Name}, {conventions.SerializerFactoryPropertyName}, \"{codeElement.ContentType}\");");
            }
            if(queryStringParam != null) {
                writer.WriteLine($"if ({queryStringParam.Name} != null) {{");
                writer.IncreaseIndent();
                writer.WriteLines($"var qParams = new {operationName?.ToFirstCharacterUpperCase()}QueryParameters();",
                            $"{queryStringParam.Name}.Invoke(qParams);",
                            "qParams.AddQueryParameters(requestInfo.QueryParameters);");
                writer.DecreaseIndent();
                writer.WriteLine("}");
            }
            if(headersParam != null) {
                writer.WriteLines($"{headersParam.Name}?.Invoke(requestInfo.Headers);",
                        "return requestInfo;");
            }
        }
        private void WriteSerializerBody(bool shouldHide, CodeClass parentClass, LanguageWriter writer) {
            var additionalDataProperty = parentClass.GetChildElements(true).OfType<CodeProperty>().FirstOrDefault(x => x.PropertyKind == CodePropertyKind.AdditionalData);
            if(shouldHide)
                writer.WriteLine("base.Serialize(writer);");
            foreach(var otherProp in parentClass
                                            .GetChildElements(true)
                                            .OfType<CodeProperty>()
                                            .Where(x => x.PropertyKind == CodePropertyKind.Custom)
                                            .OrderBy(x => x.Name)) {
                writer.WriteLine($"writer.{GetSerializationMethodName(otherProp.Type)}(\"{otherProp.SerializationName ?? otherProp.Name.ToFirstCharacterLowerCase()}\", {otherProp.Name.ToFirstCharacterUpperCase()});");
            }
            if(additionalDataProperty != null)
                writer.WriteLine($"writer.WriteAdditionalData({additionalDataProperty.Name});");
        }
        private static string GetSendRequestMethodName(bool isVoid, bool isStream, string returnType) {
            if(isVoid) return "SendNoContentAsync";
            else if(isStream) return $"SendPrimitiveAsync<{returnType}>";
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
        private void WriteMethodPrototype(CodeMethod code, LanguageWriter writer, string returnType, bool shouldHide, bool isVoid) {
            var staticModifier = code.IsStatic ? "static " : string.Empty;
            var hideModifier = shouldHide ? "new " : string.Empty;
            var genericTypePrefix = isVoid ? string.Empty : "<";
            var genricTypeSuffix = code.IsAsync && !isVoid ? ">": string.Empty;
            // TODO: Task type should be moved into the refiner
            var completeReturnType = $"{(code.IsAsync ? "async Task" + genericTypePrefix : string.Empty)}{(code.IsAsync && isVoid ? string.Empty : returnType)}{genricTypeSuffix}";
            var parameters = string.Join(", ", code.Parameters.Select(p=> conventions.GetParameterSignature(p)).ToList());
            writer.WriteLine($"{conventions.GetAccessModifier(code.Access)} {staticModifier}{hideModifier}{completeReturnType} {code.Name}({parameters}) {{");
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
