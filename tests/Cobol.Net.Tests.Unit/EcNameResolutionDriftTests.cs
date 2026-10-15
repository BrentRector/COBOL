// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ WRITTEN EXCEPTION-NAMES RESOLVE THROUGH ONE FUNNEL (<c>EcNameResolution</c> — kb/Work R05), AND THIS
/// KEEPS IT TRUE. Before the funnel, the unknown-name diagnostic and the introduction gate existed as four
/// verbatim copies each, which is the shape under which the §15.33 width advisory would have been added to one
/// arm of six (feedback_two_arm_dispatch — the most reproducible defect shape in this project's history).
///
/// <para>The guard is source-form: every <c>ExceptionCatalog.TryGet</c> call site in the compiler is either
/// the funnel or an ADJUDICATED direct caller with its reason and its call count pinned. A new site — or a new
/// call inside an adjudicated file — fails here until it is routed through the funnel or adjudicated with a
/// reason. The two RAISING-phrase files may bypass <c>TryResolve</c> (an unresolved word there may legally be
/// a CLASS name, §14.2.2 SR8/SR9) but MUST call <c>EcNameResolution.Advise</c> on accepted names — asserted.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class EcNameResolutionDriftTests
{
    /// <summary>Adjudicated direct <c>ExceptionCatalog.TryGet</c> callers: file → (expected call count, reason,
    /// mustAdvise). Adding or moving a call is an adjudication, not a formality.</summary>
    private static readonly Dictionary<string, (int Count, string Reason, bool MustAdvise)> Adjudicated =
        new(StringComparer.Ordinal)
    {
        [Path.Combine("Binding", "TurnState.cs")] =
            (1, "NameMatches — level-2 coverage of names the front end's >>TURN stage ALREADY resolved (SR2, kb/Work PB2740)", false),
        // kb/Work PB815/PB814: the PD-header arm (EcBinder) and the METHOD-ID arm (DataBinder.Oo) were two
        // verbatim copies of this partition; both now call RaisingPhrase.Partition, so ONE adjudicated site.
        [Path.Combine("Binding", "RaisingPhrase.cs")] =
            (1, "the ONE procedure-division-header RAISING partition (§14.2.2 SR7–SR9); an unresolved word may "
                + "be a class-name or an interface-name", true),
        [Path.Combine("CodeGen", "EcEmitter.cs")] =
            (3, "emit-side level tests on names already bound and validated, plus EmitConditionSet's Table 13 "
                + "fatality lookup of a level-3 name the EMITTER spells (never a written name; kb/Work PB1549)", false),
        [Path.Combine("CodeGen", "Verbs", "ControlFlowEmitter.cs")] =
            (1, "emit-side level test on a bound RAISING name", false),
    };

    private static IEnumerable<(string Rel, string Text)> CompilerSources()
    {
        string root = TestRepo.Src("Cobol.Net.Compiler");
        foreach (string f in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            if (f.Contains("Generated", StringComparison.Ordinal)) continue;
            yield return (Path.GetRelativePath(root, f), File.ReadAllText(f));
        }
    }

    [Fact]
    public void EveryTryGetCallSite_IsTheFunnel_OrAdjudicated()
    {
        var found = new Dictionary<string, int>(StringComparer.Ordinal);
        var rule = new Dictionary<string, int>(StringComparer.Ordinal);
        var advises = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (rel, text) in CompilerSources())
        {
            int n = Regex.Matches(text, @"ExceptionCatalog\.TryGet\(").Count;
            if (n > 0) found[rel] = n;
            int r = Regex.Matches(text, @"ExceptionNameRule\.TryGet\(").Count;
            if (r > 0) rule[rel] = r;
            if (text.Contains("EcNameResolution.Advise(", StringComparison.Ordinal)) advises.Add(rel);
        }

        // Population: the funnel exists and the scan saw it — a rename must fail loudly, not vacuously. The funnel
        // reaches the catalog through ExceptionNameRule (the §7.3.25.3 SR2 home both it and the front end's >>TURN
        // stage ask, kb/Work PB2740), so it carries no direct ExceptionCatalog.TryGet of its own, and it is the
        // compiler's ONLY caller of the rule (TryResolve and AdviseResolved).
        Assert.True(rule.Count == 1 && rule.GetValueOrDefault(Path.Combine("Binding", "EcNameResolution.cs")) == 2,
            "the compiler's ExceptionNameRule.TryGet callers are not exactly EcNameResolution.cs (TryResolve + "
            + "AdviseResolved) — this guard must follow it:\n  "
            + string.Join("\n  ", rule.Select(kv => $"{kv.Key} ({kv.Value} call(s))")));

        var unadjudicated = found.Where(kv =>
            !Adjudicated.TryGetValue(kv.Key, out var a) || a.Count != kv.Value).ToList();
        Assert.True(unadjudicated.Count == 0,
            "ExceptionCatalog.TryGet call site(s) outside the adjudicated set (or an adjudicated file whose "
            + "call count changed) — route written-name resolution through EcNameResolution.TryResolve, or "
            + "adjudicate the direct call here WITH its reason:\n  "
            + string.Join("\n  ", unadjudicated.Select(kv => $"{kv.Key} ({kv.Value} call(s))")));

        foreach (var (rel, spec) in Adjudicated.Where(kv => kv.Value.MustAdvise))
            Assert.True(advises.Contains(rel),
                $"{rel} bypasses TryResolve (adjudicated: {spec.Reason}) but no longer calls "
                + "EcNameResolution.Advise — the accepted names lose the §15.33 width advisory (kb/Work R05)");
    }

    /// <summary>The SR2 rule's own home and its callers OUTSIDE the compiler (the front end and the editions assembly
    /// cannot reference the binder): <c>ExceptionNameRule.TryGet</c> is called by the <c>&gt;&gt;TURN</c> stage alone
    /// (the binder's callers are pinned above), and the catalog is read directly only by the rule and by the §8.12
    /// directive-word reservation, which asks whether a word is RESERVED, not whether a written name resolves.</summary>
    [Fact]
    public void OutsideTheCompiler_TheSr2RuleIsExceptionNameRule()
    {
        var expectedRuleCallers = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [Path.Combine("Cobol.Net.Frontend", "Preprocessor", "TurnDirectiveProcessor.cs")] = 1,
        };
        var expectedCatalogCallers = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [Path.Combine("Cobol.Net.Editions", "ExceptionNameRule.cs")] = 1,
            [Path.Combine("Cobol.Net.Frontend", "Common", "CompilerDirectiveWords.cs")] = 1,
        };
        var ruleCallers = new Dictionary<string, int>(StringComparer.Ordinal);
        var catalogCallers = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (string project in new[] { "Cobol.Net.Frontend", "Cobol.Net.Editions" })
        {
            string root = TestRepo.Src(project);
            foreach (string f in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                string rel = Path.GetRelativePath(root, f);
                if (rel.StartsWith("obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                    || rel.StartsWith("bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal)) continue;
                string text = File.ReadAllText(f);
                int r = Regex.Matches(text, @"ExceptionNameRule\.TryGet\(").Count;
                if (r > 0) ruleCallers[Path.Combine(project, rel)] = r;
                int c = Regex.Matches(text, @"ExceptionCatalog\.TryGet\(").Count;
                if (c > 0) catalogCallers[Path.Combine(project, rel)] = c;
            }
        }
        Assert.Equal(expectedRuleCallers.OrderBy(kv => kv.Key), ruleCallers.OrderBy(kv => kv.Key));
        Assert.Equal(expectedCatalogCallers.OrderBy(kv => kv.Key), catalogCallers.OrderBy(kv => kv.Key));
    }

    [Fact]   // The migrated codes must come from the catalog descriptors — a bare literal is the split-code
             // shape DiagnosticRegistryDrift exists to prevent, reasserted here at the source level.
    public void MigratedEcCodes_HaveNoBareLiteralsLeft()
    {
        var offenders = CompilerSources()
            .Where(s => Regex.IsMatch(s.Text, "\"COBOLNET0711\"|\"COBOLNET0878\"|\"COBOLNET1636\""))
            .Select(s => s.Rel).ToList();
        Assert.True(offenders.Count == 0,
            "bare COBOLNET0711/0878/1636 string literal(s) — these codes are catalog descriptors "
            + "(DiagnosticCatalog.EcName*) and must be emitted through them:\n  "
            + string.Join("\n  ", offenders));
    }
}
