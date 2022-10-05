using System.Collections.Generic;

namespace Kiota.Builder;

public record LanguageInformation {
    public LanguageMaturityLevel MaturityLevel {get; set;}
    public List<LanguageDependency> Dependencies {get; set;} 
    public string DependencyInstallCommand {get; set;}
};

public record LanguageDependency {
    public string Name {get; set;}
    public string Version {get; set;}
}

public enum LanguageMaturityLevel {
    Experimental,
    Preview,
    Stable
}
