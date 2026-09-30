// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Binding.Bound;
using CobolNet.CodeGen.Emit;
using CobolNet.Runtime;

namespace CobolNet.CodeGen;

using static CobolNet.CodeGen.Emit.EmitText;

/// <summary>
/// SORT / MERGE / RELEASE / RETURN emission (ISO/IEC 1989:2023 §14.9.40 / §14.9.24 / §14.9.32 / §14.9.34; P7
/// Step 9e — a real collaborator over the per-unit <see cref="EmitContext"/>): the release / sequence / return
/// phases (SORT GR9) over the in-memory per-SD image store (<c>CobolNet.Runtime.IO.CobolSort</c>). USING/GIVING
/// route through the SAME runtime file verbs as explicit OPEN/READ/WRITE/CLOSE — the GR12/GR15 "as if" semantics,
/// so FILE STATUS (and the G6 USE declaratives when they land) apply to the implicit I/O for free. INPUT/OUTPUT
/// PROCEDURE run as bounded dispatcher ranges (<c>__Dispatch(start, end)</c>) — the dispatcher's bounded return
/// IS the GR11/GR14 compiler-inserted return mechanism. Format 2 sorts the typed element array in place with a
/// typed comparer (COBOLNET_DESIGN §8.2).
/// </summary>
internal sealed class SortEmitter(EmitContext ctx,
    SequentialIoEmitter seqIo, MoveEmitter move, ArithmeticEmitter arith, EcEmitter ec)
{
    /// <summary>The statement dispatcher — property-wired by <see cref="UnitEmitters"/> (the RETURN AT END /
    /// NOT AT END phrase bodies nest arbitrary statement lists, a cyclic edge no ctor order can satisfy).</summary>
    internal StatementEmitter Statements { get; set; } = null!;

    /// <summary>SORT Format 1 (ISO §14.9.40 GR9 — the three phases): (a) release — the USING files' records via
    /// implicit OPEN INPUT / READ / RELEASE / CLOSE (GR12), or the INPUT PROCEDURE as a bounded dispatch (GR11);
    /// (b) sequence — one stable key sort (GR8; stability = the GR3 DUPLICATES IN ORDER order, safe without the
    /// phrase per GR4); (c) return — the GIVING files via implicit OPEN OUTPUT / RETURN / WRITE / CLOSE, each file
    /// receiving the FULL sorted result (GR15), or the OUTPUT PROCEDURE whose RETURN statements pull records (GR14).</summary>
    public void EmitSort(BoundSort so)
    {
        var w = ctx.Writer;
        string sd = FileKeyExpr(so.File);
        var tx = new Transfer(TerminationLabel(), merge: false);
        w.Line($"{RuntimeApi.SortInit(sd, WeightsExpr(so.Collating), NatWeightsExpr(so.Collating))};   // SORT {so.File.CobolName} (ISO §14.9.40.4; BOTH GR5 sequences snapshotted — §14.6.6 r5)");
        using (StatementBody(sd))
        {
            // Phase a — release (GR9a). A USING file must not be open when the phase commences (GR9 —
            // EC-SORT-MERGE-FILE-OPEN, tested by the runtime before any implicit OPEN; kb/Work PB1036).
            EmitFilesNotOpen(sd, so.Using);
            if (so.Using.Count > 0)
                foreach (var input in so.Using)
                {
                    EmitInputFile(input, sd, so.File.RecordWidth, so.Varying, tx);
                    EmitInputClose(input, tx);   // GR12c — the release phase ends with each file's as-if CLOSE
                }
            else if (so.InputProcedure is { IsEmpty: false } ip)   // an EMPTY procedure releases nothing (kb/Work PB440)
            {
                w.Line($"{RuntimeApi.SortEnterProcedure(sd, output: false)};   // §14.9.32.4 GR1 — RELEASE is legal from here to the sequence phase");
                Statements.EmitProcedureRange(ip, "   // INPUT PROCEDURE (GR11 — the bounded return IS the inserted return mechanism)");
            }

            // Phase b — sequence (GR9b).
            w.Line($"{RuntimeApi.SortSort(sd, KeysExpr(so.Keys), so.DuplicatesInOrder ? "true" : "false")};   // the GR5 sequences are the Init snapshot's (§14.6.6 r5)");

            // Phase c — return (GR9c). A GIVING file must not be open when THIS phase commences — the input
            // procedure may have opened it, so the test is here and not at statement start (GR9; kb/Work PB1036).
            EmitFilesNotOpen(sd, so.Giving);
            if (so.Giving.Count > 0)
                foreach (var output in so.Giving)
                    EmitGivingFile(output, sd, tx);
            else if (so.OutputProcedure is { IsEmpty: false } op)
            {
                w.Line($"{RuntimeApi.SortEnterProcedure(sd, output: true)};   // §14.9.34.4 GR1 — RETURN is legal until the statement ends");
                Statements.EmitProcedureRange(op, "   // OUTPUT PROCEDURE (GR14 — RETURNs request the next sorted record)");
            }

            // §14.9.40.4 GR17's landing point: "the SORT statement is terminated" skips the REMAINING implicit
            // transfers and the phases after them — terminating the statement is not abandoning the run unit. It
            // is also where every FATAL implicit-transfer status lands (RuleFor — §9.1.13.1's "control is
            // transferred to the end of the statement", kb/Work PB993), so any SORT with a USING or GIVING file
            // jumps here; one with only procedures emits no label. The store's release is StatementBody's finally.
            if (tx.Terminable) w.Line($"{tx.EndLabel}: ;");
        }
    }

    /// <summary>⛔ THE SORT/MERGE STATEMENT'S BODY: everything after <c>Init</c>, with the store's release in a
    /// <c>finally</c> (kb/Work PB1036). Every way out of an executing SORT/MERGE must end it — the phases'
    /// normal end, the GR17 / PB993 termination label, a RESUME AT procedure-name that leaves through <c>__pc</c>,
    /// and a fatal EC-SORT-MERGE-* condition the runtime raises mid-statement, which the statement guard disposes
    /// of AFTER the stack has unwound past this point (§14.6.13.1.3 2)). A store left behind would still hold its
    /// procedure phase, and the next SORT/MERGE would read it as "a procedure of an executing statement is
    /// running" — an EC-SORT-MERGE-ACTIVE raised against a program that did nothing wrong. <c>Init</c> stays
    /// OUTSIDE the <c>try</c>: its -ACTIVE raise happens before this statement owns a store, and releasing one
    /// then would discard an enclosing statement's store on the same file.</summary>
    private FinallyScope StatementBody(string sd)
    {
        var w = ctx.Writer;
        w.Line("try");
        w.Line("{");
        w.Indent();
        return new FinallyScope(w, $"{RuntimeApi.SortClose(sd)};", "the statement ends — whichever way it ends");
    }

    /// <summary>The emitted-text scope <see cref="StatementBody"/> hands out: disposing it closes the <c>try</c>
    /// block and emits its one-line <c>finally</c>.</summary>
    private readonly struct FinallyScope(CodeWriter w, string finallyLine, string comment) : IDisposable
    {
        public void Dispose()
        {
            w.Outdent();
            w.Line("}");
            w.Line($"finally {{ {finallyLine} }}   // {comment}");
        }
    }

    /// <summary>The EC-SORT-MERGE-FILE-OPEN tests for <paramref name="files"/> (§14.9.40.4 GR9, §14.9.24.4 GR7 /
    /// GR12; kb/Work PB1036) — emitted only where the condition is enabled at this statement (§14.6.13.1.1: with
    /// checking off nothing is raised, so the checking-off output carries no test at all).</summary>
    private void EmitFilesNotOpen(string sd, IReadOnlyList<FileModel> files)
    {
        if (!ec.EnabledHere("EC-SORT-MERGE-FILE-OPEN")) return;
        foreach (var file in files)
            ctx.Writer.Line($"{RuntimeApi.SortFileNotOpen(sd, FileKeyExpr(file))};   // §14.9.40.4 GR9 / §14.9.24.4 GR7, GR12 — EC-SORT-MERGE-FILE-OPEN");
    }

    /// <summary>The end-of-statement label a USE procedure that did not complete normally jumps to — SORT's
    /// §14.9.40.4 GR17 termination, and the §14.9.33.4 GR2 a) 1. landing of a RESUME AT NEXT STATEMENT whose
    /// "applicable statement" is the SORT/MERGE rather than an I-O statement the program wrote. The `__`
    /// prefix keeps it out of the COBOL name space exactly as the dispatcher internals do.</summary>
    private string TerminationLabel() => $"__srtEnd{ctx.Names.NextSort()}";

    /// <summary>MERGE (ISO §14.9.24): obtain every USING file's records via the implicit OPEN INPUT / READ / CLOSE
    /// (GR7), k-way-merge the pre-sorted streams — equal keys keep USING-file order, all of one file before the
    /// next (GR4) — then write the FULL merged result to every GIVING file (GR12) or run the OUTPUT PROCEDURE
    /// (GR8/GR9). Unordered input is GR6's EC-SORT-MERGE-SEQUENCE, raised by the runtime's <c>Merge</c> when
    /// checking is enabled (kb/Work PB1036).</summary>
    public void EmitMerge(BoundMerge mg)
    {
        var w = ctx.Writer;
        string sd = FileKeyExpr(mg.File);
        var tx = new Transfer(TerminationLabel(), merge: true);
        w.Line($"{RuntimeApi.SortInit(sd, WeightsExpr(mg.Collating), NatWeightsExpr(mg.Collating))};   // MERGE {mg.File.CobolName} (ISO §14.9.24.4; BOTH GR5 sequences snapshotted — §14.6.6 r5)");
        using (StatementBody(sd))
        {
            // GR7 / GR12: "At the start of execution of the MERGE statement" no USING or GIVING file may be open —
            // one test point for both, unlike SORT's per-phase GR9 (kb/Work PB1036).
            EmitFilesNotOpen(sd, [.. mg.Using, .. mg.Giving]);
            // §14.9.24.4 GR7 c) (kb/Work PB1141): the USING file's as-if CLOSE — "If an output procedure is specified,
            // this termination is not performed until after control passes the last statement in the output
            // procedure." So with an OUTPUT PROCEDURE each file's FILE STATUS still holds the as-if READ's at end
            // '10' inside the procedure, and a CLOSE USE procedure runs after it; with GIVING (or no procedure) the
            // close follows the file's own release loop, as SORT's does.
            bool closeAfterOutputProcedure = mg.OutputProcedure is not null;
            foreach (var input in mg.Using)
            {
                w.Line($"{RuntimeApi.SortNextInput(sd)};   // a new pre-sorted USING stream (GR4 — file order breaks ties)");
                EmitInputFile(input, sd, mg.File.RecordWidth, mg.Varying, tx);
                if (!closeAfterOutputProcedure) EmitInputClose(input, tx);
            }
            w.Line($"{RuntimeApi.SortMerge(sd, KeysExpr(mg.Keys))};   // the GR5 sequences are the Init snapshot's; GR6's sequence test");
            if (mg.Giving.Count > 0)
                foreach (var output in mg.Giving)
                    EmitGivingFile(output, sd, tx);   // GR12 — each file-name-4 receives the WHOLE merged result
            else if (mg.OutputProcedure is { IsEmpty: false } op)
            {
                w.Line($"{RuntimeApi.SortEnterProcedure(sd, output: true)};   // §14.9.34.4 GR1 — RETURN is legal until the statement ends");
                Statements.EmitProcedureRange(op, "   // OUTPUT PROCEDURE (GR9)");
            }
            if (closeAfterOutputProcedure)
                foreach (var input in mg.Using)
                    EmitInputClose(input, tx);   // GR7 c) — after control passes the output procedure's last statement
            // MERGE has no GR17 of its own — only SORT's rule names the statement's termination — but the LANDING
            // rule is the same one: §14.9.33.4 GR2 a) 1. makes the applicable statement of a condition raised
            // inside an implicit transfer the MERGE itself, so a RESUME AT NEXT STATEMENT leaves the whole
            // statement here rather than falling into the next USING stream.
            if (tx.Terminable) w.Line($"{tx.EndLabel}: ;");
        }
    }

    /// <summary>The implicit USING transfer for one input file (SORT GR12 / MERGE GR7): OPEN INPUT, READ-loop
    /// releasing each record — through the ordinary runtime file verbs so status (and declaratives,
    /// later) behave exactly as for explicit I/O; the closing CLOSE is <see cref="EmitInputClose"/>. The loop ends on AT END or ANY unsuccessful read (a missing
    /// file's failed OPEN makes the first READ unsuccessful — never a spin). Each of the three as-if statements
    /// stores its own status and offers it to its own USE hook (kb/Work PB837); the file's FILE STATUS item then
    /// holds the final (CLOSE) status, the only value visible after the statement.</summary>
    private void EmitInputFile(FileModel input, string sdLit, int sdWidth, SortVaryingInfo? varying, Transfer tx)
    {
        var w = ctx.Writer;
        string f = FileKeyExpr(input);
        string tmp = $"__srt{ctx.Names.NextSort()}";
        // ISO §14.9.40.4 GR12 a) / §14.9.24.4 GR7 a) — the SHARING phrase of the as-if OPEN is CONDITIONAL on the
        // file control entry: "If the file-control entry for the file has a SHARING clause with the ALL phrase,
        // the initiation is performed as if an OPEN statement with the INPUT phrase and the SHARING WITH READ
        // ONLY phrase had been executed; otherwise, the initiation is performed as if an OPEN statement with the
        // INPUT phrase and WITHOUT a SHARING phrase is executed." The condition is a compile-time fact, so it is
        // decided HERE and never re-derived in the runtime (kb/Work PB714).
        EmitImplicitOpen(input, BoundOpenMode.Input,
            input.Sharing == SharingMode.AllOther ? SharingMode.ReadOnly : null,
            "implicit OPEN INPUT (ISO §14.9.40.4 GR12a / §14.9.24.4 GR7a)", TransferIo.UsingOpen, tx);
        // §14.9.40 GR12 b) / §14.9.24 GR7 b): "Each record is obtained as if a READ statement with the NEXT
        // phrase, the IGNORING LOCK phrase, and the AT END phrase had been executed." An IGNORING LOCK read is a
        // GOVERNED read (§14.9.30.4 GR12 is what suppresses the GR9 conflict), so it renders the ONE governed
        // Format-1 entry with `ignoringLock: true` — never an ungoverned one, which would answer '51' against
        // another connector's lock and truncate the transfer, and which could not see the sharing mode the
        // implicit OPEN established anyway (§9.1.15; kb/Work PB683). §14.9.30.4 GR11 c) still sets the AUTOMATIC lock
        // through it. The "false" is §14.9.30.4 GR19's NEXT: this retrieval is the forward walk of §14.9.43.4
        // ("the records … are transferred … in the order in which they are made available"), never a
        // statement-written direction — this loop renders no READ statement of the program's (kb/Work PB334).
        using (w.Block($"while ({RuntimeApi.FileReadSharedOk(f, "false", "FileRecordLock.None", "false", "true", "FileRetryKind.None", "0", tmp)})"))
        {
            // GR12 b) / MERGE GR7 b): a record READ larger than the SD's largest record — or, for a
            // variable-length SD, smaller than its smallest — is EC-SORT-MERGE-RELEASE, tested by the runtime
            // against the size the record had when READ (LastReadLength: the frame length on a varying input
            // file, the record width on a fixed one); a fixed SD passes min 0 (kb/Work PB1036). Fixed SD: the
            // short record space-fills right to the fixed length (GR7c/MERGE GR2c — the Store pad); varying SD:
            // each record releases at the size it was READ (GR12b; Read pads the area). The varying arm releases
            // the CURRENT RECORD itself (FileConnector.CurrentRecord — the area image sliced to LastReadLength
            // whenever the record fits the area, and the whole record when a variable-length record reaches past
            // the character area, determination D-FRA, kb/Work PB981), WITH the extent table it was read with
            // (D-FRA (v); kb/Work PB1053), so a variable-length record crosses the sort exactly as it crossed the file.
            var (min, max) = RecordRange(varying, sdWidth);
            w.Line(varying is not null
                ? $"{RuntimeApi.SortRelease(sdLit, RuntimeApi.FileCurrentRecord(f), RuntimeApi.FileLastReadLength(f), min, max, RuntimeApi.FileCurrentRecordExtents(f))};"
                : $"{RuntimeApi.SortRelease(sdLit, RuntimeApi.StrStore(tmp, $"{sdWidth}"), RuntimeApi.FileLastReadLength(f), min, max)};");
        }
        // ⛔ TWO as-if statements, TWO statuses, TWO hooks (kb/Work PB837). The loop above exits ONLY on an
        // unsuccessful retrieval, so the connector's status here IS that retrieval's — the as-if READ of
        // §14.9.40.4 GR12 b) / §14.9.24.4 GR7 b) — and it is offered to the USE procedure BEFORE GR12 c) / GR7 c)'s
        // CLOSE overwrites it: "These implicit functions are performed such that any applicable USE procedures are
        // executed" (GR12's closing paragraph), and §14.9.49.4 GR6 runs the procedures "upon the unsuccessful
        // execution of an input-output operation unless an AT END or INVALID KEY phrase takes precedence". The
        // as-if READ carries the AT END phrase, so the at end condition ('10') is the one status its hook passes
        // over (atEndHandled) — a '30', or the '47' GR2 of §14.9.30.4 gives a READ on a connector whose implicit
        // OPEN failed, reaches the declarative exactly as it would from an explicit READ. The one hook that used
        // to sit after the CLOSE read the CLOSE's status for both, so a failed retrieval followed by a successful
        // CLOSE ('00') ran no declarative at all.
        seqIo.EmitStoreFileStatus(input);
        EmitTransferHook(input, RuntimeApi.FileStatus(f), TransferIo.UsingRead, tx, atEndHandled: true);
    }

    /// <summary>The as-if CLOSE that ends one USING file's processing (SORT GR12 c) / MERGE GR7 c)) — its own step so
    /// MERGE can place it after the output procedure (kb/Work PB1141), and offered to its own USE hook.</summary>
    private void EmitInputClose(FileModel input, Transfer tx)
    {
        var w = ctx.Writer;
        string f = FileKeyExpr(input);
        w.Line($"{RuntimeApi.FileClose(f)};   // implicit CLOSE (GR12c / GR7c)");
        // The as-if CLOSE gets the hook an explicit CLOSE gets (SequentialIoEmitter.EmitClose): no AT END phrase
        // exists on a CLOSE, so nothing takes precedence over its declarative.
        seqIo.EmitStoreFileStatus(input);
        EmitTransferHook(input, RuntimeApi.FileStatus(f), TransferIo.UsingClose, tx);
    }

    /// <summary>The implicit GIVING transfer for one output file (SORT GR15 / MERGE GR12): REWIND the return
    /// cursor (EACH file receives the full result), OPEN OUTPUT, RETURN→WRITE loop, CLOSE. A fixed-length GIVING
    /// file space-fills a shorter returned record to its record width (GR16c / MERGE GR13c — the connector's
    /// fixed-width fit); a relative GIVING file's key sequence 1..n (GR15b) is the G5 relative slice.</summary>
    private void EmitGivingFile(FileModel output, string sdLit, Transfer tx)
    {
        var w = ctx.Writer;
        string f = FileKeyExpr(output);
        string tmp = $"__srt{ctx.Names.NextSort()}";
        w.Line($"{RuntimeApi.SortRewind(sdLit)};   // each GIVING file receives the FULL result (GR15 / MERGE GR12)");
        // ISO §14.9.40.4 GR15 a) / §14.9.24.4 GR12 a) — UNCONDITIONAL, unlike the USING arm: "The initiation is
        // performed as if an OPEN statement with the OUTPUT and SHARING WITH NO OTHER phrases had been executed."
        // §9.1.15 1) is what that buys: "The sharing with no other mode specifies exclusive access to a physical
        // file" (kb/Work PB714).
        // MERGE GR12 a)'s "processing for the file connector that caused the exception condition is bypassed"
        // skips THIS file's WRITE loop and CLOSE and goes on to the next file-name-4 (kb/Work PB993).
        tx.BypassLabel = $"__srtBy{ctx.Names.NextSort()}";
        tx.Bypassed = false;
        EmitImplicitOpen(output, BoundOpenMode.Output, SharingMode.NoOther,
            "implicit OPEN OUTPUT (ISO §14.9.40.4 GR15a / §14.9.24.4 GR12a)", TransferIo.GivingOpen, tx);
        using (w.Block($"while ({RuntimeApi.SortReturn(sdLit, tmp)})"))
        {
            // "Each record is written as if a WRITE statement without any optional phrases had been executed"
            // (GR15 b) / MERGE GR12 b) — through the ONE governed WRITE entry, like every other emitted WRITE
            // (kb/Work PB683). GR15 a) opens the file SHARING WITH NO OTHER, under which §9.1.15 1) ignores
            // record locks, so the governed body's release/acquire discipline is vacuous here — but the routing
            // decision is not the emitter's to make, and the runtime is where the open mode is known.
            string ws = $"__srw{ctx.Names.NextSort()}";
            // The returned record is written with the extent table it was released with (D-FRA (v); kb/Work PB1053).
            w.Line($"string {ws} = {RuntimeApi.FileWriteShared(f, tmp, "-1", "FileRecordLock.None", "FileRetryKind.None", "0", seqIo.LinageArg(output), areaExtents: RuntimeApi.SortLastReturnedExtents(sdLit))};   // implicit WRITE without optional phrases (GR15b)");
            // ⛔ EACH as-if WRITE owes its OWN hook (kb/Work PB837, the GIVING twin of the USING retrieval): GR15's
            // closing paragraph performs the implicit functions "such that any associated USE AFTER
            // EXCEPTION/ERROR procedures are executed", and the write has no phrase that could take precedence
            // (§14.9.49.4 GR6). A single hook after the CLOSE read the CLOSE's status for every failed write.
            // Emitted only on an unsuccessful write, so the per-record cost of a clean transfer is one test.
            using (w.Block($"if ({IoStatusClass.Unsuccessful(ws)})"))
            {
                seqIo.EmitStoreFileStatus(output);
                string? used = EmitTransferUse(output, TransferIo.GivingWrite, tx);
                // "On the first attempt to write outside the externally defined boundaries of the file, any USE
                // AFTER EXCEPTION procedure … is executed; if that USE procedure completes normally or if no such
                // USE procedure is specified, the processing of the file is terminated as in General rule 15c"
                // (MERGE GR12's paragraph: "as in General rule 12c") — the CLOSE below. The boundary is the one
                // write failure whose rule is specific, so it is tested BEFORE the general disposition (a '34' is
                // fatal by §9.1.13.1's class, and would otherwise terminate the whole statement).
                w.Line($"if ({IoStatusClass.WriteBoundary(ws)}) break;   // GR15 / MERGE GR12 — terminated as in GR15c");
                EmitDisposition(ws, used, TransferIo.GivingWrite, tx);
            }
        }
        w.Line($"{RuntimeApi.FileClose(f)};   // implicit CLOSE (GR15c)");
        seqIo.EmitStoreFileStatus(output);
        EmitTransferHook(output, RuntimeApi.FileStatus(f), TransferIo.GivingClose, tx);
        if (tx.Bypassed) w.Line($"{tx.BypassLabel}: ;   // MERGE GR12 a) — the bypassed file's processing ends here");
        tx.BypassLabel = null;
    }

    /// <summary>⛔ THE ONE IMPLICIT OPEN OF SORT AND MERGE — every <i>"as if an OPEN statement …"</i> initiation
    /// the standard writes, rendered WITH the SHARING phrase that rule names.
    /// <para>There are exactly FOUR such rules and all four reach this method: §14.9.40.4 GR12 a) and
    /// §14.9.24.4 GR7 a) for the USING transfer, §14.9.40.4 GR15 a) and §14.9.24.4 GR12 a) for the GIVING
    /// transfer. (SORT and MERGE share <see cref="EmitInputFile"/> / <see cref="EmitGivingFile"/>, so the verb
    /// axis needs no arm of its own; a search of the standard for <c>"as if an OPEN statement"</c> returns those
    /// four occurrences and nothing else, so a fifth one would have exactly one place to land.) The emitter used
    /// to render <c>CobolFile.OpenInput</c> / <c>OpenOutput</c> here — entry points with NO sharing parameter at
    /// all — so the registry arbitrated the transfer under the file's OWN SHARING clause and neither override
    /// ever took effect (kb/Work PB714).</para>
    /// <para><paramref name="sharing"/> <c>null</c> is the USING rule's <i>"without a SHARING phrase"</i> arm and
    /// is NOT the same as handing the phrase-bearing entry an override it would ignore. GR12 a) spells the
    /// difference out — <i>"The absence of the SHARING phrase means that the sharing mode is completely determined
    /// by the SHARING clause, if any, in the file control entry"</i> — and the phrase-bearing runtime entry also
    /// REGISTERS an otherwise-unregistered connector's record-locking posture, which for a phrase-less open would
    /// contradict §14.9.27.4 GR23 (kb/Work PB316). So the null arm renders the plain mode-specific entry, exactly
    /// as an unphrased explicit OPEN does, and the non-null arm renders the same shared entry the explicit
    /// <c>OPEN … SHARING</c> renders.</para>
    /// <para>The FILE STATUS store belongs to the as-if OPEN as much as the sharing phrase does: §9.1.13.1 sets
    /// the I-O status <i>"during the execution of a CLOSE, DELETE, OPEN, READ, REWRITE, START, UNLOCK or WRITE
    /// statement and prior to the execution of … any applicable exception processing statements"</i> and
    /// §12.4.5.8.4 GR1 updates the FILE STATUS item whenever the connector's I-O status is, so the USE procedure
    /// GR12 a) / GR15 a) invoke on the next line observes the status THIS open produced — a '61' among them. The
    /// value visible after the whole statement is still the implicit CLOSE's, which overwrites it.</para>
    /// <para>No RETRY and no NO REWIND: none of the four rules names either phrase, and §14.7.9.3 GR4 a) makes
    /// the absent RETRY phrase "no further attempt" — which is what <see cref="SequentialIoEmitter.RenderRetry"/>
    /// renders for a null spec, borrowed rather than re-spelled so the two OPEN sites cannot drift.</para></summary>
    private void EmitImplicitOpen(FileModel file, BoundOpenMode mode, SharingMode? sharing, string ruleComment,
        TransferIo io, Transfer tx)
    {
        var w = ctx.Writer;
        string f = FileKeyExpr(file);
        // §12.4.5.3 GR3 names SORT and MERGE beside OPEN, so the implicit open associates with the file the
        // ELEMENT EXECUTING THE SORT/MERGE names — its own ASSIGN specification and LINAGE operands (PB673).
        string elementArgs = seqIo.ExecutingElementArgs(file);
        if (sharing is { } sm)
        {
            var (retryKind, retryAmount) = seqIo.RenderRetry(null);
            w.Line($"{RuntimeApi.FileOpenShared(f, $"{RuntimeApi.FileOpenModeExpr(mode)}, true, "
                + $"{SequentialIoEmitter.RuntimeSharing(sm)}, {retryKind}, {retryAmount}, OpenTapePhrase.None, {elementArgs}")};"
                + $"   // {ruleComment}");
        }
        else
        {
            w.Line($"{RuntimeApi.FileOpen(f, mode, Binding.Bound.BoundOpenTapePhrase.None, elementArgs)};   // {ruleComment}");
        }
        seqIo.EmitStoreFileStatus(file);   // §9.1.13.1 / §12.4.5.8.4 GR1 — before the declarative, not after it
        // A failed implicit OPEN reaches a USE declarative (GR12a / GR15a); one that does not complete normally
        // terminates the SORT/MERGE (§14.9.40.4 GR17), and the verb's own rule then disposes of the status —
        // GR15's "If a fatal exception condition exists for file-name-3 as a result of the implicit OPEN during
        // file initiation, the SORT is terminated" among them (kb/Work PB993).
        EmitTransferHook(file, RuntimeApi.FileStatus(f), io, tx);
    }

    /// <summary>One SORT/MERGE statement's implicit-transfer emission state: the statement's end label (the
    /// landing of "the SORT/MERGE statement is terminated"), the verb, whether anything jumped to the label, and
    /// the current GIVING file's bypass label (MERGE GR12 a)).</summary>
    private sealed class Transfer(string endLabel, bool merge)
    {
        public string EndLabel { get; } = endLabel;
        public bool Merge { get; } = merge;
        public bool Terminable { get; set; }
        public string? BypassLabel { get; set; }
        public bool Bypassed { get; set; }
    }

    /// <summary>The as-if input-output statements of the implicit transfers — every statement GR12/GR15 (SORT) and
    /// GR7/GR12 (MERGE) perform "such that any applicable USE procedures are executed".</summary>
    internal enum TransferIo { UsingOpen, UsingRead, UsingClose, GivingOpen, GivingWrite, GivingClose }

    /// <summary>What an as-if statement's unsuccessful I-O status does to the statement, after its USE procedure.</summary>
    internal enum Disposition { Continue, Terminate, Bypass }

    /// <summary>A transfer rule: the disposition of a FATAL and of a NONFATAL unsuccessful status, each split on
    /// whether an applicable USE procedure ran and completed normally.</summary>
    internal readonly record struct TransferRule(Disposition FatalCompleted, Disposition FatalOtherwise,
        Disposition NonfatalCompleted, Disposition NonfatalOtherwise)
    {
        public bool NeedsCompletion => FatalCompleted != FatalOtherwise || NonfatalCompleted != NonfatalOtherwise;
    }

    /// <summary>⛔ THE ONE TABLE OF THE SORT/MERGE IMPLICIT-TRANSFER TERMINATION RULES (kb/Work PB993). Every as-if
    /// statement of every transfer reads its disposition HERE, so the verb axis (SORT/MERGE) and the statement
    /// axis (OPEN/READ/WRITE/CLOSE × USING/GIVING) are one lookup, never an arm per call site
    /// (<c>SortTransferRuleDriftTests</c> pins that every cell is answered).
    /// <para><b>The default is §9.1.13.1's.</b> A fatal status (3x/4x/7x, and 9x, which this implementation
    /// defines as fatal) is disposed of "after the execution of any applicable exception processing statement, or
    /// if none applies, after completion of the normal input-output control system error processing", and this
    /// implementation CONTINUES the run unit: "control is transferred to the end of the statement that produced the
    /// fatal exception condition unless the rules for that statement define other behavior". The statement that
    /// produced it is the SORT/MERGE — the as-if statement is not a statement of the program (§14.9.33.4 GR2 a) 1.)
    /// — so the default fatal disposition is TERMINATE. §14.6.13.1.3 2) ("If the executed statement is a MERGE or
    /// SORT statement, then the rules for those statements apply") puts these rules ahead of the run-unit
    /// termination §14.6.13.1.3 5)/7) would impose under checking, so the hook passes <c>__verbRule</c>. A
    /// nonfatal status continues (§14.6.13.1.4: "execution continues as if the exception did not occur unless there
    /// are one or more specific rules").</para>
    /// <para><b>The specific rules</b>, each overriding the default for its own cell:</para>
    /// <list type="bullet">
    /// <item>SORT GIVING OPEN — §14.9.40.4 GR15: "If a fatal exception condition exists for file-name-3 as a result
    ///   of the implicit OPEN during file initiation, the SORT is terminated" (the default, stated); the nonfatal
    ///   half continues with or without a USE procedure.</item>
    /// <item>SORT USING OPEN — §14.9.40.4 GR12 a): a nonfatal status continues "if there is an applicable USE
    ///   procedure that completes normally or if there is no applicable USE procedure"; GR12 b)'s "If a fatal
    ///   exception condition exists for file-name-1, the SORT is terminated" is the fatal half (⚠ DETERMINATION:
    ///   file-name-1 is the sort file, which has no I-O status, so the sentence can only mean the USING file whose
    ///   processing the rule describes — GR12 b)'s own "at end condition exists for file-name-1" has the same slip;
    ///   the reading agrees with the default either way).</item>
    /// <item>MERGE USING OPEN — §14.9.24.4 GR7 a): "If a nonfatal exception condition exists as a result of the
    ///   execution of the implicit OPEN statement, the MERGE statement is terminated unless there is an applicable
    ///   USE procedure that completes normally".</item>
    /// <item>MERGE USING CLOSE — GR7's closing paragraph: a nonfatal CLOSE continues with a USE that completes
    ///   normally and with none.</item>
    /// <item>MERGE GIVING OPEN — §14.9.24.4 GR12 a): "If a fatal exception condition exists as a result of this
    ///   implicit OPEN statement and there is an applicable USE procedure that completes normally, processing for the
    ///   file connector that caused the exception condition is bypassed"; without one, the default.</item>
    /// <item>MERGE GIVING WRITE — §14.9.24.4 GR12 b): "If an exception condition exists as a result of this implicit
    ///   WRITE statement and there is an applicable USE procedure that completes normally, the MERGE continues
    ///   execution, otherwise the MERGE statement is terminated" — fatal and nonfatal alike.</item>
    /// <item>The WRITE boundary ('24'/'34') — both verbs close the file (GR15 / MERGE GR12 closing paragraphs); it
    ///   is tested at the WRITE site before this table, because it is specific to one status value.</item>
    /// </list>
    /// <para>⚠ DETERMINATION — "an exception condition exists" means an UNSUCCESSFUL status (first character not
    /// '0'). A successful '0x' (an OPTIONAL file's '05', say) runs no USE procedure (§14.9.49.4 GR6: "upon the
    /// unsuccessful execution") and leaves the connector usable, so it never terminates a transfer.</para></summary>
    internal static TransferRule RuleFor(bool merge, TransferIo io) => (merge, io) switch
    {
        (true, TransferIo.UsingOpen) => new(Disposition.Terminate, Disposition.Terminate, Disposition.Continue, Disposition.Terminate),
        (true, TransferIo.GivingOpen) => new(Disposition.Bypass, Disposition.Terminate, Disposition.Continue, Disposition.Continue),
        (true, TransferIo.GivingWrite) => new(Disposition.Continue, Disposition.Terminate, Disposition.Continue, Disposition.Terminate),
        _ => new(Disposition.Terminate, Disposition.Terminate, Disposition.Continue, Disposition.Continue),
    };

    /// <summary>The as-if statement's USE hook and its disposition, in that order (the USE procedure runs first —
    /// every rule above is stated "after" it). <paramref name="status"/> is the C# expression of the status the
    /// as-if statement produced.</summary>
    private void EmitTransferHook(FileModel file, string status, TransferIo io, Transfer tx, bool atEndHandled = false)
    {
        string? used = EmitTransferUse(file, io, tx, atEndHandled);
        EmitDisposition(status, used, io, tx);
    }

    /// <summary>The as-if statement's USE hook: jumps to the end label when the procedure does not complete
    /// normally (§14.9.40.4 GR17; §14.9.33.4 GR2 a) 1.), and, only where the rule turns on it, declares the
    /// "an applicable USE procedure completed normally" local and returns its name.</summary>
    private string? EmitTransferUse(FileModel file, TransferIo io, Transfer tx, bool atEndHandled = false)
    {
        string? used = RuleFor(tx.Merge, io).NeedsCompletion ? $"__sru{ctx.Names.NextSort()}" : null;
        tx.Terminable |= seqIo.EmitUseHook(file, atEndHandled: atEndHandled, notNormalLabel: tx.EndLabel,
            verbDisposes: true, useCompletedVar: used).Terminable;
        return used;
    }

    /// <summary>Render <see cref="RuleFor"/>'s cell for one as-if statement: a fatal status, then any other
    /// unsuccessful one. A Continue cell emits nothing.</summary>
    private void EmitDisposition(string status, string? used, TransferIo io, Transfer tx)
    {
        var w = ctx.Writer;
        var rule = RuleFor(tx.Merge, io);
        string fatal = Render(rule.FatalCompleted, rule.FatalOtherwise, used, tx);
        string nonfatal = Render(rule.NonfatalCompleted, rule.NonfatalOtherwise, used, tx);
        string tag = $"   // {(tx.Merge ? "MERGE" : "SORT")} {io} — the implicit-transfer rule (kb/Work PB993)";
        if (fatal.Length > 0)
            w.Line($"if ({IoStatusClass.Fatal(status)}) {{ {fatal} }}{tag}");
        if (nonfatal.Length > 0)
            w.Line($"{(fatal.Length > 0 ? "else " : "")}if ({IoStatusClass.Unsuccessful(status)}) {{ {nonfatal} }}{(fatal.Length > 0 ? "" : tag)}");
    }

    private static string Render(Disposition completed, Disposition otherwise, string? used, Transfer tx)
    {
        string Jump(Disposition d)
        {
            switch (d)
            {
                case Disposition.Terminate: tx.Terminable = true; return $"goto {tx.EndLabel};";
                case Disposition.Bypass: tx.Bypassed = true; return $"goto {tx.BypassLabel ?? throw new InvalidOperationException("bypass outside a GIVING file")};";
                default: return "";
            }
        }
        if (completed == otherwise) return Jump(completed);
        string a = Jump(completed), b = Jump(otherwise);
        if (a.Length == 0) return $"if (!{used}) {b}";
        if (b.Length == 0) return $"if ({used}) {a}";
        return $"if ({used}) {a} else {b}";
    }

    /// <summary>RELEASE (ISO §14.9.32): FROM first MOVEs into the record (GR4 — identical to the explicit MOVE),
    /// then the record's character image goes to the sort store (GR2). A varying SD releases the leading
    /// DEPENDING-ON characters (§13.18.43 GR13 — the current value of the data item names the released length);
    /// a fixed SD releases the named record's image (a shorter secondary record space-fills via the sort store's
    /// fixed-compare space extension; §14.9.40 GR7c).</summary>
    public void EmitRelease(BoundRelease rl)
    {
        var w = ctx.Writer;
        if (rl.FromMove is { } fromMove) move.Emit(fromMove);   // GR4 a) — the BOUND implicit MOVE (PB348)
        string sd = FileKeyExpr(rl.File);
        string image = OperandText.RecordAreaImage(rl.Record);   // THE ONE record-area channel (kb/Work PB327)
        // §13.18.43.4 GR14 b) / GR19 b): a size outside the record range is EC-SORT-MERGE-RELEASE and the RELEASE
        // is unsuccessful — the runtime's test, before the release (kb/Work PB1036).
        var (min, max) = RecordRange(rl.Varying, rl.RecordWidth);
        if (rl.Varying is { Depending: { } dep })
        {
            // §13.18.43 GR13a: the released record's length = the RECORD VARYING DEPENDING ON item's current value.
            // The runtime slices the area to it AFTER the range test — an emitted reference modification would
            // raise EC-BOUND-REF-MOD for a value past the area, a condition no RELEASE rule names.
            w.Line($"{RuntimeApi.SortReleaseStatement(sd, image, min, max, $"(int){PlaceRenderer.CountRead(dep)}")};");
            return;
        }
        // GR13b/c (no DEPENDING — incl. a varying m-TO-n SD): the named record's own size; the image renders at
        // exactly that width, so the release carries it — with the record's extent table when it is a
        // variable-length group (D-FRA (v); kb/Work PB1053; the DEPENDING arm above sends a record cut to another
        // length, which no table describes). The STATEMENT entry: §14.9.32.4 GR1's phase test.
        w.Line($"{RuntimeApi.SortReleaseStatement(sd, image, min, max, extents: OperandText.RecordAreaExtents(rl.Record))};");
    }

    /// <summary>The record range the EC-SORT-MERGE-RELEASE tests use (kb/Work PB1036): a variable-length
    /// sort-merge file's smallest and largest record (§13.18.43.4 GR14 / GR19 — integer-2..integer-3 or
    /// integer-4..integer-5, defaulted per GR9/GR10 by <c>FileModel.VaryMin</c>/<c>VaryMax</c>), else 0 and the
    /// fixed record width — §14.9.40.4 GR12 b)'s smaller-than-smallest test applies only "If file-name-1 is
    /// specified with variable-length records".</summary>
    private static (int Min, int Max) RecordRange(SortVaryingInfo? varying, int recordWidth) =>
        varying is { } v ? (v.Min, v.Max) : (0, recordWidth);

    /// <summary>RETURN (ISO §14.9.34): pull the next record in key order into the SD record area (GR3); a varying
    /// SD restores the returned record's length into the DEPENDING item (§13.18.43 GR15); INTO then MOVEs the
    /// record area to the receiver (GR5 — skipped when at end); control goes to NOT AT END / AT END (GR3/GR4).
    /// At end the record area is undefined and a further RETURN in the same output procedure is
    /// EC-SORT-MERGE-RETURN (GR3); with that checking off the store deterministically reports at-end again.</summary>
    public void EmitReturn(BoundReturn rt)
    {
        var w = ctx.Writer;
        string sd = FileKeyExpr(rt.File);
        string tmp = $"__srt{ctx.Names.NextSort()}";
        // The STATEMENT entry — §14.9.34.4 GR1's phase test and GR3's after-at-end test precede the retrieval
        // (kb/Work PB349); the implicit GIVING loop renders the unchecked SortReturn.
        using (w.Block($"if ({RuntimeApi.SortReturnStatement(sd, tmp)})"))
        {
            // GR3 — made available in the record area: the returned record IS the current record, at its own length.
            seqIo.EmitRecordAreaStore(rt.File, rt.RecordArea, tmp, tmp, RuntimeApi.SortLastReturnedExtents(sd));
            if (rt.Varying is { Depending: { } dep })   // §13.18.43 GR15 — the length restored into DEPENDING
                arith.StoreArith(dep, new NumX(RuntimeApi.SortLastReturnedLength(sd), 0), CobolRounding.Truncation);
            // GR5 b) — RETURN then MOVE THE CURRENT RECORD → identifier-1, the move BOUND by
            // MoveBinder.BindIntoPhrase (kb/Work PB348): its sender is the record area sliced to the
            // §13.18.43.4 GR16 byte count through THE SAME builder both READ arms use (kb/Work PB339), and the
            // MOVE statement's own syntax rules have already been applied to it.
            if (rt.IntoMove is { } intoMove) move.Emit(intoMove);
            if (rt.NotAtEnd is { } not) Statements.EmitStatementList(not);
        }
        // §14.9.34.4 GR3 — the at end condition: control transfers to imperative-statement-1, and on return from
        // it to the end of the statement. ⚠ The null guard is now DEFENSIVE ONLY, and must not become an empty
        // else that reads as licensed: §14.9.34.2 prints the AT END line unbracketed, so the phrase is required
        // and COBOLNET1850 (SortBinder.BindReturn → StatementValidation.ScreenOmittedRequiredPhrase) rejects the
        // program before codegen. An empty arm here is exactly the wrong answer kb/Work PB350 reports — control
        // falling through onto a record area the same rule leaves undefined.
        using (w.Block("else"))
        {
            if (rt.AtEnd is { } at) Statements.EmitStatementList(at);
        }
    }

    /// <summary>SORT Format 2 — the in-place TABLE sort (ISO §14.9.40 GR18–GR24): a stable
    /// (<c>Enumerable.OrderBy</c>) typed-comparer sort over the element array, copied back into the table (GR24).
    /// Stability preserves the pre-sort relative order of equal keys (GR3c DUPLICATES IN ORDER; without the phrase
    /// the order is undefined, GR4 — stable is conformant). Numeric keys compare by value (GR19 → the relation-
    /// condition rules, §8.8.4.2 — never collated); character keys compare under the §14.9.40.4 GR5-resolved sequence.</summary>
    public void EmitTableSort(BoundTableSort ts)
    {
        var w = ctx.Writer;
        int id = ctx.Names.NextSort();
        // The element's STORAGE type is final only after the post-bind whole-group analysis (StoreAsImage) —
        // read it now, at emit time, never at bind time.
        string elem = ts.Table.ElementType;
        // The GR5 carriers are materialized BEFORE the comparer lambda (a local declared inside it would be
        // rebuilt on every comparison) and only for the classes this statement's keys actually use. `declared`
        // is scoped to THIS statement: two keys of one class share the one carrier, and the next statement's
        // carriers are named off its own `id`, so nothing crosses the boundary.
        var declared = new HashSet<string>();
        var weightsArg = ts.Keys
            .Select(k => TableWeightsArg(ts.Collating, CollatingSelection.Of(k.Key.OperandPic), id, declared))
            .ToList();
        w.Line($"var __ta{id} = {ts.ArrayPath};   // SORT table (ISO §14.9.40.4 Format 2 — in place, GR18/GR24)");        w.Line($"System.Comparison<{elem}> __tc{id} = (__a, __b) =>");
        w.Line("{");
        w.Indent();
        w.Line("int __c;");
        for (int i = 0; i < ts.Keys.Count; i++)
        {
            var key = ts.Keys[i];
            // GR2: key significance = statement order; GR19a/b: DESCENDING inverts the per-key result.
            w.Line($"__c = {TableCompare(key, "__a", "__b", weightsArg[i])};");
            w.Line($"if (__c != 0) return {(key.Descending ? "-__c" : "__c")};");
        }
        w.Line("return 0;   // GR19c — equal on every key; OrderBy stability keeps the pre-sort order (GR3c)");
        w.Outdent();
        w.Line("};");
        // Through CobolTable.SortInPlace, never a bare Enumerable.OrderBy: the framework's array sort re-throws a
        // comparer's exception as InvalidOperationException, which would hide a key comparison's fatal COBOL
        // exception condition from the statement guard (kb/Work PB230 — measured, not deduced).
        // GR20 — only the CURRENT occurrences are sorted and placed back (GR24): an OCCURS DEPENDING table's
        // data-name-1 count (clamped, EC-BOUND-ODO outside — §13.18.38.4 GR7), a dynamic-capacity table's current
        // capacity, a fixed table's whole array (kb/Work PB1174 — the physical array used to be sorted).
        string occurrences = ts.Count is AllCount.Capacity
            ? RuntimeApi.TableCurrentOccurrences($"__ta{id}", null)
            : RuntimeApi.TableCurrentOccurrences($"__ta{id}", PlaceRenderer.OccurrenceCount(ts.Count));
        w.Line($"{RuntimeApi.TableSortInPlace(occurrences, $"__tc{id}")};   // GR24 — placed back in data-name-2");
    }

    /// <summary>The shorter-operand extension a BOOLEAN comparison takes (ISO §8.8.4.2.8 rule 2 — "as though the
    /// shorter operand were extended on the right by sufficient boolean zeros"), spelled exactly as the
    /// relation-condition renderer spells it. Never combined with a collating sequence: GR5 names no sequence for
    /// class boolean, so the two arguments are mutually exclusive by construction.</summary>
    private const string BooleanPad = ", pad: '0'";

    /// <summary>One table-sort key's <c>int</c> comparison expression over element variables <paramref name="a"/>
    /// and <paramref name="b"/>, dispatched on the key's CLASS (ISO §14.9.40 GR19 → the relation-condition rules):
    /// numeric keys compare by VALUE (§8.8.4.2.4 — never through a collating sequence; an image-stored zoned leaf
    /// decodes via its profile), national keys compare their national character positions under the GR5 national
    /// sequence (§8.8.4.2.9), boolean keys compare by boolean value with the shorter operand extended by boolean
    /// ZEROS and no sequence (§8.8.4.2.8), and alphanumeric/ordinary-group keys compare as characters under the GR5
    /// alphanumeric sequence (§8.8.4.2.7). <paramref name="weightsArg"/> already carries that choice.</summary>
    private static string TableCompare(BoundTableSortKey key, string a, string b, string weightsArg)
    {
        string pa = key.MemberPath.Length == 0 ? a : $"{a}.{key.MemberPath}";
        string pb = key.MemberPath.Length == 0 ? b : $"{b}.{key.MemberPath}";
        DataItem k = key.Key;
        // ⛔ A GROUP-USAGE GROUP IS AN ELEMENTARY OPERAND (§13.18.29.4 GR1b/GR2b; D20/PB79) AND MUST NOT TAKE THE
        // AsImage() ARM BELOW (kb/Work PB678, the shape kb/Work PB327 fixed one channel over): a NATIONAL group's
        // operand value is its m national POSITIONS — the generated AsNat() — never AsImage()'s 2m UTF-16BE bytes;
        // a BIT group's is its boolean positions — AsBits() — never the packed bytes. Reading those through
        // AsImage() would compare a national group against the ALPHANUMERIC weights of its byte pairs, which is
        // exactly the defect this dispatch removes, one category over.
        if (k.IsAsIfElementary)
            return k.GroupUsage is GroupUsage.National
                ? RuntimeApi.StrCompare($"{pa}.AsNat()", $"{pb}.AsNat()", weightsArg)
                : RuntimeApi.StrCompare($"{pa}.AsBits()", $"{pb}.AsBits()", BooleanPad + weightsArg);
        // §8.8.4.2.8 — a boolean comparison extends the shorter operand on the RIGHT with boolean zeros, and
        // takes no collating sequence (weightsArg is empty for the boolean class).
        if (k.OperandPic is { Category: PicCategory.Boolean })
            return RuntimeApi.StrCompare(pa, pb, BooleanPad + weightsArg);
        // ⛔ V59 RESIDUE FIX (DA5): IsImageCapable, not the pre-V59 IsCharacterImage. §14.9.40.4 GR8
        // (`cite.py`-verified) makes a key comparison IDENTICAL to a relation condition — key data items are
        // "compared according to the rules for comparison of operands in a relation condition" — and a GROUP
        // operand in a relation condition is class alphanumeric (§8.8.4.2.1) compared over its
        // representation, which `OperandText` already renders through `AsImage()` gated on IsImageCapable. Testing
        // the stricter predicate here therefore made SORT and `IF a > b` DISAGREE about the very same two group
        // operands: the IF compared their byte images while the SORT threw "no whole-group character image" —
        // a claim V59 falsified, since the codec is emitted for exactly IsImageCapable items. A group with a
        // dynamic member or pointer/object-class leaf is still genuinely imageless and stays loud (kb/Work
        // PB164 + R40 — every NUMERIC leaf kind joined the image), so the wording matches the predicate tested.
        // ⚠ NOT A BUG, AND DELIBERATE: a big-endian two's-complement image is NOT order-preserving across zero,
        // so a group key holding a NEGATIVE binary leaf orders by its bytes rather than its value. That is what
        // the standard prescribes — GR8 defers to the relation-condition rules, and those make a GROUP
        // alphanumeric — and it is exactly what the same group compared with IF already does. A program wanting
        // value order names the ELEMENTARY numeric item as the key, which takes the by-value arm below.
        // (Reaching here the group is an ORDINARY one — the GROUP-USAGE arms above own the national and bit
        // groups, which are elementary operands of their own class rather than alphanumeric byte images.)
        if (k.IsGroup)
            return k.IsImageCapable
                ? RuntimeApi.StrCompare($"{pa}.AsImage()", $"{pb}.AsImage()", weightsArg)
                : LoudValue("int", TierCIsland.Reason($"table-sort key '{k.CobolName}' over a mixed-usage group"));
        return k.Pic switch
        {
            // ⛔ A WINDOWED numeric key decodes through THE ONE windowed reader (kb/Work PB186), never a lane
            // spelled here: this arm used to call the SIGNED image decode for every form, so an unsigned 16-byte
            // binary key at or above 2^127 compared as a negative value, and it excluded the FLOAT form outright,
            // leaving a windowed COMP-1/COMP-2 key to the native arm's `CompareTo` on its IEEE-byte STRING. Both
            // operands are the same item, so the scales agree and the decoded values compare directly — GR19's
            // "rules for comparison of operands in a relation condition" are §8.8.4.2.4's algebraic value for a
            // numeric key. SendingRef.Normal — a key comparison REFERENCES the content of both operands, so
            // §14.6.13.2 rules 2 and 3 apply to a windowed key exactly as to an arithmetic operand (kb/Work PB230).
            { Category: PicCategory.Numeric } pic when k.StoreAsImage =>
                $"{NumericRenderer.WindowedNum(pa, k, pic, SendingRef.Normal).Expr}.CompareTo({NumericRenderer.WindowedNum(pb, k, pic, SendingRef.Normal).Expr})",
            { Category: PicCategory.Numeric, IsFloat: false } => $"({pa}).CompareTo({pb})",
            { Category: PicCategory.Numeric } => $"({pa}).CompareTo({pb})",   // COMP-1/2 — IEEE value order
            _ => RuntimeApi.StrCompare(pa, pb, weightsArg),
        };
    }

    /// <summary>The runtime key-descriptor array literal for a statement's compile-time key descriptors
    /// (offset/length BYTE windows into the SD record image + the class that selects the comparator and the
    /// numeric profile that decodes a numeric window, ISO §14.9.40.3 SR6e / GR5 / GR8).</summary>
    private static string KeysExpr(IReadOnlyList<BoundSortMergeKey> keys) =>
        RuntimeApi.SortKeyArray(keys.Select(k =>
            $"new({k.Offset}, {k.Length}, {(k.Descending ? "true" : "false")}, {RuntimeApi.SortKeyClass(k.Class)}, "
            + $"{(k.Item is { } ki ? ki.ProfileName : "default")}"
            + (k.LayoutRecord is { } lr ? $", {RuntimeApi.ContiguousLayoutOf(PlaceRenderer.Read(lr))}" : "")
            + ")"));

    /// <summary>The ALPHANUMERIC collation argument for a statement's GR5-resolved sequence: <c>null</c> for the
    /// native order, the compiled <c>__COLLATE</c> carrier when the resolved sequence IS the program collating
    /// sequence, else the statement alphabet's own inline carrier (a non-PCS statement alphabet has no emitted
    /// field — ST108A/ST137A shape).</summary>
    private string WeightsExpr(SortCollation c) =>
        c.Alphanumeric is not { } def ? "null"
        : ReferenceEquals(def, ctx.Data.Collating) ? "__COLLATE"
        : CollationEmit.New(def);

    /// <summary>Its NATIONAL twin (ISO §14.9.40.4 GR5 — the sequence for keys of class national, determined
    /// SEPARATELY): <c>__COLLATE_NAT</c> when the resolved sequence IS the program's national collating sequence,
    /// else the statement alphabet-name-2's own inline carrier, else <c>null</c> for the native national order —
    /// which on the D-N1/D-N3 substrate IS code-point order.</summary>
    private string NatWeightsExpr(SortCollation c) =>
        c.National is not { } def ? "null"
        : ReferenceEquals(def, ctx.Data.NationalCollating) ? "__COLLATE_NAT"
        : CollationEmit.New(def);

    /// <summary>The trailing collation argument a TABLE-sort key comparison takes, chosen by the key's CLASS
    /// (ISO §14.9.40.4 GR5 through the ONE classifier): the alphanumeric carrier for an alphabetic/alphanumeric or
    /// ordinary group key, the national carrier for a key of class national, and NOTHING for the classes GR5 names
    /// no sequence for — a numeric key never reaches here and a BOOLEAN key compares by value (§8.8.4.2.8,
    /// "regardless of their usage"), which is why an alphabet reordering '0' and '1' must not touch it.
    /// A non-PCS statement alphabet is materialized once per statement into a local carrier.</summary>
    private string TableWeightsArg(SortCollation c, CollatingClass cls, int id, HashSet<string> declared) => cls switch
    {
        CollatingClass.Alphanumeric => CarrierArg(c.Alphanumeric, ctx.Data.Collating, "__COLLATE", $"__sw{id}",
            "statement COLLATING SEQUENCE (GR5a)", CollationEmit.New, declared),
        CollatingClass.National => CarrierArg(c.National, ctx.Data.NationalCollating, "__COLLATE_NAT", $"__swn{id}",
            "statement COLLATING SEQUENCE FOR NATIONAL (GR5a)", CollationEmit.New, declared),
        _ => "",
    };

    /// <summary>One trailing carrier argument, emitting the local at most once per statement (two keys of one
    /// class must not declare <c>__sw{id}</c> twice — CS0128).</summary>
    private string CarrierArg<T>(T? def, T? programSequence, string programField, string local, string why,
        Func<T, string> render, HashSet<string> declared) where T : class
    {
        if (def is null) return "";
        if (ReferenceEquals(def, programSequence)) return $", {programField}";
        if (declared.Add(local))
            ctx.Writer.Line($"CobolCollation {local} = {render(def)};   // {why}");
        return $", {local}";
    }
}
