// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Binding.Bound;
using CobolNet.CodeGen.Emit;
using CobolNet.Runtime;

namespace CobolNet.CodeGen;

using static CobolNet.CodeGen.Emit.EmitText;

/// <summary>
/// The RUN-UNIT emission orchestrator (P7 Step 9n — the inter-program half formerly on the
/// <c>CSharpEmitter.Call</c> partial; COBOLNET_INTERPROGRAM_DESIGN D1–D5; ISO §14.9.4 / §14.9.5 / §14.2 /
/// §8.4.6.3): the module preamble, the MULTI-UNIT unit loop (every top-level program unit and every contained
/// program compiles — one instantiable C# class per program, nested programs as nested classes, ONE
/// <c>.g.cs</c> / ONE assembly, the first unit as entry — design D3, SSOT §18 #8), the program-class plumbing
/// (the <see cref="ICobolProgram"/> ABI, LINKAGE carrier mapping, GLOBAL bridges, EXTERNAL backings), and the
/// run-unit entry wrapper. One instance per <c>EmitBound</c>; owns the run-unit-scoped emitter state (the
/// <see cref="NameAllocator"/> + the three Step-9b state objects) and the CURRENT per-unit composition root:
/// <see cref="BeginUnit"/> re-creates <see cref="Current"/> at every unit switch (program class, OO class
/// half, interface unit) — consumers that span switches (OoEmitter) read through <see cref="Current"/> LIVE,
/// never a captured copy (the Step-9m hazard).
/// </summary>
internal sealed class ProgramEmitter
{
    // The run-unit-scoped emitter state (Step 9b — EmitterState.cs): three cohesive per-scope objects the
    // collaborator emitters receive explicitly. The per-unit/per-statement mutation discipline is documented
    // on each; NameAllocator is ONE per run unit so unique-name sequences span units (Step 9a).
    private readonly NameAllocator _names = new();
    private readonly DispatchState _dispatchState = new();
    private readonly EcState _ecState = new();
    private readonly CallUnitState _callState = new();
    private OoEmitter _oo = null!;

    /// <summary>The WHEN-COMPILED stamp for THIS compilation (§15.99.3 r2 — "the date and time of compilation
    /// of the compilation unit that contains this function"), captured once per <c>EmitBound</c> through the
    /// injectable <see cref="Binding.Procedure.IntrinsicBinder.CompileClock"/> seam (deep-dive D6) and shared
    /// by every unit of the run unit — contained source units bake the CONTAINING compilation's stamp (r2's
    /// second sentence). Per-compilation, NOT per-process (kb/Work PB120): a long-lived compiler process gives
    /// each successive compilation its OWN capture.</summary>
    private readonly Lazy<string> _whenCompiledStamp;

    /// <param name="inputs">The compilation's ambient-input record (kb/Work PB985). The WHEN-COMPILED capture reads
    /// the compile clock THROUGH it, and only on first use — a compilation that never renders WHEN-COMPILED never
    /// reads the clock, so its output is a function of its inputs alone.</param>
    internal ProgramEmitter(CobolNet.Frontend.CompilationInputs? inputs = null)
    {
        var recorder = inputs ?? new CobolNet.Frontend.CompilationInputs();
        _whenCompiledStamp = new Lazy<string>(
            () => RuntimeApi.DateFormat21(recorder.ReadCompilationTime(Binding.Procedure.IntrinsicBinder.CompileClock)),
            LazyThreadSafetyMode.None);
    }

    /// <summary>The CURRENT unit's collaborator set — re-created by <see cref="BeginUnit"/> at each unit
    /// switch (the ONE unit-switch entry all three unit kinds share, Step 9m/9n).</summary>
    internal UnitEmitters Current { get; private set; } = null!;

    /// <summary>Begin one emitted unit: re-create the per-unit context/renderer quadruple and every
    /// collaborator emitter over the fresh writer/data/resolver (the <see cref="UnitEmitters"/> ctor wires
    /// the cycles).</summary>
    internal void BeginUnit(CodeWriter w, DataBinder data, ReferenceResolver refs)
        => Current = new UnitEmitters(w, data, refs, _names, _dispatchState, _ecState, _callState, _oo,
            _whenCompiledStamp);

    /// <summary>The EMIT half (rearch PHASE-03 Step 14a / PHASE-06 Step 2): render the run unit's C# from an
    /// already-bound immutable <see cref="BoundCompilation"/> — reached ONLY after the driver confirmed the edition
    /// sink is clean, so codegen never runs on an errored tree (exit criterion 9). Reads the compilation
    /// READ-ONLY — the storage-form decision and the file-connector qualification already ran inside Bind; the
    /// OO class table and interface-data forests arrive ON the compilation (P6 Step 2), so emission no longer
    /// touches the bind host's session state.</summary>
    internal string Emit(BoundCompilation comp)
    {
        _ecState.Active = comp.EcActive;
        _ecState.OoClasses = comp.OoClasses;
        _oo = new OoEmitter(_dispatchState, _ecState, _callState, this, comp.InterfaceData, comp.OoAdapters);
        var units = comp.Units;
        var classes = comp.ClassUnits;
        bool anyFiles = comp.AnyFiles;

        var w = new CodeWriter();
        w.Line("// <auto-generated>");
        w.Line("//   Generated by WiseOwl COBOL — do not edit. A COBOL program compiled to typed-native C#.");
        w.Line("// </auto-generated>");
        w.Line("#nullable enable");
        w.Line("#pragma warning disable CS0164   // unreferenced label — SEARCH/NEXT-SENTENCE emit per-boundary labels; not every one is jumped to");
        w.Line("using System;                    // Int128 — the wide arithmetic carrier (numeric design D1)");
        w.Line("using CobolNet.Runtime;          // CobolNum / CobolString substrates + the inter-program ABI (ManagedPointer / ICobolProgram / ProgramRegistry)");
        if (anyFiles)
            w.Line("using CobolNet.Runtime.IO;       // CobolFile — the sequential file-I/O facade (§8)");
        if (_ecState.Active || classes.Count > 0)
            // The EC model, OR any class (D10): every class's generated __CobolInvoke switch raises
            // CobolFatalException (EC-OO-UNIVERSAL, GR7c). A class-less EC-free program keeps the
            // zero-scaffolding invariant byte-exact (SSOT §18.16 — the test greps the namespace).
            w.Line("using CobolNet.Runtime.Exceptions; // CobolFatalException — the EC signal type (ISO §14.6.13) + the D10 universal-INVOKE raises (§14.9.23.4 GR7c)");
        if (UnitsOf(comp).Any(u => u.Data.Classification is not null)
            || classes.Any(c => c.Data.Classification is not null || c.FactoryData.Classification is not null))
            // A CHARACTER CLASSIFICATION clause anywhere in the compilation group — a program, or a CLASS-ID whose methods
            // carry it as an activation local: the Globalization types (kb/Work PB64 T5 / PB111). Zero-scaffolding otherwise.
            w.Line("using CobolNet.Runtime.Globalization; // CharacterClassification / LocalePhraseKind — OBJECT-COMPUTER CHARACTER CLASSIFICATION (ISO §12.3.6)");
        w.Line();

        // Interfaces first (readability only — Roslyn needs no ordering), then classes (source order), then
        // the program classes and the run-unit entry wrapper. A class-only/interface-only compilation unit is
        // legal (§10.6) — its module emits the types and an empty Main.
        foreach (var iface in comp.OoClasses.Interfaces)
            _oo.EmitInterfaceUnit(iface, w);
        foreach (var cls in classes)
            _oo.EmitClassUnit(cls, w);
        if (units.Count == 0)
        {
            using (w.Block("internal static class Program"))
            using (w.Block("private static void Main()")) { }
            return w.ToString();
        }

        foreach (var unit in units)
            if (unit.Parent is null && !unit.IsPrototype)   // a prototype has no body (§10.6.2 SR4f) — no class
                EmitProgramClass(unit, w);
        EmitEntryWrapper(units, w, anyFiles);
        return w.ToString();
    }

    // ── Program-class emission (design D3/D4) ───────────────────────────────────────────────────────────────

    /// <summary>⛔ THE ONE PLACE A CHARACTER-CARRIED LINKAGE FORMAL'S LENGTH REGIME IS DECIDED — and the
    /// standard describes THREE of them, where this dispatch used to have two (kb/Work PB165).
    /// <list type="bullet">
    ///   <item><b>ANY LENGTH</b> (ISO §13.18.2 GR1) — the formal's length IS the argument's, fixed for the
    ///     activation: the full-string view, width sentinel −1.</item>
    ///   <item><b>DYNAMIC LENGTH</b> (ISO §13.18.19.4 GR1/GR2) — the length VARIES during execution, minimum
    ///     zero, maximum the LIMIT phrase: the full-string view whose store carries §8.5.1.10.4's replace-and-
    ///     truncate rule. This arm was MISSING, and §13.18.19.3 SR1 makes its absence maximally destructive —
    ///     the PICTURE of a dynamic-length item is exactly ONE symbol, so the fixed arm below delivered a
    ///     ONE-CHARACTER formal for every such crossing (measured: a 7-character argument arrived as
    ///     <c>LEN=1</c>, and the callee's store spliced one character into the caller's seven).</item>
    ///   <item><b>fixed</b> — the declared width window (§14.2.3 GR8: the callee touches only its formal's
    ///     character positions).</item>
    /// </list>
    /// The BY VALUE leg is the detached value copy (§14.2.3 GR10). A BY VALUE dynamic-length formal cannot
    /// arise — §14.2.2 SR2 admits only class numeric, message-tag, object or pointer BY VALUE — so the dynamic
    /// arm is stated first without a mode test rather than duplicated under both.</summary>
    private static string FormalTextCarrier(LinkageFormal f, int fixedWidth) =>
        f.Item.IsDynamicLength
            ? RuntimeApi.ArgAdaptDynText("__args", f.Position, $"{f.Item.DynMaxSize}")
        : f.ByValue
            // GR10's record is of the FORMAL's description (kb/Work PB873), so its profile rides along — the
            // profile field RecordStructEmitter declares for every elementary numeric item.
            ? f.Item.IsElementary && f.Item.Pic is { Category: PicCategory.Numeric } fp && fp.Usage is not Usage.Index
                ? RuntimeApi.ArgAdaptTextValue("__args", f.Position, $"{fixedWidth}", f.Item.ProfileName, $"{fp.Scale}")
                : RuntimeApi.ArgAdaptTextValue("__args", f.Position, $"{fixedWidth}", "null", "0", GroupFormalLayout(f.Item))
            : RuntimeApi.ArgAdaptText("__args", f.Position, f.Item.IsAnyLength ? "-1" : $"{fixedWidth}",
                GroupFormalLayout(f.Item),
                // An image-stored NUMERIC formal (kb/Work PB992) states its description, so an OMITTED
                // argument's benign read is the numeric zero the documented GR12 leniency promises.
                f.Item.IsElementary && f.Item.StoreAsImage ? f.Item.ProfileName : null);

    /// <summary>A fixed-length GROUP formal's §8.5.1.12 layout, the one fact a VARIABLE-LENGTH group argument
    /// needs to meet it (ISO §14.8.2.2 / §8.5.1.12.2; kb/Work PB965) — its layout literal when it has a table,
    /// <see cref="RuntimeApi.NoTableGroupLayout"/> when it has none (its length is then the whole description),
    /// and null for a formal that is not a group, which no variable-length group is compatible with
    /// (§8.5.1.12.1).</summary>
    private static string? GroupFormalLayout(DataItem formal) =>
        // A bit / national group crosses as its ELEMENTARY value (kb/Work PB1166 — CallEmitter.CallStringRead),
        // which has no §8.5.1.12 image layout to meet.
        !ItemCategory.IsGroupItem(formal) || formal.IsAsIfElementary
            || VariableLengthCompatibility.Layout(formal) is not { } layout ? null
        : VariableLengthCompatibility.HasTableOrVariable(layout) ? CallEmitter.LayoutArray(layout)
        : RuntimeApi.NoTableGroupLayout;

    /// <summary>⛔ THE ONE ADOPTION EXPRESSION for a formal's carrier at the activation boundary — one arm per
    /// <see cref="CallCrossing"/>, each with its BY REFERENCE (§14.2.3 GR8 aliasing) and BY VALUE (GR10
    /// detached copy) form. The RESIDENT and the ROUND-TRIP loops call THIS rather than each spelling the
    /// dispatch out: they had two copies of it and both were missing the managed arm (kb/Work PB663), which is
    /// this repo's two-arm-dispatch shape with the arms one method apart.
    /// <paramref name="textWidth"/> is the character arm's window — the formal's PICTURE length for a resident
    /// formal, its whole record image for a round-tripped one.</summary>
    private static string FormalAdopt(LinkageFormal f, CallCrossing crossing, string carrier, int textWidth) =>
        crossing switch
        {
            CallCrossing.Native => f.ByValue
                ? RuntimeApi.ArgAdaptNumValue("__args", f.Position, f.Item.ProfileName, $"{f.Item.Pic!.Scale}", carrier)
                : RuntimeApi.ArgAdaptNum("__args", f.Position, f.Item.ProfileName, $"{f.Item.Pic!.Scale}", carrier),
            // The managed slot aliases (or, BY VALUE, copies) the caller's REFERENCE — §14.2.3 GR10 makes that
            // copy "a SET statement" for a formal of class object or pointer, which is the reference copy
            // itself; §14.8.2.3.2 forces both sides to the same category and class, so the carrier type is the
            // same T on both ends and the adoption needs no conversion machinery at all.
            CallCrossing.Managed => f.ByValue
                ? RuntimeApi.ArgAdaptSlotValue("__args", f.Position, carrier)
                : RuntimeApi.ArgAdaptSlot("__args", f.Position, carrier),
            // §8.5.1.12's component carrier, adopted whole — there is no width window to apply, because the
            // receiving group's own FromVarImage is what re-fits both halves (kb/Work PB204). The formal's own
            // §8.5.1.12 layout rides along so a FIXED-length group argument can be met at the spans the pair
            // corresponds at (§14.8.2.2; kb/Work PB965).
            CallCrossing.VarGroup => f.ByValue
                ? RuntimeApi.ArgAdaptVarGroupValue("__args", f.Position, FormalLayout(f.Item))
                : RuntimeApi.ArgAdaptVarGroup("__args", f.Position, FormalLayout(f.Item)),
            _ => FormalTextCarrier(f, textWidth),
        };

    /// <summary>⛔ THE ONE PLACE A LINKAGE FORMAL'S CROSSING FORM IS DECIDED (kb/Work PB663) — the ACTIVATED
    /// half of <see cref="CallEmitter.CrossingOf(Place)"/>, which is the ACTIVATING half. Before this there was
    /// a <c>bool isNum</c> here and a three-predicate chain there: two formulations of one rule, and this one
    /// had only TWO arms where the model has four, so every formal of class pointer or object-reference fell
    /// into the CHARACTER arm and was declared as a space-filled <c>ManagedPointer&lt;string&gt;</c> — not a
    /// wrong value but uncompilable C# (Roslyn CS1503 on the formal's first reference, on conforming source).
    /// <para>A CARRIER-RESIDENT formal has no <see cref="Place"/> to classify — its storage IS the carrier's
    /// <c>Value</c> — so it is classified from its own <c>DataItem</c>, by the same three tests in the same
    /// order: the managed-slot population (<c>SlotWindow.CarriedBySlot</c>), then a native fixed-point leaf,
    /// then the character image. It can never be VarGroup: residency demands a childless elementary item.</para>
    /// <para>A non-resident formal whose item resolves to NO place keeps the character arm, whose copy-in
    /// stages the existing loud — an unresolvable formal must not acquire a silently-typed cell.</para></summary>
    internal static CallCrossing FormalCrossing(LinkageFormal f, Place? place) =>
        f.CarrierResident
            ? SlotWindow.CarriedBySlot(f.Item) ? CallCrossing.Managed
            : f.Item.Pic is { Category: PicCategory.Numeric, IsFloat: false } && !f.Item.StoreAsImage
                ? CallCrossing.Native
                : CallCrossing.Text
        : place is null ? CallCrossing.Text
        : CallEmitter.CrossingOf(place);

    /// <summary>The <c>T</c> of the formal's <c>ManagedPointer&lt;T&gt;</c> field, one per crossing form.
    /// Native AND Managed carry the formal's OWN cell type (kb/Work R12 + PB663): a wide or unsigned numeric
    /// formal used to get a <c>ManagedPointer&lt;long&gt;</c> cell its carrier-typed reads could not compile
    /// against, and a pointer/object-reference formal got a <c>ManagedPointer&lt;string&gt;</c> SPACE IMAGE
    /// that every reference to it was a Roslyn CS1503 against. A VARIABLE-LENGTH group formal takes the
    /// §8.5.1.12 component carrier (kb/Work PB204): its storage has no fixed record window.
    /// <para>⛔ THE INVARIANT <c>LinkageCarrierDriftTests</c> pins: a formal crosses as a CHARACTER IMAGE only
    /// when its own storage IS a C# string. Any class the data model gives a non-string carrier and this
    /// dispatch does not name is therefore a RED TEST, not a silently space-filled cell.</para></summary>
    internal static string FormalCarrierType(LinkageFormal f, CallCrossing crossing) => crossing switch
    {
        CallCrossing.Native or CallCrossing.Managed => f.Item.ElementType,
        CallCrossing.VarGroup => RuntimeApi.VarGroupType,
        _ => "string",
    };

    /// <summary>Emit one program's instantiable class (design D3 — a static class cannot recurse or hold the
    /// per-activation copies INITIAL/RECURSIVE need; the registry's cached singleton realizes last-used state,
    /// §14.6.2.3.3), its <see cref="ICobolProgram"/> ABI surface, and its contained programs as nested classes.</summary>
    private void EmitProgramClass(BoundUnit unit, CodeWriter w)
    {
        var data = unit.Data;
        BeginUnit(w, data, unit.Refs);
        var refs = Current.Refs;
        _callState.SelfPath = unit.Path;
        _callState.ReturningPlace = data.LinkageReturning is { } ret ? refs.ResolveItem(ret) : null;
        _callState.Formals = data.LinkageFormals;   // §8.8.4.8.4 GR1c forwarding (kb/Work PB165)
        _ecState.UnitHasF3 = unit.Bound.Declaratives?.Any(d => d.EcEntries is not null) ?? false;   // → __EcDispatch exists
        _ecState.UnitHasF3Perform = unit.Bound.Ec?.HasF3Perform ?? false;   // → __EcPerform + the F3-frame interceptor (§14.9.28)
        _ecState.UnitHasF4 = unit.Bound.Declaratives?.Any(d => d.Eo is not null) ?? false;   // → __EcObjDispatch exists (EC-OO F4)
        // §7.3.21.4 GR1 — this program or function's automatic propagation (kb/Work PB1119), read at its fatal default.
        _ecState.Propagation = AutomaticPropagation.Of(data.AutomaticPropagation, inMethod: false,
            unit.Bound.RaisingObjects ?? [], _ecState.OoClasses);
        // A containing program with USE … GLOBAL declaratives makes this unit's I-O hooks walk outward on a
        // no-local-match (ISO §14.9.49.4 GR4b) — consumed by EmitDispatcher/EmitUseMachinery.
        _dispatchState.OuterGlobalUse = ChainHasGlobalUse(unit.Parent);

        // Inherited GLOBAL files' FILE STATUS routing (§12.4.5.8.4 GR1 NOTE 1 — see the field doc): resolve each
        // ancestor's status item with the ANCESTOR's resolver, then re-anchor the place behind the __outer chain.
        _callState.InheritedStatusPlace.Clear();
        int statusDepth = 0;
        for (var anc = unit.Parent; anc is not null; anc = anc.Parent)
        {
            statusDepth++;
            string outerPrefix = RuntimeApi.OuterChain(statusDepth);
            foreach (var f in anc.Data.Files)
                if (f.IsGlobal && f.FileStatusItem is { } si && !_callState.InheritedStatusPlace.ContainsKey(f)
                    && anc.Refs.ResolveItem(si) is { } sp && PrefixPlace(sp, outerPrefix) is { } pp)
                    _callState.InheritedStatusPlace[f] = pp;
        }

        // Per-formal carrier shape, resolved once: a carrier-resident formal aliases per access; a group /
        // redefined formal round-trips its character image at the activation boundary (deep-dive hard problem —
        // the whole-struct round trip, realized at the call boundary).
        var formals = data.LinkageFormals
            .Select(f =>
            {
                Place? place = f.CarrierResident ? null : refs.ResolveItem(f.Item);
                var crossing = FormalCrossing(f, place);
                return (Formal: f, Place: place, Crossing: crossing, Carrier: FormalCarrierType(f, crossing));
            })
            .ToList();

        using (w.Block($"internal sealed class {unit.ClassName} : ICobolProgram"))
        {
            if (unit.Parent is { } parent)
            {
                w.Line($"private readonly {parent.ClassName} __outer;   // the containing program's instance (GLOBAL storage lives there, ISO §13.18.27)");
                using (w.Block($"public {unit.ClassName}({parent.ClassName} __o)")) w.Line("__outer = __o;");
            }
            w.Line("private bool __asCalled;   // true during a CALL activation — EXIT PROGRAM is CONTINUE otherwise (ISO §14.9.14.4 GR2)");
            if (data.Files.Count > 0)
                // The guard's storage duration IS the connector's scope (kb/Work PB168): §14.6.2.3.2
                // action 3 puts internal connectors in no open mode only when data enters the INITIAL
                // state — for a non-INITIAL unit's static data that is cases 1–3 — and §14.6.2.3.3 keeps
                // them LAST-USED otherwise. A RECURSIVE unit's fresh per-activation instances therefore
                // share ONE registration (static; __ResetStatics returns it to false on the initial-state
                // cases), while an INITIAL/canceled unit re-registers per fresh instance as before.
                w.Line(data.UnitStaticFiles
                    ? "private static bool __filesRegistered;   // connectors register once per RUN UNIT — last-used across recursive activations (ISO §14.6.2.3.2 cases 1–3 / §14.6.2.3.3; kb/Work PB168); reset by __ResetStatics"
                    : "private bool __filesRegistered;   // connectors register once per INSTANCE — a canceled/INITIAL program gets fresh connectors (ISO §14.6.2.3.2)");
            // The report ENGINES are per-INSTANCE objects and get their own per-INSTANCE guard — the PB168
            // review fleet caught them riding the (now sometimes static) registration guard: a RECURSIVE
            // unit's second activation skipped the block and NRE'd on a null __RPT_n. One guard per scope.
            if (data.Reports.Count > 0)
                w.Line("private bool __reportsConstructed;   // report engines construct once per INSTANCE (kb/Work PB168 — never behind the run-unit registration guard)");
            // The OBJECT-COMPUTER members — __COLLATE / __COLLATE_NAT / the __CLASSIFY field — from the ONE helper the OO
            // emitter shares (kb/Work PB111: a CLASS-ID with either clause used to be a CS0103 on its emitted methods).
            ObjectComputerEmit.EmitMembers(data, w, classificationField: true);

            foreach (var b in unit.Bridges)
            {
                switch (b.Kind)
                {
                    case CallBridgeKind.Presence:
                        w.Line($"private bool {b.Field} => {b.Path};   // a GLOBAL formal's omitted-argument presence (ISO §8.8.4.8.4 GR1; kb/Work PB971)");
                        continue;
                    case CallBridgeKind.Cell:
                        w.Line($"private StorageCell {b.Field} => {b.Path};   // the cell behind a GLOBAL cell-backed item of a containing program (ISO §13.18.27.4 GR2; kb/Work PB1009)");
                        continue;
                }
                string type = b.Kind switch
                {
                    CallBridgeKind.Index => "long",
                    CallBridgeKind.Backing => "string",
                    CallBridgeKind.Address => "ManagedPointer",
                    // The carrier's cell type is the formal's own crossing form — the ONE dispatch the container
                    // declares it with (a resident formal has no Place, so it classifies from its DataItem).
                    CallBridgeKind.Carrier => $"ManagedPointer<{FormalCarrierType(b.Formal!, FormalCrossing(b.Formal!, null))}>",
                    _ => b.Item!.Occurs is not null ? b.Item.ElementType + "[]" : b.Item.ElementType,
                };
                w.Line($"private ref {type} {b.Field} => ref {b.Path};   // GLOBAL item of a containing program (ISO §13.18.27.4 GR2 — container storage, contained visibility)");
            }
            _oo.EmitExternalBackings(data, w);
            _oo.EmitPointerBackings(data, w);   // BASED bridges + ADDRESS-OF cells — the ONE renderer the OO type-half shares (kb/Work PB956)

            new DataEmitter(Current.Ctx).Emit();

            foreach (var (f, _, crossing, carrier) in formals)
            {
                // The UNBOUND seed (ISO §13.7.4 GR3 — a linkage item referenced outside an activation that
                // supplied it). Native AND Managed default through the item's OWN PicInfo.DefaultInitializer
                // (0L / 0UL / (Int128)0 / (UInt128)0 / ManagedPointer.Null / ProgramPointer.Null /
                // FunctionPointer.Null / null), so the cell type and the seed cannot drift (kb/Work R12 +
                // PB663 — the managed seed IS §13.18.63's initial state for its class). A variable-length
                // carrier seeds EMPTY — a space image of its collapsed width would be a wrong-shaped value,
                // not a benign one (kb/Work PB204).
                string init = crossing switch
                {
                    CallCrossing.Native or CallCrossing.Managed
                        => $"ManagedPointer<{carrier}>.Cell({f.Item.Pic!.DefaultInitializer})",
                    CallCrossing.VarGroup => $"ManagedPointer<{carrier}>.Cell({RuntimeApi.VarGroupEmpty})",
                    _ => $"ManagedPointer<string>.Cell(new string(' ', {Math.Max(1, f.Item.ImageWidth)}))",
                };
                w.Line($"private ManagedPointer<{carrier}> {f.CarrierField} = {init};   "
                    + $"// LINKAGE formal #{f.Position + 1} — the caller-storage carrier (ISO §13.7.1; design D1)");
                // The presence member every guarded reference to this formal reads (kb/Work PB971): Uid-keyed, so
                // a contained program's GLOBAL bridge of it (BinderDriver) cannot collide with its own formals.
                if (f.Item.OmittedGuard is { } og)
                    w.Line($"private bool {og.Presence} => {f.CarrierField}.IsNull;   "
                        + "// the omitted-argument condition of this formal (ISO §8.8.4.8.4 GR1; §14.9.4.4 GR11)");
            }
            w.Line();

            EmitCallMethod(unit, formals, w);
            if (OoEmitter.WantsExternalDescribes(data))
            {
                // §14.8.4: the main-program activation registers its external descriptions too (the ACTIVATOR
                // mask is zero there — store-only). A CALLed activation gets it from the activation BOUNDARY,
                // which calls DescribeExternals() before Call() (§14.9.4.4 GR3e precedes GR3g — kb/Work PB233).
                w.Line("void ICobolProgram.Activate() { DescribeExternals(); __Activate(); }");
                _oo.EmitExternalDescribes(data, unit.Path, "public void DescribeExternals()", $"{data.ExternalCheckMask}", w);
            }
            else
                w.Line("void ICobolProgram.Activate() => __Activate();");
            // The INSTANCE-owned ADDRESS-OF cells (WORKING-STORAGE of a non-recursive program) end when the instance is
            // discarded — an INITIAL program's activation exit, any other program's CANCEL (ISO §8.6.4; ICobolProgram.
            // EndStorage; kb/Work PB1216). Emitted only when the unit owns such a cell (zero scaffolding).
            var instanceCells = data.CellFieldsOf(CellLifetime.Instance).ToList();
            if (instanceCells.Count > 0)
                using (w.Block("public void EndStorage(StorageEnd __why)   // §8.6.4 / §8.6.5: the instance's storage ends — pointers into it are no longer valid (§13.18.5.4 GR4)"))
                    foreach (string cell in instanceCells) w.Line($"{cell}.End(__why);");
            using (w.Block("public void CloseFiles()"))   // CANCEL §14.9.5 GR9 / run-unit close §14.6.11
                foreach (var file in data.Files)
                    // CANCEL closes INTERNAL connectors only (§14.9.5 GR9); an EXTERNAL connector persists
                    // (GR8 / §13.18.22.4 GR4a). An SD is not a file connector at all — it never registers
                    // (the same skip every registration/IO loop takes), so closing it would Require() a
                    // name the registry cannot hold.
                    if (!file.IsExternal && !file.IsSortMerge)
                        // GR9 scopes the implicit CLOSE to a connector "that is open" — the guarded entry
                        // skips a closed one instead of stamping it '42' (kb/Work PB154); a close failure
                        // maps to '30' inside FileConnector.Close (PB140), so the loop never abandons the
                        // remaining files ("executed for ALL such files, even when an error occurs").
                        w.Line($"{RuntimeApi.FileCloseIfOpen(FileKeyExpr(file))};");
            // §14.6.13.1.4 #3 for a condition raised at a RUNTIME site: the ABI face of this unit's Format-3
            // selection, so the raise site — which has no statement-level node to hang a dispatch on — reaches
            // the declaratives of the ACTIVATION that is executing (kb/Work PB367b; the activation boundary in
            // ProgramTable installs it). A unit with no F3 machinery emits nothing and takes the interface
            // default's "no qualifying declarative", which is what keeps the zero-scaffolding invariant.
            if (Current.Ec.UnitHasDispatchFunnel)
                w.Line($"int INonfatalSelector.NonfatalDispatch(string __ec) => {Current.Ec.EcDispatchExpr("__ec", "\"\"")};"
                    + "   // ISO §14.6.13.1.4 #3 / §14.9.49.4 GR3");
            if (unit.Children.Count > 0 && ChainHasGlobalUse(unit))
                EmitRunGlobalUse(unit, w);
            // The Format-2 half of §14.9.49.4 GR4 (kb/Work PB369): per visible report, the selector its GENERATE
            // and TERMINATE statements hand the engine. Emitted BEFORE the dispatcher, which emits the statements.
            Current.ReportWriter.EmitBeforeReportingSelectors(unit, w);
            w.Line();

            if (unit.Bound.Paragraphs.Count > 0)
                Current.Dispatch.EmitDispatcher(unit.Bound, w);
            else
                using (w.Block("public void __Activate()")) { }

            foreach (var child in unit.Children)
                EmitProgramClass(child, w);
        }
    }

    /// <summary>Every program unit of the compilation — the top-level units and, recursively, their contained
    /// programs (the walk the usings header needs: a CHARACTER CLASSIFICATION clause on any unit, including an
    /// inherited one, puts the Globalization types in the generated source).</summary>
    private static IEnumerable<BoundUnit> UnitsOf(BoundCompilation comp)
    {
        var stack = new Stack<BoundUnit>(comp.Units);
        while (stack.Count > 0)
        {
            var u = stack.Pop();
            yield return u;
            foreach (var c in u.Children) stack.Push(c);
        }
    }

    /// <summary>Re-anchor a CONTAINER-resolved place behind the contained class's <c>__outer</c> instance chain
    /// (the §12.4.5.8.4 GR1 NOTE 1 status routing). A FILE STATUS item is never subscripted (§12.4.5.8 SR1 — no
    /// OCCURS), so its member path / Tier-B backing prefix textually. An unexpected place shape returns null —
    /// the caller then falls back to the loud-guard path, never a silent wrong-storage store (§1.4).</summary>
    private static Place? PrefixPlace(Place p, string prefix) => p switch
    {
        MemberPlace m => new MemberPlace(m.Path.Reroot(prefix), m.MemberItem),
        // `with` rather than a fresh construction: the window's CODING (kb/Work PB203, PB231) is part of the
        // place's identity and re-anchoring changes only WHERE the storage lives, never which positions the
        // member holds.
        // ⛔ EVERY PATH THE PLACE CARRIES IS RE-ANCHORED, NOT JUST THE BACKING. A SlotWindow coding carries a
        // SECOND structural path — the class's StorageCell, which holds the area's managed slots beside its byte
        // image (kb/Work PB231) — and re-anchoring one path and not the other would name storage in two
        // different classes. It is the same re-anchoring, so it is applied by the same expression; a coding that
        // carries no path is unaffected. (Unreachable today: this method exists for a container-resolved FILE
        // STATUS item, which §12.4.5.8.3 SR1 makes a two-character alphanumeric item — never a pointer. Written
        // as the rule anyway, because the next caller of PrefixPlace will not know that.)
        RedefViewPlace r => r with
        {
            Backing = r.Backing.Reroot(prefix),
            Coding = r.Coding is CellWindowCoding c ? c with { Cell = c.Cell.Reroot(prefix) } : r.Coding,
        },
        _ => null,
    };

    /// <summary>True when <paramref name="u"/> or any of its containers declares a FORMAT 1 <c>USE GLOBAL AFTER …</c>
    /// declarative (ISO §14.9.49.4 GR4b — the containment chain a contained program's I-O check walks outward). A
    /// Format-2 <c>USE GLOBAL BEFORE REPORTING</c> is GR4's other half and has its own walk, the per-report selector
    /// (<c>ReportWriterEmitter.EmitBeforeReportingSelectors</c>; kb/Work PB369) — it never reaches an I-O check.</summary>
    private static bool ChainHasGlobalUse(BoundUnit? u)
    {
        for (; u is not null; u = u.Parent)
            if (u.Bound.Declaratives is { } ds && ds.Any(d => d.Global && d.ReportGroup is null)) return true;
        return false;
    }

    /// <summary>Emit the cross-program GLOBAL USE dispatch member (ISO §14.9.49.4 GR4b): a contained program's
    /// <c>__IoCheck</c> fallthrough (no local match, GR4a) calls the container instance's
    /// <c>__RunGlobalUse</c>, which examines THIS program's <c>USE … GLOBAL</c> declaratives — file-name scope
    /// before open-mode scope (GR5) — and on a match runs the handler in THIS instance (the declaring program's
    /// data, §8.4.6.2); otherwise the walk continues to the next container ("repeated with the next higher
    /// directly containing source element", GR4b) or stops false at the outermost. Emitted only on classes a
    /// contained program can actually reach (children exist + the chain has GLOBAL declaratives), so a
    /// declarative-free compilation group's generated source is unchanged.</summary>
    private void EmitRunGlobalUse(BoundUnit unit, CodeWriter w)
    {
        var decls = unit.Bound.Declaratives ?? [];
        // Both GLOBAL tiers come from the ONE UseTierEmitter the two local selectors use, and both are
        // edition-invariant — the determination is written there (kb/Work PB344).
        using (w.Block("public bool __RunGlobalUse(string __f)"))
        {
            // ⛔ DISCARDING __RunUse's RESUME ACTION HERE IS THE RULE, NOT the PB141 defect it resembles.
            // ISO §14.9.33.4 GR1: "If the RESUME statement is executed within the scope of execution of a global
            // declarative, it is the equivalent of the execution of a CONTINUE statement." Every declarative this
            // selector can run is a GLOBAL one (globalOnly), so its resume action is a CONTINUE by definition and
            // there is nothing to hand back — unlike __IoCheck's LOCAL tier, where discarding it WAS kb/Work
            // PB141. Said out loud because the two call sites look identical and only one of them may discard
            // (measured as a candidate defect and refuted by the rule, kb/Work PB368).
            UseTierEmitter.EmitScopeTiers(w, decls,
                i => $"{_dispatchState.RunUseCall(i, decls[i].Range)}; return true;", globalOnly: true);
            w.Line(unit.Parent is { } p && ChainHasGlobalUse(p)
                ? "return __outer.__RunGlobalUse(__f);   // continue outward (§14.9.49.4 GR4b)"
                : "return false;   // outermost source element reached — no qualifying GLOBAL declarative (GR4b)");
        }
        w.Line();
    }

    /// <summary>Emit the opaque-ABI <c>Call</c> body: positional formal mapping (ISO §14.2.3 GR2), the
    /// activation, boundary copy-out for image formals, and RETURNING delivery (GR7).</summary>
    private void EmitCallMethod(
        BoundUnit unit, List<(LinkageFormal Formal, Place? Place, CallCrossing Crossing, string Carrier)> formals,
        CodeWriter w)
    {
        using (w.Block("public void Call(CobolArg[] __args, CobolArg? __ret)"))
        {
            // §14.9.4.4 GR3e runs OUTSIDE this method: the external-conformance check is an activation-ATTEMPT
            // step that precedes GR3g's transfer of control, so the activation boundary calls
            // ICobolProgram.DescribeExternals() before Call() (kb/Work PB233). Keeping it here made every
            // EC-EXTERNAL raise indistinguishable from an exception escaping the callee's BODY, which is the
            // fact GR3i turns on — the ordering guarantee (before LOCAL-STORAGE re-initialization and formal
            // adoption) is strictly stronger now, not weaker.
            // LOCAL-STORAGE is AUTOMATIC data (ISO §13.6.4 GR1): "placed in the initial state every time the
            // … program … is activated" (§14.6.2.3.2) — for an INITIAL or RECURSIVE unit the fresh instance
            // per activation already IS that state, but a cached-singleton unit (neither attribute) re-enters
            // the SAME instance, so its LS roots re-initialize HERE, at every CALL activation entry, through
            // the SAME composed initializers the field declarations carry (the ONE ValueInitializer channel —
            // §13.18.63 VALUE semantics; the OoEmitMethod LS-local pattern, program-class edition). An LS
            // table's INDEXED BY cell resets with its table. Emitted only when an LS section EXISTS.
            // ⛔ A CELL-BACKED root has no root field — a BASED item's storage is its implicit data-address pointer and an
            // ADDRESS-OF-taken record's is its StorageCell — so the field loop below skips it, and those channels are
            // re-seeded by the ONE per-activation seed the method arm uses (OoEmitter.ActivationPointerSeeds; kb/Work
            // PB1132): §14.6.2.3.2 action 5 "The address of each based item is set to null", and the record's cell starts
            // at its initial image. The activation's non-formal LINKAGE roots join it: §8.6.5 ends a linkage-section
            // based association "at the end of the execution of the runtime element", so the next activation starts
            // with none — NULL, the method arm's answer (a formal's storage is the activator's, adopted below, and the
            // RETURNING item keeps the last-used state every other program RETURNING item keeps).
            if (!unit.Initial && !unit.Recursive)
                foreach (var (member, _, fresh, _) in _oo.ActivationPointerSeeds(unit.Data, unit.Data.LocalStorageRoots
                             .Concat(unit.Data.LinkageRoots.Where(r => !ReferenceEquals(r, unit.Data.LinkageReturning)
                                 && !unit.Data.LinkageFormals.Any(f => ReferenceEquals(f.Item, r))))))
                    w.Line($"{member} = {fresh};   // per-activation data-pointer storage (ISO §14.6.2.3.2 action 5; §8.6.4; §8.6.5)");
            if (!unit.Initial && !unit.Recursive && unit.Data.LocalStorageRoots.Count > 0)
            {
                var fields = new DataEmitter(Current.Ctx);
                foreach (var root in unit.Data.LocalStorageRoots)
                {
                    if (unit.Data.CallSuppressedRootFields.Contains(root.CsName)) continue;
                    if (root.Class is { Tier: RedefinesTier.Alias } && !root.IsCanonical) continue;   // Tier-A view — no field
                    if (fields.MethodRedefinesBackingDecl(root) is { } bkl)   // Tier-B canonical → the ONE string backing
                        w.Line($"{bkl.Name} = {bkl.Init};   // LOCAL-STORAGE {root.CobolName} (Tier-B backing) — initial state each activation (§13.6.4 GR1 / §14.6.2.3.2)");
                    else if (root.Class is { Tier: RedefinesTier.StringCanonical })
                        continue;   // a non-canonical Tier-B member — a window over the backing, no field
                    else
                        w.Line($"{root.CsName} = {fields.RootDecl(root).Init};   // LOCAL-STORAGE {root.CobolName ?? "FILLER"} — initial state each activation (§13.6.4 GR1 / §14.6.2.3.2)");
                    foreach (var idx in DataBinder.IndexDeclarationsUnder(root))
                        if (!unit.Data.CallSuppressedRootFields.Contains(idx.Cell))
                            w.Line($"{idx.Cell} = 1;   // INDEX-NAME {idx.Name} (LOCAL-STORAGE table cell)");
                }
            }
            foreach (var (f, place, crossing, carrier) in formals)
            {
                if (f.CarrierResident)
                {
                    // Per-access aliasing of the caller's storage (§14.2.3 GR8): every reference to the formal
                    // reads/writes through this carrier (its CsName IS `__lnk{Uid}.Value`). An ANY LENGTH formal
                    // (ISO §13.18.2 GR1 — its length IS the caller's argument length) takes the FULL-STRING
                    // view (the width -1 sentinel), never a Pic.Length=1 window that would truncate the caller.
                    // A BY VALUE formal adopts the DETACHED value-copy cell instead (§14.2.3 GR10 — the
                    // activated element's stores reach only the copy, never the caller; §14.2.2 SR2 restricts
                    // the carried shape to class numeric, object or pointer, so the text leg has no BY VALUE
                    // arm while the managed one does).
                    w.Line($"{f.CarrierField} = {FormalAdopt(f, crossing, carrier, CallEmitter.ElementaryFormalWindow(f.Item))};");
                    continue;
                }
                // Boundary round-trip formal (group / redefined): adopt the carrier, copy the caller's image in.
                // A REDEFINED fixed-point BY VALUE formal (still class numeric — SR2-legal) rides the image
                // round trip over a DETACHED cell (§14.2.3 GR10): copy-in below, and NO copy-out at return.
                w.Line($"{f.CarrierField} = {FormalAdopt(f, crossing, carrier, Math.Max(1, CallEmitter.BoundaryImageWidth(f.Item)))};");
                using (w.Block($"if ({RuntimeApi.ArgAdaptPresent("__args", f.Position)})"))
                {
                    if (place is null)
                        w.Line(LoudStmt($"LINKAGE formal '{f.Item.CobolName}' is not resolvable to storage"));
                    else if (crossing is CallCrossing.VarGroup)
                        w.Line(PlaceRenderer.WriteVarGroupImage(place, $"{f.CarrierField}.Value",
                            "LINKAGE formal copy-in of"));
                    else if (crossing is CallCrossing.Text)
                        w.Line(CallEmitter.CallStringWrite(place, $"{f.CarrierField}.Value"));
                    else
                        // Native AND Managed write the storage's own value — for a managed slot that is the
                        // reference itself (kb/Work PB663), never an image of it.
                        w.Line(PlaceRenderer.Write(place, $"{f.CarrierField}.Value"));
                }
            }
            w.Line("__asCalled = true;");
            // The activation's LOCAL-STORAGE cells end with it (ISO §8.6.4 — "persists while that instance of the runtime
            // element is in active state"), for EVERY kind of unit: a fresh INITIAL / RECURSIVE instance and the cached
            // singleton alike. A pointer returned past this point names storage that is no longer a valid address
            // (§8.6.5; §13.18.5.4 GR4 — kb/Work PB1216). The same DataBinder.LifetimeOfCell answer the cell's declaration
            // modifier reads, so a cell cannot be declared per-activation and never ended.
            string endCells = string.Concat(unit.Data.CellFieldsOf(CellLifetime.Activation)
                .Select(c => $" {c}.End(StorageEnd.ActivationEnded);"));
            w.Line($"try {{ __Activate(); }} finally {{ __asCalled = false;{endCells} }}");
            foreach (var (f, place, crossing, _) in formals)
            {
                if (f.CarrierResident || place is null || f.ByValue) continue;
                // Copy the (possibly mutated) formal back to the caller's storage — the BY REFERENCE result
                // becomes visible at activation end (§14.2.3 GR8/GR9; a BY CONTENT cell absorbs it invisibly;
                // a BY VALUE formal is SKIPPED above — its stores must never reach the caller, §14.2.3 GR10).
                using (w.Block($"if ({RuntimeApi.ArgAdaptPresent("__args", f.Position)})"))
                    w.Line(crossing switch
                    {
                        CallCrossing.VarGroup =>
                            $"{f.CarrierField}.Value = {PlaceRenderer.VarGroupImage(place, "LINKAGE formal copy-out of")};",
                        CallCrossing.Text => $"{f.CarrierField}.Value = {CallEmitter.CallStringRead(place)};",
                        _ => $"{f.CarrierField}.Value = {PlaceRenderer.Read(place)};",
                    });
            }
            if (_callState.ReturningPlace is { } ret)
                w.Line($"{ReturningDelivery(ret)};");
        }
    }

    /// <summary>⛔ THE RETURNING DELIVERY (ISO §14.6.5 — "The result of the execution of a program, function, or
    /// method that specifies a RETURNING phrase in its procedure division header, is the content of the data
    /// item referenced by that RETURNING phrase"), with the SENDING item's description beside its content
    /// (kb/Work PB962/PB965) — the receiver's arrives on <c>__ret</c> itself (<c>CobolArg</c>):
    /// <list type="bullet">
    /// <item>a variable-length group — its §8.5.1.12 carrier plus its layout (§14.8.3.2's compatibility
    /// sentence admits a FIXED-length receiver, which the runtime meets at the corresponding spans);</item>
    /// <item>a fixed-length group with a table — its image plus its layout (the same sentence, other way
    /// round);</item>
    /// <item>a fixed-point numeric item — its content plus its <c>NumProfile</c>: §14.8.3.3 gives a conforming
    /// receiver the same PICTURE and USAGE, so the delivery is a content transfer under that one description,
    /// never a re-parse of the text as a number (which aborted on spaces);</item>
    /// <item>anything else — its content alone.</item>
    /// </list></summary>
    private static string ReturningDelivery(Place ret)
    {
        string? layout = CallEmitter.BoundaryLayout(ret);
        if (CallEmitter.CallPlaceIsVarGroup(ret))
            return RuntimeApi.ArgAdaptStoreReturn("__ret", PlaceRenderer.VarGroupImage(ret, "RETURNING item"), layout!);
        string? profile = ret.DenotedItem is { Pic: { Category: PicCategory.Numeric, IsFloat: false, Usage: not Usage.Index } } item
            ? item.ProfileName : null;
        if (CallEmitter.CallPlaceIsString(ret))
            return layout is not null
                ? RuntimeApi.ArgAdaptStoreReturnGroup("__ret", CallEmitter.CallStringRead(ret), layout)
                : RuntimeApi.ArgAdaptStoreReturn("__ret", CallEmitter.CallStringRead(ret), profile);
        return RuntimeApi.ArgAdaptStoreReturn("__ret", PlaceRenderer.Read(ret), profile);
    }

    /// <summary>A variable-length group formal's §8.5.1.12 layout, emitted for its adapter (kb/Work PB965).</summary>
    private static string FormalLayout(DataItem formal) =>
        CallEmitter.LayoutArray(VariableLengthCompatibility.Layout(formal)
            ?? throw new InvalidOperationException($"variable-length formal '{formal.CobolName}' has no §8.5.1.12 layout"));

    /// <summary>Emit the module registrar + the run-unit entry wrapper. <c>__CobolModule</c> is the ONE public,
    /// well-known discovery surface of a compiled module (deep-dive D2; the generated program classes are
    /// internal): its <c>Register()</c> registers every program unit (containers before containees), serving
    /// both the own-run-unit <c>Main</c> AND a CALLing run unit's sibling-assembly probe
    /// (<c>ProgramRegistry.ResolveVisible</c> rule-4 fallthrough — the implementor-defined §14.9.4.4 GR3b
    /// locate step; §14.6.1: a run unit contains one or more runtime modules). <c>Main</c> runs the first
    /// program as main and performs the §14.6.11 implicit CLOSE at run-unit termination; STOP RUN unwinds to
    /// here (§14.9.43); a main-program GOBACK already returned normally through its activation entry.</summary>
    private void EmitEntryWrapper(IReadOnlyList<BoundUnit> units, CodeWriter w, bool anyFiles)
    {
        using (w.Block("public static class __CobolModule"))
        using (w.Block("public static void Register()"))
            foreach (var u in units)
            {
                if (u.IsPrototype) continue;   // a prototype registers no runtime module — the separately-compiled definition does (§10.6.3 GR1)
                string parentPath = u.Parent is { } p ? CsLiteral(p.Path) : "null";
                string factory = u.Parent is { } pp
                    ? $"static __o => new {u.ClassRef}(({pp.ClassRef})__o!)"
                    : $"static __o => new {u.ClassRef}()";
                // A RECURSIVE unit with static WS storage OR unit-scoped file connectors registers its
                // __ResetStatics — the runtime's §14.6.2.3.2 initial-state hook (run-unit start / CANCEL /
                // INITIAL-container cascade). ⛔ THE ONE condition is DataBinder.EmitsStaticReset, shared
                // with RecordStructEmitter's emission so the pair cannot diverge (kb/Work PB168). The
                // optional argument is OMITTED for every other unit, keeping their lines byte-identical.
                string reset = u.Data.EmitsStaticReset ? $", {u.ClassRef}.__ResetStatics" : "";
                // §14.8.2.1 / §14.9.4.4 GR3d (kb/Work PB133 wave C2b): a unit WITH formals registers its
                // count facts and its ACTIVATED-half checking bit; the required count excludes the trailing
                // OPTIONAL run (§14.8.2.1's omissible tail). Formal-less units' lines stay byte-identical.
                int fc = u.Data.LinkageFormals.Count;
                int rq = fc;
                while (rq > 0 && u.Data.LinkageFormals[rq - 1].Optional) rq--;
                string argMeta = fc > 0
                    ? $", formalCount: {fc}, requiredCount: {rq}, argMismatchChecking: {CallEmitter.CallBool(u.Data.ArgMismatchChecking)}"
                    : "";
                string resetNamed = reset.Length > 0 && argMeta.Length > 0
                    ? reset.Replace(", ", ", staticReset: ") : reset;
                // A FUNCTION-ID registers with its discriminator (kb/Work PB154 — §8.4.6.3's first paragraph:
                // a function-name is not a program-name, so CALL/CANCEL/SET TO ENTRY must not see it and a
                // FUNCTION reference must not see a program). Program lines stay byte-identical.
                string fnFlag = u.IsFunction ? ", isFunction: true" : "";
                // PROGRAM-ID/FUNCTION-ID … AS literal (§11.10.4 GR1 / §11.5.4 GR1): the unit registers under BOTH
                // names — Name for what MODULE-NAME reports (§15.65.4 r4), the externalized one for what
                // CALL/CANCEL/ENTRY resolve (§14.9.4.4 GR3b → §8.3.2.2). OMITTED when they coincide, so every
                // AS-less unit's Register line stays byte-identical (kb/Work PB303).
                string extName = string.Equals(u.ExternalizedName, u.Name, StringComparison.Ordinal)
                    ? "" : $", externalizedName: {CsLiteral(u.ExternalizedName)}";
                w.Line($"ProgramRegistry.Register({CsLiteral(u.Path)}, {CsLiteral(u.Name)}, {parentPath}, "
                    + $"{CallEmitter.CallBool(u.Initial)}, {CallEmitter.CallBool(u.Common)}, {CallEmitter.CallBool(u.Recursive)}, {factory}{resetNamed}{argMeta}{fnFlag}{extName});");
            }
        w.Line();
        // The run-unit main is the first top-level PROGRAM unit (§8.3.1). A prototype precedes every other unit
        // (§10.6.2 SR1), so units[0] may be a prototype; a function/prototype-only module (a callable library —
        // the cross-assembly UDF-3 target) has no main and only exposes Register() for the sibling probe.
        // A PROGRAM prototype (§11.10.2 Format 2, kb/Work PB894) is not a program definition and has no body
        // (§10.6.2 SR4 f), so it is never the main — the same exclusion the Register loop above applies.
        var mainUnit = units.FirstOrDefault(u => u is { Parent: null, IsFunction: false, IsPrototype: false });
        using (w.Block("internal static class Program"))
        using (w.Block("private static void Main()"))
        {
            w.Line("ProgramRegistry.Reset();");
            if (anyFiles) w.Line($"{RuntimeApi.FileInit()};");
            w.Line("__CobolModule.Register();");
            if (mainUnit is not null)
            {
                // The run-unit TERMINATION surface is owned by the runtime's RunMain boundary
                // (ProgramTable.RunMain): the STOP-status flush (§14.9.42.4 GR5, via the RunUnit.ExitStatus setter),
                // the §14.6.12 abnormal-termination diagnostic + nonzero exit on a fatal EC, and the §14.6.11
                // implicit CLOSE of ALL run-unit connectors. Runtime-side so each applies to the WHOLE run unit —
                // incl. a separately-compiled CALLed module whose EC/file descriptors this compilation group never
                // saw — not just this group (SSOT §18.16 keeps the wrapper scaffolding-free). The entry wrapper only
                // catches StopRun, the normal STOP RUN / main-program-GOBACK unwind boundary.
                w.Line($"try {{ ProgramRegistry.RunMain({CsLiteral(mainUnit.Path)}); }}");
                w.Line("catch (StopRun) { }");
            }
        }
    }
}
