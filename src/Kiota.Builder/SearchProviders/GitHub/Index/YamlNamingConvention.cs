using Kiota.Builder.Extensions;
using YamlDotNet.Serialization;
namespace Kiota.Builder.SearchProviders.GitHub.Index;

internal class YamlNamingConvention : INamingConvention
{
    public string Apply(string value)
    {
        return value.ToFirstCharacterLowerCase();
    }
}
