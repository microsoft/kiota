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
        protected CodeTypeBase(CodeElement parent) : base(parent) {
            
        }
        public bool ActionOf {get;set;} = false;
        public bool IsNullable {get;set;} = true;
        public CodeTypeCollectionKind CollectionKind {get;set;} = CodeTypeCollectionKind.None;
        public bool IsCollection { get { return CollectionKind != CodeTypeCollectionKind.None; } }
        public bool IsArray { get { return CollectionKind == CodeTypeCollectionKind.Array; } }
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
