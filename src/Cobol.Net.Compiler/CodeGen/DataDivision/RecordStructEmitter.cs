// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.CodeGen.Emit;

namespace CobolNet.CodeGen;

/// <summary>The record-struct / profile / root-field emitter of the DATA DIVISION (P7 Step 9l): a nested
/// <c>record struct</c> per group, a static <c>NumProfile</c> per numeric leaf, the INDEXED BY cells, and one
/// field per top-level item — a COBOL record IS a .NET record; no byte substrate, no flattening. Wired by
/// <see cref="DataEmitter"/>.</summary>
internal sealed class RecordStructEmitter(EmitContext ctx, PhysicalModel phys, GroupImageCodec codec, ValueInitializer vals)
{
    /// <summary>Emit every WORKING-STORAGE / FILE-SECTION type, profile, index field, and root field.</summary>
    public void Emit()
    {
        var w = ctx.Writer;
        foreach (var root in ctx.Data.Roots) EmitStructTypeDecls(root, w);
        foreach (var root in ctx.Data.Roots) EmitProfiles(root, w);
        // An OCCURS DYNAMIC CAPACITY register is an implicit VIEW over the table's Capacity — it has NO storage
        // field, but DISPLAY / MOVE-to-alphanumeric of the register formats through its own NumProfile (ISO
        // §13.18.38 GR15 / §8.5.1.9.1; data-model D9), so emit those profiles (not fields) too.
        foreach (var tbl in ctx.Data.CapacityRegisters.Values)
            if (tbl.OccursSpec?.CapacityRegister is { } reg) EmitProfiles(reg, w);
        // Programs are instantiable classes (interprogram design D3): root + index fields are INSTANCE fields —
        // a fresh instance IS the §14.6.2.3.2 initial state; the registry's cached singleton IS last-used state.
        // EXCEPT the static channel (StaticRootFields/StaticIndexCells): a RECURSIVE unit's WS is STATIC data
        // (ISO §13.5.4 GR1) — ONE copy in last-used state across all activations (§14.6.2.3.3) — while its
        // fresh-per-activation instance carries the automatic data (LOCAL-STORAGE, §13.6.4 GR1) and formals.
        // The suppression filter skips members another mechanism provides: a carrier-resident LINKAGE formal
        // (its field is `__lnk{Uid}.Value` — caller storage, ISO §13.7.1). An inherited GLOBAL table's index cell
        // is never among the unit's OWN declarations — the container emits it and a ref-bridge reaches it
        // (§13.18.27 GR2; kb/Work PB919).
        foreach (var idx in ctx.Data.IndexNames.Own)
            if (!ctx.Data.CallSuppressedRootFields.Contains(idx.Cell))
                // A RECURSIVE unit's WS table cell rides its table's static storage (RouteStaticUnitStorage —
                // a last-used table with per-activation indexes would silently lose SET positions).
                w.Line($"private {(ctx.Data.StaticIndexCells.Contains(idx.Cell) ? "static " : "")}long {idx.Cell} = 1;   // INDEX-NAME {idx.Name}");
        // (A METHOD's table cells are never here: a method owns no WORKING-STORAGE, §13.5.3 SR1, and its LOCAL/LINKAGE
        // table cells are per-activation method locals, emitted in OoEmitMethod.)
        foreach (var f in phys.RootPhysicals())
            if (!ctx.Data.CallSuppressedRootFields.Contains(f.Name))
                // A static-channel root is a STATIC field: a RECURSIVE unit's WS (§13.5.4 GR1 — see
                // RouteStaticUnitStorage).
                w.Line($"private {(ctx.Data.StaticRootFields.Contains(f.Name) ? "static " : "")}{f.Type} {f.Name} = {f.Init};   // {f.Comment}");
        if (ctx.Data.EmitsStaticReset) EmitStaticReset(w);
    }

    /// <summary>Emit the <c>__ResetStatics</c> of a unit with STATIC data — a RECURSIVE program or a function
    /// (static working-storage, §13.5.4 GR1) — the
    /// §14.6.2.3.2 initial-state action for that static data AND a unit's file-registration guard. A program
    /// registers it with the run-unit ProgramTable; a class's factory adopts it when the run unit first creates the
    /// factory object (<c>OoEmitter.EmitClassUnit</c>); either way the run unit runs it at its start AND at its
    /// termination (<c>RunUnit.AdoptStaticStorage</c> / <c>RunUnit.Terminate</c>; §14.6.11 items 3, 4 and 6 — a
    /// static POINTER, OBJECT REFERENCE or dynamic item no longer keeps storage alive past the run unit; kb/Work
    /// PB1069). It reassigns every static WS root field / Tier-B backing to the SAME composed
    /// initializer its declaration carries (the ONE ValueInitializer channel — §13.18.63 VALUE semantics
    /// identical to first-load), every static WS index cell to 1, and <c>__filesRegistered</c> to false so
    /// the next activation re-registers fresh connectors in no open mode (action 3; kb/Work PB168). Invoked
    /// by the runtime at run-unit start (§14.6.2.3.2 case 1 — robust to a re-run of the same loaded module),
    /// after CANCEL (case 3 / §14.9.5 GR3), and via an INITIAL container's implicit cancel cascade (case 2).
    /// EXTERNAL records never enter the static channel (they are run-unit ExternalStore cells, untouched per
    /// §14.9.5 GR8). Emission and hook registration read THE ONE shared condition
    /// (<c>DataBinder.EmitsStaticReset</c> — the caller's gate and ProgramEmitter's), so the pair cannot
    /// diverge into a CS0103 in generated code.</summary>
    private void EmitStaticReset(CodeWriter w)
    {
        var stmts = new List<string>();
        foreach (var root in ctx.Data.WorkingStorageRoots)
        {
            // §14.6.2.3.2 action 5: "The address of each based item is set to null" — the static bridge
            // field (kb/Work PB154; the DATA lives in the allocated cell, so there is no value to re-seed).
            // FIRST in the chain: a BASED root's class can be any tier (a plain X(n) BASED is Tier-B), and
            // BASED-ness decides its reset shape before tier does. Emitted from THIS source-ordered walk,
            // like every other reset line — never by iterating the membership set (a HashSet's enumeration
            // order is not generated-source-worthy).
            if (root.IsBased && root.Class?.BasedPointerField is { } bp
                && ctx.Data.StaticBasedBridgeAddrs.Contains(bp))
                stmts.Add($"{bp} = ManagedPointer.Null;   // {root.CobolName ?? "FILLER"} BASED address → NULL (§14.6.2.3.2 #5)");
            else if (root.Class is { Tier: RedefinesTier.StringCanonical } cls)
            {
                // The class's ONE string backing is the storage; members are windows. Reset once, at the canonical.
                if (ReferenceEquals(cls.Canonical, root) && ctx.Data.StaticRootFields.Contains(cls.BackingCsName))
                    stmts.Add($"{cls.BackingCsName} = {RuntimeApi.StrStore(codec.ImageInitOf(root), $"{cls.Width}")};   // {root.CobolName ?? "FILLER"} (Tier-B backing)");
                // An ADDRESS-OF-taken record's static StorageCell (kb/Work PB234): re-seeded IN PLACE with the SAME
                // image its declaration carries (ProgramEmitter's cell initializer — one composer, ImageInitOf), so
                // a pointer taken before the CANCEL still names the unit's one copy (§13.5.4 GR1).
                else if (ReferenceEquals(cls.Canonical, root)
                         && ctx.Data.PtrAddressableCellOf.TryGetValue(cls, out var cell)
                         && ctx.Data.StaticAddressableCells.Contains(cell))
                    stmts.Add($"{cell}.Reinitialize({RuntimeApi.StrStore(codec.ImageInitOf(root), $"{cls.Width}")}){codec.CellDynSeeds(root)};   // {root.CobolName ?? "FILLER"} (ADDRESS-OF cell, §14.6.2.3.2 #2)");
            }
            else if (!(root.Class is { Tier: RedefinesTier.Alias } && !root.IsCanonical)   // a Tier-A view has no field
                && ctx.Data.StaticRootFields.Contains(root.CsName))
                stmts.Add($"{root.CsName} = {vals.FieldInit(root)};   // {root.CobolName ?? "FILLER"}");
            foreach (var idx in DataBinder.IndexDeclarationsUnder(root))
                if (ctx.Data.StaticIndexCells.Contains(idx.Cell))
                    stmts.Add($"{idx.Cell} = 1;   // INDEX-NAME {idx.Name}");
        }
        // The unit-scoped file-registration guard returns to false on the initial-state cases so the next
        // activation re-registers — §14.6.2.3.2 action 3's "not ... in any open mode" realized as fresh
        // connectors; FileRegistry.Register closes anything it displaces (kb/Work PB168).
        if (ctx.Data.UnitStaticFiles && ctx.Data.Files.Count > 0)
            stmts.Add("__filesRegistered = false;   // internal file connectors → no open mode via re-registration (§14.6.2.3.2 #3; kb/Work PB168)");
        w.Line();
        using (w.Block("internal static void __ResetStatics()   // static WS → initial state (ISO §14.6.2.3.2; §14.9.5.4 GR3)"))
            foreach (var s in stmts) w.Line(s);
    }

    /// <summary>The C# (type, initializer) pair for declaring one root as a METHOD LOCAL (the OO slice-2
    /// LINKAGE/LOCAL-STORAGE mapping) — the same composed initializer a field declaration gets, so a group
    /// local's OCCURS arrays and VALUE seeds are identical to field semantics (§8.6.4: LOCAL-STORAGE
    /// re-initializes on every activation — a C# local declaration does exactly that).</summary>
    public (string Type, string Init) RootDecl(DataItem item) => (item.FieldType, vals.FieldInit(item));

    /// <summary>The (name, init) of a method-scoped Tier-B REDEFINES class's ONE string backing when
    /// <paramref name="root"/> is that class's canonical (M2-OO-1h step 3). The class-level field loop suppresses
    /// this backing (it is method-scoped), so <c>OoEmitMethod</c> emits it as a method LOCAL of type
    /// <c>string</c> — the members are windows over it (via <see cref="RedefViewPlace"/>). Null otherwise.</summary>
    public (string Name, string Init)? MethodRedefinesBackingDecl(DataItem root) =>
        // A CELL-BACKED class (BASED / ADDRESS OF — kb/Work PB956) has no stored string: its backing is the
        // class-level `ref` bridge over the cell, and a local of the same name would SHADOW it, splitting the
        // one storage area in two.
        root.Class is { Tier: RedefinesTier.StringCanonical, IsCellBacked: false } cls && ReferenceEquals(cls.Canonical, root)
            ? (cls.BackingCsName, RuntimeApi.StrStore(codec.ImageInitOf(root), $"{cls.Width}"))
            : null;

    private void EmitStructTypeDecls(DataItem item, CodeWriter w)
    {
        if (!item.IsGroup) return;
        // ⛔ A GROUP COLLAPSED INTO A TIER-B REDEFINES BACKING HAS NO TYPE, because it has no FIELD: the whole
        // class is ONE string backing and every member — the canonical included — is a window over it
        // (COBOLNET_DESIGN §4.2; PhysicalModel.BuildPhysicals). Emitting a type anyway was the CRASH half of
        // kb/Work PB203: the type's members come from the PHYSICAL model (empty for a collapsed group) while
        // GroupImageCodec.EmitBitMethods composes AsBits()/FromBits() from group.Children, so a GROUP-USAGE BIT
        // member of a redefines class emitted `AsBits() => BV1 + BV2` over fields that do not exist — four
        // CS0103 on legal source. The two arms are reconciled by not emitting the dead type at all: its
        // AsImage()/AsBits() could only ever have been "", which is a FALSE contract for a group that occupies
        // storage, and PlaceRenderer never calls them for a view (a RedefViewPlace's Read is its window).
        if (item.Class is { Tier: RedefinesTier.StringCanonical }) return;
        foreach (var child in item.Children) EmitStructTypeDecls(child, w);   // nested types first (any order is fine)
        using (w.Block($"private record struct {item.StructName}"))
        {
            foreach (var f in phys.PhysicalChildrenOf(item))
                w.Line($"public {f.Type} {f.Name};   // {f.Comment}");

            // An image-capable group gets the whole-group image facility (COBOLNET_DESIGN §14.4): AsImage
            // concatenates the leaves' character images — a string-stored leaf its characters, a NATIVE
            // fixed-point leaf (DISPLAY/BINARY/PACKED) its zoned digit image (trailing-overpunch sign, the
            // §13.18.60 USAGE GR4 implementor representation) — and FromImage distributes a character image back
            // into them. Used by whole-group MOVE / DISPLAY / compare / WRITE / RELEASE and the READ / RETURN
            // record-area distribution; the SD/FD record codec IS this pair (§8.2). Only a variable-length
            // group or a group with a pointer/object-class leaf stays outside the static codec
            // (DataItem.IsImageCapable; kb/Work PB164 + R40 — every NUMERIC leaf kind is in the image).
            // ⚠ Gated on the ELEMENT shape (ElementImageCapable), not IsImageCapable: a dynamic-capacity
            // TABLE's element struct has a well-defined per-occurrence image the current-extent composer
            // concatenates (CS1061 without it) — the STATIC record codec still consults IsImageCapable at
            // every consumer, so no dynamic group joins a record window; methods emitted ⊇ methods used.
            // A group with a class pointer/object leaf gets the ONE-WAY AsImage only (kb/Work PB244 - the
            // DISPLAY / MOVE-sender transfer image; EmitImageMethods withholds FromImage).
            if (item.ElementTransferImageCapable) codec.EmitImageMethods(item, w);
            // A VARIABLE-LENGTH group instead carries CurrentImage() — the §14.9.11.4 GR7 documented DISPLAY
            // format (A.1 item 57; kb/Work PB164): fixed members by the ONE member-image law, dynamic members
            // at their current extent. DISPLAY-only — such a group stays out of the static record codec (D9).
            // AND the AsVarImage()/FromVarImage() pair its ACTIVATION-BOUNDARY crossing needs (§14.8.2.2 /
            // §14.8.3.2 admit such a group subject to §8.5.1.12 compatibility; kb/Work PB204). Both come from
            // the same capability and the same member-image law, so a group that displays is a group that
            // crosses; it still stays out of the STATIC record codec (D9 — a record window has a fixed width
            // and this group does not).
            else if (item.CurrentExtentImageCapable)
            {
                codec.EmitCurrentImageMethod(item, w);
                codec.EmitVarImageMethods(item, w);
            }
        }
    }

    private void EmitProfiles(DataItem item, CodeWriter w)
    {
        // FLOATS INCLUDED (kb/Work PB164 wave 2): a float leaf's profile carries the Ieee byte form its image
        // lanes dispatch on — the IsFloat:false here was the PROFILE-emission copy of the drifted filter
        // (a float leaf in a group referenced `_P_n` the emitter never declared: CS0103 in generated code).
        if (item.IsElementary && item.Pic is { Category: PicCategory.Numeric })
            // INTERNAL (not private): an INVOKE BY CONTENT conversion composes the FORMAL's value/image at
            // the CALL SITE, qualifying the profile by its owner class ({OWNER}._P_n) — same assembly, one
            // generated file (the OO slice-2 review's cross-class-profile rule).
            w.Line($"internal static readonly NumProfile {item.ProfileName} = {item.Pic.ProfileInitializer(ctx.SignEncoding)};");
        foreach (var child in item.Children) EmitProfiles(child, w);
    }

}
