using System;
using System.Collections.Generic;
using System.Linq;

namespace Kiota.Builder
{
    public class CodeUnionType : CodeTypeBase, ICloneable {
        public void AddType(params CodeType[] codeTypes) {
            AddMissingParent(codeTypes);
            foreach(var codeType in codeTypes.Where(x => x != null && !Types.Contains(x)))
                types.Add(codeType);
        }
        private readonly List<CodeType> types = new ();
        public IEnumerable<CodeType> Types { get => types; }

        public override object Clone() {
            var value = new CodeUnionType{
            }.BaseClone<CodeUnionType>(this);
            if(Types?.Any() ?? false)
                value.AddType(Types.ToArray());
            return value;
        }
    }
}
