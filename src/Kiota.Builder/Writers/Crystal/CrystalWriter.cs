using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.PathSegmenters;

namespace Kiota.Builder.Writers.Crystal
{
    public class CrystalWriter : LanguageWriter
    {
        private readonly CrystalConventionService conventionService;
        public CrystalWriter(string outputPath, string clientNamespaceName)
        {
            conventionService = new CrystalConventionService();
            PathSegmenter = new CrystalPathSegmenter(outputPath, clientNamespaceName);
            
            // Register all the Crystal-specific writers
            AddOrReplaceCodeElementWriter(new CodeClassDeclarationWriter(conventionService));
            AddOrReplaceCodeElementWriter(new CodeClassEndWriter(conventionService));
            AddOrReplaceCodeElementWriter(new CodeMethodWriter(conventionService));
            AddOrReplaceCodeElementWriter(new CodePropertyWriter(conventionService));
            AddOrReplaceCodeElementWriter(new CodeNameSpaceWriter(conventionService, (CrystalPathSegmenter)PathSegmenter));
            AddOrReplaceCodeElementWriter(new CodeTypeWriter(conventionService));
            AddOrReplaceCodeElementWriter(new CodeEnumWriter(conventionService));
            AddOrReplaceCodeElementWriter(new CodeIndexerWriter(conventionService));
        }

        public override string ToString()
        {
            return "Crystal";
        }
    }
}
