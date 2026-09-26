// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>The mode a file connector is opened in (ISO/IEC 1989:2023 §14.9.25 OPEN).</summary>
public enum FileOpenMode
{
    /// <summary>OPEN INPUT — read-only sequential access from the start.</summary>
    Input,
    /// <summary>OPEN OUTPUT — write a new file from the start (existing content discarded).</summary>
    Output,
    /// <summary>OPEN EXTEND — append after the existing records.</summary>
    Extend,
    /// <summary>OPEN I-O — read and rewrite an existing file.</summary>
    IO,
}

/// <summary>⛔ THE PER-FILE-NAME TAPE PHRASE of one OPEN group — the alternation ISO §14.9.27.2 prints beside
/// each file-name, <c>[ WITH NO REWIND ]</c> in 2023 and <c>[ REVERSED | WITH NO REWIND ]</c> at COBOL-85
/// (VERSION_CHANGE_REFERENCE row 7.12 records REVERSED's deletion by ISO 2002).
/// <para>ONE enum, never one bool per phrase: the grammar makes the alternatives mutually exclusive, and the
/// run-time effect of each is then an ARM of one switch in <c>FileRegistry.OpenCore</c> rather than an
/// independent parameter each entry point has to remember to thread. That is not a style preference — the
/// second phrase was DROPPED for exactly as long as the first one had a parameter of its own (kb/Work PB668
/// after kb/Work PB317).</para></summary>
public enum OpenTapePhrase
{
    /// <summary>No tape phrase was written for this file-name.</summary>
    None,
    /// <summary>WITH NO REWIND — §14.9.27.4 GR11/GR12; on this compiler's only medium category (a) Non-unit it
    /// is ignored and the successful OPEN reports '07' (§9.1.13.2 item 6).</summary>
    NoRewind,
    /// <summary>REVERSED — the COBOL-85 phrase: the file is positioned at its END and every subsequent READ
    /// retrieves the PRECEDING record, the at end condition arising at the file's first record. Legal source
    /// only at <c>--std 85</c>; the edition gate is <c>open-reversed-removed-2002</c> (COBOLNET0902).</summary>
    Reversed,
}

/// <summary>The file-sharing mode of a connector (ISO/IEC 1989:2023 §9.1.15; the SHARING clause §12.4.5.15 / the
/// OPEN SHARING phrase §14.9.27). Governs whether OTHER connectors may open the same physical file (§14.9.27.4
/// <see cref="Table19"/> → status '61'). These three members are the whole of what §9.1.15 specifies — <i>"The
/// sharing with no other mode …"</i>, <i>"The sharing with read only mode …"</i>, <i>"The sharing with all other
/// mode …"</i> — so there is deliberately no fourth "default" member: a connector that writes NEITHER a SHARING
/// clause nor an OPEN SHARING phrase carries the UNDETERMINED implementor default, represented as a null
/// <c>FileSharing?</c> (see <see cref="FileRegistry.ImplementorDefaultSharing"/>, kb/Work PB322). Every connector
/// takes part in the Table-19 arbitration whatever it declared (kb/Work PB321).</summary>
public enum FileSharing
{
    /// <summary>SHARING WITH NO OTHER — exclusive; no other connector may open the file (record locks ignored, GR3).</summary>
    NoOther,
    /// <summary>SHARING WITH READ ONLY — other connectors may open only for INPUT.</summary>
    ReadOnly,
    /// <summary>SHARING WITH ALL OTHER — other connectors may open in any mode (record locking is then observable).</summary>
    AllOther,
}

/// <summary>The record-locking mode of a connector (ISO §12.4.5.9 LOCK MODE).</summary>
public enum FileLockMode
{
    /// <summary>No LOCK MODE clause — no record locking (the implementor default, GR1).</summary>
    None,
    /// <summary>LOCK MODE IS MANUAL — a lock is acquired only for a READ … WITH LOCK (GR5).</summary>
    Manual,
    /// <summary>LOCK MODE IS AUTOMATIC — a lock is acquired on every successful READ (GR4).</summary>
    Automatic,
}

/// <summary>The explicit lock-RETENTION phrase on a READ/WRITE/REWRITE — the printed
/// <c>[ WITH LOCK | WITH NO LOCK ]</c> bracket of ISO §14.9.30.2 / §14.9.51.2 / §14.9.35.2.
/// <para>⛔ IGNORING LOCK IS DELIBERATELY ABSENT (kb/Work PB331). It is an alternative of READ's OTHER printed
/// bracket, which §5.2.6.1 makes an independent selection, so <c>READ … IGNORING LOCK WITH NO LOCK</c> is one
/// legal statement that has to say BOTH things at once — a single enum member cannot. It travels beside this
/// one as the <c>ignoringLock</c> argument of <see cref="CobolFile.ReadShared"/> /
/// <see cref="CobolFile.ReadKeyedShared"/>.</para></summary>
public enum FileRecordLock
{
    /// <summary>No phrase — the connector's LOCK MODE governs (AUTOMATIC auto-locks; MANUAL/None do not).</summary>
    None,
    /// <summary>WITH LOCK — request a lock on the accessed record (MANUAL locking).</summary>
    WithLock,
    /// <summary>WITH NO LOCK — do not lock the accessed record.</summary>
    WithNoLock,
}

/// <summary>Which ADVANCING phrases ONE WRITE statement carries — the print-control bracket of ISO §14.9.51.2
/// Format 1. It is the WRITE's <i>presentation</i> shape, and nothing else: it does NOT decide whether the
/// statement is lock-governed.
/// <para>⛔ A WRITE WITH AN ADVANCING PHRASE IS STILL A WRITE STATEMENT. §14.9.51.4 GR10 and GR11 are ALL FILES
/// rules — <i>"If the locking mode of the write file connector is single record locking, any record lock
/// associated with that file connector is released by the execution of the WRITE statement"</i> and <i>"If
/// record locks have an effect for the write file connector and the WITH LOCK phrase is specified or implied,
/// the record lock associated with the record written is set …"</i> — and Format 1's printed general format
/// carries the <c>retry-phrase</c> and the <c>WITH LOCK / WITH NO LOCK</c> bracket ALONGSIDE the ADVANCING
/// phrase, so `WRITE R AFTER ADVANCING 1 LINE WITH LOCK RETRY 5 TIMES` is one legal statement. This descriptor
/// therefore RIDES WITH the record into the ONE governed entry (<see cref="CobolFile.WriteShared"/>); it never
/// selects a separate ungoverned one. Three separate emitter arms used to, and the two advancing arms dropped
/// the lock and RETRY phrases on the floor (kb/Work PB683).</para></summary>
public enum WriteAdvanceKind : byte
{
    /// <summary>No ADVANCING phrase — the plain record write.</summary>
    None,
    /// <summary>BEFORE ADVANCING — the record is presented, then the medium advances.</summary>
    Before,
    /// <summary>AFTER ADVANCING — the medium advances, then the record is presented.</summary>
    After,
    // ⛔ A `BeforeAndAfter` MEMBER LIVED HERE AND IS GONE (kb/Work PB712). It carried a SECOND advance amount for
    // `WRITE … BEFORE ADVANCING n AFTER ADVANCING m` — a spelling §14.9.51.2 Format 1 never prints. The printed
    // phrase has ONE `ADVANCING` operand and §14.9.51.4 GR25 a) advances the page "the number of lines equal to
    // that value", once; the two WORDS only place that one advance (GR25 e)/f)), and the combined form places it
    // exactly where BEFORE does. There are TWO presentation shapes, which is what this enum now spells.
}

/// <summary>The ADVANCING phrase of ONE WRITE statement, as data — see <see cref="WriteAdvanceKind"/> for why
/// this travels as an argument of the governed WRITE rather than as a choice of runtime entry.</summary>
/// <param name="Kind">The §14.9.51.4 GR25 e)/f) placement of the advance, decided in the binder from the
/// statement's BEFORE/AFTER words.</param>
/// <param name="Lines">The phrase's single line count (§14.9.51.4 GR25 a)–d)); <c>-1</c> is ADVANCING PAGE,
/// which §14.9.51.3 SR17 forbids when both words are written.</param>
public readonly record struct WriteAdvance(WriteAdvanceKind Kind, int Lines)
{
    /// <summary>A WRITE with no ADVANCING phrase.</summary>
    public static WriteAdvance None => default;
}

/// <summary>The RETRY phrase kind (ISO §14.7.9).</summary>
public enum FileRetryKind
{
    /// <summary>No RETRY phrase — a single lock attempt (GR4a).</summary>
    None,
    /// <summary>RETRY n TIMES — the lock check is attempted n+1 times (GR1); a zero or negative n makes no
    /// further attempt (GR4a).</summary>
    Times,
    /// <summary>RETRY FOR n SECONDS — a timeout period clamped by GR2 to the implementor's MAXIMUM MEANINGFUL
    /// VALUE, which COBOL.NET defines as ZERO (Annex A.1 item 166, recorded in docs/CONFORMANCE.md §7): a lock
    /// here is held only by a file connector of the executing run unit, which cannot release it while this
    /// statement runs, so no positive timeout could change the outcome. The period is therefore zero-length,
    /// no further attempt is made, and the conflict's OWN §9.1.13 status stands — never a sleep.</summary>
    Seconds,
    /// <summary>RETRY FOREVER — GR3's unbounded wait. The conflict's own §9.1.13 status stands, except that a
    /// wait on a record locked by another file connector is the deadlock §9.1.13.8 item 2 defines and this
    /// implementation detects ('52'; A.1 item 109). See <c>FileRegistry.ExhaustionStatus</c>.</summary>
    Forever,
}

/// <summary>What an honest probe of a physical file's presence can answer — <b>three</b> states, not two.
/// <para>ISO/IEC 1989:2023 draws the distinction and prices it in two different I-O status values: §9.1.13.6
/// item 5 sets '35' because <i>"an OPEN statement with the INPUT, I-O, or EXTEND phrase is attempted on a file
/// that is not described as optional and the physical file is not present"</i>, while §14.9.27.4 GR3 sets '37'
/// because <i>"the file associated with file-name-1 is present and insufficient authority exists to open the
/// file"</i> — restated as §9.1.13.6 item 6 b), which adds <i>"The ability to detect this is processor
/// dependent"</i>. A BOOLEAN probe cannot express the difference, so it has to invent one of the two answers,
/// and <c>File.Exists</c> invents the wrong one: it swallows every access error and returns false. A present
/// file the process may not observe is then reported to the COBOL program as ABSENT — '35' where the standard
/// requires '37', and for an OPTIONAL file a <i>successful</i> open over an invented empty file (kb/Work PB323;
/// the DELETE FILE twin of the same defect was kb/Work PB140).</para></summary>
public enum FilePresence
{
    /// <summary>The physical file is not present — §9.1.13.6 item 5's '35' for a non-optional INPUT/I-O/EXTEND,
    /// and the condition §14.9.27.4 GR13 and GR17 both open with (<i>"If the file is not present"</i>) for the
    /// OPTIONAL at-end and creation arms.</summary>
    Absent,
    /// <summary>The physical file is present and observable.</summary>
    Present,
    /// <summary>The process was refused the authority even to observe whether the file is there. §14.9.27.4 GR3
    /// and §9.1.13.6 item 6 b) make this '37' — never '35', and never an OPTIONAL file's invented empty one:
    /// a refusal is not evidence of absence.</summary>
    Unauthorized,
}

/// <summary>Host-filesystem probes shared by every file organization. ⛔ <b>THIS IS THE ONE PLACE THE RUNTIME
/// ASKS THE OPERATING ENVIRONMENT ABOUT A PHYSICAL FILE</b> — two questions, each asked once per OPEN by
/// <see cref="FileConnector.Open"/> and never by an organization body: <see cref="Probe"/> ("is it there, and
/// may this process observe it?", handed down to <c>OpenCore</c>; <c>FileRegistry.DeleteFile</c> takes the same
/// answer for §14.9.10.4 GR14/GR16) and <see cref="PermitsWrite"/> ("may this process WRITE it?", §14.9.27.4
/// GR16 / §9.1.13.6 item 6 a)). <c>HostFileProbeDriftTests</c> keeps it that way: <c>File.Exists</c>,
/// <c>FileInfo.Exists</c> and <c>File.GetAttributes</c> are forbidden everywhere else under
/// <c>Cobol.Net.Runtime/IO</c>, because every hand-written probe is another chance to answer "absent" to a
/// question the operating environment refused to answer at all — and <see cref="PermitsWrite"/> may be CALLED
/// only from <see cref="FileConnector"/>, because a capability asked inside one organization's arm is a
/// capability the other organizations do not ask (kb/Work PB328).
/// <para>⛔ IT IS ALSO THE ONE PLACE A HOST-PATH <b>STREAM</b> IS OPENED — <see cref="OpenConnectorStream"/>
/// (a connector's own long-lived handle) and <see cref="OpenAuxiliary"/> (a short-lived handle the runtime
/// takes for its own bookkeeping over a path a connector of THIS run unit may already hold). The share mode is
/// derived here from the role, never spelled at a call site, because a call site that omits it inherits the
/// .NET constructor default and the default is WRONG in both directions: <c>new FileStream(p, m, a)</c> is
/// <c>FileShare.Read</c> and <c>new FileStream(p, m)</c> is <c>FileShare.None</c>, so a bookkeeping read of a
/// file the connector holds open for WRITE is refused by the operating environment and the refusal escapes an
/// OPEN that §9.1.15 and Table 19 had already ALLOWED (kb/Work PB713 — <c>OPEN EXTEND</c> on a sharing-active
/// LINE SEQUENTIAL file killed the run unit with an unhandled <c>IOException</c> naming "another process" that
/// was the program itself). <c>HostFileProbeDriftTests</c> forbids every other stream/content open under
/// <c>Cobol.Net.Runtime/IO</c> for the same reason it forbids <c>File.Exists</c>: the mistake is invisible in
/// review and costs a crash, so the guard is structural.</para></summary>
public static class HostFile
{
    /// <summary>Probe the physical file at <paramref name="hostPath"/>, distinguishing §9.1.13.6 item 5's
    /// ABSENT from §14.9.27.4 GR3's PRESENT-but-unauthorized. Never throws for either of those two.
    /// <para>The probe is <c>File.GetAttributes</c> rather than <c>File.Exists</c> precisely because its
    /// FAILURES carry the distinction: a missing file or directory raises
    /// <see cref="FileNotFoundException"/>/<see cref="DirectoryNotFoundException"/>, while a refused one raises
    /// <see cref="UnauthorizedAccessException"/>. <c>File.Exists</c> collapses both to <c>false</c>.</para>
    /// <para>A DIRECTORY at the path answers <see cref="FilePresence.Absent"/>: §9.1.6's fixed file attributes
    /// describe a <i>physical file</i>, and a directory of that name is not one — so "the physical file is not
    /// present" (§9.1.13.6 item 5) is the true statement about it. That is also the answer <c>File.Exists</c>
    /// gave, so the OPEN path is unchanged; DELETE FILE on a directory moves from a factually wrong '37'
    /// ("insufficient authority") to §14.9.10.4 GR14's '05', the successful completion for a file that is not
    /// there.</para>
    /// <para>A path the host will not accept as a file name at all — too long for it
    /// (<see cref="PathTooLongException"/>), or malformed (<see cref="ArgumentException"/>,
    /// <see cref="NotSupportedException"/>) — likewise names no present file. That is a statement about the
    /// PATH, not an input-output failure, so it belongs with Absent rather than with '30'; it is also the
    /// reading <c>RecordFraming.ReadHeader</c> already takes, and the answer <c>File.Exists</c> gave.</para>
    /// <para>Every OTHER <see cref="IOException"/> PROPAGATES on purpose: §9.1.13.6 item 1's '30' (<i>"A
    /// permanent error exists and no further information is available concerning the input-output
    /// operation"</i>) is mapped in exactly ONE place — <see cref="FileConnector.Open"/>'s catch — and a probe
    /// that swallowed it here would have to guess between '35' and '37' for a file it never reached.</para></summary>
    public static FilePresence Probe(string hostPath)
    {
        try
        {
            return File.GetAttributes(hostPath).HasFlag(FileAttributes.Directory)
                ? FilePresence.Absent : FilePresence.Present;
        }
        catch (FileNotFoundException) { return FilePresence.Absent; }              // §9.1.13.6 item 5
        catch (DirectoryNotFoundException) { return FilePresence.Absent; }         // §9.1.13.6 item 5
        catch (UnauthorizedAccessException) { return FilePresence.Unauthorized; }  // §14.9.27.4 GR3 / item 6 b)
        catch (PathTooLongException) { return FilePresence.Absent; }               // a path shape, not an I/O failure
        catch (ArgumentException) { return FilePresence.Absent; }                  // not a file name the host takes
        catch (NotSupportedException) { return FilePresence.Absent; }
    }

    /// <summary>Whether the operating environment permits WRITE operations on the PRESENT physical file at
    /// <paramref name="hostPath"/> — the question §14.9.27.4 GR16 and §9.1.13.6 item 6 a) price at '37'.
    /// <para>⛔ THE PROBE IS A REAL WRITE OPEN, because nothing weaker answers the question the standard asks.
    /// A read-only ATTRIBUTE, a mode bit and a deny-write ACE are three different mechanisms with the same
    /// consequence, and a host may add a fourth; asking the host to hand over a writable handle asks about the
    /// consequence rather than about any one mechanism. <c>FileMode.Open</c> (never <c>Create</c>/<c>Truncate</c>)
    /// with no bytes written leaves the file byte-for-byte and timestamp-for-timestamp as it was, which
    /// §14.9.27.4 GR25 requires of an OPEN that is about to be unsuccessful (<i>"If the execution of the OPEN
    /// statement is unsuccessful, the file is not affected"</i>) and which the SUCCESSFUL path needs just as
    /// much — the probe closes before the organization's own stream opens. <c>FileShare.ReadWrite</c> is the
    /// most permissive request there is, so the probe itself never manufactures a sharing failure that the
    /// organization's own open would not have hit.</para>
    /// <para>ONLY <see cref="UnauthorizedAccessException"/> is an answer. Every other
    /// <see cref="IOException"/> PROPAGATES on purpose, exactly as <see cref="Probe"/>'s do: §9.1.13.6 item 1's
    /// '30' (<i>"A permanent error exists and no further information is available concerning the input-output
    /// operation"</i>) is mapped in ONE place — <see cref="FileConnector.Open"/>'s catch — and a probe that
    /// swallowed a host refusal here would have to invent "writable" for a file it never reached. That is also
    /// what the sequential organization has always done, since its I-O/EXTEND streams are themselves write
    /// opens; the probe makes the keyed organizations answer the same way (kb/Work PB328).</para></summary>
    public static bool PermitsWrite(string hostPath)
    {
        try
        {
            using var _ = new FileStream(hostPath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
            return true;
        }
        catch (UnauthorizedAccessException) { return false; }   // §9.1.13.6 item 6 a) — '37'
    }

    /// <summary>⛔ THE ONE HOST CLASSIFICATION OF A REFUSAL AS A SHARING CONFLICT (kb/Work PB860): did the operating
    /// environment refuse this operation because another handle holds the physical file in a manner that
    /// excludes it? That is §9.1.13.9's condition — <i>"that physical file is already open by another file
    /// connector in a manner that conflicts with this request"</i> — and the host SAYS so, so it must never be
    /// answered with §9.1.13.6 item 1's '30', whose whole definition is that <i>"no further information is
    /// available"</i>.
    /// <para>The host's own words, and nothing else: Windows reports <c>ERROR_SHARING_VIOLATION</c> (32) for a
    /// share-mode refusal and <c>ERROR_LOCK_VIOLATION</c> (33) for a byte-range one, both surfaced by .NET as
    /// the HRESULT <c>0x8007xxxx</c>; .NET on Unix implements <see cref="FileShare"/> with <c>flock(LOCK_NB)</c>
    /// and surfaces its refusal, <c>EWOULDBLOCK</c>, as the raw errno — 11 on Linux, 35 on the BSD family
    /// (macOS). The errno is a per-host fact, so the platform question is asked HERE, once, and never at a call
    /// site (kb/Work PB795).</para>
    /// <para>The caller decides what the refusal MEANS for its statement — '61' for an OPEN (§9.1.13.9 1)),
    /// '62' for a DELETE FILE (§9.1.13.9 2)) — because the standard gives the two statements different values
    /// for the one host fact.</para></summary>
    public static bool IsSharingRefusal(IOException refusal) => refusal.HResult switch
    {
        unchecked((int)0x80070020) or unchecked((int)0x80070021) => true,   // ERROR_SHARING/LOCK_VIOLATION
        int errno when !OperatingSystem.IsWindows() => errno == UnixWouldBlock,
        _ => false,
    };

    /// <summary>Is the PRESENT physical file at <paramref name="hostPath"/> held open by a handle this call does
    /// not own — §9.1.13.9 2)'s <i>"that physical file is currently open by another file connector"</i>, asked of
    /// the host for a connector that is not this run unit's (kb/Work PB860)?
    /// <para>⛔ ASKED BY REQUESTING EXCLUSIVE ACCESS, because the delete itself cannot answer it on every host.
    /// Windows refuses to delete a file any handle holds without <c>FILE_SHARE_DELETE</c>, but a Unix unlink
    /// never consults open handles, so a DELETE FILE there would destroy a file another run unit is reading and
    /// report success. A <see cref="FileShare.None"/> request is refused by every outstanding handle on both
    /// (Windows' share modes; .NET's <c>flock(LOCK_EX)</c> against the <c>LOCK_SH</c>/<c>LOCK_EX</c> every
    /// connector's own handle takes), and the refusal is classified by <see cref="IsSharingRefusal"/>. The
    /// request opens for READ with <see cref="FileMode.Open"/>, so it cannot create, truncate or change the file;
    /// a file this process may not READ is not refused here — insufficient authority is the delete's own
    /// question (§14.9.10.4 GR16), answered by the delete.</para></summary>
    public static bool IsHeldByAnother(string hostPath)
    {
        try
        {
            using var _ = new FileStream(hostPath, FileMode.Open, FileAccess.Read, FileShare.None);
            return false;
        }
        catch (IOException e) when (IsSharingRefusal(e)) { return true; }
        // Not a sharing question: an absent file is GR14's, a refused one GR16's, any other failure the delete's
        // own — each answered by the caller's presence probe or by the delete, never guessed here.
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }

    /// <summary><c>EWOULDBLOCK</c> (= <c>EAGAIN</c>) on this Unix host — 11 on Linux, 35 on the BSD family.</summary>
    private static readonly int UnixWouldBlock = OperatingSystem.IsLinux() || OperatingSystem.IsAndroid() ? 11 : 35;

    /// <summary>⛔ THE ONE HOST CLASSIFICATION OF A WRITE FAILURE AS THE MEDIUM'S BOUNDARY (kb/Work PB1192): did
    /// the operating environment refuse to store bytes because the medium is full, a quota is exhausted, or the
    /// file has reached the largest size the host allows? That is the manner in which COBOL.NET defines the
    /// externally-defined boundary of a physical sequential file — ISO §9.1.13.6 3), <i>"The implementor
    /// specifies the manner in which these boundaries are defined"</i> (Annex A.1 item 108, docs/CONFORMANCE.md
    /// <c>DOC-A.1-108</c>).
    /// <para>The host's own words, and nothing else, exactly as <see cref="IsSharingRefusal"/> reads them:
    /// Windows reports <c>ERROR_HANDLE_DISK_FULL</c> (39), <c>ERROR_DISK_FULL</c> (112),
    /// <c>ERROR_FILE_TOO_LARGE</c> (223) and <c>ERROR_DISK_QUOTA_EXCEEDED</c> (1295) as the HRESULT
    /// <c>0x8007xxxx</c>; .NET on Unix surfaces the raw errno — <c>EFBIG</c> 27 and <c>ENOSPC</c> 28 on every
    /// Unix, <c>EDQUOT</c> 122 on Linux and 69 on the BSD family (macOS). The errno is a per-host fact, so it is
    /// asked HERE, once (kb/Work PB795).</para></summary>
    public static bool IsMediumBoundary(IOException failure) => failure.HResult switch
    {
        unchecked((int)0x80070027) or unchecked((int)0x80070070)
            or unchecked((int)0x800700DF) or unchecked((int)0x8007050F) => true,
        int errno when !OperatingSystem.IsWindows() => errno is UnixFileTooLarge or UnixNoSpace || errno == UnixQuotaExceeded,
        _ => false,
    };

    /// <summary><c>EFBIG</c> — 27 on Linux and on the BSD family.</summary>
    private const int UnixFileTooLarge = 27;

    /// <summary><c>ENOSPC</c> — 28 on Linux and on the BSD family.</summary>
    private const int UnixNoSpace = 28;

    /// <summary><c>EDQUOT</c> on this Unix host — 122 on Linux, 69 on the BSD family.</summary>
    private static readonly int UnixQuotaExceeded = OperatingSystem.IsLinux() || OperatingSystem.IsAndroid() ? 122 : 69;

    // ── The third question: what may OTHER handles do while this one is open? ───────────────────────────────

    /// <summary>A file connector's OWN long-lived READ or READ-WRITE stream on its physical file.
    /// <paramref name="share"/> is <see cref="FileConnector.HostShare"/> — the ISO §9.1.15 <b>file lock</b>,
    /// derived once by <see cref="FileLockPosture"/> from the connector's arbitrated sharing mode and the set of
    /// connectors Table 19 has already admitted on the physical file, and handed down by the registry
    /// immediately before the OPEN body runs.
    /// <para>⛔ THE POSTURE IS NOT DECIDED HERE, AND NOT AT A CALL SITE. It used to be a
    /// <c>sharedConnector ? ReadWrite : Read</c> ternary — one boolean standing for "this SELECT wrote a SHARING
    /// or LOCK MODE clause" — which answered §9.1.15 backwards in both directions: a
    /// <c>SHARING WITH NO OTHER</c> connector, the one the standard calls <i>exclusive</i>, got the MOST
    /// permissive handle, and two clause-less connectors Table 19 PERMITS to share one file were refused by the
    /// host and answered '30' (kb/Work PB740). <see cref="FileLockPosture"/> is where the rule lives now; this
    /// role only spends it.</para>
    /// <para>⛔ AND THE SAME POSTURE DECIDES WHETHER THE HANDLE MAY HOLD A BUFFER OF ITS OWN (kb/Work PB753).
    /// A handle whose file lock admits another writer is UNBUFFERED (<c>bufferSize: 1</c>), exactly as
    /// <see cref="OpenConnectorWriteStream"/>'s repositioning writer is and for the mirror-image reason. The
    /// connector above already keeps ONE buffer — the <see cref="StreamReader"/>'s — and invalidates it when a
    /// sibling releases a record (<c>SequentialConnector.EnsureReaderCoherent</c>), but it can only discard the
    /// buffer it owns: a second, lower buffer keeps the superseded bytes and hands them straight back, because
    /// <see cref="FileStream.Seek"/> reuses its read buffer whenever the target offset falls inside it. That was
    /// measured — with the invalidation in place and this handle still 4096-byte buffered, a sibling's REWRITE
    /// was still invisible. §14.9.30.4 GR21 c) selects <i>"the first existing record in the physical file"</i>;
    /// one managed buffer between the connector and the file is the most that leaves true.</para></summary>
    public static FileStream OpenConnectorStream(string hostPath, FileMode mode, FileAccess access,
        FileShare share, FileOptions options = FileOptions.None) =>
        new(hostPath, mode, access, share,
            FileLockPosture.AdmitsAnotherWriter(share) ? 1 : 4096, options);

    /// <summary>A SHORT-LIVED stream the runtime opens for its own bookkeeping over a host path — the write-base
    /// measurement of a shared <c>OPEN EXTEND</c>, the §14.9.27.4 GR10 store-header read, the varying framing's
    /// parse check. Every one of them runs BEFORE the connector's own handle for that OPEN exists, which is the
    /// property that lets this role be permissive without weakening anything: it is never the handle a COBOL
    /// statement is served through, and it never stands in for a §9.1.15 file lock.
    /// <para>⛔ A KEYED STORE'S WHOLE-FILE LOAD AND PERSIST ARE NOT ON THIS LIST ANY MORE (kb/Work PB771). They
    /// were, and that is exactly why the RELATIVE and INDEXED organizations established no file lock at all:
    /// the only handle they ever took was this one, so nothing said anything to another run unit between the
    /// OPEN and the CLOSE. They now travel through the connector's own long-lived
    /// <see cref="OpenConnectorStream"/> handle, which both organizations hold for the life of the OPEN.</para>
    /// It is <b>always</b> <see cref="FileShare.ReadWrite"/>, and the reason is not permissiveness for
    /// its own sake: the path may already be held open by a file connector OF THIS RUN UNIT, and a handle's
    /// share mode has to admit the access every outstanding handle already holds or the operating environment
    /// refuses it. A refusal here is a failure the COBOL statement never asked for — §9.1.15 and §14.9.27.4
    /// Table 19 had already ALLOWED the open, and the standard's only outcomes for an OPEN are an I-O status
    /// (§14.9.27.4 GR25 / §9.1.13.6) — so it must never be manufactured by the runtime's own second handle
    /// (kb/Work PB713). <see cref="FileOptions.SequentialScan"/> because every one of these is a single
    /// forward pass over the whole file.</summary>
    public static FileStream OpenAuxiliary(string hostPath, FileMode mode, FileAccess access) =>
        new(hostPath, mode, access, FileShare.ReadWrite, 4096, FileOptions.SequentialScan);

    /// <summary>A file connector's own long-lived WRITE stream — the third role, and the one .NET does not
    /// have. <c>FileMode.Append</c> is not the host's atomic append: it seeks to the end ONCE, at open, and
    /// every later write goes at that stream's own position (and <c>FileMode.Create</c> anchors at zero). That
    /// is right for the connector that holds the physical file to itself and WRONG the moment §9.1.15 lets a
    /// second connector write to the same file — both writers anchor at the same offset and the later one
    /// overwrites the earlier record, on '00' from both WRITEs (kb/Work PB739).
    /// <para>So the POSTURE decides, not the call site and not a clause. When
    /// <paramref name="share"/> does not admit another writer — <c>SHARING WITH NO OTHER</c>'s
    /// <see cref="FileShare.None"/>, <c>READ ONLY</c>'s and the undetermined implementor default's
    /// <see cref="FileShare.Read"/>, with no writing sibling admitted — this connector holds the only writable
    /// handle there is, so it gets the plain buffered stream it always had plus
    /// <see cref="FileOptions.SequentialScan"/>. When the posture DOES admit another writer (kb/Work PB740 made
    /// that a derived fact rather than a clause's side effect, so a clause-less pair the arbiter permits is
    /// covered too) it gets <see cref="SharedAppendStream"/>, which positions at the physical end before every
    /// write — the semantics §14.9.51.4 GR19 asks for, <i>"the added records follow the records present in the
    /// physical file when it was opened"</i>, read at the moment each record is added rather than once at the
    /// OPEN. Its handle is UNBUFFERED (<c>bufferSize: 1</c>): the connector already batches a whole logical
    /// record into its own writer and flushes it as one release (§14.9.51.4 GR12), and a second buffer between
    /// that release and the file is exactly the thing that hid the record from the other connector.</para>
    /// <para><paramref name="mode"/> is <see cref="FileMode.Append"/> for <c>OPEN EXTEND</c> and
    /// <see cref="FileMode.Create"/> for <c>OPEN OUTPUT</c> — Table 19 admits an incoming EXTEND against an
    /// existing connector open in the OUTPUT mode (its <c>extend I-O output</c> column group), so the OUTPUT
    /// writer needs the same choice the EXTEND writer does; a <c>Create</c> under
    /// <see cref="SharedAppendStream"/> truncates first and then appends from zero, which is the same
    /// sequence.</para></summary>
    public static Stream OpenConnectorWriteStream(string hostPath, FileMode mode, FileShare share) =>
        FileLockPosture.AdmitsAnotherWriter(share)
            ? new SharedAppendStream(new FileStream(hostPath, mode, FileAccess.Write,
                share, bufferSize: 1, FileOptions.None))
            : new FileStream(hostPath, mode, FileAccess.Write,
                share, 4096, FileOptions.SequentialScan);
}

/// <summary>
/// The append semantics the operating environment has and .NET's <c>FileMode.Append</c> does not: EVERY write
/// lands at the physical end of the file as it stands at that moment, not at an offset captured when the
/// handle was opened. It exists for ISO §14.9.51.4 GR19 — <i>"If two or more file connectors for a sequential
/// file add records by sharing the physical file after opening it in extend mode, the added records follow the
/// records present in the physical file when it was opened, but are otherwise in an undefined order"</i>: only
/// the ORDER between two sharing connectors is undefined, so neither may land on top of the other's record.
/// <para>Opened only through <see cref="HostFile.OpenConnectorWriteStream"/>, and only for a connector whose
/// §9.1.15 file lock admits another writer — one that holds the only writable handle there is cannot race
/// anything and does not pay the repositioning.</para>
/// </summary>
internal sealed class SharedAppendStream(FileStream inner) : Stream
{
    private readonly FileStream _inner = inner;

    /// <summary>⛔ THE WHOLE POINT. Reposition at the CURRENT end, then write: another connector's record may
    /// have arrived since this stream's last write, and §14.9.51.4 GR19 puts this record after it.</summary>
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        _inner.Seek(0, SeekOrigin.End);
        _inner.Write(buffer);
    }

    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count) =>
        Write(new ReadOnlySpan<byte>(buffer, offset, count));

    /// <inheritdoc/>
    public override void WriteByte(byte value) => Write(new ReadOnlySpan<byte>(in value));

    /// <inheritdoc/>
    public override void Flush() => _inner.Flush();
    /// <inheritdoc/>
    public override bool CanRead => false;
    /// <inheritdoc/>
    public override bool CanSeek => false;   // the position is not the caller's to choose — see Write
    /// <inheritdoc/>
    public override bool CanWrite => true;
    /// <inheritdoc/>
    public override long Length => _inner.Length;
    /// <inheritdoc/>
    public override long Position { get => _inner.Position; set => throw new NotSupportedException(); }
    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    /// <inheritdoc/>
    public override void SetLength(long value) => throw new NotSupportedException();
    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing) _inner.Dispose();
        base.Dispose(disposing);
    }
}
