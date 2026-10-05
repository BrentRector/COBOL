// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Binding.Bound;
using CobolNet.CodeGen.Emit;

namespace CobolNet.CodeGen;

using static CobolNet.CodeGen.Emit.EmitText;

/// <summary>
/// The data-pointer statement emitters (Phase-4b increment 2 — ISO §14.9.39 Formats 7/10, §14.9.3, §14.9.15;
/// the PHASE4_RECONCILIATION "M2-DATA-5 / M2-PROC-5 — increment 2" design; P7 Step 9d — a real collaborator over
/// the per-unit <see cref="EmitContext"/>). Every pointer VALUE renders onto the ONE <c>ManagedPointer</c>
/// carrier; every runtime rule lives in <c>CobolPtr</c> (Deref/UpBy/Allocate/Free) — the emitters only wire
/// places to helpers, through the <see cref="RuntimeApi"/> façade.
/// </summary>
internal sealed class PtrEmitter(EmitContext ctx, NumericRenderer num, EcState ecState, EcEmitter ec)
{
    /// <summary>The C# expression for an <c>ADDRESS OF identifier</c> value (ISO §8.4.3.11 GR1): a BASED
    /// item's value IS its implicit data-address pointer (§8.6.5 :8791); a cell-forced record renders a
    /// window pointer at the item's class offset; an EXTERNAL record's cell is the run-unit
    /// <c>ExternalStore</c> cell (the increment-2 unification — no special case). The bind pass guaranteed
    /// cell-backing, so the fall-through is a loud guard, never silence.</summary>
    public string AddressOfText(BoundAddressOf a)
    {
        var item = a.Item;
        DataItem root = item;
        while (root.Parent is { } p) root = p;
        if (root.Class is not { } cls)
            return LoudValue("ManagedPointer", $"ADDRESS OF '{item.CobolName}' without cell storage");
        // A SUBSCRIPTED operand addresses the OCCURRENCE (§8.4.3.11 GR1): the class offset plus the binder's
        // occurrence displacement — the occurrences lie end-to-end in the ONE cell image.
        string off = a.OccursDisplacement is { } disp ? $"{item.ClassOffset} + {disp}" : $"{item.ClassOffset}";
        // A REFERENCE-MODIFIED operand's address is its leftmost position's (§8.4.3.11.4 GR1 over §8.4.3.3.4 GR5): the
        // checked zero-based position, times the storage width of a position (RefModPlace.PositionBits — a bit
        // position is a bit, so a bit item's byte displacement divides by eight; its SR4 b) alignment proof makes
        // that exact), from the bit of the first byte at which identifier-1 itself starts.
        if (a.RefMod is { } rm)
        {
            // The size the position is tested against is identifier-1's CURRENT one (§8.4.3.3.4 5)): for an
            // occurs-depending group that is the run-time extent a read of the reference measures (kb/Work PB1969).
            string size = rm.CurrentExtent is { } odo ? PlaceRenderer.CurrentPositionsExpr(odo)
                : rm.Positions is { } n ? n.ToString(System.Globalization.CultureInfo.InvariantCulture)
                : RuntimeApi.UnknownRefModSize;
            string pos = RuntimeApi.StrRefModStartOffset(RuntimeApi.RefModStart(rm.Spec.Start),
                RuntimeApi.RefModLength(rm.Spec.Length), size, rm.Spec.AllowZeroLength);
            off += rm is { UnitBits: 8, LeadBits: 0 }
                ? $" + (long){pos}"
                : $" + (({rm.LeadBits}L + (long){pos} * {rm.UnitBits}) / 8)";
        }
        if (cls.BasedPointerField is { } addr)
            // The based item itself reads its implicit pointer (§8.6.5 :8791); a SUBORDINATE's address is
            // the base displaced by its class offset (§8.4.3.11 GR1 — the address OF THE ITEM; the review's
            // ClassOffset-drop finding). UpBy's GR18 null trap is the right posture: taking the address of a
            // child of an unallocated based record has no address to take.
            return item.ClassOffset == 0 && ReferenceEquals(item, root) && a.OccursDisplacement is null && a.RefMod is null
                ? addr
                : RuntimeApi.PtrUpBy(addr, off);
        // Every other cell-backed class — ADDRESS-OF-forced or EXTERNAL — addresses the cell member the class
        // NAMES (RedefinesClass.BackingCellCsName): the unit's own per-instance cell, the EXTERNAL cell property
        // over ExternalStore (whose one seed composer the property already calls), or — in a contained program —
        // the ref-bridge to the container's cell (kb/Work PB1009). This used to look the class up in THIS unit's
        // tables, which a contained program's ADDRESS OF a GLOBAL name could never satisfy.
        if (cls.IsCellBacked)
            return $"ManagedPointer.At({cls.BackingCellCsName}, {off})";
        return LoudValue("ManagedPointer", $"ADDRESS OF '{item.CobolName}' — unrecognized cell backing");
    }

    /// <summary>⛔ THE ONE C# EXPRESSION FOR A §8.4.3.13 PROGRAM-ADDRESS-IDENTIFIER VALUE (kb/Work PB1021): the
    /// program named by literal-1 / program-prototype-name-1's externalized name, or by the run-time CONTENT of
    /// identifier-1 (§8.4.3.13.4 GR1a), located through the run-unit program table. GR4's miss is EC-PROGRAM-NOT-
    /// FOUND with the predefined NULL as the value; <paramref name="callArgument"/> selects the CALL argument's
    /// delivery (§14.9.4.4 GR3a — the CALL's own pre-transfer failure) over the ordinary-operand one (the fatal
    /// in-statement raise), and the checking state is this statement's compile-time TURN state.</summary>
    public string ProgramAddressText(BoundProgramAddress pa, bool callArgument)
    {
        string nameExpr = pa.NameLiteral is { } lit
            ? CsLiteral(lit)
            : NameImage(pa.NamePlace!);   // §8.4.3.13.4 GR1a — the identifier's content
        bool checkNotFound = ecState.Info?.Enabled.Any(e => e.Ec == "EC-PROGRAM-NOT-FOUND") == true;
        return $"ProgramRegistry.{(callArgument ? "EntryOfArgument" : "EntryOfOperand")}({nameExpr}, "
            + $"{(checkNotFound ? "true" : "false")})";
    }

    /// <summary>⛔ THE ONE C# STRING THAT IS identifier-1's CONTENT, for every address-identifier that LOCATES a unit by
    /// name (§8.4.3.12.4 GR1 a and §8.4.3.13.4 GR1 a — "the content of the data item referenced by identifier-1"; the
    /// vendor <c>TO ENTRY</c> identifier reads it the same way; kb/Work PB1417 + PB1452). It is the operand's character
    /// IMAGE (<see cref="OperandText.FieldImage"/>), never <see cref="PlaceRenderer.Read"/>: SR1 admits an alphanumeric
    /// or national GROUP item (§8.5.2.1), whose <c>Read</c> is its record struct and not a string — the ref-modified
    /// slice and an elementary item read as the same string either way.</summary>
    private static string NameImage(Place name) => OperandText.FieldImage(name);

    /// <summary>The C# value expression of an address-identifier OPERAND (§8.4.3.1.2 identifier Format 9), either
    /// arm — a <c>ManagedPointer</c> for the data arm, a <c>ProgramPointer</c> for the program arm.</summary>
    public string AddressOperandText(BoundAddressOperand a) =>
        a.Data is { } da ? AddressOfText(da) : ProgramAddressText(a.Program!, callArgument: false);

    /// <summary>ONE <c>ADDRESS OF data-name-1</c> receiver of a SET Format 7 (ISO §14.9.39.4 GR13 — "the
    /// address identified by identifier-6 is assigned to each based item referenced by data-name-1 in the order
    /// specified"): the based item's implicit data-address pointer (§8.6.5) takes the address VALUE — a
    /// snapshot, never live tracking.
    /// <para><paramref name="src"/> is the already-landed sender expression, rendered ONCE by
    /// <c>SetEmitter.EmitSetPointer</c> for the whole receiving list, because identifier-6 sits OUTSIDE the
    /// printed repetition. A <c>ManagedPointer.Null</c> sender is SR19's TO NULL, which disassociates the item
    /// (§13.18.5 GR2's initial state; kb/Work PB89).</para></summary>
    public void EmitSetAddressOfBased(DataItem based, string src)
    {
        if (based.Class?.BasedPointerField is not { } addr)
        {
            ctx.Writer.Line(LoudStmt($"SET ADDRESS OF '{based.CobolName}' — the based item has no pointer bridge "
                + $"({based.Class?.RejectReason ?? "unclassified"})"));
            return;
        }
        ctx.Writer.Line($"{addr} = {src};   // SET ADDRESS OF (ISO §14.9.39.4 F7 GR13 — a snapshot)");
    }

    /// <summary><c>SET pointer… {UP|DOWN} BY n</c> (ISO §14.9.39 Format 10): the amount evaluates ONCE, then
    /// each pointer moves by n bytes (GR20; character positions in this model). Every §14.9.39.4 rule over the
    /// statement lives in the runtime's <c>CobolPtr</c> — GR18's NULL trap, GR19's integrality test on the
    /// AMOUNT (EC-SIZE-ADDRESS) and GR20's range test on the RESULTING ADDRESS (EC-RANGE-PTR).
    /// <para>⛔ TWO lanes, not three, and the shape is <see cref="SetEmitter.LandAmount"/>'s by construction:
    /// a NATIVE-FLOAT amount keeps its <c>double</c> so the integrality test runs on the double, and every other
    /// amount arrives as its EXACT scaled <c>Int128</c> with its scale. The former third lane rendered
    /// <c>long __ptrBy = (long)(…)</c>, which WRAPPED the magnitude GR20 rejects — measured,
    /// <c>SET P UP BY 18446744073709551618</c> displaced by 2 and <c>SET P UP BY 1844674407370955162.0</c>
    /// raised GR19's condition for an amount that IS an integer (kb/Work PB465).</para></summary>
    public void EmitSetPointerUpDown(BoundSetPointerUpDown s)
    {
        var w = ctx.Writer;
        // The amount lands through the ONE landing (kb/Work PB84): `SET P UP BY A ** 2` — an SDIDI intermediate
        // under native arithmetic since PB69, and every STANDARD-DECIMAL expression — was `(long)(CobolDec)`.
        NumX x = num.Landed(NumericRenderer.DeU(num.Render(s.Amount, ReceiverContext.None)), ReceiverContext.None);
        string tmp = $"__ptrBy{ctx.Names.NextPtr()}";
        if (x.Real)
        {
            w.Line($"double {tmp} = ({x.Expr});");
            foreach (var t in s.Targets)
                w.Line(PlaceRenderer.Write(t, RuntimeApi.PtrUpByAmountReal(PlaceRenderer.Read(t), tmp, s.Down))
                    + "   // SET pointer UP/DOWN BY float (ISO §14.9.39.4 GR19/GR20)");
            return;
        }
        // DOWN is carried as a FLAG rather than a negated temp: the amount may sit at the carrier's own
        // boundary, where forming `-tmp` here is the overflow the guard is supposed to catch.
        w.Line($"Int128 {tmp} = (Int128)({x.Expr});");
        foreach (var t in s.Targets)
            w.Line(PlaceRenderer.Write(t, RuntimeApi.PtrUpByAmount(PlaceRenderer.Read(t), tmp, $"{x.Scale}", s.Down))
                + "   // SET pointer UP/DOWN BY (ISO §14.9.39.4 GR19/GR20)");
    }

    /// <summary>ALLOCATE (ISO §14.9.3). Form 1: a fresh cell of ⌈expr⌉ characters (GR1 — a fractional request
    /// rounds UP; GR2 — ≤0 yields NULL, no EC) delivered to RETURNING; INITIALIZED = binary-zero fill (GR6).
    /// Form 2: the BASED item's implicit pointer takes a cell of its template width (GR3/GR4b), RETURNING
    /// also set when present (GR4a); the form-2 INITIALIZED effect (GR7) is NOT emitted here — the binder
    /// lowers it to the spec's own INITIALIZE expansion (a <c>BoundInitialize</c> sequenced after this node),
    /// so it renders through the ONE INITIALIZE/MOVE path.</summary>
    public void EmitAllocate(BoundAllocate s)
    {
        var w = ctx.Writer;
        // GR6/GR8/GR9: INITIALIZED = binary zeros; else the OPTIONS INITIALIZE clause's specified-fill-
        // character when written (GR8/GR9 key on the CLAUSE, not on its section list — the allocated storage
        // belongs to no section; the CONFORMANCE.md GR8 determination); else space (content undefined —
        // space-filling is the conformant choice).
        string fill = s.Initialized ? "'\\0'" : OptionsFillCharLiteral();
        string na = $"__notAvail{ctx.Names.NextPtr()}";
        w.Line($"bool {na};");
        if (s.Based is { } based)
        {
            if (based.Class?.BasedPointerField is not { } addr || based.Class is not { } cls)
            {
                // Unreachable from a compiled program since PB151's bind-time COBOLNET1695 — the belt stays.
                w.Line(LoudStmt($"ALLOCATE '{based.CobolName}' — the based item has no pointer bridge "
                    + $"({based.Class?.RejectReason ?? "unclassified"})"));
                return;
            }
            w.Line($"{addr} = {RuntimeApi.PtrAllocate($"(System.Int128){cls.Width}", fill, na)};   // ALLOCATE based-item (ISO §14.9.3.4 GR3/GR4b)");
            if (s.Returning is { } ret2)
                w.Line(PlaceRenderer.Write(ret2, addr) + "   // GR4a — the RETURNING pointer also receives the address");
            EmitStorageNotAvail(w, na);   // GR5c — data-name-2 and the based address already hold NULL
            return;
        }
        NumX x = num.Landed(NumericRenderer.DeU(num.Render(s.Chars!, ReceiverContext.None)), ReceiverContext.None);   // kb/Work PB84
        // GR1's round-UP per lane (kb/Work PB151/PB142). §14.9.3.4 GR1 and §14.7.9.3 GR1 (RETRY n TIMES) are the
        // standard's ONLY two "rounded up to the next whole number" clauses, so the scaled lane's round-up is
        // NumericRenderer.AlignRoundedUp — the one place that rule is written — and not a second local ceiling.
        // The NATIVE-FLOAT lane stays inside CobolPtr.AllocateReal, which fuses the same ceiling with GR2's
        // ≤ 0 ⇒ NULL and the storage-not-available outcomes only ALLOCATE defines (the old `(long)(double)`
        // truncated 2.5 → 2, a silently undersized cell). Either way the size travels as the FULL Int128 — the
        // old `(long)` narrowing wrapped a 20-digit request into a small VALID allocation (the PB22 cast family).
        string alloc = x.Real
            ? RuntimeApi.PtrAllocateReal($"({x.Expr})", fill, na)
            : RuntimeApi.PtrAllocate($"(System.Int128)({NumericRenderer.AlignRoundedUp(x)})", fill, na);
        w.Line(PlaceRenderer.Write(s.Returning!, alloc)
            + "   // ALLOCATE n CHARACTERS (ISO §14.9.3.4 GR1/GR2/GR5" + (s.Initialized ? "/GR6" : "/GR8") + ")");
        EmitStorageNotAvail(w, na);
    }

    /// <summary>The GR5c raise: storage not available → the nonfatal EC-STORAGE-NOT-AVAIL reports through
    /// the checking-gated block (§14.6.13.1.4 — an unchecked nonfatal condition is not raised; the EmitFree
    /// EC-STORAGE-NOT-ALLOC pattern). GR5a/b need no code here — the runtime already returned NULL into
    /// every address the statement assigns.</summary>
    private void EmitStorageNotAvail(CodeWriter w, string na)
    {
        if (ecState.Info?.Enabled.Any(e => e.Ec == "EC-STORAGE-NOT-AVAIL") != true)
        {
            w.Line($"_ = {na};   // EC-STORAGE-NOT-AVAIL checking not enabled (§14.6.13.1.4 — not raised; the pointers hold NULL per §14.9.3.4 GR5a/b)");
            return;
        }
        using (w.Block($"if ({na})"))
        {
            ec.EmitConditionRaise("EC-STORAGE-NOT-AVAIL", "§14.9.3.4 GR5c — set to exist (nonfatal)");
        }
    }

    /// <summary>The OPTIONS INITIALIZE clause's fill character as a C# char literal (§14.9.3.4 GR8/GR9;
    /// kb/Work PB151's arm).
    /// <para>⛔ This USED to be the decode itself — two private methods, invisible to every other consumer, whose
    /// <c>raw[0]</c> fall-through silently accepted a fill literal §11.9.10.3 SR1 forbids and took its first
    /// character, and whose private §11.9.10.4 GR5 map spelled HIGH-VALUES as U+FFFF where the rest of the
    /// compiler then said U+00FF (kb/Work PB152). literal-1's byte is now decoded ONCE at bind time, where SR1 gets
    /// its diagnostic, and the figurative arms resolve through the compiler's single definition — all three
    /// initial-state arms reading one fact through <see cref="InitialStateBackground"/>.</para></summary>
    private string OptionsFillCharLiteral() => _background.AllocateFillLiteral();

    /// <summary>The shared §14.6.2.3.2 background resolver, held once rather than constructed per ALLOCATE:
    /// it caches the section-root sets it may need, and an emitter-lifetime field is the same lifetime
    /// <c>ValueInitializer</c> gives it.</summary>
    private readonly InitialStateBackground _background = new(ctx);

    /// <summary><c>SET program-pointer… TO ENTRY {literal | identifier}</c> (ISO §14.9.39 Format 9 + §8.4.3.13;
    /// P10 Step 7): resolve the named OUTERMOST program through the run-unit ProgramTable ONCE, assign the
    /// result to every target. Not locatable → GR4: the value is the NULL program address and
    /// EC-PROGRAM-NOT-FOUND is set to exist. The condition is FATAL (Table 13): with checking enabled it is raised
    /// through <see cref="EcEmitter.EmitConditionRaise"/>, and unless a declarative or WHEN phrase RESUMEs, the run
    /// unit terminates abnormally (§14.6.13.1.3 5)/7); kb/Work PB1549 — the outcome the CALL arm reaches). With
    /// checking off, §14.6.13.1.3 8) leaves continuation to the implementor; this one continues with the NULL
    /// value.</summary>
    public void EmitSetEntry(BoundSetEntry s)
    {
        var w = ctx.Writer;
        string nameExpr = s.NameLiteral is { } lit
            ? EmitText.CsLiteral(lit)
            : NameImage(s.NamePlace!);   // §8.4.3.13 GR1a — the identifier's value names the program
        int id = ctx.Names.NextPtr();
        string nf = $"__ppNf{id}";
        w.Line($"bool {nf};");
        w.Line($"var __ppE{id} = ProgramRegistry.EntryOf({nameExpr}, out {nf});   // SET … TO ENTRY (ISO §8.4.3.13.4 GR1/GR4)");
        foreach (var t in s.Targets)
            w.Line(PlaceRenderer.Write(t, $"__ppE{id}") + "   // SET program-pointer (ISO §14.9.39 Format 9)");
        bool checkNotFound = ecState.Info?.Enabled.Any(e => e.Ec == "EC-PROGRAM-NOT-FOUND") == true;
        if (checkNotFound)
        {
            using (w.Block($"if ({nf})"))
            {
                // The §15.32.3 r2 pair rides the ambient statement context (kb/Work R14).
                // Fatal (Table 13): unresumed, the run unit terminates abnormally (§14.6.13.1.3 5)/7)) — the same
                // outcome the CALL arm (ProgramRegistry.EntryOfOperand's throw) reaches (kb/Work PB1549).
                ec.EmitConditionRaise("EC-PROGRAM-NOT-FOUND", "§8.4.3.13.4 GR4 — set to exist");
            }
        }
        else
            w.Line($"_ = {nf};   // EC-PROGRAM-NOT-FOUND checking not enabled: fatal, so §14.6.13.1.3 8) leaves continuation to the implementor — this one continues with the GR4 NULL value");
    }

    /// <summary><c>SET function-pointer… TO ADDRESS OF FUNCTION {function-prototype-name-1 | identifier-1}</c>
    /// (ISO §14.9.39.2 Format 8 + §8.4.3.12; kb/Work PB452): resolve the named function through the run-unit
    /// ProgramTable ONCE (§8.4.3.12.4 GR2 — the externalized function-name), screen it, then assign to every
    /// receiver. The <see cref="EmitSetEntry"/> program twin, with ONE structural difference — Format 8 carries a
    /// SECOND screen:
    /// <list type="number">
    /// <item>§8.4.3.12.4 GR4 — not locatable: EC-FUNCTION-NOT-FOUND is set to exist and "the value of the
    /// address-identifier is the predefined address NULL". The value is DEFINED, so the store still happens, and
    /// the raise is checking-gated; both conditions are FATAL (Table 13), so a checked raise that is not RESUMEd
    /// terminates the run unit (§14.6.13.1.3 5)/7); kb/Work PB1549).</item>
    /// <item>§14.9.39.4 GR14 — located but of a different signature: EC-FUNCTION-PTR-INVALID, "no data items are
    /// changed, and the execution of the SET statement is terminated". ⛔ THE STORE-SKIP IS NOT CHECKING-GATED:
    /// the standard NAMES the outcome, so it holds whether or not the condition is being checked; only the EC
    /// RAISE is gated. (The two legs are deliberately opposite, and each follows its own rule's wording.)</item>
    /// </list></summary>
    public void EmitSetFunctionAddress(BoundSetFunctionAddress s)
    {
        var w = ctx.Writer;
        // §8.4.3.12.4 GR1: a) the CONTENT of identifier-1, resolved at statement time; b) function-prototype-name-1,
        // whose externalized function-name the binder already resolved to a compile-time constant.
        string nameExpr = s.NameLiteral is { } name
            ? CsLiteral(name)
            : NameImage(s.NamePlace!);
        int id = ctx.Names.NextPtr();
        string nf = $"__fpNf{id}";
        w.Line($"bool {nf};");
        w.Line($"var __fpA{id} = ProgramRegistry.FunctionAddressOf({nameExpr}, out {nf});   // ADDRESS OF FUNCTION (ISO §8.4.3.12.4 GR1/GR2)");

        // GR14's signature screen. ExpectedFormals < 0 means the receivers' prototype has no compile-time
        // signature (a §12.3.8.4 GR11 c) external-repository prototype): there is nothing for "the same
        // signature" to name, so the screen is not emitted rather than run against a guessed arity.
        bool screenSignature = s.ExpectedFormals >= 0;
        string bad = $"__fpBad{id}";
        if (screenSignature)
            w.Line($"bool {bad} = !ProgramRegistry.FunctionSignatureMatches(__fpA{id}, {s.ExpectedFormals});"
                + "   // §14.9.39.4 GR14 / §13.18.60.4 GR26");

        void EmitStores()
        {
            foreach (var t in s.Targets)
                w.Line(PlaceRenderer.Write(t, $"__fpA{id}") + "   // SET function-pointer (ISO §14.9.39.4 GR14)");
        }

        if (screenSignature)
        {
            using (w.Block($"if (!{bad})")) EmitStores();
            bool checkInvalid = ecState.Info?.Enabled.Any(e => e.Ec == "EC-FUNCTION-PTR-INVALID") == true;
            if (checkInvalid)
                using (w.Block("else"))
                {
                    // The §15.32.3 r2 pair rides the ambient statement context (kb/Work R14).
                    ec.EmitConditionRaise("EC-FUNCTION-PTR-INVALID", "§14.9.39.4 GR14 — set to exist (fatal)");
                }
        }
        else
            EmitStores();

        bool checkNotFound = ecState.Info?.Enabled.Any(e => e.Ec == "EC-FUNCTION-NOT-FOUND") == true;
        if (checkNotFound)
        {
            using (w.Block($"if ({nf})"))
            {
                ec.EmitConditionRaise("EC-FUNCTION-NOT-FOUND", "§8.4.3.12.4 GR4 — set to exist (fatal)");
            }
        }
        else
            w.Line($"_ = {nf};   // EC-FUNCTION-NOT-FOUND checking not enabled: fatal, so §14.6.13.1.3 8) leaves continuation to the implementor — this one continues with the GR4 NULL value");
    }

    /// <summary>FREE (ISO §14.9.15 GR1/GR2 — per operand, left to right): the helper runs the three-way;
    /// the nonfatal EC-STORAGE-NOT-ALLOC (GR1c) reports through the TurnState-gated status block ONLY when
    /// its checking is enabled (§14.6.13.1.4 — an unchecked nonfatal condition is not raised).</summary>
    public void EmitFree(BoundFree s)
    {
        var w = ctx.Writer;
        bool checkNotAlloc = ecState.Info?.Enabled.Any(e => e.Ec == "EC-STORAGE-NOT-ALLOC") == true;
        foreach (var op in s.Operands)
        {
            string na = $"__notAlloc{ctx.Names.NextPtr()}";
            w.Line($"bool {na};");
            w.Line(PlaceRenderer.Write(op, RuntimeApi.PtrFree(PlaceRenderer.Read(op), na)) + "   // FREE (ISO §14.9.15.4 GR1)");
            if (checkNotAlloc)
            {
                using (w.Block($"if ({na})"))
                {
                    // The §15.32.3 r2 pair rides the ambient statement context (kb/Work R14).
                    // §14.6.13.1.4 3)/4): the F3 selection runs; a nonfatal condition with no handler (or
                    // RESUME NEXT / a completed declarative) simply continues (the review finding — the
                    // status set alone never consulted the declarative model).
                    ec.EmitConditionRaise("EC-STORAGE-NOT-ALLOC", "§14.9.15.4 GR1c — nonfatal (§14.6.13.1.1)");
                }
            }
            else
                w.Line($"_ = {na};   // EC-STORAGE-NOT-ALLOC checking not enabled (§14.6.13.1.4 — not raised)");
        }
    }
}
