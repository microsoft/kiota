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
            var returnType = localConventions.GetTypeString(codeElement.ReturnType, codeElement);
            var isVoid = "void".Equals(returnType, StringComparison.OrdinalIgnoreCase);
            WriteMethodDocumentation(codeElement, writer, isVoid);
            WriteMethodPrototype(codeElement, writer, returnType, isVoid);
            writer.IncreaseIndent();
            var parentClass = codeElement.Parent as CodeClass;
            var inherits = (parentClass.StartBlock as CodeClass.Declaration).Inherits != null;
            var requestBodyParam = codeElement.Parameters.OfKind(CodeParameterKind.RequestBody);
            var queryStringParam = codeElement.Parameters.OfKind(CodeParameterKind.QueryParameter);
            var headersParam = codeElement.Parameters.OfKind(CodeParameterKind.Headers);
            var optionsParam = codeElement.Parameters.OfKind(CodeParameterKind.Options);
            var currentPathProperty = parentClass.Properties.FirstOrDefault(x => x.IsOfKind(CodePropertyKind.CurrentPath));
            if(!codeElement.IsOfKind(CodeMethodKind.Setter))
                foreach(var parameter in codeElement.Parameters.Where(x => !x.Optional).OrderBy(x => x.Name)) {
                    writer.WriteLine($"if(!{parameter.Name}) throw new Error(\"{parameter.Name} cannot be undefined\");");
                }
            switch(codeElement.MethodKind) {
                case CodeMethodKind.IndexerBackwardCompatibility:
                    var pathSegment = codeElement.PathSegment;
                    localConventions.AddRequestBuilderBody(currentPathProperty != null, returnType, writer, $" + \"/{(string.IsNullOrEmpty(pathSegment) ? string.Empty : pathSegment + "/" )}\" + id");
                    break;
                case CodeMethodKind.Deserializer:
                    WriteDeserializerBody(codeElement, parentClass, writer);
                    break;
                case CodeMethodKind.Serializer:
                    WriteSerializerBody(inherits, parentClass, writer);
                    break;
                case CodeMethodKind.RequestGenerator:
                    WriteRequestGeneratorBody(codeElement, requestBodyParam, queryStringParam, headersParam, optionsParam, parentClass, writer);
                break;
                case CodeMethodKind.RequestExecutor:
                    WriteRequestExecutorBody(codeElement, new List<CodeParameter> {requestBodyParam, queryStringParam, headersParam, optionsParam}, isVoid, returnType, writer);
                    break;
                case CodeMethodKind.Getter:
                    WriteGetterBody(codeElement, writer, parentClass);
                    break;
                case CodeMethodKind.Setter:
                    WriteSetterBody(codeElement, writer, parentClass);
                    break;
                case CodeMethodKind.ClientConstructor:
                    WriteConstructorBody(parentClass, codeElement, writer, inherits);
                    WriteApiConstructorBody(parentClass, codeElement, writer);
                break;
                case CodeMethodKind.Constructor:
                    WriteConstructorBody(parentClass, codeElement, writer, inherits);
                    break;
                case CodeMethodKind.RequestBuilderWithParameters:
                    WriteRequestBuilderWithParametersBody(codeElement, currentPathProperty, returnType, writer);
                    break;
                case CodeMethodKind.RequestBuilderBackwardCompatibility:
                    throw new InvalidOperationException("RequestBuilderBackwardCompatibility is not supported as the request builders are implemented by properties.");
                default:
                    WriteDefaultMethodBody(codeElement, writer);
                    break;
            }
            writer.DecreaseIndent();
            writer.WriteLine("};");
        }
        private void WriteRequestBuilderWithParametersBody(CodeMethod codeElement, CodeProperty currentPathProperty, string returnType, LanguageWriter writer)
        {
            var codePathParameters = codeElement.Parameters
                                                        .Where(x => x.IsOfKind(CodeParameterKind.Path))
                                                        .Select(x => x.Name);
            var codePathParametersSuffix = codePathParameters.Any() ? 
                                            ", " + codePathParameters.Aggregate((x, y) => $"{x}, {y}") :
                                            string.Empty;
            localConventions.AddRequestBuilderBody(currentPathProperty != null, returnType, writer, additionalPathParameters: codePathParametersSuffix);
        }
        private static void WriteApiConstructorBody(CodeClass parentClass, CodeMethod method, LanguageWriter writer) {
            var httpCoreProperty = parentClass.Properties.FirstOrDefault(x => x.IsOfKind(CodePropertyKind.HttpCore));
            var httpCoreParameter = method.Parameters.FirstOrDefault(x => x.IsOfKind(CodeParameterKind.HttpCore));
            var backingStoreParameter = method.Parameters.FirstOrDefault(x => x.IsOfKind(CodeParameterKind.BackingStore));
            var httpCorePropertyName = httpCoreProperty.Name.ToFirstCharacterLowerCase();
            writer.WriteLine($"this.{httpCorePropertyName} = {httpCoreParameter.Name};");
            WriteSerializationRegistration(method.SerializerModules, writer, "registerDefaultSerializer");
            WriteSerializationRegistration(method.DeserializerModules, writer, "registerDefaultDeserializer");
            if(backingStoreParameter != null)
                writer.WriteLine($"this.{httpCorePropertyName}.enableBackingStore({backingStoreParameter.Name});");
        }
        private static void WriteSerializationRegistration(List<string> serializationModules, LanguageWriter writer, string methodName) {
            if(serializationModules != null)
                foreach(var module in serializationModules)
                    writer.WriteLine($"{methodName}({module});");
        }
        private static void WriteConstructorBody(CodeClass parentClass, CodeMethod currentMethod, LanguageWriter writer, bool inherits) {
            if(inherits)
                writer.WriteLine("super();");
            var propertiesWithDefaultValues = new List<CodePropertyKind> {
                CodePropertyKind.AdditionalData,
                CodePropertyKind.BackingStore,
                CodePropertyKind.RequestBuilder,
            };
            if(currentMethod.Parameters.Any(x => x.IsOfKind(CodeParameterKind.Path)) &&
                parentClass.GetPropertiesOfKind(CodePropertyKind.PathSegment).FirstOrDefault() is CodeProperty pathSegmentProperty &&
                !string.IsNullOrEmpty(pathSegmentProperty.DefaultValue)) {
                var defaultValue = pathSegmentProperty.DefaultValue
                                                        .Replace("\"", "`")
                                                        .Replace("{", "${")
                                                        .Replace("}", " ?? ''}");
                writer.WriteLine($"this.{pathSegmentProperty.NamePrefix}{pathSegmentProperty.Name.ToFirstCharacterLowerCase()} = {defaultValue};");
            } else
                propertiesWithDefaultValues.Add(CodePropertyKind.PathSegment);
            foreach(var propWithDefault in parentClass.GetPropertiesOfKind(propertiesWithDefaultValues.ToArray())
                                            .Where(x => !string.IsNullOrEmpty(x.DefaultValue))
                                            .OrderByDescending(x => x.PropertyKind)
                                            .ThenBy(x => x.Name)) {
                writer.WriteLine($"this.{propWithDefault.NamePrefix}{propWithDefault.Name.ToFirstCharacterLowerCase()} = {propWithDefault.DefaultValue};");
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
                writer.WriteLine($"this.{property.Name.ToFirstCharacterLowerCase()} = {parameter.Name};");
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
                    writer.WriteLines($"let value = this.{backingStore.NamePrefix}{backingStore.Name.ToFirstCharacterLowerCase()}.get<{conventions.GetTypeString(codeElement.AccessedProperty.Type, codeElement)}>(\"{codeElement.AccessedProperty.Name.ToFirstCharacterLowerCase()}\");",
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
        private void WriteRequestExecutorBody(CodeMethod codeElement, IEnumerable<CodeParameter> parameters, bool isVoid, string returnType, LanguageWriter writer) {
            if(codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");

            var generatorMethodName = (codeElement.Parent as CodeClass)
                                                .Methods
                                                .FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestGenerator) && x.HttpMethod == codeElement.HttpMethod)
                                                ?.Name
                                                ?.ToFirstCharacterLowerCase();
            writer.WriteLine($"const requestInfo = this.{generatorMethodName}(");
            var requestInfoParameters = parameters.Select(x => x?.Name).Where(x => x != null);
            if(requestInfoParameters.Any()) {
                writer.IncreaseIndent();
                writer.WriteLine(requestInfoParameters.Aggregate((x,y) => $"{x}, {y}"));
                writer.DecreaseIndent();
            }
            writer.WriteLine(");");
            var isStream = localConventions.StreamTypeName.Equals(returnType, StringComparison.OrdinalIgnoreCase);
            var returnTypeWithoutCollectionSymbol = GetReturnTypeWithoutCollectionSymbol(codeElement, returnType);
            var genericTypeForSendMethod = GetSendRequestMethodName(isVoid, isStream, codeElement.ReturnType.IsCollection, returnTypeWithoutCollectionSymbol);
            var newFactoryParameter = GetTypeFactory(isVoid, isStream, returnTypeWithoutCollectionSymbol);
            writer.WriteLine($"return this.httpCore?.{genericTypeForSendMethod}(requestInfo,{newFactoryParameter} responseHandler) ?? Promise.reject(new Error('http core is null'));");
        }
        private string GetReturnTypeWithoutCollectionSymbol(CodeMethod codeElement, string fullTypeName) {
            if(!codeElement.ReturnType.IsCollection) return fullTypeName;
            var clone = codeElement.ReturnType.Clone() as CodeTypeBase;
            clone.CollectionKind = CodeTypeBase.CodeTypeCollectionKind.None;
            return conventions.GetTypeString(clone, codeElement);
        }
        private const string RequestInfoVarName = "requestInfo";
        private void WriteRequestGeneratorBody(CodeMethod codeElement, CodeParameter requestBodyParam, CodeParameter queryStringParam, CodeParameter headersParam, CodeParameter optionsParam, CodeClass currentClass, LanguageWriter writer) {
            if(codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");
            
            var currentPathProperty = currentClass.GetPropertiesOfKind(CodePropertyKind.CurrentPath).FirstOrDefault();
            var pathSegmentProperty = currentClass.GetPropertiesOfKind(CodePropertyKind.PathSegment).FirstOrDefault();
            var rawUrlProperty = currentClass.GetPropertiesOfKind(CodePropertyKind.RawUrl).FirstOrDefault();
            var httpCoreProperty = currentClass.GetPropertiesOfKind(CodePropertyKind.HttpCore).FirstOrDefault();
            writer.WriteLines($"const {RequestInfoVarName} = new RequestInformation();",
                                $"{RequestInfoVarName}.setUri({GetPropertyCall(currentPathProperty, "''")}, {GetPropertyCall(pathSegmentProperty, "''")}, {GetPropertyCall(rawUrlProperty, "false")});",
                                $"{RequestInfoVarName}.httpMethod = HttpMethod.{codeElement.HttpMethod.ToString().ToUpperInvariant()};");
            if(headersParam != null)
                writer.WriteLine($"{headersParam.Name} && {RequestInfoVarName}.setHeadersFromRawObject(h);");
            if(queryStringParam != null)
                writer.WriteLines($"{queryStringParam.Name} && {RequestInfoVarName}.setQueryStringParametersFromRawObject(q);");
            if(requestBodyParam != null) {
                if(requestBodyParam.Type.Name.Equals(localConventions.StreamTypeName, StringComparison.OrdinalIgnoreCase))
                    writer.WriteLine($"{RequestInfoVarName}.setStreamContent({requestBodyParam.Name});");
                else {
                    var spreadOperator = requestBodyParam.Type.AllTypes.First().IsCollection ? "..." : string.Empty;
                    writer.WriteLine($"{RequestInfoVarName}.setContentFromParsable(this.{httpCoreProperty.Name.ToFirstCharacterLowerCase()}, \"{codeElement.ContentType}\", {spreadOperator}{requestBodyParam.Name});");
                }
            }
            if(optionsParam != null)
                writer.WriteLine($"{optionsParam.Name} && {RequestInfoVarName}.addMiddlewareOptions(...{optionsParam.Name});");
            writer.WriteLine($"return {RequestInfoVarName};");
        }
        private static string GetPropertyCall(CodeProperty property, string defaultValue) => property == null ? defaultValue : $"this.{property.Name}";
        private void WriteSerializerBody(bool inherits, CodeClass parentClass, LanguageWriter writer) {
            var additionalDataProperty = parentClass.GetPropertiesOfKind(CodePropertyKind.AdditionalData).FirstOrDefault();
            if(inherits)
                writer.WriteLine("super.serialize(writer);");
            foreach(var otherProp in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)) {
                var spreadOperator = otherProp.Type is CodeType cType && cType.IsCollection && cType.TypeDefinition is CodeEnum ? "..." : string.Empty;
                writer.WriteLine($"writer.{GetSerializationMethodName(otherProp.Type)}(\"{otherProp.SerializationName ?? otherProp.Name.ToFirstCharacterLowerCase()}\", {spreadOperator}this.{otherProp.Name.ToFirstCharacterLowerCase()});");
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
                        writer.WriteLine($"{localConventions.DocCommentPrefix}@returns a Promise of {code.ReturnType.Name.ToFirstCharacterUpperCase()}");
                    else
                        writer.WriteLine($"{localConventions.DocCommentPrefix}@returns a {code.ReturnType.Name}");
                writer.WriteLine(localConventions.DocCommentEnd);
            }
        }
        private static readonly CodeParameterOrderComparer parameterOrderComparer = new();
        private void WriteMethodPrototype(CodeMethod code, LanguageWriter writer, string returnType, bool isVoid) {
            var accessModifier = localConventions.GetAccessModifier(code.Access);
            var methodName = (code.MethodKind switch {
                CodeMethodKind.Getter or CodeMethodKind.Setter => code.AccessedProperty?.Name,
                CodeMethodKind.Constructor or CodeMethodKind.ClientConstructor => "constructor",
                _ => code.Name,
            })?.ToFirstCharacterLowerCase();
            var asyncPrefix = code.IsAsync && code.MethodKind != CodeMethodKind.RequestExecutor ? " async ": string.Empty;
            var parameters = string.Join(", ", code.Parameters.OrderBy(x => x, parameterOrderComparer).Select(p=> localConventions.GetParameterSignature(p, code)).ToList());
            var asyncReturnTypePrefix = code.IsAsync ? "Promise<": string.Empty;
            var asyncReturnTypeSuffix = code.IsAsync ? ">": string.Empty;
            var nullableSuffix = code.ReturnType.IsNullable && !isVoid ? " | undefined" : string.Empty;
            var accessorPrefix = code.MethodKind switch {
                    CodeMethodKind.Getter => "get ",
                    CodeMethodKind.Setter => "set ",
                    _ => string.Empty
                };
            var isConstructor = code.IsOfKind(CodeMethodKind.Constructor, CodeMethodKind.ClientConstructor);
            var shouldHaveTypeSuffix = !code.IsAccessor && !isConstructor;
            var returnTypeSuffix = shouldHaveTypeSuffix ? $" : {asyncReturnTypePrefix}{returnType}{nullableSuffix}{asyncReturnTypeSuffix}" : string.Empty;
            writer.WriteLine($"{accessModifier} {accessorPrefix}{methodName}{asyncPrefix}({parameters}){returnTypeSuffix} {{");
        }
        private string GetDeserializationMethodName(CodeTypeBase propType) {
            var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
            var propertyType = localConventions.TranslateType(propType);
            if(propType is CodeType currentType) {
                if(currentType.TypeDefinition is CodeEnum currentEnum)
                    return $"getEnumValue{(currentEnum.Flags || isCollection ? "s" : string.Empty)}<{currentEnum.Name.ToFirstCharacterUpperCase()}>({propertyType.ToFirstCharacterUpperCase()})";
                else if(isCollection)
                    if(currentType.TypeDefinition == null)
                        return $"getCollectionOfPrimitiveValues<{propertyType.ToFirstCharacterLowerCase()}>()";
                    else
                        return $"getCollectionOfObjectValues<{propertyType.ToFirstCharacterUpperCase()}>({propertyType.ToFirstCharacterUpperCase()})";
            }
            return propertyType switch
            {
                "string" or "boolean" or "number" or "Guid" or "Date" => $"get{propertyType.ToFirstCharacterUpperCase()}Value()",
                _ => $"getObjectValue<{propertyType.ToFirstCharacterUpperCase()}>({propertyType.ToFirstCharacterUpperCase()})",
            };
        }
        private string GetSerializationMethodName(CodeTypeBase propType) {
            var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
            var propertyType = localConventions.TranslateType(propType);
            if(propType is CodeType currentType) {
                if(currentType.TypeDefinition is CodeEnum currentEnum)
                    return $"writeEnumValue<{currentEnum.Name.ToFirstCharacterUpperCase()}>";
                else if(isCollection)
                    if(currentType.TypeDefinition == null)
                        return $"writeCollectionOfPrimitiveValues<{propertyType.ToFirstCharacterLowerCase()}>";
                    else
                        return $"writeCollectionOfObjectValues<{propertyType.ToFirstCharacterUpperCase()}>";
            }
            return propertyType switch
            {
                "string" or "boolean" or "number" or "Guid" or "Date" => $"write{propertyType.ToFirstCharacterUpperCase()}Value",
                _ => $"writeObjectValue<{propertyType.ToFirstCharacterUpperCase()}>",
            };
        }
        private string GetTypeFactory(bool isVoid, bool isStream, string returnType) {
            if(isVoid) return string.Empty;
            else if(isStream || conventions.IsPrimitiveType(returnType)) return $" \"{returnType}\",";
            else return $" {returnType},";
        }
        private string GetSendRequestMethodName(bool isVoid, bool isStream, bool isCollection, string returnType) {
            if(isVoid) return "sendNoResponseContentAsync";
            else if(isCollection) {
                if(conventions.IsPrimitiveType(returnType)) return $"sendCollectionOfPrimitiveAsync<{returnType}>";
                else return $"sendCollectionAsync<{returnType}>";
            }
            else if(isStream || conventions.IsPrimitiveType(returnType)) return $"sendPrimitiveAsync<{returnType}>";
            else return $"sendAsync<{returnType}>";
        }
    }
}
