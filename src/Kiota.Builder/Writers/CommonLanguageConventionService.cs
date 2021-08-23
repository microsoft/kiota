using System;
using System.Linq;

namespace Kiota.Builder.Writers {
    public abstract class CommonLanguageConventionService : ILanguageConventionService {
        public abstract string StreamTypeName
        {
            get;
        }
        public abstract string VoidTypeName
        {
            get;
        }
        public abstract string DocCommentPrefix
        {
            get;
        }
        public abstract string PathSegmentPropertyName
        {
            get;
        }
        public abstract string CurrentPathPropertyName
        {
            get;
        }
        public abstract string HttpCorePropertyName
        {
            get;
        }
        public abstract string RawUrlPropertyName
        {
            get;
        }
        public abstract string ParseNodeInterfaceName
        {
            get;
        }

        public abstract string GetAccessModifier(AccessModifier access);
        public abstract string GetParameterSignature(CodeParameter parameter);
        public abstract string GetTypeString(CodeTypeBase code);

        public string TranslateType(CodeTypeBase type) {
            if(type is CodeType currentType)
                return TranslateType(currentType);
            else if(type is CodeUnionType currentUnionType)
                return TranslateType(currentUnionType.AllTypes.First());
            else
                throw new InvalidOperationException("Unknown type");
        }

        public abstract string TranslateType(CodeType type);
        public abstract void WriteShortDescription(string description, LanguageWriter writer);
    }
}
