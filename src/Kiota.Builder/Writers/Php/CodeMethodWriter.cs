using System.Linq;
using System.Text;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers.Extensions;

namespace Kiota.Builder.Writers.Php
{
    public class CodeMethodWriter: BaseElementWriter<CodeMethod, PhpConventionService>
    {
        public CodeMethodWriter(PhpConventionService conventionService) : base(conventionService) { }

        public override void  WriteCodeElement(CodeMethod codeElement, LanguageWriter writer)
        {

            var parentClass = codeElement.Parent as CodeClass;
            var inherits = (parentClass?.StartBlock as CodeClass.Declaration)?.Inherits != null;
            WriteMethodPhpDocs(codeElement, writer);
            WriteMethodsAndParameters(codeElement, writer, codeElement.IsOfKind(CodeMethodKind.Constructor, CodeMethodKind.ClientConstructor));
            switch (codeElement.MethodKind)
            {
                    case CodeMethodKind.Constructor: 
                        WriteConstructorBody(parentClass, codeElement, writer, inherits);
                        break;
                    case CodeMethodKind.Deserializer:
                        break;
            }
            conventions.WriteCodeBlockEnd(writer);
        }
        
        private void WriteConstructorBody(CodeClass parentClass, CodeMethod currentMethod, LanguageWriter writer, bool inherits) {
            if(inherits)
                writer.WriteLine("parent::__construct();");
            foreach(var propWithDefault in parentClass.GetPropertiesOfKind(CodePropertyKind.AdditionalData,
                                                                            CodePropertyKind.BackingStore,
                                                                            CodePropertyKind.RequestBuilder,
                                                                            CodePropertyKind.PathSegment)
                                            .Where(x => !string.IsNullOrEmpty(x.DefaultValue))
                                            .OrderByDescending(x => x.PropertyKind)
                                            .ThenBy(x => x.Name)) {
                writer.WriteLine($"$this->{propWithDefault.NamePrefix}{propWithDefault.Name.ToFirstCharacterLowerCase()} = {conventions.ReplaceDoubleQuoteWithSingleQuote(propWithDefault.DefaultValue)};");
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
                writer.WriteLine($"$this->{property.Name.ToFirstCharacterLowerCase()} = ${parameter.Name};");
            }
        }

        private void WriteMethodPhpDocs(CodeMethod codeMethod, LanguageWriter writer)
        {
            var methodDescription = codeMethod.Description ?? string.Empty;

            var hasMethodDescription = !string.IsNullOrEmpty(methodDescription.Trim(' '));
            var parametersWithDescription = codeMethod.Parameters.Where(x => !string.IsNullOrEmpty(x.Description));
            if (!hasMethodDescription && !parametersWithDescription.Any())
            {
                return;
            }

            writer.WriteLine(conventions.DocCommentStart);
            if(hasMethodDescription){
                writer.WriteLine(
                    $"{conventions.DocCommentPrefix}{methodDescription}");
            }

            foreach (var parameterWithDescription in parametersWithDescription)
            {
                writer.WriteLine($"{conventions.DocCommentPrefix}@param {conventions.GetTypeString(parameterWithDescription.Type)} ${parameterWithDescription.Name} {parameterWithDescription.Description}");
            }
            writer.WriteLine($"{conventions.DocCommentPrefix}@return {conventions.GetTypeString(codeMethod.ReturnType)}");
            writer.WriteLine(conventions.DocCommentEnd);
        }
        
        /**
         * Writes the method signatures and puts the parameters.
         * for example this writes
         * function methodName(int $parameter, string $parameter2){
         */
        private void WriteMethodsAndParameters(CodeMethod codeMethod, LanguageWriter writer, bool isConstructor = false)
        {
            var methodParameters = string.Join(", ", codeMethod.Parameters.Select(x => conventions.GetParameterSignature(x)).ToList());

            var methodName = isConstructor ? "__construct" : codeMethod.Name.ToFirstCharacterLowerCase();
            writer.WriteLine($"{conventions.GetAccessModifier(codeMethod.Access)} function {methodName}({methodParameters}) {{");
            writer.IncreaseIndent();
            
        }
    }
}
