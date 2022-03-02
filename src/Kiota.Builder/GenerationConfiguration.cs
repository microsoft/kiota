using System;
using System.Collections.Generic;

namespace Kiota.Builder {
    public class GenerationConfiguration {
        public string OpenAPIFilePath { get; set; } = "openapi.yaml";
        public string OutputPath { get; set; } = "./output";
        public string ClientClassName { get; set; } = "ApiClient";
        public string ClientNamespaceName { get; set; } = "ApiSdk";
        public GenerationLanguage Language { get; set; } = GenerationLanguage.CSharp;
        public string ApiRootUrl { get; set; }
        public List<string> PropertiesPrefixToStrip { get; set; } = new() { "@odata."};
        public HashSet<string> IgnoredRequestContentTypes { get; set; } = new();
        public bool UsesBackingStore { get; set; }
        public List<string> Serializers { get; set; } = new();
        public List<string> Deserializers { get; set; } = new();
        public bool ShouldWriteNamespaceIndices { get { return BarreledLanguages.Contains(Language); } }
        public bool ShouldWriteBarrelsIfClassExists { get { return BarreledLanguagesWithConstantFileName.Contains(Language); } }
        public bool ShouldRenderMethodsOutsideOfClasses { get { return MethodOutsideOfClassesLanguages.Contains(Language); } }
        private static readonly HashSet<GenerationLanguage> MethodOutsideOfClassesLanguages = new () {
            GenerationLanguage.Go,
        };
        private static readonly HashSet<GenerationLanguage> BarreledLanguages = new () {
            GenerationLanguage.Ruby,
            // TODO: add typescript once we have a barrel writer for it
           GenerationLanguage.TypeScript
        };
        private static readonly HashSet<GenerationLanguage> BarreledLanguagesWithConstantFileName = new () {
            //TODO: add typescript once we have a barrel writer for it

          GenerationLanguage.TypeScript
        };
    }
}
