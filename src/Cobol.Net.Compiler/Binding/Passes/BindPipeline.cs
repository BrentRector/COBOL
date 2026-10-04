// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using System.Collections.Generic;
using System.Linq;
using CobolNet.Frontend.Generated;

using CobolNet.Binding.Model;

namespace CobolNet.Binding.Passes;

using Core = CobolParserCore;

/// <summary>
/// The DECLARED, asserted order of the bind pipeline (rearchitecture PHASE 05 Step 3 / PHASE 06 Step 3). Replaces
/// the two formerly-hidden pipelines — <c>DataBinder.BindResolve</c>'s comment-ordered method calls and the
/// middle-end sequence the fused emitter ran — with explicit manifests whose phase contract
/// (<see cref="IPassInfo.Requires"/>/<see cref="IPassInfo.Produces"/>) is verified at startup by
/// <see cref="ValidateDag"/> over the FULL chain (<see cref="ValidateFullChainOnce"/>).
/// <para><b>Two sections, one chain:</b> <see cref="Build"/> is the per-UNIT resolve prefix (run by
/// <c>DataBinder.BindResolve</c> against each unit/class forest, up to <see cref="PassPhase.FilesResolved"/>);
/// <see cref="GroupTail"/> is the whole-GROUP middle-end (run by <c>BinderDriver.Bind</c> once per compilation —
/// procedure binding, usage collection, the storage-form computation; the passes that need every unit's BOUND
/// tree). The concatenation is validated as ONE monotone chain, so a group pass reading a resolve-produced fact
/// too early is a startup error exactly like a mis-ordered resolve pass.</para>
/// </summary>
internal static class BindPipeline
{
    /// <summary>Build the ordered pass list for one program unit's resolution. The two declaration-context binders
    /// close over <paramref name="program"/>; every other pass runs against the binder instance passed to
    /// <see cref="IBindPass.Run"/>. Built per <c>BindResolve</c> call because the two program-context passes need
    /// the parse tree — a trivial allocation; the SSOT for the pass order lives HERE.</summary>
    public static IReadOnlyList<IBindPass> Build(Core.ProgramUnitContext program) => new IBindPass[]
    {
        // ── The per-unit resolution passes, in the EXACT pre-P5 BindResolve order (DataBinder.cs). ──
        // The §13.16.3 SR9 VALUE-implied PICTURE (kb/Work PB504/PB831/PB1300; DataBinder.ImpliedPicture.cs).
        // FIRST, and the placement is the design. SR9 implies the clause for the entry the programmer WROTE, and
        // §13.18.57.4 GR3 then says "when the description of type-name-1 includes an implicit PICTURE clause
        // derived from a VALUE clause, that implicit PICTURE clause becomes part of the description of the
        // subject of the entry" — so the template must OWN its implied PICTURE before ExpandTypes copies its
        // description, exactly as it owns a written one. Run after the copy (the former order), the template had
        // no PICTURE to hand over and the subject's OWN VALUE literal implied one instead: `01 U TYPEDEF VALUE
        // "ABCD". 01 W TYPE U VALUE "Q".` bound W as PIC X(1) where GR3 makes it PIC X(4) (kb/Work PB1300). An
        // entry whose description is still a pending TYPE / SAME AS reference is not picture-less in SR9's
        // sense (IsPictureLessLeaf) — its PICTURE is the referenced description's — so it waits for the copy.
        // BEFORE UsageInheritancePass as well, so that from this point on the entry is INDISTINGUISHABLE from
        // one whose source wrote `PICTURE X(n)` — §13.18.60.4 GR1 inheritance, the §13.18.60.3 SR3/SR5/SR12/SR20
        // screens, SIGN inheritance and every later pass apply to it through their existing single sites.
        new BindPass("SynthesizeImpliedPictures", PassPhase.None, PassPhase.None, d => d.SynthesizeImpliedPictures()),
        new BindPass("ExpandTypes", PassPhase.None, PassPhase.TypesExpanded, d => d.ExpandTypes()),
        // What a CONSTANT RECORD's SUBTREE may not contain — §13.16.3 SR13, §13.18.40.3 SR32, §13.18.38.3 SR19/SR23/SR33
        // (kb/Work PB1262). Rules about an item's POSITION, so they are asked of the COMPOSED forest: immediately after
        // ExpandTypes, which supplies the TYPE / SAME AS clones, and before anything clears a flag the rule reads.
        new BindPass("CheckConstantRecordSubtrees", PassPhase.TypesExpanded, PassPhase.TypesExpanded, d => d.CheckConstantRecordSubtrees()),
        new BindPass("UsageInheritancePass", PassPhase.TypesExpanded, PassPhase.UsageResolved, d => d.UsageInheritancePass()),
        // The §13.18.60.3 USAGE declaration-PLACEMENT screen — SR14/SR15/SR4 (kb/Work PB183). Placed HERE, and
        // not one pass earlier or later, for two reasons. It needs UsageInheritancePass to have settled
        // §13.18.60.4 GR1 group-usage inheritance, so an INHERITED pointer usage is screened exactly as a
        // written one. And it must precede ClassifyRedefinesClasses, whose Tier-D backstop rejects a nested
        // pointer/object leaf under REDEFINES with a message that names THIS rule as the missing screen: the
        // declaration's own defect should be reported before the downstream machinery's defense against it.
        // A plain syntax rule, so NOT the terminal VersionConformancePass — that pass is the edition gate.
        new BindPass("CheckUsageDeclarations", PassPhase.UsageResolved, PassPhase.UsageResolved, d => d.CheckUsageDeclarations()),
        // The §13.18.1.3 SR1 ALIGNED subject screen (kb/Work PB487). Placed HERE for the SAME reason as the pass
        // above: SR1 asks whether the entry is "a bit group item or an elementary bit data item", and a group
        // becomes a bit group by §13.16.4 GR1 INHERITANCE, which UsageInheritancePass settles. Nothing later
        // than that — it reads only declared shape, and BitLayout must not lay out an item whose ALIGNED clause
        // was never adjudicated.
        new BindPass("CheckAlignedClauses", PassPhase.UsageResolved, PassPhase.UsageResolved, d => d.CheckAlignedClauses()),
        // The §13.16.3 SR24 condition-name ASSOCIATION screen — the entry a level-88 entry may be associated
        // with, and whether it follows one at all (kb/Work PB488). Placed HERE for the same reason as the two
        // passes above: exclusion c) asks whether a group contains "items with a usage other than display",
        // which §13.18.60.4 GR1 inheritance can supply, so UsageInheritancePass must have run; nothing later
        // is needed, because every other exclusion reads declared shape the entry walk already settled. It
        // cannot live inside BindCondition at all — four exclusions ask about SUBORDINATE entries, which a
        // condition-name on a group is written BEFORE.
        new BindPass("CheckConditionNameAssociations", PassPhase.UsageResolved, PassPhase.UsageResolved, d => d.CheckConditionNameAssociations()),
        // The §13.16.3 SR8 closing guard — every elementary item has a PICTURE or a synthesized profile
        // (kb/Work PB487). Placed immediately after UsageInheritancePass because that is the LAST pass which
        // can legitimately fill a null Pic (a group header shedding its usage to leaves, §13.18.60.4 GR1; a
        // PICTURE-less INDEX / OBJECT REFERENCE leaf taking the inherited profile), and BEFORE everything that
        // reads a Pic. Without it `01 M.` — plain COBOL, no exotic clause — reached MoveEmitter with a null Pic
        // and crashed the compiler with an unhandled NullReferenceException and no diagnostic at all.
        new BindPass("CheckPictureRequired", PassPhase.UsageResolved, PassPhase.UsageResolved, d => d.CheckPictureRequired()),
        // The BLANK WHEN ZERO / JUSTIFIED SUBJECT screen (§13.18.8.3 SR1/SR2, §13.18.32.3 SR3; kb/Work PB507).
        // After CheckPictureRequired, whose ElementaryOnly rows have already cleared both clauses from every GROUP,
        // and after UsageInheritancePass because SR2's usage may be INHERITED (§13.18.60.4 GR1). Before
        // everything that reads the flags (the edit/store emitters), so a refused clause is never applied.
        new BindPass("CheckClauseSubjects", PassPhase.UsageResolved, PassPhase.UsageResolved, d => d.CheckClauseSubjects()),
        // The §13.18.52.3 SR1/SR2 SIGN clause subject screen (kb/Work PB537). Placed HERE for the ALIGNED screen's
        // reason — SR1's group bullet asks the group KIND (a bit group can be one by §13.16.4 GR1 inheritance) and
        // SR2 asks the elementary item's usage (§13.18.60.4 GR1 can supply it) — and BEFORE InheritSignClauses,
        // because a refused clause is cleared and must not go on to shed onto subordinates.
        new BindPass("CheckSignClauses", PassPhase.UsageResolved, PassPhase.UsageResolved, d => d.CheckSignClauses()),
        new BindPass("InheritSignClauses", PassPhase.UsageResolved, PassPhase.SignResolved, d => d.InheritSignClauses()),
        // The §13.18.13.3 SR3 a)/b) CODE-SET record screen — §13.18.52.3 SR3's twin (kb/Work PB536). Placed
        // HERE, immediately after InheritSignClauses, and the placement IS the fix: SR3 asks whether a record
        // item is "described with the SIGN IS SEPARATE clause" and "described as usage display", and BOTH facts
        // can arrive by GROUP INHERITANCE — §13.18.52.4 GR1 for the sign, §13.18.60.4 GR1 for the usage — so the
        // screen must run no earlier than the last pass that can supply one. Run from BindFileSection, where it
        // used to live, it read the entry-bind default trailing over-punch and REFUSED a legal record whose
        // group carried SIGN IS SEPARATE. Nothing later is needed: it reads declared shape only, and ExpandTypes
        // (the first pass) has already composed the leaves of a record described with TYPE or SAME AS.
        new BindPass("CheckCodeSetRecordItems", PassPhase.SignResolved, PassPhase.SignResolved, d => d.CheckCodeSetRecordItems()),
        new BindPass("ResolveRedefines", PassPhase.SignResolved, PassPhase.SignResolved, d => d.ResolveRedefines()),
        new BindPass("ClassifyRedefinesClasses", PassPhase.SignResolved, PassPhase.RedefinesClassified, d => d.ClassifyRedefinesClasses()),
        new BindPass("CheckStrongTypeDeclarations", PassPhase.RedefinesClassified, PassPhase.StrongTypeChecked, d => d.CheckStrongTypeDeclarations()),
        new BindPass("OoRouteMethodRedefinesBackings", PassPhase.RedefinesClassified, PassPhase.StrongTypeChecked, d => d.OoRouteMethodRedefinesBackings()),
        // The §13.18.63.3 SR13/SR14 group-level VALUE subordinate screen (kb/Work PB184). Placed HERE because
        // SR14's "explicitly or IMPLICITLY described with usage DISPLAY" needs UsageInheritancePass to have
        // propagated a group-level USAGE down to the leaves (§13.18.60.4 GR1) — an inherited COMP must be
        // caught exactly as a written one is — and nothing later than that: it reads only declared shape.
        new BindPass("CheckGroupValueDeclarations", PassPhase.StrongTypeChecked, PassPhase.StrongTypeChecked, d => d.CheckGroupValueDeclarations()),
        new BindPass("OdoResolve", PassPhase.StrongTypeChecked, PassPhase.OccursResolved, d => d.OdoResolve()),
        new BindPass("DynamicResolve", PassPhase.OccursResolved, PassPhase.OccursResolved, d => d.DynamicResolve()),
        // The OCCURS KEY phrase (§13.18.38.3 SR3) resolved ONCE through the §8.4.2.2 resolver, qualifiers and all
        // (kb/Work PB1018) — after the forest and every TYPEDEF clone exist, before SEARCH ALL / the table SORT read it.
        new BindPass("OccursKeyResolve", PassPhase.OccursResolved, PassPhase.OccursResolved, d => d.OccursKeyResolve()),
        // The §13.18.63.3 SR18–SR23 Format-2 (table) VALUE geometry + the §13.18.63.4 GR12–GR16 odometer
        // resolution (kb/Work PB505; DataBinder.TableValue.cs). Placed HERE, and no earlier, because every one of
        // those rules is written against the entry's OCCURS ANCESTORS — DataItem.Parent is assigned only after
        // the entry binds — and because the dimension ceilings it measures subscripts against are the ones
        // OdoResolve / DynamicResolve have settled (§8.5.1.8's allocated maximum, the dynamic expected capacity).
        // Nothing later than that: it reads declared shape only, and the emitters read the plan it produces.
        new BindPass("ResolveTableValues", PassPhase.OccursResolved, PassPhase.OccursResolved, d => d.ResolveTableValues()),
        // The §13.18.63.3 SR7 VALUE-literal CLASS screen (kb/Work PB646). Placed HERE for two reasons, and it
        // needs both. SR7 reads §8.5.2.1 Table 2's CLASS of a numeric-edited item, which IS its usage
        // ("Numeric-edited (if usage is display)" / "(if usage is national)"), so UsageInheritancePass must have
        // settled §13.18.60.4 GR1 — an INHERITED usage has to give the same verdict as a written one, in both
        // directions. And the rule reaches formats 2 and 5 as well, whose per-occurrence literals only exist
        // once ResolveTableValues has built the plan. Nothing later is needed: it reads declared shape only.
        new BindPass("CheckValueLiteralClasses", PassPhase.OccursResolved, PassPhase.OccursResolved, d => d.CheckValueLiteralClasses()),
        new BindPass("ResolveFiles", PassPhase.OccursResolved, PassPhase.FilesResolved, d => d.ResolveFiles()),
        new BindPass("GateFileRecordByteSurface", PassPhase.FilesResolved, PassPhase.FilesResolved, d => d.GateFileRecordByteSurface()),
        new BindPass("ResolveReports", PassPhase.FilesResolved, PassPhase.FilesResolved, d => d.ResolveReports()),
        new BindPass("CallBindExternalAndGlobal", PassPhase.FilesResolved, PassPhase.FilesResolved, d => d.CallBindExternalAndGlobal(program)),
        new BindPass("PtrBindBasedAndAddressables", PassPhase.FilesResolved, PassPhase.FilesResolved, d => d.PtrBindBasedAndAddressables(program)),
        // A RECURSIVE unit's WS → the static-field channel (§13.5.4 GR1 / §14.6.2.3.3; no-op unless the binder's
        // UnitStaticWs). AFTER the pointer pass — the last tier-overwrite seam — so Tier-B backings, EXTERNAL
        // re-basing, and BASED/ADDRESS-OF forcing are settled facts when the roots route.
        new BindPass("RouteStaticUnitStorage", PassPhase.FilesResolved, PassPhase.FilesResolved, d => d.RouteStaticUnitStorage()),
        new BindPass("MarkFileRecordImageLeaves", PassPhase.FilesResolved, PassPhase.FilesResolved, d => d.MarkFileRecordImageLeaves()),
    };

    /// <summary>The whole-GROUP middle-end manifest (P6 Steps 3–4 — the formerly-hidden second pipeline, now
    /// declared): run by <c>BinderDriver.Bind</c> once per compilation, in this order, after every unit/class DATA
    /// division is bound. Each body needs every unit's BOUND tree or the settled whole-group facts. The TERMINAL
    /// pass is the <c>VersionConformancePass</c> — the SOLE edition gate (P3 pipeline design §2.4): a
    /// <c>CheckOnly</c> compile's verdict is complete once Bind returns, and the driver halts before emit on any
    /// diagnostics.</summary>
    public static IReadOnlyList<GroupBindPass> GroupTail() => new GroupBindPass[]
    {
        // Every unit's PROCEDURE DIVISION binds (user-function signature table first — §8.4.3.2.4 GR1 forward refs).
        new("ProcedureBinding", PassPhase.FilesResolved, PassPhase.ProcedureBound, BinderDriver.BindProcedures),
        // Whole-group operand collection from the BOUND trees + boundary formals (PHASE-05 Step 5; DEVLOG 752/753).
        new("UsageCollectionPass", PassPhase.ProcedureBound, PassPhase.UsageCollected, UsageCollectionPass.Run),
        // Settle every StoreAsImage flag (temp re-sync → image marking → OO harmonize), then classify StorageForm.
        new("StorageFormPass", PassPhase.UsageCollected, PassPhase.StorageComputed, StorageFormPass.Run),
        // THE edition gate (P3's two-arm pass, now the NAMED terminal pass — P6 Step 4): the parse-tree arm fires
        // every syntactic introduction/removal/phrase gate on RECOGNITION; the bound-tree arm fires the
        // genuinely-semantic gates. Reports through the session's edition sink, so the Bind result carries every
        // edition diagnostic for BOTH a full compile and a CheckOnly verdict.
        new("VersionConformancePass", PassPhase.StorageComputed, PassPhase.EditionConformanceChecked,
            // Fully qualified: CobolNet.Binding.Validation (StatementValidation, P7 Step 10c) shadows the
            // relative `Validation.` lookup from inside CobolNet.Binding.*.
            ctx => CobolNet.Validation.VersionConformancePass.Run(ctx, ctx.Session.Edition.Edition, ctx.Session.Edition)),
    };

    /// <summary>Assert the FULL declared chain (the per-unit resolve prefix + the group tail) is a monotone phase
    /// chain, once per process — the pass-list shape is compile-time-fixed, so validating once suffices ("startup
    /// assert"; a benign race merely re-validates). Called from the first <c>BindResolve</c> AND the first
    /// <c>BinderDriver.Bind</c>, whichever runs first.</summary>
    public static void ValidateFullChainOnce()
    {
        if (_chainValidated) return;
        // Build's two declaration-context passes capture the program, but ValidateDag only reads the
        // Name/Requires/Produces metadata (never runs a pass), so a null program is safe here.
        ValidateDag([.. Build(program: null!), .. GroupTail()]);
        _chainValidated = true;
    }

    private static bool _chainValidated;

    /// <summary>Assert the pass list is a monotone phase chain: every pass's <see cref="IPassInfo.Requires"/> is
    /// already produced by a preceding pass, and no pass's <see cref="IPassInfo.Produces"/> regresses below the running
    /// high-water mark. Throws <see cref="InvalidOperationException"/> on the first violation.</summary>
    public static void ValidateDag(IReadOnlyList<IPassInfo> passes)
    {
        var produced = PassPhase.None;   // the running high-water mark over the passes seen so far
        foreach (var p in passes)
        {
            if (p.Requires > produced)
                throw new InvalidOperationException(
                    $"BindPipeline order violation: pass '{p.Name}' requires phase {p.Requires}, but the preceding "
                    + $"passes have only produced {produced}. A pass was reordered before its prerequisite.");
            if (p.Produces < produced)
                throw new InvalidOperationException(
                    $"BindPipeline monotonicity violation: pass '{p.Name}' produces phase {p.Produces}, regressing "
                    + $"below the already-produced {produced}.");
            if (p.Produces > produced) produced = p.Produces;
        }
    }
}
