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
/// ⛔ A WRITTEN object-class-name OR interface-name RESOLVES THROUGH ONE FUNNEL (<c>OoNameResolution</c> —
/// kb/Work PB365), AND THIS KEEPS IT TRUE. ISO §8.4.6.4 scopes both to "the name of the containing object
/// class definition or declared in the REPOSITORY paragraph of that or a containing source element";
/// <c>OoClassTable</c> holds the COMPILATION GROUP's names, which is a strictly wider set. Every site that
/// called <c>Find</c> / <c>FindInterface</c> directly therefore answered a different question than the one the
/// standard asks — silently, because widening a set cannot fail a lookup.
///
/// <para>The guard is source-form: a direct <c>Find</c> / <c>FindInterface</c> call in the compiler is either
/// the funnel, or an ADJUDICATED re-lookup with its reason and its call count pinned. A new NAME REFERENCE
/// site fails here until it goes through the funnel.</para>
/// </summary>
public sealed class OoNameResolutionDriftTests
{
    /// <summary>Adjudicated direct callers: file → (expected call count, reason). Every one of these RE-LOOKS-UP
    /// a name that was ALREADY scope-checked where the source wrote it (an item's declared
    /// <c>PicInfo.ObjectClassName</c>, a symbol's own <c>Name</c>), so they are not references to be scoped —
    /// they are symbol lookups on an already-validated name. Adding or moving a call is an adjudication.</summary>
    private static readonly Dictionary<string, (int Count, string Reason)> Adjudicated =
        new(StringComparer.Ordinal)
    {
        [Path.Combine("Oo", "OoNameResolution.cs")] =
            (4, "the funnel itself — the scoped Lookup's two, plus the two group-wide defined-anywhere tests "
                + "that decide WHICH failure message to print (including whether the name is the standard class "
                + "BASE, kb/Work PB1548)"),
        [Path.Combine("Oo", "OoConformance.cs")] =
            (12, "§9.3.8.2.3 / §14.9.39.3 conformance over the recv/send names of an ALREADY-BUILT "
                + "ObjectRefDescriptor — scope-checked through this funnel at the data description entry "
                + "that WROTE them, so these are symbol lookups on a validated name, not references; plus the "
                + "two §9.3.8.2.3 rule 9 b)/c) lookups of a RAISING element's class / interface, scope-checked "
                + "where the procedure division header WROTE it (RaisingPhrase.Partition, kb/Work PB972); plus the "
                + "two node lookups of §9.3.8.2.3's closing-sentence reference walk (Reaches, kb/Work PB1498), whose "
                + "nodes are the names of already-built returning-item descriptors and class/interface symbols"),
        [Path.Combine("Binding", "Procedure", "Verbs", "OoBinder.cs")] =
            (4, "re-lookup of a receiver's/target's declared ObjectClassName (scope-checked at its data "
                + "description entry, COBOLNET0813)"),
        [Path.Combine("Binding", "Procedure", "Verbs", "EcBinder.cs")] =
            (3, "the GOBACK §14.9.18.3 SR4 / EXIT §14.9.14.3 SR5 identifier check (RaisingObjectMismatch, kb/Work "
                + "PB815/PB814): a) / c) the superclass walk from the operand's declared (or containing) class, "
                + "b) the operand's interface and each header-listed interface — all names already scope-checked "
                + "where they were WRITTEN (the data description entry, and RaisingPhrase.Partition's funnel "
                + "lookup)"),
        [Path.Combine("Binding", "ReferenceResolver.cs")] =
            (1, "the property-reference INSTANCE form's re-lookup of the receiving item's declared class"),
        [Path.Combine("CodeGen", "EmitterState.cs")] =
            (2, "AutomaticPropagation.Of (kb/Work PB1119, §14.6.13.1.5 EXIT/GOBACK item 3): the re-lookup of each "
                + "class / interface an element's PROCEDURE DIVISION header RAISING phrase names, to render its C# "
                + "type test — scope-checked where the header WROTE it (RaisingPhrase.Partition's funnel lookup)"),
    };

    /// <summary>The lookup shape that WAS the bug: <c>Find</c> / <c>FindInterface</c> on the group-wide table
    /// (<c>OoClasses</c> at the binder sites, <c>table</c> inside the Oo namespace). Anchored on the receiver
    /// so unrelated <c>List.Find</c> / <c>ImplementorCodeNames.Find</c> calls are not swept in, and on the dot
    /// so the table's own accessor DECLARATIONS are not counted as call sites.</summary>
    private static readonly Regex TableLookup = new(
        @"(?:OoClasses|\btable)\s*\??\.\s*(?:Find|FindInterface)\(", RegexOptions.Compiled);

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
    public void EveryClassOrInterfaceNameLookup_IsTheFunnel_OrAdjudicated()
    {
        var found = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var (rel, text) in CompilerSources())
        {
            int n = TableLookup.Matches(text).Count;
            if (n > 0) found[rel] = n;
        }

        // Population: the funnel exists and this scan saw it. A rename must fail loudly, not vacuously
        // (feedback_green_gates_arent_evidence).
        Assert.True(found.ContainsKey(Path.Combine("Oo", "OoNameResolution.cs")),
            "OoNameResolution.cs is gone or no longer calls Find/FindInterface — this guard must follow it");

        var unadjudicated = found.Where(kv =>
            !Adjudicated.TryGetValue(kv.Key, out var a) || a.Count != kv.Value).ToList();
        Assert.True(unadjudicated.Count == 0,
            "OoClassTable.Find / FindInterface call site(s) outside the adjudicated set (or an adjudicated "
            + "file whose call count changed) — resolve a WRITTEN class-name / interface-name through "
            + "OoNameResolution so the ISO §8.4.6.4 REPOSITORY scope applies, or adjudicate the direct call "
            + "here WITH its reason (kb/Work PB365):\n  "
            + string.Join("\n  ", unadjudicated.Select(kv => $"{kv.Key} ({kv.Value} call(s))")));
    }

    [Fact]   // The complement of the scan above, which can only prove that nobody ELSE resolves a name: the
             // sites the funnel's doc comment names really do call it (feedback_measure_the_selectors_complement).
    public void TheNamedReferenceSites_CallTheFunnel()
    {
        foreach (string rel in new[]
                 {
                     Path.Combine("Oo", "OoClassTable.cs"),                          // INHERITS, IMPLEMENTS
                     Path.Combine("Binding", "Procedure", "ProcedureTableBuilder.cs"),// USE Format 4
                     // USAGE OBJECT REFERENCE lives in the DataBinder's Oo
                     // partial: kb/Work PB389 moved the §13.18.60.2 adjudication into
                     // OoBindObjectRefDescriptor, and the written name is resolved there. DataBinder.cs
                     // itself no longer references a class-name or interface-name — and if one ever
                     // reappears there, EveryClassOrInterfaceNameLookup_IsTheFunnel_OrAdjudicated is the
                     // arm that fails, so dropping it from this list opens no hole.
                     Path.Combine("Binding", "DataBinder.Oo.cs"),
                     Path.Combine("Binding", "RaisingPhrase.cs"),                     // PD-header + METHOD-ID RAISING (PB815)
                     Path.Combine("Binding", "Procedure", "Verbs", "OoBinder.cs"),    // INVOKE / SET class-name
                     Path.Combine("Binding", "ReferenceResolver.cs"),                 // property-ref qualifier
                 })
        {
            string text = File.ReadAllText(Path.Combine(TestRepo.Src("Cobol.Net.Compiler"), rel));
            Assert.True(text.Contains("OoNameResolution.", StringComparison.Ordinal),
                $"{rel} no longer resolves its written class-name / interface-name through OoNameResolution "
                + "(kb/Work PB365 — ISO §8.4.6.4 scopes the name to the referring source element)");
        }
    }
}
