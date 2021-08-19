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
                CodeParameterKind.RawUrl => 3,
                CodeParameterKind.QueryParameter => 4,
                CodeParameterKind.Headers => 5,
                CodeParameterKind.Options => 6,
                CodeParameterKind.ResponseHandler => 7,
                CodeParameterKind.Serializer => 8,
                CodeParameterKind.BackingStore => 9,
                CodeParameterKind.Custom => 12,
                CodeParameterKind.RequestBody => 11,
                CodeParameterKind.SetterValue => 10,
                _ => 13,
            };
        }
        private static int optionalWeight = 1000;
        private static int kindWeight = 10;
    }
}
