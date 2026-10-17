// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A DATA REFERENCE IS IDENTIFIED IN ONE PLACE, AND A SUM COUNTER IS COUNTED WITH THE DATA-NAMES OF ITS SPELLING
/// (kb/Work PB3287, PB3232).
/// <para><b>PB3287.</b> <c>ReferenceResolver.ResolveForAddressOf</c> re-sequenced the front half of
/// <c>ResolveImplCore</c> by hand (the ref-mod count, the name lookup, the subscript screen) and dropped two of its
/// screens: ADDRESS OF a LINKAGE record the procedure division may not reference (ISO §13.7.3 SR4) compiled clean, and
/// an ambiguous or index-name operand was told it "names none". The sequence is now ONE method,
/// <c>Identify</c>; the two entries differ only by what they build after it. A screen added to it reaches both, and a
/// third entry that re-wrote the sequence would be red here.</para>
/// <para><b>PB3232.</b> A REPORT SECTION sum counter is a data-name (§13.18.54.4 GR5), so for §8.4.2.2.3 SR1 it is one
/// candidate among the ordinary data items of its spelling. <c>SumCounterFamilyFor</c> counted the counters alone, and
/// consulted the ordinary lookup only when the written qualifiers reached no counter, so the counter silently won
/// every collision it had no qualifier for.</para>
/// <para>These are source scans because the property is WHERE the rule is written; the behavioural half is the
/// conformance corpus (<c>pb3232_sum_counter_qualified_beside_data_item</c>, the <c>pb3232-</c> and <c>pb3287-</c>
/// negatives, <c>pb3287_address_of_linkage_sr4_admitted</c>).</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class ReferenceIdentificationDriftTests
{
    private static readonly string ReferenceResolver = File.ReadAllText(
        Path.Combine(TestRepo.Src("Cobol.Net.Compiler"), "Binding", "ReferenceResolver.cs"));

    /// <summary>The text of the member whose declaration line contains <paramref name="signature"/>, through its closing
    /// brace (brace matching over the source; a member is found by its signature, so a rename is red here).</summary>
    private static string BodyOf(string signature)
    {
        int at = ReferenceResolver.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(at >= 0, $"{signature} is no longer in ReferenceResolver.cs — the scan measured nothing");
        int open = ReferenceResolver.IndexOf('{', at);
        int depth = 0;
        for (int i = open; i < ReferenceResolver.Length; i++)
        {
            if (ReferenceResolver[i] == '{') depth++;
            else if (ReferenceResolver[i] == '}' && --depth == 0) return ReferenceResolver[open..(i + 1)];
        }
        throw new InvalidOperationException($"unbalanced braces after {signature}");
    }

    /// <summary>Drop line comments so a rule NAMED in prose is never mistaken for a rule APPLIED in code.</summary>
    private static string Code(string s) => Regex.Replace(s, @"//[^\n]*", " ");

    [Fact]
    public void EveryIdentifierEntry_AsksTheOneIdentificationStep()
    {
        string core = Code(BodyOf("private RefResolution ResolveImplCore("));
        string addressOf = Code(BodyOf("internal AddressOfOperand? ResolveForAddressOf("));
        Assert.Contains("Identify(dref, report, forAddressOf: false)", core, StringComparison.Ordinal);
        Assert.Contains("Identify(dref, report: true, forAddressOf: true)", addressOf, StringComparison.Ordinal);

        // The entries keep only their own tail: none re-reads the written reference, re-looks the name up or re-runs a
        // screen the identification step owns (the pre-PB3287 ResolveForAddressOf did all four).
        foreach (var (entry, body) in new[] { ("ResolveImplCore", core), ("ResolveForAddressOf", addressOf) })
            foreach (string owned in new[]
                     { "ReadWritten(", "FindItem(", "ResolveUnqualified(", "ResolveQualified(", "ReadSubscripts(",
                       "ScreenRefModCount(", "ScreenLinkageReference(", "ReportUnidentified(" })
                Assert.False(body.Contains(owned, StringComparison.Ordinal),
                    $"ReferenceResolver.{entry} calls {owned} itself — that belongs to ReferenceResolver.Identify, the ONE "
                    + "identification step of a data reference (kb/Work PB3287); a second sequence drops screens (§13.7.3 SR4, "
                    + "§8.4.2.2.3 SR1) the first one has.");
    }

    [Fact]
    public void TheIdentificationStep_OwnsEveryScreenOfTheFrontHalf()
    {
        string identify = Code(BodyOf("private Identified Identify("));
        foreach (string screen in new[]
                 { "CapacityRegisterFor(", "IsExceptionObjectRegister(", "SumCounterFor(", "ReadWritten(", "ScreenRefModCount(",
                   "ResolveQualified(", "ResolveUnqualified(", "ReportUnidentified(", "ScreenLinkageReference(", "ReadSubscripts(" })
            Assert.True(identify.Contains(screen, StringComparison.Ordinal),
                $"ReferenceResolver.Identify no longer asks {screen} — the screen moved out of the one identification step.");
        // §13.7.3 SR4 is asked at exactly two sites of the resolver — the identification step (every identifier) and the
        // reported lookup of a TABLE name (FindReportedItem) — besides the definition.
        Assert.Equal(3, Regex.Matches(Code(ReferenceResolver), @"\bScreenLinkageReference\(").Count);
    }

    [Fact]
    public void ASumCounterIsCountedWithTheOrdinaryDataItemsOfItsSpelling()
    {
        string body = Code(BodyOf("internal (ReportModel Report, ReportSumFamily Family)? SumCounterFamilyFor("));
        // The counters (`matches`) and the ordinary candidates the SAME qualifiers reach in the SAME source element are
        // ONE count: the ambiguity verdict is asked of their sum, never of the counters alone.
        Assert.Matches(@"matches\.Count\s*\+\s*data\.QualifiedCandidates\(name,\s*qualifiers,", body);
        Assert.Matches(@"DeclaringDepth\(item\)\s*==\s*counterDepth", body);
        Assert.Matches(@"if\s*\(\s*sameSpelling\s*>\s*1\s*\)", body);
        Assert.DoesNotMatch(@"if\s*\(\s*matches\.Count\s*>\s*1\s*\)", body);
    }

    [Fact]
    public void TheTableAllArgument_AsksTheSameCounterResolver()
    {
        // The other arm of the same dispatch: FUNCTION SUM(counter(ALL)) takes its counter from SumCounterFamilyFor too, so
        // it inherits the counted-together verdict and keeps no candidate set of its own.
        string text = File.ReadAllText(Path.Combine(TestRepo.Src("Cobol.Net.Compiler"),
            "Binding", "Procedure", "Verbs", "IntrinsicBinder.cs"));
        Assert.Contains("ctx.Refs.SumCounterFamilyFor(dref, name, quals, report: true)", text, StringComparison.Ordinal);
        // ... and its table lookup is the REPORTED one (the §8.4.2.2 report and the §13.7.3 SR4 screen), not a bare FindItem.
        Assert.Contains("ctx.Refs.FindReportedItem(dref, name, quals)", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Refs.FindItem(", text, StringComparison.Ordinal);
    }

    /// <summary>The two references that name a TABLE rather than a place — the SEARCH / SORT table operand and the
    /// table(ALL) intrinsic argument — look their item up through <c>FindReportedItem</c>, which reports an unidentified
    /// name and screens a LINKAGE item the procedure division may not reference (§13.7.3 SR4). A bare
    /// <c>FindItem</c> there is the pre-PB3287 shape: <c>SEARCH L-TABLE</c> compiled clean over such a record.</summary>
    [Fact]
    public void TheTableOperand_LooksItsItemUpThroughTheReportedLookup()
    {
        string body = Code(BodyOf("public TableOperand? ResolveTableOperand("));
        Assert.Contains("FindReportedItem(dref, name, written.Qualifiers)", body, StringComparison.Ordinal);
        Assert.DoesNotContain("FindItem(", body, StringComparison.Ordinal);
        string reported = Code(BodyOf("internal DataItem? FindReportedItem("));
        Assert.Contains("ReportUnidentified(", reported, StringComparison.Ordinal);
        Assert.Contains("ScreenLinkageReference(", reported, StringComparison.Ordinal);
    }
}
