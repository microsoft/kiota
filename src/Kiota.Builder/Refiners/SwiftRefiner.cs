namespace Kiota.Builder.Refiners {
    public class SwiftRefiner : CommonLanguageRefiner
    {
        public SwiftRefiner(GenerationConfiguration configuration) : base(configuration) {}
        public override void Refine(CodeNamespace generatedCode)
        {
            ReplaceReservedNames(
                generatedCode,
                new SwiftReservedNamesProvider(),
                x => $"{x}_escaped");
        }
    }
}
