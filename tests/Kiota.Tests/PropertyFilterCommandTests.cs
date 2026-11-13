using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using kiota;
using kiota.Extension;
using kiota.Handlers.Client;
using Kiota.Builder.Configuration;
using Kiota.Builder.WorkspaceManagement;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kiota.Tests;

public sealed class PropertyFilterCommandTests
{
    private static IServiceProvider EmptyServiceProvider => new ServiceCollection().BuildServiceProvider();

    private static Command GetCommand(string commandName) => commandName switch
    {
        "add" => KiotaClientCommands.GetAddCommand(EmptyServiceProvider),
        "edit" => KiotaClientCommands.GetEditCommand(EmptyServiceProvider),
        _ => KiotaHost.GetRootCommand(EmptyServiceProvider).Subcommands.Single(static x => x.Name == "generate"),
    };

    private static List<string> RequiredArguments(string commandName)
    {
        var arguments = new List<string>();
        if (commandName != "generate")
            arguments.AddRange(["--client-name", "Client"]);
        if (commandName != "edit")
            arguments.AddRange(["--language", "CSharp", "--openapi", "https://example.com/openapi.yaml"]);
        return arguments;
    }

    [Theory]
    [InlineData("generate")]
    [InlineData("add")]
    [InlineData("edit")]
    public void AcceptsRepeatedLiteralPropertySelectors(string commandName)
    {
        var command = GetCommand(commandName);
        var arguments = new List<string>
        {
            "--include-property", "User#name",
            "--include-property", "User#Name",
            "--exclude-property", "User#literal*?#field",
        };
        if (commandName != "generate")
            arguments.AddRange(["--client-name", "Client"]);
        if (commandName != "edit")
            arguments.AddRange(["--language", "CSharp", "--openapi", "https://example.com/openapi.yaml"]);
        var result = command.Parse([.. arguments]);
        Assert.Empty(result.Errors);
        var includeOption = Assert.IsType<Option<List<string>>>(command.Options.Single(static x => x.Name == "--include-property"));
        var excludeOption = Assert.IsType<Option<List<string>>>(command.Options.Single(static x => x.Name == "--exclude-property"));
        Assert.Equal(["User#name", "User#Name"], result.GetValue(includeOption));
        Assert.Equal(["User#literal*?#field"], result.GetValue(excludeOption));
        Assert.Equal(commandName == "edit", command.Options.Any(static x => x.Name == "--clear-property-filters"));
    }

    [Theory]
    [InlineData("generate")]
    [InlineData("add")]
    [InlineData("edit")]
    public void ExpandsRepeatedGroupsAndUnionsLiteralSelectors(string commandName)
    {
        var command = GetCommand(commandName);
        var arguments = RequiredArguments(commandName);
        arguments.AddRange([
            "--include-property", "'User#displayName, mail,displayName'",
            "--include-property", "User#Mail,custom#name",
            "--include-property", "Entity#id",
            "--include-property", "User#first,last",
            "--include-property", "User#mail",
        ]);
        var result = command.Parse([.. arguments]);
        Assert.Empty(result.Errors);
        var option = Assert.IsType<Option<List<string>>>(command.Options.Single(static x => x.Name == "--include-property"));
        var includes = result.GetIncludedProperties(option);
        Assert.Equal(7, includes.Count);
        Assert.True(includes.SetEquals(["User#displayName", "User#mail", "User#Mail", "User#custom#name", "Entity#id", "User#first", "User#last"]));
        Assert.DoesNotContain("user#mail", includes);
        Assert.DoesNotContain(command.Options, static x => x.Name == "--include-properties");
    }

    [Theory]
    [InlineData("User")]
    [InlineData("#name,mail")]
    [InlineData("User#")]
    [InlineData("User#name,")]
    [InlineData("User#,name")]
    [InlineData("User#name,,mail")]
    [InlineData("User#name,  ")]
    public void RejectsMalformedGroupsAtCommandParsing(string group)
    {
        var command = GetCommand("generate");
        var arguments = RequiredArguments("generate");
        arguments.AddRange(["--include-property", group]);
        var result = command.Parse([.. arguments]);
        Assert.Contains(result.Errors, static x => x.Message.Contains("Invalid property selector", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("generate")]
    [InlineData("add")]
    [InlineData("edit")]
    public void RejectsMissingGroupedOptionValues(string commandName)
    {
        var command = GetCommand(commandName);
        var arguments = RequiredArguments(commandName);
        arguments.Add("--include-property");
        Assert.NotEmpty(command.Parse([.. arguments]).Errors);
    }

    [Theory]
    [InlineData("generate")]
    [InlineData("add")]
    [InlineData("edit")]
    public void RequiresRepeatedOptionsForSeparateSelectorArguments(string commandName)
    {
        var command = GetCommand(commandName);
        var arguments = RequiredArguments(commandName);
        arguments.AddRange(["--include-property", "User#mail", "Entity#id"]);
        Assert.Contains(command.Parse([.. arguments]).Errors,
            static x => x.Message.Contains("Entity#id", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EditingWithGroupsReplacesAndPersistsExpandedSelectors(bool clear)
    {
        var configuration = new GenerationConfiguration
        {
            IncludeProperties = ["User#old"],
            ExcludeProperties = ["User#secret"],
        };
        var command = GetCommand("edit");
        var arguments = RequiredArguments("edit");
        arguments.AddRange(["--include-property", "User#name,mail", "--include-property", "Entity#id"]);
        if (clear)
            arguments.Add("--clear-property-filters");
        var result = command.Parse([.. arguments]);
        Assert.Empty(result.Errors);
        Assert.IsType<EditHandler>(command.Action).ApplyPropertyFilters(configuration, result);
        Assert.True(configuration.IncludeProperties.SetEquals(["User#name", "User#mail", "Entity#id"]));
        Assert.True(configuration.ExcludeProperties.SetEquals(clear ? [] : ["User#secret"]));

        var persisted = new ApiClientConfiguration(configuration);
        var restored = new GenerationConfiguration();
        persisted.UpdateGenerationConfigurationFromApiClientConfiguration(restored, "Client");
        Assert.True(restored.IncludeProperties.SetEquals(configuration.IncludeProperties));
        Assert.DoesNotContain("User#name,mail", restored.IncludeProperties);
    }

    [Theory]
    [InlineData("generate")]
    [InlineData("add")]
    [InlineData("edit")]
    public void PreservesStarPatternsUntilTheSchemaIsAvailable(string commandName)
    {
        var command = GetCommand(commandName);
        var arguments = RequiredArguments(commandName);
        arguments.AddRange(["--include-property", "microsoft.graph.*#display*,mail", "--exclude-property", "microsoft.graph.user#preferred*"]);
        var result = command.Parse([.. arguments]);
        Assert.Empty(result.Errors);
        var option = Assert.IsType<Option<List<string>>>(command.Options.Single(static x => x.Name == "--include-property"));
        var includes = result.GetIncludedProperties(option);
        Assert.True(includes.SetEquals(["microsoft.graph.*#display*", "microsoft.graph.*#mail"]));
        var persisted = new ApiClientConfiguration(new GenerationConfiguration { IncludeProperties = includes });
        var restored = new GenerationConfiguration();
        persisted.UpdateGenerationConfigurationFromApiClientConfiguration(restored, "Client");
        Assert.True(restored.IncludeProperties.SetEquals(includes));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void EditingPreservesOrClearsSavedRulesBeforeApplyingReplacements(bool clear, bool replace)
    {
        var client = new ApiClientConfiguration();
        client.IncludeProperties.UnionWith(["User#name", "User#Name"]);
        client.ExcludeProperties.Add("User#mail");
        var configuration = new GenerationConfiguration();
        client.UpdateGenerationConfigurationFromApiClientConfiguration(configuration, "Client");
        var command = KiotaClientCommands.GetEditCommand(EmptyServiceProvider);
        var arguments = new List<string> { "--client-name", "Client" };
        if (clear)
            arguments.Add("--clear-property-filters");
        if (replace)
            arguments.AddRange(["--include-property", "'User#id'", "--include-property", "User#Id", "--include-property", "User#id"]);
        var result = command.Parse([.. arguments]);
        Assert.Empty(result.Errors);
        var handler = Assert.IsType<EditHandler>(command.Action);
        handler.ApplyPropertyFilters(configuration, result);
        Assert.True(configuration.IncludeProperties.SetEquals(replace ? ["User#id", "User#Id"] : clear ? [] : ["User#name", "User#Name"]));
        Assert.True(configuration.ExcludeProperties.SetEquals(clear ? [] : ["User#mail"]));
        var persisted = new ApiClientConfiguration(configuration);
        Assert.True(persisted.IncludeProperties.SetEquals(configuration.IncludeProperties));
        Assert.True(persisted.ExcludeProperties.SetEquals(configuration.ExcludeProperties));
        Assert.True(client.IncludeProperties.SetEquals(["User#name", "User#Name"]));
        Assert.Single(client.ExcludeProperties);
    }

    [Fact]
    public void EditingCanReplaceExclusionsWithoutChangingInclusions()
    {
        var configuration = new GenerationConfiguration();
        configuration.IncludeProperties.Add("User#name");
        configuration.ExcludeProperties.Add("User#mail");
        var command = KiotaClientCommands.GetEditCommand(EmptyServiceProvider);
        var result = command.Parse(["--client-name", "Client", "--exclude-property", "\"User#literal*?\""]);
        Assert.Empty(result.Errors);
        Assert.IsType<EditHandler>(command.Action).ApplyPropertyFilters(configuration, result);
        Assert.True(configuration.IncludeProperties.SetEquals(["User#name"]));
        Assert.True(configuration.ExcludeProperties.SetEquals(["User#literal*?"]));
    }
}
