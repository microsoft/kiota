using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Microsoft.OpenApi.Expressions;

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

        public static CodeClass AddInheritanceClassToModelClass(CodeClass modelClass)
        {
            var parentClass = CreateModelClass("SuperClass");
            (modelClass.Parent as CodeNamespace).AddClass(parentClass);
            modelClass.StartBlock.Inherits = new CodeType
            {
                Name = "someParentClass",
                TypeDefinition = parentClass
            };
            return parentClass;
        }

        public static void AddSerializationPropertiesToModelClass(CodeClass modelClass)
        {
            // Additional Data
            modelClass.AddProperty(new CodeProperty
            {
                Name = "additionalData",
                Kind = CodePropertyKind.AdditionalData,
                Type = new CodeType
                {
                    Name = "string"
                }
            });
            modelClass.AddProperty(new CodeProperty
            {
                Name = "dummyProp",
                Type = new CodeType
                {
                    Name = "string"
                }
            });

            // string array or primitive array
            modelClass.AddProperty(new CodeProperty
            {
                Name = "dummyColl",
                Type = new CodeType
                {
                    Name = "string",
                    CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
                }
            });

            // CodeClass property

            var propertyClass = CreateModelClass("SomeComplexType");
            (modelClass.Parent as CodeNamespace).AddClass(propertyClass);
            modelClass.AddProperty(new CodeProperty
            {
                Name = "dummyComplexColl",
                Type = new CodeType
                {
                    Name = "Complex",
                    CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
                    TypeDefinition = propertyClass
                }
            });

            // enum collection
            var propertyEnum = new CodeEnum
            {
                Name = "EnumType"
            };
            (modelClass.Parent as CodeNamespace).AddEnum(propertyEnum);
            modelClass.AddProperty(new CodeProperty
            {
                Name = "dummyEnumCollection",
                Type = new CodeType
                {
                    Name = "SomeEnum",
                    TypeDefinition = propertyEnum
                }
            });

            modelClass.AddProperty(new CodeProperty
            {
                Name = "definedInParent",
                Type = new CodeType
                {
                    Name = "string"
                },
                OriginalPropertyFromBaseType = new CodeProperty
                {
                    Name = "definedInParent",
                    Type = new CodeType
                    {
                        Name = "string"
                    }
                }
            });
        }
    }
}
