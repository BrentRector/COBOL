// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The COBOL-2002 file-sharing / record-locking registry (Phase 4d M2-FILE-1; ISO/IEC 1989:2023 §9.1.15/§9.1.16
/// / §12.4.5.9 / §14.9.27 / §14.9.47 / §14.7.9). End-to-end behavior (OPEN 61, WITH LOCK 51, RETRY, IGNORING
/// LOCK, UNLOCK, AUTOMATIC auto-lock) rides the <c>file_sharing</c> golden; these lock the
/// <c>CobolFile.Locks</c> primitives the emitter wires to: the record-lock grant/deny, the 53/54 ceilings, the
/// Table-19 open-conflict classifier, and the single-run-unit RETRY loop.
/// </summary>
public sealed class CobolFileLockTests
{
    private const int Random = 1;   // KeyedAccess.Random

    /// <summary>Register two RELATIVE connectors bound to ONE physical file (the two-SELECTs-one-file scenario
    /// §9.1.15 note :11729 makes 51/61 realizable in a single run unit), both MANUAL sharers.</summary>
    private static void TwoConnectorsOneFile(string a, string b, string host)
    {
        CobolFile.Init();
        CobolFile.RegisterRelative(a, host, 8, false, Random, 4);
        CobolFile.RegisterRelative(b, host, 8, false, Random, 4);
        CobolFile.RegisterSharing(a, FileSharing.AllOther, FileLockMode.Manual, false);
        CobolFile.RegisterSharing(b, FileSharing.AllOther, FileLockMode.Manual, false);
    }

    [Fact]
    public void RecordLock_Grants_ThenBlocksAnotherConnector_51()
    {
        TwoConnectorsOneFile("CA", "CB", "lk-grant.dat");
        Assert.Equal(FileStatusCode.Success, CobolFile.LockRecord("CA", "1"));
        Assert.True(CobolFile.IsLockedByOther("CB", "1"));
        Assert.Equal(FileStatusCode.RecordLocked, CobolFile.LockRecord("CB", "1"));   // 51 — another connector holds it
        Assert.False(CobolFile.IsLockedByOther("CA", "1"));                            // GR8: not "other" to its own owner
    }

    [Fact]
    public void SelfRelock_IsIdempotent_GR8()
    {
        TwoConnectorsOneFile("CA", "CB", "lk-self.dat");
        Assert.Equal(FileStatusCode.Success, CobolFile.LockRecord("CA", "7"));
        Assert.Equal(FileStatusCode.Success, CobolFile.LockRecord("CA", "7"));   // re-lock of a held record: idempotent
    }

    [Fact]
    public void Unlock_ReleasesEveryLock_ThenAnotherConnectorMayLock()
    {
        TwoConnectorsOneFile("CA", "CB", "lk-unlock.dat");
        CobolFile.LockRecord("CA", "1");
        CobolFile.LockRecord("CA", "2");
        Assert.True(CobolFile.IsLockedByOther("CB", "1"));
        CobolFile.ReleaseAllForConnector("CA");                                   // UNLOCK / CLOSE (§9.1.16 :11754)
        Assert.False(CobolFile.IsLockedByOther("CB", "1"));
        Assert.Equal(FileStatusCode.Success, CobolFile.LockRecord("CB", "1"));
    }

    [Fact]
    public void ReleaseSingle_ReleasesOnlyTheNamedRecord()
    {
        TwoConnectorsOneFile("CA", "CB", "lk-single.dat");
        CobolFile.LockRecord("CA", "1");
        CobolFile.LockRecord("CA", "2");
        CobolFile.ReleaseSingle("CA", "1");
        Assert.False(CobolFile.IsLockedByOther("CB", "1"));
        Assert.True(CobolFile.IsLockedByOther("CB", "2"));   // record 2 stays locked
    }

    [Fact]
    public void ConnectorLockLimit_16thRecord_Is54()
    {
        CobolFile.Init();
        CobolFile.RegisterRelative("CA", "lk-conn.dat", 8, false, Random, 4);
        CobolFile.RegisterSharing("CA", FileSharing.AllOther, FileLockMode.Manual, true);   // MULTIPLE — many locks
        for (int i = 1; i <= 15; i++)
            Assert.Equal(FileStatusCode.Success, CobolFile.LockRecord("CA", i.ToString()));   // GR7 impl max ≥15
        Assert.Equal(FileStatusCode.ConnectorLockLimit, CobolFile.LockRecord("CA", "16"));    // 54
    }

    [Fact]
    public void RunUnitLockLimit_256thRunUnitLock_Is53()
    {
        // 17 connectors × 15 locks (each at its own connector ceiling, each on its OWN physical file so no 51/54
        // cross-talk) = 255 run-unit locks; an 18th connector's FIRST lock exceeds the run-unit max → 53.
        CobolFile.Init();
        for (int c = 0; c < 17; c++)
        {
            string name = "C" + c;
            CobolFile.RegisterRelative(name, $"lk-run-{c}.dat", 8, false, Random, 4);
            CobolFile.RegisterSharing(name, FileSharing.AllOther, FileLockMode.Manual, true);
            for (int i = 1; i <= 15; i++)
                Assert.Equal(FileStatusCode.Success, CobolFile.LockRecord(name, i.ToString()));
        }
        CobolFile.RegisterRelative("COVER", "lk-run-cover.dat", 8, false, Random, 4);
        CobolFile.RegisterSharing("COVER", FileSharing.AllOther, FileLockMode.Manual, true);
        Assert.Equal(FileStatusCode.RunUnitLockLimit, CobolFile.LockRecord("COVER", "1"));   // 53
    }

    // ⛔ The Table-19 open-conflict matrix is NOT tested here any more. It used to be a six-row [InlineData]
    // theory labelled '(a)'–'(e)' against a table of 35 cells, with NO row whose incoming mode was OUTPUT —
    // so §9.1.13.9 1) e) had neither an implementation nor a failing test, and the theory was GREEN while an
    // incoming OPEN OUTPUT truncated files another connector held open (kb/Work PB321). Its replacement is
    // `OpenTable19Tests`, which transcribes all 35 printed cells and enumerates all 144 connector pairs, so
    // it cannot be green for want of a row. The facade delegation is exercised there through FileRegistry.

    [Fact]
    public void RetryLoop_TimesExhausts_ForeverDeadlockBails_SuccessShortCircuits()
    {
        // A conflict that never clears: n TIMES exhausts to the conflict status (51), FOREVER bails to 52.
        Assert.Equal(FileStatusCode.RecordLocked,
            FileRegistry.RetryLoop(() => RetryAttempt.InRunUnit(FileStatusCode.RecordLocked), FileRetryKind.Times, 3));
        Assert.Equal(FileStatusCode.Deadlock,
            FileRegistry.RetryLoop(() => RetryAttempt.InRunUnit(FileStatusCode.RecordLocked), FileRetryKind.Forever, 0));
        // A conflict that clears on the 2nd attempt: TIMES stops as soon as it succeeds.
        int calls = 0;
        Assert.Equal(FileStatusCode.Success, FileRegistry.RetryLoop(
            () => { calls++; return RetryAttempt.InRunUnit(calls >= 2 ? FileStatusCode.Success : FileStatusCode.RecordLocked); },
            FileRetryKind.Times, 5));
        Assert.Equal(2, calls);
    }

    /// <summary>⛔ THE DRIFT TEST for the §14.7.9.3 conflict-status CLASS rule (kb/Work PB142; the design is
    /// docs/COBOLNET_FILES_DESIGN.md D8). Every retry form × every conflict class, asserted cell by cell, so
    /// that "the exhausted retry lands the CONFLICT'S OWN §9.1.13 status" cannot silently regrow a manufactured
    /// literal at any one call site. The single exception — FOREVER on a record LOCK, the §9.1.13.8 item 2
    /// deadlock this implementation detects — is one cell here, not a rule spread over six callers.</summary>
    [Theory]
    // ── RECORD OPERATION conflict, §9.1.13.8 item 1 ('51'): the conflict's own status, except FOREVER.
    [InlineData(FileRetryKind.None, 0, FileStatusCode.RecordLocked, FileStatusCode.RecordLocked)]
    [InlineData(FileRetryKind.Times, 0, FileStatusCode.RecordLocked, FileStatusCode.RecordLocked)]    // GR4a
    [InlineData(FileRetryKind.Times, -3, FileStatusCode.RecordLocked, FileStatusCode.RecordLocked)]   // GR4a
    [InlineData(FileRetryKind.Times, 2, FileStatusCode.RecordLocked, FileStatusCode.RecordLocked)]    // GR1
    [InlineData(FileRetryKind.Seconds, 0, FileStatusCode.RecordLocked, FileStatusCode.RecordLocked)]  // GR4a
    [InlineData(FileRetryKind.Seconds, -5, FileStatusCode.RecordLocked, FileStatusCode.RecordLocked)] // GR4a
    [InlineData(FileRetryKind.Seconds, 30, FileStatusCode.RecordLocked, FileStatusCode.RecordLocked)] // GR2 clamp
    [InlineData(FileRetryKind.Forever, 0, FileStatusCode.RecordLocked, FileStatusCode.Deadlock)]      // GR3 + item 2
    // ── FILE SHARING conflict, §9.1.13.9 item 1 ('61', OPEN): NEVER a deadlock — that clause defines none.
    [InlineData(FileRetryKind.None, 0, FileStatusCode.FileSharingConflict, FileStatusCode.FileSharingConflict)]
    [InlineData(FileRetryKind.Times, 0, FileStatusCode.FileSharingConflict, FileStatusCode.FileSharingConflict)]
    [InlineData(FileRetryKind.Times, -3, FileStatusCode.FileSharingConflict, FileStatusCode.FileSharingConflict)]
    [InlineData(FileRetryKind.Times, 2, FileStatusCode.FileSharingConflict, FileStatusCode.FileSharingConflict)]
    [InlineData(FileRetryKind.Seconds, 0, FileStatusCode.FileSharingConflict, FileStatusCode.FileSharingConflict)]
    [InlineData(FileRetryKind.Seconds, -5, FileStatusCode.FileSharingConflict, FileStatusCode.FileSharingConflict)]
    [InlineData(FileRetryKind.Seconds, 30, FileStatusCode.FileSharingConflict, FileStatusCode.FileSharingConflict)]
    [InlineData(FileRetryKind.Forever, 0, FileStatusCode.FileSharingConflict, FileStatusCode.FileSharingConflict)]
    // ── FILE SHARING conflict, §9.1.13.9 item 2 ('62', DELETE FILE): §14.9.10.4 GR15b is imperative.
    [InlineData(FileRetryKind.None, 0, FileStatusCode.DeleteFileSharing, FileStatusCode.DeleteFileSharing)]
    [InlineData(FileRetryKind.Times, 0, FileStatusCode.DeleteFileSharing, FileStatusCode.DeleteFileSharing)]
    [InlineData(FileRetryKind.Times, -3, FileStatusCode.DeleteFileSharing, FileStatusCode.DeleteFileSharing)]
    [InlineData(FileRetryKind.Times, 2, FileStatusCode.DeleteFileSharing, FileStatusCode.DeleteFileSharing)]
    [InlineData(FileRetryKind.Seconds, 0, FileStatusCode.DeleteFileSharing, FileStatusCode.DeleteFileSharing)]
    [InlineData(FileRetryKind.Seconds, -5, FileStatusCode.DeleteFileSharing, FileStatusCode.DeleteFileSharing)]
    [InlineData(FileRetryKind.Seconds, 30, FileStatusCode.DeleteFileSharing, FileStatusCode.DeleteFileSharing)]
    [InlineData(FileRetryKind.Forever, 0, FileStatusCode.DeleteFileSharing, FileStatusCode.DeleteFileSharing)]
    // ── NOT A CONFLICT AT ALL: §14.7.9.3 GR4 scopes the whole discipline to the two conflict conditions, so an
    //    OPEN's '35' (or any other unsuccessful status) is the statement's own answer and RETRY cannot touch it.
    [InlineData(FileRetryKind.None, 0, FileStatusCode.FileNotFound, FileStatusCode.FileNotFound)]
    [InlineData(FileRetryKind.Times, 2, FileStatusCode.FileNotFound, FileStatusCode.FileNotFound)]
    [InlineData(FileRetryKind.Seconds, 30, FileStatusCode.FileNotFound, FileStatusCode.FileNotFound)]
    [InlineData(FileRetryKind.Forever, 0, FileStatusCode.FileNotFound, FileStatusCode.FileNotFound)]
    public void RetryLoop_LandsTheConflictsOwnStatus_ByClass(
        FileRetryKind kind, int amount, string conflict, string expected) =>
        Assert.Equal(expected, FileRegistry.RetryLoop(() => RetryAttempt.InRunUnit(conflict), kind, amount));

    /// <summary>§14.7.9.3 GR4 again, on the axis the status cannot witness: a NON-conflict status must not be
    /// RE-ATTEMPTED either, and GR4a's zero/negative screen must make no attempt beyond the first.</summary>
    [Theory]
    [InlineData(FileRetryKind.None, 0, FileStatusCode.RecordLocked, 1)]
    [InlineData(FileRetryKind.Times, 0, FileStatusCode.RecordLocked, 1)]     // GR4a — zero: no further attempt
    [InlineData(FileRetryKind.Times, -3, FileStatusCode.RecordLocked, 1)]    // GR4a — negative: likewise
    [InlineData(FileRetryKind.Times, 3, FileStatusCode.RecordLocked, 4)]     // GR1 — n further attempts
    [InlineData(FileRetryKind.Seconds, 0, FileStatusCode.RecordLocked, 1)]   // GR4a — zero: no further attempt
    [InlineData(FileRetryKind.Seconds, -5, FileStatusCode.RecordLocked, 1)]  // GR4a — negative: likewise
    [InlineData(FileRetryKind.Seconds, 30, FileStatusCode.RecordLocked, 1)]  // GR2 — zero-length clamped period
    [InlineData(FileRetryKind.Forever, 0, FileStatusCode.RecordLocked, 2)]   // GR3 — one re-check, then the deadlock
    [InlineData(FileRetryKind.Times, 3, FileStatusCode.FileNotFound, 1)]     // GR4 — not a conflict: no retry
    [InlineData(FileRetryKind.Forever, 0, FileStatusCode.FileNotFound, 1)]   // GR4 — not a conflict: no retry
    public void RetryLoop_AttemptCount_FollowsGR1AndGR4(
        FileRetryKind kind, int amount, string conflict, int expectedCalls)
    {
        int calls = 0;
        FileRegistry.RetryLoop(() => { calls++; return RetryAttempt.InRunUnit(conflict); }, kind, amount);
        Assert.Equal(expectedCalls, calls);
    }

    /// <summary>§14.7.9.3 GR3 against a holder OUTSIDE the run unit (kb/Work PB1163): <i>"If the FOREVER phrase is
    /// specified, the mass storage control system shall attempt to gain access to a locked resource until the
    /// input-output operation has been completed"</i>. Such a holder can release while the statement waits, so
    /// FOREVER keeps attempting — pausing <see cref="FileRegistry.RetryInterval"/> before each re-attempt (A.1 item
    /// 165) — and lands the attempt that completed. The loop used to stop after one re-attempt on the premise that no
    /// releaser existed. The pause is observed, never slept, so the test times nothing.</summary>
    [Theory]
    [InlineData(FileStatusCode.FileSharingConflict)]   // OPEN, §9.1.13.9 item 1
    [InlineData(FileStatusCode.DeleteFileSharing)]     // DELETE FILE, §9.1.13.9 item 2
    public void RetryForever_AgainstAHolderOutsideTheRunUnit_WaitsUntilTheOperationCompletes(string conflict)
    {
        int calls = 0, pauses = 0;
        FileRegistry.PauseObserver.Value = interval => { Assert.Equal(FileRegistry.RetryInterval, interval); pauses++; };
        try
        {
            string status = FileRegistry.RetryLoop(
                () => ++calls <= 25 ? RetryAttempt.OutsideRunUnit(conflict) : RetryAttempt.InRunUnit(FileStatusCode.Success),
                FileRetryKind.Forever, 0);
            Assert.Equal(FileStatusCode.Success, status);
        }
        finally { FileRegistry.PauseObserver.Value = null; }
        Assert.Equal(26, calls);    // the 25 refused attempts and the one that completed
        Assert.Equal(25, pauses);   // one interval before each re-attempt
    }

    /// <summary>The same discipline's other arms against an outside holder (kb/Work PB1163): n TIMES pauses before
    /// each of its n re-attempts (GR1, A.1 item 165) and then lands the conflict's own status; FOREVER stops waiting
    /// the moment the holder is one of this run unit's own connectors, which cannot release while the statement
    /// executes (the A.1 item 109 deadlock); and a re-attempt against an in-run-unit holder never pauses.</summary>
    [Fact]
    public void RetryAgainstAnOutsideHolder_PausesBeforeEachReattempt_AndOnlyThen()
    {
        int pauses = 0;
        FileRegistry.PauseObserver.Value = _ => pauses++;
        try
        {
            Assert.Equal(FileStatusCode.FileSharingConflict, FileRegistry.RetryLoop(
                () => RetryAttempt.OutsideRunUnit(FileStatusCode.FileSharingConflict), FileRetryKind.Times, 3));
            Assert.Equal(3, pauses);

            pauses = 0;
            int calls = 0;
            Assert.Equal(FileStatusCode.DeleteFileSharing, FileRegistry.RetryLoop(
                () => ++calls == 1 ? RetryAttempt.OutsideRunUnit(FileStatusCode.DeleteFileSharing)
                                   : RetryAttempt.InRunUnit(FileStatusCode.DeleteFileSharing),
                FileRetryKind.Forever, 0));
            Assert.Equal(2, calls);    // the outside refusal, then an in-run-unit one that no wait can clear
            Assert.Equal(1, pauses);

            pauses = 0;
            FileRegistry.RetryLoop(() => RetryAttempt.InRunUnit(FileStatusCode.RecordLocked), FileRetryKind.Times, 3);
            FileRegistry.RetryLoop(() => RetryAttempt.InRunUnit(FileStatusCode.RecordLocked), FileRetryKind.Forever, 0);
            Assert.Equal(0, pauses);
        }
        finally { FileRegistry.PauseObserver.Value = null; }
    }

    /// <summary>⛔ THE §14.9.30.4 GR9 DRIFT TEST — the READ's OWN binding to §14.7.9, on BOTH read formats
    /// (kb/Work PB346). GR9 is three sentences and the last two are delegations: <i>"If the RETRY phrase is
    /// specified, additional attempts may be made to read the record as specified in the rules in 14.7.9,
    /// RETRY phrase"</i> and <i>"The I-O status is set in accordance with the rules for the RETRY phrase"</i>.
    /// Every other RETRY test in this file reaches <c>RetryLoop</c> through DELETE FILE, REWRITE or DELETE
    /// RECORD; until this one, no test passed a RETRY phrase to a READ at all, so a Format-1 or Format-2 read
    /// could have dropped or mis-plumbed its retry arguments and stayed green — which is exactly how the
    /// SECONDS/FOREVER conflation survived in five descriptions after the code was fixed.
    /// <para>The expected column is derived, not observed: the holder is a file connector of the EXECUTING run
    /// unit and cannot release while this statement runs, so every form exhausts. §14.7.9.3 GR4 a) (no phrase,
    /// or an arithmetic expression evaluating negative or zero) and the clause's closing paragraph both land
    /// "the appropriate value … according to the rules for 9.1.13", i.e. §9.1.13.8 item 1's '51'; GR1's count
    /// and GR2's timeout period reach the same closing paragraph — GR2 because its period is clamped to this
    /// implementation's maximum meaningful value of ZERO (Annex A.1 item 166). GR3's FOREVER alone is the
    /// §9.1.13.8 item 2 deadlock this implementation detects, '52'.</para></summary>
    [Theory]
    [InlineData(FileRetryKind.None, 0, FileStatusCode.RecordLocked)]       // GR4a — no phrase
    [InlineData(FileRetryKind.Times, 0, FileStatusCode.RecordLocked)]      // GR4a — zero arithmetic-expression-1
    [InlineData(FileRetryKind.Times, -3, FileStatusCode.RecordLocked)]     // GR4a — negative
    [InlineData(FileRetryKind.Times, 2, FileStatusCode.RecordLocked)]      // GR1 — exhausted count
    [InlineData(FileRetryKind.Seconds, 0, FileStatusCode.RecordLocked)]    // GR4a — zero arithmetic-expression-2
    [InlineData(FileRetryKind.Seconds, -5, FileStatusCode.RecordLocked)]   // GR4a — negative
    [InlineData(FileRetryKind.Seconds, 30, FileStatusCode.RecordLocked)]   // GR2 — clamped to a zero-length period
    [InlineData(FileRetryKind.Forever, 0, FileStatusCode.Deadlock)]        // GR3 + §9.1.13.8 item 2
    public void ReadUnderEveryRetryForm_BindsGR9ToTheRetryRules_OnBothFormats(
        FileRetryKind kind, int amount, string expected)
    {
        string tag = $"{(int)kind}-{amount}";

        // ── FORMAT 2 (random keyed access): the governed random-read entry, RELATIVE organization.
        // Its conflict leg is PRE-read like Format 1's since kb/Work PB338 (§14.9.30.4 GR10 a)/d)), so
        // the entry OWNS the retrieval and there is no separate read left to govern afterwards. ──────────
        TwoOpenSharers("RGA", "RGB", $"lk-r2retry-{tag}.dat");
        CobolFile.SetRelativeKey("RGA", 1);
        Assert.Equal(FileStatusCode.Success,
            CobolFile.ReadKeyedShared("RGA", -1, "", FileRecordLock.WithLock, false, FileRetryKind.None, 0, out _));
        CobolFile.SetRelativeKey("RGB", 1);
        Assert.Equal(expected,
            CobolFile.ReadKeyedShared("RGB", -1, "", FileRecordLock.None, false, kind, amount, out _));
        Assert.Equal(expected, CobolFile.Status("RGB"));   // §14.9.30.4 GR1 — the connector's I-O status is updated

        // ── FORMAT 1 (sequential access): the pre-read conflict leg, SEQUENTIAL organization. ───────────────
        CobolFile.Init();
        string host = $"lk-r1retry-{tag}.dat";
        CobolFile.Register("RSA", host, 5, false, false);
        CobolFile.Register("RSB", host, 5, false, false);
        CobolFile.RegisterSharing("RSA", FileSharing.AllOther, FileLockMode.Manual, false);
        CobolFile.RegisterSharing("RSB", FileSharing.AllOther, FileLockMode.Manual, false);
        CobolFile.OpenOutput("RSA", host, assignDynamic: false, page: null);
        CobolFile.Write("RSA", "AAAAA", -1, page: null);
        CobolFile.Write("RSA", "BBBBB", -1, page: null);
        CobolFile.Close("RSA");
        CobolFile.OpenInput("RSA", host, assignDynamic: false, page: null);
        CobolFile.OpenInput("RSB", host, assignDynamic: false, page: null);
        Assert.Equal(FileStatusCode.Success, CobolFile.ReadShared(
            "RSA", false, FileRecordLock.WithLock, false, false, FileRetryKind.None, 0, out string held));
        Assert.Equal("AAAAA", held);
        Assert.Equal(expected, CobolFile.ReadShared(
            "RSB", false, FileRecordLock.None, false, false, kind, amount, out string img));
        Assert.Equal(expected, CobolFile.Status("RSB"));
        Assert.Equal("", img);   // §14.9.30.4 GR10 e) — the READ is unsuccessful; no record is made available
        CobolFile.Close("RSA");
        CobolFile.Close("RSB");
    }

    [Fact]
    public void Unlock_OfANotOpenFile_Is42()
    {
        CobolFile.Init();
        CobolFile.RegisterRelative("CX", "lk-notopen.dat", 8, false, Random, 4);
        CobolFile.RegisterSharing("CX", FileSharing.AllOther, FileLockMode.Manual, false);
        CobolFile.Unlock("CX", records: false);
        Assert.Equal(FileStatusCode.FileNotOpen, CobolFile.Status("CX"));   // 42 — §9.1.13 (:11579)
    }

    // ── P10 Step 8: the governed mutating verbs (§14.9.35 GR11/GR12 · §14.9.10 GR6/GR7 · §14.9.51 GR10/GR11)
    //    and the sequential-organization governed READ (§9.1.16 ordinal identity; §14.9.30 GR9/GR22) ──────────

    /// <summary>Two open RELATIVE sharers on one seeded physical file (record 1 = AAAAA).</summary>
    private static void TwoOpenSharers(string a, string b, string host)
    {
        TwoConnectorsOneFile(a, b, host);
        CobolFile.OpenOutput(a, host, assignDynamic: false, page: null);
        CobolFile.SetRelativeKey(a, 1);
        Assert.Equal(FileStatusCode.Success, CobolFile.WriteKeyed(a, "AAAAA"));
        CobolFile.Close(a);
        CobolFile.OpenIO(a, host, assignDynamic: false, page: null);
        CobolFile.OpenIO(b, host, assignDynamic: false, page: null);
        Assert.Equal(FileStatusCode.Success, CobolFile.Status(a));
        Assert.Equal(FileStatusCode.Success, CobolFile.Status(b));
    }

    [Fact]
    public void RewriteAndDelete_HonorAnotherConnectorsLock_51_ThenUnlockFrees()
    {
        TwoOpenSharers("MA", "MB", "lk-mut.dat");
        CobolFile.SetRelativeKey("MA", 1);
        Assert.Equal(FileStatusCode.Success,
            CobolFile.ReadKeyedShared("MA", -1, "", FileRecordLock.WithLock, false, FileRetryKind.None, 0, out _));   // MA locks record 1
        CobolFile.SetRelativeKey("MB", 1);
        // §14.9.35 GR11 — the record identified for rewriting is locked by another connector → 51 (not rewritten).
        Assert.Equal(FileStatusCode.RecordLocked,
            CobolFile.RewriteShared("MB", "BBBBB", -1, FileRecordLock.None, FileRetryKind.None, 0));
        // §14.9.10 GR6 — same for deletion; a bounded RETRY exhausts to 51, FOREVER deadlock-bails to 52 (§14.7.9).
        Assert.Equal(FileStatusCode.RecordLocked, CobolFile.DeleteShared("MB", "", FileRetryKind.Times, 2));
        Assert.Equal(FileStatusCode.Deadlock,
            CobolFile.RewriteShared("MB", "BBBBB", -1, FileRecordLock.None, FileRetryKind.Forever, 0));
        CobolFile.Unlock("MA", records: false);   // §14.9.47 GR1
        Assert.Equal(FileStatusCode.Success,
            CobolFile.RewriteShared("MB", "BBBBB", -1, FileRecordLock.None, FileRetryKind.None, 0));
    }

    [Fact]
    public void WriteShared_WithLock_LocksTheWrittenRecord_AndSingleLockingReleasesItOnTheNextWrite()
    {
        TwoOpenSharers("WA", "WB", "lk-wrlock.dat");
        CobolFile.SetRelativeKey("WA", 2);
        // §14.9.51 GR11 — WITH LOCK locks the record written.
        Assert.Equal(FileStatusCode.Success,
            CobolFile.WriteShared("WA", "BBBBB", -1, FileRecordLock.WithLock, FileRetryKind.None, 0, page: null));
        Assert.True(CobolFile.IsLockedByOther("WB", "2"));
        // §14.9.51 GR10 / §12.4.5.9 GR6 — single locking: the next WRITE releases the prior lock.
        CobolFile.SetRelativeKey("WA", 3);
        Assert.Equal(FileStatusCode.Success,
            CobolFile.WriteShared("WA", "CCCCC", -1, FileRecordLock.None, FileRetryKind.None, 0, page: null));
        Assert.False(CobolFile.IsLockedByOther("WB", "2"));
    }

    [Fact]
    public void ReadShared_SequentialOrdinals_Conflict51_AndAdvancingOnLockSkips()
    {
        CobolFile.Init();
        const string host = "lk-seq.dat";
        CobolFile.Register("SA", host, 5, false, false);
        CobolFile.Register("SB", host, 5, false, false);
        CobolFile.RegisterSharing("SA", FileSharing.AllOther, FileLockMode.Manual, false);
        CobolFile.RegisterSharing("SB", FileSharing.AllOther, FileLockMode.Manual, false);
        CobolFile.OpenOutput("SA", host, assignDynamic: false, page: null);
        CobolFile.Write("SA", "AAAAA", -1, page: null);
        CobolFile.Write("SA", "BBBBB", -1, page: null);
        CobolFile.Write("SA", "CCCCC", -1, page: null);
        CobolFile.Close("SA");
        CobolFile.OpenInput("SA", host, assignDynamic: false, page: null);
        CobolFile.OpenInput("SB", host, assignDynamic: false, page: null);
        // SA reads ordinal 1 WITH LOCK (§9.1.16 — the sequential lock identity is the ordinal position).
        Assert.Equal(FileStatusCode.Success,
            CobolFile.ReadShared("SA", false, FileRecordLock.WithLock, false, false, FileRetryKind.None, 0, out string img));
        Assert.Equal("AAAAA", img);
        // SB's READ of ordinal 1 conflicts BEFORE the physical read (§14.9.30 GR9; FPI unchanged, GR10a) → 51.
        Assert.Equal(FileStatusCode.RecordLocked,
            CobolFile.ReadShared("SB", false, FileRecordLock.None, false, false, FileRetryKind.None, 0, out _));
        Assert.Equal(FileStatusCode.RecordLocked, CobolFile.Status("SB"));
        // ADVANCING ON LOCK (§14.9.30 GR22) skip-scans the locked record — SB gets ordinal 2, no conflict raised.
        Assert.Equal(FileStatusCode.Success,
            CobolFile.ReadShared("SB", false, FileRecordLock.None, true, false, FileRetryKind.None, 0, out img));
        Assert.Equal("BBBBB", img);
        // IGNORING LOCK would have delivered the locked record itself (GR12) — proven by the file_sharing_seq golden.
        CobolFile.Close("SA");
        CobolFile.Close("SB");
    }

    /// <summary>
    /// ⛔ §14.9.30.4 GR22 IS A FORMAT-1 RULE, NOT A SEQUENTIAL-ORGANIZATION ONE (kb/Work PB340). The skip-scan
    /// loop lived only inside the sequential arm of the governed read, so a RELATIVE or INDEXED
    /// <c>READ … NEXT ADVANCING ON LOCK</c> answered '51' — the one status GR22 says cannot arise ("A record
    /// operation conflict condition does not exist"). Both organizations now enter the SAME
    /// <c>CobolFile.ReadShared</c> the sequential one does, so this asserts the shared entry on both, with the
    /// non-advancing '51' as the control that proves the lock was really in the way.
    /// </summary>
    [Theory]
    [InlineData(false)]   // RELATIVE — the lock identity is the RRN
    [InlineData(true)]    // INDEXED  — the lock identity is the prime key
    public void ReadShared_AdvancingOnLock_SkipScansOnKeyedOrganizations_GR22(bool indexed)
    {
        const int Dynamic = 2;   // KeyedAccess.Dynamic
        CobolFile.Init();
        string host = indexed ? "lk-adv-ix.dat" : "lk-adv-rel.dat";
        string[] recs = indexed ? ["K001ALPHA", "K002BRAVO"] : ["ALPHA", "BRAVO"];
        int width = indexed ? 9 : 5;
        void Register(string n, int access)
        {
            if (indexed) CobolFile.RegisterIndexed(n, host, width, false, access, 0, 4);
            else CobolFile.RegisterRelative(n, host, width, false, access, 4);
        }
        Register("SEED", Random);
        CobolFile.OpenOutput("SEED", host, assignDynamic: false, page: null);
        for (int i = 0; i < recs.Length; i++)
        {
            if (!indexed) CobolFile.SetRelativeKey("SEED", i + 1);
            Assert.Equal(FileStatusCode.Success, CobolFile.WriteKeyed("SEED", recs[i]));
        }
        CobolFile.Close("SEED");
        foreach (string n in new[] { "KA", "KB", "KC" })
        {
            Register(n, Dynamic);
            CobolFile.RegisterSharing(n, FileSharing.AllOther, FileLockMode.Manual, false);
            CobolFile.OpenIO(n, host, assignDynamic: false, page: null);
        }
        // KA takes the first record WITH LOCK (§14.9.30.4 GR11 d) — manual locking sets it only on request).
        Assert.Equal(FileStatusCode.Success,
            CobolFile.ReadShared("KA", false, FileRecordLock.WithLock, false, false, FileRetryKind.None, 0, out string a));
        Assert.Equal(recs[0], a);
        // The CONTROL: a plain READ NEXT of that same first record is the GR9 record operation conflict, '51'.
        Assert.Equal(FileStatusCode.RecordLocked,
            CobolFile.ReadShared("KC", false, FileRecordLock.None, false, false, FileRetryKind.None, 0, out _));
        // GR22: ADVANCING ON LOCK reads the locked record, discards it, re-executes — the SECOND record is made
        // available with a successful status and no conflict condition.
        Assert.Equal(FileStatusCode.Success,
            CobolFile.ReadShared("KB", false, FileRecordLock.None, true, false, FileRetryKind.None, 0, out string b));
        Assert.Equal(recs[1], b);
        foreach (string n in new[] { "KA", "KB", "KC" }) CobolFile.Close(n);
    }

    /// <summary>
    /// THE COMBINATION A SINGLE ENUM COULD NOT SAY (kb/Work PB331): IGNORING LOCK and WITH NO LOCK are
    /// alternatives of two DIFFERENT printed brackets of ISO 14.9.30.2, and 5.2.6.1 selects "a unique
    /// combination of possibilities from a series of brackets", so one READ may write both. The runtime now
    /// takes them as two arguments, and this drives BOTH of the rules that then apply, each with the control
    /// arm that falsifies it:
    /// <list type="bullet">
    /// <item>14.9.30.4 GR11 b) — under MULTIPLE record locking, "the NO LOCK phrase … and the record accessed
    /// was already locked by that file connector … that record lock is released". CONTROL: the same
    /// IGNORING-LOCK read with NO retention phrase must KEEP the lock, or the release assertion would pass for
    /// a runtime that simply released everything.</item>
    /// <item>14.9.30.4 GR12 — "If the IGNORING LOCK phrase is specified …, the requested record is made
    /// available, even if it is locked". CONTROL: the same read WITHOUT the phrase must fail 51.</item>
    /// </list>
    /// <para>The two rules cannot meet on one record — GR11 b) needs THIS connector's lock and GR12 another's,
    /// and a record carries one lock — so they are exercised in sequence on the same pair of connectors.</para>
    /// <para>Before PB331, `IGNORING LOCK` was a member of the SAME enum as `WITH NO LOCK`, so the first half
    /// was unreachable by construction: whichever member the phrase pair collapsed to, the other rule's test
    /// could not fire.</para>
    /// </summary>
    [Fact]
    public void ReadKeyedShared_IgnoringLockWithNoLock_ReleasesThisConnectorsLock_AndReadsAnothersLockedRecord()
    {
        CobolFile.Init();
        const string host = "lk-ignore-nolock.dat";
        CobolFile.RegisterRelative("IA", host, 8, false, Random, 4);
        CobolFile.RegisterRelative("IB", host, 8, false, Random, 4);
        // MULTIPLE record locking, so GR11 a)'s blanket per-statement release is NOT what frees the lock below.
        CobolFile.RegisterSharing("IA", FileSharing.AllOther, FileLockMode.Manual, true);
        CobolFile.RegisterSharing("IB", FileSharing.AllOther, FileLockMode.Manual, true);
        CobolFile.OpenOutput("IA", host, assignDynamic: false, page: null);
        CobolFile.SetRelativeKey("IA", 1);
        Assert.Equal(FileStatusCode.Success, CobolFile.WriteKeyed("IA", "ALPHA"));
        CobolFile.Close("IA");
        CobolFile.OpenIO("IA", host, assignDynamic: false, page: null);
        CobolFile.OpenIO("IB", host, assignDynamic: false, page: null);

        string Read(string who, FileRecordLock retention, bool ignoring)
        {
            CobolFile.SetRelativeKey(who, 1);
            return CobolFile.ReadKeyedShared(who, -1, "", retention, ignoring, FileRetryKind.None, 0, out _);
        }

        // ── GR11 b): IA locks record 1, then re-reads it IGNORING LOCK WITH NO LOCK ──
        Assert.Equal(FileStatusCode.Success, Read("IA", FileRecordLock.WithLock, false));
        Assert.True(CobolFile.IsLockedByOther("IB", "1"));
        // CONTROL — IGNORING LOCK alone sets no lock and releases none (GR11 d): the lock is still IA's.
        Assert.Equal(FileStatusCode.Success, Read("IA", FileRecordLock.None, true));
        Assert.True(CobolFile.IsLockedByOther("IB", "1"));
        // SUBJECT — both brackets at once: the NO LOCK phrase releases the lock IA already held.
        Assert.Equal(FileStatusCode.Success, Read("IA", FileRecordLock.WithNoLock, true));
        Assert.False(CobolFile.IsLockedByOther("IB", "1"));

        // ── GR12: IB locks record 1; IA reads it anyway ──
        Assert.Equal(FileStatusCode.Success, Read("IB", FileRecordLock.WithLock, false));
        // CONTROL — without the phrase the record is inaccessible to the other connector (GR9 -> 51).
        Assert.Equal(FileStatusCode.RecordLocked, Read("IA", FileRecordLock.None, false));
        // SUBJECT — IGNORING LOCK makes it available, and WITH NO LOCK rides along without disturbing IB's lock.
        Assert.Equal(FileStatusCode.Success, Read("IA", FileRecordLock.WithNoLock, true));
        Assert.True(CobolFile.IsLockedByOther("IA", "1"));
        CobolFile.Close("IA");
        CobolFile.Close("IB");
    }

    [Fact]
    public void DeleteFile_OpenByAnotherConnector_62_ThenCloseFrees()
    {
        CobolFile.Init();
        const string host = "lk-dfs.dat";
        CobolFile.Register("DA", host, 5, false, false);
        CobolFile.Register("DB", host, 5, false, false);
        CobolFile.OpenOutput("DA", host, assignDynamic: false, page: null);
        CobolFile.Write("DA", "HELLO", -1, page: null);
        // §9.1.13.9 item 2 / §14.9.10 GR15 — the physical file is open by ANOTHER connector → 62 (not deleted).
        // EVERY retry form exhausts to 62: §14.7.9.3's closing paragraph lands the conflict's own §9.1.13 status
        // and §9.1.13.9 defines no deadlock value for a FILE SHARING conflict (kb/Work PB142).
        Assert.Equal(FileStatusCode.DeleteFileSharing, CobolFile.DeleteFile("DB"));
        Assert.Equal(FileStatusCode.DeleteFileSharing, CobolFile.DeleteFile("DB", FileRetryKind.Times, 2));
        Assert.Equal(FileStatusCode.DeleteFileSharing, CobolFile.DeleteFile("DB", FileRetryKind.Forever, 0));
        Assert.Equal(FileStatusCode.DeleteFileSharing, CobolFile.DeleteFile("DB", FileRetryKind.Seconds, 0));
        Assert.Equal(FileStatusCode.DeleteFileSharing, CobolFile.DeleteFile("DB", FileRetryKind.Seconds, 30));
        CobolFile.Close("DA");
        Assert.Equal(FileStatusCode.Success, CobolFile.DeleteFile("DB"));            // GR20 — deleted
        Assert.Equal(FileStatusCode.OptionalFileNotFound, CobolFile.DeleteFile("DB"));   // GR14 — absent = successful '05'
    }

    /// <summary>ISO §14.9.40.4 GR15 a) / §14.9.24.4 GR12 a) — SORT's and MERGE's implicit GIVING open is
    /// <i>"as if an OPEN statement with the OUTPUT and SHARING WITH NO OTHER phrases had been executed"</i>, and
    /// §9.1.15 1) makes that mode <i>"exclusive access to a physical file"</i>. The exclusive half is not
    /// observable from a single-threaded COBOL program — no statement of the program's runs between the implicit
    /// OPEN and the implicit CLOSE — so it is pinned HERE, on the very call
    /// <c>SortEmitter.EmitImplicitOpen</c> renders (kb/Work PB714).
    /// <para>The CONTROL arm is what the emitter used to render: a plain <c>OpenOutput</c>, which leaves the
    /// connector's sharing mode at §9.1.15's undetermined implementor default, under which
    /// <see cref="FileRegistry.Conflicts"/> finds a Table 19 <i>Normal open</i> candidate and lets the second
    /// connector in.</para></summary>
    [Fact]
    public void SortGivingImplicitOpen_TakesNoOther_AndLocksOutEveryOtherConnector_GR15a()
    {
        const string host = "pb714-giving.dat";
        // A GIVING file that declares SHARING WITH ALL OTHER, so both connectors are sharing participants and
        // Table 19 — not the operating environment's own handle sharing — is the only arbiter of what follows.
        static void TwoSharers()
        {
            CobolFile.Init();
            CobolFile.Register("GA", "pb714-giving.dat", 5, false, false);
            CobolFile.Register("GB", "pb714-giving.dat", 5, false, false);
            CobolFile.RegisterSharing("GA", FileSharing.AllOther, FileLockMode.Manual, false);
            CobolFile.RegisterSharing("GB", FileSharing.AllOther, FileLockMode.Manual, false);
        }

        // CONTROL — the pre-PB714 rendering: a plain OPEN OUTPUT, so the GIVING connector is arbitrated under
        // its OWN ALL OTHER clause and GB's OPEN INPUT finds Table 19's (all other/input × all other/extend I-O
        // output) cell = Normal open. It SUCCEEDS, which is the missing refusal PB714 is about.
        TwoSharers();
        CobolFile.OpenOutput("GA", host, assignDynamic: false, page: null);
        Assert.Equal(FileStatusCode.Success, CobolFile.Status("GA"));
        CobolFile.OpenInput("GB", host, assignDynamic: false, page: null);
        Assert.Equal(FileStatusCode.Success, CobolFile.Status("GB"));
        CobolFile.Close("GB");
        CobolFile.Close("GA");

        // SUBJECT — the rendering GR15 a) names, argument for argument as SortEmitter emits it.
        TwoSharers();
        CobolFile.OpenShared("GA", FileOpenMode.Output, hasSharingOverride: true, FileSharing.NoOther,
            FileRetryKind.None, 0, tape: OpenTapePhrase.None, host, assignDynamic: false, page: null);
        Assert.Equal(FileStatusCode.Success, CobolFile.Status("GA"));
        // Table 19's "sharing with no other / extend I-O input output" column is Unsuccessful open in EVERY row,
        // so no second connector gets in, in any mode — §9.1.13.9 item 1 → '61'.
        CobolFile.OpenInput("GB", host, assignDynamic: false, page: null);
        Assert.Equal(FileStatusCode.FileSharingConflict, CobolFile.Status("GB"));
        CobolFile.OpenExtend("GB", host, assignDynamic: false, page: null);
        Assert.Equal(FileStatusCode.FileSharingConflict, CobolFile.Status("GB"));
        // §9.1.15's lock "is removed by an explicit or implicit CLOSE statement executed for that file
        // connector" — the implicit GR15 c) CLOSE ends the exclusivity, so the same open then succeeds.
        CobolFile.Close("GA");
        CobolFile.OpenInput("GB", host, assignDynamic: false, page: null);
        Assert.Equal(FileStatusCode.Success, CobolFile.Status("GB"));
        CobolFile.Close("GB");
    }

    /// <summary>ISO §14.9.40.4 GR12 a) / §14.9.24.4 GR7 a) — the USING arm's override, at the same layer. The
    /// conformance goldens drive this end to end; this pins the two renderings against each other so the
    /// CONTROL arm cannot quietly become the subject again (kb/Work PB714).</summary>
    [Fact]
    public void SortUsingImplicitOpen_ReadOnlyOverride_RefusedAgainstNonInputConnector_GR12a()
    {
        CobolFile.Init();
        const string host = "pb714-using.dat";
        CobolFile.Register("UA", host, 5, false, false);
        CobolFile.Register("UB", host, 5, false, false);
        // Both declare SHARING WITH ALL OTHER — the one clause GR12 a) singles out.
        CobolFile.RegisterSharing("UA", FileSharing.AllOther, FileLockMode.Manual, false);
        CobolFile.RegisterSharing("UB", FileSharing.AllOther, FileLockMode.Manual, false);
        CobolFile.OpenOutput("UB", host, assignDynamic: false, page: null);
        CobolFile.Write("UB", "AAAAA", -1, page: null);
        CobolFile.Close("UB");
        CobolFile.OpenExtend("UB", host, assignDynamic: false, page: null);   // "other than input" (§9.1.15 2)
        Assert.Equal(FileStatusCode.Success, CobolFile.Status("UB"));

        // CONTROL — the pre-PB714 rendering: the file's OWN ALL OTHER clause governs, and Table 19's
        // (all other/input × all other/extend I-O output) cell is Normal open, so the sort read the file.
        CobolFile.OpenInput("UA", host, assignDynamic: false, page: null);
        Assert.Equal(FileStatusCode.Success, CobolFile.Status("UA"));
        CobolFile.Close("UA");

        // SUBJECT — GR12 a)'s override. Row "SHARING WITH READ ONLY / INPUT" × the same column is Unsuccessful
        // open; §9.1.15 2) says it in words: read only "will be unsuccessful if the physical file is associated
        // with another file connector whose open mode is other than input".
        CobolFile.OpenShared("UA", FileOpenMode.Input, hasSharingOverride: true, FileSharing.ReadOnly,
            FileRetryKind.None, 0, tape: OpenTapePhrase.None, host, assignDynamic: false, page: null);
        Assert.Equal(FileStatusCode.FileSharingConflict, CobolFile.Status("UA"));
        CobolFile.Close("UB");
    }

    /// <summary>
    /// ⛔ THE CROSS-CONNECTOR MATRIX (kb/Work PB669): a locked record is inaccessible to a file connector
    /// that declared NO SHARING clause and NO LOCK MODE clause, because ISO §9.1.16 states the rule over the
    /// RECORD and the OTHER connector with no qualification on the reading connector at all — <i>"While locked
    /// by a given file connector, a record is not accessible to another file connector in the same or a
    /// different run unit, except by the execution of a READ statement with the IGNORING LOCK phrase."</i>
    /// §12.4.5.9.4 GR1 a) and b) 1. restrict a clause-less connector only from SETTING locks, and GR1 b) 2.
    /// leaves its lock mode to the implementor, whose determination here is <c>FileLockMode.None</c>.
    /// <para>Every governed verb that states the record operation conflict is driven, because the defect was
    /// ONE early return copied into each of them: §14.9.30.4 GR9 (both READ formats), §14.9.35.4 GR11
    /// (REWRITE) and §14.9.10.4 GR6 (DELETE). The pre-fix answer was '00' for every one of them, and the
    /// DELETE actually REMOVED the locked record.</para>
    /// <para>THREE CONTROLS, so the assertions cannot pass for a runtime that simply refuses everything:
    /// §14.9.30.4 GR12's IGNORING LOCK still reads the record; an UNLOCKED record of the SAME physical file is
    /// still available through the plain connector (GR9 names the record IDENTIFIED for access); and the
    /// REVERSE direction holds — the plain connector SETS no lock, so the MANUAL connector reads the record
    /// the plain one just read with no conflict, which is GR1 b) 2.'s determination being honoured rather than
    /// the check being applied symmetrically by accident.</para>
    /// </summary>
    [Fact]
    public void PlainConnector_SeesAnothersRecordLock_ButSetsNone_9_1_16()
    {
        CobolFile.Init();
        const string host = "pb669-visibility.dat";
        CobolFile.RegisterRelative("VA", host, 5, false, Random, 4);
        CobolFile.RegisterRelative("VB", host, 5, false, Random, 4);
        // VA declares the posture; VB declares NOTHING — no RegisterSharing call at all, which is exactly what
        // the emitter omits for a SELECT with neither clause.
        CobolFile.RegisterSharing("VA", FileSharing.AllOther, FileLockMode.Manual, false);
        CobolFile.OpenOutput("VB", host, assignDynamic: false, page: null);
        CobolFile.SetRelativeKey("VB", 1);
        Assert.Equal(FileStatusCode.Success, CobolFile.WriteKeyed("VB", "ALPHA"));
        CobolFile.SetRelativeKey("VB", 2);
        Assert.Equal(FileStatusCode.Success, CobolFile.WriteKeyed("VB", "BRAVO"));
        CobolFile.Close("VB");
        CobolFile.OpenIO("VA", host, assignDynamic: false, page: null);
        CobolFile.OpenIO("VB", host, assignDynamic: false, page: null);

        // VA locks record 1 (§14.9.30.4 GR11 d) — manual locking, the LOCK phrase written).
        CobolFile.SetRelativeKey("VA", 1);
        Assert.Equal(FileStatusCode.Success,
            CobolFile.ReadKeyedShared("VA", -1, "", FileRecordLock.WithLock, false, FileRetryKind.None, 0, out _));

        // SUBJECT — the three conflict rules, through the clause-less connector.
        CobolFile.SetRelativeKey("VB", 1);
        Assert.Equal(FileStatusCode.RecordLocked,              // §14.9.30.4 GR9
            CobolFile.ReadKeyedShared("VB", -1, "", FileRecordLock.None, false, FileRetryKind.None, 0, out _));
        CobolFile.SetRelativeKey("VB", 1);
        Assert.Equal(FileStatusCode.RecordLocked,              // §14.9.35.4 GR11
            CobolFile.RewriteShared("VB", "ZULU ", -1, FileRecordLock.None, FileRetryKind.None, 0));
        CobolFile.SetRelativeKey("VB", 1);
        Assert.Equal(FileStatusCode.RecordLocked,              // §14.9.10.4 GR6
            CobolFile.DeleteShared("VB", "", FileRetryKind.None, 0));

        // CONTROL 1 — §14.9.30.4 GR12: IGNORING LOCK still makes the record available.
        CobolFile.SetRelativeKey("VB", 1);
        Assert.Equal(FileStatusCode.Success,
            CobolFile.ReadKeyedShared("VB", -1, "", FileRecordLock.None, true, FileRetryKind.None, 0, out string ign));
        Assert.Equal("ALPHA", ign);

        // CONTROL 2 — an UNLOCKED record of the same physical file is unaffected.
        CobolFile.SetRelativeKey("VB", 2);
        Assert.Equal(FileStatusCode.Success,
            CobolFile.ReadKeyedShared("VB", -1, "", FileRecordLock.None, false, FileRetryKind.None, 0, out string two));
        Assert.Equal("BRAVO", two);

        // CONTROL 3 — the REVERSE direction: §12.4.5.9.4 GR1 b) 2.'s implementor default is NO record locking,
        // so VB's read of record 2 left no lock and VA reads it freely.
        CobolFile.SetRelativeKey("VA", 2);
        Assert.Equal(FileStatusCode.Success,
            CobolFile.ReadKeyedShared("VA", -1, "", FileRecordLock.None, false, FileRetryKind.None, 0, out _));

        // And the record VA holds is released by UNLOCK (§14.9.47 GR1), after which VB reads it.
        CobolFile.Unlock("VA", false);
        CobolFile.SetRelativeKey("VB", 1);
        Assert.Equal(FileStatusCode.Success,
            CobolFile.ReadKeyedShared("VB", -1, "", FileRecordLock.None, false, FileRetryKind.None, 0, out string after));
        Assert.Equal("ALPHA", after);
        CobolFile.Close("VA");
        CobolFile.Close("VB");
    }
}
