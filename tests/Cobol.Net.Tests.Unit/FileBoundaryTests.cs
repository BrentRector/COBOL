// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using CobolNet.Runtime;
using CobolNet.Runtime.IO;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The runtime's two MEDIUM boundaries, pinned where a COBOL golden cannot reach them (kb/Work PB1192, PB690).
/// <list type="bullet">
/// <item>The externally-defined boundaries of ISO §9.1.13.5 item 4 ('24', a relative or indexed file — Annex A.1
/// item 107, the store's capacity <see cref="RecordFraming.MaxStoreBytes"/>) and §9.1.13.6 item 3 ('34', a
/// sequential file — Annex A.1 item 108, the host medium's capacity, <see cref="HostFile.IsMediumBoundary"/>).
/// The capacity test is ARITHMETIC over the one size formula (<see cref="RecordFraming.FrameBytes"/>), so these
/// tests hold that formula against the bytes <see cref="RecordFraming.WriteStore"/> really composes: a drift
/// between them would let a store past the boundary be released, or refuse one inside it.</item>
/// <item>The file coded character set of Annex A.1 item 31 (owner decision kb/Work R47): ISO/IEC 8859-1, with a
/// strict encoding so an unrepresentable character can never become a silent <c>?</c>.</item>
/// </list>
/// </summary>
public sealed class FileBoundaryTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "CobolNet_FBT_" + Guid.NewGuid().ToString("N")[..8]);

    public FileBoundaryTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }

    private string Host(string name) => Path.Combine(_dir, name);

    private static readonly char Euro = (char)0x20AC;
    private static readonly char EAcute = (char)0x00E9;

    // ── §9.1.13.6 item 3 / item 1: the host's write refusal ────────────────────────────────────────────────

    /// <summary>The host's medium-exhaustion refusals are the sequential boundary ('34'); every other write
    /// failure is §9.1.13.6 item 1's '30'. The Windows HRESULTs are negative and can never be a Unix errno, so
    /// they classify on every host; the raw errnos classify only where the host reports errnos.</summary>
    [Fact]
    public void WriteFailure_MediumExhaustionIs34_AnyOtherFailureIs30()
    {
        foreach (int hr in new[] { unchecked((int)0x80070027), unchecked((int)0x80070070), unchecked((int)0x800700DF), unchecked((int)0x8007050F) })
            Assert.Equal(FileStatusCode.PermanentBoundary, FileStatusCode.ForWriteFailure(new IOException("full", hr)));
        string unixExpected = OperatingSystem.IsWindows() ? FileStatusCode.PermanentError : FileStatusCode.PermanentBoundary;
        int quota = OperatingSystem.IsLinux() ? 122 : 69;
        foreach (int errno in new[] { 27, 28, quota })
            Assert.Equal(unixExpected, FileStatusCode.ForWriteFailure(new IOException("errno", errno)));
        Assert.Equal(FileStatusCode.PermanentError, FileStatusCode.ForWriteFailure(new IOException("io", unchecked((int)0x80131620))));
        Assert.Equal(FileStatusCode.PermanentError, FileStatusCode.ForWriteFailure(new IOException("sharing", unchecked((int)0x80070020))));
    }

    /// <summary>§14.9.51.4 GR20 end to end, on the one host medium that is full by construction: Linux's
    /// <c>/dev/full</c> refuses every write with ENOSPC. The writer buffers, so the refusal surfaces on the WRITE
    /// whose record fills the buffer — that WRITE is '34', never an escaping IOException. A host without the device
    /// must not be Linux (so this cannot pass on the CI host by not running).</summary>
    [Fact]
    public void SequentialWrite_OnAFullMedium_Is34_AtTheWrite()
    {
        const string full = "/dev/full";
        if (!File.Exists(full))
        {
            Assert.False(OperatingSystem.IsLinux(), "a Linux host provides /dev/full; the boundary was not exercised");
            return;
        }
        var c = new SequentialConnector(full, recordWidth: 512, lineSequential: false);
        Assert.Equal(FileStatusCode.Success, c.Open(FileOpenMode.Output));
        string record = new('X', 512);
        string status = FileStatusCode.Success;
        int writes = 0;
        while (status == FileStatusCode.Success && writes < 100_000)
        {
            status = c.Write(record, -1, page: null);
            writes++;
        }
        Assert.Equal(FileStatusCode.PermanentBoundary, status);
        c.Close();   // its own final flush is refused too: §9.1.13.6 item 1's '30' through Close's catch
    }

    /// <summary>§9.1.13.6 item 1 on the READ side (kb/Work PB1512): a host failure while a sequential READ or
    /// START FIRST/LAST reads the medium is the permanent error '30' — never an escaping IOException — and the
    /// unsuccessful READ leaves no valid file position, so the next sequential READ is '46' (§14.9.30.4 GR18,
    /// §9.1.13.7 item 6). The failure is produced by the host itself, measured per host: Linux refuses a read of
    /// <c>/proc/self/mem</c> at address 0 with EIO, and Windows refuses a read of a byte range another handle has
    /// LOCKED. A host offering neither must be neither of them, so the test cannot pass on a CI host by not
    /// running.</summary>
    [Fact]
    public void SequentialRead_HostFailure_Is30_AndInvalidatesThePosition()
    {
        FileStream? locker = null;
        string path;
        if (OperatingSystem.IsLinux() && File.Exists("/proc/self/mem")) path = "/proc/self/mem";
        else if (OperatingSystem.IsWindows())
        {
            path = Host("locked.dat");
            File.WriteAllBytes(path, Encoding.Latin1.GetBytes(new string('A', 64)));
        }
        else
        {
            Assert.False(OperatingSystem.IsLinux() || OperatingSystem.IsWindows(), "the host failure was not exercised");
            return;
        }
        var c = new SequentialConnector(path, recordWidth: 16, lineSequential: false);
        Assert.Equal(FileStatusCode.Success, c.Open(FileOpenMode.Input));
        try
        {
            if (OperatingSystem.IsWindows())
            {
                locker = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                locker.Lock(0, 64);
            }
            Assert.False(c.Read(previous: false, out _));
            Assert.Equal(FileStatusCode.PermanentError, c.Status);
            Assert.False(c.Read(previous: false, out _));
            Assert.Equal(FileStatusCode.NoValidNextRecord, c.Status);   // '46' — the '30' READ left no position
            Assert.Equal(FileStatusCode.PermanentError, c.StartFirstLast(last: false));
        }
        finally
        {
            locker?.Dispose();   // releases the region lock with the handle
            c.Close();
        }
    }

    // ── §9.1.13.5 item 4: the store's capacity ───────────────────────────────────────────────────────────

    /// <summary>The capacity is the largest array the store is composed in and loaded from.</summary>
    [Fact]
    public void MaxStoreBytes_IsTheLargestArrayTheStoreIsComposedIn() =>
        Assert.Equal(RecordFraming.MaxStoreBytes, (long)Array.MaxLength);

    /// <summary>The relative store's size arithmetic agrees with the image <see cref="RecordFraming.WriteStore"/>
    /// composes, through every mutation: a release above the highest RRN (gap tags), a replacement by a longer
    /// record, a record with an extent table, a removal of the highest record (its gap tags go with it).</summary>
    [Fact]
    public void RelativeStore_PredictedSize_IsTheComposedSize()
    {
        var attributes = new RelativeConnector("r.dat", 8, KeyedAccess.Random, 4, varyMin: 1, varyMax: 8).DeclaredAttributes;
        var store = new RelativeStore();
        void PutAndCheck(long rrn, StoredFrame frame)
        {
            long predicted = RecordFraming.HeaderBytes(attributes) + store.FramedBytesAfterPut(rrn, frame);
            store.Put(rrn, frame);
            Assert.Equal(predicted, Composed(attributes, store.Ordinal()));
        }
        PutAndCheck(3, new StoredFrame("ABC", null));
        PutAndCheck(10, new StoredFrame("ABCDEFGH", null));
        PutAndCheck(3, new StoredFrame("ABCDEFG", null));
        PutAndCheck(5, new StoredFrame("ABCD", RecordFraming.VoidExtents(2)));
        Assert.True(store.Remove(10));
        Assert.Equal(RecordFraming.HeaderBytes(attributes) + store.FramedBytesAfterPut(5, store.Slots[5]),
            Composed(attributes, store.Ordinal()));
        Assert.Equal([null, null, "ABCDEFG", null, "ABCD"], store.Ordinal().Select(f => f?.Image));
    }

    /// <summary>The indexed store's running size agrees with the composed image through Add, Replace and Remove.</summary>
    [Fact]
    public void IndexedStore_RecordBytes_IsTheComposedSize()
    {
        var attributes = new IndexedConnector("i.dat", 8, KeyedAccess.Random, 0, 4, varyMin: 4, varyMax: 8).DeclaredAttributes;
        var store = new IndexedStore();
        var a = new KeyedRec { Image = "K001AAAA" };
        var b = new KeyedRec { Image = "K002", Extents = RecordFraming.VoidExtents(1) };
        store.Add(a);
        store.Add(b);
        Check();
        store.Replace(a, "K001A", null);
        Check();
        Assert.True(store.Remove(b));
        Check();
        store.Clear();
        Check();

        void Check() => Assert.Equal(RecordFraming.HeaderBytes(attributes) + store.RecordBytes,
            Composed(attributes, store.Recs.Select(r => (StoredFrame?)new StoredFrame(r.Image, r.Extents))));
    }

    /// <summary>The relative boundary, exactly: a 4-character fixed record's store is a 20-byte header, a 4-byte
    /// gap tag for every empty slot below the record and the record's own 8-byte frame — so RRN 536 870 891 is
    /// the last that fits <see cref="RecordFraming.MaxStoreBytes"/> (2 147 483 588 bytes) and 536 870 892 the first
    /// that does not (2 147 483 592). Beyond the store, GR29 b)'s highest permitted RRN (2 147 483 647) splits
    /// '24' (a relative record number that does not fit — the invalid key condition) from '34' (one that is not
    /// permitted at all). Nothing is released by either, so the CLOSE persists only the in-bounds record.</summary>
    [Fact]
    public void RelativeWrite_BoundaryIsTheStoreCapacity_AndTheKeyRangeIs34()
    {
        var c = new RelativeConnector(Host("rel.dat"), 4, KeyedAccess.Random, 18);
        long header = RecordFraming.HeaderBytes(c.DeclaredAttributes);
        Assert.Equal(20, header);
        var probe = new RelativeStore();
        var four = new StoredFrame("ABCD", null);
        Assert.True(header + probe.FramedBytesAfterPut(536_870_891, four) <= RecordFraming.MaxStoreBytes);
        Assert.True(header + probe.FramedBytesAfterPut(536_870_892, four) > RecordFraming.MaxStoreBytes);

        Assert.Equal(FileStatusCode.Success, c.Open(FileOpenMode.Output));
        c.SetPendingKey(536_870_892);
        Assert.Equal(FileStatusCode.BoundaryViolation, c.Write("ABCD"));
        c.SetPendingKey(RelativeConnector.HighestRelativeRecordNumber);
        Assert.Equal(FileStatusCode.BoundaryViolation, c.Write("ABCD"));
        c.SetPendingKey(5);
        Assert.Equal(FileStatusCode.Success, c.Write("ABCD"));
        // The permanent errors LAST: under Annex A.1 item 105 a '3x' stays in effect until the CLOSE, which runs
        // normally and ends it.
        c.SetPendingKey(RelativeConnector.HighestRelativeRecordNumber + 1);
        Assert.Equal(FileStatusCode.PermanentBoundary, c.Write("ABCD"));
        c.SetPendingKey(0);
        Assert.Equal(FileStatusCode.PermanentBoundary, c.Write("ABCD"));
        Assert.Equal(FileStatusCode.Success, c.Close());
        Assert.Equal(header + (4 * 4) + 8, new FileInfo(Host("rel.dat")).Length);
    }

    // ── Annex A.1 item 31: the file coded character set ──────────────────────────────────────────────────

    /// <summary>U+0000–U+00FF have a byte image and nothing above does; the strict encoding round-trips all 256
    /// and REFUSES the rest instead of writing '?'.</summary>
    [Fact]
    public void FileCharacterSet_IsLatin1_AndRefusesRatherThanReplaces()
    {
        Assert.False(FileCharacterSet.HasCharacterWithoutByteImage("A" + EAcute + (char)0xFF));
        Assert.True(FileCharacterSet.HasCharacterWithoutByteImage("A" + Euro));
        Assert.True(FileCharacterSet.HasCharacterWithoutByteImage(((char)0x100).ToString()));
        Assert.True(FileCharacterSet.HasCharacterWithoutByteImage(((char)0xFFFE).ToString()));
        var all = new string(Enumerable.Range(0, 256).Select(i => (char)i).ToArray());
        byte[] bytes = FileCharacterSet.Medium.GetBytes(all);
        Assert.Equal(Enumerable.Range(0, 256).Select(i => (byte)i), bytes);
        Assert.Equal(all, FileCharacterSet.Medium.GetString(bytes));
        Assert.Throws<EncoderFallbackException>(() => FileCharacterSet.Medium.GetBytes("A" + Euro));
    }

    /// <summary>The REWRITE arms of the keyed organizations ask the same refusal as their WRITEs ('91'), and the
    /// record in the store is left as it was (§14.9.35.4: an unsuccessful REWRITE replaces nothing).</summary>
    [Fact]
    public void KeyedRewrite_OfARecordWithoutAByteImage_Is91_AndReplacesNothing()
    {
        var r = new RelativeConnector(Host("rw.dat"), 4, KeyedAccess.Random, 4);
        Assert.Equal(FileStatusCode.Success, r.Open(FileOpenMode.Output));
        r.SetPendingKey(1);
        Assert.Equal(FileStatusCode.Success, r.Write("ABCD"));
        Assert.Equal(FileStatusCode.Success, r.Close());
        Assert.Equal(FileStatusCode.Success, r.Open(FileOpenMode.IO));
        r.SetPendingKey(1);
        Assert.Equal(FileStatusCode.CharacterWithoutByteImage, r.Rewrite("A" + Euro + "CD"));
        Assert.Equal(FileStatusCode.Success, r.ReadRandom(out string rel));
        Assert.Equal("ABCD", rel);
        Assert.Equal(FileStatusCode.Success, r.Close());

        var ix = new IndexedConnector(Host("ix.dat"), 8, KeyedAccess.Random, 0, 4);
        Assert.Equal(FileStatusCode.Success, ix.Open(FileOpenMode.Output));
        Assert.Equal(FileStatusCode.Success, ix.Write("K001DATA"));
        Assert.Equal(FileStatusCode.Success, ix.Close());
        Assert.Equal(FileStatusCode.Success, ix.Open(FileOpenMode.IO));
        Assert.Equal(FileStatusCode.CharacterWithoutByteImage, ix.Rewrite("K001D" + Euro + "TA"));
        Assert.Equal(FileStatusCode.Success, ix.ReadRandom(-1, "K001    ", out string idx));
        Assert.Equal("K001DATA", idx);
        Assert.Equal(FileStatusCode.Success, ix.Close());
    }

    /// <summary>The '91' test asks what the WRITE TRANSFERS: a varying record's area positions past its length
    /// never reach the medium, so a character there refuses nothing (§13.18.43 GR13 — the record is written at
    /// its length).</summary>
    [Fact]
    public void SequentialWrite_TestsOnlyTheTransferredLength_OfAVaryingRecord()
    {
        var c = new SequentialConnector(Host("v.dat"), recordWidth: 6, lineSequential: false, varyMin: 1, varyMax: 6);
        Assert.Equal(FileStatusCode.Success, c.Open(FileOpenMode.Output));
        Assert.Equal(FileStatusCode.Success, c.Write("ABC" + Euro + "EF", 3, page: null));
        Assert.Equal(FileStatusCode.CharacterWithoutByteImage, c.Write("ABC" + Euro + "EF", 4, page: null));
        Assert.Equal(FileStatusCode.Success, c.Close());
    }

    /// <summary>§12.4.5.7.4 GR1 — "Each collating sequence is a fixed file attribute" — over the WHOLE native
    /// repertoire (kb/Work PB1093): two sequences that order every Latin-1 character identically but U+0100 / U+0101
    /// differently are different attributes, and the fingerprint says so (it hashed only U+0000–U+00FF before).</summary>
    [Fact]
    public void CollatingFingerprint_SeesADifferenceAboveLatin1()
    {
        var ab = new AlphanumericCollation([0x100, 0x101], [0, 1], [0x100, 0x101], 2);
        var ba = new AlphanumericCollation([0x100, 0x101], [1, 0], [0x101, 0x100], 2);
        for (int c = 0; c <= 0xFF; c++) Assert.Equal(ab.Weight((char)c), ba.Weight((char)c));
        Assert.NotEqual(FixedFileAttributes.Fingerprint(ab), FixedFileAttributes.Fingerprint(ba));
        Assert.Equal(FixedFileAttributes.Fingerprint(ab),
            FixedFileAttributes.Fingerprint(new AlphanumericCollation([0x100, 0x101], [0, 1], [0x100, 0x101], 2)));
    }

    /// <summary>With no CODE-SET a line sequential file is UTF-8 text (kb/Work PB1760), so its character set is every
    /// Unicode scalar value from U+0020 in BOTH record-area classes: U+20AC, a surrogate PAIR and HIGH-VALUE (U+FFFF)
    /// are members; a C0 control and an unpaired surrogate (including a non-UTF-8 byte's U+DC80–U+DCFF escape) are
    /// not. A national area's characters reach it as UTF-16BE byte pairs.</summary>
    [Fact]
    public void LineSequentialCharacterSet_IsTheUtf8ScalarValues_FromSpace()
    {
        Assert.False(LineSequentialCharacterSet.HasCharacterOutside("A" + EAcute + Euro + "\uFFFF", national: false, codeSet: null));
        Assert.False(LineSequentialCharacterSet.HasCharacterOutside("A\uD83D\uDE00", national: false, codeSet: null));   // one pair
        Assert.True(LineSequentialCharacterSet.HasCharacterOutside("A\uD83D", national: false, codeSet: null));          // unpaired
        Assert.True(LineSequentialCharacterSet.HasCharacterOutside("A\uDCE9", national: false, codeSet: null));          // byte escape
        Assert.True(LineSequentialCharacterSet.HasCharacterOutside("A\t", national: false, codeSet: null));
        // N"€" as its UTF-16BE bytes: 0x20 0xAC.
        Assert.False(LineSequentialCharacterSet.HasCharacterOutside(new string([(char)0x20, (char)0xAC]), national: true, codeSet: null));
        Assert.True(LineSequentialCharacterSet.HasCharacterOutside(new string([(char)0x00, (char)0x0A]), national: true, codeSet: null));
    }

    /// <summary>The UTF-8 line codec (kb/Work PB1760): every member round-trips through the byte channel as its UTF-8
    /// bytes; a byte that is not UTF-8 decodes to its U+DC80–U+DCFF escape (so the record keeps the byte and the READ
    /// reports '09'); a national area travels as its characters, not its pair bytes.</summary>
    [Fact]
    public void LineSequentialEncoding_IsUtf8_WithAByteEscape()
    {
        string text = "h" + EAcute + Euro + "\uFFFF\uD83D\uDE00";
        string channel = LineSequentialEncoding.ToChannel(text, nationalArea: false);
        Assert.Equal(System.Text.Encoding.UTF8.GetBytes(text), channel.Select(c => (byte)c).ToArray());
        Assert.Equal(text, LineSequentialEncoding.FromChannel(channel, nationalArea: false));
        Assert.Equal("caf\uDCE9", LineSequentialEncoding.FromChannel("caf\u00E9", nationalArea: false));   // Latin-1 é
        Assert.Equal("\uDCE2\uDC82", LineSequentialEncoding.FromChannel("\u00E2\u0082", nationalArea: false));   // torn €
        string pairs = CobolBits.NatBytes(Euro + "A");
        Assert.Equal("\u00E2\u0082\u00ACA", LineSequentialEncoding.ToChannel(pairs, nationalArea: true));
        Assert.Equal(pairs, LineSequentialEncoding.FromChannel("\u00E2\u0082\u00ACA", nationalArea: true));
        Assert.Throws<InvalidOperationException>(() => LineSequentialEncoding.ToChannel("A\uD83D", nationalArea: false));
    }

    /// <summary>⛔ THE STORAGE-BYTE LAW (kb/Work PB1759, design D29): byte 0xFF is HIGH-VALUE (U+FFFF), every other byte
    /// b is U+00bb, and a character's byte is its low byte — so U+00FF and U+FFFF share 0xFF, and the fixed-record
    /// channel writes HIGH-VALUE as 0xFF and reads 0xFF back as HIGH-VALUE.</summary>
    [Fact]
    public void StorageByteLaw_HighValueIsTheByteFF_OnTheChannelToo()
    {
        Assert.Equal('\uFFFF', NativeCollatingSequence.HighValue);
        Assert.Equal('\uFFFF', StorageByte.ToChar(0xFF));
        Assert.Equal('\u00FE', StorageByte.ToChar(0xFE));
        Assert.Equal((byte)0xFF, StorageByte.ToByte('\uFFFF'));
        Assert.Equal((byte)0xFF, StorageByte.ToByte('\u00FF'));
        Assert.True(StorageByte.HasByte('\uFFFF'));
        Assert.False(StorageByte.HasByte('\u0100'));
        Assert.False(StorageByte.HasByte('\uFFFE'));
        Assert.Equal("A\u00FF", FileCharacterSet.ToChannel("A\uFFFF", codeSet: null));
        Assert.Equal("A\uFFFF", FileCharacterSet.FromChannel("A\u00FF", codeSet: null));
        Assert.False(FileCharacterSet.HasCharacterWithoutByteImage("A\uFFFF"));
    }

    private static long Composed(FixedFileAttributes attributes, IEnumerable<StoredFrame?> frames)
    {
        var ms = new MemoryStream();
        RecordFraming.WriteStore(ms, attributes, frames);
        return ms.Length;
    }
}
