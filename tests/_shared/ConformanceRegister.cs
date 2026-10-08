// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;

namespace CobolNet.Tests.Shared;

/// <summary>
/// <c>docs/CONFORMANCE.md</c> §7 — the Annex A.1 implementor-defined element register — parsed.
/// </summary>
/// <remarks>
/// <para>
/// ⛔ ONE PARSER PER LANGUAGE, and this is the C# one. Its twin is <c>section7_rows</c> /
/// <c>register_determinations</c> in <c>scripts/spec/inventory_schema.py</c>. Two engines read the same ARTIFACT,
/// which is the same shape as the <c>derived-verdicts</c> evaluator: what exists twice is a ten-line reader, not
/// the register. It lives here rather than in either test class because two of them need it —
/// <c>SpecTraceabilityInventoryDriftTests.AnchorObliged</c> asks whether an item has a determination at all, and
/// <c>DerivedVerdictDriftTests</c>'s <c>determination-prefix</c> arm reads what the determination SAYS — and
/// three sites keying on one markdown table independently is <c>feedback_one_rule_one_place</c> verbatim.
/// </para>
/// <para>
/// ⚠ THE SPLIT IS ON UNESCAPED PIPES, and that is not fastidiousness: the item-82 determination writes an
/// absolute value as <c>\|v\|</c>, so a naive <c>Split('|')</c> reports that row with two extra cells and
/// mis-places everything after them.
/// </para>
/// </remarks>
internal static class ConformanceRegister
{
    private const string Heading = "## 7. Annex A.1";

    /// <summary>One data row of the §7 table, by cell.</summary>
    /// <param name="Key">the inventory rule-id the row discharges — <c>DOC-A.1-&lt;n&gt;</c></param>
    /// <param name="Element">the A.1 element the row claims to be about</param>
    /// <param name="Determination">WHAT WE DO — the determination itself</param>
    /// <param name="Pinned">the spec-derived test-ref(s) that pin it, or an em dash</param>
    internal sealed record Row(string Key, string Element, string Determination, string Pinned);

    private static readonly Regex UnescapedPipe = new(@"(?<!\\)\|", RegexOptions.Compiled);

    /// <summary>
    /// The register file's text, read ONCE per test assembly. <c>docs/CONFORMANCE.md</c> is ~1 MB and every
    /// <c>LoadSchema()</c> now wants a second section out of it; re-reading it per call put the whole file back
    /// on a path that already runs a dozen times a gate.
    /// </summary>
    /// <remarks>⚠ Declared BEFORE <see cref="Parsed"/>: static field initializers run in declaration order.</remarks>
    private static readonly Lazy<string> Body = new(() => File.ReadAllText(Path));

    private static readonly Lazy<List<Row>> Parsed = new(() => Parse(Body.Value));

    /// <summary>The register file itself, so a caller can assert it exists before believing an empty parse.</summary>
    internal static string Path => TestRepo.Docs("CONFORMANCE.md");

    /// <summary>Every data row of §7, in document order — parsed once per test assembly.</summary>
    internal static IReadOnlyList<Row> Rows => Parsed.Value;

    /// <summary>
    /// <c>rule-id → determination cell</c>. An item with SEVERAL determinations (A.1-56 and A.1-92 legitimately
    /// have two rows each) keeps the FIRST, which is the one its obligation is discharged by — matching
    /// <c>register_determinations</c> on the Python side.
    /// </summary>
    internal static IReadOnlyDictionary<string, string> Determinations { get; } = Build();

    /// <summary>A determination cell with its leading markdown emphasis removed — a determination is written for
    /// a human first (<c>**Not provided.** Each usage holds …</c>), so a predicate reading the cell must not be
    /// reading the bold markers.</summary>
    internal static string Plain(string cell) => cell.TrimStart('*', '_', ' ', '\t');

    private static Dictionary<string, string> Build()
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var r in Rows) map.TryAdd(r.Key, r.Determination);
        return map;
    }

    internal static List<Row> Parse(string text) =>
        [.. Section(text, Heading, "A.1 item")
            .Select(c => new Row(c[0], c[1], Cell(c, 2), Cell(c, 3)))];

    /// <summary>
    /// Every data row of ONE register table in <c>docs/CONFORMANCE.md</c>, as raw cell lists.
    /// Mirrors <c>inventory_schema.register_section</c> in Python.
    /// </summary>
    /// <remarks>
    /// ⛔ IT STOPS AT THE NEXT <c>## </c> HEADING, and that is not tidiness. Until §8 existed §7 was the LAST
    /// section, so reading to end-of-file was indistinguishable from reading the section — and the moment a
    /// second register was appended, every §8 row would have been served to <see cref="Determinations"/> as an
    /// A.1 determination keyed by something that is not an A.1 item. Bounding it is what makes the NEXT section
    /// free, and it is the same edit made to the Python parser in the same change set.
    /// </remarks>
    internal static List<string[]> Section(string text, string heading, string headerCell)
    {
        var rows = new List<string[]>();
        int start = text.IndexOf(heading, StringComparison.Ordinal);
        if (start < 0) return rows;
        string body = text[(start + heading.Length)..];
        int next = body.IndexOf("\n## ", StringComparison.Ordinal);
        if (next >= 0) body = body[..next];
        foreach (string line in body.Split('\n'))
        {
            if (!line.StartsWith("| ", StringComparison.Ordinal)) continue;
            string[] cells = Cells(line);
            if (cells.Length < 2 || cells[0] == headerCell || cells[0].All(c => c is '-' or ':' or ' ')) continue;
            rows.Add(cells);
        }

        return rows;
    }

    private static string Cell(string[] cells, int i) => cells.Length > i ? cells[i] : "";

    /// <summary>One data row of the §8 derivation register, by cell — the twin of Python's
    /// <c>DerivationRow</c>.</summary>
    /// <param name="Key">the derivation anchor fragment — <c>DRV-&lt;rule-id&gt;</c></param>
    /// <param name="Arm">which of the three §1.1 grounds this determination stands on</param>
    /// <param name="Names">the arm's OBJECT — an A.2 item, the closed set, or the indistinguishable rule-id</param>
    /// <param name="Argument">the derivation itself, written for a reader</param>
    /// <param name="Signature">the owner's signature, one of the keys of the schema's <c>derivation.signatures</c></param>
    internal sealed record DerivationRow(string Key, string Arm, string Names, string Argument, string Signature);

    /// <summary>
    /// <c>§8 row key → row</c> for the register of owner-signed DERIVATIONS (<c>kb/Work/PB386</c>). Read with
    /// the heading the SCHEMA names, so the section can be renamed in one place.
    /// </summary>
    internal static Dictionary<string, DerivationRow> Derivations(
        string heading, string headerCell, string? text = null)
    {
        var map = new Dictionary<string, DerivationRow>(StringComparer.Ordinal);
        foreach (string[] c in Section(text ?? Body.Value, heading, headerCell))
            map.TryAdd(c[0], new DerivationRow(c[0], c[1], Cell(c, 2), Cell(c, 3), Cell(c, 4)));
        return map;
    }

    private static string[] Cells(string line)
    {
        string[] parts = UnescapedPipe.Split(line.TrimEnd('\r', '\n'));
        return parts.Length < 3 ? [] : [.. parts[1..^1].Select(c => c.Replace("\\|", "|").Trim())];
    }
}
