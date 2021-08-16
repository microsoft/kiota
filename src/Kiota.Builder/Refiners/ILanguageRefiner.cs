namespace Kiota.Builder.Refiners
{
    public interface ILanguageRefiner
    {
        void Refine(CodeNamespace generatedCode);
        public static void Refine(GenerationConfiguration config, CodeNamespace generatedCode) {
            switch (config.Language)
            {
                case GenerationLanguage.CSharp:
                    new CSharpRefiner(config).Refine(generatedCode);
                    break;
                case GenerationLanguage.TypeScript:
                    new TypeScriptRefiner(config).Refine(generatedCode);
                    break;
                case GenerationLanguage.Java:
                    new JavaRefiner(config).Refine(generatedCode);
                    break;
                case GenerationLanguage.Ruby:
                    new RubyRefiner(config).Refine(generatedCode);
                    break;
                case GenerationLanguage.PHP:
                    new PhpRefiner(config).Refine(generatedCode);
                    break;
                default:
                    break; //Do nothing
            }
        }
    }
}
