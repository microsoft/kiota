using System;
using System.IO;
using System.Linq;

namespace kiota.core
{
    public class CSharpWriter : LanguageWriter
    {

        public override void WriteNamespaceEnd(CodeNamespace.End code)
        {
            DecreaseIndent();
            WriteLine("}");
        }

        public override void WriteNamespaceDeclaration(CodeNamespace.Declaration code)
        {
            WriteLine($"namespace {code.Name} {{");
            IncreaseIndent();
        }

        public override void WriteCodeClassDeclaration(CodeClass.Declaration code)
        {
            WriteLine($"public class {code.Name} {{");
            IncreaseIndent();
        }

        public override void WriteCodeClassEnd(CodeClass.End code)
        {
            DecreaseIndent();
            WriteLine("}");
        }

        public override void WriteProperty(CodeProperty code)
        {
            WriteLine($"public {code.Type.Name} {code.Name} {{get;}}");
        }

        public override void WriteIndexer(CodeIndexer code)
        {
            WriteLine($"public {code.ReturnType} this[string {code.Name}] {{get {{ return null; }} }}");
        }

        public override void WriteMethod(CodeMethod code)
        {
            WriteLine($"public Task<{code.ReturnType}> {code.Name}({string.Join(',', code.Parameters.Select(p=> GetParameterSignature(p)).ToList())}) {{ return base.GetAsync(); }}");
        }
        private string GetParameterSignature(CodeParameter parameter)
        {
            return $"{parameter.Type.Name}  : {parameter.Name}";
        }
        public override string GetFileSuffix()
        {
            return ".cs";
        }
    }
}
