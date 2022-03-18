using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers.Extensions;

namespace Kiota.Builder.Writers.Go {
    public class CodeMethodWriter : BaseElementWriter<CodeMethod, GoConventionService>
    {
        public CodeMethodWriter(GoConventionService conventionService) : base(conventionService){}
        public override void WriteCodeElement(CodeMethod codeElement, LanguageWriter writer)
        {
            if(codeElement == null) throw new ArgumentNullException(nameof(codeElement));
            if(codeElement.ReturnType == null) throw new InvalidOperationException($"{nameof(codeElement.ReturnType)} should not be null");
            if(writer == null) throw new ArgumentNullException(nameof(writer));
            if(codeElement.Parent is not IProprietableBlock) throw new InvalidOperationException("the parent of a method should be a class or an interface");
            
            var returnType = conventions.GetTypeString(codeElement.ReturnType, codeElement.Parent);
            var writePrototypeOnly = codeElement.Parent is CodeInterface;
            WriteMethodPrototype(codeElement, writer, returnType, writePrototypeOnly);
            if(writePrototypeOnly) return;
            var parentClass = codeElement.Parent as CodeClass;
            var inherits = parentClass.StartBlock.Inherits != null && !parentClass.IsErrorDefinition;
            writer.IncreaseIndent();
            var requestOptionsParam = codeElement.Parameters.OfKind(CodeParameterKind.ParameterSet);
            var requestParamSetDefinition = requestOptionsParam != null && requestOptionsParam.Type is CodeType rpsType &&
                                            rpsType.TypeDefinition is CodeClass rpsTypeDef ? rpsTypeDef : null;
            var requestBodyParam = requestParamSetDefinition?.GetPropertiesOfKind(CodePropertyKind.RequestBody).FirstOrDefault();
            var queryStringParam = requestParamSetDefinition?.GetPropertiesOfKind(CodePropertyKind.QueryParameter).FirstOrDefault();
            var headersParam = requestParamSetDefinition?.GetPropertiesOfKind(CodePropertyKind.Headers).FirstOrDefault();
            var optionsParam = requestParamSetDefinition?.GetPropertiesOfKind(CodePropertyKind.Options).FirstOrDefault();
            var requestParams = new RequestProperties(requestOptionsParam, requestBodyParam, queryStringParam, headersParam, optionsParam);
            switch(codeElement.Kind) {
                case CodeMethodKind.Serializer:
                    WriteSerializerBody(parentClass, writer, inherits);
                break;
                case CodeMethodKind.Deserializer:
                    WriteDeserializerBody(codeElement, parentClass, writer, inherits);
                break;
                case CodeMethodKind.IndexerBackwardCompatibility:
                    WriteIndexerBody(codeElement, parentClass, writer, returnType);
                break;
                case CodeMethodKind.RequestGenerator:
                    WriteRequestGeneratorBody(codeElement, requestParams, writer, parentClass, returnType);
                break;
                case CodeMethodKind.RequestExecutor:
                    WriteRequestExecutorBody(codeElement, requestParams, returnType, parentClass, writer);
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
                case CodeMethodKind.RawUrlConstructor:
                    WriteRawUrlConstructorBody(parentClass, codeElement, writer);
                break;
                case CodeMethodKind.RequestBuilderBackwardCompatibility:
                    WriteRequestBuilderBody(parentClass, codeElement, writer);
                    break;
                case CodeMethodKind.RequestBuilderWithParameters:
                    WriteRequestBuilderBody(parentClass, codeElement, writer);
                    break;
                case CodeMethodKind.NullCheck:
                    WriteNullCheckBody(writer);
                    break;
                case CodeMethodKind.Factory:
                    WriteFactoryMethodBody(codeElement, writer);
                    break;
                default:
                    writer.WriteLine("return nil");
                break;
            }
            writer.CloseBlock();
        }
        private void WriteFactoryMethodBody(CodeMethod codeElement, LanguageWriter writer){
            var parseNodeParameter = codeElement.Parameters.OfKind(CodeParameterKind.ParseNode);
            if(codeElement.ShouldWriteDiscriminatorSwitch && parseNodeParameter != null) {
                writer.WriteLine($"if {parseNodeParameter.Name.ToFirstCharacterLowerCase()} != nil {{");
                writer.IncreaseIndent();
                writer.WriteLine($"mappingValueNode, err := {parseNodeParameter.Name.ToFirstCharacterLowerCase()}.GetChildNode(\"{codeElement.DiscriminatorPropertyName}\")");
                WriteReturnError(writer, codeElement.ReturnType.Name);
                writer.WriteLine("if mappingValueNode != nil {");
                writer.IncreaseIndent();
                writer.WriteLines($"mappingValue, err := mappingValueNode.GetStringValue()");
                WriteReturnError(writer, codeElement.ReturnType.Name);
                writer.WriteLine("if mappingValue != nil {");
                writer.IncreaseIndent();
                writer.WriteLines("mappingStr := *mappingValue",
                                    "switch mappingStr {");
                writer.IncreaseIndent();
                foreach(var mappedType in codeElement.DiscriminatorMappings) {
                    writer.WriteLine($"case \"{mappedType.Key}\":");
                    writer.IncreaseIndent();
                    writer.WriteLine($"return {conventions.GetImportedStaticMethodName(mappedType.Value, codeElement.Parent)}(), nil");
                    writer.DecreaseIndent();
                }
                writer.CloseBlock();
                writer.CloseBlock();
                writer.CloseBlock();
                writer.CloseBlock();
            }

            writer.WriteLine($"return New{codeElement.Parent.Name.ToFirstCharacterUpperCase()}(), nil");
        }
        private void WriteMethodDocumentation(CodeMethod code, string methodName, LanguageWriter writer) {
            if(!string.IsNullOrEmpty(code.Description))
                conventions.WriteShortDescription($"{methodName.ToFirstCharacterUpperCase()} {code.Description.ToFirstCharacterLowerCase()}", writer);
        }
        private static void WriteNullCheckBody(LanguageWriter writer)
        {
            writer.WriteLine("return m == nil");
        }
        private const string TempParamsVarName = "urlParams";
        private static void WriteRawUrlConstructorBody(CodeClass parentClass, CodeMethod codeElement, LanguageWriter writer)
        {
            var rawUrlParam = codeElement.Parameters.OfKind(CodeParameterKind.RawUrl);
            var requestAdapterParam = codeElement.Parameters.OfKind(CodeParameterKind.RequestAdapter);
            var pathParamsSuffix = string.Join(", ", codeElement.OriginalMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.Path)).Select(x => "nil").ToArray());
            if(!string.IsNullOrEmpty(pathParamsSuffix)) pathParamsSuffix = ", " + pathParamsSuffix;
            writer.WriteLines($"{TempParamsVarName} := make(map[string]string)",
                            $"{TempParamsVarName}[\"request-raw-url\"] = {rawUrlParam.Name.ToFirstCharacterLowerCase()}",
                            $"return New{parentClass.Name.ToFirstCharacterUpperCase()}Internal({TempParamsVarName}, {requestAdapterParam.Name.ToFirstCharacterLowerCase()}{pathParamsSuffix})");
        }
        private void WriteRequestBuilderBody(CodeClass parentClass, CodeMethod codeElement, LanguageWriter writer)
        {
            var importSymbol = conventions.GetTypeString(codeElement.ReturnType, parentClass);
            conventions.AddRequestBuilderBody(parentClass, importSymbol, writer, pathParameters: codeElement.Parameters.Where(x => x.IsOfKind(CodeParameterKind.Path)));
        }
        private void WriteSerializerBody(CodeClass parentClass, LanguageWriter writer, bool inherits) {
            var additionalDataProperty = parentClass.GetPropertyOfKind(CodePropertyKind.AdditionalData);
            var shouldDeclareErrorVar = !inherits;
            if(inherits) {
                writer.WriteLine($"err := m.{parentClass.StartBlock.Inherits.Name.ToFirstCharacterUpperCase()}.Serialize(writer)");
                WriteReturnError(writer);
            }
            foreach(var otherProp in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)) {
                var accessorName = otherProp.IsNameEscaped && !string.IsNullOrEmpty(otherProp.SerializationName) ? otherProp.SerializationName : otherProp.Name.ToFirstCharacterLowerCase();
                WriteSerializationMethodCall(otherProp.Type, parentClass, otherProp.SerializationName ?? otherProp.Name.ToFirstCharacterLowerCase(), $"m.Get{accessorName.ToFirstCharacterUpperCase()}()", shouldDeclareErrorVar, writer);
            }
            if(additionalDataProperty != null) {
                writer.WriteLine("{");
                writer.IncreaseIndent();
                writer.WriteLine($"err {errorVarDeclaration(shouldDeclareErrorVar)}= writer.WriteAdditionalData(m.Get{additionalDataProperty.Name.ToFirstCharacterUpperCase()}())");
                WriteReturnError(writer);
                writer.CloseBlock();
            }
            writer.WriteLine("return nil");
        }
        private static string errorVarDeclaration(bool shouldDeclareErrorVar) => shouldDeclareErrorVar ? ":" : string.Empty;
        private static readonly CodeParameterOrderComparer parameterOrderComparer = new();
        private void WriteMethodPrototype(CodeMethod code, LanguageWriter writer, string returnType, bool writePrototypeOnly) {
            var parentBlock = code.Parent;
            var returnTypeAsyncSuffix = code.IsAsync ? "error" : string.Empty;
            if(!string.IsNullOrEmpty(returnType) && code.IsAsync)
                returnTypeAsyncSuffix = $", {returnTypeAsyncSuffix}";
            var isConstructor = code.IsOfKind(CodeMethodKind.Constructor, CodeMethodKind.ClientConstructor, CodeMethodKind.RawUrlConstructor);
            var methodName = code.Kind switch {
                CodeMethodKind.Constructor when parentBlock is CodeClass parentClass && parentClass.IsOfKind(CodeClassKind.RequestBuilder) => $"New{code.Parent.Name.ToFirstCharacterUpperCase()}Internal", // internal instantiation with url template parameters
                CodeMethodKind.Factory => $"Create{parentBlock.Name.ToFirstCharacterUpperCase()}FromDiscriminatorValue",
                _ when isConstructor => $"New{code.Parent.Name.ToFirstCharacterUpperCase()}",
                _ when code.Access == AccessModifier.Public => code.Name.ToFirstCharacterUpperCase(),
                _ => code.Name.ToFirstCharacterLowerCase()
            };
            if(!writePrototypeOnly)
                WriteMethodDocumentation(code, methodName, writer);
            var parameters = string.Join(", ", code.Parameters.OrderBy(x => x, parameterOrderComparer).Select(p => conventions.GetParameterSignature(p, parentBlock)).ToList());
            var classType = conventions.GetTypeString(new CodeType { Name = parentBlock.Name, TypeDefinition = parentBlock }, parentBlock);
            var associatedTypePrefix = isConstructor ||code.IsStatic || writePrototypeOnly ? string.Empty : $"(m {classType}) ";
            var finalReturnType = isConstructor ? classType : $"{returnType}{returnTypeAsyncSuffix}";
            var errorDeclaration = code.IsOfKind(CodeMethodKind.ClientConstructor, 
                                                CodeMethodKind.Constructor, 
                                                CodeMethodKind.Getter, 
                                                CodeMethodKind.Setter,
                                                CodeMethodKind.IndexerBackwardCompatibility,
                                                CodeMethodKind.Deserializer,
                                                CodeMethodKind.RequestBuilderWithParameters,
                                                CodeMethodKind.RequestBuilderBackwardCompatibility,
                                                CodeMethodKind.RawUrlConstructor,
                                                CodeMethodKind.NullCheck) || code.IsAsync ? 
                                                    string.Empty :
                                                    "error";
            if(!string.IsNullOrEmpty(finalReturnType) && !string.IsNullOrEmpty(errorDeclaration))
                finalReturnType += ", ";
            var openingBracket = writePrototypeOnly ? string.Empty : " {";
            var funcPrefix = writePrototypeOnly ? string.Empty : "func ";
            writer.WriteLine($"{funcPrefix}{associatedTypePrefix}{methodName}({parameters})({finalReturnType}{errorDeclaration}){openingBracket}");
        }
        private void WriteGetterBody(CodeMethod codeElement, LanguageWriter writer, CodeClass parentClass) {
            var backingStore = parentClass.GetBackingStoreProperty();
            writer.WriteLine("if m == nil {");
            writer.IncreaseIndent();
            writer.WriteLine("return nil");
            writer.DecreaseIndent();
            writer.WriteLine("} else {");
            writer.IncreaseIndent();
            if(backingStore == null || (codeElement.AccessedProperty?.IsOfKind(CodePropertyKind.BackingStore) ?? false))
                writer.WriteLine($"return m.{codeElement.AccessedProperty?.Name?.ToFirstCharacterLowerCase()}");
            else 
                if(!(codeElement.AccessedProperty?.Type?.IsNullable ?? true) &&
                   !(codeElement.AccessedProperty?.ReadOnly ?? true) &&
                    !string.IsNullOrEmpty(codeElement.AccessedProperty?.DefaultValue)) {
                    writer.WriteLines($"{conventions.GetTypeString(codeElement.AccessedProperty.Type, parentClass)} value = m.{backingStore.NamePrefix}{backingStore.Name.ToFirstCharacterLowerCase()}.Get(\"{codeElement.AccessedProperty.Name.ToFirstCharacterLowerCase()}\")",
                        "if value == nil {");
                    writer.IncreaseIndent();
                    writer.WriteLines($"value = {codeElement.AccessedProperty.DefaultValue};",
                        $"m.Set{codeElement.AccessedProperty?.Name?.ToFirstCharacterUpperCase()}(value);");
                    writer.CloseBlock();
                    writer.WriteLine("return value;");
                } else
                    writer.WriteLine($"return m.Get{backingStore.Name.ToFirstCharacterUpperCase()}().Get(\"{codeElement.AccessedProperty?.Name?.ToFirstCharacterLowerCase()}\");");
            writer.CloseBlock();

        }
        private void WriteApiConstructorBody(CodeClass parentClass, CodeMethod method, LanguageWriter writer) {
            var requestAdapterProperty = parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter);
            var requestAdapterPropertyName = requestAdapterProperty.Name.ToFirstCharacterLowerCase();
            var backingStoreParameter = method.Parameters.FirstOrDefault(x => x.IsOfKind(CodeParameterKind.BackingStore));
            WriteSerializationRegistration(method.SerializerModules, writer, parentClass, "RegisterDefaultSerializer", "SerializationWriterFactory");
            WriteSerializationRegistration(method.DeserializerModules, writer, parentClass, "RegisterDefaultDeserializer", "ParseNodeFactory");
            writer.WriteLine($"m.{requestAdapterPropertyName}.SetBaseUrl(\"{method.BaseUrl}\")");
            if(backingStoreParameter != null)
                writer.WriteLine($"m.{requestAdapterPropertyName}.EnableBackingStore({backingStoreParameter.Name});");
        }
        private void WriteSerializationRegistration(List<string> serializationModules, LanguageWriter writer, CodeClass parentClass, string methodName, string interfaceName) {
            var interfaceImportSymbol = conventions.GetTypeString(new CodeType { Name = interfaceName, IsExternal = true }, parentClass, false, false);
            var methodImportSymbol = conventions.GetTypeString(new CodeType { Name = methodName, IsExternal = true }, parentClass, false, false);
            if(serializationModules != null)
                foreach(var module in serializationModules) {
                    var moduleImportSymbol = conventions.GetTypeString(new CodeType { Name = module, IsExternal = true }, parentClass, false, false);
                    moduleImportSymbol = moduleImportSymbol.Split('.').First();
                    writer.WriteLine($"{methodImportSymbol}(func() {interfaceImportSymbol} {{ return {moduleImportSymbol}.New{module}() }})");
                }
        }
        private void WriteConstructorBody(CodeClass parentClass, CodeMethod currentMethod, LanguageWriter writer, bool inherits) {
            writer.WriteLine($"m := &{parentClass.Name.ToFirstCharacterUpperCase()}{{");
            if(inherits || parentClass.IsErrorDefinition) {
                writer.IncreaseIndent();
                var parentClassName = parentClass.StartBlock.Inherits.Name.ToFirstCharacterUpperCase();
                writer.WriteLine($"{parentClassName}: *{conventions.GetImportedStaticMethodName(parentClass.StartBlock.Inherits, parentClass)}(),");
                writer.DecreaseIndent();
            }
            writer.CloseBlock(decreaseIndent: false);
            foreach(var propWithDefault in parentClass.GetPropertiesOfKind(CodePropertyKind.BackingStore,
                                                                            CodePropertyKind.RequestBuilder,
                                                                            CodePropertyKind.UrlTemplate,
                                                                            CodePropertyKind.PathParameters)
                                            .Where(x => !string.IsNullOrEmpty(x.DefaultValue))
                                            .OrderBy(x => x.Name)) {
                writer.WriteLine($"m.{propWithDefault.NamePrefix}{propWithDefault.Name.ToFirstCharacterLowerCase()} = {propWithDefault.DefaultValue};");
            }
            foreach(var propWithDefault in parentClass.GetPropertiesOfKind(CodePropertyKind.AdditionalData) //additional data and backing Store rely on accessors
                                            .Where(x => !string.IsNullOrEmpty(x.DefaultValue))
                                            .OrderBy(x => x.Name)) {
                writer.WriteLine($"m.Set{propWithDefault.Name.ToFirstCharacterUpperCase()}({propWithDefault.DefaultValue});");
            }
            if(parentClass.IsOfKind(CodeClassKind.RequestBuilder)) {
                if(currentMethod.IsOfKind(CodeMethodKind.Constructor)) {
                    var pathParametersParam = currentMethod.Parameters.FirstOrDefault(x => x.IsOfKind(CodeParameterKind.PathParameters));
                    conventions.AddParametersAssignment(writer, 
                                                        pathParametersParam.Type.AllTypes.OfType<CodeType>().FirstOrDefault(),
                                                        pathParametersParam.Name.ToFirstCharacterLowerCase(),
                                                        currentMethod.Parameters
                                                                    .Where(x => x.IsOfKind(CodeParameterKind.Path))
                                                                    .Select(x => (x.Type, x.UrlTemplateParameterName, x.Name.ToFirstCharacterLowerCase()))
                                                                    .ToArray());
                    AssignPropertyFromParameter(parentClass, currentMethod, CodeParameterKind.PathParameters, CodePropertyKind.PathParameters, writer, conventions.TempDictionaryVarName);
                }
                AssignPropertyFromParameter(parentClass, currentMethod, CodeParameterKind.RequestAdapter, CodePropertyKind.RequestAdapter, writer);
            }
        }
        private static void AssignPropertyFromParameter(CodeClass parentClass, CodeMethod currentMethod, CodeParameterKind parameterKind, CodePropertyKind propertyKind, LanguageWriter writer, string variableName = default) {
            var property = parentClass.GetPropertyOfKind(propertyKind);
            if(property != null) {
                var parameter = currentMethod.Parameters.FirstOrDefault(x => x.IsOfKind(parameterKind));
                if(!string.IsNullOrEmpty(variableName))
                    writer.WriteLine($"m.{property.Name.ToFirstCharacterLowerCase()} = {variableName};");
                else if(parameter != null)
                    writer.WriteLine($"m.{property.Name.ToFirstCharacterLowerCase()} = {parameter.Name};");
            }
        }
        private static void WriteSetterBody(CodeMethod codeElement, LanguageWriter writer, CodeClass parentClass) {
            var backingStore = parentClass.GetBackingStoreProperty();
            writer.WriteLine("if m != nil {");
            writer.IncreaseIndent();
            if(backingStore == null)
                writer.WriteLine($"m.{codeElement.AccessedProperty?.Name?.ToFirstCharacterLowerCase()} = value");
            else
                writer.WriteLine($"m.Get{backingStore.Name.ToFirstCharacterUpperCase()}().Set(\"{codeElement.AccessedProperty?.Name?.ToFirstCharacterLowerCase()}\", value)");
            writer.CloseBlock();
        }
        private void WriteIndexerBody(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer, string returnType) {
            var pathParametersProperty = parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters);
            var idParameter = codeElement.Parameters.First();
            conventions.AddParametersAssignment(writer, pathParametersProperty.Type, $"m.{pathParametersProperty.Name.ToFirstCharacterLowerCase()}",
                (idParameter.Type, codeElement.OriginalIndexer.ParameterName, "id"));
            conventions.AddRequestBuilderBody(parentClass, returnType, writer, urlTemplateVarName: conventions.TempDictionaryVarName);
        }
        private void WriteDeserializerBody(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer, bool inherits) {
            var fieldToSerialize = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom);
            if(inherits)
                writer.WriteLine($"res := m.{parentClass.StartBlock.Inherits.Name.ToFirstCharacterUpperCase()}.{codeElement.Name.ToFirstCharacterUpperCase()}()");
            else
                writer.WriteLine($"res := make({codeElement.ReturnType.Name})");
            if(fieldToSerialize.Any()) {
                var parsableImportSymbol = GetConversionHelperMethodImport(parentClass, "ParseNode");
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
            var propertyTypeImportName = conventions.GetTypeString(property.Type, parentClass, false, false);
            var deserializationMethodName = GetDeserializationMethodName(property.Type, parentClass);
            writer.WriteLine($"val, err := {deserializationMethodName}");
            WriteReturnError(writer);
            writer.WriteLine("if val != nil {");
            writer.IncreaseIndent();
            var (valueArgument, pointerSymbol, dereference) = (property.Type.AllTypes.First().TypeDefinition, property.Type.IsCollection) switch {
                (CodeClass, false) or (CodeEnum, false) => ($"val.(*{propertyTypeImportName})", string.Empty, true),
                (CodeClass, true) or (CodeEnum, true) => ("res", "*", true),
                (CodeInterface, false) => ($"val.({propertyTypeImportName})", string.Empty, false),
                (CodeInterface, true) => ("res", string.Empty, false),
                (_, true) => ("res", "*", true),
                _ => ("val", string.Empty, true),
            };
            if(property.Type.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None)
                WriteCollectionCast(propertyTypeImportName, "val", "res", writer, pointerSymbol, dereference);
            var setterName = property.IsNameEscaped && !string.IsNullOrEmpty(property.SerializationName) ? property.SerializationName : property.Name;
            writer.WriteLine($"m.Set{setterName.ToFirstCharacterUpperCase()}({valueArgument})");
            writer.CloseBlock();
            writer.WriteLine("return nil");
            writer.CloseBlock();
        }
        private static void WriteCollectionCast(string propertyTypeImportName, string sourceVarName, string targetVarName, LanguageWriter writer, string pointerSymbol = "*", bool dereference = true) {
            writer.WriteLines($"{targetVarName} := make([]{propertyTypeImportName}, len({sourceVarName}))",
                                $"for i, v := range {sourceVarName} {{");
            writer.IncreaseIndent();
            var derefPrefix = dereference ? "*(" : string.Empty;
            var derefSuffix = dereference ? ")" : string.Empty;
            writer.WriteLine($"{targetVarName}[i] = {derefPrefix}v.({pointerSymbol}{propertyTypeImportName}){derefSuffix}");
            writer.CloseBlock();
        }
        private void WriteRequestExecutorBody(CodeMethod codeElement, RequestProperties requestParams, string returnType, CodeClass parentClass, LanguageWriter writer) {
            if(codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");
            if(returnType == null) throw new InvalidOperationException("return type cannot be null"); // string.Empty is a valid return type
            var isPrimitive = conventions.IsPrimitiveType(returnType);
            var isBinary = conventions.StreamTypeName.Equals(returnType.TrimStart('*'), StringComparison.OrdinalIgnoreCase);
            var sendMethodName = returnType switch {
                "void" => "SendNoContentAsync",
                _ when string.IsNullOrEmpty(returnType) => "SendNoContentAsync",
                _ when codeElement.ReturnType.IsCollection && isPrimitive => "SendPrimitiveCollectionAsync",
                _ when isPrimitive || isBinary => "SendPrimitiveAsync",
                _ when codeElement.ReturnType.IsCollection => "SendCollectionAsync",
                _ => "SendAsync"
            };
            var responseHandlerParam = codeElement.Parameters.FirstOrDefault(x => x.IsOfKind(CodeParameterKind.ResponseHandler));
            var typeShortName = returnType.Split('.').Last().ToFirstCharacterUpperCase();
            var isVoid = string.IsNullOrEmpty(typeShortName);
            WriteGeneratorMethodCall(codeElement, requestParams, writer, $"{RequestInfoVarName}, err := ");
            WriteReturnError(writer, returnType);
            var constructorFunction = returnType switch {
                _ when isVoid => string.Empty,
                _ when isPrimitive || isBinary => $"\"{returnType.TrimCollectionAndPointerSymbols()}\", ",
                _ => $"{conventions.GetImportedStaticMethodName(codeElement.ReturnType, codeElement.Parent, "Create", "FromDiscriminatorValue", "able")}, ",
            };
            var errorMappingVarName = "nil";
            if(codeElement.ErrorMappings.Any()) {
                errorMappingVarName = "errorMapping";
                writer.WriteLine($"{errorMappingVarName} := {conventions.AbstractionsHash}.ErrorMappings {{");
                writer.IncreaseIndent();
                foreach(var errorMapping in codeElement.ErrorMappings) {
                    writer.WriteLine($"\"{errorMapping.Key.ToUpperInvariant()}\": {conventions.GetImportedStaticMethodName(errorMapping.Value, codeElement.Parent, "Create", "FromDiscriminatorValue", "able")},");
                }
                writer.CloseBlock();
            }
            var assignmentPrefix = isVoid ?
                        "err =" :
                        "res, err :=";
            writer.WriteLine($"{assignmentPrefix} m.requestAdapter.{sendMethodName}({RequestInfoVarName}, {constructorFunction}{responseHandlerParam?.Name ?? "nil"}, {errorMappingVarName})");
            WriteReturnError(writer, returnType);
            var valueVarName = string.Empty;
            if(codeElement.ReturnType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None) {
                var propertyTypeImportName = conventions.GetTypeString(codeElement.ReturnType, parentClass, false, false);
                var isInterface = codeElement.ReturnType.AllTypes.First().TypeDefinition is CodeInterface;
                WriteCollectionCast(propertyTypeImportName, "res", "val", writer, isInterface ? string.Empty : "*", !isInterface);
                valueVarName = "val, ";
            }
            var resultReturnCast = isVoid switch {
                true => string.Empty,
                _ when !string.IsNullOrEmpty(valueVarName) => valueVarName,
                _ => $"res.({returnType}), "
            };
            writer.WriteLine($"return {resultReturnCast}nil");
        }
        private static void WriteGeneratorMethodCall(CodeMethod codeElement, RequestProperties requestParams, LanguageWriter writer, string prefix) {
            var generatorMethodName = (codeElement.Parent as CodeClass)
                                                .Methods
                                                .FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestGenerator) && x.HttpMethod == codeElement.HttpMethod)
                                                ?.Name
                                                ?.ToFirstCharacterUpperCase();
            var paramsList = new List<CodeParameter> { requestParams.paramSet };
            var requestInfoParameters = paramsList.Where(x => x != null)
                                                .Select(x => x.Name)
                                                .ToList();
            var skipIndex = requestParams.requestBody == null ? 1 : 0;
            if(codeElement.IsOverload && !codeElement.OriginalMethod.Parameters.Any(x => x.IsOfKind(CodeParameterKind.QueryParameter)) || // we're on an overload and the original method has no query parameters
                !codeElement.IsOverload && requestParams.queryString == null) // we're on the original method and there is no query string parameter
                skipIndex++;// we skip the query string parameter null value
            requestInfoParameters.AddRange(paramsList.Where(x => x == null).Skip(skipIndex).Select(x => "nil"));
            var paramsCall = requestInfoParameters.Any() ? requestInfoParameters.Aggregate((x,y) => $"{x}, {y}") : string.Empty;
            writer.WriteLine($"{prefix}m.{generatorMethodName}({paramsCall});");
        }
        private const string RequestInfoVarName = "requestInfo";
        private void WriteRequestGeneratorBody(CodeMethod codeElement, RequestProperties requestParams, LanguageWriter writer, CodeClass parentClass, string returnType) {
            if(codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");
            
            var urlTemplateParamsProperty = parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters);
            var urlTemplateProperty = parentClass.GetPropertyOfKind(CodePropertyKind.UrlTemplate);
            var requestAdapterProperty = parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter);
            writer.WriteLine($"{RequestInfoVarName} := {conventions.AbstractionsHash}.NewRequestInformation()");
            writer.WriteLines($"{RequestInfoVarName}.UrlTemplate = {GetPropertyCall(urlTemplateProperty, "\"\"")}",
                        $"{RequestInfoVarName}.PathParameters = {GetPropertyCall(urlTemplateParamsProperty, "\"\"")}",
                        $"{RequestInfoVarName}.Method = {conventions.AbstractionsHash}.{codeElement.HttpMethod?.ToString().ToUpperInvariant()}");
            if(requestParams.requestBody != null) {
                var bodyParamReference = $"{requestParams.paramSet.Name}.{requestParams.requestBody.Name.ToFirstCharacterUpperCase()}";
                if(requestParams.requestBody.Type.Name.Equals("binary", StringComparison.OrdinalIgnoreCase))
                    writer.WriteLine($"{RequestInfoVarName}.SetStreamContent({bodyParamReference})");
                else
                    writer.WriteLine($"{RequestInfoVarName}.SetContentFromParsable(m.{requestAdapterProperty.Name.ToFirstCharacterLowerCase()}, \"{codeElement.ContentType}\", {bodyParamReference})");
            }
            if(requestParams.queryString != null) {
                var queryStringName = requestParams.queryString.Name.ToFirstCharacterUpperCase();
                writer.WriteLine($"if {requestParams.paramSet.Name} != nil && {requestParams.paramSet.Name}.{queryStringName} != nil {{");
                writer.IncreaseIndent();
                writer.WriteLine($"requestInfo.AddQueryParameters(*({requestParams.paramSet.Name}.{queryStringName}))");
                writer.CloseBlock();
            }
            if(requestParams.headers != null) {
                var headersName = requestParams.headers.Name.ToFirstCharacterUpperCase();
                writer.WriteLine($"if {requestParams.paramSet.Name} != nil && {requestParams.paramSet.Name}.{headersName} != nil {{");
                writer.IncreaseIndent();
                writer.WriteLine($"{RequestInfoVarName}.Headers = {requestParams.paramSet.Name}.{headersName}");
                writer.CloseBlock();
            }
            if(requestParams.options != null) {
                var optionsName = requestParams.options.Name.ToFirstCharacterUpperCase();
                writer.WriteLine($"if {requestParams.paramSet.Name} != nil && len({requestParams.paramSet.Name}.{optionsName}) != 0 {{");
                writer.IncreaseIndent();
                writer.WriteLine($"err := {RequestInfoVarName}.AddRequestOptions({requestParams.paramSet.Name}.{optionsName}...)");
                WriteReturnError(writer, returnType);
                writer.CloseBlock();
            }
            writer.WriteLine($"return {RequestInfoVarName}, nil");
        }
        private static string GetPropertyCall(CodeProperty property, string defaultValue) => property == null ? defaultValue : $"m.{property.Name.ToFirstCharacterLowerCase()}";

        private static void WriteReturnError(LanguageWriter writer, params string[] returnTypes) {
            writer.WriteLine("if err != nil {");
            writer.IncreaseIndent();
            var nilsPrefix = GetNilsErrorPrefix(returnTypes);
            writer.WriteLine($"return {nilsPrefix}err");
            writer.CloseBlock();
        }
        private static string GetNilsErrorPrefix(params string[] returnTypes) {
            var sanitizedTypes = returnTypes.Where(x => !string.IsNullOrEmpty(x));
            return !sanitizedTypes.Any() ?
                            string.Empty :
                            sanitizedTypes.Select(_ => "nil").Aggregate((x,y) => $"{x}, {y}") + ", ";
        }
        private string GetDeserializationMethodName(CodeTypeBase propType, CodeClass parentClass) {
            var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
            var propertyTypeName = conventions.GetTypeString(propType, parentClass, false, false);
            var propertyTypeNameWithoutImportSymbol = conventions.TranslateType(propType, false);
            if(propType is CodeType currentType) {
                if(isCollection)
                    if(currentType.TypeDefinition == null)
                        return $"n.GetCollectionOfPrimitiveValues(\"{propertyTypeName.ToFirstCharacterLowerCase()}\")";
                    else if (currentType.TypeDefinition is CodeEnum)
                        return $"n.GetCollectionOfEnumValues({conventions.GetImportedStaticMethodName(propType, parentClass, "Parse")})";
                    else
                        return $"n.GetCollectionOfObjectValues({GetTypeFactory(propType, parentClass, propertyTypeNameWithoutImportSymbol)})";
                else if (currentType.TypeDefinition is CodeEnum currentEnum) {
                    return $"n.GetEnum{(currentEnum.Flags ? "Set" : string.Empty)}Value({conventions.GetImportedStaticMethodName(propType, parentClass, "Parse")})";
                }
            }
            return propertyTypeNameWithoutImportSymbol switch {
                _ when conventions.IsPrimitiveType(propertyTypeNameWithoutImportSymbol) => 
                    $"n.Get{propertyTypeNameWithoutImportSymbol.ToFirstCharacterUpperCase()}Value()",
                _ when conventions.StreamTypeName.Equals(propertyTypeNameWithoutImportSymbol, StringComparison.OrdinalIgnoreCase) =>
                    "n.GetByteArrayValue()",
                _ => $"n.GetObjectValue({GetTypeFactory(propType, parentClass, propertyTypeNameWithoutImportSymbol)})",
            };
        }
        private string GetTypeFactory(CodeTypeBase propTypeBase, CodeClass parentClass, string propertyTypeName) {
            if(propTypeBase is CodeType propType)
                return $"{conventions.GetImportedStaticMethodName(propType, parentClass, "Create", "FromDiscriminatorValue", "able")}";
            else return GetTypeFactory(propTypeBase.AllTypes.First(), parentClass, propertyTypeName);
        }
        private void WriteSerializationMethodCall(CodeTypeBase propType, CodeElement parentBlock, string serializationKey, string valueGet, bool shouldDeclareErrorVar, LanguageWriter writer) {
            serializationKey = $"\"{serializationKey}\"";
            var errorPrefix = $"err {errorVarDeclaration(shouldDeclareErrorVar)}= writer.";
            var isEnum = propType is CodeType eType && eType.TypeDefinition is CodeEnum;
            var isComplexType = propType is CodeType cType && (cType.TypeDefinition is CodeClass || cType.TypeDefinition is CodeInterface);
            var isInterface = propType is CodeType iType && iType.TypeDefinition is CodeInterface;
            if(isEnum || propType.IsCollection)
                writer.WriteLine($"if {valueGet} != nil {{");
            else
                writer.WriteLine("{");// so the err var scope is limited
            writer.IncreaseIndent();
            if(isEnum && !propType.IsCollection)
                writer.WriteLine($"cast := (*{valueGet}).String()");
            else if(isComplexType && propType.IsCollection) {
                var parsableSymbol = GetConversionHelperMethodImport(parentBlock, "Parsable");
                writer.WriteLines($"cast := make([]{parsableSymbol}, len({valueGet}))",
                                $"for i, v := range {valueGet} {{");
                writer.IncreaseIndent();
                if(isInterface)
                    writer.WriteLine($"cast[i] = v.({parsableSymbol})");
                else
                    writer.WriteLines($"temp := v", // temporary creating a new reference to avoid pointers to the same object
                        $"cast[i] = {parsableSymbol}(&temp)");
                writer.CloseBlock();
            }
            var collectionPrefix = propType.IsCollection ? "CollectionOf" : string.Empty;
            var collectionSuffix = propType.IsCollection ? "s" : string.Empty;
            var propertyTypeName = conventions.GetTypeString(propType, parentBlock, false, false)
                                    .Split('.')
                                    .Last()
                                    .ToFirstCharacterUpperCase();
            var reference = (isEnum, isComplexType, propType.IsCollection) switch {
                (true, false, false) => $"&cast",
                (true, false, true) => $"{conventions.GetTypeString(propType, parentBlock, false, false).Replace(propertyTypeName, "Serialize" + propertyTypeName)}({valueGet})", //importSymbol.SerializeEnumName
                (false, true, true) => $"cast",
                (_, _, _) => valueGet,
            };
            if(isComplexType)
                propertyTypeName = "Object";
            else if(isEnum)
                propertyTypeName = "String";
            else if (conventions.StreamTypeName.Equals(propertyTypeName, StringComparison.OrdinalIgnoreCase))
                propertyTypeName = "ByteArray";
            writer.WriteLine($"{errorPrefix}Write{collectionPrefix}{propertyTypeName}Value{collectionSuffix}({serializationKey}, {reference})");
            WriteReturnError(writer);
            writer.CloseBlock();
        }
        private string GetConversionHelperMethodImport(CodeElement parentBlock, string name) {
            var conversionMethodType = new CodeType { Name = name, IsExternal = true };
            return conventions.GetTypeString(conversionMethodType, parentBlock, true, false);
        }
    }
}
