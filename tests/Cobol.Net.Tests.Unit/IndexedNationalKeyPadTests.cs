// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB679 — a short record is extended with the RECORD AREA's own space wherever a connector pads one to a
/// span, and on a NATIONAL record area that space is the two bytes 0x00 0x20 (ISO §14.9.30.4 GR15: "If the
/// record-area associated with file-name-1 is specified implicitly or explicitly as national, a trailing space is
/// defined to be the national space character"). The byte-level <c>PadRight(' ')</c> an indexed key slice used
/// manufactured U+2020 positions inside a national key window, which no space-padded key equals.
/// <para>The witness is a RECORD VARYING indexed file whose national prime key spans three national positions
/// (six bytes) and whose stored record is ONE position (two bytes) long: the key a WRITE stores and the key a
/// random READ builds from the same short operand must be the same value, which they are only if both pads are the
/// national one.</para>
/// </summary>
public sealed class IndexedNationalKeyPadTests
{
    private const int Dynamic = 2;   // KeyedAccess.Dynamic
    private const int PrimeKey = -1;

    private static string Tmp() => Path.Combine(Path.GetTempPath(), $"pb679-{Guid.NewGuid():N}.dat");

    /// <summary>One national position, "A" = 0x00 0x41, in a record area whose key spans three positions.</summary>
    private const string ShortNationalRecord = "\0A";

    private static (FileRegistry reg, string host) OpenIo(bool national)
    {
        var reg = new FileRegistry();
        string host = Tmp();
        // 10-byte record area, prime key at offset 0 for 6 bytes, RECORD VARYING 2..10.
        reg.RegisterIndexed("X", host, 10, false, Dynamic, 0, 6, 2, 10);
        if (national) reg.RegisterNationalArea("X");
        reg.OpenStatic("X", FileOpenMode.Output);
        Assert.Equal("00", reg.WriteKeyed("X", ShortNationalRecord, ShortNationalRecord.Length));
        reg.Close("X");
        reg.OpenStatic("X", FileOpenMode.IO);
        return (reg, host);
    }

    /// <summary>On a national record area the stored key (its record shorter than the key span) and the operand
    /// key (the area image fitted to the width) are both padded with national spaces, so the READ finds the
    /// record. Before the fix the stored key was <c>00 41 20 20 20 20</c> and the READ answered '23'.</summary>
    [Fact]
    public void NationalRecordArea_ShortRecord_KeyIsPaddedWithNationalSpaces_SoTheReadFindsIt()
    {
        var (reg, host) = OpenIo(national: true);
        try
        {
            Assert.Equal("00", reg.ReadKeyed("X", PrimeKey, ShortNationalRecord, out string image));
            Assert.Equal(ShortNationalRecord, reg.CurrentRecord("X"));
            Assert.Equal(10, image.Length);
            // The area image a READ hands back is padded with the SAME space: pairs of 00 20 after the record.
            Assert.Equal("\0A\0 \0 \0 \0 ", image);
        }
        finally { try { File.Delete(host); } catch { } }
    }

    /// <summary>The control: an ALPHANUMERIC record area keeps the alphanumeric space. The same two characters
    /// written and read back pad with 0x20, and the READ finds the record too — the fix changed which space, not
    /// whether there is one.</summary>
    [Fact]
    public void AlphanumericRecordArea_ShortRecord_KeepsThe0x20Pad()
    {
        var (reg, host) = OpenIo(national: false);
        try
        {
            Assert.Equal("00", reg.ReadKeyed("X", PrimeKey, ShortNationalRecord, out string image));
            Assert.Equal("\0A        ", image);
        }
        finally { try { File.Delete(host); } catch { } }
    }

    /// <summary>The blank record area an UNSUCCESSFUL READ hands back (§14.9.30.4 GR18) is the area's own space
    /// too: national spaces on a national area, never a string of U+2020 positions.</summary>
    [Fact]
    public void UnsuccessfulRead_NationalRecordArea_IsBlankedWithNationalSpaces()
    {
        var (reg, host) = OpenIo(national: true);
        try
        {
            Assert.Equal("23", reg.ReadKeyed("X", PrimeKey, "\0B", out string image));   // no such key
            Assert.Equal("\0 \0 \0 \0 \0 ", image);
        }
        finally { try { File.Delete(host); } catch { } }
    }
}
