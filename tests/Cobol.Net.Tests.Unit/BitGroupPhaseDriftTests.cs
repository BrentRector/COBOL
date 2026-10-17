// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A BYTE BOUNDARY INSIDE A BIT GROUP IS A BYTE OF THE RECORD, AND THE WALK KNOWS WHERE ITS GROUP BEGINS IN ONE
/// (kb/Work PB3158). §8.5.1.6.3 starts a bit group that follows a same-level bit item at "the next bit position in
/// storage", so the group can begin mid-byte, and §13.18.1.4 GR1 aligns an ALIGNED subject on "the first available byte
/// boundary" — of the record, not of the group it sits in. <c>BitLayout.PhaseOf</c> is where in a byte a group begins;
/// the one walk starts its cursor there, so every <c>RoundUpToByte</c> it performs is the record's. These facts keep the
/// shape that makes that true and cheap:
/// <list type="bullet">
/// <item>every public width, stride, run and extent reader derives the phase through <c>PhaseOf</c>, so a caller that
/// does not know where the group sits still gets the record's answer;</item>
/// <item>the walk never calls <c>PhaseOf</c> itself: it passes down the phase it already holds (the explicit <c>…At</c>
/// readers). A walk that asked <c>PhaseOf</c> for each child would re-walk the parent for every sibling, which is
/// exponential in a run of same-level bit groups;</item>
/// <item>a table whose element lays out differently at different phases strides whole bytes (<c>HoldsByteStart</c>),
/// so laying every occurrence out at the first one's phase is exact, and a group begins where its first item does
/// (kb/Work PB3167); the placement dispatch is written once (<c>Continues</c>).</item>
/// </list>
/// The behaviour is pinned by the goldens <c>tests/conformance/2002/pb3158_aligned_in_mid_byte_bit_group.cob</c> and
/// <c>pb3167_aligned_member_in_bit_group_table.cob</c> and <c>BitRunImageDriftTests</c> (BRD09, BRD10, BRD13, BRD14).
/// </summary>
public sealed class BitGroupPhaseDriftTests
{
    private static string BitLayoutText() =>
        File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "Binding", "Model", "BitLayout.cs"));

    [Theory]
    [InlineData("RunBits", "RunBitsAt")]
    [InlineData("TableBits", "TableBitsAt")]
    [InlineData("WidthBits", "WidthBitsAt")]
    [InlineData("StrideBits", "StrideBitsAt")]
    [InlineData("ExtentBits", "ExtentBitsAt")]
    public void EveryPublicReader_AsksPhaseOfAndDelegatesToTheExplicitPhaseForm(string publicName, string explicitName)
    {
        var delegation = new Regex(
            $@"public static int {publicName}\([^)]*\)\s*=>\s*{explicitName}\([^;]*PhaseOf\(", RegexOptions.CultureInvariant);
        Assert.True(delegation.IsMatch(BitLayoutText()),
            $"BitLayout.{publicName} no longer reads {explicitName} at PhaseOf(item): a bit group that begins mid-byte would "
            + "count its byte boundaries from its own start again (kb/Work PB3158).");
    }

    [Fact]
    public void TheWalk_PassesItsPhaseDown_AndNeverAsksPhaseOfPerChild()
    {
        string text = BitLayoutText();
        int start = text.IndexOf("private static int Walk(", StringComparison.Ordinal);
        Assert.True(start >= 0, "BitLayout.Walk was renamed; update this guard with it.");
        int open = text.IndexOf('{', start);
        int depth = 0, end = open;
        for (; end < text.Length; end++)
        {
            if (text[end] == '{') depth++;
            else if (text[end] == '}' && --depth == 0) break;
        }
        string body = text[open..end];
        // The body's REDEFINES chase and the per-child placement read starts and the explicit forms, never PhaseOf.
        Assert.DoesNotMatch(@"\bPhaseOf\(", body);
        Assert.Matches(@"RunBitsAt\(c,\s*cursor", body);
    }

    /// <summary>kb/Work PB3167: laying every occurrence of a table out as the first is exact only because a table
    /// whose layout depends on its phase strides whole bytes. The stride asks THAT question (<c>HoldsByteStart</c>,
    /// read through the walk's own placement predicate), never the ALIGNED clause alone, which left an ALIGNED
    /// MEMBER of a table element inside a byte in every occurrence after the first; and a group keeps its position
    /// only when its first item does (§8.5.1.6.3, both directions of "the same bit position").</summary>
    [Fact]
    public void TheStride_AsksWhetherTheLayoutDependsOnThePhase_AndAGroupBeginsWithItsFirstItem()
    {
        string text = BitLayoutText();
        Assert.Matches(@"private static int StrideBitsAt\(DataItem item, int phase\) =>\s*HoldsByteStart\(item\)", text);
        Assert.Matches(@"KeepsPosition\(c\.Children\[0\]\)", text);
        // One placement dispatch: the walk, PhaseOf and HoldsByteStart ask Continues, never spell its two arms again.
        Assert.Single(Regex.Matches(text, @"prev is null \? !?KeepsPosition"));
        Assert.True(Regex.Matches(text, @"!Continues\(prev, ").Count >= 3, "a placement site no longer asks BitLayout.Continues");
    }
}
