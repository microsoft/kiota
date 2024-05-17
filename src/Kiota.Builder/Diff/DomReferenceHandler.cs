using System.Text.Json.Serialization;

namespace Kiota.Builder.Diff;

internal class DomReferenceHandler : ReferenceHandler
{
    public override ReferenceResolver CreateResolver()
    {
        return new DomReferenceResolver();
    }
}
