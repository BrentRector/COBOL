// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime.Tree;
using CobolNet.Editions.Diagnostics;
using CobolNet.Runtime;
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

        // ⛔ A FORMAL WHOSE STORAGE IS AN AREA IS LAID OVER ITS ARGUMENT'S CELL (kb/Work PB2087). ISO §14.2.3 GR8: "If the
        // argument is passed by reference, the activated runtime element operates as if the formal parameter occupies the
        // same storage area as the argument." A non-resident formal — a group, a formal another LINKAGE entry REDEFINES,
        // a formal whose ADDRESS OF is taken (CallBindLinkage) — is therefore described exactly as a BASED item is: its
        // class moves onto a cell through the ONE forcer, and the cell is the one its implicit data-address pointer
        // addresses, which the activation sets to the argument's area (CobolArgAdapt.Area). The pointer IS the formal's
        // carrier field, so the omitted-argument condition, the GLOBAL bridge and ADDRESS OF all read the one member.
        // A formal whose area the cell cannot carry keeps the image round trip at the boundary (CellCanCarry).
        // A METHOD's formals take the same rule (the method arm, kb/Work PB2087): the pointer is the formal's per-activation
        // data-address member (OoFormal.CarrierLocal), set at each activation to the argument's area.
        var areaFormals = _linkageFormals.Where(f => !f.CarrierResident).Select(f => (f.Item, Pointer: f.CarrierField))
            .Concat(OoBoundMethods.SelectMany(m => m.Binding?.Formals ?? []).Where(f => !f.CarrierResident)
                .Select(f => (f.Item, Pointer: f.CarrierLocal)));
        foreach (var (item, pointer) in areaFormals)
        {
            if (!CellCanCarry(item)) continue;
            if (ForceStringCanonical(item, "LINKAGE formal parameter") is not { } cls) continue;
            cls.BasedPointerField = pointer;
            _ptrBasedBridges.Add((cls.BackingCsName, cls.BackingCellCsName, pointer, cls.Width));
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
            if (AlreadyInACell(root)) continue;
            if (ForceStringCanonical(root, "ADDRESS OF target record") is not { } cls) continue;   // rejected → loud
            ClaimAddressableCell(cls);
        }

        // ⛔ AN ITEM PASSED BY REFERENCE LIVES IN A CELL (kb/Work PB2087, PB2089) — the activating half of §14.2.3 GR8.
        // An argument whose storage is a cell crosses with its AREA (CobolArg.Area: the cell and the offset the argument
        // begins at), and an area formal is laid over exactly those positions, so the two elements share ONE storage
        // area for the whole activation. EVERY item named as a BY REFERENCE operand is claimed by the same forcer ADDRESS
        // OF uses — a group, a character item AND a native numeric or pointer item alike, because the activating element
        // cannot know the formal's description: the activated element may lay a group, a REDEFINED or an addressed
        // formal over the argument, and then only a shared cell is "the same storage area" (a native field cannot hold
        // the characters a redefinition stores — kb/Work PB2089: with N native, `CALL "S" USING N N` against a REDEFINED
        // LK-1 and an elementary LK-2 gave LK-1 a private copy whose copy-back undid LK-2's store). Nothing else changes
        // storage, so a program that passes nothing by reference pays nothing. An operand whose area the cell cannot
        // carry, or that is ambiguous here (the CALL bind reports it), keeps its ordinary storage and crosses through its
        // carrier alone — an area formal then holds a copy, stored back at return.
        // A CLASS unit's statements live in its METHODS' procedure divisions, each resolved in its own method's scope
        // (§11.7.4 GR5) — the ADDRESS OF scan's rule (PtrScanAddressOfTargets); the INVOKE operands are scanned with the
        // CALL ones (the INVOKE arm of the same rule, kb/Work PB2087).
        bool IsUserFunction(string fn) => UserFunctionNames.Contains(fn) || UnitSelfName is { } self && CobolNames.Same(fn, self);
        var own = OoIsClassUnit
            ? OoBoundMethods.Where(m => m.Ctx?.procedureDivision() is not null)
                .SelectMany(m => ScanByReferenceOperands(m.Ctx!.procedureDivision(), IsUserFunction).Select(t => (t.Name, t.Qualifiers, t.Invoke, t.SlotsOnly, Method: (OoMethodSymbol?)m)))
            : program.procedureDivision() is { } opd
                ? ScanByReferenceOperands(opd, IsUserFunction).Select(t => (t.Name, t.Qualifiers, t.Invoke, t.SlotsOnly, Method: (OoMethodSymbol?)null))
                : [];
        var operands = own.Select(t => (t.Name, t.Qualifiers, t.Invoke, t.SlotsOnly, t.Method, Contained: false))
            .Concat(program.nestedProgram().SelectMany(n => ScanByReferenceOperands(n, IsUserFunction))
                .Select(t => (t.Name, t.Qualifiers, t.Invoke, t.SlotsOnly, Method: (OoMethodSymbol?)null, Contained: true)));
        foreach (var (name, quals, invoke, slotsOnly, method, contained) in operands)
        {
            if (ResolveByReferenceOperand(name, quals, method) is not { } hit) continue;
            // ⛔ A GROUP WHOSE VALUES RIDE MANAGED SLOTS CROSSES ONLY AS AN AREA (kb/Work PB1940): a strongly-typed group
            // with an object-reference or pointer leaf has no character image, so its BY CONTENT record (§14.2.3 GR9) is
            // a detached copy of its area and a CALL's RETURNING delivery (§14.6.5) is a store into its receiver's area.
            // Either needs the operand in a cell; every other BY CONTENT operand or RETURNING receiver crosses as a value.
            if (slotsOnly && !CobolNet.Compiler.Oo.OoClassTable.LeafCarried(hit)) continue;
            DataItem root = hit.Root;
            if (contained && !CallGlobalRoots.Contains(root)) continue;
            if (AlreadyInACell(root) || root.Section is EntrySection.Linkage || !CellCanCarry(root)) continue;
            // Object data never crosses an INVOKE BY REFERENCE (§14.9.23.3 SR10: "Identifier-3 shall not reference a data
            // item defined in the file or working-storage section of a factory or instance object"), so a bare one is
            // passed BY CONTENT (GR6 a) 2.) and needs no cell.
            if (invoke && OoIsObjectData(root)) continue;
            if (ForceStringCanonical(root, "BY REFERENCE operand") is { } cls) ClaimAddressableCell(cls);
        }

        // ⛔ THE ACTIVATED HALF FOR A RETURNING ITEM WHOSE VALUES RIDE MANAGED SLOTS (kb/Work PB1940): a strongly-typed
        // group with an object-reference or pointer leaf is delivered as its AREA (§14.6.5 — "the content of the data item
        // referenced by that RETURNING phrase"; CobolArgAdapt.StoreReturnArea), its characters and its references into the
        // receiver's area, so the item itself lives in a cell.
        if (!OoIsClassUnit && LinkageReturning is { } ret)
            ClaimSlotCarriedCell(ret, "RETURNING item");
    }

    /// <summary>Claim <paramref name="root"/> onto a cell when it is a group whose values ride managed slots (a
    /// strongly-typed group with an object-reference or pointer leaf), which a program activation carries only as a cell
    /// AREA (kb/Work PB1940): a program's RETURNING item, and a user-defined function's caller-side result temporary
    /// (§8.4.3.2.4 GR1 — the receiver of the function's RETURNING delivery, created as the function-identifier binds).</summary>
    internal void ClaimSlotCarriedCell(DataItem root, string what)
    {
        if (CobolNet.Compiler.Oo.OoClassTable.LeafCarried(root) && !AlreadyInACell(root) && CellCanCarry(root)
            && ForceStringCanonical(root, what) is { } cls)
            ClaimAddressableCell(cls);
    }

    /// <summary>True when <paramref name="root"/>'s storage is ALREADY a cell (or the pointer-routed window over one), so
    /// no claim may re-base it: a BASED item or an area formal (its implicit data-address pointer — §8.6.5), a record
    /// another claim already put on a per-instance cell, or an EXTERNAL record (the run-unit <c>ExternalStore</c> cell).</summary>
    private bool AlreadyInACell(DataItem root) =>
        root.IsBased
        || root.Class is { BasedPointerField: not null }
        || root.Class is { } existing && PtrAddressableCellOf.ContainsKey(existing)
        || root.Class is { Tier: RedefinesTier.StringCanonical } ext && CallExternalBackings.Any(b => b.BackingCsName == ext.BackingCsName);

    /// <summary>Record a forced class's per-instance cell. ⛔ THE CLASS NAMES ITS OWN CELL (kb/Work PB231): the field used
    /// to carry an ad-hoc <c>_cell_{NAME}</c> spelling that only this list knew, so a place builder could not reach the
    /// cell to address the area's MANAGED SLOTS. It is now <see cref="RedefinesClass.BackingCellCsName"/>, the ONE name
    /// every cell surface uses and <c>ReferenceResolver.BuildCellPath</c> resolves.</summary>
    private void ClaimAddressableCell(RedefinesClass cls)
    {
        string cell = cls.BackingCellCsName;
        _ptrAddressableCellOf[cls] = cell;
        _ptrAddressableBackings.Add((cls.BackingCsName, cell, cls.Canonical, cls.Width));
    }

    /// <summary>Resolve one scanned BY REFERENCE operand in the scope its statement binds in — the owning METHOD's
    /// (§11.7.4 GR5) when <paramref name="method"/> is set, else the unit's — or null when it names nothing or is
    /// ambiguous: the statement's bind reports both (§8.4.2.2.3 SR1); a storage claim never guesses between candidates.</summary>
    private DataItem? ResolveByReferenceOperand(string name, List<string> quals, OoMethodSymbol? method)
    {
        var scope = method is null ? Model.Scope.Program : new Model.Scope(method.DataScope);
        if (quals.Count == 0)
            return Symbols.TryResolveUnqualified(name, scope, out var candidates) && candidates.Count == 1 ? candidates[0] : null;
        var saved = ActiveMethodScope;
        ActiveMethodScope = method?.DataScope;
        try { return new ReferenceResolver(this).FindItem(name, quals); }
        finally { ActiveMethodScope = saved; }
    }

    /// <summary>The data-name heads of every operand an activation may pass BY REFERENCE under one parse subtree, each tagged
    /// with whether an INVOKE passes it (its object-data screen differs, §14.9.23.3 SR10): each INVOKE argument passed BY
    /// REFERENCE (§14.9.23.4 GR6 a) 1.), each
    /// CALL USING operand in a BY REFERENCE phrase or before any phrase (ISO §14.9.4.4 GR5: "Both the BY CONTENT and BY
    /// REFERENCE phrases are transitive across the parameters that follow them until another BY CONTENT or BY REFERENCE
    /// phrase is encountered"; GR9 a) 1.: "BY REFERENCE is assumed"), and each argument of a USER-DEFINED function
    /// (<paramref name="isUserFunction"/>), whose identifier argument is passed by reference whenever its formal is — a
    /// fact of the function's header, not of the reference, so every one is a candidate. Only an operand that IS one
    /// identifier is yielded (<see cref="OperandIdentifier"/>): a subscript, a reference modifier's operands or an
    /// arithmetic expression's terms are SENDING operands of the reference, never passed — and an expression argument
    /// is passed by content. An address-identifier is ADDRESS OF's own surface.
    /// <para>Also yielded, tagged <c>SlotsOnly</c> (kb/Work PB1940): each CALL USING operand passed BY CONTENT and each CALL
    /// RETURNING receiver — claimed only when it is a group whose values ride managed slots, which crosses only as an
    /// area.</para></summary>
    internal static IEnumerable<(string Name, List<string> Qualifiers, bool Invoke, bool SlotsOnly)> ScanByReferenceOperands(IParseTree root,
        Func<string, bool> isUserFunction)
    {
        foreach (var node in PtrDescendants(root))
        {
            // An INVOKE argument (§14.9.23.2): BY REFERENCE, or written with no BY phrase — which §14.9.23.4 GR6 a) 1.
            // passes BY REFERENCE whenever the formal is BY REFERENCE and the argument is an identifier.
            if (node is Core.InvokeUsingContext invokeUsing)
            {
                foreach (var ia in invokeUsing.invokeArgument())
                {
                    IParseTree? operand = ia.REFERENCE() is not null ? ia.dataReference()
                        : ia.CONTENT() is null && ia.VALUE() is null ? ia.arithmeticExpression() : null;
                    if (operand is not null && OperandIdentifier(operand) is { } ih) yield return (ih.Name, ih.Qualifiers, true, false);
                }
                continue;
            }
            if (node is Core.FunctionCallContext fc && fc.functionArgList() is { } fargs && fc.functionName() is { } fname
                && isUserFunction(fname.GetText()))
            {
                foreach (var farg in fargs.functionArgument())
                    if (OperandIdentifier(farg) is { } h) yield return (h.Name, h.Qualifiers, false, false);
                continue;
            }
            if (node is Core.CallReturningPhraseContext ret)
            {
                if (HeadOf(ret.dataReference()) is { } rh) yield return (rh.Name, rh.Qualifiers, false, true);
                continue;
            }
            if (node is not Core.CallUsingPhraseContext phrase) continue;
            // The phrase in force (§14.9.4.4 GR5 — transitive until the next BY phrase): null = BY VALUE.
            bool? byReference = true;
            foreach (var arg in phrase.callArgument())
            {
                IParseTree? operand;
                if (arg.callByReference() is { } r) { byReference = true; operand = r.dataReference(); }
                else if (arg.callByContent() is { } c) { byReference = false; operand = c.arithmeticExpression(); }
                else if (arg.callByValue() is not null) { byReference = null; operand = null; }
                else operand = byReference is null ? null : arg.arithmeticExpression();
                if (operand is not null && OperandIdentifier(operand) is { } h)
                    yield return (h.Name, h.Qualifiers, false, byReference is false);
            }
        }

        // The identifier an operand consists of, or null when the operand is anything more (an expression, a literal):
        // the OUTERMOST data reference under it, when its text is the whole operand's.
        static (string Name, List<string> Qualifiers)? OperandIdentifier(IParseTree operand) =>
            PtrDescendants(operand).Prepend(operand).OfType<Core.DataReferenceContext>().FirstOrDefault() is { } dref
            && dref.GetText() == operand.GetText()
                ? HeadOf(dref)
                : null;

        static (string Name, List<string> Qualifiers)? HeadOf(Core.DataReferenceContext dref)
        {
            if (dref.cobolWord() is not { } head) return null;
            var quals = new List<string>();
            foreach (var suffix in dref.dataReferenceSuffix())
                if (suffix.qualification() is { } q) quals.Add(q.cobolWord().GetText());
            return (head.GetText(), quals);
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
