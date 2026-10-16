// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using CobolNet.Tests.Shared;
using Xunit;

using InventoryRow = CobolNet.Tests.Shared.TraceabilityInventory.Row;
using WorkNote = CobolNet.Tests.Shared.WorkRegister.Note;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE BACK-LINK FROM A LANDED FIX TO THE INVENTORY ROWS IT CLOSED: every <c>kind: defect</c> note that has
/// LANDED names those rows in <c>closes_rows</c>, or says why it closed none — and every row it names is one
/// the inventory itself calls closed today.
/// </summary>
/// <remarks>
/// <para>
/// ⛔ WHY (owner decision 2026-09-19, <c>kb/Work/PB245</c>). The landing loop had no step that recorded which
/// inventory rows a fix CLOSED. <c>inventory_rows</c> is the claim a note makes while it is OPEN and it empties
/// as the rows are re-verdicted, so the moment a note landed, the link was gone — and "is this row still real?"
/// could only be answered by measuring the compiler again. It was measured once, on one clause family: of
/// fourteen §15 rows holding the GAP open, <b>thirteen closed CONFORMS on first re-measurement</b>, on
/// mechanisms seven landings had already shipped. The GAP denominator — the number the owner watches, and the
/// one <c>work.py next</c> ranks the campaign on — counted closed work as open for months. Two more data points
/// followed (<c>RV-15.78.4-1</c>, <c>kb/Work/PB190</c>; <c>kb/Work/PB257</c>'s evidence file), and the owner's
/// answer was to make the back-link DATA rather than a habit.
/// </para>
/// <para>
/// ⭐ WHAT THIS GATE ADDS THAT <c>DefectiveRowCoverageDriftTests</c> CANNOT. That gate reads the CURRENT
/// defective set and asks who owns it, so a row that was wrongly left defective is visible to it only as work
/// somebody still owes. This one reads the opposite direction: a landing SAYS what it closed, and the saying is
/// checked against the inventory. A landed note naming a row that is still non-OK is a landing that did not
/// re-verdict what it claims to have fixed — the exact event PB245 measured, and one no amount of prose could
/// make falsifiable.
/// </para>
/// <para>
/// ⚠ THE EMPTY ANSWER IS ALLOWED AND MUST BE SPOKEN. Most defects never had an inventory row; a landing that
/// closed none writes <c>closes_rows: []</c> AND a <c>closes_rows_reason:</c>. The reason is required to be a
/// sentence rather than a token — this gate can stop SILENCE, and it does not pretend to grade prose.
/// </para>
/// <para>
/// ⛔ <see cref="TheseChecks_ActuallyFail_OnAFabricatedRegister"/> exists because this gate will spend most of
/// its life green, and a green gate that never looked at anything is indistinguishable from one that works
/// (<c>feedback_green_gates_arent_evidence</c>). It drives the same pure predicates with a register built to
/// break each one and a register built to PASS it, so the failures prove discrimination.
/// </para>
/// </remarks>
public sealed class ClosesRowsBackLinkDriftTests
{
    /// <summary>
    /// The shortest thing this gate will accept as a stated reason. A reason is a SENTENCE — "no inventory row
    /// tracks this mechanism; the defect is in the harness" — not a token, and every spelling of the token is
    /// shorter than this. ⚠ It is a floor on effort, not a judge of content: a determined writer can pad. What
    /// it forecloses is the silence the field replaced, which is what actually happened.
    /// </summary>
    private const int MinimumReasonLength = 20;

    /// <summary>The spellings that say nothing, for the cases that squeeze past the floor.</summary>
    private static readonly string[] VacuousReasons =
        ["n/a", "na", "none", "nothing", "tbd", "todo", "unknown", "-", "no rows", "not applicable"];

    private static bool IsVacuous(string reason)
    {
        string r = reason.Trim().Trim('"').Trim().ToLowerInvariant();
        return r.Length < MinimumReasonLength || VacuousReasons.Contains(r, StringComparer.Ordinal);
    }

    /// <summary>A note is subject to the back-link obligation when it is a DEFECT that has reached a terminal
    /// status. Analyses, adjudications and owner decisions record their outcome in prose and close no row by
    /// landing; a defect is the kind whose landing changes what the inventory says.</summary>
    private static bool Obliged(WorkNote n) => n.Kind == "defect" && n.IsTerminal;

    // ── the checks, as pure functions so the self-test can drive the SAME code ───────────────────────

    /// <summary>Landed defect notes that say NOTHING about what they closed — the silence PB245 is about.</summary>
    private static List<string> LandedNotesThatSayNothing(IEnumerable<WorkNote> register) =>
        [.. from n in register
            where Obliged(n) && n.ClosesRows.Length == 0 && IsVacuous(n.ClosesRowsReason)
            orderby n.Id
            select $"{n.File}: status {n.Status} and no `closes_rows` — name the inventory rows this landing "
                   + "re-verdicted, or write `closes_rows: []` with a `closes_rows_reason:` saying why it closed "
                   + "none. A landing that records neither is how fourteen §15 rows held the GAP open on "
                   + "mechanisms seven landings had already closed (kb/Work/PB245)"];

    /// <summary>
    /// Rows a note claims to have CLOSED that the inventory does not agree are closed — either because no such
    /// rule-id exists, or because the row's computed <c>state</c> is still GAP.
    /// </summary>
    /// <remarks>
    /// ⚠ THE PREDICATE IS THE ROW'S <c>state</c>, NOT ITS VERDICT, and the two are not the same question.
    /// <c>state</c> is what the schema computes from the verdict AND the evidence that verdict requires,
    /// and it is what the burn-down counts — so a CONFORMS row whose covering test is missing is still a
    /// GAP row, and a landing that claimed it would be claiming work the number the owner watches has not
    /// been told about. Measured the day this gate landed: the two answers coincided on all 4,348 rows, so
    /// choosing the stronger one costs nothing today and cannot silently weaken later.
    /// </remarks>
    private static List<string> ClosedRowsTheInventoryDenies(
        IEnumerable<WorkNote> register, IEnumerable<InventoryRow> rows)
    {
        var known = rows.ToDictionary(r => r.RuleId, r => r, StringComparer.Ordinal);
        var bad = new List<string>();
        foreach (var n in register.OrderBy(n => n.Id, StringComparer.Ordinal))
        {
            foreach (string claim in n.ClosesRows)
            {
                if (!known.TryGetValue(claim, out var row))
                {
                    bad.Add($"{n.File}: closes_rows names '{claim}', which is not a rule-id in the inventory — "
                            + "a claim that satisfies nothing while looking like a closed row");
                }
                else if (!row.IsClosed)
                {
                    bad.Add($"{n.File}: closes_rows names '{claim}', whose verdict is "
                            + $"'{(row.Verdict.Length == 0 ? "(none)" : row.Verdict)}' and whose state is "
                            + $"'{(row.State.Length == 0 ? "(none)" : row.State)}' — that row still counts "
                            + "toward the GAP, so either the landing owes it a `record_verdicts` batch or "
                            + "the claim is wrong");
                }
            }
        }
        return bad;
    }

    private static string Report(string what, List<string> bad, int scale) =>
        $"{bad.Count} {what} (of {scale} examined):\n  " + string.Join("\n  ", bad.Take(25))
        + (bad.Count > 25 ? $"\n  … and {bad.Count - 25} more" : "");

    // ── the gate ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>⛔ THE INVARIANT, first half: a landed defect note states what it closed.</summary>
    [Fact]
    public void EveryLandedDefectNote_StatesWhatItClosed()
    {
        var register = WorkRegister.Load();

        // ⛔ POPULATION FIRST. A MISSING observation is not a NEGATIVE one: a register that failed to parse, or
        // one in which the field had been deleted wholesale, would satisfy the assertion below while measuring
        // nothing (feedback_verdict_evidence_invariant).
        Assert.True(register.Count > 100, $"only {register.Count} work notes parsed — the register did not load");
        Assert.True(register.Count(Obliged) > 50,
            $"only {register.Count(Obliged)} landed defect note(s) — the register did not load, or the status "
            + "vocabulary changed underneath this gate");
        Assert.True(register.Any(n => n.ClosesRows.Length > 0),
            "no work note carries a `closes_rows` entry — the frontmatter key this gate reads is gone, and the "
            + "gate would pass by measuring nothing. Re-derive it with scripts/spec/backfill_closes_rows.py.");
        // ⛔ A MALFORMED VALUE IS NAMED, not merely counted: the reader reports a value it could not
        // finish instead of reading it as an empty claim (kb/Work/PB875), and a gate that said only
        // "Assert.Empty() Failure" would hand the reader back the silence the error code replaced.
        var malformed = register.Where(n => n.Errors.Length > 0)
                                .Select(n => $"{n.File}: {string.Join(", ", n.Errors)}").ToList();
        Assert.True(malformed.Count == 0,
            Report("note(s) whose frontmatter this reader could not finish", malformed, register.Count));

        var bad = LandedNotesThatSayNothing(register);
        Assert.True(bad.Count == 0, Report("landed defect note(s) with no back-link", bad, register.Count));
    }

    /// <summary>⛔ THE INVARIANT, second half: every row a landing claims to have closed really is closed.</summary>
    [Fact]
    public void EveryClosesRowsEntry_NamesARowTheInventoryCallsClosed()
    {
        var register = WorkRegister.Load();
        var rows = TraceabilityInventory.Rows();

        Assert.True(rows.Count > 1000, $"only {rows.Count} inventory rows parsed — the inventory did not load");
        Assert.True(register.Sum(n => n.ClosesRows.Length) > 100,
            "the register claims almost no closed rows — this gate would then be measuring nothing");

        Assert.Contains(rows, r => r.IsClosed);      // the OK/GAP field itself has to be populated

        var bad = ClosedRowsTheInventoryDenies(register, rows);
        Assert.True(bad.Count == 0, Report("closed-row claim(s) the inventory denies", bad, rows.Count));
    }

    /// <summary>
    /// ⛔ THE EVIDENCE THAT THIS GATE INSPECTS ANYTHING — a register built to break each check, and one built to
    /// pass it.
    /// </summary>
    [Fact]
    public void TheseChecks_ActuallyFail_OnAFabricatedRegister()
    {
        InventoryRow Row(string id, string verdict, string state) => new(id, verdict, state);
        WorkNote Note(string id, string kind, string status, string[] closes, string reason = "") =>
            new($"{id}.md", id, kind, status, [], closes, reason, []);

        var inventory = new[]
        {
            Row("GR-1.1-1", "CONFORMS", "OK"), Row("GR-1.1-2", "PARTIAL", "GAP"),
            Row("GR-1.1-3", "", "GAP"), Row("GR-1.1-4", "DOCUMENTED-NON-SUPPORT", "OK"),
            // ⛔ A RESOLVING VERDICT IS NOT A CLOSED ROW. The schema computes `state` from the verdict AND
            // its evidence, so a CONFORMS row whose covering test is missing still counts toward the GAP —
            // and the GAP counting closed work as open is the entire harm kb/Work/PB245 measured.
            Row("GR-1.1-5", "CONFORMS", "GAP"),
        };

        // ── half one: a landed defect that says nothing ──────────────────────────────────────────────
        Assert.Single(LandedNotesThatSayNothing([Note("PB1", "defect", "landed", [])]));
        Assert.Single(LandedNotesThatSayNothing([Note("PB1", "defect", "retired", [])]));
        // …and the ways to be correct: name a row, or say why there is none.
        Assert.Empty(LandedNotesThatSayNothing([Note("PB1", "defect", "landed", ["GR-1.1-1"])]));
        Assert.Empty(LandedNotesThatSayNothing(
            [Note("PB1", "defect", "landed", [], "no inventory row tracks this harness defect at all")]));
        // A token is not a reason, however it is spelled or padded to look like one.
        Assert.Single(LandedNotesThatSayNothing([Note("PB1", "defect", "landed", [], "n/a")]));
        Assert.Single(LandedNotesThatSayNothing([Note("PB1", "defect", "landed", [], "   none   ")]));
        Assert.Single(LandedNotesThatSayNothing([Note("PB1", "defect", "landed", [], "not applicable")]));
        // Still OPEN, or not a defect: the obligation belongs to a landing, and only a defect's landing.
        foreach (string live in new[] { "open", "half", "owner", "blocked" })
            Assert.Empty(LandedNotesThatSayNothing([Note("PB1", "defect", live, [])]));
        foreach (string kind in new[] { "analysis", "adjudication", "decision" })
            Assert.Empty(LandedNotesThatSayNothing([Note("PB1", kind, "landed", [])]));

        // ── half two: a claim the inventory denies ───────────────────────────────────────────────────
        // The event this gate exists for: the note landed and says it closed a row that is STILL defective.
        Assert.Single(ClosedRowsTheInventoryDenies(
            [Note("PB1", "defect", "landed", ["GR-1.1-2"])], inventory));
        // An UNADJUDICATED row (empty verdict) is not closed either — "nobody looked" is not "it conforms".
        Assert.Single(ClosedRowsTheInventoryDenies(
            [Note("PB1", "defect", "landed", ["GR-1.1-3"])], inventory));
        // A typo'd rule-id satisfies nothing while looking like a closed row.
        Assert.Single(ClosedRowsTheInventoryDenies(
            [Note("PB1", "defect", "landed", ["GR-1.1-99"])], inventory));
        // Both resolving verdicts are accepted — the vocabulary is the schema's, not this gate's.
        Assert.Empty(ClosedRowsTheInventoryDenies(
            [Note("PB1", "defect", "landed", ["GR-1.1-1", "GR-1.1-4"])], inventory));
        // A claim on a note that has NOT landed is checked too: a row named as closed is a factual claim
        // whatever the note's status, and a wrong one is worth catching before the landing inherits it.
        Assert.Single(ClosedRowsTheInventoryDenies(
            [Note("PB1", "defect", "open", ["GR-1.1-2"])], inventory));
        Assert.Equal(3, ClosedRowsTheInventoryDenies(
            [Note("PB1", "defect", "landed", ["GR-1.1-2", "GR-1.1-3"]),
             Note("PB2", "defect", "landed", ["GR-1.1-1", "GR-1.1-99"])], inventory).Count);
        // A CONFORMS verdict whose row is still GAP is REFUSED as a closed row.
        Assert.Single(ClosedRowsTheInventoryDenies(
            [Note("PB1", "defect", "landed", ["GR-1.1-5"])], inventory));
    }

    /// <summary>
    /// ⛔ THE CROSS-LANGUAGE PARITY GATE FOR THE REGISTER'S FRONTMATTER. This reader, the Python reader and the
    /// recorded expectation must give the SAME answer for every case in
    /// <c>tests/version-matrix/work-frontmatter-parity-cases.json</c>.
    /// </summary>
    /// <remarks>
    /// <c>kb/Work/PB875</c> is the note recording what happens when they do not: a list WRAPPED across two lines
    /// was truncated by one reader and DISCARDED by the other, so a note claimed rows in the register that the
    /// gate enforcing claims could not see — and the permissive reader was the only one anybody ran by hand. A
    /// fixture both sides are compared against is only half the check; running the other engine FOR REAL is the
    /// half that catches an engine whose behaviour and whose fixture were edited together.
    /// </remarks>
    [Fact]
    public void TheFrontmatterReader_AgreesWithTheFixtureAndWithPython()
    {
        string path = TestRepo.VersionMatrix("work-frontmatter-parity-cases.json");
        Assert.True(File.Exists(path), $"the frontmatter parity fixture is missing: {path}");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var cases = doc.RootElement.GetProperty("cases").EnumerateArray().ToList();
        Assert.True(cases.Count >= 8, $"the fixture carries {cases.Count} case(s) — too few to measure a reader");

        var bad = new List<string>();
        var mine = new List<Dictionary<string, object>>();
        foreach (var c in cases)
        {
            string name = c.GetProperty("name").GetString()!;
            var n = WorkRegister.Parse("fixture.md", c.GetProperty("text").GetString()!);
            var got = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["note"] = n is not null,
                ["id"] = n?.Id ?? "",
                ["kind"] = n?.Kind ?? "",
                ["status"] = n?.Status ?? "",
                ["terminal"] = n?.IsTerminal ?? false,   // the DONE vocabulary's answer (kb/Work PB3196)
                ["inventory_rows"] = n?.InventoryRows ?? [],
                ["closes_rows"] = n?.ClosesRows ?? [],
                ["closes_rows_reason"] = n?.ClosesRowsReason ?? "",
                ["errors"] = (string[])[.. (n?.Errors ?? []).Order(StringComparer.Ordinal)],
            };
            mine.Add(got);
            foreach (var want in c.GetProperty("expect").EnumerateObject())
            {
                string shown = Show(got[want.Name]);
                string expected = Show(FromJson(want.Value));
                if (shown != expected) bad.Add($"'{name}': {want.Name} is {shown}, the fixture says {expected}");
            }
        }

        // …and the OTHER engine, RUN rather than assumed.
        var run = PythonInstrument.Run(TestRepo.Scripts("spec", "work.py"), "parity", "--json");
        string? line = run.Stdout.Split('\n').Select(l => l.Trim())
            .FirstOrDefault(l => l.StartsWith("JSON ", StringComparison.Ordinal));
        Assert.True(line is not null,
            $"work.py parity --json emitted no JSON line.\n{run.Stdout}\n{run.Stderr}");
        using var py = JsonDocument.Parse(line!["JSON ".Length..]);
        var theirs = py.RootElement.GetProperty("parity").EnumerateArray().ToList();
        Assert.Equal(cases.Count, theirs.Count);
        for (int i = 0; i < cases.Count; i++)
        {
            foreach (string key in mine[i].Keys)
            {
                string shown = Show(mine[i][key]);
                string expected = Show(FromJson(theirs[i].GetProperty(key)));
                if (shown != expected)
                {
                    bad.Add($"'{cases[i].GetProperty("name").GetString()}': {key} — this engine {shown}, "
                            + $"Python {expected}");
                }
            }
        }
        Assert.Equal(0, run.ExitCode);   // work.py parity carries its own population + falsification checks

        Assert.True(bad.Count == 0, Report("frontmatter parity disagreement(s)", bad, cases.Count));
    }

    /// <summary>A fixture value as this test compares it — arrays and scalars in one spelling.</summary>
    private static object FromJson(JsonElement e) => e.ValueKind switch
    {
        JsonValueKind.Array => (string[])[.. e.EnumerateArray().Select(x => x.GetString()!)],
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => e.GetString() ?? "",
    };

    private static string Show(object v) =>
        v is string[] a ? "[" + string.Join(", ", a) + "]" : v is bool b ? (b ? "true" : "false") : $"'{v}'";
}
