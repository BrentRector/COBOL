// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The battery gate over the PHASE-14 Step-0 spec-traceability inventory — the artifact whose GAP count DEFINES
/// v1.0 (owner decision D13: zero GAP = P14 done).
/// </summary>
/// <remarks>
/// <para>
/// A completion metric that nothing audits is a self-report. Every row of
/// <c>tests/version-matrix/traceability-inventory.json</c> claims three things — that a rule was located in the
/// code, that a verdict was reached against the spec text, and that a spec-derived test covers it — and each of
/// those claims is cheap to write and expensive to earn. These tests make the cheap version fail:
/// <c>state</c> is RECOMPUTED here rather than believed, a <c>code-location</c> must name a file and a symbol that
/// still exist, and a <c>test-ref</c> must resolve to a test that is really on disk.
/// </para>
/// <para>
/// ⛔ The referential half is deliberately HERE and nowhere else. <c>record_verdicts.py</c> validates an incoming
/// batch's SHAPE and stops; it does not re-check that references resolve, because that predicate has to keep
/// holding as the tree changes underneath a row recorded sessions ago — which only something running every build
/// can do. One rule, one place (<c>feedback_one_rule_one_place</c>).
/// </para>
/// <para>
/// The vocabulary these tests enforce is not written down here either: verdicts, their <c>resolves</c> flag, their
/// required evidence, the test-ref forms, the per-KIND evidence rules and the anchored-file spaces are read from
/// <c>tests/version-matrix/inventory-schema.json</c>, the same file the Python side reads. What exists twice is
/// the evaluator, not the rule — and <see cref="EveryRowState_IsDerived_NotAsserted"/> is what would catch the two
/// evaluators drifting apart.
/// </para>
/// <para>
/// ⚖ WITHIN this side the predicate exists ONCE. <see cref="AnchorFor"/> and <see cref="IsObservable"/> are the
/// only places that decide what a kind-anchored row's anchor is and whether it names an implementing site;
/// <see cref="DerivedState"/> and <see cref="MisanchoredRows"/> both route through them. Three sites each keying
/// on the same regex independently would have meant that a single typo in <c>implementation-pattern</c> silently
/// reclassified every DOC row while three "independent" assertions stayed green.
/// </para>
/// <para>
/// ⚠ <see cref="TheseChecks_ActuallyFail_OnAFabricatedInventory"/> is not a formality. A gate that has only ever
/// been observed passing is indistinguishable from one that inspects nothing
/// (<c>feedback_green_gates_arent_evidence</c>), and this one will spend most of its life green over rows nobody
/// has touched. It runs every check above against rows built to break it, one defect class at a time.
/// </para>
/// </remarks>
public sealed class SpecTraceabilityInventoryDriftTests
{
    // ── the artifacts under test ─────────────────────────────────────────────────────────────────────

    private sealed record Row(
        string RuleId, string Section, string Kind, int Ordinal, string Subject,
        string Verdict, string CodeLocation, string TestRef, string Derivation, string Editions, string Notes,
        string State);

    private sealed record CatalogRule(string Id, string Section, string Kind, int Ordinal, string Subject);

    private sealed record Verdict(string Name, bool Resolves, string[] Requires);

    private sealed record TestRefForm(string Scheme, string? PathTemplate, string? TestDir, bool SpecDerived);

    /// <summary>
    /// The per-KIND evidence rules (<c>kinds</c> in the schema): what a row of this kind costs, as opposed to
    /// what its VERDICT costs. <c>AnchorTemplate</c> expands the row's own rule-id into the register anchor it
    /// must carry; <c>Implementation</c> is the predicate that decides, from the row's own locations, whether
    /// there is anything in the compiler to observe; <c>AnchorExemptVerdicts</c> names the verdicts that MAY owe
    /// no anchor — whether one actually does is the REGISTER's call, see <see cref="AnchorObliged"/>.
    /// </summary>
    private sealed record KindRule(string AnchorTemplate, Regex? Implementation, string[] AnchorExemptVerdicts);

    /// <summary>One arm of the <c>derivation</c> rule: the shape its <c>Names</c> cell must have, and the
    /// check that shape feeds.</summary>
    private sealed record DerivationArm(Regex Names, string Check);

    /// <summary>
    /// One reason a claimed derivation does not stand — a STABLE CODE plus the sentence a reviewer reads. The
    /// twin of Python's <c>inventory_schema.Refusal</c>.
    /// </summary>
    /// <remarks>
    /// The code exists so <see cref="TheDerivationRule_AgreesWithTheFixtureAndWithPython"/> can compare what the
    /// two engines REFUSED, not merely that they refused: two evaluators rejecting one row for two different
    /// reasons look identical under a boolean, and that is precisely the shape <c>kb/Work/PB315</c>'s
    /// disagreement hid behind for months.
    /// </remarks>
    private sealed record Refusal(string Code, string Message);

    /// <summary>
    /// <c>derivation</c> in the schema — §1.1's owner-signed ALTERNATIVE to a spec-derived test, for a rule
    /// that carries no observable obligation (owner decision <c>kb/Work/PB386</c>, 2026-09-03). The twin of
    /// <c>inventory_schema.Derivation</c>.
    /// </summary>
    /// <param name="Field">the inventory field carrying the claim</param>
    /// <param name="AnchorTemplate">the §8 anchor, COMPUTED from the row's own rule-id</param>
    /// <param name="Heading">the §8 register heading, so the section can be renamed in one place</param>
    /// <param name="Signature">the literal owner signature a determination must carry</param>
    /// <param name="Arms">the three grounds the owner accepted</param>
    /// <param name="Register">docs/CONFORMANCE.md §8, parsed — injectable so the self-test is hermetic</param>
    /// <param name="Undefined">Annex A.2 item → the rule-ids it covers, from the generated artifact</param>
    private sealed record DerivationRule(
        string Field, string AnchorTemplate, string Heading, string Signature,
        IReadOnlyDictionary<string, DerivationArm> Arms,
        IReadOnlyDictionary<string, ConformanceRegister.DerivationRow> Register,
        IReadOnlyDictionary<int, IReadOnlySet<string>> Undefined);

    private sealed record Schema(
        IReadOnlyDictionary<string, Verdict> Verdicts,
        string[] Editions,
        Regex CodeLocationPattern,
        string CodeLocationSeparator,
        string TestRefSeparator,
        IReadOnlyDictionary<string, TestRefForm> TestRefForms,
        bool SpecDerivedRequired,
        Regex[] DisqualifyingMethods,
        IReadOnlyDictionary<string, KindRule> Kinds,
        IReadOnlyDictionary<string, Regex> AnchoredFiles,
        //: The rule-ids docs/CONFORMANCE.md §7 carries a determination for. Not part of the schema FILE — it is
        //: the register the schema POINTS AT, and `AnchorObliged` needs it to tell a WITHDRAWN A.1 item from a
        //: documented decline. Injectable so the fabricated-inventory self-test drives both arms hermetically.
        IReadOnlySet<string> RegisterItems,
        //: §1.1's owner-signed alternative to §1(c)'s test — `null` when the schema declares none, so removing
        //: the object from the JSON closes the door in both engines at once.
        DerivationRule? Derivation,
        //: Every rule-id the catalog knows, for the `indistinguishable-consequent` arm.
        IReadOnlySet<string> CatalogRuleIds);

    private static string Str(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()! : "";

    private static Schema LoadSchema()
    {
        string path = TestRepo.VersionMatrix("inventory-schema.json");
        Assert.True(File.Exists(path), $"inventory schema missing: {path}");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var root = doc.RootElement;

        var verdicts = new Dictionary<string, Verdict>(StringComparer.Ordinal);
        foreach (var p in root.GetProperty("verdicts").EnumerateObject())
        {
            verdicts[p.Name] = new Verdict(
                p.Name,
                p.Value.GetProperty("resolves").GetBoolean(),
                [.. p.Value.GetProperty("requires").EnumerateArray().Select(x => x.GetString()!)]);
        }

        var testRef = root.GetProperty("test-ref");
        var forms = new Dictionary<string, TestRefForm>(StringComparer.Ordinal);
        foreach (var p in testRef.GetProperty("forms").EnumerateObject())
        {
            string tpl = Str(p.Value, "path-template");
            string dir = Str(p.Value, "test-dir");
            forms[p.Name] = new TestRefForm(p.Name, tpl.Length > 0 ? tpl : null, dir.Length > 0 ? dir : null,
                p.Value.GetProperty("spec-derived").GetBoolean());
        }

        var loc = root.GetProperty("code-location");

        // `kinds` and `anchored-files` both carry $-prefixed DOCUMENTATION siblings inside the same object, so
        // both readers skip them by prefix rather than by name — a new comment key must never read as a kind.
        var kinds = new Dictionary<string, KindRule>(StringComparer.Ordinal);
        if (root.TryGetProperty("kinds", out var kindsElem))
        {
            foreach (var p in kindsElem.EnumerateObject())
            {
                if (p.Name.StartsWith('$')) continue;
                string impl = Str(p.Value, "implementation-pattern");
                string[] exempt = p.Value.TryGetProperty("anchor-exempt-verdicts", out var ex)
                    ? [.. ex.EnumerateArray().Select(x => x.GetString()!)]
                    : [];
                kinds[p.Name] = new KindRule(
                    Str(p.Value, "anchor-template"),
                    impl.Length > 0 ? new Regex(impl, RegexOptions.Compiled) : null,
                    exempt);
            }
        }

        var anchored = new Dictionary<string, Regex>(StringComparer.Ordinal);
        if (loc.TryGetProperty("anchored-files", out var anchoredElem))
        {
            foreach (var p in anchoredElem.EnumerateObject())
            {
                if (p.Name.StartsWith('$')) continue;
                anchored[p.Name] = new Regex(p.Value.GetString()!, RegexOptions.Compiled);
            }
        }

        return new Schema(
            verdicts,
            [.. root.GetProperty("editions").EnumerateArray().Select(x => x.GetString()!)],
            new Regex(loc.GetProperty("pattern").GetString()!, RegexOptions.Compiled),
            loc.GetProperty("separator").GetString()!,
            testRef.GetProperty("separator").GetString()!,
            forms,
            testRef.GetProperty("spec-derived-required").GetBoolean(),
            [.. testRef.GetProperty("disqualifying-method-patterns").EnumerateArray()
                .Select(x => new Regex(x.GetString()!, RegexOptions.Compiled))],
            kinds,
            anchored,
            ConformanceRegister.Determinations.Keys.ToHashSet(StringComparer.Ordinal),
            root.TryGetProperty("derivation", out var derivation) ? LoadDerivation(derivation) : null,
            CatalogIds.Value);
    }

    /// <summary>
    /// The <c>derivation</c> rule, with the two artifacts it points at already read: <c>docs/CONFORMANCE.md</c>
    /// §8, and the GENERATED Annex A.2 undefined-element list.
    /// </summary>
    /// <remarks>
    /// ⛔ THE A.2 LIST IS READ FROM DATA, NOT PARSED FROM THE SPEC HERE. <c>scripts/spec/extract_annex_a2.py</c>
    /// owns the extraction and <see cref="AnnexA2UndefinedListDriftTests"/> proves the artifact still equals the
    /// standard; parsing 1.3 MB of spec markdown on this side too would be the second parser this whole schema
    /// exists to avoid, and <see cref="EveryRowState_IsDerived_NotAsserted"/> recomputes 4,311 rows per build.
    /// </remarks>
    private static DerivationRule LoadDerivation(JsonElement d)
    {
        var arms = new Dictionary<string, DerivationArm>(StringComparer.Ordinal);
        foreach (var p in d.GetProperty("arms").EnumerateObject())
        {
            if (p.Name.StartsWith('$')) continue;
            arms[p.Name] = new DerivationArm(
                new Regex(Str(p.Value, "names-pattern"), RegexOptions.Compiled), Str(p.Value, "check"));
        }

        string heading = Str(d, "register-heading");
        string listRel = Str(d, "undefined-list");
        string listPath = Path.Combine(TestRepo.Root, listRel.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(listPath),
            $"the Annex A.2 undefined-element list is missing: {listRel} — "
            + "run python scripts/spec/extract_annex_a2.py");
        var undefined = new Dictionary<int, IReadOnlySet<string>>();
        using (var list = JsonDocument.Parse(File.ReadAllText(listPath)))
        {
            foreach (var it in list.RootElement.GetProperty("items").EnumerateArray())
            {
                undefined[it.GetProperty("item").GetInt32()] =
                    it.GetProperty("rule-ids").EnumerateArray().Select(x => x.GetString()!)
                      .ToHashSet(StringComparer.Ordinal);
            }
        }

        return new DerivationRule(
            Str(d, "field"), Str(d, "anchor-template"), heading, Str(d, "signature"), arms,
            ConformanceRegister.Derivations(heading, Str(d, "register-header-cell")), undefined);
    }

    /// <summary>
    /// Every rule-id the catalog knows, parsed ONCE per test assembly. <c>LoadSchema()</c> runs a dozen times a
    /// gate and the catalog is 4,311 rules; the <c>indistinguishable-consequent</c> arm needs the ids and
    /// nothing else, so re-parsing that file per call is pure cost.
    /// </summary>
    private static readonly Lazy<IReadOnlySet<string>> CatalogIds =
        new(() => LoadCatalog().Select(c => c.Id).ToHashSet(StringComparer.Ordinal));

    private static List<Row> LoadInventory()
    {
        string path = TestRepo.VersionMatrix("traceability-inventory.json");
        Assert.True(File.Exists(path),
            $"inventory missing: {path} — run python scripts/spec/build_inventory.py");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return [.. doc.RootElement.EnumerateArray().Select(e => new Row(
            Str(e, "rule-id"), Str(e, "section"), Str(e, "kind"), e.GetProperty("ordinal").GetInt32(),
            Str(e, "subject"), Str(e, "verdict"), Str(e, "code-location"), Str(e, "test-ref"),
            Str(e, "derivation"), Str(e, "editions"), Str(e, "notes"), Str(e, "state")))];
    }

    private static List<CatalogRule> LoadCatalog()
    {
        string path = TestRepo.Docs("rearchitecture", "spec-rule-catalog.json");
        Assert.True(File.Exists(path),
            $"rule catalog missing: {path} — run python scripts/spec/extract_rule_catalog.py");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return [.. doc.RootElement.GetProperty("rules").EnumerateArray().Select(e => new CatalogRule(
            Str(e, "id"), Str(e, "section"), Str(e, "kind"), e.GetProperty("ordinal").GetInt32(),
            Str(e, "subject")))];
    }

    private static string[] Split(string value, string separator) =>
        [.. value.Split(separator.Trim(), StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)];

    // ── the checks, as pure functions so the self-test can drive the SAME code ───────────────────────

    /// <summary>
    /// The state a row is ENTITLED to, per the schema. Mirrors <c>Schema.state_for</c> in Python — and
    /// <see cref="EveryRowState_IsDerived_NotAsserted"/> is what catches the two drifting apart.
    /// </summary>
    /// <remarks>
    /// The spec-derived clause is separate from <c>requires</c> on purpose: it is what makes
    /// CONFORMS-but-untested expressible (the rule verified against the code, no test yet pinning it), a category
    /// the design doc §3 Phase C names outright.
    /// </remarks>
    private static string DerivedState(Row r, Schema s)
    {
        if (!s.Verdicts.TryGetValue(r.Verdict, out var v) || !v.Resolves) return "GAP";
        if (!v.Requires.All(f => Field(r, f).Length > 0)) return "GAP";
        // A kind that declares evidence rules pays them too, and BOTH are extra COSTS rather than an escape: the
        // computed anchor must be present (a determination filed under another item is not evidence for this
        // row), and something in the compiler must implement it. A DOC row whose only location is its own §7
        // anchor therefore stays a GAP — closing it would widen the design doc §1(a) definition of DONE, which
        // is the owner's to widen and not an agent's (kb/Work PB280 Q2, answered NO on 2026-09-02). ⚠ Both costs
        // are charged only to a row with a determination to point at: a DOC row an A.4 module WITHDREW has no §7
        // row to anchor and nothing implemented to observe (A.1 preamble). It still owes its WITNESS test, which
        // is what keeps such a row a GAP here. A row that is DOCUMENTED-NON-SUPPORT because the OPTIONAL element
        // is not provided (PB280 Q1) is the opposite case — §7 states the non-provision, so it pays both.
        if (AnchorObliged(r, s) && (!IsAnchored(r, s) || !IsObservable(r, s))) return "GAP";
        if (!s.SpecDerivedRequired) return r.TestRef.Length > 0 ? "OK" : "GAP";
        if (Split(r.TestRef, s.TestRefSeparator).Any(x => IsSpecDerived(x, s))) return "OK";
        return DerivationStands(r, s) ? "OK" : "GAP";
    }

    /// <summary>
    /// Does this row carry an owner-signed DERIVATION that stands in place of §1(c)'s test? Mirrors
    /// <c>Schema.derivation_stands</c> in Python.
    /// </summary>
    /// <remarks>
    /// ⛔ IT IS CONSULTED HERE AND ONLY HERE — after the verdict, after <c>requires</c>, after a kind's anchor
    /// and observability costs, and exactly where a spec-derived <c>test-ref</c> would have been. That ORDER is
    /// the encoding of how <c>kb/Work/PB386</c> and <c>PB280</c> Q2 coexist: a DOC row with nothing in the
    /// compiler to observe still computes GAP, derivation or no derivation, because it never reaches this line.
    /// Moving the call earlier would silently reverse an owner decision.
    /// </remarks>
    private static bool DerivationStands(Row r, Schema s) =>
        s.Derivation is { } d && r.Derivation.Trim().Length > 0 && DerivationRefusals(r, s).Count == 0;

    private static string DerivationAnchor(Row r, Schema s) =>
        s.Derivation!.AnchorTemplate.Replace("{rule-id}", r.RuleId, StringComparison.Ordinal);

    /// <summary>
    /// Why this row's derivation does NOT stand — empty when it does. The twin of
    /// <c>inventory_schema.Derivation.refusals</c>, and the six bounds the owner set on
    /// <c>kb/Work/PB386</c> written as code rather than as prose.
    /// </summary>
    /// <remarks>
    /// ⛔ ONE PREDICATE, TWO CALLERS ON EACH SIDE: <c>record_verdicts.validate</c> prints these reasons at record
    /// time and <see cref="DerivedState"/> asks only whether the list is empty. Writing the bounds twice is how a
    /// writer and a gate come to disagree about one rule — the entire content of <c>kb/Work/PB315</c>.
    /// </remarks>
    private static List<Refusal> DerivationRefusals(Row r, Schema s)
    {
        var bad = new List<Refusal>();
        if (s.Derivation is not { } d) return bad;
        string claimed = r.Derivation.Trim();
        if (claimed.Length == 0) return bad;

        string anchor = DerivationAnchor(r, s);
        if (!string.Equals(claimed, anchor, StringComparison.Ordinal))
        {
            bad.Add(new("not-computed-anchor",
                $"{r.RuleId}: derivation '{claimed}' is not the anchor computed from this row's rule-id "
                + $"('{anchor}') — it is derived, never chosen"));
        }

        // ⛔ REFUSAL 1 — the row demonstrably HAS an observable obligation. A derivation asserts that no
        // spec-derived test can exist; a spec-derived test on the same row refutes that outright.
        if (Split(r.TestRef, s.TestRefSeparator).Any(x => IsSpecDerived(x, s)))
        {
            bad.Add(new("has-spec-derived-test",
                $"{r.RuleId}: already carries a SPEC-DERIVED test-ref, so it has an observable obligation and "
                + "may not close on a derivation (design doc §1.1 refusal 1)"));
        }

        // ⛔ REFUSAL 2 — a derivation explains why no test can exist, never why a non-resolving verdict is fine.
        if (!s.Verdicts.TryGetValue(r.Verdict, out var v) || !v.Resolves)
        {
            bad.Add(new("verdict-does-not-resolve",
                $"{r.RuleId}: verdict '{r.Verdict}' does not resolve — a derivation stands in for the covering "
                + "test, not for the verdict"));
        }

        // ⛔ REFUSAL 3 — a kind that owes a register determination must have STATED it first. A DOC row asks the
        // implementor to state a choice, and "nothing to observe" about an unstated choice is unfalsifiable
        // (kb/Work PB280 Q2); DOC-A.1-19 is admitted precisely because §7 states its determination.
        if (AnchorFor(r, s) is not null && !s.RegisterItems.Contains(r.RuleId))
        {
            bad.Add(new("determination-not-stated",
                $"{r.RuleId}: kind {r.Kind} owes a determination in docs/CONFORMANCE.md and none is filed — "
                + "nothing yet for a derivation to be about"));
        }

        string key = anchor[(anchor.IndexOf('#') + 1)..];
        if (!d.Register.TryGetValue(key, out var entry))
        {
            bad.Add(new("no-register-row",
                $"{r.RuleId}: docs/CONFORMANCE.md {d.Heading.Trim('#', ' ')} carries no row keyed '{key}'"));
            return bad;
        }
        if (!string.Equals(entry.Signature, d.Signature, StringComparison.Ordinal))
        {
            bad.Add(new("bad-signature",
                $"{r.RuleId}: determination '{key}' is signed '{entry.Signature}', not '{d.Signature}' — a "
                + "derivation is the OWNER's, and the signature records it"));
        }
        if (!d.Arms.TryGetValue(entry.Arm, out var arm))
        {
            bad.Add(new("unknown-arm",
                $"{r.RuleId}: determination '{key}' names arm '{entry.Arm}', not one of "
                + string.Join('/', d.Arms.Keys.Order())));
            return bad;
        }
        var m = arm.Names.Match(entry.Names);
        if (!m.Success)
        {
            bad.Add(new("names-shape",
                $"{r.RuleId}: determination '{key}' arm {entry.Arm}: 'Names' cell '{entry.Names}' does not "
                + $"match {arm.Names}"));
            return bad;
        }

        switch (arm.Check)
        {
            case "a2-item":
                int item = int.Parse(m.Groups[1].Value);
                if (!d.Undefined.TryGetValue(item, out var covered))
                {
                    bad.Add(new("a2-no-such-item",
                        $"{r.RuleId}: determination '{key}': Annex A.2 has no item {item}"));
                }
                else if (!covered.Contains(r.RuleId))
                {
                    bad.Add(new("a2-does-not-cover",
                        $"{r.RuleId}: determination '{key}': Annex A.2 item {item} does not cover this rule — it "
                        + $"resolves to [{string.Join(", ", covered.Order())}] (design doc §1.1 refusal 4)"));
                }
                break;
            case "stated":
                break;      // the names-pattern IS the shape check for a reviewed argument
            case "rule-exists":
                string named = m.Groups[1].Value;
                if (string.Equals(named, r.RuleId, StringComparison.Ordinal))
                {
                    bad.Add(new("self-indistinguishable",
                        $"{r.RuleId}: determination '{key}': a rule cannot be indistinguishable from itself"));
                }
                else if (!s.CatalogRuleIds.Contains(named))
                {
                    bad.Add(new("unknown-rule",
                        $"{r.RuleId}: determination '{key}': names '{named}', not a rule in the catalog"));
                }
                break;
            default:
                bad.Add(new("unknown-check",
                    $"{r.RuleId}: determination '{key}': arm {entry.Arm} declares check '{arm.Check}', which "
                    + "this evaluator does not implement — the schema and the gate disagree"));
                break;
        }

        return bad;
    }

    /// <summary>Every live row whose <c>derivation</c> claim does not stand — the gate over §1.1.</summary>
    private static List<string> BadDerivations(IEnumerable<Row> rows, Schema s) =>
        [.. rows.Where(r => r.Derivation.Trim().Length > 0)
                .SelectMany(r => DerivationRefusals(r, s)).Select(x => $"[{x.Code}] {x.Message}")];

    /// <summary>
    /// The register anchor this row's KIND obliges, COMPUTED from the row's own rule-id — or <c>null</c> when the
    /// kind declares no rule. Mirrors <c>Schema.anchor_for</c> in Python.
    /// </summary>
    /// <remarks>
    /// ⛔ Written ONCE on this side and read by <see cref="DerivedState"/> and <see cref="MisanchoredRows"/>, so
    /// the two cannot disagree about what a row's anchor is. The anchor being a FUNCTION of the row is the whole
    /// mechanism: a determination cannot be filed under the wrong item the way <c>kb/Work/A11</c> recorded — the
    /// §15.3.3.2 fractional-seconds determination sat under item 87, whose obligation is FORMATTED-CURRENT-DATE's
    /// accuracy, because the NUMBER was inherited and never re-derived.
    /// </remarks>
    private static string? AnchorFor(Row r, Schema s) =>
        s.Kinds.TryGetValue(r.Kind, out var k) && k.AnchorTemplate.Length > 0
            ? k.AnchorTemplate.Replace("{rule-id}", r.RuleId, StringComparison.Ordinal)
            : null;

    private static bool IsAnchored(Row r, Schema s) =>
        AnchorFor(r, s) is { } anchor
        && Split(r.CodeLocation, s.CodeLocationSeparator).Contains(anchor, StringComparer.Ordinal);

    /// <summary>
    /// Does this row OWE the anchor <see cref="AnchorFor"/> computes — i.e. is there a determination to point
    /// at? Mirrors <c>Schema.anchor_obliged</c> in Python.
    /// </summary>
    /// <remarks>
    /// <para>
    /// CONFORMS, PARTIAL and DIVERGES each assert something about what the register says, so each owes its own §7
    /// row unconditionally.
    /// </para>
    /// <para>
    /// ⛔ THE EXEMPTION IS KEYED ON "HAS A §7 ROW", NOT ON THE VERDICT (kb/Work PB280 Q1, 2026-09-02).
    /// DOCUMENTED-NON-SUPPORT has two grounds on a DOC row and they differ exactly here. An item an A.4 module
    /// WITHDREW — the conditioning facility is not implemented, so A.1's preamble makes it "not required if the
    /// optional or processor-dependent feature is not implemented" — has no §7 row, and nothing to anchor. An
    /// OPTIONAL element §7 records as "Not provided." HAS one, because stating the non-provision IS the
    /// determination, and owes its anchor exactly as CONFORMS does. Keyed on the verdict, those rows would be
    /// excused their own determination and the register audit would stop holding their witness in agreement with
    /// §7's <c>Pinned by</c> — silently, at the moment the witness landed. <c>anchor-exempt-verdicts</c>
    /// therefore names the verdicts that MAY be exempt, and the register decides.
    /// </para>
    /// </remarks>
    private static bool AnchorObliged(Row r, Schema s) =>
        AnchorFor(r, s) is not null
        && s.Kinds.TryGetValue(r.Kind, out var k)
        && (!k.AnchorExemptVerdicts.Contains(r.Verdict, StringComparer.Ordinal)
            || s.RegisterItems.Contains(r.RuleId));

    /// <summary>
    /// Does this row name a site in the compiler through which a program could observe the determination?
    /// </summary>
    /// <remarks>
    /// The predicate is the row's OWN <c>code-location</c> list read through its kind's
    /// <c>implementation-pattern</c> — the greenfield <c>src/Cobol.Net.*</c> predicate
    /// <c>scripts/spec/audit_annex_a1.py</c>'s source sweep already applies. A PATH is falsifiable by an
    /// independent reader who goes looking for the code; an opinion is not. A kind that declares no pattern is
    /// vacuously observable, so adding a kind never tightens an existing one.
    /// </remarks>
    private static bool IsObservable(Row r, Schema s)
    {
        if (!s.Kinds.TryGetValue(r.Kind, out var k) || k.Implementation is null) return true;
        string? anchor = AnchorFor(r, s);
        return Split(r.CodeLocation, s.CodeLocationSeparator)
            .Any(x => !string.Equals(x, anchor, StringComparison.Ordinal) && k.Implementation.IsMatch(x));
    }

    /// <summary>
    /// A row whose KIND obliges a register anchor and whose locations do not carry the anchor its own rule-id
    /// computes — either none at all, or, the <c>kb/Work/A11</c> shape, the anchor of a DIFFERENT item.
    /// </summary>
    private static List<string> MisanchoredRows(IEnumerable<Row> rows, Schema s) =>
        [.. from r in rows
            where (r.Verdict.Length > 0 || r.CodeLocation.Length > 0)
                  && AnchorObliged(r, s) && !IsAnchored(r, s)
            select $"{r.RuleId}: kind {r.Kind} requires the computed register anchor '{AnchorFor(r, s)}' among "
                   + $"its code-location(s) [{r.CodeLocation}] — the anchor is derived from the rule-id, never "
                   + "chosen, so a determination filed under another item cannot be spelled"];

    /// <summary>
    /// Any <c>code-location</c> naming a file whose fragments are a NAMED ANCHOR SPACE, with a fragment outside
    /// that space — or with no fragment at all.
    /// </summary>
    /// <remarks>
    /// ⛔ This is the check that turns <c>docs/CONFORMANCE.md#7</c> red. The resolver below satisfies a
    /// <c>#Symbol</c> by a word search over the file body: exact for a C# identifier, worthless for a number —
    /// <c>#7</c> matches the digit 7 anywhere in a 790-line document, and five live rows were <c>state: OK</c> on
    /// that basis. A BARE citation of an anchored file is the same defect one step weaker, resolving on
    /// <c>File.Exists</c> alone, so it is a violation too; three more live rows were doing it.
    /// </remarks>
    private static List<string> BadAnchorFragments(IEnumerable<Row> rows, Schema s) =>
        [.. from r in rows
            where r.CodeLocation.Length > 0
            from loc in Split(r.CodeLocation, s.CodeLocationSeparator)
            let parts = loc.Split('#', 2)
            where s.AnchoredFiles.ContainsKey(parts[0])
            let rx = s.AnchoredFiles[parts[0]]
            where parts.Length == 1 || !rx.IsMatch(parts[1])
            select $"{r.RuleId}: code-location '{loc}' — '{parts[0]}' is an anchored file, so its fragment must "
                   + $"match {rx} ({(parts.Length == 1 ? "there is no fragment at all" : $"got '{parts[1]}'")})"];

    private static string Field(Row r, string name) => name switch
    {
        "verdict" => r.Verdict,
        "code-location" => r.CodeLocation,
        "test-ref" => r.TestRef,
        "derivation" => r.Derivation,
        "editions" => r.Editions,
        "notes" => r.Notes,
        _ => throw new InvalidOperationException(
            $"inventory-schema.json requires a field '{name}' that no inventory row has — "
            + "add it to the row schema in build_inventory.py and to Field() here."),
    };

    private static List<string> BadVerdicts(IEnumerable<Row> rows, Schema s) =>
        [.. rows.Where(r => r.Verdict.Length > 0 && !s.Verdicts.ContainsKey(r.Verdict))
                .Select(r => $"{r.RuleId}: verdict '{r.Verdict}' is not in the schema vocabulary")];

    private static List<string> BadStates(IEnumerable<Row> rows, Schema s) =>
        [.. rows.Where(r => r.State != DerivedState(r, s))
                .Select(r => $"{r.RuleId}: state '{r.State}' but the schema derives '{DerivedState(r, s)}' "
                             + $"from verdict '{r.Verdict}'")];

    private static List<string> MissingEvidence(IEnumerable<Row> rows, Schema s) =>
        [.. from r in rows
            where s.Verdicts.TryGetValue(r.Verdict, out _)
            let v = s.Verdicts[r.Verdict]
            from f in v.Requires
            where Field(r, f).Length == 0
            select $"{r.RuleId}: verdict {r.Verdict} requires a non-empty '{f}'"];

    /// <summary>
    /// Is this one ref an acceptable BASIS for closing a row — spec-derived, not a differential?
    /// </summary>
    /// <remarks>
    /// Two ways to fail. The FORM can be inherently differential (a NIST CCVS golden, a characterization
    /// snapshot). Or the form can be spec-derived-capable while the specific test is not: an xUnit test named
    /// <c>*_MatchesLegacy</c> says in its own name that its expected value came from the former legacy engine, which
    /// CLAUDE.md rule 1 forbids as authority. Keying on the repo's own naming convention is narrow, but it is
    /// exact where it applies, and it fails LOUDLY rather than quietly accepting a differential as coverage.
    /// </remarks>
    private static bool IsSpecDerived(string reference, Schema s)
    {
        int colon = reference.IndexOf(':');
        string scheme = colon < 0 ? reference : reference[..colon];
        if (!s.TestRefForms.TryGetValue(scheme, out var form) || !form.SpecDerived) return false;
        string body = colon < 0 ? "" : reference[(colon + 1)..].Trim();
        string method = body[(body.LastIndexOf('.') + 1)..];
        return !s.DisqualifyingMethods.Any(rx => rx.IsMatch(method));
    }

    /// <summary>
    /// A row that is CLOSED (state OK) but rests only on differential evidence. <see cref="DerivedState"/> already
    /// refuses to close such a row, so in a healthy tree this is unreachable — it is kept as a named, independent
    /// assertion of the RULE, so that a future bug in the derivation cannot quietly close rows on NIST goldens.
    /// </summary>
    private static List<string> DifferentialOnlyCoverage(IEnumerable<Row> rows, Schema s)
    {
        if (!s.SpecDerivedRequired) return [];
        // ⚠ A row closed on an owner-signed DERIVATION is not "covered by a differential" — it is covered by
        // nothing, on purpose, because §1.1 says no test can exist for it. Excluding it here is the SECOND ARM
        // of the same rule `DerivedState` implements, and forgetting it would have turned every PB386 closure
        // red under a check about NIST goldens (`feedback_two_arm_dispatch`). `DerivationStands` is the same
        // predicate, so the two arms cannot come to disagree.
        return [.. from r in rows
                   where r.State == "OK"
                   let refs = Split(r.TestRef, s.TestRefSeparator)
                   where !refs.Any(x => IsSpecDerived(x, s)) && !DerivationStands(r, s)
                   select $"{r.RuleId}: state OK but covered ONLY by non-spec-derived test(s) "
                          + $"[{string.Join(", ", refs)}] — a differential cannot close a row (design doc §1c)"];
    }

    private static List<string> BadEditions(IEnumerable<Row> rows, Schema s) =>
        [.. from r in rows
            where r.Editions.Length > 0
            from e in Split(r.Editions, ",")
            where !s.Editions.Contains(e)
            select $"{r.RuleId}: editions names '{e}', not one of {string.Join('/', s.Editions)}"];

    private static List<string> UnresolvedCodeLocations(IEnumerable<Row> rows, Schema s, string root)
    {
        var text = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var bad = new List<string>();
        foreach (var r in rows.Where(r => r.CodeLocation.Length > 0))
        {
            foreach (string loc in Split(r.CodeLocation, s.CodeLocationSeparator))
            {
                if (!s.CodeLocationPattern.IsMatch(loc))
                {
                    bad.Add($"{r.RuleId}: code-location '{loc}' is not '<repo-relative-path>[#Symbol]'");
                    continue;
                }

                string[] parts = loc.Split('#', 2);
                string file = Path.Combine(root, parts[0].Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(file))
                {
                    bad.Add($"{r.RuleId}: code-location '{loc}' — no such file");
                    continue;
                }
                if (parts.Length == 1) continue;

                if (!text.TryGetValue(file, out string? body))
                    text[file] = body = File.ReadAllText(file);
                // A symbol, not a line number: it survives every edit that does not delete the thing it names,
                // which is the only event this gate should go red for.
                if (!Regex.IsMatch(body, $@"\b{Regex.Escape(parts[1])}\b"))
                    bad.Add($"{r.RuleId}: code-location '{loc}' — '{parts[1]}' is no longer in that file");
            }
        }
        return bad;
    }

    private static List<string> UnresolvedTestRefs(IEnumerable<Row> rows, Schema s, string root)
    {
        // Both caches matter at full scale: 3,790 rows will eventually cite a few hundred distinct test classes,
        // and the uncached form re-globs the directory AND re-reads the same .cs file once per citing row.
        var sources = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        var text = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var bad = new List<string>();
        foreach (var r in rows.Where(r => r.TestRef.Length > 0))
        {
            foreach (string reference in Split(r.TestRef, s.TestRefSeparator))
            {
                int colon = reference.IndexOf(':');
                string scheme = colon < 0 ? reference : reference[..colon];
                string body = colon < 0 ? "" : reference[(colon + 1)..].Trim();

                if (!s.TestRefForms.TryGetValue(scheme, out var form))
                {
                    bad.Add($"{r.RuleId}: test-ref '{reference}' — unknown form '{scheme}', expected one of "
                            + string.Join('/', s.TestRefForms.Keys.Order()));
                    continue;
                }
                if (body.Length == 0)
                {
                    bad.Add($"{r.RuleId}: test-ref '{reference}' has an empty body");
                    continue;
                }

                if (form.PathTemplate is { } template)
                {
                    string rel = template.Replace("{0}", body);
                    if (!File.Exists(Path.Combine(root, rel.Replace('/', Path.DirectorySeparatorChar))))
                        bad.Add($"{r.RuleId}: test-ref '{reference}' — no such case, expected {rel}");
                    continue;
                }

                int dot = body.LastIndexOf('.');
                if (dot <= 0 || dot == body.Length - 1)
                {
                    bad.Add($"{r.RuleId}: test-ref '{reference}' — expected <Class>.<Method>");
                    continue;
                }
                string cls = body[..dot], method = body[(dot + 1)..];
                string dir = Path.Combine(root, form.TestDir!.Replace('/', Path.DirectorySeparatorChar));
                if (!sources.TryGetValue(cls + '|' + dir, out string[]? files))
                    sources[cls + '|' + dir] = files = Directory.Exists(dir)
                        ? Directory.GetFiles(dir, cls + ".cs", SearchOption.AllDirectories) : [];

                if (files.Length == 0)
                {
                    bad.Add($"{r.RuleId}: test-ref '{reference}' — no {cls}.cs under {form.TestDir}");
                    continue;
                }
                if (!files.Any(f =>
                {
                    if (!text.TryGetValue(f, out string? src)) text[f] = src = File.ReadAllText(f);
                    return Regex.IsMatch(src, $@"\b{Regex.Escape(method)}\s*\(");
                }))
                {
                    bad.Add($"{r.RuleId}: test-ref '{reference}' — {cls}.cs has no method '{method}'");
                }
            }
        }
        return bad;
    }

    private static string Report(string what, List<string> violations, int scanned)
    {
        string head = $"{violations.Count} {what} over {scanned} inventory row(s):";
        return head + Environment.NewLine
            + string.Join(Environment.NewLine, violations.Take(20).Select(v => "    " + v))
            + (violations.Count > 20 ? $"{Environment.NewLine}    … and {violations.Count - 20} more" : "");
    }

    // ── the gate ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The inventory covers the catalog EXACTLY — no orphan row, no unreviewed rule hidden by omission.</summary>
    [Fact]
    public void Inventory_CoversTheCatalog_Exactly()
    {
        var rows = LoadInventory();
        var catalog = LoadCatalog();

        var inRows = rows.Select(r => r.RuleId).ToHashSet(StringComparer.Ordinal);
        var inCatalog = catalog.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);

        var missing = inCatalog.Except(inRows).Order().Take(10).ToList();
        var orphan = inRows.Except(inCatalog).Order().Take(10).ToList();
        Assert.True(missing.Count == 0 && orphan.Count == 0,
            $"inventory {rows.Count} rows vs catalog {catalog.Count} rules — "
            + $"rules with no row: [{string.Join(", ", missing)}]; rows with no rule: [{string.Join(", ", orphan)}] "
            + "— re-run python scripts/spec/build_inventory.py");
        Assert.Equal(rows.Count, rows.Select(r => r.RuleId).Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>A row's catalog-owned fields are REGENERATED, so a hand-edit to one is drift, not data.</summary>
    [Fact]
    public void EveryRow_MatchesItsCatalogEntry()
    {
        var catalog = LoadCatalog().ToDictionary(c => c.Id, StringComparer.Ordinal);
        var bad = new List<string>();
        foreach (var r in LoadInventory())
        {
            if (!catalog.TryGetValue(r.RuleId, out var c)) continue;   // covered by the coverage test
            if (r.Section != c.Section || r.Kind != c.Kind || r.Ordinal != c.Ordinal || r.Subject != c.Subject)
                bad.Add($"{r.RuleId}: row ({r.Section}/{r.Kind}/{r.Ordinal}) disagrees with the catalog "
                        + $"({c.Section}/{c.Kind}/{c.Ordinal})");
        }
        Assert.True(bad.Count == 0, Report("row(s) disagree with the catalog", bad, catalog.Count));
    }

    /// <summary>Every recorded verdict is one the schema defines.</summary>
    [Fact]
    public void EveryVerdict_IsInTheSchemaVocabulary()
    {
        var rows = LoadInventory();
        var bad = BadVerdicts(rows, LoadSchema());
        Assert.True(bad.Count == 0, Report("unknown verdict(s)", bad, rows.Count));
    }

    /// <summary>
    /// <c>state</c> is DERIVED from the verdict and its evidence — never taken at face value. This is the test that
    /// makes the burn-down honest: without it, closing a GAP costs one hand-edited string.
    /// </summary>
    [Fact]
    public void EveryRowState_IsDerived_NotAsserted()
    {
        var rows = LoadInventory();
        var bad = BadStates(rows, LoadSchema());
        Assert.True(bad.Count == 0, Report("row(s) whose stored state is not the derived one", bad, rows.Count));
    }

    /// <summary>Each verdict carries the evidence the schema says that verdict requires.</summary>
    [Fact]
    public void EveryVerdict_CarriesTheEvidenceItRequires()
    {
        var rows = LoadInventory();
        var bad = MissingEvidence(rows, LoadSchema());
        Assert.True(bad.Count == 0, Report("verdict(s) missing required evidence", bad, rows.Count));
    }

    /// <summary>
    /// ⛔ A row closes on a SPEC-DERIVED test or it does not close. The definition of DONE
    /// (<c>DESIGN-spec-conformance-review.md</c> §1c) requires the expected value to be computed from the spec,
    /// and CLAUDE.md rule 1 makes NIST, the legacy and GnuCOBOL regression nets rather than authority — a
    /// differential is structurally blind to a violation both sides share, which is exactly the case a
    /// conformance review exists to catch.
    /// </summary>
    [Fact]
    public void EveryResolvedRow_CitesASpecDerivedTest()
    {
        var rows = LoadInventory();
        var bad = DifferentialOnlyCoverage(rows, LoadSchema());
        Assert.True(bad.Count == 0, Report("row(s) closed on differential evidence alone", bad, rows.Count));
    }

    /// <summary>An <c>editions</c> field names only the four editions the compiler implements.</summary>
    [Fact]
    public void EveryEditionsField_NamesOnlyKnownEditions()
    {
        var rows = LoadInventory();
        var bad = BadEditions(rows, LoadSchema());
        Assert.True(bad.Count == 0, Report("bad edition name(s)", bad, rows.Count));
    }

    /// <summary>
    /// ⛔ Every row of a kind that carries a register anchor anchors ITS OWN register entry. The anchor is
    /// computed from the row's rule-id, so this fails on the <c>kb/Work/A11</c> shape — a determination filed
    /// under a neighbouring item — which no amount of reading the prose had caught.
    /// </summary>
    [Fact]
    public void EveryKindAnchoredRow_AnchorsItsOwnRegisterEntry()
    {
        var rows = LoadInventory();
        var s = LoadSchema();
        Assert.True(s.Kinds.Count > 0,
            "inventory-schema.json declares no `kinds` — this gate would then measure nothing, so the schema, "
            + "not the gate, is what changed.");
        var bad = MisanchoredRows(rows, s);
        Assert.True(bad.Count == 0, Report("misanchored row(s)", bad, rows.Count));
    }

    /// <summary>
    /// Every fragment on a file whose fragments are a named anchor space is a legal anchor of that space — and an
    /// anchored file is never cited bare.
    /// </summary>
    [Fact]
    public void EveryRegisterAnchor_IsAWellFormedAnchorOfItsFile()
    {
        var rows = LoadInventory();
        var s = LoadSchema();
        Assert.True(s.AnchoredFiles.Count > 0,
            "inventory-schema.json lists no `anchored-files` — the mechanism that turns `#7` red is gone.");
        var bad = BadAnchorFragments(rows, s);
        Assert.True(bad.Count == 0, Report("ill-formed register anchor(s)", bad, rows.Count));
    }

    /// <summary>
    /// ⛔ Every row that closes on an owner-signed DERIVATION still stands under its own arm. The A.2 arm is
    /// re-resolved against the standard's own undefined-element list on every build; the two argument arms are
    /// re-checked for shape and for the existence of what they name.
    /// </summary>
    /// <remarks>
    /// A derivation moves the v1.0 burn-down without a test, which is the whole reason <c>kb/Work/PB386</c>'s
    /// second stated cost is that it be CHECKABLE: "or the GAP metric becomes cheaper to move than the work it
    /// stands for". This is where that cost is charged continuously, as the register, the catalog and the spec
    /// change underneath a determination recorded sessions ago.
    /// </remarks>
    [Fact]
    public void EveryDerivation_StandsUnderItsOwnArm()
    {
        var rows = LoadInventory();
        var s = LoadSchema();
        Assert.True(s.Derivation is not null,
            "inventory-schema.json declares no `derivation` — §1.1's evidence kind is gone, so this gate would "
            + "measure nothing and the schema, not the gate, is what changed.");
        var bad = BadDerivations(rows, s);
        Assert.True(bad.Count == 0, Report("row(s) whose derivation does not stand", bad, rows.Count));
    }

    /// <summary>
    /// ⛔ THE POPULATION GUARD. The rows closed on a derivation rather than a test are enumerated HERE, so the
    /// door the owner opened cannot widen by accident.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>kb/Work/PB386</c>'s third stated cost: "every existing CONFORMS-but-untested row becomes a candidate
    /// for the escape, so the population must be MEASURED before the door opens, not after." It was — eight
    /// rows, the whole CONFORMS-but-untested band except <c>DOC-A.1-93</c>, which is a real defect
    /// (<c>kb/Work/PB383</c>). This list is the measurement, held.
    /// </para>
    /// <para>
    /// ⚖ THIS IS THE ONE HAND LIST IN THE MECHANISM, AND IT IS DELIBERATE. Every CHECKER above is data-driven —
    /// the arms, their shapes, the A.2 resolution and the register are all read rather than typed — precisely so
    /// the next case costs one register row. The guard is the opposite by design: adding a ninth row must be an
    /// EDIT SOMEONE MAKES, in a file a reviewer reads, or "checkable" degrades into "checked once".
    /// </para>
    /// </remarks>
    [Fact]
    public void TheRowsClosedOnADerivation_AreTheEnumeratedPopulation()
    {
        string[] population =
        [
            // ⚙ GR-14.9.30.4-23 LEFT on 2026-09-24 (wave 59 H, kb/Work R43 item 3): the READ reel-swap rule takes
            // its WRITE twin GR-14.9.51.4-24's disposition — DOCUMENTED-NON-SUPPORT, because its trigger exists
            // only for a multi-reel/unit file no connector is — and its §8 derivation was retired with it.
            "DOC-A.1-19", "GR-14.9.11.4-11", "GR-14.9.30.4-20", "GR-14.9.30.4-3",
            "GR-14.9.34.4-2", "GR-14.9.5.4-11", "RV-15.50.4-9",
            // ⚙ The NINTH, added deliberately (kb/Work PB235, 2026-09-05) — this edit is the point of the list.
            // §14.9.6.4 GR3 symbol e's unit-media branch 1. Its antecedent needs a physical file in §14.9.6.4
            // GR2 category (b) or (c); no connector answers either, the placement is FORCED by §9.1.13.2 item
            // 6's non-reel/unit definition of the '07' this compiler reports, and it is documented in
            // docs/CONFORMANCE.md §7 (Annex A.1 item 24). It joins the SAME medium determination the owner
            // already signed for GR-14.9.30.4-20 and -23 — this is that argument reaching the CLOSE rule it
            // was originally about, not a new ground. CloseTable14Tests reds if the placement ever changes.
            "GR-14.9.6.4-L2.1",
            // ⚙ The TENTH, ELEVENTH and TWELFTH, added deliberately (owner decisions Q25–Q27, 2026-09-09, after a
            // plain-language brief; DEVLOG 1577). GR-14.9.27.4-12: OPEN's rewindable-medium rule, arm
            // unpopulatable-antecedent over the SAME closed medium set as GR-14.9.6.4-L2.1 (§14.9.27.4 GR11 and GR12
            // partition the media; every connector answers non-unit, forced by §9.1.13.2 item 6; kb/Work PB317).
            // GR-14.9.20.4-9 and GR-14.9.28.4-2: the MECHANICAL undefined-A.2 arm — Annex A.2 items 20
            // (INITIALIZE onto overlapping storage) and 37 (overlapping PERFORM ranges) resolve to exactly these
            // rule-ids, so no expected value exists to test for (kb/Work PB370).
            "GR-14.9.27.4-12", "GR-14.9.20.4-9", "GR-14.9.28.4-2",
            // ⚙ The THIRTEENTH, added deliberately (kb/Work PB245, wave 40, 2026-09-19) — and it is the first
            // whose vacuity is the STANDARD'S rather than this implementation's. RV-15.4.1-4 is §15.4.1 NOTE 1
            // item 4, "The result is explicitly implementor-defined", whose normative twin is the same clause's
            // rule 2 c). NOTE 1's own lead-in scopes item 4 to *the result of an equivalent arithmetic
            // expression*, so the set it governs is the §15 functions that HAVE one — closed, enumerable, and
            // enumerated MECHANICALLY rather than recalled: the fourteen clauses containing "The equivalent
            // arithmetic expression is as follows" (ABS, ANNUITY, COMBINED-DATETIME, DATE-TO-YYYYMMDD,
            // DAY-TO-YYYYDDD, FACTORIAL, MEAN, PRESENT-VALUE, SECONDS-FROM-FORMATTED-TIME, SIGN,
            // STANDARD-DEVIATION, SUM, VARIANCE, YEAR-TO-YYYY). NOT ONE of those fourteen function clauses
            // contains "implementor-defined", "implementor defines" or "defined by the implementor" anywhere in
            // it, so the antecedent is false for every EAE the standard defines and §1(c)'s covering test is
            // unmeetable rather than unwritten. ⚠ Unlike the medium- and device-set rows above, this one cannot
            // be voided by a later implementation choice — only by a future edition making some EAE-bearing
            // function's returned value explicitly implementor-defined, at which point the rule-digest changes
            // and the row is re-adjudicated by construction. docs/CONFORMANCE.md §8 DRV-RV-15.4.1-4 carries the
            // enumeration itself so the argument is re-checkable rather than believed.
            "RV-15.4.1-4",
            // ⚙ The FOURTEENTH and FIFTEENTH, added deliberately (owner decision kb/Work R43 item 4, 2026-09-24,
            // extending PB386; answers PB468 Q9) — the first rows of the NEW `definitional` arm. §4.4 1) and 2)
            // DEFINE the conforming run unit, a property of the USER's program: their subject is the run unit,
            // they contain no "shall", and every obligation they allude to is its own inventory row. They are the
            // only such rows in the catalog (§4.3, the conforming compilation group, has none).
            "GR-4.4-1", "GR-4.4-2",
        ];

        var s = LoadSchema();
        var closed = LoadInventory()
            .Where(r => r.Derivation.Trim().Length > 0 && DerivedState(r, s) == "OK")
            .Select(r => r.RuleId).Order(StringComparer.Ordinal).ToList();

        Assert.True(population.Order(StringComparer.Ordinal).SequenceEqual(closed, StringComparer.Ordinal),
            "the set of rows closed on an owner-signed derivation is not the measured population.\n"
            + $"    expected: [{string.Join(", ", population.Order(StringComparer.Ordinal))}]\n"
            + $"    actual  : [{string.Join(", ", closed)}]\n"
            + "    A row added here is a row the burn-down moves without a test. It needs an owner-signed §8 "
            + "determination AND a deliberate edit to this list (kb/Work PB386).");
    }

    /// <summary>
    /// ⛔ THE CROSS-LANGUAGE PARITY GATE FOR §1.1. This engine, the Python engine and the recorded expectation
    /// must give the SAME answer — state and refusal codes — for every case in
    /// <c>tests/version-matrix/derivation-parity-cases.json</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>kb/Work/PB315</c> is the note recording what happens when <c>Schema.state_for</c> and
    /// <see cref="DerivedState"/> read one rule differently: the disagreement was UNOBSERVABLE until a row first
    /// exercised the divergent branch, and it then looked exactly like a correct batch. The owner's first stated
    /// cost for opening this door was that both engines learn the evidence kind in the same change set — this
    /// is what makes that claim falsifiable rather than a sentence in a commit message.
    /// </para>
    /// <para>
    /// ⚠ The fixture's world is FABRICATED on purpose. All eight live determinations are correct, so an arm
    /// cannot be falsified against the real document, and a fixture built from it would pass for the wrong
    /// reason (<c>feedback_probe_the_shape_the_subject_hides</c>).
    /// </para>
    /// </remarks>
    [Fact]
    public void TheDerivationRule_AgreesWithTheFixtureAndWithPython()
    {
        string path = TestRepo.VersionMatrix("derivation-parity-cases.json");
        Assert.True(File.Exists(path), $"the derivation parity fixture is missing: {path}");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var world = doc.RootElement.GetProperty("world");
        var cases = doc.RootElement.GetProperty("cases").EnumerateArray().ToList();
        Assert.True(cases.Count > 10,
            $"the parity fixture carries {cases.Count} case(s) — too few to be measuring the rule");

        var baseSchema = LoadSchema();
        Assert.True(baseSchema.Derivation is not null,
            "inventory-schema.json declares no `derivation` — the schema, not this gate, is what changed.");

        var mine = new List<(string Name, string State, string[] Refusals)>();
        foreach (var c in cases)
        {
            var s = WorldSchema(baseSchema, world, c);
            var r = FixtureRow(c.GetProperty("row"));
            mine.Add((c.GetProperty("name").GetString()!, DerivedState(r, s),
                      [.. DerivationRefusals(r, s).Select(x => x.Code).Order(StringComparer.Ordinal)]));
        }

        var bad = new List<string>();
        for (int i = 0; i < cases.Count; i++)
        {
            string want = cases[i].GetProperty("state").GetString()!;
            string[] wantRefusals =
                [.. cases[i].GetProperty("refusals").EnumerateArray().Select(x => x.GetString()!)
                    .Order(StringComparer.Ordinal)];
            if (mine[i].State != want)
                bad.Add($"'{mine[i].Name}': this engine derives {mine[i].State}, the fixture says {want}");
            if (!mine[i].Refusals.SequenceEqual(wantRefusals, StringComparer.Ordinal))
            {
                bad.Add($"'{mine[i].Name}': this engine refuses [{string.Join(", ", mine[i].Refusals)}], the "
                        + $"fixture says [{string.Join(", ", wantRefusals)}]");
            }
        }

        // …and the OTHER engine, run for real rather than assumed. A fixture both sides are compared against is
        // only half the check: this is the half that would have caught PB315.
        var run = PythonInstrument.Run(TestRepo.Scripts("spec", "audit_derivations.py"), "--parity", "--json");
        string? line = run.Stdout.Split('\n').Select(l => l.Trim())
            .FirstOrDefault(l => l.StartsWith("JSON ", StringComparison.Ordinal));
        Assert.True(line is not null,
            $"audit_derivations.py --parity --json emitted no JSON line.\n{run.Stdout}\n{run.Stderr}");
        using var py = JsonDocument.Parse(line!["JSON ".Length..]);
        var theirs = py.RootElement.GetProperty("parity").EnumerateArray().ToList();
        Assert.Equal(cases.Count, theirs.Count);
        for (int i = 0; i < cases.Count; i++)
        {
            string state = theirs[i].GetProperty("state").GetString()!;
            string[] refusals =
                [.. theirs[i].GetProperty("refusals").EnumerateArray().Select(x => x.GetString()!)
                    .Order(StringComparer.Ordinal)];
            if (state != mine[i].State)
                bad.Add($"'{mine[i].Name}': Python derives {state}, this engine {mine[i].State}");
            if (!refusals.SequenceEqual(mine[i].Refusals, StringComparer.Ordinal))
            {
                bad.Add($"'{mine[i].Name}': Python refuses [{string.Join(", ", refusals)}], this engine "
                        + $"[{string.Join(", ", mine[i].Refusals)}]");
            }
        }
        Assert.Equal(0, run.ExitCode);

        Assert.True(bad.Count == 0, Report("derivation parity disagreement(s)", bad, cases.Count));
    }

    /// <summary>The schema a parity case is evaluated under — the fabricated world, plus any per-case override.</summary>
    private static Schema WorldSchema(Schema s, JsonElement world, JsonElement c)
    {
        JsonElement Field(string name) =>
            c.TryGetProperty("world-overrides", out var o) && o.TryGetProperty(name, out var v)
                ? v : world.GetProperty(name);

        var register = new Dictionary<string, ConformanceRegister.DerivationRow>(StringComparer.Ordinal);
        foreach (var r in Field("register").EnumerateArray())
        {
            register[Str(r, "key")] = new ConformanceRegister.DerivationRow(
                Str(r, "key"), Str(r, "arm"), Str(r, "names"), Str(r, "argument"), Str(r, "signature"));
        }

        var undefined = new Dictionary<int, IReadOnlySet<string>>();
        foreach (var p in Field("undefined").EnumerateObject())
        {
            if (p.Name.StartsWith('$')) continue;
            undefined[int.Parse(p.Name)] =
                p.Value.EnumerateArray().Select(x => x.GetString()!).ToHashSet(StringComparer.Ordinal);
        }

        return s with
        {
            Derivation = s.Derivation! with { Register = register, Undefined = undefined },
            CatalogRuleIds = Field("catalog-rule-ids").EnumerateArray().Select(x => x.GetString()!)
                .ToHashSet(StringComparer.Ordinal),
            RegisterItems = Field("register-items").EnumerateArray().Select(x => x.GetString()!)
                .ToHashSet(StringComparer.Ordinal),
        };
    }

    private static Row FixtureRow(JsonElement e) => new(
        Str(e, "rule-id"), Str(e, "section"), Str(e, "kind"),
        e.TryGetProperty("ordinal", out var o) ? o.GetInt32() : 0, Str(e, "subject"),
        Str(e, "verdict"), Str(e, "code-location"), Str(e, "test-ref"), Str(e, "derivation"),
        Str(e, "editions"), Str(e, "notes"), Str(e, "state"));

    /// <summary>Every traceability link still points at code that exists — the link is the whole point of it.</summary>
    [Fact]
    public void EveryCodeLocation_ResolvesInTheTree()
    {
        var rows = LoadInventory();
        var bad = UnresolvedCodeLocations(rows, LoadSchema(), TestRepo.Root);
        Assert.True(bad.Count == 0, Report("unresolved code-location(s)", bad, rows.Count));
    }

    /// <summary>Every claimed covering test is really on disk — "tested" is checked, never asserted.</summary>
    [Fact]
    public void EveryTestRef_ResolvesToARealTest()
    {
        var rows = LoadInventory();
        var bad = UnresolvedTestRefs(rows, LoadSchema(), TestRepo.Root);
        Assert.True(bad.Count == 0, Report("unresolved test-ref(s)", bad, rows.Count));
    }

    /// <summary>
    /// ⛔ THE EVIDENCE THAT THIS GATE INSPECTS ANYTHING. Each check above is run against a row built to violate it;
    /// a check that returned clean here would be a check that never looked.
    /// </summary>
    [Fact]
    public void TheseChecks_ActuallyFail_OnAFabricatedInventory()
    {
        var s = LoadSchema();
        string root = TestRepo.Root;

        Row Base(string id) => new(id, "15.7", "AR", 1, "ABS function", "", "", "", "", "", "", "GAP");

        // A verdict outside the vocabulary.
        Assert.Single(BadVerdicts([Base("X-1") with { Verdict = "PROBABLY-FINE" }], s));

        // A GAP row hand-promoted to OK with nothing behind it — the cheapest possible way to fake the burn-down.
        Assert.Single(BadStates([Base("X-2") with { Verdict = "CONFORMS", State = "OK" }], s));

        // The mirror: a fully-evidenced CONFORMS row left marked GAP, so the metric under-reports.
        Assert.Single(BadStates([Base("X-3") with
        {
            Verdict = "CONFORMS",
            CodeLocation = "scripts/spec/build_inventory.py",
            TestRef = "unit:SpecTraceabilityInventoryDriftTests.EveryRowState_IsDerived_NotAsserted",
            State = "GAP",
        }], s));

        // CONFORMS with no code-location: the one field that verdict requires. (test-ref is NOT required to
        // record a CONFORMS — only to CLOSE the row — which is what makes CONFORMS-but-untested expressible.)
        Assert.Single(MissingEvidence([Base("X-4") with { Verdict = "CONFORMS" }], s));

        // An edition the compiler does not have.
        Assert.Single(BadEditions([Base("X-5") with { Editions = "85,1974" }], s));

        // ⛔ DIFFERENTIAL-ONLY COVERAGE. A NIST golden alone, and a *_MatchesLegacy test alone, are both real
        // shapes — the first Phase-B batch produced seven of them before this rule existed. Neither may close a
        // row, so the derived state is GAP and a row asserting OK on that basis is caught twice over.
        Row Covered(string id, string testRef) => Base(id) with
        {
            Verdict = "CONFORMS", CodeLocation = "scripts/spec/inventory_schema.py", TestRef = testRef, State = "OK",
        };
        Assert.Single(DifferentialOnlyCoverage([Covered("X-5a", "nist:IF128A")], s));
        Assert.Single(DifferentialOnlyCoverage(
            [Covered("X-5b", "conformance-test:IntrinsicFunctionDifferentialTests.ExactFamily_MatchesLegacy")], s));
        Assert.Single(BadStates([Covered("X-5c", "nist:IF128A")], s));   // OK asserted; GAP derived

        // …but the SAME row also citing one spec-derived test is fine: corroboration is welcome, it just cannot
        // be the whole basis.
        Assert.Empty(DifferentialOnlyCoverage(
            [Covered("X-5d", "nist:IF128A; conformance:2023/da2_function_as_text")], s));
        Assert.Empty(BadStates([Covered("X-5e", "nist:IF128A; conformance:2023/da2_function_as_text")], s));

        // CONFORMS-but-untested: verdict recordable, row NOT closed. This is the category the split exists for.
        Assert.Empty(MissingEvidence(
            [Base("X-5f") with { Verdict = "CONFORMS", CodeLocation = "scripts/spec/inventory_schema.py" }], s));
        Assert.Empty(BadStates(
            [Base("X-5g") with { Verdict = "CONFORMS", CodeLocation = "scripts/spec/inventory_schema.py" }], s));

        // A code-location whose FILE is gone, and one whose SYMBOL is gone from a file that still exists.
        Assert.Single(UnresolvedCodeLocations(
            [Base("X-6") with { CodeLocation = "src/Cobol.Net.Compiler/NoSuchFile.cs" }], s, root));
        Assert.Single(UnresolvedCodeLocations(
            [Base("X-7") with { CodeLocation = "scripts/spec/build_inventory.py#NoSuchSymbolAnywhere" }], s, root));

        // Test refs: an unknown form, a golden that does not exist, and an xUnit method that does not exist.
        Assert.Single(UnresolvedTestRefs([Base("X-8") with { TestRef = "vibes:it-looked-right" }], s, root));
        Assert.Single(UnresolvedTestRefs([Base("X-9") with { TestRef = "conformance:2023/no_such_case" }], s, root));
        Assert.Single(UnresolvedTestRefs(
            [Base("X-10") with { TestRef = "unit:SpecTraceabilityInventoryDriftTests.NoSuchMethod" }], s, root));

        // And the positive controls — the same checkers must PASS on references that really do resolve, or the
        // failures above would prove only that the checker rejects everything.
        Assert.Empty(UnresolvedCodeLocations(
            [Base("X-11") with { CodeLocation = "scripts/spec/inventory_schema.py#state_for" }], s, root));
        Assert.Empty(UnresolvedTestRefs(
            [Base("X-12") with
            {
                TestRef = "unit:SpecTraceabilityInventoryDriftTests.EveryRowState_IsDerived_NotAsserted",
            }], s, root));

        // ── kind-DOC rows: the register anchor, and what a DOC row costs ─────────────────────────────
        //
        // A DOC row is an obligation to DOCUMENT an implementor-defined element (ISO §4.2.5), so its evidence is
        // a §7 determination filed under its own item PLUS a site in the compiler PLUS a spec-derived test. Each
        // of the three is driven separately below, and each positive control shows the checker discriminating.
        const string Anchor = "docs/CONFORMANCE.md#DOC-A.1-19";
        const string Src = "src/Cobol.Net.Runtime/Control/ProgramTable.cs#CancelNode";
        Row Doc(string id) => new(id, "A.1", "DOC", 19,
            "CANCEL statement (result of canceling a non-COBOL program)", "CONFORMS", "", "", "", "", "", "GAP");

        // A src site and no register anchor at all: the determination is unlocatable.
        Assert.Single(MisanchoredRows([Doc("DOC-A.1-19") with { CodeLocation = Src }], s));
        // ⛔ THE A11 SHAPE — the anchor of a DIFFERENT item. `#DOC-A.1-18` is a real anchor of a real row, so
        // nothing about it is malformed; it is simply not THIS row's, and only a COMPUTED anchor can see that.
        Assert.Single(MisanchoredRows(
            [Doc("DOC-A.1-19") with { CodeLocation = $"docs/CONFORMANCE.md#DOC-A.1-18; {Src}" }], s));
        // …and the control: its own anchor passes, so the two failures above are not blanket rejection.
        Assert.Empty(MisanchoredRows([Doc("DOC-A.1-19") with { CodeLocation = $"{Anchor}; {Src}" }], s));
        // A non-DOC row is untouched by the anchor rule — adding a kind must not retroactively bind 4,089 others.
        Assert.Empty(MisanchoredRows([Base("X-13") with { Verdict = "CONFORMS", CodeLocation = Src }], s));

        // ⛔ `#7`: the fragment five live rows carried, which the resolver satisfies against the digit 7 anywhere
        // in the file. And the bare path, which is the same defect resolving on File.Exists alone.
        Assert.Single(BadAnchorFragments([Doc("DOC-A.1-19") with { CodeLocation = "docs/CONFORMANCE.md#7" }], s));
        Assert.Single(BadAnchorFragments([Doc("DOC-A.1-19") with { CodeLocation = "docs/CONFORMANCE.md" }], s));
        // The controls: every anchor space the document really has stays legal — §7's item rows, §1's §4.2.16
        // summary, §5's Annex A.4 rows — and a file that is not anchored is not policed.
        Assert.Empty(BadAnchorFragments([Doc("DOC-A.1-19") with { CodeLocation = Anchor }], s));
        Assert.Empty(BadAnchorFragments(
            [Base("X-14") with { CodeLocation = "docs/CONFORMANCE.md#4.2.16" }], s));
        Assert.Empty(BadAnchorFragments(
            [Base("X-15") with { CodeLocation = "docs/CONFORMANCE.md#A.4.9" }], s));
        Assert.Empty(BadAnchorFragments([Base("X-16") with { CodeLocation = Src }], s));

        // OBSERVABLE but untested, asserted OK — the ordinary §1(c) failure, on a DOC row.
        Assert.Single(BadStates(
            [Doc("DOC-A.1-19") with { CodeLocation = $"{Anchor}; {Src}", State = "OK" }], s));
        // ⛔ NO POLICY ARM. A row whose ONLY location is its register anchor does not close — not with no test,
        // and not with a spec-derived one either. Closing it would widen the design doc §1(a) definition of DONE
        // (a traceability link into src/Cobol.Net.*, or a recorded owner non-support decision), and that is the
        // owner's completion metric, not an agent's (kb/Work PB280 Q2). Both arms are driven, because the
        // second is the one a future "it has a test, surely it closes" reading would break.
        Assert.Single(BadStates([Doc("DOC-A.1-19") with { CodeLocation = Anchor, State = "OK" }], s));
        Assert.Single(BadStates(
            [Doc("DOC-A.1-19") with
            {
                CodeLocation = Anchor, TestRef = "conformance:2023/pb154_cancel_active", State = "OK",
            }], s));
        // A code location outside src/Cobol.Net.* is not an implementing site: tooling, scripts and test harness
        // code are not the compiler, so a row resting on one is as unobservable as one resting on nothing.
        Assert.Single(BadStates(
            [Doc("DOC-A.1-19") with
            {
                CodeLocation = $"{Anchor}; tools/impact/ImpactProbe.cs#Hit",
                TestRef = "conformance:2023/pb154_cancel_active", State = "OK",
            }], s));
        // ✔ And the shape that DOES close: own anchor + a greenfield site + a spec-derived test.
        Assert.Empty(BadStates(
            [Doc("DOC-A.1-19") with
            {
                CodeLocation = $"{Anchor}; {Src}", TestRef = "conformance:2023/pb154_cancel_active", State = "OK",
            }], s));
        // …which is also the row `DifferentialOnlyCoverage` must leave alone — no kind exemption exists there,
        // and none is needed, because every closing DOC row carries a spec-derived test like every other row.
        Assert.Empty(DifferentialOnlyCoverage(
            [Doc("DOC-A.1-19") with
            {
                CodeLocation = $"{Anchor}; {Src}", TestRef = "conformance:2023/pb154_cancel_active", State = "OK",
            }], s));

        // ── the anchor is owed by the rows with a determination to point at, and the REGISTER decides ──
        //
        // ⛔ A DOC row a DECLINED module WITHDRAWS has no determination to anchor and nothing implemented to
        // observe: A.1's own preamble makes the item "not required if the optional or processor-dependent
        // feature is not implemented". Found only by landing the A.1 lane and the derived-verdict lane onto one
        // tree (2026-09-02): the derived lane stamps items 84, 85, 173 and 86 DOCUMENTED-NON-SUPPORT, and the
        // anchor rule — written where every DOC row was a determination — refused the whole 308-record batch.
        //
        // ⛔ AND THE DISCRIMINATOR IS THE §7 ROW, NOT THE VERDICT (kb/Work PB280 Q1). Both cases below carry
        // DOCUMENTED-NON-SUPPORT and differ ONLY in whether the register documents the item, so a predicate
        // keyed on the verdict passes the first and silently excuses the second. The fabricated register is two
        // literal ids, because the pair has to be driven in one place to be a pair at all.
        var reg = s with { RegisterItems = new HashSet<string>(StringComparer.Ordinal) { "DOC-A.1-127" } };
        Row Declined(string id) => Doc(id) with
        {
            Verdict = "DOCUMENTED-NON-SUPPORT",
            Notes = "the conditioning module is Not claimed (docs/CONFORMANCE.md §5)",
            CodeLocation = "",
        };
        Assert.Empty(MisanchoredRows([Declined("DOC-A.1-19")], reg));
        // …AND THE EXEMPTION IS NOT AN ESCAPE. Without its module's WITNESS the row still does not close, so a
        // module cannot move the burn-down by being declined — the §1(c) rule survives the carve-out intact.
        Assert.Single(BadStates([Declined("DOC-A.1-19") with { State = "OK" }], reg));
        // The pairing that proves the exemption DISCRIMINATES rather than blanket-disabling the check: the same
        // absent anchor on a CONFORMS row is still caught (driven above), and the declined row closes exactly
        // when its witness arrives.
        Assert.Single(MisanchoredRows([Doc("DOC-A.1-19") with { CodeLocation = "" }], reg));
        Assert.Empty(BadStates(
            [Declined("DOC-A.1-19") with
            {
                TestRef = "conformance:2023/pb154_cancel_active", State = "OK",
            }], reg));

        // ⛔ THE DOCUMENTED DECLINE — same verdict, and §7 DOES carry the row. Item 127 is A.1-optional and its
        // determination opens "Not provided.", so the non-provision IS the determination: the row owes its
        // anchor, and must name a site to observe, exactly as a CONFORMS row does. Under a verdict-keyed
        // predicate every assertion below passes vacuously.
        const string Anchor127 = "docs/CONFORMANCE.md#DOC-A.1-127";
        Row Documented(string loc) => Doc("DOC-A.1-127") with
        {
            Verdict = "DOCUMENTED-NON-SUPPORT",
            Notes = "kb/Work PB280 Q1 — the OPTIONAL element is not provided; §7 records it",
            CodeLocation = loc,
        };
        Assert.Single(MisanchoredRows([Documented("")], reg));
        Assert.Single(MisanchoredRows([Documented($"docs/CONFORMANCE.md#DOC-A.1-19; {Src}")], reg));
        Assert.Empty(MisanchoredRows([Documented($"{Anchor127}; {Src}")], reg));
        // The anchor alone does not close it — PB280 Q2, answered NO: a row with nothing observable stays a GAP
        // whatever its test-ref, and that is the same rule the CONFORMS arm pays above.
        Assert.Single(BadStates(
            [Documented(Anchor127) with { TestRef = "conformance:2023/pb154_cancel_active", State = "OK" }], reg));
        // ✔ And the shape that DOES close: its own computed anchor, a greenfield site, and a witness.
        Assert.Empty(BadStates(
            [Documented($"{Anchor127}; {Src}") with
            {
                TestRef = "conformance:2023/pb154_cancel_active", State = "OK",
            }], reg));

        // ── the DERIVATION: §1.1's owner-signed alternative to a test, and every bound the owner set ──
        //
        // ⛔ THE ARMS CANNOT BE FALSIFIED AGAINST TODAY'S DOCUMENT, where all eight determinations are correct.
        // So the register and the Annex A.2 list are FABRICATED here, and every refusal is driven with a
        // positive control beside it — a checker that rejected everything would satisfy the negatives alone.
        var d = s.Derivation!;
        var fakeRegister = new Dictionary<string, ConformanceRegister.DerivationRow>(StringComparer.Ordinal)
        {
            ["DRV-GR-14.9.5.4-11"] = new("DRV-GR-14.9.5.4-11", "undefined-A.2", "A.2 item 4", "…", d.Signature),
            ["DRV-GR-14.9.5.4-12"] = new("DRV-GR-14.9.5.4-12", "undefined-A.2", "A.2 item 41", "…", d.Signature),
            ["DRV-GR-14.9.5.4-13"] = new("DRV-GR-14.9.5.4-13", "undefined-A.2", "A.2 item 999", "…", d.Signature),
            ["DRV-GR-14.9.5.4-14"] = new("DRV-GR-14.9.5.4-14", "unpopulatable-antecedent",
                                         "the two-valued DISPLAY device set", "…", d.Signature),
            ["DRV-GR-14.9.5.4-15"] = new("DRV-GR-14.9.5.4-15", "unpopulatable-antecedent", "—", "…", d.Signature),
            ["DRV-GR-14.9.5.4-16"] = new("DRV-GR-14.9.5.4-16", "indistinguishable-consequent",
                                         "GR-14.9.5.4-7", "…", d.Signature),
            ["DRV-GR-14.9.5.4-17"] = new("DRV-GR-14.9.5.4-17", "indistinguishable-consequent",
                                         "GR-99.99.99.4-1", "…", d.Signature),
            ["DRV-GR-14.9.5.4-18"] = new("DRV-GR-14.9.5.4-18", "undefined-A.2", "A.2 item 4", "…", "owner: 1999"),
            ["DRV-GR-14.9.5.4-19"] = new("DRV-GR-14.9.5.4-19", "made-it-up", "A.2 item 4", "…", d.Signature),
        };
        var fakeA2 = new Dictionary<int, IReadOnlySet<string>>
        {
            // -18 is covered so the bad-signature row fails for the SIGNATURE alone: a case with two causes
            // cannot show which check fired.
            [4] = new HashSet<string>(StringComparer.Ordinal) { "GR-14.9.5.4-11", "GR-14.9.5.4-18" },
            [41] = new HashSet<string>(StringComparer.Ordinal) { "GR-14.9.30.4-3" },
        };
        var ds = s with
        {
            Derivation = d with { Register = fakeRegister, Undefined = fakeA2 },
            CatalogRuleIds = new HashSet<string>(StringComparer.Ordinal) { "GR-14.9.5.4-7" },
        };
        Row Derived(string id, string subject = "CANCEL statement") => new(
            id, "14.9.5.4", "GR", 11, subject, "CONFORMS",
            "src/Cobol.Net.Runtime/Control/ProgramTable.cs#CallPointer", "",
            $"docs/CONFORMANCE.md#DRV-{id}", "85,2002,2014,2023", "", "OK");

        // ✔ THE SHAPE THAT CLOSES: a resolving verdict, its evidence, and an A.2 arm that mechanically covers
        // this very rule. Driven FIRST, so every refusal below is a discrimination and not a blanket no.
        Assert.Empty(DerivationRefusals(Derived("GR-14.9.5.4-11"), ds));
        Assert.Empty(BadStates([Derived("GR-14.9.5.4-11")], ds));
        // …and the two argument arms, likewise accepted when their shape holds.
        Assert.Empty(BadStates([Derived("GR-14.9.5.4-14")], ds));
        Assert.Empty(BadStates([Derived("GR-14.9.5.4-16")], ds));

        static string[] Codes(List<Refusal> rs) => [.. rs.Select(x => x.Code).Order(StringComparer.Ordinal)];

        // ⛔ DRIFT (ii) — AN A.2 ARM NAMING AN ITEM THAT DOES NOT COVER THIS ROW. Item 41 is a real A.2 item
        // about a real undefined rule; it is simply not THIS one, which only a mechanical resolution can see.
        Assert.Equal(["a2-does-not-cover"], Codes(DerivationRefusals(Derived("GR-14.9.5.4-12"), ds)));
        Assert.Equal(["a2-no-such-item"], Codes(DerivationRefusals(Derived("GR-14.9.5.4-13"), ds)));
        Assert.Single(BadStates([Derived("GR-14.9.5.4-12")], ds));

        // ⛔ DRIFT (i) — A DERIVATION ON A ROW WITH AN OBSERVABLE OBLIGATION IS REFUSED. This is the owner's
        // explicit bound: a spec-derived test on the same row REFUTES the claim that none can exist. ⚠ The row
        // still computes OK — on the TEST — so the refusal is only visible in the predicate, which is exactly
        // why `record_verdicts` asks it rather than inferring the bound from the state.
        var tested = Derived("GR-14.9.5.4-11") with { TestRef = "conformance:2023/pb154_cancel_scope" };
        Assert.Equal(["has-spec-derived-test"], Codes(DerivationRefusals(tested, ds)));
        // …and a NON-spec-derived ref does not trip it: a NIST golden is corroboration, not an obligation met.
        Assert.Empty(DerivationRefusals(Derived("GR-14.9.5.4-11") with { TestRef = "nist:IF128A" }, ds));

        // The remaining four bounds, one row each.
        Assert.Equal(["verdict-does-not-resolve"],
            Codes(DerivationRefusals(Derived("GR-14.9.5.4-11") with { Verdict = "PARTIAL", Notes = "n" }, ds)));
        // ⚙ …and the register is looked up by the COMPUTED key, never by the claimed string — so citing another
        // row's determination is caught as a wrong ANCHOR and cannot smuggle in that row's arm.
        Assert.Equal(["not-computed-anchor"], Codes(DerivationRefusals(
            Derived("GR-14.9.5.4-11") with { Derivation = "docs/CONFORMANCE.md#DRV-GR-14.9.5.4-14" }, ds)));
        Assert.Equal(["bad-signature"], Codes(DerivationRefusals(Derived("GR-14.9.5.4-18"), ds)));
        Assert.Equal(["unknown-arm"], Codes(DerivationRefusals(Derived("GR-14.9.5.4-19"), ds)));
        Assert.Equal(["names-shape"], Codes(DerivationRefusals(Derived("GR-14.9.5.4-15"), ds)));
        Assert.Equal(["unknown-rule"], Codes(DerivationRefusals(Derived("GR-14.9.5.4-17"), ds)));
        Assert.Equal(["no-register-row"], Codes(DerivationRefusals(Derived("GR-14.9.5.4-99"), ds)));

        // ⛔ AND THE ORDER THAT KEEPS PB280 Q2 ANSWERED. A kind-DOC row pays its anchor and its observability
        // BEFORE a derivation is consulted, so a row whose only location is its §7 anchor stays a GAP with a
        // perfectly valid derivation attached — the escape cannot be routed around the owner's earlier "no".
        var docReg = new Dictionary<string, ConformanceRegister.DerivationRow>(StringComparer.Ordinal)
        {
            ["DRV-DOC-A.1-19"] = new("DRV-DOC-A.1-19", "indistinguishable-consequent", "GR-14.9.5.4-7", "…",
                                     d.Signature),
        };
        var docS = s with
        {
            Derivation = d with { Register = docReg, Undefined = fakeA2 },
            CatalogRuleIds = new HashSet<string>(StringComparer.Ordinal) { "GR-14.9.5.4-7" },
            RegisterItems = new HashSet<string>(StringComparer.Ordinal) { "DOC-A.1-19" },
        };
        const string Drv19 = "docs/CONFORMANCE.md#DRV-DOC-A.1-19";
        Assert.Single(BadStates(
            [Doc("DOC-A.1-19") with { CodeLocation = Anchor, Derivation = Drv19, State = "OK" }], docS));
        // ✔ …and the shape that DOES close on a derivation: its own §7 anchor, a greenfield site, no test.
        Assert.Empty(BadStates(
            [Doc("DOC-A.1-19") with { CodeLocation = $"{Anchor}; {Src}", Derivation = Drv19, State = "OK" }],
            docS));
        // ⛔ REFUSAL 3 — the same row where §7 does NOT document the item: the documentation obligation is not
        // yet STATED, so there is nothing for a derivation to be about (PB280 Q2's unfalsifiable shape).
        Assert.Equal(["determination-not-stated"], Codes(DerivationRefusals(
            Doc("DOC-A.1-19") with { CodeLocation = $"{Anchor}; {Src}", Derivation = Drv19 },
            docS with { RegisterItems = new HashSet<string>(StringComparer.Ordinal) })));
    }
}
