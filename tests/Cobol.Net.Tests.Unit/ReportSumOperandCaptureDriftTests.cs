// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Editions.Diagnostics;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A REPORT-SECTION CLAUSE OPERAND IS A WRITTEN REFERENCE, AND THIS KEEPS IT ONE (kb/Work PB482).
/// <para><b>The bug this guard is shaped from.</b> Every value operand of the report section was captured with
/// <c>DataBinder.KeyReference</c> — the FILE-CONTROL key helper, which keeps the base word and the IN/OF
/// qualifiers and DROPS the subscript and the reference modifier — or, for the <c>UPON</c> operand, with the
/// even weaker <c>cobolWord()?.GetText() ?? GetText()</c> first-word reduction. The consequences were measured
/// on the base tree and are all four kinds of harm at once: <c>SUM WS-CELL(2)</c> compiled and then ABORTED the
/// process at the first GENERATE; <c>SUM WS-TXT(1:2)</c> silently summed the whole item; <c>UPON CFT</c> named a
/// control footing and totalled nothing; <c>UPON NOSUCH</c> named nothing at all and was accepted.</para>
/// <para><b>The invariant.</b> An addend written as identifier-1 is an ORDINARY IDENTIFIER (ISO §8.4.3.1.2
/// Format 2, qualified-data-name-with-subscripts), so the capture keeps the WHOLE reference and the VALUE is
/// bound in the procedure phase through the one expression binder. Every <c>KeyReference</c> call in the report
/// binder therefore has to sit inside a NAMED capture helper that says which rules it screens — the guard below
/// is the containment check, and its adjudication table is where the one remaining bare arm is visible.</para>
/// <para>⭐ IT CARRIES NO COPY OF THE RULES. The SR5 / SR7 sentences are re-read from
/// <c>specs/ISO_COBOL.md</c> on every run (the <c>ControlClauseCitationDriftTests</c> pattern), so a
/// transcription repair flows through instead of going stale.</para>
/// </summary>
public sealed class ReportSumOperandCaptureDriftTests
{
    private static string BinderPath =>
        TestRepo.Src("Cobol.Net.Compiler", "Binding", "DataBinder.Reports.cs");

    /// <summary>The methods of <c>DataBinder.Reports.cs</c> that may call <c>KeyReference</c> directly, and why.
    /// ⛔ Adding a row is an ADJUDICATION: it says this operand genuinely cannot carry a subscript or a
    /// reference modifier, or that the clause's own note says where its residue is tracked.</summary>
    private static readonly Dictionary<string, string> KeyReferenceCallers = new(StringComparer.Ordinal)
    {
        ["SumAddendRef"] = "the SUM addend capture — keeps the whole reference and screens §13.18.54.3 SR5",
        ["UponDetailRef"] = "the UPON data-name-2 capture — screens §13.18.54.3 SR7's shape",
        ["ControlOperandRef"] = "the CONTROL / TYPE CH-CF / RESET ON capture (kb/Work PB205) — keeps the ref-mod",
        // ⛔ ADJUDICATED AT THE TRAIN-35 MERGE, not inherited. `OCCURS … DEPENDING ON data-name-1` (§13.18.38.2
        // Format 3, landed by kb/Work PB562 in train 34) writes DATA-NAME-1, not identifier-1 — and that position
        // admits QUALIFICATION ONLY: §8.4.2.2.2 Format 1 is the qualified-data-name, which carries no subscript,
        // and §8.4.3.3.3 SR5's NOTE bars reference modification "where data-name-n is used in a general format or
        // syntax rule". So the key helper's name + IN/OF pair IS the whole written reference here and nothing is
        // dropped — the opposite of §13.18.54.3 SR5's addend, which is an identifier.
        // ⚠ RESIDUE, reported to the registrar rather than fixed here (it is PB562's clause, not PB482's): the
        // grammar's `dataReference` will still PARSE `DEPENDING ON WS-T(2)`, and the subscript is then dropped in
        // silence instead of being refused the way COBOLNET2046 refuses a subscripted UPON operand.
        ["ReportOccursOf"] = "OCCURS … DEPENDING ON data-name-1 (§13.18.38.2 Format 3) — a data-name position: "
                             + "qualification only (§8.4.2.2.2 Format 1), no reference modification (§8.4.3.3.3 SR5 NOTE)",
        // ⛔ THE SOURCE CLAUSE'S ARM IS NO LONGER ONE OF THESE (kb/Work PB1292). §13.18.53's identifier-1 is an
        // identifier exactly as §13.18.54.3 SR5's is, and `BindSourceReference` — the one bare capture that dropped
        // its subscript and reference modification and staged COBOLNET0899 — is DELETED: the identifier is kept as its
        // WRITTEN reference (`FieldReferenceSource`) and bound in the procedure phase by the one operand binder.
        // The RD CODE clause's identifier-1 shared that arm, so it keeps the whole reference too and asks the
        // key helper only for the BASE item its §13.18.12.3 SR2 screen is about:
        ["ResolveReportCode"] = "RD CODE identifier-1 (§13.18.12.3 SR2) — the screen is over the BASE item's description; the subscript and "
                                + "reference modification ride in the written reference and bind in the procedure phase",
    };

    private static readonly Regex MethodDecl = new(
        @"^\s{4}(?:private|internal|public|protected)[^;=]*?\b(?<name>[A-Z][A-Za-z0-9_]*)\s*\(",
        RegexOptions.Compiled);

    [Fact]
    public void EveryKeyReferenceCapture_IsANamedOperandHelper()
    {
        string[] lines = File.ReadAllLines(BinderPath);

        // Population: the scan must actually see the calls it is judging (feedback_green_gates_arent_evidence —
        // a guard that looked at nothing passes for the wrong reason).
        int calls = lines.Count(l => l.Contains("KeyReference(", StringComparison.Ordinal)
                                     && !l.Contains("private static", StringComparison.Ordinal));
        Assert.True(calls >= 4,
            $"only {calls} KeyReference call sites found in DataBinder.Reports.cs — the capture moved, "
            + "so this guard is judging nothing. Follow it, do not lower the floor.");

        string current = "<file scope>";
        var offenders = new List<string>();
        for (int i = 0; i < lines.Length; i++)
        {
            if (MethodDecl.Match(lines[i]) is { Success: true } m) current = m.Groups["name"].Value;
            if (!lines[i].Contains("KeyReference(", StringComparison.Ordinal)) continue;
            if (lines[i].Contains("private static", StringComparison.Ordinal)) continue;   // the helper itself
            if (!KeyReferenceCallers.ContainsKey(current)) offenders.Add($"{current} (line {i + 1})");
        }
        Assert.True(offenders.Count == 0,
            "a report-section clause operand is captured with the FILE STATUS key helper, which DROPS the "
            + "subscript and the reference modifier (kb/Work PB482). Capture it in a named helper that keeps "
            + "the whole written reference and screens the clause's own rules:\n  "
            + string.Join("\n  ", offenders));
    }

    [Fact]   // The complement of the containment scan: the two SUM captures really are wired in
             // (feedback_measure_the_selectors_complement — a scan proves only what it DID NOT find).
    public void BindSumClause_CapturesThroughTheHelpers_AndNotByFirstWord()
    {
        string binder = File.ReadAllText(BinderPath);
        Assert.Contains("term.Addends.Add(SumAddendRef(", binder);
        Assert.Contains("UponDetailRef(up, model)", binder);
        Assert.DoesNotContain("cobolWord()?.GetText() ??", binder);
        // SR7's operand resolves through the ONE report-group funnel (kb/Work PB365), never a bare name scan.
        Assert.Contains("ReportGroupResolution.Resolve(", binder);
    }

    [Fact]   // ONE counter per ENTRY (§13.18.54.4 GR1) however many times SUM appears (SR1): the grammar's one
             // reportSumClause holds every SUM group (§13.18.54.2 — RESET and the rounded-phrase follow them, kb/Work
             // PB1295), the binder makes each group a term, and a single-group capture is what silently discarded
             // every group but one.
    public void SumGroupsOfOneEntry_AreCollected_NotOverwritten()
    {
        string binder = File.ReadAllText(BinderPath);
        Assert.Contains("sumClause ??= sm", binder);   // the first clause wins; a second is refused once per written entry
        Assert.Contains("BindSumClause(sumClause,", binder);
        Assert.Contains("foreach (var sg in clause.reportSumGroup())", binder);
        Assert.DoesNotContain("sumClause = sm;", binder);
    }

    [Fact]   // The addend arrives at the emitter as a BOUND EXPRESSION — that is what makes a subscript, an
             // index-name and an arithmetic subscript all work through the ONE machinery.
    public void Emitter_RendersTheAddendExpression_NotAResolvedItem()
    {
        string emitter = File.ReadAllText(
            TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Verbs", "ReportWriterEmitter.cs"));
        Assert.Contains("AddSumTerm(", emitter);
        // The bound addend EXPRESSIONS go through the ONE numeric renderer's ADD fold (kb/Work PB1686 — the same
        // `Fold` ArithmeticEmitter.EmitInPlace evaluates an ADD's operands with), combined with the counter's content
        // and stored once; never a resolved storage item and never a per-addend alignment.
        Assert.Contains("num.Fold(addendExprs, rcv)", emitter);
        // The run-time loud that used to fire for EVERY table addend may survive only as the
        // already-diagnosed-at-bind backstop, never as the resolution path.
        Assert.DoesNotContain("SUM addend not resolvable to storage", emitter);

        string rwBinder = File.ReadAllText(
            TestRepo.Src("Cobol.Net.Compiler", "Binding", "Procedure", "Verbs", "ReportWriterBinder.cs"));
        Assert.Contains("a.Value = host.Expr.BindExpr(a.Ctx)", rwBinder);
    }

    [Fact]   // The inherited-citation guard: the two new diagnostics must quote the rules they enforce, read
             // out of the spec on every run (CLAUDE.md rule 1's failure mode is INHERITING a clause number).
    public void SumOperandDiagnostics_CiteTheRulesTheyEnforce()
    {
        var rules = SumSyntaxRules();
        Assert.Contains("shall specify a numeric data item not defined in the report section", rules[5]);
        Assert.Contains("shall be the name of a detail", rules[7]);
        Assert.Contains("may be qualified only by a report-name", rules[7]);

        // Read the DESCRIPTORS, not their source text: the concatenation is already resolved, so a re-wrap of
        // the catalogue cannot make this guard pass or fail for the wrong reason.
        var sr5 = DiagnosticCatalog.ReportSumAddendNotNumeric;
        Assert.Equal("COBOLNET2045", sr5.Code);
        Assert.Contains(rules[5], sr5.Title);
        Assert.Contains("13.18.54.3", sr5.IsoSection);

        var sr7 = DiagnosticCatalog.ReportSumUponNotDetail;
        Assert.Equal("COBOLNET2046", sr7.Code);
        Assert.Contains(rules[7], sr7.Title);
        Assert.Contains("13.18.54.3", sr7.IsoSection);
    }

    /// <summary>The numbered syntax rules of §13.18.54.3, read out of the spec transcription.</summary>
    private static Dictionary<int, string> SumSyntaxRules()
    {
        string[] lines = File.ReadAllLines(TestRepo.Specs("ISO_COBOL.md"));
        int start = Array.FindIndex(lines, l => Regex.IsMatch(l, @"^#{2,6}\s+13\.18\.54\.3\b"));
        Assert.True(start >= 0, "§13.18.54.3 is missing from specs/ISO_COBOL.md — this guard must follow the clause.");
        int end = Array.FindIndex(lines, start + 1, l => Regex.IsMatch(l, @"^#{2,6}\s+13\.18\.54\.4\b"));
        Assert.True(end > start, "§13.18.54.4 not found after §13.18.54.3 — the heading shape changed.");

        var rules = new Dictionary<int, string>();
        foreach (string l in lines[start..end])
            if (Regex.Match(l, @"^(\d+)\\?\)\s+(.*)$") is { Success: true } m)
                rules[int.Parse(m.Groups[1].Value)] = m.Groups[2].Value.Trim();
        Assert.True(rules.Count >= 9,
            $"only {rules.Count} syntax rules parsed from §13.18.54.3 — fix the scanner, do not lower the floor.");
        return rules;
    }
}
