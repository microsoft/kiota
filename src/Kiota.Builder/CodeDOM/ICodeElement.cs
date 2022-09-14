namespace Kiota.Builder.CodeDOM {
    public interface ICodeElement {
        string Name { get; set; }
        CodeElement Parent { get; set; }
    }
}
