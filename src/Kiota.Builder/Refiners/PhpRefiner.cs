using System;

namespace Kiota.Builder.Refiners
{
    public class PhpRefiner: CommonLanguageRefiner
    {
        public PhpRefiner(GenerationConfiguration configuration) : base(configuration) { }


        private static readonly Tuple<string, string>[] defaultNamespaces = new Tuple<string, string>[]
        {
            new("ParseNodeInterface", "Microsoft\\Kiota\\Abstractions\\Serialization\\ParseNodeInterface")
        };
        public override void Refine(CodeNamespace generatedCode)
        {
            //AddInnerClasses(generatedCode);
            AddDefaultImports(generatedCode, defaultNamespaces, 
                Array.Empty<Tuple<string, string>>(), 
                Array.Empty<Tuple<string, string>>(),
                Array.Empty<Tuple<string, string>>());
        }
    }
}
