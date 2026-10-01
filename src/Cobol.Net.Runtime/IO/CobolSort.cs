// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Runtime.IO;

/// <summary>
/// The in-memory sort/merge store (ISO/IEC 1989:2023 §14.9.40 SORT / §14.9.24 MERGE): one record-IMAGE buffer per
/// sort-merge (SD) file, keyed by COBOL file-name. Records cross this boundary as their character image (a
/// <see cref="string"/> at the record's released length) — the typed record ↔ image conversion stays in the
/// generated code, exactly like <see cref="CobolFile"/> (the substrate stays typed; COBOLNET_DESIGN §8.2: the sort
/// store holds serialized images, key windows are computed at compile time). The compiler emits
/// <c>Init → {Release… | NextInput+Release…} → Sort|Merge → {Return… (Rewind per GIVING file)} → Close</c>
/// around each SORT/MERGE statement — the three GR9 phases.
/// </summary>
public static class CobolSort
{
    /// <summary>⛔ THE COMPARISON CLASS of one key — the ONE thing that selects a key's comparator (ISO
    /// §14.9.40.4 GR5 / §14.9.24.4 GR5: "The alphanumeric collating sequence that applies to the comparison of key
    /// data items of class alphabetic and class alphanumeric, and the national collating sequence that applies to
    /// the comparison of key data items of class national, are each separately determined …" — TWO sequences,
    /// chosen by the KEY's class, and neither of them names class numeric or class boolean). GR8/GR19 defer the
    /// comparison itself to the relation-condition rules, so each arm is one of those clauses:
    /// §8.8.4.2.7 alphanumeric, §8.8.4.2.9 national, §8.8.4.2.8 boolean (by VALUE, never collated),
    /// §8.8.4.2.4 numeric (algebraic, never collated).</summary>
    public enum KeyClass
    {
        /// <summary>Class alphabetic / alphanumeric, and every GROUP key (§8.8.4.2.1 makes a group operand
        /// class alphanumeric): the byte window compared under the GR5 alphanumeric sequence.</summary>
        Alphanumeric,

        /// <summary>Class national (§8.8.4.2.9): the window's UTF-16BE byte PAIRS decode back to national
        /// character positions (§13.18.60.4 GR8, D-N1) and compare under the GR5 NATIONAL sequence.</summary>
        National,

        /// <summary>Class boolean (§8.8.4.2.8 — "a comparison of their boolean value, regardless of their usage"):
        /// the packed window compares position-wise with NO collating sequence.</summary>
        Boolean,

        /// <summary>Class numeric (§8.8.4.2.4): the window decodes through the key's own <see cref="NumProfile"/>
        /// and compares ALGEBRAICALLY — a collating sequence never applies.</summary>
        Numeric,
    }

    /// <summary>One compile-time key descriptor (ISO §14.9.40.3 SR6a/SR6e — keys are fixed BYTE windows of
    /// the SD record; the same positions are the key in EVERY record): the window (<paramref name="Offset"/>,
    /// <paramref name="Length"/>, both in bytes — a national position is two of them), the direction (GR8a/b),
    /// and the <see cref="KeyClass"/> that selects the comparator.
    /// <para>⛔ A KEY THAT FOLLOWS A VARIABLE-LENGTH MEMBER (kb/Work PB1025; docs/CONFORMANCE.md §3 D-KWV) carries
    /// its record type's <paramref name="Layout"/>, and <paramref name="Offset"/> is then its offset in the record's
    /// FIXED run: the record image the store holds is contiguous (§8.5.1.11.2), so the key's byte positions in THIS
    /// record are found by the same take step that decomposes it (<see cref="CobolContiguousLayout.Position"/>).
    /// Null for every key no variable-length member precedes — its window is the same in every record.</para></summary>
    public readonly record struct Key(int Offset, int Length, bool Descending, KeyClass Class, NumProfile Profile,
        CobolContiguousLayout? Layout = null)
    {
        /// <summary>The key's first byte position in <paramref name="image"/> — located through the record's own
        /// EXTENT TABLE when it was released with one that describes it (D-FRA (v); kb/Work PB1053), exactly where a
        /// RETURN of the record decomposes it.</summary>
        public int At(string image, RecordExtents? extents = null) =>
            Layout is { } l ? l.Position(image, Offset, extents) : Offset;
    }

    /// <summary>The per-SD store: released images (in release order — the stability anchor GR3 requires), the
    /// USING stream boundaries for MERGE, and the return cursor.</summary>
    private sealed class Store
    {
        /// <summary>Each released record with the extent table it was released with (D-FRA (v); kb/Work PB1053).</summary>
        public readonly List<StoredFrame> Records = [];
        public readonly List<int> StreamStarts = [];   // MERGE: index where each USING file's records begin
        public int Cursor;
        public int LastReturnedLength;
        /// <summary>The extent table of the most recently RETURNed record — null when it carries none.</summary>
        public RecordExtents? LastReturnedExtents;
        /// <summary>The ALPHANUMERIC collating sequence snapshotted at statement start (ISO §14.6.6 r5) — see
        /// <see cref="Init(string, CobolCollation?, CobolCollation?)"/>.</summary>
        public CobolCollation? Collation;
        /// <summary>Its NATIONAL twin — GR5 determines the two sequences SEPARATELY, so they snapshot
        /// separately and a statement may carry one, both or neither.</summary>
        public CobolCollation? NatCollation;
        /// <summary>Which procedure of the executing SORT/MERGE statement is running — the state §14.9.32.4 GR1
        /// ("within the range of an input procedure being executed by a SORT statement that references the
        /// file-name") and §14.9.34.4 GR1 ("within the range of an output procedure being executed by a MERGE or
        /// SORT statement that references file-name-1") test. The store exists only between <see cref="Init(string,
        /// CobolCollation?, CobolCollation?)"/> and <see cref="Close"/>, so "no store" is "no statement executing".</summary>
        public ProcedurePhase Phase;
        /// <summary>§14.9.34.4 GR3's latch: the at end condition has occurred for this file in the current output
        /// procedure, so a further RETURN is EC-SORT-MERGE-RETURN.</summary>
        public bool AtEndReached;
    }

    /// <summary>The procedure phase of an executing SORT/MERGE (see <see cref="Store.Phase"/>). <c>None</c> covers
    /// every part of the statement that runs no program procedure: the USING/GIVING transfers and the sequence
    /// phase — a USE declarative that runs from an implicit transfer is not in the range of either procedure.</summary>
    private enum ProcedurePhase { None, Input, Output }

    /// <summary>⛔ THE SORT/MERGE STATEMENT'S OWN RAISES ARE MARKED HERE (kb/Work PB1036). The five entries the
    /// statement itself executes — <see cref="Init(string, CobolCollation?, CobolCollation?)"/>, <see cref="FileNotOpen"/>,
    /// the implicit <see cref="Release(string, string, int, int, int)"/>, <see cref="Sort"/> and <see cref="Merge"/> —
    /// run their bodies under this exception FILTER, which sets <see cref="CobolFatalException.RaisedBySortMerge"/>
    /// and declines the catch, so the condition unwinds with its stack intact. ISO §14.6.13.1.3 2) ("If the
    /// executed statement is a MERGE or SORT statement, then the rules for those statements apply") keys on the
    /// statement that EXECUTED, and only this code knows it: an EC-SORT-MERGE-* condition, but equally an
    /// EC-LOCALE-* condition a locale-collated key raises while the sequence phase compares records. The RELEASE and
    /// RETURN statement entries are deliberately not marked — the executed statement there is the RELEASE or the
    /// RETURN — and neither is anything raised by a statement of an input or output procedure.</summary>
    private static bool RaisedByTheStatement(CobolFatalException e)
    {
        e.RaisedBySortMerge = true;
        return false;
    }

    private static readonly Dictionary<string, Store> Files = new(StringComparer.OrdinalIgnoreCase);

    private static Store Get(string name)
    {
        if (!Files.TryGetValue(name, out var f)) Files[name] = f = new Store();
        return f;
    }

    /// <summary>Begin a SORT/MERGE statement on <paramref name="name"/>: a fresh, empty store (ISO §14.9.40 GR9a —
    /// the release phase starts). Re-executing a SORT on the same SD reuses the connector with a clean buffer.</summary>
    public static void Init(string name) => Init(name, null, null);

    /// <summary>Begin a SORT/MERGE statement with its TWO GR5-resolved COLLATING SEQUENCES (the statement's own
    /// alphabet-name-1 / alphabet-name-2, else the program collating sequences, else null = native): each is
    /// SNAPSHOTTED here, at statement start (ISO §14.6.6 r5 — "A locale switch during execution of a SORT or MERGE
    /// statement has no effect on the processing of that SORT or MERGE statement" — a SET LOCALE in an INPUT
    /// PROCEDURE must not move the sequence the sort phase uses), and <see cref="Sort"/> / <see cref="Merge"/> use
    /// the snapshots. §14.9.40.4 GR5 determines the two SEPARATELY: <paramref name="collation"/> applies to key
    /// data items of class alphabetic and alphanumeric, <paramref name="national"/> to those of class national.
    /// <para>⛔ EC-SORT-MERGE-ACTIVE is tested HERE, before the store is touched (kb/Work PB1036): §14.9.40.4 GR10
    /// ("If the range of the input procedure causes the execution of any MERGE, RETURN, or format 1 SORT
    /// statements, the EC-SORT-MERGE-ACTIVE exception condition is set to exist"), GR13 and §14.9.24.4 GR8 (the
    /// output procedure's MERGE, RELEASE or format 1 SORT) all forbid a SORT/MERGE while any procedure of an
    /// executing SORT/MERGE runs — whatever file it names. A raise therefore leaves an enclosing statement on the
    /// same file intact. With checking off the results are undefined (GR10) and the statement runs.</para></summary>
    public static void Init(string name, CobolCollation? collation, CobolCollation? national)
    {
        try { InitCore(name, collation, national); }
        catch (CobolFatalException e) when (RaisedByTheStatement(e)) { }
    }

    private static void InitCore(string name, CobolCollation? collation, CobolCollation? national)
    {
        if (ExceptionState.SortMergeActiveChecking && AnyProcedureRunning(ProcedurePhase.Input, ProcedurePhase.Output))
            ExceptionState.SortMergeActiveError($"SORT/MERGE on {name}: executed within the range of an input or "
                + "output procedure of an executing SORT/MERGE statement (ISO §14.9.40.4 GR10 / GR13, §14.9.24.4 GR8)");
        var f = Get(name);
        f.Records.Clear();
        f.StreamStarts.Clear();
        f.Cursor = 0;
        f.LastReturnedLength = 0;
        f.LastReturnedExtents = null;
        f.Collation = collation?.Snapshot();
        f.NatCollation = national?.Snapshot();
        f.Phase = ProcedurePhase.None;
        f.AtEndReached = false;
    }

    /// <summary>Enter the SORT's INPUT PROCEDURE (<paramref name="output"/> false) or the SORT/MERGE's OUTPUT
    /// PROCEDURE (true) — emitted immediately before the bounded dispatch of the procedure range. The input phase
    /// ends when <see cref="Sort"/> begins the sequence phase; the output phase ends at <see cref="Close"/>.</summary>
    public static void EnterProcedure(string name, bool output)
    {
        var f = Get(name);
        f.Phase = output ? ProcedurePhase.Output : ProcedurePhase.Input;
        f.AtEndReached = false;
    }

    /// <summary>The RELEASE STATEMENT (ISO §14.9.32.4). GR1: "A RELEASE statement may be executed only when it is
    /// within the range of an input procedure being executed by a SORT statement that references the file-name
    /// associated with record-name-1. If it is executed at any other time, the EC-FLOW-RELEASE exception condition
    /// is set to exist." The test precedes the release, so a raise leaves the record unreleased; with checking off
    /// nothing is raised (§14.6.13.1.1) and the record is released as GR2 describes — a store with no executing
    /// SORT is discarded by the next <see cref="Init(string, CobolCollation?, CobolCollation?)"/>. The implicit
    /// USING transfer (§14.9.40.4 GR12 b) is not a RELEASE statement and uses <see cref="Release"/> (kb/Work PB349).
    /// <para>Two more tests precede the release (kb/Work PB1036). <b>EC-SORT-MERGE-ACTIVE</b>: a RELEASE within
    /// the range of any executing output procedure (§14.9.40.4 GR13, §14.9.24.4 GR8). ⚠ DETERMINATION: the same
    /// RELEASE also fails GR1, and §14.6.13.1.1 leaves which one is set to exist undefined ("if more than one
    /// exception is detected during the execution of a statement, the one that is set to exist is undefined");
    /// -ACTIVE, the rule written for exactly this range, is tested first, and GR1's general "any other time" only
    /// when -ACTIVE is not raised (docs/CONFORMANCE.md §3 D-SMA). <b>EC-SORT-MERGE-RELEASE</b>: the record's
    /// size — <paramref name="size"/>, the §13.18.43.4 GR13 a) DEPENDING ON value, else the image's own length
    /// (GR13 b)/c)) — outside <paramref name="min"/>..<paramref name="max"/>: "If a RELEASE statement is being
    /// executed, the EC-SORT-MERGE-RELEASE exception condition is set to exist and the execution of the RELEASE
    /// statement is unsuccessful" (§13.18.43.4 GR14 b) / GR19 b)), so a raise releases nothing. The record is
    /// sliced to <paramref name="size"/> HERE, after the test, never by an emitted reference modification — a
    /// DEPENDING ON value past the record area would otherwise raise EC-BOUND-REF-MOD, a condition no rule of the
    /// RELEASE names. With checking off the record releases at <paramref name="size"/> exactly as the lenient
    /// reference modification sliced it before (<see cref="Released"/>).</para></summary>
    /// <param name="extents">The record's EXTENT TABLE when a variable-length group record is released
    /// (determination D-FRA (v); kb/Work PB1053) — it travels with the record through the sort, as a file frame
    /// carries it.</param>
    /// <returns><see langword="true"/> when the record was released to the sort file — the condition this
    /// implementation gates the "also available as a record of other files referenced in the same SAME RECORD AREA
    /// clause" store on (§14.9.32.4 GR3 states no success condition of its own; WRITE and REWRITE name one,
    /// §14.9.51.4 GR4 and §14.9.35.4 GR6 — kb/Work PB1195); <see langword="false"/> when no SORT is executing and the
    /// record is discarded.</returns>
    public static bool ReleaseStatement(string name, string image, int min, int max, int? size = null,
        RecordExtents? extents = null)
    {
        if (ExceptionState.SortMergeActiveChecking && AnyProcedureRunning(ProcedurePhase.Output))
            ExceptionState.SortMergeActiveError($"RELEASE for sort file {name}: executed within the range of an "
                + "output procedure of an executing SORT/MERGE statement (ISO §14.9.40.4 GR13, §14.9.24.4 GR8)");
        if (!Files.TryGetValue(name, out var f) || f.Phase != ProcedurePhase.Input)
            ExceptionState.FlowReleaseError($"RELEASE for sort file {name}: not within the range of an input "
                + "procedure being executed by a SORT statement that references it (ISO §14.9.32.4 GR1)");
        image ??= "";
        int bytes = size ?? image.Length;
        if (bytes < min || bytes > max)
            ExceptionState.SortMergeReleaseError($"RELEASE for sort file {name}: a {bytes}-byte record is outside "
                + $"the record range {min} to {max} (ISO §13.18.43.4 GR14 b) / GR19 b))");
        if (f is null) return false;   // no SORT executing: there is no sort file to release to — never a stranded store
        f.Records.Add(new StoredFrame(size is { } s ? Released(image, s) : image, extents));
        return true;
    }

    /// <summary>The record area image at the §13.18.43.4 GR13 a) length <paramref name="size"/> — the lenient
    /// reference modification <c>(1:size)</c> this slicing replaced, kept byte-identical for checking off: a
    /// negative size is the whole area, zero is the zero-length record, and a size past the area space-extends.</summary>
    private static string Released(string image, int size) =>
        size < 0 ? image
        : size == 0 ? ""
        : size <= image.Length ? image[..size]
        : image.PadRight(size);

    /// <summary>Is a procedure of an executing SORT/MERGE statement — in one of <paramref name="phases"/> —
    /// running? The -ACTIVE rules (§14.9.40.4 GR10 / GR13, §14.9.24.4 GR8) name "the range of the input
    /// procedure" / "the range of the output procedure" of ANY executing statement, not only of the file the
    /// offending statement names. Asked only while EC-SORT-MERGE-ACTIVE checking is enabled.</summary>
    private static bool AnyProcedureRunning(params ReadOnlySpan<ProcedurePhase> phases)
    {
        foreach (var store in Files.Values)
            if (phases.Contains(store.Phase))
                return true;
        return false;
    }

    /// <summary>The RETURN STATEMENT (ISO §14.9.34.4). GR1: a RETURN "may be executed only when it is within the
    /// range of an output procedure being executed by a MERGE or SORT statement that references file-name-1. If it
    /// is executed at any other time, the EC-FLOW-RETURN exception condition is set to exist." GR3: "After the
    /// execution of imperative-statement-1 in the AT END phrase, no RETURN statement may be executed as part of
    /// the current output procedure. If such a RETURN statement is executed, the EC-SORT-MERGE-RETURN exception
    /// condition is set to exist and the results of the execution of the RETURN statement are undefined." Both
    /// tests precede the retrieval; with checking off the store's deterministic answer stands (at end again, or
    /// at end for a file with no executing statement). The implicit GIVING transfer uses <see cref="Return"/>.
    /// <para>EC-SORT-MERGE-ACTIVE is tested first (kb/Work PB1036): a RETURN within the range of any executing
    /// input procedure (§14.9.40.4 GR10), the same determination as <see cref="ReleaseStatement"/>'s — the rule
    /// written for that range wins over GR1's general "any other time" (docs/CONFORMANCE.md §3 D-SMA).</para></summary>
    public static bool ReturnStatement(string name, out string image)
    {
        if (ExceptionState.SortMergeActiveChecking && AnyProcedureRunning(ProcedurePhase.Input))
            ExceptionState.SortMergeActiveError($"RETURN for sort-merge file {name}: executed within the range of "
                + "an input procedure of an executing SORT statement (ISO §14.9.40.4 GR10)");
        if (!Files.TryGetValue(name, out var f) || f.Phase != ProcedurePhase.Output)
            ExceptionState.FlowReturnError($"RETURN for sort-merge file {name}: not within the range of an output "
                + "procedure being executed by a MERGE or SORT statement that references it (ISO §14.9.34.4 GR1)");
        else if (f.AtEndReached)
            ExceptionState.SortMergeReturnError($"RETURN for sort-merge file {name}: executed after the at end "
                + "condition in the current output procedure (ISO §14.9.34.4 GR3)");
        if (f is null)
        {
            image = "";   // no statement executing: nothing to make available — the at end path
            return false;
        }
        bool got = Return(name, out image);
        if (!got && f.Phase == ProcedurePhase.Output) f.AtEndReached = true;
        return got;
    }

    /// <summary>Mark the start of the next USING stream (MERGE only): records released after this call belong to
    /// the next file-name-2/-3 in statement order — the tie-break order ISO §14.9.24 GR4 prescribes.</summary>
    public static void NextInput(string name)
    {
        var f = Get(name);
        f.StreamStarts.Add(f.Records.Count);
    }

    /// <summary>RELEASE one record image at its released length (ISO §14.9.32 GR2; §14.9.40 GR12b for the implicit
    /// USING release) — the RELEASE STATEMENT's §14.9.32.4 GR1 test lives in <see cref="ReleaseStatement"/>, and
    /// this implicit release is not a RELEASE statement. Its one test is the size rule of §14.9.40.4 GR12 b) /
    /// §14.9.24.4 GR7 b) (kb/Work PB1036): "If the size of the record read from the file referenced by file-name-2
    /// is larger than the largest record allowed in the file description entry for file-name-1, the
    /// EC-SORT-MERGE-RELEASE exception condition is set to exist and the execution of the SORT statement is
    /// terminated", and the smaller-than-smallest twin when file-name-1 has variable-length records — so the
    /// caller passes <paramref name="min"/> 0 for a fixed-length sort-merge file. <paramref name="readSize"/> is
    /// the size the record had when it was READ, not the image's length after the fixed-length fit. A raise
    /// releases nothing and the SORT/MERGE statement's guard terminates the statement (§14.6.13.1.3 2)); with
    /// checking off the store accepts the record as the transfer shaped it. <paramref name="extents"/> is the
    /// record's EXTENT TABLE when a variable-length group record is released (D-FRA (v); kb/Work PB1053).</summary>
    public static void Release(string name, string image, int readSize, int min, int max, RecordExtents? extents = null)
    {
        try { ReleaseCore(name, image, readSize, min, max, extents); }
        catch (CobolFatalException e) when (RaisedByTheStatement(e)) { }
    }

    private static void ReleaseCore(string name, string image, int readSize, int min, int max, RecordExtents? extents)
    {
        if (readSize < min || readSize > max)
            ExceptionState.SortMergeReleaseError($"implicit release to sort-merge file {name}: a {readSize}-byte "
                + $"record is outside the record range {min} to {max} (ISO §14.9.40.4 GR12 b), §14.9.24.4 GR7 b))");
        Get(name).Records.Add(new StoredFrame(image ?? "", extents));
    }

    /// <summary>⛔ THE SORT/MERGE STATEMENT'S OWN ASSOCIATION (§12.4.5.3 GR3; kb/Work PB1097): <i>"The association
    /// occurs at the time of execution of an OPEN, SORT, or MERGE statement that referenced file-name-1"</i>, and
    /// when the content of data-name-1 is not consistent <i>"the OPEN, SORT, or MERGE statement is unsuccessful"</i>.
    /// A sort-merge file is the in-memory sort store (§13.4.6), never a host connector, so nothing else asks the
    /// question for it; the rule is <see cref="FileConnector.AssociationFailure"/>, the very one every OPEN
    /// applies (DOC-A.1-73). <see langword="false"/> = the association cannot be made: the statement is
    /// unsuccessful, which §3.176 defines as an attempt that "does not result in the execution of all the
    /// operations specified by that statement" — the emitted statement leaves before any phase runs. The sort-merge
    /// file entry has no FILE STATUS clause (§12.4.5.1 Format 4), so there is no I-O status to set.</summary>
    /// <param name="assign">The executing element's specification: data-name-1's content.</param>
    public static bool AssociationMade(string assign) =>
        FileConnector.AssociationFailure(assign, dynamic: true, out _) is null;

    /// <summary>The EC-SORT-MERGE-FILE-OPEN test for one USING or GIVING file (kb/Work PB1036), rendered where the
    /// rule places it: SORT at the start of the phase that processes the file — §14.9.40.4 GR9 a) "If the file
    /// referenced by file-name-2 is in an open mode when this phase commences" and its GR9 c) twin for
    /// file-name-3 — and MERGE for every USING and GIVING file "at the start of execution of the MERGE
    /// statement" (§14.9.24.4 GR7 / GR12). The test precedes the implicit OPEN, so a raise leaves the program's
    /// own connector exactly as it was. With checking off the results are undefined (SORT GR9) and the transfer
    /// proceeds: its implicit OPEN answers '41', which the transfer rule disposes of (kb/Work PB993).</summary>
    public static void FileNotOpen(string name, string file)
    {
        try { FileNotOpenCore(name, file); }
        catch (CobolFatalException e) when (RaisedByTheStatement(e)) { }
    }

    private static void FileNotOpenCore(string name, string file)
    {
        if (CobolFile.OpenModeIfOpen(file) is { } mode)
            ExceptionState.SortMergeFileOpenError($"SORT/MERGE on {name}: the USING/GIVING file {file} is open "
                + $"({mode}) (ISO §14.9.40.4 GR9, §14.9.24.4 GR7 / GR12)");
    }

    /// <summary>The sequence phase (ISO §14.9.40 GR9b): a STABLE key sort. Stability realizes GR3's DUPLICATES IN
    /// ORDER (equal keys keep USING-file / RELEASE order — the buffer holds them in exactly that order); without
    /// the phrase the relative order is undefined (GR4), so the stable result is conformant there too —
    /// <paramref name="duplicatesInOrder"/> is accepted for the call-site's traceability.</summary>
    public static void Sort(string name, Key[] keys, bool duplicatesInOrder)
    {
        try { SortCore(name, keys, duplicatesInOrder); }
        catch (CobolFatalException e) when (RaisedByTheStatement(e)) { }
    }

    private static void SortCore(string name, Key[] keys, bool duplicatesInOrder)
    {
        _ = duplicatesInOrder;   // stability is unconditional — GR3 satisfied, GR4 (undefined) safely refined
        var f = Get(name);
        f.Phase = ProcedurePhase.None;   // the sequence phase ends the input procedure (§14.9.40.4 GR9 a/b)
        int n = f.Records.Count;
        var idx = new int[n];
        for (int i = 0; i < n; i++) idx[i] = i;
        var columns = KeyColumns.Build(f.Records, keys, f.Collation, f.NatCollation);
        Array.Sort(idx, (x, y) =>
        {
            int c = columns.Compare(x, y);
            return c != 0 ? c : x - y;   // tie → original (release) order: the stable sort
        });
        var sorted = new List<StoredFrame>(n);
        foreach (int i in idx) sorted.Add(f.Records[i]);
        f.Records.Clear();
        f.Records.AddRange(sorted);
        f.Cursor = 0;
    }

    /// <summary>The merge operation (ISO §14.9.24 GR1): a k-way merge of the pre-sorted USING streams. Equal keys
    /// take the record from the EARLIEST stream first, and within one stream the records keep their file order —
    /// exactly GR4a/GR4b.
    /// <para>EC-SORT-MERGE-SEQUENCE (kb/Work PB1036): §14.9.24.4 GR6 — "If the records in the file referenced by
    /// file-name-2 and file-name-3 are not ordered as described in the ASCENDING or DESCENDING KEY clauses and the
    /// collating sequence associated with the MERGE statement, the EC-SORT-MERGE-SEQUENCE exception condition is
    /// set to exist, all files associated with the MERGE statement are closed, and the results of the merge
    /// operation are undefined." Each stream is checked against the SAME key columns the merge compares with, so
    /// "ordered" means exactly what the merge assumes. The raise precedes the merge; every USING file was closed by
    /// its implicit CLOSE before this phase and no GIVING file is opened until after it, so "all files … are
    /// closed" already holds. With checking off the scan is skipped and the k-way merge yields a deterministic
    /// stream-merge order, conformant within "undefined".</para></summary>
    public static void Merge(string name, Key[] keys)
    {
        try { MergeCore(name, keys); }
        catch (CobolFatalException e) when (RaisedByTheStatement(e)) { }
    }

    private static void MergeCore(string name, Key[] keys)
    {
        var f = Get(name);
        int streams = f.StreamStarts.Count;
        var pos = new int[streams];
        var end = new int[streams];
        for (int s = 0; s < streams; s++)
        {
            pos[s] = f.StreamStarts[s];
            end[s] = s + 1 < streams ? f.StreamStarts[s + 1] : f.Records.Count;
        }
        var merged = new List<StoredFrame>(f.Records.Count);
        var columns = KeyColumns.Build(f.Records, keys, f.Collation, f.NatCollation);
        if (ExceptionState.SortMergeSequenceChecking)
            for (int s = 0; s < streams; s++)
                for (int i = pos[s] + 1; i < end[s]; i++)
                    if (columns.Compare(i - 1, i) > 0)
                    {
                        ExceptionState.SortMergeSequenceError($"MERGE on {name}: record {i - pos[s] + 1} of USING "
                            + $"file {s + 1} is out of KEY order (ISO §14.9.24.4 GR6)");
                        break;
                    }
        while (true)
        {
            int best = -1;
            for (int s = 0; s < streams; s++)
            {
                if (pos[s] >= end[s]) continue;
                // STRICT less-than: an equal head never displaces an earlier stream's record (GR4a).
                if (best < 0 || columns.Compare(pos[s], pos[best]) < 0)
                    best = s;
            }
            if (best < 0) break;
            merged.Add(f.Records[pos[best]++]);
        }
        f.Records.Clear();
        f.Records.AddRange(merged);
        f.Cursor = 0;
    }

    /// <summary>RETURN the next record in key order (ISO §14.9.34 GR3): <see langword="true"/> with the record's
    /// image at its own length, or <see langword="false"/> at end — the UNCHECKED primitive the implicit GIVING
    /// transfer uses; the RETURN statement's GR1 / GR3 tests live in <see cref="ReturnStatement"/>. A post-end
    /// call deterministically reports at-end again (a conformant refinement of GR3's "undefined").</summary>
    public static bool Return(string name, out string image)
    {
        var f = Get(name);
        if (f.Cursor >= f.Records.Count)
        {
            image = "";
            f.LastReturnedLength = 0;
            f.LastReturnedExtents = null;
            return false;   // at end — the record area's content is undefined (GR3); the caller leaves it as-is
        }
        var returned = f.Records[f.Cursor++];
        image = returned.Image;
        f.LastReturnedLength = image.Length;
        f.LastReturnedExtents = returned.Extents;
        return true;
    }

    /// <summary>The length of the most recently RETURNed record — the value a varying SD's RECORD VARYING
    /// DEPENDING ON item receives (ISO §13.18.43 GR15: each returned record restores its own length).</summary>
    public static int LastReturnedLength(string name) => Get(name).LastReturnedLength;

    /// <summary>The EXTENT TABLE the most recently RETURNed record was released with (docs/CONFORMANCE.md §3 D-FRA
    /// (v); kb/Work PB1053) — what an out-of-line variable-length group record decomposes it by, and what the
    /// implicit GIVING WRITE frames it with. Null when the record carries none.</summary>
    public static RecordExtents? LastReturnedExtents(string name) => Get(name).LastReturnedExtents;

    /// <summary>Rewind the return cursor to the first record — emitted before EACH GIVING file's write-out, so
    /// every file-name-3/-4 receives the FULL sorted/merged result (ISO §14.9.40 GR15 / §14.9.24 GR12).</summary>
    public static void Rewind(string name) => Get(name).Cursor = 0;

    /// <summary>End the SORT/MERGE statement: drop the buffered records (the sort file has no persistent storage —
    /// ISO §9 sort-merge file model: only RELEASE/RETURN/SORT/MERGE ever reference it).</summary>
    public static void Close(string name) => Files.Remove(name);

    // ── Key comparison (ISO §14.9.40 GR8 / §14.9.24 GR3 — ONE policy for SORT and MERGE) ─────────────────────

    // ⛔ THE SEQUENCES ARE THE STORE'S, AND ONLY THE STORE'S. <see cref="Sort"/> and <see cref="Merge"/> used to
    // take their own copy of the collating arguments and fall back to it when the store held none, which made
    // "the store holds null" mean BOTH "the native order was snapshotted" and "nothing was snapshotted" — two
    // states one field cannot carry, and the emitted code passed the SAME expression to Init and to Sort anyway.
    // Init is the §14.6.6 r5 statement-start snapshot ("A locale switch during execution of a SORT or MERGE
    // statement has no effect on the processing of that SORT or MERGE statement"); the sequence phase reads it.

    /// <summary>The key columns of a record buffer, DECODED ONCE per record before the sort or merge compares
    /// anything (ISO §14.9.40 GR8 — the relation-condition comparison rules per key): a NUMERIC key column holds the
    /// records' algebraic values (GR8 / §8.8.4.2.4 — a collating sequence NEVER applies to a numeric comparison); a
    /// NATIONAL key column holds the records' DECODED national character positions (§8.8.4.2.9 compares national
    /// POSITIONS, and the record image carries each as a UTF-16BE byte pair — §13.18.60.4 GR8 / D-N1); a collated
    /// column under a sequence that supports keys (<see cref="CobolCollation.SupportsKeys"/> — the LOCALE arm)
    /// holds the records' materialized <see cref="Collation.CollationKey"/>s (built through the collator's
    /// <see cref="Collation.Cache.CollationKeyCache"/>, so records sharing a value share one key); any other
    /// character column holds the records' key windows, sliced once, compared under that key's own sequence (an
    /// ALPHABET table) or the native order. <see cref="Compare"/> then compares two records most significant key
    /// first; DESCENDING inverts the per-key result (GR8b); 0 ⇔ all keys equal (GR8c — the caller's stability or
    /// stream order then decides, GR3/GR4). Compared to re-slicing and re-walking every key at every comparison, an
    /// n-record sort does n key decodes instead of 2·n·log n.
    /// <para>⛔ THE SEQUENCE IS PER KEY, NOT PER STATEMENT (ISO §14.9.40.4 GR5 / §14.9.24.4 GR5): one SORT resolves
    /// an alphanumeric AND a national sequence and each key takes the one its CLASS names. A single per-statement
    /// sequence applied the alphanumeric weights to a national key's UTF-16BE bytes — right by accident in the
    /// native order (byte order IS code point order) and silently wrong under any declared alphabet
    /// (kb/Work PB678).</para></summary>
    private sealed class KeyColumns
    {
        private readonly Key[] _keys;
        private readonly Int128[]?[] _numeric;
        /// <summary>The unsigned 16-byte binary key columns (kb/Work PB186) — see <see cref="NumProfile.ImageExceedsInt128"/>.</summary>
        private readonly UInt128[]?[] _unsignedWide;
        private readonly double[]?[] _float;
        private readonly Collation.CollationKey[]?[] _collationKeys;
        private readonly string[]?[] _slices;
        /// <summary>The GR5-selected sequence of each key, parallel to <see cref="_keys"/> — null for the native
        /// order and for the two classes GR5 names no sequence for (numeric, boolean).</summary>
        private readonly CobolCollation?[] _seq;

        private KeyColumns(Key[] keys, Int128[]?[] numeric, UInt128[]?[] unsignedWide, double[]?[] floats, Collation.CollationKey[]?[] collationKeys, string[]?[] slices, CobolCollation?[] seq)
        {
            _keys = keys;
            _numeric = numeric;
            _unsignedWide = unsignedWide;
            _float = floats;
            _collationKeys = collationKeys;
            _slices = slices;
            _seq = seq;
        }

        public static KeyColumns Build(List<StoredFrame> records, Key[] keys, CobolCollation? collation, CobolCollation? national)
        {
            int n = records.Count;
            var numeric = new Int128[]?[keys.Length];
            var unsignedWide = new UInt128[]?[keys.Length];
            var floats = new double[]?[keys.Length];
            var collationKeys = new Collation.CollationKey[]?[keys.Length];
            var slices = new string[]?[keys.Length];
            var seq = new CobolCollation?[keys.Length];
            for (int k = 0; k < keys.Length; k++)
            {
                var key = keys[k];
                seq[k] = SequenceFor(key.Class, collation, national);
                // A FLOAT key (an Ieee-form profile, kb/Work PB164 wave 2) decodes through the IEEE lane and
                // compares as its ALGEBRAIC double value (§14.9.40.4 GR8 → §8.8.4.2.4 — a numeric key compares
                // by value regardless of usage; its raw big-endian IEEE bytes would order every negative after
                // every positive). double.CompareTo is a total order, so an exotic NaN payload cannot throw.
                if (key.Class is KeyClass.Numeric && key.Profile.ByteForm is NumericByteForm.Ieee32 or NumericByteForm.Ieee64)
                {
                    var col = new double[n];
                    for (int i = 0; i < n; i++) col[i] = CobolNum.ParseImageFloat(Slice(records[i], key), key.Profile);
                    floats[k] = col;
                }
                // An UNSIGNED 16-byte binary key (kb/Work PB186): its [0, 2^128) range does not fit the Int128
                // column — the signed decode returns a value at or above 2^127 as a NEGATIVE Int128, so ALL-ONES
                // would sort before 7. §14.9.40.4 GR8 / §14.9.24.4 GR3 compare keys "according to the rules for
                // comparison of operands in a relation condition", which for numeric operands is §8.8.4.2.4's
                // algebraic value, so the key decodes through the unsigned lane and orders as a UInt128.
                else if (key.Class is KeyClass.Numeric && key.Profile.ImageExceedsInt128)
                {
                    var col = new UInt128[n];
                    for (int i = 0; i < n; i++) col[i] = CobolNum.ParseImageU128(Slice(records[i], key), key.Profile);
                    unsignedWide[k] = col;
                }
                else if (key.Class is KeyClass.Numeric)
                {
                    var col = new Int128[n];
                    for (int i = 0; i < n; i++) col[i] = NumericKey(records[i], key);
                    numeric[k] = col;
                }
                else if (seq[k] is { SupportsKeys: true })
                {
                    var col = new Collation.CollationKey[n];
                    for (int i = 0; i < n; i++) col[i] = seq[k]!.KeyOf(Operand(records[i], key))!;
                    collationKeys[k] = col;
                }
                else
                {
                    var col = new string[n];
                    for (int i = 0; i < n; i++) col[i] = Operand(records[i], key);
                    slices[k] = col;
                }
            }
            return new KeyColumns(keys, numeric, unsignedWide, floats, collationKeys, slices, seq);
        }

        /// <summary>⛔ THE ONE class → collating-sequence selection (ISO §14.9.40.4 GR5 / §14.9.24.4 GR5): the
        /// ALPHANUMERIC sequence for keys of class alphabetic and alphanumeric, the NATIONAL sequence for keys of
        /// class national, and NO sequence for the two classes GR5 does not name — numeric keys compare
        /// algebraically (§8.8.4.2.4) and boolean keys compare by boolean value "regardless of their usage"
        /// (§8.8.4.2.8), so a declared alphabet must not reach either.</summary>
        private static CobolCollation? SequenceFor(KeyClass cls, CobolCollation? alnum, CobolCollation? national) => cls switch
        {
            KeyClass.Alphanumeric => alnum,
            KeyClass.National => national,
            _ => null,
        };

        /// <summary>Compare records <paramref name="x"/> and <paramref name="y"/> on every key, most significant first.</summary>
        public int Compare(int x, int y)
        {
            for (int k = 0; k < _keys.Length; k++)
            {
                int c;
                if (_numeric[k] is { } nums) c = nums[x].CompareTo(nums[y]);
                else if (_unsignedWide[k] is { } uws) c = uws[x].CompareTo(uws[y]);
                else if (_float[k] is { } fs) c = fs[x].CompareTo(fs[y]);
                else if (_collationKeys[k] is { } ck) c = ck[x].CompareTo(ck[y]);
                else
                {
                    var s = _slices[k]!;
                    c = _seq[k] is { } q ? q.Compare(s[x], s[y]) : CobolString.Compare(s[x], s[y]);
                }
                if (c != 0) return _keys[k].Descending ? -c : c;
            }
            return 0;
        }
    }

    /// <summary>The key's COMPARISON OPERAND out of a record image — the window for every class but national, and
    /// for a NATIONAL key the window's UTF-16BE byte pairs decoded back to national character positions through
    /// the ONE decoder <see cref="CobolBits.NatReadWindow"/>. §8.8.4.2.9 states the comparison over national
    /// character positions ("the length of an operand is the number of national character positions in the
    /// operand") while the record image stores two bytes per position (§13.18.60.4 GR8; D-N1), so comparing the
    /// raw window would weigh BYTES — and the high byte of every Latin national character is U+0000, which is how
    /// a national key silently compared EQUAL across whole records before kb/Work PB678.</summary>
    private static string Operand(StoredFrame record, in Key k) =>
        k.Class is KeyClass.National
            ? CobolBits.NatReadWindow(record.Image ?? "", k.At(record.Image ?? "", record.Extents),
                k.Length / CobolBits.BytesPerNational)
            : Slice(record, k);

    /// <summary>The key's character window of a record image. A record shorter than the window (a varying record
    /// — §14.9.40.3 SR6g requires keys within the MINIMUM size, so a conforming program never hits this; the
    /// lenient path space-extends, matching the §8.8.4.2.1 shorter-operand rule) is padded with spaces.</summary>
    private static string Slice(StoredFrame record, in Key k)
    {
        string image = record.Image ?? "";
        int at = k.At(image, record.Extents);
        int needed = at + k.Length;
        if (image.Length < needed) image = image.PadRight(needed);
        return image.Substring(at, k.Length);
    }

    /// <summary>Decode a numeric key window to its algebraic value through the KEY ITEM'S OWN profile — the ONE
    /// description of what those bytes are (<see cref="CobolNum.ParseImage"/>: zoned digits for USAGE DISPLAY,
    /// radix-2 / BCD for BINARY / PACKED, V59). A profile rebuilt from the window width alone could only ever
    /// describe a zoned key, which is how a COMP key would have sorted by its digit characters instead of its
    /// value. Scale is irrelevant for ordering: both operands of one key share one PICTURE, so the unscaled
    /// values order identically to the scaled ones.</summary>
    /// <summary>Decode a fixed-point numeric key's window with the leaf's own profile (zoned/radix-2/BCD —
    /// V59). A profile with NO byte form (<see cref="NumericByteForm.None"/> — no shipping usage since R40) throws
    /// the codec's loud invariant break rather than yielding an invented ordering; float keys and UNSIGNED 16-byte
    /// binary keys (<see cref="NumProfile.ImageExceedsInt128"/>, kb/Work PB186) take their own lanes in
    /// <see cref="KeyColumns.Build"/>, never this one.</summary>
    private static Int128 NumericKey(StoredFrame record, in Key k) => CobolNum.ParseImage(Slice(record, k), k.Profile);
}
