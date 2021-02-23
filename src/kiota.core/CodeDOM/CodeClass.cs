using System;
using System.Linq;

namespace kiota.core
{
    public enum CodeClassKind {
        Custom,
        RequestBuilder,
        Model,
    }
    /// <summary>
    /// CodeClass represents an instance of a Class to be generated in source code
    /// </summary>
    public class CodeClass : CodeBlock
    {
        private string name;

        public CodeClass(CodeElement parent):base(parent)
        {
            StartBlock = new Declaration(this);
            EndBlock = new End(this);
        }
        public CodeClassKind ClassKind { get; set; } = CodeClassKind.Custom;

        /// <summary>
        /// Name of Class
        /// </summary>
        public override string Name
        {
            get => name;
            set
            {
                name = value;
                StartBlock = new Declaration(this) { Name = name };
            }
        }

        public void SetIndexer(CodeIndexer indexer)
        {
            this.InnerChildElements.Add(indexer);
        }

        public void AddProperty(params CodeProperty[] properties)
        {
            if(!properties.Any() || properties.Any(x => x == null))
                throw new ArgumentNullException(nameof(properties));
            AddMissingParent(properties);
            this.InnerChildElements.AddRange(properties);
        }

        public bool ContainsMember(string name)
        {
            return this.InnerChildElements.Any(e => e.Name == name);
        }

        public void AddMethod(params CodeMethod[] methods)
        {
            if(!methods.Any() || methods.Any(x => x == null))
                throw new ArgumentOutOfRangeException(nameof(methods));
            AddMissingParent(methods);
            this.InnerChildElements.AddRange(methods);
        }

        public void AddInnerClass(params CodeClass[] codeClasses)
        {
            if(!codeClasses.Any() || codeClasses.Any(x => x == null))
                throw new ArgumentOutOfRangeException(nameof(codeClasses));
            AddMissingParent(codeClasses);
            this.InnerChildElements.AddRange(codeClasses);
        }

        public class Declaration : BlockDeclaration
        {
            public Declaration(CodeElement parent):base(parent)
            {
                
            }
            /// <summary>
            /// Class name
            /// </summary>
            public override string Name
            {
                get; set;
            }
        }

        public class End : BlockEnd
        {
            public End(CodeElement parent):base(parent)
            {
                
            }
        }
    }
}
