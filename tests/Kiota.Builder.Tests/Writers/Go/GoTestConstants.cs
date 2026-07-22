namespace Kiota.Builder.Tests.Writers.Go;

internal static class GoTestConstants
{
    /// <summary>
    /// The line feed character that Go tooling (e.g. gofmt) mandates for line endings.
    /// Go output always uses this regardless of the host OS newline, so tests assert against it directly.
    /// </summary>
    internal const string LineFeed = "\n";
}
