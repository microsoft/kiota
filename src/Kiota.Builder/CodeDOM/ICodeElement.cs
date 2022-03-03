namespace Kiota.Builder {
    public interface ICodeElement {
        string Name { get; set; }
        CodeElement Parent { get; set; }
    }
}
