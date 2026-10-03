// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB1040 — the ONE run-time rule of "do these two boundary items conform" that a dynamic Format-1 CALL applies
/// to a RETURNING pair at call initiation (ISO §14.9.4.4 GR3 d) → §14.8.3.3: the receiving operand "shall have the same
/// ALIGN, BLANK WHEN ZERO, DYNAMIC LENGTH, JUSTIFIED, PICTURE, SIGN, and USAGE clauses"). The end-to-end witness is
/// conformance:2002/pb1040_returning_length_conformance; these pin each clause of the rule on its own, because a golden
/// that raises for the first difference cannot tell WHICH clause raised — a mutant that dropped the sign comparison
/// would still pass it. Every case differs from the baseline in exactly one respect.
/// </summary>
public sealed class BoundaryItemConformanceTests
{
    private static NumProfile Profile(int digits = 5, int scale = 0, bool signed = false,
        NumericSign sign = NumericSign.TrailingOverpunch, NumericTruncation truncation = NumericTruncation.DigitCount,
        NumericByteForm byteForm = NumericByteForm.Zoned) => new()
    {
        Digits = digits, FractionDigits = scale, Signed = signed, SignKind = sign,
        Truncation = truncation, ByteForm = byteForm,
    };

    [Fact]
    public void AnIdenticalProfile_Conforms()
    {
        Assert.True(Profile().ConformsTo(Profile()));
    }

    [Fact]
    public void ThePictureDigitCount_IsAClause()
    {
        Assert.False(Profile(digits: 3).ConformsTo(Profile(digits: 5)));
    }

    [Fact]
    public void TheFractionScale_IsAClause()
    {
        Assert.False(Profile(scale: 2).ConformsTo(Profile(scale: 0)));
    }

    [Fact]
    public void TheOperationalSign_IsAClause()
    {
        Assert.False(Profile(signed: true).ConformsTo(Profile(signed: false)));
    }

    [Fact]
    public void ThePositionOfASign_IsAClauseOnlyWhenTheItemIsSigned()
    {
        Assert.False(Profile(signed: true, sign: NumericSign.LeadingSeparate)
            .ConformsTo(Profile(signed: true, sign: NumericSign.TrailingOverpunch)));
        // An unsigned item has no sign to place: its SignKind is the unused default and never a difference.
        Assert.True(Profile(signed: false, sign: NumericSign.LeadingSeparate)
            .ConformsTo(Profile(signed: false, sign: NumericSign.TrailingOverpunch)));
    }

    [Fact]
    public void TheUsage_IsAClause_ByteFormAndCapacityDiscipline()
    {
        Assert.False(Profile(byteForm: NumericByteForm.Packed).ConformsTo(Profile(byteForm: NumericByteForm.Zoned)));
        Assert.False(Profile(truncation: NumericTruncation.BinaryCapacity)
            .ConformsTo(Profile(truncation: NumericTruncation.DigitCount)));
    }

    [Fact]
    public void TheOverPunchConvention_IsAPropertyOfTheProgram_NotAClause()
    {
        var ibm = Profile();
        var ascii = Profile() with { SignEncoding = SignEncoding.Ascii };
        Assert.True(ibm.ConformsTo(ascii));
    }

    [Fact]
    public void TwoTextItems_ConformExactlyWhenTheirLengthsAreEqual()
    {
        Assert.True(new BoundaryItem(null, 5).Conforms(new BoundaryItem(null, 5)));
        Assert.False(new BoundaryItem(null, 3).Conforms(new BoundaryItem(null, 5)));
        Assert.False(new BoundaryItem(null, 5).Conforms(new BoundaryItem(null, 3)));
    }

    [Fact]
    public void ANumericItem_NeverConformsToACharacterOne()
    {
        // Same character length, different category: §14.8.3.3's PICTURE clause differs.
        var numeric = new BoundaryItem(Profile(digits: 5), 5);
        var character = new BoundaryItem(null, 5);
        Assert.False(numeric.Conforms(character));
        Assert.False(character.Conforms(numeric));
    }

    [Fact]
    public void ANativeNumericCell_IsComparedByItsProfile_AndAnImageCarriedOneByLengthToo()
    {
        var native = new BoundaryItem(Profile(digits: 5));                 // no character length
        var image = new BoundaryItem(Profile(digits: 5), 5);               // image-carried: same PICTURE
        Assert.True(native.Conforms(image));
        Assert.True(image.Conforms(native));
        Assert.False(new BoundaryItem(Profile(digits: 3)).Conforms(image));
    }

    [Fact]
    public void AnItemThatStatesNothing_IsNeverCompared()
    {
        // A pointer, a DYNAMIC LENGTH or ANY LENGTH item, a variable-length group: their conformance is not this
        // registry's, and a rule that refused them would reject conforming programs (§14.8.3.3 rules 4 and 5).
        var nothing = new BoundaryItem(null);
        Assert.False(nothing.IsStated);
        Assert.True(nothing.Conforms(new BoundaryItem(null, 5)));
        Assert.True(new BoundaryItem(Profile(), 5).Conforms(nothing));
        Assert.True(nothing.Conforms(nothing));
    }

    [Fact]
    public void TheCarriedDescription_OfACobolArg_IsItsNumAndLength()
    {
        var arg = new CobolArg(CobolPassMode.Reference, ManagedPointer.Null, Profile(), null, 7);
        Assert.Equal(new BoundaryItem(Profile(), 7), arg.Item);
        Assert.Equal(CobolArg.Unstated, new CobolArg(CobolPassMode.Reference, ManagedPointer.Null, null).Length);
    }
}
