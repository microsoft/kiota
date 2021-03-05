using System;
using System.Collections.Generic;
using System.Linq;

namespace Kiota.Builder {
    public class CodeTypeBase: CodeTerminal, ICloneable {
        public enum CodeTypeCollectionKind {
            None,
            Array,
            Complex
        }
        public CodeTypeBase(CodeElement parent) : base(parent) {
            
        }
        public CodeTypeBase(CodeTypeBase ancestor): base(ancestor.Parent)
        {
            ActionOf = ancestor.ActionOf;
            IsNullable = ancestor.IsNullable;
            CollectionKind = ancestor.CollectionKind;
        }
        public bool ActionOf = false;
        public bool IsNullable = true;
        public CodeTypeCollectionKind CollectionKind = CodeTypeCollectionKind.None;

        public object Clone()
        {
            return new CodeTypeBase(this);
        }
        public IEnumerable<CodeType> AllTypes {
            get {
                if(this is CodeType currentType)
                    return new CodeType[] { currentType };
                else if (this is CodeUnionType currentUnion)
                    return currentUnion.Types;
                else
                    return Enumerable.Empty<CodeType>();
            }
        }
    }
}
