using System;
using System.Collections.Generic;
using System.Text;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers.CSharp;
using Xunit;

namespace Kiota.Builder.Tests.Writers.CSharp
{
    public class CSharpConventionServiceTests
    {
        private readonly CSharpConventionService instance = new();


        [Fact]
        public void WritesSeeDoc()
        {
            // Tests that "<see cref=" documentation tags are written properly
            
            CodeElement targetElement = new CodeClass();

            // Reference to any class:
            CodeType codeType = new CodeType()
            {
                Name = "SomeClass"
            };

            var see = instance.GetTypeStringForDocumentation(codeType, targetElement);

            Assert.Equal("<see cref=\"SomeClass\"/>", see);

            // Reference to byte[]:
            codeType = new CodeType()
            {
                Name = "base64"
            };

            see = instance.GetTypeStringForDocumentation(codeType, targetElement);

            // Brackets are stripped, the word "array" is added.
            Assert.Equal("<see cref=\"byte\"/> array", see);
        }
    }
}
