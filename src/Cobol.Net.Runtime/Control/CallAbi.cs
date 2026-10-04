// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>How a CALL argument is passed (ISO §14.9.4 / §14.2.3 GR8–10).</summary>
public enum CobolPassMode
{
    /// <summary>BY REFERENCE — the callee operates as if the formal occupies the caller's storage (§14.2.3 GR8).</summary>
    Reference,
    /// <summary>BY CONTENT — a copy allocated at CALL initiation, then treated as if by reference (§14.2.3 GR9).</summary>
    Content,
    /// <summary>BY VALUE — a converted value copy (§14.2.3 GR10; COBOL-2002+).</summary>
    Value,
}

/// <summary>
/// One CALL argument crossing the opaque ABI (design D2): the pass mode, the carrier, and the DESCRIPTION of
/// the storage that carrier holds — the one fact the callee-side adapters need to reinterpret a native numeric
/// cell through a differently-scaled or character-shaped formal (the D5-sanctioned category boundary).
/// <para>⛔ THE DESCRIPTION IS A WHOLE <see cref="NumProfile"/>, NOT A (digits, scale) PAIR (kb/Work PB873). A
/// native cell holds a VALUE; the storage it stands for has a REPRESENTATION — the operational sign and where it
/// sits (§13.18.52), the usage's byte form (§13.18.60.4) — and a formal that sees the argument as characters
/// sees that representation: §14.2.3 GR8 "operates as if the formal parameter occupies the same storage area as
/// the argument", and GR9's first branch moves the argument into its record "without conversion". The pair this
/// replaced could spell only the digit run, so an image built from it was always UNSIGNED ZONED: <c>-12.34</c>
/// in a <c>PIC S9(4)V99</c> argument reached an image-carried formal as <c>00123D</c> (+12.34) where the storage
/// holds <c>00123M</c>, and a <c>COMP-5</c> argument reached a <c>PIC X(2)</c> formal as a zoned digit run
/// instead of its two binary bytes. Carrying the profile makes every attribute the image needs travel with it —
/// the next one (a national form, a new usage) is automatic.</para>
/// <para>When the activating element has performed GR9/GR10's COMPUTE into a record of the FORMAL's
/// description (<see cref="CobolArgAdapt.LandForFormal{T}"/>), the description it carries from then on IS the
/// formal's — GR11 resolves every reference to data-name-1 "in accordance with their description in the linkage
/// section" — so the one field answers both of GR9's branches.</para>
/// </summary>
/// <param name="Mode">The pass mode (ISO §14.9.4.4 GR5 transitivity resolved at bind time).</param>
/// <param name="Carrier">The storage carrier (<see cref="ManagedPointer.Null"/> for OMITTED, GR11).</param>
/// <param name="Num">The numeric description of the carried storage; null for character, group, pointer,
/// object-reference and index storage, whose carrier needs no numeric reinterpretation.</param>
/// <param name="Layout">⛔ The GROUP description of the carried storage (kb/Work PB965): its §8.5.1.12 layout
/// (the triples <see cref="CobolVarGroup.CorrespondingSpans"/> reads), stated for a group that has a table or a
/// variable-length member — the one fact a boundary needs to put a FIXED-length group opposite a
/// VARIABLE-length one. §8.5.1.12.2's correspondence is a relation between the two groups' layouts and the two
/// sides of a CALL are compiled apart, so the layout travels with the storage exactly as <see cref="Num"/>
/// does; null for every other carrier, and for a group with neither (whose correspondence with any
/// variable-length group fails, which the null answers).</param>
/// <param name="Length">⛔ The CHARACTER LENGTH of the carried storage (kb/Work PB1040): the width of the string
/// image a text-carried item always holds, stated for an item whose length is FIXED at compile time. §14.8.2.3.2
/// rule 1 and §14.8.3.3 give a conforming pair "the same … PICTURE" and so the same length, and a string carrier
/// whose image is not exactly its item's width is a corrupt item — so the length travels with the carrier exactly as
/// <see cref="Num"/> and <see cref="Layout"/> do, for the activating element to CHECK a result against (§14.9.4.4
/// GR3 d)) and for the delivery to FIT one to (the unchecked store). <see cref="Unstated"/> for every item with no
/// fixed character length: a native cell, a pointer, a DYNAMIC LENGTH or ANY LENGTH item (§8.5.1.10; §14.8.3.3 rule
/// 5 — an ANY LENGTH sender "matches" whatever length the receiver has), and a variable-length group, whose
/// description is its <see cref="Layout"/>. Only a RETURNING item states it today: a BY REFERENCE argument is fitted
/// by the formal's own window on the activated side (kb/Work PB165 owns the argument-side check on the dynamic
/// lane).</param>
/// <remarks>⛔ A RETURNING ITEM CROSSES AS A <see cref="CobolArg"/> TOO (kb/Work PB962 + PB965). Its storage is
/// the activating element's (§14.2.3 GR6 NOTE 1), so the delivery performed at the activated element's return
/// needs the RECEIVER's description as much as an argument's adapter needs the argument's — a group receiver's
/// layout to meet a compatible group of a different shape (§14.8.3.2), a numeric receiver's profile. A bare
/// carrier could state neither.</remarks>
public readonly record struct CobolArg(CobolPassMode Mode, ManagedPointer Carrier, NumProfile? Num, int[]? Layout = null,
    int Length = CobolArg.Unstated)
{
    /// <summary><see cref="Length"/> of an item with no fixed character length.</summary>
    public const int Unstated = -1;

    /// <summary>The description facts of this item that a dynamic CALL can compare with the activated unit's own
    /// (<see cref="BoundaryItem"/>).</summary>
    public BoundaryItem Item => new(Num, Length);

    /// <summary>The carried storage's digit count; 0 when <see cref="Num"/> is null.</summary>
    public int Digits => Num?.Digits ?? 0;

    /// <summary>The carried storage's net scale (§13.18.40 — may be negative); 0 when <see cref="Num"/> is null.</summary>
    public int Scale => Num?.FractionScale ?? 0;
}

/// <summary>
/// ⛔ THE DESCRIPTION OF A BOUNDARY ITEM THAT BOTH SIDES OF A DYNAMIC CALL CAN STATE (kb/Work PB1040): the two facts
/// <see cref="CobolArg"/> carries for the activating element's storage (<see cref="CobolArg.Num"/> — a numeric
/// item's whole PICTURE, SIGN and USAGE — and <see cref="CobolArg.Length"/> — a text-carried item's character
/// length) and the same two facts an activated unit REGISTERS for its own RETURNING item
/// (<see cref="ProgramTable.Register"/>), so §14.9.4.4 GR3 d) has something to compare at call initiation when the
/// callee is located by name at run time. ONE rule of "do these two descriptions conform", written over the pair
/// of facts and nowhere else; kb/Work PB165's per-formal registry is the next consumer.
/// </summary>
/// <param name="Num">The numeric item's profile; null for an item that is not numeric (character, group, pointer).</param>
/// <param name="Length">The fixed character length of a text-carried item; <see cref="CobolArg.Unstated"/> otherwise.</param>
public readonly record struct BoundaryItem(NumProfile? Num, int Length = CobolArg.Unstated)
{
    /// <summary>True when the item states anything at all — a native cell of a category with no profile, a pointer, a
    /// DYNAMIC LENGTH item and an ANY LENGTH item state nothing, and nothing is comparable against them.</summary>
    public bool IsStated => Num is not null || Length != CobolArg.Unstated;

    /// <summary>The item in the words of an EC-PROGRAM-ARG-MISMATCH message.</summary>
    public string Describe() =>
        (Num, Length) switch
        {
            ({ } n, var len) when len != CobolArg.Unstated =>
                $"numeric, {n.Digits} digit(s), scale {n.FractionScale}, {len} character(s)",
            ({ } n, _) => $"numeric, {n.Digits} digit(s), scale {n.FractionScale}",
            (_, var len) when len != CobolArg.Unstated => $"{len} character(s)",
            _ => "no stated description",
        };

    /// <summary>Whether a SENDING item and a RECEIVING item conform (§14.8.3.3: "the same … PICTURE, SIGN, and
    /// USAGE clauses"; §14.8.2.3.2 rule 1: "the same length"), judged over what both state: an item with a numeric
    /// profile never conforms to one without (category), two profiles conform when they state one PICTURE, SIGN and
    /// USAGE (<see cref="NumProfile.ConformsTo"/>), and two character lengths conform when equal. An item that states
    /// nothing is not compared — its conformance is a compile-time fact or another registry's.</summary>
    public bool Conforms(in BoundaryItem other) =>
        !IsStated || !other.IsStated
        || ((Num is null) == (other.Num is null)
            && (Num is not { } mine || mine.ConformsTo(other.Num!.Value))
            && (Length == CobolArg.Unstated || other.Length == CobolArg.Unstated || Length == other.Length));
}

/// <summary>
/// ⛔ THE PREDEFINED NULL WRITTEN AS AN ARGUMENT (kb/Work PB1630) — the carrier of <c>USING [BY CONTENT | BY VALUE]
/// NULL</c> on a CALL or a user-defined function activation. NULL is an IDENTIFIER, not a literal (§8.4.3.1.2
/// Format 8, predefined-address; Format 6's NULL object reference), and §8.4.3.10.3 SR1 a) admits it "as an argument
/// in a program-prototype format CALL statement, a function-prototype format function activation, or a method
/// invocation". Its category is decided by "the associated data item's class" — the FORMAL's — which the activating
/// element does not know when §12.3.8.4 GR10 c) takes the details from an external repository. So the carrier holds
/// no storage of any type: the formal's slot adapter (<see cref="CobolArgAdapt.Slot{T}"/> /
/// <see cref="CobolArgAdapt.SlotValue{T}"/>) supplies a fresh cell holding the null of its own carrier, and every
/// other adapter's type switch refuses it as EC-PROGRAM-ARG-MISMATCH (§14.8.2.3.3: only a formal of class pointer or
/// object reference takes its argument by a SET, and §14.9.25.3 SR1 bars class pointer and object from a MOVE).
/// <para>It is a PRESENT argument (<see cref="ManagedPointer.IsNull"/> false): <see cref="ManagedPointer.Null"/> as a
/// carrier is the OMITTED argument (§14.9.4.4 GR11), which is a different fact — a NULL argument makes the
/// omitted-argument condition false and the formal's value the null address.</para>
/// </summary>
public sealed class PredefinedNullArgument : ManagedPointer
{
    /// <summary>The one instance; the carrier is stateless and never written through.</summary>
    public static readonly PredefinedNullArgument Instance = new();

    private PredefinedNullArgument() { }
}

/// <summary>
/// The uniform program ABI every compiled program class implements (design D2 — the typed analog of the
/// rejected byte <c>Entry(ManagedPointer[])</c>). <see cref="Call"/> activates the program as a CALLed program
/// (positional formal mapping, §14.2.3 GR2); <see cref="Activate"/> runs it as the run-unit main program;
/// <see cref="CloseFiles"/> closes this program's file connectors (CANCEL §14.9.5 GR9 implicit CLOSE).
/// </summary>
public interface ICobolProgram : INonfatalSelector
{
    /// <summary>Activate as a CALLed program: map <paramref name="args"/> positionally onto the LINKAGE formals
    /// (ISO §14.2.3 GR2 — correspondence is positional, never by name), run, and deliver the RETURNING value (if
    /// any) through <paramref name="returning"/> (§14.2.3 GR7).</summary>
    void Call(CobolArg[] args, CobolArg? returning);

    /// <summary>Activate as the run-unit's main program (no arguments; LINKAGE unbound, ISO §13.7.4 GR3).</summary>
    void Activate();

    /// <summary>Register this element's external descriptions and run the §14.8.4 conformance check
    /// (ISO §14.9.4.4 GR3e). It is an ACTIVATION-ATTEMPT step, not part of the activated element's execution:
    /// GR3e precedes GR3g's "control is transferred to the called program", and a violation makes "the program
    /// call ... not successful" — so the activation boundary calls it BEFORE <see cref="Call"/>, which is what
    /// lets the boundary mark everything escaping <see cref="Call"/> as post-transfer (GR3i). A unit with no
    /// external record or file connector, or with no enabling EC-EXTERNAL &gt;&gt;TURN in the group, emits no
    /// override and takes this no-op (zero scaffolding).</summary>
    void DescribeExternals() { }

    /// <summary>End the life of every data-pointer cell this INSTANCE owns, for <paramref name="reason"/> (ISO §8.6.4 /
    /// §8.6.5; kb/Work PB1216): the activation boundary calls it when an instance is DISCARDED — at the return of an
    /// INITIAL or RECURSIVE activation, whose fresh instance dies with it, and at a CANCEL of the program — so every
    /// pointer taken into that storage is thereafter "not a valid address of storage" (§13.18.5.4 GR4). A unit with no
    /// instance-owned ADDRESS-OF cell emits no override and takes this no-op (zero scaffolding); a STATIC cell is
    /// ended by the unit's <c>__ResetStatics</c> (<see cref="StorageCell.Reinitialize"/>), and a LOCAL-STORAGE cell by
    /// the end of each activation inside <see cref="Call"/> itself.</summary>
    void EndStorage(StorageEnd reason) { }

    /// <summary>Close every file connector this program owns (CANCEL GR9 / run-unit termination §14.6.11).</summary>
    void CloseFiles();

    /// <summary>Select and execute this element's USE declarative for a NONFATAL exception condition raised at a
    /// RUNTIME site (ISO §14.6.13.1.4 #3 — "If there is an applicable USE statement in the source unit that
    /// specifies the exception-name associated with the exception condition … the associated declarative is
    /// executed"), returning the <see cref="Exceptions.DispatchResult"/> protocol: <c>Normal</c> completed normally, <c>ResumeNext</c> RESUME AT NEXT
    /// STATEMENT, <c>NoHandler</c> no qualifying declarative, a value <c>≥ 0</c> RESUME AT that pc.
    /// <para>This is the §14.9.49.4 GR3 selection seen from OUTSIDE the generated code. A raise the emitter can
    /// place a dispatch AT (RAISE, an I-O status, an ON OVERFLOW-less STRING) never needs it; a condition detected
    /// INSIDE the runtime — an untranslatable code unit deep in an expression, a dynamic table growing under a
    /// receiving subscript, an inverted THRU range, a dynamic-length resize — has no statement-level node to hang
    /// a dispatch on, and before this channel existed it set the last exception status and returned, so its
    /// declarative never ran (kb/Work PB367b).</para>
    /// <para>The DEFAULT is "no qualifying declarative": an element with no Format-3 selection machinery answers
    /// it without emitting anything, which is what keeps the zero-scaffolding invariant true for every program
    /// that declares no USE AFTER EXCEPTION CONDITION declarative and every non-COBOL implementation of this
    /// interface.</para></summary>
    int INonfatalSelector.NonfatalDispatch(string ec) => Exceptions.DispatchResult.NoHandler;
}

/// <summary>The §14.6.13.1.4 #3 selection of ONE runtime element, seen from a RUNTIME raise site: the activation
/// now executing installs its selector in <see cref="Exceptions.ExceptionState.NonfatalDispatcher"/> and restores
/// its activator's on return, so a nonfatal condition detected inside the runtime selects over the declaratives of
/// the source element containing the raising statement (§14.9.49.4 GR4 a)) and never its activator's. A PROGRAM
/// is its own selector (<see cref="ICobolProgram"/>, installed by <c>ProgramTable</c>); an OO METHOD is a runtime
/// element too (§15.65.4 r5 — "an INVOKE statement" activates one) and installs a <see cref="NonfatalSelectorFn"/>
/// over its own selection — or <see cref="NonfatalSelectorFn.None"/> when it declares none (kb/Work PB1010).</summary>
public interface INonfatalSelector
{
    /// <summary>The <see cref="Exceptions.DispatchResult"/> protocol: <c>Normal</c> completed normally, <c>ResumeNext</c> RESUME AT NEXT STATEMENT,
    /// <c>NoHandler</c> no qualifying declarative, a value <c>≥ 0</c> RESUME AT that pc (see <see cref="ICobolProgram"/>).</summary>
    int NonfatalDispatch(string ec);
}

/// <summary>A method activation's <see cref="INonfatalSelector"/> — the method's own generated selection (a local
/// function capturing the activation's data) behind the runtime's one interface (kb/Work PB1010).</summary>
public sealed class NonfatalSelectorFn(Func<string, int> select) : INonfatalSelector
{
    /// <summary>The selector of an element that declares no USE procedures and no exception-checking PERFORM:
    /// "no qualifying declarative", shared, allocation-free.</summary>
    public static readonly NonfatalSelectorFn None = new(static _ => Exceptions.DispatchResult.NoHandler);

    public int NonfatalDispatch(string ec) => select(ec);
}

/// <summary>
/// Callee-side positional argument adapters (design D2/D5): each maps <c>args[i]</c> onto a formal parameter's
/// carrier shape. Same-shape carriers pass through untouched (fully typed aliasing); a category mismatch (e.g. a
/// caller <c>PIC X(4)</c> viewed by the callee as <c>PIC 9(4)</c>) builds a CONVERTING view over the caller's
/// storage — the one sanctioned transient-character boundary (design D5; legal COBOL exercised by NIST), never a
/// persisted byte image.
/// <para>TWO DIFFERENT FAILURES, TWO DIFFERENT ANSWERS (kb/Work PB615). A missing / OMITTED argument yields the
/// omitted carrier (<see cref="Omitted{T}"/>): §14.9.4.4 GR11 makes the omitted-argument condition true, and a
/// reference reads the type's benign empty value — the GR12 raise is the REFERENCE's, through
/// <see cref="OmittedFormal"/> (kb/Work PB971), never the carrier's. A SUPPLIED argument whose carrier the formal's adapter cannot read is NOT omitted — it is a
/// violation of the §14.8.2 conformance rules, and §14.9.4.4 GR3 d) answers it with "the program call is not successful"
/// (EC-PROGRAM-ARG-MISMATCH): <see cref="Unreadable{T}"/> fails the activation LOUD, never a silent zero
/// indistinguishable from an omitted argument.</para>
/// </summary>
public static class CobolArgAdapt
{
    /// <summary>True when argument <paramref name="i"/> was supplied and is not OMITTED (ISO §14.9.4.4 GR11 —
    /// the omitted-argument condition is the negation of this).</summary>
    public static bool Present(CobolArg[] args, int i) => i < args.Length && !args[i].Carrier.IsNull;

    /// <summary>Read any NATIVE NUMERIC carrier cell as its Int128-lane unscaled value, or null when the cell is
    /// not a native numeric (kb/Work R12 — the carrier set is the four <c>PicInfo.ClrType</c> integer carriers;
    /// a <c>UInt128</c> value beyond <see cref="Int128.MaxValue"/> passes as its container BITS, the same
    /// contract the R10 store path uses, so the typed write below reinterprets it exactly).</summary>
    private static Int128? ReadNumericCell(ManagedPointer p) => p switch
    {
        ManagedPointer<long> x => x.Value,
        ManagedPointer<ulong> x => (Int128)x.Value,
        ManagedPointer<Int128> x => x.Value,
        ManagedPointer<UInt128> x => unchecked((Int128)x.Value),
        _ => null,
    };

    /// <summary>Read any NATIVE FLOATING-POINT carrier cell as its binary64 value, or null when the cell is not
    /// one (kb/Work PB238 — the float lane joined this ABI's carrier vocabulary because a BY VALUE argument
    /// stopped being narrowed to the exact <c>Int128</c> lane at the CALLER; §14.2.3 GR10 makes the crossing
    /// "a COMPUTE statement without the ROUNDED phrase", and a caller-side integer cast performed a truncation
    /// the callee's own COMPUTE was supposed to perform at the FORMAL's scale — <c>1.5</c> reached a
    /// <c>PIC S9(3)V99</c> formal as <c>1.00</c>). It is a SECOND lane rather than a widening of
    /// <see cref="ReadNumericCell"/>: no <c>Int128</c> holds a fractional binary64 value, so the conversion has
    /// to happen where the destination scale is known.</summary>
    private static double? ReadRealCell(ManagedPointer p) => p switch
    {
        ManagedPointer<double> x => x.Value,
        ManagedPointer<float> x => x.Value,
        _ => null,
    };

    /// <summary>The write half of <see cref="ReadRealCell"/>; false when the cell is not a native float.</summary>
    private static bool WriteRealCell(ManagedPointer p, double v)
    {
        switch (p)
        {
            case ManagedPointer<double> x: x.Value = v; return true;
            case ManagedPointer<float> x: x.Value = (float)v; return true;
            default: return false;
        }
    }

    /// <summary>⛔ THE ONE NUMERIC LANDING OF THIS ABI (ISO §14.2.3 GR9/GR10; kb/Work PB288). Every numeric arm
    /// below reaches its receiving side through this: the crossing IS "a COMPUTE statement without the ROUNDED
    /// phrase" whose receiving operand is a data item of the FORMAL's description (GR9's prototyped/NESTED
    /// branch, GR10's allocated record), and GR11 resolves every reference to the formal through that same
    /// linkage description — so the GR8 aliasing view owes the identical landing, one value at a time, rather
    /// than a raw reinterpretation of the caller's bits.
    /// <para>The landing has TWO halves and both are load-bearing. (1) The scale alignment widens with STORE
    /// semantics (<see cref="CobolNum.RescaleStoreCap"/>): §14.7.5 case 3's overflow "after radix point
    /// alignment" is a receiver overflow, and with no SIZE ERROR phrase and checking off its documented
    /// disposition is the result's LOW-ORDER digits (CONFORMANCE.md DOC-A.1-70) — never the binary two's
    /// complement of an intermediate, which is not any rule. <c>BY CONTENT 1000000000000000000000000000000</c>
    /// into a <c>PIC S9(9)V9(9)</c> formal used to arrive as 873995514.006732800 because the unchecked
    /// <c>Rescale</c> formed 10^39 and wrapped. (2) The digit-capacity conformance
    /// (<see cref="CobolNum.Store"/>) then reduces to the formal's picture — WITHOUT it the
    /// <c>T.CreateTruncating</c> below is itself a BINARY truncation to the carrier, so an 18-digit argument
    /// viewed through a <c>PIC S9(4)V99</c> formal arrived as −9838.16 where the low-order digits are 5678.00.
    /// The float lane's <see cref="CobolNum.Store"/> composition was already right in <see cref="NumValue"/>
    /// and missing in <see cref="Num"/> — the two-arm shape this helper exists to make unrepeatable.</para></summary>
    /// <remarks><paramref name="toScale"/> is the emitted formal's <c>PicInfo.Scale</c> and
    /// <paramref name="receiver"/> is the profile emitted from the SAME <c>PicInfo</c>, whose
    /// <c>FractionDigits</c> is that same <c>Scale</c> (<c>PicInfo.ProfileInitializer</c>) — so
    /// <see cref="CobolNum.Store"/>'s own internal rescale is the identity here and cannot re-introduce an
    /// unchecked widening behind this one. Keep them emitted from one <c>PicInfo</c>.</remarks>
    private static Int128 Land(Int128 unscaled, int fromScale, int toScale, in NumProfile receiver) =>
        CobolNum.Store(CobolNum.RescaleStoreCap(unscaled, fromScale, toScale, CobolRounding.Truncation),
                       toScale, receiver, CobolRounding.Truncation);

    /// <summary>The binary64 lane of <see cref="Land(Int128, int, int, in NumProfile)"/>: the quantization to
    /// the receiving description's scale happens HERE (kb/Work PB238), and past the <c>Int128</c> carrier
    /// <see cref="CobolFloat.ToScaledUnchecked"/> keeps supplying the exact expansion's low-order digits
    /// (kb/Work PB77) for the same <see cref="CobolNum.Store"/> to reduce to the picture.</summary>
    private static Int128 Land(double value, int toScale, in NumProfile receiver) =>
        CobolNum.Store(CobolFloat.ToScaledUnchecked(value, toScale, CobolRounding.Truncation),
                       toScale, receiver, CobolRounding.Truncation);

    /// <summary>The EC-SIZE-CHECKED twin of <see cref="Land(Int128, int, int, in NumProfile)"/> — the SAME
    /// §14.2.3 GR9/GR10 COMPUTE, with §14.7.5's no-phrase rule 4 in force instead of DOC-A.1-70's low-order
    /// digits (kb/Work PB640). It exists only on the ACTIVATING side (<see cref="LandForFormal"/>): the
    /// crossing's COMPUTE is performed by the activating runtime element — GR9/GR10 say the allocated record is
    /// "allocated by the activating runtime element during the process of initiating the activation" — so the
    /// checking state that decides between the two dispositions is the ACTIVATING element's, and so are the
    /// declaratives that see the raise. <c>toScale</c> is not a parameter here because it can only ever be
    /// <c>receiver.FractionScale</c> (the remark on the unchecked overload), and
    /// <see cref="CobolNum.StoreOrRaise(Int128, int, in NumProfile, CobolRounding)"/> reads it from the
    /// profile — so the checked landing cannot be handed a scale the capacity check disagrees with.</summary>
    private static Int128 LandChecked(Int128 unscaled, int fromScale, in NumProfile receiver) =>
        CobolNum.StoreOrRaise(unscaled, fromScale, receiver, CobolRounding.Truncation);

    /// <summary>The binary64 lane of <see cref="LandChecked(Int128, int, in NumProfile)"/>. The quantizer is
    /// <see cref="CobolFloat.ToScaled"/>, NOT the unchecked twin: past the <c>Int128</c> carrier
    /// <c>ToScaledUnchecked</c> hands back the exact expansion's LOW-ORDER digits (kb/Work PB77), which is the
    /// checking-off disposition and would pass the capacity test as a small value — the §14.7.5 case-3 fact is
    /// about the ALGEBRAIC result, so the checked lane takes the saturating landing the capacity test then
    /// rejects.</summary>
    private static Int128 LandChecked(double value, int toScale, in NumProfile receiver) =>
        CobolNum.StoreOrRaise(CobolFloat.ToScaled(value, toScale, CobolRounding.Truncation),
                              toScale, receiver, CobolRounding.Truncation);

    /// <summary>The value of one argument landed into a record of the FORMAL's description (ISO §14.2.3
    /// GR9/GR10's "COMPUTE statement without the ROUNDED phrase"), or null when the carrier is outside this
    /// ABI's numeric vocabulary — the drift boundary <c>CallAbiNumericCarrierDriftTests</c> pins, which the
    /// callers turn into the §14.9.4.4 GR12 omitted carrier rather than a reinterpretation of storage.
    /// <para>⛔ ONE SCALAR LANDING FOR BOTH SIDES OF THE BOUNDARY (kb/Work PB640). The activating side
    /// (<see cref="LandForFormal"/>, which is where GR9/GR10 put the COMPUTE whenever the formal's description
    /// is knowable there) and the activated side (<see cref="NumValue"/>, the residual landing for a crossing
    /// whose formal the caller could not know) are the same three carrier arms and the same
    /// <see cref="Land(Int128, int, int, in NumProfile)"/>; only <paramref name="checking"/> differs.</para></summary>
    private static Int128? LandScalar(in CobolArg a, in NumProfile formal, int formalScale, bool checking) =>
        a.Carrier switch
        {
            // §14.2.3 GR10's COMPUTE from the FLOAT lane (kb/Work PB238): the quantization to the formal's
            // scale happens at the RECEIVER and truncates — exactly what an un-ROUNDED COMPUTE does.
            { } rp when ReadRealCell(rp) is { } rv =>
                checking ? LandChecked(rv, formalScale, formal) : Land(rv, formalScale, formal),
            { } np when ReadNumericCell(np) is { } nv =>
                checking ? LandChecked(nv, a.Scale, formal) : Land(nv, a.Scale, formalScale, formal),
            // An IMAGE-carried NUMERIC argument (a redefined item, a Tier-B window) is the COMPUTE's sending
            // operand, so its VALUE is its carrier read through ITS OWN description (kb/Work PB873) — decoding it
            // through the formal's profile read the argument's sign and scale as if they were the formal's. The
            // carrier is the item's STORAGE image (`CallEmitter.CallStringRead`; kb/Work PB970), so the decode is
            // THE record-image codec's, of every byte form — a float one on its own lane.
            ManagedPointer<string> sp when a.Num is { ByteForm: NumericByteForm.Ieee32 or NumericByteForm.Ieee64 } fd =>
                checking ? LandChecked(CobolNum.ParseImageFloat(sp.Value, fd), formalScale, formal)
                         : Land(CobolNum.ParseImageFloat(sp.Value, fd), formalScale, formal),
            ManagedPointer<string> sp when a.Num is { ByteForm: not NumericByteForm.None } d =>
                checking ? LandChecked(CobolNum.ParseImage(sp.Value, d), d.FractionScale, formal)
                         : Land(CobolNum.ParseImage(sp.Value, d), d.FractionScale, formalScale, formal),
            // A CHARACTER argument has no numeric description of its own: its storage decodes through the
            // formal's (the record-image codec — for a zoned formal the DISPLAY decode; kb/Work PB970, the same
            // reading Num's GR8 view takes); the rescale is then the identity and only the capacity conformance
            // remains.
            ManagedPointer<string> sp =>
                checking ? LandChecked(CobolNum.ParseImage(sp.Value, formal), formalScale, formal)
                         : Land(CobolNum.ParseImage(sp.Value, formal), formalScale, formalScale, formal),
            _ => null,
        };

    /// <summary>The CHARACTER IMAGE of a native numeric cell under the description <paramref name="d"/> it
    /// carries — the bytes the storage holds (§14.2.3 GR8: the formal "occupies the same storage area as the
    /// argument"), through THE record-image codec, so the operational sign, its position and the usage's byte
    /// form all appear exactly as they do in storage (kb/Work PB873). Null when the cell is not of the lane
    /// <paramref name="d"/> describes.</summary>
    private static string? CellImage(ManagedPointer cell, in NumProfile d) => d.ByteForm switch
    {
        NumericByteForm.Ieee32 or NumericByteForm.Ieee64 =>
            ReadRealCell(cell) is { } r ? CobolNum.FormatImageFloat(r, d) : null,
        NumericByteForm.None => null,
        _ => ReadNumericCell(cell) is { } n ? CobolNum.FormatImage(n, d) : null,
    };

    /// <summary>The write half of <see cref="CellImage"/>: decode <paramref name="image"/> through the SAME
    /// description and store the value into the cell.</summary>
    private static void WriteCellImage(ManagedPointer cell, string image, in NumProfile d)
    {
        if (d.ByteForm is NumericByteForm.Ieee32 or NumericByteForm.Ieee64)
            WriteRealCell(cell, CobolNum.ParseImageFloat(image, d));
        else
            WriteNumericCell(cell, CobolNum.ParseImage(image, d));
    }

    /// <summary>True when <paramref name="formal"/> describes a FLOATING-POINT item — the test that sends a crossing
    /// to the float lane (<see cref="LandFloat"/>) instead of the fixed-point one (<see cref="LandScalar"/>).
    /// ONE spelling for every arm of this ABI that has a fixed-point and a float landing (kb/Work PB1114).</summary>
    private static bool IsFloatFormal(in NumProfile formal) =>
        formal.ByteForm is NumericByteForm.Ieee32 or NumericByteForm.Ieee64;

    /// <summary>⛔ THE ONE FLOAT LANDING OF THIS ABI — §14.2.3 GR9/GR10's "COMPUTE statement without the ROUNDED
    /// phrase" when the receiving record is FLOATING-POINT (kb/Work PB1114), the float twin of
    /// <see cref="LandScalar"/>. The sending operand is landed by <see cref="FloatResultant"/>, THE one transfer of
    /// an arithmetic value into a float resultant identifier: implied TRUNCATION (§14.7.4.3 rule 10 — the value
    /// nearer to zero), a fixed-point sender converted EXACTLY in one rounding (never through a binary64 first, so a
    /// binary32 formal takes no double rounding — kb/Work PB1110), and a value past the format's range the §14.7.5
    /// case-3 size error. <paramref name="checking"/> selects the disposition as <see cref="LandScalar"/>'s does:
    /// EC-SIZE-TRUNCATION raises (<see cref="CobolSizeError.FloatTruncation"/>) on the activating side when
    /// enabled, and the no-phrase unchecked landing stands otherwise (CONFORMANCE.md DOC-A.1-70). Null when the
    /// carrier is outside the numeric vocabulary. The landed binary64 is exact in a binary32 formal's carrier.</summary>
    private static double? LandFloat(in CobolArg a, in NumProfile formal, bool checking)
    {
        bool single = formal.ByteForm is NumericByteForm.Ieee32;
        const CobolRounding Mode = CobolRounding.Truncation;   // "without the ROUNDED phrase"
        double Real(double v) => checking ? FloatResultant.FromRealOrRaise(v, Mode, single) : FloatResultant.FromReal(v, Mode, single);
        double Scaled(Int128 u, int scale) =>
            checking ? FloatResultant.FromScaledOrRaise(u, scale, Mode, single) : FloatResultant.FromScaled(u, scale, Mode, single);
        return a.Carrier switch
        {
            { } rp when ReadRealCell(rp) is { } rv => Real(rv),
            // The 16-byte unsigned container reaches 2^128 − 1, past every signed carrier (kb/Work R10): its own lane.
            ManagedPointer<UInt128> up => checking
                ? FloatResultant.FromUnsignedScaledOrRaise(up.Value, a.Scale, Mode, single)
                : FloatResultant.FromUnsignedScaled(up.Value, a.Scale, Mode, single),
            { } np when ReadNumericCell(np) is { } nv => Scaled(nv, a.Scale),
            // An image-carried argument's carrier is its STORAGE image (kb/Work PB970) — the float lane's IEEE bytes
            // or a fixed-point record image, each through THE record-image codec under its own description.
            ManagedPointer<string> sp when a.Num is { ByteForm: NumericByteForm.Ieee32 or NumericByteForm.Ieee64 } fd =>
                Real(CobolNum.ParseImageFloat(sp.Value, fd)),
            ManagedPointer<string> sp when a.Num is { ByteForm: not NumericByteForm.None } d =>
                Scaled(CobolNum.ParseImage(sp.Value, d), d.FractionScale),
            // A CHARACTER argument has no numeric description of its own: its storage reads as the formal's.
            ManagedPointer<string> sp => Real(CobolNum.ParseImageFloat(sp.Value, formal)),
            _ => null,
        };
    }

    /// <summary>⛔ THE ACTIVATING ELEMENT'S §14.2.3 GR9/GR10 CROSSING (kb/Work PB640) — the caller-side half of
    /// this ABI's numeric landing, emitted by <c>CallEmitter.ArgText</c> around the argument carrier it just
    /// built, for every BY CONTENT / BY VALUE argument whose corresponding formal parameter is a fixed-point
    /// numeric item KNOWN AT THE CALL SITE.
    /// <para>WHY THE CALLER AND NOT THE CALLEE. GR9 and GR10 both say the allocated record is "allocated by the
    /// activating runtime element during the process of initiating the activation", and it is the ACTIVATING
    /// element that supplies the COMPUTE's sending operand — so every consequence of that COMPUTE is the
    /// activating element's: its <c>&gt;&gt;TURN EC-SIZE CHECKING</c> state decides whether a §14.7.5 case-3
    /// overflow raises (enablement is a property of a compilation group, §14.6.13.1.1), its USE declaratives
    /// are the ones §14.6.13.1.3 selects over, and §14.9.4.4 GR3g transfers control to the called program only
    /// "if a fatal exception condition has not been raised" — which a landing performed after the transfer can
    /// no longer honour. Before PB640 the whole landing ran callee-side, so an argument that overflowed the
    /// formal's description under checking arrived as DOC-A.1-70's low-order digits with no raise at all.</para>
    /// <para>WHEN THE CALLER CANNOT. GR9's FIRST branch — a program with no program-specifier in the activating
    /// element's REPOSITORY paragraph and no NESTED phrase — allocates a record "of the same length as the
    /// argument" and moves it "without conversion", so there is no COMPUTE to perform and no formal description
    /// to perform it against. The set of crossings that ARE a COMPUTE (a prototyped program, a NESTED CALL, a
    /// method, a function) is exactly the set whose formal is knowable at the call site; §14.8.2.3.3 draws the
    /// same partition for conformance. <see cref="NumValue"/> and <see cref="Num"/> therefore keep their own
    /// landing for the residue, and for an already-landed argument it is the identity: the carrier IS the
    /// formal's, at the formal's scale, within the formal's capacity.</para>
    /// <para><paramref name="checking"/> is decided at COMPILE time from the CALL statement's own TURN state —
    /// the same kernel selection the arithmetic store makes — so a unit with checking off emits the landing it
    /// always had.</para></summary>
    /// <typeparam name="T">The formal's carrier (<c>PicInfo.ClrType</c>), so the callee's own adapter sees a
    /// same-carrier same-scale argument and aliases it (§14.2.3 GR9's "treated … as if it were passed by
    /// reference") rather than converting it a second time.</typeparam>
    public static CobolArg LandForFormal<T>(CobolArg arg, NumProfile formal, int formalScale, bool checking)
        where T : struct, System.Numerics.INumberBase<T>
    {
        // OMITTED (§14.9.4.4 GR11): there is no argument to be the COMPUTE's sending operand.
        if (arg.Carrier.IsNull) return arg;
        // A FLOATING-POINT formal is the same COMPUTE on the float lane (kb/Work PB1114): the landed value is a
        // binary64 that is exact in the formal's own carrier, never an Int128 through a zero-digit profile.
        if (IsFloatFormal(formal))
        {
            if (LandFloat(arg, formal, checking) is not { } fv) return arg;
            T floatLanded = T.CreateTruncating(fv);
            return arg.Carrier is ManagedPointer<T> sameFloat && sameFloat.Value == floatLanded
                ? arg with { Num = formal }
                : arg with { Carrier = ManagedPointer<T>.Cell(floatLanded), Num = formal };
        }
        // A carrier outside the numeric vocabulary (a variable-length group, an object/pointer handle) is not a
        // numeric crossing to land — it reaches the callee unchanged and takes that side's own arm.
        if (LandScalar(arg, formal, formalScale, checking) is not { } v) return arg;
        T landed = T.CreateTruncating(v);
        // The allocated record IS the argument from here on (GR9's last sentence), so the meta the ABI carries
        // becomes the record's own description — which is what makes the callee-side landing the identity.
        // ⚡ The CONFORMING case — same carrier, same scale, inside the formal's capacity — keeps the cell the
        // activating element has ALREADY allocated (the BY CONTENT / BY VALUE snapshot this CALL site built),
        // because the COMPUTE is the identity over it and a second cell would be pure garbage on the call path.
        // CobolArg is a readonly record struct, so the meta update itself allocates nothing either way.
        // The formal's WHOLE description, sign and byte form included (kb/Work PB873): an image-carried formal
        // reads the landed record through it, and the pair this replaced carried only (Digits, Scale).
        if (arg.Carrier is ManagedPointer<T> same && arg.Scale == formalScale && same.Value == landed)
            return arg with { Num = formal };
        return arg with { Carrier = ManagedPointer<T>.Cell(landed), Num = formal };
    }

    /// <summary>The write half of <see cref="ReadNumericCell"/>; false when the cell is not a native numeric.</summary>
    private static bool WriteNumericCell(ManagedPointer p, Int128 v)
    {
        switch (p)
        {
            case ManagedPointer<long> x: x.Value = unchecked((long)v); return true;
            case ManagedPointer<ulong> x: x.Value = unchecked((ulong)v); return true;
            case ManagedPointer<Int128> x: x.Value = v; return true;
            case ManagedPointer<UInt128> x: x.Value = unchecked((UInt128)v); return true;
            default: return false;
        }
    }

    /// <summary>⛔ A CLASS-POINTER ARGUMENT PASSED BY CONTENT TO A NON-POINTER FORMAL arrives as its STORAGE IMAGE
    /// (kb/Work PB970 arm 2). ISO §14.8.2.3.3 1) — for a program activated with no program-specifier and no NESTED
    /// phrase, "the formal parameter shall be of the same length as the corresponding argument" is the whole
    /// conformance rule when the formal "is not of class object or pointer", so a <c>USAGE POINTER</c> argument
    /// BY CONTENT into a <c>PIC X(8)</c> (or an 8-byte binary) formal is CONFORMING; and §14.2.3 GR9 says what the
    /// formal then holds: "That argument is moved to this allocated record without conversion" — the pointer's
    /// 8 storage positions, which <see cref="PointerImage"/> defines (DOC-A.1-216). The record is detached (GR9 —
    /// it "does not occupy the same storage area as the argument"), so the image rides a fresh string cell and the
    /// ordinary character / numeric arms adopt it exactly as they adopt any other storage image.
    /// <para>Only BY CONTENT: BY REFERENCE §14.8.2.3.2 requires a pointer formal for a pointer argument, and BY
    /// VALUE GR10 allocates the formal's own description filled by COMPUTE or SET, neither of which a pointer can
    /// send to a non-pointer — both stay the loud <see cref="Unreadable{T}"/> they are. A GROUP formal is not
    /// routed here either: §14.8.2.2 2) makes that pairing a MOVE, which a pointer cannot send. Nor is a formal of
    /// any length but the pointer's 8 (<paramref name="formalWidth"/>; negative = ANY LENGTH, whose length rule
    /// 2c "considered to match"): rule 1's same-length requirement is the pairing's whole conformance, and its
    /// violation is §14.9.4.4 GR3 d)'s EC-PROGRAM-ARG-MISMATCH (conformance:2002/pb615_unreadable_argument_carrier).
    /// Returns <paramref name="args"/> itself (no allocation) whenever the argument is anything else.</para></summary>
    /// <summary>The cheap pre-test for <see cref="WithPointerContent"/>: a BY CONTENT argument on a class-pointer
    /// slot. Asked first so the common (non-pointer) crossing never computes the formal's image width.</summary>
    private static bool IsPointerContent(in CobolArg a) =>
        a.Mode is CobolPassMode.Content
        && a.Carrier is ManagedPointer<ManagedPointer> or ManagedPointer<ProgramPointer> or ManagedPointer<FunctionPointer>;

    private static CobolArg[] WithPointerContent(CobolArg[] args, int i, int formalWidth)
    {
        if (args[i].Mode is not CobolPassMode.Content || PointerImage.OfCarrier(args[i].Carrier) is not { } image
            || formalWidth >= 0 && formalWidth != image.Length)
            return args;
        var copy = (CobolArg[])args.Clone();
        copy[i] = args[i] with { Carrier = ManagedPointer<string>.Cell(image), Num = null };
        return copy;
    }

    /// <summary>Adapt argument <paramref name="i"/> to a NUMERIC formal described by <paramref name="formal"/>
    /// (the callee's profile) at <paramref name="formalScale"/>. GENERIC over the formal's CARRIER
    /// (<c>long</c> / <c>ulong</c> / <c>Int128</c> / <c>UInt128</c> — <c>PicInfo.ClrType</c>'s integer set;
    /// kb/Work R12: the wide and unsigned tiers used to be routed onto a STRING crossing whose write half was
    /// never implemented — the generated C# did not compile — while the callee side hardcoded a
    /// <c>ManagedPointer&lt;long&gt;</c> cell its own carrier-typed reads could not use). A same-carrier
    /// same-scale argument aliases directly; a different scale or different carrier gets a converting view over
    /// the SAME storage (§14.2.3 GR8); a character carrier gets the zoned decode/encode view through the
    /// callee's profile — the D5 boundary. Conversions ride the Int128 lane with the R10 bits contract at the
    /// UInt128 ends, so a full-container value crosses losslessly between same-carrier cells.</summary>
    public static ManagedPointer<T> Num<T>(CobolArg[] args, int i, NumProfile formal, int formalScale)
        where T : struct, System.Numerics.INumberBase<T>
    {
        if (!Present(args, i)) return Omitted<T>();
        if (IsPointerContent(args[i]))
            args = WithPointerContent(args, i, formal.StorageLength > 0 ? formal.StorageLength : CobolNum.FormatImage(Int128.Zero, formal).Length);
        switch (args[i].Carrier)
        {
            case ManagedPointer<T> tp when args[i].Scale == formalScale:
                return tp;   // same carrier, same scale — pure typed aliasing (the common conforming case)
            case ManagedPointer<string> sp when formal.ByteForm is NumericByteForm.Ieee32 or NumericByteForm.Ieee64:
                // A FLOATING-POINT formal over the caller's storage image (kb/Work PB970): the same GR8 view on
                // the float lane of THE record-image codec — the IEEE bytes the formal occupies, read and
                // written as the formal's own usage. The fixed-point decode below throws on this profile (and
                // before PB970 read the bytes as DISPLAY digits).
                return ManagedPointer<T>.OverField(
                    () => T.CreateTruncating(CobolNum.ParseImageFloat(sp.Value, formal)),
                    v => sp.Value = CobolNum.FormatImageFloat(double.CreateTruncating(v), formal));
            case ManagedPointer<string> sp:
                // The D5 boundary: the caller's CHARACTER storage viewed as the callee's numeric — decode and
                // re-encode through the callee's profile on each access (same storage area, §14.2.3 GR8). The
                // carrier is STORAGE (kb/Work PB970: an image-carried numeric argument crosses as its record image,
                // a character one as its characters), so the codec is THE record-image codec under the FORMAL's
                // description — the bytes the formal "occupies" read as the formal reads them. For a zoned formal
                // it is the DISPLAY decode it always was (ParseImage's zoned arm IS ParseDisplay).
                return ManagedPointer<T>.OverField(
                    // Through the shared Land, like every other arm: the decode is already at the formal's
                    // scale so the rescale is the identity, but the capacity conformance must not be the one
                    // arm that skips it (§14.2.3 GR11 — every reference resolves through the SAME description).
                    () => T.CreateTruncating(Land(CobolNum.ParseImage(sp.Value, formal), formalScale, formalScale, formal)),
                    v => sp.Value = CobolNum.FormatImage(Int128.CreateTruncating(v), formal));
            case { } fc when IsFloatFormal(formal) && (ReadRealCell(fc) is not null || ReadNumericCell(fc) is not null
                                                        || fc is ManagedPointer<UInt128>):
            {
                // A native numeric cell of ANOTHER description viewed through a FLOATING-POINT formal (kb/Work
                // PB1114): the converting view on the float lane — the read is the shared float landing (§14.2.3
                // GR9/GR10 + GR11), the write un-scales back into the caller's cell in ITS representation. The
                // fixed-point arms below land through a float description's ZERO digits, which is the value 0.
                int callerScale = args[i].Scale;
                return ManagedPointer<T>.OverField(
                    () => T.CreateTruncating(LandFloat(args[i], formal, checking: false)!.Value),
                    v =>
                    {
                        double dv = double.CreateTruncating(v);
                        if (!WriteRealCell(fc, dv))
                            WriteNumericCell(fc, CobolFloat.ToScaledUnchecked(dv, callerScale, CobolRounding.Truncation));
                    });
            }
            case { } rp when ReadRealCell(rp) is not null:
                // A native FLOAT cell viewed through a fixed-point formal (kb/Work PB238): the same §14.2.3 GR8
                // converting view, on the float lane, because no Int128 holds the fractional value. The scale
                // conversion is the receiver's, in BOTH directions — read lands through the formal's own
                // description (§14.2.3 GR9/GR10 + GR11, the shared Land), write un-scales back into the caller's
                // float.
                return ManagedPointer<T>.OverField(
                    () => T.CreateTruncating(Land(ReadRealCell(rp)!.Value, formalScale, formal)),
                    v => WriteRealCell(rp, CobolFloat.ScaledToDouble(Int128.CreateTruncating(v), formalScale)));
            case { } np when ReadNumericCell(np) is not null:
            {
                // A native numeric cell of a DIFFERENT carrier or scale: a converting view over the caller's
                // storage, landed through the formal's description on every read (§14.2.3 GR11 — the shared
                // Land; before kb/Work PB288 this read an UNCHECKED Rescale straight into T.CreateTruncating,
                // so both the scale alignment and the carrier narrowing were binary wraps).
                // Same-scale cross-carrier reads/writes are bit-faithful through the Int128 lane.
                int callerScale = args[i].Scale;
                return ManagedPointer<T>.OverField(
                    () => T.CreateTruncating(Land(ReadNumericCell(np)!.Value, callerScale, formalScale, formal)),
                    // The write-back's receiving side is the CALLER's item, whose capacity discipline is its own
                    // carrier's (WriteNumericCell); only the scale alignment is this ABI's, and it takes the same
                    // store-semantics widening rather than the wrapping one.
                    v => WriteNumericCell(np, CobolNum.RescaleStoreCap(Int128.CreateTruncating(v), formalScale, callerScale, CobolRounding.Truncation)));
            }
            default:
                return Unreadable<T>(args, i, "a numeric formal");
        }
    }

    /// <summary>Adapt argument <paramref name="i"/> to a CHARACTER formal of <paramref name="width"/> characters.
    /// A character carrier gets a width-window view: reads are the first <paramref name="width"/> positions
    /// (space-padded when the caller's storage is shorter); writes SPLICE into the caller's storage, preserving
    /// the caller's own width invariant (§14.2.3 GR8 — the callee touches only its formal's character positions).
    /// A native numeric cell gets a view of its STORAGE image under the description it carries (kb/Work PB873/PB970).
    /// <para><paramref name="width"/> = <c>-1</c> is the ANY LENGTH mode (ISO §13.18.2 GR1): the formal's length
    /// IS the caller's argument length, so the callee sees the caller's FULL string (a zero-length argument
    /// yields the zero-length item, GR1a) and every write re-fits to the argument's CURRENT length (GR1b — the
    /// item behaves as n repetitions of its picture symbol, n fixed by the activation).</para>
    /// <para>⛔ <paramref name="groupLayout"/> is stated for a GROUP formal only — its §8.5.1.12 layout, or the
    /// empty array for a group with no table (whose one §8.5.1.12 fact is its length, <see cref="CobolVarGroup.FixedRun"/>).
    /// It admits a VARIABLE-LENGTH GROUP argument (kb/Work PB965): §14.8.2.2 "If either the formal parameter or
    /// the argument is a variable length group, the formal parameter and the argument shall be compatible, as
    /// described in 8.5.1.12", and §8.5.1.12.1 admits the pair "only one of the operands may be a variable-length
    /// group". The argument arrives on the §8.5.1.12 carrier with its layout (<see cref="CobolArg.Layout"/>); the
    /// ONE correspondence walk (<see cref="CobolVarGroup.CorrespondingSpans"/>) pairs the formal's tables with
    /// the argument's dynamic-capacity tables, the view reads the argument's image through it
    /// (<see cref="CobolVarGroup.ToFixedImage"/> — each table fitted to the formal's occurrence count, §8.5.1.12.3
    /// sentence 3), and a store overlays the argument's storage (<see cref="CobolVarGroup.OverlayFixedImage"/>,
    /// §14.2.3 GR8). Before PB965's finisher the pair was refused at bind by a size compare on the collapsed
    /// width, and this arm did not exist.</para></summary>
    /// <remarks><paramref name="formalNum"/> is an image-stored NUMERIC formal's own description (kb/Work PB992) —
    /// read only by the omitted arm, whose benign value (the documented §14.9.4.4 GR12 leniency: "a numeric view
    /// answers zero") is then the formal's ZERO image rather than the empty string.</remarks>
    public static ManagedPointer<string> Text(CobolArg[] args, int i, int width, int[]? groupLayout = null,
                                              NumProfile? formalNum = null)
    {
        if (!Present(args, i))
            return formalNum is { } fz
                ? ManagedPointer<string>.OmittedArgument(() => CobolNum.FormatImage(0, fz), _ => { })
                : Omitted<string>();
        if (groupLayout is null && IsPointerContent(args[i])) args = WithPointerContent(args, i, width);
        switch (args[i].Carrier)
        {
            case ManagedPointer<CobolVarGroup> vp when VarGroupSpans(args[i], groupLayout, width) is { } spans:
                return ManagedPointer<string>.OverField(
                    () => CobolVarGroup.ToFixedImage(vp.Value ?? CobolVarGroup.Empty, width, spans),
                    v => vp.Value = CobolVarGroup.OverlayFixedImage(vp.Value ?? CobolVarGroup.Empty,
                        CobolString.Store(v, width), spans));
            case ManagedPointer<string> sp when width < 0:   // ANY LENGTH (§13.18.2 GR1) — the full-string view
                return ManagedPointer<string>.OverField(
                    () => sp.Value ?? "",
                    v => sp.Value = CobolString.Store(v, sp.Value?.Length ?? 0));
            case ManagedPointer<string> sp:
                return ManagedPointer<string>.OverField(
                    () => CobolString.Store(sp.Value, width),
                    v => sp.Value = CobolString.WindowInto(sp.Value, 1, Math.Min(width, sp.Value?.Length ?? width), v));
            case { } np when args[i].Num is { } d && CellImage(np, d) is { } image:
            {
                // ⛔ THE STORAGE'S OWN IMAGE (kb/Work PB873; §14.2.3 GR8 — "operates as if the formal parameter
                // occupies the same storage area as the argument"; GR9's first branch moves it "without
                // conversion"). The image is the carried description's record image — sign, sign position and
                // byte form included — so a signed argument keeps its operational sign, a SIGN SEPARATE one its
                // extra position, and a binary/packed one its bytes. After GR9/GR10's COMPUTE the carried
                // description IS the formal's (LandForFormal), so the same arm serves both branches.
                // ANY LENGTH (width -1): the view width is the argument's own image width — n follows the
                // ARGUMENT's description (§13.18.2 GR1), never the formal's one-symbol picture.
                int viewWidth = width < 0 ? image.Length : width;
                return ManagedPointer<string>.OverField(
                    () => CobolString.Store(CellImage(np, d), viewWidth),
                    // A store touches only the formal's character positions (GR8) — splice into the CURRENT
                    // image, exactly as the character arm above splices into the caller's string.
                    v =>
                    {
                        string current = CellImage(np, d)!;
                        WriteCellImage(np, CobolString.WindowInto(current, 1, Math.Min(viewWidth, current.Length), v), d);
                    });
            }
            case { } np when args[i].Num is null && ReadNumericCell(np) is not null:
            {
                // A native cell that carries NO numeric description — a USAGE INDEX item, whose storage
                // description has no digit positions for a profile to state: its image stays the unsigned digit
                // run of the view width, the representation this ABI has always given it.
                int w = Math.Max(1, width);
                var prof = new NumProfile
                {
                    Digits = w, FractionDigits = 0, Signed = false,
                    Truncation = NumericTruncation.DigitCount, ByteForm = NumericByteForm.Zoned,
                };
                return ManagedPointer<string>.OverField(
                    () => CobolNum.FormatDisplay(ReadNumericCell(np)!.Value, prof),
                    v => WriteNumericCell(np, CobolNum.ParseDisplay(v, prof)));
            }
            default:
                return Unreadable<string>(args, i, $"a character formal of {width} position(s)");
        }
    }

    /// <summary>Adapt argument <paramref name="i"/> to a BY VALUE NUMERIC formal (ISO §14.2.3 GR10): the activated
    /// element operates on "the record in the linkage section … allocated by the activating runtime element" — a
    /// data item OF THE FORMAL'S OWN DESCRIPTION that does NOT alias the argument, filled as if by "a COMPUTE
    /// statement without the ROUNDED phrase" with the argument as the sending operand. Realized as a DETACHED
    /// cell: the argument's value is rescaled to the formal's scale (truncation — the un-ROUNDED COMPUTE) and
    /// conformed to the formal's digit capacity via <see cref="CobolNum.Store"/>; the callee's stores reach only
    /// the cell, never the caller's storage (contrast <see cref="Num"/>, the §14.2.3 GR8 aliasing view).</summary>
    /// <remarks>⛔ THE RESIDUAL LANDING, NOT THE ONLY ONE (kb/Work PB640). When the activating element could
    /// know the formal's description it has ALREADY performed this COMPUTE — <see cref="LandForFormal"/>, which
    /// is where GR9/GR10 put it — and the argument arrives on the formal's own carrier at the formal's own
    /// scale, within its capacity, so the three arms below are the identity. This side stays because GR9's
    /// first branch (a non-prototyped, non-NESTED program CALL) gives the caller no formal to land against and
    /// because a non-COBOL activator supplies whatever it supplies; it shares
    /// <see cref="LandScalar"/> with the activating side so the two cannot answer differently.</remarks>
    public static ManagedPointer<T> NumValue<T>(CobolArg[] args, int i, NumProfile formal, int formalScale)
        where T : struct, System.Numerics.INumberBase<T> =>
        // Land's 16-byte-unsigned result is container BITS (R10); CreateTruncating reinterprets them exactly.
        !Present(args, i) ? Omitted<T>()
        // A FLOATING-POINT formal lands on the float lane (kb/Work PB1114): the fixed-point landing's Int128 store
        // through a float description's zero digits was the value 0.
        : IsFloatFormal(formal) ? LandFloat(args[i], formal, checking: false) is { } fv
            ? ManagedPointer<T>.Cell(T.CreateTruncating(fv))
            : Unreadable<T>(args, i, "a BY VALUE numeric formal")
        : LandScalar(args[i], formal, formalScale, checking: false) is { } v
            ? ManagedPointer<T>.Cell(T.CreateTruncating(v))
            : Unreadable<T>(args, i, "a BY VALUE numeric formal");

    /// <summary>Adapt argument <paramref name="i"/> to a BY VALUE formal whose callee-side storage is a CHARACTER
    /// image of <paramref name="width"/> positions (a REDEFINED numeric formal — still class numeric, §14.2.2
    /// SR2-legal, but image-carried): the same §14.2.3 GR10 detached copy as <see cref="NumValue"/>, in image
    /// form. Writes reach only the cell (contrast <see cref="Text"/>, the GR8 splice-through view).
    /// <para>⛔ THE RECORD IS OF THE FORMAL'S DESCRIPTION (kb/Work PB873). GR10's record is "allocated by the
    /// activating runtime element" and filled by "a COMPUTE statement without the ROUNDED phrase" whose receiving
    /// operand is that record — so its image is the LANDED value in the formal's own representation
    /// (<paramref name="formal"/>), never the argument's digit run under a hard-coded unsigned profile, which
    /// is what dropped the sign of <c>-12.34</c>. <paramref name="formal"/> is null only for a formal with no
    /// numeric description, which takes the argument's own image.</para></summary>
    public static ManagedPointer<string> TextValue(CobolArg[] args, int i, int width, NumProfile? formal, int formalScale,
        int[]? groupLayout = null)
    {
        if (!Present(args, i)) return Omitted<string>();
        // A variable-length group argument into a fixed-length GROUP formal BY CONTENT (kb/Work PB965): §14.8.2.2
        // rule 2's MOVE, which §14.9.25.4 GR9 performs through the same correspondence (§8.5.1.12.3 sentence 3).
        if (args[i].Carrier is ManagedPointer<CobolVarGroup> vp && VarGroupSpans(args[i], groupLayout, width) is { } vspans)
            return ManagedPointer<string>.Cell(CobolVarGroup.ToFixedImage(vp.Value ?? CobolVarGroup.Empty, width, vspans));
        if (formal is { } f)
        {
            string? image = IsFloatFormal(f)
                ? LandFloat(args[i], f, checking: false) is { } dv ? CobolNum.FormatImageFloat(dv, f) : null
                : LandScalar(args[i], f, formalScale, checking: false) is { } v ? CobolNum.FormatImage(v, f) : null;
            return image is null
                ? Unreadable<string>(args, i, "a BY VALUE numeric formal")
                : ManagedPointer<string>.Cell(CobolString.Store(image, width));
        }
        return args[i].Carrier switch
        {
            ManagedPointer<string> sp => ManagedPointer<string>.Cell(CobolString.Store(sp.Value, width)),
            { } np when args[i].Num is { } d && CellImage(np, d) is { } img =>
                ManagedPointer<string>.Cell(CobolString.Store(img, width)),
            _ => Unreadable<string>(args, i, $"a BY VALUE character formal of {width} position(s)"),
        };
    }

    /// <summary>Adapt argument <paramref name="i"/> to a VARIABLE-LENGTH GROUP formal (ISO §14.8.2.2's
    /// compatibility sentence via §14.9.4.3 SR25; kb/Work PB204). The carrier IS the
    /// <see cref="CobolVarGroup"/> the caller built, aliased whole: unlike <see cref="Text"/> there is no width
    /// window to apply here, because the fixed run and the component list are BOTH re-fitted by the receiving
    /// group's own emitted distributor — which knows its own geometry and is the only thing that can. A
    /// <para>⛔ A FIXED-LENGTH GROUP ARGUMENT IS A LEGAL SENDER TOO (kb/Work PB965). §14.8.2.2 requires only that
    /// "the formal parameter and the argument shall be compatible, as described in 8.5.1.12", and §8.5.1.12.1
    /// admits the pair ("only one of the operands may be a variable-length group"). Such an argument arrives on
    /// the fixed group's own character carrier, carrying its layout (<see cref="CobolArg.Layout"/>); the formal's
    /// layout is <paramref name="formalLayout"/>, and <see cref="CobolVarGroup.CorrespondingSpans"/> pairs them.
    /// The view decomposes the argument's image into the §8.5.1.12 carrier — its corresponding table crossing at
    /// its fixed occurrence count (§8.5.1.12.3 sentence 3) — and, BY REFERENCE, writes the formal's changes back
    /// into the SAME storage (§14.2.3 GR8) through the inverse. Before PB965 this arm did not exist: the
    /// argument read as an empty carrier and the callee's stores were lost.</para>
    /// <para>⚠ The callee cannot grow a fixed-length argument's table past its fixed extent — that storage has
    /// no more occurrences — so the write-back fits each component to its table exactly as §14.6.9.2 fits a
    /// dynamic sending table into a non-dynamic receiving one: superfluous occurrences are not moved, missing
    /// ones are space filled (<see cref="CobolVarGroup.ToFixedImage"/>).</para>
    /// <para>Any other carrier, or a layout pair that does not correspond, means the two sides do not conform
    /// (§14.8.2.2 via §14.9.4.4 GR3 d)) — loud, never a silent reinterpretation.</para></summary>
    public static ManagedPointer<CobolVarGroup> VarGroup(CobolArg[] args, int i, int[] formalLayout)
    {
        if (!Present(args, i)) return Omitted<CobolVarGroup>();
        return args[i].Carrier switch
        {
            ManagedPointer<CobolVarGroup> vp => vp,
            ManagedPointer<string> sp when CobolVarGroup.CorrespondingSpans(FixedLayoutOf(args[i], sp), formalLayout) is { } spans =>
                ManagedPointer<CobolVarGroup>.OverField(
                    () => CobolVarGroup.FromFixedImage(sp.Value ?? "", spans),
                    v => sp.Value = CobolVarGroup.ToFixedImage(v, sp.Value?.Length ?? 0, spans)),
            _ => Unreadable<CobolVarGroup>(args, i, "a variable-length group formal"),
        };
    }

    /// <summary>Adapt argument <paramref name="i"/> to a DYNAMIC LENGTH formal (ISO §13.18.19; kb/Work PB165).
    /// The THIRD length regime beside the fixed window and ANY LENGTH's activation-fixed one, and it had no arm
    /// at all: a dynamic-length formal took <see cref="Text"/> at its PICTURE length, which §13.18.19.3 SR1
    /// pins at exactly ONE symbol — so every such crossing delivered one character (measured: a 7-character
    /// argument arrived as <c>LEN=1</c>, and the callee's store spliced one character back into the caller's
    /// seven).
    /// <para>The view is the caller's FULL string, and a store carries §8.5.1.10.4's dynamic-length semantics
    /// through the ONE store helper: the new content replaces the old, the new length IS the sending length —
    /// never padded, minimum zero (§13.18.19.4 GR1) — truncated on the right at <paramref name="limit"/>
    /// (the item's §8.5.1.10.1 maximum size — GR2's LIMIT phrase bounded by the implementor maximum).</para>
    /// <para>BY REFERENCE this is §14.2.3 GR8's shared storage area, so the varying length is the CALLER's item
    /// varying. BY CONTENT / BY VALUE the caller already snapshotted the argument into a detached cell, and
    /// §14.2.3 GR9's second regime describes exactly this record — "a dynamic-length elementary item of the
    /// same category and described with the same dynamic-length-structure-name as the formal parameter" — with
    /// the argument moved into it, which is what the full-string view plus the dynamic store performs.</para>
    /// </summary>
    public static ManagedPointer<string> DynText(CobolArg[] args, int i, int limit)
    {
        if (!Present(args, i)) return Omitted<string>();
        return args[i].Carrier switch
        {
            ManagedPointer<string> sp => ManagedPointer<string>.OverField(
                () => sp.Value ?? "",
                v => sp.Value = CobolDynString.Store(v, limit)),
            // A numeric carrier reaching a dynamic-length formal is not a crossing to invent: §14.8.2.3.2
            // rule 2 requires the same DYNAMIC LENGTH and PICTURE clauses BY REFERENCE, and §14.8.2.3.3's MOVE
            // rules give a numeric sender an alphanumeric receiver only through its digit image — which is a
            // FIXED width and so contradicts the receiver's varying one. It takes the loud omitted carrier.
            _ => Unreadable<string>(args, i, "a dynamic-length formal"),
        };
    }

    /// <summary>The BY VALUE / BY CONTENT twin of <see cref="VarGroup"/> (ISO §14.2.3 GR9/GR10 — a copy
    /// allocated by the activating element): a DETACHED cell holding the argument's carrier value, so the
    /// callee's stores never reach the caller's storage. A fixed-length group argument decomposes through the
    /// same correspondence <see cref="VarGroup"/> uses (kb/Work PB965).</summary>
    public static ManagedPointer<CobolVarGroup> VarGroupValue(CobolArg[] args, int i, int[] formalLayout)
    {
        if (!Present(args, i)) return Omitted<CobolVarGroup>();
        return args[i].Carrier switch
        {
            ManagedPointer<CobolVarGroup> vp => ManagedPointer<CobolVarGroup>.Cell(vp.Value ?? CobolVarGroup.Empty),
            ManagedPointer<string> sp when CobolVarGroup.CorrespondingSpans(FixedLayoutOf(args[i], sp), formalLayout) is { } spans =>
                ManagedPointer<CobolVarGroup>.Cell(CobolVarGroup.FromFixedImage(sp.Value ?? "", spans)),
            _ => Unreadable<CobolVarGroup>(args, i, "a BY VALUE variable-length group formal"),
        };
    }

    /// <summary>Adapt argument <paramref name="i"/> to a MANAGED-SLOT formal — a formal of class pointer or
    /// class object-reference, whose value is a managed reference and has no byte image at all (kb/Work PB663;
    /// the <c>SlotWindow.CarriedBySlot</c> population, which is where the compiler decides the same thing about
    /// storage). It is the FOURTH crossing form beside the native cell, the character image and the
    /// variable-length carrier, and the one that had no arm: a pointer formal took <see cref="Text"/> and
    /// arrived as a <c>ManagedPointer&lt;string&gt;</c> space image, which is not merely the wrong value — the
    /// generated C# does not compile (CS1503 on the first reference).
    /// <para>The adaptation is pure ALIASING and admits exactly the same carrier type, because §14.8.2.3.2
    /// leaves no crossing to invent here: <i>"If either the argument or the formal parameter is of class
    /// pointer, the corresponding formal parameter or argument shall be of class pointer and the corresponding
    /// items shall be of the same category"</i>, and its object-reference rules 1–3 likewise force the same
    /// universal/interface-name/object-class-name on both sides. Same category plus same class means the same
    /// <c>PicInfo.ClrType</c>, so a conforming pairing IS a same-<c>T</c> carrier (§14.2.3 GR8's "same storage
    /// area", realized with zero indirection). A carrier of any other shape means the two sides disagreed about
    /// the crossing — it degrades to the loud omitted carrier rather than reinterpreting storage, the same way
    /// <see cref="VarGroup"/> does.</para>
    /// <para>⛔ The ONE other carrier a slot admits is <see cref="PredefinedNullArgument"/> — the predefined NULL
    /// written as the argument (kb/Work PB1630). It carries no storage of any type, because its category is the
    /// FORMAL's (§8.4.3.10.3 SR1 names "the associated data item's class"), so the formal's adapter supplies the
    /// value: a fresh cell holding the null of the formal's own carrier (<see cref="PredefinedNull{T}"/>).</para></summary>
    public static ManagedPointer<T> Slot<T>(CobolArg[] args, int i)
    {
        if (!Present(args, i)) return Omitted<T>();
        return args[i].Carrier switch
        {
            ManagedPointer<T> mp => mp,
            PredefinedNullArgument => ManagedPointer<T>.Cell(PredefinedNull<T>()),
            _ => Unreadable<T>(args, i, "a pointer or object-reference formal"),
        };
    }

    /// <summary>The BY VALUE / BY CONTENT twin of <see cref="Slot{T}"/> (ISO §14.2.3 GR10 — a record "allocated
    /// by the activating runtime element", the argument its sending operand in <i>"a SET statement"</i> when the
    /// formal is of class object or pointer): a DETACHED cell holding the argument's reference value, so the
    /// callee's stores never reach the caller's storage. That SET of one pointer (or object reference) from
    /// another of the same category IS the reference copy this cell performs — there is no conversion for it
    /// to apply, which is why the numeric lane's landing machinery has no counterpart here. A NULL argument
    /// (§14.9.4.3 SR22 admits identifier-4 of class pointer or object BY VALUE, and NULL is one — §8.4.3.1.2
    /// Format 8) takes the same arm <see cref="Slot{T}"/> does.</summary>
    public static ManagedPointer<T> SlotValue<T>(CobolArg[] args, int i)
    {
        if (!Present(args, i)) return Omitted<T>();
        return args[i].Carrier switch
        {
            ManagedPointer<T> mp => ManagedPointer<T>.Cell(mp.Value),
            PredefinedNullArgument => ManagedPointer<T>.Cell(PredefinedNull<T>()),
            _ => Unreadable<T>(args, i, "a BY VALUE pointer or object-reference formal"),
        };
    }

    /// <summary>⛔ THE NULL VALUE OF A MANAGED-SLOT CARRIER — the ONE runtime statement of it (kb/Work PB1630). §8.4.3.10.4
    /// GR1–GR3: associated with a data-pointer, program-pointer or function-pointer, the predefined address NULL
    /// "references a data item of category data-pointer that contains the null address" (and its program / function
    /// twins); §8.4.3.7.4 GR1: the NULL object reference "contains the null object reference value". A data pointer's
    /// is the <see cref="ManagedPointer.Null"/> carrier, never a CLR null a later dereference would trip on;
    /// <see cref="ProgramPointer"/> and <see cref="FunctionPointer"/> are readonly structs whose <c>default</c> IS their
    /// null address; an object reference's CLR null IS its null reference. The compiler's twin is each managed
    /// item's <c>PicInfo.DefaultInitializer</c>, which the INVOKE lane and INITIALIZE's implicit SET TO NULL render.</summary>
    internal static T PredefinedNull<T>() =>
        typeof(T) == typeof(ManagedPointer) ? (T)(object)ManagedPointer.Null : default!;

    /// <summary>⛔ THE INITIAL IMAGE OF AN ANY LENGTH RETURNING ITEM (kb/Work PB1167) — ISO §13.18.2.4 GR1 b): the item is
    /// "treated as though there were n repetitions of the picture symbol in the character-string in its PICTURE clause,
    /// where n is the length of the corresponding argument or returning item of the activating runtime element", and
    /// §14.8.3.3 rule 5 makes the sending returning item's length "considered to match" the receiver's. n is therefore
    /// known to the activator and to nobody else, so it crosses with the receiver (<see cref="CobolArg"/>) and the
    /// activated unit seeds the item at activation entry, before its first statement can examine or store into it:
    /// <list type="number">
    /// <item>the receiver states a fixed character length (<see cref="CobolArg.Length"/>) — that length;</item>
    /// <item>the receiver is itself ANY LENGTH or DYNAMIC LENGTH (an unstated length over a string carrier) — its
    ///   carrier's current length, which may be zero (§13.18.2.4 GR1 a));</item>
    /// <item>the activation has no receiver (a CALL without a RETURNING phrase) — GR1 is silent, so the declared
    ///   PICTURE's length (<paramref name="declared"/>; docs/CONFORMANCE.md records the determination).</item>
    /// </list>
    /// <paramref name="fill"/> is the item's initial character — a space, or '0' (a clear bit) for a boolean item.</summary>
    public static string ReturningSeed(CobolArg? ret, char fill, int declared) =>
        new(fill, ret switch
        {
            { Length: not CobolArg.Unstated } r => r.Length,
            { Carrier: ManagedPointer<string> { Value: { } text } } => text.Length,
            _ => declared,
        });

    /// <summary>Deliver a RETURNING value to the caller's RETURNING item (ISO §14.6.5 — "the result is placed in
    /// the data item referenced by that RETURNING phrase of that activating statement"). Null-tolerant: a CALL
    /// without RETURNING discards the value (§14.9.4.4 GR4 has no receiver). <paramref name="ret"/> is the
    /// receiver's carrier AND description (<see cref="CobolArg"/> — kb/Work PB962/PB965), built by the activating
    /// element exactly as it builds a BY REFERENCE argument.
    /// <para>These description-free numeric legs serve a returning item with no numeric profile (USAGE INDEX);
    /// every fixed-point returning item delivers through <see cref="StoreReturn(CobolArg?, Int128, NumProfile)"/>
    /// or <see cref="StoreReturn(CobolArg?, string, NumProfile)"/>, which carry the sending description.</para></summary>
    public static void StoreReturn(CobolArg? ret, long value) => StoreReturnNum(ret, value, null);

    /// <inheritdoc cref="StoreReturn(CobolArg?, long)"/>
    public static void StoreReturn(CobolArg? ret, ulong value) => StoreReturnNum(ret, (Int128)value, null);

    /// <inheritdoc cref="StoreReturn(CobolArg?, long)"/>
    public static void StoreReturn(CobolArg? ret, Int128 value) => StoreReturnNum(ret, value, null);

    /// <inheritdoc cref="StoreReturn(CobolArg?, long)"/>
    /// <remarks>The string leg renders the UNSIGNED value's own text BEFORE the bits reinterpretation — the
    /// Int128 lane carries a top-half UInt128 as negative bits, which is exactly right for a typed cell and
    /// exactly wrong for a character image.</remarks>
    public static void StoreReturn(CobolArg? ret, UInt128 value)
    {
        if (ret is { Carrier: ManagedPointer<string> sp } r) { sp.Value = FitToReceiver(value.ToString(), r); return; }
        StoreReturnNum(ret, unchecked((Int128)value), null);
    }

    /// <summary>⛔ A FIXED-POINT RETURNING ITEM HELD AS A NATIVE CELL (kb/Work PB962). <paramref name="sent"/> is
    /// the returning item's own description. §14.6.5 places "the content of the data item referenced by that
    /// RETURNING phrase" into the receiver, and §14.8.3.3 makes a conforming receiver's PICTURE and USAGE the
    /// same as the sender's — so the delivery is a CONTENT transfer under that one description: the value into a
    /// native cell, and into an image-carried receiver the value's character representation under the
    /// description, never the value's C# text (<c>Int128.ToString</c> dropped the sign's representation and the
    /// digit count — a <c>-12.5</c> in <c>PIC S9(3)V9</c> arrived in an image-carried receiver as <c>+12.5</c>).
    /// An image-carried receiver's string carrier speaks the item's STORAGE image (its write half stores it as
    /// it stands — kb/Work PB970), which is <see cref="CobolNum.FormatImage(Int128, in NumProfile)"/>.</summary>
    public static void StoreReturn(CobolArg? ret, Int128 value, NumProfile sent) => StoreReturnNum(ret, value, sent);

    /// <inheritdoc cref="StoreReturn(CobolArg?, Int128, NumProfile)"/>
    public static void StoreReturn(CobolArg? ret, UInt128 value, NumProfile sent)
    {
        // A 16-byte unsigned container beyond Int128's range only arises from a COMP-5 capacity value; its
        // character representation is its own digits (the bits lane would read negative).
        if (ret is { Carrier: ManagedPointer<string> sp } r && value > (UInt128)Int128.MaxValue)
        {
            sp.Value = FitToReceiver(CobolNum.FormatImage(value, sent), r);
            return;
        }
        StoreReturnNum(ret, unchecked((Int128)value), sent);
    }

    /// <summary>⛔ A FLOATING-POINT RETURNING ITEM (kb/Work PB962's sibling sweep — the delivery set was "total
    /// over the carriers" except this one: a <c>USAGE FLOAT-LONG</c> returning item's <c>double</c> matched no
    /// overload and the generated C# did not compile, CS0315). §14.8.3.3 gives a conforming receiver the same
    /// USAGE, so the value lands in the receiver's float cell as it stands; any other receiver is a pair
    /// §14.8.3.3 does not admit (§14.9.4.4 GR3 d)), loud rather than a guessed conversion.</summary>
    public static void StoreReturn(CobolArg? ret, double value)
    {
        if (ret is not { Carrier: var c }) return;
        if (WriteRealCell(c, value)) return;
        Undeliverable(c, $"the floating-point result {value}");
    }

    private static void StoreReturnNum(CobolArg? ret, Int128 value, NumProfile? sent)
    {
        if (ret is not { Carrier: var c } r) return;
        if (WriteNumericCell(c, value)) return;
        if (WriteRealCell(c, sent is { } rs ? CobolFloat.ScaledToDouble(value, rs.FractionScale) : (double)value)) return;
        if (c is ManagedPointer<string> sp)
        {
            // An image-carried receiver's carrier is its STORAGE image (kb/Work PB970), so the content arrives
            // in a record representation — §14.8.3.3's conforming receiver has the SAME description as the sender,
            // so the two are one. A non-conforming pair under unchecked GR3 d) is stored under the RECEIVER's own
            // description (the value a native-cell receiver would take), never the sender's, so the item's image
            // keeps its own width (kb/Work PB1040).
            sp.Value = (r.Num ?? sent) is { } s ? CobolNum.FormatImage(value, s) : FitToReceiver(value.ToString(), r);
            return;
        }
        Undeliverable(c, $"the numeric result {value}");
    }

    /// <summary>⛔ A FIXED-POINT RETURNING ITEM HELD AS A CHARACTER IMAGE (a redefined or otherwise image-carried
    /// item — kb/Work PB962). <paramref name="text"/> is the item's STORAGE image (kb/Work PB970) and <paramref name="sent"/> its
    /// description. This is a CONTENT transfer, not a value conversion: §14.6.5 places "the content of the data
    /// item" into the receiver, and §14.8.3.3 gives a conforming receiver the same PICTURE and USAGE. So an
    /// image-carried receiver takes the text as it stands, and a native cell takes it decoded under the ONE
    /// description both items share — THE record-image codec (<see cref="CobolNum.ParseImage"/>, whose zoned arm
    /// is <see cref="CobolNum.ParseDisplay"/> and answers for ANY content). Before PB962 this pair rode the
    /// description-free leg below, which re-parsed the text as a C# number and ABORTED the run unit with
    /// EC-PROGRAM-ARG-MISMATCH whenever the content was not a digit run (spaces after a zero-length MOVE) —
    /// citing §14.8.3.3 against a pair §14.8.3.3 admits.
    /// <para>A usage-DISPLAY receiver never reaches the native arm below: a CALL RETURNING receiver is a character
    /// channel, so the compiler stores it as its image (kb/Work PB992, <c>StorageFormPass</c>). For the other
    /// usages (kb/Work PB970) a NATIVE cell holds a VALUE, not characters, so content that is not a valid numeric
    /// representation arrives as the value <c>ParseImage</c> reads from it — the same residue every character
    /// view of a native numeric cell has (a BY REFERENCE character formal's store, a group MOVE into it).</para></summary>
    public static void StoreReturn(CobolArg? ret, string text, NumProfile sent)
    {
        if (ret is not { Carrier: var c } r) return;
        if (c is ManagedPointer<string> sp) { sp.Value = ImageForReceiver(text, sent, r); return; }
        // The text is the returning item's STORAGE image (kb/Work PB970), decoded by THE record-image codec
        // under its own description — the float lane on its own decode.
        if (sent.ByteForm is NumericByteForm.Ieee32 or NumericByteForm.Ieee64)
        {
            StoreReturn(ret, CobolNum.ParseImageFloat(text, sent));
            return;
        }
        Int128 v = CobolNum.ParseImage(text, sent);
        if (WriteNumericCell(c, v)) return;
        if (WriteRealCell(c, CobolFloat.ScaledToDouble(v, sent.FractionScale))) return;
        Undeliverable(c, $"the numeric result \"{text}\"");
    }

    /// <summary>String-shaped RETURNING delivery of a returning item with NO numeric description — an
    /// alphanumeric (or other character-class) elementary item, or a group with no table.
    /// <para>⛔ TOTAL OVER THE CARRIERS THIS COMPILER EMITS, AND NEVER A SILENT NO-OP (kb/Work PB165 closing
    /// GR-14.9.4.4-4). §14.9.4.4 GR4 says "the result of the activated program is placed into identifier-3";
    /// nothing happening is the one outcome that rule excludes.</para>
    /// <para>A numeric receiver of a character-class sender is a pair §14.8.3.3 declares non-conforming (it
    /// requires the same PICTURE and USAGE clauses), which §14.9.4.4 GR3d makes EC-PROGRAM-ARG-MISMATCH. The
    /// delivery reads the result's DIGIT IMAGE — the characters the receiver would have seen had the pair
    /// conformed by length — and a result with no such reading is loud rather than lost. A NUMERIC sender never
    /// reaches this leg: it carries its description (<see cref="StoreReturn(CobolArg?, string, NumProfile)"/>).</para></summary>
    public static void StoreReturn(CobolArg? ret, string value)
    {
        if (ret is not { Carrier: var c } r) return;               // a CALL without RETURNING discards (GR4 has no receiver)
        // A CHARACTER receiver takes the text. A NUMERIC receiver on a character carrier (an image-stored item —
        // every usage-DISPLAY CALL RETURNING receiver is one, kb/Work PB992) is the non-conforming pair below,
        // and reads the same digit image a native cell does, written back in its own record representation.
        if (c is ManagedPointer<string> sp && r.Num is null) { sp.Value = FitToReceiver(value, r); return; }
        // A table-less fixed group into a variable-length receiver (kb/Work PB965): its one §8.5.1.12 fact is
        // its length (CobolVarGroup.FixedRun).
        if (c is ManagedPointer<CobolVarGroup> vp && r.Layout is { } rl
            && CobolVarGroup.CorrespondingSpans(CobolVarGroup.FixedRun(value.Length), rl) is { } vspans)
        {
            vp.Value = CobolVarGroup.FromFixedImage(value, vspans);
            return;
        }
        string t = value.Trim();
        if (Int128.TryParse(t, out Int128 v))
        {
            if (WriteNumericCell(c, v)) return;
            if (c is ManagedPointer<string> ns && r.Num is { } rn) { ns.Value = CobolNum.FormatImage(v, rn); return; }
        }
        if (double.TryParse(t, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double d) && WriteRealCell(c, d)) return;
        Undeliverable(c, $"the character result \"{value}\"");
    }

    /// <summary>⛔ A FIXED-LENGTH GROUP returning item with a table (kb/Work PB965). <paramref name="layout"/> is
    /// its §8.5.1.12 layout. §14.8.3.2: "If either the sending or the receiving operand is a variable length
    /// group, the sending operand and the receiving operand shall be compatible, as described in 8.5.1.12" — so
    /// a VARIABLE-length receiver is legal, and the image decomposes into its carrier at the spans the two
    /// layouts correspond at (<see cref="CobolVarGroup.CorrespondingSpans"/>), the fixed table crossing at its
    /// occurrence count (§8.5.1.12.3 sentence 3). A character receiver takes the image as it stands.</summary>
    public static void StoreReturnGroup(CobolArg? ret, string image, int[] layout)
    {
        if (ret is not { Carrier: var c } r) return;
        if (c is ManagedPointer<string> sp) { sp.Value = FitToReceiver(image, r); return; }
        if (c is ManagedPointer<CobolVarGroup> vp && r.Layout is { } rl
            && CobolVarGroup.CorrespondingSpans(layout, rl) is { } spans)
        {
            vp.Value = CobolVarGroup.FromFixedImage(image, spans);
            return;
        }
        Undeliverable(c, "the group result");
    }

    /// <summary>The §8.5.1.12 layout of a FIXED-length group on a character carrier: the one it carries, or —
    /// for a group with no table, which carries none — the single fixed run of its length
    /// (<see cref="CobolVarGroup.FixedRun"/>).</summary>
    private static int[] FixedLayoutOf(in CobolArg a, ManagedPointer<string> sp) =>
        a.Layout ?? CobolVarGroup.FixedRun(sp.Value?.Length ?? 0);

    /// <summary>The pair's correspondence spans when a VARIABLE-length group argument <paramref name="a"/> meets a
    /// fixed-length GROUP formal of <paramref name="width"/> characters whose layout is
    /// <paramref name="groupLayout"/> (the empty array = a table-less group, <see cref="CobolVarGroup.FixedRun"/>);
    /// null for a non-group formal, an argument that carries no layout, or a pair that does not correspond.</summary>
    private static int[]? VarGroupSpans(in CobolArg a, int[]? groupLayout, int width) =>
        groupLayout is null || a.Layout is not { } argLayout ? null
        : CobolVarGroup.CorrespondingSpans(groupLayout.Length == 0 ? CobolVarGroup.FixedRun(width) : groupLayout, argLayout);

    /// <summary>⛔ THE UNCHECKED RETURNING STORE INTO A TEXT-CARRIED RECEIVER (kb/Work PB1040): <paramref name="image"/>
    /// in the receiver's OWN width. §14.9.4.4 GR3 d) makes a result whose length does not conform an
    /// EC-PROGRAM-ARG-MISMATCH "if checking for it is enabled in both the activated program and activating runtime
    /// element" (<see cref="ProgramTable.CallProgram"/> raises it before the activation); with checking off the rule
    /// leaves the content to the implementor, and the receiver's string carrier is its item's whole image, so an
    /// image of any other length would corrupt the item (<c>FUNCTION LENGTH</c> says 5 while the image is 3). The
    /// store is the alphanumeric receiving rule of §14.6.8.5 — "aligned at the leftmost character position in the data
    /// item with space fill or truncation to the right" — which is what <see cref="CobolString.Store"/> states and
    /// what every other fixed-width character store here does (a JUSTIFIED RIGHT receiver's differing alignment
    /// belongs to the conforming pair, which the checked path guarantees and this one does not have).
    /// A receiver that states no length (<see cref="CobolArg.Unstated"/>) takes the image as it stands.</summary>
    private static string FitToReceiver(string image, in CobolArg receiver) =>
        receiver.Length == CobolArg.Unstated ? image : CobolString.Store(image, receiver.Length);

    /// <summary>A numeric sender's STORAGE image <paramref name="text"/> for a text-carried receiver
    /// (<see cref="StoreReturn(CobolArg?, string, NumProfile)"/>): as it stands when it is the receiver's width — the
    /// conforming pair, and the only one that may hold content that is not a valid numeric representation
    /// (kb/Work PB962) — otherwise the unchecked GR3 d) store: the VALUE under the receiver's own description when it
    /// has one (a numeric receiver reads as the number the sender held, as a native-cell receiver does), else the
    /// alphanumeric fit of <see cref="FitToReceiver"/>. A floating-point sender's bytes have no digit reading and
    /// take the fit.</summary>
    private static string ImageForReceiver(string text, in NumProfile sent, in CobolArg receiver) =>
        receiver.Length == CobolArg.Unstated || text.Length == receiver.Length ? text
        : receiver.Num is { } rn && sent.ByteForm is not (NumericByteForm.Ieee32 or NumericByteForm.Ieee64)
            ? CobolNum.FormatImage(CobolNum.ParseImage(text, sent), rn)
            : CobolString.Store(text, receiver.Length);

    /// <summary>The RETURNING delivery has no leg for this (result shape, carrier) pair — §14.9.4.4 GR4's
    /// "placed into identifier-3" cannot be honoured, and §14.8.3's conformance rules are what such a pair
    /// violates (§14.9.4.4 GR3d ⇒ EC-PROGRAM-ARG-MISMATCH). Raised through the same
    /// <see cref="CobolCallException"/> the activation-time GR3d count check uses, so a run unit sees ONE
    /// EC-PROGRAM-ARG-MISMATCH mechanism — never a silent discard, which is what this replaced.</summary>
    private static void Undeliverable(ManagedPointer ret, string what) =>
        throw new CobolCallException(
            $"RETURNING delivery: {what} has no conforming store into the activating element's RETURNING item "
            + $"(carrier {ret.GetType().Name}) — ISO §14.8.3 conformance; §14.9.4.4 GR3d — EC-PROGRAM-ARG-MISMATCH",
            "EC-PROGRAM-ARG-MISMATCH");

    /// <summary>Variable-length-group RETURNING delivery (ISO §14.8.3.2's compatibility sentence — the
    /// returning half of the same admission §14.8.2.2 grants arguments; kb/Work PB204). <paramref name="layout"/>
    /// is the sending group's §8.5.1.12 layout: a FIXED-length receiver is legal too (§8.5.1.12.1 "only one of
    /// the operands may be a variable-length group"), and the carrier is rebuilt into its image at the spans the
    /// receiver's layout corresponds at — a dynamic table's occurrences fitted to the fixed table as §14.6.9.2
    /// fits a dynamic sender into a non-dynamic receiver (kb/Work PB965).</summary>
    public static void StoreReturn(CobolArg? ret, CobolVarGroup value, int[] layout)
    {
        if (ret is not { Carrier: var c } r) return;
        if (c is ManagedPointer<CobolVarGroup> vp) { vp.Value = value; return; }
        if (c is ManagedPointer<string> sp && CobolVarGroup.CorrespondingSpans(FixedLayoutOf(r, sp), layout) is { } spans)
        {
            sp.Value = CobolVarGroup.ToFixedImage(value, sp.Value?.Length ?? 0, spans);
            return;
        }
        Undeliverable(c, "the variable-length-group result");
    }

    /// <summary>Data-pointer RETURNING delivery (kb/Work PB133 wave B — §14.2.3 GR7 over a USAGE POINTER
    /// item; the PB111 shape: legal source drew CS1503 because no overload matched the carrier type).</summary>
    public static void StoreReturn(CobolArg? ret, ManagedPointer value)
    {
        if (ret is not { Carrier: var c }) return;
        if (c is ManagedPointer<ManagedPointer> pp) { pp.Value = value; return; }
        Undeliverable(c, "the data-pointer result");
    }

    /// <summary>Program-pointer RETURNING delivery (kb/Work PB133 wave B — §13.18.60 GR24's identity
    /// struct crosses by value; same CS1503 shape as the data pointer).</summary>
    public static void StoreReturn(CobolArg? ret, ProgramPointer value)
    {
        if (ret is not { Carrier: var c }) return;
        if (c is ManagedPointer<ProgramPointer> pp) { pp.Value = value; return; }
        Undeliverable(c, "the program-pointer result");
    }

    /// <summary>Function-pointer RETURNING delivery (§13.18.60.4 GR26's identity struct crosses by value, the
    /// program-pointer twin above). Before this overload a FUNCTION-ID or program RETURNING a USAGE FUNCTION-POINTER item
    /// generated <c>StoreReturn(__ret, L_RES)</c> that bound only to the object-reference generic and drew CS0315 — the
    /// PB111 / PB133 shape (legal source refused by the backend because no overload matched the carrier type), one
    /// pointer category after the two that were fixed.</summary>
    public static void StoreReturn(CobolArg? ret, FunctionPointer value)
    {
        if (ret is not { Carrier: var c }) return;
        if (c is ManagedPointer<FunctionPointer> fp) { fp.Value = value; return; }
        Undeliverable(c, "the function-pointer result");
    }

    /// <summary>Object-reference RETURNING delivery (kb/Work PB133 wave B). The CobolObject constraint keeps
    /// this overload away from every numeric/string carrier (a value type or string never derives it), so the
    /// specific lanes above stay untouched. An IDENTICALLY-described returning pair (the §14.8.3 conforming
    /// case a prototype-less CALL can realize today) matches the typed carrier exactly. A DIFFERENTLY-described
    /// pair that conforms — §14.8.3.3 rule 1: "as if a SET statement were performed" with the activated returning
    /// item as the sender, so a subclass-typed sender into a superclass-typed or universal receiver — stores through
    /// the carrier's own type (<see cref="ManagedPointer.TryAssign"/>, kb/Work PB1164); the bind-time conformance
    /// (<c>ParameterConformance.ReturningConformanceReason</c>) has already refused the other direction.</summary>
    public static void StoreReturn<T>(CobolArg? ret, T? value) where T : CobolObject
    {
        if (ret is not { Carrier: var c }) return;
        if (c is ManagedPointer<T?> tp) { tp.Value = value; return; }
        if (c.TryAssign(value)) return;
        Undeliverable(c, "the object-reference result");
    }

    /// <summary>⛔ A SUPPLIED ARGUMENT THE FORMAL CANNOT READ (kb/Work PB615) — never the omitted carrier. Every
    /// adapter's type switch ends here once its readable arms are exhausted: the argument's carrier is outside the
    /// shapes the formal's crossing form can take, which only a pairing that violates §14.8.2's conformance rules
    /// (or a compiler defect) produces. §14.9.4.4 GR3d: "If a violation of these rules is detected, the
    /// EC-PROGRAM-ARG-MISMATCH exception condition is set to exist if checking for it is enabled in both the
    /// activated program and activating runtime element, the program call is not successful" — so this is an
    /// ACTIVATION failure, raised while the activated element adopts its formals and before any of its
    /// statements runs. It rides the same <see cref="CobolCallException"/> the GR3d argument-count check and
    /// the RETURNING delivery (<see cref="Undeliverable"/>) use — one EC-PROGRAM-ARG-MISMATCH mechanism — marked
    /// <see cref="CobolCallException.RaisedAtAdoption"/> so the activation boundary keeps it attributable to THIS
    /// CALL's GR3h rather than marking it as a condition propagated from the called program (GR3i).
    /// <para>Before PB615 each switch fell to <see cref="Omitted{T}"/>, whose read answered <c>default</c> — a
    /// supplied argument read as zero, silently, indistinguishable from an omitted one.</para></summary>
    private static ManagedPointer<T> Unreadable<T>(CobolArg[] args, int position, string formal) =>
        throw new CobolCallException(
            $"CALL argument #{position + 1}: its carrier ({args[position].Carrier.GetType().Name}) cannot be read "
            + $"through {formal} — the argument and the formal parameter do not conform (ISO §14.8.2 via "
            + "§14.9.4.4 GR3d — EC-PROGRAM-ARG-MISMATCH)",
            "EC-PROGRAM-ARG-MISMATCH") { RaisedAtAdoption = true };

    /// <summary>The omitted/absent CALL argument's carrier (ISO §14.9.4.4 GR11; kb/Work PB133 wave C): IsNull
    /// answers true — what makes the §8.8.4.8 omitted-argument condition and GR1c's TRANSITIVE omission work
    /// through the ordinary Present test. Reads answer the type's benign empty value and stores are ignored.
    /// <para>⛔ The carrier RAISES NOTHING (kb/Work PB971). The *-ARG-OMITTED condition is a fact about the
    /// REFERENCE and about the kind of element that owns the formal (§14.9.4.4 GR12 program, §8.4.3.2.4 GR8
    /// function, §14.9.23.4 GR10 method), so it is raised at the rendered reference by
    /// <see cref="OmittedFormal"/>. This carrier used to raise EC-PROGRAM-ARG-OMITTED on read: the PROGRAM name
    /// in a function, and never for a group formal (whose reference reads the callee's boundary copy, not this
    /// carrier). Checking off, GR12 leaves the content undefined — the benign value is the documented
    /// implementor choice.</para></summary>
    private static ManagedPointer<T> Omitted<T>() => ManagedPointer<T>.OmittedArgument(
        () =>
        {
            // The benign empty value per carrier shape — a REFERENCE carrier must not hand back null and
            // turn a documented GR12 leniency into an NRE (kb/Work PB204 added the var-group carrier).
            if (typeof(T) == typeof(string)) return (T)(object)"";
            if (typeof(T) == typeof(CobolVarGroup)) return (T)(object)CobolVarGroup.Empty;
            // Every other carrier's benign empty value is its NULL (a managed slot — kb/Work PB663: `default` for a
            // data pointer would hand back a CLR null and make the documented GR12 leniency an NRE the first time
            // the callee referenced the formal, the trap the string and var-group arms exist for) or its
            // `default` (a native numeric cell), which PredefinedNull answers for both.
            return PredefinedNull<T>();
        },
        // GR12 leaves a store into the omitted formal undefined: it is ignored (there is no caller storage).
        _ => { });
}
