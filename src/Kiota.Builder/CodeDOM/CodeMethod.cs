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
        ClientConstructor
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
        public CodeMethod(CodeElement parent): base(parent) {}
        public HttpMethod? HttpMethod {get;set;}
        public CodeMethodKind MethodKind {get;set;} = CodeMethodKind.Custom;
        public string ContentType { get; set; }
        public AccessModifier Access {get;set;} = AccessModifier.Public;
        public CodeTypeBase ReturnType {get;set;}
        public List<CodeParameter> Parameters {get;set;} = new List<CodeParameter>();
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

        public object Clone()
        {
            return new CodeMethod(Parent) {
                MethodKind = MethodKind,
                ReturnType = ReturnType?.Clone() as CodeTypeBase,
                Parameters = Parameters.Select(x => x.Clone() as CodeParameter).ToList(),
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
            };
        }

        public void AddParameter(params CodeParameter[] methodParameters)
        {
            if(methodParameters == null || methodParameters.Any(x => x == null))
                throw new ArgumentNullException(nameof(methodParameters));
            if(!methodParameters.Any())
                throw new ArgumentOutOfRangeException(nameof(methodParameters));
            AddMissingParent(methodParameters);
            Parameters.AddRange(methodParameters);
        }
    }
}
