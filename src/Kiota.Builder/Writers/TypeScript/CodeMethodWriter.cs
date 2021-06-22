using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers.Extensions;

namespace Kiota.Builder.Writers.TypeScript {
    public class CodeMethodWriter : BaseElementWriter<CodeMethod, TypeScriptConventionService>
    {
        public CodeMethodWriter(TypeScriptConventionService conventionService) : base(conventionService){}
        private TypeScriptConventionService localConventions;
        public override void WriteCodeElement(CodeMethod codeElement, LanguageWriter writer)
        {
            if(codeElement == null) throw new ArgumentNullException(nameof(codeElement));
            if(codeElement.ReturnType == null) throw new InvalidOperationException($"{nameof(codeElement.ReturnType)} should not be null");
            if(writer == null) throw new ArgumentNullException(nameof(writer));
            if(!(codeElement.Parent is CodeClass)) throw new InvalidOperationException("the parent of a method should be a class");

            localConventions = new TypeScriptConventionService(writer); //because we allow inline type definitions for methods parameters
            var returnType = localConventions.GetTypeString(codeElement.ReturnType);
            var isVoid = "void".Equals(returnType, StringComparison.OrdinalIgnoreCase);
            WriteMethodDocumentation(codeElement, writer, isVoid);
            WriteMethodPrototype(codeElement, writer, returnType, isVoid);
            writer.IncreaseIndent();
            var parentClass = codeElement.Parent as CodeClass;
            var inherits = (parentClass.StartBlock as CodeClass.Declaration).Inherits != null;
            var requestBodyParam = codeElement.Parameters.OfKind(CodeParameterKind.RequestBody);
            var queryStringParam = codeElement.Parameters.OfKind(CodeParameterKind.QueryParameter);
            var headersParam = codeElement.Parameters.OfKind(CodeParameterKind.Headers);
            if(!codeElement.IsOfKind(CodeMethodKind.Setter))
                foreach(var parameter in codeElement.Parameters.Where(x => !x.Optional).OrderBy(x => x.Name)) {
                    writer.WriteLine($"if(!{parameter.Name}) throw new Error(\"{parameter.Name} cannot be undefined\");");
                }
            switch(codeElement.MethodKind) {
                case CodeMethodKind.IndexerBackwardCompatibility:
                    var pathSegment = codeElement.PathSegment;
                    localConventions.AddRequestBuilderBody(returnType, writer, $" + \"/{(string.IsNullOrEmpty(pathSegment) ? string.Empty : pathSegment + "/" )}\" + id");
                    break;
                case CodeMethodKind.Deserializer:
                    WriteDeserializerBody(codeElement, parentClass, writer);
                    break;
                case CodeMethodKind.Serializer:
                    WriteSerializerBody(inherits, parentClass, writer);
                    break;
                case CodeMethodKind.RequestGenerator:
                    WriteRequestGeneratorBody(codeElement, requestBodyParam, queryStringParam, headersParam, writer);
                break;
                case CodeMethodKind.RequestExecutor:
                    WriteRequestExecutorBody(codeElement, requestBodyParam, queryStringParam, headersParam, isVoid, returnType, writer);
                    break;
                case CodeMethodKind.Getter:
                    WriteGetterBody(codeElement, writer, parentClass);
                    break;
                case CodeMethodKind.Setter:
                    WriteSetterBody(codeElement, writer, parentClass);
                    break;
                case CodeMethodKind.Constructor:
                    WriteConstructorBody(parentClass, writer, inherits);
                    break;
                default:
                    WriteDefaultMethodBody(codeElement, writer);
                    break;
            }
            writer.DecreaseIndent();
            writer.WriteLine("};");
        }
        private static void WriteConstructorBody(CodeClass parentClass, LanguageWriter writer, bool inherits) {
            if(inherits)
                writer.WriteLine("super();");
            foreach(var propWithDefault in parentClass.GetPropertiesOfKind(CodePropertyKind.AdditionalData,
                                                                            CodePropertyKind.BackingStore,
                                                                            CodePropertyKind.RequestBuilder,
                                                                            CodePropertyKind.PathSegment)
                                            .Where(x => !string.IsNullOrEmpty(x.DefaultValue))
                                            .OrderByDescending(x => x.PropertyKind)
                                            .ThenBy(x => x.Name)) {
                writer.WriteLine($"this.{propWithDefault.NamePrefix}{propWithDefault.Name.ToFirstCharacterLowerCase()} = {propWithDefault.DefaultValue};");
            }
        }
        private static void WriteSetterBody(CodeMethod codeElement, LanguageWriter writer, CodeClass parentClass) {
            var backingStore = parentClass.GetBackingStoreProperty();
            if(backingStore == null)
                writer.WriteLine($"this.{codeElement.AccessedProperty?.NamePrefix}{codeElement.AccessedProperty?.Name?.ToFirstCharacterLowerCase()} = value;");
            else
                writer.WriteLine($"this.{backingStore.NamePrefix}{backingStore.Name.ToFirstCharacterLowerCase()}.set(\"{codeElement.AccessedProperty?.Name?.ToFirstCharacterLowerCase()}\", value);");
        }
        private void WriteGetterBody(CodeMethod codeElement, LanguageWriter writer, CodeClass parentClass) {
            var backingStore = parentClass.GetBackingStoreProperty();
            if(backingStore == null)
                writer.WriteLine($"return this.{codeElement.AccessedProperty?.NamePrefix}{codeElement.AccessedProperty?.Name?.ToFirstCharacterLowerCase()};");
            else 
                if(!(codeElement.AccessedProperty?.Type?.IsNullable ?? true) &&
                    !(codeElement.AccessedProperty?.ReadOnly ?? true) &&
                    !string.IsNullOrEmpty(codeElement.AccessedProperty?.DefaultValue)) {
                    writer.WriteLines($"let value = this.{backingStore.NamePrefix}{backingStore.Name.ToFirstCharacterLowerCase()}.get<{conventions.GetTypeString(codeElement.AccessedProperty.Type)}>(\"{codeElement.AccessedProperty.Name.ToFirstCharacterLowerCase()}\");",
                        "if(!value) {");
                    writer.IncreaseIndent();
                    writer.WriteLines($"value = {codeElement.AccessedProperty.DefaultValue};",
                        $"this.{codeElement.AccessedProperty?.NamePrefix}{codeElement.AccessedProperty?.Name?.ToFirstCharacterLowerCase()} = value;");
                    writer.DecreaseIndent();
                    writer.WriteLines("}", "return value;");
                } else
                    writer.WriteLine($"return this.{backingStore.NamePrefix}{backingStore.Name.ToFirstCharacterLowerCase()}.get(\"{codeElement.AccessedProperty?.Name?.ToFirstCharacterLowerCase()}\");");

        }
        private static void WriteDefaultMethodBody(CodeMethod codeElement, LanguageWriter writer) {
            var promisePrefix = codeElement.IsAsync ? "Promise.resolve(" : string.Empty;
            var promiseSuffix = codeElement.IsAsync ? ")" : string.Empty;
            writer.WriteLine($"return {promisePrefix}{(codeElement.ReturnType.Name.Equals("string") ? "''" : "{} as any")}{promiseSuffix};");
        }
        private void WriteDeserializerBody(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer) {
            var inherits = (parentClass.StartBlock as CodeClass.Declaration).Inherits != null;
            writer.WriteLine($"return new Map<string, (item: T, node: {localConventions.ParseNodeInterfaceName}) => void>([{(inherits ? $"...super.{codeElement.Name.ToFirstCharacterLowerCase()}()," : string.Empty)}");
            writer.IncreaseIndent();
            var parentClassName = parentClass.Name.ToFirstCharacterUpperCase();
            foreach(var otherProp in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)) {
                writer.WriteLine($"[\"{otherProp.SerializationName ?? otherProp.Name.ToFirstCharacterLowerCase()}\", (o, n) => {{ (o as unknown as {parentClassName}).{otherProp.Name.ToFirstCharacterLowerCase()} = n.{GetDeserializationMethodName(otherProp.Type)}; }}],");
            }
            writer.DecreaseIndent();
            writer.WriteLine("]);");
        }
        private void WriteRequestExecutorBody(CodeMethod codeElement, CodeParameter requestBodyParam, CodeParameter queryStringParam, CodeParameter headersParam, bool isVoid, string returnType, LanguageWriter writer) {
            if(codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");

            var generatorMethodName = (codeElement.Parent as CodeClass)
                                                .GetChildElements(true)
                                                .OfType<CodeMethod>()
                                                .FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestGenerator) && x.HttpMethod == codeElement.HttpMethod)
                                                ?.Name
                                                ?.ToFirstCharacterLowerCase();
            writer.WriteLine($"const requestInfo = this.{generatorMethodName}(");
            var requestInfoParameters = new List<string> { requestBodyParam?.Name, queryStringParam?.Name, headersParam?.Name }.Where(x => x != null);
            if(requestInfoParameters.Any()) {
                writer.IncreaseIndent();
                writer.WriteLine(requestInfoParameters.Aggregate((x,y) => $"{x}, {y}"));
                writer.DecreaseIndent();
            }
            writer.WriteLine(");");
            var isStream = localConventions.StreamTypeName.Equals(returnType, StringComparison.OrdinalIgnoreCase);
            var genericTypeForSendMethod = GetSendRequestMethodName(isVoid, isStream, returnType);
            var newFactoryParameter = GetTypeFactory(isVoid, isStream, returnType);
            writer.WriteLine($"return this.httpCore?.{genericTypeForSendMethod}(requestInfo,{newFactoryParameter} responseHandler) ?? Promise.reject(new Error('http core is null'));");
        }
        private void WriteRequestGeneratorBody(CodeMethod codeElement, CodeParameter requestBodyParam, CodeParameter queryStringParam, CodeParameter headersParam, LanguageWriter writer) {
            if(codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");
            
            writer.WriteLines("const requestInfo = new RequestInfo();",
                                $"requestInfo.URI = (this.{localConventions.CurrentPathPropertyName} ?? '') + this.{localConventions.PathSegmentPropertyName},",
                                $"requestInfo.httpMethod = HttpMethod.{codeElement.HttpMethod.ToString().ToUpperInvariant()},");
            if(headersParam != null)
                writer.WriteLine($"{headersParam.Name} && requestInfo.setHeadersFromRawObject(h);");
            if(queryStringParam != null)
                writer.WriteLines($"{queryStringParam.Name} && requestInfo.setQueryStringParametersFromRawObject(q);");
            if(requestBodyParam != null) {
                if(requestBodyParam.Type.Name.Equals(localConventions.StreamTypeName, StringComparison.OrdinalIgnoreCase))
                    writer.WriteLine($"requestInfo.setStreamContent({requestBodyParam.Name});");
                else
                    writer.WriteLine($"requestInfo.setContentFromParsable({requestBodyParam.Name}, this.{localConventions.SerializerFactoryPropertyName}, \"{codeElement.ContentType}\");");
            }
            writer.WriteLine("return requestInfo;");
        }
        private void WriteSerializerBody(bool inherits, CodeClass parentClass, LanguageWriter writer) {
            var additionalDataProperty = parentClass.GetPropertiesOfKind(CodePropertyKind.AdditionalData).FirstOrDefault();
            if(inherits)
                writer.WriteLine("super.serialize(writer);");
            foreach(var otherProp in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)) {
                writer.WriteLine($"writer.{GetSerializationMethodName(otherProp.Type)}(\"{otherProp.SerializationName ?? otherProp.Name.ToFirstCharacterLowerCase()}\", this.{otherProp.Name.ToFirstCharacterLowerCase()});");
            }
            if(additionalDataProperty != null)
                writer.WriteLine($"writer.writeAdditionalData(this.{additionalDataProperty.Name.ToFirstCharacterLowerCase()});");
        }
        private void WriteMethodDocumentation(CodeMethod code, LanguageWriter writer, bool isVoid) {
            var isDescriptionPresent = !string.IsNullOrEmpty(code.Description);
            var parametersWithDescription = code.Parameters.Where(x => !string.IsNullOrEmpty(code.Description));
            if (isDescriptionPresent || parametersWithDescription.Any()) {
                writer.WriteLine(localConventions.DocCommentStart);
                if(isDescriptionPresent)
                    writer.WriteLine($"{localConventions.DocCommentPrefix}{TypeScriptConventionService.RemoveInvalidDescriptionCharacters(code.Description)}");
                foreach(var paramWithDescription in parametersWithDescription.OrderBy(x => x.Name))
                    writer.WriteLine($"{localConventions.DocCommentPrefix}@param {paramWithDescription.Name} {TypeScriptConventionService.RemoveInvalidDescriptionCharacters(paramWithDescription.Description)}");
                
                if(!isVoid)
                    if(code.IsAsync)
                        writer.WriteLine($"{localConventions.DocCommentPrefix}@returns a Promise of {code.ReturnType.Name}");
                    else
                        writer.WriteLine($"{localConventions.DocCommentPrefix}@returns a {code.ReturnType.Name}");
                writer.WriteLine(localConventions.DocCommentEnd);
            }
        }
        private void WriteMethodPrototype(CodeMethod code, LanguageWriter writer, string returnType, bool isVoid) {
            var accessModifier = localConventions.GetAccessModifier(code.Access);
            var methodName = (code.IsOfKind(CodeMethodKind.Getter, CodeMethodKind.Setter) ?
                code.AccessedProperty?.Name :
                code.Name
            ).ToFirstCharacterLowerCase();
            var asyncPrefix = code.IsAsync && code.MethodKind != CodeMethodKind.RequestExecutor ? " async ": string.Empty;
            var parameters = string.Join(", ", code.Parameters.Select(p=> localConventions.GetParameterSignature(p)).ToList());
            var asyncReturnTypePrefix = code.IsAsync ? "Promise<": string.Empty;
            var asyncReturnTypeSuffix = code.IsAsync ? ">": string.Empty;
            var nullableSuffix = code.ReturnType.IsNullable && !isVoid ? " | undefined" : string.Empty;
            var accessorPrefix = code.MethodKind switch {
                    CodeMethodKind.Getter => "get ",
                    CodeMethodKind.Setter => "set ",
                    _ => string.Empty
                };
            var shouldHaveTypeSuffix = !code.IsAccessor && code.MethodKind != CodeMethodKind.Constructor;
            var returnTypeSuffix = shouldHaveTypeSuffix ? $" : {asyncReturnTypePrefix}{returnType}{nullableSuffix}{asyncReturnTypeSuffix}" : string.Empty;
            writer.WriteLine($"{accessModifier} {accessorPrefix}{methodName}{asyncPrefix}({parameters}){returnTypeSuffix} {{");
        }
        private string GetDeserializationMethodName(CodeTypeBase propType) {
            var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
            var propertyType = localConventions.TranslateType(propType.Name);
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
                case "Date":
                    return $"get{propertyType.ToFirstCharacterUpperCase()}Value()";
                default:
                    return $"getObjectValue<{propertyType.ToFirstCharacterUpperCase()}>({propertyType.ToFirstCharacterUpperCase()})";
            }
        }
        private string GetSerializationMethodName(CodeTypeBase propType) {
            var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
            var propertyType = localConventions.TranslateType(propType.Name);
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
                case "Date":
                    return $"write{propertyType.ToFirstCharacterUpperCase()}Value";
                default:
                    return $"writeObjectValue<{propertyType.ToFirstCharacterUpperCase()}>";
            }
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
    }
}
