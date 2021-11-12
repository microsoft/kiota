namespace Kiota.Builder.Tests {
    public static class CodeDomExtensions {
        public static void AddBackingStoreProperty(this CodeClass codeClass) {
            codeClass?.AddProperty(new CodeProperty {
                Name = "backingStore",
                PropertyKind = CodePropertyKind.BackingStore
            });
        }
        public static void AddAccessedProperty(this CodeMethod codeMethod) {
            codeMethod.AccessedProperty = new CodeProperty {
                Name = "someProperty"
            };
            (codeMethod.Parent as CodeClass)?.AddProperty(codeMethod.AccessedProperty);
        }
    }
}
