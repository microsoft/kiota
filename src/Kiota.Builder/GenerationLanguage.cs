namespace Kiota.Builder;

public enum GenerationLanguage
{
    CSharp,
    Java,
    TypeScript,
    PHP,
    Python,
    Go,
    Ruby,
    Dart,
    HTTP,
    // Emits PowerShell cmdlet classes that call a Kiota-generated C# client. Bypasses the
    // CodeDOM/refiner/writer pipeline, same shape as plugin generation; see
    // KiotaBuilder.GeneratePowerShellWrapperAsync.
    PowerShellWrapper
}
