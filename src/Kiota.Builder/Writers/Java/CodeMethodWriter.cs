using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers.Extensions;

namespace Kiota.Builder.Writers.Java {
    public class CodeMethodWriter : BaseElementWriter<CodeMethod, JavaConventionService>
    {
        public CodeMethodWriter(JavaConventionService conventionService) : base(conventionService){}
        public override void WriteCodeElement(CodeMethod codeElement, LanguageWriter writer)
        {
            if(codeElement == null) throw new ArgumentNullException(nameof(codeElement));
            if(codeElement.ReturnType == null) throw new InvalidOperationException($"{nameof(codeElement.ReturnType)} should not be null");
            if(writer == null) throw new ArgumentNullException(nameof(writer));
            if(!(codeElement.Parent is CodeClass)) throw new InvalidOperationException("the parent of a method should be a class");

            var returnType = conventions.GetTypeString(codeElement.ReturnType);
            WriteMethodDocumentation(codeElement, writer);
            if(returnType.Equals("void", StringComparison.OrdinalIgnoreCase))
            {
                if(codeElement.IsOfKind(CodeMethodKind.RequestExecutor))
                    returnType = "Void"; //generic type for the future
            } else if(!codeElement.IsAsync)
                writer.WriteLine(codeElement.ReturnType.IsNullable && !codeElement.IsAsync ? "@javax.annotation.Nullable" : "@javax.annotation.Nonnull");
            WriteMethodPrototype(codeElement, writer, returnType);
            writer.IncreaseIndent();
            var parentClass = codeElement.Parent as CodeClass;
            var inherits = (parentClass.StartBlock as CodeClass.Declaration).Inherits != null;
            var requestBodyParam = codeElement.Parameters.OfKind(CodeParameterKind.RequestBody);
            var queryStringParam = codeElement.Parameters.OfKind(CodeParameterKind.QueryParameter);
            var headersParam = codeElement.Parameters.OfKind(CodeParameterKind.Headers);
            foreach(var parameter in codeElement.Parameters.Where(x => !x.Optional).OrderBy(x => x.Name)) {
                writer.WriteLine($"Objects.requireNonNull({parameter.Name});");
            }
            switch(codeElement.MethodKind) {
                case CodeMethodKind.Serializer:
                    WriteSerializerBody(parentClass, writer);
                break;
                case CodeMethodKind.Deserializer:
                    WriteDeserializerBody(codeElement, parentClass, writer);
                break;
                case CodeMethodKind.IndexerBackwardCompatibility:
                    WriteIndexerBody(codeElement, writer, returnType);
                break;
                case CodeMethodKind.RequestGenerator:
                    WriteRequestGeneratorBody(codeElement, requestBodyParam, queryStringParam, headersParam, writer);
                break;
                case CodeMethodKind.RequestExecutor:
                    WriteRequestExecutorBody(codeElement, requestBodyParam, queryStringParam, headersParam, returnType, writer);
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
                    writer.WriteLine("return null;");
                break;
            }
            writer.DecreaseIndent();
            writer.WriteLine("}");
        }
        private static void WriteConstructorBody(CodeClass parentClass, LanguageWriter writer, bool inherits) {
            if(inherits)
                writer.WriteLine("super();");
            foreach(var propWithDefault in parentClass.GetPropertiesOfKind(CodePropertyKind.BackingStore,
                                                                            CodePropertyKind.RequestBuilder,
                                                                            CodePropertyKind.PathSegment)
                                            .Where(x => !string.IsNullOrEmpty(x.DefaultValue))
                                            .OrderBy(x => x.Name)) {
                writer.WriteLine($"this.{propWithDefault.NamePrefix}{propWithDefault.Name.ToFirstCharacterLowerCase()} = {propWithDefault.DefaultValue};");
            }
            foreach(var propWithDefault in parentClass.GetPropertiesOfKind(CodePropertyKind.AdditionalData) //additional data and backing Store rely on accessors
                                            .Where(x => !string.IsNullOrEmpty(x.DefaultValue))
                                            .OrderBy(x => x.Name)) {
                writer.WriteLine($"this.set{propWithDefault.Name.ToFirstCharacterUpperCase()}({propWithDefault.DefaultValue});");
            }
        }
        private static void WriteSetterBody(CodeMethod codeElement, LanguageWriter writer, CodeClass parentClass) {
            var backingStore = parentClass.GetBackingStoreProperty();
            if(backingStore == null)
                writer.WriteLine($"this.{codeElement.AccessedProperty?.NamePrefix}{codeElement.AccessedProperty?.Name?.ToFirstCharacterLowerCase()} = value;");
            else
                writer.WriteLine($"this.get{backingStore.Name.ToFirstCharacterUpperCase()}().set(\"{codeElement.AccessedProperty?.Name?.ToFirstCharacterLowerCase()}\", value);");
        }
        private void WriteGetterBody(CodeMethod codeElement, LanguageWriter writer, CodeClass parentClass) {
            var backingStore = parentClass.GetBackingStoreProperty();
            if(backingStore == null || (codeElement.AccessedProperty?.IsOfKind(CodePropertyKind.BackingStore) ?? false))
                writer.WriteLine($"return this.{codeElement.AccessedProperty?.NamePrefix}{codeElement.AccessedProperty?.Name?.ToFirstCharacterLowerCase()};");
            else 
                if(!(codeElement.AccessedProperty?.Type?.IsNullable ?? true) &&
                   !(codeElement.AccessedProperty?.ReadOnly ?? true) &&
                    !string.IsNullOrEmpty(codeElement.AccessedProperty?.DefaultValue)) {
                    writer.WriteLines($"{conventions.GetTypeString(codeElement.AccessedProperty.Type)} value = this.{backingStore.NamePrefix}{backingStore.Name.ToFirstCharacterLowerCase()}.get(\"{codeElement.AccessedProperty.Name.ToFirstCharacterLowerCase()}\");",
                        "if(value == null) {");
                    writer.IncreaseIndent();
                    writer.WriteLines($"value = {codeElement.AccessedProperty.DefaultValue};",
                        $"this.set{codeElement.AccessedProperty?.Name?.ToFirstCharacterUpperCase()}(value);");
                    writer.DecreaseIndent();
                    writer.WriteLines("}", "return value;");
                } else
                    writer.WriteLine($"return this.get{backingStore.Name.ToFirstCharacterUpperCase()}().get(\"{codeElement.AccessedProperty?.Name?.ToFirstCharacterLowerCase()}\");");

        }
        private void WriteIndexerBody(CodeMethod codeElement, LanguageWriter writer, string returnType) {
            var pathSegment = codeElement.PathSegment;
            conventions.AddRequestBuilderBody(returnType, writer, $" + \"/{(string.IsNullOrEmpty(pathSegment) ? string.Empty : pathSegment + "/" )}\" + id");
        }
        private void WriteDeserializerBody(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer) {
            var inherits = (parentClass.StartBlock as CodeClass.Declaration).Inherits != null;
            var fieldToSerialize = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom);
            writer.WriteLine($"return new HashMap<>({(inherits ? "super." + codeElement.Name.ToFirstCharacterLowerCase()+ "()" : fieldToSerialize.Count())}) {{{{");
            if(fieldToSerialize.Any()) {
                writer.IncreaseIndent();
                fieldToSerialize
                        .OrderBy(x => x.Name)
                        .Select(x => 
                            $"this.put(\"{x.SerializationName ?? x.Name.ToFirstCharacterLowerCase()}\", (o, n) -> {{ (({parentClass.Name.ToFirstCharacterUpperCase()})o).set{x.Name.ToFirstCharacterUpperCase()}({GetDeserializationMethodName(x.Type)}); }});")
                        .ToList()
                        .ForEach(x => writer.WriteLine(x));
                writer.DecreaseIndent();
            }
            writer.WriteLine("}};");
        }
        private void WriteRequestExecutorBody(CodeMethod codeElement, CodeParameter requestBodyParam, CodeParameter queryStringParam, CodeParameter headersParam, string returnType, LanguageWriter writer) {
            if(codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");
            
            var generatorMethodName = (codeElement.Parent as CodeClass)
                                                .GetChildElements(true)
                                                .OfType<CodeMethod>()
                                                .FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestGenerator) && x.HttpMethod == codeElement.HttpMethod)
                                                ?.Name
                                                ?.ToFirstCharacterLowerCase();
            writer.WriteLine("try {");
            writer.IncreaseIndent();
            writer.WriteLine($"final RequestInfo requestInfo = {generatorMethodName}(");
            var requestInfoParameters = new List<string> { requestBodyParam?.Name, queryStringParam?.Name, headersParam?.Name }.Where(x => x != null);
            if(requestInfoParameters.Any()) {
                writer.IncreaseIndent();
                writer.WriteLine(requestInfoParameters.Aggregate((x,y) => $"{x}, {y}"));
                writer.DecreaseIndent();
            }
            writer.WriteLine(");");
            var sendMethodName = conventions.PrimitiveTypes.Contains(returnType) ? "sendPrimitiveAsync" : "sendAsync";
            if(codeElement.Parameters.Any(x => x.IsOfKind(CodeParameterKind.ResponseHandler)))
                writer.WriteLine($"return this.httpCore.{sendMethodName}(requestInfo, {returnType}.class, responseHandler);");
            else
                writer.WriteLine($"return this.httpCore.{sendMethodName}(requestInfo, {returnType}.class, null);");
            writer.DecreaseIndent();
            writer.WriteLine("} catch (URISyntaxException ex) {");
            writer.IncreaseIndent();
            writer.WriteLine("return java.util.concurrent.CompletableFuture.failedFuture(ex);");
            writer.DecreaseIndent();
            writer.WriteLine("}");
        }
        private void WriteRequestGeneratorBody(CodeMethod codeElement, CodeParameter requestBodyParam, CodeParameter queryStringParam, CodeParameter headersParam, LanguageWriter writer) {
            if(codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");
            
            writer.WriteLine("final RequestInfo requestInfo = new RequestInfo() {{");
            writer.IncreaseIndent();
            writer.WriteLines($"uri = new URI({conventions.CurrentPathPropertyName} + {conventions.PathSegmentPropertyName});",
                        $"httpMethod = HttpMethod.{codeElement.HttpMethod?.ToString().ToUpperInvariant()};");
            writer.DecreaseIndent();
            writer.WriteLine("}};");
            if(requestBodyParam != null)
                if(requestBodyParam.Type.Name.Equals(conventions.StreamTypeName, StringComparison.OrdinalIgnoreCase))
                    writer.WriteLine($"requestInfo.setStreamContent({requestBodyParam.Name});");
                else
                    writer.WriteLine($"requestInfo.setContentFromParsable({requestBodyParam.Name}, {conventions.SerializerFactoryPropertyName}, \"{codeElement.ContentType}\");");
            if(queryStringParam != null) {
                var httpMethodPrefix = codeElement.HttpMethod.ToString().ToFirstCharacterUpperCase();
                writer.WriteLine($"if ({queryStringParam.Name} != null) {{");
                writer.IncreaseIndent();
                writer.WriteLines($"final {httpMethodPrefix}QueryParameters qParams = new {httpMethodPrefix}QueryParameters();",
                            $"{queryStringParam.Name}.accept(qParams);",
                            "qParams.AddQueryParameters(requestInfo.queryParameters);");
                writer.DecreaseIndent();
                writer.WriteLine("}");
            }
            if(headersParam != null) {
                writer.WriteLine($"if ({headersParam.Name} != null) {{");
                writer.IncreaseIndent();
                writer.WriteLine($"{headersParam.Name}.accept(requestInfo.headers);");
                writer.DecreaseIndent();
                writer.WriteLine("}");
            }
            writer.WriteLine("return requestInfo;");
        }
        private void WriteSerializerBody(CodeClass parentClass, LanguageWriter writer) {
            var additionalDataProperty = parentClass.GetPropertiesOfKind(CodePropertyKind.AdditionalData).FirstOrDefault();
            if((parentClass.StartBlock as CodeClass.Declaration).Inherits != null)
                writer.WriteLine("super.serialize(writer);");
            foreach(var otherProp in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)) {
                writer.WriteLine($"writer.{GetSerializationMethodName(otherProp.Type)}(\"{otherProp.SerializationName ?? otherProp.Name.ToFirstCharacterLowerCase()}\", this.get{otherProp.Name.ToFirstCharacterUpperCase()}());");
            }
            if(additionalDataProperty != null)
                writer.WriteLine($"writer.writeAdditionalData(this.get{additionalDataProperty.Name.ToFirstCharacterUpperCase()}());");
        }
        private void WriteMethodPrototype(CodeMethod code, LanguageWriter writer, string returnType) {
            var accessModifier = conventions.GetAccessModifier(code.Access);
            var genericTypeParameterDeclaration = code.IsOfKind(CodeMethodKind.Deserializer) ? " <T>": string.Empty;
            var returnTypeAsyncPrefix = code.IsAsync ? "java.util.concurrent.CompletableFuture<" : string.Empty;
            var returnTypeAsyncSuffix = code.IsAsync ? ">" : string.Empty;
            var isConstructor = code.IsOfKind(CodeMethodKind.Constructor);
            var methodName = (code.MethodKind switch {
                (CodeMethodKind.Constructor) => code.Parent.Name.ToFirstCharacterUpperCase(),
                (CodeMethodKind.Getter) => $"get{code.AccessedProperty?.Name?.ToFirstCharacterUpperCase()}",
                (CodeMethodKind.Setter) => $"set{code.AccessedProperty?.Name?.ToFirstCharacterUpperCase()}",
                _ => code.Name.ToFirstCharacterLowerCase()
            });
            var parameters = string.Join(", ", code.Parameters.Select(p=> conventions.GetParameterSignature(p)).ToList());
            var throwableDeclarations = code.IsOfKind(CodeMethodKind.RequestGenerator) ? "throws URISyntaxException ": string.Empty;
            var finalReturnType = isConstructor ? string.Empty : $" {returnTypeAsyncPrefix}{returnType}{returnTypeAsyncSuffix}";
            writer.WriteLine($"{accessModifier}{genericTypeParameterDeclaration}{finalReturnType} {methodName}({parameters}) {throwableDeclarations}{{");
        }
        private void WriteMethodDocumentation(CodeMethod code, LanguageWriter writer) {
            var isDescriptionPresent = !string.IsNullOrEmpty(code.Description);
            var parametersWithDescription = code.Parameters.Where(x => !string.IsNullOrEmpty(code.Description));
            if (isDescriptionPresent || parametersWithDescription.Any()) {
                writer.WriteLine(conventions.DocCommentStart);
                if(isDescriptionPresent)
                    writer.WriteLine($"{conventions.DocCommentPrefix}{JavaConventionService.RemoveInvalidDescriptionCharacters(code.Description)}");
                foreach(var paramWithDescription in parametersWithDescription.OrderBy(x => x.Name))
                    writer.WriteLine($"{conventions.DocCommentPrefix}@param {paramWithDescription.Name} {JavaConventionService.RemoveInvalidDescriptionCharacters(paramWithDescription.Description)}");
                
                if(code.IsAsync)
                    writer.WriteLine($"{conventions.DocCommentPrefix}@return a CompletableFuture of {code.ReturnType.Name}");
                else
                    writer.WriteLine($"{conventions.DocCommentPrefix}@return a {code.ReturnType.Name}");
                writer.WriteLine(conventions.DocCommentEnd);
            }
        }
        private string GetDeserializationMethodName(CodeTypeBase propType) {
            var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
            var propertyType = conventions.TranslateType(propType.Name);
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
            var propertyType = conventions.TranslateType(propType.Name);
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
    }
}
