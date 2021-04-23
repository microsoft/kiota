namespace Kiota.Builder
{
    public interface ILanguageRefiner
    {
        void Refine();
        public static void Refine(GenerationLanguage language, CodeNamespace generatedCode) {
            switch (language)
            {
                case GenerationLanguage.CSharp:
                    new CSharpRefiner(generatedCode).Refine();
                    break;
                case GenerationLanguage.TypeScript:
                    new TypeScriptRefiner(generatedCode).Refine();
                    break;
                case GenerationLanguage.Java:
                    new JavaRefiner(generatedCode).Refine();
                    break;
                default:
                    break; //Do nothing
            }
        }
    }
}
