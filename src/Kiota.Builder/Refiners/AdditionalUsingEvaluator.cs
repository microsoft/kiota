using System;

namespace Kiota.Builder.Refiners {
    public record AdditionalUsingEvaluator(Func<CodeElement, bool> CodeElementEvaluator, string NamespaceName, params string[] ImportSymbols);
}
