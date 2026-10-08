// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;
using CobolNet.Frontend.Parsing;
using CobolNet.Editions;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>The sequential file-I/O verb binder (P7 Step 10h — a real collaborator over
/// <see cref="BinderContext"/>, carved from the core partial; ISO §14.9.27/.7/.51/.30/.35; COBOLNET_DESIGN
/// §8): OPEN/CLOSE/WRITE/READ/REWRITE with the keyed reroutes (the emitter-mirror SeqIo↔KeyedIo cycle), ADVANCING
/// (the SPECIAL-NAMES mnemonic zero-advance via <see cref="BinderContext.Mnemonics"/>), and the SD loud-guard
/// seam. The <c>… FROM</c> / <c>… INTO</c> phrases are IMPLICIT MOVES and are bound as such by
/// <c>MoveBinder.BindFromPhrase</c> / <c>BindIntoPhrase</c> (kb/Work PB348) — the former <c>WriteSource</c>
/// operand hand-off, which applied none of the three verbs' phrase rules, is gone. The WRITE SR13/SR18/SR19 + OPEN SR8 checks live in <c>StatementValidation</c> (pure checks);
/// <c>BindRetry</c> stays a shared spine member on the host.</summary>
internal sealed class SequentialIoBinder(BinderContext ctx, StatementBinder host, KeyedIoBinder keyedIo,
    FileLockBinder fileLock)
{
    public BoundStatement BindOpen(Core.OpenStatementContext o)
    {
        var opens = new List<BoundOpenFile>();
        foreach (var clause in o.openClause())
        {
            BoundOpenMode mode = MapOpenMode(clause.openMode());
            // ⛔ THE PHRASES ARE SCOPED TO THIS GROUP, WHICH IS WHY THEY ARE DECLARED INSIDE THIS LOOP.
            // §14.9.27.2's general format nests [sharing-phrase] and [retry-phrase] INSIDE the outer repeated
            // group, beside the open mode and the file-names they govern; the outer group carries the trailing
            // ellipsis. §14.9.27.4 GR20 then lists all four together — "These separate OPEN statements would
            // each have the same open mode specification, the sharing-phrase, retry-phrase, and REWIND phrase
            // as specified in the OPEN statement" — and the open mode is unarguably per-group, so the phrases
            // beside it are too. Hoisting either to statement scope let one group's phrase govern every file
            // in the statement, which both destroyed a sibling file's file-control sharing at run time and
            // rejected legal source at SR8 (kb/Work PB316).
            // The 2002 EDITION gate is elsewhere: the post-bind VersionConformancePass (Step 14c) recognizes
            // the SHARING phrase on the parse tree; the binder just records it.
            SharingMode? sharing = clause.sharingPhrase() is { } sp && sp.sharingMode() is { } sm
                ? DataBinder.MapSharing(sm)   // ONE mapper for both writers of the `sharingMode` production
                : null;
            RetrySpec? retry = clause.retryPhrase() is { } rp ? host.BindRetry(rp) : null;
            foreach (var spec in clause.openFileSpec())
            {
                // The ONE file-name resolution step (kb/Work PB236 — §8.4.2.1 through COBOLNET1639).
                if (!ctx.Validation.ResolveFile(spec.dataReference(), "OPEN", out var file, admitsReportFile: true)) return BoundRejected.Reported(ctx.Edition);
                // §14.9.27.3 SR8: OPEN … SHARING WITH ALL OTHER (clause or phrase) requires a LOCK MODE clause,
                // unless file-name-1 is subject to an APPLY COMMIT clause. The effective mode is THIS group's
                // phrase over the file-control clause — §14.9.27.4 GR23: "If there is no SHARING phrase on the
                // OPEN statement, then file sharing is completely specified in the file control entry."
                // ⛔ THIS IS SR8's ONLY SITE (kb/Work PB319): the rule speaks about the OPEN statement's
                // operand, so a file control entry cannot host it. The full antecedent lives in the callee.
                ctx.Data.ScreenApplyCommitSubject(file, clause.sharingPhrase() is not null, "OPEN with a SHARING phrase",
                    "ISO §14.9.27.3 SR7: \"The sharing phrase shall not be specified for a file subject to an APPLY "
                    + "COMMIT clause\"");   // SR7 — the phrase, never the file control entry's clause (kb/Work PB666)
                ctx.Validation.CheckOpenSharingAllOther(file, sharing ?? file.Sharing);   // SR8 — pure check
                ctx.Validation.CheckOpenModeForFile(file, mode);                          // SR1 + SR2 — pure check (kb/Work PB318)
                // ⛔ THE ONE READ of the per-file-name TAPE PHRASE — `openFileSpec`'s
                // `(REVERSED | WITH? NO REWIND)?` alternation, which §14.9.27.2 prints as one bracket beside
                // each file-name. BOTH alternatives land in ONE enum slot on the bound entry, because the
                // grammar makes them mutually exclusive and a bool per phrase is a second place for the next
                // one to be dropped — which is exactly what happened: kb/Work PB317 gave NO REWIND a field and
                // left REVERSED reading nothing, so `OPEN INPUT F REVERSED` compiled at --std 85 and delivered
                // the records FORWARD with no diagnostic (kb/Work PB668). Reading `spec.REWIND()` — not
                // `spec.NO()` — keeps the NO REWIND arm keyed on the phrase's own required word rather than on
                // a word the grammar shares with other phrases.
                // The 2002 EDITION gate for REVERSED stays where every removal gate lives, the post-bind
                // VersionConformancePass (`open-reversed-removed-2002`, COBOLNET0902); NO REWIND survives into
                // 2023 ungated. This binder is edition-agnostic and binds the phrase the source wrote.
                BoundOpenTapePhrase tape =
                    spec.REWIND() is not null ? BoundOpenTapePhrase.NoRewind
                    : spec.REVERSED() is not null ? BoundOpenTapePhrase.Reversed
                    : BoundOpenTapePhrase.None;
                // The phrase's own syntax rules — ONE table-driven screen for both alternatives (§14.9.27.3
                // SR5 + SR6 for NO REWIND; the COBOL-85 general format's INPUT/record-sequential restrictions
                // for REVERSED). A screen, not a branch: a violation is REPORTED and the statement still binds
                // with the phrase DROPPED, so the run-time phrase effects only ever see the medium/mode
                // combinations their general rules define (kb/Work PB317; the rows are PB318's
                // SR-14.9.27.3-5/-6). Screening here is what makes §14.9.27.4 GR11's "does not apply to the
                // storage medium" answerable — the CLOSE twin of SR5 (§14.9.6.3 SR1) plays exactly this role
                // for Table 14's N/A cells — and what keeps REVERSED off the LINE SEQUENTIAL medium that has
                // no backward walk (§14.9.30.3 SR7).
                if (!ctx.Validation.CheckOpenTapePhrase(file, mode, tape)) tape = BoundOpenTapePhrase.None;
                opens.Add(new BoundOpenFile(file, mode, sharing, retry, tape, UnsupportedOrg(file, "OPEN")));
            }
        }
        // ISO §14.9.27.4 GR20 — one implicit OPEN statement per file-name, in source order, each carrying its own
        // group's phrases (already true of BoundOpenFile), and "processing resumes at the next implicit OPEN
        // statement, if any". The per-file __IoCheck hook is that landing for an I-O declarative (a -2 action
        // falls into the next file's open); the series is what gives the same boundary to a condition that
        // UNWINDS to the statement's EC guard, which otherwise abandons every remaining file (kb/Work PB419).
        return BoundImplicitSeries.Of([.. opens.Select(f => (BoundStatement)new BoundOpen([f]))]);
    }

    public BoundStatement BindClose(Core.CloseStatementContext c)
    {
        var closes = new List<(FileModel, BoundCloseKind)>();
        foreach (var phrase in c.closeFilePhrase())
        {
            string name = phrase.fileName().GetText();
            // The ONE file-name resolution step (kb/Work PB236 — §8.4.2.1 through COBOLNET1639).
            if (!ctx.Validation.ResolveFile(phrase.fileName(), "CLOSE", out var file, admitsReportFile: true)) return BoundRejected.Reported(ctx.Edition);
            // §13.4.6.3 SR3: an SD file-name in a CLOSE — the statement previously compiled and ran against an
            // unregistered connector whose fail-open status read '00' (kb/Work PB140).
            if (ctx.Validation.ScreenSortMergeFile(file, "CLOSE") is not null)
                return BoundRejected.Reported(ctx.Edition);   // the screen REPORTED; a loud runtime stage on top would re-answer it (PB236)
            // §14.9.6.3 SR1: "The NO REWIND, REEL, and UNIT phrases may be used only with files that are of
            // sequential organization" (record and line sequential both, §9.1.7.2). WITH LOCK is not
            // organization-restricted. The old acceptance degraded to a stale FILE STATUS value at run time.
            if (phrase.closeOption() is { } o && o.LOCK() is null && !file.IsSequential)
            {
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.ClosePhraseOrganization,
                    $"CLOSE '{name}' with the {(o.REWIND() is not null ? "NO REWIND" : "REEL/UNIT")} phrase — "
                    + $"the phrase may be used only with a sequential-organization file (ISO §14.9.6.3 SR1)");
            }
            // The WRITTEN FORM only — the four rows of Table 14 (§14.9.6.4 GR3) plus WITH LOCK. What each form
            // DOES is the runtime's Table14 lookup against the file's §14.9.6.4 GR2 category; the binder no
            // longer pre-resolves the Non-unit column here, which is what let REEL/UNIT and REEL/UNIT FOR
            // REMOVAL collapse into one kind and left opt.REMOVAL() with no consumer (kb/Work PB235).
            BoundCloseKind kind = phrase.closeOption() is { } opt
                ? opt.LOCK() is not null ? BoundCloseKind.WithLock
                : opt.REEL() is not null || opt.UNIT() is not null
                    ? opt.REMOVAL() is not null ? BoundCloseKind.ReelUnitForRemoval : BoundCloseKind.ReelUnit
                : opt.REWIND() is not null ? BoundCloseKind.NoRewind
                : BoundCloseKind.Normal
                : BoundCloseKind.Normal;
            closes.Add((file, kind));
        }
        // §14.9.6.4 GR5 (report files): the raise site is the emitter's; the CHECKING state is bind-time.
        bool rnt = ctx.EcState.Turn.Enabled("EC-REPORT-NOT-TERMINATED", null, c.Start.Line);
        // ISO §14.9.6.4 GR10 — one implicit CLOSE statement per file-name, in source order, with "processing
        // resumes at the next implicit CLOSE statement, if any" as the resumption point (kb/Work PB419).
        return BoundImplicitSeries.Of([.. closes.Select(f =>
            (BoundStatement)new BoundClose([f]) { ReportNotTerminatedCheck = rnt })]);
    }

    /// <summary>⛔ THE ONE SITE for Annex A.4.13 — the declined <c>FILE file-name-1</c> alternative of
    /// <c>{ record-name-1 | FILE file-name-1 }</c>, shared by WRITE (§14.9.51.2, both formats) and REWRITE
    /// (§14.9.35.2). Returns true when the phrase was written, having named it (COBOLNET1706); the caller
    /// then yields <see cref="BoundUnsupported"/> without binding.
    /// <para>Placed BEFORE the sequential/keyed reroute so Format 1 and Format 2 are covered by the same
    /// check — the alternative is declined for every organization, and a check after the reroute would have
    /// left the indexed/relative arm undiagnosed. Both verbs call this single helper rather than each
    /// carrying a copy: the previous shape had WRITE silently IMPLEMENTING the unclaimed module (a live
    /// <c>else if (w.fileName() …)</c> arm writing the whole record area) while REWRITE fell through to a
    /// run-time <c>NotImplemented</c> throw whose interpolated record name was EMPTY — one construct, two
    /// arms, two postures, neither of them the documented one (feedback_two_arm_dispatch).</para>
    /// <para>The whole-record-area write it replaced was not merely undiagnosed but WRONG: §14.9.51.4 GR8
    /// derives the implicit record from the DESCRIPTION OF identifier-1, not from the largest record's
    /// view.</para></summary>
    private bool DeclinedFilePhrase(Core.FileNameContext? fileName, string verb)
    {
        if (fileName is null) return false;
        ctx.Edition.Declined(DiagnosticCatalog.WriteRewriteFileUnclaimed,
            $"{verb} FILE {fileName.GetText()}");
        return true;
    }

    public BoundStatement BindWrite(Core.WriteStatementContext w)
    {
        if (DeclinedFilePhrase(w.fileName(), "WRITE"))                      // Annex A.4.13 item 2) — COBOLNET1706
            return BoundRejected.Reported(ctx.Edition);   // Declined REPORTED it; a declined element is not an unbuilt one (PB236)
        // ⛔ THE record-name-1 IDENTITY RULE IS THE SHARED ONE (kb/Work PB347) — §14.9.51.3 SR5 is RELEASE's
        // §14.9.32.3 SR1 and REWRITE's §14.9.35.3 SR1 written a third time, so all three ask
        // StatementValidation.ResolveRecordName. Its two arms are DIFFERENT diagnoses and are kept apart: a
        // name that resolves to nothing already had COBOLNET1639 (§8.4.2.1) from the resolver and stays a
        // deferral here, while a reference that resolves but is not a logical record is the SOURCE's error and
        // is refused at bind (§4.2.2 ¶2). Conflated, `WRITE WS-REC` drew only the COBOLNET1756 deferral
        // warning — the compiler announcing ITS gap for what is the program's mistake.
        // The call sits BEFORE the `file.IsSequential` reroute below, so the RELATIVE and INDEXED arms are under
        // the rule too and not just this file's sequential one (feedback_two_arm_dispatch).
        if (w.recordName()?.dataReference() is not { } rn)
            return new BoundUnsupported($"WRITE record '{w.recordName()?.GetText()}' not resolvable to a file");
        if (host.Expr.ResolveSending(rn) is var recordAnswer && recordAnswer.Place is not { } record)
            return recordAnswer.Refusal(ctx.Edition);   // the resolver's answer (kb/Work PB1030)
        if (!ctx.Validation.ResolveRecordName(record, rn.GetText(), "WRITE",
                "record-name-1 \"is the name of a logical record in the file section of the data division and "
                + "may be qualified\" (ISO §14.9.51.3 SR5)",
                "ISO §14.9.51.3 SR21 — \"If record-name-1 is defined in a containing program and is referenced in a "
                + "contained program, the file description entry for the file-name associated with record-name-1 shall "
                + "contain a GLOBAL clause.\"", out var file))
            return BoundRejected.Reported(ctx.Edition);   // ResolveRecordName REPORTED; a refused operand is not an unbuilt one
        // §14.9.51.3 SR12's REPORT half (its sort-merge half is UnsupportedOrg's) — kb/Work PB1171.
        if (!ctx.Validation.ScreenReportFileReference(file, "WRITE", "ISO §14.9.51.3 SR12 — \"The file description "
                + "entry associated with the write file shall not contain the REPORT clause\""))
            return BoundRejected.Reported(ctx.Edition);
        // The WRITE lock/RETRY phrases (§14.9.51 Format 1/2 — [retry-phrase] [WITH LOCK | WITH NO LOCK]) bind
        // for EVERY organization; the emitter routes a lock-relevant statement through the governed runtime entry.
        BoundRecordLock wlock = fileLock.CheckRecordLockPhrase(file, w.recordLockPhrase(), "WRITE");   // §14.9.51 SR22 → COBOLNET1512
        RetrySpec? wretry = fileLock.BindVerbRetry(w.retryPhrase());                                    // §14.7.9 / §14.9.51 GR16
        if (!file.IsSequential) return keyedIo.BindWrite(w, file, record, wlock, wretry);   // relative/indexed WRITE (ISO 14.9.51 GR29-42)

        // §14.9.51.3 SR2 — "If the organization of the write file is sequential, format 1 shall be specified."
        // Format 1 (§14.9.51.2) has no INVALID KEY bracket at all, so the phrase is not admissible on THIS arm.
        // It was nevertheless PARSED and then dropped on the floor with no diagnostic and no branch: the program
        // compiled clean at every edition and neither imperative ran (kb/Work PB691). That is the third arm of
        // one silent drop — PB144 screened it for REWRITE just below, PB334 for READ — and the reason it hid is
        // that WRITE's `writeInvalidKey` sub-rule was consumed by the KEYED binder only (feedback_two_arm_dispatch).
        // Gated through the ONE severity seam (StatementValidation.ScreenForbiddenPhrase → COBOLNET1720): an
        // error under strict, a warning under --permissive with the phrase BOUND, not dropped — because
        // §9.1.14's final rule item 2 gives the NOT INVALID KEY imperative a live meaning even here ("If the I-O
        // status indicates a successful completion, control is transferred to … the imperative-statement
        // specified in the NOT INVALID KEY phrase if it is specified"). The INVALID arm is provably dead on this
        // organization — every invalid-key status (§9.1.13.5 items 1–4, '21'–'24') names a relative or indexed
        // file — so the emitter renders it as a status-first branch that simply never fires, never a reroute.
        // The ONE print-control phrase (§14.9.51.2 Format 1 prints one bracket, one `ADVANCING`, one operand),
        // read once: SR13, SR17, SR18 and the bind all ask about the SAME phrase.
        Core.WriteBeforeAfterContext? adv = w.writeBeforeAfter();
        KeyedInvalidKey? winvalid = null;
        if (w.writeInvalidKey() is { } wik)
        {
            ctx.Validation.ScreenForbiddenPhrase(true,
                PhraseBlocks.StartsWithNot(wik) ? "NOT INVALID KEY" : "INVALID KEY", "WRITE",
                "a file with sequential organization", "ISO §14.9.51.3 SR2");
            winvalid = keyedIo.KeyedInvalidPhrase(wik.statementBlock(), PhraseBlocks.StartsWithNot(wik));
        }

        // END-OF-PAGE phrases (ISO §14.9.51 GR27b/GR28): blocks[0] = AT EOP, blocks[1] = NOT AT EOP — the grammar
        // rule `writeAtEndOfPage : AT? (END_OF_PAGE|EOP) statementBlock (NOT AT? (END_OF_PAGE|EOP) statementBlock)?`
        // fixes that order (the readAtEnd block shape).
        List<BoundStatement>? atEop = null, notAtEop = null;
        if (w.writeAtEndOfPage() is { } eop)
        {
            // SR19 (the silent-drop bug class): the END-OF-PAGE / NOT END-OF-PAGE phrase requires a LINAGE clause
            // in the file's file description entry — a bind-time rejection, never a dropped branch.
            ctx.Validation.CheckWriteEopLinage(file);                                     // SR19 — pure check
            ctx.Validation.CheckWriteEopAdvancingPage(AnyAdvancePage(adv));   // SR18 — pure check
            (atEop, notAtEop) = PhraseBlocks.Split(eop.statementBlock(), PhraseBlocks.StartsWithNot(eop), b => host.BindBlocks([b]));
        }
        // SR13: with a LINAGE clause, the ADVANCING phrase shall not name a SPECIAL-NAMES mnemonic (the
        // implementor positioning rules and the logical-page model are mutually exclusive).
        ctx.Validation.CheckWriteAdvancingMnemonic(file, MnemonicOperand(adv) is not null);   // SR13 — pure check
        // SR17: the COBOL-2023 pair of words may not carry the PAGE operand (GR25 g)/h) place the record
        // "depending on the phrase used", which the pair leaves unanswered).
        ctx.Validation.CheckWriteBeforeAfterPage(
            adv is not null && adv.BEFORE() is not null && adv.AFTER() is not null,
            AnyAdvancePage(adv));   // SR17 — pure check

        // The FROM phrase is an implicit MOVE and is BOUND as one (§14.9.51.4 GR5 a); §14.9.51.3 SR6 —
        // kb/Work PB348), so it carries the MOVE statement's syntax rules and the storage facts codegen needs.
        return new BoundWrite(file, record,
            host.Move.BindFromPhrase(FromPhraseRules.Write, record, w.recordFromPhrase()?.sendingOperand()),
            BindAdvancing(adv), UnsupportedOrg(file, "WRITE"), atEop, notAtEop)
        { Lock = wlock, Retry = wretry, InvalidKey = winvalid };
    }

    public BoundStatement BindRead(Core.ReadStatementContext r)
    {
        string name = r.fileName().GetText();
        // The ONE file-name resolution step (kb/Work PB236 — §8.4.2.1 through COBOLNET1639).
        if (!ctx.Validation.ResolveFile(r.fileName(), "READ", out var file)) return BoundRejected.Reported(ctx.Edition);
        if (!file.IsSequential) return keyedIo.BindRead(r, file);   // relative/indexed READ F1/F2 (ISO 14.9.30; KeyedIo partial)
        // READ … INTO is an IMPLICIT MOVE and is bound as one (ISO §14.9.30.4 GR4 b); kb/Work PB348): the
        // sender is the record area sliced to its §13.18.43.4 GR16 byte count, resolved through the LARGEST
        // record's view (ReferenceResolver.RecordArea, §13.18.33.4 GR3) exactly as the emitter's splice is.
        // A file with no usable record area leaves the move null and the phrase inert — the same reach the emitter's own
        // `area is not null` guard had, now decided once, at bind time.
        BoundMove? intoMove = r.readInto()?.dataReference() is { } d && host.Expr.ResolveReceiving(d) is { } recv
            && ctx.Refs.RecordArea(file) is { } readArea
            ? host.Move.BindIntoPhrase(file, readArea, recv, IntoPhraseRules.Read)
            : null;
        List<BoundStatement>? atEnd = null, notAtEnd = null;
        if (r.readAtEnd() is { } ae)
            (atEnd, notAtEnd) = PhraseBlocks.Split(ae.statementBlock(), PhraseBlocks.StartsWithNot(ae), b => host.BindBlocks([b]));
        // The READ lock phrases bind on the sequential organization too — §14.9.30's GR7–GR12 lock rules are
        // ALL-FORMATS rules, and the sequential record's lock identity is its ordinal position (§9.1.16).
        // The two INDEPENDENT lock brackets of §14.9.30.2, bound in the order the rules read them: bracket 2
        // (retention) first, because SR3 tests bracket 1's IGNORING LOCK against it (kb/Work PB331).
        var contention = r.readLockContentionPhrase();
        BoundRecordLock retention = fileLock.CheckRecordLockPhrase(file, r.recordLockPhrase(), "READ");   // §14.9.30 SR4 → COBOLNET1512

        // ── The three phrases this arm PARSED AND DROPPED until kb/Work PB334 ──────────────────────────────
        // §14.9.30.4 GR19 is the rule that decides a READ's format: "An implicit or explicit NEXT phrase or a
        // PREVIOUS phrase results in a sequential read: otherwise, the read is a random read and the rules for
        // format 2 apply." On THIS arm that decision is unconditional — §12.4.5.5.2 SR2 bars ACCESS MODE
        // RANDOM/DYNAMIC on a sequential file, so §14.9.30.3 SR8 implies NEXT whenever no direction is written
        // and every READ here is a Format-1 read. That is why the KEY and INVALID KEY phrases below are
        // screened outright rather than conditioned on an access mode.
        bool previous = r.readDirection()?.PREVIOUS() is not null;
        // §14.9.30.3 SR6 — the ONE check, shared with KeyedIoBinder.BindRead. Reachable here only while
        // §12.4.5.5.2 SR2 goes unenforced at the file control entry; the phrases are forbidden either way.
        ctx.Validation.CheckReadRandomAccessPhrases(file, contention?.readAdvancingOnLock() is not null,
            atEnd is not null, notAtEnd is not null, r.readDirection()?.NEXT() is not null, previous);
        // §14.9.30.3 SR7 — "The phrase PREVIOUS shall not be specified if FILE ORGANIZATION LINE SEQUENTIAL is
        // specified in the file control entry for file-name-1." ⛔ THIS IS SR7's ONLY POSSIBLE SITE, and it had
        // none at all before: the rule pairs an ORGANIZATION with a READ DIRECTION, and the direction did not
        // exist below the parse tree on the one arm every LINE SEQUENTIAL file takes. A line-sequential
        // `READ … PREVIOUS` was accepted and read FORWARD (kb/Work PB334).
        ctx.Validation.ScreenForbiddenPhrase(previous && file.Organization == FileOrganization.LineSequential,
            "PREVIOUS", "READ", "a file with line sequential organization", "ISO §14.9.30.3 SR7");
        // §14.9.30.3 SR10 — the ONE check, shared with the keyed arm. A SEQUENTIAL or LINE SEQUENTIAL file is
        // never INDEXED, so the phrase is always forbidden here; the keyed arm reached only RELATIVE files,
        // which is why the diagnostic LOOKED present (the two-arm dispatch — feedback_two_arm_dispatch).
        if (r.readKey() is not null) ctx.Validation.CheckReadKeyOrganization(file);
        // The INVALID KEY bracket is a FORMAT-2 phrase (§14.9.30.2) and every READ on this arm is Format 1 by
        // GR19, so writing it is never conforming source. The screen routes through EditionContext.Removed, so
        // under --permissive it WARNS and the bind stands — which is why the phrase is bound rather than
        // dropped: §14.9.30.4 GR13c gives it its only possible meaning, "control is transferred … if the NOT AT
        // END phrase or NOT INVALID KEY phrase is specified, to imperative-statement-2", i.e. the NOT INVALID
        // KEY imperative runs on a successful read. The INVALID arm is never rendered — a sequential READ
        // raises no '2x' status (§9.1.13.5), so the invalid key condition cannot exist for it. Dropping the
        // whole bracket, as this arm did, compiled a NOT INVALID KEY block away in silence (kb/Work PB334).
        KeyedInvalidKey? invalid = null;
        if (r.readInvalidKey() is { } ik)
        {
            ctx.Validation.ScreenForbiddenPhrase(true,
                PhraseBlocks.StartsWithNot(ik) ? "NOT INVALID KEY" : "INVALID KEY", "READ",
                "a file with sequential organization, whose READ is a Format-1 sequential read",
                "ISO §14.9.30.2 Format 1 · §14.9.30.4 GR19");
            invalid = keyedIo.KeyedInvalidPhrase(ik.statementBlock(), PhraseBlocks.StartsWithNot(ik));
        }
        return new BoundRead(file, intoMove, atEnd, notAtEnd, UnsupportedOrg(file, "READ"))
        {
            Kind = previous ? ReadKind.Previous : ReadKind.Next,                          // §14.9.30.4 GR19; SR8
            InvalidKey = invalid,                                                         // §14.9.30.4 GR13c (permissive only)
            Lock = retention,
            Retry = fileLock.BindVerbRetry(contention?.retryPhrase()),                    // §14.7.9 / §14.9.30 GR9
            AdvancingOnLock = contention?.readAdvancingOnLock() is not null,              // §14.9.30 GR22
            IgnoringLock = fileLock.CheckIgnoringLock(file, contention, retention),       // §14.9.30 GR12; SR3 → COBOLNET1818
        };
    }

    public BoundStatement BindRewrite(Core.RewriteStatementContext rw)
    {
        if (DeclinedFilePhrase(rw.fileName(), "REWRITE"))                    // Annex A.4.13 item 1) — COBOLNET1706
            return BoundRejected.Reported(ctx.Edition);   // Declined REPORTED it; a declined element is not an unbuilt one (PB236)
        // §14.9.35.3 SR1 is WRITE's §14.9.51.3 SR5 word for word — but they are two rules that happen to share a
        // sentence, not one rule, so each site quotes its OWN clause and the shared MECHANISM is the helper, not
        // the message (kb/Work PB347). Placed, like WRITE's, BEFORE the `file.IsSequential` reroute, so the
        // relative and indexed arms are under the rule too (feedback_two_arm_dispatch).
        if (rw.recordName()?.dataReference() is not { } rn)
            return new BoundUnsupported($"REWRITE record '{rw.recordName()?.GetText()}' not resolvable to a file");
        if (host.Expr.ResolveSending(rn) is var recordAnswer && recordAnswer.Place is not { } record)
            return recordAnswer.Refusal(ctx.Edition);   // the resolver's answer (kb/Work PB1030)
        if (!ctx.Validation.ResolveRecordName(record, rn.GetText(), "REWRITE",
                "record-name-1 \"is the name of a logical record in the file section of the data division and "
                + "may be qualified\" (ISO §14.9.35.3 SR1)",
                "ISO §14.9.35.3 SR3 — \"If record-name-1 is defined in a containing program and is referenced in a "
                + "contained program, the file description entry for the file associated with record-name-1 shall "
                + "contain a GLOBAL clause.\"", out var file))
            return BoundRejected.Reported(ctx.Edition);   // ResolveRecordName REPORTED; a refused operand is not an unbuilt one
        // §14.9.35.3 SR11's REPORT half, asked of the record's file (the FILE format is declined) — kb/Work PB1171.
        if (!ctx.Validation.ScreenReportFileReference(file, "REWRITE", "ISO §14.9.35.3 SR11 — \"File-name-1 shall "
                + "not reference a report file or a sort-merge file description entry\""))
            return BoundRejected.Reported(ctx.Edition);
        BoundRecordLock rlock = fileLock.CheckRecordLockPhrase(file, rw.recordLockPhrase(), "REWRITE");   // §14.9.35 SR4 → COBOLNET1512
        RetrySpec? rretry = fileLock.BindVerbRetry(rw.retryPhrase());                                      // §14.7.9 / §14.9.35 GR11
        if (!file.IsSequential) return keyedIo.BindRewrite(rw, file, record, rlock, rretry);   // relative/indexed REWRITE (ISO 14.9.35 GR18-25)
        // §14.9.35.3 SR2, arm (a) — "Neither the INVALID KEY phrase nor the NOT INVALID KEY phrase shall be
        // specified for a REWRITE statement that references a file with SEQUENTIAL ORGANIZATION". This arm
        // dropped the parsed phrase on the floor with no diagnostic at all, which is a strictly worse shape than
        // its relative twin in KeyedIoBinder (which at least bound it as dead): the rule has TWO arms and only
        // one of them was ever noticed (kb/Work PB144 — the two-arm dispatch again). A sequential REWRITE raises
        // only 4x statuses, so nothing is rerouted either way.
        // ⛔ PB144 STOPPED ONE STEP SHORT, and PB691's sweep of the identical WRITE arm found it: the screen
        // landed but the phrase was still DROPPED, on the reasoning that "there is no '2x' invalid-key condition
        // for it to carry". That is true of the INVALID arm and FALSE of the NOT INVALID arm — §9.1.14's final
        // rule item 2 runs the NOT INVALID imperative on a SUCCESSFUL completion, which a sequential REWRITE
        // certainly has — so under --permissive the tolerated phrase still meant nothing and printed nothing.
        // Bound now, exactly as the WRITE arm above and as the keyed twin in KeyedIoBinder.BindRewrite.
        KeyedInvalidKey? rinvalid = null;
        if (rw.rewriteInvalidKeyPhrase() is { } ik)
        {
            ctx.Validation.ScreenForbiddenPhrase(true,
                PhraseBlocks.StartsWithNot(ik) ? "NOT INVALID KEY" : "INVALID KEY", "REWRITE",
                "a file with sequential organization", "ISO §14.9.35.3 SR2");
            rinvalid = keyedIo.KeyedInvalidPhrase(ik.statementBlock(), PhraseBlocks.StartsWithNot(ik));
        }
        return new BoundRewrite(file, record,
            host.Move.BindFromPhrase(FromPhraseRules.Rewrite, record, rw.recordFromPhrase()?.sendingOperand()),
            UnsupportedOrg(file, "REWRITE"))
        { Lock = rlock, Retry = rretry, InvalidKey = rinvalid };
    }

    /// <summary>The §14.9.51.3 SR18 test — does this WRITE's print-control phrase name the PAGE operand?
    /// One operand per statement (§14.9.51.2 Format 1 prints one), so this is one nullable check.</summary>
    private static bool AnyAdvancePage(Core.WriteBeforeAfterContext? wba) => wba?.PAGE() is not null;

    /// <summary>Bind the ONE <c>[BEFORE] [AFTER] ADVANCING …</c> phrase of a WRITE (ISO §14.9.51.2 Format 1), or
    /// null for a plain WRITE. An ADVANCING operand naming a SPECIAL-NAMES mnemonic (<c>XXXXX073 IS
    /// MNEMONIC-NAME</c>, SQ207M — the NIST preprocessor supplies the feature-name CSP) positions per the
    /// IMPLEMENTOR's rules for the associated feature (§14.9.51.4 GR25 d)), which are the feature-name's row in
    /// <see cref="ImplementorNames"/> — see <see cref="FeatureAdvancing"/>.
    /// <para>⛔ THE PHRASE IS ONE ADVANCE, NOT ONE PER WORD, AND THE TWO WORDS ONLY PLACE IT (kb/Work PB712).
    /// §14.9.51.4 GR25 a)–d) fix the AMOUNT from the single printed operand — a) says the page "is advanced the
    /// number of lines equal to that value", once. GR25 e)–h) then say WHERE that one advance goes relative to
    /// the presentation: e) <i>"If the BEFORE phrase is used, the line is presented before the … page is advanced
    /// according to General rule 25a, 25b, 25c, and 25d"</i>; f) <i>"If the AFTER phrase is used and the BEFORE
    /// phrase is not used, the line is presented after …[;] If the AFTER phrase is used and the BEFORE phrase is
    /// also used, the printed page is advanced … AFTER THE LINE WAS PRESENTED as specified in General rule
    /// 25e."</i> So BEFORE and BEFORE-AFTER bind IDENTICALLY — present, then advance once — and only a lone
    /// AFTER advances first. (g)/h) place the PAGE operand "depending on the phrase used", which the pair leaves
    /// unanswered — which is exactly what SR17 removes.) This binder therefore emits ONE
    /// <see cref="BoundAdvancing"/> whose <c>Before</c> IS that placement decision; the tree carries no second
    /// amount, because the format prints no second operand.</para></summary>
    private BoundAdvancing? BindAdvancing(Core.WriteBeforeAfterContext? wba)
    {
        if (wba is null) return null;
        bool before = wba.BEFORE() is not null;   // §14.9.51.4 GR25 e)/f): BEFORE, alone or with AFTER, presents first
        if (wba.PAGE() is not null) return new BoundAdvancing(before, true, null);
        if (MnemonicOperand(wba) is (var m, var name, var system))
        {
            // A mnemonic-name is a WORD (§8.3.2.2.16), so it takes no subscript, reference modification or
            // qualifier (kb/Work PB2499). The grammar cannot refuse one in THIS slot (it is identifier-2's
            // dataReference too), so this arm does, with the one message the parse layer gives the other three.
            if (m.dataReferenceSuffix().Length > 0)
                ctx.Edition.Error(DiagnosticCatalog.StatementFormatShape,
                    MnemonicNameSlot.SuffixMessage("WRITE … ADVANCING", name, "§14.9.51.2 Format 1"));
            // ⛔ LINE / LINES BELONG TO THE COUNT ALTERNATIVE ONLY (kb/Work PB1189). §14.9.51.2 Format 1 prints
            // `{ identifier-2 | integer-1 } [ LINE | LINES ]` and `{ mnemonic-name-1 | PAGE }` as the two operand
            // alternatives, so `mnemonic-name-1 LINES` is no WRITE statement. The grammar cannot tell a mnemonic
            // from identifier-2 (both are a dataReference), so the refusal lives here, on the arm that knows; the
            // PAGE twin is a parse error because PAGE is a keyword. The word used to be dropped in silence.
            if ((wba.LINE() ?? wba.LINES()) is { } word)
                ctx.Edition.Error(DiagnosticCatalog.StatementFormatShape,
                    $"WRITE … ADVANCING {DataBinder.WrittenText(m)} {word.GetText()}: '{name}' "
                    + "is a mnemonic-name, and ISO §14.9.51.2 Format 1 writes LINE or LINES only after the count "
                    + "alternative { identifier-2 | integer-1 } — the other alternative is { mnemonic-name-1 | PAGE } "
                    + $"with no such word. Remove '{word.GetText()}'.");
            return FeatureAdvancing(before, name, system);
        }
        BoundOperand lines =
            wba.integerLiteral() is { } il ? new BoundNumericLiteral(il.GetText())
            // identifier-2 as a function-identifier (§8.4.3.1.2 Format 1),
            // or as an inline method invocation (§8.4.3.1.2 Format 4) — kb/Work PB1421's sibling sweep —
            // each screened on its temporary item below.
            : wba.functionCall() is { } fc ? host.Intrinsic.IntrinsicOperand(fc)
            : wba.inlineMethodInvocation() is { } imi ? host.Oo.OoInlineInvocationOperand(imi)
            : wba.dataReference() is { } d ? host.Expr.FieldOperand(d)
            : wba.literal() is { } lit ? host.Expr.LiteralOperand(lit)
            : new BoundNumericLiteral("1");
        ScreenAdvancingCount(lines, wba);
        return new BoundAdvancing(before, false, lines);
    }

    /// <summary>⛔ THE ADVANCING COUNT'S TWO ALTERNATIVES, SCREENED ON THE BOUND SHAPE (kb/Work PB1023). §14.9.51.2
    /// prints `{ identifier-2 | integer-1 } [ LINE | LINES ]`, and each alternative has its own rule: SR14 —
    /// "Identifier-2 shall reference an integer data item" — is the <see cref="OperandPositions.WriteAdvancingIdentifier"/>
    /// row of the ONE operand-class screen; SR15 — "Integer-1 shall be positive or zero" — closes the literal, which
    /// is an INTEGER literal by its name (§8.3.3.3.2: "An integer literal is a fixed-point numeric literal that
    /// contains no decimal point"), asked of the ONE integer classifier. The screen keys on the BOUND shape, not the
    /// parse arm, because a constant-name parses as a dataReference and stands for integer-1 (§13.10.4 GR1), and
    /// the grammar's wide `literal` arm admits shapes neither alternative names. Before this, a PIC X item holding
    /// "2" advanced two lines, a PIC 9V9 holding 1.5 advanced one, and `ADVANCING 1.5 LINES` / `"2" LINES` /
    /// `-1 LINES` compiled — every one silently.</summary>
    private void ScreenAdvancingCount(BoundOperand lines, Core.WriteBeforeAfterContext wba)
    {
        switch (lines)
        {
            case BoundOperandError:
                return;   // reported where it was made
            // identifier-2 in every identifier format (a data reference, or a function-identifier's or inline
            // method invocation's temporary item, a folded function value among them — kb/Work PB1421): the ONE
            // operand-class screen classes each.
            case BoundFieldOperand or BoundComputedOperand or BoundNumericLiteral { FunctionValue: true }:
                OperandClassScreen.Screen(ctx.Edition, OperandPositions.WriteAdvancingIdentifier, lines,
                    DataBinder.WrittenText((Antlr4.Runtime.ParserRuleContext?)wba.functionCall()
                        ?? (Antlr4.Runtime.ParserRuleContext?)wba.inlineMethodInvocation() ?? wba.dataReference()!));
                return;
            case BoundNumericLiteral n when IntrinsicResultType.IsIntegerOperand(n) && !n.Text.TrimStart().StartsWith('-'):
            case BoundFigurative { Kind: 'Z' }:   // ZERO read numerically is the integer 0 (§8.3.3.6.4 GR4)
                return;
            default:
                string text = wba.dataReference() is { } dr ? DataBinder.WrittenText(dr)
                    : wba.literal() is { } l ? DataBinder.WrittenText(l)
                    : DataBinder.WrittenText(wba.integerLiteral());
                ctx.Edition.Error(DiagnosticCatalog.WriteAdvancingOperand,
                    $"WRITE ADVANCING integer-1 '{text}' shall be an integer literal that is positive or zero "
                    + "(ISO §14.9.51.3 SR15; §8.3.3.3.2 — \"An integer literal is a fixed-point numeric literal that "
                    + "contains no decimal point\"), or identifier-2 an integer data item (SR14)");
                return;
        }
    }

    /// <summary>⛔ mnemonic-name-1 OR identifier-2 — ASKED OF THE REFERENCE'S WORD (kb/Work PB2499's WRITE arm).
    /// §14.9.51.2 Format 1 prints both in the ADVANCING operand and the grammar parses both as one
    /// <c>dataReference</c>, so only the SPECIAL-NAMES registry tells them apart. The lookup used to key on the
    /// reference's WHOLE text, so <c>ADVANCING MYTOP (1)</c> missed the mnemonic, fell through to the data
    /// resolver and drew "'MYTOP (1)' is not defined — no declaration … gives the name 'MYTOP'" for a name
    /// SPECIAL-NAMES does declare. Keyed on the word, the suffixed mnemonic reaches its own arm, where
    /// <see cref="BindAdvancing"/> refuses the suffix by name. Pure (no report), because the SR13 screen asks it
    /// too.</summary>
    private (Core.DataReferenceContext Ref, string Name, ImplementorName System)? MnemonicOperand(
        Core.WriteBeforeAfterContext? wba)
        => wba?.dataReference() is { } m && m.cobolWord() is { } word
           && ctx.Mnemonics.Of(wba).TryGetValue(word.GetText(), out var system)
            ? (m, word.GetText(), system)
            : null;

    /// <summary>The ADVANCING operand is a SPECIAL-NAMES mnemonic-name. §14.9.51.3 SR16: "<i>When mnemonic-name-1
    /// is specified, the name is associated with a feature-name specified by the implementor.</i>" — so a switch's
    /// or a device's mnemonic is COBOLNET2243 (kb/Work PB862; it used to bind as a zero-line advance, whatever it
    /// named). A feature-name advances per its row in the ONE table (§14.9.51.4 GR25 d — "according to the rules
    /// specified by the implementor", Annex A.1 item 222): <see cref="FeatureAdvance.TopOfPage"/> (C01) is the
    /// <c>ADVANCING PAGE</c> advance, <see cref="FeatureAdvance.SuppressSpacing"/> (CSP) a zero-line advance.</summary>
    private BoundAdvancing FeatureAdvancing(bool before, string mnemonic, ImplementorName system)
    {
        if (system.Kind != SystemNameKind.Feature)
            ctx.Edition.Error(DiagnosticCatalog.WriteAdvancingMnemonicNotFeature, $"WRITE … ADVANCING '{mnemonic}': "
                + $"'{mnemonic}' is the mnemonic-name of the {ImplementorNames.KindWord(system.Kind)} '{system.Name}' — "
                + "\"When mnemonic-name-1 is specified, the name is associated with a feature-name specified by the "
                + "implementor\" (ISO §14.9.51.3 SR16), and a mnemonic-name that is not a feature-name's may not be "
                + "specified in WRITE (§12.3.7.3 SR5, SR7)");
        return system.Advance == FeatureAdvance.TopOfPage
            ? new BoundAdvancing(before, true, null)
            : new BoundAdvancing(before, false, new BoundNumericLiteral("0"));
    }

    private static BoundOpenMode MapOpenMode(Core.OpenModeContext m) =>
        m.OUTPUT() is not null ? BoundOpenMode.Output
        : m.EXTEND() is not null ? BoundOpenMode.Extend
        : m.I_O() is not null ? BoundOpenMode.IO
        : BoundOpenMode.Input;

    // ⛔ `FileOfRecord` LIVED HERE AND IS GONE (kb/Work PB347). It answered "which file owns the record
    // CONTAINING this reference" while all three of its callers — WRITE, REWRITE and RELEASE — asked "which
    // file has this reference AS a record", so a subordinate item or a reference-modified record passed at
    // every one of them. The rule now has ONE home in the syntax-rule catalog,
    // `StatementValidation.ResolveRecordName`, which owns both the identity test and the diagnosis.

    /// <summary>The §13.4.6.3 SR3/SR4 sort-merge screen (a BIND-TIME error, kb/Work PB140 — routed through the
    /// ONE <c>StatementValidation.ScreenSortMergeFile</c>), returning the message the verb's bound node keeps
    /// as its loud belt. Every ISO §12.4.5.10 organization (sequential, line sequential, relative, indexed)
    /// has a dedicated bind/emit path — the relative/indexed verbs route through the KeyedIo partial —
    /// so this seam fires only for an SD file, and stays the single seam a future organization gates on.</summary>
    private string? UnsupportedOrg(FileModel file, string verb) => ctx.Validation.ScreenSortMergeFile(file, verb);
}
