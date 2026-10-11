// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics;
using System.Runtime.InteropServices;
using CobolNet.Runtime.IO;
using CobolNet.Tests.Shared;
using Xunit;
using Xunit.Abstractions;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ISO §14.9.27.4 GR3 — <i>"If the file associated with file-name-1 is present and insufficient authority
/// exists to open the file, the execution of the OPEN statement is unsuccessful, and the I-O status value in
/// the file connector referenced by file-name-1 is set to '37'."</i> (§9.1.13.6 item 6 b) restates it for OPEN
/// and DELETE FILE together and adds <i>"The ability to detect this is processor dependent"</i>.)
/// <para>These are the arms no conformance golden can reach: the corpus runner compiles a <c>.cob</c> and
/// compares its output, with nowhere to express "and first make this file unreadable", so the ONLY place the
/// authority precondition can be established is a test that builds it. kb/Work PB323 is what the gap cost —
/// every <c>OpenCore</c> asked <c>File.Exists</c>, which swallows an access error and answers <c>false</c>, so
/// a present file the process may not read was reported as '35' (not present) and, for an OPTIONAL file, as a
/// SUCCESSFUL '05' open over an invented empty file whose first READ hit at end.</para>
/// <para>The denial itself is real, not simulated: a DACL deny ACE on Windows, mode 000 on Unix. The helper
/// PROVES the denial took — with a raw <c>File.OpenRead</c>, never with <see cref="HostFile.Probe"/>, because
/// checking a precondition with the subject under test lets a broken subject certify its own premise.</para>
/// <para>⛔ THE ONE ESCAPE HATCH, AND WHY IT MAY NOT BE A PREDICTION (kb/Work PB795). A process running as root
/// cannot be denied by mode bits at all, so on such a host these assertions have nothing to assert. The hatch
/// therefore has to answer <i>"can THIS host refuse THIS process?"</i>, and for two days it answered from a
/// hard-coded sentence instead — <i>"a merely elevated Windows token does not bypass an explicit deny ACE for
/// its own SID, so Windows has no bypass case here"</i>. On the GitHub Windows runner the precondition was not
/// established and that sentence turned the guard red with <i>"the helper is broken"</i>, about a host the test
/// had never looked at.</para>
/// <para>So every precondition is now CLASSIFIED from measurements — see <see cref="HostCapability"/>. The
/// denial is applied, the host's own tool is asked whether it took, and the access is then really attempted;
/// <i>the tool failed</i>, <i>the host granted it anyway</i> and <i>the host refused it</i> are three different
/// answers with three different consequences. And a test whose precondition was not established no longer
/// returns silently: <see cref="NoPrecondition"/> turns it RED with that evidence unless the bypass is MEASURED
/// (feedback: green_gates_arent_evidence, reachability_is_measured_not_deduced).</para>
/// </summary>
public sealed class FileAuthorityPresenceTests : IDisposable
{
    private const int SeqAccess = 0;   // KeyedAccess.Sequential

    private readonly string _root =
        Path.Combine(Path.GetTempPath(), $"pb323-{Guid.NewGuid():N}");

    /// <summary>Where a measured host fact is recorded even when nothing fails — the classification of a host
    /// that BYPASSES file permissions is the one outcome that cannot be an assertion (there is nothing left to
    /// assert), so it is written into the run's result instead of being dropped on the floor.</summary>
    private readonly ITestOutputHelper _log;

    /// <summary>The classification of the LAST precondition this test attempted to establish — measured, with
    /// the host's own evidence attached (<see cref="HostCapability.Classify"/>).</summary>
    private HostCapability.Denial _denial;

    public FileAuthorityPresenceTests(ITestOutputHelper log)
    {
        _log = log;
        Directory.CreateDirectory(_root);
    }

    /// <summary>⛔ THE PRECONDITION WAS NOT ESTABLISHED, AND THAT IS NOT ALLOWED TO BE SILENT (kb/Work PB795).
    /// <para>Every one of these sites used to be a bare <c>return</c>. A host on which the deny helper failed
    /// therefore turned twenty §14.9.27.4 GR3 assertions into vacuous passes and exactly ONE test red — and
    /// that one test named the helper as the cause from a claim about the platform, not from anything it had
    /// measured. Both halves of that are the defect: the twenty silent passes AND the unmeasured verdict.</para>
    /// <para>A test that could not establish its precondition now goes RED here, carrying the host's own
    /// evidence, unless this process is MEASURED to bypass file permissions on this host — the single case in
    /// which there is genuinely nothing to assert, and which is then recorded rather than hidden.</para></summary>
    private void NoPrecondition()
    {
        _log.WriteLine(_denial.Because);
        Assert.True(_denial.Verdict == HostCapability.DenialVerdict.BypassedByThisIdentity, _denial.Because);
    }

    public void Dispose()
    {
        try
        {
            Restore(_root);
            Directory.Delete(_root, recursive: true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    // ── The presence probe itself (HostFile.Probe) ───────────────────────────────────────────────────────────

    /// <summary>§9.1.13.6 item 5's "the physical file is not present" — the answer that earns '35'.</summary>
    [Fact]
    public void Probe_MissingFile_IsAbsent() =>
        Assert.Equal(FilePresence.Absent, HostFile.Probe(Path.Combine(_root, "nothing.dat")));

    /// <summary>A path the host will not accept as a file name is a statement about the PATH, not an
    /// input-output failure: it names no present file, so it is Absent (§9.1.13.6 item 5) and not §9.1.13.6
    /// item 1's '30'. This is also the answer <c>File.Exists</c> gave, so no OPEN changed behaviour when the
    /// probe replaced it — the pin is here because <c>File.GetAttributes</c>, unlike <c>File.Exists</c>,
    /// THROWS for these and the mapping had to be chosen rather than inherited.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Probe_APathTheHostWillNotAccept_IsAbsent(string path) =>
        Assert.Equal(FilePresence.Absent, HostFile.Probe(path));

    [Fact]
    public void Probe_APathLongerThanTheHostAllows_IsAbsent() =>
        Assert.Equal(FilePresence.Absent,
            HostFile.Probe(Path.Combine(_root, new string('x', 400), new string('y', 400))));

    [Fact]
    public void Probe_ExistingFile_IsPresent()
    {
        string p = Path.Combine(_root, "there.dat");
        File.WriteAllText(p, "X");
        Assert.Equal(FilePresence.Present, HostFile.Probe(p));
    }

    /// <summary>A DIRECTORY at the ASSIGN path is not the §9.1.6 <i>physical file</i>, so "the physical file is
    /// not present" is the true statement about it — the same answer <c>File.Exists</c> gave, which is why the
    /// PB323 refactor left every OPEN of such a path at '35'.</summary>
    [Fact]
    public void Probe_DirectoryAtThePath_IsAbsent()
    {
        string p = Path.Combine(_root, "adir");
        Directory.CreateDirectory(p);
        Assert.Equal(FilePresence.Absent, HostFile.Probe(p));
    }

    /// <summary>⛔ THE WHOLE DEFECT IN ONE ASSERTION. <c>File.Exists</c> answers <c>false</c> here — asserted
    /// alongside, so the test states what it is discriminating against and cannot quietly stop discriminating.
    /// A refusal is not evidence of absence.</summary>
    [Fact]
    public void Probe_PresentButRefused_IsUnauthorized_WhereFileExistsSaysAbsent()
    {
        if (Refused() is not { } witness) { NoPrecondition(); return; }
        Assert.False(File.Exists(witness));                                  // the two-valued probe's lie
        Assert.Equal(FilePresence.Unauthorized, HostFile.Probe(witness));    // the three-valued truth
    }

    /// <summary>The escape hatch, made visible AND made a measurement. This test is red for exactly one
    /// reason — the denial could not be APPLIED, which is the helper being broken and nothing else — and its
    /// message then carries the host tool's own exit codes and output. A host that applied the denial and
    /// granted the access anyway is a statement about the HOST: the classification names the token identity the
    /// access was granted to and is written into the run's output, and every GR3 test in this class states the
    /// same thing for itself through <see cref="NoPrecondition"/> rather than passing vacuously.
    /// <para>⚠ It can no longer be red merely because a host defeated one particular denial mechanism: that was
    /// kb/Work PB795, where the GR3 witness was refused only as a SIDE EFFECT of
    /// <c>icacls /inheritance:r</c> emptying the child's DACL — measured on 2026-09-06, a directory-scoped deny
    /// ACE alone leaves <c>File.OpenRead</c> of the child SUCCEEDING, because opening a child by full path
    /// consults the parent for FILE_TRAVERSE and "Bypass traverse checking" is granted to Everyone by
    /// default.</para></summary>
    [Fact]
    public void TheDenyHelperWorksOnThisHost()
    {
        Refused();
        _log.WriteLine(_denial.Because);
        Assert.False(_denial.Verdict == HostCapability.DenialVerdict.ToolFailed, _denial.Because);
    }

    // ── GR3 on the OPEN statement, every organization, OPTIONAL and not ──────────────────────────────────────

    /// <summary>GR3 is not conditioned on the OPTIONAL clause, and that is the silent half of the defect:
    /// GR13's at-end arm opens with <i>"If the file is NOT present"</i>, so an OPTIONAL file that IS present
    /// and merely refused must never take it. Before PB323 the non-optional legs answered '35' and the OPTIONAL
    /// legs answered a SUCCESSFUL '05'.</summary>
    [Theory]
    [InlineData("seq", false)]
    [InlineData("seq", true)]
    [InlineData("rel", false)]
    [InlineData("rel", true)]
    [InlineData("idx", false)]
    [InlineData("idx", true)]
    public void OpenInput_PresentButRefused_Is37(string organization, bool optional)
    {
        if (Refused() is not { } witness) { NoPrecondition(); return; }
        var reg = Register(organization, witness, optional);
        reg.OpenStatic("F", FileOpenMode.Input);
        Assert.Equal("37", reg.Status("F"));
    }

    /// <summary>GR17's creation arm carries the SAME "If the file is not present" precondition as GR13, so a
    /// refused I-O or EXTEND target is '37' and is NOT created — an unsuccessful OPEN leaves the file
    /// unaffected (GR25).</summary>
    [Theory]
    [InlineData(FileOpenMode.IO, false)]
    [InlineData(FileOpenMode.IO, true)]
    [InlineData(FileOpenMode.Extend, false)]
    [InlineData(FileOpenMode.Extend, true)]
    public void OpenIoOrExtend_PresentButRefused_Is37(FileOpenMode mode, bool optional)
    {
        if (Refused() is not { } witness) { NoPrecondition(); return; }
        var reg = Register("seq", witness, optional);
        reg.OpenStatic("F", mode);
        Assert.Equal("37", reg.Status("F"));
    }

    /// <summary>⛔ THE OVER-FIRE GUARD, and the reason GR3's short-circuit deliberately excludes OUTPUT.
    /// §14.9.27.4 GR18 makes OUTPUT a CREATION that never consults presence, and §9.1.13.6 item 5's '35' is
    /// defined only <i>"for an OPEN statement with the INPUT, I-O, or EXTEND phrase"</i> — so a directory this
    /// process may WRITE but not LIST legitimately accepts a new file. The probe answers Unauthorized for that
    /// path; a blanket '37' would reject an OPEN OUTPUT the operating environment carries out perfectly.</summary>
    [Fact]
    public void OpenOutput_NewFileUnderARefusedDirectory_StillSucceeds()
    {
        if (Refused() is null) { NoPrecondition(); return; }
        string fresh = Path.Combine(_root, "locked", "brand-new.dat");
        if (!RawCreateSucceeds(fresh)) return;   // a host that refuses creation too has no case to guard here
        var reg = Register("seq", fresh, optional: false);
        reg.OpenStatic("F", FileOpenMode.Output);
        Assert.Equal("00", reg.Status("F"));
        reg.Close("F");
    }

    /// <summary>An OUTPUT over a file that IS present and refused is still '37' — GR3's answer, reached through
    /// the creating stream's own <c>UnauthorizedAccessException</c> and <c>FileConnector.Open</c>'s catch rather
    /// than through the presence short-circuit. Both routes, one status.</summary>
    [Fact]
    public void OpenOutput_OverAPresentRefusedFile_Is37()
    {
        if (Refused() is not { } witness) { NoPrecondition(); return; }
        var reg = Register("seq", witness, optional: false);
        reg.OpenStatic("F", FileOpenMode.Output);
        Assert.Equal("37", reg.Status("F"));
    }

    // ── GR16's write capability: the SAME file, the SAME answer, every organization (kb/Work PB328) ──────────

    /// <summary>⛔ THE NINE CELLS. §14.9.27.4 GR16 — <i>"If the I-O phrase is specified, the file shall support
    /// the input and output statements that are permitted for the organization of that file when opened in the
    /// I-O mode. If the file does not support those statements, the I-O status value for file-name-1 is set to
    /// '37' and the execution of the OPEN statement is unsuccessful."</i> §9.1.13.6 item 6 a) prices the other
    /// two write modes identically: 1. <i>"the EXTEND or OUTPUT phrase is specified but the file will not
    /// support write operations"</i>, 2. the I-O restatement of GR16.
    /// <para>Neither rule names an organization, and Table 20 is why they cannot: REWRITE sits under the I-O
    /// column for sequential, random AND dynamic access, so "supports the statements permitted in the I-O mode"
    /// entails write capability whatever the organization. kb/Work PB328 was the three organizations
    /// disagreeing about that on ONE file — the sequential arm answered '37' because its I-O and EXTEND streams
    /// happen to be write opens, while the relative and indexed arms only read their store and answered '00',
    /// after which READ and REWRITE both reported '00' and the loss surfaced as a '30' at CLOSE on a file that
    /// was byte-for-byte unchanged.</para></summary>
    [Theory]
    [InlineData("seq", FileOpenMode.IO)]
    [InlineData("seq", FileOpenMode.Extend)]
    [InlineData("seq", FileOpenMode.Output)]
    [InlineData("rel", FileOpenMode.IO)]
    [InlineData("rel", FileOpenMode.Extend)]
    [InlineData("rel", FileOpenMode.Output)]
    [InlineData("idx", FileOpenMode.IO)]
    [InlineData("idx", FileOpenMode.Extend)]
    [InlineData("idx", FileOpenMode.Output)]
    public void OpenAWriteMode_PresentButNotWritable_Is37(string organization, FileOpenMode mode)
    {
        if (NotWritable(organization) is not { } witness) { NoPrecondition(); return; }
        var reg = Register(organization, witness, optional: false);
        reg.OpenStatic("F", mode);
        Assert.Equal("37", reg.Status("F"));
    }

    /// <summary>The OPTIONAL clause does not enter GR16 or §9.1.13.6 item 6 a) at all, and that is the silent
    /// half: GR17's creation arm opens with <i>"If the file is NOT present"</i>, so a file that IS present and
    /// merely unwritable must never be treated as one to create.</summary>
    [Theory]
    [InlineData("seq")]
    [InlineData("rel")]
    [InlineData("idx")]
    public void OpenIo_PresentButNotWritable_OptionalFile_IsStill37(string organization)
    {
        if (NotWritable(organization) is not { } witness) { NoPrecondition(); return; }
        var reg = Register(organization, witness, optional: true);
        reg.OpenStatic("F", FileOpenMode.IO);
        Assert.Equal("37", reg.Status("F"));
    }

    /// <summary>§14.9.27.4 GR25 — <i>"If the execution of the OPEN statement is unsuccessful, the file is not
    /// affected"</i>. The capability answer is obtained by really asking the host for a writable handle, so the
    /// probe itself has to be provably inert: <c>FileMode.Open</c> with no bytes written leaves content AND
    /// last-write time exactly as they were.</summary>
    [Fact]
    public void TheRefusedOpenLeavesTheFileUnaffected()
    {
        if (NotWritable("idx") is not { } witness) { NoPrecondition(); return; }
        byte[] before = File.ReadAllBytes(witness);
        DateTime stamp = File.GetLastWriteTimeUtc(witness);
        var reg = Register("idx", witness, optional: false);
        reg.OpenStatic("F", FileOpenMode.IO);
        Assert.Equal("37", reg.Status("F"));
        Assert.Equal(before, File.ReadAllBytes(witness));
        Assert.Equal(stamp, File.GetLastWriteTimeUtc(witness));
    }

    /// <summary>⛔ THE OVER-FIRE GUARD. A read-only file supports every statement Table 20 permits in the INPUT
    /// mode, and §9.1.13.6 item 6 a) 3. asks only whether <i>"the file will not support read operations"</i>.
    /// Probing WRITE capability for an INPUT open would refuse an OPEN the operating environment carries out
    /// perfectly — and reading a write-protected master file is one of the oldest idioms there is.</summary>
    [Theory]
    [InlineData("seq")]
    [InlineData("rel")]
    [InlineData("idx")]
    public void OpenInput_PresentButNotWritable_StillOpens(string organization)
    {
        if (NotWritable(organization) is not { } witness) { NoPrecondition(); return; }
        var reg = Register(organization, witness, optional: false);
        reg.OpenStatic("F", FileOpenMode.Input);
        Assert.Equal("00", reg.Status("F"));
        reg.Close("F");
    }

    /// <summary>The complement that keeps the '37' above from being unconditional: the same organization, the
    /// same mode, a file this process CAN write, still opens normally (Table 18, "file is available").</summary>
    [Theory]
    [InlineData("seq")]
    [InlineData("rel")]
    [InlineData("idx")]
    public void OpenIo_PresentAndWritable_StillOpens(string organization)
    {
        string p = Path.Combine(_root, $"writable-{organization}.dat");
        var make = Register(organization, p, optional: false);
        make.OpenStatic("F", FileOpenMode.Output);
        Assert.Equal("00", make.Status("F"));
        make.Close("F");

        var reg = Register(organization, p, optional: false);
        reg.OpenStatic("F", FileOpenMode.IO);
        Assert.Equal("00", reg.Status("F"));
        reg.Close("F");
    }

    /// <summary>The write-capability probe against the PRESENCE probe, on one file: they are different
    /// questions and the file answers them differently. This is the assertion that states what the fix is
    /// discriminating against — before kb/Work PB328 the keyed OPEN arms had only the presence answer, which
    /// says "Present" here, and read it as permission to proceed.</summary>
    [Fact]
    public void PermitsWrite_SeesWhatThePresenceProbeCannot()
    {
        if (NotWritable() is not { } witness) { NoPrecondition(); return; }
        Assert.Equal(FilePresence.Present, HostFile.Probe(witness));   // present, and observable
        Assert.False(HostFile.PermitsWrite(witness));                  // and still not writable
    }

    [Fact]
    public void PermitsWrite_AnOrdinaryFile_IsTrue()
    {
        string p = Path.Combine(_root, "ordinary.dat");
        File.WriteAllText(p, "HELLO-RECORD-0001");
        Assert.True(HostFile.PermitsWrite(p));
    }

    /// <summary>§9.1.13.6 item 6 a) 3. — <i>"the INPUT phrase is specified but the file will not support read
    /// operations"</i>, the third face of the same '37'. Its answer is NOT the write probe (which excludes
    /// INPUT on purpose) and NOT GR3's presence short-circuit (the file here is fully observable: the
    /// DIRECTORY is readable and only the FILE is refused, so <c>HostFile.Probe</c> says Present). It is the
    /// organization's own eager read — the sequential <c>StreamReader</c>, the keyed <c>Attach()</c> — whose
    /// <c>UnauthorizedAccessException</c> reaches the same '37' through <c>FileConnector.Open</c>'s catch.
    /// <para>⛔ ASSERTED BECAUSE IT WAS ASSUMED. <c>FileConnector.Open</c>'s GR16 comment justifies excluding
    /// INPUT from the write probe by claiming item 6 a) 3. is already answered; an exclusion resting on an
    /// unmeasured claim is how a gap ships. All three organizations were measured before the claim was
    /// written (feedback: reachability_is_measured_not_deduced).</para></summary>
    [Theory]
    [InlineData("seq")]
    [InlineData("rel")]
    [InlineData("idx")]
    public void OpenInput_PresentAndObservableButNotReadable_Is37(string organization)
    {
        if (NotReadable() is not { } witness) { NoPrecondition(); return; }
        Assert.Equal(FilePresence.Present, HostFile.Probe(witness));   // not GR3's arm: the file IS observable
        var reg = Register(organization, witness, optional: false);
        reg.OpenStatic("F", FileOpenMode.Input);
        Assert.Equal("37", reg.Status("F"));
    }

    /// <summary>⛔ GR16'S OTHER HALF, AND THE ANSWER TO "WHICH ARM DID YOU FIX?". §14.9.27.4 GR16 requires the
    /// file to support <i>"the input AND output statements that are permitted for the organization of that
    /// file when opened in the I-O mode"</i> — Table 20 lists READ under the I-O column alongside REWRITE — so
    /// a file this process may WRITE but not READ is '37' on OPEN I-O just as surely as a read-only one is.
    /// The write probe passes on this file by construction; the '37' therefore comes from the organization's
    /// own eager read, on every organization, and the two halves together cover the rule rather than the half
    /// that happened to be the defect.</summary>
    [Theory]
    [InlineData("seq")]
    [InlineData("rel")]
    [InlineData("idx")]
    public void OpenIo_PresentAndWritableButNotReadable_Is37(string organization)
    {
        if (NotReadable() is not { } witness) { NoPrecondition(); return; }
        Assert.True(HostFile.PermitsWrite(witness),
            "The premise of this test is a file the WRITE probe accepts; if it does not, the '37' below could "
            + "be the write half answering and GR16's read half would still be unmeasured.");
        var reg = Register(organization, witness, optional: false);
        reg.OpenStatic("F", FileOpenMode.IO);
        Assert.Equal("37", reg.Status("F"));
    }

    /// <summary>⛔ kb/Work PB2848 — a record-locking OUTPUT writer on a file this process may WRITE but not READ
    /// is successful. §14.9.27.4 GR3's '37' is "insufficient authority … to open the file" for the mode, and the
    /// mode here is OUTPUT: the connector's handle asks for READ as well only so a Linux host accepts the lock
    /// presence announcement (<c>SequentialConnector.HostAccess</c>), which is a preference of the lock, not a
    /// need of the open. The connector keeps a write-only handle and numbers its records by the run unit's mint,
    /// because it cannot count the medium; the record is in the file. Run on a file that
    /// <see cref="NotReadable"/> denies READ and still grants WRITE, SHARING WITH ALL OTHER so the writer takes the
    /// record-locking posture and the sharing flush.</summary>
    [Fact]
    public void OpenOutput_RecordLockingWriterOnAWriteOnlyFile_IsSuccessful()
    {
        if (NotReadable() is not { } witness) { NoPrecondition(); return; }
        Assert.True(HostFile.PermitsWrite(witness), "The premise is a file the host lets this process write.");
        var reg = Register("seq", witness, optional: false);
        reg.RegisterSharing("F", FileSharing.AllOther, FileLockMode.Manual, false);
        reg.OpenStatic("F", FileOpenMode.Output);
        Assert.Equal("00", reg.Status("F"));
        reg.Write("F", "WRITE-ONLY-REC-01", -1, null);
        Assert.Equal("00", reg.Status("F"));
        reg.Close("F");
        Assert.Equal("00", reg.Status("F"));
        RestoreWindowsOrUnixRead(witness);
        Assert.Equal("WRITE-ONLY-REC-01", File.ReadAllText(witness));
    }

    /// <summary>The other side of the same boundary: EXTEND on that file stays '37'. A record-locking EXTEND writer
    /// numbers the records already in the file (§9.1.16's identity is the ordinal), which it cannot do without
    /// reading them, so for EXTEND the read half is the open's need and not a preference.</summary>
    [Fact]
    public void OpenExtend_RecordLockingWriterOnAWriteOnlyFile_Is37()
    {
        if (NotReadable() is not { } witness) { NoPrecondition(); return; }
        var reg = Register("seq", witness, optional: false);
        reg.RegisterSharing("F", FileSharing.AllOther, FileLockMode.Manual, false);
        reg.OpenStatic("F", FileOpenMode.Extend);
        Assert.Equal("37", reg.Status("F"));
    }

    /// <summary>⛔ kb/Work PB2848 — the host path of an ASSIGN target the host cannot make absolute is data, not a
    /// crash. <see cref="CobolFile.ResolveHostPath"/> runs at registration and before the OPEN body, outside every
    /// I-O-status catch, and the content of a data-name-1 is an arbitrary string (§12.4.5.3 GR3 b)): a path longer
    /// than the host allows or a NUL threw <see cref="PathTooLongException"/> / <see cref="ArgumentException"/>
    /// out of the run unit. The spelling is returned as written and the OPEN reports what the host says.
    /// <para>A NUL is refused by every host. A 40,000-character spelling is refused where the host bounds a path's
    /// length (Windows); a host whose path resolution has no such bound (Linux) makes it absolute, and that absolute
    /// path is then its identity. Either way nothing escapes, so the long case asserts the two outcomes the host may
    /// choose between, never one host's.</para></summary>
    [Theory]
    [InlineData(40000)]
    [InlineData(-1)]
    public void ResolveHostPath_ASpellingTheHostCannotMakeAbsolute_IsReturnedAsWritten(int length)
    {
        string target = length < 0 ? "dir/a\0b.dat" : new string('a', length) + ".dat";
        string resolved = CobolFile.ResolveHostPath(target);
        if (length < 0)
        {
            Assert.Equal(target, resolved);
            return;
        }
        Assert.True(resolved == target || (Path.IsPathRooted(resolved) && resolved.EndsWith(target, StringComparison.Ordinal)),
            $"neither the spelling as written nor its absolute form: {resolved[..Math.Min(120, resolved.Length)]}");
    }

    [Theory]
    [InlineData(FileOpenMode.Input, "35")]
    [InlineData(FileOpenMode.Output, "30")]
    public void Open_ADynamicTargetTheHostCannotMakeAbsolute_IsAnIoStatus(FileOpenMode mode, string expected)
    {
        var reg = Register("seq", Path.Combine(_root, "placeholder.dat"), optional: false);
        reg.Open("F", mode, new string('a', 40000) + ".dat", assignDynamic: true, page: null);
        Assert.Equal(expected, reg.Status("F"));
    }

    /// <summary>Give <paramref name="witness"/> its read access back so the test can read what the write-only
    /// handle wrote — <see cref="Dispose"/> restores it too, but a test that reads the file needs it now.</summary>
    private static void RestoreWindowsOrUnixRead(string witness)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) RestoreWindowsAccess(witness);
        else File.SetUnixFileMode(witness, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }

    /// <summary>The escape hatch for the two capability helpers, classified exactly as
    /// <see cref="TheDenyHelperWorksOnThisHost"/> is. Each is measured separately, because they establish
    /// different preconditions through different mechanisms — a read-only attribute or mode bit for GR16's
    /// unwritable file, a deny-read ACE or a write-only mode for §9.1.13.6 item 6 a) 3.'s unreadable one — and
    /// a host may defeat either one alone.</summary>
    [Fact]
    public void TheCapabilityHelpersWorkOnThisHost()
    {
        NotWritable();
        var write = _denial;
        NotReadable();
        var read = _denial;
        _log.WriteLine(write.Because);
        _log.WriteLine(read.Because);
        Assert.False(write.Verdict == HostCapability.DenialVerdict.ToolFailed, write.Because);
        Assert.False(read.Verdict == HostCapability.DenialVerdict.ToolFailed, read.Because);
    }

    // ── The DELETE FILE twin, now on the same probe (§14.9.10.4 GR14/GR16) ───────────────────────────────────

    /// <summary>§14.9.10.4 GR16's '37'. Correct since kb/Work PB140; pinned here because PB323 moved it onto
    /// the shared <see cref="HostFile.Probe"/> — the refactor has to preserve it.</summary>
    [Fact]
    public void DeleteFile_PresentButRefused_Is37()
    {
        if (Refused() is not { } witness) { NoPrecondition(); return; }
        var reg = Register("seq", witness, optional: false);
        Assert.Equal("37", reg.DeleteFile("F"));
    }

    /// <summary>§14.9.10.4 GR14's '05' — a SUCCESSFUL completion for a file that is not present. A DIRECTORY at
    /// the path is not the §9.1.6 physical file, so it takes the absent arm; before PB323 unified the probe,
    /// <c>File.Delete</c>'s refusal made this a factually wrong '37' ("insufficient authority").</summary>
    [Fact]
    public void DeleteFile_DirectoryAtThePath_Is05()
    {
        string p = Path.Combine(_root, "notafile");
        Directory.CreateDirectory(p);
        var reg = Register("seq", p, optional: false);
        Assert.Equal("05", reg.DeleteFile("F"));
        Assert.True(Directory.Exists(p), "GR14's '05' means the FILE was not there; the directory is untouched.");
    }

    // ── Fixtures ────────────────────────────────────────────────────────────────────────────────────────────

    private static FileRegistry Register(string organization, string host, bool optional)
    {
        var reg = new FileRegistry();
        switch (organization)
        {
            case "seq": reg.Register("F", host, 17, false, optional, -1, -1); break;
            case "rel": reg.RegisterRelative("F", host, 17, optional, SeqAccess, 4, -1, -1); break;
            case "idx": reg.RegisterIndexed("F", host, 17, optional, SeqAccess, 0, 4, -1, -1); break;
            default: throw new ArgumentOutOfRangeException(nameof(organization), organization, "seq|rel|idx");
        }
        return reg;
    }

    /// <summary>Establish §14.9.27.4 GR3's precondition on <c>locked/secret.dat</c> — a file that IS present
    /// and that this process may not read — and return the file's path, or <c>null</c> when this host did not
    /// refuse the read. Either way <see cref="_denial"/> carries the CLASSIFICATION (kb/Work PB795): the denial
    /// was applied and worked, was applied and the host granted the access anyway, or was never applied at all.
    /// <para>The denial is verified with a RAW <c>File.OpenRead</c>, never with <see cref="HostFile.Probe"/>:
    /// checking the precondition with the subject under test would let a broken subject certify its own
    /// premise. A <c>null</c> return is asserted about, not assumed — see <see cref="NoPrecondition"/>.</para>
    /// </summary>
    private string? Refused()
    {
        string dir = Path.Combine(_root, "locked");
        Directory.CreateDirectory(dir);
        string witness = Path.Combine(dir, "secret.dat");
        if (!File.Exists(witness)) File.WriteAllText(witness, "HELLO-RECORD-0001");
        (bool applied, string detail) = DenyPresence(dir, witness);

        bool refused = false;
        try { using (File.OpenRead(witness)) { } }
        catch (UnauthorizedAccessException) { refused = true; }
        catch (IOException) { }
        _denial = HostCapability.Classify(refused, applied, $"GR3's present-but-refused witness {witness}", detail);
        return refused ? witness : null;
    }

    /// <summary>⛔ THE DENIAL IS EXPLICIT ON BOTH OBJECTS, AND THAT IS THE kb/Work PB795 FIX. GR3's witness has
    /// to be a file this process can neither open NOR observe — <c>HostFile.Probe</c> must answer
    /// <c>Unauthorized</c> and <c>File.Exists</c> must answer <c>false</c> — which on Windows takes TWO deny
    /// ACEs, because the two questions are answered by two different objects:
    /// <list type="bullet">
    /// <item><description>the FILE's own <c>(RX)</c> deny takes FILE_READ_DATA <i>and</i> FILE_READ_ATTRIBUTES,
    /// which is what makes <c>File.GetAttributes</c> raise <see cref="UnauthorizedAccessException"/>;</description></item>
    /// <item><description>the DIRECTORY's <c>(RX)</c> deny takes FILE_LIST_DIRECTORY, which is what removes the
    /// implicit FILE_READ_ATTRIBUTES a caller gets on a child of a directory it may list.</description></item>
    /// </list>
    /// <para>Measured on 2026-09-06, each alone is insufficient: a directory-scoped deny leaves
    /// <c>File.OpenRead</c> of the child SUCCEEDING (opening a child by full path consults the parent only for
    /// FILE_TRAVERSE, and "Bypass traverse checking" is granted to Everyone by default), and a file-scoped deny
    /// alone leaves <c>File.GetAttributes</c> SUCCEEDING. Until PB795 only the directory was denied, and the
    /// refusal came from a SIDE EFFECT of <c>/inheritance:r</c> — it stops the directory propagating inheritable
    /// ACEs, so the child's DACL, which held nothing else, was left empty. A precondition resting on an
    /// inheritance side effect is a precondition that a host can silently not have, and the GitHub Windows
    /// runner is a host that did not (while the file-scoped deny in <see cref="NotReadable"/> worked there in
    /// the same run — that is what narrowed it). The <c>/inheritance:r /grant:r</c> pair stays because it makes
    /// the directory's own DACL deterministic; nothing now DEPENDS on what it propagates.</para>
    /// <para>The file must be denied FIRST: once the directory refuses this process, <c>icacls</c> can no longer
    /// reach the child to set anything on it.</para>
    /// <para><c>Applied</c> is what separates "the host bypassed the denial" from "the denial was never made" —
    /// the host's own tool reporting success, or the mode reading back as it was set. It is deliberately NOT the
    /// same measurement as the refusal itself (feedback: verdict_evidence_invariant).</para></summary>
    private static (bool Applied, string Detail) DenyPresence(string dir, string witness)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            string who = $"{Environment.UserDomainName}\\{Environment.UserName}";
            ProcessObservation[] runs =
            [
                Icacls(witness, "/deny", $"{who}:(RX)"),
                Icacls(dir, "/inheritance:r", "/grant:r", $"{who}:(F)"),
                Icacls(dir, "/deny", $"{who}:(RX)"),
            ];
            return (runs.All(r => r.Completed && r.ExitCode == 0),
                $"deny (RX) for {who} on the file then on its directory — icacls: "
                + string.Join(" | ", runs.Select(Describe)));
        }

        // Mode 000 on the file AND on its directory. Unix has no bypass-traverse equivalent, so the directory
        // alone would do — the file is denied too so that both platforms establish the precondition explicitly
        // rather than one of them relying on a property of the other object. The file's mode is read back
        // BEFORE the directory is closed off, because afterwards it cannot be stat'd at all.
        File.SetUnixFileMode(witness, UnixFileMode.None);
        bool fileSet = File.GetUnixFileMode(witness) == UnixFileMode.None;
        File.SetUnixFileMode(dir, UnixFileMode.None);
        bool dirSet = File.GetUnixFileMode(dir) == UnixFileMode.None;
        return (fileSet && dirSet,
            $"chmod 000 on {witness} (read back: {fileSet}) and on {dir} (read back: {dirSet}).");
    }

    /// <summary>Create <c>readonly.dat</c>, make it unwritable to this process while leaving it PRESENT and
    /// fully observable, and return its path — the precondition of §14.9.27.4 GR16 and §9.1.13.6 item 6 a),
    /// actually established — or <c>null</c> when this host did not refuse the write. <see cref="_denial"/>
    /// carries the classification either way.
    /// <para>This is deliberately NOT <see cref="Refused"/>'s denial: that one hides the file from the presence
    /// probe, which is GR3's precondition and a different rule. GR16's file is one the process can see, stat
    /// and read perfectly — the '37' has to come from the write capability alone, so the two probes must give
    /// different answers on it. The denial is verified with a RAW write open, never with
    /// <c>HostFile.PermitsWrite</c>: checking the precondition with the subject under test would let a broken
    /// subject certify its own premise. A <c>null</c> return is asserted about, not assumed — see
    /// <see cref="NoPrecondition"/> and <see cref="TheCapabilityHelpersWorkOnThisHost"/>.</para></summary>
    /// <summary>⛔ THE WITNESS HAS TO BE A FILE THE ORGANIZATION UNDER TEST CAN ACTUALLY INTERPRET, or the test
    /// establishes TWO contradictions and measures the wrong one. Every witness here used to be the plain text
    /// <c>"HELLO-RECORD-0001"</c>, shared by all three organizations; since kb/Work PB802 a RELATIVE or INDEXED
    /// connector reads its §9.1.6 fixed file attributes from the framed store's own HEADER, so a plain text file
    /// opened through one of those descriptions is a §14.9.27.4 GR10 file attribute conflict — '39' — and GR10
    /// is answered BEFORE GR16's capability question, which is what these tests are about. So a keyed witness is
    /// CREATED THROUGH ITS OWN ORGANIZATION first (an OPEN OUTPUT establishes the attributes, GR18) and only
    /// then denied; the ACL is then the only thing wrong with it, which is the whole point of the fixture.
    /// <para>A record-sequential witness stays plain text, because plain bytes ARE that organization's format
    /// (§9.1.7.2) — nothing about it can contradict a description.</para>
    /// <para>The geometry matches <see cref="Register"/>'s exactly (17-byte records; relative key digits 4;
    /// indexed prime key at 0 for 4), so the record sizes and the key table agree too and the ONLY question the
    /// OPEN can fail on is the capability one.</para></summary>
    private static void MakeInterpretableSubject(string organization, string witness)
    {
        if (organization == "seq") { File.WriteAllText(witness, "HELLO-RECORD-0001"); return; }
        var reg = Register(organization, witness, optional: false);
        reg.OpenStatic("F", FileOpenMode.Output);
        Assert.Equal("00", reg.Status("F"));   // the subject must really have been created
        reg.Close("F");
    }

    private string? NotWritable(string organization = "seq")
    {
        string witness = Path.Combine(_root, $"readonly-{organization}.dat");
        if (!File.Exists(witness)) MakeInterpretableSubject(organization, witness);
        bool applied;
        string detail;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            File.SetAttributes(witness, FileAttributes.ReadOnly);
            applied = File.GetAttributes(witness).HasFlag(FileAttributes.ReadOnly);
            detail = $"FileAttributes.ReadOnly on {witness} (read back: {applied}).";
        }
        else
        {
            File.SetUnixFileMode(witness, UnixFileMode.UserRead);
            applied = File.GetUnixFileMode(witness) == UnixFileMode.UserRead;
            detail = $"chmod 400 on {witness} (read back: {applied}).";
        }

        bool refused = false;
        try { using (new FileStream(witness, FileMode.Open, FileAccess.Write, FileShare.ReadWrite)) { } }
        catch (UnauthorizedAccessException) { refused = true; }
        catch (IOException) { }
        _denial = HostCapability.Classify(refused, applied, $"GR16's present-but-unwritable witness {witness}", detail);
        return refused ? witness : null;
    }

    /// <summary>Create <c>unreadable.dat</c> as a WRITE-ONLY file — refused read, still writable, and with its
    /// directory listable so the presence probe answers <c>Present</c>. It is the exact complement of
    /// <see cref="NotWritable"/>, and having both is what keeps §14.9.27.4 GR16's two halves separable: GR16
    /// requires the file to support <i>"the input AND output statements"</i> the I-O mode permits, so a file
    /// that fails EITHER half is '37' and a probe that only asked about writing would answer half a rule.
    /// <para>Deliberately not <see cref="Refused"/>'s shape: there the file is denied its ATTRIBUTES too and
    /// the answer comes from GR3's presence short-circuit. Here <c>File.GetAttributes</c> succeeds, so the '37'
    /// can only come from the organization's own read. On Windows the deny ACE is <c>(RD)</c> — read DATA only;
    /// a blanket <c>(R)</c> takes the synchronize/read-control rights a write open also needs and would
    /// silently make the file unwritable too, collapsing the complement. Verified with a raw
    /// <c>File.OpenRead</c>, never with the subject under test. Returns <c>null</c> when this host did not
    /// refuse the read; <see cref="_denial"/> carries the classification.</para></summary>
    private string? NotReadable()
    {
        string witness = Path.Combine(_root, "unreadable.dat");
        if (!File.Exists(witness)) File.WriteAllText(witness, "HELLO-RECORD-0001");
        bool applied;
        string detail;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            string who = $"{Environment.UserDomainName}\\{Environment.UserName}";
            var run = Icacls(witness, "/deny", $"{who}:(RD)");
            applied = run.Completed && run.ExitCode == 0;
            detail = $"deny (RD) for {who} on {witness} — icacls: {Describe(run)}";
        }
        else
        {
            File.SetUnixFileMode(witness, UnixFileMode.UserWrite);
            applied = File.GetUnixFileMode(witness) == UnixFileMode.UserWrite;
            detail = $"chmod 200 on {witness} (read back: {applied}).";
        }

        bool refused = false;
        try { using (File.OpenRead(witness)) { } }
        catch (UnauthorizedAccessException) { refused = true; }
        catch (IOException) { }
        _denial = HostCapability.Classify(refused, applied, $"item 6 a) 3.'s present-but-unreadable witness {witness}", detail);
        return refused ? witness : null;
    }

    /// <summary>Whether the host really does allow creating a NEW file under the refused directory — the
    /// premise of the OPEN OUTPUT guard. Unix mode 000 removes write along with read, so only the Windows
    /// deny-RX shape has an unreadable-but-writable directory for GR18's creation arm to be guarded in.</summary>
    private static bool RawCreateSucceeds(string path)
    {
        try
        {
            using (File.Create(path)) { }
            File.Delete(path);
            return true;
        }
        catch (UnauthorizedAccessException) { return false; }
        catch (IOException) { return false; }
    }

    private static void Restore(string root)
    {
        // The GR16 witness is read-only, and a read-only file defeats the recursive Directory.Delete below on
        // Windows — the temp tree would then leak one directory per run.
        string ro = Path.Combine(root, "readonly.dat");
        if (File.Exists(ro))
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) File.SetAttributes(ro, FileAttributes.Normal);
            else File.SetUnixFileMode(ro, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        string nr = Path.Combine(root, "unreadable.dat");
        if (File.Exists(nr))
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) RestoreWindowsAccess(nr);
            else File.SetUnixFileMode(nr, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        string dir = Path.Combine(root, "locked");
        if (!Directory.Exists(dir)) return;
        // ⛔ THE DIRECTORY FIRST, THEN THE FILE INSIDE IT. The GR3 witness carries its own deny ACE (or mode
        // 000), and while the directory still refuses this process nothing inside it can be reached to undo
        // that — icacls answers "Access is denied" and the temp tree leaks one directory per run.
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            RestoreWindowsAccess(dir);
            Icacls(dir, "/inheritance:e");
        }
        else
        {
            File.SetUnixFileMode(dir,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
        string secret = Path.Combine(dir, "secret.dat");
        if (!File.Exists(secret)) return;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) RestoreWindowsAccess(secret);
        else File.SetUnixFileMode(secret, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }

    /// <summary>⛔ UNDOING AN <c>icacls /deny</c> TAKES TWO STEPS, AND ONLY ONE OF THEM WAS EVER DONE. Its
    /// documented behaviour is <i>"an explicit deny ACE is added for the stated permissions AND the same
    /// permissions in any explicit grant are removed"</i> — so <c>/remove:d</c>, which deletes the deny ACE,
    /// leaves the grant with a hole in it. Measured on 2026-09-06: after
    /// <c>/inheritance:r /grant:r who:(F)</c> + <c>/deny who:(RX)</c> + <c>/remove:d who</c>, the directory's
    /// DACL reads <c>who:(W,D,WDAC,WO,DC)</c> — Full minus read and execute — so the recursive
    /// <c>Directory.Delete</c> in <see cref="Dispose"/> cannot enumerate it and the temp tree leaks one
    /// permanently undeletable directory per test-class instance, on every Windows run there has ever been.
    /// Re-granting is what actually restores it.</summary>
    private static void RestoreWindowsAccess(string target)
    {
        string who = $"{Environment.UserDomainName}\\{Environment.UserName}";
        Icacls(target, "/remove:d", who);
        Icacls(target, "/grant", $"{who}:(F)");
    }

    /// <summary>Run <c>icacls</c> through <see cref="ProcessObserver"/> — the ONE child-process seam
    /// (<c>ProcessObservationDriftTests</c>). A private <c>WaitForExit(n)</c> here would report a contention
    /// timeout as a silently-unchanged DACL, and the tests above would then read the missing denial as a
    /// conforming '35'. The observer's budget applies.
    /// <para>⛔ THE OBSERVATION IS RETURNED, AND SINCE kb/Work PB795 IT IS READ. Whether the denial WORKED is
    /// still settled by a raw open and never by what icacls said — but whether it was APPLIED can only be
    /// answered by the tool that applied it, and those are two different questions. Conflating them is what let
    /// a host that defeated one denial mechanism be reported as a broken helper.</para></summary>
    private static ProcessObservation Icacls(string target, params string[] args)
    {
        var psi = new ProcessStartInfo("icacls")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        psi.ArgumentList.Add(target);
        foreach (string a in args) psi.ArgumentList.Add(a);
        return ProcessObserver.ObserveOrThrow(psi);
    }

    /// <summary>One child-process observation, collapsed to a single line a CI log can carry.</summary>
    private static string Describe(ProcessObservation run)
    {
        string text = Collapse(run.Stdout);
        string err = Collapse(run.Stderr);
        return $"[exit {run.ExitCode}] {text}" + (err.Length == 0 ? "" : $" stderr: {err}");
    }

    private static string Collapse(string text) =>
        string.Join(" ", text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
