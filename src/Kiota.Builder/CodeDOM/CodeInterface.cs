using System;
using System.Linq;

namespace Kiota.Builder.CodeDOM;

public enum CodeInterfaceKind
{
    Custom,
    Model,
    QueryParameters,
    RequestConfiguration
}

public class CodeInterface : ProprietableBlock<CodeInterfaceKind, InterfaceDeclaration>, ITypeDefinition
{
    public CodeClass? OriginalClass
    {
        get; set;
    }
}
public class InterfaceDeclaration : ProprietableBlockDeclaration
{
    public CodeProperty? GetOriginalPropertyDefinedFromBaseType(string propertyName)
    {
        ArgumentException.ThrowIfNullOrEmpty(propertyName);

        return Implements.OfType<CodeType>()
                        .Where(static x => !x.IsExternal)
                        .Select(static x => x.TypeDefinition)
                        .OfType<CodeInterface>()
                        .Select(currentParentInterface =>
        {
            if (currentParentInterface.FindChildByName<CodeProperty>(propertyName, false) is CodeProperty currentProperty && !currentProperty.ExistsInBaseType)
                return currentProperty;
            else
                return currentParentInterface.StartBlock.GetOriginalPropertyDefinedFromBaseType(propertyName);
        }).FirstOrDefault();
    }
}
