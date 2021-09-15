using System;
using System.Collections.Generic;
using System.Linq;

namespace Kiota.Builder
{
    public class CodeEnum : CodeElement, IDocumentedElement, ITypeDefinition {
        public List<string> Options { get; set; } = new List<string>();
        public bool Flags { get; set; }
        public string Description {get; set;}
        private readonly List<CodeUsing> usings = new ();
        public IEnumerable<CodeUsing> Usings { get => usings; }
        public void AddUsings(params CodeUsing[] usingsToAdd) {
            if(usingsToAdd == null || !usingsToAdd.Any()) throw new ArgumentNullException(nameof(usingsToAdd));
            AddMissingParent(usingsToAdd);
            usings.AddRange(usingsToAdd);
        }
    }
}
