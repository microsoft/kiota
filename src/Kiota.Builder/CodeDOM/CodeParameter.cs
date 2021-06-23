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
        SerializationFactory
    }

    public class CodeParameter : CodeTerminal, ICloneable, IDocumentedElement
    {
        public CodeParameter(CodeElement parent): base(parent)
        {
            
        }
        public CodeParameterKind ParameterKind {get;set;}= CodeParameterKind.Custom;
        public CodeTypeBase Type {get;set;}
        public bool Optional {get;set;}= false;
        public string Description {get; set;}
        public bool IsOfKind(params CodeParameterKind[] kinds) {
            return kinds?.Contains(ParameterKind) ?? false;
        }
        public object Clone()
        {
            return new CodeParameter(Parent) {
                Optional = Optional,
                ParameterKind = ParameterKind,
                Name = Name.Clone() as string,
                Type = Type?.Clone() as CodeTypeBase,
                Description = Description?.Clone() as string,
            };
        }
    }
}
