using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Kiota.Builder.CodeDOM;
using static Kiota.Builder.CodeDOM.CodeTypeBase;

namespace Kiota.Builder.Writers.AL;
public static class ALVariableProvider
{
    public static ALConventionService ConventionService { get; } = new();
    public static CodeProperty GetObjectProperty(string name, string defaultValue)
    {
        var prop = GetVariable(name, new CodeType { Name = "Object", IsExternal = true }, defaultValue, false, false);
        prop.SetObjectProperty();
        return prop;
    }
    public static CodeProperty GetLocalVariable(string name, string type, string defaultValue)
    {
        return GetVariable(name, new CodeType { Name = type }, defaultValue, true, false);
    }
    public static CodeProperty GetLocalVariable(string name, CodeType type, string defaultValue)
    {
        return GetVariable(name, type, defaultValue, true, false);
    }
    public static CodeProperty GetGlobalVariable(string name, string type, string defaultValue, string pragma = "")
    {
        return GetVariable(name, new CodeType { Name = type }, defaultValue, false, true, pragma);
    }
    public static CodeProperty GetGlobalVariable(string name, CodeType type, string defaultValue, string pragma = "")
    {
        return GetVariable(name, type, defaultValue, false, true, pragma);
    }
    public static CodeProperty GetVariable(string name, CodeType type, string defaultValue, bool local, bool global, string pragma = "")
    {
        var prop = new CodeProperty
        {
            Name = name,
            Type = type,
            DefaultValue = defaultValue
        };
        if (local)
            prop.SetLocalVariable();
        if (global)
            prop.SetGlobalVariable();
        if (!string.IsNullOrEmpty(pragma))
            prop.SetPragmas([pragma]);
        return prop;
    }
    public static CodeParameter GetParameterP(string name, CodeTypeBase type, string defaultValue)
    {
        return GetVariableP(name, type, defaultValue, false, false);
    }
    public static CodeParameter GetParameterP(string name, string type, string defaultValue)
    {
        return GetVariableP(name, new CodeType { Name = type }, defaultValue, false, false);
    }
    public static CodeParameter ToParameter(this CodeIndexer indexer)
    {
        ArgumentNullException.ThrowIfNull(indexer);
        return GetParameterP(indexer.Name, indexer.ReturnType, "");
    }
    public static CodeParameter GetLocalVariableP(string name, string type, string defaultValue)
    {
        return GetVariableP(name, new CodeType { Name = type }, defaultValue, true, false);
    }
    public static CodeParameter GetLocalVariableP(string name, CodeTypeBase type, string defaultValue)
    {
        return GetVariableP(name, type, defaultValue, true, false);
    }
    public static CodeParameter GetGlobalVariableP(string name, string type, string defaultValue)
    {
        return GetVariableP(name, new CodeType { Name = type }, defaultValue, false, true);
    }
    public static CodeParameter GetGlobalVariableP(string name, CodeType type, string defaultValue)
    {
        return GetVariableP(name, type, defaultValue, false, true);
    }
    public static CodeParameter GetVariableP(string name, CodeTypeBase type, string defaultValue, bool local, bool global)
    {
        var param = new CodeParameter
        {
            Name = name,
            Type = type,
            DefaultValue = defaultValue
        };
        if (local)
            param.SetLocalVariable();
        if (global)
            param.SetGlobalVariable();
        return param;
    }
    public static IEnumerable<ALVariable> CombineVariablesOfSameType(IEnumerable<ALVariable> properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        // all names should be concatenated, separated by a comma
        var combinedProperties = new List<ALVariable>();
        foreach (var property in properties)
        {
            //var existingProperty = combinedProperties.FirstOrDefault(x => x.Type.Name == property.Type.Name && x.Type.CollectionKind == property.Type.CollectionKind);
            var existingProperty = combinedProperties.FirstOrDefault(x => x.Type.Name == property.Type.Name && x.Type.CollectionKind == property.Type.CollectionKind);
            if ((existingProperty is not null) && existingProperty.CanBeCombined(property))
                existingProperty.Name += $", {property.Name}";
            else
                combinedProperties.Add(property);
        }
        return combinedProperties;
    }
    public static IEnumerable<CodeEnumOption> GetDefaultObjectProperties(CodeEnum codeEnum)
    {
        return
        [
            GetObjectProperty("Access", "Internal").ToCodeEnumOption()
        ];
    }
    public static IEnumerable<CodeProperty> GetDefaultObjectProperties(CodeClass codeClass)
    {
        return
        [
            GetObjectProperty("Access", "Internal")
        ];
    }
    public static IEnumerable<CodeProperty> GetDefaultGlobals(CodeClass codeClass)
    {
        var globals = new List<CodeProperty>
        {
            GetGlobalVariable("JSONHelper", new CodeType { Name = "codeunit \"JSON Helper SOHH\"", IsExternal = true }, "2", "AA0137"),
            GetGlobalVariable("DebugCall", "Boolean", "4"),
            GetGlobalVariable($"{ConventionService.ModelCodeunitJsonBodyVariableName}", "JsonToken", "3"),
            GetGlobalVariable("SubToken", "JsonToken", "3")
        };
        return globals;
    }
    public static IEnumerable<CodeMethod> GetDefaultIApiClientMethods(CodeClass codeClass)
    {
        var defaults = new List<CodeMethod>
        {
            GetSystemRestClientHttpResponseMessageGetResponseMethod(codeClass),
            GetSystemRestClientHttpResponseMessageSetResponseMethod(codeClass),
            GetHttpResponseMessageGetResponseMethod(codeClass),
            GetHttpResponseMessageSetResponseMethod(codeClass),
        };
        return defaults;
    }
    public static CodeMethod GetSystemRestClientHttpResponseMessageGetResponseMethod(CodeClass codeClass)
    {
        var method = new CodeMethod
        {
            Name = "Response",
            SimpleName = "Response",
            Access = AccessModifier.Public,
            ReturnType = new CodeType { Name = "codeunit System.RestClient.\"Http Response Message\"", IsExternal = true },
            Kind = CodeMethodKind.Custom
        };
        method.AddCustomProperty("sorting-value", "96");
        return method;
    }
    public static CodeMethod GetSystemRestClientHttpResponseMessageSetResponseMethod(CodeClass codeClass)
    {
        var method = new CodeMethod
        {
            Name = "Response-overload",
            SimpleName = "Response",
            Access = AccessModifier.Public,
            ReturnType = new CodeType { Name = "void" },
            Kind = CodeMethodKind.Custom
        };
        method.AddParameter(GetParameterP("var response", new CodeType { Name = "codeunit System.RestClient.\"Http Response Message\"", IsExternal = true }, "1"));
        method.AddCustomProperty("sorting-value", "97");
        return method;
    }
    public static CodeMethod GetHttpResponseMessageGetResponseMethod(CodeClass codeClass)
    {
        var method = new CodeMethod
        {
            Name = "HttpResponse",
            SimpleName = "HttpResponse",
            Access = AccessModifier.Public,
            ReturnType = new CodeType { Name = "HttpResponseMessage" },
            Kind = CodeMethodKind.Custom
        };
        method.AddCustomProperty("sorting-value", "98");
        return method;
    }
    public static CodeMethod GetHttpResponseMessageSetResponseMethod(CodeClass codeClass)
    {
        var method = new CodeMethod
        {
            Name = "HttpResponse-overload",
            SimpleName = "HttpResponse",
            Access = AccessModifier.Public,
            ReturnType = new CodeType { Name = "void" },
            Kind = CodeMethodKind.Custom
        };
        method.AddCustomProperty("sorting-value", "99");
        method.AddParameter(GetParameterP("var response", new CodeType { Name = "HttpResponseMessage" }, "1"));
        return method;
    }
    public static IEnumerable<CodeMethod> GetDefaultModelCodeunitMethods(CodeClass codeClass)
    {
        var defaults = new List<CodeMethod>
        {
            GetSetBodyMethod(codeClass),
            GetSetBodyFullMethod(codeClass),
            GetModelTestMethod(codeClass),
            GetToJsonMethod(codeClass)
        };
        var fullToJsonMethod = GetFullToJsonMethod(codeClass);
        if (fullToJsonMethod != null)
            defaults.Add(fullToJsonMethod);
        return defaults;
    }
    public static CodeMethod GetSetBodyMethod(CodeClass codeClass)
    {
        var method = new CodeMethod
        {
            Name = "AAASetBody", // the "AAA" is a workaround to force this method to be the first one in the class
            SimpleName = "SetBody",
            Access = AccessModifier.Public,
            ReturnType = new CodeType
            {
                Name = "void"
            },
            Documentation = new CodeDocumentation
            {
                DocumentationLabel = "Creates a new instance of the model",
            },
            Kind = CodeMethodKind.Custom,
        };
        method.AddParameter(GetParameterP($"New{ConventionService.ModelCodeunitJsonBodyVariableName}", "JsonToken", "1"));
        return method;
    }
    public static CodeMethod GetSetBodyFullMethod(CodeClass codeClass)
    {
        var method = GetSetBodyMethod(codeClass);
        method.Name = $"{method.Name}-overload";   // needed as workaround, to be able to add overloads (AL doesn't support optional params)
                                                   // this is changed back in a later step        
        method.AddParameter(GetParameterP("Debug", "Boolean", "2"));
        return method;
    }
    public static CodeMethod GetModelTestMethod(CodeClass codeClass)
    {
        var method = new CodeMethod
        {
            Name = "AAAValidate",
            SimpleName = "ValidateBody",
            Access = AccessModifier.Private,
            ReturnType = new CodeType
            {
                Name = "void"
            },
            Documentation = new CodeDocumentation
            {
                DocumentationLabel = "Test all properties/assignments",
            },
            Kind = CodeMethodKind.Custom
        };
        codeClass.GetPropertyMethods().ToList().ForEach(x => { method.AddParameter(ALVariableProvider.GetLocalVariableP(x.Name, x.ReturnType, "0")); });
        method.SetPragmasVariables(["AA0021", "AA0202"]);
        return method;
    }
    public static CodeMethod GetToJsonMethod(CodeClass codeClass)
    {
        var method = new CodeMethod
        {
            Name = "ToJson",
            SimpleName = "ToJson",
            Access = AccessModifier.Public,
            ReturnType = new CodeType { Name = "JsonToken" },
            Kind = CodeMethodKind.Serializer
        };
        return method;
    }
    public static CodeMethod? GetFullToJsonMethod(CodeClass codeClass)
    {
        var method = GetToJsonMethod(codeClass);
        method.Name = $"{method.Name}-overload";   // needed as workaround, to be able to add overloads (AL doesn't support optional params)
                                                   // this is changed back in a later step        
        var methods = codeClass.GetPropertyMethods().ToList();
        if (methods.Count == 0)
            return null;
        codeClass.GetPropertyMethods().ToList().ForEach(x =>
        {
            var param = new CodeParameter { Name = x.Name, Type = x.ReturnType };
            if (ConventionService.IsCodeunitType(param.Type))
                if (param.Type.IsCollection)
                {
                    param.AddCustomProperty("corresponding-array", $"{x.Name}Array");
                    param.AddCustomProperty("foreach-variable", x.Name.GetSingularName());
                }
            method.AddParameter(param);
        });
        foreach (var meth in methods.Where(x => x.ReturnType.IsCollection && ConventionService.IsCodeunitType(x.ReturnType)))
        {
            method.AddParameter(ALVariableProvider.GetLocalVariableP($"{meth.Name}Array", "JsonArray", "3"));
            method.AddParameter(ALVariableProvider.GetLocalVariableP($"{meth.Name.GetSingularName()}", (CodeType)meth.ReturnType.CloneWithoutCollection(), "2"));
        }
        method.AddParameter(ALVariableProvider.GetLocalVariableP("TargetJson", "JsonObject", "1"));
        if (method.Variables().Count() > 1)
            method.SetPragmasVariables(["AA0021"]); // AA0021: Variable declarations should be ordered by type.
        method.SetPragmas(["AA0245"]); // AA0245: The name of the parameter '<name>' is identical to a field, method, or action in the same scope.
        return method;
    }

    public static CodeMethod GetApiClientConfigurationMethod(CodeClass codeClass)
    {
        var method = new CodeMethod
        {
            Name = "AAAConfiguration",
            SimpleName = "Configuration",
            Access = AccessModifier.Public,
            ReturnType = new CodeType { Name = "codeunit \"Kiota ClientConfig SOHH\"", IsExternal = true },
            Kind = CodeMethodKind.Custom
        };
        method.AddCustomProperty("sorting-value", "27");
        return method;
    }
    public static CodeMethod GetApiClientConfigurationWithParameterMethod(CodeClass codeClass)
    {
        var method = GetApiClientConfigurationMethod(codeClass);
        method.Name = $"{method.Name}-overload";   // needed as workaround, to be able to add overloads (AL doesn't support optional params)
                                                   // this is changed back in a later step        
        method.AddParameter(GetParameterP("config", new CodeType { Name = "codeunit \"Kiota ClientConfig SOHH\"", IsExternal = true }, "1"));
        method.AddCustomProperty("sorting-value", "28");
        return method;
    }
    public static CodeMethod GetApiClientDefaultConfigurationMethod(CodeClass codeClass)
    {
        var method = new CodeMethod
        {
            Name = "DefaultConfiguration",
            SimpleName = "DefaultConfiguration",
            Access = AccessModifier.Private,
            ReturnType = new CodeType { Name = "codeunit \"Kiota ClientConfig SOHH\"", IsExternal = true },
            Kind = CodeMethodKind.Custom
        };
        method.AddCustomProperty("sorting-value", "29");
        return method;
    }
    public static CodeMethod GetApiClientInitializerMethod(CodeClass codeClass)
    {
        var method = new CodeMethod
        {
            Name = "AAAInitialize",
            SimpleName = "Initialize",
            Access = AccessModifier.Public,
            ReturnType = new CodeType { Name = "void" },
            Kind = CodeMethodKind.Custom
        };
        method.AddParameter(GetParameterP("NewAPIAuthorization", new CodeType { Name = "codeunit \"Kiota API Authorization SOHH\"", IsExternal = true }, "1"));
        return method;
    }
    public static CodeMethod GetIndexerClassSetIdentifierMethod(CodeClass codeClass, CodeIndexer? indexer)
    {
        var method = new CodeMethod
        {
            Name = "AAASetIdentifier",
            SimpleName = "SetIdentifier",
            Access = AccessModifier.Public,
            ReturnType = new CodeType { Name = "void" },
            Kind = CodeMethodKind.Custom
        };
        if (indexer is not null)
            method.AddParameter(indexer.IndexParameter);
        return method;
    }
    public static CodeMethod GetSetConfigurationMethod(CodeClass codeClass)
    {
        var method = new CodeMethod
        {
            Name = "AAASetConfiguration",
            SimpleName = "SetConfiguration",
            Access = AccessModifier.Public,
            ReturnType = new CodeType { Name = "void" },
            Kind = CodeMethodKind.Custom
        };
        method.AddParameter(GetParameterP("NewReqConfig", new CodeType { Name = "codeunit \"Kiota ClientConfig SOHH\"", IsExternal = true }, "1"));
        return method;
    }
}

public class ALVariable
{
    public string Name
    {
        get; set;
    }
    public CodeTypeBase Type
    {
        get; set;
    }
    public string Value { get; set; } = string.Empty;
    public string Pragmas { get; set; } = string.Empty;
    public string DefaultValue { get; set; } = string.Empty;
    public bool Locked
    {
        get; set;
    }
    public ALVariable(string name, CodeTypeBase type)
    {
        Name = name;
        Type = type;
    }
    public ALVariable(string name, CodeTypeBase type, string defaultValue)
    {
        Name = name;
        Type = type;
        DefaultValue = defaultValue;
        if ((Type.Name == "Label") && string.IsNullOrEmpty(DefaultValue))
            throw new ArgumentNullException(nameof(defaultValue), "Labels must have a default value");
    }
    public ALVariable(string name, CodeTypeBase type, string defaultValue, string value)
    {
        Name = name;
        Type = type;
        DefaultValue = defaultValue;
        Value = value;
    }
    public ALVariable(string name, CodeTypeBase type, string defaultValue, string value, string pragmas)
    {
        Name = name;
        Type = type;
        DefaultValue = defaultValue;
        Value = value;
        Pragmas = pragmas;
    }

    public CodeProperty ToCodeProperty()
    {
        var prop = new CodeProperty
        {
            Name = Name,
            Type = Type,
            DefaultValue = DefaultValue
        };
        prop.AddCustomProperty("value", Value);
        prop.SetPragmas([Pragmas]);
        return prop;
    }
    public CodeParameter ToCodeParameter()
    {
        var param = new CodeParameter
        {
            Name = Name,
            Type = Type,
            DefaultValue = DefaultValue
        };
        param.AddCustomProperty("value", Value);
        param.SetPragmas([Pragmas]);
        return param;
    }

    public void Write(ALWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        var variableType = ALVariableProvider.ConventionService.GetTypeString(Type);
        if (variableType == "Label")
            Value = $" '{Value}'";
        writer.WritePragmaConditionalDisable(Pragmas, true);
        writer.WriteLine($"{Name}: {variableType}{Value};");
        writer.WritePragmaConditionalRestore(Pragmas, true);
    }
    public bool CanBeCombined(ALVariable aLVariable)
    {
        if (aLVariable is null)
            return false;
        if (aLVariable.Type.Name != Type.Name)
            return false;
        if (aLVariable.Type.CollectionKind != Type.CollectionKind)
            return false;
        if (ALVariableProvider.ConventionService.GetTypeString(Type) == "Label")
            if (aLVariable.Value != Value)
                return false;
        if (aLVariable.Pragmas != Pragmas)
            return false;
        return true;
    }
}

public class ALObjectProperty
{
    public string Name
    {
        get; set;
    }
    public string Value { get; set; } = string.Empty;
    public ALObjectProperty(string name)
    {
        Name = name;
    }
    public ALObjectProperty(string name, string value)
    {
        Name = name;
        Value = value;
    }
    public void Write(ALWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteLine($"{Name} = {Value};");
    }
}
