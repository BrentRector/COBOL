// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;

namespace CobolNet.Tests.Shared;

/// <summary>
/// THE C# reader of <c>kb/Work/</c> — the ONE work register (CLAUDE.md rule 8) — and the twin of
/// <c>scripts/spec/work.py</c>'s <c>parse_frontmatter</c>.
/// </summary>
/// <remarks>
/// <para>
/// ⛔ THIS EXISTS BECAUSE THE FORMAT HAD TWO READERS THAT DISAGREED, AND ONE OF THEM WAS A GATE.
/// <c>kb/Work/PB875</c> is the note: a note's <c>inventory_rows</c> list WRAPPED across two lines — the normal
/// YAML shape, which <c>kb/Work/PB205</c> had carried for months — was read by the Python side as a TRUNCATED
/// list and by the gate's own private parser (<c>value.StartsWith('[') &amp;&amp; value.EndsWith(']')</c>, else
/// <c>[]</c>) as NO LIST AT ALL. So a note claimed four inventory rows in the register while the gate that
/// enforces claims saw none, and <c>work.py check</c> called the same note well-formed. Measured on the live
/// register the day this reader replaced it: of 939 notes, exactly one parsed differently — PB205, from 4 rows
/// to its real 12.
/// </para>
/// <para>
/// ⛔ AND BOTH READERS FAILED <b>OPEN</b>: a malformed value produced emptiness, which reads exactly like a note
/// that claims nothing. Here a value the grammar cannot finish is an <see cref="Note.Errors"/> CODE and the key
/// is left ABSENT — never silently empty — and the codes are what
/// <c>tests/version-matrix/work-frontmatter-parity-cases.json</c> compares, because two readers that reject one
/// note for two different reasons look identical under "it was rejected" (the shape <c>kb/Work/PB315</c> hid
/// behind for months in the §1.1 engines).
/// </para>
/// <para>
/// ⚠ DELIBERATELY NOT A YAML PARSER. The register's frontmatter is a flat block of <c>key: scalar</c> and
/// <c>key: [a, b]</c> lines; pulling a YAML dependency in on this side alone would put the two readers on
/// different grammars again, which is the drift this file exists to end. What holds them together instead is the
/// parity fixture, evaluated here AND by <c>work.py parity --json</c> run for real
/// (<c>ClosesRowsBackLinkDriftTests.TheFrontmatterReader_AgreesWithTheFixtureAndWithPython</c>).
/// </para>
/// </remarks>
internal static class WorkRegister
{
    /// <summary>
    /// The statuses that mean an item is DONE — the vocabulary <c>work.py</c>'s <c>TERMINAL_STATUSES</c> owns.
    /// Every other status, including one added tomorrow, counts as LIVE: that default fails SAFE, because an
    /// unrecognized status then keeps a note holding its rows rather than silently dropping them out of coverage.
    /// </summary>
    public static readonly string[] TerminalStatuses = ["landed", "retired"];

    /// <summary>One <c>kb/Work/</c> note, reduced to the facts the register's gates read.</summary>
    /// <param name="File">The note's file name, which is how a failure message names it.</param>
    /// <param name="InventoryRows">What the note CLAIMS while it is open.</param>
    /// <param name="ClosesRows">What its landing CLOSED — the back-link that survives the landing (PB245).</param>
    /// <param name="ClosesRowsReason">Why a landing closed no row, when it closed none.</param>
    /// <param name="Errors">Codes for values this grammar could not finish. Never silently empty.</param>
    internal sealed record Note(
        string File,
        string Id,
        string Kind,
        string Status,
        string[] InventoryRows,
        string[] ClosesRows,
        string ClosesRowsReason,
        string[] Errors)
    {
        public bool IsLive => !TerminalStatuses.Contains(Status, StringComparer.Ordinal);

        public bool IsTerminal => !IsLive;
    }

    /// <summary>
    /// The frontmatter block: everything between the opening <c>---</c> line and the closing one. The register is
    /// LF (kb/Work PB3029); <c>\r?\n</c> because a reader that hard-codes LF would answer "this is not a note" for
    /// a note a Windows tool wrote CRLF — SILENTLY — where the line-endings audit names it.
    /// </summary>
    private static readonly Regex FrontMatter =
        new(@"\A---\r?\n(?<body>.*?)\r?\n---[ \t]*(?:\r?\n|\z)", RegexOptions.Singleline | RegexOptions.Compiled);

    /// <summary>
    /// A frontmatter key. Anything else on a line — a colon inside a title, a wrapped list's continuation — is
    /// not one. A VALUE is a list when it opens with <c>[</c>, whatever its key, so a field added to the
    /// register tomorrow needs no edit here and no second copy of a key vocabulary on the Python side.
    /// </summary>
    private static readonly Regex Key = new(@"\A[a-z_]+\z", RegexOptions.Compiled);

    /// <summary><c>"A", B , </c> → <c>[A, B]</c> — the ONE list-member grammar, quoted or bare.</summary>
    private static string[] SplitList(string inner) =>
        [.. inner.Split(',').Select(x => x.Trim().Trim('"').Trim('\'').Trim()).Where(x => x.Length > 0)];

    /// <summary>
    /// Parse one note's frontmatter, or <c>null</c> when the file is not a note.
    /// </summary>
    /// <remarks>
    /// ⛔ A RUNAWAY IS BOUNDED BY THE NEXT KEY. A list left open by a missing bracket would otherwise keep eating
    /// lines until some later line happened to end in <c>]</c> — so <c>tags: [cobolsharp, work, defect]</c>
    /// silently becomes three members of <c>inventory_rows</c> and the malformation is never reported. A line
    /// that begins a new key ends the open list with the error instead; no continuation of a real wrapped list
    /// can look like one, because its members are rule-ids, clause numbers and tag words.
    /// </remarks>
    public static Note? Parse(string file, string text)
    {
        var fm = FrontMatter.Match(text);
        if (!fm.Success) return null;

        var scalars = new Dictionary<string, string>(StringComparer.Ordinal);
        var lists = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var errors = new List<string>();

        string? open = null;            // the list key whose value is still being accumulated across lines
        string buf = "";
        foreach (string raw in fm.Groups["body"].Value.Split('\n'))
        {
            string line = raw.Trim();
            if (open is not null)
            {
                int colon = line.IndexOf(':');
                if (colon >= 0 && Key.IsMatch(line[..colon].Trim()))
                {
                    errors.Add($"unterminated-list:{open}");
                    (open, buf) = (null, "");
                }
                else
                {
                    buf += " " + line;
                    if (buf.EndsWith(']'))
                    {
                        lists[open] = SplitList(buf[(buf.IndexOf('[') + 1)..^1]);
                        (open, buf) = (null, "");
                    }
                    continue;
                }
            }

            int i = line.IndexOf(':');
            if (i < 0) continue;        // a continuation of a wrapped SCALAR, a comment, a blank line
            string k = line[..i].Trim(), v = line[(i + 1)..].Trim();
            if (!Key.IsMatch(k)) continue;
            if (!v.StartsWith('[')) scalars[k] = v.Trim('"');
            else if (v.EndsWith(']')) lists[k] = SplitList(v[1..^1]);
            else (open, buf) = (k, v);
        }
        if (open is not null) errors.Add($"unterminated-list:{open}");

        string Scalar(string k) => scalars.TryGetValue(k, out var v) ? v : "";
        string[] List(string k) => lists.TryGetValue(k, out var v) ? v : [];

        return new Note(file, Scalar("id"), Scalar("kind"), Scalar("status"),
                        List("inventory_rows"), List("closes_rows"), Scalar("closes_rows_reason"),
                        [.. errors]);
    }

    /// <summary>Every note in <c>kb/Work/</c>, read once.</summary>
    /// <remarks>
    /// The directory must EXIST: a gate that cannot find the register has to fail rather than pass vacuously
    /// over zero notes (<c>feedback_verdict_evidence_invariant</c>). The caller asserts the population it needs.
    /// </remarks>
    public static List<Note> Load()
    {
        string dir = TestRepo.Kb("Work");
        if (!Directory.Exists(dir))
        {
            throw new DirectoryNotFoundException(
                $"the work register is missing: {dir} — it is THE work register (CLAUDE.md rule 8), and a gate "
                + "that cannot find it must fail rather than pass vacuously.");
        }
        var notes = new List<Note>();
        foreach (string path in Directory.EnumerateFiles(dir, "*.md"))
        {
            if (Parse(Path.GetFileName(path), File.ReadAllText(path)) is { } n) notes.Add(n);
        }
        return notes;
    }
}
