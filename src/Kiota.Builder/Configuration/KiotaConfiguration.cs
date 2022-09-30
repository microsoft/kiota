namespace Kiota.Builder.Configuration;

public class KiotaConfiguration {
    public GenerationConfiguration Generation { get; set; } = new();
    public SearchConfiguration Search { get; set; } = new();
    public DownloadConfiguration Download { get; set; } = new();
}
