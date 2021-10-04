using System.Collections.Generic;

namespace Kiota.Builder {
    public class CodeParameterOrderComparer : IComparer<CodeParameter>
    {
        public int Compare(CodeParameter x, CodeParameter y)
        {
            return (x, y) switch {
                (null, null) => 0,
                (null, _) => -1,
                (_, null) => 1,
                _ => x.Optional.CompareTo(y.Optional) * optionalWeight +
                    getKindOrderHint(x.ParameterKind).CompareTo(getKindOrderHint(y.ParameterKind)) * kindWeight,
            };
        }
        private static int getKindOrderHint(CodeParameterKind kind) {
            return kind switch {
                CodeParameterKind.CurrentPath => 1,
                CodeParameterKind.HttpCore => 2,
                CodeParameterKind.Path => 3,
                CodeParameterKind.RawUrl => 4,
                CodeParameterKind.QueryParameter => 5,
                CodeParameterKind.Headers => 6,
                CodeParameterKind.Options => 7,
                CodeParameterKind.ResponseHandler => 8,
                CodeParameterKind.Serializer => 9,
                CodeParameterKind.BackingStore => 10,
                CodeParameterKind.SetterValue => 11,
                CodeParameterKind.RequestBody => 12,
                CodeParameterKind.Custom => 13,
                _ => 14,
            };
        }
        private static readonly int optionalWeight = 1000;
        private static readonly int kindWeight = 10;
    }
}
