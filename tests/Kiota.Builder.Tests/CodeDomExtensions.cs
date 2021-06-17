namespace Kiota.Builder.Tests {
    public static class CodeDomExtensions {
        public static void AddBackingStoreProperty(this CodeClass codeClass) {
            codeClass?.AddProperty(new CodeProperty(codeClass) {
                Name = "backingStore",
                PropertyKind = CodePropertyKind.BackingStore
            });
        }
        public static void AddAccessedProperty(this CodeMethod codeMethod) {
            codeMethod.AccessedProperty = new CodeProperty(codeMethod.Parent) {
                Name = "someProperty"
            };
            (codeMethod.Parent as CodeClass)?.AddProperty(codeMethod.AccessedProperty);
        }
    }
}
