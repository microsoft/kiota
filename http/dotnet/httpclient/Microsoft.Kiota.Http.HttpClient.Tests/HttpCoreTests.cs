using System;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Abstractions.Store;
using Moq;
using Xunit;

namespace Microsoft.Kiota.Http.HttpClient.Tests
{
    public class HttpCoreTests
    {
        private readonly IAuthenticationProvider _authenticationProvider;

        public HttpCoreTests()
        {
            _authenticationProvider = new Mock<IAuthenticationProvider>().Object;
        }

        [Fact]
        public void ThrowsArgumentNullExceptionOnNullAuthenticationProvider()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => new HttpCore(null));
            Assert.Equal("authenticationProvider", exception.ParamName);
        }

        [Fact]
        public void EnablesBackingStore()
        {
            // Arrange
            var httpCore = new HttpCore(_authenticationProvider);
            var backingStore = new Mock<IBackingStoreFactory>().Object;

            //Assert the that we originally have an in memory backing store
            Assert.IsAssignableFrom<InMemoryBackingStoreFactory>(BackingStoreFactorySingleton.Instance);

            // Act
            httpCore.EnableBackingStore(backingStore);

            //Assert the backing store has been updated
            Assert.IsAssignableFrom(backingStore.GetType(), BackingStoreFactorySingleton.Instance);
        }
    }
}
