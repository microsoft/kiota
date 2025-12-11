using System;
using System.Collections.Generic;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.PathSegmenters;

public class GoPathSegmenter(string rootPath, string clientNamespaceName) : CommonPathSegmenter(rootPath, clientNamespaceName)
{
    private static readonly HashSet<string> specialFileNameSuffixes = new(StringComparer.OrdinalIgnoreCase) {
        "test"       ,

        "aix"       ,
        "android"   ,
        "darwin"    ,
        "dragonfly" ,
        "freebsd"   ,
        "hurd"      ,
        "illumos"   ,
        "ios"       ,
        "js"        ,
        "linux"     ,
        "nacl"      ,
        "netbsd"    ,
        "openbsd"   ,
        "plan9"     ,
        "solaris"   ,
        "wasip1"    ,
        "windows"   ,
        "zos"       ,

        "aix"       ,
        "android"   ,
        "darwin"    ,
        "dragonfly" ,
        "freebsd"   ,
        "hurd"      ,
        "illumos"   ,
        "ios"       ,
        "linux"     ,
        "netbsd"    ,
        "openbsd"   ,
        "solaris"   ,

        "386"         ,
        "amd64"       ,
        "amd64p32"    ,
        "arm"         ,
        "armbe"       ,
        "arm64"       ,
        "arm64be"     ,
        "loong64"     ,
        "mips"        ,
        "mipsle"      ,
        "mips64"      ,
        "mips64le"    ,
        "mips64p32"   ,
        "mips64p32le" ,
        "ppc"         ,
        "ppc64"       ,
        "ppc64le"     ,
        "riscv"       ,
        "riscv64"     ,
        "s390"        ,
        "s390x"       ,
        "sparc"       ,
        "sparc64"     ,
        "wasm"        ,
    };

    public override string FileSuffix => ".go";
    public override IEnumerable<string> GetAdditionalSegment(CodeElement currentElement, string fileName)
    {
        return currentElement switch
        {
            CodeNamespace => new[] { GetLastFileNameSegment(currentElement) },// We put barrels inside namespace folders
            _ => Enumerable.Empty<string>(),
        };
    }

    public override string NormalizeFileName(CodeElement currentElement)
    {
        return currentElement switch
        {
            CodeNamespace => "go",
            _ => GetLastFileNameSegment(currentElement)
                .Replace("_escaped", "Escaped", StringComparison.OrdinalIgnoreCase)// prevent potential double escaping.
                .Replace("_", "_escaped_", StringComparison.OrdinalIgnoreCase)// at this point we have the guarantee that the element name is unique based on how child elements are held in Kiota.
                                                                              // The existence of the '_' character in an element name could make `ToSnakeCase` give the same file name.
                                                                              // e.g. codeScanningVariantAnalysisStatus and codeScanningVariantAnalysis_status both give code_scanning_variant_analysis
                                                                              // This shouldn't break anything as golang does not care about filenames.
                .ToSnakeCase()
                .EscapeSuffix(specialFileNameSuffixes).ShortenFileName(100),
        };
    }
    public override string NormalizeNamespaceSegment(string segmentName) => segmentName?.ToLowerInvariant() ?? string.Empty;
}
