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
        RequestAdapter,
        /// <summary>
        /// The set of parameters to be carried over to the next request builder.
        /// </summary>
        PathParameters,
        Options,
        Serializer,
        BackingStore,
        /// <summary>
        /// A single parameter to be provided by the SDK user which will be added to the path parameters.
        /// </summary>
        Path,
        RawUrl,
        /// <summary>
        /// A single parameter to be provided by the SDK user which will contain query parameters, request body, options, etc.
        /// Only used for languages that do not support overloads or optional parameters like go.
        /// </summary>
        ParameterSet,
        /// <summary>
        /// A single parameter to be provided by the SDK user which can be used to cancel requests.
        /// </summary>
        Cancellation,
        /// <summary>
        /// A parameter representing the parse node to be used for deserialization during discrimination.
        /// </summary>
        ParseNode
    }

    public class CodeParameter : CodeTerminalWithKind<CodeParameterKind>, ICloneable, IDocumentedElement
    {
        private CodeTypeBase type;
        public CodeTypeBase Type {get => type; set {
            EnsureElementsAreChildren(type);
            type = value;
        }}
        public bool Optional {get;set;}= false;
        public string Description {get; set;}
        public string DefaultValue {get; set;}
        /// <summary>
        /// The name of the url template parameter this path parameter maps to.
        /// </summary>
        public string UrlTemplateParameterName { get; set; }
        public object Clone()
        {
            return new CodeParameter {
                Optional = Optional,
                Kind = Kind,
                Name = Name.Clone() as string,
                Type = Type?.Clone() as CodeTypeBase,
                Description = Description?.Clone() as string,
                DefaultValue = DefaultValue?.Clone() as string,
                Parent = Parent,
                UrlTemplateParameterName = UrlTemplateParameterName?.Clone() as string,
            };
        }
    }
}
