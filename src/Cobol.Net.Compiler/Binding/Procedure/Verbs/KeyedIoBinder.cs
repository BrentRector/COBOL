// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

// ═══════════════════════════════════════════════════════════════════════════════════════════════════════════════
//  RELATIVE + INDEXED file organizations — the keyed-I/O bound nodes and binder (ISO/IEC 1989:2023 §9.1.7.3/.4,
//  §12.4.5, §14.9.10 DELETE, §14.9.30 READ F1/F2, §14.9.35 REWRITE, §14.9.41 START, §14.9.51 WRITE; COBOLNET_DESIGN
//  §8). The sequential subsystem is extended, never forked: BindRead/BindWrite/BindRewrite route non-sequential
//  organizations here; OPEN/CLOSE flow through the existing nodes (the CobolFile facade dispatches by connector).
// ═══════════════════════════════════════════════════════════════════════════════════════════════════════════════

/// <summary>P7 Step 10h: a real collaborator over <see cref="BinderContext"/> — the SeqIo↔KeyedIo cycle
/// mirrors the emitter side (SequentialIoBinder holds THIS binder for the org reroutes; the WRITE/REWRITE
/// FROM/INTO phrases bind through <c>host.Move</c>, the ONE home of every MOVE - kb/Work PB348). The nine bound
/// types stayed in <c>Binding/Bound/BoundKeyedIo.cs</c>. This binder is edition-AGNOSTIC and holds ZERO
/// <c>Check</c> calls: every I/O phrase gate fires on RECOGNITION in the VersionConformancePass PARSE arm, so a
/// statement this binder refuses — an undeclared file, an unresolvable key operand, an SD — still names the
/// edition of the phrase it wrote (kb/Work PB353; the Kind/Mode/Length/AdvancingOnLock members no longer carry a
/// gate at all).
/// <para>⛔ IT SCREENS STATEMENTS, NOT ENTRIES. A <c>KeyedValidateFile</c> here used to check the FILE CONTROL
/// ENTRY's own syntax rules on the first keyed verb that named the file, behind a <c>_keyedCheckedFiles</c> memo
/// that existed only to make that once-per-file — so an entry whose keys break those rules compiled clean when
/// no keyed verb named it (kb/Work PB699). Those rules moved WHOLE to <see cref="FileControlKeyRules"/>, which
/// <c>DataBinder.ResolveFiles</c> runs over every declared file. Nothing about a key clause belongs here; a rule
/// belongs here only if the standard states it about a STATEMENT.</para></summary>
internal sealed class KeyedIoBinder(BinderContext ctx, StatementBinder host, FileLockBinder fileLock)
{
    // ── READ (ISO §14.9.30 Formats 1 and 2 on relative/indexed organizations) ─────────────────────────────────

    /// <summary>Bind a READ of a RELATIVE/INDEXED file. The format decision follows the syntax rules: an explicit
    /// NEXT/PREVIOUS is sequential (GR19); ACCESS SEQUENTIAL implies NEXT (SR8); ACCESS DYNAMIC implies NEXT only
    /// when an AT END / NOT AT END phrase is present — otherwise it is a Format-2 random read (SR9); ACCESS RANDOM
    /// is always Format 2 and forbids NEXT/PREVIOUS/AT END (SR6).</summary>
    public BoundStatement BindRead(Core.ReadStatementContext r, FileModel file)
    {
        // Bracket 1 of §14.9.30.2 — [ADVANCING ON LOCK | IGNORING LOCK | retry-phrase] (kb/Work PB331).
        var contention = r.readLockContentionPhrase();
        // READ ... INTO is an IMPLICIT MOVE and is bound as one (ISO §14.9.30.4 GR4 b); kb/Work PB348) - the
        // SAME call the sequential arm makes, so the two organizations cannot disagree about which rules the
        // phrase carries (feedback_two_arm_dispatch).
        BoundMove? into = r.readInto()?.dataReference() is { } d && host.Expr.ResolveReceiving(d) is { } recv
            && ctx.Refs.RecordArea(file) is { } readArea
            ? host.Move.BindIntoPhrase(file, readArea, recv, IntoPhraseRules.Read)
            : null;
        List<BoundStatement>? atEnd = null, notAtEnd = null;
        if (r.readAtEnd() is { } ae)
            (atEnd, notAtEnd) = PhraseBlocks.Split(ae.statementBlock(), PhraseBlocks.StartsWithNot(ae), b => host.BindBlocks([b]));
        KeyedInvalidKey? invalid =
            r.readInvalidKey() is { } ik ? KeyedInvalidPhrase(ik.statementBlock(), PhraseBlocks.StartsWithNot(ik)) : null;

        bool next = r.readDirection()?.NEXT() is not null;
        bool previous = r.readDirection()?.PREVIOUS() is not null;
        // READ PREVIOUS (§14.9.30 Format 1) is a COBOL-2002 introduction; the edition gate lives in the
        // VersionConformancePass PARSE arm (VisitReadDirection), firing on the printed phrase, not on this
        // binder's IBoundRead.Kind — which a refused READ never reaches (kb/Work PB353).

        // §14.9.30.3 SR6 — the ONE check, shared with the sequential arm (kb/Work PB334). Gated through the ONE
        // severity seam (kb/Work PB144): an error under strict, a warning under --permissive with the bind
        // UNCHANGED, because the CCVS-85 corpus is lenient about phrase placement (the L1–L3 family) and the
        // emitter's status-first branches make a phrase that cannot fire ('1x' on a random read) simply dead,
        // never silently rerouted. SR10/SR11 (the KEY phrase) are hard errors below and always were.
        ctx.Validation.CheckReadRandomAccessPhrases(file, contention?.readAdvancingOnLock() is not null,
            atEnd is not null, notAtEnd is not null, next, previous);
        ReadKind kind = file.AccessMode switch
        {
            FileAccessMode.Random => ReadKind.Random,
            // §14.9.30.3 SR9 — dynamic: "the NEXT phrase is implied if any of the following phrases is
            // specified: ADVANCING, AT END, or NOT AT END". ⛔ ALL THREE, not two: while ADVANCING was missing
            // from this test, `READ f ADVANCING ON LOCK` on a DYNAMIC file bound as the Format-2 random read,
            // where ADVANCING ON LOCK is not even in the general format — so the GR22 skip-scan could not run
            // for the one spelling SR9 exists to name (kb/Work PB340). A bare READ under dynamic access, with
            // none of the three, stays the Format-2 random read.
            FileAccessMode.Dynamic => previous ? ReadKind.Previous
                : next || contention?.readAdvancingOnLock() is not null || r.readAtEnd() is not null
                    ? ReadKind.Next
                    : ReadKind.Random,
            // §14.9.30 SR8 — sequential access: NEXT implied.
            _ => previous ? ReadKind.Previous : ReadKind.Next,
        };

        int keyIndex = -1;   // the prime record key (GR31) / the relative key item (GR29) when no KEY phrase
        if (r.readKey()?.dataReference() is { } keyRef)
        {
            // SR10 — ⛔ NOT WRITTEN HERE: `StatementValidation.CheckReadKeyOrganization` is the ONE site, so the
            // sequential arm enforces the SAME rule with the SAME diagnostic (kb/Work PB334). A violation is
            // REPORTED and the bind continues with the default key of reference (kb/Work PB236); SR11 below is
            // then not asked, because a file that is not INDEXED has no RECORD KEY for it to name.
            // SR11: the operand names a declared (prime or alternate) key — matched by identity OR by STORAGE
            // POSITION (§12.4.5.12.4 GR4: the key's identical BYTE POSITIONS are implicitly keys in EVERY record
            // description of the file; covers REDEFINES and duplicate names). ⛔ SR11 has NO generic-key arm —
            // "Data-name-1 or record-key-name-1 shall be specified in the RECORD KEY clause or an ALTERNATE
            // RECORD KEY clause associated with file-name-1" — so it asks `RecordLayout.KeyIndexOfKeyItem`, the
            // IDENTITY answer, never START's SR6 b) generic screen (kb/Work PB354).
            if (ctx.Validation.CheckReadKeyOrganization(file))
            {
                if (kind != ReadKind.Random)
                    ctx.Edition.Error("COBOLNET0864", $"READ … KEY on '{file.CobolName}' is a Format-2 phrase and "
                        + "cannot combine with NEXT/PREVIOUS/AT END (ISO §14.9.30 general formats)");
                else if (host.Expr.ResolveSending(keyRef).PlaceOrReported(ctx.Edition) is not { } keyPlace)
                    return BoundRejected.Reported(ctx.Edition);   // the resolver's diagnostic, never SR11's (kb/Work PB1030)
                // ⛔ SR11 IS AN IDENTITY OVER DATA ITEMS, SO IT ASKS DenotedItem — START's SR5/SR6 twin (kb/Work
                // PB602), fixed there and not here until kb/Work PB481's key sweep. A reference-modified operand
                // denotes §8.4.3.3.4 GR5's "unique data item that is a subset of the data item referenced by
                // identifier-1", which no RECORD KEY clause names; asked of Item it inherited the key's answer and
                // `READ IXF KEY IS IX-KEY(1:3)` compiled clean and read on the whole key.
                else if (keyPlace.DenotedItem is not { } keyItem
                    || Model.RecordLayout.KeyIndexOfKeyItem(file, keyItem) is not { } ki)
                {
                    return BoundRejected.Report(ctx.Edition, "COBOLNET0864", $"READ … KEY IS {keyRef.GetText()} on '{file.CobolName}': the "
                        + "operand shall be the RECORD KEY or an ALTERNATE RECORD KEY of the file (ISO §14.9.30.3 SR11)");
                }
                else keyIndex = ki;
            }
        }
        // READ … ADVANCING ON LOCK (§14.9.30 record-lock phrase, COBOL-2002); the edition gate lives in the
        // VersionConformancePass PARSE arm (VisitReadAdvancingOnLock), beside its two printed siblings
        // (IGNORING LOCK, the retention phrase) — one construct id, one arm (kb/Work PB353).
        // The two INDEPENDENT lock brackets of §14.9.30.2 (kb/Work PB331): bracket 2 (retention) binds first,
        // because SR3 tests bracket 1's IGNORING LOCK against it. Same shape as SequentialIoBinder.BindRead —
        // both READ arms, so the split cannot be half-applied (feedback_two_arm_dispatch).
        BoundRecordLock retention = fileLock.CheckRecordLockPhrase(file, r.recordLockPhrase(), "READ");
        return new BoundKeyedRead(file, kind, keyIndex, into, atEnd, notAtEnd, invalid)
        {
            Lock = retention,
            Retry = fileLock.BindVerbRetry(contention?.retryPhrase()),
            AdvancingOnLock = contention?.readAdvancingOnLock() is not null,
            IgnoringLock = fileLock.CheckIgnoringLock(file, contention, retention),
        };
    }

    // ── WRITE / REWRITE (ISO §14.9.51 / §14.9.35 on relative/indexed organizations) ────────────────────────────

    /// <summary>Bind a WRITE on a RELATIVE/INDEXED file (called from <c>BindWrite</c> with the record, owning
    /// file, and the already-validated lock/RETRY phrases resolved — §14.9.51 [retry-phrase] [WITH LOCK|WITH NO
    /// LOCK]). The print-control phrases (ADVANCING / END-OF-PAGE) apply to sequential print files only and fail
    /// loud here.</summary>
    public BoundStatement BindWrite(Core.WriteStatementContext w, FileModel file, Place record,
        BoundRecordLock lock_, RetrySpec? retry)
    {
        if (w.writeBeforeAfter() is not null || w.writeAtEndOfPage() is not null)
            // ⛔ A SYNTAX RULE, NOT A DEFERRAL (kb/Work PB909): SR3 is SR2's mirror, and COBOLNET1720 is where SR2
            // already lands. It is an error in BOTH lanes — Format 2 gives a print-control phrase no meaning to bind.
            return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.IoPhraseForbiddenHere, $"the "
                + (w.writeBeforeAfter() is not null ? "ADVANCING" : "END-OF-PAGE")
                + $" phrase shall not be specified for a WRITE statement that references the "
                + $"{file.Organization.ToString().ToLowerInvariant()} file '{file.CobolName}': \"If the organization "
                + "of the write file is indexed or relative, format 2 shall be specified\" (ISO §14.9.51.3 SR3), and "
                + "Format 2 prints no print-control phrase");
        KeyedInvalidKey? invalid =
            w.writeInvalidKey() is { } ik ? KeyedInvalidPhrase(ik.statementBlock(), PhraseBlocks.StartsWithNot(ik)) : null;
        return new BoundKeyedWrite(file, record,
            host.Move.BindFromPhrase(FromPhraseRules.Write, record, w.writeFrom()?.sendingOperand()), invalid)
        { Lock = lock_, Retry = retry };
    }

    /// <summary>Bind a REWRITE on a RELATIVE/INDEXED file. §14.9.35 SR2: the INVALID KEY phrases shall not be
    /// specified for a relative-organization file in SEQUENTIAL access mode (its rewrite has no key condition —
    /// only '43'/'49' logic errors, which route to exception processing).</summary>
    public BoundStatement BindRewrite(Core.RewriteStatementContext rw, FileModel file, Place record,
        BoundRecordLock lock_, RetrySpec? retry)
    {
        // §14.9.35.3 SR2, arm (b) — "… or a file with relative organization and sequential access mode". Arm (a),
        // sequential ORGANIZATION, never reaches here and is screened at SequentialIoBinder.BindRewrite, which
        // dropped the phrase entirely; the rule has two arms and only this one was even commented (kb/Work
        // PB144). Gated through the ONE severity seam: error under strict, warning + unchanged bind under
        // --permissive, where the phrase stays dead in the status-first branches.
        KeyedInvalidKey? invalid = null;
        if (rw.rewriteInvalidKeyPhrase() is { } ik)
        {
            ctx.Validation.ScreenForbiddenPhrase(
                file.Organization == FileOrganization.Relative
                    && file.AccessMode == FileAccessMode.Sequential,
                PhraseBlocks.StartsWithNot(ik) ? "NOT INVALID KEY" : "INVALID KEY", "REWRITE",
                "a file with relative organization and sequential access mode", "ISO §14.9.35.3 SR2");
            invalid = KeyedInvalidPhrase(ik.statementBlock(), PhraseBlocks.StartsWithNot(ik));
        }
        return new BoundKeyedRewrite(file, record,
            host.Move.BindFromPhrase(FromPhraseRules.Rewrite, record, rw.rewriteFrom()?.sendingOperand()), invalid)
        { Lock = lock_, Retry = retry };
    }

    // ── DELETE (ISO §14.9.10 Formats 1 and 2) ──────────────────────────────────────────────────────────────────

    /// <summary>Bind <c>DELETE file RECORD</c> (ISO §14.9.10 Format 1). SR1: never for sequential organization;
    /// SR2: no INVALID KEY phrases in sequential access mode (the deletion target is the prior READ's record, GR2
    /// — there is no key condition to raise).</summary>
    public BoundStatement BindDelete(Core.DeleteStatementContext del)
    {
        string name = del.fileName().GetText();
        // The ONE file-name resolution step (kb/Work PB236 — §8.4.2.1 through COBOLNET1639).
        if (!ctx.Validation.ResolveFile(del.fileName(), "DELETE", out var file)) return BoundRejected.Reported(ctx.Edition);
        // §13.4.6.3 SR3: an SD file (its SELECT may even carry ORGANIZATION RELATIVE/INDEXED) previously bound
        // and ran against an unregistered connector — the fail-open status read '00' (kb/Work PB140).
        if (ctx.Validation.ScreenSortMergeFile(file, "DELETE") is not null)
            return BoundRejected.Reported(ctx.Edition);   // the screen REPORTED; a loud runtime stage on top would re-answer it (PB236)
        if (file.IsSequential)
        {
            return BoundRejected.Report(ctx.Edition, "COBOLNET0865", $"DELETE RECORD shall not be specified for sequential-organization "
                + $"file '{name}' (ISO §14.9.10.3 SR1)");
        }
        // §14.9.10.3 SR2 — the INVALID KEY / NOT INVALID KEY phrases shall not be specified for a DELETE RECORD
        // that references a file in SEQUENTIAL ACCESS MODE. Gated through the ONE severity seam (kb/Work PB144):
        // an error under strict, a warning under --permissive with the bind unchanged — a sequential-access
        // DELETE raises only 4x statuses, so the tolerated phrase stays dead in the status-first branches and is
        // never silently rerouted.
        KeyedInvalidKey? invalid = null;
        if (del.deleteInvalidKeyPhrase() is { } ik)
        {
            ctx.Validation.ScreenForbiddenPhrase(file.AccessMode == FileAccessMode.Sequential,
                PhraseBlocks.StartsWithNot(ik) ? "NOT INVALID KEY" : "INVALID KEY", "DELETE RECORD",
                "a file that is in sequential access mode", "ISO §14.9.10.3 SR2");
            invalid = KeyedInvalidPhrase(ik.statementBlock(), PhraseBlocks.StartsWithNot(ik));
        }
        return new BoundKeyedDelete(file, invalid)
        { Retry = fileLock.BindVerbRetry(del.retryPhrase()) };   // §14.7.9 / §14.9.10 GR6
    }

    /// <summary>Bind <c>DELETE FILE file</c> (ISO §14.9.10 Format 2 — COBOL-2023). The construct parses at every
    /// edition; its introduction gate fires on RECOGNITION in the VersionConformancePass parse arm
    /// (<c>VisitDeleteFileStatement</c>, Step 14h.2 — COBOLNET0900 below 2023), so an undeclared file-name names
    /// its edition too; no <c>BoundKeyedDeleteFile</c> node is involved in the gate.</summary>
    public BoundStatement BindDeleteFile(Core.DeleteFileStatementContext df)
    {
        // §14.9.10.2 Format 2 (kb/Work PB134): [OVERRIDE] {file-name-1}…. GR12: multiple names execute as if
        // a separate DELETE FILE statement had been written for EACH, in order — each element re-binds the
        // phrase blocks, the exact textual-duplication semantics the rule states. The OVERRIDE phrase precedes
        // the WHOLE name list, so GR12's as-if duplication carries it onto every element (kb/Work PB196).
        bool overridden = df.OVERRIDE() is not null;   // §14.9.10.4 GR18 second sentence
        var steps = new List<BoundStatement>();
        foreach (var fn in df.fileName())
        {
            string name = fn.GetText();
            // The ONE file-name resolution step (kb/Work PB236 — §8.4.2.1 through COBOLNET1639). GR12's as-if
            // duplication means each name is its own statement, so a bad one drops its own element and the
            // others still bind.
            if (!ctx.Validation.ResolveFile(fn, "DELETE FILE", out var file)) continue;
            // §14.9.10.3 SR3 / §13.4.6.3 SR3: DELETE FILE of the sort-merge file rejects at bind time —
            // it previously compiled and the statement's TWO status channels answered oppositely (kb/Work PB140).
            if (ctx.Validation.ScreenSortMergeFile(file, "DELETE FILE") is not null)
                continue;   // the screen REPORTED; a loud runtime stage on top would re-answer it (PB236)
            List<BoundStatement>? on = null, notOn = null;
            if (df.deleteFileOnException() is { } ex)
                (on, notOn) = PhraseBlocks.Split(ex.statementBlock(), PhraseBlocks.StartsWithNot(ex), b => host.BindBlocks([b]));
            steps.Add(new BoundKeyedDeleteFile(file, on, notOn)
            {
                Retry = fileLock.BindVerbRetry(df.retryPhrase()),   // §14.7.9 / §14.9.10 GR15 — the '62' re-attempt
                Override = overridden,                              // §14.9.10.4 GR18 — suppress the attribute match
            });
        }
        return steps.Count == 1 ? steps[0] : new BoundSequence(steps);
    }

    // ── START (ISO §14.9.41) ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>Bind START, all THREE organizations. SR1: access shall be sequential or dynamic; SR2: a
    /// sequential-organization file admits ONLY the FIRST/LAST forms; SR3: NOT EQUAL is not a valid operator;
    /// SR4: neither operand may be subject to an OCCURS clause; SR5: a relative operand shall be the RELATIVE
    /// KEY item; SR6: an indexed operand is a record key (a) or a same-class/category/usage leftmost-coincident
    /// shorter item (b — the generic key); SR8: WITH LENGTH requires indexed organization; GR8/GR15: a missing
    /// KEY phrase means EQUAL on the relative key / prime key. FIRST/LAST are 2002+, gated on RECOGNITION in the
    /// VersionConformancePass parse arm (VisitStartStatement) — never on the node this method may not build.</summary>
    public BoundStatement BindStart(Core.StartStatementContext st)
    {
        string name = st.fileName().GetText();
        // The ONE file-name resolution step (kb/Work PB236 — §8.4.2.1 through COBOLNET1639).
        if (!ctx.Validation.ResolveFile(st.fileName(), "START", out var file)) return BoundRejected.Reported(ctx.Edition);
        // §9.1.19 / §13.4.6.3 SR3: "The only statements that may reference a sort file are the RELEASE, RETURN,
        // and SORT statements." START was the ONE keyed verb that skipped this screen (BindDelete,
        // BindDeleteFile, BindClose and SequentialIoBinder.UnsupportedOrg all call it — the PB140 consolidation),
        // so an SD whose SELECT carried ORGANIZATION INDEXED accepted a START and ran against an unregistered
        // connector (kb/Work PB352).
        if (ctx.Validation.ScreenSortMergeFile(file, "START") is not null)
            return BoundRejected.Reported(ctx.Edition);   // the screen REPORTED; a loud runtime stage on top would re-answer it (PB236)
        if (file.AccessMode == FileAccessMode.Random)
            ctx.Edition.Error(DiagnosticCatalog.IoStatementOperandRule, $"START on '{name}': the access mode shall be sequential or "
                + "dynamic (ISO §14.9.41.3 SR1)");
        KeyedInvalidKey? invalid =
            st.startInvalidKeyPhrase() is { } ik ? KeyedInvalidPhrase(ik.statementBlock(), PhraseBlocks.StartsWithNot(ik)) : null;

        if (st.FIRST() is not null || st.LAST() is not null)
        {
            // START FIRST/LAST (§14.9.41) is a COBOL-2002 introduction; the edition gate fires on RECOGNITION in
            // the VersionConformancePass parse arm (VisitStartStatement), never on BoundKeyedStart.Mode. This is the
            // ONLY form §14.9.41.3 SR2 leaves open on a sequential-organization file, and it binds for all three
            // organizations — the connector answers GR11/GR12 (relative), GR18/GR19 (indexed) or GR20/GR21
            // (sequential) on the ONE CobolFile.StartFirstLast entry.
            return new BoundKeyedStart(file, st.LAST() is not null ? KeyedStartMode.Last : KeyedStartMode.First,
                "==", -1, null, null, invalid);
        }

        // ── The organization-INDEPENDENT syntax rules, ALL of them, before any arm bails ────────────────────
        // ⛔ EVERY RULE A STATEMENT VIOLATES GETS REPORTED (kb/Work PB352). SR3, SR4 and SR8 constrain the KEY
        // phrase whatever the organization is, so they are screened here, above SR2's sequential rejection and
        // above the operand-resolution bail — each of which used to swallow the rules below it. Measured before
        // the reorder: `START SQF KEY IS = SQ-REC WITH LENGTH 2` on a sequential file violates SR8 AND SR2 and
        // reported NEITHER.
        var kp = st.startKeyPhrase();
        string op = kp?.comparisonOperator() is { } oc ? ConditionBinder.MapOperator(oc.GetText()) : "==";   // GR8/GR15 — EQUAL
        // §14.9.41.3 SR3 has TWO halves: relational-operator "is a relational operator specified in the
        // general-relation format of 8.8.4.2, Simple relation conditions, with the exception of the relational
        // operators 'IS NOT EQUAL TO' or 'IS NOT='". The MEMBERSHIP half is the grammar's: comparisonOperator IS
        // the printed general-relation operator set, so `NOT >=` and its spellings never reach this binder (kb/Work
        // PB333 screened them here; kb/Work PB1034 moved the screen to the one rule every condition shares). Only
        // the EXCLUSION half is START's own.
        if (op == "!=")
        {
            ctx.Edition.Error(DiagnosticCatalog.IoStatementOperandRule, $"START on '{name}': the relational operator shall not be "
                + "'IS NOT EQUAL TO' (ISO §14.9.41.3 SR3)");
            op = "==";
        }
        // WITH LENGTH (§14.9.41 GR13–GR14 partial-key count) is a COBOL-2002 introduction; the edition gate fires
        // on RECOGNITION in the VersionConformancePass parse arm (VisitStartWithLength). SR8 tests
        // the PHRASE against the ORGANIZATION and needs no operand, so it is screened before the operand resolves.
        BoundExpr? length = kp?.startWithLength()?.arithmeticExpression() is { } le ? host.Expr.BindExpr(le) : null;
        if (length is not null && file.Organization != FileOrganization.Indexed)
            ctx.Edition.Error(DiagnosticCatalog.IoStatementOperandRule, $"START … WITH LENGTH on '{name}': the LENGTH phrase requires "
                + "indexed organization (ISO §14.9.41.3 SR8)");
        Place? operand = null;
        if (kp?.dataReference() is { } dref && host.Expr.ResolveSending(dref) is var keyAnswer
            && (operand = keyAnswer.Place) is null)
            return keyAnswer.Refusal(ctx.Edition);   // the resolver's answer (kb/Work PB1030)

        // §14.9.41.3 SR4 — "Data-name-1 or record-key-name-1 shall not be subject to any OCCURS clauses."
        // ⛔ ITS OWN NAMED CHECK, AHEAD OF BOTH ORGANIZATION ARMS (kb/Work PB354 part 2/3). It used to have no
        // site at all: the indexed arm rejected an OCCURS operand only as a SIDE EFFECT of RecordLayout.OffsetOf
        // bailing out, under SR6's message — a sentence that is FALSE of an item which does begin at the key's
        // leftmost position — and the relative arm, which never calls OffsetOf, did not reject it at all.
        bool operandUnderOccurs = operand is not null && Model.RecordLayout.IsSubjectToOccurs(operand.Item);
        if (operandUnderOccurs)
            ctx.Edition.Error(DiagnosticCatalog.IoStatementOperandRule, $"START on '{name}': '{operand!.Item.CobolName}' is subject to an "
                + "OCCURS clause; data-name-1 shall not be (ISO §14.9.41.3 SR4)");

        // §14.9.41.3 SR2 — "If the organization of the file referenced by file-name-1 is sequential, either the
        // FIRST or the LAST phrase shall be specified." The general format makes FIRST / KEY / LAST mutually
        // exclusive (the bracket is plain — verified against the printed page), so on a sequential file every
        // remaining form — the bare START and the KEY phrase alike — violates SR2. ⛔ This is a REJECTION of an
        // ill-formed statement, never a refusal of the organization: the FIRST/LAST arm above accepts it.
        if (file.IsSequential)
        {
            return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.IoStatementOperandRule, $"START on sequential-organization file '{name}': either the "
                + "FIRST or the LAST phrase shall be specified (ISO §14.9.41.3 SR2)");
        }
        // The SR4 violation is reported; the key screens below cannot run on an operand with no single fixed
        // position, so the statement stops here rather than reporting a second, wrong reason.
        if (operandUnderOccurs) return BoundRejected.Reported(ctx.Edition);

        if (file.Organization == FileOrganization.Relative)
        {
            // ⛔ "SHALL BE THE DATA ITEM" IS AN IDENTITY OVER DATA ITEMS, SO IT ASKS DenotedItem (kb/Work PB602).
            // Asked of Place.Item — which answers about the ATTRIBUTES AND STORAGE a reference reads — a
            // reference-modified slice of the relative key reported the key itself and `START RLF KEY IS =
            // WS-RK(1:2)` compiled clean, positioning the file on the relative record number the key's first two
            // characters spell (§12.4.5.13.4 GR1 makes 1 the first legal one). §8.4.3.3.4 GR5 makes that slice "a
            // unique data item that is a subset of the data item referenced by identifier-1" — not it — and
            // §13.18.45.4 GR1 makes a non-THROUGH level-66 alias another data item over the same storage.
            if (operand is not null && !ReferenceEquals(operand.DenotedItem, file.RelativeKeyItem))
                ctx.Edition.Error(DiagnosticCatalog.IoStatementOperandRule, $"START on '{name}': data-name-1 shall be the RELATIVE KEY "
                    + $"item '{file.RelativeKeyItem?.CobolName ?? "(none)"}' (ISO §14.9.41.3 SR5)");
            operand ??= file.RelativeKeyItem is { } rk ? ctx.Refs.ResolveItem(rk) : null;
            // ⛔ GR8's SUBSTITUTION HAS NO OPERAND, SO THE STATEMENT HAS NO MEANING (kb/Work PB236, row
            // GR-14.9.41.4-8). §14.9.41.4 GR8: "If the KEY phrase is omitted, the START statement behaves as
            // though KEY IS EQUAL TO data-name-1 had been specified, where data-name-1 is the name of the key
            // specified in the RELATIVE KEY clause associated with file-name-1." A relative file in ACCESS
            // SEQUENTIAL may legally omit the RELATIVE KEY clause (§12.4.5.1 Format 2 BRACKETS it and §12.4.5.2
            // SR10 requires it only for DYNAMIC or RANDOM), so this program is syntactically
            // legal and semantically empty: there is no data-name-1 to substitute. §4.2.2 ¶4 leaves flagging a
            // general rule to the implementor ("An implementation may, but is not required to, flag violations
            // of such rules"), and the alternative to flagging is emitting code that cannot work — so this
            // rejects, in the SAME COBOLNET0862 channel this method already uses for SR1/SR3/SR5/SR6/SR8, and
            // no longer as a silent compile followed by a run-time abort.
            if (operand is null)
            {
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.IoStatementOperandRule, $"START on '{name}': the KEY phrase is omitted, so ISO "
                    + "§14.9.41.4 GR8 substitutes KEY IS EQUAL TO the RELATIVE KEY item — but this file control "
                    + "entry has no RELATIVE KEY clause, so there is no key to compare. Add a RELATIVE KEY "
                    + "clause to the SELECT, or write an explicit KEY phrase.");
            }
            return new BoundKeyedStart(file, KeyedStartMode.Key, op, -1, operand, null, invalid);
        }

        int keyIndex = -1;
        if (operand is not null)
        {
            // §14.9.41.3 SR6 — the operand is EITHER a) a prime or alternate record key of the file, OR b) a
            // generic key: leftmost-coincident within a record OF THE FILE (b1), of the same class, category and
            // usage as that key (b2), and no longer than it (b3). The two arms are separate rules with separate
            // homes in RecordLayout, so b2 could finally be written down (kb/Work PB354 part 4).
            // ⛔ BOTH ARMS ARE IDENTITIES OVER DATA ITEMS, so they ask DenotedItem, not Item (kb/Work PB602) —
            // a) names "a prime or alternate record key OF THE FILE", b) "a data item … within a record of the
            // file". A reference-modified operand denotes §8.4.3.3.4 GR5's "unique data item", which no record
            // description declares and which therefore satisfies neither arm; asked of Item it inherited the
            // base item's answer and `START IXF KEY IS = K(1:2)` compiled clean.
            if (operand.DenotedItem is not { } keyItem
                || (Model.RecordLayout.KeyIndexOfKeyItem(file, keyItem)
                    ?? Model.RecordLayout.GenericKeyIndex(file, keyItem)) is not { } ki)
            {
                string written = kp?.dataReference()?.GetText() ?? operand.Item.CobolName ?? "data-name-1";
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.IoStatementOperandRule, $"START on '{name}': '{written}' is neither a "
                    + "record key of the file nor an item that begins at the leftmost character position of one "
                    + "within a record of that file, has the same class, category and usage as that key, and is "
                    + "no longer than it (ISO §14.9.41.3 SR6)");
            }
            keyIndex = ki;
        }
        else
            operand = file.RecordKeyItem is { } pk ? ctx.Refs.ResolveItem(pk) : null;   // GR15 — prime key EQUAL
        if (operand is null)
            return new BoundUnsupported($"START on '{name}' with no resolvable RECORD KEY item");
        return new BoundKeyedStart(file, KeyedStartMode.Key, op, keyIndex, operand, length, invalid);
    }

    // ── Shared keyed-I/O helpers ───────────────────────────────────────────────────────────────────────────────

    /// <summary>Build the INVALID/NOT INVALID pair from the phrase's statement blocks — the shared two-branch
    /// shape via the ONE <see cref="PhraseBlocks.Split"/> extractor (P7 Step 10b), splitting an
    /// <c>INVALID KEY</c> / <c>NOT INVALID KEY</c> bracket into its two imperatives (§14.9.30.2 /
    /// §14.9.51.2 / §14.9.35.2 / §14.9.10.2 / §14.9.41.2 — a §5.2.6.4 choice-indicator group, so either
    /// order). Internal rather than private because the SEQUENTIAL arm binds the pair too: §14.9.30.2 Format 1
    /// has no INVALID KEY phrase and §14.9.51.3 SR2 forbids the phrase on a sequential-organization WRITE, but
    /// the report routes through <c>EditionContext.Removed</c>, which under <c>--permissive</c> leaves the bind
    /// standing — and §9.1.14 still governs what a standing bind MEANS, so both binders build the node here
    /// rather than each writing the split out (kb/Work PB334, PB691).</summary>
    internal KeyedInvalidKey KeyedInvalidPhrase(Core.StatementBlockContext[] blocks, bool notFirst)
    {
        var (inv, not) = PhraseBlocks.Split(blocks, notFirst, b => host.BindBlocks([b]));
        return new KeyedInvalidKey(inv, not);
    }

}
