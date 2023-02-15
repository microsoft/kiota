using Kiota.Generated;
internal class Server
{
    public string GetVersion()
    {
        return KiotaVersion.Current();
    }
}
