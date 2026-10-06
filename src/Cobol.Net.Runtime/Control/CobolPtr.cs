// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Runtime;

/// <summary>
/// The data-pointer runtime operations (Phase-4b increment 2 — ADDRESS OF / BASED / SET ADDRESS OF /
/// ALLOCATE-FREE over the ONE <see cref="ManagedPointer"/> carrier and the ONE <see cref="StorageCell"/>
/// shared-storage shape; PHASE4_RECONCILIATION "M2-DATA-5 / M2-PROC-5 — increment 2" design). Helpers, not a
/// second carrier: every pointer VALUE stays a <see cref="ManagedPointer"/>; every pointed storage stays a
/// character-image cell (never a byte substrate). Fatal exception conditions throw
/// <see cref="CobolFatalException"/> (the documented runtime raise-point channel; §14.6.13.1.3 #7/#8
/// loud-failure doctrine); nonfatal ones report through out-parameters for the emitter's TurnState-gated
/// <c>ExceptionState.Set</c> block.
/// </summary>
public static class CobolPtr
{
    /// <summary>Dereference a BASED item's implicit pointer for a view of <paramref name="classWidth"/>
    /// characters (ISO §13.18.5): GR3 — a reference while the pointer is NULL is <c>EC-DATA-PTR-NULL</c>
    /// (Fatal); GR4 — an invalid address (a freed cell, a non-window carrier, or a window that does not fit
    /// inside the cell) is <c>EC-BOUND-PTR</c> (Fatal). Returns the cell whose <c>Ref</c> the generated
    /// bridge property aliases.</summary>
    public static StorageCell Deref(ManagedPointer? p, long classWidth)
    {
        // ⛔ THESE FOUR THROW WHETHER OR NOT CHECKING IS ON, and that is the owner's decided rule rather than an
        // oversight: checking-off is lenient only where the standard NAMES the outcome, and §13.18.5.4 GR3/GR4
        // name none. This method must return a StorageCell, so "lenient" could only mean fabricating one and
        // letting the program run on garbage — which no other COBOL does (GnuCOBOL and gcobol list both
        // conditions fatal and otherwise SIGSEGV; Micro Focus traps to RTS 114; IBM abends S0C4; NetCOBOL
        // JMP0071I-U). The helper is still called FIRST when checking is on, so the last-exception status is set
        // and the statement guard's `catch … when (EcName == …)` can select a USE declarative.
        // An OMITTED formal's area answers IsNull — its presence fact — yet addresses a real cell, so an unchecked
        // reference reads blank storage (CellPointer.OmittedArgument; kb/Work PB2087).
        if (p is null || p.IsNull && p is not CellPointer)
        {
            ExceptionState.DataPtrNullError(
                "reference to a based item whose data-address pointer is NULL (ISO 13.18.5.4 GR3)");
            throw new CobolFatalException("EC-DATA-PTR-NULL",
                "reference to a based item whose data-address pointer is NULL (ISO 13.18.5.4 GR3)");
        }
        if (p is not CellPointer w)
        {
            ExceptionState.BoundPtrError(
                "reference to a based item whose data-address pointer does not address data storage (ISO 13.18.5.4 GR4)");
            throw new CobolFatalException("EC-BOUND-PTR",
                "reference to a based item whose data-address pointer does not address data storage (ISO 13.18.5.4 GR4)");
        }
        if (w.Generation != w.Cell.Generation)
        {
            // The storage this pointer named has ENDED (kb/Work PB1216): FREE, the end of the activation that owned it,
            // or a CANCEL — §13.18.5.4 GR4's "not a valid address of storage", with §8.6.5 naming the ways the
            // association ceases ("the actual data no longer exists, as specified 8.6.4 … and in 14.9.3").
            string ended = DanglingDetail(w.Cell.LastEnd);
            ExceptionState.BoundPtrError(ended);
            throw new CobolFatalException("EC-BOUND-PTR", ended);
        }
        // ⛔ THE SUBTRACTION IS ON THE CELL SIDE, and that is the whole point (kb/Work PB465). The obvious
        // `w.Offset + classWidth > w.Cell.Ref.Length` WRAPS at 2^63 — and an offset that high is now a REACHABLE
        // pointer value, because DOC-A.1-216 makes every representable address a legal data-pointer VALUE and
        // defers the failure to exactly this test. Measured before the fix: two `SET P UP BY 9223372036854775807`s
        // produced a window that passed this bound and read position 0 of a 4-character cell. Written this way the
        // test is exact for EVERY `long` offset with no wide arithmetic on a path every based read takes:
        // `Ref.Length` is an `int` and `classWidth` a non-negative class width, so their difference cannot
        // overflow `long`, and a negative difference correctly fails every offset.
        if (w.Offset < 0 || w.Offset > (long)w.Cell.Ref.Length - classWidth)
        {
            string oob = $"reference to a based item outside its addressed storage (offset {w.Offset} + width "
                + $"{classWidth} over {w.Cell.Ref.Length} positions — ISO 13.18.5.4 GR4)";
            ExceptionState.BoundPtrError(oob);
            throw new CobolFatalException("EC-BOUND-PTR", oob);
        }
        return w.Cell;
    }

    /// <summary>The EC-BOUND-PTR detail of a dereference through a pointer whose storage ended for <paramref name="why"/>.</summary>
    private static string DanglingDetail(StorageEnd why) => why switch
    {
        StorageEnd.Freed =>
            "reference to a based item addressing storage released by FREE (ISO 14.9.15 GR1a / 13.18.5.4 GR4)",
        StorageEnd.ActivationEnded =>
            "reference to a based item addressing storage whose life ended with its activation — a LOCAL-STORAGE or "
            + "initial item persists only while the runtime element is in active state (ISO 8.6.4 / 8.6.5 / 13.18.5.4 GR4)",
        StorageEnd.Cancelled =>
            "reference to a based item addressing static storage whose life ended at a CANCEL of the program that "
            + "contains it (ISO 8.6.4 / 8.6.5 / 13.18.5.4 GR4)",
        _ => "reference to a based item addressing storage that no longer exists (ISO 8.6.5 / 13.18.5.4 GR4)",
    };

    /// <summary>The window pointer's character offset (0 for the null carrier — <see cref="Deref"/> trips
    /// FIRST on every generated read/write path, so this never masks a null dereference).</summary>
    public static long OffsetOf(ManagedPointer? p) => p is CellPointer w ? w.Offset : 0;

    /// <summary>The LOWEST value a data-pointer data item may hold, as a character-position displacement from
    /// the origin of the storage it addresses (ISO §14.9.39.4 GR20's "the range of values allowed by the
    /// implementor for a data-pointer data item"; §A.1 216 requires it documented, and
    /// <c>docs/CONFORMANCE.md</c> §7 DOC-A.1-216 does).</summary>
    public const long MinAddress = long.MinValue;

    /// <summary>The HIGHEST value a data-pointer data item may hold (the <see cref="MinAddress"/> twin —
    /// §14.9.39.4 GR20 · §A.1 216 · DOC-A.1-216).</summary>
    public const long MaxAddress = long.MaxValue;

    /// <summary>The widest displacement that could possibly carry an in-range address to another in-range
    /// address, <see cref="MaxAddress"/> − <see cref="MinAddress"/> = 2^64 − 1. Testing the amount against it
    /// FIRST is what lets the sum below be formed in <see cref="Int128"/> without overflowing it in turn —
    /// the boundary has to be a value this code can SEE, never a wrap it cannot (kb/Work PB459's lesson,
    /// applied to the pointer carrier by PB465).</summary>
    private static readonly Int128 AddressSpan = (Int128)MaxAddress - MinAddress;

    /// <summary>Displace a data pointer (ISO §14.9.39.4): GR18 — a NULL operand is <c>EC-DATA-PTR-NULL</c>
    /// (Fatal); GR20 — "the address contained in each identifier-9 … is incremented, if UP is specified, or
    /// decremented, if DOWN is specified, by the number of bytes specified by arithmetic-expression-3"
    /// (character positions in this model), and "if this new address is outside the range of values allowed by
    /// the implementor for a data-pointer data item, the EC-RANGE-PTR exception condition is set to exist and
    /// the value of the data item referenced by identifier-9 is unchanged".
    /// <para>⛔ The range GR20 names is <see cref="MinAddress"/>..<see cref="MaxAddress"/> — the addresses this
    /// implementation can represent, NOT the bounds of the addressed storage. An address that leaves its cell
    /// but stays representable is a legal pointer VALUE; §13.18.5.4 GR4 raises EC-BOUND-PTR when it is
    /// DEREFERENCED. A cell-bounds test here would reject conforming programs (DOC-A.1-216).</para></summary>
    /// <param name="by">The displacement, as an <see cref="Int128"/>: arithmetic-expression-3 may be a
    /// 31-digit integer item, and narrowing it at the emitter would WRAP it before this guard could see it —
    /// measured, <c>SET P UP BY 18446744073709551618</c> moved the pointer by 2 (kb/Work PB465).</param>
    /// <param name="down">DOWN BY — negated HERE, after the span test, so the negation itself cannot overflow.</param>
    public static ManagedPointer UpBy(ManagedPointer? p, Int128 by, bool down = false)
    {
        // §14.9.39.4 GR19 and GR20 each state the unsuccessful outcome for this statement — "the execution of
        // the SET statement is unsuccessful, and the content of identifier-9 is unchanged" / "the value of the
        // data item referenced by identifier-9 is unchanged" — so with checking OFF these return the operand
        // UNCHANGED rather than terminating. Unlike Deref there IS a defined thing to return.
        if (p is null || p.IsNull)
        {
            ExceptionState.DataPtrNullError(
                "SET pointer UP/DOWN BY with a NULL pointer operand (ISO 14.9.39.4 GR18)");
            // `ManagedPointer.Null` IS the unchanged value here: the operand already held the predefined
            // address NULL (that is the condition), and the C#-null carrier normalises to the same thing.
            return ManagedPointer.Null;
        }
        if (p is not CellPointer w)
        {
            ExceptionState.BoundPtrError(
                "SET pointer UP/DOWN BY over a pointer that does not address data storage (ISO 14.9.39 Format 10)");
            return p;   // unchanged — a non-null carrier that simply does not address storage
        }
        if (by >= -AddressSpan && by <= AddressSpan)
        {
            Int128 r = (Int128)w.Offset + (down ? -by : by);
            if (r >= MinAddress && r <= MaxAddress) return new CellPointer(w.Cell, (long)r, w.Generation);   // the SAME life of the storage (kb/Work PB1216)
        }
        return Unrepresentable(p);
    }

    /// <summary>GR20's range arm, written ONCE for every way of reaching it: the new address is outside the
    /// implementor data-pointer range, so EC-RANGE-PTR is set to exist (Table 13 Fatal) and the operand is
    /// unchanged. ⛔ This is NOT GR19's condition — GR19 is about whether the AMOUNT is an integer, and
    /// answering a magnitude question with EC-SIZE-ADDRESS reported a condition whose antecedent was false and
    /// aborted run units on legal COBOL (kb/Work PB465).</summary>
    private static ManagedPointer Unrepresentable(ManagedPointer? p)
    {
        ExceptionState.RangePtrError(
            "a data-pointer displaced outside the implementor range of data-pointer values "
            + "(ISO 14.9.39.4 GR20; the range is DOC-A.1-216 in docs/CONFORMANCE.md §7)");
        return p ?? ManagedPointer.Null;   // GR20 verbatim — identifier-9 is unchanged
    }

    /// <summary>SET pointer UP/DOWN BY with an EXACT scaled fixed-point amount — §14.9.39.4's two rules as TWO
    /// checks with TWO outcomes (kb/Work PB465):
    /// <list type="number">
    /// <item>GR19, on the AMOUNT: "if arithmetic-expression-3 does not evaluate to an integer, the
    /// EC-SIZE-ADDRESS exception condition is set to exist, the execution of the SET statement is unsuccessful,
    /// and the content of identifier-9 is unchanged". The amount arrives as its scaled value with its scale, so
    /// the divisibility test IS the integrality test; an integer value at any scale (2.0, or 1.0E19) is NOT this
    /// case and must move normally.</item>
    /// <item>GR20, on the RESULT: an address outside <see cref="MinAddress"/>..<see cref="MaxAddress"/> is
    /// EC-RANGE-PTR with the operand unchanged — a different rule, a different condition, a different
    /// message.</item>
    /// </list>
    /// The integrality DECISION itself is <see cref="SetAmount"/>'s, shared with the three index formats that
    /// state the identical test.</summary>
    public static ManagedPointer UpByAmount(ManagedPointer? p, Int128 scaledBy, int scale, bool down)
    {
        if (SetAmount.Land(scaledBy, scale, out Int128 whole) == SetAmountLanding.NotAnInteger)
            return NotAnInteger(p);
        return UpBy(p, whole, down);
    }

    /// <summary>The NATIVE-FLOAT lane of <see cref="UpByAmount"/> (kb/Work PB151): GR19's integrality test runs
    /// on the DOUBLE — an emitter-side <c>(long)(double)</c> truncation bypasses the raise entirely. An integral
    /// amount too large for the widest integer carrier is NOT GR19's case (it IS an integer): no representable
    /// address can result from it, which is GR20's.</summary>
    public static ManagedPointer UpByAmountReal(ManagedPointer? p, double by, bool down) =>
        SetAmount.Land(by, out Int128 whole) switch
        {
            SetAmountLanding.NotAnInteger => NotAnInteger(p),
            SetAmountLanding.BeyondCarrier => Unrepresentable(p),
            _ => UpBy(p, whole, down),
        };

    /// <summary>GR19's arm, written ONCE: the amount does not evaluate to an integer, so EC-SIZE-ADDRESS is set
    /// to exist (Table 13 Fatal), the SET is unsuccessful, and identifier-9 is unchanged.</summary>
    private static ManagedPointer NotAnInteger(ManagedPointer? p)
    {
        ExceptionState.SizeAddressError(
            "SET pointer UP/DOWN BY an amount that does not evaluate to an integer (ISO 14.9.39.4 GR19)");
        return p ?? ManagedPointer.Null;   // GR19 verbatim — unsuccessful; identifier-9 unchanged
    }

    /// <summary>ALLOCATE (ISO §14.9.3): a fresh <paramref name="size"/>-character cell. GR2: a request of
    /// zero or less returns the NULL pointer and NO exception condition exists. GR5: storage NOT AVAILABLE —
    /// a request past the cell model's capacity, or an OutOfMemory landing — returns NULL with
    /// <paramref name="notAvail"/> set for the emitter's checking-gated EC-STORAGE-NOT-AVAIL block (the
    /// <see cref="Free"/> notAlloc twin; kb/Work PB151 — the old <c>checked((int)size)</c> threw an unhandled
    /// OverflowException, and the emitter's earlier <c>(long)</c> narrowing could WRAP a 20-digit request
    /// into a small VALID allocation, the PB22 cast family's unswept sibling — the size now arrives as the
    /// full <see cref="Int128"/>). GR6/GR8: <paramref name="fill"/> is the content — <c>'\0'</c> for
    /// INITIALIZED with CHARACTERS, the OPTIONS INITIALIZE clause's specified-fill-character when that
    /// clause is written (GR8), else the space (content undefined; space-filling is the conformant
    /// choice).</summary>
    public static ManagedPointer Allocate(Int128 size, char fill, out bool notAvail)
    {
        notAvail = false;
        if (size <= 0) return ManagedPointer.Null;
        if (size > int.MaxValue) { notAvail = true; return ManagedPointer.Null; }
        try
        {
            var cell = new StorageCell { Ref = new string(fill, (int)size), Allocated = true };
            return new CellPointer(cell, 0);
        }
        catch (OutOfMemoryException) { notAvail = true; return ManagedPointer.Null; }
    }

    /// <summary>ALLOCATE with a NATIVE-FLOAT arithmetic-expression-1 (kb/Work PB151): GR1's "rounded up to
    /// the next whole number" happens HERE, on the double — the old emitter's <c>(long)(double)</c>
    /// truncated 2.5 to 2, a silently undersized cell. A NaN request is "not available".</summary>
    public static ManagedPointer AllocateReal(double size, char fill, out bool notAvail)
    {
        if (double.IsNaN(size)) { notAvail = true; return ManagedPointer.Null; }
        double up = Math.Ceiling(size);                          // GR1
        if (up <= 0) { notAvail = false; return ManagedPointer.Null; }   // GR2
        if (up > int.MaxValue) { notAvail = true; return ManagedPointer.Null; }
        return Allocate((Int128)up, fill, out notAvail);
    }

    /// <summary>FREE (ISO §14.9.15 GR1): (a) a pointer addressing the START of storage obtained by ALLOCATE
    /// and not yet freed — release it (the cell's life ends, <see cref="StorageCell.End"/>, and its image is
    /// dropped; every dangling alias then fails loud at <see cref="Deref"/>, the "contents become undefined"
    /// license made loud) and the operand becomes NULL; (b) a NULL operand — no operation; (c) anything else
    /// — the operand is unchanged and <paramref name="notAlloc"/> reports the nonfatal
    /// <c>EC-STORAGE-NOT-ALLOC</c> for the emitter's TurnState-gated status block.</summary>
    public static ManagedPointer Free(ManagedPointer? p, out bool notAlloc)
    {
        notAlloc = false;
        if (p is null || p.IsNull) return ManagedPointer.Null;   // GR1b — no-op
        // "currently allocated" (GR1a): an allocated cell whose CURRENT life the pointer names — a pointer into an
        // ended life (an earlier FREE) is not the start of storage that is currently allocated, so it falls to GR1c.
        if (p is CellPointer { Offset: 0, Cell.Allocated: true } w && w.Generation == w.Cell.Generation)
        {
            w.Cell.End(StorageEnd.Freed);   // GR1a — released; dangling aliases trip Deref loud
            return ManagedPointer.Null;
        }
        notAlloc = true;           // GR1c — not the start of an allocation
        return p;
    }

    /// <summary>⛔ READ A MANAGED SLOT of a shared storage area (kb/Work PB231 — the pointer third): the value
    /// of a class-pointer / class-object member of a BASED, EXTERNAL or ADDRESS-OF-taken record, which lives
    /// in <see cref="StorageCell.SlotAt"/> rather than in the cell's byte image because a managed reference is
    /// not a byte sequence. <paramref name="nullState"/> is the item's COBOL NULL state — the value an
    /// unwritten slot has, which realizes ISO §14.9.3.4 GR9 ("data items of class object or class pointer in
    /// the allocated storage are initialized to null") and §13.18.63.4's "data items of class message-tag,
    /// class object, and class pointer are initialized to null" with ONE expression, the same
    /// <c>PicInfo.DefaultInitializer</c> an ordinary declared field is seeded from.
    /// <para>The window's byte geometry is unchanged and unused here — the slot is keyed by the SAME byte
    /// offset the byte window would have had, so the two halves of the area cannot disagree about where a
    /// member sits.</para></summary>
    public static T SlotRead<T>(StorageCell cell, int byteOffset, T nullState) =>
        cell.SlotAt(byteOffset) is T v ? v : nullState;

    /// <summary>Store a managed slot — the receiving twin of <see cref="SlotRead{T}"/>. Returns the value so
    /// the emitted store is an expression statement of the same shape the byte-window codings use.</summary>
    public static void SlotWrite(StorageCell cell, int byteOffset, object? value) =>
        cell.SetSlotAt(byteOffset, value);
}
