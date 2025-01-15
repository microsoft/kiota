using System.Text.Json.Serialization;
using Kiota.Builder.Configuration;

namespace Kiota.Builder.Settings;
public class SettingsFile
{
    [JsonPropertyName("rest-client.environmentVariables")]
    public EnvironmentVariables EnvironmentVariables
    {
        get; set;
    }

    public SettingsFile()
    {
        EnvironmentVariables = new EnvironmentVariables();
    }
}

public class EnvironmentVariables
{
    [JsonPropertyName("$shared")]
    public SharedAuth Shared
    {
        get; set;
    }

    [JsonPropertyName("remote")]
    public AuthenticationSettings Remote
    {
        get; set;
    }

    [JsonPropertyName("development")]
    public AuthenticationSettings Development
    {
        get; set;
    }
    
    public EnvironmentVariables()
    {
        Shared = new SharedAuth();
        Remote = new AuthenticationSettings();
        Development = new AuthenticationSettings();
    }
}

public class SharedAuth
{

}

public class AuthenticationSettings
{
    public string HostAddress
    {
        get; set;
    }
    public string? BasicAuth
    {
        get; set;
    }
    public string? Bearer
    {
        get; set;
    }

    public AuthenticationSettings()
    {
        HostAddress = "";
    }
}
