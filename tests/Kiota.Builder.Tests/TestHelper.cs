using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Tests
{
    public class TestHelper
    {
        public static CodeClass CreateModelClass(string className = "model")
        {
            var testClass = new CodeClass
            {
                Name = className,
                Kind = CodeClassKind.Model
            };

            var deserializer = new CodeMethod
            {
                Name = "DeserializerMethod",
                ReturnType = new CodeType { },
                Kind = CodeMethodKind.Deserializer
            };

            var serializer = new CodeMethod
            {
                Name = "SerializerMethod",
                ReturnType = new CodeType { },
                Kind = CodeMethodKind.Serializer
            };
            testClass.AddMethod(deserializer);
            testClass.AddMethod(serializer);
            return testClass;
        }
    }
}
