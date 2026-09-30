// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using Xunit;
using CobolClass = CobolNet.Runtime.CobolClass;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The CODED CHARACTER SET an alphabet references (ISO §12.3.7.4 GR7 + Table 6; kb/Work PB110) and the runtime
/// membership test of the alphabet-name class condition (§8.8.4.4.4 GR3 a; kb/Work PB109): the ordinal↔character
/// correspondence per phrase (the CONFORMANCE.md determinations) and the three membership kinds.
/// </summary>
public sealed class CodedCharacterSetTests
{
    /// <summary>§13.18.13.4 GR6's medium classification (kb/Work PB1542): an identity set SMALLER than the one-byte
    /// record channel — STANDARD-1 / STANDARD-2 (GR7 c) and the ASCII code-name — carries its identity table so
    /// the connector can refuse what the set lacks; the whole-channel identity sets carry nothing; EBCDIC carries
    /// its 256-entry page; a literal-phrase alphabet and UTF-8 / UCS-4 stay the A.3 item 27 non-support.</summary>
    [Fact]
    public void Medium_CarriesACorrespondence_ExactlyWhenTheSetIsNotTheWholeChannelIdentity()
    {
        foreach (var iso646 in new[]
                 {
                     new CodedCharacterSet("STANDARD-1", National: false, null),
                     new CodedCharacterSet("STANDARD-2", National: false, null),
                     new CodedCharacterSet("ASCII", National: false, null, ImplementorCodeNames.Ascii),
                 })
        {
            Assert.Equal(CodeSetMedium.Translated, iso646.Medium);
            Assert.Equal(Enumerable.Range(0, 128).Select(u => (char)u), iso646.MediumCorrespondence!);
        }
        foreach (var whole in new[]
                 {
                     new CodedCharacterSet("NATIVE", National: false, null),
                     new CodedCharacterSet("UTF-16", National: true, null),
                 })
        {
            Assert.Equal(CodeSetMedium.Identity, whole.Medium);
            Assert.Null(whole.MediumCorrespondence);
        }
        var ebcdic = new CodedCharacterSet("EBCDIC", National: false, ImplementorCodeNames.Ebcdic.Table, ImplementorCodeNames.Ebcdic);
        Assert.Equal(CodeSetMedium.Translated, ebcdic.Medium);
        Assert.Equal(256, ebcdic.MediumCorrespondence!.Length);
        Assert.Equal(CodeSetMedium.NotProvided, new CodedCharacterSet("UTF-8", National: true, null).Medium);
        Assert.Null(new CodedCharacterSet("UTF-8", National: true, null).MediumCorrespondence);
    }

    /// <summary>The identity sets: ordinal n is code unit / scalar n−1; STANDARD-1/2 stop at 128 (ISO/IEC 646 IRV);
    /// UCS-4/UTF-8 ordinals skip the surrogate block (not scalar values) and reach the supplementary planes as
    /// surrogate PAIRS (one character, two code units).</summary>
    [Fact]
    public void CharAt_FollowsEachPhrasesCorrespondence()
    {
        var std = new CodedCharacterSet("STANDARD-1", National: false, null);
        Assert.Equal(128, std.OrdinalCount);
        Assert.Equal("\0", std.CharAt(1));
        Assert.Equal("A", std.CharAt(66));
        Assert.Equal("", std.CharAt(128));
        Assert.Null(std.CharAt(129));
        Assert.Null(std.CharAt(0));

        var native = new CodedCharacterSet("NATIVE", National: false, null);
        Assert.Equal(65536, native.OrdinalCount);
        Assert.Equal("é", native.CharAt(0xEA));           // ordinal 234 → U+00E9
        Assert.Equal("￿", native.CharAt(65536));

        var ucs4 = new CodedCharacterSet("UCS-4", National: true, null);
        Assert.Equal("A", ucs4.CharAt(66));
        Assert.Equal("퟿", ucs4.CharAt(0xD800));       // the last BMP scalar before the surrogate block
        Assert.Equal("", ucs4.CharAt(0xD801));       // the block is SKIPPED — surrogates are not characters
        Assert.Equal("\U00010000", ucs4.CharAt(0xD801 + 0x2000));   // a supplementary character is ONE ordinal, a PAIR of code units
    }

    /// <summary>A literal-phrase alphabet's set: the ordinals ARE the collating positions + 1 (GR7 k4's determination) —
    /// the specified characters by RepByPos, the unspecified tail arithmetically (GR7 k3's placement).</summary>
    [Fact]
    public void CharAt_LiteralAlphabet_UsesTheCollatingPositions()
    {
        // ALPHABET "Z" THRU "A" (the pb110 corpus shape): the SPARSE table specifies codes 'A'..'Z' at positions
        // 25..0; every unspecified character follows in native order (GR7 k3), the first of them at position 26.
        var codes = Enumerable.Range('A', 26).Select(c => (ushort)c).ToArray();
        var positions = codes.Select(c => (ushort)('Z' - c)).ToArray();
        var rep = Enumerable.Range(0, 26).Select(i => (ushort)('Z' - i)).ToArray();
        var table = new CollatingTable(codes, positions, rep, 26, HighValue: (char)255, LowValue: 'Z');
        var set = new CodedCharacterSet("literal-phrase", National: false, table);
        Assert.Equal("Z", set.CharAt(1));                   // position 0
        Assert.Equal("A", set.CharAt(26));                  // position 25
        Assert.Equal("\0", set.CharAt(27));                 // the first unspecified char (code 0)
        Assert.Equal("a", set.CharAt(27 + 'a' - 26));       // code 0x61: 26 specified codes below it
        Assert.Equal(65536, set.OrdinalCount);

        // The NATIONAL sparse table's inverse (the FOR NATIONAL literal-phrase alphabet): specified codes 65/90 at
        // positions 0/1; every unspecified code unit c takes position NextFree + (c - |specified < c|).
        var nat = new CollatingTable(Codes: [65, 90], Positions: [1, 0], RepByPos: [90, 65], NextFree: 2,
            HighValue: (char)0xFFFF, LowValue: 'Z');
        var natSet = new CodedCharacterSet("literal-phrase", National: true, nat);
        Assert.Equal("Z", natSet.CharAt(1));                 // position 0
        Assert.Equal("A", natSet.CharAt(2));                 // position 1
        Assert.Equal("\0", natSet.CharAt(3));                // the first unspecified code unit (0)
        Assert.Equal("B", natSet.CharAt(3 + 66 - 1));        // code 66: 65 specified codes below it... position = 2 + (66 - 1)
    }

    /// <summary>kb/Work PB1557 — an alphabet that specifies EVERY native character (legal: §12.3.7.3 SR14 b4/c4
    /// bound the count by the native set's size with "shall not exceed") has 65,536 positions, one more than a
    /// 16-bit counter holds. The builders counted it in one and handed <see cref="CollatingTable.Build"/> a
    /// NextFree of 0, so the table claimed no specified block and every ordinal of its set "did not exist". The
    /// count is an <c>int</c> now, and Build REFUSES a count that disagrees with the positions it is given, so a
    /// builder that miscounts fails in the compiler, never in a user's program.</summary>
    [Fact]
    public void Build_FullRepertoire_HasEveryOrdinal_AndRefusesAMiscount()
    {
        // ALPHABET … IS 65536 THRU 1: native code unit c (ordinal c+1) at position 65535 − c.
        var pos = new Dictionary<char, int>(CollatingTable.Repertoire);
        var order = new List<char>(CollatingTable.Repertoire);
        for (int c = CollatingTable.Repertoire - 1; c >= 0; c--)
        {
            pos[(char)c] = CollatingTable.Repertoire - 1 - c;
            order.Add((char)c);
        }
        var table = CollatingTable.Build(pos, order, order, CollatingTable.Repertoire);
        Assert.Equal(CollatingTable.Repertoire, table.NextFree);

        var set = new CodedCharacterSet("literal-phrase", National: true, table);
        Assert.Equal(((char)0xFFFF).ToString(), set.CharAt(1));                   // position 0
        Assert.Equal(((char)0xFFBE).ToString(), set.CharAt(66));                  // native ordinal 65537 − 66
        Assert.Equal("\0", set.CharAt(CollatingTable.Repertoire));                // position 65535
        Assert.Null(set.CharAt(CollatingTable.Repertoire + 1));

        // The runtime carrier reads the same count: CHAR(66) → U+FFBE and ORD of it → 66 (§15.15.4 r1 / §15.70.1).
        var runtime = table.Collation(national: true);
        Assert.Equal(0xFFBE, runtime.CharAt(65));
        Assert.Equal(65, runtime.Weight((char)0xFFBE));

        // The wrapped count the 16-bit builder used to pass is refused, not tabulated.
        Assert.Throws<ArgumentOutOfRangeException>(() => CollatingTable.Build(pos, order, order, 0));
    }

    /// <summary>The runtime membership kinds (§8.8.4.4.4 GR3 a — kb/Work PB109): Ascii = the 128 ISO 646 characters;
    /// ScalarValues = well-formed UTF-16 (an unpaired surrogate is not a character of UCS-4/UTF-8); AllNative is total;
    /// a zero-length operand is FALSE (GR1).</summary>
    [Fact]
    public void IsInCodedSet_ThreeKinds()
    {
        Assert.True(CobolClass.IsInCodedSet("Hi !~\t", CobolClass.CodedSetKind.Ascii));
        Assert.False(CobolClass.IsInCodedSet("café", CobolClass.CodedSetKind.Ascii));
        Assert.False(CobolClass.IsInCodedSet("", CobolClass.CodedSetKind.Ascii));
        Assert.False(CobolClass.IsInCodedSet(null, CobolClass.CodedSetKind.AllNative));
        Assert.True(CobolClass.IsInCodedSet("café￿", CobolClass.CodedSetKind.AllNative));
        Assert.True(CobolClass.IsInCodedSet("a\U00010000b", CobolClass.CodedSetKind.ScalarValues));
        Assert.False(CobolClass.IsInCodedSet("a\uD800b", CobolClass.CodedSetKind.ScalarValues));    // unpaired high
        Assert.False(CobolClass.IsInCodedSet("a\uDC00", CobolClass.CodedSetKind.ScalarValues));     // unpaired low
    }
}
