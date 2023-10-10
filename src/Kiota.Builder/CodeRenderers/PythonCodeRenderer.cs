using Kiota.Builder.Configuration;
using Kiota.Builder.OrderComparers;

namespace Kiota.Builder.CodeRenderers;
public class PythonCodeRenderer : CodeRenderer
{
    public PythonCodeRenderer(GenerationConfiguration configuration) : base(configuration, new CodeElementOrderComparerPython()) { }
}
