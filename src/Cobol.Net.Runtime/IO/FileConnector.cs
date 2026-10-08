// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>
/// The shared control logic of every file-organization connector (DESIGN-runtime-library §2.2): the ISO §9.1.13
/// FILE STATUS register, the §14.9.30 GR21 / §14.9.35 GR5 read-position guard pair, the OPEN/CLOSE preamble with
/// its open-mode bookkeeping (§14.9.49.4 GR6b–e USE-declarative mode scoping), the OPTIONAL-file absence flag,
/// the RECORD IS VARYING bounds (§13.18.43 GR9/GR10), and the record-area fit/store shaping. Organization-
/// specific record SELECTION and the org-specific verb surfaces stay on the subclasses
/// (<see cref="SequentialConnector"/> · <see cref="RelativeConnector"/> · <see cref="IndexedConnector"/>) —
/// the base is the TRUE common denominator only; anything the organizations do differently remains theirs.
/// </summary>
public abstract class FileConnector
{
    protected FileConnector(string hostPath, int recordWidth, int varyMin, int varyMax)
    {
        HostPath = hostPath;
        RecordWidth = recordWidth < 1 ? 1 : recordWidth;
        VaryMin = varyMin;
        VaryMax = varyMax;
    }

    /// <summary>The resolved host path of the physical file this connector is ASSOCIATED with — the connector's
    /// half of ISO §12.4.5.3 GR3 ("The ASSIGN clause specifies the association of the file connector referenced by
    /// file-name-1 to a physical file identified by device-name-1, literal-1, or the content of the data item
    /// referenced by data-name-1"). Empty = NOT YET ASSOCIATED, which only a bare <c>ASSIGN USING data-name-1</c>
    /// file can be: it carries no device-name/literal, so nothing identifies a physical file until the first
    /// OPEN/SORT/MERGE runs <see cref="Associate"/>. The setter is private and <see cref="Associate"/> is its ONE
    /// caller — GR3's "the association occurs at the time of execution of an OPEN, SORT, or MERGE statement".</summary>
    public string HostPath { get; private set; }

    /// <summary>
    /// ISO §12.4.5.3 GR3, reached from §14.9.27.4 GR26 — establish the connector-to-physical-file association.
    /// <para>GR3's lead sentence fixes the TIMING: <i>"The association occurs at the time of execution of an OPEN,
    /// SORT, or MERGE statement that referenced file-name-1"</i>, and GR3 b) fixes the VALUE: <i>"When the USING
    /// phrase of the ASSIGN clause is specified, the file connector referenced by file-name-1 is associated with a
    /// physical file identified by the content of the data item referenced by data-name-1 in the runtime element
    /// that executes the OPEN, SORT, or MERGE statement."</i> So this runs at every OPEN/SORT/MERGE, never once at
    /// registration — §9.1.21 exists to let one connector reach different physical files during a run unit, and the
    /// standard's own concepts annex states the consequence (D.19.9.2 NOTE: <i>"The MOVE statements only have an
    /// effect on the dynamic assignment when a subsequent OPEN statement for the file connector is executed."</i>).</para>
    /// <para>⛔ THE ASSOCIATION VALUE IS AN ARGUMENT OF THE STATEMENT, NEVER STATE ON THIS OBJECT (kb/Work PB673).
    /// BOTH arms of GR3 name the element that runs the statement, not the one that registered the connector —
    /// a) <i>"identified by the specification of device-name-1 or the value of literal-1 <b>in the source unit that
    /// specifies</b> the OPEN, SORT, or MERGE statement"</i>, b) <i>"identified by the content of the data item
    /// referenced by data-name-1 <b>in the runtime element that executes</b> the OPEN, SORT, or MERGE
    /// statement"</i> — and one file connector may be described by MANY such elements: an EXTERNAL file connector
    /// is one object per run unit shared by every describing element (§13.18.22.4 GR4 a), whose file control
    /// entries §12.4.5.3 GR1 b requires only to be CONSISTENT (unlike GR1 i's FILE STATUS, which must name the
    /// same external item). So the emitter renders the EXECUTING element's own ASSIGN specification at the
    /// OPEN/SORT/MERGE site and passes it here; a connector-held source answered with the LAST installer's.</para>
    /// <para>One entry point for both arms, never a second mechanism: <paramref name="dynamic"/> selects GR3 b's
    /// content rules (the implementor's §12.4.5.3 GR4 determination, below) over GR3 a's plain literal.</para>
    /// <para>An OPEN of an ALREADY-OPEN connector is unsuccessful at §14.9.27.4 GR2's '41' and GR25's "the file is
    /// not affected", so the association is left standing: re-pointing an open connector would strand its streams
    /// and its physical-file-table registration on the old path.</para>
    /// <para>Returns null when the association stands, or the I-O status of a failed one. The allowable CONTENT of
    /// data-name-1 and the consistency rules are the implementor's (§12.4.5.3 GR4; Annex A.1 items 10 and 73) —
    /// WiseOwl COBOL's determination is in docs/CONFORMANCE.md §7 (DOC-A.1-73): the content with leading and trailing
    /// SPACES removed is the assign target and is then mapped to a host path exactly as literal-1 would be; content
    /// that is empty after that removal, or that carries a control character, names no physical file, so the
    /// association cannot be made and §9.1.13.6 item 2's '31' is the status the standard reserves for exactly this
    /// ("A permanent error exists during execution of an OPEN statement because the content of the data item
    /// referenced by the data-name specified in the USING phrase of the file control entry is not consistent with
    /// the specification for the device-name or literal in the ASSIGN clause of that file control entry").</para>
    /// </summary>
    /// <param name="spec">The EXECUTING element's ASSIGN specification: the content of data-name-1 when
    /// <paramref name="dynamic"/> (GR3 b), else the value of literal-1 / device-name-1 (GR3 a).</param>
    /// <param name="dynamic">True when the file control entry in the executing element writes the USING phrase.</param>
    public string? Associate(string spec, bool dynamic)
    {
        if (IsOpen) return null;                           // §14.9.27.4 GR2/GR25 — an already-open connector is untouched
        if (AssociationFailure(spec, dynamic, out string? hostPath) is { } failed) return failed;
        if (hostPath is not null) HostPath = hostPath;
        return null;
    }

    /// <summary>⛔ THE ONE RULE FOR WHETHER AN ASSIGN SPECIFICATION CAN BE ASSOCIATED (§12.4.5.3 GR3 / GR4; Annex A.1
    /// items 10 and 73, docs/CONFORMANCE.md DOC-A.1-73) — a pure function of the specification, so the OPEN
    /// (<see cref="Associate"/>, which then records the host path on its connector) and the SORT/MERGE statement's
    /// association of its OWN sort-merge file (<see cref="CobolSort.AssociationMade"/>, which has no connector to
    /// record it on — a sort-merge file is the in-memory sort store, §13.4.6; kb/Work PB1097) ask the same question
    /// and cannot answer it differently. Returns null when the association can be made, else the I-O status of the
    /// failure ('31'); <paramref name="hostPath"/> is the physical file the specification identifies, or null when
    /// it identifies none and the registration's stands (the bare <c>ASSIGN USING</c> placeholder).</summary>
    internal static string? AssociationFailure(string spec, bool dynamic, out string? hostPath)
    {
        hostPath = null;
        if (!dynamic)
        {
            // GR3 a) — literal-1/device-name-1 identify the physical file directly. The value is a source-text
            // constant, so re-resolving it at every OPEN is idempotent for the single-describer case and is what
            // makes the MULTI-describer case right: an EXTERNAL connector registered by one element must still be
            // associated with the file the EXECUTING element's own entry names. §12.4.5.2 SR4 already bars a
            // zero-length literal, so an empty spec here can only be the bare `ASSIGN USING` shape's placeholder —
            // it identifies nothing and leaves the registration's unassociated (empty) host path standing.
            if (spec.Length != 0) hostPath = CobolFile.ResolveHostPath(spec);
            return null;
        }
        string target = spec.Trim(' ');
        foreach (char ch in target)
            if (ch < ' ') return FileStatusCode.AssignNotConsistent;   // '31' §9.1.13.6 item 2
        if (target.Length == 0) return FileStatusCode.AssignNotConsistent;
        hostPath = CobolFile.ResolveHostPath(target);
        return null;
    }

    /// <summary>The record area's width in character positions.</summary>
    protected readonly int RecordWidth;

    /// <summary>The RECORD IS VARYING bounds (ISO §13.18.43 GR9/GR10); (-1,-1) = fixed-length records.</summary>
    protected readonly int VaryMin, VaryMax;

    /// <summary>True when the FD declares RECORD IS VARYING (the store then length-frames records).</summary>
    protected bool IsVarying => VaryMin >= 0;

    /// <summary>The latest I-O status (ISO §9.1.13). "00" until the first operation. ⛔ THE SETTER IS THE ONE
    /// I-O-STATUS ASSIGNMENT PATH, and it records that the connector was accessed (kb/Work PB63 / RV-15.28.4-2):
    /// §9.1.13.1 names CLOSE, DELETE, OPEN, READ, REWRITE, START, UNLOCK and WRITE as the statements that set the
    /// I-O status, and §15.28.4 r2a's "never been opened, attempted to be opened, or otherwise attempted to be
    /// accessed" is exactly "no statement has set it" — so a CLOSE or READ on a never-opened connector (status 42 /
    /// 47) IS an attempted access. <see cref="EverAccessed"/> used to be a flag written at two hand-picked sites
    /// (OPEN and DELETE FILE), which made every other verb wrong by default.</summary>
    public string Status
    {
        get => _status;
        protected set
        {
            _status = value;
            IoConditionName = null;   // a fresh operation: §9.1.13.1's correspondence answers unless one is named
            EverAccessed = true;
            // §9.1.13.7 3): the '43' DELETE/REWRITE gate holds only when the IMMEDIATELY-previous operation on
            // this connector was a successful READ. Every status assignment IS an operation's outcome
            // (§9.1.13.1's closed statement set — CLOSE, DELETE, OPEN, READ, REWRITE, START, UNLOCK, WRITE), so
            // the gate drops HERE, at the one chokepoint — and the READ terminals re-arm it AFTER their own
            // assignment. Four verbs leaked the gate when each entry cleared it by hand: UNLOCK, DELETE FILE,
            // the already-open '41' OPEN and the sharing '38'/'61' OPEN arms all reported '00' on a following
            // DELETE where the spec requires '43' (kb/Work PB140).
            PrevOpWasSuccessfulRead = false;
            // §9.1.13.1: a permanent error (an I-O status beginning '3', §9.1.13.6) "remains in effect for all
            // subsequent input-output operations on the file" — recorded HERE, at the one chokepoint every
            // operation's outcome passes, so no producer (a host failure '30', a boundary '34', the ODO READ's
            // '34') can forget to start it. Only an OPEN connector keeps one: a failed OPEN leaves the connector
            // closed, which already is the corrected state (docs/CONFORMANCE.md DOC-A.1-105).
            if (value.Length > 0 && value[0] == '3' && IsOpen) _permanentError = value;
        }
    }
    private string _status = FileStatusCode.Success;

    /// <summary>⛔ THE PERMANENT ERROR IN EFFECT (kb/Work PB1541; ISO §9.1.13.1: <i>"The permanent error condition
    /// remains in effect for all subsequent input-output operations on the file unless an implementor-defined
    /// technique is invoked to correct the permanent error condition"</i>; Annex A.1 item 105, which is optional and
    /// documented when provided — <c>docs/CONFORMANCE.md</c> DOC-A.1-105): the status that began it, or
    /// <see langword="null"/> when none is. The technique this processor provides is <b>CLOSE</b>: a completed CLOSE
    /// ends the condition (<see cref="Close"/>), so the next OPEN starts clean. Set only by the
    /// <see cref="Status"/> setter; read by <see cref="PermanentErrorReplay"/>.</summary>
    public string? PermanentErrorInEffect => _permanentError;
    private string? _permanentError;

    /// <summary>⛔ THE ONE REPLAY of a permanent error in effect — every input-output statement except CLOSE starts
    /// by asking it, through the preconditions each verb family already shares (<see cref="ReadOpenModeGuard"/>,
    /// <see cref="StartOpenModeGuard"/>, <see cref="MutationOpenModeGuard"/>, the WRITE entries' own first line,
    /// <see cref="Open"/>, and the registry's UNLOCK and DELETE FILE). The statement is unsuccessful, touches
    /// nothing, and sets the same I-O status again — which §9.1.13.1 also makes raise that status's exception
    /// condition again. Returns the status, or <see langword="null"/> when no permanent error is in effect. Assigns
    /// nothing: the caller owns the single <see cref="Status"/> assignment (the '43' gate drops exactly once).</summary>
    protected string? PermanentErrorReplay() => _permanentError;

    /// <summary>Set the I-O status directly (facade-level conditions: a locked-file OPEN, a REEL/UNIT CLOSE) — an
    /// attempted access like any other status assignment.</summary>
    public void SetStatus(string status) => Status = status;

    /// <summary>⛔ THE EC-I-O LEVEL-3 EXCEPTION-NAME A RULE NAMED FOR THE LAST I-O OPERATION ON THIS CONNECTOR,
    /// when one did; <see langword="null"/> when ISO §9.1.13.1's status→EC correspondence is the answer (kb/Work
    /// PB526).
    /// <para><b>Why a connector needs this at all.</b> §9.1.13.1 maps an I-O status's FIRST DIGIT to an
    /// exception-name, and that map covers every EC-I-O condition the standard reaches through a status. It does
    /// not cover EC-I-O-LINAGE: §13.18.34.4 GR6 b) 2 sets that condition to exist by NAME, and no status value
    /// corresponds to it — so an operation stopped by it has to report a status (§9.1.13.1: the value <i>"is set
    /// during the execution of a … WRITE statement"</i>, and §14.6.13.1.3 #3 routes a fatal EC-I-O through
    /// §9.1.13's rules, which are rules about the status) AND the name, because the status alone would raise the
    /// wrong condition. §14.6.13.1.1 licenses the override: <i>"Unless otherwise specified, if more than one
    /// exception is detected during the execution of a statement, the one that is set to exist is undefined"</i> —
    /// GR6 b) 2 is the "otherwise specified".</para>
    /// <para>⛔ IT IS PER-OPERATION, NOT PER-CONNECTOR STATE: the <see cref="Status"/> setter — the ONE I-O-status
    /// assignment path — clears it, so a named condition cannot outlive the operation that named it and be read
    /// by the next verb's hook. Set it with <see cref="SetIoCondition"/>, never by assigning the two
    /// separately.</para></summary>
    public string? IoConditionName { get; private set; }

    /// <summary>Report an operation whose exception condition a rule NAMES rather than leaving to §9.1.13.1's
    /// status→EC correspondence — an unsuccessful one (§13.18.34.4 GR6 b) 2's EC-I-O-LINAGE) or a SUCCESSFUL one
    /// (§14.9.51.4 GR27 a)'s EC-I-O-EOP / EC-I-O-EOP-OVERFLOW, which the correspondence cannot reach because a
    /// clean '00' corresponds to no condition at all): the status and the name are set together and returned, so the two
    /// cannot be assigned out of order (the <see cref="Status"/> setter clears the name).</summary>
    protected string SetIoCondition(string status, string ecName)
    {
        Status = status;
        IoConditionName = ecName;
        return status;
    }

    /// <summary>The connector has been opened, attempted to be opened, or otherwise attempted to be accessed (ISO
    /// §15.28.4 r2a / §15.29.4 r2a) — FUNCTION EXCEPTION-FILE(connector) returns two spaces until this is true.
    /// Set by the <see cref="Status"/> setter, i.e. by EVERY statement that assigns an I-O status; a never-touched
    /// SELECTed connector stays false.</summary>
    public bool EverAccessed { get; private set; }

    /// <summary>The file-name exactly as spelled in the SELECT clause (ISO §15.28.4 r1c/r2b, §15.29.4 r1c/r2b —
    /// "the file-name exactly as specified in the SELECT clause"), carried from the compiler's FileModel at
    /// registration (kb/Work PB63); the registry KEY is an emit-side namespace and is never displayed.</summary>
    public string SelectName { get; set; } = "";

    /// <summary>True for a SELECT OPTIONAL file (ISO §14.9.27 GR13/GR17).</summary>
    public bool IsOptional { get; set; }

    /// <summary>⛔ THE ISO EDITION THE PROGRAM THAT SELECTED THIS FILE WAS COMPILED FOR (85 / 2002 / 2014 /
    /// 2023 — the CLI's <c>--std</c>), carried from the compiler at registration exactly as
    /// <see cref="SelectName"/> and <see cref="IsOptional"/> are (kb/Work PB344). It is the argument every
    /// runtime-side per-edition behaviour gate passes to
    /// <see cref="DialectBehaviors.IsActive(DialectBehavior, int)"/> — the ONE place a rule that the standard
    /// CHANGED is written down.
    /// <para>PER CONNECTOR, not per process and not per registry: a run unit may link programs from separate
    /// compilations, and the rule that governs a file's records is the one the source element that DESCRIBED
    /// the file was compiled under — the same reasoning that makes <c>SignEncoding</c> a property of the
    /// compiled program. It defaults to the newest edition, which is also the CLI's default target, so a
    /// connector constructed outside a registration (a focused runtime unit test) behaves as
    /// <c>--std 2023</c>.</para></summary>
    public int Edition { get; set; } = 2023;

    /// <summary>The registry's per-physical-file record-store table (kb/Work PB143 — §14.9.10.4 GR5's "removed
    /// from the physical file"): the keyed connectors attach their record images through it so every connector
    /// over one host path sees ONE store. Null (a connector constructed outside a registry, e.g. a focused
    /// unit test) keeps the private-store behavior.</summary>
    internal KeyedStoreTable? SharedStores { get; set; }

    /// <summary>The live state of the PHYSICAL FILE this connector is associated with — the ONE object that
    /// stands for the medium, held by EVERY connector opened through a registry, whatever its SELECT declared.
    /// <para>⛔ EVERY connector, because the questions answered off it are not all record-locking questions
    /// (kb/Work PB753). Table 19 arbitrates every open over this state already (<c>st.Open[name]</c> records
    /// every successful one, kb/Work PB321), and since kb/Work PB740 two CLAUSE-LESS connectors may share one
    /// physical file — so "this SELECT wrote a SHARING or LOCK MODE clause" cannot gate a rule about what the
    /// medium holds. <see cref="PhysicalFileTable.State.ReleaseGeneration"/> is such a rule: §14.9.30.4 GR21 c)
    /// selects <i>"the first existing record in the physical file"</i> regardless of any clause.</para></summary>
    internal PhysicalFileTable.State? Physical { get; private set; }

    /// <summary>The §9.1.16 RECORD-LOCKING view of <see cref="Physical"/> — non-null only for a connector whose
    /// SELECT registered a SHARING or LOCK MODE clause (or whose OPEN carried a SHARING/RETRY phrase). It is
    /// what the release-ordinal mint a shared <c>OPEN EXTEND</c> numbers its appended records from reads
    /// (kb/Work PB739), and the gate is the record-locking one because the ordinal IS the lock identity:
    /// §12.4.5.9.4 GR1 b) 2. leaves a clause-less connector's record locking to the implementor and this
    /// compiler's determination is <see cref="FileLockMode.None"/> (kb/Work PB740), so there is no identity to
    /// mint and no whole-file measurement to pay for.
    /// <para>⛔ ONE FACT, ONE FIELD, AND NOW ONE DERIVATION. Participation used to be a separate <c>bool</c>
    /// beside the registry's own posture map, which is two independently maintained answers to one question
    /// (kb/Work PB713 removed the second writer); it is a computed VIEW of <see cref="Physical"/> and the flag
    /// <see cref="AssociatePhysical"/> sets alongside it, so a connector that "participates" but holds no
    /// shared state still cannot be expressed.</para></summary>
    internal PhysicalFileTable.State? SharedPhysical => _locksRecords ? Physical : null;

    private bool _locksRecords;

    /// <summary>⛔ THE ONE WRITER OF BOTH FACTS ABOVE, so they cannot fall out of step: the registry calls it
    /// in exactly one place, immediately before the connector's OPEN body runs (the kb/Work PB713 ordering —
    /// <c>OpenCore</c> reads them, so assigning afterwards would open every stream on the PREVIOUS open's
    /// answer).</summary>
    /// <param name="physical">The physical file's shared state — every connector gets it.</param>
    /// <param name="locksRecords">Whether this connector registered a §9.1.15/§9.1.16 posture.</param>
    internal void AssociatePhysical(PhysicalFileTable.State physical, bool locksRecords)
    {
        Physical = physical;
        _locksRecords = locksRecords;
    }

    /// <summary>The ISO §9.1.15 <b>file lock</b> this connector's own host handles carry — <i>"The successful
    /// opening of a file establishes a file lock for the applicable sharing rules, thereby preventing other run
    /// units from opening that file with incompatible sharing rules."</i> DERIVED, in one place, by
    /// <see cref="FileLockPosture.OfSharingMode"/> from the connector's arbitrated sharing mode; the registry
    /// assigns it immediately before the OPEN body runs and nothing re-assigns it while the connector is open
    /// (Table 19 admits a pair only when each side's posture already admits the other's access, kb/Work PB322).
    /// <para>⛔ IT REPLACED A BOOLEAN THAT ANSWERED THE WRONG QUESTION (kb/Work PB740). <c>SharedStreams</c> —
    /// "did this SELECT write a SHARING or LOCK MODE clause" — chose between <see cref="FileShare.ReadWrite"/>
    /// and <see cref="FileShare.Read"/>, and got §9.1.15 backwards in both directions: <c>SHARING WITH NO
    /// OTHER</c>, which §9.1.15 1) calls <i>exclusive access</i>, took the permissive arm (an external process
    /// appended to a file the program had declared exclusive, measured across processes), while two clause-less
    /// connectors Table 19 PERMITS to share one file took the restrictive one and the second OPEN answered '30'
    /// — a status no Table 19 row and no §9.1.13.9 item produces.</para></summary>
    internal FileShare HostShare => FileLockPosture.OfSharingMode(HostSharing);

    /// <summary>The sharing mode this connector's current OPEN is arbitrated under (§9.1.15: the OPEN's SHARING
    /// phrase, else the file control entry's clause, else the implementor default of the open mode) — the ONE
    /// fact both halves of the file lock derive from: the host share mode of the connector's own handle
    /// (<see cref="HostShare"/>) and the cross-run-unit region lock (<see cref="RunUnitFileLock"/>). The registry
    /// assigns it immediately before the OPEN body runs; outside a registry nothing has arbitrated, so it is the
    /// READ ONLY posture the .NET path constructors gave.</summary>
    internal FileSharing HostSharing { get; set; } = FileSharing.ReadOnly;

    /// <summary>The §9.1.15 file lock this connector holds against other RUN UNITS while it is open — the part
    /// of the lock a host's <see cref="FileShare"/> cannot say (<see cref="RunUnitFileLock"/>, kb/Work PB833);
    /// null where the host cannot hold it and the share mode of the connector's own handle is all there is.</summary>
    private RunUnitFileLock? _runUnitLock;

    /// <summary>Establish the cross-run-unit file lock of the OPEN in <paramref name="mode"/>. Returns the
    /// failing I-O status — §9.1.13.9 1)'s '61', <i>"that physical file is already open by another file
    /// connector in a manner that conflicts with this request"</i> — or null when the lock is held or the host
    /// cannot hold one.
    /// <para>⛔ OUTPUT ASKS A HOST WHOSE SHARE MODES ARE THE LOCK FIRST (<see cref="HostFile.ShareModesAreMandatory"/>).
    /// Table 19's OUTPUT rows are unsuccessful against every column (§9.1.13.9 1) e)), but an OUTPUT open
    /// <i>creates</i> the file — it truncates what is there — and a host share mode only refuses an open whose ACCESS
    /// it excludes, so a handle that admits a writer admits the truncation: measured on Windows, an <c>OPEN
    /// OUTPUT</c> in a second run unit answered '00' against a file the first held open <c>SHARING WITH ALL
    /// OTHER</c>, and emptied it. A <see cref="FileShare.None"/> request (<see cref="HostFile.IsHeldByAnother"/>)
    /// is refused by every outstanding handle there is, which is exactly the OUTPUT rows' meaning, and it is made
    /// BEFORE anything is created (§14.9.27.4 GR25, <i>"If the execution of the OPEN statement is unsuccessful, the
    /// file is not affected"</i>). A host without mandatory share modes has the region lock instead, which
    /// refuses an OUTPUT like every other cell of the table and asks a REGULAR file only — the probe is a read
    /// open, which on a FIFO waits for a writer that is this program's own next statement.</para></summary>
    /// <param name="mode">The open mode being established.</param>
    /// <param name="existedBeforeBody">True before <see cref="OpenCore"/> on a file that is present; false after
    /// the body created it, when the connector's own handle would make the OUTPUT question answer yes.</param>
    private string? TakeRunUnitFileLock(FileOpenMode mode, bool existedBeforeBody)
    {
        if (existedBeforeBody && mode == FileOpenMode.Output && HostFile.ShareModesAreMandatory && HostFile.IsHeldByAnother(HostPath))
            return FileStatusCode.FileSharingConflict;
        return RunUnitFileLock.Take(HostPath, HostSharing, mode, out _runUnitLock) == RunUnitFileLock.Outcome.Refused
            ? FileStatusCode.FileSharingConflict
            : null;
    }

    private void ReleaseRunUnitFileLock()
    {
        _runUnitLock?.Dispose();
        _runUnitLock = null;
    }

    /// <summary>The ACCESS this connector's own long-lived host handle takes of the physical file in
    /// <paramref name="mode"/> — what a SIBLING connector's §9.1.15 file lock has to admit for this connector to
    /// open at all (FileLockPostureDriftTests measures that every Table-19-admitted pair does).
    /// <para>The default is the open mode's floor (<see cref="FileLockPosture.AccessOf"/>): INPUT reads, OUTPUT
    /// and EXTEND write, I-O does both. ⛔ AN ORGANIZATION THAT NEEDS MORE OF THE PHYSICAL FILE THAN ITS OPEN
    /// MODE IMPLIES SAYS SO HERE (kb/Work PB771), because the host cannot guess it: a format that is
    /// rewritten WHOLE — the keyed organizations' framed store — must read the existing records in every
    /// writable mode before it can write them back, so its EXTEND handle asks for
    /// <see cref="FileAccess.ReadWrite"/>. A handle opened with the mode-derived guess would admit only the
    /// write and the host would refuse the read, which is kb/Work PB713's '30' arriving by another route.</para>
    /// </summary>
    internal virtual FileAccess HostAccess(FileOpenMode mode) => FileLockPosture.AccessOf(mode);


    /// <summary>Release whatever host handle this connector's <see cref="OpenCore"/> took before the OPEN turned
    /// out to be UNSUCCESSFUL — §9.1.15 establishes a file lock on <i>"The SUCCESSFUL opening of a file"</i> and
    /// §14.9.27.4 GR25 leaves an unsuccessful one with <i>"the file … not affected"</i>, so an unsuccessful OPEN
    /// shall leave no handle behind at all.
    /// <para>⛔ THE CLOSE CANNOT DO IT, which is why this hook exists rather than a comment asking each
    /// organization to be careful: <see cref="Close"/> answers '42' and never reaches <see cref="CloseCore"/>
    /// for a connector that is not open (§9.1.13.7 item 2), so a handle taken by a body that then failed or
    /// threw would be held until the process ended — a file lock against other run units that no COBOL
    /// statement could remove. The base does nothing because a connector that takes no handle has nothing to
    /// release; every organization that takes one overrides it, and the override is the same code its
    /// <see cref="CloseCore"/> already runs.</para></summary>
    protected virtual void AbandonOpen() { }

    /// <summary>True while the connector is in an open mode (ISO §9.1.4): set by a success-family OPEN, cleared
    /// by ANY completed CLOSE (§14.9.6.4 GR8 — and an unsuccessful close does not keep the open mode either).
    /// ⛔ ONE bit with ONE job (kb/Work PB140): the FPI's "optional file not present" state lives on
    /// <see cref="OptionalAbsent"/> and is NOT part of openness — folding it in forced every CloseCore to
    /// mutate the file position indicator §14.9.6.4 GR6 says a CLOSE leaves unchanged.</summary>
    public bool IsOpen => _openMode;
    private bool _openMode;

    /// <summary>The current open mode (meaningful while <see cref="ModeKnown"/>).</summary>
    protected FileOpenMode Mode;

    /// <summary>True while the file is open OR a failed OPEN recorded its attempted mode (ISO §14.9.49.4 GR6b —
    /// "in the process of being opened"); cleared by a successful CLOSE (§9.1.4 — the file is then in no mode).</summary>
    protected bool ModeKnown;

    /// <summary>The open-mode view for USE-declarative mode scoping (ISO §14.9.49.4 GR6b–e): <c>(int)</c> of the
    /// mode while open OR the ATTEMPTED mode of a failed OPEN; −1 after a successful CLOSE / before any OPEN.</summary>
    public int OpenModeView => ModeKnown ? (int)Mode : -1;

    /// <summary>The open mode while the connector IS OPEN (ISO §9.1.4), null otherwise.
    /// ⛔ DELIBERATELY NOT <see cref="OpenModeView"/>: that view also answers with the ATTEMPTED mode of a
    /// FAILED open (§14.9.49.4 GR6b's "in the process of being opened"), which is what USE-declarative mode
    /// scoping needs and is WRONG for any rule that asks whether the connector IS OPEN in a given mode.
    /// §14.9.21.4 GR3 is such a rule -- "the INITIATE statement may be executed only if the corresponding
    /// file connector is open in the extend mode or the output mode" -- and after an UNSUCCESSFUL OPEN OUTPUT the
    /// GR6b view still answers "output", which would let an INITIATE proceed into a connector that is not open.</summary>
    public FileOpenMode? OpenModeIfOpen => _openMode ? Mode : null;

    /// <summary>OPEN INPUT/I-O of an absent OPTIONAL file: the connector is open but no physical store exists
    /// (positioned at EOF / "not present", ISO §14.9.27 GR13; §9.1.13.2 item 5).</summary>
    protected bool OptionalAbsent;

    // Read-position state (ISO §14.9.30 GR21 / §14.9.35 GR5): a sequential READ after an unsuccessful READ with
    // no reposition is itself unsuccessful ('46'); a sequential-access REWRITE/DELETE requires the immediately-
    // previous op be a successful READ (else '43'). The '43' gate drops in the Status SETTER (every outcome is
    // an operation; the READ terminals re-arm it after assigning) — kb/Work PB140's chokepoint. The '46' poison
    // is NOT setter-managed: only a reposition (OPEN / START / a successful READ) clears it — a successful
    // intervening WRITE establishes no next record, so it must survive other verbs' status assignments.

    /// <summary>True after an unsuccessful READ/START with no reposition since — the next sequential READ is '46'.</summary>
    protected bool LastReadUnsuccessful;

    /// <summary>True when the immediately-previous operation was a successful READ (the '43' gate).</summary>
    protected bool PrevOpWasSuccessfulRead;

    /// <summary>A successful READ's terminal: assign the status (the setter drops the '43' gate like every
    /// other outcome) and then RE-ARM it — the ONE place a read outcome may do so (§9.1.13.7 3)). A successful
    /// READ is also a reposition, so the '46' poison clears (§14.9.30 GR21).</summary>
    protected string ReadSucceeded(string status)
    {
        Status = status;
        PrevOpWasSuccessfulRead = true;
        LastReadUnsuccessful = false;
        return status;
    }

    /// <summary>The file position indicator's "optional file not present" state (ISO §9.1.13.2 item 5 /
    /// §14.9.6.4 GR6) — exposed for the registry's REEL/UNIT surface, which must perform NO unit processing
    /// for an absent optional file.</summary>
    internal bool OptionalNotPresent => OptionalAbsent;

    // ── READ preconditions (ISO §14.9.30.4) ─────────────────────────────────────────────────────────────────
    //
    // ⛔ ONE COPY OF EACH READ PRECONDITION, FOR ALL FIVE READ ENTRIES (kb/Work PB336). The open-mode guard,
    // the '46' poison and the absent-OPTIONAL arm are ALL-FORMATS rules of the READ statement, so they belong
    // to the connector base and not to each organization. Written out per connector they were three copies of
    // one rule (SequentialConnector.Read · RelativeConnector.ReadSequential · IndexedConnector.ReadSequential),
    // and all three carried the SAME ordering defect: the absent-OPTIONAL arm was tested BEFORE GR21's '46',
    // so every sequential READ after the first on an absent optional file re-reported '10' — although the arm
    // itself had already armed the '46' the next READ owed. Order here is the rules' own order.

    /// <summary>The open-mode precondition every READ shares (ISO §14.9.30.4 GR2: "The open mode of the file
    /// connector referenced by file-name-1 shall be input or I-O. If it is any other value, the execution of the
    /// READ statement is unsuccessful and the I-O status value for file-name-1 is set to '47'."; §9.1.13.7 item
    /// 7). Returns the failing status, or <see langword="null"/> when the READ may proceed. Assigns nothing —
    /// the caller owns the single <see cref="Status"/> assignment, so the '43' gate drops exactly once.</summary>
    protected string? ReadOpenModeGuard() =>
        PermanentErrorReplay()   // §9.1.13.1 — a permanent error in effect answers first (kb/Work PB1541)
        ?? (!IsOpen || Mode is FileOpenMode.Output or FileOpenMode.Extend ? FileStatusCode.ReadNotOpenForInput : null);

    /// <summary>The preconditions of a SEQUENTIAL READ, in the standard's own order: GR2's open mode ('47');
    /// then ISO §14.9.30.4 GR21's first sentence — "For a sequential READ statement, if the previous READ or
    /// START statement for the file connector was unsuccessful, then the READ statement is unsuccessful and the
    /// I-O status is set to '46'" (§9.1.13.7 item 6) — then the absent-OPTIONAL arm, which §9.1.13.4 item 1 c)
    /// scopes to "a sequential READ statement … attempted FOR THE FIRST TIME on a file described as optional and
    /// the physical file is not present" ('10'). GR21 shall be tested FIRST: it is what makes '10' first-time-
    /// only, because the '10' arm is itself an unsuccessful READ and arms the poison for its successor.
    /// Returns the failing status, or <see langword="null"/> when the READ may proceed.</summary>
    protected string? SequentialReadGuard()
    {
        if (ReadOpenModeGuard() is { } notOpen) return notOpen;                     // '47' GR2
        if (LastReadUnsuccessful) return FileStatusCode.NoValidNextRecord;          // '46' GR21 / §9.1.13.7 6
        // This READ is itself unsuccessful, so it arms GR21 for its successor — that is what makes '10'
        // first-time-only, and why the two arms shall be written in this order.
        if (OptionalAbsent) { LastReadUnsuccessful = true; return FileStatusCode.AtEnd; }   // '10' §9.1.13.4 1 c)
        return null;
    }

    /// <summary>The same three preconditions <see cref="SequentialReadGuard"/> tests, asked WITHOUT its side
    /// effect — a sequential READ executed now would reach the physical-retrieval stage. It is what a
    /// <see cref="PeekSequentialRecordId"/> shall consult, so that a mode/position failure keeps its own status
    /// ('47'/'46'/'10') instead of being pre-empted by a '51' the governed read would otherwise report for a
    /// record the failing READ was never going to touch. Each organization ANDs its own remaining precondition
    /// (a live stream; a valid file position indicator).</summary>
    protected bool SequentialReadReachesRetrieval =>
        ReadOpenModeGuard() is null && !LastReadUnsuccessful && !OptionalAbsent;

    // ── The pre-read record identity (ISO §14.9.30.4 GR9/GR10 a)) ───────────────────────────────────────────
    //
    // ⛔ EVERY ORGANIZATION CAN NAME THE RECORD ITS NEXT READ WOULD MAKE AVAILABLE, BEFORE THE READ COMMITS
    // (kb/Work PB338). GR9's record-operation-conflict check is asked of "the record identified for access",
    // and GR10 a) — "The file position indicator is unchanged" — can only hold if that check runs BEFORE the
    // position moves. §14.9.30.4 GR13 a) says the same thing from the other side: the I-O status is updated
    // "and, IF the record operation conflict condition did not occur, the file position indicator is set."
    // These two virtuals are how the registry asks each organization, so the governed read's ordering is
    // structural instead of a per-arm convention. Both answer "" for "no record is identified", which is
    // every case where the read would fail before it could touch one; the governed entry then skips the
    // conflict check and lets the physical step report its own status.

    /// <summary>The lock identity (§9.1.16) of the record a FORMAT-1 sequential READ in this direction would
    /// make available — computed by running the organization's own §14.9.30.4 GR21 selection WITHOUT committing
    /// the file position indicator. "" when no record is identified.</summary>
    public virtual string PeekSequentialRecordId(bool previous) { _ = previous; return ""; }

    /// <summary>The lock identity (§9.1.16) of the record a FORMAT-2 random READ would make available —
    /// §14.9.30.4 GR29's relative record number, or GR32's first record matching the key of reference —
    /// computed WITHOUT establishing the key of reference (GR10 d): <i>"The key of reference for indexed files
    /// is unchanged"</i>. "" when no such record exists, which is the invalid-key condition and carries no lock.
    /// <paramref name="recordImage"/> supplies the key slice for an indexed read (GR32);
    /// <paramref name="recordExtents"/> is that record area's EXTENT TABLE when it is a variable-length group
    /// (determination D-FRA (v); kb/Work PB1053) — what locates a key its variable-length members precede.</summary>
    public virtual string PeekRandomReadRecordId(int keyIndex, string recordImage, RecordExtents? recordExtents)
    { _ = keyIndex; _ = recordImage; _ = recordExtents; return ""; }

    /// <summary>ISO §14.9.30.4 GR18 — <i>"Unless otherwise specified, at the completion of any unsuccessful
    /// execution of a READ statement, the content of the associated record area is undefined, the key of
    /// reference is undefined for indexed files, and the file position indicator is set to indicate that no
    /// valid record position has been established."</i> The registry applies it to the ONE unsuccessful READ it
    /// decides itself and the connector never sees — the §12.4.5.9.4 GR7 record-lock-limit denial ('53'/'54'),
    /// which is refused BEFORE the physical retrieval and so leaves a position no connector step invalidated.
    /// <para>⛔ NOT the record operation conflict condition: GR10 a)/d) are GR18's "otherwise specified" and
    /// require the indicator and the key of reference to be UNCHANGED, which under the pre-read ordering costs
    /// no action at all.</para></summary>
    internal void ApplyUnsuccessfulReadPosition() => InvalidateFilePosition();

    // ── START preconditions (ISO §14.9.41.4) ────────────────────────────────────────────────────────────────
    //
    // ⛔ ONE COPY, FOR ALL THREE ORGANIZATIONS, exactly as the READ preconditions above (kb/Work PB336's
    // lesson, applied to START by PB352). GR1's open-mode test was written out five times — twice in each keyed
    // connector and once more when the sequential arm landed — and every copy carried the SAME omission: it
    // returned '47' WITHOUT invalidating the file position indicator, although §14.9.41.4 GR7 applies to EVERY
    // unsuccessful START and a '47' START is unsuccessful.

    /// <summary>ISO §14.9.41.4 GR7 — <i>"Following the unsuccessful execution of a START statement, the file
    /// position indicator is set to indicate that no valid record position has been established."</i> On this
    /// base that state is the '46' poison §9.1.13.7 item 6 a) reads back on the next sequential READ; a keyed
    /// connector, whose FPI is a key VALUE plus a validity bit, overrides to clear the bit as well.</summary>
    protected virtual void InvalidateFilePosition() => LastReadUnsuccessful = true;

    /// <summary>The START statement's open-mode precondition (ISO §14.9.41.4 GR1: <i>"The open mode of the file
    /// connector referenced by file-name-1 shall be input or I-O."</i>; §9.1.13.7 item 7 — <i>"The execution of a
    /// READ or START statement is attempted referencing a file connector that is not open in the input or I-O
    /// mode"</i> → '47'; Table 20's blank Output/Extend cells). Applies GR7 on the way out, because the refused
    /// START is an unsuccessful one. Returns the failing status, or <see langword="null"/> when the START may
    /// proceed. Assigns nothing — the caller owns the single <see cref="Status"/> assignment, so the '43' gate
    /// drops exactly once.</summary>
    protected string? StartOpenModeGuard()
    {
        if (PermanentErrorReplay() is { } stuck)   // §9.1.13.1 — still an unsuccessful START, so GR7 applies (kb/Work PB1541)
        {
            InvalidateFilePosition();
            return stuck;
        }
        if (IsOpen && Mode is not (FileOpenMode.Output or FileOpenMode.Extend)) return null;
        InvalidateFilePosition();
        return FileStatusCode.ReadNotOpenForInput;
    }

    /// <summary>The absent-OPTIONAL arm of a RANDOM (format-2) READ — ISO §9.1.13.5 item 3 b), "a START or
    /// random READ statement is attempted on a file described as optional and the physical file is not present"
    /// → '23'. TWO deliberate differences from <see cref="SequentialReadGuard"/>, both read off the rule texts:
    /// §14.9.30.4 GR21 opens "For a sequential READ statement", so a random READ never yields '46'; and 3 b)
    /// carries no "first time" qualifier, so an absent optional file answers '23' to EVERY random READ. The
    /// unsuccessful read still arms the poison — §9.1.13.7 item 6 b) ("The preceding READ statement referencing
    /// that file connector was unsuccessful") is not restricted to sequential reads, so the next SEQUENTIAL READ
    /// on a dynamic-access connector is GR21's '46'. Kept separate from <see cref="ReadOpenModeGuard"/> because
    /// the indexed connector establishes its key of reference (GR30/GR31) between the two.</summary>
    protected string? RandomReadAbsentOptionalGuard()
    {
        if (OptionalAbsent) { LastReadUnsuccessful = true; return FileStatusCode.RecordNotFound; }   // '23' 3 b)
        return null;
    }

    /// <summary>The length of the most recently read record (ISO §13.18.43 GR15 — the frame length on a varying
    /// file, the record width on a fixed one; the RECORD VARYING DEPENDING item receives it after a READ).
    /// Written only through <see cref="NoteRecordRead"/>, so it can never disagree with
    /// <see cref="CurrentRecord"/>.</summary>
    public int LastReadLength { get; private set; }

    /// <summary>⛔ THE CURRENT RECORD — the record the most recent successful READ made available, at its OWN
    /// length (never fitted to the record area): §13.18.43.4 GR16's "number of bytes in the current record".
    /// The record area's character image is this record fitted to <see cref="RecordWidth"/>; an OUT-OF-LINE
    /// record of the area (a dynamic-length record or a variable-length group — docs/CONFORMANCE.md §7
    /// determination D-FRA, kb/Work PB981) has no window over that image and receives THIS instead, so its
    /// dynamic members are not truncated to the character area nor padded with the area's filler.</summary>
    public string CurrentRecord { get; private set; } = "";

    /// <summary>The ONE writer of <see cref="LastReadLength"/> and <see cref="CurrentRecord"/>: every
    /// organization's successful READ reports the record it made available here.</summary>
    protected void NoteRecordRead(string record, RecordExtents? extents)
    {
        CurrentRecord = record;
        CurrentRecordExtents = extents;
        LastReadLength = record.Length;
    }

    /// <summary>The EXTENT TABLE the current record was sent with (determination D-FRA (v); kb/Work PB1053) —
    /// where each variable-length component of the variable-length group that wrote it ended — or null when the
    /// record carries none (it was written through a record with no variable-length components, or the
    /// organization frames no table — a LINE SEQUENTIAL line, a fixed-length block). An out-of-line
    /// variable-length group record decomposes <see cref="CurrentRecord"/> by it. Written only through
    /// <see cref="NoteRecordRead"/>, beside the record it describes.</summary>
    public RecordExtents? CurrentRecordExtents { get; private set; }

    /// <summary>The maximum size specified by the record description entries, when it EXCEEDS the record area's
    /// character width — ISO §14.9.30.4 GR14/GR15's truncation bound ("the record is truncated on the right to
    /// the maximum size"). Zero (the default) means <see cref="RecordWidth"/>, which is the bound for every file
    /// whose records all have a character window; only a variable-length record (D-FRA, kb/Work PB981) reaches
    /// past it.</summary>
    public int RecordMax { get; init; }

    /// <summary>GR14/GR15's "maximum size specified by the record description entries".</summary>
    protected int MaxRecordSize => RecordMax > RecordWidth ? RecordMax : RecordWidth;

    // ── Record-lock identity (ISO §9.1.16 — record locking applies to EVERY organization; the READ/REWRITE/
    //    WRITE/DELETE lock rules §14.9.30 GR7–GR12 / §14.9.35 GR11–GR12 / §14.9.51 GR10–GR11 / §14.9.10 GR6–GR7
    //    are ALL-FORMATS rules). Each organization names its records for the PhysicalFileTable: the RRN for
    //    relative, the prime record key for indexed, the record's 1-based ordinal position for sequential (the
    //    natural analogue — a relative store's frames ARE ordinals). Empty string = no identified record.

    /// <summary>The lock identity of the record most recently made available by a READ (§9.1.16 — the record a
    /// post-read lock acquisition targets; §14.9.30 GR11c/d).</summary>
    public virtual string LastReadRecordId => "";

    /// <summary>The handle through which this connector's record locks are PUBLISHED to other run units
    /// (<see cref="HostRegionLocks"/>, kb/Work PB2660) — §9.1.16, <i>"While locked by a given file connector, a
    /// record is not accessible to another file connector in the same or a different run unit"</i>. Null for a
    /// connector whose locks only its own run unit can see; <see cref="PhysicalFileTable"/> then keeps them in its
    /// table alone.</summary>
    internal virtual Microsoft.Win32.SafeHandles.SafeFileHandle? RecordLockHandle => null;

    /// <summary>Whether <see cref="RecordLockHandle"/> is open for writing, so it can hold an EXCLUSIVE lock
    /// (<see cref="HostRegionLocks.Hold"/>).</summary>
    internal virtual bool RecordLockHandleWritable => false;

    /// <summary>Enter one record statement's hold on the connector's store — see <see cref="StoreStatement"/>.
    /// Inert for every organization but the keyed ones.</summary>
    internal virtual StoreStatement BeginStatement() => default;

    /// <summary>Run a RETRY phrase's wait for a holder in ANOTHER run unit (§14.7.9.3) outside this connector's
    /// store statement, so that run unit can execute the statement that gives the lock back (kb/Work PB2660).</summary>
    internal virtual void WaitOutsideStatement(Action wait) => wait();

    /// <summary>Whether THIS connector already held a record lock on <see cref="LastReadRecordId"/> when the governed READ
    /// that made it available began its own §14.9.30.4 GR11 lock actions — null when that READ ran none (no record
    /// locking governs the file, or the record has no identity). A READ whose record then proves unsuccessful AFTER
    /// the lock actions (§9.1.13.6 item 4 b)'s '34', <c>FileRegistry.ReadExceedsRecordMaximum</c>) puts the record's
    /// lock back exactly as it was: GR11 b)/c)/d) speak of the SUCCESSFUL execution, and an earlier READ WITH LOCK's lock
    /// under multiple record locking is not this READ's to give up.</summary>
    internal bool? RecordLockHeldBeforeRead { get; set; }

    /// <summary>The lock identity of the record a REWRITE/DELETE executed NOW would target (§14.9.35 GR11 /
    /// §14.9.10 GR6 — the pre-operation conflict check; <paramref name="recordImage"/> supplies the key slice
    /// for an indexed random/dynamic target, §14.9.35 GR23 / §14.9.10 GR3, and <paramref name="recordExtents"/> the
    /// record area's EXTENT TABLE — D-FRA (v), kb/Work PB1053). "" when no record is identified.
    /// <para>⛔ A CONNECTOR NOT OPEN IN THE I-O MODE IDENTIFIES NO RECORD (kb/Work PB1194), the same contract the two
    /// READ peeks keep. §14.9.35.4 GR3 (<i>"If the open mode is some other value or the file is not open, the I-O
    /// status in the rewrite file connector is set to '49'"</i>) and §14.9.10.4 GR1 settle the statement before any
    /// record is reached, so the registry's §9.1.16 conflict check has nothing to ask. The keyed arms used to name
    /// the record in the key area whatever the open state, so a REWRITE through a CLOSED connector of a record
    /// another connector held locked answered '51' where '49' is the only applicable status: with no open mode no
    /// sharing mode is in effect, and the lock rule presupposes one (§9.1.15).</para></summary>
    public string MutationTargetRecordId(string recordImage, RecordExtents? recordExtents) =>
        MutationOpenModeGuard() is null ? MutationTargetRecordIdCore(recordImage, recordExtents) : "";

    /// <summary>The organization's answer to <see cref="MutationTargetRecordId"/> for a connector open in the I-O
    /// mode.</summary>
    protected virtual string MutationTargetRecordIdCore(string recordImage, RecordExtents? recordExtents) => "";

    /// <summary>The open-mode precondition REWRITE and DELETE RECORD share — ISO §14.9.35.4 GR3, <i>"The rewrite file
    /// connector shall have an open mode of I-O. If the open mode is some other value or the file is not open, the
    /// I-O status in the rewrite file connector is set to '49'"</i>, and §14.9.10.4 GR1, <i>"The open mode of the
    /// file connector referenced by file-name-1 shall be I-O"</i>, whose status is §9.1.13.7 9)'s '49'. Returns
    /// the failing status, or <see langword="null"/> when the statement may proceed. Assigns nothing, like
    /// <see cref="ReadOpenModeGuard"/>: the caller owns the single status assignment. Written once here for
    /// every organization's REWRITE and DELETE body and for <see cref="MutationTargetRecordId"/>.</summary>
    protected string? MutationOpenModeGuard() =>
        PermanentErrorReplay()   // §9.1.13.1 — a permanent error in effect answers first (kb/Work PB1541)
        ?? (IsOpen && Mode == FileOpenMode.IO ? null : FileStatusCode.DeleteRewriteNotOpenForIO);

    /// <summary>The lock identity of the record released by the most recent successful WRITE (§14.9.51 GR11 —
    /// the WITH LOCK acquisition target).</summary>
    public virtual string LastWrittenRecordId => "";

    // ── OPEN / CLOSE (the shared preamble; ISO §14.9.27 / §14.9.6) ───────────────────────────────────────────

    /// <summary>OPEN the file in <paramref name="mode"/> (ISO §14.9.27 / §9.1.13.4): the already-open guard
    /// ('41', §9.1.13.7 item 1), the attempted-mode bookkeeping (GR6b), the read-position reset, the ONE
    /// presence probe, the GR3 authority answer ('37'), the GR10 attribute comparison ('39') and the GR16 write
    /// capability answer ('37'), then the organization's <see cref="OpenCore"/> under the shared permission/I-O
    /// failure mapping ('37'/'30'). Sets and returns the status.</summary>
    public string Open(FileOpenMode mode)
    {
        if (IsOpen) return Status = PermanentErrorReplay() ?? FileStatusCode.FileAlreadyOpen;   // §9.1.13.1 (kb/Work PB1541) before '41'
        Mode = mode;
        ModeKnown = true;   // a FAILED open still records the attempted mode (GR6b "being opened")
        OptionalAbsent = false;
        LastReadUnsuccessful = false;   // OPEN is a reposition; the '43' gate drops in the Status setter
        // ⛔ ONE presence probe per OPEN, taken HERE and handed down to OpenCore (kb/Work PB323). Two things
        // hang on it — GR3's authority answer and GR10's "the file exists" precondition — and every
        // organization's OpenCore needs it again for Table 18, so asking the host once and passing the answer
        // is both the single rule site and the only way the two questions cannot disagree across a racing
        // filesystem. It is HostFile.Probe, not File.Exists: see FilePresence for why two states are not enough.
        // Absent is the pre-probe default only to satisfy definite assignment on the throwing path, where
        // _openMode is false and nothing below reads it.
        var presence = FilePresence.Absent;
        string s;
        try
        {
            presence = HostFile.Probe(HostPath);
            // §14.9.27.4 GR3 — "If the file associated with file-name-1 is present and insufficient authority
            // exists to open the file, the execution of the OPEN statement is unsuccessful, and the I-O status
            // value in the file connector referenced by file-name-1 is set to '37'." (§9.1.13.6 item 6 b)
            // restates it for OPEN and DELETE FILE and adds "The ability to detect this is processor
            // dependent".) THE RULE LIVES HERE, ONCE, ABOVE THE ORGANIZATIONS: a refusal is not evidence of
            // absence, so it must never reach OpenCore's Table-18 arms, which would read it as '35' — or, for
            // an OPTIONAL file, as GR13's successful open over a file that is not there.
            // OUTPUT is excluded, and that exclusion is REQUIRED, not a convenience: GR18 makes OUTPUT a
            // CREATION that never consults presence, §9.1.13.6 item 5's '35' is defined only "for an OPEN
            // statement with the INPUT, I-O, or EXTEND phrase", and a directory the process may write but not
            // list legitimately accepts a new file — probing it answers Unauthorized while the OPEN OUTPUT
            // genuinely succeeds. When an OUTPUT target IS present-and-refused, the creating stream itself
            // raises UnauthorizedAccessException and the catch below returns the same '37'.
            if (presence is FilePresence.Unauthorized && mode is not FileOpenMode.Output)
                return Status = FileStatusCode.PermissionDenied;   // '37' §14.9.27.4 GR3 / §9.1.13.6 item 6 b)
            // §14.9.27.4 GR10 — the fixed-file-attribute comparison, BEFORE any organization work: "During the
            // execution of the OPEN statement when the file connector is matched with the file and the file
            // exists, the attributes of the file connector as specified in the file control paragraph and the
            // file description entry are compared with the fixed file attributes of the file. If the attributes
            // don't match, a file attribute conflict condition occurs, the execution of the OPEN statement is
            // unsuccessful, and the I-O status associated with file-name-1 is set to '39'." An UNSUCCESSFUL
            // OPEN leaves the file unaffected (GR25), so the check has to precede OpenCore, whose
            // OUTPUT/creation arms truncate. OUTPUT is excluded because GR18 makes it a CREATION ("If the OUTPUT
            // phrase is specified, the successful execution of the OPEN statement creates the file"), and §9.1.6
            // fixes a file's attributes at creation — so an OPEN OUTPUT ESTABLISHES them (in the store the
            // organization writes) rather than being judged against the previous file's. GR10's own precondition
            // is "the file EXISTS", which is Present and nothing else.
            if (mode is not FileOpenMode.Output && presence is FilePresence.Present && FixedAttributeConflict())
                return Status = FileStatusCode.FixedAttributeConflict;   // '39' §9.1.13.6 item 7
            // §14.9.27.4 GR16 — "If the I-O phrase is specified, the file shall support the input and output
            // statements that are permitted for the organization of that file when opened in the I-O mode. If
            // the file does not support those statements, the I-O status value for file-name-1 is set to '37'
            // and the execution of the OPEN statement is unsuccessful." §9.1.13.6 item 6 a) prices the other
            // write mode the same way: 1. "the EXTEND or OUTPUT phrase is specified but the file will not
            // support write operations", 2. the I-O restatement of GR16.
            // ⛔ THE WRITE CAPABILITY IS AN OPEN-CONTRACT QUESTION, ASKED HERE, ONCE, FOR EVERY ORGANIZATION —
            // never inside whichever arm happens to touch a stream. GR16 names no organization, and Table 20
            // makes it organization-independent: REWRITE sits under the I-O column for sequential, random AND
            // dynamic access, so "supports the statements permitted in the I-O mode" entails write capability
            // for every organization there is. Asking it in the arms is what kb/Work PB328 was — the sequential
            // arm asked it by accident (its I-O and EXTEND streams ARE write opens, so the shared catch below
            // turned a refusal into '37'), while the relative and indexed arms only read their store, so the
            // SAME read-only file answered '37' sequentially and '00' keyed, and the loss surfaced as a '30' at
            // CLOSE on a byte-identical file after READ and REWRITE had both reported success. Asked here, an
            // organization added later inherits the answer instead of re-earning it.
            // INPUT is excluded and the exclusion is REQUIRED, not an optimization: a read-only file supports
            // every statement Table 20 permits in the input mode, so probing write for it would refuse an OPEN
            // the operating environment carries out perfectly. §9.1.13.6 item 6 a) 3. ("the INPUT phrase is
            // specified but the file will not support read operations") is the input mode's own question, and
            // it is already answered eagerly by every organization — the sequential reader and the keyed
            // Attach() both open/read the store, and their UnauthorizedAccessException reaches the same '37'
            // through the catch below.
            // The Present guard is likewise required: an ABSENT target has no capability to probe, and the two
            // modes that reach OpenCore absent both CREATE (GR17's optional I-O/EXTEND, GR18's OUTPUT), where
            // the creating stream's own refusal is the authority answer.
            if (presence is FilePresence.Present && mode is not FileOpenMode.Input && !HostFile.PermitsWrite(HostPath))
                return Status = FileStatusCode.PermissionDenied;   // '37' §14.9.27.4 GR16 / §9.1.13.6 item 6 a)
            // §9.1.15 — "The successful opening of a file establishes a file lock for the applicable sharing
            // rules, thereby preventing other run units from opening that file with incompatible sharing rules":
            // THE LOCK IS TAKEN HERE, ONCE, ABOVE THE ORGANIZATIONS, and BEFORE OpenCore on a file that exists,
            // because an OPEN that is going to be unsuccessful leaves the file unaffected (GR25) and OUTPUT's
            // body truncates it (kb/Work PB833). A file that does not exist yet is locked after the body has
            // created it, below: nothing can hold what is not there.
            if (presence is FilePresence.Present && TakeRunUnitFileLock(mode, existedBeforeBody: true) is { } refusal)
                return Status = refusal;   // '61' §9.1.13.9 1)
            s = OpenCore(mode, presence);
            if (s[0] == '0' && _runUnitLock is null && TakeRunUnitFileLock(mode, existedBeforeBody: false) is { } lateRefusal) s = lateRefusal;
        }
        catch (UnauthorizedAccessException) { s = FileStatusCode.PermissionDenied; }
        // §9.1.13.9 1) — "A file sharing conflict condition exists because an OPEN statement is attempted on a
        // physical file and that physical file is already open by another file connector in a manner that
        // conflicts with this request". A connector of THIS run unit is refused by the Table 19 arbiter before
        // the body runs (FileRegistry's '61'); a connector of ANOTHER run unit is visible only as the host's own
        // refusal, and the host says which refusal it is (kb/Work PB860 — every organization answered '30').
        catch (IOException e) when (HostFile.IsSharingRefusal(e)) { s = FileStatusCode.FileSharingConflict; }
        catch (IOException) { s = FileStatusCode.PermanentError; }
        // §9.1.13.6 item 1's '30' — "a permanent error exists and no further information is available". Since
        // §12.4.5.3 GR3 b) made the host path a RUNTIME VALUE (the content of data-name-1), an arbitrary string can
        // now reach the .NET path APIs, and the shapes they reject outside the IOException family — an embedded
        // separator the platform forbids, a form the platform does not support — must be an I-O status like every
        // other open failure, never an escaping exception. Associate() already screens the empty and control-
        // character content ('31'); this is the residue only the operating environment can judge.
        catch (ArgumentException) { s = FileStatusCode.PermanentError; }
        catch (NotSupportedException) { s = FileStatusCode.PermanentError; }
        // The connector's input-output areas are allocated HERE, by the OPEN that takes its streams (ISO §12.4.5.14.3
        // GR1; HostFile.InputOutputAreaBuffer, kb/Work PB643). RESERVE integer-1 may ask for more memory than the run
        // unit has — that is "current resource availability", not a compile-time limit (§4.2.15's limit,
        // HostFile.MaxInputOutputAreas, only bounds the count's arithmetic) — and an OPEN has only I-O statuses as
        // outcomes, so an allocation the runtime cannot make is §9.1.13.6 item 1's '30' like any other permanent
        // failure, never an OutOfMemoryException killing the run unit (the CobolDynTable precedent, kb/Work PB1410).
        catch (OutOfMemoryException) { s = FileStatusCode.PermanentError; }
        _openMode = s[0] == '0';   // a success-family OPEN ('00'/'05'/'07') puts the connector in its open mode
        // §9.1.15 — "The SUCCESSFUL opening of a file establishes a file lock": an unsuccessful one establishes
        // none, and §14.9.27.4 GR25 leaves the file unaffected. Any handle the body took on its way to failing
        // goes back HERE, because Close() answers '42' for a connector that is not open and never reaches
        // CloseCore (kb/Work PB771).
        // ⛔ The release is OUTSIDE the try above, so a host refusal during it escaped the OPEN as an unhandled
        // IOException (kb/Work PB1512's sibling: an INDEXED OPEN OUTPUT on a full device threw from
        // KeyedConnector.AbandonOpen). The OPEN's outcome is already decided and stored — `s` is its I-O status —
        // and the disposal that refuses a final flush has still closed the host handle, so the refusal adds no
        // outcome of its own: it is not allowed to REPLACE the status with a crash (SharedExtendOpenDriftTests:
        // an OPEN has only I-O statuses as outcomes).
        if (!_openMode)
        {
            try { AbandonOpen(); }
            catch (IOException) { }   // the status above stands (§14.9.27.4 GR25 — the unsuccessful OPEN's own)
            ReleaseRunUnitFileLock();   // §9.1.15 — only a SUCCESSFUL opening establishes the lock
        }
        // ⛔ NOTHING IS RECORDED HERE, AND THE ABSENCE IS THE DESIGN (kb/Work PB802). §9.1.6's fixed file
        // attributes "apply to the file at the time it is created", and the two moments the OPEN statement
        // CREATES a file — GR18's OUTPUT, GR17's absent OPTIONAL I-O/EXTEND — are exactly the moments each
        // organization's OpenCore writes its own store: the RELATIVE and INDEXED arms call
        // KeyedConnector.Persist (RecordFraming.ComposeStore), whose HEADER is those attributes, and the sequential arms write a format
        // that records none. So the establishment rides the format instead of a second step beside it, which is
        // what "no sidecar of any type" means and what removes the failure mode the second step had (kb/Work
        // PB684: a catalog this process could not write left the PREVIOUS file's attributes in force).
        return Status = s;
    }

    /// <summary>⛔ §14.9.27.4 GR10's PER-ORGANIZATION ANSWER: does THIS organization's physical format record a
    /// §9.1.6 fixed file attribute that CONTRADICTS <see cref="DeclaredAttributes"/>? Asked once per OPEN by
    /// <see cref="Open"/>, on a file that exists and in a mode other than OUTPUT; true is the file attribute
    /// conflict condition and I-O status '39' (§9.1.13.6 item 7).
    /// <para>⛔ THIS VIRTUAL IS THE ANNEX A.1 ITEM 129 VALIDATED SET, and the set is stated as a RULE rather
    /// than a table: <b>exactly what each organization's format encodes</b>. GR10 grants precisely that latitude
    /// — "The implementor defines which of the fixed-file attributes are validated during the execution of the
    /// OPEN statement. The validation of fixed-file attributes may vary depending on the organization or storage
    /// medium of the file." — so the variation lives in the OVERRIDES, each beside the format it reads, and an
    /// organization added later brings its own answer instead of needing a row in a switch. (It used to be a
    /// hand-kept <c>MediumFixesRecordLayout</c> classification over one uniform sidecar catalog; owner decision
    /// 2026-09-07, kb/Work PB802, replaced the catalog with the file's own bytes.) The determination is
    /// documented at <c>docs/CONFORMANCE.md</c> row <c>DOC-A.1-129</c> and designed in
    /// <c>docs/COBOLNET_FILES_DESIGN.md</c> D10.</para>
    /// <para>THE BASE ANSWER IS "NO", and it is the honest one for a format that records nothing — a
    /// fixed-length record sequential file is plain bytes and a line sequential file is plain text, so no
    /// §9.1.6 attribute of either can be read back to be contradicted. §9.1.7.2 says so, and §9.1.13.2 items 3,
    /// 5 and 7 answer the resulting disagreements with the SUCCESSFUL '04', '06' and '09' rather than a refused
    /// OPEN. It must never become a guess: a validated set may not MANUFACTURE a '39' from an attribute the
    /// file never stated.</para></summary>
    protected virtual bool FixedAttributeConflict() => false;

    // ── The connector's declared fixed file attributes (ISO §9.1.6) ──────────────────────────────────────────

    /// <summary>This connector's fixed file attributes AS DECLARED — §14.9.27.4 GR10's "the attributes of the
    /// file connector as specified in the file control paragraph and the file description entry". Assembled
    /// HERE, once, for every organization: the record type and the minimum/maximum logical record size are the
    /// RECORD clause's (§13.18.43 GR9/GR10 for a varying file; a fixed file's single record width for both
    /// bounds). Organizations contribute only what is theirs — <see cref="DeclaredOrganization"/> and, for an
    /// indexed file, <see cref="DeclaredKeys"/>.</summary>
    public FixedFileAttributes DeclaredAttributes => new(
        DeclaredOrganization,
        IsVarying,
        IsVarying ? VaryMin : RecordWidth,
        IsVarying ? VaryMax : RecordWidth,
        DeclaredKeys);

    /// <summary>This organization's name in the persisted catalog — §9.1.6's primary attribute and nothing
    /// else, so one of the three values §9.1.6 names ("There are three organizations: sequential, relative, and
    /// indexed"): <see cref="FixedFileAttributes.Sequential"/>, <see cref="FixedFileAttributes.Relative"/> or
    /// <see cref="FixedFileAttributes.Indexed"/>. §9.1.7.2's record-sequential / line-sequential distinction is
    /// §9.1.6's separately listed <i>record delimiter</i>, not a fourth organization, and it is outside the
    /// §14.9.27.4 GR10 validated set (<see cref="FixedFileAttributes.Conflicts"/>).</summary>
    protected abstract string DeclaredOrganization { get; }

    /// <summary>Which of §14.9.6.4 GR2's four categories this connector's PHYSICAL FILE falls in — the index
    /// Table 14 (§14.9.6.4 GR3) is read by, and the ONE place WiseOwl COBOL's medium determination is stated in
    /// code. GR2 requires every supported physical file to be in exactly one category; making it an abstract
    /// property makes that total by construction, where it used to be a sentence in a doc comment on one CLOSE
    /// method (kb/Work PB235). See <see cref="PhysicalFileCategory"/> for why the answers are (a) and (d).</summary>
    public abstract PhysicalFileCategory Category { get; }

    /// <summary>This connector's record keys for the catalog (§9.1.6 — prime key, alternate keys, SUPPRESS WHEN,
    /// key collating sequence): index 0 the prime key, 1.. the alternates in declaration order. Only an indexed
    /// file has any; sequential and relative files answer the shared empty list.</summary>
    protected virtual IReadOnlyList<FixedFileAttributes.KeyDescriptor> DeclaredKeys => [];

    /// <summary>The drain of the ONE print line held back on this connector — the report writer's:
    /// <see cref="CobolReport"/> keeps its most recent report line unwritten so that a relative line with an
    /// integer-2 of zero can overwrite it (ISO §13.18.35.4 GR3; kb/Work PB1247). Whoever writes next to the device
    /// claims it first (<see cref="FileRegistry.HoldLine"/>), which writes out a different holder's line, and
    /// <see cref="Close"/> writes out the holder's line before the file closes. Null = nothing held.</summary>
    internal Action? HeldLineDrain { get; set; }

    /// <summary>CLOSE the file (ISO §14.9.6): the not-open guard ('42', §9.1.13.7 item 2), then the
    /// organization's <see cref="CloseCore"/> under the OPEN twin's exception mapping — an OS-level close
    /// failure (a flush that cannot complete, a store that cannot persist) is the §9.1.13.6 item 1 permanent
    /// error '30', where it previously escaped the emitted statement and aborted the run unit (kb/Work
    /// PB140). Successful or not, a completed CLOSE takes the connector out of its open mode (§14.9.6.4 GR8;
    /// the failure left it BOTH associated and open before). Sets and returns the status.</summary>
    public string Close()
    {
        if (!IsOpen) return Status = FileStatusCode.FileNotOpen;
        // A producer that holds its last line back writes it out FIRST — on EVERY close path (the CLOSE statement,
        // the run unit's implicit CloseAll, a CANCEL), because they all funnel through here. One-shot: the next
        // OPEN starts with none.
        if (HeldLineDrain is { } drain)
        {
            HeldLineDrain = null;
            // The held line is a WRITE the program already issued; a CLOSE is the correction technique of a permanent
            // error in effect (DOC-A.1-105), so the condition is lifted first or the drain's write would be refused
            // and the line silently lost (kb/Work PB1541). A failure of the drain itself ends here with the CLOSE.
            _permanentError = null;
            drain();
        }
        string s;
        try { s = CloseCore(); }
        catch (UnauthorizedAccessException) { s = FileStatusCode.PermanentError; }
        catch (IOException) { s = FileStatusCode.PermanentError; }
        ReleaseRunUnitFileLock();   // §9.1.15 — "The file lock is removed by an explicit or implicit CLOSE statement"
        _openMode = false;
        // §9.1.13.1 / Annex A.1 item 105 — THE CORRECTION TECHNIQUE: a completed CLOSE ends a permanent error in
        // effect, successful or not (an unsuccessful CLOSE also leaves the connector closed), so the next OPEN
        // starts clean (kb/Work PB1541, docs/CONFORMANCE.md DOC-A.1-105). A CLOSE that itself fails '30' does not
        // restart the condition: `_openMode` is already false, which the Status setter's IsOpen test reads.
        _permanentError = null;
        return Status = s;
    }

    /// <summary>The organization-specific OPEN body: reset org state, create/load the physical store, and
    /// return the resulting status. Runs under the base's '37'/'30' exception mapping; the base has already
    /// recorded the attempted mode, reset the shared position state, and answered §14.9.27.4 GR3 and GR10.
    /// <para><paramref name="presence"/> is the base's ONE <see cref="HostFile.Probe"/> answer — an
    /// implementation must never re-ask the host, and must never call <c>File.Exists</c>, whose two-valued
    /// answer is what kb/Work PB323 was. For every mode but OUTPUT it is <see cref="FilePresence.Present"/> or
    /// <see cref="FilePresence.Absent"/> only: GR3 has already turned <see cref="FilePresence.Unauthorized"/>
    /// into '37' above, so a Table-18 arm may read it as a plain two-state answer. OUTPUT can still see
    /// Unauthorized, and ignores it — GR18 makes OUTPUT a creation that does not consult presence.</para></summary>
    protected abstract string OpenCore(FileOpenMode mode, FilePresence presence);

    /// <summary>The organization-specific CLOSE body (flush/persist/dispose); returns the resulting status.
    /// A successful close must clear <see cref="ModeKnown"/> (§9.1.4 — the file is then in no open mode).</summary>
    protected abstract string CloseCore();

    /// <summary>⛔ ISO §12.4.5.14.3 GR1 — the number of input-output areas this connector allocates: the RESERVE
    /// clause's integer-1, set by the emitter right after registration for exactly the files whose file control entry
    /// writes the clause (kb/Work PB643, the <see cref="NationalRecordArea"/> pattern), else
    /// <see cref="HostFile.ImplementorInputOutputAreas"/>. Each organization spends it through
    /// <see cref="HostFile.InputOutputAreaBuffer"/> when its OPEN takes its streams, so it is read at every OPEN
    /// and never cached.</summary>
    public int InputOutputAreas { get; internal set; } = HostFile.ImplementorInputOutputAreas;

    // ── Record-area shaping ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>⛔ §14.9.30.4 GR15's RECORD-AREA CATEGORY — true when the record area is "specified implicitly or
    /// explicitly as national", set by the emitter right after registration for exactly those files (kb/Work
    /// PB327). It decides which SPACE the short-record pad below uses, and nothing else. It is a property of the
    /// FD's record description, not of the physical file, so it is declared once per connector rather than
    /// re-derived per read.</summary>
    public bool NationalRecordArea { get; internal set; }

    /// <summary>⛔ THE FD's RECORD CLAUSE CARRIES A DEPENDING PHRASE — set by the emitter right after registration for
    /// exactly those files (kb/Work PB1191), the <see cref="NationalRecordArea"/> pattern. §14.9.51.4 GR21 strips a
    /// line sequential record's trailing spaces only for a file description "not containing a RECORD clause with the
    /// DEPENDING phrase"; GR22 says such a record is transferred at data-name-1's length, spaces included. So a
    /// connector with this property never trims (<see cref="TrimRecordEnd"/>).</summary>
    public bool RecordLengthFromDepending { get; internal set; }

    /// <summary>⛔ THE FILE'S §13.18.13 CODE-SET CONVERSION, or null for the native character set — §13.18.13.4
    /// GR7, "<i>If the CODE-SET clause is not specified, the native character set is assumed for data on the
    /// external media</i>". Set by the emitter right after registration for exactly the files whose CODE-SET
    /// clause names a coded character set that is not the whole native one-byte channel under the identity — a
    /// translated code (EBCDIC) or a smaller set (ISO/IEC 646: STANDARD-1, STANDARD-2, ASCII) — the
    /// <see cref="NationalRecordArea"/> pattern. GR2 makes it a property established at OPEN and constant for
    /// the connector, so it is declared once rather than re-derived per record.</summary>
    public CodeSetConversion? CodeSet { get; internal set; }

    /// <summary>True for a LINE SEQUENTIAL file — a TEXT file, whose default encoding is UTF-8 (<see cref="MediumEncoding"/>).</summary>
    protected virtual bool LineSequential => false;

    /// <summary>⛔ THE FILE'S ENCODING — an attribute of the FILE, carried by its connector (owner decision kb/Work R51
    /// item 3; design <c>COBOLNET_FILES_DESIGN.md</c> D29; kb/Work PB1760): a CODE-SET clause selects its alphabet's
    /// coded character set (§13.18.13.4 GR6); otherwise a LINE SEQUENTIAL file is UTF-8 text and every other file is
    /// one byte per character position under the storage-byte law (ISO/IEC 8859-1, HIGH-VALUE the byte 0xFF —
    /// DOC-A.1-31). Derived, never stored, so it cannot disagree with the two facts it is made of.</summary>
    public MediumEncoding MediumEncoding =>
        CodeSet is not null ? MediumEncoding.CodeSet : LineSequential ? MediumEncoding.Utf8 : MediumEncoding.SingleByte;

    /// <summary>⛔ GR6 a — the NATIVE form of a record image just taken off the storage medium, decoded by the file's
    /// <see cref="MediumEncoding"/>. Every physical read of record data passes through here; see
    /// <see cref="CodeSetConversion"/> for why the framing around the record does not.</summary>
    protected string FromMedium(string mediumImage) => MediumEncoding is MediumEncoding.Utf8
        ? LineSequentialEncoding.FromChannel(mediumImage, NationalRecordArea)
        : FileCharacterSet.FromChannel(mediumImage, CodeSet);

    /// <summary>⛔ GR6 b — the STORAGE-MEDIUM form of a record image about to be written, encoded by the file's
    /// <see cref="MediumEncoding"/>. Every physical write of record data passes through here.
    /// <para>⛔ <paramref name="national"/> is the category of the record being SENT (§14.9.51.4 GR21 / GR22: "If
    /// record-name-1 is specified implicitly or explicitly as national …"), never the connector's area category —
    /// an FD with a national and an alphanumeric record description writes each as what it is (kb/Work PB1191).</para></summary>
    protected string ToMedium(string nativeImage, bool national) => MediumEncoding is MediumEncoding.Utf8
        ? LineSequentialEncoding.ToChannel(nativeImage, national)
        : FileCharacterSet.ToChannel(nativeImage, CodeSet);

    /// <summary>⛔ THE ONE ANSWER TO "can this record be written in the file's coded character set?" for every
    /// organization's WRITE and REWRITE (owner decision kb/Work R47; Annex A.1 item 31; kb/Work PB690, PB1150,
    /// PB1542): true when the record holds a character with no image in the FILE'S coded character set — with no
    /// <see cref="CodeSet"/>, the ISO/IEC 8859-1 file character set
    /// (<see cref="FileCharacterSet.HasCharacterWithoutByteImage"/>); with one, the CODE-SET alphabet's own set,
    /// because §13.18.13.4 GR6 b replaces "<i>each native coded character in the record … with its associated coded
    /// character as defined in the alphabet being used</i>" and a character with no associated coded character
    /// cannot be replaced (<see cref="CodeSetConversion.HasCharacterWithoutImage"/>). The statement is then
    /// unsuccessful before anything reaches the medium: '91' (<see cref="FileStatusCode.CharacterWithoutByteImage"/>)
    /// for a record sequential, report, relative or indexed file — decided HERE, at the output statement, even for
    /// an indexed or relative store that reaches the medium only at CLOSE, so the persist can never meet a record
    /// it cannot encode; a line sequential file reaches '71' first, through its own character set, which excludes
    /// the same characters (<c>SequentialConnector.RecordAreaOutsideLineCharacterSet</c>).</summary>
    protected bool RecordHasCharacterWithoutByteImage(ReadOnlySpan<char> record) =>
        CodeSet?.HasCharacterWithoutImage(record) ?? FileCharacterSet.HasCharacterWithoutByteImage(record);

    /// <summary>Pad/truncate to the record width — the record-AREA image a READ makes available (a shorter
    /// varying record space-fills the area; its true length is <see cref="LastReadLength"/>).
    /// <para>⛔ A NATIONAL RECORD AREA PADS WITH THE NATIONAL SPACE (ISO §14.9.30.4 GR15 — "If the record-area
    /// associated with file-name-1 is specified implicitly or explicitly as national, a trailing space is defined
    /// to be the national space character"; kb/Work PB327). This channel carries one CHARACTER per BYTE, and a
    /// national space is the two bytes 0x00 0x20 (§13.18.60.4 GR8 leaves the size to the implementor — D-N1 pins
    /// two, UTF-16BE), so the pad is written as the pair, aligned to the AREA's own even byte boundary: the
    /// national positions of the area start at even offsets, and an odd short read leaves a half position whose
    /// content §14.9.30.4 GR14/GR15 do not define. Padding the bytes with 0x20 instead would have manufactured
    /// U+2020 characters — the identical trap <c>CobolBits.NatWriteWindow</c> documents on the write side.</para></summary>
    protected string Fit(string s) => FitRecord(s, RecordWidth, NationalRecordArea);

    /// <summary>⛔ THE ONE RECORD FIT (kb/Work PB327): the alphanumeric fill (§14.9.30.4 GR15: "a trailing space
    /// is defined to be the alphanumeric space character"), or GR15's national one — both
    /// <see cref="RecordFill.Fit"/>, which the SORT/MERGE transfers' own short-record fill shares (kb/Work PB1140).
    /// Every site that pads a record image to a declared span routes through it, so the two fills cannot diverge.
    /// Truncation is identical in both (GR15's over-length arm truncates "on the right to the maximum size", in bytes).
    /// <para>⛔ <paramref name="national"/> says WHICH category the rule being applied keys on, and each caller names
    /// it: the READ side and a record area at rest are the AREA's category (<see cref="NationalRecordArea"/>, GR15
    /// — "the record-area associated with file-name-1"), while a WRITE or REWRITE is keyed on record-name-1
    /// (§14.9.51.4 GR22, §14.9.35.4 GR17 c) — its own category, which an FD with both a national and an alphanumeric
    /// record description does not share with the area (kb/Work PB1191).</para></summary>
    protected static string FitRecord(string s, int width, bool national) => RecordFill.Fit(s, width, national);

    /// <summary>⛔ THE RECORD AREA IN ITS ALL-SPACES STATE — what an unsuccessful READ hands back in place of a
    /// record (§14.9.30.4 GR18: its content is then undefined, and the connector offers spaces). It is
    /// <see cref="FitRecord"/> of the empty image, so a NATIONAL record area holds national spaces and never
    /// U+2020 positions a byte-level <c>new string(' ', n)</c> manufactures (kb/Work PB679, the sibling of the key
    /// pad fixed in <see cref="IndexedConnector"/>): ONE pad, ONE trim, keyed on the area's category.</summary>
    protected string BlankRecordArea() => FitRecord("", RecordWidth, NationalRecordArea);

    /// <summary>⛔ THE ONE TRAILING-SPACE TRIM of a record image on the way OUT to a line-oriented stream — the
    /// inverse of <see cref="FitRecord"/>'s pad, and it has to shed the SAME space (kb/Work PB327). A national
    /// record area's trailing space is the national space, the two bytes 0x00 0x20 (§14.9.30.4 GR15;
    /// §13.18.60.4 GR8 / D-N1), so it sheds in PAIRS from the area's own even byte boundary: a plain
    /// <c>TrimEnd()</c> removed only the 0x20 of the last pair and stopped at its 0x00, leaving an odd-length
    /// record with half a national position on disk (measured: `01 L-REC PIC N(5).` holding N"AB" wrote nine
    /// bytes, `00 41 00 42 00 20 00 20 00`).
    /// <para>⛔ <paramref name="national"/> is the category of the record being WRITTEN — §14.9.51.4 GR21: "If
    /// record-name-1 is specified implicitly or explicitly as alphanumeric, a space is defined to be the alphanumeric
    /// space character. If record-name-1 is specified implicitly or explicitly as national, a space is defined to be
    /// the national space character" — and not the connector's area flag: an FD that carries a national record
    /// description AND an alphanumeric one writes each with its own space (kb/Work PB1191).</para></summary>
    protected string TrimRecordEnd(string image, bool national)
    {
        // §14.9.51.4 GR22: a DEPENDING-phrase file keeps the record at data-name-1's length — GR21's strip does not apply.
        if (RecordLengthFromDepending) return image;
        // GR21's "spaces" are the alphanumeric SPACE CHARACTER only — <c>TrimEnd()</c> stripped every Unicode White_Space
        // character too, so a trailing U+00A0 (a member of the line sequential character set, DOC-A.1-115) vanished.
        if (!national) return image.TrimEnd(' ');
        int n = image.Length & ~1;                                  // whole national positions only
        while (n >= 2 && image[n - 2] == '\0' && image[n - 1] == ' ') n -= 2;
        return image[..n];
    }

    /// <summary>⛔ A RECORD SENT TO A FILE OF FIXED-LENGTH RECORDS, in the FIXED FORM those records have (determination
    /// D-FRA (vi); kb/Work PB1562). The sending side of a WRITE / REWRITE hands every connector the same thing — a
    /// variable-length group record's contiguous image with the extent table of its layout. A connector of
    /// VARIABLE-length records frames both (D-FRA (v)); a FIXED connector cannot, because §13.18.43.4 GR6 and §9.1.6
    /// make its records one size and its record type and size fixed attributes that a second program describing
    /// the file with a plain <c>PIC X(n)</c> shares, so it carries the record in the one shape every record of the
    /// file has (<see cref="CobolContiguousLayout.ToFixedForm"/>), and the table, which described the CONTIGUOUS
    /// image, is spent: it is returned null, so nothing downstream frames or keys by it. Asked at the two places a
    /// program's record area enters a connector — the registry's governed WRITE and REWRITE entries, above the
    /// organizations, and <c>IndexedConnector.AreaKey</c> for a key a statement reads from the record area. A record
    /// with no table (a character-window record, a dynamic-length elementary record) comes back as it is — its
    /// <see cref="Fit"/> already is its fixed form — and so does any record sent to a file of variable-length
    /// records.</summary>
    internal (string Image, RecordExtents? Extents) FixedForm(string image, RecordExtents? extents) =>
        !IsVarying && extents?.Layout is { } layout && layout.ToFixedForm(image, extents) is var form
        && !ReferenceEquals(form, image) ? (form, null) : (image, extents);

    /// <summary>The stored image of a record being written: a varying record keeps exactly its declared length
    /// (ISO §13.18.43 GR13 — truncate/pad the area image to it); a fixed record fills the record width. Returns
    /// null (→ '44') when a varying length violates the declared bounds (GR14 / §14.9.35 GR20).</summary>
    protected string? Stored(string image, int length)
    {
        if (!IsVarying) return Fit(image);
        int len = length >= 0 ? length : image.Length;
        if (len < VaryMin || len > VaryMax) return null;
        return image.Length == len ? image : image.Length > len ? image[..len] : FitRecord(image, len, NationalRecordArea);   // the ONE pad (kb/Work PB679)
    }

    /// <summary>A record about to be stored, with the EXTENT TABLE it was sent with (determination D-FRA (v);
    /// kb/Work PB1053) when <paramref name="stored"/> is still the image that table describes. A record
    /// <see cref="Stored"/> truncated or padded (a RECORD VARYING DEPENDING length, a fixed-length record width)
    /// is no longer the image the variable-length group composed, so it is stored without one — a table that
    /// misdescribed it would place a key, or split the record, at characters that are not there.</summary>
    private protected static StoredFrame Framed(string stored, string image, RecordExtents? extents) =>
        new(stored, stored.Length == image.Length ? extents : null);
}
