using System.Collections.Generic;

namespace Kiota.Builder {
    public class GenerationConfiguration {
        public string OpenAPIFilePath { get; set; } = "openapi.yaml";
        public string OutputPath { get; set; } = "./output";
        public string ClientClassName { get; set; } = "ApiClient";
        public string ClientNamespaceName { get; set; } = "ApiClient";
        public GenerationLanguage Language { get; set; } = GenerationLanguage.CSharp;
        public string ApiRootUrl { get; set; } = "https://graph.microsoft.com/v1.0";
        public List<string> PropertiesPrefixToStrip { get; set; } = new() { "@odata."};
        public HashSet<string> IgnoredRequestContentTypes { get; set; } = new();
        public string BackingStore { get; set; } = string.Empty;
        public List<string> Serializers { get; set; } = new();
        public bool UsesBackingStore {
            get => !string.IsNullOrEmpty(BackingStore);
        }
    }
}
