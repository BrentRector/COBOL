// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ ONE GROUP, TWO COMPOSERS, ONE ANSWER — the drift net under kb/Work PB584's mechanism.
///
/// <para>ISO §8.5.1.6.3 puts "an elementary bit data item immediately following an elementary bit data item or
/// bit group item of the same level" at the next BIT position, so consecutive same-level bit members SHARE a
/// byte. A group's layout is therefore a run walk, and WiseOwl COBOL composes that group's character image TWICE:
/// the record-struct lane emits <c>AsImage()</c> over <c>PhysicalModel</c>'s physical fields, and the
/// compile-time SEED (<c>GroupImageCodec.ImageInitOf</c>) composes the string a Tier-B REDEFINES backing, an
/// EXTERNAL cell, a BASED cell or an OO backing starts from. Only the first knew about runs, so declaring a
/// REDEFINES alias over a MIXED bit/character group silently changed what its members held.</para>
///
/// <para>The net is the ALIAS-vs-NO-ALIAS pair: the identical declaration with and without
/// <c>01 V REDEFINES G PIC X(n).</c> must read back identically, because §13.18.44.4 GR1's storage association
/// adds a description of the same area and no data. Each case below is one member shape; adding a shape to the
/// table measures both lanes on it automatically, which is the property the run law's single computation
/// (<see cref="CobolNet.Binding.Model.BitLayout"/>'s run map) exists to keep true.</para>
/// </summary>
public sealed class BitRunImageDriftTests
{
    /// <summary>Boolean data (PICTURE symbol 1 / USAGE BIT) and GROUP-USAGE are COBOL-2002 introductions, so
    /// every program here compiles at the introducing edition; the composition rule itself is edition-invariant.
    /// </summary>
    private const int Edition = 2002;

    [Theory]
    // A two-member run plus a character member — the note's own shape. §8.5.1.6.3 gives M1|M2 one byte and M3
    // the next; the seed used to give each bit member a byte of its own.
    [InlineData("BRD01", 2,
        "05 M1 PIC 1(4) USAGE BIT VALUE B\"0100\". 05 M2 PIC 1(4) USAGE BIT VALUE B\"0001\". 05 M3 PIC X(1) VALUE \"B\".",
        "M1|M2|M3")]
    // The VALUE forms on a bit leaf: §8.3.3.6.4 GR4's zero format over boolean positions, and GR2's Format-6
    // ALL literal-1 repeated to them — the two arms the seed lanes each had one of.
    [InlineData("BRD02", 2,
        "05 M1 PIC 1(4) USAGE BIT VALUE ZERO. 05 M2 PIC 1(4) USAGE BIT VALUE ALL B\"1\". 05 M3 PIC X(1) VALUE \"C\".",
        "M1|M2|M3")]
    // A THREE-member run crossing a byte: 12 bits is ceil(12/8) = 2 characters, and the character member then
    // starts at "the first bit position of the first available byte".
    [InlineData("BRD03", 3,
        "05 M1 PIC 1(4) USAGE BIT VALUE B\"1100\". 05 M2 PIC 1(4) USAGE BIT VALUE B\"0011\"."
        + " 05 M3 PIC 1(4) USAGE BIT VALUE B\"1010\". 05 M4 PIC X(1) VALUE \"D\".",
        "M1|M2|M3|M4")]
    // A character member FIRST — the run does not start at the group's first byte.
    [InlineData("BRD04", 2,
        "05 M1 PIC X(1) VALUE \"A\". 05 M2 PIC 1(4) USAGE BIT VALUE B\"1010\". 05 M3 PIC 1(4) USAGE BIT VALUE B\"0101\".",
        "M1|M2|M3")]
    // A bit GROUP and a bit LEAF at the same level: §8.5.1.6.3 rule 1 names "an elementary bit data item or bit
    // group item of the same level", so both are run members (D20/PB79).
    [InlineData("BRD05", 2,
        "05 M1 GROUP-USAGE BIT. 10 N1 PIC 1(4) VALUE B\"1100\". 05 M2 PIC 1(4) USAGE BIT VALUE B\"0011\"."
        + " 05 M3 PIC X(1) VALUE \"E\".",
        "N1|M2|M3")]
    // A fixed-OCCURS bit member: §13.18.63.4 GR9 gives every occurrence the VALUE and the run carries them all.
    [InlineData("BRD06", 2,
        "05 M1 PIC 1(4) USAGE BIT OCCURS 2 VALUE B\"1010\". 05 M2 PIC X(1) VALUE \"F\".",
        "M1 (1)|M1 (2)|M2")]
    // ⛔ PB1572 — a bit GROUP CONTINUING a run, then a bit leaf after it: the group sits at bit 1 and its FIRST
    // member shares the group's start bit ("the alignment of the start of a group item and the alignment of the
    // first item within that group, when the first item is a bit data item, are at the same bit position in
    // storage"). The REDEFINES class walk used to round the group's absolute bit up to a byte before its first
    // member, so N1 read bit 8 and the aliased group answered differently from its twin.
    [InlineData("BRD07", 1,
        "05 M1 PIC 1 USAGE BIT VALUE B\"1\". 05 M2 GROUP-USAGE BIT. 10 N1 PIC 1 USAGE BIT VALUE B\"0\"."
        + " 10 N2 PIC 1 USAGE BIT VALUE B\"1\". 05 M3 PIC 1 USAGE BIT VALUE B\"1\".",
        "M1|N1|N2|M2|M3")]
    // The same law one level down: S6 continues Q1's run INSIDE S5, which itself starts mid-byte, so Q2 sits at
    // the sum of two relative placements — the shape a per-level absolute round-up gets wrong twice.
    [InlineData("BRD08", 1,
        "05 M1 PIC 1(4) USAGE BIT VALUE B\"1010\". 05 S5 GROUP-USAGE BIT. 10 Q1 PIC 1(2) USAGE BIT VALUE B\"01\"."
        + " 10 S6 GROUP-USAGE BIT. 15 Q2 PIC 1 USAGE BIT VALUE B\"1\".",
        "M1|Q1|Q2|S6|S5")]
    public void AliasedGroup_ReadsBackExactlyAsItsUnaliasedTwin(string pid, int width, string members, string reads)
    {
        string aliased = Program(pid + "A", members, reads, $"01 V REDEFINES G PIC X({width}).");
        string plain = Program(pid + "P", members, reads, null);
        var compiler = new CobolNetCompiler(Edition);
        var (okA, outA, detailA) = compiler.CompileAndRun(aliased);
        var (okP, outP, detailP) = compiler.CompileAndRun(plain);
        Assert.True(okA, detailA);
        Assert.True(okP, detailP);
        Assert.Equal(Normalize(outP), Normalize(outA));
    }

    /// <summary>⛔ THE RUN LAW HAS ONE COMPUTATION AND BOTH COMPOSERS ASK IT. The behavioural theory above
    /// catches the shapes it enumerates; this catches the RE-INLINING that put them out of step in the first
    /// place — <c>PhysicalModel</c> owned a private run scan and the image seed had none, so the two could not
    /// be compared at all. If either composer stops consulting <c>BitLayout.RunsOf</c>, it is answering
    /// §8.5.1.6.3 by itself again.</summary>
    [Fact]
    public void BothImageComposers_AskTheOneRunLaw()
    {
        foreach (string rel in new[] { Path.Combine("CodeGen", "DataDivision", "PhysicalModel.cs"),
                                       Path.Combine("CodeGen", "DataDivision", "GroupImageCodec.cs") })
        {
            string text = File.ReadAllText(TestRepo.Src(Path.Combine("Cobol.Net.Compiler", rel)));
            Assert.True(Regex.IsMatch(text, @"BitLayout\.RunsOf\s*\("),
                $"{rel} composes a group image but no longer asks BitLayout.RunsOf (ISO §8.5.1.6.3).");
        }
    }

    /// <summary>⛔ THE §8.5.1.6.3 CURSOR HAS ONE WALK (kb/Work PB1572). <c>BitLayout</c>'s private walk is the only
    /// code that rounds a placement up to a byte or asks whether a member shares its predecessor's byte; the
    /// extent (<c>ExtentBits</c>), the in-group offset (<c>StartBitWithin</c>) and the REDEFINES-class offsets
    /// (<c>DataBinder.AssignClassBitOffsets</c>, through <c>ChildStarts</c>) all read it. The class walk once kept
    /// a third cursor seeded with the group's ABSOLUTE bit offset and rounded that up, which placed a bit group's
    /// first member a byte too far. Any caller outside <c>BitLayout.cs</c> spelling either call is a second walk.</summary>
    [Fact]
    public void NoCodeOutsideBitLayout_RunsItsOwnPlacementCursor()
    {
        string root = TestRepo.Src();
        string layout = Path.GetFullPath(TestRepo.Src("Cobol.Net.Compiler", "Binding", "Model", "BitLayout.cs"));
        var offenders = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                        && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
            .Where(f => !string.Equals(Path.GetFullPath(f), layout, StringComparison.OrdinalIgnoreCase))
            .Where(f => Regex.IsMatch(File.ReadAllText(f), @"\b(SharesByteWith|RoundUpToByte)\s*\("))
            .Select(f => Path.GetRelativePath(root, f))
            .ToList();
        Assert.True(offenders.Count == 0,
            "a second ISO 8.5.1.6.3 placement cursor: " + string.Join(", ", offenders)
            + " calls SharesByteWith/RoundUpToByte outside BitLayout.cs — read BitLayout.ChildStarts / StartBitWithin instead.");
        string binder = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "Binding", "DataBinder.cs"));
        Assert.Matches(@"BitLayout\.ChildStarts\s*\(", binder);
    }

    private static string Normalize(string s) => s.Replace("\r\n", "\n").TrimEnd('\n');

    /// <summary>The program under test, FREE FORM (the harness's own convention — see
    /// <c>SendingValueOnceDriftTests</c>): one group, the member declarations under test, optionally the
    /// REDEFINES alias, and one DISPLAY of every read.</summary>
    private static string Program(string pid, string members, string reads, string? alias)
    {
        string display = string.Join(" \"][\" ",
            reads.Split('|', StringSplitOptions.RemoveEmptyEntries).Select(r => r.Trim()));
        return $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. {pid}.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 G.
            {members}
            {alias ?? "*> no alias - the record-struct lane"}
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY "[" {display} "]".
                STOP RUN.
            """;
    }
}
