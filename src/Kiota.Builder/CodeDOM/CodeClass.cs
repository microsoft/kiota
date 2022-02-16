using System;
using System.Collections.Generic;
using System.Linq;

namespace Kiota.Builder;

public enum CodeClassKind {
    Custom,
    RequestBuilder,
    Model,
    QueryParameters,
    /// <summary>
    /// A single parameter to be provided by the SDK user which will contain query parameters, request body, options, etc.
    /// Only used for languages that do not support overloads or optional parameters like go.
    /// </summary>
    ParameterSet,
}
/// <summary>
/// CodeClass represents an instance of a Class to be generated in source code
/// </summary>
public class CodeClass : ProprietableBlock<CodeClassKind>, ITypeDefinition
{
    private string name;
    public CodeClass():base()
    {
        StartBlock = new ClassDeclaration() { Parent = this};
        EndBlock = new ClassEnd() { Parent = this };
    }

    public bool IsErrorDefinition { get; set; }

    /// <summary>
    /// Name of Class
    /// </summary>
    public override string Name
    {
        get => name;
        set
        {
            name = value;
            StartBlock.Name = name;
        }
    }

    public void SetIndexer(CodeIndexer indexer)
    {
        if(indexer == null)
            throw new ArgumentNullException(nameof(indexer));
        if(InnerChildElements.Values.OfType<CodeIndexer>().Any())
            throw new InvalidOperationException("this class already has an indexer, remove it first");
        AddRange(indexer);
    }
    public IEnumerable<CodeClass> AddInnerClass(params CodeClass[] codeClasses)
    {
        if(codeClasses == null || codeClasses.Any(x => x == null))
            throw new ArgumentNullException(nameof(codeClasses));
        if(!codeClasses.Any())
            throw new ArgumentOutOfRangeException(nameof(codeClasses));
        return AddRange(codeClasses);
    }
    public CodeClass GetParentClass() {
        if(StartBlock is ClassDeclaration declaration)
            return declaration.Inherits?.TypeDefinition as CodeClass;
        else return null;
    }
    
    public CodeClass GetGreatestGrandparent(CodeClass startClassToSkip = null) {
        var parentClass = GetParentClass();
        if(parentClass == null)
            return startClassToSkip != null && startClassToSkip == this ? null : this;
        // we don't want to return the current class if this is the start node in the inheritance tree and doesn't have parent
        else
            return parentClass.GetGreatestGrandparent(startClassToSkip);
    }
    public class ClassDeclaration : ProprietableBlockDeclaration
    {
        private CodeType inherits;
        public CodeType Inherits { get => inherits; set {
            EnsureElementsAreChildren(value);
            inherits = value;
        } }
    }

    public class ClassEnd : BlockEnd
    {
    }
}
