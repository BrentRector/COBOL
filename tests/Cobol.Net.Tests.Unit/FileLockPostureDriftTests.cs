// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using CobolNet.Runtime.IO;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ ISO §9.1.15 ADDRESSES TWO AUDIENCES WITH ONE SHARING MODE, AND THE OPERATING ENVIRONMENT'S SHARE MODE CAN
/// ONLY SERVE THE SECOND (kb/Work PB740).
///
/// <para><b>Inside the run unit</b> — <i>"Multiple paths of access may exist in the same runtime element,
/// contained elements, separate runtime elements within the same run unit, or runtime elements in different run
/// units"</i> — the gate is named exactly: <i>"Before access to a shared physical file is allowed through an
/// OPEN statement, the sharing mode and the open mode of that OPEN statement shall be allowed by all other file
/// connectors that are currently associated with the physical file, as described in … Table 19"</i>. So a pair
/// <c>Table19.Conflicts</c> PERMITS shall open, and the host handle may not veto it.</para>
///
/// <para><b>Outside it</b> — <i>"The successful opening of a file establishes a file lock for the applicable
/// sharing rules, thereby preventing other run units from opening that file with incompatible sharing
/// rules"</i> — the host share mode IS that lock, and §9.1.15's three rules say what it denies.</para>
///
/// <para><b>What PB740 was.</b> One boolean — "did this SELECT write a SHARING or LOCK MODE clause?" — was spent
/// on both questions, and answered each backwards. Two clause-less connectors on one ASSIGN that Table 19
/// permits to share (<c>OPEN INPUT F-A</c> then <c>OPEN EXTEND F-B</c>) were refused by the host and the second
/// OPEN answered '30' — a status no Table 19 row and no §9.1.13.9 item produces — and the appended record was
/// never written. Its mirror, measured across processes: a connector opened <c>SHARING WITH NO OTHER</c>, the
/// mode §9.1.15 1) calls <i>exclusive access</i>, took <see cref="FileShare.ReadWrite"/>, so an external process
/// appended four bytes to a file the program held open, while a connector with NO clause refused the same
/// write.</para>
///
/// <para>The guard is therefore the WHOLE agreement, not the reported cell: every (sharing mode × open mode)
/// pair on both sides, measured through real connectors on a real physical file, against
/// <c>Table19.Conflicts</c>' own verdict — because the reported pair (INPUT + EXTEND) is one cell of a
/// disagreement the two layers can have anywhere, and the golden that had certified Table 19 could not see it
/// (both of its clause-less legs held the ACCESS axis fixed).</para>
///
/// <para>⛔ AND WHAT THE OPERATING ENVIRONMENT CAN EXPRESS IS MEASURED, NOT ASSUMED (kb/Work PB795). The file
/// lock is only ever as strong as the host's share modes, and .NET reaches them on Unix through an advisory
/// <c>flock</c> with two states: <see cref="FileShare.None"/> is <c>LOCK_EX</c> and every other value is
/// <c>LOCK_SH</c>. §9.1.15 2)'s <i>"restricts concurrent access … to input mode"</i> therefore has no
/// expression on such a host at all, and asserting it there was two days of red CI that named nothing.
/// <see cref="ExpectedOutsideWriter"/> translates the standard's answer through
/// <see cref="HostCapability.Sharing"/>'s MEASUREMENT, <see cref="TheHostsShareModeSemanticsAreTheDocumentedOnes"/>
/// pins the measurement itself against the determination in <c>DESIGN-runtime-library.md</c>, and a refused or an
/// admitted OPEN is asserted as a DELTA so that it measures the OPEN rather than the host.</para>
/// </summary>
public sealed class FileLockPostureDriftTests
{
    private static string Tmp(string tag) =>
        Path.Combine(Path.GetTempPath(), $"pb740-{tag}-{Guid.NewGuid():N}.dat");

    private static void TryDelete(string host)
    {
        // ONE artifact per physical file (kb/Work PB802): a keyed file's §9.1.6 fixed file attributes
        // are the store header INSIDE it, so deleting the data file deletes them.
        try { File.Delete(host); } catch (IOException) { }
    }

    /// <summary>The four spellings a connector's §9.1.15 sharing mode can have at the registry. The first is the
    /// implementor default of the open mode (<c>FileRegistry.ImplementorDefaultSharing</c>, kb/Work PB322)
    /// reached the way a program reaches it — by writing no clause at all.</summary>
    public enum Mode { ClauseLess, NoOther, ReadOnly, AllOther }

    /// <summary>⛔ THE AXIS THE FILE-LOCK MEASUREMENT WAS MISSING (kb/Work PB771). §9.1.6: <i>"There are three
    /// organizations: sequential, relative, and indexed"</i>, and §9.1.15 names none of them — <i>"The
    /// successful opening of a file establishes a file lock for the applicable sharing rules"</i> is owed by
    /// every organization there is. RELATIVE and INDEXED owed it and paid nothing: they held no long-lived host
    /// handle at all between OPEN and CLOSE, so a <c>SHARING WITH NO OTHER</c> keyed file was open to any other
    /// run unit and the CLOSE then rewrote it whole from a snapshot taken at the OPEN.</summary>
    public enum Org { Sequential, Relative, Indexed }

    /// <summary>Register one connector of <paramref name="org"/> on <paramref name="host"/>. Every connector on
    /// one physical file declares the SAME attributes, because §14.9.27.4 GR10 compares them at each OPEN.</summary>
    private static void RegisterOrg(FileRegistry reg, string name, string host, Org org)
    {
        switch (org)
        {
            case Org.Relative:
                reg.RegisterRelative(name, host, recordWidth: 4, optional: false, accessMode: 0,
                    relativeKeyDigits: 4, varyMin: -1, varyMax: -1);
                break;
            case Org.Indexed:
                reg.RegisterIndexed(name, host, recordWidth: 4, optional: false, accessMode: 0,
                    primeOffset: 0, primeLength: 4, varyMin: -1, varyMax: -1);
                break;
            default:
                reg.Register(name, host, recordWidth: 4, lineSequential: false, optional: false,
                    varyMin: -1, varyMax: -1);
                break;
        }
    }

    /// <summary>Seed one record through a connector of <paramref name="org"/> and close — the physical file the
    /// measurement then opens exists and holds a record in that organization's own format.</summary>
    private static void SeedOne(FileRegistry reg, string host, Org org)
    {
        RegisterOrg(reg, "S", host, org);
        reg.OpenStatic("S", FileOpenMode.Output);
        reg.WriteShared("S", "SEED", -1, FileRecordLock.None, FileRetryKind.None, 0, page: null);
        reg.Close("S");
    }

    /// <summary>The sharing mode a connector declared with spelling <paramref name="m"/> holds while open in
    /// <paramref name="open"/>: the declared mode, or — for the clause-less spelling — the implementor default
    /// of that open mode.</summary>
    private static FileSharing SharingOf(Mode m, FileOpenMode open) => m switch
    {
        Mode.NoOther => FileSharing.NoOther,
        Mode.ReadOnly => FileSharing.ReadOnly,
        Mode.AllOther => FileSharing.AllOther,
        _ => FileRegistry.ImplementorDefaultSharing(open),
    };

    public static TheoryData<Mode, FileOpenMode, Mode, FileOpenMode, Org> EveryPair()
    {
        var data = new TheoryData<Mode, FileOpenMode, Mode, FileOpenMode, Org>();
        foreach (var org in Enum.GetValues<Org>())
            foreach (var sa in Enum.GetValues<Mode>())
                foreach (var ma in Enum.GetValues<FileOpenMode>())
                    foreach (var sb in Enum.GetValues<Mode>())
                        foreach (var mb in Enum.GetValues<FileOpenMode>())
                            data.Add(sa, ma, sb, mb, org);
        return data;
    }

    private static void RegisterAndShare(FileRegistry reg, string name, string host, Mode m,
        Org org = Org.Sequential)
    {
        RegisterOrg(reg, name, host, org);
        // ClauseLess registers NOTHING: a SELECT with neither clause never reaches RegisterSharing, which is
        // exactly the shape PB740 measured. The other three record their declared sharing mode.
        if (m != Mode.ClauseLess) reg.RegisterSharing(name, SharingOf(m, FileOpenMode.Input), FileLockMode.Manual, multiple: false);
    }

    // ── The agreement, measured on real handles ─────────────────────────────────────────────────────────────

    /// <summary>⛔ THE REGRESSION, AS A THEOREM RATHER THAN A CASE. For EVERY (sharing mode × open mode) on the
    /// existing side crossed with every one on the incoming side, two connectors open one physical file through
    /// a real registry and the outcome shall be the arbiter's: a pair <c>Conflicts</c> permits gets a
    /// success-family status (§9.1.13.2), and a pair it refuses gets '61' (§9.1.13.9 item 1). '30' — the host
    /// refusing what the standard allowed — cannot appear anywhere in the matrix.</summary>
    [Theory]
    [MemberData(nameof(EveryPair))]
    public void TheHostHandleNeverVetoesWhatTable19Permits(Mode sa, FileOpenMode ma, Mode sb, FileOpenMode mb,
        Org org)
    {
        string host = Tmp($"{org}-{sa}{ma}-{sb}{mb}");
        try
        {
            var reg = new FileRegistry();
            SeedOne(reg, host, org);

            RegisterAndShare(reg, "A", host, sa, org);
            RegisterAndShare(reg, "B", host, sb, org);

            reg.OpenStatic("A", ma);
            Assert.True(reg.Status("A")[0] == '0',
                $"The FIRST open, alone on the physical file, answered '{reg.Status("A")}' — the matrix measures "
                + "the SECOND open and cannot say anything if the first one did not happen.");

            reg.OpenStatic("B", mb);
            bool refused = Table19.Conflicts((SharingOf(sb, mb), mb), (SharingOf(sa, ma), ma));
            string got = reg.Status("B");

            if (refused)
                Assert.True(got == FileStatusCode.FileSharingConflict,
                    $"Table 19 refuses ({org}: {sa} {ma}) × ({sb} {mb}), so §9.1.13.9 item 1's '61' is the only "
                    + $"answer; got '{got}'.");
            else
                Assert.True(got[0] == '0',
                    $"Table 19 PERMITS ({org}: {sa} {ma}) × ({sb} {mb}) — §9.1.15 puts the gate on the file "
                    + "connectors and Table 19, not on the operating environment's handle — but the OPEN answered "
                    + $"'{got}'. '30' here is the host vetoing an open the standard allowed (kb/Work PB740), and "
                    + "since kb/Work PB771 the keyed organizations hold a handle that can do it too.");

            reg.CloseAll();
        }
        finally { TryDelete(host); }
    }

    /// <summary>The pair PB740 reported, end to end and with its DATA: two connectors on one ASSIGN declaring
    /// <c>SHARING WITH ALL OTHER</c> (two clause-less connectors are NOT a Table 19 pair since kb/Work PB322:
    /// the INPUT one is READ ONLY and refuses the EXTEND), <c>OPEN INPUT</c> then <c>OPEN EXTEND</c>, the
    /// appended record reaching the physical file (§14.9.51.4
    /// GR12 <i>"releases a logical record to the operating environment"</i>, GR18 the successor relationship)
    /// — and the READER's file position indicator surviving the widening its sibling forced, which is the half
    /// a status-only assertion cannot see: the reposture rebuilds the handle, and a rebuild at the wrong offset
    /// re-serves a record the program has already read (§9.1.12).</summary>
    [Fact]
    public void AllOtherInputThenExtend_Appends_AndTheReaderKeepsItsPosition()
    {
        string host = Tmp("input-extend");
        try
        {
            var reg = new FileRegistry();
            reg.Register("S", host, 4, false, false, -1, -1);
            reg.OpenStatic("S", FileOpenMode.Output);
            reg.Write("S", "AAAA", -1, page: null);
            reg.Write("S", "BBBB", -1, page: null);
            reg.Close("S");

            reg.Register("A", host, 4, false, false, -1, -1);
            reg.Register("B", host, 4, false, false, -1, -1);
            reg.RegisterSharing("A", FileSharing.AllOther, FileLockMode.Manual, multiple: false);
            reg.RegisterSharing("B", FileSharing.AllOther, FileLockMode.Manual, multiple: false);

            reg.OpenStatic("A", FileOpenMode.Input);
            Assert.Equal(FileStatusCode.Success, reg.Status("A"));
            Assert.Equal(FileStatusCode.Success,
                reg.ReadShared("A", false, FileRecordLock.None, false, false, FileRetryKind.None, 0, out string r1));
            Assert.Equal("AAAA", r1);

            // The OPEN that used to answer '30'. It widens A's handle, which is A's handle being REBUILT.
            reg.OpenStatic("B", FileOpenMode.Extend);
            Assert.Equal(FileStatusCode.Success, reg.Status("B"));
            reg.Write("B", "CCCC", -1, page: null);
            Assert.Equal(FileStatusCode.Success, reg.Status("B"));

            // A's file position indicator is where its own READ left it — not rewound by the rebuild.
            Assert.Equal(FileStatusCode.Success,
                reg.ReadShared("A", false, FileRecordLock.None, false, false, FileRetryKind.None, 0, out string r2));
            Assert.Equal("BBBB", r2);

            reg.CloseAll();
            Assert.Equal("AAAABBBBCCCC", File.ReadAllText(host));
        }
        finally { TryDelete(host); }
    }

    // ── The file lock against everything that is not this run unit ──────────────────────────────────────────

    /// <summary>⛔ THE MIRROR, and it needs no second process: a host share mode names no requester, so a handle
    /// opened by this test is exactly as foreign to the connector's handle as one opened by another program.
    /// §9.1.15 1) — <i>"The sharing with no other mode specifies exclusive access to a physical file"</i> — with
    /// two control arms, because the defect was a RELATIVE inversion and an absolute assertion about one mode
    /// cannot see one: the clause-less arm is the posture PB740 left untouched (an open owner question), and
    /// the ALL OTHER arm is §9.1.15 3)'s <i>"allows concurrent access"</i>. Before the fix the three arms read
    /// (refused, refused, refused) with NO OTHER the most permissive of them at the OS.
    /// <para>⚠ THE ARMS ARE THE STANDARD'S; WHAT THE HOST CAN MAKE OF THEM IS MEASURED (kb/Work PB795). The
    /// clause-less arm asserted that a <see cref="FileShare.Read"/> file lock refuses an outside WRITER, and on
    /// a host whose sharing is one advisory lock with two states that refusal cannot exist —
    /// <see cref="ExpectedOutsideWriter"/> is where the translation happens, and it degrades exactly one rule
    /// on exactly the hosts that measurably cannot express it.</para></summary>
    /// <summary>The three §9.1.15 sharing modes whose file lock is observable from outside, crossed with every
    /// §9.1.6 organization — because §9.1.15 names no organization and the rule was implemented in one
    /// (kb/Work PB771).</summary>
    public static TheoryData<Mode, bool, bool, Org> EveryModeAndOrganization()
    {
        var data = new TheoryData<Mode, bool, bool, Org>();
        foreach (var org in Enum.GetValues<Org>())
        {
            data.Add(Mode.NoOther, false, false, org);     // §9.1.15 1) exclusive — nothing else may read or write
            data.Add(Mode.ClauseLess, true, false, org);   // OPEN INPUT's default: SHARING WITH READ ONLY (§9.1.15 2))
            data.Add(Mode.AllOther, true, true, org);      // §9.1.15 3) concurrent access through other connectors
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(EveryModeAndOrganization))]
    public void TheFileLockIsTheSharingMode_MeasuredFromOutsideTheConnector(
        Mode mode, bool outsiderMayRead, bool outsiderMayWrite, Org org)
    {
        string host = Tmp($"lock-{org}-{mode}");
        try
        {
            var reg = new FileRegistry();
            SeedOne(reg, host, org);

            RegisterAndShare(reg, "F", host, mode, org);
            reg.OpenStatic("F", FileOpenMode.Input);
            Assert.Equal(FileStatusCode.Success, reg.Status("F"));

            (bool expectWrite, string why) = ExpectedOutsideWriter(mode, outsiderMayWrite);
            Assert.Equal(outsiderMayRead, HostCapability.OutsiderCan(host, FileAccess.Read));
            Assert.True(expectWrite == HostCapability.OutsiderCan(host, FileAccess.Write),
                $"({org}, {mode}) an outside writer: expected {expectWrite}. {why}");

            // §9.1.15: "The file lock is removed by an explicit or implicit CLOSE statement executed for that
            // file connector" — so after the CLOSE every arm reads the same.
            reg.Close("F");
            Assert.True(HostCapability.OutsiderCan(host, FileAccess.Write));
        }
        finally { TryDelete(host); }
    }

    /// <summary>⛔ ISO §9.1.15'S ANSWER, TRANSLATED THROUGH WHAT THIS HOST'S SHARE MODES CAN ACTUALLY EXPRESS —
    /// and the translation is a MEASUREMENT (<see cref="HostCapability.Sharing"/>), never an
    /// <c>OSPlatform</c> literal, so a host that gains or loses the ability moves this and turns
    /// <see cref="TheHostsShareModeSemanticsAreTheDocumentedOnes"/> red rather than silently re-classifying
    /// every arm (kb/Work PB795).
    /// <para><b>The determination, and it belongs to the RUNTIME, not to this test</b>
    /// (<c>DESIGN-runtime-library.md</c>). .NET implements <see cref="FileShare"/> on Unix with an advisory
    /// <c>flock</c> that has two states — <see cref="FileShare.None"/> takes <c>LOCK_EX</c> and every other
    /// value takes <c>LOCK_SH</c> — so §9.1.15 2)'s <i>"restricts concurrent access … to input mode"</i> has no
    /// expression there at all. The posture is NOT changed to compensate: mapping it onto
    /// <see cref="FileShare.None"/> would refuse the reader rule 2 explicitly ADMITS, trading an under-refusal
    /// for an over-refusal of access the standard grants. So on such a host rule 2's file lock degrades toward
    /// rule 3's <i>"allows concurrent access"</i> and never toward rule 1's exclusivity — a user on Linux gets
    /// weaker protection from other run units than on Windows, and this is where that is written down instead
    /// of being a red test nobody could explain.</para>
    /// <para>Rule 1 survives everywhere, because <c>LOCK_EX</c> is exactly the one thing a binary advisory lock
    /// CAN say — subject to it being advisory, which binds only processes that also take the lock.</para></summary>
    private static (bool Expected, string Why) ExpectedOutsideWriter(Mode mode, bool isoAnswer)
    {
        var host = HostCapability.Sharing;
        if (host.SeparatesReadersFromWriters)
            return (isoAnswer,
                "This host's share modes are access-discriminating, so §9.1.15 is enforced as written. "
                + host.Because);
        if (FileLockPosture.OfSharingMode(SharingOf(mode, FileOpenMode.Input)) == FileShare.None)
            return (isoAnswer,
                "§9.1.15 1)'s exclusive access is the one rule a binary advisory lock can still express, so "
                + "this arm is unchanged on this host. " + host.Because);
        return (true,
            "⚠ MEASURED: this host's FileShare cannot express a share mode that "
            + "admits a reader and refuses a writer, so §9.1.15 2)'s file lock degrades toward rule 3's "
            + "concurrent access rather than toward rule 1's exclusivity (widening it to FileShare.None would "
            + "refuse the reader rule 2 admits). ⛔ THIS ARM PINS A DEFECT, NOT A DETERMINATION — fcntl region "
            + "locks are advisory in the same way and are PER-ACCESS, so rule 2 IS expressible on this host and "
            + "kb/Work PB833 owns closing it; GR-9.1.15-2 stays PARTIAL until then. DESIGN-runtime-library.md "
            + "carries it; kb/Work PB795 is why it is written down rather than a red nobody could explain. " + host.Because);
    }

    /// <summary>⛔ THE HOST CAPABILITY EVERY ARM ABOVE LEANS ON, ASSERTED RATHER THAN ASSUMED (kb/Work PB795).
    /// Three of these are properties EVERY host this compiler supports must have, and the fourth is the
    /// documented split between them. Without this test the translation in
    /// <see cref="ExpectedOutsideWriter"/> could quietly answer "this host enforces nothing" and every arm
    /// above would pass by expecting nothing — the exact shape of a green test holding a gap open.</summary>
    [Fact]
    public void TheHostsShareModeSemanticsAreTheDocumentedOnes()
    {
        var host = HostCapability.Sharing;
        Assert.True(host.EnforcesExclusiveAccess,
            "§9.1.15 1) — a FileShare.None handle shall exclude an outside reader AND an outside writer. This "
            + "host excludes neither or only one, so nothing above can measure a file lock at all. " + host.Because);
        Assert.True(host.ReadShareAdmitsAnOutsideReader,
            "§9.1.15 2) restricts other access *to input mode*; it does not exclude it. " + host.Because);
        Assert.True(host.ReadWriteShareAdmitsAnOutsideWriter,
            "§9.1.15 3) — *allows concurrent access*. " + host.Because);
        Assert.True(OperatingSystem.IsWindows() == host.SeparatesReadersFromWriters,
            "THE DOCUMENTED DETERMINATION (DESIGN-runtime-library.md, kb/Work PB795): Windows share modes are "
            + "mandatory and per-access, so §9.1.15 2) is enforced as written there; every other host reaches "
            + "FileShare through .NET's advisory flock, which has one shared state for every mode but None and "
            + "therefore cannot express rule 2. If this assertion moved, the runtime's own guarantee moved with "
            + "it and the design doc has to be re-derived — never this line adjusted. " + host.Because);
    }

    /// <summary>A clause-less OPEN that Table 19 REFUSES leaves the connectors already open exactly as they
    /// were (§14.9.27.4 GR25 — <i>"the file is not affected"</i>), and one that it admits adds nothing an outsider
    /// could use. A clause-less reader (SHARING WITH READ ONLY) alone denies an outside writer where the host can
    /// express that; a second clause-less reader joins it without changing that; a clause-less appender is
    /// refused '61' and the reader's file lock is still exactly what the reader alone establishes; when the
    /// second reader closes the set shrinks back.</summary>
    [Fact]
    public void ARefusedOrAdmittedClauseLessOpen_DoesNotMoveTheFileLockAnOutsiderSees()
    {
        string host = Tmp("bounded");
        try
        {
            var reg = new FileRegistry();
            reg.Register("S", host, 4, false, false, -1, -1);
            reg.OpenStatic("S", FileOpenMode.Output);
            reg.Write("S", "SEED", -1, page: null);
            reg.Close("S");

            reg.Register("A", host, 4, false, false, -1, -1);
            reg.Register("B", host, 4, false, false, -1, -1);
            reg.Register("C", host, 4, false, false, -1, -1);

            reg.OpenStatic("A", FileOpenMode.Input);
            (bool alone, string why) = ExpectedOutsideWriter(Mode.ClauseLess, isoAnswer: false);
            Assert.True(alone == HostCapability.OutsiderCan(host, FileAccess.Write),
                $"A clause-less reader alone: an outside writer, expected {alone}. {why}");

            reg.OpenStatic("B", FileOpenMode.Input);
            Assert.Equal(FileStatusCode.Success, reg.Status("B"));
            Assert.True(alone == HostCapability.OutsiderCan(host, FileAccess.Write),
                "A second clause-less reader (Table 19: READ ONLY input against READ ONLY input is a normal open) "
                + "shall admit an outsider nothing the first did not. " + why);

            reg.OpenStatic("C", FileOpenMode.Extend);
            Assert.Equal(FileStatusCode.FileSharingConflict, reg.Status("C"));   // the default of EXTEND is NO OTHER
            Assert.True(alone == HostCapability.OutsiderCan(host, FileAccess.Write),
                "⛔ THE REFUSED OPEN IS NOT AN OPEN (§14.9.27.4 GR25): the readers' file lock shall be exactly what "
                + "it was before the statement ran. " + why);
            Assert.True(HostCapability.OutsiderCan(host, FileAccess.Read), "Neither posture denies a reader.");

            reg.Close("B");
            Assert.True(alone == HostCapability.OutsiderCan(host, FileAccess.Write),
                "The set shrank back to one clause-less reader, so its file lock shall be exactly what that "
                + "connector alone establishes. " + why);
            reg.CloseAll();
        }
        finally { TryDelete(host); }
    }

    /// <summary>The OTHER sentence each of §9.1.15's three rules ends with, which the file lock and Table 19
    /// between them do not cover: <i>"Record locks are ignored"</i> (rule 1, exclusive access — there is no
    /// other connector for a lock to exclude), against <i>"Record locks are in effect"</i> (rules 2 and 3).
    /// Measured where a lock is observable at all — through a sibling connector's view of the lock table — so
    /// the three rules are asserted whole rather than in the half the OS handle happens to show.</summary>
    [Theory]
    [InlineData(Mode.NoOther, false)]    // §9.1.15 1) "Record locks are ignored."
    [InlineData(Mode.ReadOnly, true)]    // §9.1.15 2) "Record locks are in effect."
    [InlineData(Mode.AllOther, true)]    // §9.1.15 3) "Record locks are in effect."
    public void TheSharingModeAlsoDecidesWhetherRecordLocksAreInEffect(Mode mode, bool locksInEffect)
    {
        string host = Tmp($"locks-{mode}");
        try
        {
            var reg = new FileRegistry();
            reg.Register("S", host, 4, false, false, -1, -1);
            reg.OpenStatic("S", FileOpenMode.Output);
            reg.Write("S", "SEED", -1, page: null);
            reg.Close("S");

            reg.Register("F", host, 4, false, false, -1, -1);
            reg.RegisterSharing("F", SharingOf(mode, FileOpenMode.Input), FileLockMode.Manual, multiple: false);
            reg.Register("Q", host, 4, false, false, -1, -1);   // the observer; never opened, so Table 19 is silent

            reg.OpenStatic("F", FileOpenMode.Input);
            Assert.Equal(FileStatusCode.Success, reg.Status("F"));
            Assert.Equal(FileStatusCode.Success,
                reg.ReadShared("F", false, FileRecordLock.WithLock, false, false, FileRetryKind.None, 0, out _));

            Assert.Equal(locksInEffect, reg.IsLockedByOther("Q", "1"));
            reg.Close("F");
        }
        finally { TryDelete(host); }
    }

    // ── The file lock is owed by EVERY organization, and the handle is what pays it (kb/Work PB771) ─────────

    /// <summary>⛔ THE DELIVERABLE INVARIANT: <b>no organization completes an OPEN without a live host handle
    /// carrying its derived posture</b> — measured THROUGH the connectors on a real physical file, never
    /// asserted about them. §9.1.15 3): <i>"The successful opening of a file establishes a file lock for the
    /// applicable sharing rules, thereby preventing other run units from opening that file with incompatible
    /// sharing rules"</i>, and it names no organization, so all three owe it.
    /// <para>The measurement is <see cref="HostCapability.OutsiderCan"/> with the connector open in
    /// <c>SHARING WITH NO OTHER</c>: §9.1.15 1)'s <i>"exclusive access to a physical file"</i> is the one rule
    /// every host this compiler supports can express (<see cref="TheHostsShareModeSemanticsAreTheDocumentedOnes"/>
    /// measures that), so a live, correctly-postured handle is exactly what an outsider being refused proves —
    /// and its absence is exactly what RELATIVE and INDEXED had. Asserting the FIELD would have proved only that
    /// a field was assigned.</para>
    /// <para>⛔ AND IT IS ASSERTED OVER EVERY OPEN MODE, not over INPUT alone: the keyed connectors take their
    /// handle on six separate arms of two <c>OpenCore</c> switches (§14.9.27.4 GR17's absent-OPTIONAL creation
    /// and GR18's OUTPUT among them), and one arm forgetting it is this defect back in a corner.</para></summary>
    [Theory]
    [MemberData(nameof(EveryOrganizationAndOpenMode))]
    public void EveryOrganizationHoldsALiveFileLockWhileOpen(Org org, FileOpenMode mode)
    {
        string host = Tmp($"live-{org}-{mode}");
        try
        {
            var reg = new FileRegistry();
            SeedOne(reg, host, org);

            RegisterAndShare(reg, "F", host, Mode.NoOther, org);
            reg.OpenStatic("F", mode);
            Assert.True(reg.Status("F")[0] == '0',
                $"({org} {mode}) the OPEN answered '{reg.Status("F")}' — nothing below can measure a file lock "
                + "over an open that did not happen.");

            Assert.False(HostCapability.OutsiderCan(host, FileAccess.Write),
                $"({org} {mode}) §9.1.15 1) — SHARING WITH NO OTHER 'specifies exclusive access to a physical "
                + "file', and another run unit wrote to it while this connector held it open. That is a "
                + "connector completing an OPEN without a live host handle carrying its derived posture "
                + $"(kb/Work PB771). {HostCapability.Sharing.Because}");
            Assert.False(HostCapability.OutsiderCan(host, FileAccess.Read),
                $"({org} {mode}) exclusive access excludes a READER too — §9.1.15 1) admits no other connector "
                + $"in any mode. {HostCapability.Sharing.Because}");

            // §9.1.15: "The file lock is removed by an explicit or implicit CLOSE statement executed for that
            // file connector" — the give-back, for every organization, so the lock is not merely taken.
            reg.Close("F");
            Assert.True(reg.Status("F")[0] == '0', $"({org} {mode}) CLOSE answered '{reg.Status("F")}'.");
            Assert.True(HostCapability.OutsiderCan(host, FileAccess.Write),
                $"({org} {mode}) the file lock outlived the CLOSE.");
        }
        finally { TryDelete(host); }
    }

    public static TheoryData<Org, FileOpenMode> EveryOrganizationAndOpenMode()
    {
        var data = new TheoryData<Org, FileOpenMode>();
        foreach (var org in Enum.GetValues<Org>())
            foreach (var mode in Enum.GetValues<FileOpenMode>())
                data.Add(org, mode);
        return data;
    }

    /// <summary>The ACCESSES a connector's own handle may take of its physical file in <paramref name="mode"/>
    /// — the domain the symbolic theorems below quantify over (kb/Work PB771).
    /// <para>It is a closed BAND rather than a list of the organizations there happen to be, so a fourth one is
    /// covered without editing this. The LOWER bound is the open mode's own floor
    /// (<see cref="FileLockPosture.AccessOf"/>): a handle that cannot carry out its mode's statements is not a
    /// handle for that mode. The UPPER bound adds READ and nothing else — a format that is rewritten whole has
    /// to read what is there before it writes it back, but ⛔ NO MODE MAY ADD <b>WRITE</b>, because
    /// §14.9.27.4 GR16 and §9.1.13.6 item 6 a) ask write capability of every mode BUT input (item 6 a) 3. is
    /// the input mode's own question and it is about READ operations): a connector that asked the host for
    /// write access on OPEN INPUT would be refused by a read-only file the standard says it may read — which
    /// is exactly why <c>FileConnector.Open</c> excludes INPUT from its own <c>PermitsWrite</c> probe.
    /// <see cref="EveryOrganisationsHandleAccessLiesInThatBand"/> measures that both bounds hold of every
    /// organization that actually exists.</para></summary>
    private static IEnumerable<FileAccess> AccessesAHandleMayTake(FileOpenMode mode)
    {
        var floor = FileLockPosture.AccessOf(mode);
        yield return floor;
        if (floor != (floor | FileAccess.Read)) yield return floor | FileAccess.Read;
    }

    /// <summary>⛔ THE PREMISE OF <see cref="AccessesAHandleMayTake"/>, MEASURED — and the drift guard that makes
    /// the superset honest. Every connector this runtime has answers <c>HostAccess</c> inside the band, and the
    /// count of concrete organizations is pinned: a fourth one lands here, red, instead of silently falling
    /// outside a theorem that was proved without it.</summary>
    [Fact]
    public void EveryOrganisationsHandleAccessLiesInThatBand()
    {
        string host = Tmp("band");
        try
        {
            var reg = new FileRegistry();
            foreach (var org in Enum.GetValues<Org>())
            {
                RegisterOrg(reg, $"C{org}", host, org);
                var c = typeof(FileRegistry)
                    .GetMethod("Require", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(reg, [$"C{org}"]) as FileConnector;
                Assert.NotNull(c);
                foreach (var mode in Enum.GetValues<FileOpenMode>())
                {
                    var taken = c.HostAccess(mode);
                    var floor = FileLockPosture.AccessOf(mode);
                    Assert.True((taken & floor) == floor,
                        $"{org} takes {taken} in {mode}, which does not include the mode's own floor {floor} — "
                        + "a handle that cannot carry out its open mode's statements.");
                    Assert.True((taken & ~(floor | FileAccess.Read)) == 0,
                        $"{org} takes {taken} in {mode}, outside the band [{floor}, {floor | FileAccess.Read}] "
                        + "the theorems quantify over — adding WRITE to a mode that does not need it would have "
                        + "the host refuse an OPEN §14.9.27.4 GR16 allows (see AccessesAHandleMayTake).");
                }
            }

            int concrete = typeof(FileConnector).Assembly.GetTypes()
                .Count(t => !t.IsAbstract && typeof(FileConnector).IsAssignableFrom(t));
            Assert.True(concrete == Enum.GetValues<Org>().Length,
                $"{concrete} concrete FileConnector organizations exist but this class's Org enum names "
                + $"{Enum.GetValues<Org>().Length}. A new organization owes §9.1.15's file lock exactly as the "
                + "other three do (kb/Work PB771) — add it to Org, to RegisterOrg and to SeedOne, and every "
                + "measurement in this class covers it.");
        }
        finally { TryDelete(host); }
    }

    // ── The structural half ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>⛔ THE POSTURE IS DERIVED IN ONE PLACE. Every <c>FileShare</c> value under
    /// <c>Cobol.Net.Runtime/IO</c> lives in <c>FileLockPosture</c> (which derives it) or <c>FileSupport</c>
    /// (whose bookkeeping role has a fixed one) — anywhere else is a call site deciding §9.1.15 for itself,
    /// which is the shape that produced PB740's two opposite wrong answers from one boolean.</summary>
    [Fact]
    public void NoOtherSiteUnderRuntimeIoNamesAShareMode()
    {
        string io = TestRepo.Src("Cobol.Net.Runtime", "IO");
        Assert.True(Directory.Exists(io), $"The IO subsystem moved: {io} is not a directory.");

        var offenders = new List<string>();
        foreach (string file in Directory.EnumerateFiles(io, "*.cs", SearchOption.AllDirectories))
        {
            string name = Path.GetFileName(file);
            if (name is "FileLockPosture.cs" or "FileSupport.cs") continue;
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                string t = lines[i].TrimStart();
                if (t.StartsWith("//", StringComparison.Ordinal) || t.StartsWith("///", StringComparison.Ordinal))
                    continue;
                if (lines[i].Contains("FileShare.", StringComparison.Ordinal))
                    offenders.Add($"{Path.GetRelativePath(io, file)}:{i + 1}: {t}");
            }
        }

        Assert.True(offenders.Count == 0,
            "A FileShare value outside FileLockPosture. §9.1.15's file lock is a DERIVATION — the arbitrated "
            + "sharing mode, widened by the connectors Table 19 has admitted — and a site that names a share "
            + "mode has answered it locally. Sites:\n  " + string.Join("\n  ", offenders));
    }

    /// <summary>The positive complement, for the reason a ban over a subsystem that names no share mode passes
    /// exactly as green: the derivation still lives in its named home, still answers all three standard modes
    /// (and no fourth, undetermined, one), and the registry still applies it in one place.</summary>
    [Fact]
    public void TheDerivationStillLivesInItsNamedHome()
    {
        string text = File.ReadAllText(TestRepo.Src("Cobol.Net.Runtime", "IO", "Sharing", "FileLockPosture.cs"));
        Assert.Contains("public static FileShare OfSharingMode(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("public static FileShare For(", text, StringComparison.Ordinal);   // no widening
        Assert.DoesNotContain("null =>", text, StringComparison.Ordinal);
        foreach (string arm in new[] { "FileSharing.NoOther => FileShare.None", "FileSharing.ReadOnly => FileShare.Read",
                                       "FileSharing.AllOther => FileShare.ReadWrite" })
            Assert.Contains(arm, text, StringComparison.Ordinal);

        string registry = TestRepo.Src("Cobol.Net.Runtime", "IO", "FileRegistry.cs");
        var writers = File.ReadAllLines(registry)
            .Where(l => l.Contains("HostShare =", StringComparison.Ordinal)
                        && !l.TrimStart().StartsWith("//", StringComparison.Ordinal)
                        && !l.TrimStart().StartsWith("///", StringComparison.Ordinal))
            .ToList();
        Assert.True(writers.Count == 1,
            $"The §9.1.15 file lock must be applied in exactly one place; found {writers.Count}:\n  "
            + string.Join("\n  ", writers));
    }

    /// <summary>The host's rule is symmetric — a new open succeeds only if its access is admitted by every
    /// outstanding handle's share mode AND its own share mode admits every outstanding handle's access — so this
    /// is the one conversion the theorem below needs: the share flags a handle shall carry to ADMIT an access.</summary>
    private static FileShare Admitting(FileAccess access) =>
        ((access & FileAccess.Read) != 0 ? FileShare.Read : FileShare.None)
        | ((access & FileAccess.Write) != 0 ? FileShare.Write : FileShare.None);

    /// <summary>⛔ THE THEOREM BEHIND THE MEASURED MATRIX, checked symbolically over all 35 printed cells and over
    /// every access a handle may take (<see cref="AccessesAHandleMayTake"/>): wherever Table 19 prints
    /// <i>Normal open</i>, the two connectors' BASE postures (<see cref="FileLockPosture.OfSharingMode"/>, the
    /// sharing mode's own file lock and nothing more) are mutually admissible at the host — the incoming access
    /// allowed by the existing share mode, and the incoming share mode allowing the existing access. That is the
    /// property that makes "the arbiter decides, the handle obeys" true by construction: no handle is ever
    /// rebuilt to admit a sibling, because none needs widening.
    /// <para>It is a theorem about THREE sharing modes, which is why it can be stated at all since kb/Work PB322:
    /// every connector holds one of them (<see cref="FileRegistry.ImplementorDefaultSharing"/> gives the
    /// clause-less ones theirs from the open mode). While the default was <i>undetermined</i> the property
    /// failed for it — a clause-less reader (<see cref="FileShare.Read"/>) had to be widened to admit a sibling
    /// appender the arbiter admitted — and a widening and handle-rebuild machinery existed to compensate.</para></summary>
    [Fact]
    public void EveryNormalOpenCellHasMutuallyAdmissiblePostures()
    {
        foreach (var exS in Table19.StandardModes)
            foreach (var exM in Enum.GetValues<FileOpenMode>())
                foreach (var incS in Table19.StandardModes)
                    foreach (var incM in Enum.GetValues<FileOpenMode>())
                    {
                        if (Table19.Cell(incS, incM, exS, exM) != OpenSharingOutcome.NormalOpen) continue;
                        var exShare = FileLockPosture.OfSharingMode(exS);
                        var incShare = FileLockPosture.OfSharingMode(incS);
                        foreach (var exAccess in AccessesAHandleMayTake(exM))
                            foreach (var incAccess in AccessesAHandleMayTake(incM))
                            {
                                Assert.True(exShare.HasFlag(Admitting(incAccess)),
                                    $"Table 19 permits ({exS} {exM}) + ({incS} {incM}) but the existing connector's "
                                    + $"file lock {exShare} does not admit {incAccess} — which is what a whole-store "
                                    + "organization's handle takes in every writable mode (kb/Work PB771).");
                                Assert.True(incShare.HasFlag(Admitting(exAccess)),
                                    $"Table 19 permits ({exS} {exM}) + ({incS} {incM}) but the incoming connector's "
                                    + $"file lock {incShare} does not admit {exAccess}.");
                            }
                    }
    }
}
