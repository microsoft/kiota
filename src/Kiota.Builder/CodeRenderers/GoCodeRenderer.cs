using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.OrderComparers;
using Kiota.Builder.Writers;

namespace Kiota.Builder.CodeRenderers;

/// <summary>
/// A code renderer for Go that post-processes the generated source so that running
/// <c>go fmt</c> (gofmt) on the output is a no-op. The individual element writers cannot
/// reason about sibling declarations, so the whitespace rules that gofmt enforces across a
/// whole file (a single blank line between top-level declarations and column-aligned struct
/// fields) are applied here once the file has been fully rendered.
/// </summary>
public class GoCodeRenderer : CodeRenderer
{
    public GoCodeRenderer(GenerationConfiguration configuration, CodeElementOrderComparer? elementComparer = null) : base(configuration, elementComparer)
    {
    }

    public override async Task RenderCodeNamespaceToSingleFileAsync(LanguageWriter writer, CodeElement codeElement, string outputFile, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentException.ThrowIfNullOrEmpty(outputFile);

        using var stringWriter = new StringWriter();
        writer.SetTextWriter(stringWriter);
        RenderCode(writer, codeElement);

        var formatted = FormatGoSource(stringWriter.ToString());

#pragma warning disable CA2007
        await using var stream = new FileStream(outputFile, FileMode.Create);
#pragma warning restore CA2007
        await using var fileWriter = new StreamWriter(stream, new UTF8Encoding(false), 1024, leaveOpen: true);
        await fileWriter.WriteAsync(formatted.AsMemory(), cancellationToken).ConfigureAwait(false);
        if (!cancellationToken.IsCancellationRequested)
            await fileWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Applies the gofmt whitespace conventions to a rendered Go file.
    /// </summary>
    internal static string FormatGoSource(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        // gofmt strips trailing whitespace from every line.
        var lines = source.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal)
                            .Split('\n').Select(static l => l.TrimEnd()).ToList();
        AlignFieldBlocks(lines);
        var normalized = NormalizeBlankLines(lines);
        return string.Join("\n", normalized).TrimEnd('\n') + "\n";
    }

    private static bool IsBlank(string line) => string.IsNullOrWhiteSpace(line);
    private static bool IsTopLevel(string line) => line.Length > 0 && line[0] is not (' ' or '\t');
    private static bool IsTopLevelComment(string line) => IsTopLevel(line) && line.StartsWith("//", StringComparison.Ordinal);

    private static readonly string[] DeclarationKeywords = ["func ", "type ", "var ", "const ", "import "];
    private static bool IsDeclarationKeyword(string line) =>
        IsTopLevel(line) && DeclarationKeywords.Any(k => line.StartsWith(k, StringComparison.Ordinal));

    /// <summary>
    /// gofmt keeps exactly one blank line between top-level declarations, removes leading and
    /// trailing blank lines and collapses runs of blank lines. This replicates that for the
    /// shapes kiota emits without re-implementing the printer for declaration bodies.
    /// </summary>
    private static List<string> NormalizeBlankLines(List<string> lines)
    {
        // collapse runs of blank lines and drop leading blanks
        var collapsed = new List<string>(lines.Count);
        foreach (var line in lines)
        {
            if (IsBlank(line))
            {
                if (collapsed.Count == 0 || IsBlank(collapsed[^1]))
                    continue;
            }
            collapsed.Add(line);
        }

        var result = new List<string>(collapsed.Count + 8);
        for (var i = 0; i < collapsed.Count; i++)
        {
            var line = collapsed[i];
            if (StartsTopLevelDeclaration(collapsed, i) && result.Count > 0 && !IsBlank(result[^1]))
                result.Add(string.Empty);
            result.Add(line);
        }
        return result;
    }

    /// <summary>
    /// Whether the declaration that starts at <paramref name="index"/> requires a preceding blank
    /// line. A declaration is introduced either by a declaration keyword or by the first line of a
    /// doc-comment block that is directly attached to such a keyword. The package clause is excluded
    /// because the blank line that follows it is handled by the next declaration.
    /// </summary>
    private static bool StartsTopLevelDeclaration(List<string> lines, int index)
    {
        var line = lines[index];
        if (IsDeclarationKeyword(line))
            // a keyword preceded by its own doc-comment block is handled when that block starts
            return index == 0 || !IsTopLevelComment(lines[index - 1]);
        if (IsTopLevelComment(line))
        {
            // only act on the first comment line of the block
            if (index > 0 && IsTopLevelComment(lines[index - 1])) return false;
            // walk the contiguous comment block and check the declaration it documents
            var j = index;
            while (j < lines.Count && IsTopLevelComment(lines[j])) j++;
            return j < lines.Count && IsDeclarationKeyword(lines[j]);
        }
        return false;
    }

    /// <summary>
    /// gofmt aligns consecutive field declarations inside <c>struct { }</c> and <c>const ( )</c>
    /// blocks into columns. Groups are broken by blank lines and comments, which matches how
    /// tabwriter sees the source. Import blocks are intentionally excluded because gofmt does not
    /// column-align them.
    /// </summary>
    private static void AlignFieldBlocks(List<string> lines)
    {
        for (var i = 0; i < lines.Count; i++)
        {
            char closer;
            if (IsTopLevel(lines[i]) && lines[i].StartsWith("type ", StringComparison.Ordinal) && lines[i].EndsWith("struct {", StringComparison.Ordinal))
                closer = '}';
            else if (IsTopLevel(lines[i]) && lines[i].StartsWith("const (", StringComparison.Ordinal))
                closer = ')';
            else
                continue;

            var groupStart = -1;
            var j = i + 1;
            for (; j < lines.Count && !(IsTopLevel(lines[j]) && lines[j].StartsWith(closer)); j++)
            {
                var isField = lines[j].StartsWith('\t') && !lines[j].TrimStart().StartsWith("//", StringComparison.Ordinal) && !IsBlank(lines[j]);
                if (isField)
                {
                    if (groupStart < 0) groupStart = j;
                }
                else if (groupStart >= 0)
                {
                    AlignFieldGroup(lines, groupStart, j - 1);
                    groupStart = -1;
                }
            }
            if (groupStart >= 0)
                AlignFieldGroup(lines, groupStart, j - 1);
            i = j;
        }
    }

    private static void AlignFieldGroup(List<string> lines, int start, int end)
    {
        if (end <= start) return; // a single field never needs alignment
        var rows = new List<string[]>(end - start + 1);
        for (var i = start; i <= end; i++)
            rows.Add(SplitFieldCells(lines[i].TrimStart('\t')));

        // a cell is aligned (padded) only when it is not the last cell of its row, mirroring tabwriter
        var columnWidths = new Dictionary<int, int>();
        foreach (var cells in rows)
            for (var c = 0; c < cells.Length - 1; c++)
                columnWidths[c] = Math.Max(columnWidths.GetValueOrDefault(c), cells[c].Length);

        for (var i = 0; i < rows.Count; i++)
        {
            var cells = rows[i];
            var builder = new StringBuilder("\t");
            for (var c = 0; c < cells.Length; c++)
            {
                builder.Append(cells[c]);
                if (c < cells.Length - 1)
                    builder.Append(' ', columnWidths[c] - cells[c].Length + 1);
            }
            lines[start + i] = builder.ToString();
        }
    }

    /// <summary>
    /// Splits a struct field declaration into its name, type and optional tag columns. Everything
    /// after the type is treated as a single trailing cell so struct tags are never split.
    /// </summary>
    private static string[] SplitFieldCells(string field)
    {
        var firstSpace = field.IndexOf(' ', StringComparison.Ordinal);
        if (firstSpace < 0) return [field];
        var name = field[..firstSpace];
        var rest = field[(firstSpace + 1)..].TrimStart();
        var secondSpace = rest.IndexOf(' ', StringComparison.Ordinal);
        if (secondSpace < 0) return [name, rest];
        return [name, rest[..secondSpace], rest[(secondSpace + 1)..].TrimStart()];
    }
}
