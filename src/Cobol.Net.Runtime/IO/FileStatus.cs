// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>The ISO/IEC 1989:2023 §9.1.13 I-O status codes the sequential connector reports (a focused subset of the
/// full table — the codes a sequential file can produce). The two-character code is stored into the FILE STATUS
/// data item after each I/O verb and steers the AT END / declarative branches.</summary>
public static class FileStatusCode
{
    /// <summary>00 — successful completion.</summary>
    public const string Success = "00";
    /// <summary>04 — a READ was successful but the physical record is shorter or longer than the min/max record
    /// length for the file's fixed attributes (ISO §9.1.13.2 item 3 / §14.9.30 GR14 — record-sequential only; the
    /// record is still delivered). Clarified in COBOL-2023 (Annex E.2 item 15), version-invariant behavior.</summary>
    public const string RecordLengthShortLong = "04";
    /// <summary>06 — a LINE-SEQUENTIAL READ found a record longer than the maximum record size; it is truncated on the
    /// right, the READ is SUCCESSFUL, and the file position indicator references the next unread character in the
    /// record so the following READ continues the remainder (ISO §14.9.30.4 GR15 + NOTE 3; §9.1.13.2 item 5).</summary>
    public const string LineRecordTooLong = "06";
    /// <summary>09 — a LINE SEQUENTIAL READ completed SUCCESSFULLY but the record area it delivered holds one or
    /// more characters outside the implementor-defined line sequential character set (ISO §14.9.30.4 GR16;
    /// §9.1.13.2 item 7). The record IS delivered — the first digit '0' makes this a successful completion, so
    /// the only exception condition it raises is the continuable EC-I-O-WARNING. The set is
    /// <see cref="LineSequentialCharacterSet"/> (Annex A.1 item 115; docs/CONFORMANCE.md DOC-A.1-115), the SAME
    /// set the '71' WRITE/REWRITE rules below use. ⛔ GR16 is stated AFTER GR15 and its only condition is "the
    /// execution of the READ statement is successful", so it also lands on a truncated ('06') read: '09' is the
    /// status a bad character produces whether or not the line was over-length (kb/Work PB329).</summary>
    public const string LineRecordInvalidCharRead = "09";
    /// <summary>05 — OPEN of an OPTIONAL file that is not present (created on OUTPUT/EXTEND/I-O, EOF on INPUT).</summary>
    public const string OptionalFileNotFound = "05";
    /// <summary>07 — ISO §9.1.13.2 item 6: <i>"An OPEN or CLOSE statement is successfully executed but a CLOSE
    /// statement with the NO REWIND, REEL/UNIT, or FOR REMOVAL phrase or an OPEN statement with the NO REWIND
    /// phrase references a physical file on a non-reel/unit medium."</i> ⛔ THIS STATUS IS THE MEDIUM
    /// DETERMINATION MADE OBSERVABLE: it exists only for a non-reel/unit medium, so every connector that can
    /// report it is §14.9.6.4 GR2 category (a) Non-unit (<see cref="PhysicalFileCategory"/>). It was two bare
    /// "07" literals in <see cref="FileRegistry"/> before kb/Work PB235.</summary>
    public const string PhraseOnNonReelMedium = "07";
    /// <summary>10 — end-of-file reached on a sequential READ (the AT END condition).</summary>
    public const string AtEnd = "10";
    /// <summary>30 — permanent I/O error with no more specific code.</summary>
    public const string PermanentError = "30";
    /// <summary>31 — ISO §9.1.13.6 item 2: "A permanent error exists during execution of an OPEN statement because
    /// the content of the data item referenced by the data-name specified in the USING phrase of the file control
    /// entry is not consistent with the specification for the device-name or literal in the ASSIGN clause of that
    /// file control entry." The status of §12.4.5.3 GR3's closing sentence — the OPEN/SORT/MERGE whose dynamic
    /// association cannot be made. Raised by <see cref="FileConnector.Associate"/>; the consistency rule it applies
    /// is the implementor's (§12.4.5.3 GR4, Annex A.1 items 10/73 — docs/CONFORMANCE.md §7, DOC-A.1-73).</summary>
    public const string AssignNotConsistent = "31";
    /// <summary>35 — OPEN INPUT/I-O/EXTEND on a non-optional file that is not present.</summary>
    public const string FileNotFound = "35";
    /// <summary>37 — OPEN failed: insufficient access permission.</summary>
    public const string PermissionDenied = "37";
    /// <summary>Classify a DELETE FILE IOException (ISO §14.9.10.4 GR17 vs §9.1.13.6 item 1): a storage medium
    /// that does not allow deletion — a write-protected volume (Windows ERROR_WRITE_PROTECT, HResult
    /// 0x80070013) or a read-only mount (Unix EROFS = errno 30, which .NET surfaces as the IOException's
    /// HResult) — is the '37' GR17 states; anything else is the generic permanent error '30'. Annex E.3.3
    /// item 35 lists '37' (never '30' from a medium refusal) among the statuses DELETE FILE sets. Public so
    /// the mapping is directly unit-testable (kb/Work PB140).</summary>
    public static string ForDeleteFileFailure(IOException ex) =>
        ex.HResult is unchecked((int)0x80070013) or 30 ? PermissionDenied
        // §9.1.13.9 2) — "a DELETE FILE statement is attempted on a physical file and that physical file is
        // currently open by another file connector": the host's sharing refusal, when the other connector is
        // not this run unit's (kb/Work PB860).
        : HostFile.IsSharingRefusal(ex) ? DeleteFileSharing
        : PermanentError;

    /// <summary>38 — OPEN of a file previously CLOSEd WITH LOCK (the ≤2014 CLOSE … WITH LOCK leg; NOT part of the
    /// 2002 5x/6x file-sharing family — that construct is COBOLNET0902-rejected at 2023 via the
    /// close-with-lock-removed-2023 gate, so 38 stays only for the still-legal ≤2014 path).</summary>
    public const string FileLocked = "38";
    /// <summary>39 — the file attribute conflict condition (ISO §9.1.13.6 item 7): an OPEN or DELETE FILE is
    /// unsuccessful because a conflict was detected between the physical file's fixed file attributes (§9.1.6)
    /// and the attributes specified for the file in the source unit. Produced by <c>FileConnector.Open</c> from
    /// §14.9.27.4 GR10's comparison, which asks each organization what ITS OWN FORMAT records
    /// (<c>FileConnector.FixedAttributeConflict</c> — the Annex A.1 item 129 validated set, stated as that rule)
    /// and compares it through <see cref="FixedFileAttributes.Conflicts"/>. DELETE FILE's own §14.9.10.4
    /// GR18 leg is separately (and emptily) defined at <c>FileRegistry.ValidateFixedFileAttributes</c>.</summary>
    public const string FixedAttributeConflict = "39";
    /// <summary>41 — OPEN attempted on an already-open file.</summary>
    public const string FileAlreadyOpen = "41";
    /// <summary>42 — CLOSE attempted on a file that is not open.</summary>
    public const string FileNotOpen = "42";
    /// <summary>43 — no successful READ immediately preceded a sequential REWRITE.</summary>
    public const string NoSuccessfulReadBeforeDeleteRewrite = "43";
    /// <summary>46 — no valid next record for a sequential READ (a prior READ was unsuccessful, no reposition).</summary>
    public const string NoValidNextRecord = "46";
    /// <summary>47 — READ/START on a file not open for INPUT or I-O.</summary>
    public const string ReadNotOpenForInput = "47";
    /// <summary>48 — WRITE on a file not open for OUTPUT, EXTEND, or I-O.</summary>
    public const string WriteNotOpenForOutput = "48";
    /// <summary>49 — DELETE/REWRITE on a file not open for I-O.</summary>
    public const string DeleteRewriteNotOpenForIO = "49";
    /// <summary>44 — record-size boundary violation (§9.1.13.6 item 2): a WRITE/REWRITE outside the RECORD IS
    /// VARYING bounds (§13.18.43 GR14 / §14.9.35 GR20), or a record-sequential REWRITE whose size differs from
    /// the record being replaced (§14.9.35 GR16).</summary>
    public const string RecordSizeViolation = "44";

    /// <summary>71 — a line-sequential WRITE (ISO §14.9.51.4 GR23) or REWRITE (§14.9.35.4 GR17 d) whose record area
    /// contains a character outside the implementor-defined line sequential character set. The statement is
    /// UNSUCCESSFUL and, per §9.1.13.10 item 1, "the record area remains unchanged" — nothing reaches the medium.
    /// The set is <see cref="LineSequentialCharacterSet"/> (Annex A.1 item 115; docs/CONFORMANCE.md DOC-A.1-115),
    /// the SAME set <see cref="LineRecordInvalidCharRead"/> ('09') applies on the READ side. ⛔ '7' is a FATAL
    /// first digit (§9.1.13.1 — EC-I-O-RECORD-CONTENT), so nothing corrupt is written. ⚠ With EC-I-O checking
    /// off, a program without a FILE STATUS item or a USE declarative today CONTINUES after the '71' — the open
    /// defect kb/Work PB322 (part E): GnuCOBOL, the rule-1 precedent for that latitude, terminates.</summary>
    public const string LineRecordInvalidChar = "71";

    /// <summary>02 — successful completion; a duplicate alternate record key was detected (ISO §9.1.13.2 item 2).</summary>
    public const string DuplicateAlternateKey = "02";
    /// <summary>14 — relative sequential READ: the RRN's significant digits exceed the RELATIVE KEY item (§9.1.13.4 item 2).</summary>
    public const string RelativeKeyOverflow = "14";
    /// <summary>21 — key sequence error on a sequentially-accessed indexed file (§9.1.13.5 item 1).</summary>
    public const string SequenceError = "21";
    /// <summary>22 — duplicate key: relative slot, prime key, or a no-DUPLICATES alternate (§9.1.13.5 item 2).</summary>
    public const string DuplicateKey = "22";
    /// <summary>23 — record not found on keyed access / keyed access to an absent optional file (§9.1.13.5 item 3).</summary>
    public const string RecordNotFound = "23";
    /// <summary>24 — invalid-key boundary violation (§9.1.13.5 item 4): a relative or indexed WRITE or REWRITE whose
    /// record the store cannot hold — the store would exceed <see cref="RecordFraming.MaxStoreBytes"/>, the
    /// boundary COBOL.NET defines for those organizations (Annex A.1 item 107, docs/CONFORMANCE.md DOC-A.1-107;
    /// §14.9.51.4 GR33 b) / GR42 d)) — or a sequential-access relative WRITE whose relative record number has
    /// more significant digits than the relative key item (§14.9.51.4 GR33 c)).</summary>
    public const string BoundaryViolation = "24";
    /// <summary>34 — permanent-error boundary violation: a sequential WRITE or REWRITE the host medium cannot
    /// store (§9.1.13.6 item 3, §14.9.51.4 GR20 — the boundary is the medium's capacity, Annex A.1 item 108,
    /// DOC-A.1-108, classified by <see cref="HostFile.IsMediumBoundary"/>), or a random/dynamic relative WRITE
    /// whose relative key is less than 1 or greater than <see cref="RelativeConnector.HighestRelativeRecordNumber"/>
    /// (§14.9.51.4 GR29 b)).</summary>
    public const string PermanentBoundary = "34";

    /// <summary>The I-O status of a WRITE or REWRITE whose bytes the host refused to store (kb/Work PB1192): the
    /// medium's boundary ('34', §9.1.13.6 item 3 — <see cref="HostFile.IsMediumBoundary"/>) or, for any other
    /// host failure, §9.1.13.6 item 1's '30' — never an escaping .NET exception.</summary>
    public static string ForWriteFailure(IOException ex) =>
        HostFile.IsMediumBoundary(ex) ? PermanentBoundary : PermanentError;

    // ── The COBOL-2002 file-sharing / record-locking status family (ISO §9.1.13.8/9; Phase 4d M2-FILE-1). The
    //    '5' first digit maps to EC-I-O-RECORD-OPERATION, '6' to EC-I-O-FILE-SHARING (§9.1.13.1) — both
    //    continuable; the EC bridge (ExceptionCatalog) already routes them, so producing the code is the whole job.
    /// <summary>51 — a record READ/REWRITE/DELETE could not lock the record because another file connector holds
    /// its lock (§9.1.13.8 item 1).</summary>
    public const string RecordLocked = "51";
    /// <summary>52 — the implementor-detected deadlock (§9.1.13.8 item 2; the detection conditions are the
    /// Annex A.1 item 109 determination recorded in docs/CONFORMANCE.md §7). COBOL.NET detects one exactly when
    /// a RETRY FOREVER waits on a record locked by another file connector: that holder is inside the executing
    /// run unit and cannot release while this statement runs, so §14.7.9.3 GR3's "until the operation has been
    /// completed" would never terminate.
    /// <para>⛔ This is a RECORD-conflict value only. A FILE SHARING conflict lands on its own §9.1.13.9 status
    /// ('61' OPEN / '62' DELETE FILE) — that clause defines no deadlock value, and answering '52' there raises
    /// EC-I-O-RECORD-OPERATION instead of EC-I-O-FILE-SHARING (§9.1.13.1). See
    /// <c>FileRegistry.ExhaustionStatus</c>, the ONE place the landing is decided.</para></summary>
    public const string Deadlock = "52";
    /// <summary>53 — the maximum number of record locks for the run unit has been exceeded (§9.1.13.8 item 3).</summary>
    public const string RunUnitLockLimit = "53";
    /// <summary>54 — the maximum number of record locks for this file connector has been exceeded (§9.1.13.8 item 4).</summary>
    public const string ConnectorLockLimit = "54";
    /// <summary>'90' — ⛔ THE FIRST OF THE TWO IMPLEMENTOR-DEFINED I-O STATUSES THIS COMPILER DEFINES (the other is
    /// <see cref="CharacterWithoutByteImage"/>'s '91'; ISO §9.1.13.11 item 1:
    /// <i>"I-O status = 9x. An implementor-defined condition exists. This condition shall not duplicate any other
    /// condition specified by another I-O status value. The value of x is defined by the implementor."</i>) — the
    /// LINAGE value-rule violation of §13.18.34.4 GR6 b): the page size is not greater than zero, or a specified
    /// footing start is not greater than zero or greater than the page size. Annex A.1 item 110 is the
    /// documentation obligation it carries; the determination is docs/CONFORMANCE.md §7 DOC-A.1-110.
    /// <para><b>Why 9x and not a value in another class.</b> §9.1.13.2–§9.1.13.10 enumerate every other class's
    /// values and none of them is this condition, so placing it in one would "duplicate another condition
    /// specified by another I-O status value" in the only sense that matters: the value would assert a condition
    /// class the standard defines as something else (a '5x' says the record is locked by another file connector;
    /// a '4x' is one of the nine enumerated logic errors). §9.1.13.11 is the slot the standard provides for
    /// exactly this, and '9' is a FATAL first digit under this implementation's own determination
    /// (<c>ExceptionCatalog.IsFatalIoStatus</c> — §9.1.13.1's <i>"any that begin with the digit 9 that the
    /// implementor defines as fatal"</i>), which is what Table 13's <b>Fatal</b> for EC-I-O-LINAGE requires.</para>
    /// <para>⛔ IT TRAVELS WITH AN EXCEPTION-NAME. §9.1.13.1's status→EC correspondence would read a '9x' as
    /// EC-I-O-IMP, and §13.18.34.4 GR6 b) 2 names EC-I-O-LINAGE, so the connector records the name beside the
    /// status (<see cref="FileConnector.IoConditionName"/>) and the generated hook reads the pair. A status
    /// alone cannot carry a condition the status table does not list.</para></summary>
    public const string LinageValueViolation = "90";

    /// <summary>'91' — the SECOND implementor-defined I-O status (§9.1.13.11 item 1; Annex A.1 item 110,
    /// docs/CONFORMANCE.md DOC-A.1-110): a WRITE or REWRITE whose record holds a character with no byte image in
    /// the file's coded character set — a character above <see cref="FileCharacterSet.Highest"/> written to a
    /// record sequential, report, relative or indexed file with no CODE-SET conversion (owner decision
    /// kb/Work R47; Annex A.1 item 31, DOC-A.1-31; kb/Work PB690). The statement is unsuccessful, nothing reaches
    /// the medium and the record area is unchanged — never the silent <c>?</c> Latin-1's replacement fallback
    /// used to write. A LINE SEQUENTIAL file answers the standard's own '71' instead
    /// (<see cref="LineRecordInvalidChar"/>): §9.1.13.10 item 1 is that organization's value for a record-area
    /// character outside its character set, and §9.1.13.11 forbids duplicating it.
    /// <para>Why 9x: §9.1.13.10 confines '71' to line sequential files; a '3x' permanent error would stay in
    /// effect on the connector (Annex A.1 item 105) although the next record may well be writable; every other
    /// class asserts a different condition. '9' is FATAL here (<c>ExceptionCatalog.IsFatalIoStatus</c>) and its
    /// §9.1.13.1 correspondence is EC-I-O-IMP.</para></summary>
    public const string CharacterWithoutByteImage = "91";

    /// <summary>61 — OPEN failed: a sharing conflict, based on the sharing mode of a previously-opened file
    /// connector or this OPEN's SHARING phrase, prevents the open (§9.1.13.9 item 1, sub-cases a–e).</summary>
    public const string FileSharingConflict = "61";
    /// <summary>62 — DELETE FILE failed: the file is currently open by another file connector (§9.1.13.9 item 2).</summary>
    public const string DeleteFileSharing = "62";
}
