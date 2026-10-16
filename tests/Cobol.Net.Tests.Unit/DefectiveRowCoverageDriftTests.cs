// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Tests.Shared;
using Xunit;

// The ONE C# reader of kb/Work/ lives in tests/_shared/WorkRegister.cs. This gate used to carry its own private
// parser, and kb/Work/PB875 is what that cost: the private parser answered a list WRAPPED across two lines with
// NO list at all, so kb/Work/PB390 claimed four inventory rows in the register that this gate could not see,
// while `work.py check` called the same note well-formed. The reader's Python twin is held to the same parity
// fixture (tests/version-matrix/work-frontmatter-parity-cases.json), and the frontmatter shapes this class used
// to assert inline are cases in that fixture.
using InventoryRow = CobolNet.Tests.Shared.TraceabilityInventory.Row;
using WorkNote = CobolNet.Tests.Shared.WorkRegister.Note;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE INVARIANT THAT KEEPS THE BURN-DOWN AND THE WORK REGISTER THE SAME LIST: every P14 inventory row
/// carrying a DEFECTIVE verdict is claimed by a LIVE note in <c>kb/Work/</c>.
/// </summary>
/// <remarks>
/// <para>
/// The two artifacts answer the same question from opposite ends. <c>tests/version-matrix/traceability-inventory.json</c>
/// says which spec rules this compiler does not yet implement correctly; <c>kb/Work/</c> — the ONE work register
/// (CLAUDE.md rule 8) — says what is left to do. Nothing held them together, and the drift was measured on
/// 2026-08-31: of 138 rows verdicted PARTIAL / NOT-IMPLEMENTED / DIVERGES, <b>131 were invisible to
/// <c>work.py next</c></b>. A defect the register cannot see cannot be ranked, cannot be scheduled, and reads to
/// every later session as work nobody owes — while the number the owner watches counted it.
/// </para>
/// <para>
/// The link is a note's <c>inventory_rows</c> frontmatter list, and it is deliberately the NOTE→ROW direction.
/// The reverse (scraping a note id out of a row's prose <c>notes</c> field) is how the 131 came to be measured,
/// and it is the wrong direction twice over: the row's notes are forensic narrative, so a note id can appear in
/// one for reasons that are not ownership; and a note that lands keeps naming its rows, so a landed note whose
/// rows were never re-verdicted turns this gate RED — which is the event most worth failing on, and the one a
/// prose scrape cannot see.
/// </para>
/// <para>
/// ⚠ NOTHING HERE CARRIES ITS OWN COPY OF THE VOCABULARY. "Defective" is DERIVED from
/// <c>tests/version-matrix/inventory-schema.json</c> — a verdict whose <c>resolves</c> flag is false — so adding a
/// verdict to the schema extends this gate with no edit here, which is the one-rule-one-place discipline the
/// schema itself is built on (<c>feedback_one_rule_one_place</c>). The status vocabulary is the mirror image:
/// only <c>landed</c> and <c>retired</c> are TERMINAL, and every other status — including one added to
/// <c>work.py</c> tomorrow — counts as live. That default is chosen to fail SAFE: an unrecognized status keeps
/// a note holding its rows rather than silently dropping them out of coverage.
/// </para>
/// <para>
/// ⛔ <see cref="TheseChecks_ActuallyFail_OnAFabricatedRegister"/> is not a formality
/// (<c>feedback_green_gates_arent_evidence</c>): this gate will spend most of its life green, and a green gate
/// that never looked at anything is indistinguishable from one that works. It drives the same pure functions
/// with a register built to break them, and — the half that is easy to omit — with a register built to PASS
/// them, so the failures prove discrimination rather than blanket rejection.
/// </para>
/// </remarks>
public sealed class DefectiveRowCoverageDriftTests
{
    // ── the artifacts under test ─────────────────────────────────────────────────────────────────────
    //
    // The inventory and the verdict vocabulary come from TraceabilityInventory (tests/_shared), so "defective"
    // is DERIVED from the schema's `resolves` flag in one place and this gate, ClosesRowsBackLinkDriftTests and
    // anything added next all read the same answer.

    /// <summary>The verdicts the schema marks as NOT resolving — the ones that leave a rule outstanding.</summary>
    private static HashSet<string> LoadDefectiveVerdicts() => TraceabilityInventory.DefectiveVerdicts();

    private static List<InventoryRow> LoadInventory() => TraceabilityInventory.Rows();

    /// <summary>
    /// The register, through the ONE reader (<see cref="WorkRegister"/>). A file with no frontmatter is not a
    /// note; a note with no <c>id</c> is a malformed one and is kept, because dropping it here would hide it
    /// from <c>work.py check</c>'s complement — this gate reads what the register SAYS, and says so.
    /// </summary>
    private static List<WorkNote> LoadRegister() => WorkRegister.Load();

    // ── the checks, as pure functions so the self-test can drive the SAME code ───────────────────────

    /// <summary>Defective rows that no LIVE note claims — the set this gate holds at empty.</summary>
    private static List<string> UnclaimedDefectiveRows(
        IEnumerable<InventoryRow> rows, IEnumerable<WorkNote> register, HashSet<string> defective)
    {
        var claimed = register.Where(n => n.IsLive)
                              .SelectMany(n => n.InventoryRows)
                              .ToHashSet(StringComparer.Ordinal);
        return [.. from r in rows
                   where defective.Contains(r.Verdict) && !claimed.Contains(r.RuleId)
                   orderby r.RuleId
                   select $"{r.RuleId}: verdict {r.Verdict}, and no open kb/Work note names it in "
                          + "`inventory_rows` — register it (a note), or record the verdict it has earned"];
    }

    /// <summary>
    /// A claim on a rule-id the inventory does not have. A typo here is worse than useless: it satisfies nothing
    /// while LOOKING like ownership, so the row it meant to claim stays uncovered and the note reads as done.
    /// </summary>
    private static List<string> ClaimsNamingNoRow(IEnumerable<InventoryRow> rows, IEnumerable<WorkNote> register)
    {
        var known = rows.Select(r => r.RuleId).ToHashSet(StringComparer.Ordinal);
        return [.. from n in register
                   from claim in n.InventoryRows
                   where !known.Contains(claim)
                   orderby n.Id, claim
                   select $"{n.File}: inventory_rows names '{claim}', which is not a rule-id in the inventory"];
    }

    private static string Report(string what, List<string> bad, int scale) =>
        $"{bad.Count} {what} (of {scale} examined):\n  " + string.Join("\n  ", bad.Take(25))
        + (bad.Count > 25 ? $"\n  … and {bad.Count - 25} more" : "");

    // ── the gate ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// ⛔ THE INVARIANT. Every defective-verdict row is claimed by a live work note.
    /// </summary>
    /// <remarks>
    /// Two ways to go green, and both are correct outcomes: register the row on a note (it is real work), or
    /// record the verdict the rule has actually earned (CONFORMS with its spec-derived witness, or
    /// DOCUMENTED-NON-SUPPORT against a recorded owner decision / <c>docs/CONFORMANCE.md</c> §4 licence). What
    /// is no longer available is the third option that produced the 131 — leaving a row defective and writing
    /// the reason in a paragraph.
    /// </remarks>
    [Fact]
    public void EveryDefectiveRow_IsClaimedByALiveWorkNote()
    {
        var rows = LoadInventory();
        var register = LoadRegister();
        var defective = LoadDefectiveVerdicts();

        // ⛔ POPULATION FIRST. A MISSING observation is not a NEGATIVE one (feedback_verdict_evidence_invariant):
        // an inventory that failed to parse, or a register directory that came back empty, would satisfy the
        // assertion below while measuring nothing at all.
        Assert.True(rows.Count > 1000, $"only {rows.Count} inventory rows parsed — the inventory did not load");
        Assert.True(register.Count > 100, $"only {register.Count} work notes parsed — the register did not load");
        Assert.True(register.Any(n => n.InventoryRows.Length > 0),
            "no work note carries an `inventory_rows` claim — the frontmatter key this gate reads is gone, and "
            + "the gate would pass by measuring nothing.");
        Assert.Contains(rows, r => defective.Contains(r.Verdict));

        var bad = UnclaimedDefectiveRows(rows, register, defective);
        Assert.True(bad.Count == 0, Report("defective row(s) with no live work note", bad, rows.Count));
    }

    /// <summary>Every <c>inventory_rows</c> claim names a rule-id the inventory really has.</summary>
    [Fact]
    public void EveryInventoryRowsClaim_NamesARealRule()
    {
        var rows = LoadInventory();
        var register = LoadRegister();
        var bad = ClaimsNamingNoRow(rows, register);
        Assert.True(bad.Count == 0, Report("claim(s) naming no inventory rule", bad, register.Count));
    }

    /// <summary>
    /// ⛔ THE EVIDENCE THAT THIS GATE INSPECTS ANYTHING — a register built to break each check, and one built
    /// to pass it.
    /// </summary>
    [Fact]
    public void TheseChecks_ActuallyFail_OnAFabricatedRegister()
    {
        var defective = new HashSet<string>(StringComparer.Ordinal) { "PARTIAL", "NOT-IMPLEMENTED", "DIVERGES" };
        // This gate reads the VERDICT (is the row defective?); the row's computed OK/GAP state belongs
        // to ClosesRowsBackLinkDriftTests, so the fabricated rows here carry it only to be well-formed.
        InventoryRow Row(string id, string verdict) => new(id, verdict, "GAP");
        WorkNote Note(string id, string status, params string[] rows) =>
            new($"{id}.md", id, "defect", status, rows, [], "", []);

        var inventory = new[]
        {
            Row("GR-1.1-1", "PARTIAL"), Row("GR-1.1-2", "CONFORMS"), Row("GR-1.1-3", "DIVERGES"),
        };

        // A defective row nothing claims.
        Assert.Equal(2, UnclaimedDefectiveRows(inventory, [], defective).Count);

        // Claimed by a note that has LANDED — the exact event worth failing on: the fix shipped and the row was
        // never re-verdicted, so the inventory still publishes a defect the compiler no longer has.
        Assert.Single(UnclaimedDefectiveRows(
            inventory, [Note("PB1", "landed", "GR-1.1-1"), Note("PB2", "open", "GR-1.1-3")], defective));
        Assert.Single(UnclaimedDefectiveRows(
            inventory, [Note("PB1", "retired", "GR-1.1-1"), Note("PB2", "open", "GR-1.1-3")], defective));

        // Claimed by live notes of every non-terminal status — all of these are real work, and all count.
        foreach (string live in new[] { "open", "half", "owner", "blocked" })
        {
            Assert.Empty(UnclaimedDefectiveRows(
                inventory, [Note("PB1", live, "GR-1.1-1"), Note("PB2", "open", "GR-1.1-3")], defective));
        }

        // A NON-defective row needs no claim, and a claim on one is not an error either — a note may legitimately
        // own a row it has already fixed, or one it is about to break.
        Assert.Empty(UnclaimedDefectiveRows(
            [Row("GR-1.1-2", "CONFORMS")], [Note("PB1", "open", "GR-1.1-2")], defective));

        // A typo'd claim satisfies nothing while looking like ownership: BOTH checks must notice.
        Assert.Single(ClaimsNamingNoRow(inventory, [Note("PB1", "open", "GR-1.1-99")]));
        Assert.Single(UnclaimedDefectiveRows(inventory,
            [Note("PB1", "open", "GR-1.1-99"), Note("PB2", "open", "GR-1.1-3")], defective));
        Assert.Empty(ClaimsNamingNoRow(inventory, [Note("PB1", "open", "GR-1.1-1", "GR-1.1-2")]));

        // ⚠ THE FRONTMATTER READER IS NO LONGER TESTED HERE, and that is the point rather than an omission: the
        // shapes this block used to assert (LF and CRLF, quoted and bare members, no claim at all, a file that is
        // not a note) are now cases in tests/version-matrix/work-frontmatter-parity-cases.json, where the PYTHON
        // reader is held to the same answers. A private copy of those assertions beside a private copy of the
        // parser is exactly how the two readers drifted apart (kb/Work/PB875). The same holds for IsLive: the
        // fixture's `terminal` field states it for every status of work.py's STATUSES, in both engines (PB3196).
    }
}
