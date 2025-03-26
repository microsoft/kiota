using System.Text.Json;

namespace kiota.Rpc
{
    public record AdaptiveCardInfo(string dataPath, JsonElement card);
}
