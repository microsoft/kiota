using System;
using System.Collections.Generic;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Refiners
{
    public class PhpRefiner: CommonLanguageRefiner
    {
        public PhpRefiner(GenerationConfiguration configuration) : base(configuration) { }


        private static readonly Tuple<string, string>[] defaultNamespaces = 
        {
            new("ParseNode", "Microsoft\\Kiota\\Abstractions\\Serialization")
        };

        private static readonly Tuple<string, string>[] defaultRequestBuilderNamespaces =
        {
            new("HttpMethod", "Microsoft\\Kiota\\Abstractions"),
            new("HttpCore","Microsoft\\Kiota\\Abstractions"),
            new("RequestInformation", "Microsoft\\Kiota\\Abstractions"),
            new("ResponseHandler", "Microsoft\\Kiota\\Abstractions"),
            new("QueryParametersBase", "Microsoft\\Kiota\\Abstractions"),
            new("MiddlewareOption", "Microsoft\\Kiota\\Abstractions")
        };

        private static readonly Tuple<string, string>[] defaultNamespacesForModels = { };
        public override void Refine(CodeNamespace generatedCode)
        {
            //AddInnerClasses(generatedCode);
            AddDefaultImports(generatedCode, defaultNamespaces, 
                defaultNamespacesForModels, 
                defaultRequestBuilderNamespaces);
            AddPropertiesAndMethodTypesImports(generatedCode, true, false, true);
            ReplaceIndexersByMethodsWithParameter(generatedCode, generatedCode, false, "ById");
            AddGetterAndSetterMethods(generatedCode,new HashSet<CodePropertyKind>()
            {
                CodePropertyKind.Custom,
                CodePropertyKind.AdditionalData,
                CodePropertyKind.BackingStore
            }, _configuration.UsesBackingStore, true);
            ReplaceReservedNames(generatedCode, new PhpReservedNamesProvider(), reservedWord => $"Graph{reservedWord.ToFirstCharacterUpperCase()}");
            ReplaceBinaryByNativeType(generatedCode, "StreamInterface", "Psr\\Http\\Message", true);
            MoveClassesWithNamespaceNamesUnderNamespace(generatedCode);
        }
    }
}
