using System;
using System.Collections.Generic;
using System.Linq;

namespace Kiota.Builder
{
    public class CodeUnionType : CodeTypeBase, ICloneable {
        public CodeUnionType(CodeElement parent) : base(parent) {
            
        }
        public void AddType(params CodeType[] codeTypes) {
            foreach(var codeType in codeTypes.Where(x => x != null && !Types.Contains(x)))
                Types.Add(codeType);
        }
        public List<CodeType> Types { get; private set; } = new List<CodeType>();

        public override object Clone() {
            return new CodeUnionType(this.Parent){
                Types = new List<CodeType>(Types),
            }.BaseClone<CodeUnionType>(this);
        }
    }
}
