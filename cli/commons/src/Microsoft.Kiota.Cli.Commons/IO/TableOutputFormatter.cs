using System.Text.Json;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Microsoft.Kiota.Cli.Commons.IO;

/// <summary>
/// The table output formatter
/// </summary>
public class TableOutputFormatter : IOutputFormatter
{
    private readonly IAnsiConsole _ansiConsole;

    /// <summary>
    /// Creates a new table output formatter with a default console
    /// </summary>
    public TableOutputFormatter() : this(AnsiConsole.Console)
    {
    }

    /// <summary>
    /// Creates a new table output formatter with the provided console
    /// </summary>
    /// <param name="console">The console to use</param>
    public TableOutputFormatter(IAnsiConsole console)
    {
        _ansiConsole = console;
    }

    /// <inheritdoc />
    public void WriteOutput(string content, IOutputFormatterOptions options)
    {
        using var doc = JsonDocument.Parse(content);
        var table = ConstructTable(doc);
        _ansiConsole.Write(table);
    }

    /// <inheritdoc />
    public async Task WriteOutputAsync(Stream content, IOutputFormatterOptions options, CancellationToken cancellationToken = default) {
        using var doc = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);
        var table = ConstructTable(doc, cancellationToken);
        _ansiConsole.Write(table);
    }

    /// <summary>
    /// Construct a table given a JSON document
    /// </summary>
    /// <param name="document">The parsed json document</param>
    /// <param name="cancellationToken">The cancellation token</param>
    public Table ConstructTable(JsonDocument document, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        var root = GetRootElement(document.RootElement);
        var firstElement = GetFirstElement(root);

        IEnumerable<string> propertyNames = GetPropertyNames(firstElement, cancellationToken);
        var table = new Table();
        table.Expand();

        foreach (var propertyName in propertyNames)
        {
            cancellationToken.ThrowIfCancellationRequested();
            table.AddColumn(propertyName, column =>
            {
                if (firstElement.ValueKind == JsonValueKind.Object)
                {
                    var hasProp = firstElement.TryGetProperty(propertyName, out var property);
                    if (property.ValueKind == JsonValueKind.Number)
                        column.RightAligned().PadLeft(10);
                }
            });
        }

        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var row in root.EnumerateArray())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var rowCols = GetRowColumns(propertyNames, row);
                table.AddRow(rowCols);
            }
        }
        else if (root.ValueKind == JsonValueKind.Object)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var rowCols = GetRowColumns(propertyNames, root);
            table.AddRow(rowCols);
        }
        else
        {
            cancellationToken.ThrowIfCancellationRequested();
            table.AddRow(GetPropertyValue(root));
        }

        return table;
    }

    private static JsonElement GetRootElement(JsonElement input) {
        var root = input;
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("value", out var value))
            root = value;
        
        return root;
    }

    private static JsonElement GetFirstElement(JsonElement root) {
        var firstElement = root;
        if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
        {
            var enumerated = root.EnumerateArray();
            firstElement = enumerated.FirstOrDefault();
        }
        
        return firstElement;
    }

    private static IEnumerable<string> GetPropertyNames(JsonElement firstElement, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        IEnumerable<string> propertyNames;
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
            var objectEnumerator = firstElement.EnumerateObject();
            var buffer = new List<string>();
            foreach (var property in objectEnumerator)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (restrictedValueKinds.Contains(property.Value.ValueKind)) {
                    continue;
                }

                buffer.Add(property.Name);
            }
            propertyNames = buffer;
        }
        
        return propertyNames;
    }

    private static IEnumerable<IRenderable> GetRowColumns(IEnumerable<string> propertyNames, JsonElement row)
    {
        return propertyNames.Select(p =>
        {
            var propertyName = p;
            if (row.ValueKind == JsonValueKind.Object)
            {
                var hasProp = row.TryGetProperty(propertyName, out var property);
                if (hasProp)
                    return GetPropertyValue(property);
                else
                    return new Markup("-");
            }

            return GetPropertyValue(row);
        });
    }

    private static IRenderable GetPropertyValue(JsonElement property)
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
