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
public class CodeClass : CodeBlock, IDocumentedElement, ITypeDefinition
{
    private string name;
    public CodeClass():base()
    {
        StartBlock = new Declaration() { Parent = this};
        EndBlock = new End() { Parent = this };
    }
    public CodeClassKind ClassKind { get; set; } = CodeClassKind.Custom;

    public bool IsErrorDefinition { get; set; }

    public string Description {get; set;}

    /// <summary>
    /// Gets/Sets the name of the property to use for discrimination during deserialization.
    /// </summary>
    public string DiscriminatorPropertyName { get; set; }
    /// <summary>
    /// Gets/Sets the discriminator values for the class where the key is the value as represented in the payload.
    /// </summary>
    public Dictionary<string, CodeTypeBase> DiscriminatorMappings { get; set; } = new();
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

    public IEnumerable<CodeProperty> AddProperty(params CodeProperty[] properties)
    {
        if(properties == null || properties.Any(x => x == null))
            throw new ArgumentNullException(nameof(properties));
        if(!properties.Any())
            throw new ArgumentOutOfRangeException(nameof(properties));
        return AddRange(properties);
    }
    public CodeProperty GetPropertyOfKind(CodePropertyKind kind) =>
    Properties.FirstOrDefault(x => x.IsOfKind(kind));
    public IEnumerable<CodeProperty> Properties => InnerChildElements.Values.OfType<CodeProperty>();
    public IEnumerable<CodeMethod> Methods => InnerChildElements.Values.OfType<CodeMethod>();

    public bool ContainsMember(string name)
    {
        return InnerChildElements.ContainsKey(name);
    }

    public IEnumerable<CodeMethod> AddMethod(params CodeMethod[] methods)
    {
        if(methods == null || methods.Any(x => x == null))
            throw new ArgumentNullException(nameof(methods));
        if(!methods.Any())
            throw new ArgumentOutOfRangeException(nameof(methods));
        return AddRange(methods);
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
        if(StartBlock is Declaration declaration)
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

    public bool IsOfKind(params CodeClassKind[] kinds) {
        return kinds?.Contains(ClassKind) ?? false;
    }

    public class Declaration : BlockDeclaration
    {
        private CodeType inherits;
        public CodeType Inherits { get => inherits; set {
            EnsureElementsAreChildren(value);
            inherits = value;
        } }
        private readonly List<CodeType> implements = new ();
        public void AddImplements(params CodeType[] types) {
            if(types == null || types.Any(x => x == null))
                throw new ArgumentNullException(nameof(types));
            EnsureElementsAreChildren(types);
            implements.AddRange(types);
        }
        public IEnumerable<CodeType> Implements => implements;
    }

    public class End : BlockEnd
    {
    }
}
