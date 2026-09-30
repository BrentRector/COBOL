// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime.Tree;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

using CobolNet.Binding.Model;
using CobolNet.Compiler.Oo;

namespace CobolNet.Binding;

using Core = CobolParserCore;
using CobolNet.Compiler.Oo;

/// <summary>
/// The data-pointer half of the data binder (Phase-4b increment 2 — ISO §13.18.5 BASED / §8.4.3.11 ADDRESS OF;
/// the PHASE4_RECONCILIATION "M2-DATA-5 / M2-PROC-5 — increment 2" design). Both jobs are instances of the ONE
/// cell-backing mechanism (<see cref="ForceStringCanonical"/>, the factored EXTERNAL re-basing): a BASED 01/77
/// becomes a storage TEMPLATE whose backing is a pointer-deref bridge (<c>ref CobolPtr.Deref(__addr_X, w).Ref</c>
/// — every reference windows the ADDRESSED cell at the pointer's runtime offset); an ADDRESS-OF-taken item's
/// record moves onto a per-instance <see cref="CobolNet.Runtime.StorageCell"/> so a
/// <see cref="CobolNet.Runtime.CellPointer"/> can alias its storage with structural (§8.8.4.2) equality.
/// </summary>
public sealed partial class DataBinder
{
    /// <summary>The per-instance cell backings the emitter renders for ADDRESS-OF-taken records:
    /// <c>private readonly StorageCell {CellField} = new StorageCell {{ Ref = «ImageInitOf(Canonical)» }};</c> +
    /// <c>private ref string {Backing} =&gt; ref {CellField}.Ref;</c>. The seed honors the record's VALUE
    /// clauses exactly like the Tier-B stored-backing initializer (the emitter computes it via the ONE
    /// <c>FieldEmitter.ImageInitOf</c> — the based_pointer first-run lesson: a default image loses VALUE).
    /// (READ-ONLY view — P6 Step 5.)</summary>
    internal IReadOnlyList<(string Backing, string CellField, DataItem Canonical, int Width)> PtrAddressableBackings => _ptrAddressableBackings;
    private readonly List<(string Backing, string CellField, DataItem Canonical, int Width)> _ptrAddressableBackings = [];

    /// <summary>The BASED bridges the emitter renders: the implicit data-address pointer field
    /// (<c>private ManagedPointer {AddrField} = ManagedPointer.Null;</c> — initially NULL, §13.18.5 GR2) +
    /// the deref bridge (<c>private ref string {Backing} =&gt; ref CobolPtr.Deref({AddrField}, {Width}).Ref;</c>
    /// — GR3/GR4 loud at every reference). (READ-ONLY view — P6 Step 5.)</summary>
    internal IReadOnlyList<(string Backing, string CellProp, string AddrField, int Width)> PtrBasedBridges => _ptrBasedBridges;
    private readonly List<(string Backing, string CellProp, string AddrField, int Width)> _ptrBasedBridges = [];

    /// <summary>ADDRESS-OF-forced classes → their cell FIELD name (the emitter's
    /// <c>ManagedPointer.At({cell}, {offset})</c> source; bind-time validation checks membership).
    /// (READ-ONLY view — P6 Step 5.)</summary>
    internal IReadOnlyDictionary<RedefinesClass, string> PtrAddressableCellOf => _ptrAddressableCellOf;
    private readonly Dictionary<RedefinesClass, string> _ptrAddressableCellOf = [];

    /// <summary>⛔ WHO ENDS A CELL'S LIFE, ONE CLASSIFICATION (kb/Work PB1216). ISO §13.18.5.4 GR4 raises EC-BOUND-PTR for a
    /// reference through an address that is "not a valid address of storage", and §8.6.5 says the association "may cease
    /// to exist because the actual data no longer exists, as specified 8.6.4" — so every cell-backed record's storage
    /// duration (§8.6.4) decides which event ENDS the cell, and this is the one place the three are told apart:
    /// <list type="bullet">
    /// <item><see cref="CellLifetime.Static"/> — a RECURSIVE unit's static WORKING-STORAGE and a method's WORKING-STORAGE
    /// (<see cref="StaticAddressableCells"/>): one copy, re-seeded IN PLACE by <c>__ResetStatics</c> at a CANCEL, which
    /// ends the old life (<c>StorageCell.Reinitialize</c>).</item>
    /// <item><see cref="CellLifetime.Activation"/> — LOCAL-STORAGE and non-formal LINKAGE (a program's, a method's):
    /// "persists while that instance of the runtime element is in active state", so the cell ends at the activation's exit
    /// — inside <c>Call</c> for a program, in the method's <c>finally</c> (<c>OoEmitter.ActivationPointerSeeds</c>).</item>
    /// <item><see cref="CellLifetime.Instance"/> — a program's WORKING-STORAGE cell owned by its instance: an INITIAL
    /// program's items end at its activation's exit, any other program's at the CANCEL that drops the instance, both
    /// through the emitted <c>EndStorage</c> (<c>ICobolProgram.EndStorage</c>).</item>
    /// </list>
    /// A cell added to <see cref="PtrAddressableBackings"/> tomorrow is classified here and so ended by the matching
    /// event without an edit elsewhere; <c>CellLifetimeDriftTests</c> reads the generated program and fails if a cell
    /// field is named by none of them.</summary>
    internal CellLifetime LifetimeOfCell(string cellField, DataItem canonical) =>
        StaticAddressableCells.Contains(cellField) ? CellLifetime.Static
        : OoMethodScopedRoots.Contains(canonical) || LocalStorageRoots.Contains(canonical)
          || LinkageRoots.Contains(canonical) ? CellLifetime.Activation
        : CellLifetime.Instance;

    /// <summary>The cell fields of <see cref="PtrAddressableBackings"/> whose life ends with <paramref name="lifetime"/>'s
    /// event, in declaration order.</summary>
    internal IEnumerable<string> CellFieldsOf(CellLifetime lifetime) =>
        _ptrAddressableBackings.Where(b => LifetimeOfCell(b.CellField, b.Canonical) == lifetime).Select(b => b.CellField);

    /// <summary>The post-build data-pointer pass (runs beside <see cref="CallBindExternalAndGlobal"/> — the
    /// proven post-classification tier-overwrite seam): (1) every BASED root becomes a pointer-routed
    /// StringCanonical template; (2) every plain record named by a data-address-identifier (<c>ADDRESS OF x</c> — a SET sender or a CALL argument) is
    /// forced onto a per-instance cell so its address is takeable. The pre-scan is a parse-tree walk — the
    /// data pass must decide storage BEFORE any statement binds (emission shape is per-class).</summary>
    internal void PtrBindBasedAndAddressables(Core.ProgramUnitContext program)
    {
        foreach (var root in Roots.Where(r => r.IsBased))
        {
            // A rejected class keeps its RejectReason — and the rejection is a BIND-TIME diagnostic now
            // (kb/Work PB151): the bare continue left BasedPointerField null and every ALLOCATE/ADDRESS
            // reference crashed at RUN time on a program that compiled clean, while the EXTERNAL twin
            // (CallMakeExternal) always diagnosed the identical failure at bind — the two-arm shape. What the
            // shared byte cell still cannot carry is a national or pointer-class leaf; every numeric byte form
            // rides it (kb/Work PB164) and so does every boolean position, USAGE BIT packing included
            // (kb/Work PB231 — the interpolated RejectReason names the actual leaf).
            if (ForceStringCanonical(root, "BASED item") is not { } cls)
            {
                Edition.Error(DiagnosticCatalog.BasedRecordSubstrate,
                    $"BASED item '{root.CobolName}' has a subordinate the shared byte cell cannot carry "
                    + $"({root.Class?.RejectReason ?? "unclassified"}) — ALLOCATE/ADDRESS bridging for it "
                    + "is recognized but not yet implemented (kb/Work PB164; ISO §13.18.5 / §14.9.3)");
                continue;
            }
            string addr = NamingConvention.AddressCarrierName(root.CsName);
            cls.BasedPointerField = addr;
            _ptrBasedBridges.Add((cls.BackingCsName, cls.BackingCellCsName, addr, cls.Width));
        }

        // The addressed names: this unit's own procedure division(s), plus — kb/Work PB1009 — every CONTAINED
        // program's, restricted to this unit's GLOBAL names. §13.18.27.4 GR2 lets a contained program reference
        // a global name "without describing it again", and the storage it references is THIS unit's, so the
        // decision that the storage must be addressable belongs here, where the storage is declared. (A name the
        // contained program also declares locally shadows the global one; forcing the global's cell anyway is
        // harmless — it changes where the value lives, never what it is.)
        var targets = PtrScanAddressOfTargets(program).Select(t => (t.Name, t.Qualifiers, t.Method, Contained: false))
            .Concat(program.nestedProgram().SelectMany(PtrScanAddressOfSenders)
                .Select(t => (t.Name, t.Qualifiers, Method: (OoMethodSymbol?)null, Contained: true)));
        foreach (var (name, quals, method, contained) in targets)
        {
            // An unqualified head keeps the historical first-candidate pick (a duplicate-name mis-force is
            // loud-caught at the SET bind's cell check); a QUALIFIED head resolves through the ONE §8.4.2.2
            // qualification machinery so the RIGHT record is forced. Inside a METHOD both lookups run in the
            // method's own scope (§11.7.4 GR5 — a method-local name shadows object data and is invisible to the
            // unit-wide maps, which OoScopeSubtree emptied of it), exactly as the SET bind will resolve it.
            DataItem? hit = PtrResolveAddressOfTarget(name, quals, method);
            if (hit is null)
                continue;   // unresolved / ambiguous — the SET bind reports 0869 with the source text
            DataItem root = hit;
            while (root.Parent is { } p) root = p;
            if (contained && !CallGlobalRoots.Contains(root)) continue;   // not a global name — not this unit's to force
            if (root.IsBased) continue;                 // ADDRESS OF a based item reads its implicit pointer (§8.6.5)
            if (root.Class is { } existing && PtrAddressableCellOf.ContainsKey(existing)) continue;
            if (root.Class is { Tier: RedefinesTier.StringCanonical } ext
                && CallExternalBackings.Any(b => b.BackingCsName == ext.BackingCsName))
                continue;                               // EXTERNAL — already cell-backed (ExternalStore); At() takes it directly
            if (ForceStringCanonical(root, "ADDRESS OF target record") is not { } cls) continue;   // rejected → loud
            // ⛔ THE CLASS NAMES ITS OWN CELL (kb/Work PB231): the field used to carry an ad-hoc `_cell_{NAME}`
            // spelling that only this list knew, so a place builder could not reach the cell to address the
            // area's MANAGED SLOTS. It is now RedefinesClass.BackingCellCsName, the ONE name every cell surface
            // uses and ReferenceResolver.BuildCellPath resolves.
            string cell = cls.BackingCellCsName;
            _ptrAddressableCellOf[cls] = cell;
            _ptrAddressableBackings.Add((cls.BackingCsName, cell, cls.Canonical, cls.Width));
        }

        // ⛔ A METHOD's cell-backed data takes its STORAGE DURATION from its section (kb/Work PB956): a method
        // WORKING-STORAGE record is ONE per-class copy persisting across activations (§8.6.4 static items / OO deep-dive D3 —
        // the static channel every other method-WS root rides), so its implicit pointer or its cell is a STATIC
        // member. A LOCAL-STORAGE or LINKAGE record is per ACTIVATION (§8.6.4; §8.6.5 — a based entry's implicit
        // pointer lives as long as the entry's storage) — OoEmitter.EmitMethod re-seeds its member on entry and
        // restores the activator's on exit, so a recursive activation never sees its caller's address.
        foreach (var root in OoMethodScopedRoots)
            if (OoRootOwner.TryGetValue(root, out var owner) && owner.Binding!.StaticRoots.Contains(root)
                && root.Class is { IsCellBacked: true } c && ReferenceEquals(c.Canonical, root))
            {
                if (c.BasedPointerField is { } bp) _staticBasedBridgeAddrs.Add(bp);
                else if (PtrAddressableCellOf.ContainsKey(c)) _staticAddressableCells.Add(c.BackingCellCsName);
            }
    }

    /// <summary>Resolve one scanned <c>ADDRESS OF</c> head in the scope its statement will bind in: the owning
    /// METHOD's (§11.7.4 GR5) when <paramref name="method"/> is set, else the unit's.</summary>
    private DataItem? PtrResolveAddressOfTarget(string name, List<string> quals, OoMethodSymbol? method)
    {
        var scope = method is null ? Model.Scope.Program : new Model.Scope(method.DataScope);
        if (quals.Count == 0)
            return Symbols.TryResolveUnqualified(name, scope, out var candidates) && candidates.Count > 0 ? candidates[0] : null;
        var saved = ActiveMethodScope;
        ActiveMethodScope = method?.DataScope;
        try { return new ReferenceResolver(this).FindItem(name, quals); }
        finally { ActiveMethodScope = saved; }
    }

    /// <summary>Collect the data-names taken by every §8.4.3.11 DATA-ADDRESS-IDENTIFIER in the procedure
    /// division. The head name + its OF/IN qualifiers are yielded for EVERY operand shape (a subscripted operand
    /// forces the same containing record — the occurrence displacement is a bind-time offset over the ONE cell,
    /// never separate storage).
    /// <para>⛔ IT WALKS THE IDENTIFIER'S OWN RULE, NOT A STATEMENT (kb/Work PB239). It used to recognize only a
    /// SET Format-7 sender, so the day a second surface took the identifier — the CALL argument §14.9.4.3
    /// SR3/SR4 name — its record would have been left off cell storage and the bind would have refused it.
    /// Every surface now spells the identifier through the ONE <c>dataAddressIdentifier</c> rule, so every one
    /// of them is forced here by construction. The receiving <c>ADDRESS OF data-name-1</c> of SET Format 7 is a
    /// different rule (<c>setAddressReceiver</c>) and is never forced: it names a BASED item (kb/Work PB450).
    /// </para></summary>
    private IEnumerable<(string Name, List<string> Qualifiers, OoMethodSymbol? Method)> PtrScanAddressOfTargets(Core.ProgramUnitContext program)
    {
        // A CLASS unit's statements live in its METHODS' procedure divisions — the synthetic unit OoDriver
        // binds carries the data divisions only — so each method body is scanned and its targets carry the
        // method whose scope resolves them (kb/Work PB956).
        if (OoIsClassUnit)
        {
            foreach (var m in OoBoundMethods)
                if (m.Ctx?.procedureDivision() is { } mpd)
                    foreach (var (n, q) in PtrScanAddressOfSenders(mpd))
                        yield return (n, q, m);
            yield break;
        }
        if (program.procedureDivision() is not { } pd) yield break;
        foreach (var (n, q) in PtrScanAddressOfSenders(pd))
            yield return (n, q, null);
    }

    /// <summary>The data-address-identifier heads under one parse subtree (a procedure division or a whole
    /// contained program) — shared with <c>DataBinder.CallBindLinkage</c>'s addressed-formal scan (kb/Work PB1019).</summary>
    internal static IEnumerable<(string Name, List<string> Qualifiers)> PtrScanAddressOfSenders(IParseTree pd)
    {
        foreach (var ctx in PtrDescendants(pd))
            if (ctx is Core.DataAddressIdentifierContext { } dai && dai.dataReference() is { } target)
            {
                if (target.cobolWord() is not { } head) continue;
                var quals = new List<string>();
                foreach (var suffix in target.dataReferenceSuffix())
                    if (suffix.qualification() is { } q) quals.Add(q.cobolWord().GetText());
                yield return (head.GetText(), quals);
            }
    }

    private static IEnumerable<IParseTree> PtrDescendants(IParseTree node)
    {
        for (int i = 0; i < node.ChildCount; i++)
        {
            var child = node.GetChild(i);
            yield return child;
            foreach (var sub in PtrDescendants(child)) yield return sub;
        }
    }
}
