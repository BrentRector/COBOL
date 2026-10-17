// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// A DERIVED verdict is one that follows mechanically from a single owner determination plus the rule's own
/// SCOPE — and this is what stops the derived population drifting back into a spread of hand-adjudicated states.
///
/// <para><b>The case that motivated it.</b> ARITHMETIC IS STANDARD-BINARY is declined (kb/Work PB198). Sixteen
/// catalog rules are conditioned SOLELY on that mode, so every one of them is unreachable and there is nothing
/// left to adjudicate row by row — yet before this landed they carried <b>four different verdicts</b>:
/// NOT-IMPLEMENTED (9), blank (2), NEEDS-OWNER-DECISION (4) and CONFORMS (1). Four answers to one question.</para>
///
/// <para><b>Why the predicate is data and not code.</b> The selectors live in <c>inventory-schema.json</c> under
/// <c>derived-verdicts</c>, and BOTH the batch generator (Python — <c>scripts/spec/inventory_schema.py</c>'s
/// <c>DerivedSelector</c>, driven by <c>derive_verdict_batch.py</c>) and this test read them. Writing the same
/// predicate twice in two languages is exactly the drift kb/Work PB194 records, where a mode SET was spelled one
/// way in two files and the other way in a third. ⚠ That claim was ASPIRATIONAL until 2026-09-02: no Python
/// consumer of <c>derived-verdicts</c> existed, so this test was the only reader and the PB198 batch was made by
/// hand. Three of the six A.4 module refuters found that independently.</para>
///
/// <para><b>The selectors are subtle, so the tests assert they are still SHARP</b> — a predicate that quietly
/// widened to select everything, or narrowed to select nothing, would keep this file green while meaning
/// nothing. Every sentinel below corresponds to a draft that was measurably wrong.</para>
/// </summary>
public sealed class DerivedVerdictDriftTests
{
    private sealed record Rule(string Id, string Section, string Kind, string Text, string Requirement, int Sublist);

    /// <summary>⛔ The transcription spells a COBOL operand name's hyphen two ways. 29 catalog rules carry
    /// U+2011 NON-BREAKING HYPHEN where the rest carry ASCII '-' — SR-13.18.14.3-12 reads "Identifier‑1 shall be
    /// described in the file, working‑storage, …" — so an arm written <c>\bidentifier-1\b</c> matches NOTHING
    /// there. It cost two rows on the first measured run of the A.4 landing, and under-selection is the worse
    /// direction: the drift test stays green and the rows stay unstamped. Folded in the ONE place each engine
    /// reads the text; the Python side does the same in <c>DerivedSelector.text_of</c>. U+2013 EN DASH is left
    /// alone — the standard uses it as the MINUS glyph AND as a clause-range separator ("13.16–13.18").</summary>
    private static string Normalize(string text) => text.Replace('‐', '-').Replace('‑', '-');

    /// <summary>⛔ A NOTE is not part of the rule, so no predicate sees one — the standard's Introduction
    /// (unnumbered, verbal-forms paragraph): "Information marked as 'NOTE' is intended to assist the understanding
    /// or use of the document." It kept §11.9.5.2 GR2 out of <c>standard-binary-only</c> until kb/Work R43: its
    /// NOTE 1 encourages STANDARD-DECIMAL support and tripped the mixed-rule exclusion (PB1517). The Python twin
    /// is <c>inventory_schema.NOTE_MARK</c>; measured when it landed, the cut changed exactly that one row.</summary>
    private static readonly Regex NoteMark = new(@"\bNOTE(?:\s+\d+)?\s+(?=[A-Z(])", RegexOptions.Compiled);

    private static string Normative(string text)
    {
        var note = NoteMark.Match(text);
        return note.Success ? text[..note.Index] : text;
    }

    /// <summary>The sentence introducing the printed list each rule is an item of — the <c>lead-in</c> arm field
    /// (kb/Work R43 / PB1519; Python twin <c>DerivedSelector.lead_ins</c>). A list is its kind-section-sublist
    /// triple and its lead-in is the rule immediately BEFORE its first item, so an inner list interrupting an
    /// outer one leaves the outer list's lead-in alone; a plain rule (sublist 1) has none.</summary>
    private static Dictionary<string, string> LeadIns(List<Rule> rules)
    {
        var first = new Dictionary<(string, string, int), string>();
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        string previous = "";
        foreach (var r in rules)
        {
            if (r.Sublist > 1)
            {
                var key = (r.Kind, r.Section, r.Sublist);
                if (!first.TryGetValue(key, out var lead)) first[key] = lead = previous;
                map[r.Id] = lead;
            }
            else map[r.Id] = "";
            previous = r.Text;
        }
        return map;
    }

    /// <summary>⛔ A clause number is a dotted PATH, not a string, and this is the one rule that must never be a
    /// raw <c>StartsWith</c>. Both engines used one until 2026-09-02, under which §13.18.30 falls inside
    /// §13.18.3 and §13.18.40 (PICTURE) inside §13.18.4 (BACKGROUND-COLOR). Measured on the screen selector,
    /// whose clause arm names five such prefixes: raw selects 543 rules where component-wise selects 156 — 387
    /// extra, 33 of them already adjudicated (32 CONFORMS + 1 DOCUMENTED-NON-SUPPORT), i.e. a silent flip of
    /// thirty-two verified rows to non-support. A trailing dot in the data is accepted and ignored, so the
    /// hand-written "13.18.3." convention that used to be the only defence cannot become load-bearing again.
    /// </summary>
    internal static bool SectionMatches(string prefix, string section)
    {
        string[] p = prefix.Trim().Trim('.').Split('.', StringSplitOptions.RemoveEmptyEntries);
        string[] s = section.Trim().Trim('.').Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (p.Length > s.Length) return false;
        for (int i = 0; i < p.Length; i++)
            if (!string.Equals(p[i], s[i], StringComparison.Ordinal)) return false;
        return true;
    }

    /// <summary>A parenthesised or semicolon-introduced clause citation inside a rule's own text — how an Annex
    /// A.1 documentation obligation names the clause that creates it: "(13.18.24, FORMAT clause, General rule
    /// 10)".</summary>
    private static readonly Regex XrefCitation = new(@"[(;]\s*(\d+(?:\.\d+)*)\s*,", RegexOptions.Compiled);

    private static JsonElement Schema() =>
        JsonDocument.Parse(File.ReadAllText(TestRepo.VersionMatrix("inventory-schema.json"))).RootElement;

    private static List<Rule> Catalog()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(
            TestRepo.At("docs", "rearchitecture", "spec-rule-catalog.json")));
        return [.. doc.RootElement.GetProperty("rules").EnumerateArray()
            .Select(r => new Rule(r.GetProperty("id").GetString()!,
                                  r.TryGetProperty("section", out var s) ? s.GetString() ?? "" : "",
                                  r.TryGetProperty("kind", out var k) ? k.GetString() ?? "" : "",
                                  Normative(Normalize(r.TryGetProperty("text", out var t) ? t.GetString() ?? "" : "")),
                                  // The A.1 REQUIREMENT CLASS the standard states in the item's own sentence
                                  // ("This item is optional."), parsed out of Annex A.1 by
                                  // extract_rule_catalog.py. Only DOC rules carry it.
                                  r.TryGetProperty("requirement", out var q) ? q.GetString() ?? "" : "",
                                  r.TryGetProperty("sublist", out var l) && l.ValueKind == JsonValueKind.Number
                                      ? l.GetInt32() : 1))];
    }

    private static Dictionary<string, JsonElement> Inventory()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(TestRepo.VersionMatrix("traceability-inventory.json")));
        var map = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var row in doc.RootElement.EnumerateArray())
            map[row.GetProperty("rule-id").GetString()!] = row.Clone();
        return map;
    }

    private static List<string> Strings(JsonElement arm, string field) =>
        arm.TryGetProperty(field, out var a) ? [.. a.EnumerateArray().Select(x => x.GetString()!)] : [];

    /// <summary>Apply one <c>derived-verdicts</c> selector to the catalog — the same predicate
    /// <c>DerivedSelector.select</c> applies in Python.
    ///
    /// <para><b><c>arms</c> is a DISJUNCTION of CONJUNCTIONS</b>: within one arm every field that is present must
    /// hold; a rule is selected when ANY arm holds and NO entry-level <c>excludes-patterns</c> matches its text.
    /// Four modules of the 2026-09-02 A.4 landing needed four different scoping mechanisms, and the flat
    /// requires-sections/requires-pattern pair could express only two of them:</para>
    /// <list type="bullet">
    /// <item><c>sections</c> alone — §8.8.1.4 is TITLED for the declined mode and its rules never repeat the
    /// phrase (GR-8.8.1.4.2-1 is just "Any operand of an arithmetic expression that is not already in SBIDI is
    /// converted into SBIDI form"), so a text-only predicate missed all six.</item>
    /// <item><c>pattern</c> alone — a rule stating the facility's own functionality from inside a MANDATORY
    /// clause, e.g. GR-14.6.11-1's implicit COMMIT over all active APPLY COMMIT clauses.</item>
    /// <item>both together, an AND-gate — <c>file-name-1</c> is STATEMENT-LOCAL (in §13.4.5.4 it is the file
    /// description entry's own subject), so A.4.13's text ungated selects 176 rules instead of 12. The same gate
    /// is the only reach to a rule scoped by OPERAND NAME: §12.3.7.3's data-name-1/-2 ARE the CURSOR and CRT
    /// STATUS operands, and those rules carry no screen vocabulary at all.</item>
    /// <item><c>xref-sections</c> + <c>kinds</c> — an Annex A.1 obligation citing a declined clause, keyed on the
    /// SAME clause numbers as the clause arm so the next module gets it free instead of needing a second copy.
    /// </item>
    /// <item><c>excludes-kinds</c>, per arm — a general format (kind FMT) is evidence about a CLAUSE, never about
    /// one of its formats, so the TEXT axis must never take one. It also replaced A.4.3's <c>^&lt;pre</c>
    /// exclusion, a RENDERING proxy for a kind predicate that leaks on 56 of 322 FMT rows.</item>
    /// <item><c>requirement</c> — the A.1 REQUIREMENT CLASS the standard itself states, and
    /// <c>determination-prefix</c> — what <c>docs/CONFORMANCE.md</c> §7 SAYS about the element. Added for
    /// kb/Work PB280 Q1, the first derived verdict that is not a module decline: the rules it selects are
    /// perfectly reachable, and what is common to them is the ADJUDICATION. Both axes read something the OWNER
    /// wrote rather than an agent's reading of a rule's text. ⚠ Neither is falsifiable against today's data —
    /// see <see cref="TheSelectorEngine_ProvesEveryAxisCanFail"/>.</item>
    /// </list></summary>
    private static (List<string> ids, string verdict) Select(string name)
    {
        var sel = Schema().GetProperty("derived-verdicts").GetProperty(name);
        var excludes = sel.TryGetProperty("excludes-patterns", out var ex)
            ? ex.EnumerateArray().Select(p => new Regex(p.GetString()!, RegexOptions.IgnoreCase)).ToList()
            : [];
        var arms = sel.GetProperty("arms").EnumerateArray().Select(a => (
            sections: Strings(a, "sections"),
            xrefs: Strings(a, "xref-sections"),
            kinds: Strings(a, "kinds"),
            notKinds: Strings(a, "excludes-kinds"),
            requirement: Strings(a, "requirement"),
            determination: Strings(a, "determination-prefix"),
            pattern: a.TryGetProperty("pattern", out var p)
                ? new Regex(p.GetString()!, RegexOptions.IgnoreCase) : null,
            leadIn: a.TryGetProperty("lead-in", out var li)
                ? new Regex(li.GetString()!, RegexOptions.IgnoreCase) : null,
            // The per-arm twin of `excludes-patterns` (Python: `excludes-pattern`). kb/Work R43 / PB1519: A.4.10's
            // item-1 arm must not stamp the supported item 2 (`interface-name`), while its item-3 arm must reach
            // GR-9.3.6-L4.2, whose exact-match criteria name "an interface-name".
            notPattern: a.TryGetProperty("excludes-pattern", out var np)
                ? new Regex(np.GetString()!, RegexOptions.IgnoreCase) : null)).ToList();

        Assert.All(arms, a => Assert.True(
            a.sections.Count + a.xrefs.Count + a.kinds.Count + a.requirement.Count + a.determination.Count > 0
            || a.pattern is not null || a.leadIn is not null,
            $"derived-verdicts.{name}: an arm with no positive field selects the entire catalog"));

        var determinations = ConformanceRegister.Determinations;
        var catalog = Catalog();
        var leadIns = arms.Any(a => a.leadIn is not null) ? LeadIns(catalog) : [];
        var ids = new List<string>();
        foreach (var r in catalog)
        {
            if (excludes.Any(x => x.IsMatch(r.Text))) continue;
            foreach (var a in arms)
            {
                if (a.kinds.Count > 0 && !a.kinds.Contains(r.Kind)) continue;
                if (a.notKinds.Contains(r.Kind)) continue;
                if (a.requirement.Count > 0 && !a.requirement.Contains(r.Requirement)) continue;
                // What §7 says about THIS element, found by the rule's own id — which is exactly the anchor
                // `kinds.DOC.anchor-template` computes for it. An item with no §7 row yields "", which starts
                // with no prefix, so an undetermined element is never selected: a MISSING determination is not
                // a NEGATIVE one (feedback_verdict_evidence_invariant).
                if (a.determination.Count > 0)
                {
                    string said = determinations.TryGetValue(r.Id, out var cell) ? ConformanceRegister.Plain(cell) : "";
                    if (said.Length == 0
                        || !a.determination.Any(p => said.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                        continue;
                }

                if (a.sections.Count > 0 && !a.sections.Any(p => SectionMatches(p, r.Section))) continue;
                if (a.xrefs.Count > 0
                    && !XrefCitation.Matches(r.Text).Any(m => a.xrefs.Any(p => SectionMatches(p, m.Groups[1].Value))))
                    continue;
                if (a.pattern is not null && !a.pattern.IsMatch(r.Text)) continue;
                if (a.notPattern is not null && a.notPattern.IsMatch(r.Text)) continue;
                if (a.leadIn is not null
                    && !(leadIns.TryGetValue(r.Id, out var lead) && lead.Length > 0 && a.leadIn.IsMatch(lead)))
                    continue;
                ids.Add(r.Id);
                break;
            }
        }
        return (ids, sel.GetProperty("verdict").GetString()!);
    }

    private static void AssertHeldAtTheDerivedVerdict(string name, string why)
    {
        var (ids, verdict) = Select(name);
        var inv = Inventory();

        var wrong = new List<string>();
        foreach (string id in ids)
        {
            if (!inv.TryGetValue(id, out var row)) { wrong.Add($"{id}: no inventory row"); continue; }
            string got = row.TryGetProperty("verdict", out var v) ? v.GetString() ?? "" : "";
            if (!string.Equals(got, verdict, StringComparison.Ordinal))
                wrong.Add($"{id}: {(got.Length == 0 ? "(blank)" : got)}");
        }

        Assert.True(wrong.Count == 0,
            $"every rule the '{name}' selector reaches must carry the ONE derived verdict '{verdict}' — {why}. "
            + $"Diverging rows:\n  " + string.Join("\n  ", wrong)
            + $"\nRe-derive with the schema's selector (python scripts/spec/derive_verdict_batch.py {name}) "
            + "rather than adjudicating one row.");
    }

    // ── The determinations ────────────────────────────────────────────────────────────────────────────────────
    // One fact per declined module. They are separate [Fact]s on purpose: a single loop over every selector
    // would report the first failure and hide the rest, and these six modules fail for unrelated reasons.

    [Fact]
    public void NoStandardBinaryConditionedRow_Diverges() =>
        AssertHeldAtTheDerivedVerdict("standard-binary-only",
            "ARITHMETIC IS STANDARD-BINARY is declined (kb/Work PB198)");

    [Fact]
    public void NoScreenHandlingConditionedRow_Diverges() =>
        AssertHeldAtTheDerivedVerdict("screen-handling-only",
            "Annex A.4.2 ACCEPT/DISPLAY screen handling is Not claimed (CONFORMANCE.md §5, §4 item 4)");

    [Fact]
    public void NoCommitAndRollbackConditionedRow_Diverges() =>
        AssertHeldAtTheDerivedVerdict("commit-and-rollback-only",
            "Annex A.4.3 commit and rollback is Not claimed (CONFORMANCE.md §5, §4 item 2)");

    [Fact]
    public void NoFormatOrSelectWhenConditionedRow_Diverges() =>
        AssertHeldAtTheDerivedVerdict("format-select-when-only",
            "Annex A.4.8 FORMAT and SELECT WHEN file handling is Not claimed (CONFORMANCE.md §5)");

    [Fact]
    public void NoDeclinedOoOptionalItemRow_Diverges() =>
        AssertHeldAtTheDerivedVerdict("oo-optional-items-only",
            "Annex A.4.10 items 1 and 3 are Not claimed; item 2 IS supported (CONFORMANCE.md §5, kb/Work PB285)");

    [Fact]
    public void NoRewriteOrWriteFilePhraseRow_Diverges() =>
        AssertHeldAtTheDerivedVerdict("rewrite-write-file-only",
            "Annex A.4.13 REWRITE FILE and WRITE FILE is Not claimed (CONFORMANCE.md §5)");

    [Fact]
    public void NoValidateConditionedRow_Diverges() =>
        AssertHeldAtTheDerivedVerdict("validate-only",
            "Annex A.4.14 VALIDATE is Not claimed (CONFORMANCE.md §5, §4 item 3)");

    [Fact]
    public void NoAsynchronousMessagingConditionedRow_Diverges() =>
        AssertHeldAtTheDerivedVerdict("mcs-only",
            "Annex A.3 item 4 asynchronous messaging is Not claimed (CONFORMANCE.md §2 row 4, §4 item 1; kb/Work R43)");

    [Fact]
    public void NoUnprovidedFloatingPointFormatRow_Diverges() =>
        AssertHeldAtTheDerivedVerdict("float-formats-not-provided",
            "FLOAT-BINARY-128 and the standard decimal floating-point usages are not provided (CONFORMANCE.md §2 "
            + "rows 13, 17, 19; kb/Work R43)");

    [Fact]
    public void NoRecordKeySourceFormRow_Diverges() =>
        AssertHeldAtTheDerivedVerdict("record-key-source-only",
            "the record-key SOURCE form, Annex A.3 item 40, is Not claimed (CONFORMANCE.md §2 row 40; kb/Work R43)");

    /// <summary>The one selector that is NOT a module decline: an owner ADJUDICATION common to a whole class of
    /// rows. It also drifts by a route none of the others can — a new §7 determination — and that is deliberate:
    /// writing "Not provided." into §7 for an optional item turns this red until the batch is re-run, which is
    /// the register and the inventory being held to one answer instead of two.</summary>
    [Fact]
    public void NoOptionalNotProvidedA1Row_Diverges() =>
        AssertHeldAtTheDerivedVerdict("a1-optional-not-provided",
            "an A.1-OPTIONAL element docs/CONFORMANCE.md §7 records as 'Not provided.' is documented "
            + "non-support (owner, kb/Work PB280 Q1, 2026-09-02)");

    /// <summary>The optional selector's twin over A.1's OTHER conditioned class (kb/Work PB1536 Q1, owner decision
    /// R43 item 3): a CONDITIONALLY-REQUIRED element whose §7 cell opens "Condition absent." is documented
    /// non-support, contingent on the capability staying absent. It drifts by the same route — a §7 row written
    /// "Condition absent." turns this red until the batch is re-run.</summary>
    [Fact]
    public void NoConditionAbsentA1Row_Diverges() =>
        AssertHeldAtTheDerivedVerdict("a1-condition-absent",
            "an A.1-CONDITIONALLY-REQUIRED element whose condition docs/CONFORMANCE.md §7 records as absent is "
            + "documented non-support (owner, kb/Work R43 item 3, 2026-09-24)");

    // ── The engine ────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>⛔ THE COMPONENT-WISE PREFIX RULE, ASSERTED DIRECTLY, INCLUDING THE COLLISION THAT WOULD HAVE
    /// SHIPPED. A raw <c>StartsWith</c> puts §13.18.30 inside §13.18.3 and §13.18.40 (PICTURE) inside §13.18.4.
    /// The screen selector names both of those prefixes, so under the old matcher it selected 543 rules instead
    /// of 156 and flipped 32 CONFORMS rows to non-support. The predicate below fails on a dotless collision by
    /// construction, in both languages.</summary>
    [Theory]
    [InlineData("13.18.3", "13.18.30.2", false)]   // HIGHLIGHT is not inside AUTO
    [InlineData("13.18.4", "13.18.40.4", false)]   // PICTURE is not inside BACKGROUND-COLOR
    [InlineData("13.18.6", "13.18.63.3", false)]   // VALUE is not inside BELL
    [InlineData("13.18.6", "13.18.60.3", false)]   // USAGE is not inside BELL
    [InlineData("14.9.7", "14.9.70.4", false)]     // and it holds for clause numbers that do not exist YET
    [InlineData("13.18.3", "13.18.3.2", true)]
    [InlineData("13.18.3.", "13.18.3.2", true)]    // a trailing dot in the DATA is accepted and ignored
    [InlineData("8.8.1.4", "8.8.1.4.2", true)]
    [InlineData("13.18.51", "13.18.51", true)]     // the clause itself, not only its children
    public void SectionPrefixes_MatchComponentWise_NeverAsRawStrings(string prefix, string section, bool expected)
        => Assert.Equal(expected, SectionMatches(prefix, section));

    /// <summary>The hyphen fold, asserted on the two real catalog rules that need it. Without it the screen
    /// selector's operand arm silently drops SR-13.18.14.3-12 and SR-13.18.35.3-12 — an UNDER-selection, which
    /// leaves every other assertion in this file green.</summary>
    [Fact]
    public void NonBreakingHyphens_AreFolded_SoAnOperandArmCanSeeThem()
    {
        Assert.Equal("identifier-1", Normalize("identifier‑.").Replace(".", "1")[..12]);
        var byId = Catalog().ToDictionary(r => r.Id, r => r.Text, StringComparer.Ordinal);
        foreach (string id in (string[])["SR-13.18.14.3-12", "SR-13.18.35.3-12"])
            Assert.Matches(new Regex(@"\bIdentifier-1\b", RegexOptions.IgnoreCase), byId[id]);
    }

    // ── Sharpness: a predicate is evidence ONLY about what it RETURNED ─────────────────────────────────────────
    // feedback_measure_the_selectors_complement. Every id below is a measured edge, not an illustration: it is
    // either a row a wrong draft dropped, or a row a wrong draft took.

    [Fact]
    public void TheSelector_IsStillSharp_NeitherEverythingNorNothing()
    {
        // It selected 16 of 4,311 rows when it landed — 10 by text and 6 by clause; the bounds are generous
        // enough to survive ordinary catalog growth and tight enough that a pattern typo which matched
        // everything, or a regex that silently compiled to a never-match, would fail here.
        var (ids, _) = Select("standard-binary-only");
        Assert.InRange(ids.Count, 8, 45);

        // The CLAUSE arm must still be live: these six never say "standard-binary" in their text, and a
        // text-only predicate (the first two drafts) silently dropped all of them.
        foreach (string id in (string[])["GR-8.8.1.4.2-1", "GR-8.8.1.4.2-2", "GR-8.8.1.4.4-1",
                                         "GR-8.8.1.4.4-2", "GR-8.8.1.4.4-3", "GR-8.8.1.4.4-4"])
            Assert.Contains(id, ids);

        // And it must still be MODE-sense, not word-sense: the FLOAT-BINARY usage rules of §11.9.8 name
        // "standard-binary" and have nothing to do with the arithmetic mode. They were selected by the first
        // draft of this predicate; if they ever come back, the exclusion has lost the usage/mode distinction.
        Assert.DoesNotContain("SR-11.9.8.3-1", ids);
        Assert.DoesNotContain("SR-11.9.8.3-2", ids);
        Assert.DoesNotContain("SR-11.9.8.3-3", ids);

        // Conversely, a rule with BOTH arms keeps its own verdict — it still has reachable content under a mode
        // we support. §15.67.3 r4 caps digits at 35 under standard-binary AND at 34 under standard-decimal.
        Assert.DoesNotContain("AR-15.67.3-4", ids);

        // And the rule whose only "standard decimal" is a USAGE must still be IN — dropping it was the bug in
        // the second draft of this predicate.
        Assert.Contains("AR-15.43.3-3", ids);

        // ⛔ A NOTE CANNOT EXCLUDE A RULE (kb/Work R43 / PB1517). §11.9.5.2 GR2 IS the mode; only its NOTE 1 —
        // "Implementors are strongly encouraged to provide support for the STANDARD-DECIMAL phrase" — mentions
        // standard-decimal, and until the NOTE cut that informative sentence tripped the mixed-rule exclusion.
        Assert.Contains("GR-11.9.5.2-2", ids);
        // …while a rule whose NORMATIVE text names both modes still keeps its own verdict.
        Assert.DoesNotContain("GR-14.7.7-3", ids);
    }

    [Fact]
    public void TheScreenSelector_IsStillSharp()
    {
        var (ids, _) = Select("screen-handling-only");
        Assert.InRange(ids.Count, 120, 200);   // 156 when it landed

        // The CLAUSE arm — these say nothing about screens ("The word EOL is equivalent to the words END OF
        // LINE"), so a text-only predicate misses 52 rows including both wholly-screen general formats.
        foreach (string id in (string[])["FMT-13.17.2", "FMT-13.9.2", "GR-9.2.3-1", "SR-13.18.21.3-1",
                                         "GR-13.18.61.4-2"])
            Assert.Contains(id, ids);
        // The TEXT arm, reaching into shared clauses.
        foreach (string id in (string[])["GR-13.18.35.4-14", "SR-13.18.38.3-11", "SR-13.18.60.3-17",
                                         "GR-11.9.10.4-3", "GR-14.6.2.3.2-L2.4", "SR-14.9.39.3-16"])
            Assert.Contains(id, ids);
        // The OPERAND arms — the third scoping mechanism, and the five rows the first draft could not see.
        // §12.3.7's data-name-1/-2 ARE the CURSOR and CRT STATUS operands; MINUS and identifier-1 exist only in
        // the screen format of COLUMN and LINE. The LINE pair is feedback_two_arm_dispatch verbatim: the COLUMN
        // arm was inspected and the LINE arm was not.
        foreach (string id in (string[])["SR-12.3.7.3-4", "SR-12.3.7.3-29", "SR-12.3.7.3-30",
                                         "SR-13.18.14.3-11", "SR-13.18.14.3-12",
                                         "SR-13.18.35.3-11", "SR-13.18.35.3-12"])
            Assert.Contains(id, ids);

        // ⛔ THE SINGLE MOST IMPORTANT PAIR IN THIS FILE. FMT-14.9.1.2 and FMT-14.9.11.2 are the ACCEPT and
        // DISPLAY general formats — both currently PARTIAL, both carrying the compiler's most-used statements —
        // and they match a screen pattern only because their OPTIONAL format names screen-name-1. A diagram is
        // evidence about a CLAUSE, never about one format. If either returns, `excludes-kinds` has been lost off
        // the text arm and a supported statement has just been documented as unsupported.
        Assert.DoesNotContain("FMT-14.9.1.2", ids);
        Assert.DoesNotContain("FMT-14.9.11.2", ids);

        // THE ON EXCEPTION OPERAND ARM (kb/Work R43 / PB1151). The exception phrases exist only in the screen
        // formats, and the band's rules never say "screen" — a text-only predicate dropped all five ACCEPT rows.
        // (GR25 of ACCEPT and GR18 of DISPLAY carry their nested exception lists as their own body since kb/Work
        // PB55; those items were rows GR-14.9.1.4-L2.1 … L3.2 and GR-14.9.11.4-L2.1/L2.2 until then.)
        foreach (string id in (string[])["GR-14.9.1.4-24", "GR-14.9.1.4-25", "GR-14.9.11.4-17", "GR-14.9.11.4-18"])
            Assert.Contains(id, ids);
        // …and the device/temporal rules of the same two clauses stay out: GR1 of each is ordinary surface.
        Assert.DoesNotContain("GR-14.9.1.4-1", ids);
        Assert.DoesNotContain("GR-14.9.11.4-1", ids);

        // The COLUMN/LINE format-1 (report-writer) rules must stay out — the clause is shared.
        Assert.DoesNotContain("GR-13.18.14.4-1", ids);
        Assert.DoesNotContain("GR-13.18.35.4-1", ids);
        // Near-misses that enumerate the screen construct ALONGSIDE its data-division twin, so content survives.
        Assert.DoesNotContain("SR-13.18.20.3-3", ids);
        Assert.DoesNotContain("GR-11.9.10.4-1", ids);
        // SR-12.3.7.3-2's CONTENT is what it FORBIDS — every clause other than CURSOR and CRT STATUS — which
        // survives entirely. It is the one §12.3.7.3 rule the operand arm must NOT take.
        Assert.DoesNotContain("SR-12.3.7.3-2", ids);
        // The Annex A.1 arms — owner decision R42 (2026-09-24): an A.1 item of the DECLINED screen module is
        // withdrawn by A.1's preamble, exactly as A.4.8's items 84/85/173 are. Ten items, by two arms: the
        // cross-reference into a declined clause, and the SCREEN FORMAT of a shared statement.
        foreach (string id in (string[])["DOC-A.1-3", "DOC-A.1-4", "DOC-A.1-11", "DOC-A.1-27", "DOC-A.1-41",
                                         "DOC-A.1-45", "DOC-A.1-83", "DOC-A.1-91", "DOC-A.1-172", "DOC-A.1-199"])
            Assert.Contains(id, ids);
        // ⛔ ACCEPT's DEVICE and temporal formats are claimed and share §14.9.1 with the screen format: their A.1
        // items (conversion, transfer size, …) must never be withdrawn. If one appears, the screen-format arm
        // lost its `pattern` and is keying on the bare cross-reference.
        foreach (string id in (string[])["DOC-A.1-1", "DOC-A.1-2", "DOC-A.1-5", "DOC-A.1-111"])
            Assert.DoesNotContain(id, ids);
        // And the clause arm must not have widened past its own paths (the dotless collision).
        Assert.DoesNotContain("GR-13.18.40.4-1", ids);   // PICTURE, inside "13.18.4" only under a raw prefix
        Assert.DoesNotContain("SR-13.18.30.3-1", ids);   // HIGHLIGHT SR1 is real; §13.18.30 is not §13.18.3
    }

    [Fact]
    public void TheCommitRollbackSelector_IsStillSharp()
    {
        var (ids, _) = Select("commit-and-rollback-only");
        Assert.InRange(ids.Count, 15, 40);   // 25 when it landed

        // Clause arm: seven §12.4.6.3 rules say only "this clause" and a text-only predicate drops all of them.
        Assert.Contains("SR-12.4.6.3.3-1", ids);
        // Clause arm, EC half: the APPLY COMMIT clause's own GR6 (EC-FLOW-APPLY-COMMIT) is module functionality and
        // stays IN. The selector carries no EC-FLOW exclusion at all (kb/Work PB259 dropped the anchored one); if
        // this drops out, an exclusion keyed on the EC-FLOW names came back and is reaching the clause arm.
        Assert.Contains("GR-12.4.6.3.4-6", ids);
        // Text arm: the module's own functionality stated inside the MANDATORY §14.6.11. Note the PLURAL.
        Assert.Contains("GR-14.6.11-1", ids);
        // The closest call in the set — see the entry's $selector for why "separately" settles it.
        Assert.Contains("SR-12.4.6.3.3-10", ids);
        // The APPLY COMMIT clause is refused by name whole, so its general format is declined with it (kb/Work
        // R43 / PB1099 Q2); the COMMIT/ROLLBACK statements are accepted inert and their formats stay OUT.
        Assert.Contains("FMT-12.4.6.3.2", ids);
        Assert.DoesNotContain("FMT-14.9.36.2", ids);

        // ⛔ FOUR ROWS THAT ARE IMPLEMENTED AND GOLDEN-COVERED (kb/Work PB137). If any returns, an exclusion was
        // dropped and shipped code is being documented as unsupported.
        foreach (string id in (string[])["SR-14.9.7.3-1", "SR-14.9.7.3-2", "GR-14.9.7.4-1", "GR-14.9.36.4-7"])
            Assert.DoesNotContain(id, ids);
        // Each statement's GR2 SETS EC-FLOW-COMMIT / EC-FLOW-ROLLBACK, the declined module's own condition, so
        // nothing survives it (kb/Work PB259 settled both). If either drops out, an EC-FLOW exclusion came back.
        foreach (string id in (string[])["GR-14.9.7.4-2", "GR-14.9.36.4-2"])
            Assert.Contains(id, ids);
        // FMT-14.9.36.2 would be wrong as non-support — ROLLBACK IS a recognized, accepted-inert statement in the
        // shipped grammar. Excluded by KIND on the statement arm, not by matching "^<pre": that rendering proxy
        // leaks on 56 of 322 FMT rows. (The REFUSED clause's own format, FMT-12.4.6.3.2, is in — asserted above.)
        Assert.DoesNotContain("FMT-14.9.36.2", ids);
        // ⛔ AND THE SCOPE STOPS AT THE MODULE'S OWN CLAUSES. These name APPLY COMMIT only as an antecedent; with
        // the module absent they are VACUOUSLY SATISFIED, which is not the same fact as unsupported, and their
        // verdicts speak about UNLOCK / sharing / LOCK MODE — facilities A.4.7 CLAIMS.
        foreach (string id in (string[])["SR-14.9.47.3-2", "GR-9.1.15-3", "SR-12.4.5.9.3-1"])
            Assert.DoesNotContain(id, ids);
        // ⚖ DOC-A.1-28's SUBJECT is the module itself ("Commit and rollback (interaction with other facilities and
        // languages)", §9.1.18), so owner decision R42 (2026-09-24) withdraws it through the Annex A.1 arm.
        Assert.Contains("DOC-A.1-28", ids);
    }

    [Fact]
    public void TheFormatSelectWhenSelector_IsStillSharp()
    {
        var (ids, _) = Select("format-select-when-only");
        Assert.InRange(ids.Count, 30, 55);   // 37 when it landed; 39 with the selection arm (R43)

        // The TEXT arm must stay live — a clause-only predicate drops all three.
        foreach (string id in (string[])["SR-13.4.5.3-6", "GR-13.18.13.4-5", "SR-14.9.27.3-9"])
            Assert.Contains(id, ids);
        // The Annex A.1 arm: obligations whose cross-reference points INTO a declined clause. These are the
        // first DOC rows in the inventory to carry any verdict; audit_annex_a1.py applies the same A.1-preamble
        // exclusion so §7's counter and the inventory cannot give two answers about one item.
        foreach (string id in (string[])["DOC-A.1-84", "DOC-A.1-85", "DOC-A.1-173"])
            Assert.Contains(id, ids);

        // ⛔ The A.1 arm is keyed on `kinds: [DOC]`, and FMT-13.16.2's cross-reference table also cites
        // "13.18.51, SELECT WHEN clause". It is the higher-level construct A.4.1 NOTE 1 makes NOT optional, and
        // it carries a dozen live clauses besides. If it returns, the kind filter has been lost.
        Assert.DoesNotContain("FMT-13.16.2", ids);
        Assert.DoesNotContain("FMT-13.4.5.2", ids);
        // The CODE-SET no-SELECT-WHEN branches are the behaviour WITHOUT the module, implemented today in
        // DataBinder.BindCodeSetClause. Excluding on the word CODE-SET would have dropped three of the module's
        // OWN rules instead, because excludes apply to every arm — which is why the pattern is narrow.
        foreach (string id in (string[])["SR-13.18.13.3-3", "GR-13.18.13.4-3"])
            Assert.DoesNotContain(id, ids);
        // THE SELECTION ARM (kb/Work R43): a record description entry being SELECTED is SELECT WHEN's own
        // mechanism. I-O status 45 has no producer without it (PB1518), and CODE-SET GR4 is declined jointly
        // with A.4.13 — its FILE-phrase branch is the only other content it has (PB1255).
        Assert.Contains("GR-9.1.13.7-5", ids);
        Assert.Contains("GR-13.18.13.4-4", ids);
        // One name in a list of several — content survives entirely.
        foreach (string id in (string[])["GR-13.18.49.4-1", "GR-13.18.57.4-1", "SR-13.16.3-13", "GR-14.9.24.4-2"])
            Assert.DoesNotContain(id, ids);
    }

    [Fact]
    public void TheOoOptionalItemsSelector_IsStillSharp()
    {
        var (ids, _) = Select("oo-optional-items-only");
        Assert.InRange(ids.Count, 4, 12);   // 3 when it landed; 6 with item 3's exact-match criteria (R43)

        foreach (string id in (string[])["SR-8.4.3.8.3-5", "SR-11.3.3-7", "GR-11.3.4-4"])
            Assert.Contains(id, ids);

        // ⛔ ITEM 2 IS SUPPORTED AND THIS IS NOW THE OWNER'S ROW, NOT AN AGENT'S READING (kb/Work PB285).
        // CobolOO.g4:77 parses `INHERITS FROM interfaceName+`; OoClassTable enforces §11.6.3 SR2 (COBOLNET0840) and
        // SR3/SR6 (COBOLNET2791). SR-11.6.3-6's text is verbatim parallel to SR-11.3.3-7 and matches the pattern, so the
        // `interface-name` exclusion is the only thing holding it out; if it returns, an implemented, shipping
        // facility is being documented as unsupported. SR-11.6.3-5 is ordinary work to adjudicate.
        Assert.DoesNotContain("SR-11.6.3-6", ids);
        Assert.DoesNotContain("SR-11.6.3-5", ids);
        // A clause arm over [11.3, 11.6, 8.4.3.8] selects 40 rows where 3 are conditioned, so there is none.
        Assert.DoesNotContain("FMT-11.3.2", ids);
        Assert.DoesNotContain("FMT-11.6.2", ids);
        // Mixed blocks keep their own verdicts: SR-11.3.3-6's second sentence has a satisfiable antecedent in a
        // LINEAR chain, and GR-9.3.6-L4.3 absorbs the overload tie-break — the sharpest trap for a widening.
        Assert.DoesNotContain("SR-11.3.3-6", ids);
        // ITEM 3 (kb/Work R43 / PB1519): §9.3.6's exact-match criteria, reached by the sentence that introduces
        // them — they carry no vocabulary of their own. L4.2 names "an interface-name", so the item-2 exclusion
        // must stay on the ITEM-1 arm (`excludes-pattern`); as a global exclusion it silently dropped it. (L5 until
        // kb/Work PB55 folded §9.3.6 3) e)'s nested list, the sub-list numbered before this one.)
        foreach (string id in (string[])["GR-9.3.6-L4.1", "GR-9.3.6-L4.2", "GR-9.3.6-L4.3"])
            Assert.Contains(id, ids);
        // The introducing rule is half live (its MOVE-sender sentence), so R43 item 2 gives it the live verdict.
        Assert.DoesNotContain("GR-9.3.6-L3.7", ids);
        Assert.DoesNotContain("GR-9.3.5.3-7", ids);
        // The pattern is anchored to "in an INHERITS clause"; an unanchored "appear more than once" draft
        // dragged in the SUM clause and the procedure division header. Measured, not imagined.
        Assert.DoesNotContain("SR-13.18.54.3-1", ids);
        Assert.DoesNotContain("SR-14.2.2-1", ids);
    }

    [Fact]
    public void TheMcsSelector_IsStillSharp()
    {
        var (ids, _) = Select("mcs-only");
        Assert.InRange(ids.Count, 20, 40);   // 28 when it landed (kb/Work R43)
        // Both statements, general formats included — §4.2.6 ¶3 releases the declined syntax.
        foreach (string id in (string[])["FMT-14.9.31.2", "FMT-14.9.38.2", "SR-14.9.31.3-3", "GR-14.9.38.4-8"])
            Assert.Contains(id, ids);
        // The message-tag class and protocol stated from elsewhere.
        foreach (string id in (string[])["GR-8.4.3.10.4-4", "GR-8.8.4.2.1-9", "GR-14.6.11-7", "DOC-A.1-212"])
            Assert.Contains(id, ids);
        // ⛔ THE CLASS LIST. Message-tag inside an enumeration of classes constrains the others too, which are
        // live: SR-8.8.4.2.3-5's object/pointer halves are verified, GR-13.18.63.4-4 initializes every class.
        foreach (string id in (string[])["SR-8.8.4.2.3-5", "GR-13.18.63.4-4", "SR-13.16.3-10", "SR-14.9.11.3-1"])
            Assert.DoesNotContain(id, ids);
        // A text arm never takes a general format: USAGE's and INITIALIZE's formats list MESSAGE-TAG among others.
        Assert.DoesNotContain("FMT-13.18.60.2", ids);
        Assert.DoesNotContain("FMT-14.9.20.2", ids);
    }

    [Fact]
    public void TheFloatFormatsSelector_IsStillSharp()
    {
        var (ids, _) = Select("float-formats-not-provided");
        Assert.InRange(ids.Count, 8, 20);   // 11 when it landed (kb/Work R43)
        foreach (string id in (string[])["GR-13.18.60.4-16", "GR-13.18.60.4-17", "GR-13.18.60.4-18",
                                         "GR-13.18.60.4-20", "SR-11.9.9.3-1", "SR-11.9.9.3-6", "DOC-A.1-47"])
            Assert.Contains(id, ids);
        // ⛔ HALF-DECLINED RULES KEEP THE LIVE HALF'S VERDICT (R43 item 2): the numeric-category usage list and
        // the endianness rule name the PROVIDED binary floats too, and DOC-A.1-48 documents their default.
        foreach (string id in (string[])["GR-8.5.2.12-2", "GR-13.18.60.4-19", "DOC-A.1-48"])
            Assert.DoesNotContain(id, ids);
        // decimal128 is ALSO the standard-decimal arithmetic intermediate format, which IS provided.
        Assert.DoesNotContain("GR-8.8.1.5.2-2", ids);
        // One row, one determination: these two are standard-binary-only's.
        Assert.DoesNotContain("AR-15.43.3-3", ids);
        Assert.DoesNotContain("AR-15.58.3-3", ids);
    }

    [Fact]
    public void TheRecordKeySourceSelector_IsStillSharp()
    {
        var (ids, _) = Select("record-key-source-only");
        Assert.InRange(ids.Count, 3, 10);   // 5 when it landed (kb/Work R43)
        foreach (string id in (string[])["SR-12.4.5.6.3-6", "GR-12.4.5.6.4-2", "SR-12.4.5.12.3-5",
                                         "GR-12.4.5.12.4-2", "SR-14.9.41.3-7"])
            Assert.Contains(id, ids);
        // "data-name-1 or record-key-name-1" constrains the plain key too — each arm's own exclusion.
        Assert.DoesNotContain("GR-12.4.5.6.4-6", ids);
        Assert.DoesNotContain("SR-14.9.41.3-6", ids);
        Assert.DoesNotContain("GR-12.4.5.12.4-4", ids);
        // Each general format prints both key forms.
        Assert.DoesNotContain("FMT-12.4.5.12.2", ids);
        Assert.DoesNotContain("FMT-12.4.5.6.2", ids);
    }

    [Fact]
    public void TheRewriteWriteFileSelector_IsStillSharp()
    {
        var (ids, _) = Select("rewrite-write-file-only");
        Assert.InRange(ids.Count, 8, 20);   // 12 when it landed

        // The second alternative, which no "FILE phrase" text arm reaches: SR-14.9.35.3-11 is "File-name-1 shall
        // not reference a report file or a sort-merge file description entry." — and its leading capital F is
        // why the pattern carries (?i) in the DATA rather than trusting the reader's options. Case-sensitively
        // the arm returns 11.
        Assert.Contains("SR-14.9.35.3-11", ids);
        Assert.Contains("SR-14.9.51.3-7", ids);

        // ⛔ THE MIRROR ARMS. These are conditioned on the FILE phrase being NOT specified, i.e. they govern
        // plain `REWRITE/WRITE record-name-1`, fully supported surface. They match the pattern and are held out
        // by one exclusion; if either returns, two supported rules have been declared unsupported.
        Assert.DoesNotContain("SR-14.9.35.3-9", ids);
        Assert.DoesNotContain("SR-14.9.51.3-11", ids);
        // ⛔ THE SCOPE GATE. `file-name-1` is STATEMENT-LOCAL: in §13.4.5.4 it is the FD's own subject. This row
        // is a genuine ungated match, so it is the anchor that actually proves the AND-gate is still there —
        // ungated the selector takes 176 rules of DELETE, OPEN, CLOSE, READ, START, UNLOCK and every FD clause.
        Assert.DoesNotContain("GR-13.4.5.4-1", ids);
        // The asymmetric twin: WRITE's SR-12 says "the write file", which SR1 defines as covering the
        // record-name arm too. The asymmetry is the standard's own wording, not a reading.
        Assert.DoesNotContain("SR-14.9.51.3-12", ids);
        // Belongs to A.4.8 and also covers READ statements.
        Assert.DoesNotContain("DOC-A.1-173", ids);
    }

    [Fact]
    public void TheValidateSelector_IsStillSharp()
    {
        var (ids, _) = Select("validate-only");
        Assert.InRange(ids.Count, 55, 110);   // 75 when it landed

        Assert.Contains("FMT-14.9.50.2", ids);          // clause arm
        Assert.Contains("GR-13.18.40.4-19", ids);       // text arm, reaching outside the module's own clauses
        // Its identical sibling had been left BLANK by hand while -19 carried the verdict — the four-answers-to-
        // one-question drift a derived verdict exists to stop.
        Assert.Contains("GR-13.18.40.4-15", ids);
        // DOC-A.1-86 cites exactly the two rules the text arm already stamps, so the selector's own reasoning
        // convicts it. Reached by matching "format validation" rather than "format validation STAGE".
        Assert.Contains("DOC-A.1-86", ids);
        // ⛔ GR-13.18.64.4-2's antecedent is "If the VARYING clause is NOT specified in a report description
        // entry", so the Report Writer licence cannot reach it — and its complementary half -6 was selected on
        // an antecedent word-for-word identical. It had been excluded; that exclusion is gone.
        Assert.Contains("GR-13.18.64.4-2", ids);
        Assert.Contains("GR-13.18.64.4-6", ids);

        // The PICTURE symbol table CONFORMS today, with code and a golden; its only VALIDATE text is an
        // EXCEPTION ("except within the execution of a VALIDATE statement"). A rule that mentions VALIDATE as an
        // exception is not conditioned on it.
        Assert.DoesNotContain("GR-13.18.40.4-14", ids);
        // The Report Writer arm of VARYING, which §5 records as implemented under A.4.11.
        Assert.DoesNotContain("GR-13.18.64.4-3", ids);
        Assert.DoesNotContain("GR-13.18.64.4-5", ids);
        // ⚖ THE §13.18.11 CLASS CLAUSE IS IN, by OWNER DECISION 2026-09-02 (kb/Work PB375) — these three
        // assertions were `DoesNotContain` while the question was open. Its content is entirely VALIDATE, yet
        // Annex A.4 never lists it and it carries no obsolete-feature NOTE, so the alternative reading (not
        // optional ⇒ A.4.1 obliges us to ACCEPT `CLASS IS NUMERIC`) was live and an agent could not settle it.
        // The deciding ground is §13.16.2 itself: its printed Format-1 validation-clauses group opens with
        // `[ class-clause ]` and maps it to "13.18.11, CLASS clause".
        foreach (string id in (string[])
                 ["SR-13.18.11.3-1", "GR-13.18.11.4-1", "GR-13.18.11.4-2", "GR-13.18.11.4-3",
                  "GR-13.18.11.4-4", "FMT-13.18.11.2"])
            Assert.Contains(id, ids);
        // ⛔ AND THE WORD IS NOT THE CLAUSE — the arm keys on the SECTION, so the two CLAIMED constructs that
        // spell CLASS are untouched: §12.3.7's SPECIAL-NAMES CLASS clause and §8.8.4.4's simple class
        // condition. This is what fails if someone re-expresses the arm as a text pattern on "CLASS clause".
        // (Both ids are real catalog rows — a DoesNotContain on a rule-id that does not exist is vacuously
        // green, which is how a control stops being evidence.)
        var catalogIds = Catalog().Select(r => r.Id).ToHashSet(StringComparer.Ordinal);
        foreach (string id in (string[])["SR-12.3.7.3-1", "SR-8.8.4.4.3-1"])
        {
            Assert.Contains(id, catalogIds);
            Assert.DoesNotContain(id, ids);
        }
        // EC-DATA-INCOMPATIBLE names VALIDATE only to exempt it.
        Assert.DoesNotContain("GR-14.6.13.2-1", ids);
        Assert.DoesNotContain("GR-14.6.13.2-2", ids);
    }

    [Fact]
    public void TheA1OptionalNotProvidedSelector_IsStillSharp()
    {
        var (ids, _) = Select("a1-optional-not-provided");
        Assert.InRange(ids.Count, 1, 30);   // 2 when it landed; the ceiling is A.1's 30 optional items

        // The rows the owner's answer settled. 127 was BLANK when the selector landed — held out of the
        // 2026-09-02 A.1 back-fill for exactly this open question — so the selector overwrote no adjudication.
        Assert.Contains("DOC-A.1-127", ids);   // OBJECT-COMPUTER computer-name: one object computer, the runtime
        Assert.Contains("DOC-A.1-150", ids);   // RECORD DELIMITER feature-name: the set of names is empty

        // ⛔ AND THE ROW THAT LEFT, asserted rather than silently dropped (2026-09-09, owner decision Q30 /
        // kb/Work PB592). Item 206 was the selector's second row on the premise that the BINARY-CHAR family
        // holds "exactly the GR12 minimum range". §13.18.60.4 GR12's table is asymmetric — the SIGNED rows
        // print a STRICT `<` on BOTH sides — so -2**(w-1) is outside the minimum band, and this compiler
        // stores it; the element IS provided. Its §7 cell now opens "Provided", the selector no longer takes
        // it, and the row carries an adjudicated CONFORMS. This is the paragraph in `$selector` running
        // forwards, and it is asserted HERE so that a cell edited back to "Not provided." (or a determination
        // -prefix arm quietly widened) turns red instead of re-absorbing the row.
        Assert.DoesNotContain("DOC-A.1-206", ids);
        Assert.StartsWith("Provided", ConformanceRegister.Plain(ConformanceRegister.Determinations["DOC-A.1-206"]),
            StringComparison.Ordinal);

        // ⛔ EVERY SELECTED ROW IS AN OPTIONAL ITEM WHOSE §7 CELL OPENS "Not provided." — asserted here rather
        // than trusted, because a widened predicate would still contain the two rows above and stay green.
        var byId = Catalog().ToDictionary(r => r.Id, r => r, StringComparer.Ordinal);
        foreach (string id in ids)
        {
            Assert.Equal("optional", byId[id].Requirement);
            Assert.StartsWith("Not provided",
                ConformanceRegister.Plain(ConformanceRegister.Determinations[id]),
                StringComparison.OrdinalIgnoreCase);
        }

        // A DOC row §7 does not carry at all must stay out: nothing has been determined about it, and a MISSING
        // determination is not a NEGATIVE one. Item 85 (FORMAT clause exclusions — A.1-optional, withdrawn with
        // the unclaimed A.4.8) is the anchor now: since doc-rows-2 (2026-09-25) every optional item that CAN
        // arise has a §7 row. Item 7 held this anchor until 2026-09-24, when kb/Work PB1522 step 3 wrote its
        // 'Not provided.' row and the selector took it with no edit to the predicate; item 67 held it until
        // 2026-09-25, when doc-rows-2 wrote its R44-only row — which opens with the determination, not "Not
        // provided", so the selector does NOT take it. Both moves are asserted below so they are visible.
        Assert.DoesNotContain("DOC-A.1-85", ids);
        Assert.False(ConformanceRegister.Determinations.ContainsKey("DOC-A.1-85"),
            "item 85 is the anchor for 'optional, but undetermined' — if §7 has grown a row for it, this "
            + "assertion's subject moved and another optional item with no determination must take its place");
        Assert.Equal("optional", byId["DOC-A.1-85"].Requirement);
        Assert.DoesNotContain("DOC-A.1-67", ids);
        Assert.True(ConformanceRegister.Determinations.ContainsKey("DOC-A.1-67"));
        Assert.Equal("optional", byId["DOC-A.1-67"].Requirement);
        Assert.Contains("DOC-A.1-7", ids);
        // A REQUIRED item with a determination stays out — item 19's is positive, so this is the pair working
        // together rather than either axis alone. The axes are separated in the engine's own self-test.
        Assert.DoesNotContain("DOC-A.1-19", ids);
        // …and the four items the A.4 declines WITHDREW belong to their module's selector, not to this one:
        // they have no §7 row, which is the whole difference between a withdrawn item and a documented decline.
        foreach (string id in (string[])["DOC-A.1-84", "DOC-A.1-85", "DOC-A.1-86", "DOC-A.1-173"])
            Assert.DoesNotContain(id, ids);
    }

    [Fact]
    public void TheA1ConditionAbsentSelector_IsStillSharp()
    {
        var (ids, _) = Select("a1-condition-absent");
        Assert.InRange(ids.Count, 1, 27);   // 12 when it landed; the ceiling is A.1's 27 conditional items

        // The rows the owner's answer settled (kb/Work PB1536 Q1): the non-COBOL-program family, the IMP directive,
        // the single character set and encoding, the hex-digit mapping, RECORD DELIMITER feature-name-1, the lock
        // status and the listing controls. All twelve were BLANK when the selector landed.
        foreach (string id in (string[])["DOC-A.1-13", "DOC-A.1-15", "DOC-A.1-30", "DOC-A.1-34", "DOC-A.1-37",
                     "DOC-A.1-88", "DOC-A.1-96", "DOC-A.1-98", "DOC-A.1-142", "DOC-A.1-149", "DOC-A.1-152",
                     "DOC-A.1-200"])
            Assert.Contains(id, ids);

        // ⛔ EVERY SELECTED ROW IS A CONDITIONAL ITEM WHOSE §7 CELL OPENS "Condition absent." — asserted rather than
        // trusted, because a widened predicate would still contain the twelve above and stay green.
        var byId = Catalog().ToDictionary(r => r.Id, r => r, StringComparer.Ordinal);
        foreach (string id in ids)
        {
            Assert.Equal("conditionally required", byId[id].Requirement);
            Assert.StartsWith("Condition absent",
                ConformanceRegister.Plain(ConformanceRegister.Determinations[id]), StringComparison.OrdinalIgnoreCase);
        }

        // A conditional item whose condition HOLDS keeps its positive determination and stays out: item 65 (a
        // .NET host activator exists, so EXIT/GOBACK into a non-COBOL runtime element is determined).
        Assert.Equal("conditionally required", byId["DOC-A.1-65"].Requirement);
        Assert.True(ConformanceRegister.Determinations.ContainsKey("DOC-A.1-65"));
        Assert.DoesNotContain("DOC-A.1-65", ids);
        // Item 36 was the anchor for 'conditional, but undetermined' (a MISSING determination is not a NEGATIVE
        // one) until doc-rows-2 (2026-09-25) wrote its row: the condition HOLDS (one UTF-16 set serves both
        // classes), so it is a second positive-determination case and stays out. Since then EVERY conditional A.1
        // item has a §7 row, so the undetermined axis has no live anchor in this file; the selector engine's own
        // self-test keeps it (a blank determination never matches a determination-prefix).
        Assert.Equal("conditionally required", byId["DOC-A.1-36"].Requirement);
        Assert.True(ConformanceRegister.Determinations.ContainsKey("DOC-A.1-36"));
        Assert.DoesNotContain("DOC-A.1-36", ids);
        // …and the optional item the same witness pins (143) belongs to the OPTIONAL selector, not to this one.
        Assert.DoesNotContain("DOC-A.1-143", ids);
    }

    /// <summary>⛔ THE REGISTER IS REALLY BEING READ, asserted before anything above is believed. A parser that
    /// silently returned nothing would leave <c>determination-prefix</c> matching nothing at all — an
    /// UNDER-selection, the direction that keeps every other fact in this file green
    /// (<c>feedback_measure_the_selectors_complement</c>).</summary>
    [Fact]
    public void TheRegisterParser_ReadsSection7_AndNotAnEmptyTable()
    {
        Assert.True(File.Exists(ConformanceRegister.Path), $"the register is missing: {ConformanceRegister.Path}");
        Assert.InRange(ConformanceRegister.Rows.Count, 20, 400);        // 47 rows when this landed
        Assert.All(ConformanceRegister.Rows, r => Assert.Matches(@"^DOC-A\.1-\d+$", r.Key));

        // The escaped-pipe case, which is why the split is not `Split('|')`: item 82's determination writes an
        // absolute value as \|v\|, and a naive split gives that row two extra cells and mis-places the rest.
        var escaped = ConformanceRegister.Parse(
            "## 7. Annex A.1\n| DOC-A.1-82 | E | rounds \\|v\\| away from zero | — |\n");
        Assert.Equal("rounds |v| away from zero", Assert.Single(escaped).Determination);
    }

    /// <summary>
    /// ⛔ AN AXIS THE LIVE DATA CANNOT SEPARATE IS FALSIFIED SOMEWHERE THAT CAN (the `requirement` axis today;
    /// both of the two newest ones when this landed).
    /// </summary>
    /// <remarks>
    /// Every assertion above measures a selector against the LIVE catalog and the LIVE register — the right
    /// check for a selector that has landed, and powerless over an axis the live data cannot separate. ⚠ THE
    /// TWO AXES ARE NO LONGER EQUALLY BLIND, and the sentence that stood here said otherwise: re-measured
    /// 2026-09-09, §7 carries 58 rows and A.1 has 30 optional items, of which 5 have a §7 row — and only 2 of
    /// those 5 open "Not provided.". Items 10, 184 and (since owner decision Q30 / kb/Work PB592) 206 are
    /// A.1-OPTIONAL rows whose determination is positive, so a predicate that matched ANY determination now
    /// selects five rows instead of two and this file DOES turn red. The <c>requirement</c> axis is still
    /// blind here — every "Not provided." row is on an optional item, so dropping it changes nothing live —
    /// which is why the self-test below is not retired. <c>derive_verdict_batch.py --self-test</c> drives each
    /// axis against a fabricated catalog and
    /// a fabricated register, one broken thing at a time; the runner (scripts/self_tests.py) runs it in every gate rather than when
    /// a human remembers — the failure mode measured on <c>audit_annex_a1.py</c> on 2026-09-01.
    /// </remarks>
    [Fact]
    public void TheSelectorEngine_ProvesEveryAxisCanFail()
    {
        string script = TestRepo.Scripts("spec", "derive_verdict_batch.py");
        Assert.True(File.Exists(script), $"the derived-verdict batch generator is missing: {script}");
        // Holding the CASE NAMES from the source: a shrinking self-test still exits 0, and the runner
        // (scripts/self_tests.py) runs it in every gate and CI job (kb/Work PB2882, PB3022).
        SelfTestArmSource.AssertDeclares(script, new[]
                 {
                     "the REQUIREMENT axis discriminates",
                     "the KIND axis discriminates",
                     "the DETERMINATION axis discriminates",
                     "an item with NO §7 row is never selected",
                     "a determination that merely CONTAINS the phrase is not one that begins with it",
                     "the emphasis strip is real",
                     "an arm with only negative fields is refused",
                     // kb/Work R43: the lead-in axis, the NOTE cut and the per-arm exclusion.
                     "the LEAD-IN axis selects a list's items by the sentence that introduces them",
                     "a nested list does not inherit its parent list's lead-in",
                     "a NOTE cannot EXCLUDE a rule",
                     "a NOTE cannot SELECT a rule",
                     "an arm's OWN exclusion does not reach its sibling arm",
                 });
    }

    /// <summary>Two determinations must never claim the same row: the row would then carry whichever batch ran
    /// last, and the drift facts above would both pass while disagreeing about WHY. The Python generator refuses
    /// such a batch outright; this is the same invariant asserted against the data.</summary>
    [Fact]
    public void NoTwoSelectors_ClaimTheSameRow()
    {
        var names = Schema().GetProperty("derived-verdicts").EnumerateObject()
            .Where(p => !p.Name.StartsWith('$')).Select(p => p.Name).ToList();
        Assert.InRange(names.Count, 2, 40);

        var owner = new Dictionary<string, string>(StringComparer.Ordinal);
        var clashes = new List<string>();
        foreach (string n in names)
            foreach (string id in Select(n).ids)
                if (!owner.TryAdd(id, n))
                    clashes.Add($"{id}: claimed by both '{owner[id]}' and '{n}'");

        Assert.True(clashes.Count == 0, "one row, one determination:\n  " + string.Join("\n  ", clashes));
    }
}
