// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE ONE-WIDTH INVARIANT (V59). An item that participates in the record/group character image must occupy
/// THE SAME number of positions there as it occupies in storage — <see cref="DataItem.ImageWidth"/> and
/// <see cref="DataItem.ByteWidth"/> are two views of one fact, never two answers.
/// <para>
/// This is a CLASS-retiring test, not an instance test. The compiler shipped two answers for two whole phases:
/// <c>PicInfo.StorageWidth</c> pinned BINARY at 1-2-4-8 and PACKED at <c>Digits/2+1</c> BCD, <c>ByteWidth</c>
/// documented them and <c>FUNCTION BYTE-LENGTH</c> reported them, while the image used <c>Pic.Digits</c>. For
/// <c>05 G-COMP PIC 9(4) COMP. 05 G-PACK PIC 9(4) COMP-3.</c> the compiler answered BYTE-LENGTH(G) = 5 and
/// LENGTH(G) = 8 and accepted <c>REDEFINES G PIC X(8)</c>. ISO §15.14.4 r1 returns the length in BYTES and
/// §15.50.4 r3 in ALPHANUMERIC CHARACTER POSITIONS; in a single-byte-character model those cannot disagree, and
/// a CONFORMING program observes the disagreement with no file, no REDEFINES and no byte pun at all. Nothing in
/// the battery said so, because nothing compared the two.
/// </para>
/// <para>
/// Scope of the invariant, and why each exclusion is principled rather than convenient:
/// <list type="bullet">
/// <item>NATIONAL is excluded because it is deliberately 2 bytes per character POSITION (D-N1/D-N3) — the two
/// views legitimately differ by exactly that factor, which this test asserts rather than skips.</item>
/// <item>BOOLEAN is excluded because its unit is a boolean position, not a character position (§15.50.4 r1).</item>
/// <item>Everything NOT <see cref="DataItem.IsImageCapable"/> is excluded because it never reaches an image at
/// all — USAGE INDEX is the loud Tier-C island (floats and COMP-5 joined the image in kb/Work PB164 waves
/// 1–2 and are enumerated below). An item outside the image cannot contradict it.</item>
/// </list>
/// </para>
/// </summary>
public sealed class ImageWidthIsStorageWidthTests
{
    private static DataItem Leaf(PicInfo pic) => new() { Level = 5, CobolName = "X", CsName = "X", Pic = pic };

    private static PicInfo Numeric(Usage usage, int digits, int scale = 0, bool signed = false) =>
        new(PicCategory.Numeric, usage, Length: digits, Digits: digits, Scale: scale, Signed: signed);

    /// <summary>Every numeric usage that reaches an image, across the digit counts where the pinned width
    /// table steps (1-2-4-8-16 for the binary forms; every packed byte boundary), signed and unsigned —
    /// including the COMP-5/BINARY-CHAR..DOUBLE family kb/Work PB164 wave 1 admitted and the float family
    /// wave 2 admitted (this enumeration was itself a drifted copy of the retired usage union, so the
    /// ONE-WIDTH invariant was never asserted for them; a float's width ignores the digit axis — asserted
    /// constant across it rather than skipped).</summary>
    public static TheoryData<Usage, int, bool> ImageCapableNumerics()
    {
        var data = new TheoryData<Usage, int, bool>();
        foreach (var usage in new[] { Usage.Display, Usage.Binary, Usage.Packed, Usage.Comp5,
                     Usage.BinaryChar, Usage.BinaryShort, Usage.BinaryLong, Usage.BinaryDouble,
                     Usage.Float, Usage.Double, Usage.FloatShort, Usage.FloatLong, Usage.FloatExtended,
                     Usage.FloatBinary32, Usage.FloatBinary64 })
            foreach (int digits in new[] { 1, 2, 3, 4, 5, 6, 8, 9, 10, 15, 18, 19, 31 })
                foreach (bool signed in new[] { false, true })
                    data.Add(usage, digits, signed);
        return data;
    }

    [Theory]
    [MemberData(nameof(ImageCapableNumerics))]
    public void ImageWidth_EqualsByteWidth_ForEveryImageCapableNumeric(Usage usage, int digits, bool signed)
    {
        var item = Leaf(Numeric(usage, digits, signed: signed));
        if (!item.IsImageCapable) return;   // outside the image entirely — cannot contradict it
        Assert.Equal(item.ByteWidth, item.ImageWidth);
    }

    /// <summary>The pinned widths themselves, stated as a table so a silent change to the representation is a
    /// RED TEST rather than a surprise on disk. These ARE the documented on-disk form (ISO §13.18.60.4 GR4 leaves
    /// BINARY's representation and GR11 PACKED's to the implementor; §4.2.16 obliges us to document the choice).
    /// BINARY = 1-2-4-8 by digit count; PACKED = <c>Digits/2 + 1</c> BCD bytes (sign nibble included).</summary>
    [Theory]
    [InlineData(Usage.Binary, 1, 1)]
    [InlineData(Usage.Binary, 2, 1)]
    [InlineData(Usage.Binary, 3, 2)]
    [InlineData(Usage.Binary, 4, 2)]
    [InlineData(Usage.Binary, 5, 4)]
    [InlineData(Usage.Binary, 9, 4)]
    [InlineData(Usage.Binary, 10, 8)]
    [InlineData(Usage.Binary, 18, 8)]
    // The 16-byte tier: §13.18.60.4 GR4 requires storage SUFFICIENT for the range the picture implies, and a
    // signed 19-digit picture (10^19−1) does not fit 2^63−1. Legal at COBOL-2002+ and stored as Int128.
    [InlineData(Usage.Binary, 19, 16)]
    [InlineData(Usage.Binary, 31, 16)]
    [InlineData(Usage.Packed, 1, 1)]
    [InlineData(Usage.Packed, 2, 2)]
    [InlineData(Usage.Packed, 3, 2)]
    [InlineData(Usage.Packed, 4, 3)]
    [InlineData(Usage.Packed, 9, 5)]
    [InlineData(Usage.Packed, 18, 10)]
    // The IEEE interchange widths (kb/Work PB164 wave 2 — §13.18.60.4 GR13–GR15): 4 bytes binary32,
    // 8 bytes binary64, independent of the (inert) digit axis.
    [InlineData(Usage.Float, 1, 4)]
    [InlineData(Usage.FloatShort, 1, 4)]
    [InlineData(Usage.FloatBinary32, 1, 4)]
    [InlineData(Usage.Double, 1, 8)]
    [InlineData(Usage.FloatLong, 1, 8)]
    [InlineData(Usage.FloatExtended, 1, 8)]
    [InlineData(Usage.FloatBinary64, 1, 8)]
    public void PinnedStorageWidths_AreTheDocumentedOnDiskForm(Usage usage, int digits, int expectedBytes)
    {
        Assert.Equal(expectedBytes, Numeric(usage, digits).StorageWidth);
        Assert.Equal(expectedBytes, Leaf(Numeric(usage, digits)).ByteWidth);
    }

    /// <summary>A DISPLAY leaf is one byte per character position — the two views agree trivially, which is what
    /// makes the BINARY/PACKED disagreement a defect rather than a modelling choice.</summary>
    [Fact]
    public void DisplayLeaf_ImageAndByteWidthsAgree()
    {
        var item = Leaf(Numeric(Usage.Display, 4));
        Assert.Equal(4, item.ImageWidth);
        Assert.Equal(4, item.ByteWidth);
    }

    /// <summary>The R40 INDEX pin's ONE-WIDTH agreement: the picture-less index item occupies its 8
    /// occurrence-number bytes in BOTH views (the same 8 FUNCTION BYTE-LENGTH always reported — item 211).</summary>
    [Fact]
    public void IndexLeaf_ImageAndByteWidthsAgreeAtEight()
    {
        var item = Leaf(PicInfo.IndexItem);
        Assert.Equal(8, item.ImageWidth);
        Assert.Equal(8, item.ByteWidth);
        Assert.True(item.IsImageCapable);
    }

    /// <summary>NATIONAL is the ONE sanctioned divergence and it is exactly 2× (D-N1/D-N3) — asserted, so a
    /// future change cannot quietly widen the exemption.</summary>
    [Fact]
    public void NationalLeaf_IsExactlyTwoBytesPerPosition()
    {
        var item = Leaf(new PicInfo(PicCategory.National, Usage.National, Length: 4, Digits: 0, Scale: 0, Signed: false));
        Assert.Equal(4, item.ImageWidth);
        Assert.Equal(8, item.ByteWidth);
    }

    /// <summary>
    /// ⛔ THE SEED-WIDTH AXIS — the drift test for kb/Work PB188. The one-width invariant above says
    /// <see cref="DataItem.ImageWidth"/> and <see cref="DataItem.ByteWidth"/> agree; this says the third
    /// number, <c>Pic.Length</c> (the PICTURE's character/digit count), DOES NOT — for exactly the leaves
    /// V59/PB164 admitted to the image.
    /// <para>That divergence is what made PB188 possible. A <c>StoreAsImage</c> numeric leaf's storage is a
    /// <c>CharImage(item.ImageWidth)</c> window (<c>StorageFormPass.Classify</c>), and the VALUE seeders used to
    /// hand-spell <c>StrStore(chars, pic.Length)</c>. While every such leaf was zoned DISPLAY the two numbers
    /// were the same and nothing could catch it; once a byte-form leaf could be <c>StoreAsImage</c> the seed
    /// wrote a window of the wrong LENGTH and displaced every following member of the group image. Both
    /// seeders now encode through <c>RuntimeApi.NumFormatImage</c> with the item's own profile, so the width
    /// comes from the pinned byte form by construction — this test states the fact that makes any future
    /// re-introduction of a <c>pic.Length</c>-wide numeric seed wrong.</para>
    /// <para>The last row is the case that shows the axis is not only about COMP: a SIGN SEPARATE zoned DISPLAY
    /// item carries a sign POSITION that its picture's digit count does not (§13.18.52), so digits and stored
    /// characters differ there too.</para>
    /// </summary>
    [Theory]
    [InlineData(Usage.Binary, 4, false, 2)]     // PIC 9(4) COMP        — 4 digits, 2 bytes
    [InlineData(Usage.Comp5, 18, false, 8)]     // PIC 9(18) COMP-5     — 18 digits, 8 bytes
    [InlineData(Usage.Packed, 3, true, 2)]      // PIC S9(3) COMP-3     — 3 digits, 2 bytes
    [InlineData(Usage.Binary, 31, true, 16)]    // PIC S9(31) COMP      — 31 digits, 16 bytes
    [InlineData(Usage.Float, 1, false, 4)]      // COMP-1               — 1 "digit", 4 bytes
    public void ByteFormLeaf_ImageWidthIsNotThePictureLength(Usage usage, int digits, bool signed, int expectedBytes)
    {
        var pic = Numeric(usage, digits, signed: signed);
        var item = Leaf(pic);
        Assert.True(item.IsImageCapable);
        Assert.Equal(expectedBytes, item.ImageWidth);
        Assert.Equal(item.ImageWidth, item.ByteWidth);
        Assert.NotEqual(pic.Length, item.ImageWidth);   // ⛔ the seed width is NOT the picture length
    }

    /// <summary>The SIGN SEPARATE row of the same axis, kept apart because it needs a <c>SignKind</c> rather
    /// than a usage: a zoned DISPLAY item stores its sign in its own character position, so it occupies
    /// <c>Digits + 1</c> positions while its picture length is <c>Digits</c>.</summary>
    [Theory]
    [InlineData("LeadingSeparate")]
    [InlineData("TrailingSeparate")]
    public void SignSeparateDisplayLeaf_ImageWidthIsNotThePictureLength(string signKind)
    {
        var pic = Numeric(Usage.Display, 3, signed: true) with { SignKind = signKind };
        var item = Leaf(pic);
        Assert.Equal(4, item.ImageWidth);
        Assert.Equal(item.ImageWidth, item.ByteWidth);
        Assert.NotEqual(pic.Length, item.ImageWidth);
    }

    /// <summary>A group's two widths compose from its leaves', so the invariant holds transitively — this is the
    /// shape the original defect actually presented as (BYTE-LENGTH(G) = 5 vs LENGTH(G) = 8).</summary>
    [Fact]
    public void MixedUsageGroup_ImageWidthEqualsByteWidth()
    {
        var group = new DataItem { Level = 1, CobolName = "G", CsName = "G" };
        group.Children.Add(Leaf(Numeric(Usage.Binary, 4)));    // 2 bytes
        group.Children.Add(Leaf(Numeric(Usage.Packed, 4)));    // 3 bytes
        group.Children.Add(Leaf(Numeric(Usage.Display, 4)));   // 4 bytes
        Assert.Equal(9, group.ByteWidth);
        Assert.Equal(group.ByteWidth, group.ImageWidth);
    }

    private static PicInfo Alnum(int length) =>
        new(PicCategory.Alphanumeric, Usage.Display, Length: length, Digits: 0, Scale: 0, Signed: false);

    private static PicInfo National(int length) =>
        new(PicCategory.National, Usage.National, Length: length, Digits: 0, Scale: 0, Signed: false);

    private static DataItem Add(DataItem group, DataItem child)
    {
        child.Parent = group;
        group.Children.Add(child);
        return child;
    }

    /// <summary>⛔ kb/Work PB1665 — THE GROUP ARM OF THE ONE-WIDTH INVARIANT. The national leaf is the one
    /// sanctioned divergence between an ELEMENTARY item's two widths (positions vs bytes, asserted above), but an
    /// ALPHANUMERIC group is a string of storage characters (§8.5.2.1; §14.9.25.4 GR4 moves it as an alphanumeric
    /// item), so a national position counts as the two it occupies and the group's ImageWidth IS its ByteWidth —
    /// the quantity every group MOVE, comparison and boundary window is sized by. Counting the leaf once made
    /// <c>MOVE G5 TO G6</c> drop the trailing members.</summary>
    [Fact]
    public void AlphanumericGroupHoldingANationalLeaf_ImageWidthEqualsByteWidth()
    {
        var group = new DataItem { Level = 1, CobolName = "G5", CsName = "G5" };
        Add(group, Leaf(Alnum(2)));
        Add(group, Leaf(National(2)));
        Add(group, Leaf(Alnum(2)));
        Assert.Equal(8, group.ByteWidth);
        Assert.Equal(8, group.ImageWidth);
    }

    /// <summary>A GROUP-USAGE NATIONAL group is as-if PICTURE N(m) (§13.18.29.4 GR2b): m is NATIONAL POSITIONS, so
    /// its ImageWidth stays the position count and only its ByteWidth doubles. A group nested in it is counted the
    /// same way even before the usage-inheritance walk has stamped its own GroupUsage.</summary>
    [Fact]
    public void NationalGroup_ImageWidthIsPositions_AndANestedGroupIsCountedTheSameWay()
    {
        var group = new DataItem { Level = 1, CobolName = "NG", CsName = "NG", GroupUsage = GroupUsage.National };
        Add(group, Leaf(National(2)));
        var inner = new DataItem { Level = 5, CobolName = "IN", CsName = "IN", GroupUsage = GroupUsage.None };
        Add(group, inner);
        Add(inner, Leaf(National(3)));
        Assert.Equal(3, inner.ImageWidth);
        Assert.Equal(5, group.ImageWidth);
        Assert.Equal(10, group.ByteWidth);
    }

    /// <summary>A national group INSIDE an alphanumeric group is an elementary national item to its parent
    /// (§13.18.29.4 GR2b), so it contributes the storage characters its positions occupy.</summary>
    [Fact]
    public void NationalGroupInsideAnAlphanumericGroup_ContributesItsBytes()
    {
        var outer = new DataItem { Level = 1, CobolName = "G", CsName = "G" };
        Add(outer, Leaf(Alnum(1)));
        var ng = new DataItem { Level = 5, CobolName = "NG", CsName = "NG", GroupUsage = GroupUsage.National };
        Add(outer, ng);
        Add(ng, Leaf(National(2)));
        Add(ng, Leaf(National(1)));
        Add(outer, Leaf(Alnum(1)));
        Assert.Equal(3, ng.ImageWidth);
        Assert.Equal(8, outer.ImageWidth);
        Assert.Equal(outer.ByteWidth, outer.ImageWidth);
    }
}
