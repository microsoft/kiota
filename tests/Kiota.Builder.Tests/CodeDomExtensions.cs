using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Tests;

public static class CodeDomExtensions
{
    public static void AddBackingStoreProperty(this CodeClass codeClass)
    {
        codeClass?.AddProperty(new CodeProperty
        {
            Name = "backingStore",
            Kind = CodePropertyKind.BackingStore,
            Type = new CodeType
            {
                Name = "BackingStore",
            },
        });
    }
    public static void AddAccessedProperty(this CodeMethod codeMethod)
    {
        codeMethod.AccessedProperty = new CodeProperty
        {
            Name = "someProperty",
            Type = new CodeType
            {
                Name = "string",
            },
        };
        (codeMethod.Parent as CodeClass)?.AddProperty(codeMethod.AccessedProperty);
    }
}
