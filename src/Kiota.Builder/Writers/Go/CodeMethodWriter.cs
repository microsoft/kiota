using System;
using System.Linq;
using Kiota.Builder.Extensions;

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
                // case CodeMethodKind.IndexerBackwardCompatibility:
                //     WriteIndexerBody(codeElement, writer, returnType);
                // break;
                // case CodeMethodKind.RequestGenerator:
                //     WriteRequestGeneratorBody(codeElement, requestBodyParam, queryStringParam, headersParam, writer);
                // break;
                // case CodeMethodKind.RequestExecutor:
                //     WriteRequestExecutorBody(codeElement, requestBodyParam, queryStringParam, headersParam, returnType, writer);
                // break;
                // case CodeMethodKind.Getter:
                //     WriteGetterBody(codeElement, writer, parentClass);
                //     break;
                // case CodeMethodKind.Setter:
                //     WriteSetterBody(codeElement, writer, parentClass);
                //     break;
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
                                                CodeMethodKind.Setter) || code.IsAsync ? 
                                                    string.Empty :
                                                    "error";
            if(!string.IsNullOrEmpty(finalReturnType) && !string.IsNullOrEmpty(errorDeclaration))
                finalReturnType += ", ";
            writer.WriteLine($"func{associatedTypePrefix} {methodName}({parameters})({finalReturnType}{errorDeclaration}) {{");
        }
    }
}
