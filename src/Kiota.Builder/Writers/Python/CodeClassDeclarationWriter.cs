using System;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Python;

public class CodeClassDeclarationWriter : BaseElementWriter<ClassDeclaration, PythonConventionService>
{
    private readonly CodeUsingWriter _codeUsingWriter;
    public CodeClassDeclarationWriter(PythonConventionService conventionService, string clientNamespaceName) : base(conventionService)
    {
        _codeUsingWriter = new(clientNamespaceName);
    }
    public override void WriteCodeElement(ClassDeclaration codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        var parentNamespace = codeElement.GetImmediateParentOfType<CodeNamespace>();
        _codeUsingWriter.WriteExternalImports(codeElement, writer); // external imports before internal imports
        if (codeElement.Parent?.Parent is not CodeClass) //Internal imports for inner classes will be written locally
        {
            _codeUsingWriter.WriteConditionalInternalImports(codeElement, writer, parentNamespace);
        }

        var parentClass = codeElement.Parent as CodeClass;
        var typeParameters = parentClass?.TypeParameters.ToArray() ?? [];
        var isInnerClass = codeElement.Parent?.Parent is CodeClass;
        if (typeParameters.Length != 0 && !isInnerClass)
            foreach (var typeParameter in typeParameters)
                writer.WriteLine($"{typeParameter.Name} = TypeVar(\"{typeParameter.Name}\")");

        WriteParentClassImportsAndDecorators(codeElement, writer);

        var derivation = GetDerivation(codeElement);
        writer.WriteLine($"class {codeElement.Name}({derivation}):");
        writer.IncreaseIndent();
        WriteInnerClassImportsAndDescriptions(codeElement, writer, parentNamespace);
        if (typeParameters.Length != 0 && !isInnerClass)
            WriteGenericTypeSubscriptionMachinery(typeParameters, writer);
    }

    internal static string GetTypeParameterSlotName(CodeTypeParameter typeParameter)
    {
        var parameterName = typeParameter.Name;
        if (parameterName.Length > 1 && parameterName[0] == 'T' && char.IsUpper(parameterName[1]))
            parameterName = parameterName[1..];
        return $"_{parameterName.ToSnakeCase()}";
    }

    private static void WriteGenericTypeSubscriptionMachinery(CodeTypeParameter[] typeParameters, LanguageWriter writer)
    {
        var slotNames = typeParameters.Select(GetTypeParameterSlotName).ToArray();
        writer.WriteLine("_specializations = {}");
        foreach (var slotName in slotNames)
            writer.WriteLine($"{slotName} = None");
        writer.WriteLine();
        var isSingle = typeParameters.Length == 1;
        var displayName = isSingle ?
            "getattr(item, '__name__', str(item))" :
            "', '.join(getattr(i, '__name__', str(i)) for i in item)";
        var slotAssignments = isSingle ?
            $"{{\"{slotNames[0]}\": item}}" :
            $"{{{string.Join(", ", slotNames.Select(static (slot, index) => $"\"{slot}\": item[{index}]"))}}}";
        writer.WriteLine("def __class_getitem__(cls, item):");
        writer.IncreaseIndent();
        writer.WriteLine("if item not in cls._specializations:");
        writer.IncreaseIndent();
        writer.WriteLine($"cls._specializations[item] = type(f\"{{cls.__name__}}[{{{displayName}}}]\", (cls,), {slotAssignments})");
        writer.DecreaseIndent();
        writer.WriteLine("return cls._specializations[item]");
        writer.DecreaseIndent();
        writer.WriteLine();
    }

    private void WriteParentClassImportsAndDecorators(ClassDeclaration codeElement, LanguageWriter writer)
    {
        if (codeElement.Parent is CodeClass parentClass)
        {
            if (codeElement.Inherits != null)
                _codeUsingWriter.WriteDeferredImport(parentClass, codeElement.Inherits.Name, writer);
            foreach (var implement in codeElement.Implements)
                _codeUsingWriter.WriteDeferredImport(parentClass, implement.Name, writer);
            if (parentClass.IsOfKind(CodeClassKind.Model) || parentClass.Parent is CodeClass)
            {
                writer.WriteLine("@dataclass");
            }
        }
    }

    private string GetDerivation(ClassDeclaration codeElement)
    {
        var abcClass = !codeElement.Implements.Any() ? string.Empty : $"{codeElement.Implements.Select(static x => x.Name).Aggregate((x, y) => x + ", " + y)}";
        var baseClass = codeElement.Inherits is CodeType inheritType &&
                        conventions.GetTypeString(inheritType, codeElement) is string inheritSymbol &&
                        !string.IsNullOrEmpty(inheritSymbol) ?
                            inheritSymbol :
                            string.Empty;
        if (codeElement.Parent is CodeClass { IsGeneric: true } parentClass)
        {
            var genericBase = $"Generic[{string.Join(", ", parentClass.TypeParameters.Select(static x => x.Name))}]";
            return string.Join(", ", new[] { baseClass, abcClass, genericBase }.Where(static x => !string.IsNullOrEmpty(x)));
        }
        if (string.IsNullOrEmpty(baseClass))
        {
            return abcClass;
        }
        else if (string.IsNullOrEmpty(abcClass))
        {
            return baseClass;
        }
        else
        {
            return $"{baseClass}, {abcClass}";
        }
    }

    private void WriteInnerClassImportsAndDescriptions(ClassDeclaration codeElement, LanguageWriter writer, CodeNamespace parentNamespace)
    {
        if (codeElement.Parent is CodeClass parent)
        {
            if (parent.Parent is CodeClass) // write imports for inner classes
            {
                _codeUsingWriter.WriteExternalImports(codeElement, writer);
                _codeUsingWriter.WriteConditionalInternalImports(codeElement, writer, parentNamespace);
            }
            conventions.WriteLongDescription(parent, writer);
            conventions.WriteDeprecationWarning(parent, writer);
        }
    }
}
