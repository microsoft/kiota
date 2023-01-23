using System;
using System.IO;
using Xunit;

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

        var line = Array.Find(File.ReadAllLines(csprojFile), l => l.Contains("<Version>"));
        line = line.Trim();
        line = line.Replace("<Version>", "");
        var version = line.Replace("</Version>", "");

        Assert.Equal(version, Kiota.Generated.KiotaVersion.Current());
    }
}
