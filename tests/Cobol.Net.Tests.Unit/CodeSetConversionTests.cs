// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using CobolNet.Runtime.IO;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ISO §13.18.13.4 GR6 b — "<i>On output, each native coded character in the record is replaced for the storage
/// medium with its associated coded character as defined in the alphabet being used</i>" — pinned on
/// <see cref="CodeSetConversion"/>'s two separate answers (kb/Work PB1150, PB1542): MEMBERSHIP, the one screen every
/// WRITE and REWRITE asks ('91'; '71' / '09' on a line sequential file), and the channel ENCODING, a bijection so a
/// store that re-persists every record at CLOSE rewrites a record it never changed byte-exactly. The goldens
/// <c>85/pb1542_code_set_unrepresentable_85</c> and <c>2023/pb1542_code_set_line_sequential</c> drive the same rules
/// through generated programs; these tests hold the type's contract where a golden cannot see it.
/// </summary>
public sealed class CodeSetConversionTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "CobolNet_CSC_" + Guid.NewGuid().ToString("N")[..8]);

    public CodeSetConversionTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }

    private static readonly char Euro = (char)0x20AC;
    private static readonly char EAcute = (char)0x00E9;

    /// <summary>ISO/IEC 646 IRV (STANDARD-1 / STANDARD-2 / ASCII): the identity over 128 characters, exactly what
    /// <c>CodedCharacterSet.MediumCorrespondence</c> emits for those sets.</summary>
    private static CodeSetConversion Iso646() => new([.. Enumerable.Range(0, 128).Select(u => (char)u)]);

    /// <summary>CCSID 37, the set the EBCDIC code-name names (<c>ImplementorCodeNames.EbcdicCodePage</c>).</summary>
    private static CodeSetConversion Ebcdic()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return new(Encoding.GetEncoding(37).GetChars([.. Enumerable.Range(0, 256).Select(b => (byte)b)]));
    }

    [Fact]
    public void Iso646_RepresentsExactlyItsOwn128Characters()
    {
        var c = Iso646();
        Assert.True(c.Represents('~'));
        Assert.True(c.Represents((char)0x7F));
        Assert.False(c.Represents((char)0x80));
        Assert.False(c.Represents(EAcute));
        Assert.False(c.Represents(Euro));
        Assert.False(c.HasCharacterWithoutImage("AB~"));
        Assert.True(c.HasCharacterWithoutImage("A" + EAcute + "B"));
    }

    /// <summary>A medium byte the set does not define reads as the native character of the same value and is
    /// written back to the same byte — the channel encoding is a bijection even where membership says no.</summary>
    [Fact]
    public void Iso646_ChannelEncodingRoundTripsAByteOutsideTheSet()
    {
        var c = Iso646();
        string medium = "X" + (char)0xE9 + "Y";
        string native = c.ToNative(medium);
        Assert.Equal("X" + EAcute + "Y", native);
        Assert.Equal(medium, c.ToMedium(native));
    }

    [Fact]
    public void Ebcdic_RepresentsTheWholeChannel_AndNothingAboveIt()
    {
        var c = Ebcdic();
        for (int n = 0; n < CodeSetConversion.ChannelUnits; n++) Assert.True(c.Represents((char)n));
        Assert.False(c.Represents(Euro));
        Assert.Equal(new string([(char)0xD2, (char)0x51]), c.ToMedium("K" + EAcute));   // CCSID 37: K = X'D2', é = X'51'
        Assert.Equal("K" + EAcute, c.ToNative(new string([(char)0xD2, (char)0x51])));
    }

    /// <summary>The guard: a character above the one-byte channel reaching the encoding means a write path skipped
    /// the statement's screen, and it fails loudly rather than writing a substitute.</summary>
    [Fact]
    public void ToMedium_OfACharacterAboveTheChannel_IsAnInvariantViolation() =>
        Assert.Throws<InvalidOperationException>(() => Ebcdic().ToMedium("A" + Euro));

    /// <summary>A correspondence whose member maps onto the value of a code unit OUTSIDE the set would give two
    /// medium units one native character under the bijection — refused at construction.</summary>
    [Fact]
    public void Constructor_RefusesACorrespondenceTheChannelCannotInvert()
    {
        Assert.Throws<ArgumentException>(() => new CodeSetConversion([(char)0x80]));
        Assert.Throws<ArgumentException>(() => new CodeSetConversion(['A', 'A']));
        Assert.Throws<ArgumentException>(() => new CodeSetConversion([Euro]));
        Assert.Throws<ArgumentException>(() => new CodeSetConversion([]));
    }

    /// <summary>kb/Work PB1150: an INDEXED store reaches the medium only at CLOSE, so the screen must be the
    /// WRITE's — '91' at the statement, and a CLOSE that persists the accepted records with '00' (the defect
    /// answered '00' at the WRITE and died of an unhandled exception in the CLOSE).</summary>
    [Fact]
    public void IndexedWrite_RefusesAnUnrepresentableRecord_AtTheStatement_AndTheCloseSucceeds()
    {
        string host = Path.Combine(_dir, "ix.dat");
        var ix = new IndexedConnector(host, 8, KeyedAccess.Random, 0, 4) { CodeSet = Ebcdic() };
        Assert.Equal(FileStatusCode.Success, ix.Open(FileOpenMode.Output));
        Assert.Equal(FileStatusCode.CharacterWithoutByteImage, ix.Write("K" + Euro + "01DATA"));
        Assert.Equal(FileStatusCode.Success, ix.Write("K001DATA"));
        Assert.Equal(FileStatusCode.Success, ix.Close());
        Assert.Equal(FileStatusCode.Success, ix.Open(FileOpenMode.IO));
        Assert.Equal(FileStatusCode.CharacterWithoutByteImage, ix.Rewrite("K001D" + Euro + "TA"));
        Assert.Equal(FileStatusCode.Success, ix.ReadRandom(-1, "K001    ", out string rec));
        Assert.Equal("K001DATA", rec);
        Assert.Equal(FileStatusCode.Success, ix.Close());
    }

    /// <summary>HIGH-VALUE crosses a CODE-SET medium as that coded character set's HIGHEST code unit (owner decision
    /// kb/Work R51 item 4): 0xFF for a complete single-byte code, 0x7F for ISO/IEC 646 — a member of every alphabet,
    /// written as that unit and read back as U+FFFF (kb/Work PB1759).</summary>
    [Fact]
    public void HighValue_IsTheCodeSetsHighestUnit()
    {
        const char hv = '\uFFFF';
        Assert.True(Iso646().Represents(hv));
        Assert.True(Ebcdic().Represents(hv));
        Assert.Equal("A\u007F", Iso646().ToMedium("A" + hv));
        Assert.Equal('\u00FF', Ebcdic().ToMedium(hv.ToString())[0]);
        Assert.Equal(hv, Iso646().ToNative("\u007F")[0]);
        Assert.Equal(hv, Ebcdic().ToNative("\u00FF")[0]);
    }

    /// <summary>The line sequential character set's alphanumeric ceiling is the file's coded character set: with
    /// a STANDARD-1 CODE-SET, U+00E9 is outside ('71' / '09'); under EBCDIC it is inside.</summary>
    [Fact]
    public void LineSequentialCharacterSet_CeilingIsTheCodeSetAlphabet()
    {
        Assert.True(LineSequentialCharacterSet.HasCharacterOutside("A" + EAcute, national: false, Iso646()));
        Assert.False(LineSequentialCharacterSet.HasCharacterOutside("AB~", national: false, Iso646()));
        Assert.False(LineSequentialCharacterSet.HasCharacterOutside("A" + EAcute, national: false, Ebcdic()));
        Assert.True(LineSequentialCharacterSet.HasCharacterOutside("A" + Euro, national: false, Ebcdic()));
    }
}
