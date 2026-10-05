// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using System.Collections.Generic;
using System.Linq;
using CobolNet.Binding.Bound;

using CobolNet.Binding.Model;

using CobolNet.Compiler.Oo;

namespace CobolNet.Binding.Passes;

/// <summary>
/// PHASE-05 Step 5 (DESIGN-data-model §2.5 step 9): the single owner of the "which groups are referenced as a WHOLE
/// character-image operand" fact — the input to the numeric-DISPLAY-leaf image promotion (§14.9.25.4 MOVE GR4). It is
/// collected here, AFTER binding, by an explicit walk of the bound tree, replacing <see cref="ReferenceResolver"/>'s
/// mid-resolve mutation of <see cref="DataBinder.WholeGroupReferenced"/>.
/// <para><b>Correctness (owner-directed redesign, 2026-07-10):</b> the bound tree is the CORRECT oracle — a group has
/// a <see cref="Place"/> in the tree EXACTLY where it is used as a whole operand (MOVE/DISPLAY/compare/ACCEPT/record
/// I-O/whole-group CORRESPONDING pair/boundary formal). The legacy resolver over-collected: it added ANY resolved
/// group, including CORRESPONDING OPERANDS and SEARCH TABLES that are decomposed to member/element access and are
/// never whole-image operands (verified output-neutral). So this pass deliberately produces a SMALLER, correct set;
/// the divergence from legacy is legacy-wrong / this-right, validated by OUTPUT (greenfield DifferentialGolden +
/// conformance byte-exact runtime), not by legacy set-equality.</para>
/// <para><b>Traversal (PHASE-07 Step 6h):</b> the per-statement whole-operand extraction is the generated exhaustive
/// <see cref="IBoundStatementVisitor{T}"/> — a new statement leaf is a COMPILE error here, never a silent
/// <c>default: break</c> — and the nested-statement RECURSION rides the generated
/// <see cref="BoundStatementTree.StatementChildren"/> (the ONE drift-proof container source), so the former hand-listed
/// recursion "cross-checked against VersionConformancePass" by prose is gone. Both changes are output-neutral: the old
/// recursion set already equalled <c>StatementChildren</c>'s containers.</para>
/// <para>A group can only surface as a whole operand in a <see cref="Place"/> field, a <see cref="BoundFieldOperand"/>,
/// a relational/88/class <see cref="BoundCondition"/>, a whole-group <see cref="CorrespondingPair"/>, a
/// <see cref="SetPlaceTarget"/>, or an INITIALIZE store — NEVER in a numeric <see cref="BoundExpr"/> or a
/// <see cref="BoundBoolExpr"/> (those carry only numeric/boolean leaves). FILE record areas and boundary-copied
/// formals are whole-image-filled by I-O / a CALL crossing WITHOUT surfacing as a procedure Place, so they are added
/// structurally.</para>
/// </summary>
internal static class UsageCollectionPass
{
    /// <summary>The GROUP pass body (P6 Step 3 — <c>BindPipeline.GroupTail</c>, Requires <c>ProcedureBound</c>,
    /// Produces <c>UsageCollected</c>): collect the whole-group set for every class forest (OBJECT + FACTORY halves,
    /// with the method USING/RETURNING formals) and every program unit, in the fused pipeline's order.</summary>
    public static void Run(GroupBindContext ctx)
    {
        foreach (var cls in ctx.Classes)
        {
            Collect(cls.Data, [cls.Bound], OoFormalGroups(cls.Symbol.Methods));
            Collect(cls.FactoryData, [cls.FactoryBound], OoFormalGroups(cls.Symbol.FactoryMethods));
            // ⛔ An ELEMENTARY method formal is a character channel's far end — the method twin of Collect's
            // LinkageFormals arm (kb/Work PB1064; PB992 built the program arm). Every method formal is BY REFERENCE
            // (the METHOD-side BY VALUE phrase is refused, DataBinder.Oo), so §14.2.3 GR8's "as if the formal parameter
            // occupies the same storage area as the argument" holds for each: the characters an argument carries into
            // it, or a group MOVE leaves in it for the argument, may be ones no numeric VALUE represents.
            AddFormalChannels(cls.Data, cls.Symbol.Methods);
            AddFormalChannels(cls.FactoryData, cls.Symbol.FactoryMethods);
        }
        foreach (var unit in ctx.Units) Collect(unit.Data, [unit.Bound]);

        static IEnumerable<DataItem> OoFormalGroups(IEnumerable<OoMethodSymbol> methods) =>
            methods.SelectMany(m => m.Binding!.Formals.Select(f => f.Item).Concat(m.Binding!.Returning is { } r ? [r] : Array.Empty<DataItem>()));

        static void AddFormalChannels(DataBinder data, IEnumerable<OoMethodSymbol> methods)
        {
            // A PROPERTY-clause accessor (§13.18.42) has no LINKAGE formal of its own — its "formal" IS the object's
            // property subject, a direct field body (OoEmitter.EmitMethod's PropertySubject arm), never a boundary.
            foreach (var m in methods.Where(m => m.PropertySubject is null))
                foreach (var f in m.Binding!.Formals)
                    if (!f.Item.IsGroup) data.CharacterChannelItems.Add(f.Item);
        }
    }

    /// <summary>Fill <paramref name="data"/>'s <see cref="DataBinder.WholeGroupReferenced"/> from the whole-group
    /// operands of the bound <paramref name="programs"/> + the boundary-copied program/OO formals
    /// (<paramref name="extraFormalGroups"/> is the OO method USING/RETURNING group items). FILE record areas are
    /// added earlier, at bind time, by <c>DataBinder.MarkFileRecordImageLeaves</c>. Runs once per binder, after
    /// procedure binding, before <c>StorageFormPass</c> reads the set.</summary>
    public static void Collect(DataBinder data, IEnumerable<BoundProgram?> programs,
        IEnumerable<DataItem>? extraFormalGroups = null)
    {
        var set = data.WholeGroupReferenced;
        var channels = data.CharacterChannelItems;

        // Boundary-copied program formals — a group formal crosses as its character image (§14.2.3 GR8).
        foreach (var lf in data.LinkageFormals)
            if (!lf.CarrierResident && lf.Item.IsGroup) set.Add(lf.Item);

        // ⛔ An ELEMENTARY BY REFERENCE formal is a character channel's far end (kb/Work PB992): ISO §14.2.3 GR8 —
        // "the activated runtime element operates as if the formal parameter occupies the same storage area as
        // the argument" — and a Format-1 activator's argument need only be "of the same length" (§14.8.2.3.2
        // rule 1), so the characters the formal holds may be ones no numeric VALUE represents.
        foreach (var lf in data.LinkageFormals)
            if (!lf.ByValue && !lf.Item.IsGroup) channels.Add(lf.Item);

        // The program/function PROCEDURE DIVISION RETURNING group item crosses the activation boundary as its image.
        if (data.LinkageReturning is { IsGroup: true } ret) set.Add(ret);

        // OO method USING/RETURNING group items (supplied by the caller — not on data as LinkageFormals).
        if (extraFormalGroups is not null)
            foreach (var g in extraFormalGroups)
                if (g.IsGroup) set.Add(g);

        var v = new Visitor(set, channels);
        foreach (var prog in programs)
            if (prog is not null)
                foreach (var para in prog.Paragraphs)
                    foreach (var s in para.Statements)
                        v.Walk(s);
    }

    /// <summary>The bound-tree walk. <see cref="Walk"/> collects a statement's OWN whole-group operand positions
    /// through the exhaustive statement visitor, then recurses every nested statement via
    /// <see cref="BoundStatementTree.StatementChildren"/>. The per-leaf <c>Visit</c> collects ONLY the node's direct
    /// operands/conditions/init-actions — nested statements are the recursion's job, so containers whose only
    /// group-bearing content is nested statements have an empty <c>Visit</c>.</summary>
    private sealed class Visitor(HashSet<DataItem> set, HashSet<DataItem> channels) : IBoundStatementVisitor<bool>
    {
        public void Walk(BoundStatement s)
        {
            s.Accept(this);
            foreach (var child in s.StatementChildren())
                Walk(child);
        }

        // ── whole-group operand positions ──
        public bool Visit(BoundMove n)
        {
            Op(n.Source);
            foreach (var t in n.Targets) P(t);
            // ISO §14.9.25.4 GR4: a group sender's move into an ELEMENTARY receiver "is treated exactly as if it
            // were an alphanumeric to alphanumeric elementary move, except that there is no conversion" — the
            // receiver takes the sending CHARACTERS (kb/Work PB992). The node's own kind decides, per receiver.
            for (int i = 0; i < n.Targets.Count; i++)
                if (n.Stores[i].Kind is MoveKind.GroupToElementary) Channel(n.Targets[i]);
            return false;
        }
        public bool Visit(BoundExceptionPerform n) => false;   // no direct data operands; imp-1..5 walked via StatementChildren
        public bool Visit(BoundDisplay n) { foreach (var o in n.Operands) Op(o); return false; }
        public bool Visit(BoundAccept n) { P(n.Target); return false; }
        public bool Visit(BoundInspect n)
        {
            // identifier-1 is an OPERAND since PB10 (it may be a function-identifier in Format 1). Op() delegates
            // a field operand to P() unchanged and ignores a computed one, which is right: this pass collects
            // whole-GROUP references for the image mechanism, and a function's returned value is a temporary,
            // never a declared group.
            Op(n.Target);
            // ⛔ REPLACING / CONVERTING WRITE CHARACTERS INTO identifier-1 (§14.9.22.4 GR7/GR20 — the replaced image is
            // what the item then holds) AND GR4 d) RETAINS THE ORIGINAL SIGN OF A SIGNED NUMERIC ONE "upon completion
            // of the INSPECT statement" — over a zero magnitude too, which is a NEGATIVE ZERO (`00}`), a sign no native
            // value carrier holds. INSPECT is therefore a character channel of the elementary identifier-1 (kb/Work
            // PB1128): a numeric DISPLAY / NATIONAL one stores its character image, the one representation that
            // holds the sign and any non-digit the replacement deposits. TALLYING only reads it and promotes nothing.
            if ((n.Replacing.Count > 0 || n.Converting is not null) && n.Target is BoundFieldOperand target)
                Channel(target.Place);
            foreach (var t in n.Tallying) { P(t.Counter); Op(t.Pattern); Op(t.Before); Op(t.After); }
            foreach (var r in n.Replacing) { Op(r.Pattern); Op(r.Replacement); Op(r.Before); Op(r.After); }
            if (n.Converting is { } c) { Op(c.From); Op(c.To); Op(c.Before); Op(c.After); }
            return false;
        }
        public bool Visit(BoundStringStmt n)
        {
            P(n.Into); P(n.Pointer);
            foreach (var snd in n.Sendings) { Op(snd.Value); Op(snd.Delimiter); }
            return false;
        }
        public bool Visit(BoundUnstringStmt n)
        {
            Op(n.Source); P(n.Pointer); P(n.Tallying);   // DA4: Source is an operand
            foreach (var r in n.Receivers) { P(r.Target); P(r.DelimiterIn); P(r.CountIn); }
            return false;
        }
        public bool Visit(BoundWrite n) { P(n.Record); return false; }   // FROM rides the child BoundMove (PB348)
        public bool Visit(BoundRewrite n) { P(n.Record); return false; }
        public bool Visit(BoundRead n) => false;   // INTO rides the child BoundMove, walked via StatementChildren
        public bool Visit(BoundKeyedRead n) => false;
        public bool Visit(BoundKeyedWrite n) { P(n.Record); return false; }
        public bool Visit(BoundKeyedRewrite n) { P(n.Record); return false; }
        public bool Visit(BoundKeyedDelete n) => false;
        public bool Visit(BoundKeyedStart n) { P(n.Operand); return false; }
        public bool Visit(BoundKeyedDeleteFile n) => false;
        public bool Visit(BoundReturn n) { P(n.RecordArea); return false; }
        public bool Visit(BoundRelease n) { P(n.Record); return false; }   // RELEASE record FROM x ≡ MOVE x TO record (§14.9.32 GR4) — the MOVE is a CHILD node now (PB348), so Visit(BoundMove) collects both sides
        public bool Visit(BoundInitialize n) { foreach (var a in n.Actions) InitAct(a); return false; }
        public bool Visit(BoundCorresponding n)
        {
            // ONLY the whole-moved group-side pairs are whole-group operands; the two statement OPERANDS are
            // decomposed, never whole-image (correctly omitted — BoundCorresponding retains no Place to them).
            foreach (var pr in n.Pairs) { P(pr.Source); P(pr.Target); }
            return false;
        }
        public bool Visit(BoundSetConditions n) { foreach (var (p, _, _) in n.Sets) P(p); return false; }
        public bool Visit(BoundCallProgram n)
        {
            Op(n.DynamicName);
            foreach (var a in n.Args)
            {
                P(a.Place); Op(a.Value);
                // §14.2.3 GR8's shared storage — the formal's characters ARE the argument's (kb/Work PB992).
                if (a.Mode is CobolNet.Runtime.CobolPassMode.Reference && !a.Omitted) Channel(a.Place);
            }
            P(n.Returning);
            // §14.6.5: the result "is the content of the data item" the callee's RETURNING phrase references, and
            // §14.9.4.4 GR4 places it into identifier-3 — a CONTENT transfer, whatever that content is. A
            // user-defined function's result temporary is exempt: it mirrors its model's storage form already
            // (the compiler-temp re-sync in StorageFormPass), which is the same answer reached structurally.
            if (!n.IsFunction) Channel(n.Returning);
            return false;
        }
        public bool Visit(BoundCancel n) { foreach (var (_, dn) in n.Targets) Op(dn); return false; }
        // ⛔ THE INVOKE TWINS OF BoundCallProgram's CHANNELS (kb/Work PB1064 — the PB992 mechanism's missing arm): a
        // method is an activated runtime element like a program, so §14.2.3 GR8's shared storage makes a BY REFERENCE
        // argument's characters the formal's, and §14.9.23.4 GR8 places the RETURNING content into identifier-4.
        public bool Visit(BoundInvoke n)
        {
            P(n.Receiver); P(n.Returning);
            if (n.Args is { } args)
                foreach (var a in args)
                {
                    P(a.Source);
                    if (a.WriteBack) Channel(a.Source);   // BY REFERENCE identifier (BY CONTENT copies — no channel)
                }
            Channel(n.Returning);
            return false;
        }
        public bool Visit(BoundInvokeUniversal n)
        {
            P(n.Receiver); P(n.MethodSource); P(n.Returning);
            // §14.9.23.3 SR6: every argument through a universal receiver is BY REFERENCE.
            foreach (var a in n.Args) { P(a.Source); Channel(a.Source); }
            Channel(n.Returning);
            return false;
        }
        public bool Visit(BoundGoToDepending n) { Op(n.Selector); return false; }
        public bool Visit(BoundGoback n) { P(n.ReturningSource); return false; }   // GOBACK RETURNING group ≡ a whole-group MOVE source
        public bool Visit(BoundSetTo n) { foreach (var t in n.Targets) SetT(t); return false; }
        public bool Visit(BoundSetUpDown n) { foreach (var t in n.Targets) SetT(t); return false; }

        // ── conditions + loop control (the nested STATEMENTS are recursed by StatementChildren) ──
        public bool Visit(BoundIf n) { Cond(n.Condition); return false; }
        public bool Visit(BoundEvaluate n) { foreach (var w in n.Whens) Cond(w.Match); return false; }
        public bool Visit(BoundInlinePerform n) { Perf(n.Control); return false; }
        public bool Visit(BoundOutOfLinePerform n) { Perf(n.Control); return false; }
        public bool Visit(BoundSearch n)
        {
            // The searched table is scanned by INDEX (element access), never a whole image — correctly NOT collected.
            if (n.AlsoVaried is { } av) SetT(av);
            foreach (var w in n.Whens) Cond(w.Condition);
            return false;
        }

        // ── leaves with no whole-group operand and no non-statement group-bearing part (nested statements, if any,
        //    are recursed by StatementChildren). Explicit so a NEW leaf is a compile error, never a silent miss. ──
        public bool Visit(BoundSequence n) => false;
        public bool Visit(BoundImplicitSeries n) => false;   // members walked via StatementChildren
        public bool Visit(BoundEcChecked n) => false;
        public bool Visit(BoundActivationSite n) => false;
        public bool Visit(BoundAddTo n) => false;
        public bool Visit(BoundAddGiving n) => false;
        public bool Visit(BoundSubtractFrom n) => false;
        public bool Visit(BoundSubtractGiving n) => false;
        public bool Visit(BoundMultiplyBy n) => false;
        public bool Visit(BoundMultiplyGiving n) => false;
        public bool Visit(BoundDivideInto n) => false;
        public bool Visit(BoundDivideGiving n) => false;
        public bool Visit(BoundDivideRemainder n) => false;
        public bool Visit(BoundCompute n) => false;
        public bool Visit(BoundComputeBoolean n) => false;
        public bool Visit(BoundPositionValue n) => false;
        public bool Visit(BoundUnsupported n) => false;
        public bool Visit(BoundRejected n) => false;
        public bool Visit(BoundStop n) => false;
        public bool Visit(BoundStopLiteral n) => false;
        public bool Visit(BoundGoTo n) => false;
        public bool Visit(BoundGoToAlterable n) => false;
        public bool Visit(BoundExitParagraph n) => false;
        public bool Visit(BoundExitSection n) => false;
        public bool Visit(BoundExitPerform n) => false;
        public bool Visit(BoundExitProgram n) => false;
        public bool Visit(BoundNop n) => false;
        public bool Visit(BoundCommitRollback n) => false;   // CONTINUE-equivalent (kb/Work PB137)
        public bool Visit(BoundContinueAfter n) => false;
        public bool Visit(BoundNextSentence n) => false;
        public bool Visit(BoundOpen n) => false;
        public bool Visit(BoundClose n) => false;
        public bool Visit(BoundUnlock n) => false;
        public bool Visit(BoundInitiate n) => false;
        public bool Visit(BoundGenerate n) => false;
        public bool Visit(BoundSuppress n) => false;
        public bool Visit(BoundTerminate n) => false;
        public bool Visit(BoundAlter n) => false;
        public bool Visit(BoundSetSwitches n) => false;
        public bool Visit(BoundSort n) => false;
        public bool Visit(BoundMerge n) => false;
        public bool Visit(BoundTableSort n) => false;
        public bool Visit(BoundMethodReturn n) => false;
        public bool Visit(BoundRaise n) => false;
        public bool Visit(BoundRaiseObject n) => false;
        public bool Visit(BoundResume n) => false;
        public bool Visit(BoundSetLastException n) => false;
        public bool Visit(BoundSetLocale n) => false;
        public bool Visit(BoundSaveLocale n) => false;
        public bool Visit(BoundSetObjectRef n) => false;
        public bool Visit(BoundObjectView n) => false;
        public bool Visit(BoundSelfReference n) => false;
        public bool Visit(BoundSetPointer n) => false;
        public bool Visit(BoundSetProgramPointer n) => false;   // program-pointer carriers never join a character image (P10 Step 7)
        public bool Visit(BoundSetFunctionPointer n) => false;   // function-pointer carriers likewise (kb/Work PB452)
        public bool Visit(BoundSetFunctionAddress n) => false;
        public bool Visit(BoundSetEntry n) => false;
        public bool Visit(BoundSetPointerUpDown n) => false;
        public bool Visit(BoundSetCapacity n) => false;
        public bool Visit(BoundSetSize n) => false;
        public bool Visit(BoundSetContent n) => false;
        public bool Visit(BoundAllocate n) => false;
        public bool Visit(BoundFree n) => false;

        private void InitAct(InitializeAction a)
        {
            switch (a)
            {
                case InitializeStore x: P(x.Target); Op(x.Source); break;
                case InitializeSetNull x: P(x.Target); break;
                // §14.9.20.4 GR4/GR6b's implicit SET — BOTH operands are data references (the receiver and
                // identifier-2), so both are recorded; only the receiver was, before the arm existed.
                case InitializeSetFrom x: P(x.Target); P(x.Source); break;
                // The loop's AllCount bound is deliberately NOT visited: this pass records WHOLE-GROUP image
                // references, and an occurrence count is always an ELEMENTARY read (an OCCURS DEPENDING integer,
                // a dynamic table's capacity register), which P() ignores by construction.
                case InitializeLoop x: foreach (var b in x.Body) InitAct(b); break;
                // Every arm of a Format-2 (table) VALUE select is a store into the SAME receiver at a different
                // occurrence (ISO §14.9.20.4 GR5c1c), so all of them are visited — the occurrence tests read only
                // the enclosing loop variables, which are not data references at all.
                case InitializeOccurrenceSelect x:
                    foreach (var arm in x.Arms) InitAct(arm.Do);
                    if (x.Otherwise is { } o) InitAct(o);
                    break;
                default: break;
            }
        }

        private void Cond(BoundCondition? c)
        {
            switch (c)
            {
                case BoundRelational x: Op(x.Left); Op(x.Right); break;
                case BoundLogical x: foreach (var o in x.Operands) Cond(o); break;
                case BoundNot x: Cond(x.Operand); break;
                case BoundCondition88 x: P(x.Parent); break;
                case BoundClassCondition x: Op(x.Operand); break;
                case BoundUserClassCondition x: Op(x.Operand); break;
                case BoundCodedSetClassCondition x: Op(x.Operand); break;   // §8.8.4.4.4 GR3 a (kb/Work PB109)
                default: break;   // sign/boolean/switch condition — numeric/boolean operand, no group
            }
        }

        private void Perf(BoundPerformControl ctl)
        {
            switch (ctl)
            {
                case PerformTimes x: Op(x.Count); break;
                case PerformUntil x: Cond(x.Until); break;
                case PerformVarying x: foreach (var lvl in x.Levels) { SetT(lvl.Var); Cond(lvl.Until); } break;
                default: break;
            }
        }

        private void Op(BoundOperand? o)
        {
            if (o is BoundFieldOperand f) P(f.Place);
        }

        private void SetT(BoundSetTarget t)
        {
            if (t is SetPlaceTarget p) P(p.Place);
        }

        /// <summary>Record the item a character channel writes through <paramref name="place"/> (kb/Work PB992).
        /// A Tier-B REDEFINES window already IS a character image, and an OCCURS DYNAMIC element stores through
        /// its table codec; every other place — a reference-modified view included, whose store splices into
        /// the item's own character positions (§8.4.3.3.4 GR6) — names the item whose storage it reaches.</summary>
        private void Channel(Place? place)
        {
            if (place is null or RedefViewPlace or DynTablePlace || place.Item.IsGroup) return;
            channels.Add(place.Item);
        }

        private void P(Place? place)
        {
            // An OCCURS DYNAMIC element access (data-model D9) stores the group as a typed element of a
            // CobolDynTable<T> — a whole-group MOVE into it round-trips through the TABLE codec, never the item-level
            // character-image mechanism. So the element group is NOT whole-image-referenced.
            if (place is DynTablePlace) return;
            if (place?.Item is { IsGroup: true } g) set.Add(g);
        }
    }
}
