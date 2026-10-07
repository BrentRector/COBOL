// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A FIXED-LENGTH GROUP OPPOSITE A VARIABLE-LENGTH ONE IS A PAIR QUESTION, AND THE RETURNING ITEM IS STORAGE
/// THE ACTIVATING ELEMENT DESCRIBES (kb/Work PB965 + PB962).
/// <para>ISO §8.5.1.12.2: "Two tables correspond if at least one of them is a dynamic-capacity table and they
/// occupy the same relative byte positions within their groups." The spans a fixed group lifts out are therefore
/// a fact about the PAIR (<see cref="CobolVarGroup.CorrespondingSpans"/>), never "every table of the fixed group" —
/// which moved the wrong table the moment a fixed table stood opposite plain bytes. Across a CALL each side is
/// compiled apart, so each side's atoms travel (<see cref="CobolArg.Atoms"/>) and the pair is decided where
/// both are in hand: the formal's adapter, or the RETURNING delivery, which now receives the receiver as a
/// <see cref="CobolArg"/> (carrier plus description) instead of a bare carrier.</para>
/// <para>§14.6.5: the result "is the content of the data item referenced by that RETURNING phrase" — a content
/// transfer under the one description a conforming pair shares (§14.8.3.3), never a re-parse of the text as a
/// number, which aborted on spaces.</para>
/// </summary>
public sealed class BoundaryGroupCorrespondenceTests
{
    private const int F = CobolVarGroup.LayoutFixed, T = CobolVarGroup.LayoutTable,
        D = CobolVarGroup.LayoutDynamicTable, L = CobolVarGroup.LayoutDynamicLength;

    // 05 S1 X(2) · 05 ST X OCCURS 3 · 05 S3 X(2)
    private static readonly int[] FixedSG = [F, 2, 0, T, 3, 1, F, 2, 0];
    // 05 L0 X(2) · 05 L1 X OCCURS DYNAMIC · 05 L3 X(2)
    private static readonly int[] VarLG = [F, 2, 0, D, 1, 1, F, 2, 0];

    // The same two groups as the ATOMS a CALL carries (CobolArg.Atoms; kb/Work PB2280) — their layouts above are
    // GroupCompatibility.Layout of these.
    private static readonly GroupAtom[] FixedSGAtoms =
        [Fx(2), new(GroupAtomKind.Table, 3, 3, 1, 1), Fx(2)];
    private static readonly GroupAtom[] VarLGAtoms =
        [Fx(2), new(GroupAtomKind.DynamicTable, 1, 1, 1, 1), Fx(2)];

    private static GroupAtom Fx(int n) => new(GroupAtomKind.Fixed, n, n);

    [Fact]
    public void TheAtomsFixturesAreTheLayoutsFixtures()
    {
        Assert.Equal(FixedSG, GroupCompatibility.Layout(FixedSGAtoms));
        Assert.Equal(VarLG, GroupCompatibility.Layout(VarLGAtoms));
    }

    [Fact]
    public void AFixedTableAtTheDynamicTablesPosition_Corresponds_AtItsFixedWidth()
        => Assert.Equal([2, 3], CobolVarGroup.CorrespondingSpans(FixedSG, VarLG)!);

    [Fact]
    public void AFixedTableOppositePlainBytes_IsPlainMaterial_NotAComponent()
    {
        // 05 LA X OCCURS 2 · 05 LB X OCCURS 3 · 05 LC X(2): LA stands opposite L0's plain bytes, so ONLY LB
        // corresponds. "Every table of the fixed group" lifted LA too and moved it into the dynamic table.
        int[] lead = [T, 2, 1, T, 3, 1, F, 2, 0];
        Assert.Equal([2, 3], CobolVarGroup.CorrespondingSpans(lead, VarLG)!);
    }

    [Fact]
    public void ThePairFails_WhereSection8_5_1_12Fails()
    {
        // No table where the dynamic table stands (§8.5.1.12.1 rule 1).
        Assert.Null(CobolVarGroup.CorrespondingSpans([F, 3, 0, T, 3, 1, F, 2, 0], VarLG));
        // Element byte lengths differ (§8.5.1.12.3 — "the byte length of their elements is equal").
        Assert.Null(CobolVarGroup.CorrespondingSpans([F, 2, 0, T, 6, 2, F, 2, 0], VarLG));
        // A dynamic-length item has no counterpart in a fixed group (§8.5.1.12.1 rule 3).
        Assert.Null(CobolVarGroup.CorrespondingSpans(FixedSG, [F, 2, 0, L, 0, 0]));
        // A group with no table at all is one fixed run: long enough to reach the dynamic table's position, it
        // has no table there; and a stated-nothing layout answers nothing.
        Assert.Null(CobolVarGroup.CorrespondingSpans(CobolVarGroup.FixedRun(7), VarLG));
        Assert.Null(CobolVarGroup.CorrespondingSpans(null, VarLG));
    }

    [Fact]
    public void ADynamicTableBeyondTheShorterGroupsEnd_IsNotCarried()
        // §8.5.1.12.2's last sentence: "treated as if it corresponds to a space-filled fixed-length table" —
        // no component, so the receiver's §14.6.9.4 space fill applies at an unaffected capacity.
        => Assert.Empty(CobolVarGroup.CorrespondingSpans([F, 2, 0], [F, 2, 0, D, 1, 1])!);

    [Fact]
    public void AFixedGroupArgument_ReachesAVariableLengthFormal_AndItsStoresComeBack()
    {
        var caller = ManagedPointer<string>.Cell("CDTTTEF");
        var args = new[] { new CobolArg(CobolPassMode.Reference, caller, null, FixedSGAtoms) };
        var formal = CobolArgAdapt.VarGroup(args, 0, VarLGAtoms);
        var v = formal.Value!;
        Assert.Equal("CDEF", v.Fixed);
        Assert.Equal(["TTT"], v.Dynamic);
        // §14.2.3 GR8: the formal occupies the argument's storage — a store comes back.
        formal.Value = new CobolVarGroup("qqEF", ["TuT"]);
        Assert.Equal("qqTuTEF", caller.Value);
        // ⚠ The callee cannot grow the fixed table: superfluous occurrences are not moved (§14.6.9.2).
        formal.Value = new CobolVarGroup("qqEF", ["abcde"]);
        Assert.Equal("qqabcEF", caller.Value);
    }

    [Fact]
    public void ANonCorrespondingFixedArgument_FailsTheActivation_Loud()
    {
        var args = new[] { new CobolArg(CobolPassMode.Reference, ManagedPointer<string>.Cell("CDETTTEF"), null,
            [Fx(3), new(GroupAtomKind.Table, 3, 3, 1, 1), Fx(2)]) };
        var ex = Assert.Throws<CobolCallException>(() => CobolArgAdapt.VarGroup(args, 0, VarLGAtoms));
        Assert.Contains("EC-PROGRAM-ARG-MISMATCH", ex.Message);
    }

    /// <summary>kb/Work PB2280 — two VARIABLE-length groups of different shapes (§14.8.2.2 2): "compatible, as described
    /// in 8.5.1.12", which constrains only where their variable-length items lie). The argument 05 H X(2) · 05 D X
    /// DYNAMIC LENGTH · 05 T X(5) meets the formal 05 H X(2) · 05 D X DYNAMIC LENGTH · 05 T X(2): the formal sees its own
    /// shape, and BY REFERENCE (§14.2.3 GR8) its store reaches only the argument storage it overlays, so the argument's
    /// tail past the formal's last character survives. Aliasing the carrier whole cut it to the formal's length.</summary>
    [Fact]
    public void AVariableLengthArgumentOfAnotherShape_IsSeenInTheFormalsShape_AndItsTailSurvives()
    {
        GroupAtom[] arg = [Fx(2), new(GroupAtomKind.DynamicLength, 0, 0), Fx(5)];
        GroupAtom[] formalShape = [Fx(2), new(GroupAtomKind.DynamicLength, 0, 0), Fx(2)];
        var caller = ManagedPointer<CobolVarGroup>.Cell(new CobolVarGroup("HHTTTTT", ["dyn"]));
        var formal = CobolArgAdapt.VarGroup([new CobolArg(CobolPassMode.Reference, caller, null, arg)], 0, formalShape);
        Assert.Equal("HHTT", formal.Value!.Fixed);
        Assert.Equal(["dyn"], formal.Value.Dynamic);
        formal.Value = new CobolVarGroup("hhTT", ["Q"]);
        Assert.Equal("hhTTTTT", caller.Value!.Fixed);
        Assert.Equal(["Q"], caller.Value.Dynamic);
        // BY CONTENT (§14.2.3 GR9): the same view, detached.
        var copy = CobolArgAdapt.VarGroupValue([new CobolArg(CobolPassMode.Content, caller, null, arg)], 0, formalShape);
        Assert.Equal("hhTT", copy.Value!.Fixed);
        copy.Value = new CobolVarGroup("zzzz", ["z"]);
        Assert.Equal("hhTTTTT", caller.Value!.Fixed);
    }

    [Fact]
    public void AnIncompatibleVariableLengthArgument_FailsTheActivation_Loud()
    {
        // A dynamic-length item opposite plain material (§8.5.1.12.1 rule 3) — EC-PROGRAM-ARG-MISMATCH, never an
        // internal error from the reshape.
        var args = new[] { new CobolArg(CobolPassMode.Reference,
            ManagedPointer<CobolVarGroup>.Cell(new CobolVarGroup("HHH", ["d"])), null,
            [Fx(3), new(GroupAtomKind.DynamicLength, 0, 0)]) };
        var ex = Assert.Throws<CobolCallException>(() =>
            CobolArgAdapt.VarGroup(args, 0, [Fx(2), new(GroupAtomKind.DynamicLength, 0, 0), Fx(1)]));
        Assert.Contains("EC-PROGRAM-ARG-MISMATCH", ex.Message);
    }

    [Fact]
    public void AFixedGroupResult_LandsInAVariableLengthReceiver_AndTheReverse()
    {
        var vg = ManagedPointer<CobolVarGroup>.Cell(CobolVarGroup.Empty);
        CobolArgAdapt.StoreReturnGroup(new CobolArg(CobolPassMode.Reference, vg, null, VarLGAtoms), "mnopqrs", FixedSGAtoms);
        Assert.Equal("mnrs", vg.Value!.Fixed);
        Assert.Equal(["opq"], vg.Value.Dynamic);

        var fg = ManagedPointer<string>.Cell(new string(' ', 7));
        CobolArgAdapt.StoreReturn(new CobolArg(CobolPassMode.Reference, fg, null, FixedSGAtoms),
            new CobolVarGroup("tuyz", ["vw"]), VarLGAtoms);
        Assert.Equal("tuvw yz", fg.Value);   // §14.6.9.2: the missing occurrence is space filled
    }

    // ── The reverse pair: a VARIABLE-length argument into a FIXED-length group formal (the PB965 finisher) ────

    [Fact]
    public void AVariableLengthArgument_ReachesAFixedGroupFormal_ThroughThePairsCorrespondence()
    {
        // VG = gh · VT capacity 2 "kl" · ij, opposite LF = X(2) · X OCCURS 3 · X(2).
        var vg = ManagedPointer<CobolVarGroup>.Cell(new CobolVarGroup("ghij", ["kl"]));
        var args = new[] { new CobolArg(CobolPassMode.Reference, vg, null, VarLGAtoms) };
        var formal = CobolArgAdapt.Text(args, 0, 7, FixedSGAtoms);
        // §8.5.1.12.3 sentence 3 + §14.6.9.2: the capacity-2 table fills a 3-occurrence view, the third spaces.
        Assert.Equal("ghkl ij", formal.Value);
        // §14.2.3 GR8 — the formal overlays the argument's storage: the fixed material and the occurrences the
        // argument HAS are stored; ⚠ the capacity is the argument's own, so the third occurrence has nowhere to go.
        formal.Value = "qqmnoij";
        Assert.Equal("qqij", vg.Value!.Fixed);
        Assert.Equal(["mn"], vg.Value.Dynamic);
    }

    [Fact]
    public void AFixedFormalOverAWiderTable_LeavesTheOccurrencesPastItsCountUntouched()
    {
        var vg = ManagedPointer<CobolVarGroup>.Cell(new CobolVarGroup("ghij", ["klmn"]));
        var formal = CobolArgAdapt.Text([new CobolArg(CobolPassMode.Reference, vg, null, VarLGAtoms)], 0, 7, FixedSGAtoms);
        Assert.Equal("ghklmij", formal.Value);   // §14.6.9.2: superfluous elements are not moved
        formal.Value = "ghKLMrs";
        Assert.Equal(["KLMn"], vg.Value!.Dynamic);
        Assert.Equal("ghrs", vg.Value.Fixed);
    }

    [Fact]
    public void ATableLessPrefixFormal_SeesOnlyItsOwnCharacters_AndTheRestSurvives()
    {
        // §14.8.2.2 rule 1's prefix: a 2-character group formal; VT lies past its last character (§8.5.1.12.2's
        // last sentence), so it is no component of the pair and must come back unchanged.
        var vg = ManagedPointer<CobolVarGroup>.Cell(new CobolVarGroup("ghij", ["kl"]));
        var formal = CobolArgAdapt.Text([new CobolArg(CobolPassMode.Reference, vg, null, VarLGAtoms)], 0, 2, []);
        Assert.Equal("gh", formal.Value);
        formal.Value = "PP";
        Assert.Equal("PPij", vg.Value!.Fixed);
        Assert.Equal(["kl"], vg.Value.Dynamic);
    }

    [Fact]
    public void ByContent_TheSameView_IsDetached()
    {
        var vg = ManagedPointer<CobolVarGroup>.Cell(new CobolVarGroup("ghij", ["kl"]));
        var cell = CobolArgAdapt.TextValue([new CobolArg(CobolPassMode.Content, vg, null, VarLGAtoms)], 0, 7, null, 0, FixedSGAtoms);
        Assert.Equal("ghkl ij", cell.Value);
        cell.Value = "zzzzzzz";
        Assert.Equal("ghij", vg.Value!.Fixed);   // §14.2.3 GR9 — the callee's stores never reach the argument
    }

    [Fact]
    public void AVariableLengthArgument_IntoANonGroupFormal_StaysLoud()
        // §8.5.1.12.1: a variable-length group is compatible only with a GROUP — an elementary formal states no
        // layout, and the carrier it cannot read fails the activation rather than being reinterpreted.
        => Assert.Throws<CobolCallException>(() => CobolArgAdapt.Text(
            [new CobolArg(CobolPassMode.Reference, ManagedPointer<CobolVarGroup>.Cell(CobolVarGroup.Empty), null, VarLGAtoms)], 0, 7));

    private static readonly NumProfile S3V1 = new()
    {
        Digits = 4, FractionDigits = 1, Signed = true, SignKind = NumericSign.TrailingOverpunch,
        Truncation = NumericTruncation.DigitCount, ByteForm = NumericByteForm.Zoned,
    };

    private static readonly NumProfile U3 = new()
    {
        Digits = 3, FractionDigits = 0, Signed = false, Truncation = NumericTruncation.DigitCount,
        ByteForm = NumericByteForm.Zoned,
    };

    [Fact]
    public void ANumericResult_IsItsContent_NeverAReParse()
    {
        // An image-carried receiver takes the content as it stands — spaces included (§14.6.5).
        var image = ManagedPointer<string>.Cell("999");
        CobolArgAdapt.StoreReturn(new CobolArg(CobolPassMode.Reference, image, U3), "   ", U3);
        Assert.Equal("   ", image.Value);
        // A native cell holds a VALUE: the same content decodes under the shared description and does not
        // abort the run unit (it used to raise EC-PROGRAM-ARG-MISMATCH). ⚠ The value a native cell reads from
        // non-numeric content is the documented character-view residue, not the content.
        var cell = ManagedPointer<long>.Cell(999);
        CobolArgAdapt.StoreReturn(new CobolArg(CobolPassMode.Reference, cell, U3), "   ", U3);
        Assert.Equal(0, cell.Value);
        // A native VALUE into an image-carried receiver is its representation under the description — the sign
        // over-punched — never the value's C# text ("-125" lost the sign and the digit count).
        var signed = ManagedPointer<string>.Cell("9999");
        CobolArgAdapt.StoreReturn(new CobolArg(CobolPassMode.Reference, signed, S3V1), (Int128)(-125), S3V1);
        Assert.Equal("012N", signed.Value);
    }

    [Fact]
    public void AFloatResult_HasADelivery()
    {
        var cell = ManagedPointer<double>.Cell(9);
        CobolArgAdapt.StoreReturn(new CobolArg(CobolPassMode.Reference, cell, null), 1.5);
        Assert.Equal(1.5, cell.Value);
    }
}
