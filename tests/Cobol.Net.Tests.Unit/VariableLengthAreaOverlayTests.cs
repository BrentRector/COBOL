// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A VARIABLE-LENGTH GROUP FORMAL IS LAID OVER ITS ARGUMENT'S AREA ONLY WHERE ITS COMPONENTS ARE THE AREA'S (kb/Work
/// PB2094). ISO §14.2.3 GR8: "If the argument is passed by reference, the activated runtime element operates as if the
/// formal parameter occupies the same storage area as the argument." A dynamic-length item and a dynamic-capacity table
/// are slots of the cell, numbered in storage order, so a description can share an area that holds them only when every
/// one of its components falls on the area's component of the same kind and extent
/// (<see cref="GroupCompatibility.LaysOver"/>); a compatible pair of another shape (§8.5.1.12) takes a fresh area filled
/// through the reshaping carrier. <see cref="CobolArgAdapt.Area(CellPointer, bool, int, GroupAtom[], Func{StorageCell})"/>
/// is the one decision, and these facts pin its every arm.
/// </summary>
public sealed class VariableLengthAreaOverlayTests
{
    private static GroupAtom Fx(int n) => new(GroupAtomKind.Fixed, n, n);

    private static readonly GroupAtom Dyn = new(GroupAtomKind.DynamicLength, 0, 0);

    private static GroupAtom DynTable(int element) => new(GroupAtomKind.DynamicTable, element, element, element, element);

    // 05 H X · 05 D X DYNAMIC LENGTH · 05 T X OCCURS DYNAMIC
    private static readonly GroupAtom[] HDT = [Fx(1), Dyn, DynTable(1)];

    [Fact]
    public void TheSameStorage_LaysOver_HoweverItsFixedMaterialIsDivided()
    {
        Assert.True(GroupCompatibility.LaysOver(HDT, HDT));
        // 05 A X(2) · 05 D … against 05 A1 X · 05 A2 X · 05 D …: one fixed run of two positions either way.
        Assert.True(GroupCompatibility.LaysOver([Fx(1), Fx(1), Dyn], [Fx(2), Dyn]));
    }

    [Fact]
    public void AShorterFixedTail_LaysOver_AndALongerOneDoesNot()
    {
        // LC = XX + dyn + XX over W = XX + dyn + X(4): LC describes W's first positions (§8.5.1.12.2 constrains the
        // positions of the variable-length items only).
        Assert.True(GroupCompatibility.LaysOver([Fx(2), Dyn, Fx(2)], [Fx(2), Dyn, Fx(4)]));
        Assert.False(GroupCompatibility.LaysOver([Fx(2), Dyn, Fx(4)], [Fx(2), Dyn, Fx(2)]));
        // A formal that ends before the area's last component describes a prefix of it.
        Assert.True(GroupCompatibility.LaysOver([Fx(2), Dyn], [Fx(2), Dyn, Fx(4)]));
    }

    [Fact]
    public void AComponentOfAnotherKindOrPlace_DoesNotLayOver()
    {
        // A fixed table opposite a dynamic-capacity one corresponds (§8.5.1.12.3) but is not the same storage.
        Assert.False(GroupCompatibility.LaysOver([Fx(1), Dyn, new(GroupAtomKind.Table, 3, 3, 1, 1)], HDT));
        // A dynamic-length item one position later.
        Assert.False(GroupCompatibility.LaysOver([Fx(2), Dyn], [Fx(1), Dyn, Fx(1)]));
        // Elements of another storage.
        Assert.False(GroupCompatibility.LaysOver([Fx(1), Dyn, DynTable(1) with { Element = [Fx(1)] }],
                                                 [Fx(1), Dyn, DynTable(1) with { Element = [Dyn, Fx(1)] }]));
    }

    [Fact]
    public void AnArgumentArea_IsShared_OnlyWhereTheFormalLaysOverIt()
    {
        var cell = new StorageCell { Ref = new string(' ', 10) };
        var area = new CellPointer(cell, 3) { DynBase = 2, Shape = HDT };
        Assert.Same(area, CobolArgAdapt.Area(area, present: true, 2, HDT, fresh: null));

        // Another shape: a fresh area, seeded by the formal's own factory, stating the formal's shape.
        var fixedTable = new GroupAtom[] { Fx(1), Dyn, new(GroupAtomKind.Table, 1, 1, 1, 1) };
        var seeded = new StorageCell { Ref = "  " };
        var fresh = CobolArgAdapt.Area(area, present: true, 2, fixedTable, () => seeded);
        Assert.NotSame(area, fresh);
        Assert.Same(seeded, fresh.Cell);
        Assert.Same(fixedTable, fresh.Shape);
        Assert.Equal(0, fresh.DynBase);

        // A fixed-length formal (no shape) over a variable-length area would read its components' reserved positions.
        Assert.NotSame(area, CobolArgAdapt.Area(area, present: true, 2, formalShape: null, fresh: null));
        // A variable-length formal over an area that states no shape (a fixed-length argument) holds a copy.
        var plain = new CellPointer(cell, 0);
        Assert.NotSame(plain, CobolArgAdapt.Area(plain, present: true, 2, HDT, fresh: null));
        // Two fixed-length descriptions share by width alone, as before.
        Assert.Same(plain, CobolArgAdapt.Area(plain, present: true, 2, formalShape: null, fresh: null));
    }

    [Fact]
    public void AnOmittedArgument_TakesTheFormalsSeededCell()
    {
        var seeded = new StorageCell { Ref = "  " };
        var omitted = CobolArgAdapt.Area(null, present: false, 2, HDT, () => seeded);
        Assert.True(omitted.IsNull);
        Assert.Same(seeded, omitted.Cell);
    }

    [Fact]
    public void TheOrdinalBase_IsReadOffTheAreasPointer()
    {
        Assert.Equal(4, CobolPtr.DynBaseOf(new CellPointer(new StorageCell(), 0) { DynBase = 4 }));
        Assert.Equal(0, CobolPtr.DynBaseOf(ManagedPointer.Null));
    }
}
