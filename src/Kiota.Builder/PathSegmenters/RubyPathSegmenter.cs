using System;
using System.Text;
using Kiota.Builder.Extensions;

namespace Kiota.Builder {
    public class RubyPathSegmenter : CommonPathSegmenter
    {
        public RubyPathSegmenter(string rootPath, string clientNamespaceName) : base(rootPath, clientNamespaceName) { }
        public override string FileSuffix => ".rb";
        public override string NormalizeFileName(string elementName) => ToSnakeCase(elementName);
        public override string NormalizeNamespaceSegment(string segmentName) => ToSnakeCase(segmentName);

        public string ToSnakeCase(string text)
        {
            if(text == null) {
                throw new ArgumentNullException(nameof(text));
            }

            if(text.Length < 2) {
                return text;
            }
            var sb = new StringBuilder();
            sb.Append(char.ToLowerInvariant(text[0]));
            for(int i = 1; i < text.Length; ++i) {
                char c = text[i];
                if(char.IsUpper(c)) {
                    sb.Append('_');
                    sb.Append(char.ToLowerInvariant(c));
                } else {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }
    }

    

    
}
