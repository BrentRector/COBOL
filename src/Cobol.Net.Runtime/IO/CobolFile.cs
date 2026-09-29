// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>
/// The static file-I/O facade the WiseOwl COBOL backend emits calls to: a PURE DELEGATOR onto the ONE
/// <see cref="FileRegistry"/> (DESIGN-runtime-library §2.2 — polymorphic connector dispatch; the former
/// sequential-first/<c>Keyed*</c>-fallthrough second dispatch mechanism is deleted). The compiler registers every
/// SELECTed file at program start, then emits a verb call per file statement; the connector owns the ISO §9.1.13
/// status machine. Records cross this boundary as their <b>character image</b> (a <see cref="string"/>) — the
/// typed record ↔ image conversion is in the generated code, keeping the substrate typed. The facade stays
/// because it is the emitted surface (byte-stable pre-G8); the registry instance is the RUN UNIT's
/// (<c>RunUnit.Current.Files</c> — lazily-established ambient, DESIGN-runtime-library §2.1).
/// </summary>
public static class CobolFile
{
    private static FileRegistry _reg => RunUnit.Current.Files;

    /// <summary>Reset the file registry (emitted once at program start).</summary>
    public static void Init() => _reg.Reset();

    /// <summary>Mint a UNIQUE per-object connector key for an instance file (M2-OO-1i, ISO §9.1.4).</summary>
    public static string MintInstanceKey(string baseKey) => _reg.MintInstanceKey(baseKey);

    /// <summary>Close and remove a per-object instance-file connector (M2-OO-1i, ISO §9.1.4).</summary>
    public static void CloseAndDrop(string key) => _reg.CloseAndDrop(key);

    /// <summary>Register a SELECTed sequential file (emitted at program start, one per SELECT); the host path is
    /// resolved from the ASSIGN target by <see cref="ResolveHostPath"/>. See <see cref="FileRegistry.Register"/>.</summary>
    public static void Register(string cobolName, string assignTarget, int recordWidth, bool lineSequential,
        bool optional, int varyMin = -1, int varyMax = -1, string? selectName = null, int edition = 2023,
        int recordMax = 0)
        => _reg.Register(cobolName, assignTarget, recordWidth, lineSequential, optional, varyMin, varyMax, selectName, edition, recordMax);

    /// <summary>Register a SELECTed RELATIVE file (emitted at program start).</summary>
    public static void RegisterRelative(string cobolName, string assignTarget, int recordWidth, bool optional,
        int accessMode, int relativeKeyDigits, int varyMin = -1, int varyMax = -1, string? selectName = null,
        int edition = 2023)
        => _reg.RegisterRelative(cobolName, assignTarget, recordWidth, optional, accessMode, relativeKeyDigits,
            varyMin, varyMax, selectName, edition);

    /// <summary>Register a SELECTed INDEXED file (emitted at program start).</summary>
    public static void RegisterIndexed(string cobolName, string assignTarget, int recordWidth, bool optional,
        int accessMode, int primeOffset, int primeLength, int varyMin = -1, int varyMax = -1,
        CobolCollation? primeCollation = null, string? selectName = null, int edition = 2023,
        CobolContiguousLayout? primeLayout = null)
        => _reg.RegisterIndexed(cobolName, assignTarget, recordWidth, optional, accessMode, primeOffset,
            primeLength, varyMin, varyMax, primeCollation, selectName, edition, primeLayout);

    /// <summary>The SELECT-spelled name of a registered connector (ISO §15.28.4 r1c/r2b; kb/Work PB63) — the
    /// no-argument FUNCTION EXCEPTION-FILE's display of the last-exception connector.</summary>
    public static string SelectNameOf(string cobolName) => _reg.SelectNameOf(cobolName);

    /// <summary>Register one ALTERNATE RECORD KEY (§12.4.5.6), in declaration order, with its optional
    /// §12.4.5.7 collating-weight table (null = native ordinal) and §12.4.5.6.4 GR6 SUPPRESS WHEN value. A key that
    /// follows a variable-length member carries its record type's layout (kb/Work PB1025).</summary>
    public static void AddAlternateKey(string name, int offset, int length, bool duplicates, CobolCollation? collation = null,
        string? suppress = null, CobolContiguousLayout? layout = null)
        => _reg.AddAlternateKey(name, offset, length, duplicates, collation, suppress, layout);

    // ⛔ EVERY OPEN ENTRY CARRIES THE EXECUTING ELEMENT'S OWN ASSIGN SPECIFICATION AND LINAGE PAGE, and none of
    // the three parameters has a default: ISO §12.4.5.3 GR3 a)/b) and §13.18.34 GR6 b) both name the runtime
    // element that EXECUTES the statement, and one file connector can be described by several of them (an
    // EXTERNAL file connector is ONE object per run unit, §13.18.22.4 GR4 a; a RECURSIVE unit's is unit-scoped
    // across activations, §8.6.4). Requiring the arguments makes an emitter path that forgets them a COMPILE
    // error rather than a silent wrong file / wrong page — the shape that replaced the connector-held
    // SetAssignUsing/SetLinage closures (kb/Work PB673; kb/Work PB168 is why they could not simply be guarded).
    //   assign        — data-name-1's CONTENT when assignDynamic, else literal-1 / device-name-1's value.
    //   assignDynamic — the executing element's file control entry writes the USING phrase (GR3 b).
    //   page          — the executing element's LINAGE operand values, or null when its FD has no LINAGE clause.
    public static void OpenInput(string name, string assign, bool assignDynamic, LinagePage? page)
        => _reg.Open(name, FileOpenMode.Input, assign, assignDynamic, page);
    public static void OpenOutput(string name, string assign, bool assignDynamic, LinagePage? page)
        => _reg.Open(name, FileOpenMode.Output, assign, assignDynamic, page);
    public static void OpenExtend(string name, string assign, bool assignDynamic, LinagePage? page)
        => _reg.Open(name, FileOpenMode.Extend, assign, assignDynamic, page);
    public static void OpenIO(string name, string assign, bool assignDynamic, LinagePage? page)
        => _reg.Open(name, FileOpenMode.IO, assign, assignDynamic, page);

    /// <summary>⛔ THE ONE WRITTEN-FORM OPEN ENTRY for a file-name carrying a §14.9.27.2 TAPE PHRASE — WITH NO
    /// REWIND (the OPEN twin of <see cref="CloseNoRewind"/>) and COBOL-85's REVERSED. It takes the mode rather
    /// than splitting into mode-specific entries because neither phrase's effect depends on WHICH admitted mode
    /// was written, and it takes the phrase as ONE enum rather than a bool per phrase for the reason
    /// <see cref="OpenTapePhrase"/> states: a per-phrase parameter is what let REVERSED stay unread for a year
    /// after NO REWIND got one (kb/Work PB317, kb/Work PB668). The medium determination itself belongs to
    /// <see cref="PhysicalFileCategory"/>, not to this facade.</summary>
    public static void OpenTape(string name, FileOpenMode mode, OpenTapePhrase tape, string assign,
        bool assignDynamic, LinagePage? page)
        => _reg.OpenTape(name, mode, tape, assign, assignDynamic, page);

    /// <summary>CLOSE the file (emitted for each closed file-name).</summary>
    public static void Close(string name) => _reg.Close(name);

    /// <summary>The IMPLICIT close (CANCEL §14.9.5 GR9 / INITIAL return §14.9.18 GR2 / run-unit close): only a
    /// connector "that is open" closes — a closed one is skipped, never stamped '42' (kb/Work PB154).</summary>
    public static void CloseIfOpen(string name) => _reg.CloseIfOpen(name);

    /// <summary>CLOSE … WITH LOCK — close, then prevent reopen (a subsequent OPEN is status 38).</summary>
    public static void CloseWithLock(string name) => _reg.CloseWithLock(name);

    /// <summary>CLOSE … REEL/UNIT — Table 14's <c>CLOSE UNIT</c> row (§14.9.6.4 GR3); on the Non-unit medium
    /// symbol e, the file staying open with status 07 (42 when it was not open).</summary>
    public static void CloseReelUnit(string name) => _reg.CloseReelUnit(name);

    /// <summary>CLOSE … REEL/UNIT FOR REMOVAL — Table 14's own <c>CLOSE UNIT FOR REMOVAL</c> row; its Non-unit
    /// cell is the same symbol e, its (b)/(c) cells add symbol d.</summary>
    public static void CloseReelUnitForRemoval(string name) => _reg.CloseReelUnitForRemoval(name);

    /// <summary>CLOSE … WITH NO REWIND — Table 14's <c>CLOSE WITH NO REWIND</c> row; on the Non-unit medium the
    /// cell is c,g, so the file IS closed and a successful close reports '07' (§9.1.13.2 item 6). The medium
    /// determination itself belongs to <see cref="PhysicalFileCategory"/>, not to this facade.</summary>
    public static void CloseNoRewind(string name) => _reg.CloseNoRewind(name);

    // ⛔ AND EVERY WRITE ENTRY CARRIES THE EXECUTING ELEMENT'S LINAGE PAGE, for the same reason and with the same
    // no-default discipline: §13.18.34 GR6 b) 2 and 3 read the operand values DURING a WRITE statement — the
    // ADVANCING PAGE one and the one that causes a page overflow — so the values belong to the element executing
    // that WRITE. `null` says the FD referenced by this statement has no LINAGE clause at all (kb/Work PB673).

    /// <summary>Plain <c>WRITE record</c> (ISO §14.9.51); <paramref name="length"/> is the varying-record length
    /// (ISO §13.18.43 GR13a), -1 = the record's own size.</summary>
    public static void Write(string name, string image, int length, LinagePage? page)
        => _reg.Write(name, image, length, page);

    /// <summary><c>WRITE record {BEFORE|AFTER} ADVANCING {n LINES | PAGE}</c>; <paramref name="lines"/> = -1 is PAGE.</summary>
    public static void WriteAdvancing(string name, string image, int lines, bool before, LinagePage? page)
        => _reg.WriteAdvancing(name, image, lines, before, page);

    /// <summary>Make <paramref name="drain"/> the holder of the one held-back print line of the connector of
    /// <paramref name="name"/> — the report writer's (ISO §13.18.35.4 GR3; kb/Work PB1247) — writing out any line
    /// a different holder still holds first (<see cref="FileConnector.HeldLineDrain"/>).</summary>
    internal static void HoldLine(string name, Action drain) => _reg.HoldLine(name, drain);

    // ⛔ No ungoverned READ / REWRITE / BEFORE-AND-AFTER-WRITE entry exists on this facade any more, and none may
    // come back (kb/Work PB683): the emitted code reaches those verbs ONLY through ReadShared / RewriteShared /
    // WriteShared, which decide record-lock governance where the OPEN statement's own SHARING phrase is visible
    // (§9.1.15) and govern EVERY connector — a miss on the posture map is §12.4.5.9.4 GR1 b) 2.'s implementor
    // default, not an ungoverned path (kb/Work PB669). The physical bodies live on the connectors, reached from
    // those governed entries. `Write` and `WriteAdvancing`
    // survive as the physical layer the runtime's own report writer and the unit tests drive directly.

    /// <summary>The file's LINAGE-COUNTER register (ISO §8.4.3.14 / §13.18.34 GR7).</summary>
    public static long LinageCounter(string name) => _reg.LinageCounter(name);

    /// <summary>The end-of-page condition of the file's most recent WRITE (ISO §14.9.51 GR26a/b).</summary>
    public static bool EndOfPage(string name) => _reg.EndOfPage(name);

    /// <summary>The length of the most recently read record (ISO §13.18.43 GR15).</summary>
    public static int LastReadLength(string name) => _reg.LastReadLength(name);

    /// <summary>The record the most recent successful READ made available, at its own length — what an
    /// OUT-OF-LINE record of the area receives (docs/CONFORMANCE.md §3 D-FRA; kb/Work PB981).</summary>
    public static string CurrentRecord(string name) => _reg.CurrentRecord(name);

    /// <summary>The EXTENT TABLE <see cref="CurrentRecord"/> was sent with — where each variable-length component of
    /// the variable-length group that wrote it ended (docs/CONFORMANCE.md §3 D-FRA (v); kb/Work PB1053); null when
    /// the record carries none. An out-of-line variable-length group record decomposes the current record by it.</summary>
    public static RecordExtents? CurrentRecordExtents(string name) => _reg.CurrentRecordExtents(name);

    /// <summary>The file's current FILE STATUS two-character code (ISO §9.1.13). "00" for an unknown name.</summary>
    public static string Status(string name) => _reg.Status(name);

    /// <summary>The EC-I-O level-3 exception-name the file's last I-O operation set to exist, or null — the
    /// generated <c>__IoCheckEc</c> hook's source for the raised name (ISO §9.1.13.1 + the rules that name their
    /// own condition; see <see cref="FileRegistry.IoConditionName"/>).</summary>
    public static string? IoConditionName(string name) => _reg.IoConditionName(name);

    /// <summary>The open-mode view for USE-declarative mode scoping (ISO §14.9.49.4 GR6b–e).</summary>
    public static int OpenModeOf(string name) => _reg.OpenModeOf(name);

    /// <summary>The open mode of a connector that IS OPEN, null otherwise (ISO §9.1.4) -- the
    /// §14.9.21.4 GR3 report-file-mode test. See <see cref="FileConnector.OpenModeIfOpen"/> for why this is
    /// not <see cref="OpenModeOf"/>.</summary>
    public static FileOpenMode? OpenModeIfOpen(string name) => _reg.OpenModeIfOpen(name);

    /// <summary>The AT END condition for a file (status 10).</summary>
    public static bool AtEnd(string name) => _reg.AtEnd(name);

    /// <summary>True when the last operation was unsuccessful (status not 00).</summary>
    public static bool Failed(string name) => _reg.Failed(name);

    /// <summary>Close every open file (emitted at run-unit termination, ISO §14.6).</summary>
    public static void CloseAll() => _reg.CloseAll();

    /// <summary>Stage the RELATIVE KEY item's value for the next keyed verb.</summary>
    public static void SetRelativeKey(string name, long rrn) => _reg.SetRelativeKey(name, rrn);

    /// <summary>The RRN last made available/released (§14.9.30 GR25 / §14.9.51 GR29a).</summary>
    public static long RelativeSlot(string name) => _reg.RelativeSlot(name);

    /// <summary>Keyed WRITE (§14.9.51) — returns the I-O status.</summary>
    public static string WriteKeyed(string name, string image, int length = -1) => _reg.WriteKeyed(name, image, length);

    /// <summary>Keyed REWRITE (§14.9.35) — returns the I-O status.</summary>
    public static string RewriteKeyed(string name, string image, int length = -1) => _reg.RewriteKeyed(name, image, length);

    /// <summary>DELETE RECORD (§14.9.10 F1).</summary>
    public static string DeleteRecord(string name, string keyedRecordImage) => _reg.DeleteRecord(name, keyedRecordImage);

    /// <summary>Sequential keyed READ [NEXT] (§14.9.30 F1).</summary>
    public static string ReadKeyedNext(string name, out string image) => _reg.ReadKeyedNext(name, out image);

    /// <summary>Sequential keyed READ PREVIOUS (§14.9.30 F1, COBOL-2002+).</summary>
    public static string ReadKeyedPrevious(string name, out string image) => _reg.ReadKeyedPrevious(name, out image);

    /// <summary>Random keyed READ (§14.9.30 F2).</summary>
    public static string ReadKeyed(string name, int keyIndex, string keyedRecordImage, out string image)
        => _reg.ReadKeyed(name, keyIndex, keyedRecordImage, out image);

    /// <summary>START on a relative file (§14.9.41 GR8–GR12).</summary>
    public static string StartRelative(string name, string op, long rrn) => _reg.StartRelative(name, op, rrn);

    /// <summary>START on an indexed file (§14.9.41 GR13–GR17); <paramref name="keyedRecordImage"/> is the RECORD
    /// AREA, which GR17 a) makes the source of the search key (kb/Work PB355).</summary>
    public static string StartIndexed(string name, int keyIndex, string op, string keyedRecordImage, StartKeyLength length,
        RecordExtents? areaExtents = null)
        => _reg.StartIndexed(name, keyIndex, op, keyedRecordImage, length, areaExtents);

    /// <summary>START FIRST/LAST (COBOL-2002+; §14.9.41 GR11/GR12), either keyed organization.</summary>
    public static string StartFirstLast(string name, bool last) => _reg.StartFirstLast(name, last);

    /// <summary>DELETE FILE (§14.9.10 Format 2, COBOL-2023); <paramref name="overridden"/> is the GR18 OVERRIDE
    /// phrase, which suppresses the fixed-file-attribute match.</summary>
    public static string DeleteFile(string name, bool overridden = false) => _reg.DeleteFile(name, overridden);

    /// <summary>FUNCTION EXCEPTION-FILE(file-connector-name) (ISO §15.28.4 r2) — the named connector's I-O status +
    /// SELECT-spelled name, or two spaces when never opened/attempted/accessed.</summary>
    public static string ExceptionFile(string name) => _reg.ExceptionFile(name);

    /// <summary>Declare a SELECTed file's record area NATIONAL (§14.9.30.4 GR15 — the short-record fill is then
    /// the national space character; kb/Work PB327).</summary>
    public static void RegisterNationalArea(string name) => _reg.RegisterNationalArea(name);

    /// <summary>Register the file's §13.18.13 CODE-SET conversion — see
    /// <see cref="FileRegistry.RegisterCodeSet"/>.</summary>
    public static void RegisterCodeSet(string name, char[] toNative) => _reg.RegisterCodeSet(name, toNative);

    /// <summary>Register a SELECTed file's declared SHARING / LOCK MODE (§12.4.5.15/§12.4.5.9). A null
    /// <paramref name="sharing"/> is the UNDETERMINED implementor default — a LOCK MODE clause is not a sharing
    /// specification (§9.1.15) — see <see cref="FileRegistry.ImplementorDefaultSharing"/> (kb/Work PB321/PB322).</summary>
    public static void RegisterSharing(string name, FileSharing? sharing, FileLockMode lockMode, bool multiple)
        => _reg.RegisterSharing(name, sharing, lockMode, multiple);

    /// <summary>OPEN with an explicit SHARING override and/or a RETRY phrase (§14.9.27). <paramref name="tape"/>
    /// carries the independent per-file-name TAPE PHRASE, which a sharing-phrase OPEN may also write
    /// (§14.9.27.2 prints both in one general format).</summary>
    public static void OpenShared(string name, FileOpenMode mode, bool hasSharingOverride, FileSharing sharingOverride,
        FileRetryKind retryKind, long retryAmount, OpenTapePhrase tape, string assign, bool assignDynamic, LinagePage? page)
        => _reg.OpenShared(name, mode, hasSharingOverride, sharingOverride, retryKind, retryAmount, tape,
            assign, assignDynamic, page);

    /// <summary>The ONE governed FORMAT-2 (random) keyed READ — relative and indexed (§9.1.16 /
    /// §14.9.30.4 GR9–GR12). Returns the I-O status; a record was made available iff it begins '0'.
    /// <paramref name="phrase"/> is the RETENTION bracket (WITH LOCK / WITH NO LOCK) and
    /// <paramref name="ignoringLock"/> the INDEPENDENT IGNORING LOCK phrase (§14.9.30.2's other bracket, GR12).
    /// A Format-1 read of any organization uses <see cref="ReadShared"/> instead.</summary>
    /// <para><paramref name="areaExtents"/> — here and on every entry below that takes a record-area image — is the
    /// record area's EXTENT TABLE when the record is a variable-length group (D-FRA (v); kb/Work PB1053): what
    /// frames the written record, and what locates a key the record's variable-length members precede.</para>
    public static string ReadKeyedShared(string name, int keyIndex, string keyedRecordImage, FileRecordLock phrase,
        bool ignoringLock, FileRetryKind retryKind, long retryAmount, out string image, RecordExtents? areaExtents = null)
        => _reg.ReadKeyedShared(name, keyIndex, keyedRecordImage, phrase, ignoringLock, retryKind, retryAmount, out image,
            areaExtents);

    /// <summary>The ONE governed FORMAT-1 READ — sequential, relative and indexed (§9.1.16 / §14.9.30.4 GR9–GR12
    /// and the GR22 ADVANCING ON LOCK skip-scan). Returns the I-O status; a record was made available iff it
    /// begins '0'. <paramref name="previous"/> is the READ's PREVIOUS direction (§14.9.30.2 Format 1);
    /// <paramref name="advancingOnLock"/> and <paramref name="ignoringLock"/> are two alternatives of the SAME
    /// printed bracket, so at most one is ever true; <paramref name="phrase"/> is the other bracket.</summary>
    public static string ReadShared(string name, bool previous, FileRecordLock phrase, bool advancingOnLock,
        bool ignoringLock, FileRetryKind retryKind, long retryAmount, out string image)
        => _reg.ReadShared(name, previous, phrase, advancingOnLock, ignoringLock, retryKind, retryAmount, out image);

    /// <summary>⛔ THE ONE WRITE ENTRY THE EMITTER RENDERS, every organization and every print-control shape
    /// (§14.9.51 GR10/GR11). <paramref name="advance"/> carries the statement's ADVANCING phrases as DATA —
    /// see <see cref="WriteAdvanceKind"/> for why a WRITE's presentation shape may not pick its own entry
    /// (kb/Work PB683).</summary>
    public static string WriteShared(string name, string image, int length, FileRecordLock phrase,
        FileRetryKind retryKind, long retryAmount, LinagePage? page, WriteAdvance advance = default,
        RecordExtents? areaExtents = null)
        => _reg.WriteShared(name, image, length, phrase, retryKind, retryAmount, page, advance, areaExtents);

    /// <summary>Governed REWRITE for a sharing-active connector, any organization (§14.9.35 GR11/GR12).</summary>
    public static string RewriteShared(string name, string image, int length, FileRecordLock phrase,
        FileRetryKind retryKind, long retryAmount, RecordExtents? areaExtents = null)
        => _reg.RewriteShared(name, image, length, phrase, retryKind, retryAmount, areaExtents);

    /// <summary>Governed DELETE RECORD for a sharing-active connector (§14.9.10 GR6/GR7).</summary>
    public static string DeleteShared(string name, string keyedRecordImage, FileRetryKind retryKind, long retryAmount,
        RecordExtents? areaExtents = null)
        => _reg.DeleteShared(name, keyedRecordImage, retryKind, retryAmount, areaExtents);

    /// <summary>DELETE FILE with a RETRY phrase (§14.9.10 GR15 — the '62' file-sharing conflict re-attempt) and
    /// the GR18 OVERRIDE flag.</summary>
    public static string DeleteFile(string name, FileRetryKind retryKind, long retryAmount, bool overridden = false)
        => _reg.DeleteFile(name, retryKind, retryAmount, overridden);

    /// <summary>UNLOCK file [RECORD[S]] (§14.9.47 GR1).</summary>
    public static void Unlock(string name, bool records) => _reg.Unlock(name, records);

    /// <summary>Acquire a record lock (§12.4.5.9 GR7 ceilings enforced; the CobolFileLockTests surface).</summary>
    public static string LockRecord(string name, string recId) => _reg.LockRecord(name, recId);

    /// <summary>True when <paramref name="recId"/> is locked by a connector OTHER than <paramref name="name"/>.</summary>
    public static bool IsLockedByOther(string name, string recId) => _reg.IsLockedByOther(name, recId);

    /// <summary>Release every record lock held by <paramref name="name"/> (UNLOCK, CLOSE).</summary>
    public static void ReleaseAllForConnector(string name) => _reg.ReleaseAllForConnector(name);

    /// <summary>Release a single record lock (the LOCK MODE single-lock discipline, §12.4.5.9 GR6).</summary>
    public static void ReleaseSingle(string name, string recId) => _reg.ReleaseSingle(name, recId);

    /// <summary>ISO §14.9.27.4 Table 19 — is an OPEN request unsuccessful against ONE connector already open on
    /// the same physical file? A null sharing mode is the undetermined implementor default (kb/Work PB322), and
    /// is arbitrated as a conflict only where every candidate mode agrees.</summary>
    public static bool Conflicts((FileSharing? Sharing, FileOpenMode Mode) ex, (FileSharing? Sharing, FileOpenMode Mode) inc)
        => FileRegistry.Conflicts(ex, inc);

    /// <summary>Resolve an ASSIGN target to a host file path: a target that already looks like a path (has a
    /// directory separator or an extension) is used verbatim; otherwise it becomes <c>&lt;lowercased&gt;.txt</c> in the
    /// current directory — the convention the legacy oracle uses, so the differential corpus finds the same file.</summary>
    public static string ResolveHostPath(string assignTarget)
    {
        // An EMPTY target identifies NO physical file, and the empty host path is exactly how a connector says
        // "not yet associated" (FileConnector.HostPath). A bare `ASSIGN USING data-name-1` file registers with one,
        // because §12.4.5.3 GR3 gives it no device-name-1/literal-1 to be associated with until an OPEN/SORT/MERGE
        // runs FileConnector.Associate. (Without this the empty target became the literal file ".txt".)
        if (assignTarget.Length == 0) return "";
        if (assignTarget.Contains('.') || assignTarget.Contains('/') || assignTarget.Contains('\\')) return assignTarget;
        return assignTarget.ToLowerInvariant() + ".txt";
    }
}
