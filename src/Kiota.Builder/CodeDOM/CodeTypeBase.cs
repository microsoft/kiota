using System;
using System.Collections.Generic;
using System.Linq;

namespace Kiota.Builder {
    public abstract class CodeTypeBase: CodeTerminal, ICloneable {
        public enum CodeTypeCollectionKind {
            None,
            Array,
            Complex
        }
        public CodeTypeBase(CodeElement parent) : base(parent) {
            
        }
        public bool ActionOf = false;
        public bool IsNullable = true;
        public CodeTypeCollectionKind CollectionKind = CodeTypeCollectionKind.None;

        public ChildType BaseClone<ChildType>(CodeTypeBase source) where ChildType : CodeTypeBase
        {
            ActionOf = source.ActionOf;
            IsNullable = source.IsNullable;
            CollectionKind = source.CollectionKind;
            Name = source.Name.Clone() as string;
            return this as ChildType;
        }

        public abstract object Clone();

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
