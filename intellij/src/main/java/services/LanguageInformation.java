package services;


import com.fasterxml.jackson.annotation.JsonIgnoreProperties;

import java.util.List;

@JsonIgnoreProperties(ignoreUnknown = true)
public class LanguageInformation {
    public MaturityLevel MaturityLevel;
    public  List<LanguageDependency> Dependencies;
    public String DependencyInstallCommand;
    public String ClientClassName;
    public String ClientNamespaceName;
    public List<String> StructuredMimeTypes;

}