namespace Kiota.Builder;
public static class Constants
{
    public const string DefaultOpenApiLabel = "default";
    public const string TempDirectoryName = "kiota";
}

public enum Authentication
{
    Basic,
    Bearer,
    OAuthV2,
    APIKey,
}
