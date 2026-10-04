// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The per-PHYSICAL-FILE keyed record store (kb/Work PB143; ISO §14.9.10.4 GR5 — a deleted record "has been
/// logically removed from the physical file and can no longer be accessed"). Before, every connector held a
/// PRIVATE snapshot loaded at OPEN and persisted WHOLE at CLOSE: a record DELETEd through one connector stayed
/// readable through another over the same host path, and the CLOSE ORDER decided which private view survived on
/// disk. Pinned over a private <see cref="FileRegistry"/> with TWO connectors bound to ONE host path, each
/// declaring SHARING WITH ALL OTHER: since kb/Work PB322 a connector with no SHARING specification is SHARING WITH
/// NO OTHER in every mode but INPUT, so Table 19 would refuse the second connector's OPEN (the defect itself
/// needed no sharing mode, only two connectors that could be open at once). The end-to-end trace rides the
/// 2002/pb143_shared_visibility golden.
/// </summary>
public sealed class SharedRecordStoreTests
{
    private const int Random = 1;   // KeyedAccess.Random

    private static string Tmp(string tag) =>
        Path.Combine(Path.GetTempPath(), $"pb143-{tag}-{Guid.NewGuid():N}.dat");

    /// <summary>Two RANDOM-access relative connectors over one host, records 1..n preloaded through A.</summary>
    private static (FileRegistry reg, string host) TwoConnectors(params string[] records)
    {
        var reg = new FileRegistry();
        string host = Tmp("two");
        reg.RegisterRelative("A", host, 8, false, Random, 4, -1, -1);
        reg.RegisterRelative("B", host, 8, false, Random, 4, -1, -1);
        reg.RegisterSharing("A", FileSharing.AllOther, FileLockMode.Manual, multiple: false);
        reg.RegisterSharing("B", FileSharing.AllOther, FileLockMode.Manual, multiple: false);
        reg.OpenStatic("A", FileOpenMode.Output);
        for (int i = 0; i < records.Length; i++)
        {
            reg.SetRelativeKey("A", i + 1);
            reg.WriteKeyed("A", records[i].PadRight(8), -1);
        }
        reg.Close("A");
        reg.OpenStatic("A", FileOpenMode.IO);
        reg.OpenStatic("B", FileOpenMode.IO);
        return (reg, host);
    }

    // GR5: a record deleted through connector A can no longer be accessed through connector B.
    [Fact]
    public void Delete_ThroughOneConnector_IsGoneForTheOther()
    {
        var (reg, host) = TwoConnectors("ALPHA", "BETA");
        try
        {
            reg.SetRelativeKey("A", 1);
            Assert.Equal("00", reg.DeleteRecord("A", ""));
            reg.SetRelativeKey("B", 1);
            Assert.Equal(FileStatusCode.RecordNotFound, reg.ReadKeyed("B", 0, "", out _));
            reg.SetRelativeKey("B", 2);
            Assert.Equal("00", reg.ReadKeyed("B", 0, "", out string img));   // the survivor is intact
            Assert.Equal("BETA    ", img);
        }
        finally { reg.Close("A"); reg.Close("B"); try { File.Delete(host); } catch { } }
    }

    // The write twin: a record WRITTEN through A is readable through B without any reopen.
    [Fact]
    public void Write_ThroughOneConnector_IsVisibleToTheOther()
    {
        var (reg, host) = TwoConnectors("ALPHA");
        try
        {
            reg.SetRelativeKey("A", 5);
            Assert.Equal("00", reg.WriteKeyed("A", "GAMMA".PadRight(8), -1));
            reg.SetRelativeKey("B", 5);
            Assert.Equal("00", reg.ReadKeyed("B", 0, "", out string img));
            Assert.Equal("GAMMA   ", img);
        }
        finally { reg.Close("A"); reg.Close("B"); try { File.Delete(host); } catch { } }
    }

    // The DURABILITY half: with one shared state, the close ORDER cannot resurrect a deleted record or drop
    // another connector's write (before, the last-closed connector's private view won the disk).
    [Fact]
    public void CloseOrder_CannotResurrectADeleteOrDropAWrite()
    {
        var (reg, host) = TwoConnectors("ALPHA", "BETA");
        try
        {
            reg.SetRelativeKey("B", 1);
            Assert.Equal("00", reg.DeleteRecord("B", ""));       // B deletes slot 1
            reg.SetRelativeKey("A", 3);
            Assert.Equal("00", reg.WriteKeyed("A", "GAMMA".PadRight(8), -1));   // A writes slot 3
            reg.Close("A");                                       // the order that USED to resurrect slot 1
            reg.Close("B");
            reg.OpenStatic("A", FileOpenMode.Input);                    // last detach dropped the store — reloads disk
            reg.SetRelativeKey("A", 1);
            Assert.Equal(FileStatusCode.RecordNotFound, reg.ReadKeyed("A", 0, "", out _));
            reg.SetRelativeKey("A", 3);
            Assert.Equal("00", reg.ReadKeyed("A", 0, "", out string img));
            Assert.Equal("GAMMA   ", img);
            reg.Close("A");
        }
        finally { try { File.Delete(host); } catch { } }
    }

    // The INDEXED twin with the shared arrival mint: records written through both connectors interleave into
    // ONE arrival order, and both are present after the shared close.
    [Fact]
    public void Indexed_SharedStore_WritesInterleaveAndPersist()
    {
        var reg = new FileRegistry();
        string host = Tmp("ix");
        reg.RegisterIndexed("A", host, 8, false, Random, 0, 2, -1, -1);
        reg.RegisterIndexed("B", host, 8, false, Random, 0, 2, -1, -1);
        reg.RegisterSharing("A", FileSharing.AllOther, FileLockMode.Manual, multiple: false);
        reg.RegisterSharing("B", FileSharing.AllOther, FileLockMode.Manual, multiple: false);
        try
        {
            reg.OpenStatic("A", FileOpenMode.Output);
            reg.Close("A");
            reg.OpenStatic("A", FileOpenMode.IO);
            reg.OpenStatic("B", FileOpenMode.IO);
            Assert.Equal("00", reg.WriteKeyed("A", "K1AAAAAA", -1));
            Assert.Equal("00", reg.WriteKeyed("B", "K2BBBBBB", -1));
            // A sees B's record at once (no reopen).
            Assert.Equal("00", reg.ReadKeyed("A", -1, "K2".PadRight(8), out string img));
            Assert.Equal("K2BBBBBB", img);
            reg.Close("A");
            reg.Close("B");
            reg.OpenStatic("B", FileOpenMode.Input);
            Assert.Equal("00", reg.ReadKeyed("B", -1, "K1".PadRight(8), out _));
            Assert.Equal("00", reg.ReadKeyed("B", -1, "K2".PadRight(8), out _));
            reg.Close("B");
        }
        finally { try { File.Delete(host); } catch { } }
    }
}
