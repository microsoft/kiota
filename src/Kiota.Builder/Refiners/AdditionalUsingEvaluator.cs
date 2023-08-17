using System;
using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Refiners;

#pragma warning disable CA1819
public record AdditionalUsingEvaluator(Func<CodeElement, bool> CodeElementEvaluator, string NamespaceName, bool IsErasable = false, params string[] ImportSymbols)
{
    public AdditionalUsingEvaluator(Func<CodeElement, bool> CodeElementEvaluator, string NamespaceName, params string[] ImportSymbols) : this(CodeElementEvaluator, NamespaceName, false, ImportSymbols) { }
}
#pragma warning restore CA1819
