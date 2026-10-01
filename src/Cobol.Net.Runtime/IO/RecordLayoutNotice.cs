// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>
/// The RUNTIME notice channel for a file whose bytes are PROVABLY inconsistent with the record description that
/// is about to read them — the support half of the V59 representation change (COBOLNET_DESIGN §14.4).
/// <para>
/// Why this exists: a fixed-length record-sequential file carries NO self-description IN ITS BYTES. If the record
/// description says 5 bytes and the file holds 8-byte records — the shape a data file written by a build before
/// V59 has, when a BINARY or PACKED field was stored as its zoned digits — every read after the first is
/// misaligned and the program computes on rubbish. The standard's own detection (§14.9.30.4 GR14/GR15 for the record area, §9.1.13.2 item 3 for the status: a record whose
/// byte count is outside the description's min/max is a SUCCESSFUL read with I-O status '04') fires only on the
/// trailing partial record, only if the total length is not a multiple, and only if the program inspects FILE
/// STATUS. That is too late and too quiet for a layout migration, so the arithmetic fact — a file length that is
/// not a whole multiple of the fixed record length — is reported at OPEN, before a single record is misread.
/// </para>
/// <para>
/// ⛔ THIS IS THE ONLY MECHANISM FOR A SEQUENTIAL RECORD-SIZE DISAGREEMENT, and it is not in competition with
/// the OPEN statement's attribute check. A sequential file records NO §9.1.6 fixed file attribute on the medium
/// — it is plain bytes, or plain text — so the §14.9.27.4 GR10 validated set for that organization is empty by
/// derivation rather than by exclusion (<c>SequentialConnector.FixedAttributeConflict</c>): §9.1.7.2 makes those
/// lengths a property of the data and of the reading program, and §9.1.13.2 item 3 answers a disagreement with a
/// successful '04' rather than a refused OPEN — so GR10's '39' never fires here and never preempts this notice.
/// GR10 covers the disagreements a format DOES record and a re-read cannot recover from (a keyed store's header,
/// a varying file's unparseable record-length prefix); this notice covers the one nothing records. One job, one
/// mechanism, on either side of that line.
/// </para>
/// <para>
/// It is also what SURVIVED owner decision 2026-09-07 (kb/Work PB802, "no sidecar of any type") for the case the
/// catalog used to answer: a RELATIVE or INDEXED store reopened through a SEQUENTIAL description now opens '00'
/// — the sequential format states nothing for the store to contradict — and this notice is what tells the user
/// on stderr that the byte count and the record description disagree.
/// </para>
/// <para>
/// It is a NOTICE, not a status change: OPEN still succeeds and the I-O status is untouched, because the standard
/// defines no OPEN condition for this and inventing one would be non-conforming. It goes to STANDARD ERROR so a
/// program's own output (which goldens compare) is never disturbed.
/// </para>
/// </summary>
public static class RecordLayoutNotice
{
    /// <summary>Emitted once per (file, host path) pair — a program that OPENs the same file in a loop should not
    /// produce a wall of identical notices.</summary>
    private static readonly HashSet<string> Reported = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Report a fixed-length record-sequential file whose byte length is not a whole multiple of its
    /// declared record length. <paramref name="hostPath"/> is the resolved host file — the name the ASSIGN clause
    /// produced, which is what the user can act on. No-op when the lengths agree or the file is empty.</summary>
    public static void CheckFixedLengthFile(string hostPath, long byteLength, int recordLength)
    {
        if (recordLength <= 0 || byteLength <= 0 || byteLength % recordLength == 0) return;
        lock (Reported)
            if (!Reported.Add(hostPath)) return;
        long whole = byteLength / recordLength;
        Console.Error.WriteLine(
            $"WiseOwl COBOL notice: file '{hostPath}' is {byteLength} bytes, which is not a whole multiple "
            + $"of the {recordLength}-byte record described for it ({whole} whole records + {byteLength % recordLength} "
            + "trailing bytes). The record description and the file's layout disagree; reads after the first record "
            + "will be misaligned. If this file was written by an earlier build, note that a BINARY or PACKED field "
            + "now occupies its true bytes (COMP 1-2-4-8-16, COMP-3 packed BCD) rather than one byte per digit.");
    }

    /// <summary>Test seam: forget which paths have been reported (the set is process-wide).</summary>
    public static void ResetForTests()
    {
        lock (Reported) Reported.Clear();
    }
}
