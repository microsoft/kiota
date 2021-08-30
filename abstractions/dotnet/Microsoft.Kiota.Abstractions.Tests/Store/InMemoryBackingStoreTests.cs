using System.Linq;
using Microsoft.Kiota.Abstractions.Store;
using Xunit;

namespace Microsoft.Kiota.Abstractions.Tests.Store
{
    public class InMemoryBackingStoreTests
    {
        [Fact]
        public void SetsAndGetsValueFromStore()
        {
            // Arrange
            var testBackingStore = new InMemoryBackingStore();
            // Act
            Assert.Empty(testBackingStore.Enumerate());
            testBackingStore.Set("name", "Peter");
            // Assert
            Assert.NotEmpty(testBackingStore.Enumerate());
            Assert.Equal("Peter",testBackingStore.Enumerate().First().Value);
        }

        [Fact]
        public void PreventsDuplicatesInStore()
        {
            // Arrange
            var testBackingStore = new InMemoryBackingStore();
            // Act
            Assert.Empty(testBackingStore.Enumerate());
            testBackingStore.Set("name", "Peter");
            testBackingStore.Set("name", "Peter Pan");// modify a second time
            // Assert
            Assert.NotEmpty(testBackingStore.Enumerate());
            Assert.Single(testBackingStore.Enumerate());
            Assert.Equal("Peter Pan", testBackingStore.Enumerate().First().Value);
        }

        [Fact]
        public void EnumeratesValuesChangedToNullInStore()
        {
            // Arrange
            var testBackingStore = new InMemoryBackingStore();
            // Act
            Assert.Empty(testBackingStore.Enumerate());
            testBackingStore.Set("name", "Peter Pan");
            testBackingStore.Set("email", "peterpan@neverland.com");
            testBackingStore.Set<string>("phone", null); // phone changes to null
            // Assert
            Assert.NotEmpty(testBackingStore.EnumerateKeysForValuesChangedToNull());
            Assert.Single(testBackingStore.EnumerateKeysForValuesChangedToNull());
            Assert.Equal(3, testBackingStore.Enumerate().Count());// all values come back
            Assert.Equal("phone", testBackingStore.EnumerateKeysForValuesChangedToNull().First());
        }
    }
}
