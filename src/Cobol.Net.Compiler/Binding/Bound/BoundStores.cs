// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using CobolNet.Runtime;

using CobolNet.Binding.Model;

namespace CobolNet.Binding.Bound;

/// <summary>
/// The bound-tree STORE analysis: does a statement write through a Place rooted at a given
/// <see cref="DataItem"/>? First client: the object-property reference desugar (ISO §8.4.3.9.4 GR1–GR3,
/// deep-dive D-P2) — a reference is a RECEIVING occurrence exactly when its synthesized temp is stored by
/// the statement it desugared out of (GR2/GR3), so the SET accessor is invoked only then; a misclassified
/// "sending" would skip a required SET (silently lost store), a misclassified "receiving" would run a
/// side-effecting SET the spec says is not invoked — hence the walk is a TOTAL explicit taxonomy over every
/// <c>BoundStatement</c> (the 2026-07-04 15-agent survey, scratchpad <c>bound_stores_classification.md</c>:
/// 119 nodes, 46 store-bearing; every store field verified against the emitter). It is the exhaustive
/// generated <see cref="IBoundStatementVisitor{T}"/> (PHASE-07 Step 6c), so a new bound-statement leaf is a
/// COMPILE error here; the nine leaves outside the classified taxonomy return <see langword="null"/> so the
/// caller stages LOUD instead of guessing (§1.4 — never silent).
/// </summary>
/// <remarks>
/// Scope facts that keep this walk small and honest:
/// - Only PLACE stores can hit a property temp. Non-Place stores (generated index/ALTER fields, external
///   switches, FILE STATUS via <c>FileModel</c>, report-engine counters, run-unit exception state,
///   <c>BoundTableSort.Storage</c>) can never target the temp — a temp is a synthesized simple local
///   (and a property subject cannot carry OCCURS, COBOLNET0842), so those arms are <c>None</c> here.
/// - <see cref="Place.Item"/> is identity-compared: every Place wrapper (RefMod/alias/view) forwards
///   <c>Item</c> to the underlying item, so a windowed write through <c>RefModPlace</c> still classifies.
/// - The child-statement recursion (<c>Kids</c>) is DEFENSIVELY total over conditional-phrase bodies (SIZE
///   ERROR / AT END / INVALID KEY / ON EXCEPTION / ON OVERFLOW / AT EOP), IF/EVALUATE arms, inline-PERFORM
///   bodies, SEARCH arms, <c>BoundEcChecked.Inner</c> and <c>BoundSequence.Steps</c>. In practice a property
///   temp is statement-local: every nested statement is bound through its OWN <c>BindStatement</c> and so
///   drains its own property ops (<c>StatementBinder.BindStatement</c>), so the temp lives in the carrying
///   statement's DIRECT operands/condition, never a separately-wrapped nested body — which is why arms that
///   omit the recursion (e.g. <c>BoundKeyedDelete</c>) are equivalent, not buggy. Out-of-line PERFORM bodies
///   are pc ranges (not nested statements), so they are never walked.
/// </remarks>
/// <summary>How a statement touches a given item through its STORE positions (the polarity that selects the
/// §8.4.3.9.4 general rule): <see cref="None"/> → the occurrence is purely SENDING (GR1, get only);
/// <see cref="Write"/> → a WRITE-only receiving position (GR2, set only — the get method is NOT invoked);
/// <see cref="ReadWrite"/> → an in-place read-modify-write position (GR3, get before + set after).</summary>
public enum StoreKind { None, Write, ReadWrite }

public static class BoundStores
{
    /// <summary>The store polarity of <paramref name="item"/> in <paramref name="s"/> —
    /// <see cref="StoreKind.None"/> = provably never stored (a pure sending occurrence);
    /// <see cref="StoreKind.Write"/> / <see cref="StoreKind.ReadWrite"/> per the emitter-verified
    /// classification; <see langword="null"/> = a statement type outside the classified taxonomy (stage
    /// loud, never guess). A property temp occurs at exactly ONE Place in the tree, so the first store
    /// found is total.</summary>
    public static StoreKind? StoreKindOf(BoundStatement s, DataItem item) =>
        s.Accept(new StoreKindVisitor(x => ReferenceEquals(x, item), crossingsStore: true));

    /// <summary>Does <paramref name="s"/>, or any statement nested in it, STORE into an item
    /// <paramref name="matches"/> accepts? The second client (kb/Work PB363, ISO §14.9.49.3 SR11 "A USE BEFORE
    /// REPORTING procedure shall not alter the value of any control data item"): the same emitter-verified
    /// classification, asked of a SET of items instead of one.
    /// <para>A BY REFERENCE CALL argument and an INVOKE write-back are NOT stores here: whether the callee alters
    /// the argument is not decidable from this source element, and a syntax rule that fired on the possibility
    /// would reject a conforming program. The OO property desugar counts them (it must run the SET accessor after
    /// a crossing), which is why the choice is a parameter of the walk and not of the node.</para>
    /// <para>A statement outside the classified taxonomy (the <see langword="null"/> arms: pointer, object and
    /// carrier stores) answers false: none of them stores into a data item that has a PICTURE.</para></summary>
    public static bool StoresInto(BoundStatement s, Func<DataItem, bool> matches) =>
        s.Accept(new StoreKindVisitor(matches, crossingsStore: false)) is StoreKind.Write or StoreKind.ReadWrite;

    /// <summary>The per-node store classification (the former <c>StoreKindOf</c> switch, one arm per leaf) —
    /// the exhaustive <see cref="IBoundStatementVisitor{T}"/> over the bound statements, carrying the target
    /// <paramref name="item"/> so recursion is <c>child.Accept(this)</c>.</summary>
    private sealed class StoreKindVisitor(Func<DataItem, bool> matches, bool crossingsStore) : IBoundStatementVisitor<StoreKind?>
    {
        private bool Hit(Place? p) => p is not null && matches(p.Item);
        private bool TargetHit(BoundSetTarget? t) => t is SetPlaceTarget sp && Hit(sp.Place);
        /// <summary>The RECEIVER of a bound <c>… INTO</c> implicit move (kb/Work PB348 — identifier-1 now rides
        /// the move rather than the I-O node). Asked of the move's own target list, which is the same single
        /// receiver the node used to hold.</summary>
        private bool IntoHit(BoundMove? m) => m is not null && m.Targets.Any(Hit);
        private bool ReceiversHit(IReadOnlyList<Receiver> rs) => rs.Any(r => Hit(r.Place));

        // Aggregate child-statement lists: a found store dominates (the temp occurs exactly once);
        // otherwise an unknown poisons the result.
        private StoreKind? Kids(params IEnumerable<BoundStatement>?[] lists)
        {
            bool sawUnknown = false;
            foreach (var list in lists)
                foreach (var child in list ?? [])
                    switch (child.Accept(this))
                    {
                        case StoreKind.Write: return StoreKind.Write;
                        case StoreKind.ReadWrite: return StoreKind.ReadWrite;
                        case null: sawUnknown = true; break;
                    }
            return sawUnknown ? null : StoreKind.None;
        }

        private StoreKind? StoreOrKids(bool stored, StoreKind kind, params IEnumerable<BoundStatement>?[] lists)
            => stored ? kind : Kids(lists);

        private bool InitStores(IReadOnlyList<InitializeAction> actions)
        {
            foreach (var a in actions)
                switch (a)
                {
                    case InitializeStore st when matches(st.Target.Item): return true;
                    case InitializeSetNull sn when matches(sn.Target.Item): return true;
                    // §14.9.20.4 GR4/GR6b's implicit SET stores into its TARGET (identifier-2 is the sender).
                    case InitializeSetFrom sf when matches(sf.Target.Item): return true;
                    case InitializeLoop lp when InitStores(lp.Body): return true;
                    // A Format-2 (table) VALUE select stores into the receiver on EVERY arm it has (ISO
                    // §14.9.20.4 GR5c1c) — the arms differ only in which literal reaches it, so any one of them
                    // answering yes is a store into this item.
                    case InitializeOccurrenceSelect sel when InitStores(
                        [.. sel.Arms.Select(x => x.Do), .. sel.Otherwise is { } o ? new[] { o } : []]): return true;
                }
            return false;
        }

        // ── Pure control / read-only / model-routed statements (survey "pure" list) ─────────────────────
        public StoreKind? Visit(BoundUnsupported n) => StoreKind.None;
        public StoreKind? Visit(BoundRejected n) => StoreKind.None;
        public StoreKind? Visit(BoundStop n) => StoreKind.None;
        public StoreKind? Visit(BoundStopLiteral n) => StoreKind.None;
        public StoreKind? Visit(BoundDisplay n) => StoreKind.None;
        public StoreKind? Visit(BoundGoTo n) => StoreKind.None;
        public StoreKind? Visit(BoundGoToDepending n) => StoreKind.None;
        public StoreKind? Visit(BoundExitParagraph n) => StoreKind.None;
        public StoreKind? Visit(BoundExitSection n) => StoreKind.None;
        public StoreKind? Visit(BoundExitPerform n) => StoreKind.None;
        public StoreKind? Visit(BoundNop n) => StoreKind.None;
        public StoreKind? Visit(BoundCommitRollback n) => StoreKind.None;   // CONTINUE-equivalent (kb/Work PB137)
        public StoreKind? Visit(BoundContinueAfter n) => StoreKind.None;   // reads its interval expr; stores nothing
        public StoreKind? Visit(BoundNextSentence n) => StoreKind.None;
        public StoreKind? Visit(BoundOpen n) => StoreKind.None;
        public StoreKind? Visit(BoundClose n) => StoreKind.None;
        public StoreKind? Visit(BoundInitiate n) => StoreKind.None;
        public StoreKind? Visit(BoundGenerate n) => StoreKind.None;
        public StoreKind? Visit(BoundSuppress n) => StoreKind.None;
        public StoreKind? Visit(BoundTerminate n) => StoreKind.None;
        public StoreKind? Visit(BoundRaise n) => StoreKind.None;
        public StoreKind? Visit(BoundResume n) => StoreKind.None;
        public StoreKind? Visit(BoundSetLastException n) => StoreKind.None;
        public StoreKind? Visit(BoundGoToAlterable n) => StoreKind.None;
        public StoreKind? Visit(BoundCancel n) => StoreKind.None;
        public StoreKind? Visit(BoundExitProgram n) => StoreKind.None;
        public StoreKind? Visit(BoundGoback n) => StoreKind.None;
        public StoreKind? Visit(BoundMethodReturn n) => StoreKind.None;
        public StoreKind? Visit(BoundAlter n) => StoreKind.None;
        public StoreKind? Visit(BoundSetSwitches n) => StoreKind.None;
        public StoreKind? Visit(BoundKeyedDelete n) => StoreKind.None;   // no data receiver; its INVALID KEY body is separately wrapped
        public StoreKind? Visit(BoundSort n) => StoreKind.None;
        public StoreKind? Visit(BoundMerge n) => StoreKind.None;
        public StoreKind? Visit(BoundTableSort n) => StoreKind.None;
        public StoreKind? Visit(BoundUnlock n) => StoreKind.None;

        // ── Wrappers / containers ───────────────────────────────────────────────────────────────────────
        public StoreKind? Visit(BoundSequence n) => Kids(n.Steps);
        // A multi-operand verb's implicit statements (ISO §14.9.20.4 GR3 and siblings — BoundImplicitSeries):
        // a container exactly like BoundSequence for this query, since any member may carry the store.
        public StoreKind? Visit(BoundImplicitSeries n) => Kids(n.Members);
        public StoreKind? Visit(BoundEcChecked n) => n.Inner.Accept(this);
        public StoreKind? Visit(BoundActivationSite n) => n.Inner.Accept(this);
        public StoreKind? Visit(BoundIf n) => Kids(n.Then, n.Else);
        public StoreKind? Visit(BoundEvaluate n) => Kids([.. n.Whens.SelectMany(w => w.Statements)], n.Other);
        public StoreKind? Visit(BoundInlinePerform n) => StoreOrKids(
            n.Control is PerformVarying pv && pv.Levels.Any(l => TargetHit(l.Var)),
            StoreKind.ReadWrite, n.Body);   // induction var: init + augment (GR12/GR13)
        public StoreKind? Visit(BoundOutOfLinePerform n) =>
            n.Control is PerformVarying pv && pv.Levels.Any(l => TargetHit(l.Var))
                ? StoreKind.ReadWrite : StoreKind.None;
        public StoreKind? Visit(BoundExceptionPerform n) => Kids(n.Imp1, n.FinallyBody);   // imp-2/3/4 are
        // appended pc-range paragraphs (analyzed at their own nodes); only imp-1 + FINALLY are inline here.

        // ── Data movement / arithmetic (polarity per the survey: in-place vs WRITE-only) ────────────────
        public StoreKind? Visit(BoundMove n) => n.Targets.Any(Hit) ? StoreKind.Write : StoreKind.None;
        public StoreKind? Visit(BoundAddTo n) => StoreOrKids(ReceiversHit(n.Targets), StoreKind.ReadWrite, n.SizeError?.OnError, n.SizeError?.NotOnError);
        public StoreKind? Visit(BoundAddGiving n) => StoreOrKids(ReceiversHit(n.Targets), StoreKind.Write, n.SizeError?.OnError, n.SizeError?.NotOnError);
        public StoreKind? Visit(BoundSubtractFrom n) => StoreOrKids(ReceiversHit(n.Targets), StoreKind.ReadWrite, n.SizeError?.OnError, n.SizeError?.NotOnError);
        public StoreKind? Visit(BoundSubtractGiving n) => StoreOrKids(ReceiversHit(n.Targets), StoreKind.Write, n.SizeError?.OnError, n.SizeError?.NotOnError);
        public StoreKind? Visit(BoundMultiplyBy n) => StoreOrKids(ReceiversHit(n.Targets), StoreKind.ReadWrite, n.SizeError?.OnError, n.SizeError?.NotOnError);
        public StoreKind? Visit(BoundMultiplyGiving n) => StoreOrKids(ReceiversHit(n.Targets), StoreKind.Write, n.SizeError?.OnError, n.SizeError?.NotOnError);
        public StoreKind? Visit(BoundDivideInto n) => StoreOrKids(ReceiversHit(n.Targets), StoreKind.ReadWrite, n.SizeError?.OnError, n.SizeError?.NotOnError);
        public StoreKind? Visit(BoundDivideGiving n) => StoreOrKids(ReceiversHit(n.Targets), StoreKind.Write, n.SizeError?.OnError, n.SizeError?.NotOnError);
        public StoreKind? Visit(BoundDivideRemainder n) => StoreOrKids(Hit(n.Quotient.Place) || Hit(n.Remainder), StoreKind.Write, n.SizeError?.OnError, n.SizeError?.NotOnError);
        public StoreKind? Visit(BoundCompute n) => StoreOrKids(ReceiversHit(n.Targets), StoreKind.Write, n.SizeError?.OnError, n.SizeError?.NotOnError);
        public StoreKind? Visit(BoundComputeBoolean n) => n.Targets.Any(Hit) ? StoreKind.Write : StoreKind.None;   // §14.9.8 F2 — no size-error phrase
        public StoreKind? Visit(BoundPositionValue n) => Hit(n.Temp) ? StoreKind.Write : StoreKind.None;
        public StoreKind? Visit(BoundCorresponding n) => StoreOrKids(n.Pairs.Any(p => Hit(p.Target)),
            n.Verb == CorrVerb.Move ? StoreKind.Write : StoreKind.ReadWrite,
            n.SizeError?.OnError, n.SizeError?.NotOnError);
        public StoreKind? Visit(BoundInitialize n) => InitStores(n.Actions) ? StoreKind.Write : StoreKind.None;

        // ── SET family ──────────────────────────────────────────────────────────────────────────────────
        public StoreKind? Visit(BoundSetConditions n) => n.Sets.Any(x => Hit(x.Parent)) ? StoreKind.Write : StoreKind.None;
        public StoreKind? Visit(BoundSetTo n) => n.Targets.Any(TargetHit) ? StoreKind.Write : StoreKind.None;
        public StoreKind? Visit(BoundSetUpDown n) => n.Targets.Any(TargetHit) ? StoreKind.ReadWrite : StoreKind.None;
        public StoreKind? Visit(BoundSetSize n) => Hit(n.Target) ? StoreKind.ReadWrite : StoreKind.None;   // reads current content, writes resized
        // Format 15 writes each receiver whole and reads none of them (§14.9.39.4 GR32-GR36 — every value
        // is a property of the DESCRIPTION, never of the current content).
        public StoreKind? Visit(BoundSetContent n) => n.Stores.Any(st => Hit(st.Target)) ? StoreKind.Write : StoreKind.None;

        // ── SEARCH ──────────────────────────────────────────────────────────────────────────────────────
        public StoreKind? Visit(BoundSearch n) => StoreOrKids(TargetHit(n.AlsoVaried), StoreKind.ReadWrite,
            n.AtEnd, [.. n.Whens.SelectMany(w => w.Statements)]);

        // ── ACCEPT / STRING / UNSTRING / INSPECT ────────────────────────────────────────────────────────
        public StoreKind? Visit(BoundAccept n) => Hit(n.Target) ? StoreKind.Write : StoreKind.None;
        public StoreKind? Visit(BoundStringStmt n) => StoreOrKids(Hit(n.Into) || Hit(n.Pointer),
            StoreKind.ReadWrite,   // Into: GR7 read-modify-write; Pointer: GR4 read + GR8 writeback
            n.OnOverflow, n.NotOnOverflow);
        public StoreKind? Visit(BoundUnstringStmt n) =>
            Hit(n.Pointer) || Hit(n.Tallying)
                ? StoreKind.ReadWrite                                   // GR11a/GR13 + GR14 read-then-add
                : StoreOrKids(n.Receivers.Any(r => Hit(r.Target) || Hit(r.DelimiterIn) || Hit(r.CountIn)),
                    StoreKind.Write, n.OnOverflow, n.NotOnOverflow);    // GR11c/d/e pure stores
        // identifier-1 is an OPERAND since PB10. A function-identifier target has no Place, so it can never be a
        // STORE — which is not a special case here but the same rule stated twice: the guard below already
        // requires REPLACING/CONVERTING, and §8.4.3.2.3 SR1 bars a function-identifier from exactly those.
        public StoreKind? Visit(BoundInspect n) =>
            (n.Replacing.Count > 0 || n.Converting is not null) && Hit((n.Target as BoundFieldOperand)?.Place)
                ? StoreKind.ReadWrite                                   // image read, modified, stored
                : n.Tallying.Any(tl => Hit(tl.Counter))
                    ? StoreKind.ReadWrite                               // GR11 counter accumulate
                    : StoreKind.None;

        // ── File I/O (FILE STATUS / record-area stores route through FileModel — never the temp) ─────────
        // The conditional-phrase recursion is TOTAL over the node's phrase bodies, so the sequential WRITE's
        // §9.1.14 pair (bound under --permissive only — kb/Work PB691) is walked exactly as BoundKeyedWrite's is;
        // the two arms of one verb may not disagree about which bodies exist (feedback_two_arm_dispatch).
        public StoreKind? Visit(BoundWrite n) => StoreOrKids(n.FromMove is not null && Hit(n.Record),
            StoreKind.ReadWrite, n.AtEop, n.NotAtEop,                  // FROM-move then read as the image
            n.InvalidKey?.Invalid, n.InvalidKey?.NotInvalid);          // the --permissive pair (PB691)
        // The INVALID KEY lists are walked on BOTH READ arms. They are §14.9.30.2 Format-2 phrases and the
        // sequential arm reports COBOLNET1720 for them, but --permissive leaves the bind standing and
        // §14.9.30.4 GR13c then RUNS the NOT INVALID KEY imperative — a store inside it is a real store, and
        // omitting the lists here is the same two-arm disagreement kb/Work PB334 was about.
        public StoreKind? Visit(BoundRead n) => StoreOrKids(IntoHit(n.IntoMove), StoreKind.Write,
            n.AtEnd, n.NotAtEnd, n.InvalidKey?.Invalid, n.InvalidKey?.NotInvalid);
        public StoreKind? Visit(BoundRewrite n) => StoreOrKids(n.FromMove is not null && Hit(n.Record),
            StoreKind.ReadWrite, n.InvalidKey?.Invalid, n.InvalidKey?.NotInvalid);   // the --permissive pair (PB691)
        public StoreKind? Visit(BoundKeyedRead n) => StoreOrKids(IntoHit(n.IntoMove), StoreKind.Write,
            n.AtEnd, n.NotAtEnd, n.InvalidKey?.Invalid, n.InvalidKey?.NotInvalid);
        public StoreKind? Visit(BoundKeyedWrite n) => StoreOrKids(n.FromMove is not null && Hit(n.Record), StoreKind.ReadWrite,
            n.InvalidKey?.Invalid, n.InvalidKey?.NotInvalid);
        public StoreKind? Visit(BoundKeyedRewrite n) => StoreOrKids(n.FromMove is not null && Hit(n.Record), StoreKind.ReadWrite,
            n.InvalidKey?.Invalid, n.InvalidKey?.NotInvalid);
        public StoreKind? Visit(BoundKeyedDeleteFile n) => Kids(n.OnException, n.NotOnException);
        public StoreKind? Visit(BoundKeyedStart n) => Kids(n.InvalidKey?.Invalid, n.InvalidKey?.NotInvalid);
        public StoreKind? Visit(BoundRelease n) => n.FromMove is not null && Hit(n.Record) ? StoreKind.ReadWrite : StoreKind.None;
        public StoreKind? Visit(BoundReturn n) => StoreOrKids(Hit(n.RecordArea) || IntoHit(n.IntoMove) || Hit(n.Varying?.Depending),
            Hit(n.RecordArea) ? StoreKind.ReadWrite : StoreKind.Write,  // area: stored then INTO-source
            n.AtEnd, n.NotAtEnd);

        // ── CALL / INVOKE (BY REFERENCE crossings: copy-in + writeback = ReadWrite) ──────────────────────
        public StoreKind? Visit(BoundCallProgram n) =>
            crossingsStore && n.Args.Any(a => a.Mode == CobolPassMode.Reference && Hit(a.Place))
                ? StoreKind.ReadWrite
                : StoreOrKids(Hit(n.Returning), StoreKind.Write, n.OnException, n.NotOnException);
        public StoreKind? Visit(BoundInvoke n) =>
            crossingsStore && (n.Args?.Any(a => a.WriteBack && Hit(a.Source)) ?? false) ? StoreKind.ReadWrite
            : Hit(n.Returning) ? StoreKind.Write
            : StoreKind.None;

        // ── Outside the classified taxonomy — return null so the caller stages LOUD (never guess). These
        //    were the former `_ => null` catch-all; now explicit so a NEW leaf cannot silently join them. ─
        public StoreKind? Visit(BoundAllocate n) => null;
        public StoreKind? Visit(BoundFree n) => null;
        public StoreKind? Visit(BoundInvokeUniversal n) => null;
        public StoreKind? Visit(BoundRaiseObject n) => null;
        public StoreKind? Visit(BoundSetCapacity n) => null;
        public StoreKind? Visit(BoundSetObjectRef n) => null;
        public StoreKind? Visit(BoundSetPointer n) => null;
        public StoreKind? Visit(BoundSetLocale n) => StoreKind.None;
        public StoreKind? Visit(BoundSaveLocale n) => null;
        public StoreKind? Visit(BoundSetProgramPointer n) => null;   // a carrier copy, never a PICTURE store (P10 Step 7)
        public StoreKind? Visit(BoundSetFunctionPointer n) => null;   // the Format-8 carrier copy (kb/Work PB452)
        public StoreKind? Visit(BoundSetFunctionAddress n) => null;   // §8.4.3.12 resolve + carrier store, never a PICTURE store
        public StoreKind? Visit(BoundSetEntry n) => null;            // a carrier assignment (§8.4.3.13)
        public StoreKind? Visit(BoundSetPointerUpDown n) => null;
    }
}
