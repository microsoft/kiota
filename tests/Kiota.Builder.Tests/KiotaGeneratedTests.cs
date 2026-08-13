using System;
using System.IO;
using Xunit;

namespace Kiota.Builder.Tests;

public class KiotaGeneratedTests
{
    [Fact]
    public void StaticallyGeneratedAssemblyVersion()
    {
        var topLevelFolder = Directory.GetParent(Path.GetDirectoryName(typeof(KiotaGeneratedTests).Assembly.Location))
            .Parent
            .Parent
            .Parent
            .Parent
            .FullName;
        var csprojFile = Path.Join(topLevelFolder, "src", "Kiota.Builder", "Kiota.Builder.csproj");

        var versionPrefix = GetLineValue(csprojFile, "VersionPrefix");
        var versionSuffix = GetLineValue(csprojFile, "VersionSuffix");
        if (!string.IsNullOrEmpty(versionSuffix) && !"$(VersionSuffix)".Equals(versionSuffix, StringComparison.OrdinalIgnoreCase))
            versionPrefix += "-" + versionSuffix;

        Assert.Equal(versionPrefix, Generated.KiotaVersion.Current());
    }
    private static string GetLineValue(string csprojFile, string key)
    {
        var line = Array.Find(File.ReadAllLines(csprojFile), l => l.Contains($"<{key}>"));
        line = line.Trim();
        line = line.Replace($"<{key}>", "");
        return line.Replace($"</{key}>", "");
    }
}
