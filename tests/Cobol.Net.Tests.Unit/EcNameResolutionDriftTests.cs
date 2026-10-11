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
        [Path.Combine("Binding", "EcNameResolution.cs")] =
            (1, "the funnel itself", false),
        [Path.Combine("Binding", "TurnState.cs")] =
            (1, "NameMatches — level-2 coverage of names that ALREADY passed TryResolve in Create", false),
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
        var advises = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (rel, text) in CompilerSources())
        {
            int n = Regex.Matches(text, @"ExceptionCatalog\.TryGet\(").Count;
            if (n > 0) found[rel] = n;
            if (text.Contains("EcNameResolution.Advise(", StringComparison.Ordinal)) advises.Add(rel);
        }

        // Population: the funnel exists and the scan saw it — a rename must fail loudly, not vacuously.
        Assert.True(found.ContainsKey(Path.Combine("Binding", "EcNameResolution.cs")),
            "EcNameResolution.cs is gone or no longer calls ExceptionCatalog.TryGet — this guard must follow it");

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
