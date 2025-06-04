using Microsoft.OpenApi;

namespace kiota.Rpc
{
    internal class ServersMapper
    {
        public static IEnumerable<string> FromServerList(IList<OpenApiServer>? servers)
        {
            var serverList = new List<string>();
            if (servers is null) return serverList;
            foreach (var server in servers)
            {
                if (server is null) continue;
                if (string.IsNullOrEmpty(server.Url)) continue;
                serverList.Add(server.Url);
            }
            return serverList;
        }
    }
}
