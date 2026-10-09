// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ TWO FILE CONNECTORS EXTENDING ONE SHARED PHYSICAL FILE KEEP BOTH RECORDS (kb/Work PB739).
/// ISO §14.9.51.4 GR19: <i>"If two or more file connectors for a sequential file add records by sharing the
/// physical file after opening it in extend mode, the added records follow the records present in the physical
/// file when it was opened, but are otherwise in an undefined order."</i> Only the relative ORDER of the two
/// connectors' records is undefined; that both are IN the file is not. GR12 makes the '00' a promise —
/// <i>"The successful execution of a WRITE statement releases a logical record to the operating
/// environment"</i> — and §9.1.15 item 3 with §14.9.27.4 Table 19 make the whole shape a Normal open.
///
/// <para><b>What PB739 was.</b> The sequential connector's EXTEND writer was a .NET <c>FileMode.Append</c>
/// handle, which seeks to the end ONCE, at open. Two connectors therefore anchored at the same offset, each
/// buffered its record, and the later flush wrote straight over the earlier one — both WRITEs reporting '00'
/// and one record simply not in the file. RELATIVE had the same defect one layer up: its extend write slot was
/// captured at OPEN into a private <c>_seqNext</c>, so both connectors minted RRN 2 and the second store write
/// replaced the first record. INDEXED was already right, and for the reason that names the fix — its release
/// identity is minted from the SHARED store at the moment of the write.</para>
///
/// <para>So the guard is the rule, not the reported arm: <b>the position and the identity of a released record
/// are read from the shared medium AT THE RELEASE, never from per-connector state captured at OPEN</b>. Every
/// organization × framing × sharing spelling that admits the extend mode is measured here, plus the exclusive
/// control, plus the static ban that keeps a naked append handle from coming back.</para>
/// </summary>
public sealed class SharedExtendWriteDriftTests
{
    private static string Tmp(string tag) =>
        Path.Combine(Path.GetTempPath(), $"pb739-{tag}-{Guid.NewGuid():N}.dat");

    private static void TryDelete(string host)
    {
        // ONE artifact per physical file (kb/Work PB802): a keyed file's §9.1.6 fixed file attributes
        // are the store header INSIDE it, so deleting the data file deletes them.
        try { File.Delete(host); } catch (IOException) { }
    }

    /// <summary>The sharing spelling under which TWO connectors can be open on one physical file at the same time
    /// and both write — reusing <see cref="SharedExtendOpenDriftTests.Spelling"/> so the two matrices cannot drift
    /// apart. Only the FILE-CONTROL ALL OTHER spelling appears: the OPEN-phrase spellings register their posture
    /// inside the OPEN call itself, which cannot express "open A, then open B, then write through each", and a
    /// connector with no SHARING clause is SHARING WITH NO OTHER in the extend mode since kb/Work PB322, so
    /// Table 19 admits no second connector beside it.</summary>
    public static TheoryData<SharedExtendOpenDriftTests.Spelling> Spellings() =>
        new(SharedExtendOpenDriftTests.Spelling.AllOtherClause);

    public static TheoryData<SharedExtendOpenDriftTests.Framing, SharedExtendOpenDriftTests.Spelling>
        SequentialShapes()
    {
        var data = new TheoryData<SharedExtendOpenDriftTests.Framing, SharedExtendOpenDriftTests.Spelling>();
        foreach (var f in Enum.GetValues<SharedExtendOpenDriftTests.Framing>())
            data.Add(f, SharedExtendOpenDriftTests.Spelling.AllOtherClause);
        return data;
    }

    private static void Share(FileRegistry reg, string name, SharedExtendOpenDriftTests.Spelling spelling)
    {
        Assert.Equal(SharedExtendOpenDriftTests.Spelling.AllOtherClause, spelling);   // the only spelling two writers can share
        reg.RegisterSharing(name, FileSharing.AllOther, FileLockMode.Manual, multiple: false);
    }

    private static void RegisterSequential(FileRegistry reg, string name, string host,
        SharedExtendOpenDriftTests.Framing framing) =>
        reg.Register(name, host, recordWidth: 4,
            lineSequential: framing == SharedExtendOpenDriftTests.Framing.LineSequential,
            optional: false,
            varyMin: framing == SharedExtendOpenDriftTests.Framing.VaryingRecordSequential ? 3 : -1,
            varyMax: framing == SharedExtendOpenDriftTests.Framing.VaryingRecordSequential ? 8 : -1);

    private static string Write(FileRegistry reg, string name, string image,
        FileRecordLock phrase = FileRecordLock.None) =>
        reg.WriteShared(name, image, -1, phrase, FileRetryKind.None, 0, page: null);

    // ── SEQUENTIAL: the record that used to be overwritten ───────────────────────────────────────────────────

    /// <summary>⛔ THE REGRESSION. Two participants open EXTEND over a one-record file and append one record
    /// each; the file then holds THREE records — the pre-existing one and both additions (§14.9.51.4 GR19).
    /// Before the fix the file held two: the second connector's buffered record landed on the first's offset.
    /// <para>The record ORDER is asserted as well as the count, and the assertion message says why that is not
    /// over-specification: GR19 leaves the two connectors' relative order undefined, but GR12 releases each
    /// record at its own WRITE, so a runtime that honours GR12 has only one order available to it.</para>
    /// <para>The fixed-width framing is the CONTROL — its append offset is arithmetic and it lost a record
    /// exactly like the other two, which is what tells this matrix apart from PB713's (where fixed width was
    /// the arm that passed).</para></summary>
    [Theory]
    [MemberData(nameof(SequentialShapes))]
    public void TwoSharedExtendConnectors_KeepBothRecords_ForEverySequentialFraming(
        SharedExtendOpenDriftTests.Framing framing, SharedExtendOpenDriftTests.Spelling spelling)
    {
        string host = Tmp($"seq-{framing}-{spelling}");
        try
        {
            var reg = new FileRegistry();
            RegisterSequential(reg, "S", host, framing);
            reg.OpenStatic("S", FileOpenMode.Output);
            Assert.Equal(FileStatusCode.Success, Write(reg, "S", "SEED"));
            reg.Close("S");

            RegisterSequential(reg, "A", host, framing);
            RegisterSequential(reg, "B", host, framing);
            Share(reg, "A", spelling);
            Share(reg, "B", spelling);
            reg.OpenStatic("A", FileOpenMode.Extend);
            reg.OpenStatic("B", FileOpenMode.Extend);
            Assert.Equal(FileStatusCode.Success, reg.Status("A"));
            Assert.Equal(FileStatusCode.Success, reg.Status("B"));
            Assert.Equal(FileStatusCode.Success, Write(reg, "A", "AAAA"));
            Assert.Equal(FileStatusCode.Success, Write(reg, "B", "BBBB"));
            reg.Close("A");
            reg.Close("B");

            Assert.Equal(new[] { "SEED", "AAAA", "BBBB" }, ReadBackSequential(host, framing));
        }
        finally { TryDelete(host); }
    }

    private static List<string> ReadBackSequential(string host, SharedExtendOpenDriftTests.Framing framing)
    {
        var reg = new FileRegistry();
        RegisterSequential(reg, "R", host, framing);
        reg.OpenStatic("R", FileOpenMode.Input);
        var got = new List<string>();
        while (reg.ReadShared("R", false, FileRecordLock.None, false, false, FileRetryKind.None, 0, out string rec)
               == FileStatusCode.Success)
            got.Add(rec);
        reg.Close("R");
        return got;
    }

    /// <summary>The §9.1.16 IDENTITY of each released record, which is the other half of the same defect: a
    /// sequential record's lock identity is its ordinal in the physical file, and each connector used to count
    /// from its own OPEN-time base, so BOTH called their first appended record ordinal 2. §9.1.16 —
    /// <i>"While locked by a given file connector, a record is not accessible to another file connector in the
    /// same or a different run unit"</i> — is written over that identity, so two connectors agreeing on the
    /// wrong number locks one record twice and leaves the other unlocked.
    /// <para>Measured through a sibling connector's view of the lock table, the only thing that can tell 2 from
    /// 3. Ordinal 1 is asserted NOT locked: it is the record that already existed.</para></summary>
    [Theory]
    [MemberData(nameof(SequentialShapes))]
    public void TwoSharedExtendConnectors_MintDistinctAscendingOrdinals(
        SharedExtendOpenDriftTests.Framing framing, SharedExtendOpenDriftTests.Spelling spelling)
    {
        string host = Tmp($"ord-{framing}-{spelling}");
        try
        {
            var reg = new FileRegistry();
            RegisterSequential(reg, "S", host, framing);
            reg.OpenStatic("S", FileOpenMode.Output);
            Assert.Equal(FileStatusCode.Success, Write(reg, "S", "SEED"));
            reg.Close("S");

            RegisterSequential(reg, "A", host, framing);
            RegisterSequential(reg, "B", host, framing);
            RegisterSequential(reg, "O", host, framing);   // the observer; never opened
            Share(reg, "A", spelling);
            Share(reg, "B", spelling);
            Share(reg, "O", spelling);
            reg.OpenStatic("A", FileOpenMode.Extend);
            reg.OpenStatic("B", FileOpenMode.Extend);
            Assert.Equal(FileStatusCode.Success, Write(reg, "A", "AAAA", FileRecordLock.WithLock));
            Assert.Equal(FileStatusCode.Success, Write(reg, "B", "BBBB", FileRecordLock.WithLock));

            Assert.False(reg.IsLockedByOther("O", "1"),
                "Ordinal 1 is the record that already existed; a lock on it means a base of 0.");
            Assert.True(reg.IsLockedByOther("O", "2"),
                "§14.9.51.4 GR19 — the first record appended through the shared file is the successor of the "
                + "one already present, so its ordinal is 2.");
            Assert.True(reg.IsLockedByOther("O", "3"),
                "The SECOND connector's record follows the FIRST connector's, so its ordinal is 3. Ordinal 2 "
                + "for both is the per-connector counter PB739 removed.");
            reg.Close("A");
            reg.Close("B");
        }
        finally { TryDelete(host); }
    }

    /// <summary>The CONTROL: one connector, exclusive, two writes. §14.9.51.4 GR19's antecedent is absent, so
    /// nothing about the append discipline may have changed for it — the two records are still consecutive and
    /// in order, over the plain <c>FileMode.Append</c> handle it keeps.</summary>
    [Theory]
    [MemberData(nameof(SequentialShapes))]
    public void ExclusiveExtend_StillAppendsInOrder(
        SharedExtendOpenDriftTests.Framing framing, SharedExtendOpenDriftTests.Spelling spelling)
    {
        _ = spelling;
        string host = Tmp($"excl-{framing}");
        try
        {
            var reg = new FileRegistry();
            RegisterSequential(reg, "S", host, framing);
            reg.OpenStatic("S", FileOpenMode.Output);
            Assert.Equal(FileStatusCode.Success, Write(reg, "S", "SEED"));
            reg.Close("S");
            reg.OpenStatic("S", FileOpenMode.Extend);
            Assert.Equal(FileStatusCode.Success, Write(reg, "S", "CCCC"));
            Assert.Equal(FileStatusCode.Success, Write(reg, "S", "DDDD"));
            reg.Close("S");

            Assert.Equal(new[] { "SEED", "CCCC", "DDDD" }, ReadBackSequential(host, framing));
        }
        finally { TryDelete(host); }
    }

    /// <summary>The THIRD write arm. A plain WRITE on a print or LINAGE file reroutes through
    /// <c>WriteAdvancing</c>, and that arm never minted a release ordinal at all — §14.9.51.4 GR12 is an
    /// ALL-FILES rule, so a print file's records are released exactly like a data file's and carry the same
    /// §9.1.16 identity. Measured on the connector rather than through the registry because the print-control
    /// entry point is <c>FileRegistry.WriteAdvancing</c>, which carries no locking phrase.</summary>
    [Fact]
    public void ThePrintControlWriteArmAlsoMintsTheReleaseOrdinal()
    {
        string host = Tmp("advancing");
        try
        {
            var shared = new PhysicalFileTable.State();
            var c = new SequentialConnector(host, recordWidth: 4, lineSequential: false);
            // The registry's one writer, spelled by hand for this focused connector-level measurement: the
            // physical file's state goes to every connector and the flag is the §9.1.16 record-locking posture
            // the mint reads through `SharedPhysical` (kb/Work PB753).
            c.AssociatePhysical(shared, locksRecords: true);
            Assert.Equal(FileStatusCode.Success, c.Open(FileOpenMode.Output));
            Assert.Equal("", c.LastWrittenRecordId);
            Assert.Equal(FileStatusCode.Success, c.WriteAdvancing("AAAA", 1, before: false, page: null));
            Assert.Equal("1", c.LastWrittenRecordId);
            Assert.Equal(FileStatusCode.Success, c.WriteAdvancing("BBBB", 1, before: false, page: null));
            Assert.Equal("2", c.LastWrittenRecordId);
            Assert.Equal(2, shared.ReleasedOrdinal);
            c.Close();
        }
        finally { TryDelete(host); }
    }

    // ── RELATIVE: the same defect one layer up ───────────────────────────────────────────────────────────────

    /// <summary>§14.9.51.4 GR29 a) — <i>"If the open mode is extend, the first record released after the OPEN is
    /// assigned a record number that is one greater than the highest relative record number existing in the
    /// physical file … If the physical file is shared and the open mode is extend, the record numbers are not
    /// necessarily consecutive"</i>, and GR31: <i>"the relative key values returned are ascending, but not
    /// necessarily consecutive"</i>. Over a file holding RRN 1, connector A releases 2 and connector B — asked
    /// AFTER A's release — releases 3. Both records survive; before the fix both minted 2 and the store write
    /// replaced.</summary>
    [Theory]
    [MemberData(nameof(Spellings))]
    public void TwoSharedExtendConnectors_KeepBothRecords_ForTheRelativeOrganization(
        SharedExtendOpenDriftTests.Spelling spelling)
    {
        string host = Tmp($"rel-{spelling}");
        try
        {
            var reg = new FileRegistry();
            reg.RegisterRelative("S", host, 4, optional: false, accessMode: 0, relativeKeyDigits: 4, -1, -1);
            reg.OpenStatic("S", FileOpenMode.Output);
            Assert.Equal(FileStatusCode.Success, Write(reg, "S", "SEED"));
            reg.Close("S");

            reg.RegisterRelative("A", host, 4, false, 0, 4, -1, -1);
            reg.RegisterRelative("B", host, 4, false, 0, 4, -1, -1);
            reg.RegisterRelative("O", host, 4, false, 0, 4, -1, -1);   // the observer; never opened
            Share(reg, "A", spelling);
            Share(reg, "B", spelling);
            Share(reg, "O", spelling);
            reg.OpenStatic("A", FileOpenMode.Extend);
            reg.OpenStatic("B", FileOpenMode.Extend);
            Assert.Equal(FileStatusCode.Success, Write(reg, "A", "AAAA", FileRecordLock.WithLock));
            Assert.Equal(FileStatusCode.Success, Write(reg, "B", "BBBB", FileRecordLock.WithLock));
            // A relative record's lock identity IS its RRN, so the lock table is the RRN each release took.
            Assert.False(reg.IsLockedByOther("O", "1"), "RRN 1 is the record that already existed.");
            Assert.True(reg.IsLockedByOther("O", "2"), "A's release is one greater than the highest existing.");
            Assert.True(reg.IsLockedByOther("O", "3"),
                "B's release is taken AFTER A's, so it is 3. Both minting 2 is the captured _seqNext PB739 removed.");
            reg.Close("A");
            reg.Close("B");

            var back = new FileRegistry();
            back.RegisterRelative("R", host, 4, false, 0, 4, -1, -1);
            back.OpenStatic("R", FileOpenMode.Input);
            var got = new List<string>();
            while (back.ReadKeyedNext("R", out string rec) == FileStatusCode.Success) got.Add(rec);
            back.Close("R");
            Assert.Equal(new[] { "SEED", "AAAA", "BBBB" }, got);
        }
        finally { TryDelete(host); }
    }

    /// <summary>The store's own invariant, which is what makes the release number O(1) and exact:
    /// <c>RelativeStore.Highest</c> is the highest RRN in the map after ANY mutation, including the removal of
    /// the maximum (§14.9.10.4 GR5 — a DELETE through a sibling connector genuinely lowers it). A stale
    /// high-water mark is the defect PB739 fixed, in a new place.</summary>
    [Fact]
    public void RelativeStoreHighWaterTracksEveryMutation()
    {
        var st = new RelativeStore();
        Assert.Equal(0, st.Highest);
        st.Put(3, new StoredFrame("c", null));
        st.Put(1, new StoredFrame("a", null));
        Assert.Equal(3, st.Highest);
        st.Put(7, new StoredFrame("g", null));
        Assert.Equal(7, st.Highest);
        Assert.True(st.Remove(7));
        Assert.Equal(3, st.Highest);        // re-derived, not left at 7
        Assert.False(st.Remove(7));
        Assert.True(st.Remove(1));
        Assert.Equal(3, st.Highest);        // removing a non-maximum leaves it alone
        st.Clear();
        Assert.Equal(0, st.Highest);
    }

    // ── INDEXED: already right, and pinned so it stays right ─────────────────────────────────────────────────

    /// <summary>§14.9.51.4 GR38 measures the extend high key <i>"when it was opened THROUGH THAT FILE
    /// CONNECTOR"</i> and then against <i>"the highest prime record key value written referencing this file
    /// connector"</i> — PER CONNECTOR, which is why B, opened when the file held only K002, may release K004
    /// after A has released K006 and still be successful. The store is shared, so both records are in the file
    /// and a sequential read returns them in key order.</summary>
    [Theory]
    [MemberData(nameof(Spellings))]
    public void TwoSharedExtendConnectors_KeepBothRecords_ForTheIndexedOrganization(
        SharedExtendOpenDriftTests.Spelling spelling)
    {
        string host = Tmp($"idx-{spelling}");
        try
        {
            var reg = new FileRegistry();
            reg.RegisterIndexed("S", host, 7, optional: false, accessMode: 0, primeOffset: 0, primeLength: 4, -1, -1);
            reg.OpenStatic("S", FileOpenMode.Output);
            Assert.Equal(FileStatusCode.Success, Write(reg, "S", "K002SSS"));
            reg.Close("S");

            reg.RegisterIndexed("A", host, 7, false, 0, 0, 4, -1, -1);
            reg.RegisterIndexed("B", host, 7, false, 0, 0, 4, -1, -1);
            Share(reg, "A", spelling);
            Share(reg, "B", spelling);
            reg.OpenStatic("A", FileOpenMode.Extend);
            reg.OpenStatic("B", FileOpenMode.Extend);
            Assert.Equal(FileStatusCode.Success, Write(reg, "A", "K006AAA"));
            Assert.Equal(FileStatusCode.Success, Write(reg, "B", "K004BBB"));
            reg.Close("A");
            reg.Close("B");

            var back = new FileRegistry();
            back.RegisterIndexed("R", host, 7, false, 0, 0, 4, -1, -1);
            back.OpenStatic("R", FileOpenMode.Input);
            var got = new List<string>();
            while (back.ReadKeyedNext("R", out string rec) == FileStatusCode.Success) got.Add(rec);
            back.Close("R");
            Assert.Equal(new[] { "K002SSS", "K004BBB", "K006AAA" }, got);
        }
        finally { TryDelete(host); }
    }

    // ── The structural half: no naked append handle can come back ────────────────────────────────────────────

    /// <summary>⛔ AN APPEND HANDLE IS BUILT IN EXACTLY ONE FILE, AND ASKED FOR THROUGH EXACTLY ONE ROLE.
    /// .NET's Append is not the host's atomic append — it seeks to the end once, at open — so a connector that
    /// takes one directly has silently declared that no other file connector will append to the same physical
    /// file, which is a statement about §9.1.15 that no call site is entitled to make.
    /// <c>HostFile.OpenConnectorWriteStream</c> is where the posture chooses between the plain handle and
    /// <c>SharedAppendStream</c>; everything else asks for the role.
    /// <para>Two shapes are banned, and NAMING the mode is neither of them: the role serves both
    /// <c>OPEN OUTPUT</c> (<c>Create</c>) and <c>OPEN EXTEND</c> (<c>Append</c>), so the mode has to be
    /// spellable at the call sites that distinguish them (kb/Work PB740). What may not happen is CONSTRUCTING a
    /// stream over an append mode, or handing that mode to one of the two roles that know nothing about
    /// repositioning — <c>OpenAuxiliary</c> (short-lived bookkeeping), <c>OpenConnectorStream</c> (a read or
    /// read-write handle) or <c>OpenConnectorStore</c> (a keyed store handle). Those are the ways a naked append
    /// handle can come back.</para></summary>
    [Fact]
    public void OnlyHostFileTakesAnAppendHandle()
    {
        string io = TestRepo.Src("Cobol.Net.Runtime", "IO");
        Assert.True(Directory.Exists(io), $"The IO subsystem moved: {io} is not a directory.");

        var offenders = new List<string>();
        foreach (string file in Directory.EnumerateFiles(io, "*.cs", SearchOption.AllDirectories))
        {
            if (string.Equals(Path.GetFileName(file), "FileSupport.cs", StringComparison.Ordinal)) continue;
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                string t = lines[i].TrimStart();
                if (t.StartsWith("//", StringComparison.Ordinal)) continue;     // prose about the rule
                if (t.StartsWith("///", StringComparison.Ordinal)) continue;
                if (!lines[i].Contains("FileMode.Append", StringComparison.Ordinal)) continue;
                // Shape 1 — a stream constructed over it. The construction may wrap onto the mode's own line,
                // so the two lines above count as part of the same statement.
                bool constructs = false;
                for (int j = Math.Max(0, i - 2); j <= i && !constructs; j++)
                    constructs = lines[j].Contains("new FileStream(", StringComparison.Ordinal)
                        || lines[j].Contains("new StreamWriter(", StringComparison.Ordinal)
                        || lines[j].Contains("new StreamReader(", StringComparison.Ordinal)
                        || lines[j].Contains("File.Open", StringComparison.Ordinal);
                // Shape 2 — handed to a role that does not reposition.
                bool wrongRole = lines[i].Contains("OpenAuxiliary(", StringComparison.Ordinal)
                    || lines[i].Contains("OpenConnectorStream(", StringComparison.Ordinal)
                    || lines[i].Contains("OpenConnectorStore(", StringComparison.Ordinal);
                if (constructs || wrongRole)
                    offenders.Add($"{Path.GetRelativePath(io, file)}:{i + 1}: {t}");
            }
        }

        Assert.True(offenders.Count == 0,
            "A FileMode.Append handle outside HostFile. It seeks to the physical end ONCE, at open, so two "
            + "§9.1.15 participants anchor at the same offset and the later flush overwrites the earlier "
            + "record — on '00' from both WRITEs (kb/Work PB739). Call HostFile.OpenConnectorWriteStream. "
            + "Sites:\n  "
            + string.Join("\n  ", offenders));
    }

    /// <summary>The positive complement, for the reason a ban over a subsystem that appends NOTHING passes
    /// exactly as green: the role still lives in its named home, still chooses on the §9.1.15 posture, and the
    /// shared arm still repositions at the physical end before each write.</summary>
    [Fact]
    public void TheAppendRoleStillLivesInItsNamedHome_AndStillRepositions()
    {
        string text = File.ReadAllText(TestRepo.Src("Cobol.Net.Runtime", "IO", "FileSupport.cs"));
        Assert.Contains("public static Stream OpenConnectorWriteStream(", text, StringComparison.Ordinal);
        Assert.Contains("FileLockPosture.AdmitsAnotherWriter(share)", text, StringComparison.Ordinal);
        Assert.Contains("new SharedAppendStream(", text, StringComparison.Ordinal);
        Assert.Contains("_inner.Seek(0, SeekOrigin.End);", text, StringComparison.Ordinal);
    }

    /// <summary>And the runtime complement of the same fact: a shared append stream writes at the END as it
    /// stands at that moment, so a byte appended behind its back is not overwritten. Measured on the stream
    /// itself, without a connector, because that is the whole contract in one line.</summary>
    [Fact]
    public void TheSharedAppendStreamWritesAtTheEndAsItStandsNow()
    {
        string host = Tmp("append");
        try
        {
            File.WriteAllBytes(host, "AA"u8.ToArray());
            using (var s = HostFile.OpenConnectorWriteStream(host, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            {
                s.Write("BB"u8);
                s.Flush();
                // Another connector appends behind this stream's back.
                using (var other = HostFile.OpenConnectorWriteStream(host, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                {
                    other.Write("CC"u8);
                    other.Flush();
                }
                s.Write("DD"u8);
                s.Flush();
            }
            Assert.Equal("AABBCCDD", File.ReadAllText(host));
        }
        finally { TryDelete(host); }
    }
}
