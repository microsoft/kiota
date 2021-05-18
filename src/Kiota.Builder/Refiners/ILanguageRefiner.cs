namespace Kiota.Builder.Refiners
{
    public interface ILanguageRefiner
    {
        void Refine(CodeNamespace generatedCode);
        public static void Refine(GenerationLanguage language, CodeNamespace generatedCode) {
            switch (language)
            {
                case GenerationLanguage.CSharp:
                    new CSharpRefiner().Refine(generatedCode);
                    break;
                case GenerationLanguage.TypeScript:
                    new TypeScriptRefiner().Refine(generatedCode);
                    break;
                case GenerationLanguage.Java:
                    new JavaRefiner().Refine(generatedCode);
                    break;
                default:
                    break; //Do nothing
            }
        }
    }
}
