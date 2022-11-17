namespace Kiota.Builder.Configuration;

public abstract class SearchConfigurationBase {
    public string SearchTerm { get; set; }
    public bool ClearCache { get; set; }
    public string Version { get; set; }
}
