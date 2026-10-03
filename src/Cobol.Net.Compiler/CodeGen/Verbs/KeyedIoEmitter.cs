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
/// RELATIVE + INDEXED file-I/O emission (ISO/IEC 1989:2023 §14.9.10/.30/.35/.41/.51; COBOLNET_DESIGN §8.3; P7
/// Step 9e — a real collaborator over the per-unit <see cref="EmitContext"/>): every keyed verb renders the SSOT
/// <b>status-first</b> shape — perform the operation, capture the two-character I-O status, store the FILE STATUS
/// item, then branch: AT END on first character <c>'1'</c> (statuses 10/14, §9.1.13.4), INVALID KEY on <c>'2'</c>
/// ONLY (§9.1.13.5/§9.1.14 — a 3x/4x failure routes to exception processing, never the INVALID KEY imperative;
/// the legacy's any-failure IsInvalidKey is a known deviation, pinned to spec), and the NOT-phrases on success
/// (<c>'0'</c>, §9.1.14 final rule). OPEN/CLOSE flow through the existing <c>BoundOpen</c>/<c>BoundClose</c>
/// emission — the runtime file facade dispatches to the keyed connectors.
/// </summary>
internal sealed class KeyedIoEmitter(EmitContext ctx, NumericRenderer num, ReferenceResolver refs,
    ArithmeticEmitter arith, MoveEmitter move)
{
    /// <summary>The file-I/O common services (status store, USE hooks, image splice, RETRY renders) and the
    /// statement dispatcher — property-wired by <see cref="UnitEmitters"/>: SequentialIo's ctor already takes
    /// THIS emitter for the shared READ/REWRITE bodies, so the reverse edge cannot be a ctor argument.</summary>
    internal SequentialIoEmitter SeqIo { get; set; } = null!;

    /// <summary>The statement dispatcher — property-wired by <see cref="UnitEmitters"/> (the AT END /
    /// INVALID KEY / ON EXCEPTION phrase bodies nest arbitrary statement lists).</summary>
    internal StatementEmitter Statements { get; set; } = null!;

    // ── Registration (Main start) ──────────────────────────────────────────────────────────────────────────────

    /// <summary>Register a relative/indexed connector (called from the core file registration): the host path,
    /// record width, access mode, and the key geometry — the RELATIVE KEY digit capacity (drives statuses 14/24,
    /// §9.1.13.4/§14.9.51 GR29a), or the prime + alternate key (offset, length) ranges of the record's character
    /// image (§12.4.5.12 / §12.4.5.6; offsets computed against the generated codec's deterministic layout).</summary>
    public void EmitRegistration(CodeWriter w, FileModel file)
    {
        // ⛔ NO RECORD-LESS EARLY RETURN HERE (kb/Work PB345). It used to read `if (file.Records.Count == 0)
        // return; // a SELECT with no FD`, a premise the standard refutes twice: an FD with no record description
        // entries is NOT "a SELECT with no FD", and §13.4.5.3 SR3 makes it legal on this very format — SR7 takes
        // the permission back only for INDEXED. So a legal RELATIVE file written without a level-01 got no
        // connector at all and every verb on it aborted the run unit. The record-less decision now belongs to the
        // ONE caller (SequentialIoEmitter.EmitFileRegistration), above the organization split, and every FD that
        // reaches emission carries a record area — §14.9.30.4 GR6's implied description when none was written.
        string name = FileKeyExpr(file), assign = CsLiteral(file.AssignTarget);
        int access = (int)file.AccessMode;   // FileAccessMode ordinals mirror the runtime KeyedAccess enum
        string opt = file.Optional ? "true" : "false";
        // A variable-length file registers its record-size bounds (ISO §13.18.43 GR9/GR10) for the GR14/§14.9.35
        // GR20 '44' boundary checks; the keyed frames already carry per-record lengths.
        string vary = file.RecordSizeVaries ? $", {file.VaryMin}, {file.VaryMax}" : "";
        if (file.Organization == FileOrganization.Relative)
        {
            int digits = file.RelativeKeyItem?.Pic?.Digits ?? 0;
            w.Line($"{RuntimeApi.FileRegisterRelative(name, assign, file.RecordWidth, opt, access, digits, vary, ctx.Data.Edition.DialectLevel, CsLiteral(file.SelectName))};");
            SequentialIoEmitter.EmitAreaRegistrations(w, file);   // §14.9.30.4 GR15 + §13.18.13.4 GR2
            return;
        }
        if (file.RecordKeyItem is not { } pk || KeyWindow(pk, file.FixedFormRecords) is not var (pkOff, pkLayout))
        {
            w.Line(LoudStmt($"indexed file '{file.CobolName}': RECORD KEY missing or not locatable in the record "
                + "image (ISO §12.4.5.12)"));
            return;
        }
        // ⛔ THE KEY WINDOW IS IN BYTES (kb/Work PB327): §12.4.5.12.4 GR4 states key correspondence over "the
        // identical BYTE POSITIONS", and RecordLayout.OffsetOf above is already a byte offset — so the WIDTH must
        // be the key's byte extent too, which for a national key (PIC N(n)) is 2n, not n (§13.18.60.4 GR8; D-N1).
        w.Line($"{RuntimeApi.FileRegisterIndexed(name, assign, file.RecordWidth, opt, access, $"{pkOff}", pk.ByteWidth, vary, ctx.Data.Edition.DialectLevel, CollationLit(file.PrimeKeyCollation), CsLiteral(file.SelectName), pkLayout)};");
        for (int i = 0; i < file.AlternateKeys.Count; i++)
        {
            var (alt, dups, suppress) = file.AlternateKeys[i];
            if (KeyWindow(alt, file.FixedFormRecords) is not var (aOff, aLayout))
            {
                w.Line(LoudStmt($"indexed file '{file.CobolName}': ALTERNATE RECORD KEY '{alt.CobolName}' not "
                    + "locatable in the record image (ISO §12.4.5.6)"));
                continue;
            }
            var altCollation = i < file.AlternateKeyCollations.Count ? file.AlternateKeyCollations[i] : null;
            // §12.4.5.6.4 GR6 — the key suppression value, already sized to the key (DataBinder.KeySuppressionOf);
            // the connector compares key IMAGES, so a national key's value rides the ONE national serializer.
            string sup = suppress is not { } s ? "null"
                : s.National ? RuntimeApi.NatBytes(CsLiteral(s.Value)) : CsLiteral(s.Value);
            w.Line($"{RuntimeApi.FileAddAlternateKey(name, $"{aOff}", alt.ByteWidth, dups ? "true" : "false", CollationLit(altCollation), sup, aLayout)};");   // bytes — see the prime key above (kb/Work PB327)
        }
        SequentialIoEmitter.EmitAreaRegistrations(w, file);   // §14.9.30.4 GR15 + §13.18.13.4 GR2
    }

    /// <summary>A record key's registration window (§12.4.5.12 / §12.4.5.6): its byte offset and, when a
    /// variable-length member of its record precedes it, the record type's layout expression — the offset is then
    /// the key's FIXED-run offset and the connector locates the key per record (kb/Work PB1025; docs/CONFORMANCE.md
    /// §3 D-KWV). ONE reader, <see cref="Binding.Model.RecordLayout.KeyWindowOf"/>, the same one the SORT/MERGE
    /// key and the §12.4.5.12.3 SR4 / §12.4.5.6.3 SR5 screens read. Null when the key has no locatable window.</summary>
    private (int Offset, string Layout)? KeyWindow(Binding.Model.DataItem key, bool fixedForm)
    {
        var root = key;
        while (root.Parent is { } p) root = p;
        if (Binding.Model.RecordLayout.KeyWindowOf(root, key, key.ByteWidth) is { FollowsVariable: true } win
            && refs.ResolveItem(root) is { } place)
            // ⛔ In a file of FIXED-length records (D-FRA (vi)) every record has the one fixed form, so the key sits at
            // ONE position — the fixed-run offset plus the MAXIMUM extent of every member that precedes it
            // (KeyWindow.MaxEnd less the key's own width) — and registers as a plain window, exactly the window a
            // second program describing the same file with plain fields declares (§9.1.6, §14.9.27.4 GR10).
            return fixedForm ? ((int)(win.MaxEnd - key.ByteWidth), "null")
                : (win.Offset, RuntimeApi.ContiguousLayoutOf(PlaceRenderer.Read(place)));
        // Every other key keeps the area offset it has always registered (§12.4.5.12.4 GR4's byte positions).
        return Binding.Model.RecordLayout.OffsetOf(key) is { } off ? (off, "null") : null;
    }

    /// <summary>The §12.4.5.7 key collating sequence as a runtime <c>CobolCollation</c> expression — the program's
    /// <c>__COLLATE</c> when the key's alphabet IS the program collating sequence, else an inline carrier — or "null"
    /// for native order (a no-clause key emits "null" so its registration is unchanged from the pre-clause engine).</summary>
    private string CollationLit(AlphabetDef? def) =>
        def is null ? "null" : ReferenceEquals(def, ctx.Data.Collating) ? "__COLLATE" : CollationEmit.New(def);

    // ── READ (ISO §14.9.30) ────────────────────────────────────────────────────────────────────────────────────

    public void EmitRead(BoundKeyedRead rd)
    {
        var w = ctx.Writer;
        FileModel file = rd.File;
        string name = FileKeyExpr(file);
        int id = ctx.Names.NextKeyedSeq();
        string st = $"__kst{id}", img = $"__kim{id}";
        // The RECORD AREA is the LARGEST record description (ReferenceResolver.RecordArea, §13.18.33.4
        // GR3 — multi-01 FDs share one area; a READ makes the record available in the WHOLE area, so a shorter Records[0] must not
        // truncate the splice — RL106A's 56/102-char pair left a stale tail).
        Place? area = refs.RecordArea(file);

        if (rd.Kind == ReadKind.Random && file.Organization == FileOrganization.Relative)
        {
            // §14.9.30 GR29 — the random read positions to the RRN held by the RELATIVE KEY item. The slot number
            // travels through the TYPED field (PIC-aware by construction — the field IS a scaled long), never a
            // byte decode (the legacy proved byte-decoding COMP relative keys breaks; brief §2.1).
            if (Rrn(file) is not { } rrn)
            {
                w.Line(LoudStmt($"relative READ on '{file.CobolName}' without a resolvable RELATIVE KEY item"));
                return;
            }
            w.Line($"{RuntimeApi.FileSetRelativeKey(name, rrn)};");
        }
        // §9.1.16 record-lock governance (Phase 4d): EVERY read is governed — 51 when another connector holds
        // the record's lock (unless IGNORING LOCK), else the WITH LOCK / AUTOMATIC lock is acquired. It lands
        // BEFORE the success block so a 51 denial leaves the record area untouched (not made available).
        // ⛔ UNCONDITIONALLY (kb/Work PB683). Governance is a RUN-TIME fact — §9.1.15 lets an OPEN statement's
        // own SHARING phrase override the file control entry — so no property of the SELECT or of this
        // statement can decide it. And the runtime GOVERNS EVERY CONNECTOR, not only the ones that opted in:
        // a connector absent from the posture map is given §12.4.5.9.4 GR1 b) 2.'s implementor default and the
        // same governed body runs — it used to fall through to the ungoverned one, which is how a clause-less
        // connector read, rewrote and DELETED records another connector held locked (kb/Work PB669).
        // ⛔ THE GOVERNED SHAPE IS PER FORMAT, NOT PER ORGANIZATION (kb/Work PB340). A Format-1 NEXT/PREVIOUS
        // walk routes through the ONE governed Format-1 read the sequential emitter also renders, because
        // §14.9.30.4 GR22's ADVANCING ON LOCK skip-scan RE-EXECUTES the read and so has to own the read itself;
        // the loop used to exist only on the sequential arm and `rd.AdvancingOnLock` was simply dropped here.
        // A Format-2 random read has no next record to advance to and no ADVANCING phrase in its general
        // format (§14.9.30.2), so it keeps the post-read adjustment.
        switch (rd.Kind)
        {
            case ReadKind.Next:
            case ReadKind.Previous:
                string prev = rd.Kind == ReadKind.Previous ? "true" : "false";
                var (rk, ra) = SeqIo.RenderRetry(rd.Retry);
                w.Line($"var {st} = {RuntimeApi.FileReadShared(name, prev, SequentialIoEmitter.RuntimeRecordLock(rd.Lock), rd.AdvancingOnLock ? "true" : "false", rd.IgnoringLock ? "true" : "false", rk, ra, img)};");
                break;
            default:
                // Indexed Format 2: the key VALUE is the key field's current content in the record area
                // (§14.9.30 GR32) — pass the area image; the connector slices the key-of-reference range.
                // ⛔ ONE CALL, NOT A READ FOLLOWED BY A GOVERNANCE PATCH (kb/Work PB338): §14.9.30.4 GR10 a)/d)
                // require the file position indicator and the key of reference to be UNCHANGED when the record
                // operation conflict condition arises, so the governed entry has to own the retrieval and decide
                // BEFORE it, for EVERY connector — §9.1.16's inaccessibility is not conditioned on the
                // reading connector's own LOCK MODE clause (kb/Work PB669).
                string keyImage = area is not null ? OperandText.RecordSendImage(area) : "\"\"";
                string? keyExtents = area is not null ? OperandText.RecordSendExtents(area) : null;
                var (retryKind, retryAmount) = SeqIo.RenderRetry(rd.Retry);
                w.Line($"var {st} = {RuntimeApi.FileReadKeyedShared(name, rd.KeyIndex, keyImage, SequentialIoEmitter.RuntimeRecordLock(rd.Lock), rd.IgnoringLock ? "true" : "false", retryKind, retryAmount, img, keyExtents)};");
                break;
        }
        using (w.Block($"if ({IoStatusClass.Successful(st)})"))
        {
            SeqIo.EmitRecordAreaStore(file, area, img, RuntimeApi.FileCurrentRecord(name), RuntimeApi.FileCurrentRecordExtents(name));
            // §9.1.13.6 item 4 b) (kb/Work PB1513): the record has landed; an OCCURS DEPENDING ON item that makes it
            // exceed the file's maximum makes the READ unsuccessful — '34' — so the success-only steps below follow
            // the verdict. The status local is the statement's own, and every later phrase test reads it. The record
            // area therefore HOLDS the landed record after that '34' — an unsuccessful READ's area content is undefined
            // (§14.9.30.4 GR18), so it is within the standard; the area-untouched posture is for the denials that
            // come BEFORE the landing (a '51', above), and SequentialIoEmitter.EmitRead states the exception.
            string? tooLong = SeqIo.OdoRecordExceedsMaximum(file);
            if (tooLong is not null)
                using (w.Block($"if ({tooLong})"))
                    w.Line($"{st} = {RuntimeApi.FileReadExceedsRecordMaximum(name)};   // §9.1.13.6 item 4 b) — the READ is unsuccessful");
            void SuccessSteps()
            {
                SeqIo.EmitReadLengthStore(file);   // §13.18.43 GR15 — the just-read length into DEPENDING
                // §14.9.30 GR25 — a sequential READ of a relative file MOVEs the RRN of the record made available
                // into the RELATIVE KEY data item (MOVE rules — the canonical numeric store path).
                if (rd.Kind != ReadKind.Random && file.Organization == FileOrganization.Relative
                    && file.RelativeKeyItem is { } rk && refs.ResolveItem(rk) is { } rkPlace)
                    arith.StoreArith(rkPlace, new NumX(RuntimeApi.FileRelativeSlot(name), 0), CobolRounding.Truncation);
            }
            if (tooLong is null) SuccessSteps();
            else using (w.Block($"if ({IoStatusClass.Successful(st)})")) SuccessSteps();
        }
        SeqIo.EmitStoreFileStatus(file);
        var hook = SeqIo.EmitUseHook(file, atEndHandled: rd.AtEnd is not null, invalidKeyHandled: rd.InvalidKey?.Invalid is not null);

        // The §9.1.14 / §14.9.30 GR24 transfer-of-control branches, uniform across the read kinds (a phrase
        // whose status family cannot arise for this kind — e.g. INVALID KEY on a sequential read — is simply
        // dead, the leniency-tolerant rendering of a CCVS-misplaced phrase): '2x' → INVALID KEY imperative;
        // '0x' (success) → INTO move + NOT AT END / NOT INVALID KEY; '1x' (10/14, the at-end family,
        // §9.1.13.4) → AT END imperative; any other unsuccessful status takes NO branch — §14.9.30.4 GR13
        // ignores both phrases when neither condition occurs and GR13 b) routes it through §9.1.12's
        // exception processing. That includes GR21's '46', a LOGIC ERROR (§9.1.13.7 6)), not the at end
        // condition GR24 is conditioned on; the determination is on IoStatusClass (kb/Work PB810).
        bool into = rd.IntoMove is not null && area is not null;
        bool hasInv = rd.InvalidKey?.Invalid is not null;
        if (hasInv)
            using (w.Block($"if ({IoStatusClass.InvalidKey(st)})"))
                Statements.EmitStatementList(rd.InvalidKey!.Invalid!);
        if (into || rd.NotAtEnd is not null || rd.InvalidKey?.NotInvalid is not null)
            using (w.Block($"{(hasInv ? "else " : "")}if ({IoStatusClass.Successful(st)})"))
            {
                // GR4 b) — READ INTO is READ then MOVE THE CURRENT RECORD (the §13.18.43.4 GR16 slice of the
                // record area, never the padded area): the BOUND move, built by the SAME binder call the
                // sequential arm makes (kb/Work PB348 over PB339).
                if (into) move.Emit(rd.IntoMove!);
                if (rd.NotAtEnd is { } nae) SeqIo.EmitNotPhrase(hook.NotPhraseGate, nae);                  // §14.9.30 — NOT AT END on success
                if (rd.InvalidKey?.NotInvalid is { } nik) SeqIo.EmitNotPhrase(hook.NotPhraseGate, nik);    // §9.1.14 — success only
            }
        if (rd.AtEnd is { } at)
            using (w.Block($"if ({IoStatusClass.AtEnd(st)})"))
                Statements.EmitStatementList(at);                                                // §14.9.30 GR24c
    }

    // ── WRITE (ISO §14.9.51) ───────────────────────────────────────────────────────────────────────────────────

    public void EmitWrite(BoundKeyedWrite wr)
    {
        var w = ctx.Writer;
        FileModel file = wr.File;
        string name = FileKeyExpr(file);
        if (wr.FromMove is { } fromMove) move.Emit(fromMove);   // §14.9.51.4 GR5 a) — the BOUND implicit MOVE
        if (file.Organization == FileOrganization.Relative && file.AccessMode != FileAccessMode.Sequential)
        {
            // §14.9.51 GR29b/GR32 — random/dynamic: the runtime element pre-set the RELATIVE KEY item with the
            // RRN to write; read it through the typed field.
            if (Rrn(file) is not { } rrn)
            {
                w.Line(LoudStmt($"relative WRITE on '{file.CobolName}' without a resolvable RELATIVE KEY item"));
                return;
            }
            w.Line($"{RuntimeApi.FileSetRelativeKey(name, rrn)};");
        }
        int id = ctx.Names.NextKeyedSeq();
        string st = $"__kst{id}";
        string wimg = SeqIo.SentRecordImage(file, wr.Record);   // THE ONE record-area channel (kb/Work PB327; D-WRT1, PB1907)
        // §9.1.16/§14.9.51 GR10-GR11 (P10 Step 8): EVERY keyed WRITE routes through the governed entry — single
        // locking releases the connector's prior lock, WITH LOCK locks the record written. Unconditional
        // (kb/Work PB683): only the runtime can see an OPEN's own SHARING phrase (§9.1.15), and it governs
        // every connector (kb/Work PB669). §13.18.43 GR13a rides in `lenArg`.
        var (retryKind, retryAmount) = SeqIo.RenderRetry(wr.Retry);
        string lenArg = SeqIo.VaryingLengthArg(file) ?? "-1";
        // The LINAGE page rides the governed entry through the SAME helper the sequential surface uses; for a
        // keyed FD it renders `null` because ISO §13.4.5.2 Format 2 (relative-or-indexed) carries no
        // linage-clause — derived from the file model, never a hand-written null here (kb/Work PB673).
        // §14.9.51.4 GR4 — the released record is also available as a record of the other files of a SAME RECORD AREA
        // clause (kb/Work PB1195): held in a local so the statement and the store after it see the SAME record.
        string? released = SeqIo.BeginReleasedRecord(file, wr.Record, ref wimg);
        w.Line($"var {st} = {RuntimeApi.FileWriteShared(name, wimg, lenArg, SequentialIoEmitter.RuntimeRecordLock(wr.Lock), retryKind, retryAmount, SeqIo.LinageArg(file), areaExtents: OperandText.RecordSendExtents(wr.Record))};");
        SeqIo.EndReleasedRecord(file, wr.Record, released, lenArg, RuntimeApi.FileStatus(name));
        // §14.9.51 GR29a/GR30 — sequential access (incl. EXTEND): the released RRN is MOVEd into the RELATIVE KEY
        // item during execution of the WRITE.
        if (file.Organization == FileOrganization.Relative && file.AccessMode == FileAccessMode.Sequential
            && file.RelativeKeyItem is { } rk && refs.ResolveItem(rk) is { } rkPlace)
            using (w.Block($"if ({IoStatusClass.Successful(st)})"))
                arith.StoreArith(rkPlace, new NumX(RuntimeApi.FileRelativeSlot(name), 0), CobolRounding.Truncation);
        SeqIo.EmitStoreFileStatus(file);
        var hook = SeqIo.EmitUseHook(file, invalidKeyHandled: wr.InvalidKey?.Invalid is not null);
        SeqIo.EmitInvalid(st, wr.InvalidKey, hook.NotPhraseGate);
    }

    // ── REWRITE (ISO §14.9.35) ─────────────────────────────────────────────────────────────────────────────────

    public void EmitRewrite(BoundKeyedRewrite rw)
    {
        var w = ctx.Writer;
        FileModel file = rw.File;
        string name = FileKeyExpr(file);
        if (rw.FromMove is { } fromMove) move.Emit(fromMove);   // §14.9.35.4 GR7 a) — the BOUND implicit MOVE
        if (file.Organization == FileOrganization.Relative && file.AccessMode != FileAccessMode.Sequential)
        {
            // §14.9.35 GR21 — random/dynamic: the slot to replace is named by the RELATIVE KEY item.
            if (Rrn(file) is not { } rrn)
            {
                w.Line(LoudStmt($"relative REWRITE on '{file.CobolName}' without a resolvable RELATIVE KEY item"));
                return;
            }
            w.Line($"{RuntimeApi.FileSetRelativeKey(name, rrn)};");
        }
        int id = ctx.Names.NextKeyedSeq();
        string st = $"__kst{id}";
        string rimg = SeqIo.SentRecordImage(file, rw.Record);   // THE ONE record-area channel (kb/Work PB327; D-WRT1, PB1907)
        // §9.1.16/§14.9.35 GR11-GR12 (P10 Step 8): EVERY keyed REWRITE routes through the governed entry —
        // another connector's lock on the target blocks it (RETRY re-checks, else 51; the record is unrewritten).
        // Unconditional (kb/Work PB683): the OPEN's own SHARING phrase (§9.1.15) is invisible here, and the
        // runtime body (§13.18.43 GR13a / §14.9.35 GR20) governs every connector, opted in or not (kb/Work PB669).
        var (retryKind, retryAmount) = SeqIo.RenderRetry(rw.Retry);
        string lenArg = SeqIo.VaryingLengthArg(file) ?? "-1";
        // §14.9.35.4 GR6 — the released record is also available as a record of the other files of a SAME RECORD AREA
        // clause (kb/Work PB1195): held in a local so the statement and the store after it see the SAME record.
        string? released = SeqIo.BeginReleasedRecord(file, rw.Record, ref rimg);
        w.Line($"var {st} = {RuntimeApi.FileRewriteShared(name, rimg, lenArg, SequentialIoEmitter.RuntimeRecordLock(rw.Lock), retryKind, retryAmount, OperandText.RecordSendExtents(rw.Record))};");
        SeqIo.EndReleasedRecord(file, rw.Record, released, lenArg, RuntimeApi.FileStatus(name));
        SeqIo.EmitStoreFileStatus(file);
        var hook = SeqIo.EmitUseHook(file, invalidKeyHandled: rw.InvalidKey?.Invalid is not null);
        SeqIo.EmitInvalid(st, rw.InvalidKey, hook.NotPhraseGate);
    }

    // ── DELETE RECORD / DELETE FILE (ISO §14.9.10) ─────────────────────────────────────────────────────────────

    public void EmitDelete(BoundKeyedDelete del)
    {
        var w = ctx.Writer;
        FileModel file = del.File;
        string name = FileKeyExpr(file);
        if (file.Organization == FileOrganization.Relative && file.AccessMode != FileAccessMode.Sequential)
        {
            // §14.9.10 GR4 — relative random/dynamic deletes the record named by the RELATIVE KEY item.
            if (Rrn(file) is not { } rrn)
            {
                w.Line(LoudStmt($"relative DELETE on '{file.CobolName}' without a resolvable RELATIVE KEY item"));
                return;
            }
            w.Line($"{RuntimeApi.FileSetRelativeKey(name, rrn)};");
        }
        // §14.9.10 GR3 — indexed random/dynamic deletes by the PRIME RECORD KEY's current content; DELETE carries
        // no record operand, so the key value is sliced from the record area image (GR8 — the area is unchanged).
        // The RECORD AREA is the LARGEST record description (ReferenceResolver.RecordArea, §13.18.33.4
        // GR3 — multi-01 FDs share one area; a READ makes the record available in the WHOLE area, so a shorter Records[0] must not
        // truncate the splice — RL106A's 56/102-char pair left a stale tail).
        Place? area = refs.RecordArea(file);
        string image = area is not null ? OperandText.RecordSendImage(area) : "\"\"";
        string? areaExtents = area is not null ? OperandText.RecordSendExtents(area) : null;
        int id = ctx.Names.NextKeyedSeq();
        string st = $"__kst{id}";
        // §9.1.16/§14.9.10 GR6-GR7 (P10 Step 8): EVERY DELETE routes through the governed entry — a record
        // locked by another connector shall not be deleted (RETRY re-checks, else 51). Unconditional (kb/Work
        // PB683): the runtime governs the removal for EVERY connector, opted in or not (kb/Work PB669) — which
        // is what stops a clause-less connector DELETING a record another one holds locked.
        var (retryKind, retryAmount) = SeqIo.RenderRetry(del.Retry);
        w.Line($"var {st} = {RuntimeApi.FileDeleteShared(name, image, retryKind, retryAmount, areaExtents)};");
        SeqIo.EmitStoreFileStatus(file);
        var hook = SeqIo.EmitUseHook(file, invalidKeyHandled: del.InvalidKey?.Invalid is not null);
        SeqIo.EmitInvalid(st, del.InvalidKey, hook.NotPhraseGate);
    }

    public void EmitDeleteFile(BoundKeyedDeleteFile df)
    {
        var w = ctx.Writer;
        // §14.9.10 Format 2 applies to EVERY organization (GR13/GR14/GR16): the sequential connector now
        // exposes its host path, so the runtime DeleteFile handles it uniformly with relative/indexed (DEVLOG
        // 612, Phase-4 track d). After a successful delete, the same name's next OPEN INPUT reports '35'
        // (file not available) — the golden's round-trip. GR12's multiple file-names are bound as one
        // BoundKeyedDeleteFile per name (kb/Work PB134), so this emitter always sees exactly one.
        int id = ctx.Names.NextKeyedSeq();
        string st = $"__kst{id}";
        // A RETRY phrase re-attempts the §14.9.10 GR15 file-sharing-conflict check ('62' — the physical file
        // open by another connector); the plain form keeps the existing single-attempt entry. The OVERRIDE
        // phrase (GR18) travels to the runtime as the flag that suppresses the fixed-file-attribute match —
        // it must be CARRIED even while the validated set is empty, or a future non-empty set silently checks
        // a program that wrote OVERRIDE precisely to opt out (kb/Work PB196).
        string ovr = df.Override ? "true" : "false";
        if (df.Retry is { } retry)
        {
            var (retryKind, retryAmount) = SeqIo.RenderRetry(retry);
            w.Line($"var {st} = {RuntimeApi.FileDeleteFileRetry(FileKeyExpr(df.File), retryKind, retryAmount, ovr)};");
        }
        else
            w.Line($"var {st} = {RuntimeApi.FileDeleteFile(FileKeyExpr(df.File), ovr)};");
        SeqIo.EmitStoreFileStatus(df.File);
        // §14.9.10.4 GR20b: the enabled level-3 EC-I-O for the status is set to exist EVEN when the ON
        // EXCEPTION phrase is written — the phrase suppresses only the declarative dispatch and the fatal
        // default (GR20c), the same third-flag shape the AT END / INVALID KEY suppressions use. The old
        // `if (OnException is null)` skip left EXCEPTION-STATUS stale inside imperative-statement-3
        // (kb/Work PB141).
        SeqIo.EmitUseHook(df.File, onExceptionHandled: df.OnException is not null);
        // §9.1.13.1/§14.9.10: ON EXCEPTION runs on an unsuccessful completion; '05' (absent file) is a SUCCESSFUL
        // completion (GR14) and takes the NOT ON EXCEPTION path.
        if (df.OnException is { } on)
        {
            using (w.Block($"if ({IoStatusClass.Unsuccessful(st)})")) Statements.EmitStatementList(on);
            if (df.NotOnException is { } not)
                using (w.Block("else")) Statements.EmitStatementList(not);
        }
        else if (df.NotOnException is { } not)
            using (w.Block($"if ({IoStatusClass.Successful(st)})")) Statements.EmitStatementList(not);
    }

    // ── START (ISO §14.9.41) ───────────────────────────────────────────────────────────────────────────────────

    public void EmitStart(BoundKeyedStart sta)
    {
        var w = ctx.Writer;
        FileModel file = sta.File;
        string name = FileKeyExpr(file);
        int id = ctx.Names.NextKeyedSeq();
        string st = $"__kst{id}";
        if (sta.Mode != KeyedStartMode.Key)
            w.Line($"var {st} = {RuntimeApi.FileStartFirstLast(name, sta.Mode == KeyedStartMode.Last ? "true" : "false")};");
        else if (file.Organization == FileOrganization.Relative)
        {
            // §14.9.41 GR9/GR10 — numeric comparison against the RELATIVE KEY item's value (typed read).
            string rrn = RuntimeApi.HostInt64(NumericRenderer.Align(num.FieldNum(sta.Operand!), 0));
            w.Line($"var {st} = {RuntimeApi.FileStartRelative(name, CsLiteral(sta.Op), rrn)};");
        }
        else
        {
            // ⛔ §14.9.41.4 GR17 a) SOURCES THE SEARCH KEY FROM THE RECORD AREA, NOT FROM data-name-1
            // (kb/Work PB355): "The specified key is set up by moving the relevant parts of the record area into
            // a temporary data area." The "specified key" is GR16's KEY OF REFERENCE — "The key specified in the
            // KEY phrase, or that shares a leftmost character with the data item specified in the KEY phrase,
            // becomes the key of reference" — so the relevant parts are THAT key's character positions in the
            // area, and data-name-1's role ends at naming the key and (GR17 b)) supplying a default length.
            // The area image therefore travels, and the connector slices the key of reference out of it with the
            // SAME `KeyOf` the random READ (§14.9.30.4 GR32) and DELETE (§14.9.10.4 GR3) use — one extraction,
            // three verbs, and a SOURCE-phrase key's several "parts" (§12.4.5.12.4 GR2) fall out of it.
            // Sending `OperandText.AsStorageImage(sta.Operand)` instead agreed with the rule only while the
            // temporary area was no longer than data-name-1: a LENGTH count that reaches PAST the operand into
            // the rest of the key invented SPACES for the characters the record area actually holds.
            // GR17 b) fixes only the LENGTH of that area — "the length specified in the LENGTH clause, if
            // specified, or else the length of record-key-name-1, if specified, or else the length of
            // data-name-1" — which is what `len` below renders, and GR14 bounds it by the key's own length.
            // The key comparison collates NATIVE at COBOL-85 (§12.4.5.7 file collating; the program collating
            // sequence does NOT silently apply to keys — brief risk note).
            // ⛔ §14.9.41.4 GR13 COUNTS THE OPERAND'S OWN CHARACTER POSITIONS, AND THE CONNECTOR SLICES BYTES
            // (kb/Work PB327): "If data-name-1 or record-key-name-1 is of class alphanumeric,
            // arithmetic-expression-1 is the number of alphanumeric character positions; if data-name-1 or
            // record-key-name-1 is of class national, arithmetic-expression-1 is the number of national character
            // positions." A national position is TWO bytes (§13.18.60.4 GR8; D-N1), so the count is scaled here,
            // once, at the one place the two units meet — and the key VALUE goes through THE ONE storage channel,
            // never its carrier, or the connector would compare one byte per position against a two-byte window.
            // GR17e's comparison per §8.8.4.2.9 is satisfied by the byte compare: UTF-16BE pair order IS code-unit
            // order over the BMP repertoire §8.5.1.4 admits (CONFORMANCE.md items 33/188).
            int natBytes = NationalWindow.PositionsOf(sta.Operand!.Item) is not null
                ? RuntimeApi.BytesPerNational : 1;
            // ⛔ §14.9.41.4 GR14 IS ASKED OF arithmetic-expression-1 ITSELF, SO THE VALUE TRAVELS UNNARROWED
            // (kb/Work PB357): "If arithmetic-expression-1 does not evaluate to a positive nonzero integer that is
            // less than or equal to the length of the associated key, the I-O status value … is set to '23'". The
            // former int cast of `Align(expr, 0)` truncated 2.5 to 2 and wrapped 4294967297 to 1 before the connector's
            // bound test ran, so both positioned with '00'. The lanes are LandAmount's (SetEmitter): the exact
            // scaled Int128 with its scale, or the native double — the fraction and the magnitude both survive.
            string len = sta.Length is { } le
                ? StartLength(num.Landed(NumericRenderer.DeU(num.Render(le, ReceiverContext.None)), ReceiverContext.None), natBytes)
                : RuntimeApi.StartKeyLengthWidth(sta.Operand!.Item.ByteWidth);
            string areaImage = refs.RecordArea(file) is { } ar
                ? OperandText.RecordSendImage(ar) : "\"\"";   // THE ONE record-area channel (kb/Work PB327)
            string? areaExtents = refs.RecordArea(file) is { } extentArea
                ? OperandText.RecordSendExtents(extentArea) : null;   // its extent table (D-FRA (v); kb/Work PB1053)
            w.Line($"var {st} = {RuntimeApi.FileStartIndexed(name, sta.KeyIndex, CsLiteral(sta.Op), areaImage, len, areaExtents)};");
        }
        SeqIo.EmitStoreFileStatus(file);
        var hook = SeqIo.EmitUseHook(file, invalidKeyHandled: sta.InvalidKey?.Invalid is not null);
        SeqIo.EmitInvalid(st, sta.InvalidKey, hook.NotPhraseGate);   // §14.9.41 GR6 — transfer per §9.1.14
    }

    /// <summary>The <c>StartKeyLength</c> expression for a WITH LENGTH count already funnelled to its exact lane
    /// (<c>Landed</c>/<c>DeU</c>): the native double, or the scaled <c>Int128</c> with its scale (kb/Work PB357).</summary>
    private static string StartLength(NumX x, int unitWidth) =>
        x.Real ? RuntimeApi.StartKeyLengthReal(x.Expr, unitWidth)
        : RuntimeApi.StartKeyLengthScaled(x.Expr, $"{x.Scale}", unitWidth);

    // ── Shared keyed emission helpers ──────────────────────────────────────────────────────────────────────────

    /// <summary>The C# <c>long</c> expression reading the file's RELATIVE KEY item (the RRN travels through the
    /// typed field — §12.4.5.13; the item is OUTSIDE the record, SR3), or null when the file has none.</summary>
    private string? Rrn(FileModel file)
    {
        if (file.RelativeKeyItem is not { } rk || refs.ResolveItem(rk) is not { } place) return null;
        return RuntimeApi.HostInt64(NumericRenderer.Align(num.FieldNum(place), 0));
    }
}
