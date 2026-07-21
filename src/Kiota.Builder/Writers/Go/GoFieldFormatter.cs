using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kiota.Builder.Writers.Go;

/// <summary>
/// Column-aligns the body of a Go <c>struct { }</c> or <c>const ( )</c> block the way gofmt does.
/// <para>
/// gofmt runs consecutive field declarations through <c>text/tabwriter</c>, padding each column to
/// the widest cell of the run so the names, types and struct tags line up. A per-element writer
/// only ever sees its own field, so it cannot know how wide its siblings are; the writer that owns
/// the whole block (<see cref="CodeClassDeclarationWriter"/> for structs,
/// <see cref="CodeEnumWriter"/> for const blocks) buffers the body lines and aligns them here in
/// one pass before writing them out. Without this, <c>gofmt -l</c> would flag every generated file
/// containing a struct with two or more undocumented fields.
/// </para>
/// <example>
/// A run of comment-less fields (typical for request query parameter structs) is padded into
/// columns:
/// <code>
/// // before                                     // after
/// type QueryParameters struct {                 type QueryParameters struct {
///     Active *bool "uri..." ------------->          Active  *bool   "uri..."
///     Keyword *string "uri..."                      Keyword *string "uri..."
/// }                                             }
/// </code>
/// The last cell of a row is never padded (tabwriter treats trailing cells as unterminated), so a
/// field without a struct tag keeps its type unpadded:
/// <code>
/// EndDateTime   *string
/// StartDateTime *string "uriparametername:\"startDateTime\""
/// </code>
/// A field documented by its own <c>//</c> comment (typical for model structs) forms a group of
/// one, and a group of one never needs padding, so documented fields are left untouched:
/// <code>
/// // The id property
/// id *string
/// // The displayName property
/// displayName *string
/// </code>
/// Flags-enum const blocks are runs of comment-less <c>NAME = value</c> rows, so they get aligned:
/// <code>
/// ROCK_MOVETYPE     = 1
/// PAPER_MOVETYPE    = 2
/// SCISSORS_MOVETYPE = 4
/// </code>
/// whereas iota const blocks come out unchanged: only the first row (<c>NAME Type = iota</c>) has
/// more than one cell, and with no sibling rows sharing its columns there is nothing to pad
/// against — the remaining bare <c>NAME</c> rows are single trailing cells.
/// </example>
/// </summary>
internal static class GoFieldFormatter
{
    /// <summary>
    /// Aligns the lines of one block body. Comment lines and blank lines break the body into
    /// groups (that is how tabwriter sees the source: a non-field line flushes the current run),
    /// each group of two or more field lines is padded into columns, and everything else is
    /// returned verbatim. Lines keep their own leading indentation.
    /// </summary>
    internal static IReadOnlyList<string> AlignFieldBlock(IReadOnlyList<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        var result = lines.ToList();
        var groupStart = -1;
        for (var i = 0; i < result.Count; i++)
        {
            var content = result[i].TrimStart(' ', '\t');
            var isField = content.Length > 0 && !content.StartsWith("//", StringComparison.Ordinal);
            if (isField)
            {
                if (groupStart < 0) groupStart = i;
            }
            else if (groupStart >= 0)
            {
                AlignFieldGroup(result, groupStart, i - 1);
                groupStart = -1;
            }
        }
        if (groupStart >= 0)
            AlignFieldGroup(result, groupStart, result.Count - 1);
        return result;
    }

    private static void AlignFieldGroup(List<string> lines, int start, int end)
    {
        if (end <= start) return; // a single field never needs alignment
        var indents = new string[end - start + 1];
        var rows = new List<string[]>(end - start + 1);
        for (var i = start; i <= end; i++)
        {
            var content = lines[i].TrimStart(' ', '\t');
            indents[i - start] = lines[i][..^content.Length];
            rows.Add(SplitFieldCells(content));
        }

        // tabwriter only pads a cell when another cell follows it on the same row; the width of a
        // column is the widest such cell across the group
        var columnWidths = new Dictionary<int, int>();
        foreach (var cells in rows)
            for (var c = 0; c < cells.Length - 1; c++)
                columnWidths[c] = Math.Max(columnWidths.GetValueOrDefault(c), cells[c].Length);

        for (var i = 0; i < rows.Count; i++)
        {
            var cells = rows[i];
            var builder = new StringBuilder(indents[i]);
            for (var c = 0; c < cells.Length; c++)
            {
                builder.Append(cells[c]);
                if (c < cells.Length - 1)
                    // pad to the column width plus the single space gofmt keeps between columns
                    builder.Append(' ', columnWidths[c] - cells[c].Length + 1);
            }
            lines[start + i] = builder.ToString();
        }
    }

    /// <summary>
    /// Splits a field declaration into its name, type and optional trailing columns. Everything
    /// after the type is kept as a single trailing cell so struct tags — which contain spaces —
    /// are never split apart.
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
