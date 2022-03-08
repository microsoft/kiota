using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Text.Json;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Microsoft.Kiota.Cli.Commons.IO;

/// <summary>
/// The table output formatter
/// </summary>
public class TableOutputFormatter : IOutputFormatter
{
    /// <inheritdoc />
    public void WriteOutput(string content, IOutputFormatterOptions options)
    {
        using var doc = JsonDocument.Parse(content);
        var table = ConstructTable(doc);
        AnsiConsole.Write(table);
    }

    /// <inheritdoc />
    public void WriteOutput(Stream content, IOutputFormatterOptions options)
    {
        using var doc = JsonDocument.Parse(content);
        var table = ConstructTable(doc);
        AnsiConsole.Write(table);
    }

    /// <summary>
    /// Constructs a table given a JSON document
    /// </summary>
    /// <param name="document">The parsed JSON document</param>
    public Table ConstructTable(JsonDocument document)
    {
        var root = document.RootElement;
        JsonElement firstElement;
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("value", out var value))
            root = value;

        if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
        {
            var enumerated = root.EnumerateArray();
            firstElement = enumerated.FirstOrDefault();
        }
        else
        {
            firstElement = root;
        }

        IEnumerable<string> propertyNames;
        var table = new Table();
        table.Expand();
        if (firstElement.ValueKind != JsonValueKind.Object)
        {
            propertyNames = new List<string> { "Value" };
        }
        else
        {
            var restrictedValueKinds = new JsonValueKind[] {
                    JsonValueKind.Array,
                    JsonValueKind.Object
                };
            propertyNames = firstElement.EnumerateObject()
                .Where(p => !restrictedValueKinds.Contains(p.Value.ValueKind))
                .Select(p => p.Name);
        }

        foreach (var propertyName in propertyNames)
            table.AddColumn(propertyName, column =>
            {
                if (firstElement.ValueKind == JsonValueKind.Object)
                {
                    JsonElement property;
                    var hasProp = firstElement.TryGetProperty(propertyName, out property);
                    if (property.ValueKind == JsonValueKind.Number)
                        column.RightAligned().PadLeft(10);
                }
            });

        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var row in root.EnumerateArray())
            {
                var rowCols = GetRowColumns(propertyNames, row);
                table.AddRow(rowCols);
            }
        }
        else if (root.ValueKind == JsonValueKind.Object)
        {
            var rowCols = GetRowColumns(propertyNames, root);
            table.AddRow(rowCols);
        }
        else
        {
            table.AddRow(GetPropertyValue(root));
        }

        return table;
    }

    private IEnumerable<IRenderable> GetRowColumns(IEnumerable<string> propertyNames, JsonElement row)
    {
        return propertyNames.Select(p =>
        {
            var propertyName = p;
            JsonElement property;
            if (row.ValueKind == JsonValueKind.Object)
            {
                var hasProp = row.TryGetProperty(propertyName, out property);
                if (hasProp)
                    return GetPropertyValue(property);
                else
                    return new Markup("-");
            }

            return GetPropertyValue(row);
        });
    }

    private IRenderable GetPropertyValue(JsonElement property)
    {
        var valueKind = property.ValueKind;
        object? value = null;
        switch (valueKind)
        {
            case JsonValueKind.String:
                value = property.GetString();
                break;
            case JsonValueKind.True:
            case JsonValueKind.False:
                value = property.GetBoolean();
                break;
            case JsonValueKind.Number:
                value = property.GetDecimal();
                break;
        }
        return new Markup(value?.ToString() ?? "-");
    }
}
