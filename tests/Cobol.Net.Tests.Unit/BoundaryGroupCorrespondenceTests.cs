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
/// a fact about the PAIR (<see cref="GroupCompatibility.Walk"/> over the two shapes, converted by
/// <see cref="CobolVarGroup.Reshape"/>), never "every table of the fixed group" —
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
    // 05 S1 X(2) · 05 ST X OCCURS 3 · 05 S3 X(2)
    private static readonly GroupAtom[] FixedSGAtoms =
        [Fx(2), new(GroupAtomKind.Table, 3, 3, 1, 1), Fx(2)];
    // 05 L0 X(2) · 05 L1 X OCCURS DYNAMIC · 05 L3 X(2)
    private static readonly GroupAtom[] VarLGAtoms =
        [Fx(2), new(GroupAtomKind.DynamicTable, 1, 1, 1, 1), Fx(2)];

    private static GroupAtom Fx(int n) => new(GroupAtomKind.Fixed, n, n);

    [Fact]
    public void AFixedTableAtTheDynamicTablesPosition_Corresponds_AtItsFixedWidth()
    {
        Assert.Null(GroupCompatibility.Walk(FixedSGAtoms, VarLGAtoms));
        var v = CobolVarGroup.FromImage("CDTTTEF", FixedSGAtoms, VarLGAtoms);
        Assert.Equal("CDEF", v.Fixed);
        Assert.Equal(["TTT"], v.Dynamic);
    }

    [Fact]
    public void AFixedTableOppositePlainBytes_IsPlainMaterial_NotAComponent()
    {
        // 05 LA X OCCURS 2 · 05 LB X OCCURS 3 · 05 LC X(2): LA stands opposite L0's plain bytes, so ONLY LB
        // corresponds. "Every table of the fixed group" lifted LA too and moved it into the dynamic table.
        GroupAtom[] lead = [new(GroupAtomKind.Table, 2, 2, 1, 1), new(GroupAtomKind.Table, 3, 3, 1, 1), Fx(2)];
        Assert.Null(GroupCompatibility.Walk(lead, VarLGAtoms));
        var v = CobolVarGroup.FromImage("AABBBCD", lead, VarLGAtoms);
        Assert.Equal("AACD", v.Fixed);
        Assert.Equal(["BBB"], v.Dynamic);
    }

    [Fact]
    public void ThePairFails_WhereSection8_5_1_12Fails()
    {
        // No table where the dynamic table stands (§8.5.1.12.1 rule 1).
        Assert.NotNull(GroupCompatibility.Walk([Fx(3), new(GroupAtomKind.Table, 3, 3, 1, 1), Fx(2)], VarLGAtoms));
        // Element byte lengths differ (§8.5.1.12.3 — "the byte length of their elements is equal").
        Assert.NotNull(GroupCompatibility.Walk([Fx(2), new(GroupAtomKind.Table, 6, 6, 2, 2), Fx(2)], VarLGAtoms));
        // A dynamic-length item has no counterpart in a fixed group (§8.5.1.12.1 rule 3).
        Assert.NotNull(GroupCompatibility.Walk(FixedSGAtoms, [Fx(2), new(GroupAtomKind.DynamicLength, 0, 0)]));
        // A group with no table at all is one fixed run: long enough to reach the dynamic table's position, it
        // has no table there.
        Assert.NotNull(GroupCompatibility.Walk(GroupCompatibility.FixedRun(7), VarLGAtoms));
    }

    [Fact]
    public void ADynamicTableBeyondTheShorterGroupsEnd_IsNotCarried()
    {
        // §8.5.1.12.2's last sentence: "treated as if it corresponds to a space-filled fixed-length table" —
        // no component, so the receiver's §14.6.9.4 space fill applies at an unaffected capacity.
        GroupAtom[] shorter = [Fx(2)];
        Assert.Null(GroupCompatibility.Walk(shorter, [Fx(2), new(GroupAtomKind.DynamicTable, 1, 1, 1, 1)]));
        var v = CobolVarGroup.FromImage("AB", shorter, [Fx(2), new(GroupAtomKind.DynamicTable, 1, 1, 1, 1)]);
        Assert.Equal("AB", v.Fixed);
        Assert.Empty(v.Dynamic);
        Assert.False(v.HasDyn(0));
    }

    // ── kb/Work PB2690: a FIXED table opposite a dynamic-capacity table of VARIABLE-LENGTH elements ────────────
    //
    // 05 H X · 05 TF OCCURS 2 { 10 DF X · 10 IF X OCCURS 2 }   against   05 H X · 05 TD OCCURS DYNAMIC { 10 DD X ·
    // 10 ID X OCCURS DYNAMIC }. The elements correspond (§8.5.1.12.3 sentence 2: ID is a dynamic-capacity table where IF
    // is a fixed one, and sentence 3 makes IF "a dynamic-capacity table whose capacity is" 2), each three bytes long.
    private static readonly GroupAtom[] ElemFixedTable = [Fx(1), new(GroupAtomKind.Table, 2, 2, 1, 1)];
    private static readonly GroupAtom[] ElemDynTable = [Fx(1), new(GroupAtomKind.DynamicTable, 1, 1, 1, 1)];
    private static readonly GroupAtom[] FixedOfElements =
        [Fx(1), new(GroupAtomKind.Table, 6, 6, 3, 3, ElemFixedTable)];
    private static readonly GroupAtom[] VarOfElements =
        [Fx(1), new(GroupAtomKind.DynamicTable, 3, 3, 3, 3, ElemDynTable)];

    [Fact]
    public void AFixedTableOfFixedElements_ReachesADynamicTableOfVariableLengthElements_ElementByElement()
    {
        Assert.Null(GroupCompatibility.Walk(FixedOfElements, VarOfElements));
        // "h" · element 1 "a" + "bc" · element 2 "d" + "ef": each occurrence becomes a carrier of its own, the fixed
        // IF table crossing at its two occurrences (§8.5.1.12.3 sentence 3), reshaped by §14.6.9.2's element move.
        var v = CobolVarGroup.FromImage("habcdef", FixedOfElements, VarOfElements);
        Assert.Equal("h", v.Fixed);
        var elements = v.ElementCarriersAt(0);
        Assert.Equal(2, elements.Length);
        Assert.Equal("a", elements[0].Fixed);
        Assert.Equal(["bc"], elements[0].Dynamic);
        Assert.Equal("d", elements[1].Fixed);
        Assert.Equal(["ef"], elements[1].Dynamic);
        // And back, fitted to the fixed extent (§14.6.9.2): the third element is not moved, and the second element's one
        // IT occurrence leaves the fixed table's second occurrence space filled.
        var grown = new CobolVarGroup("h", [""], [[new("a", ["bc"]), new("d", ["e"]), new("g", ["hi"])]]);
        Assert.Equal("habcde ", CobolVarGroup.ToImage(grown, VarOfElements, FixedOfElements, 7));
    }

    [Fact]
    public void TheCachedPairPlan_ConvertsExactlyAsTheFirstWalkDid_AndAnEqualButDistinctArrayIsAnotherKey()
    {
        // kb/Work PB2690: the compiler states each distinct array once, and the runtime memoizes the correspondence of two
        // array REFERENCES. The second conversion of the same pair takes the cached plan; a copy of the same atoms is another
        // key and is worked out afresh — both must give the one answer the standard gives.
        string first = CobolVarGroup.ToImage(CobolVarGroup.FromImage("habcdef", FixedOfElements, VarOfElements),
            VarOfElements, FixedOfElements, 7);
        string again = CobolVarGroup.ToImage(CobolVarGroup.FromImage("habcdef", FixedOfElements, VarOfElements),
            VarOfElements, FixedOfElements, 7);
        GroupAtom[] copyOfFixed = [.. FixedOfElements], copyOfVar = [.. VarOfElements];
        string copies = CobolVarGroup.ToImage(CobolVarGroup.FromImage("habcdef", copyOfFixed, copyOfVar), copyOfVar, copyOfFixed, 7);
        Assert.Equal("habcdef", first);
        Assert.Equal(first, again);
        Assert.Equal(first, copies);
        Assert.True(GroupCompatibility.SameShape(FixedOfElements, [.. FixedOfElements]));
    }

    [Fact]
    public void TheOneRunShapeOfAWidth_IsOneArray_SoTheBoundarysCachesHitOnEveryCall()
    {
        // kb/Work PB2690 (performance): the pair plans and geometries are keyed by the atom array's REFERENCE, and the
        // table-less fixed group's shape used to be built afresh on every CALL — a miss and a new entry each time.
        Assert.Same(GroupCompatibility.FixedRun(7), GroupCompatibility.FixedRun(7));
        Assert.NotSame(GroupCompatibility.FixedRun(7), GroupCompatibility.FixedRun(8));
        Assert.Equal((GroupAtomKind.Fixed, 7, 7), (GroupCompatibility.FixedRun(7)[0].Kind, GroupCompatibility.FixedRun(7)[0].Bytes,
            GroupCompatibility.FixedRun(7)[0].Chars));
    }

    [Fact]
    public void AFixedGroupArgument_ReachesADynamicTableOfVariableLengthElements_AndItsStoresComeBack()
    {
        var caller = ManagedPointer<string>.Cell("habcdef");
        var args = new[] { new CobolArg(CobolPassMode.Reference, caller, null, FixedOfElements) };
        var formal = CobolArgAdapt.VarGroup(args, 0, VarOfElements);
        Assert.Equal(2, formal.Value!.ElementCarriersAt(0).Length);
        // §14.2.3 GR8: the formal occupies the argument's storage — a store of element 2's IF and DF comes back, and the
        // formal's third element has no storage in the fixed group to land in.
        formal.Value = new CobolVarGroup("H", [""], [[new("a", ["bc"]), new("D", ["EF"]), new("x", ["yz"])]]);
        Assert.Equal("HabcDEF", caller.Value);
        // BY CONTENT (§14.2.3 GR9): the same view, detached.
        var copy = CobolArgAdapt.VarGroupValue([new CobolArg(CobolPassMode.Content, caller, null, FixedOfElements)], 0, VarOfElements);
        copy.Value = new CobolVarGroup("z", [""], [[new("q", ["rs"])]]);
        Assert.Equal("HabcDEF", caller.Value);
    }

    [Fact]
    public void ADynamicTableOfVariableLengthElements_ReachesAFixedGroupFormal_AndItsStoresOverlay()
    {
        var vg = ManagedPointer<CobolVarGroup>.Cell(new CobolVarGroup("h", [""], [[new("a", ["bcd"]), new("e", ["f"])]]));
        var args = new[] { new CobolArg(CobolPassMode.Reference, vg, null, VarOfElements) };
        var formal = CobolArgAdapt.Text(args, 0, 7, FixedOfElements, static () => "");
        // The first element's IT holds three occurrences, the fixed IF table has two (superfluous "d" not moved); the second
        // element has one, so the fixed table's second occurrence is spaces (§14.6.9.2).
        Assert.Equal("habcef ", formal.Value);
        // A store reaches only the storage the formal overlays: the argument's IT keeps its three occurrences, so "d" survives.
        // The store of "G" into the second element's second IF occurrence is a receiving reference past that element's IT
        // capacity of one, so §8.5.1.9.3 creates the occurrence (kb/Work PB2690).
        formal.Value = "HABCEFG";
        var stored = vg.Value!.ElementCarriersAt(0);
        Assert.Equal("H", vg.Value.Fixed);
        Assert.Equal(2, stored.Length);
        Assert.Equal("A", stored[0].Fixed);
        Assert.Equal(["BCd"], stored[0].Dynamic);
        Assert.Equal("E", stored[1].Fixed);
        Assert.Equal(["FG"], stored[1].Dynamic);
        // A store that leaves the second occurrence as the view showed it (spaces) creates nothing.
        vg.Value = new CobolVarGroup("h", [""], [[new("a", ["bcd"]), new("e", ["f"])]]);
        formal.Value = "HABCEF ";
        Assert.Equal(["F"], vg.Value.ElementCarriersAt(0)[1].Dynamic);
    }

    /// <summary>kb/Work PB2690 — a FIXED table of the formal stored over a dynamic-capacity table of the argument, past the
    /// argument's current capacity. §14.2.3 GR8 makes the formal occupy the argument's storage, so the store is a receiving
    /// reference with a subscript that "exceeds the current capacity of the table" and §8.5.1.9.3 creates the element and
    /// raises the capacity "to the value given by the subscript"; the boundary sees only the finished view, so a store is
    /// recognised by its effect (anything but the space fill the view showed).</summary>
    [Fact]
    public void AFixedTableOfTheFormal_StoredPastTheArgumentsCapacity_CreatesTheOccurrences()
    {
        // The argument 05 L0 X(2) · 05 TD X OCCURS DYNAMIC (capacity 1, "T") · 05 L3 X(2) meets the fixed formal 05 S1 X(2) ·
        // 05 ST X OCCURS 3 · 05 S3 X(2) (the 7-character image): the view shows "CDT  EF", occurrences 2 and 3 space filled.
        var caller = ManagedPointer<CobolVarGroup>.Cell(new CobolVarGroup("CDEF", ["T"]));
        var args = new[] { new CobolArg(CobolPassMode.Reference, caller, null, VarLGAtoms) };
        var formal = CobolArgAdapt.Text(args, 0, 7, FixedSGAtoms, static () => "");
        Assert.Equal("CDT  EF", formal.Value);
        // A store into occurrence 3 only creates occurrences 2 and 3 (the intermediate one space filled) ...
        formal.Value = "CDT zEF";
        Assert.Equal(["T z"], caller.Value!.Dynamic);
        // ... one into occurrence 2 only raises the capacity to 2 over the argument's first ...
        caller.Value = new CobolVarGroup("CDEF", ["T"]);
        formal.Value = "CDTqzEF";
        Assert.Equal(["Tqz"], caller.Value.Dynamic);
        // ... and a callee that leaves the occurrences past the capacity as the view showed them creates nothing, the
        // fixed material around the table still overlaying the argument's.
        caller.Value = new CobolVarGroup("CDEF", ["T"]);
        formal.Value = "ABT  GH";
        Assert.Equal("ABGH", caller.Value.Fixed);
        Assert.Equal(["T"], caller.Value.Dynamic);
    }

    /// <summary>kb/Work PB2690 — the same store over the argument's STORAGE, whose carrier states the element its table
    /// creates (<see cref="CobolVarGroup.Created"/>). §8.5.1.9.5: with INITIALIZED, "any elementary items not referenced as
    /// receiving operands in a statement that creates new elements in that table are first initialized" as by INITIALIZE …
    /// TO VALUE THEN TO DEFAULT, so the view shows each occurrence past the capacity as the table creates it (its NOTE: as
    /// if "physically allocated and, if specified, initialized"), and a store is whatever differs from that. The seed is
    /// per occurrence (a Format 2 VALUE varies by occurrence, CobolDynTable's seedAt), so occurrence 2 is "p" and 3 "q".</summary>
    [Fact]
    public void AStorageTable_ShowsAndCreatesTheOccurrencesPastItsCapacity_AsTheTableCreatesThem()
    {
        static CobolVarGroup Storage() =>
            new("CDEF", ["T"], null, [n => CobolVarGroup.OfImage(n == 2 ? "p" : "q")]);
        var caller = ManagedPointer<CobolVarGroup>.Cell(Storage());
        var args = new[] { new CobolArg(CobolPassMode.Reference, caller, null, VarLGAtoms) };
        var formal = CobolArgAdapt.Text(args, 0, 7, FixedSGAtoms, static () => "");
        Assert.Equal("CDTpqEF", formal.Value);
        // A store into occurrence 3 only: occurrence 2 is created as the table creates it.
        formal.Value = "CDTpzEF";
        Assert.Equal(["Tpz"], caller.Value!.Dynamic);
        // SPACES stored into occurrence 2 are a store (the space-fill test could not see one), and occurrence 3 left as
        // shown is not.
        caller.Value = Storage();
        formal.Value = "CDT qEF";
        Assert.Equal(["T "], caller.Value.Dynamic);
        // A callee that only reads creates nothing.
        caller.Value = Storage();
        formal.Value = "CDTpqEF";
        Assert.Equal(["T"], caller.Value.Dynamic);
    }

    /// <summary>kb/Work PB2690 — the nested arm: a fixed table of fixed elements over a dynamic-capacity table of
    /// variable-length elements of STORAGE. A created element is the table's created element (its own inner table at its
    /// creation, with an inner creation of its own) with the formal's element stored over it, so every item and inner
    /// occurrence the callee did not store keeps the creation (§8.5.1.9.5), and an inner store past the created element's
    /// inner capacity creates there too (§8.5.1.9.3).</summary>
    [Fact]
    public void AStorageTableOfVariableLengthElements_CreatesEachElementFromItsCreation()
    {
        static CobolVarGroup Storage() => new("h", [""], [[new("a", ["bc"])]],
            [_ => new CobolVarGroup("d", ["i"], null, [_ => CobolVarGroup.OfImage("j")])]);
        var caller = ManagedPointer<CobolVarGroup>.Cell(Storage());
        var args = new[] { new CobolArg(CobolPassMode.Reference, caller, null, VarOfElements) };
        var formal = CobolArgAdapt.Text(args, 0, 7, FixedOfElements, static () => "");
        // Element 2 does not exist: the view shows its creation, "d" and its inner table "i" with the inner creation "j".
        Assert.Equal("habcdij", formal.Value);
        formal.Value = "habcdiX";
        var stored = caller.Value!.ElementCarriersAt(0);
        Assert.Equal(2, stored.Length);
        Assert.Equal(("d", "iX"), (stored[1].Fixed, stored[1].Dynamic[0]));
        // A store into element 2's own item only: its inner table keeps its created capacity of one.
        caller.Value = Storage();
        formal.Value = "habcZij";
        stored = caller.Value!.ElementCarriersAt(0);
        Assert.Equal(("Z", "i"), (stored[1].Fixed, stored[1].Dynamic[0]));
        // Reading only creates nothing.
        caller.Value = Storage();
        formal.Value = "habcdij";
        Assert.Single(caller.Value!.ElementCarriersAt(0));
    }

    /// <summary>The creation a storage carrier states is the table's own (kb/Work PB2690): the seed a receiving reference
    /// past the capacity grows with (§8.5.1.9.3) — the INITIALIZED phrase's when it is written (§8.5.1.9.5), else the
    /// initial-state seed — at the occurrence's own number.</summary>
    [Fact]
    public void ATablesCreation_IsTheSeedItsReceivingReferenceGrowsWith_ByOccurrence()
    {
        var initialized = new CobolDynTable<string>(() => "s", 1, null, () => "i");
        Assert.Equal("i", initialized.CreatedAs(static e => e)(5).Fixed);
        initialized.RefReceiving(3);
        Assert.Equal("i", initialized.RefSending(2));
        var keyed = new CobolDynTable<string>(n => "v" + n, 1, null, n => "c" + n, 1);
        Assert.Equal("c3", keyed.CreatedAs(static e => e)(3).Fixed);
        var plain = new CobolDynTable<string>(() => "s", 1, null, null);
        Assert.Equal("s", plain.CreatedAs(static e => e)(2).Fixed);
        Assert.Empty(plain.CreatedAs(static e => e)(2).Dynamic);
    }

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
        var formal = CobolArgAdapt.Text(args, 0, 7, FixedSGAtoms, static () => "");
        // §8.5.1.12.3 sentence 3 + §14.6.9.2: the capacity-2 table fills a 3-occurrence view, the third spaces.
        Assert.Equal("ghkl ij", formal.Value);
        // §14.2.3 GR8 — the formal overlays the argument's storage: the fixed material and the occurrences the
        // argument HAS are stored, and the store into the third, which it has not, is a receiving reference past the
        // table's capacity that §8.5.1.9.3 creates (kb/Work PB2690).
        formal.Value = "qqmnoij";
        Assert.Equal("qqij", vg.Value!.Fixed);
        Assert.Equal(["mno"], vg.Value.Dynamic);
    }

    [Fact]
    public void AFixedFormalOverAWiderTable_LeavesTheOccurrencesPastItsCountUntouched()
    {
        var vg = ManagedPointer<CobolVarGroup>.Cell(new CobolVarGroup("ghij", ["klmn"]));
        var formal = CobolArgAdapt.Text([new CobolArg(CobolPassMode.Reference, vg, null, VarLGAtoms)], 0, 7, FixedSGAtoms, static () => "");
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
        var formal = CobolArgAdapt.Text([new CobolArg(CobolPassMode.Reference, vg, null, VarLGAtoms)], 0, 2, [], static () => "");
        Assert.Equal("gh", formal.Value);
        formal.Value = "PP";
        Assert.Equal("PPij", vg.Value!.Fixed);
        Assert.Equal(["kl"], vg.Value.Dynamic);
    }

    [Fact]
    public void ByContent_TheSameView_IsDetached()
    {
        var vg = ManagedPointer<CobolVarGroup>.Cell(new CobolVarGroup("ghij", ["kl"]));
        var cell = CobolArgAdapt.TextValue([new CobolArg(CobolPassMode.Content, vg, null, VarLGAtoms)], 0, 7, null, 0, FixedSGAtoms, static () => "");
        Assert.Equal("ghkl ij", cell.Value);
        cell.Value = "zzzzzzz";
        Assert.Equal("ghij", vg.Value!.Fixed);   // §14.2.3 GR9 — the callee's stores never reach the argument
    }

    [Fact]
    public void AVariableLengthArgument_IntoANonGroupFormal_StaysLoud()
        // §8.5.1.12.1: a variable-length group is compatible only with a GROUP — an elementary formal states no
        // layout, and the carrier it cannot read fails the activation rather than being reinterpreted.
        => Assert.Throws<CobolCallException>(() => CobolArgAdapt.Text(
            [new CobolArg(CobolPassMode.Reference, ManagedPointer<CobolVarGroup>.Cell(CobolVarGroup.Empty), null, VarLGAtoms)], 0, 7, null, static () => ""));

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
