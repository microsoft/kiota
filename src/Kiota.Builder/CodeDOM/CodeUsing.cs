namespace Kiota.Builder
{
    public class CodeUsing : CodeElement
    {
        private CodeType declaration;
        public CodeType Declaration { get => declaration; set {
            AddMissingParent(declaration);
            declaration = value;
        } }
    }
}
