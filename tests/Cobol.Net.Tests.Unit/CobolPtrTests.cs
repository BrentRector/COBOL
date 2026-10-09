// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using CobolNet.Runtime.Exceptions;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The data-pointer runtime rules (Phase-4b increment 2; ISO §8.8.4.2 pointer equality, §13.18.5 GR3/GR4,
/// §14.9.3 GR1/GR2/GR6, §14.9.15 GR1, §14.9.39 F10 GR18/GR20). End-to-end behavior rides the three pointer
/// goldens; these lock the carrier/cell semantics the emitters wire to.
/// </summary>
public sealed class CobolPtrTests
{
    [Fact]
    public void SameTarget_IsStructural_OverCellAndOffset()
    {
        var cell = new StorageCell { Ref = "ABCDEFGHIJ" };
        // Two INDEPENDENT window pointers to the same address compare EQUAL (§8.8.4.2 :9772 — "equal if
        // they reference the same address"; increment 1's instance identity could not express this).
        Assert.True(ManagedPointer.SameTarget(ManagedPointer.At(cell, 3), ManagedPointer.At(cell, 3)));
        Assert.False(ManagedPointer.SameTarget(ManagedPointer.At(cell, 3), ManagedPointer.At(cell, 4)));
        Assert.False(ManagedPointer.SameTarget(ManagedPointer.At(cell, 0), ManagedPointer.At(new StorageCell { Ref = "ABCDEFGHIJ" }, 0)));
        Assert.True(ManagedPointer.SameTarget(ManagedPointer.Null, null));
        Assert.False(ManagedPointer.SameTarget(ManagedPointer.At(cell, 0), ManagedPointer.Null));
    }

    [Fact]
    public void UpBy_MovesByBytes_AndComposes()
    {
        var cell = new StorageCell { Ref = "ABCDEFGHIJ" };
        var p = ManagedPointer.At(cell, 0);
        var p4 = CobolPtr.UpBy(p, 4);
        var p2 = CobolPtr.UpBy(p4, -2);
        Assert.Equal(4, ((CellPointer)p4).Offset);      // §14.9.39 F10 GR20 — byte-granular
        Assert.Equal(2, ((CellPointer)p2).Offset);
        Assert.Equal(0, ((CellPointer)p).Offset);       // pointers are VALUES — UpBy never mutates its operand
    }

    [Fact]
    public void UpByAmount_IsTheExactGr19ValueRule()
    {
        var cell = new StorageCell { Ref = "ABCDEFGHIJ" };
        var p = ManagedPointer.At(cell, 0);
        // 2.0 at scale 1 (scaled 20) IS an integer value — moves by 2 (§14.9.39.4 GR19 is a VALUE rule).
        Assert.Equal(2, ((CellPointer)CobolPtr.UpByAmount(p, 20, 1, down: false)).Offset);
        // DOWN is the flag, not a negated amount: §14.9.39.4 GR20 decrements by the same number of bytes.
        Assert.Equal(1, ((CellPointer)CobolPtr.UpByAmount(ManagedPointer.At(cell, 3), 20, 1, down: true)).Offset);

        // 2.5 at scale 1 (scaled 25) is NOT an integer. §14.9.39.4 GR19 gives BOTH arms in one sentence: "the
        // EC-SIZE-ADDRESS exception condition is set to exist, the execution of the SET statement is
        // unsuccessful, and the content of identifier-9 is unchanged" — so checking ON raises the named
        // condition, and checking OFF leaves the operand alone. It is never a silent truncation either way.
        ExceptionState.SizeAddressChecking = true;
        try
        {
            Assert.Equal("EC-SIZE-ADDRESS",
                Assert.Throws<CobolFatalException>(() => CobolPtr.UpByAmount(p, 25, 1, down: false)).EcName);
        }
        finally { ExceptionState.SizeAddressChecking = false; }

        // Checking OFF: GR19's "content of identifier-9 is unchanged" — the operand comes back as it went in.
        Assert.Equal(0, ((CellPointer)CobolPtr.UpByAmount(p, 25, 1, down: false)).Offset);
    }

    /// <summary>kb/Work PB2697 — a trailing-P amount (negative scale) displaces by its VALUE, the stored digits
    /// × 10^|scale| (§13.18.40.4 GR14), and one whose value the widest carrier cannot hold IS an integer, so it is
    /// GR20's EC-RANGE-PTR (no representable address results), never GR19's EC-SIZE-ADDRESS and never a
    /// displacement by 0 — the exact lane now answers what the native-float lane already did.</summary>
    [Fact]
    public void UpByAmount_TrailingPAmount_DisplacesByItsValue_AndPastTheCarrierIsGr20()
    {
        var cell = new StorageCell { Ref = "ABCDEFGHIJKLMNOPQRSTUVWXYZ" };
        var p = ManagedPointer.At(cell, 0);
        Assert.Equal(20, ((CellPointer)CobolPtr.UpByAmount(p, 2, -1, down: false)).Offset);   // PIC 9P holding 20
        RunUnit.Run(_ =>
        {
            ExceptionState.RangePtrChecking = true;
            Assert.Equal("EC-RANGE-PTR",
                Assert.Throws<CobolFatalException>(() => CobolPtr.UpByAmount(p, 1, -39, down: false)).EcName);
        });
        Assert.Same(p, CobolPtr.UpByAmount(p, 1, -39, down: false));                // checking off: unchanged
    }

    [Fact]
    public void UpBy_Null_RaisesWhenChecked_AndIsUnchangedWhenNot()
    {
        // §14.9.39.4 GR18 — a NULL identifier-9 sets EC-DATA-PTR-NULL.
        ExceptionState.DataPtrNullChecking = true;
        try
        {
            Assert.Equal("EC-DATA-PTR-NULL",
                Assert.Throws<CobolFatalException>(() => CobolPtr.UpBy(ManagedPointer.Null, 1)).EcName);
        }
        finally { ExceptionState.DataPtrNullChecking = false; }

        // Checking OFF is LENIENT here and loud at Deref — the owner's rule, because GR19 names the unchanged
        // outcome for a SET while §13.18.5.4 GR3/GR4 name none for a dereference (see Deref_Null_… below, which
        // still asserts the unconditional throw). A SET that cannot be performed leaves its operand NULL.
        Assert.True(CobolPtr.UpBy(ManagedPointer.Null, 1).IsNull);
    }

    [Fact]
    public void Deref_Null_And_OutOfBounds_AreLoud()
    {
        Assert.Equal("EC-DATA-PTR-NULL",
            Assert.Throws<CobolFatalException>(() => CobolPtr.Deref(ManagedPointer.Null, 1)).EcName);   // §13.18.5 GR3
        var cell = new StorageCell { Ref = "ABCDE" };
        Assert.Equal("EC-BOUND-PTR",
            Assert.Throws<CobolFatalException>(() => CobolPtr.Deref(ManagedPointer.At(cell, 4), 5)).EcName);   // GR4 — window past the end
        Assert.Same(cell, CobolPtr.Deref(ManagedPointer.At(cell, 4), 1));   // the last byte is addressable
    }

    [Fact]
    public void Allocate_SizeRules_And_InitializedFill()
    {
        Assert.True(CobolPtr.Allocate(0, ' ', out _).IsNull);        // §14.9.3 GR2 — ≤0 ⇒ NULL, no exception condition
        Assert.True(CobolPtr.Allocate(-5, ' ', out _).IsNull);
        var p = (CellPointer)CobolPtr.Allocate(4, ' ', out _);
        Assert.Equal("    ", p.Cell.Ref);                // GR8 undefined — this implementation space-fills
        Assert.True(p.Cell.Allocated);
        var z = (CellPointer)CobolPtr.Allocate(3, '\0', out _);
        Assert.Equal("\0\0\0", z.Cell.Ref);              // GR6 INITIALIZED — binary zeros
    }

    // kb/Work PB151 — §14.9.3.4 GR5: a not-available request answers NULL + the notAvail flag (the emitter's
    // checking-gated EC-STORAGE-NOT-AVAIL), never an unhandled OverflowException; and the size arrives as the
    // FULL Int128, so a 20-digit request cannot WRAP into a small valid allocation (the PB22 cast family).
    [Fact]
    public void Allocate_NotAvailable_IsNullPlusFlag_NeverAThrow()
    {
        Assert.True(CobolPtr.Allocate((Int128)3_000_000_000L, ' ', out bool na1).IsNull);
        Assert.True(na1);                                          // past int.MaxValue — GR5, was OverflowException
        Assert.True(CobolPtr.Allocate(Int128.Parse("100000000000000000000"), ' ', out bool na2).IsNull);
        Assert.True(na2);                                          // 20 digits — was a WRAPPED small live cell
    }

    // kb/Work PB151 — GR1: a native-float request rounds UP on the DOUBLE (the old (long)(double) truncated
    // 2.5 → 2, a silently undersized cell); NaN is not-available; ≤0 after the ceiling is GR2's NULL, no flag.
    [Fact]
    public void AllocateReal_CeilsAndClassifies()
    {
        var p = (CellPointer)CobolPtr.AllocateReal(2.5, ' ', out bool na);
        Assert.False(na);
        Assert.Equal(3, p.Cell.Ref.Length);                        // GR1 — rounded up to the next whole number
        Assert.True(CobolPtr.AllocateReal(-0.5, ' ', out bool naNeg).IsNull);
        Assert.False(naNeg);                                       // ceil(-0.5) = 0 → GR2's leg, no exception
        Assert.True(CobolPtr.AllocateReal(double.NaN, ' ', out bool naNan).IsNull);
        Assert.True(naNan);
    }

    // kb/Work PB151 — §14.9.39.4 GR19 on a native-float amount: non-integral leaves the pointer UNCHANGED
    // (the old (long)(double) truncation bypassed the integrality raise); an integral double displaces.
    [Fact]
    public void UpByAmountReal_IntegralityOnTheDouble()
    {
        var p = CobolPtr.Allocate(5, ' ', out _);
        Assert.Same(p, CobolPtr.UpByAmountReal(p, 1.5, down: false));   // GR19 — unsuccessful, id-9 unchanged
        Assert.Equal(2, ((CellPointer)CobolPtr.UpByAmountReal(p, 2.0, down: false)).Offset);
        Assert.Same(p, CobolPtr.UpByAmountReal(p, double.NaN, down: false));        // not an integer either
        Assert.Same(p, CobolPtr.UpByAmountReal(p, double.PositiveInfinity, down: false));
    }

    // ⛔ kb/Work PB465 — THE AXIS THE TWO TESTS ABOVE HOLD FIXED. Both drive the integrality axis
    // (integral vs fractional) with a SMALL amount; neither drives an INTEGRAL amount whose MAGNITUDE no address
    // can hold, which is where §14.9.39.4 states a DIFFERENT rule with a DIFFERENT condition. GR19 is about the
    // amount's VALUE ("does not evaluate to an integer" — EC-SIZE-ADDRESS); GR20 is about the RESULTING ADDRESS
    // ("outside the range of values allowed by the implementor for a data-pointer data item" — EC-RANGE-PTR).
    // Measured before the fix: an integral 1.0E19 set EC-SIZE-ADDRESS and terminated the run unit.
    [Fact]
    public void UpByAmountReal_AnIntegralAmountPastTheCarrier_IsGr20_NotGr19()
    {
        var p = CobolPtr.Allocate(5, ' ', out _);
        // 1.0E19 IS an integer, so GR19's antecedent is FALSE: EC-SIZE-ADDRESS must not be set even armed.
        ExceptionState.SizeAddressChecking = true;
        try { Assert.Same(p, CobolPtr.UpByAmountReal(p, 1.0E19, down: false)); }
        finally { ExceptionState.SizeAddressChecking = false; }

        // The condition the standard supplies for it is GR20's, and its two other consequents are the operand
        // unchanged (checking off) and the fatal raise (checking on).
        ExceptionState.RangePtrChecking = true;
        try
        {
            Assert.Equal("EC-RANGE-PTR",
                Assert.Throws<CobolFatalException>(() => CobolPtr.UpByAmountReal(p, 1.0E19, down: false)).EcName);
            // 1.0E300 is integral too and past the widest integer carrier — the same rule, not a conversion.
            Assert.Equal("EC-RANGE-PTR",
                Assert.Throws<CobolFatalException>(() => CobolPtr.UpByAmountReal(p, 1.0E300, down: false)).EcName);
        }
        finally { ExceptionState.RangePtrChecking = false; }
    }

    // ⛔ kb/Work PB465 — the SCALED arm of the same two-rule split, and the WRAP the emitter's former
    // `long __ptrBy = (long)(…)` produced. 2^64 + 2 displaced the pointer by 2; 1844674407370955162.0 (an
    // INTEGER at scale 1) raised GR19's condition because the wrap left 4, which 10 does not divide.
    [Fact]
    public void UpByAmount_KeepsTheFullMagnitude_AndAnswersGr20()
    {
        var cell = new StorageCell { Ref = "ABCDEFGHIJ" };
        var p = ManagedPointer.At(cell, 0);
        Int128 twoPow64Plus2 = Int128.Parse("18446744073709551618");
        ExceptionState.SizeAddressChecking = true;
        try
        {
            // An integer at any magnitude: GR19 does not apply, so nothing is raised through it …
            Assert.Same(p, CobolPtr.UpByAmount(p, twoPow64Plus2, 0, down: false));
            // … and 1844674407370955162.0 at scale 1 is an integer VALUE whose SCALED representation does not
            // fit 64 bits while the value itself does, so GR19 does not apply and GR20 moves by the whole
            // 1844674407370955162. The emitter's former `(long)` narrowing wrapped the SCALED value to 4,
            // which 10 does not divide, and raised GR19's condition on an integer (PB465's measured repro).
            Assert.Equal(1844674407370955162L,
                ((CellPointer)CobolPtr.UpByAmount(p, Int128.Parse("18446744073709551620"), 1, down: false)).Offset);
        }
        finally { ExceptionState.SizeAddressChecking = false; }
        // … and above all it did NOT move by 2 — GR20's outcome is the operand unchanged, never a wrap.
        Assert.Equal(0, ((CellPointer)CobolPtr.UpByAmount(p, twoPow64Plus2, 0, down: false)).Offset);

        ExceptionState.RangePtrChecking = true;
        try
        {
            Assert.Equal("EC-RANGE-PTR", Assert.Throws<CobolFatalException>(
                () => CobolPtr.UpByAmount(p, twoPow64Plus2, 0, down: false)).EcName);
            // The RESULT is what GR20 tests, so an amount that fits is still out of range from a high address:
            // long.MaxValue - 1 UP BY 2 leaves the range, and the operand is unchanged.
            var high = ManagedPointer.At(cell, CobolPtr.MaxAddress - 1);
            Assert.Equal("EC-RANGE-PTR",
                Assert.Throws<CobolFatalException>(() => CobolPtr.UpByAmount(high, 2, 0, down: false)).EcName);
            Assert.Equal("EC-RANGE-PTR", Assert.Throws<CobolFatalException>(
                () => CobolPtr.UpByAmount(ManagedPointer.At(cell, CobolPtr.MinAddress), 1, 0, down: true)).EcName);
        }
        finally { ExceptionState.RangePtrChecking = false; }
        // Checking OFF: GR20's "the value of the data item referenced by identifier-9 is unchanged".
        Assert.Equal(CobolPtr.MaxAddress - 1,
            ((CellPointer)CobolPtr.UpByAmount(ManagedPointer.At(cell, CobolPtr.MaxAddress - 1), 2, 0, down: false)).Offset);
        // The LAST in-range address is reachable — the guard is a range test, not an off-by-one clamp.
        Assert.Equal(CobolPtr.MaxAddress,
            ((CellPointer)CobolPtr.UpByAmount(ManagedPointer.At(cell, CobolPtr.MaxAddress - 1), 1, 0, down: false)).Offset);
    }

    [Fact]
    public void Free_ThreeWay_And_DanglingAliasIsLoud()
    {
        // (a) start-of-allocation: released, operand nulls; a dangling alias trips Deref loud (GR1a).
        var p = CobolPtr.Allocate(5, ' ', out _);
        var alias = CobolPtr.UpBy(p, 1);
        Assert.True(CobolPtr.Free(p, out bool na1).IsNull);
        Assert.False(na1);
        Assert.Equal("EC-BOUND-PTR", Assert.Throws<CobolFatalException>(() => CobolPtr.Deref(alias, 1)).EcName);
        // (b) NULL: no-op.
        Assert.True(CobolPtr.Free(ManagedPointer.Null, out bool na2).IsNull);
        Assert.False(na2);
        // (c) not the start of an allocation (a mid-block window / an ADDRESS OF cell): unchanged + notAlloc.
        var q = CobolPtr.UpBy(CobolPtr.Allocate(5, ' ', out _), 2);
        Assert.Same(q, CobolPtr.Free(q, out bool na3));
        Assert.True(na3);                                // GR1c — EC-STORAGE-NOT-ALLOC (nonfatal)
        var addr = ManagedPointer.At(new StorageCell { Ref = "HELLO" }, 0);   // not Allocated
        Assert.Same(addr, CobolPtr.Free(addr, out bool na4));
        Assert.True(na4);
        // Double FREE: the second is GR1c (already freed — not an allocation start anymore).
        var r = CobolPtr.Allocate(2, ' ', out _);
        CobolPtr.Free(r, out _);
        Assert.Same(r, CobolPtr.Free(r, out bool na5));
        Assert.True(na5);
    }
}
