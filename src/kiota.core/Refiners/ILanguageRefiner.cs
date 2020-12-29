namespace kiota.core
{
    public interface ILanguageRefiner
    {
        void Refine(CodeNamespace generatedCode);
        public static void Refine(GenerationLanguage language, CodeNamespace generatedCode) {
            var csRefiner = new CSharpRefiner();
            switch (language)
            {
                case GenerationLanguage.CSharp:
                    csRefiner.Refine(generatedCode);
                    break;
                case GenerationLanguage.TypeScript:
                    new TypeScriptRefiner().Refine(generatedCode);
                    break;
                case GenerationLanguage.Java:
                    csRefiner.Refine(generatedCode);
                    break;
                default:
                    break; //Do nothing
            }
        }
    }
}
