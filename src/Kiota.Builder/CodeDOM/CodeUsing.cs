namespace Kiota.Builder
{
    public class CodeUsing : CodeElement
    {
        private CodeType declaration;
        public CodeType Declaration { get => declaration; set {
            EnsureElementsAreChildren(declaration);
            declaration = value;
        } }
        public bool IsExternal {
            get => Declaration?.IsExternal ?? true;
        }
        public string Alias { get; set; }
    }
}
