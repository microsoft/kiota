﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

[Generator]
public class KiotaVersionGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(context.CompilationProvider, (spc, compilation) =>
        {

            var directory = Path.GetDirectoryName(compilation.SyntaxTrees.First().FilePath);

            try
            {
                XmlDocument csproj = new XmlDocument();
                csproj.Load(PathHelper.Join(directory, "Kiota.Builder.csproj"));

                var version = csproj.GetElementsByTagName("VersionPrefix")[0].InnerText;
                var majorVersion = $"{Version.Parse(version).Major}.0.0";
                var versionSuffixTag = csproj.GetElementsByTagName("VersionSuffix");
                if (versionSuffixTag != null && versionSuffixTag.Count > 0)
                {
                    var versionSuffix = versionSuffixTag[0].InnerText;
                    if (!string.IsNullOrEmpty(versionSuffix) && !"$(VersionSuffix)".Equals(versionSuffix, StringComparison.OrdinalIgnoreCase))
                        version += "-" + versionSuffix;
                }
                string source = $@"// <auto-generated/>
namespace Kiota.Generated
{{
    /// <summary>
    /// The version class
    /// </summary>
    public static class KiotaVersion
    {{
        /// <summary>
        /// The current version string
        /// </summary>
        public static string Current()
        {{
            return ""{version}"";
        }}

        /// <summary>
        /// The current major version string
        /// </summary>
        public static string CurrentMajor()
        {{
            return ""{majorVersion}"";
        }}
    }}
}}
";
                // Add the source code to the compilation
                spc.AddSource($"KiotaVersion.g.cs", SourceText.From(source, Encoding.UTF8));
            }
            catch (Exception e)
            {
                throw new FileNotFoundException("KiotaVersionGenerator expanded in an invalid project, missing 'Kiota.Builder.csproj' file.", e);
            }
        });
    }
}
