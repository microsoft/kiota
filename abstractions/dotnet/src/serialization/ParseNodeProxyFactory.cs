using System;
using System.IO;

namespace Microsoft.Kiota.Abstractions.Serialization {
    public abstract class ParseNodeProxyFactory : IParseNodeFactory {
        public string ValidContentType { get { return _concrete.ValidContentType; }}
        private readonly IParseNodeFactory _concrete;
        private readonly Action<IParsable> _onBefore;
        private readonly Action<IParsable> _onAfter;
        public ParseNodeProxyFactory(IParseNodeFactory concrete, Action<IParsable> onBefore, Action<IParsable> onAfter) {
            _concrete = concrete ?? throw new ArgumentNullException(nameof(concrete));
            _onBefore = onBefore;
            _onAfter = onAfter;
        }
        public IParseNode GetRootParseNode(string contentType, Stream content) {
            var node = _concrete.GetRootParseNode(contentType, content);
            var originalBefore = node.OnBeforeAssignFieldValues;
            var originalAfter = node.OnAfterAssignFieldValues;
            node.OnBeforeAssignFieldValues = (x) => {
                _onBefore?.Invoke(x);
                originalBefore?.Invoke(x);
            };
            node.OnAfterAssignFieldValues = (x) => {
                _onAfter?.Invoke(x);
                originalAfter?.Invoke(x);
            };
            return node;
        }
    }
}
