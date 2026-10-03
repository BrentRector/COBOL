// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ A BIT OR NATIONAL GROUP IS THE SAME OPERAND WHETHER OR NOT IT REDEFINES ANOTHER ITEM (kb/Work PB1653).
///
/// <para>ISO §13.18.29.4 GR1 b) / GR2 b): "a national group is treated as though it were an elementary data item of
/// usage national … described with PICTURE N(m)", and a bit group the same with PICTURE 1(m). A group that
/// REDEFINES another item is a Tier-B WINDOW over the class's one byte backing (<c>RedefViewPlace</c>) instead of a
/// record struct, and its VALUE alphabet is its window's coding — boolean positions for a bit group, national
/// positions for a national group — never the 2m storage bytes. The window of a national group once had no coding,
/// so every verb that read or wrote its value saw ten characters for five positions, and each verb had its own
/// way of getting it wrong (a MOVE stored national characters as bytes, ACCEPT poured m device characters through
/// the storage image, a CALL crossed 2m bytes against a formal counting m).</para>
///
/// <para>The net is the VIEW-vs-PLAIN pair, exactly as <c>BitRunImageDriftTests</c>' alias-vs-no-alias pair: the same
/// statements over <c>01 GV REDEFINES AX GROUP-USAGE …</c> and over <c>01 GP GROUP-USAGE …</c> (identical members)
/// must print the same text, and the text is pinned to the value the ISO rules derive (every row's
/// <c>expected</c>), so an agreement of two wrong answers cannot pass. Each row is one statement shape; a new verb
/// or a new group kind is one row, and both lanes are measured on it automatically.</para>
/// </summary>
public sealed class GroupViewAliasDriftTests
{
    /// <summary>GROUP-USAGE, USAGE BIT and USAGE NATIONAL are COBOL-2002 introductions; the value rules are
    /// edition-invariant.</summary>
    private const int Edition = 2002;

    private const string NationalMembers = "05 M1 PIC N(2). 05 M2 PIC N(3).";
    private const string BitMembers = "05 M1 PIC 1(2) USAGE BIT. 05 M2 PIC 1(3) USAGE BIT.";

    [Theory]
    // §14.9.25.4 GR4 — a national group is an elementary item in MOVE: positions 1-2 are M1, 3-5 are M2.
    [InlineData("GVA01", "NATIONAL", NationalMembers, "X PIC X(10)",
        "MOVE N\"ABCDE\" TO {G}. DISPLAY {G} \"|\" M1 \"|\" M2.", "ABCDE|AB|CDE")]
    // §14.9.25.4 GR6 a) — a shorter sender is left-aligned and space-filled, a longer one truncated on the right.
    [InlineData("GVA02", "NATIONAL", NationalMembers, "X PIC X(10)",
        "MOVE N\"XY\" TO {G}. DISPLAY \"[\" {G} \"]\". MOVE N\"1234567\" TO {G}. DISPLAY \"[\" {G} \"]\".",
        "[XY   ]\n[12345]")]
    // §8.4.3.3.4 GR5 a) — reference modification of a national group counts national positions.
    [InlineData("GVA03", "NATIONAL", NationalMembers, "X PIC X(10)",
        "MOVE N\"QRSTU\" TO {G}. DISPLAY {G}(2:3). MOVE N\"MN\" TO {G}(2:2). DISPLAY {G}.", "RST\nQMNTU")]
    // §14.9.22.4 — INSPECT over a national group's characters, in place.
    [InlineData("GVA04", "NATIONAL", NationalMembers, "X PIC X(10)",
        "MOVE N\"HELLO\" TO {G}. INSPECT {G} REPLACING ALL N\"L\" BY N\"Z\". DISPLAY {G}. "
        + "INSPECT {G} CONVERTING N\"HO\" TO N\"ho\". DISPLAY {G}.", "HEZZO\nhEZZo")]
    // §14.9.43.4 / §14.9.48.4 — STRING and UNSTRING deposit national characters, and leave the tail alone.
    [InlineData("GVA05", "NATIONAL", NationalMembers, "X PIC X(10)",
        "MOVE N\"ZYXWV\" TO {G}. STRING N\"123\" DELIMITED SIZE INTO {G}. DISPLAY {G}.", "123WV")]
    // §8.8.4.2 — a comparison reads the national value, not its bytes.
    [InlineData("GVA06", "NATIONAL", NationalMembers, "X PIC X(10)",
        "MOVE N\"ABCDE\" TO {G}. IF {G} = N\"ABCDE\" DISPLAY \"EQ\" ELSE DISPLAY \"NE\" END-IF.", "EQ")]
    // §13.18.29.4 GR1 b) / §8.5.1.6.3 — a bit group is its boolean positions: M1 = bits 1-2, M2 = bits 3-5.
    [InlineData("GVA08", "BIT", BitMembers, "X PIC X(1)",
        "MOVE B\"10110\" TO {G}. DISPLAY {G} \"|\" M1 \"|\" M2.", "10110|10|110")]
    // §8.4.3.3.4 GR5 a) — a bit group's reference modification counts BIT positions.
    [InlineData("GVA09", "BIT", BitMembers, "X PIC X(1)",
        "MOVE B\"10110\" TO {G}. DISPLAY {G}(2:3). MOVE B\"01\" TO {G}(1:2). DISPLAY {G}.", "011\n01110")]
    [InlineData("GVA10", "BIT", BitMembers, "X PIC X(1)",
        "MOVE B\"10110\" TO {G}. IF {G} = B\"10110\" DISPLAY \"EQ\" ELSE DISPLAY \"NE\" END-IF.", "EQ")]
    public void ViewOfGroup_BehavesExactlyAsThePlainGroup(string pid, string usage, string members, string backing,
        string statements, string expected)
    {
        var compiler = new CobolNetCompiler(Edition);
        string view = Run(compiler, pid + "V", usage, members, backing, statements, viaView: true);
        string plain = Run(compiler, pid + "P", usage, members, backing, statements, viaView: false);
        Assert.Equal(Normalize(expected), plain);
        Assert.Equal(plain, view);
    }

    /// <summary>⛔ THE STORAGE CHANNEL, the other half of the pair (kb/Work PB1904). A group's STORAGE image — what a
    /// file record, a group MOVE or a raw-storage function deposits or reads — is not its VALUE: a bit group's is its m
    /// bits PACKED into ceil(m/8) bytes (§13.18.60.4 GR5, "bits shall be used to represent a boolean data item";
    /// §8.5.1.6.3's placement), a national group's its m positions as 2m UTF-16BE bytes. FUNCTION CONVERT with source
    /// format ANY takes an argument "of any usage" whose "contents" need not be valid for it (§15.19.3 r7) and returns
    /// its bits in hexadecimal, "padded with zero bits" to a whole character (§15.19.4 r2) — the storage, so its
    /// hexadecimal form pins the channel for the view and the plain group alike. The bit view once answered its 5 boolean CHARACTERS ("3130313130") where the plain group
    /// answered its one packed byte. CONVERT is a COBOL-2023 function, hence the edition.</summary>
    [Theory]
    // B"10110" packed high-order first into one byte: 1011 0000 = X"B0".
    [InlineData("GVS01", "BIT", BitMembers, "X PIC X(1)",
        "MOVE B\"10110\" TO {G}. DISPLAY FUNCTION CONVERT ({G} ANY ANUM HEX).", "B0")]
    // N"ABCDE" as UTF-16BE pairs (D-N1): 0041 0042 0043 0044 0045.
    [InlineData("GVS02", "NATIONAL", NationalMembers, "X PIC X(10)",
        "MOVE N\"ABCDE\" TO {G}. DISPLAY FUNCTION CONVERT ({G} ANY ANUM HEX).", "00410042004300440045")]
    public void ViewOfGroup_StorageImage_IsThePlainGroups(string pid, string usage, string members, string backing,
        string statements, string expected)
    {
        var compiler = new CobolNetCompiler(2023);
        string view = Run(compiler, pid + "V", usage, members, backing, statements, viaView: true);
        string plain = Run(compiler, pid + "P", usage, members, backing, statements, viaView: false);
        Assert.Equal(Normalize(expected), plain);
        Assert.Equal(plain, view);
    }

    private static string Run(CobolNetCompiler compiler, string pid, string usage, string members, string backing,
        string statements, bool viaView)
    {
        string group = viaView ? "GV" : "GP";
        string declaration = viaView
            ? $"01 {backing}.\n01 GV REDEFINES X GROUP-USAGE {usage}. {members}"
            : $"01 GP GROUP-USAGE {usage}. {members}";
        string body = statements.Replace("{G}", group);
        string source = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. {pid}.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            {declaration}
            PROCEDURE DIVISION.
            MAIN.
            {body}
                STOP RUN.
            """;
        var (ok, output, detail) = compiler.CompileAndRun(source);
        Assert.True(ok, $"{pid}: {detail}");
        return Normalize(output);
    }

    private static string Normalize(string s) => s.Replace("\r\n", "\n").TrimEnd('\n');
}
