using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;

namespace Kiota.Builder.Tests;

public static class TestHelper
{
    public static CodeClass CreateModelClassInModelsNamespace(GenerationConfiguration config, CodeNamespace codeSpace, string className = "model", bool withInheritance = false)
    {
        var modelsNamespace = codeSpace.FindNamespaceByName(config.ModelsNamespaceName) ?? codeSpace.AddNamespace(config.ModelsNamespaceName);
        return CreateModelClass(modelsNamespace, className, withInheritance);
    }
    public static CodeClass CreateModelClass(CodeNamespace codeSpace, string className = "model", bool withInheritance = false)
    {
        var superClass = withInheritance ? CreateSuperClass(codeSpace) : default;
        var testClass = new CodeClass
        {
            Name = className,
            Kind = CodeClassKind.Model
        };
        if (withInheritance)
        {
            testClass.StartBlock.Inherits = new CodeType
            {
                Name = superClass.Name,
                TypeDefinition = superClass
            };
        }
        codeSpace.AddClass(testClass);

        var deserializer = new CodeMethod
        {
            Name = "DeserializerMethod",
            ReturnType = new CodeType { },
            Kind = CodeMethodKind.Deserializer,
            IsAsync = false,
        };

        var serializer = new CodeMethod
        {
            Name = "SerializerMethod",
            ReturnType = new CodeType { },
            Kind = CodeMethodKind.Serializer,
            IsAsync = false,
        };
        testClass.AddMethod(deserializer);
        testClass.AddMethod(serializer);
        return testClass;
    }

    private static CodeClass CreateSuperClass(CodeNamespace codeSpace)
    {
        var parentClass = CreateModelClass(codeSpace, "SuperClass");
        parentClass.AddProperty(new CodeProperty
        {
            Name = "definedInParent",
            Type = new CodeType
            {
                Name = "string"
            },
            Kind = CodePropertyKind.Custom,
        });
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

        var parentNamespace = modelClass.Parent as CodeNamespace;
        var propertyClass = CreateModelClass(parentNamespace, "SomeComplexType");
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
        var enumOption = new CodeEnumOption() { Name = "SomeOption" };
        propertyEnum.AddOption(enumOption);
        parentNamespace.AddEnum(propertyEnum);
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
            Kind = CodePropertyKind.Custom,
        });
    }

    public static CodeMethod CreateMethod(CodeClass parentClass, string methodName, string returnTypeName)
    {
        var method = new CodeMethod
        {
            Name = methodName,
            ReturnType = new CodeType
            {
                Name = returnTypeName
            }
        };
        return parentClass.AddMethod(method).First();
    }
}
