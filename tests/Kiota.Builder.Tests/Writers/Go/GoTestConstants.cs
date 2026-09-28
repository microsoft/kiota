namespace Kiota.Builder.Tests.Writers.Go;

internal static class GoTestConstants
{
    /// <summary>
    /// The line feed character that Go tooling (e.g. gofmt) mandates for line endings.
    /// Go output always uses this regardless of the host OS newline, so tests assert against it directly.
    /// </summary>
    internal const string LineFeed = "\n";
    /// <summary>
    /// The aliased serialization import prefix (hash of github.com/microsoft/kiota-abstractions-go/serialization)
    /// the Go writers use for serialization types such as Parsable and ParsableFactory.
    /// </summary>
    internal const string SerializationHashPrefix = "i878a80d2330e89d26896388a3f487eef27b0a0e6c010c493bf80be1452208f91.";
}
