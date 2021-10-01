using System;
using System.Collections.Generic;
using System.Linq;

namespace Kiota.Builder
{
    public enum CodeMethodKind
    {
        Custom,
        IndexerBackwardCompatibility,
        RequestExecutor,
        RequestGenerator,
        Serializer,
        Deserializer,
        Constructor,
        Getter,
        Setter,
        ClientConstructor,
        RequestBuilderBackwardCompatibility,
        RequestBuilderWithParameters
    }
    public enum HttpMethod {
        Get,
        Post,
        Patch,
        Put,
        Delete,
        Options,
        Connect,
        Head,
        Trace
    }

    public class CodeMethod : CodeTerminal, ICloneable, IDocumentedElement
    {
        public HttpMethod? HttpMethod {get;set;}
        public CodeMethodKind MethodKind {get;set;} = CodeMethodKind.Custom;
        public string ContentType { get; set; }
        public AccessModifier Access {get;set;} = AccessModifier.Public;
        private CodeTypeBase returnType;
        public CodeTypeBase ReturnType {get => returnType;set {
            AddMissingParent(value);
            returnType = value;
        }}
        private readonly List<CodeParameter> parameters = new ();
        public void RemoveParametersByKind(params CodeParameterKind[] kinds) {
            parameters.RemoveAll(p => p.IsOfKind(kinds));
        }
        public IEnumerable<CodeParameter> Parameters { get => parameters; }
        public string PathSegment { get; set; }
        public bool IsStatic {get;set;} = false;
        public bool IsAsync {get;set;} = true;
        public string Description {get; set;}
        public CodeProperty AccessedProperty { get; set; }
        public bool IsOfKind(params CodeMethodKind[] kinds) {
            return kinds?.Contains(MethodKind) ?? false;
        }
        public bool IsAccessor { 
            get => IsOfKind(CodeMethodKind.Getter, CodeMethodKind.Setter);
        }
        public bool IsSerializationMethod {
            get => IsOfKind(CodeMethodKind.Serializer, CodeMethodKind.Deserializer);
        }
        public List<string> SerializerModules { get; set; }
        public List<string> DeserializerModules { get; set; }
        /// <summary>
        /// Indicates whether this method is an overload for another method.
        /// </summary>
        public bool IsOverload { get { return OriginalMethod != null; } }
        /// <summary>
        /// Provides a reference to the original method that this method is an overload of.
        /// </summary>
        public CodeMethod OriginalMethod { get; set; }

        public object Clone()
        {
            var method = new CodeMethod {
                MethodKind = MethodKind,
                ReturnType = ReturnType?.Clone() as CodeTypeBase,
                Name = Name.Clone() as string,
                HttpMethod = HttpMethod,
                IsAsync = IsAsync,
                Access = Access,
                IsStatic = IsStatic,
                Description = Description?.Clone() as string,
                ContentType = ContentType?.Clone() as string,
                AccessedProperty = AccessedProperty,
                PathSegment = PathSegment?.Clone() as string,
                SerializerModules = SerializerModules == null ? null : new (SerializerModules),
                DeserializerModules = DeserializerModules == null ? null : new (DeserializerModules),
                OriginalMethod = OriginalMethod,
                Parent = Parent
            };
            if(Parameters?.Any() ?? false)
                method.AddParameter(Parameters.Select(x => x.Clone() as CodeParameter).ToArray());
            return method;
        }

        public void AddParameter(params CodeParameter[] methodParameters)
        {
            if(methodParameters == null || methodParameters.Any(x => x == null))
                throw new ArgumentNullException(nameof(methodParameters));
            if(!methodParameters.Any())
                throw new ArgumentOutOfRangeException(nameof(methodParameters));
            AddMissingParent(methodParameters);
            parameters.AddRange(methodParameters);
        }
    }
}
