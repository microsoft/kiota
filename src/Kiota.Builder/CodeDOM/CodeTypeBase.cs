using System;
using System.Collections.Generic;
using System.Linq;

namespace Kiota.Builder.CodeDOM {
    public abstract class CodeTypeBase: CodeTerminal, ICloneable {
        public enum CodeTypeCollectionKind {
            None,
            Array,
            Complex
        }

        /// <summary>
        /// Indicates that the type is a callback
        /// Example: ActionOf:true parameterA: (y: typeA) => void
        /// Example: ActionOf:false parameterA: typeA
        /// </summary>
        public bool ActionOf {get;set;}
        public bool IsNullable {get;set;} = true;
        public CodeTypeCollectionKind CollectionKind {get;set;} = CodeTypeCollectionKind.None;
        public bool IsCollection { get { return CollectionKind != CodeTypeCollectionKind.None; } }
        public bool IsArray { get { return CollectionKind == CodeTypeCollectionKind.Array; } }
        protected virtual ChildType BaseClone<ChildType>(CodeTypeBase source) where ChildType : CodeTypeBase
        {
            ActionOf = source.ActionOf;
            IsNullable = source.IsNullable;
            CollectionKind = source.CollectionKind;
            Name = source.Name?.Clone() as string;
            Parent = source.Parent;
            return this as ChildType;
        }

        public abstract object Clone();

        public IEnumerable<CodeType> AllTypes {
            get
            {
                if(this is CodeType currentType)
                    return new[] { currentType };
                if (this is CodeComposedTypeBase currentUnion)
                    return currentUnion.Types;
                return Enumerable.Empty<CodeType>();
            }
        }
    }
}
