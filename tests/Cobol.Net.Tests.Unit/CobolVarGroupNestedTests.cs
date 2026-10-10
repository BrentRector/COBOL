// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A DYNAMIC-CAPACITY TABLE WHOSE ELEMENTS ARE VARIABLE-LENGTH GROUPS IS ONE NESTED COMPONENT OF THE §8.5.1.12
/// CARRIER (kb/Work PB2496). Its occurrences ride as their own carriers (<see cref="CobolVarGroup.Elements"/>), so the
/// operations the standard defines over it are element by element: ISO §14.6.9.2 "Correspondingly numbered elements are
/// moved according to the rules of the MOVE statement" (<see cref="CobolVarGroup.Reshape"/>,
/// <see cref="CobolVarGroup.Overlay"/>) and §14.6.9.3's comparison, "each successive remaining element of the larger
/// table with spaces" (<see cref="CobolVarGroup.Compare"/>). Every expected value below is computed from those
/// sentences, never read back from the implementation.
/// </summary>
public sealed class CobolVarGroupNestedTests
{
    // One element: DD dynamic-length · IT one-character dynamic-capacity table (its atoms; element bytes 1 = one IT element).
    private static readonly GroupAtom[] ElemDyn =
        [new(GroupAtomKind.DynamicLength, 0, 0), new(GroupAtomKind.DynamicTable, 1, 1, 1, 1)];
    // The same element with IT a FIXED one-occurrence table (§8.5.1.12.3 sentence 3 makes the two correspond).
    private static readonly GroupAtom[] ElemFixed =
        [new(GroupAtomKind.DynamicLength, 0, 0), new(GroupAtomKind.Table, 1, 1, 1, 1)];
    // H X(1) · TD dynamic-capacity table of such elements · Z X(2)
    private static GroupAtom[] Group(GroupAtom[] elem) =>
        [new(GroupAtomKind.Fixed, 1, 1), new(GroupAtomKind.DynamicTable, 1, 1, 1, 1, elem), new(GroupAtomKind.Fixed, 2, 2)];

    private static CobolVarGroup DynElement(string dd, string it) => new("", [dd, it]);
    private static CobolVarGroup FixedElement(string dd, string it) => new(it, [dd]);
    private static CobolVarGroup Carrier(string h, string z, params CobolVarGroup[] elements) =>
        new(h + z, [""], [elements]);

    [Fact]
    public void Reshape_MovesCorrespondinglyNumberedElements_EachInTheReceiversElementShape()
    {
        var sent = Carrier("h", "zz", DynElement("ab", "pq"), DynElement("c", "r"));
        var got = CobolVarGroup.Reshape(sent, Group(ElemDyn), Group(ElemFixed));
        Assert.Equal("hzz", got.Fixed);
        var elements = got.ElementsAt(0)!;
        Assert.Equal(2, elements.Length);
        // IT's superfluous "q" is not moved into the ONE-occurrence table (§14.6.9.2 rule 1).
        Assert.Equal(("p", "ab"), (elements[0].Fixed, elements[0].Dyn(0)));
        Assert.Equal(("r", "c"), (elements[1].Fixed, elements[1].Dyn(0)));
        // And back: the fixed table's one occurrence becomes a dynamic table of capacity 1.
        var back = CobolVarGroup.Reshape(got, Group(ElemFixed), Group(ElemDyn)).ElementsAt(0)!;
        Assert.Equal(["ab", "p"], back[0].Dynamic);
        Assert.Equal(["c", "r"], back[1].Dynamic);
    }

    [Fact]
    public void Overlay_WritesTheViewsElementsOverTheArguments_KeepingWhatTheViewDoesNotDescribe()
    {
        var argument = Carrier("h", "zz", DynElement("ab", "pq"));
        // The formal (fixed one-occurrence IT) changed IT (1, 1) to "w" and grew the table to two elements.
        var view = Carrier("h", "zz", FixedElement("ab", "w"), FixedElement("new", " "));
        var stored = CobolVarGroup.Overlay(argument, view, Group(ElemDyn), Group(ElemFixed)).ElementsAt(0)!;
        // IT (1, 2) "q" survives: a fixed table of the formal is written over the argument's occurrences, never re-sizing them.
        Assert.Equal(["ab", "wq"], stored[0].Dynamic);
        Assert.Equal(["new", " "], stored[1].Dynamic);
    }

    [Fact]
    public void Compare_GoesElementByElement_AndComparesTheLargerTablesRemainderWithSpaces()
    {
        var shape = Group(ElemDyn);
        var two = Carrier("h", "zz", DynElement("ab", "p"), DynElement("c", "r"));
        // §8.5.1.10.4 and §8.8.4.2.7: "ab" against "ac" decides at the second character, whatever follows.
        Assert.True(CobolVarGroup.Compare(two, shape, Carrier("h", "aa", DynElement("ac", ""), DynElement("", "")), shape) < 0);
        // A third element of spaces only (empty DD, IT one space) compares equal to "nothing" (§14.6.9.3).
        var three = Carrier("h", "zz", DynElement("ab", "p"), DynElement("c", "r"), DynElement("", " "));
        Assert.Equal(0, CobolVarGroup.Compare(two, shape, three, shape));
        var threeX = Carrier("h", "zz", DynElement("ab", "p"), DynElement("c", "r"), DynElement("", "x"));
        Assert.True(CobolVarGroup.Compare(two, shape, threeX, shape) < 0);
        // Element by element, NOT the concatenated images: ("a","b") < ("ab","") although "ab" = "ab".
        Assert.True(CobolVarGroup.Compare(Carrier("h", "", DynElement("a", "b")), shape,
            Carrier("h", "", DynElement("ab", "")), shape) < 0);
    }

    [Fact]
    public void SliceAndConcat_KeepTheElementsBesideTheirComponents()
    {
        var inner = new CobolVarGroup("ab", ["x", ""], [null, [DynElement("d", "e")]]);
        var concat = CobolVarGroup.Concat([inner, new CobolVarGroup("cd", ["y"])]);
        Assert.Equal(["x", "", "y"], concat.Dynamic);
        Assert.Null(concat.ElementsAt(0));
        Assert.Single(concat.ElementsAt(1)!);
        Assert.Null(concat.ElementsAt(2));
        var slice = concat.Slice(0, 2, 1, 1);
        Assert.Equal("d", slice.ElementsAt(0)![0].Dyn(0));
    }

    [Fact]
    public void ANestedComponent_IsNeverReadAsAnEmptyTable_OrAsCharacters()
    {
        // kb/Work PB2690: a fixed-length group meets the nested table through the pair's two shapes (Reshape), so the
        // only way a nested component holds characters and no occurrence carriers is a carrier built wrongly.
        var flat = new CobolVarGroup("hzz", ["abc"]);
        Assert.Throws<InvalidOperationException>(() => flat.ElementCarriersAt(0));
        Assert.Empty(new CobolVarGroup("hzz", [""]).ElementCarriersAt(0));
    }

    [Fact]
    public void Recreate_TakesTheSendersOccurrences_AndFillsToTheMinimumWithTheBlank()
    {
        var table = new CobolDynTable<string>(() => "seed", 3, null, null);
        table.Recreate(["a"], "_", static (_, x) => x);
        Assert.Equal(3, table.Capacity);
        Assert.Equal("a__", table.CurrentImage(static e => e));
        Assert.Equal(["a", "_", "_"], table.CurrentOccurrencesAs(static e => e));
    }
}
