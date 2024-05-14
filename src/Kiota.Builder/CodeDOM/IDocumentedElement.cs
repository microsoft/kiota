using System.Text.Json.Serialization;

namespace Kiota.Builder.CodeDOM;
public interface IDocumentedElement
{
    [JsonIgnore]
    CodeDocumentation Documentation
    {
        get; set;
    }
}
