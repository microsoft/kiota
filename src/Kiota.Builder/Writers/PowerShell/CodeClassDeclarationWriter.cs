using System;
using System.Linq;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers.CSharp;
using Kiota.Builder.Writers.Extensions;

namespace Kiota.Builder.Writers.PowerShell
{
    public class CodeClassDeclarationWriter : CSharp.CodeClassDeclarationWriter
    {
        public CodeClassDeclarationWriter(CSharpConventionService conventionService) : base(conventionService) { }

        public override void WriteCodeElement(ClassDeclaration codeElement, LanguageWriter writer)
        {
            if (codeElement == null) throw new ArgumentNullException(nameof(codeElement));
            if (writer == null) throw new ArgumentNullException(nameof(writer));

            WriteNamespaces(codeElement, writer);
            WriteSummary(codeElement, writer);
            WriteClassAnnotations(codeElement, writer);
            WriteClassProptotype(codeElement, writer);
        }

        private void WriteClassAnnotations(ClassDeclaration codeElement, LanguageWriter writer)
        {
            if (codeElement.FindImplementByName("PSCmdlet") != null)
            {
                var nameSegments = codeElement.Name.SplitPascalCase();
                if (!nameSegments.Any())
                    throw new ArgumentException($"{codeElement.Name} class name is malformed. Class names should be in the form of VerbNoun_ParameterSetName");
                var verbSegment = nameSegments.First();
                var nounSegment = nameSegments.Skip(1).Aggregate((a, b) => a + b);

                writer.WriteLine($"[Cmdlet(VerbsCommon.{verbSegment}, \"{nounSegment}\")]");
                if (codeElement.Parent is CodeClass parentClass &&
                        parentClass.GetMethodsOffKind(CodeMethodKind.RequestExecutor).FirstOrDefault().ReturnType is CodeType returnType &&
                        !returnType.Name.Equals("void", StringComparison.InvariantCultureIgnoreCase))
                {
                    string outputType;
                    if (returnType.Name.EndsWith("Response"))
                        outputType = (returnType.TypeDefinition as CodeClass).FindChildByName<CodeProperty>("value").Type.Name;
                    else
                        outputType = returnType.Name;
                    writer.WriteLine($"[OutputType(typeof({outputType.ToFirstCharacterUpperCase()}))]");
                }
            }
        }
    }
}
