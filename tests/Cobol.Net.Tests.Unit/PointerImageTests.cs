// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using CobolNet.Runtime.Exceptions;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The ONE class-pointer storage-image codec (kb/Work PB970 arm 2; docs/CONFORMANCE.md §7 DOC-A.1-216) and its
/// CALL-boundary consumer. ISO §14.8.2.3.3 1): a program called with no program-specifier and no NESTED phrase
/// requires only that "the formal parameter shall be of the same length as the corresponding argument", and
/// §14.2.3 GR9 moves the argument "to this allocated record without conversion". The end-to-end witness is
/// conformance:2002/pb970_pointer_by_content_storage_image; these pin what the golden cannot see in-process —
/// absolute token shape, the FUNCTION-POINTER category, and the modes that must stay refused.
/// </summary>
public sealed class PointerImageTests
{
    private static long AsBigEndian(string image)
    {
        Assert.Equal(PointerImage.Width, image.Length);
        long v = 0;
        foreach (char c in image) { Assert.True(c <= 0xFF); v = (v << 8) | c; }
        return v;
    }

    [Fact]
    public void Null_OfEveryCategory_IsTheZeroAddress()
    {
        Assert.Equal(PointerImage.NullImage, PointerImage.Of(ManagedPointer.Null));
        Assert.Equal(PointerImage.NullImage, PointerImage.Of((ManagedPointer?)null));
        Assert.Equal(PointerImage.NullImage, PointerImage.Of(ProgramPointer.Null));
        Assert.Equal(PointerImage.NullImage, PointerImage.Of(FunctionPointer.Null));
        Assert.Equal(0, AsBigEndian(PointerImage.NullImage));
    }

    [Fact]
    public void DataPointer_IsAreaBasePlusDisplacement_BigEndian()
    {
        var cell = new StorageCell { Ref = "ABCDEFGH" };
        long at0 = AsBigEndian(PointerImage.Of(ManagedPointer.At(cell, 0)));
        Assert.NotEqual(0, at0);
        Assert.Equal(0, at0 & 0xFFFF_FFFFL);                                    // bases are k × 2^32
        Assert.Equal(at0 + 5, AsBigEndian(PointerImage.Of(CobolPtr.UpBy(ManagedPointer.At(cell, 0), 5))));
        Assert.Equal(PointerImage.Of(ManagedPointer.At(cell, 3)), PointerImage.Of(ManagedPointer.At(cell, 3)));
        // A different area never shares a base.
        long other = AsBigEndian(PointerImage.Of(ManagedPointer.At(new StorageCell { Ref = "ABCDEFGH" }, 0)));
        Assert.NotEqual(at0, other);
    }

    [Fact]
    public void ProgramAndFunctionPointers_AreStablePerName_AndDistinctPerCategory()
    {
        string p1 = PointerImage.Of(new ProgramPointer("PROGA"));
        Assert.Equal(p1, PointerImage.Of(new ProgramPointer("proga")));         // §8.3.2.2 — case-insensitive identity
        Assert.NotEqual(PointerImage.NullImage, p1);
        Assert.NotEqual(p1, PointerImage.Of(new FunctionPointer("PROGA")));     // same name, other category
        Assert.NotEqual(p1, PointerImage.Of(new ProgramPointer("PROGB")));
    }

    [Fact]
    public void ByContent_PointerIntoCharacterOrBinaryFormal_DeliversTheImage_Detached()
    {
        var cell = new StorageCell { Ref = "ABCDEFGH" };
        var slot = ManagedPointer<ManagedPointer>.Cell(ManagedPointer.At(cell, 2));
        var args = new[] { new CobolArg(CobolPassMode.Content, slot, null) };
        var view = CobolArgAdapt.Text(args, 0, 8, null, static () => "");
        Assert.Equal(PointerImage.Of(ManagedPointer.At(cell, 2)), view.Value);
        view.Value = "ZZZZZZZZ";                                                // GR9 — a record of its own
        Assert.True(ManagedPointer.SameTarget(ManagedPointer.At(cell, 2), slot.Value));

        var fslot = ManagedPointer<FunctionPointer>.Cell(new FunctionPointer("F1"));
        Assert.Equal(PointerImage.Of(new FunctionPointer("F1")),
            CobolArgAdapt.Text([new CobolArg(CobolPassMode.Content, fslot, null)], 0, 8, null, static () => "").Value);
    }

    // ── A pointer MEMBER of a shared storage area (kb/Work PB1071): the slot holds the value, the 8 positions hold
    //    the SAME codec's image of it. ISO §14.9.3.4 GR9 (null initial state), §14.9.39.4 GR20 (UP BY n moves the
    //    address by n); the image itself is the implementor's, DOC-A.1-216. ──────────────────────────────────────────

    [Fact]
    public void Allocate_SeedsTheNullImageAtEachPointerMember_AndTheFillElsewhere()
    {
        var p = (CellPointer)CobolPtr.Allocate(20, 'x', out bool na, [2, 11]);
        Assert.False(na);
        Assert.Equal("xx" + PointerImage.NullImage + "x" + PointerImage.NullImage + "x", p.Cell.Ref);
        // The CHARACTERS form (no members) is the plain fill.
        Assert.Equal("xxxx", ((CellPointer)CobolPtr.Allocate(4, 'x', out _)).Cell.Ref);
    }

    [Fact]
    public void SlotWrite_StoresTheValueAndItsImage_AndOnlyItsOwnPositions()
    {
        var area = new StorageCell { Ref = "ab" + PointerImage.NullImage + "cd" };
        var target = new StorageCell { Ref = "ABCDEFGH" };

        CobolPtr.SlotWrite(area, 2, ManagedPointer.At(target, 0));
        Assert.True(ManagedPointer.SameTarget(ManagedPointer.At(target, 0), CobolPtr.SlotRead<ManagedPointer>(area, 2, ManagedPointer.Null)));
        Assert.Equal("ab" + PointerImage.Of(ManagedPointer.At(target, 0)) + "cd", area.Ref);

        CobolPtr.SlotWrite(area, 2, CobolPtr.UpBy(ManagedPointer.At(target, 0), 3));          // GR20: the address moves by 3
        Assert.Equal(AsBigEndian(PointerImage.Of(ManagedPointer.At(target, 3))), AsBigEndian(area.Ref.Substring(2, 8)));

        CobolPtr.SlotWrite(area, 2, ManagedPointer.Null);
        Assert.Equal("ab" + PointerImage.NullImage + "cd", area.Ref);
    }

    [Fact]
    public void SlotWrite_ProgramAndFunctionPointerMembers_StoreTheirImages_AndANullDataPointerIsZero()
    {
        var area = new StorageCell { Ref = PointerImage.NullImage + PointerImage.NullImage };
        CobolPtr.SlotWrite(area, 0, new ProgramPointer("PROGA"));
        CobolPtr.SlotWrite(area, 8, new FunctionPointer("F1"));
        Assert.Equal(PointerImage.Of(new ProgramPointer("PROGA")) + PointerImage.Of(new FunctionPointer("F1")), area.Ref);

        CobolPtr.SlotWrite<ManagedPointer?>(area, 0, null);                                   // a null data-pointer: the zero address
        Assert.Equal(PointerImage.NullImage, area.Ref[..8]);
    }

    [Fact]
    public void SlotWrite_AnObjectReferenceMember_HasNoImage_AndLeavesItsReservedPositions()
    {
        var area = new StorageCell { Ref = "        " };
        CobolPtr.SlotWrite<object?>(area, 0, new object());
        CobolPtr.SlotWrite<object?>(area, 0, null);
        Assert.Equal("        ", area.Ref);
        Assert.Null(PointerImage.OfSlot<object?>(null));
    }

    [Fact]
    public void ByReference_PointerIntoCharacterFormal_StaysRefused()
    {
        // §14.8.2.3.2 — "If either the argument or the formal parameter is of class pointer, the corresponding
        // formal parameter or argument shall be of class pointer": no image is delivered BY REFERENCE.
        var slot = ManagedPointer<ManagedPointer>.Cell(ManagedPointer.Null);
        Assert.Throws<CobolCallException>(() =>
            CobolArgAdapt.Text([new CobolArg(CobolPassMode.Reference, slot, null)], 0, 8, null, static () => ""));
    }
}
