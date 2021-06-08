using System;

namespace Microsoft.Kiota.Abstractions.Serialization {
    public class SerializationWriterProxyFactory : ISerializationWriterFactory {
        private readonly ISerializationWriterFactory _concrete;
        private readonly Func<ISerializationWriter, ISerializationWriter> _constructor;
        public SerializationWriterProxyFactory(ISerializationWriterFactory concrete, Func<ISerializationWriter, ISerializationWriter> constructor) {
            _concrete = concrete ?? throw new ArgumentNullException(nameof(concrete));
            _constructor = constructor ?? throw new ArgumentNullException(nameof(constructor));
        }
        public ISerializationWriter GetSerializationWriter(string contentType) {
            return _constructor.Invoke(_concrete.GetSerializationWriter(contentType));
        }
    }
}
