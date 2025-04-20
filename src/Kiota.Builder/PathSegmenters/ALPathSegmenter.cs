using System;
using System.IO;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers.AL;

namespace Kiota.Builder.PathSegmenters;
public class ALPathSegmenter : CommonPathSegmenter
{
    public ALPathSegmenter(string rootPath, string clientNamespaceName) : base(rootPath, clientNamespaceName) { }
    public override string FileSuffix => ".al";
    public override string NormalizeNamespaceSegment(string segmentName) => segmentName.ToFirstCharacterUpperCase().ShortenFileName();
    public override string NormalizeFileName(CodeElement currentElement)
    {
        string nameAppendix = string.Empty;
        string shortName = GetLastFileNameSegment(currentElement);
        switch (currentElement)
        {
            case CodeClass: nameAppendix = ".Codeunit"; break;
            case CodeEnum: nameAppendix = ".Enum"; break;
            case CodeFunction: nameAppendix = ".Function"; break;
        }
        if (currentElement != null)
            if (currentElement.Name == "AppJson") // special case for the app.json file
                return "app.json";
        if (currentElement != null)
        {
            var originalName = currentElement.Name;
            currentElement.Name = shortName.GetShortName();
            currentElement.Name = currentElement.Name.Replace("_", string.Empty, StringComparison.OrdinalIgnoreCase);
            shortName = GetLastFileNameSegment(currentElement).ToFirstCharacterUpperCase().ShortenFileName() + nameAppendix;
            currentElement.Name = originalName;
            return shortName;
        }
        return string.Empty;
    }
    public override string NormalizePath(string fullPath)
    {
        var fileSuffix = FileSuffix;
        if (!string.IsNullOrEmpty(fullPath) && fullPath.Contains("app.json", StringComparison.OrdinalIgnoreCase)) // workaround, because I have no idea how to create a json file instead of an .al file here
        {
            fileSuffix = string.Empty;
            fullPath = fullPath.Replace("app.json.al", "app.json", StringComparison.OrdinalIgnoreCase);
        }
        if (ExceedsMaxPathLength(fullPath) && Path.GetDirectoryName(fullPath) is string directoryName)
        {
            var availableLength = MaxFilePathLength - (directoryName.Length + FileSuffix.Length + 2); // one for the folder separator and another to ensure its below limit
            return Path.Combine(directoryName, Path.GetFileName(fullPath).ShortenFileName(availableLength)[..Math.Min(64, availableLength)]) + fileSuffix;
        }
        return fullPath;
    }
    internal const int MaxFilePathLength = 32767;
    private static bool ExceedsMaxPathLength(string fullPath) => !string.IsNullOrEmpty(fullPath) && fullPath.Length > MaxFilePathLength;
}
