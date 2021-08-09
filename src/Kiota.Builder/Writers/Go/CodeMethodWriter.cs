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
            var inherits = (parentClass.StartBlock as CodeClass.Declaration).Inherits != null;
            var returnType = conventions.GetTypeString(codeElement.ReturnType, parentClass);
            WriteMethodPrototype(codeElement, writer, returnType, parentClass);
            writer.IncreaseIndent();
            var requestBodyParam = codeElement.Parameters.OfKind(CodeParameterKind.RequestBody);
            var queryStringParam = codeElement.Parameters.OfKind(CodeParameterKind.QueryParameter);
            var headersParam = codeElement.Parameters.OfKind(CodeParameterKind.Headers);
            var optionsParam = codeElement.Parameters.OfKind(CodeParameterKind.Options);
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
                case CodeMethodKind.ClientConstructor:
                    WriteConstructorBody(parentClass, codeElement, writer, inherits);
                    WriteApiConstructorBody(parentClass, codeElement, writer);
                    writer.WriteLine("return m");
                break;
                case CodeMethodKind.Constructor:
                    WriteConstructorBody(parentClass, codeElement, writer, inherits);
                    writer.WriteLine("return m");
                    break;
                default:
                    writer.WriteLine("return nil");
                break;
            }
            writer.DecreaseIndent();
            writer.WriteLine("}");
        }
        private void WriteSerializerBody(CodeClass parentClass, LanguageWriter writer) {
            var additionalDataProperty = parentClass.GetPropertiesOfKind(CodePropertyKind.AdditionalData).FirstOrDefault();
            var shouldDeclareErrorVar = true;
            if(parentClass.StartBlock is CodeClass.Declaration declaration &&
                declaration.Inherits != null) {
                writer.WriteLine($"err := m.{declaration.Inherits.Name.ToFirstCharacterUpperCase()}.Serialize(writer)");
                WriteReturnError(writer);
                shouldDeclareErrorVar = false;
            }
            foreach(var otherProp in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)) {
                WriteSerializationMethodCall(otherProp.Type, parentClass, otherProp.SerializationName ?? otherProp.Name.ToFirstCharacterLowerCase(), $"m.Get{otherProp.Name.ToFirstCharacterUpperCase()}()", shouldDeclareErrorVar, writer);
                if(shouldDeclareErrorVar)
                    shouldDeclareErrorVar = false;
            }
            if(additionalDataProperty != null) {
                writer.WriteLine($"err {errorVarDeclaration(shouldDeclareErrorVar)}= writer.WriteAdditionalData(m.Get{additionalDataProperty.Name.ToFirstCharacterUpperCase()}())");
                WriteReturnError(writer);
            }
            writer.WriteLine("return nil");
        }
        private static string errorVarDeclaration(bool shouldDeclareErrorVar) => shouldDeclareErrorVar ? ":" : string.Empty;
        private void WriteMethodPrototype(CodeMethod code, LanguageWriter writer, string returnType, CodeClass parentClass) {
            var genericTypeParameterDeclaration = code.IsOfKind(CodeMethodKind.Deserializer) ? " <T>": string.Empty;
            var returnTypeAsyncPrefix = code.IsAsync ? "func() (" : string.Empty;
            var returnTypeAsyncSuffix = code.IsAsync ? "error)" : string.Empty;
            if(!string.IsNullOrEmpty(returnType) && code.IsAsync)
                returnTypeAsyncSuffix = $", {returnTypeAsyncSuffix}";
            var isConstructor = code.IsOfKind(CodeMethodKind.Constructor, CodeMethodKind.ClientConstructor);
            var methodName = (code.MethodKind switch {
                (CodeMethodKind.Constructor or CodeMethodKind.ClientConstructor) => $"New{code.Parent.Name.ToFirstCharacterUpperCase()}",
                (CodeMethodKind.Getter) => $"Get{code.AccessedProperty?.Name?.ToFirstCharacterUpperCase()}",
                (CodeMethodKind.Setter) => $"Set{code.AccessedProperty?.Name?.ToFirstCharacterUpperCase()}",
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
                                                CodeMethodKind.IndexerBackwardCompatibility,
                                                CodeMethodKind.Deserializer) || code.IsAsync ? 
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
                        "if value == nil {");
                    writer.IncreaseIndent();
                    writer.WriteLines($"value = {codeElement.AccessedProperty.DefaultValue};",
                        $"m.Set{codeElement.AccessedProperty?.Name?.ToFirstCharacterUpperCase()}(value);");
                    writer.DecreaseIndent();
                    writer.WriteLines("}", "return value;");
                } else
                    writer.WriteLine($"return m.Get{backingStore.Name.ToFirstCharacterUpperCase()}().Get(\"{codeElement.AccessedProperty?.Name?.ToFirstCharacterLowerCase()}\");");

        }
        private void WriteApiConstructorBody(CodeClass parentClass, CodeMethod method, LanguageWriter writer) {
            var httpCoreProperty = parentClass.GetChildElements(true).OfType<CodeProperty>().FirstOrDefault(x => x.IsOfKind(CodePropertyKind.HttpCore));
            var httpCoreParameter = method.Parameters.FirstOrDefault(x => x.IsOfKind(CodeParameterKind.HttpCore));
            var httpCorePropertyName = httpCoreProperty.Name.ToFirstCharacterLowerCase();
            writer.WriteLine($"m.{httpCorePropertyName} = {httpCoreParameter.Name};");
            WriteSerializationRegistration(method.SerializerModules, writer, "RegisterDefaultSerializer");
            WriteSerializationRegistration(method.DeserializerModules, writer, "RegisterDefaultDeserializer");
            if(_usesBackingStore)
                writer.WriteLine($"m.{httpCorePropertyName}.EnableBackingStore();");
        }
        private static void WriteSerializationRegistration(List<string> serializationModules, LanguageWriter writer, string methodName) {
            if(serializationModules != null)
                foreach(var module in serializationModules)
                    writer.WriteLine($"{methodName}(func() {{ return new({module}) }})");
        }
        private static void WriteConstructorBody(CodeClass parentClass, CodeMethod currentMethod, LanguageWriter writer, bool inherits) {
            writer.WriteLine($"m := &{parentClass.Name.ToFirstCharacterUpperCase()}{{");
            if(inherits &&
                parentClass.StartBlock is CodeClass.Declaration declaration) {
                writer.IncreaseIndent();
                var parentClassName = declaration.Inherits.Name.ToFirstCharacterUpperCase();
                writer.WriteLine($"{parentClassName}: *New{parentClassName}(),");
                writer.DecreaseIndent();
            }
            writer.WriteLine("}");
            foreach(var propWithDefault in parentClass.GetPropertiesOfKind(CodePropertyKind.BackingStore,
                                                                            CodePropertyKind.RequestBuilder,
                                                                            CodePropertyKind.PathSegment)
                                            .Where(x => !string.IsNullOrEmpty(x.DefaultValue))
                                            .OrderBy(x => x.Name)) {
                writer.WriteLine($"m.{propWithDefault.NamePrefix}{propWithDefault.Name.ToFirstCharacterLowerCase()} = {propWithDefault.DefaultValue};");
            }
            foreach(var propWithDefault in parentClass.GetPropertiesOfKind(CodePropertyKind.AdditionalData) //additional data and backing Store rely on accessors
                                            .Where(x => !string.IsNullOrEmpty(x.DefaultValue))
                                            .OrderBy(x => x.Name)) {
                writer.WriteLine($"m.Set{propWithDefault.Name.ToFirstCharacterUpperCase()}({propWithDefault.DefaultValue});");
            }
            if(currentMethod.IsOfKind(CodeMethodKind.Constructor)) {
                AssignPropertyFromParameter(parentClass, currentMethod, CodeParameterKind.HttpCore, CodePropertyKind.HttpCore, writer);
                AssignPropertyFromParameter(parentClass, currentMethod, CodeParameterKind.CurrentPath, CodePropertyKind.CurrentPath, writer);
            }
        }
        private static void AssignPropertyFromParameter(CodeClass parentClass, CodeMethod currentMethod, CodeParameterKind parameterKind, CodePropertyKind propertyKind, LanguageWriter writer) {
            var property = parentClass.GetChildElements(true).OfType<CodeProperty>().FirstOrDefault(x => x.IsOfKind(propertyKind));
            var parameter = currentMethod.Parameters.FirstOrDefault(x => x.IsOfKind(parameterKind));
            if(property != null && parameter != null) {
                writer.WriteLine($"m.{property.Name.ToFirstCharacterLowerCase()} = {parameter.Name};");
            }
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
        private void WriteDeserializerBody(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer) {
            var fieldToSerialize = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom);
            if(parentClass.StartBlock is CodeClass.Declaration declaration &&
                declaration.Inherits != null)
                writer.WriteLine($"res := m.{declaration.Inherits.Name.ToFirstCharacterUpperCase()}.{codeElement.Name.ToFirstCharacterUpperCase()}()");
            else
                writer.WriteLine($"res := make({codeElement.ReturnType.Name})");
            if(fieldToSerialize.Any()) {
                var parsableImportSymbol = GetConversionHelperMethodImport(codeElement, parentClass, "ParseNode");
                fieldToSerialize
                        .OrderBy(x => x.Name)
                        .ToList()
                        .ForEach(x => WriteFieldDeserializer(x, writer, parentClass, parsableImportSymbol));
            }
            writer.WriteLine("return res");
        }
        private void WriteFieldDeserializer(CodeProperty property, LanguageWriter writer, CodeClass parentClass, string parsableImportSymbol) {
            writer.WriteLine($"res[\"{property.SerializationName ?? property.Name.ToFirstCharacterLowerCase()}\"] = func (o interface{{}}, n {parsableImportSymbol}) error {{");
            writer.IncreaseIndent();
            var propertyTypeImportName = conventions.GetTypeString(property.Type.AllTypes.First(), parentClass, false, false);
            var deserializationMethodName = GetDeserializationMethodName(property.Type, parentClass);
            writer.WriteLine($"val, err := {deserializationMethodName}");
            WriteReturnError(writer);
            var valueCast = deserializationMethodName.Contains(getObjectValueMethodName, StringComparison.OrdinalIgnoreCase) ||
                            property.Type.AllTypes.First().TypeDefinition is CodeEnum ? 
                            $".(*{propertyTypeImportName})":
                            string.Empty;
            var valueArgument = $"val{valueCast}";
            if(property.Type.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None) {
                writer.WriteLines($"res := make([]{propertyTypeImportName}, len(val))",
                                "for i, v := range val {");
                writer.IncreaseIndent();
                var castingExpression = deserializationMethodName.Contains(getCollectionOfObjectValuesMethodName, StringComparison.OrdinalIgnoreCase) ?
                            $"*(v.(*{propertyTypeImportName}))" :
                            $"v.({propertyTypeImportName})";
                writer.WriteLine($"res[i] = {castingExpression}");
                writer.DecreaseIndent();
                writer.WriteLine("}");
                valueArgument = "res";
            }
            writer.WriteLines($"o.(*{parentClass.Name.ToFirstCharacterUpperCase()}).Set{property.Name.ToFirstCharacterUpperCase()}({valueArgument})", 
                            "return nil");
            writer.DecreaseIndent();
            writer.WriteLine("}");
        }
        private void WriteRequestExecutorBody(CodeMethod codeElement, CodeParameter requestBodyParam, CodeParameter queryStringParam, CodeParameter headersParam, CodeParameter optionsParam, string returnType, LanguageWriter writer) {
            if(codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");
            var isScalar = conventions.IsScalarType(returnType);
            var sendMethodName = returnType switch {
                "void" => "SendNoContentAsync",
                _ when string.IsNullOrEmpty(returnType) => "SendNoContentAsync",
                _ when isScalar => "SendPrimitiveAsync",
                _ => "SendAsync"
            };
            var responseHandlerParam = codeElement.Parameters.FirstOrDefault(x => x.IsOfKind(CodeParameterKind.ResponseHandler));
            var typeShortName = returnType.Split('.').Last().ToFirstCharacterUpperCase();
            var isVoid = string.IsNullOrEmpty(typeShortName);
            WriteGeneratorMethodCall(codeElement, requestBodyParam, queryStringParam, headersParam, optionsParam, writer, $"{rInfoVarName}, err := ");
            WriteAsyncReturnError(writer, returnType);
            var parsableImportSymbol = GetConversionHelperMethodImport(codeElement, codeElement.Parent as CodeClass, "Parsable");
            var constructorFunction = returnType switch {
                _ when isVoid => string.Empty,
                _ when isScalar => $"\"{parsableImportSymbol}\", ",
                _ => $"func () {parsableImportSymbol} {{ return new({conventions.GetTypeString(codeElement.ReturnType, codeElement.Parent, false)}) }}, ",
            };
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
            writer.WriteLines($"uri, err := url.Parse(m.{conventions.CurrentPathPropertyName} + m.{conventions.PathSegmentPropertyName})",
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
        private string GetDeserializationMethodName(CodeTypeBase propType, CodeClass parentClass) {
            var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
            var propertyTypeName = conventions.TranslateType(propType.AllTypes.First().Name);
            if(propType is CodeType currentType) {
                if(isCollection)
                    if(currentType.TypeDefinition == null)
                        return $"n.GetCollectionOfPrimitiveValues(\"{propertyTypeName.ToFirstCharacterLowerCase()}\")";
                    else
                        return $"n.{getCollectionOfObjectValuesMethodName}({GetTypeFactory(propType, parentClass, propertyTypeName)})";
                else if (currentType.TypeDefinition is CodeEnum currentEnum)
                    return $"n.GetEnum{(currentEnum.Flags ? "Set" : string.Empty)}Value(Parse{propertyTypeName.ToFirstCharacterUpperCase()})";
            }
            return propertyTypeName switch {
                ("string" or "bool" or "int32" or "float32" or "int64" or "UUID" or "Time") => 
                    $"n.Get{propertyTypeName.ToFirstCharacterUpperCase()}Value()",
                _ => $"n.{getObjectValueMethodName}({GetTypeFactory(propType, parentClass, propertyTypeName)})",
            };
        }
        private const string getObjectValueMethodName = "GetObjectValue";
        private const string getCollectionOfObjectValuesMethodName = "GetCollectionOfObjectValues";
        private static char dot = '.';
        private string GetTypeFactory(CodeTypeBase propTypeBase, CodeClass parentClass, string propertyTypeName) {
            if(propTypeBase is CodeType propType) {
                var importSymbol = conventions.GetTypeString(propType, parentClass, false, false);
                var importNS = importSymbol.Contains(dot) ? importSymbol.Split(dot).First() + dot : string.Empty;
                return $"func () interface{{}} {{ return {importNS}New{propertyTypeName.ToFirstCharacterUpperCase()}() }}";
            } else return GetTypeFactory(propTypeBase.AllTypes.First(), parentClass, propertyTypeName);
        }
        const string parsableConversionMethodName = "ConvertToArrayOfParsable";
        const string primitiveConversionMethodName = "ConvertToArrayOfPrimitives";
        private void WriteSerializationMethodCall(CodeTypeBase propType, CodeClass parentClass, string serializationKey, string valueGet, bool shouldDeclareErrorVar, LanguageWriter writer) {
            serializationKey = $"\"{serializationKey}\"";
            var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
            var propertyType = conventions.TranslateType(propType.Name);
            var errorPrefix = $"err {errorVarDeclaration(shouldDeclareErrorVar)}= writer.";
            if(propType is CodeType currentType) {
                if(isCollection) {
                    if(currentType.TypeDefinition == null) {
                        var conversionMethodImport = GetConversionHelperMethodImport(propType, parentClass, primitiveConversionMethodName);
                        writer.WriteLine($"{errorPrefix}WriteCollectionOfPrimitiveValues({serializationKey}, {conversionMethodImport}({valueGet}))");
                        WriteReturnError(writer);
                    } else {
                        var conversionMethodImport = GetConversionHelperMethodImport(propType, parentClass, parsableConversionMethodName);
                        writer.WriteLine($"{errorPrefix}WriteCollectionOfObjectValues({serializationKey}, {conversionMethodImport}({valueGet}))");
                        WriteReturnError(writer);
                    }
                    return;
                } else if (currentType.TypeDefinition is CodeEnum currentEnum) {
                    writer.WriteLine($"if {valueGet} != nil {{");
                    writer.IncreaseIndent();
                    writer.WriteLine($"{errorPrefix}WritePrimitiveValue({serializationKey}, {valueGet}.String())");
                    WriteReturnError(writer);
                    writer.DecreaseIndent();
                    writer.WriteLine("}");
                    return;
                }
            }
            switch(propertyType) {
                case "string":
                case "bool":
                case "int32":
                case "float32":
                case "int64":
                case "UUID":
                case "Time": 
                    writer.WriteLine($"{errorPrefix}WritePrimitiveValue({serializationKey}, {valueGet})");
                    WriteReturnError(writer);
                break;
                default:
                    writer.WriteLine($"{errorPrefix}WriteObjectValue({serializationKey}, {valueGet})");
                    WriteReturnError(writer);
                break;
            };
        }
        private string GetConversionHelperMethodImport(CodeElement parentForTemporaryType, CodeClass parentClass, string name) {
            var conversionMethodType = new CodeType(parentForTemporaryType) { Name = name, IsExternal = true };
            return conventions.GetTypeString(conversionMethodType, parentClass, false);
        }
    }
}
