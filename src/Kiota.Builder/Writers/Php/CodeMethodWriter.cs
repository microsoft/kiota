using System;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Php
{
    public class CodeMethodWriter: BaseElementWriter<CodeMethod, PhpConventionService>
    {
        public CodeMethodWriter(PhpConventionService conventionService) : base(conventionService) { }

        public override void  WriteCodeElement(CodeMethod codeElement, LanguageWriter writer)
        {
            var accessModifier = conventions.GetAccessModifier(codeElement.Access);
            var returnType = conventions.GetTypeString(codeElement.ReturnType);

            var parameters = string.Join(',', codeElement.Parameters.Select(x => conventions.GetParameterSignature(x)));
            writer.Write($"{accessModifier} function {codeElement.Name.ToFirstCharacterLowerCase()}({parameters}): {returnType} ");
            writer.Write("{");
            writer.WriteLine("}");
        }
    }
}
