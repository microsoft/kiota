using System;
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
                // case CodeMethodKind.RequestGenerator:
                //     WriteRequestGeneratorBody(codeElement, requestBodyParam, queryStringParam, headersParam, writer);
                // break;
                // case CodeMethodKind.RequestExecutor:
                //     WriteRequestExecutorBody(codeElement, requestBodyParam, queryStringParam, headersParam, returnType, writer);
                // break;
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
                _ => code.Name.ToFirstCharacterLowerCase()
            });
            var parameters = string.Join(", ", code.Parameters.Select(p => conventions.GetParameterSignature(p, parentClass)).ToList());
            var classType = "*" + conventions.GetTypeString(new CodeType(parentClass) { Name = parentClass.Name, TypeDefinition = parentClass }, parentClass);
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
        private static void WriteSetterBody(CodeMethod codeElement, LanguageWriter writer, CodeClass parentClass) {
            var backingStore = parentClass.GetBackingStoreProperty();
            if(backingStore == null)
                writer.WriteLine($"m.{codeElement.AccessedProperty?.Name?.ToFirstCharacterLowerCase()} = value");
            else //TODO implement backing store setter when definition available in abstractions
                writer.WriteLine($"this.get{backingStore.Name.ToFirstCharacterUpperCase()}().set(\"{codeElement.AccessedProperty?.Name?.ToFirstCharacterLowerCase()}\", value);");
        }
        private void WriteIndexerBody(CodeMethod codeElement, LanguageWriter writer, string returnType) {
            var currentPathProperty = codeElement.Parent.GetChildElements(true).OfType<CodeProperty>().FirstOrDefault(x => x.IsOfKind(CodePropertyKind.CurrentPath));
            var pathSegment = codeElement.PathSegment;
            conventions.AddRequestBuilderBody(currentPathProperty != null, returnType, writer, $" + \"/{(string.IsNullOrEmpty(pathSegment) ? string.Empty : pathSegment + "/" )}\" + id");
        }
    }
}
