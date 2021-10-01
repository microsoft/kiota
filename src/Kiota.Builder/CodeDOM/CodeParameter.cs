using System;
using System.Linq;

namespace Kiota.Builder
{
    public enum CodeParameterKind
    {
        Custom,
        QueryParameter,
        Headers,
        ResponseHandler,
        RequestBody,
        SetterValue,
        HttpCore,
        CurrentPath,
        Options,
        Serializer,
        BackingStore,
        RawUrl,
        Path
    }

    public class CodeParameter : CodeTerminal, ICloneable, IDocumentedElement
    {
        public CodeParameterKind ParameterKind {get;set;}= CodeParameterKind.Custom;
        private CodeTypeBase type;
        public CodeTypeBase Type {get => type; set {
            AddMissingParent(type);
            type = value;
        }}
        public bool Optional {get;set;}= false;
        public string Description {get; set;}
        public string DefaultValue {get; set;}
        public bool IsOfKind(params CodeParameterKind[] kinds) {
            return kinds?.Contains(ParameterKind) ?? false;
        }
        public object Clone()
        {
            return new CodeParameter {
                Optional = Optional,
                ParameterKind = ParameterKind,
                Name = Name.Clone() as string,
                Type = Type?.Clone() as CodeTypeBase,
                Description = Description?.Clone() as string,
                DefaultValue = DefaultValue?.Clone() as string,
                Parent = Parent
            };
        }
    }
}
