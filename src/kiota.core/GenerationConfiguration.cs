namespace kiota.core {
    public class GenerationConfiguration {
        public string OpenAPIFilePath { get; set; } = "definition.yml";
        public string OutputPath { get; set; } = "./ouput";
        public string ClientClassName { get; set; } = "GraphClient";
    }
}
