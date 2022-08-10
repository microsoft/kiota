using System;
using System.Collections.Generic;

namespace Kiota.Builder {
    public class GenerationConfiguration {
        public string OpenAPIFilePath { get; set; } = "openapi.yaml";
        public string OutputPath { get; set; } = "./output";
        public string ClientClassName { get; set; } = "ApiClient";
        public string ClientNamespaceName { get; set; } = "ApiSdk";
        public string NamespaceNameSeparator { get; set; } = ".";
        public string ModelsNamespaceName { get => $"{ClientNamespaceName}{NamespaceNameSeparator}models"; }
        public GenerationLanguage Language { get; set; } = GenerationLanguage.CSharp;
        public string ApiRootUrl { get; set; }
        public bool UsesBackingStore { get; set; }
        public bool IncludeAdditionalData { get; set; } = true;
        public HashSet<string> Serializers { get; set; } = new(StringComparer.OrdinalIgnoreCase){
            "Microsoft.Kiota.Serialization.Json.JsonSerializationWriterFactory",
            "Microsoft.Kiota.Serialization.Text.TextSerializationWriterFactory"
        };
        public HashSet<string> Deserializers { get; set; } = new(StringComparer.OrdinalIgnoreCase) {
            "Microsoft.Kiota.Serialization.Json.JsonParseNodeFactory",
            "Microsoft.Kiota.Serialization.Text.TextParseNodeFactory"
        };
        public bool ShouldWriteNamespaceIndices { get { return BarreledLanguages.Contains(Language); } }
        public bool ShouldWriteBarrelsIfClassExists { get { return BarreledLanguagesWithConstantFileName.Contains(Language); } }
        public bool ShouldRenderMethodsOutsideOfClasses { get { return MethodOutsideOfClassesLanguages.Contains(Language); } }
        private static readonly HashSet<GenerationLanguage> MethodOutsideOfClassesLanguages = new () {
            GenerationLanguage.Go,
        };
        private static readonly HashSet<GenerationLanguage> BarreledLanguages = new () {
            GenerationLanguage.Ruby,
            GenerationLanguage.TypeScript,
            GenerationLanguage.Swift,
        };
        private static readonly HashSet<GenerationLanguage> BarreledLanguagesWithConstantFileName = new () {
            GenerationLanguage.TypeScript
        };
        public bool CleanOutput { get; set;}
        public HashSet<string> StructuredMimeTypes { get; set; } = new(StringComparer.OrdinalIgnoreCase) {
            "application/json",
            "application/xml",
            "text/plain",
            "text/xml",
            "text/yaml",
        };
    }
}
