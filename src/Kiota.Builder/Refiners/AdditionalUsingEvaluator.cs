using System;

using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Refiners;

public record AdditionalUsingEvaluator(Func<CodeElement, bool> CodeElementEvaluator, string NamespaceName, params string[] ImportSymbols);
